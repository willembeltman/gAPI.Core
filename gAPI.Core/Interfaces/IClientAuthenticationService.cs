namespace gAPI.Interfaces;

public interface IClientAuthenticationService
{
    string SessionId { get; }
    Task<bool> IsAuthenticatedAsync(CancellationToken ct);
    Task<Stream> GetStreamAsync(string url, CancellationToken ct);
    Task<HttpResponseMessage> GetAsync(string path, CancellationToken ct);
    Task<HttpResponseMessage> PostAsync(string path, MultipartFormDataContent content, CancellationToken ct);
    Task<HttpResponseMessage> PutAsync(string path, MultipartFormDataContent content, CancellationToken ct);
    Task<HttpResponseMessage> DeleteAsync(string path, CancellationToken ct);
}