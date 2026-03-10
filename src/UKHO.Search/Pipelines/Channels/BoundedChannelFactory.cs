using System.Threading.Channels;

namespace UKHO.Search.Pipelines.Channels
{
    public static class BoundedChannelFactory
    {
        public static CountingChannel<T> Create<T>(int capacity, bool singleReader = false, bool singleWriter = false)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                SingleReader = singleReader,
                SingleWriter = singleWriter,
                FullMode = BoundedChannelFullMode.Wait
            };

            var channel = Channel.CreateBounded<T>(options);
            return new CountingChannel<T>(channel);
        }
    }
}