using System.Text.RegularExpressions;
using SWLOR.DiscordBot.Configuration;

namespace SWLOR.DiscordBot.Core;

public static partial class TemplateRenderer
{
    public static string RenderWelcome(string template, ulong userId, string serverName, IReadOnlyDictionary<string, ulong>? channelMentions = null)
    {
        ArgumentNullException.ThrowIfNull(template);
        var rendered = template.Replace("{user}", $"<@{userId}>", StringComparison.Ordinal)
            .Replace("{server}", serverName ?? "", StringComparison.Ordinal);
        return ChannelMacroRegex().Replace(rendered, match =>
        {
            var name = match.Groups[1].Value;
            return channelMentions is not null && channelMentions.TryGetValue(name, out var id) && id != 0 ? $"<#{id}>" : match.Value;
        });
    }

    public static string RenderAnswer(string template, ulong userId, string serverName, IReadOnlyList<string> arguments, string? rawArguments = null)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(arguments);
        return AnswerMacroRegex().Replace(template, match =>
        {
            var macro = match.Groups[1].Value;
            if (macro == "user") return $"<@{userId}>";
            if (macro == "server") return serverName ?? "";
            if (macro == "args") return rawArguments ?? string.Join(' ', arguments);
            var index = macro[0] - '1';
            return index >= 0 && index < arguments.Count ? arguments[index] : "";
        });
    }

    public static string RenderTicket(string template, ulong userId, string serverName) =>
        Truncate(template.Replace("{user}", $"<@{userId}>", StringComparison.Ordinal)
            .Replace("{server}", serverName ?? "", StringComparison.Ordinal), 2000);

    public static CommunityMessage EnforceMessageLimits(CommunityMessage message)
    {
        var content = Truncate(message.Content ?? "", 2000);
        var remaining = 6000;
        var embeds = new List<CommunityEmbed>();
        foreach (var embed in (message.Embeds ?? []).Take(10))
        {
            var title = Take(embed.Title, 256, ref remaining);
            var description = Take(embed.Description, 4096, ref remaining);
            var fields = new List<CommunityEmbedField>();
            foreach (var field in embed.Fields.Take(25))
            {
                if (remaining <= 0) break;
                var name = Take(field.Name, 256, ref remaining) ?? "";
                var value = Take(field.Value, 1024, ref remaining) ?? "";
                if (name.Length > 0 && value.Length > 0) fields.Add(new CommunityEmbedField(name, value, field.Inline));
            }
            if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(description) ||
                fields.Any(field => !string.IsNullOrWhiteSpace(field.Name) && !string.IsNullOrWhiteSpace(field.Value)))
                embeds.Add(new CommunityEmbed(title, description, embed.Url, embed.Color, fields));
        }
        return message with { Content = content, Embeds = embeds };
    }

    public static CommunityEmbed RenderEmbed(AnswerEmbed embed, ulong userId, string serverName, IReadOnlyList<string> arguments, string? rawArguments = null) =>
        new(
            RenderOptional(embed.Title, userId, serverName, arguments, rawArguments),
            RenderOptional(embed.Description, userId, serverName, arguments, rawArguments),
            embed.Url,
            embed.Color,
            (embed.Fields ?? []).Select(field => new CommunityEmbedField(
                RenderAnswer(field.Name, userId, serverName, arguments, rawArguments),
                RenderAnswer(field.Value, userId, serverName, arguments, rawArguments),
                field.Inline)).ToArray());

    private static string? RenderOptional(string? value, ulong userId, string serverName, IReadOnlyList<string> arguments, string? rawArguments = null) =>
        value is null ? null : RenderAnswer(value, userId, serverName, arguments, rawArguments);

    private static string? Take(string? value, int maxLength, ref int remaining)
    {
        if (string.IsNullOrEmpty(value) || remaining <= 0) return null;
        var length = Math.Min(Math.Min(value.Length, maxLength), remaining);
        remaining -= length;
        return Truncate(value, length);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength) return value;
        var length = maxLength;
        if (length > 0 && char.IsHighSurrogate(value[length - 1])) length--;
        return value[..length];
    }

    [GeneratedRegex(@"\{#([A-Za-z0-9_-]+)\}", RegexOptions.CultureInvariant)]
    private static partial Regex ChannelMacroRegex();
    [GeneratedRegex(@"\{(user|server|args|[1-9])\}", RegexOptions.CultureInvariant)]
    private static partial Regex AnswerMacroRegex();
}
