using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using SWLOR.BackgroundServices.Configuration;
using SWLOR.BackgroundServices.Infrastructure;

namespace SWLOR.BackgroundServices.DiscordEvents
{
    /// <summary>
    /// Copies the Discord server's scheduled events (its Events tab) into Redis, where the game server lists them in /events.
    /// The game server reads the snapshot; this service never changes anything on Discord.
    /// </summary>
    public sealed class DiscordEventSync
    {
        // The game server reads this key (HostedEvents.DiscordSnapshotKey).
        public const string SnapshotKey = "swlor:discord:scheduled-events";

        private readonly BackgroundServiceSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly IAppLogger _logger;

        public DiscordEventSync(BackgroundServiceSettings settings, HttpClient httpClient, IAppLogger logger)
        {
            _settings = settings;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_settings.DiscordBotToken) || string.IsNullOrWhiteSpace(_settings.DiscordGuildId))
            {
                _logger.Info("Discord event sync is off: SWLOR_DISCORD_BOT_TOKEN or SWLOR_DISCORD_GUILD_ID is not set.");
                return;
            }

            var redisOptions = ConfigurationOptions.Parse(_settings.RedisConnection);
            redisOptions.AbortOnConnectFail = false;
            using var redis = await ConnectionMultiplexer.ConnectAsync(redisOptions);
            var database = redis.GetDatabase();
            _logger.Info($"Discord event sync reads guild {_settings.DiscordGuildId} every {_settings.DiscordEventSyncInterval.TotalMinutes:0.#} minutes.");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await SyncOnce(database, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // The last good snapshot stays in place, so the game keeps listing what it had.
                    _logger.Error("Discord event sync failed.", ex);
                }

                try
                {
                    await Task.Delay(_settings.DiscordEventSyncInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task SyncOnce(IDatabase database, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_settings.DiscordApiBaseUrl}/guilds/{Uri.EscapeDataString(_settings.DiscordGuildId)}/scheduled-events");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bot", _settings.DiscordBotToken);
            // Discord refuses bot requests without a user agent in this form.
            request.Headers.UserAgent.ParseAdd("DiscordBot (https://github.com/zunath/SWLOR_NWN, 1.0)");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.Error($"Discord event sync got {(int)response.StatusCode} {response.StatusCode}. {Truncate(body)}");
                return;
            }

            var events = JArray.Parse(body);
            var snapshot = new JObject
            {
                ["fetchedUtc"] = DateTime.UtcNow,
                ["events"] = events,
            };
            await database.StringSetAsync(SnapshotKey, snapshot.ToString(Formatting.None));
        }

        private string Truncate(string text)
        {
            return text.Length <= _settings.MaxLogContentLength
                ? text
                : text[.._settings.MaxLogContentLength] + "...";
        }
    }
}
