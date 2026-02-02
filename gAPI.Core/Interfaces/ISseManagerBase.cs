using gAPI.Sse;

namespace gAPI.Interfaces;

public interface ISseManagerBase : IAsyncDisposable
{
    Task MessageReceivedAsync(SseMessage message, CancellationToken ct);

    Task SubscribeAsync(object implementation);
    Task UnsubscribeAsync(object implementation);
}