using gAPI.Ids;
using gAPI.Sse;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Channels;

namespace gAPI.Fabric;

public sealed class FabricClient : IAsyncDisposable
{
    private readonly FabricConverter Fc = new();
    private readonly Channel<Action<BinaryWriter>> SendQueue = Channel.CreateUnbounded<Action<BinaryWriter>>();
    private readonly ConcurrentDictionary<SseServiceId, ConcurrentDictionary<SseHostId, SseHost>> Services = new();
    private readonly string? Host;
    private readonly int? Port;
    private readonly CancellationTokenSource SendCts = new();
    private CancellationTokenSource? ReceiveCts;
    private TcpClient? Tcp;
    private NetworkStream? Stream;
    private BinaryReader? Reader;
    private BinaryWriter? Writer;
    private bool FirstTime;
    private bool IsConnecting;
    private bool IsDisconnecting;

    public FabricClient()
    {
    }
    public FabricClient(string host, int port)
    {
        Host = host;
        Port = port;
    }

    public FabricHostId Id { get; set; }
    public bool IsConnected => IsDisconnecting || IsConnecting || Tcp?.Connected == true;

    public async Task ConnectAsync()
    {
        if (Host == null || Port == null) return;
        if (IsConnected || IsConnecting || IsDisconnecting) return;

        try
        {
            Console.WriteLine($"Starting FabricClient");

            IsConnecting = true;

            ReceiveCts = new CancellationTokenSource();
            Tcp = new TcpClient();
            Tcp.Connect(Host, Port.Value);
            Stream = Tcp.GetStream();
            Reader = new BinaryReader(Stream);
            Writer = new BinaryWriter(Stream);

            if (!FirstTime)
            {
                FirstTime = true;
                _ = Task.Run(SendKernel);
            }

            _ = Task.Run(ReceiveKernel);
        }
        finally
        {
            IsConnecting = false;
        }
    }
    private async Task ReconnectAsync(CancellationToken ct)
    {
        Console.WriteLine($"Reconnecting FabricClient ....");
        await DisconnectAsync();
        await ConnectAsync();
        foreach (var service in Services.Values)
        {
            foreach (var sseHost in service.Values)
            {
                //Console.WriteLine(
                //    $"Resubscribe " +
                //    $"SseHost {sseHost.Id} to " +
                //    $"{sseHost.ServiceId} " +
                //    $"(userId {sseHost.UserId}, " +
                //    $"sessionId {sseHost.SessionId})");
                await SubscribeToFabricAsync(sseHost, ct);
            }
        }
        Console.WriteLine($"Reconnecting FabricClient DONE");
    }
    public async Task DisconnectAsync()
    {
        if (IsConnecting) return;

        try
        {
            Console.WriteLine($"Disconnecting FabricClient {Id}");

            IsDisconnecting = true;

            if (ReceiveCts != null)
                await ReceiveCts.CancelAsync();
            ReceiveCts?.Dispose();
            ReceiveCts = null;

            Reader?.Dispose();
            Reader = null;

            Writer?.Dispose();
            Writer = null;

            Stream?.Dispose();
            Stream = null;

            Tcp?.Dispose();
            Tcp = null;
        }
        finally
        {
            IsDisconnecting = false;
        }
    }

    #region Kernels
    private async Task ReceiveKernel()
    {
        if (Reader == null) return;
        if (ReceiveCts == null) return;
        if (Stream == null) return;
        try
        {
            Id = Fc.ReadFabricHostId(Reader!);

            Console.WriteLine($"FabricClient {Id.Value} started");

            while (!ReceiveCts.IsCancellationRequested)
            {
                var t = Fc.ReadHostToClientMessageType(Reader);
                switch (t)
                {
                    case FabricHostToClientMessageEnum.SendSseMessageToClient:
                        var message = new SseMessage(
                            Fc.ReadServiceId(Reader),
                            Fc.ReadServiceMethodId(Reader),
                            Fc.ReadNullableUserId(Reader),
                            Fc.ReadNullableSessionId(Reader),
                            Fc.ReadMessageData(Reader));
                        await SendSseMessageToClientAsync(message, ReceiveCts.Token);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"FabricClient #{Id.Value}: Exception occured, restarting fabric client");
            Console.WriteLine($"{ex}");
            Console.WriteLine();
        }

        await ReconnectAsync(ReceiveCts.Token);
    }
    private async Task SendKernel()
    {
        await foreach (var item in SendQueue.Reader.ReadAllAsync(SendCts.Token))
        {
            while (Writer == null)
            {
                if (SendCts.IsCancellationRequested) return;
                await Task.Delay(10);
            }
            item(Writer);
            Writer.Flush();
        }
    }
    #endregion

    public async Task SubscribeAsync(SseHost sseHost, CancellationToken ct)
    {
        var sseHostsForService = Services.AddOrUpdate(
            sseHost.ServiceId,
            new ConcurrentDictionary<SseHostId, SseHost>(),
            (a, b) => b);
        sseHostsForService[sseHost.Id] = sseHost;
        //Console.WriteLine(
        //    $"Subscribe " +
        //    $"SseHost {sseHost.Id} to " +
        //    $"{sseHost.ServiceId} " +
        //    $"(userId {sseHost.UserId}, " +
        //    $"sessionId {sseHost.SessionId})");

        if (Host == null)
        {
            return;
        }

        await SubscribeToFabricAsync(sseHost, ct);
    }
    public async Task UnsubscribeAsync(SseHost sseHost, CancellationToken ct)
    {
        Services[sseHost.ServiceId].TryRemove(sseHost.Id, out _);
        //Console.WriteLine(
        //    $"Unsubscribe " +
        //    $"SseHost {sseHost.Id} from " +
        //    $"{sseHost.ServiceId} " +
        //    $"(userId {sseHost.UserId}, " +
        //    $"sessionId {sseHost.SessionId})");

        if (Host == null)
        {
            return;
        }

        await UnsubscribeFromFabricAsync(sseHost, ct);
    }
    public async Task PublishAsync(SseServiceId serviceId, SseServiceMethodId serviceMethodId, UserId? userId, SessionId? sessionId, string data, CancellationToken ct)
    {
        //Console.WriteLine(
        //    $"Publish " +
        //    $"FabricClient {Id} to " +
        //    $"{serviceId} " +
        //    $"(userId {userId}, " +
        //    $"sessionId {sessionId})");

        if (Host == null)
        {
            await SendSseMessageToClientAsync(new SseMessage(serviceId, serviceMethodId, userId, sessionId, data), ct);
            return;
        }

        await PublishToFabricAsync(serviceId, serviceMethodId, userId, sessionId, data, ct);
    }

    #region Client => Host
    private async Task SubscribeToFabricAsync(SseHost sseHost, CancellationToken ct)
    {
        await EnqueueAsync(w =>
        {
            Fc.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Subscribe);
            Fc.WriteServiceId(w, sseHost.ServiceId);
            Fc.WriteUserId(w, sseHost.UserId);
            Fc.WriteSessionId(w, sseHost.SessionId);
        }, ct);
    }
    private async Task UnsubscribeFromFabricAsync(SseHost sseHost, CancellationToken ct)
    {
        await EnqueueAsync(w =>
        {
            Fc.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.UnSubscribe);
            Fc.WriteServiceId(w, sseHost.ServiceId);
            Fc.WriteUserId(w, sseHost.UserId);
            Fc.WriteSessionId(w, sseHost.SessionId);
        }, ct);
    }
    private async Task PublishToFabricAsync(SseServiceId serviceId, SseServiceMethodId serviceMethodId, UserId? userId, SessionId? sessionId, string data, CancellationToken ct)
    {
        await EnqueueAsync(w =>
        {
            //Console.WriteLine($"{DateTime.Now:HH:mm:ss.FFF}: FabricClient.Publish (execute)");
            Fc.WriteClientToHostMessageType(w, FabricClientToHostMessageEnum.Publish);
            Fc.WriteServiceId(w, serviceId);
            Fc.WriteServiceMethodId(w, serviceMethodId);
            Fc.WriteNullableUserId(w, userId);
            Fc.WriteNullableSessionId(w, sessionId);
            Fc.WriteMessageData(w, data);
        }, ct);
    }
    private async Task EnqueueAsync(Action<BinaryWriter> write, CancellationToken ct)
    {
        try
        {
            await SendQueue.Writer.WriteAsync(write, ct);
        }
        catch (TaskCanceledException)
        {
        }
    }
    #endregion

    #region Host => Client
    public async Task SendSseMessageToClientAsync(SseMessage message, CancellationToken ct)
    {
        foreach (var sseHost in Services[message.ServiceId].Values)
        {
            try
            {
                await sseHost.Channel.Writer.WriteAsync(
                    new SseEvent("SseMessage", message), ct);
            }
            catch (TaskCanceledException)
            {
            }
        }
    }
    #endregion

    public async ValueTask DisposeAsync()
    {
        Console.WriteLine($"Closing FabricClient {Id}");
        await DisconnectAsync();
        await SendCts.CancelAsync();
        SendCts.Dispose();
    }
}