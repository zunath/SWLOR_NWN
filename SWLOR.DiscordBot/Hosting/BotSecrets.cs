using Npgsql;

namespace SWLOR.DiscordBot.Hosting;

public sealed record BotSecrets(string Token, string Database)
{
    public static BotSecrets Load()
    {
        var token = ReadRequiredFile("DISCORD_BOT_TOKEN_FILE");
        var connection = Environment.GetEnvironmentVariable("SWLOR_BOT_DATABASE");
        if (string.IsNullOrWhiteSpace(connection))
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = Environment.GetEnvironmentVariable("SWLOR_BOT_DATABASE_HOST") ?? "database",
                Port = int.TryParse(Environment.GetEnvironmentVariable("SWLOR_BOT_DATABASE_PORT"), out var port) ? port : 5432,
                Database = Environment.GetEnvironmentVariable("SWLOR_BOT_DATABASE_NAME") ?? "swlor_bot",
                Username = Environment.GetEnvironmentVariable("SWLOR_BOT_DATABASE_USER") ?? "swlor_bot",
                Password = ReadRequiredFile("SWLOR_BOT_DATABASE_PASSWORD_FILE"),
                Timeout = 15, CommandTimeout = 30, IncludeErrorDetail = false
            };
            connection = builder.ConnectionString;
        }
        // Reject malformed connection settings before hosting, without exposing their values in logs.
        var validated = new NpgsqlConnectionStringBuilder(connection) { IncludeErrorDetail = false };
        return new(token, validated.ConnectionString);
    }
    private static string ReadRequiredFile(string variable)
    {
        var path = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException($"{variable} must identify a mounted secret file.");
        var value = File.ReadAllText(path).Trim();
        if (value.Length == 0) throw new InvalidOperationException($"The secret file for {variable} is empty.");
        return value;
    }
}
