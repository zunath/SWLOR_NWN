namespace SWLOR.BackgroundServices.Configuration
{
    public sealed class BackgroundServiceSettings
    {
        public string RedisConnection { get; }
        public string ConsumerName { get; }
        public string GitHubToken { get; }
        public bool CodexReviewEnabled { get; }
        public TimeSpan HttpTimeout { get; }
        public TimeSpan EmptyReadDelay { get; }
        public TimeSpan FailureDelay { get; }
        public int BatchSize { get; }
        public int MaxAttempts { get; }
        public int MaxLogContentLength { get; }

        private BackgroundServiceSettings(
            string redisConnection,
            string consumerName,
            string githubToken,
            bool codexReviewEnabled,
            TimeSpan httpTimeout,
            TimeSpan emptyReadDelay,
            TimeSpan failureDelay,
            int batchSize,
            int maxAttempts,
            int maxLogContentLength)
        {
            RedisConnection = redisConnection;
            ConsumerName = consumerName;
            GitHubToken = githubToken;
            CodexReviewEnabled = codexReviewEnabled;
            HttpTimeout = httpTimeout;
            EmptyReadDelay = emptyReadDelay;
            FailureDelay = failureDelay;
            BatchSize = batchSize;
            MaxAttempts = maxAttempts;
            MaxLogContentLength = maxLogContentLength;
        }

        public static BackgroundServiceSettings FromEnvironment()
        {
            return new BackgroundServiceSettings(
                GetString("NWNX_REDIS_HOST", "redis:6379"),
                GetString("SWLOR_BACKGROUND_SERVICE_NAME", "worker-1"),
                Environment.GetEnvironmentVariable("SWLOR_BUG_GITHUB_TOKEN") ?? string.Empty,
                GetBool("SWLOR_BUG_CODEX_REVIEW_ENABLED", false),
                TimeSpan.FromSeconds(GetInt("SWLOR_BACKGROUND_HTTP_TIMEOUT_SECONDS", 60)),
                TimeSpan.FromSeconds(GetInt("SWLOR_BACKGROUND_EMPTY_READ_DELAY_SECONDS", 1)),
                TimeSpan.FromSeconds(GetInt("SWLOR_BACKGROUND_FAILURE_DELAY_SECONDS", 5)),
                GetInt("SWLOR_BACKGROUND_BATCH_SIZE", 10),
                GetInt("SWLOR_BACKGROUND_MAX_ATTEMPTS", 5),
                GetInt("SWLOR_BACKGROUND_MAX_LOG_CONTENT_LENGTH", 2000));
        }

        private static string GetString(string name, string defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value)
                ? defaultValue
                : value;
        }

        private static int GetInt(string name, int defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return int.TryParse(value, out var parsed) && parsed > 0
                ? parsed
                : defaultValue;
        }

        private static bool GetBool(string name, bool defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }
    }
}
