using PipcPaySimplified.Domain.Events;

namespace PicPaySimplified.Infra.Messaging.Configurations;
internal static class EventsMapping
{
    private static Dictionary<string, string> _routingKeys => new()
    {
        { typeof(TransferReceivedEvent).Name, "transfer.received" }
    };

    public static string GetRoutingKey<T>() => _routingKeys[typeof(T).Name];
}
