using System.Diagnostics;
using System.Threading.Channels;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
	public abstract class SinkNodeBase<TIn> : INode
	{
		private readonly ChannelReader<TIn> input;
		private readonly Action<string>? log;
		private readonly IPipelineFatalErrorReporter? fatalErrorReporter;
		private readonly NodeMetrics metrics;
		private Task? completion;

		protected SinkNodeBase(
			string name,
			ChannelReader<TIn> input,
			Action<string>? log = null,
			IPipelineFatalErrorReporter? fatalErrorReporter = null)
		{
			Name = name;
			this.input = input;
			this.log = log;
			this.fatalErrorReporter = fatalErrorReporter;
			metrics = new NodeMetrics(name);
		}

		public string Name { get; }

		public Task Completion => completion ?? Task.CompletedTask;

		public Task StartAsync(CancellationToken cancellationToken)
		{
			completion ??= Task.Run(() => RunAsync(cancellationToken), CancellationToken.None);
			return Task.CompletedTask;
		}

		public virtual ValueTask StopAsync(CancellationToken cancellationToken)
		{
			return ValueTask.CompletedTask;
		}

		protected abstract ValueTask HandleItemAsync(TIn item, CancellationToken cancellationToken);

		private async Task RunAsync(CancellationToken cancellationToken)
		{
			try
			{
				while (await input.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
				{
					while (input.TryRead(out var item))
					{
						metrics.RecordIn(item);
						metrics.IncrementInFlight();
						var started = Stopwatch.GetTimestamp();
						try
						{
							await HandleItemAsync(item, cancellationToken).ConfigureAwait(false);
						}
						finally
						{
							var elapsed = Stopwatch.GetElapsedTime(started);
							metrics.RecordDuration(elapsed);
							metrics.DecrementInFlight();
						}
					}
				}

				await input.Completion.ConfigureAwait(false);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				// If cancellation races with a faulted upstream completion, prefer propagating the
				// upstream exception so downstream reliably faults rather than completing cleanly.
				if (input.Completion.IsCompleted)
				{
					await input.Completion.ConfigureAwait(false);
				}
			}
			catch (Exception ex)
			{
				fatalErrorReporter?.ReportFatal(Name, ex);
				log?.Invoke($"Node '{Name}' failed: {ex.GetType().Name}: {ex.Message}");
				throw;
			}
			finally
			{
				metrics.Dispose();
			}
		}
	}
}
