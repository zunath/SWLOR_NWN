using System.Globalization;
using System.Net;
using System.Text;
using Discord;
using Discord.Net.Rest;
using SWLOR.DiscordBot.Core;

namespace SWLOR.DiscordBot.Discord;

internal sealed class TicketChannelRestClient(string baseUrl, IRestClient fallback, HttpClient channels) : IRestClient
{
    private CancellationToken sessionCancellation;

    internal static RestClientProvider CreateProvider() => baseUrl =>
    {
        var fallback = DefaultRestClientProvider.Instance(baseUrl);
        if (!string.Equals(baseUrl, DiscordConfig.APIUrl, StringComparison.Ordinal)) return fallback;
        return new TicketChannelRestClient(baseUrl, fallback, new HttpClient(CreateChannelHandler()));
    };

    internal static HttpClientHandler CreateChannelHandler() => new()
    {
        AllowAutoRedirect = false, UseCookies = false, UseProxy = false,
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    };

    public void SetHeader(string key, string value)
    {
        fallback.SetHeader(key, value);
        channels.DefaultRequestHeaders.Remove(key);
        if (value is not null) channels.DefaultRequestHeaders.Add(key, value);
    }

    public void SetCancelToken(CancellationToken cancelToken)
    {
        fallback.SetCancelToken(cancelToken);
        sessionCancellation = cancelToken;
    }

    public Task<RestResponse> SendAsync(string method, string endpoint, CancellationToken cancelToken, bool headerOnly = false,
        string? reason = null, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? requestHeaders = null) =>
        fallback.SendAsync(method, endpoint, cancelToken, headerOnly, reason, requestHeaders);

    public Task<RestResponse> SendAsync(string method, string endpoint, IReadOnlyDictionary<string, object> multipartParams,
        CancellationToken cancelToken, bool headerOnly = false, string? reason = null,
        IEnumerable<KeyValuePair<string, IEnumerable<string>>>? requestHeaders = null) =>
        fallback.SendAsync(method, endpoint, multipartParams, cancelToken, headerOnly, reason, requestHeaders);

    public Task<RestResponse> SendAsync(string method, string endpoint, string json, CancellationToken cancelToken,
        bool headerOnly = false, string? reason = null,
        IEnumerable<KeyValuePair<string, IEnumerable<string>>>? requestHeaders = null) =>
        string.Equals(baseUrl, DiscordConfig.APIUrl, StringComparison.Ordinal) && IsChannelCreate(method, endpoint)
            ? SendChannelAsync(endpoint, json, cancelToken, headerOnly, reason, requestHeaders)
            : fallback.SendAsync(method, endpoint, json, cancelToken, headerOnly, reason, requestHeaders);

    private static bool IsChannelCreate(string method, string endpoint)
    {
        var parts = endpoint.Split('/');
        return method == "POST" && parts.Length == 3 && parts[0] == "guilds" && parts[2] == "channels" &&
            ulong.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var guildId) && guildId != 0;
    }

    private async Task<RestResponse> SendChannelAsync(string endpoint, string json, CancellationToken cancelToken,
        bool headerOnly, string? reason, IEnumerable<KeyValuePair<string, IEnumerable<string>>>? requestHeaders)
    {
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(sessionCancellation, cancelToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(baseUrl), endpoint));
        if (reason is not null) request.Headers.Add("X-Audit-Log-Reason", Uri.EscapeDataString(reason));
        if (requestHeaders is not null)
            foreach (var header in requestHeaders) request.Headers.Add(header.Key, header.Value);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpResponseMessage response;
        try
        {
            response = await channels.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation.Token);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is null && ex.HttpRequestError is
            HttpRequestError.NameResolutionError or HttpRequestError.ConnectionError or HttpRequestError.SecureConnectionError)
        {
            // Only the original send can prove connection setup failed before any channel POST was transmitted.
            throw new ChannelCreationNotSentException(ex);
        }

        using (response)
        {
            var headers = response.Headers.ToDictionary(x => x.Key, x => x.Value.FirstOrDefault()!, StringComparer.OrdinalIgnoreCase);
            // Read outside the send catch: even a setup-coded body failure cannot release the durable attempt.
            var stream = !headerOnly || !response.IsSuccessStatusCode
                ? new MemoryStream(await response.Content.ReadAsByteArrayAsync(cancellation.Token), writable: false)
                : null;
            return new RestResponse(response.StatusCode, headers, stream!);
        }
    }

    public void Dispose()
    {
        channels.Dispose();
        fallback.Dispose();
    }
}
