namespace PennMUSH.Core.Http;

using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public sealed class HttpFetchResult
{
    public bool Ok { get; init; }
    public string Body { get; init; } = string.Empty;
    public int Status { get; init; }
    public string? ContentType { get; init; }
    public bool Truncated { get; init; }
    public string Error { get; init; } = string.Empty;
}

/// <summary>
/// One outbound request for <c>@http</c>. HTTP and HTTPS only, one minute,
/// at most five redirects.
/// </summary>
public sealed class HttpFetch
{
    public const int MaxBody = 8192;

    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? Transport { get; set; }

    public HttpFetchResult Send(string url, string method, string? body, string? contentType, string? userPass)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new HttpFetchResult { Error = "Unsupported protocol." };
        }

        try
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), uri);
            request.Headers.TryAddWithoutValidation("User-Agent", "PennMUSH/1.8");
            request.Headers.TryAddWithoutValidation("Accept-Charset", "utf-8, us-ascii");
            if (!string.IsNullOrEmpty(userPass))
            {
                string token = Convert.ToBase64String(Encoding.UTF8.GetBytes(userPass));
                request.Headers.TryAddWithoutValidation("Authorization", "Basic " + token);
            }

            if (body != null && method is "POST" or "PUT" or "DELETE")
            {
                var content = new StringContent(body);
                if (!string.IsNullOrEmpty(contentType))
                    content.Headers.TryAddWithoutValidation("Content-Type", contentType);
                request.Content = content;
            }

            using var response = Dispatch(request);
            string text = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            bool truncated = text.Length >= MaxBody;
            if (truncated)
                text = text[..(MaxBody - 1)];
            return new HttpFetchResult
            {
                Ok = true,
                Body = text,
                Status = (int)response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.ToString(),
                Truncated = truncated
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return new HttpFetchResult { Error = ex.Message };
        }
    }

    private HttpResponseMessage Dispatch(HttpRequestMessage request)
    {
        if (Transport != null)
            return Transport(request, CancellationToken.None).GetAwaiter().GetResult();

        return Shared.Send(request, CancellationToken.None);
    }

    private static readonly HttpClient Shared = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 5
    })
    {
        Timeout = TimeSpan.FromSeconds(60)
    };
}
