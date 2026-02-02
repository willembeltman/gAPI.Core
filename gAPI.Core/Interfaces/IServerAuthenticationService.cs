using gAPI.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Security.Claims;

namespace gAPI.Interfaces;

public interface IServerAuthenticationService
{
    string SessionId { get; }
    string? UserId { get; }
    string? CookieData { get; }
    bool UpdateCookie { get; }
    AuthenticationInitializeResult Result { get; }

    Task<AuthenticationInitializeResult> InitializeAsync(PathString path, QueryString query, IPAddress? ipAddress, string? cookieData, StringValues sessionData, StringValues stateData, CancellationToken ct);
    Task<AuthenticationInitializeResult> ReInitializeAsync(CancellationToken ct);
    Task<ClaimsPrincipal> GetClaimsPrincipalAsync(CancellationToken ct);
    Task<StringValues> GetStateData(CancellationToken ct);
}
