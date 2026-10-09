using System.Text.Json;
using Npgsql;

namespace SWLOR.DiscordBot.Hosting;

public sealed record BotSecrets(string Token, string Database)
{
    public static BotSecrets Load()
    {
        var path = Environment.GetEnvironmentVariable("SWLOR_BOT_SECRETS_FILE");
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException("SWLOR_BOT_SECRETS_FILE must identify the mounted secrets JSON file.");
        return Load(path);
    }

    public static BotSecrets Load(string path)
    {
        using var document = ReadDocument(path);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("The secrets JSON must be an object.");

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in root.EnumerateObject())
        {
            if (!names.Add(property.Name))
                throw new InvalidOperationException("The secrets JSON must not contain duplicate properties.");
            if (property.Name is not ("discordToken" or "databasePassword" or "databaseConnectionString"))
                throw new InvalidOperationException("The secrets JSON contains an unsupported property.");
        }

        var token = RequiredString(root, "discordToken");
        var password = RequiredString(root, "databasePassword");
        if (token.Any(char.IsWhiteSpace) || token.Contains('\0'))
            throw new InvalidOperationException("discordToken must not contain whitespace or NUL.");
        if (password.IndexOfAny(['\0', '\r', '\n']) >= 0)
            throw new InvalidOperationException("databasePassword must not contain line breaks or NUL.");

        NpgsqlConnectionStringBuilder builder;
        try
        {
            if (root.TryGetProperty("databaseConnectionString", out var connection))
            {
                if (connection.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(connection.GetString()))
                    throw new InvalidOperationException("databaseConnectionString must be a non-empty string when supplied.");
                builder = new NpgsqlConnectionStringBuilder(connection.GetString());
                if (builder.Password is not null)
                    throw new InvalidOperationException("Use databasePassword instead of embedding a password in databaseConnectionString.");
            }
            else
            {
                builder = new NpgsqlConnectionStringBuilder
                {
                    Host = Environment.GetEnvironmentVariable("SWLOR_BOT_DATABASE_HOST") ?? "database",
                    Port = int.TryParse(Environment.GetEnvironmentVariable("SWLOR_BOT_DATABASE_PORT"), out var port) ? port : 5432,
                    Database = Environment.GetEnvironmentVariable("SWLOR_BOT_DATABASE_NAME") ?? "swlor_bot",
                    Username = Environment.GetEnvironmentVariable("SWLOR_BOT_DATABASE_USER") ?? "swlor_bot",
                    Timeout = 15,
                    CommandTimeout = 30
                };
            }
            builder.Password = password;
            builder.IncludeErrorDetail = false;
        }
        catch (ArgumentException)
        {
            // Connection parser errors can include supplied values. Surface only a fixed diagnostic.
            throw new InvalidOperationException("databaseConnectionString contains invalid connection settings.");
        }

        return new(token, builder.ConnectionString);
    }

    private static JsonDocument ReadDocument(string path)
    {
        try { return JsonDocument.Parse(File.ReadAllText(path)); }
        catch (JsonException)
        {
            throw new InvalidOperationException("The secrets file must contain valid JSON.");
        }
    }

    private static string RequiredString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
            throw new InvalidOperationException($"The secrets JSON must contain a non-empty {name} string.");
        return value.GetString()!;
    }
}
