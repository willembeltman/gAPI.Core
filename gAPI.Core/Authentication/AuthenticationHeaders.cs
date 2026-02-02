using gAPI.Extentions;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace gAPI.Authentication;

public class AuthenticationHeaders(
    PathString path,
    QueryString query,
    IPAddress ipAdress,
    string? cookieData,
    string sessionId)
{
    public PathString Path { get; } = path;
    public QueryString Query { get; } = query;
    public IPAddress IpAdress { get; } = ipAdress;
    public string SessionId { get; set; } = sessionId;
    public string? CookieData { get; private set; } = cookieData;
    public bool UpdateCookie { get; private set; }
    public DateTimeOffset? CookieExpires { get; private set; } = DateTimeOffset.UtcNow.AddDays(7);

    public string? CookieHash
        => CookieData != null ? StringExtentions.HashString(CookieData) : null;
    public string EncodedPath
        => WebUtility.UrlEncode(Path) ?? throw new Exception("Please initialize first");

    public string CreateNewCookie()
    {
        CookieData = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        CookieExpires = DateTimeOffset.UtcNow.AddDays(7);
        UpdateCookie = true;
        return StringExtentions.HashString(CookieData);
    }

    public void RemoveCookie()
    {
        CookieData = null;
        CookieExpires = null;
        UpdateCookie = true;
    }
}

