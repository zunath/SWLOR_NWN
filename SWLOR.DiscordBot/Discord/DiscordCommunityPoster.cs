using Discord;
using Discord.Net;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SWLOR.DiscordBot.Core;
using SWLOR.DiscordBot.Hosting;

namespace SWLOR.DiscordBot.Discord;

// Discord.Net does not expose enforce_nonce on message creation; keep this narrow REST operation typed.
public sealed class DiscordCommunityPoster(HttpClient http, BotSecrets secrets, TimeProvider clock) : IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private DateTimeOffset nextAllowed;
    private static readonly JsonSerializerOptions JsonOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
    public static string Nonce(string deliveryKey) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(deliveryKey)))[..24];
    private sealed record PostPayload(
        [property: JsonPropertyName("content")] string Content,
        [property: JsonPropertyName("embeds")] EmbedPayload[] Embeds,
        [property: JsonPropertyName("allowed_mentions")] AllowedMentionsPayload AllowedMentions,
        [property: JsonPropertyName("nonce")] string Nonce,
        [property: JsonPropertyName("enforce_nonce")] bool EnforceNonce = true);
    private sealed record AllowedMentionsPayload([property: JsonPropertyName("parse")] string[] Parse);
    private sealed record EmbedPayload(
        [property: JsonPropertyName("title")] string? Title,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("color")] uint Color,
        [property: JsonPropertyName("fields")] FieldPayload[] Fields);
    private sealed record FieldPayload(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("value")] string Value,
        [property: JsonPropertyName("inline")] bool Inline);

    public async Task<ulong> SendAsync(ulong channelId, CommunityMessage message, CancellationToken ct)
    {
        var payload = new PostPayload(message.Content, message.Embeds.Select(embed => new EmbedPayload(embed.Title, embed.Description,
            embed.Url, embed.Color, embed.Fields.Select(field => new FieldPayload(field.Name, field.Value, field.Inline)).ToArray())).ToArray(),
            new([]), Nonce(message.DeliveryKey ?? Guid.NewGuid().ToString("N")));
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await gate.WaitAsync(ct);
        try
        {
            for (var attempt = 0; attempt < 4; attempt++)
            {
                var delay = nextAllowed - clock.GetUtcNow();
                if (delay > TimeSpan.Zero) await Task.Delay(delay, clock, ct);
                using var request = new HttpRequestMessage(HttpMethod.Post, $"https://discord.com/api/v10/channels/{channelId}/messages");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bot", secrets.Token);
                request.Headers.UserAgent.ParseAdd("DiscordBot (https://github.com/zunath/SWLOR_NWN, 1.0)");
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                try
                {
                    using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                    if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining) && remaining.FirstOrDefault() == "0" &&
                        response.Headers.TryGetValues("X-RateLimit-Reset-After", out var resets) &&
                        double.TryParse(resets.FirstOrDefault(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && double.IsFinite(seconds) && seconds >= 0)
                        nextAllowed = clock.GetUtcNow() + TimeSpan.FromSeconds(seconds);
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        using var retry = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
                        var secondsToWait = retry.RootElement.GetProperty("retry_after").GetDouble();
                        if (!double.IsFinite(secondsToWait) || secondsToWait < 0) throw new InvalidDataException("Invalid Discord rate limit response.");
                        nextAllowed = clock.GetUtcNow() + TimeSpan.FromSeconds(secondsToWait);
                        if (attempt < 3) continue;
                    }
                    if ((int)response.StatusCode >= 500 && attempt < 3)
                    { nextAllowed = clock.GetUtcNow() + TimeSpan.FromSeconds(attempt + 1); continue; }
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorCode = await ReadErrorCodeAsync(response, ct);
                        if (errorCode is { } code)
                            throw new HttpException(response.StatusCode, null!, code, "Discord message creation failed.", []);
                        throw new HttpRequestException("Discord message creation failed.", null, response.StatusCode);
                    }
                    using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
                    return ulong.Parse(document.RootElement.GetProperty("id").GetString()!, CultureInfo.InvariantCulture);
                }
                catch (HttpRequestException ex) when (ex.StatusCode is null && attempt < 3)
                { nextAllowed = clock.GetUtcNow() + TimeSpan.FromSeconds(attempt + 1); }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested && attempt < 3)
                { nextAllowed = clock.GetUtcNow() + TimeSpan.FromSeconds(attempt + 1); }
            }
            throw new HttpRequestException("Discord message creation exhausted bounded retries.");
        }
        finally { gate.Release(); }
    }
    private static async Task<DiscordErrorCode?> ReadErrorCodeAsync(HttpResponseMessage response, CancellationToken ct)
    {
        // Preserve only a bounded numeric code. Discord payload text and request credentials must not enter exceptions/logs.
        const int maxErrorBytes = 8192;
        ct.ThrowIfCancellationRequested();
        if (response.Content.Headers.ContentLength > maxErrorBytes) return null;
        await using var input = await response.Content.ReadAsStreamAsync(ct);
        using var body = new MemoryStream();
        var buffer = new byte[1024];
        while (true)
        {
            var read = await input.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, maxErrorBytes - (int)body.Length + 1)), ct);
            ct.ThrowIfCancellationRequested();
            if (read == 0) break;
            if (body.Length + read > maxErrorBytes) return null;
            body.Write(buffer, 0, read);
        }
        ct.ThrowIfCancellationRequested();
        try
        {
            using var document = JsonDocument.Parse(body.GetBuffer().AsMemory(0, (int)body.Length));
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                   document.RootElement.TryGetProperty("code", out var code) &&
                   code.ValueKind == JsonValueKind.Number && code.TryGetInt32(out var value) && value > 0
                ? (DiscordErrorCode)value : null;
        }
        catch (JsonException) { return null; }
    }

    public void Dispose() => gate.Dispose();
}
