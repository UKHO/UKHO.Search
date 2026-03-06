using Azure.Messaging.EventGrid;

namespace UKHO.Aspire.Configuration.Emulator.Messaging.EventGrid
{
    public interface IEventGridEventFactory
    {
        public EventGridEvent Create(string eventType, string dataVersion, BinaryData data);
    }
}