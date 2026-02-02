using gAPI.Fabric;
using gAPI.Ids;
using SpanJson;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using static System.Net.Mime.MediaTypeNames;

namespace gAPI.Sse;

public class SseHost(
    SseHostCollection sseHostCollection,
    FabricClient fabricClient,
    SseServiceId serviceId,
    UserId userId,
    SessionId sessionId)
{
    private byte closed;

    public Channel<SseEvent> Channel { get; } = System.Threading.Channels.Channel.CreateUnbounded<SseEvent>();
    public SseHostId Id { get; private set; }
    public SseServiceId ServiceId { get; } = serviceId;
    public SessionId SessionId { get; } = sessionId;
    public UserId UserId { get; } = userId;

    public async IAsyncEnumerable<SseItem<string>> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken ct)
    {
        Id = sseHostCollection.Add(this);
        //Console.WriteLine($"SseHost {Id} started");
        await fabricClient.SubscribeAsync(this, ct);

        try
        {
            yield return new SseItem<string>(Id.Value.ToString(), "SseHostId");

            while (true)
            {
                SseEvent sseMessage;
                try
                {
                    sseMessage = await Channel.Reader.ReadAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    yield break; // <- GEEN ERROR, normale shutdown
                }
                catch (ChannelClosedException)
                {
                    yield break;
                }

                if (sseMessage.SseMessage == null) continue;
                MemoryStream stream = new MemoryStream();
                await JsonSerializer.Generic.Utf8.SerializeAsync(sseMessage.SseMessage, stream, ct);
                var json = Encoding.ASCII.GetString(stream.ToArray());
                yield return new SseItem<string>(json, "SseMessage");
            }
        }
        finally
        {
            if (Interlocked.Exchange(ref closed, 1) == 0)
            {
                await fabricClient.UnsubscribeAsync(this, ct);
                sseHostCollection.Remove(Id);
            }
        }
    }
}