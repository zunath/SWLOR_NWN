using StackExchange.Redis;

namespace SWLOR.BackgroundServices.BackgroundJobs
{
    public sealed class BackgroundJob
    {
        public RedisValue Id { get; }
        public string Type { get; }
        public string Payload { get; }
        public int Attempt { get; }
        public StreamEntry Entry { get; }

        private BackgroundJob(RedisValue id, string type, string payload, int attempt, StreamEntry entry)
        {
            Id = id;
            Type = type;
            Payload = payload;
            Attempt = attempt;
            Entry = entry;
        }

        public static bool TryCreate(StreamEntry entry, out BackgroundJob? job, out string error)
        {
            var values = entry.Values.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());
            var type = values.GetValueOrDefault("type");
            var payload = values.GetValueOrDefault("payload");

            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(payload))
            {
                job = null;
                error = "Job is missing type or payload.";
                return false;
            }

            job = new BackgroundJob(entry.Id, type, payload, ParseAttempt(values.GetValueOrDefault("attempt")), entry);
            error = string.Empty;
            return true;
        }

        private static int ParseAttempt(string? value)
        {
            return int.TryParse(value, out var attempt)
                ? Math.Max(0, attempt)
                : 0;
        }
    }
}
