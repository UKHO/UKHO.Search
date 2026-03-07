using System.Diagnostics;
using System.Threading.Channels;
using UKHO.Search.Pipelines.Metrics;
using UKHO.Search.Pipelines.Supervision;

namespace UKHO.Search.Pipelines.Nodes
{
	public abstract class NodeBase<TIn, TOut> : INode
	{
		private readonly ChannelReader<TIn> input;
		private readonly ChannelWriter<TOut> output;
		private readonly Action<string>? log;
		private readonly IPipelineFatalErrorReporter? fatalErrorReporter;
		private Task? completion;

		protected NodeMetrics Metrics { get; }

		protected NodeBase(
			string name,
			ChannelReader<TIn> input,
			ChannelWriter<TOut> output,
			Action<string>? log = null,
			IPipelineFatalErrorReporter? fatalErrorReporter = null)
		{
			Name = name;
			this.input = input;
			this.output = output;
			this.log = log;
			this.fatalErrorReporter = fatalErrorReporter;
			Metrics = new NodeMetrics(name);
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

		protected async ValueTask WriteAsync(TOut item, CancellationToken cancellationToken)
		{
			await output.WriteAsync(item, cancellationToken).ConfigureAwait(false);
			Metrics.RecordOut(item);
		}

		protected virtual void CompleteOutputs(Exception? error = null)
		{
			output.TryComplete(error);
		}

		private async Task RunAsync(CancellationToken cancellationToken)
		{
			try
			{
				while (await input.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
				{
					while (input.TryRead(out var item))
					{
						Metrics.RecordIn(item);
						Metrics.IncrementInFlight();
						var started = Stopwatch.GetTimestamp();
						try
						{
							await HandleItemAsync(item, cancellationToken).ConfigureAwait(false);
						}
						finally
						{
							var elapsed = Stopwatch.GetElapsedTime(started);
							Metrics.RecordDuration(elapsed);
							Metrics.DecrementInFlight();
						}
					}
				}

				await input.Completion.ConfigureAwait(false);

				CompleteOutputs();
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				// If cancellation races with a faulted upstream completion, prefer propagating the
				// upstream exception so downstream reliably faults rather than completing cleanly.
				if (input.Completion.IsCompleted)
				{
					await input.Completion.ConfigureAwait(false);
				}

				CompleteOutputs();
			}
			catch (Exception ex)
			{
				log?.Invoke($"Node '{Name}' failed: {ex.GetType().Name}: {ex.Message}");
				CompleteOutputs(ex);
				fatalErrorReporter?.ReportFatal(Name, ex);
				throw;
			}
			finally
			{
				Metrics.Dispose();
			}
		}
	}
}
