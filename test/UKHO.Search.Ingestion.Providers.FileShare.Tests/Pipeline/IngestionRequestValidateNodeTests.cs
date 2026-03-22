using Shouldly;
using UKHO.Search.Ingestion.Providers.FileShare.Pipeline.Nodes;
using UKHO.Search.Ingestion.Requests;
using UKHO.Search.Pipelines.Channels;
using UKHO.Search.Pipelines.Errors;
using UKHO.Search.Pipelines.Messaging;
using Xunit;

namespace UKHO.Search.Ingestion.Tests.Pipeline
{
    public sealed class IngestionRequestValidateNodeTests
    {
        [Fact]
        public async Task When_request_is_invalid_it_is_marked_failed_and_routed_to_deadletter()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);

            var node = new IngestionRequestValidateNode("validate", input.Reader, output.Writer, deadLetter.Writer);

            await node.StartAsync(CancellationToken.None);

            var invalid = new IngestionRequest
            {
                RequestType = IngestionRequestType.IndexItem,
                IndexItem = null,
                DeleteItem = null,
                UpdateAcl = null
            };

            await input.Writer.WriteAsync(new Envelope<IngestionRequest>("doc-1", invalid));
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            output.Reader.TryRead(out var _)
                  .ShouldBeFalse();

            deadLetter.Reader.TryRead(out var deadLetterEnvelope)
                      .ShouldBeTrue();
            deadLetterEnvelope.Status.ShouldBe(MessageStatus.Failed);
            deadLetterEnvelope.Error.ShouldNotBeNull();
            deadLetterEnvelope.Error!.Category.ShouldBe(PipelineErrorCategory.Validation);
        }

        [Fact]
        public async Task When_request_is_valid_it_is_forwarded_to_main_output()
        {
            var input = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);
            var output = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);
            var deadLetter = BoundedChannelFactory.Create<Envelope<IngestionRequest>>(1, true, true);

            var node = new IngestionRequestValidateNode("validate", input.Reader, output.Writer, deadLetter.Writer);

            await node.StartAsync(CancellationToken.None);

            var add = new IndexRequest("doc-1", Array.Empty<IngestionProperty>(), new[] { "t1" }, DateTimeOffset.UnixEpoch, new IngestionFileList());
            var request = new IngestionRequest(IngestionRequestType.IndexItem, add, null, null);

            await input.Writer.WriteAsync(new Envelope<IngestionRequest>("doc-1", request));
            input.Writer.TryComplete();

            await node.Completion.WaitAsync(TimeSpan.FromSeconds(2));

            output.Reader.TryRead(out var okEnvelope)
                  .ShouldBeTrue();
            okEnvelope.Status.ShouldBe(MessageStatus.Ok);

            deadLetter.Reader.TryRead(out var _)
                      .ShouldBeFalse();
        }
    }
}