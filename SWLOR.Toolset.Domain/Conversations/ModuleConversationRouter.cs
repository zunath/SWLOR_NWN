using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core;

namespace SWLOR.Toolset.Domain.Conversations;

public sealed record ModuleConversationRoutingIssue(
    string FilePath,
    string ConversationId,
    string Message);

public sealed record ModuleConversationRoutingResult(
    int ReferencesFound,
    int ReferencesUpdated,
    int ReferencesAlreadyRouted,
    IReadOnlyList<ModuleConversationRoutingIssue> Issues);

/// <summary>
/// Routes module objects that reference migrated DLG resources directly into the NUI conversation
/// entry point. Only the event-script value line is changed so large GFF JSON files and their
/// original text encoding are otherwise preserved byte-for-byte.
/// </summary>
public static class ModuleConversationRouter
{
    public const string RouterScript = ScriptName.OnDialogStart;

    private static readonly string[] ResourceDirectories = { "git", "utc", "utp", "utd" };
    private static readonly string[] RouteFields = { "ScriptDialogue", "OnUsed", "OnFailToOpen" };
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    static ModuleConversationRouter()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static ModuleConversationRoutingResult RouteRepository(
        string repositoryRoot,
        IReadOnlySet<string> migratedConversationIds,
        bool applyChanges = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentNullException.ThrowIfNull(migratedConversationIds);

        var migratedIds = migratedConversationIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var issues = new List<ModuleConversationRoutingIssue>();
        var referencesFound = 0;
        var referencesUpdated = 0;
        var referencesAlreadyRouted = 0;

        foreach (var directoryName in ResourceDirectories)
        {
            var directory = Path.Combine(repositoryRoot, "Module", directoryName);
            if (!Directory.Exists(directory))
                continue;

            foreach (var path in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
            {
                var encodedText = ReadEncodedText(path);
                var root = JObject.Parse(encodedText.Text, new JsonLoadSettings
                {
                    LineInfoHandling = LineInfoHandling.Load
                });
                var newline = encodedText.Text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
                var lines = encodedText.Text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
                var fileUpdated = false;

                foreach (var resource in root.DescendantsAndSelf().OfType<JObject>())
                {
                    var conversationId = resource["Conversation"]?["value"]?.Value<string>();
                    if (string.IsNullOrWhiteSpace(conversationId) || !migratedIds.Contains(conversationId))
                        continue;

                    referencesFound++;
                    var routes = RouteFields
                        .Select(field => (Field: field, Value: resource[field]?["value"] as JValue))
                        .Where(route => route.Value?.Type == JTokenType.String)
                        .ToArray();
                    if (routes.Length != 1)
                    {
                        issues.Add(new ModuleConversationRoutingIssue(
                            Path.GetRelativePath(repositoryRoot, path),
                            conversationId,
                            routes.Length == 0
                                ? "The resource has no supported interaction event field."
                                : $"The resource has multiple interaction event fields: {string.Join(", ", routes.Select(route => route.Field))}."));
                        continue;
                    }

                    var routeValue = routes[0].Value!;
                    var currentScript = routeValue.Value<string>() ?? string.Empty;
                    if (currentScript.Equals(RouterScript, StringComparison.OrdinalIgnoreCase))
                    {
                        referencesAlreadyRouted++;
                        continue;
                    }

                    if (routeValue is not IJsonLineInfo lineInfo || !lineInfo.HasLineInfo())
                    {
                        issues.Add(new ModuleConversationRoutingIssue(
                            Path.GetRelativePath(repositoryRoot, path),
                            conversationId,
                            $"The {routes[0].Field} value has no source line information."));
                        continue;
                    }

                    var lineIndex = lineInfo.LineNumber - 1;
                    var oldJsonValue = JsonConvert.SerializeObject(currentScript);
                    var valueIndex = lines[lineIndex].LastIndexOf(oldJsonValue, StringComparison.Ordinal);
                    if (valueIndex < 0)
                    {
                        issues.Add(new ModuleConversationRoutingIssue(
                            Path.GetRelativePath(repositoryRoot, path),
                            conversationId,
                            $"The {routes[0].Field} value could not be located on its source line."));
                        continue;
                    }

                    lines[lineIndex] = lines[lineIndex][..valueIndex] +
                                       JsonConvert.SerializeObject(RouterScript) +
                                       lines[lineIndex][(valueIndex + oldJsonValue.Length)..];
                    referencesUpdated++;
                    fileUpdated = true;
                }

                if (applyChanges && fileUpdated)
                    File.WriteAllText(path, string.Join(newline, lines), encodedText.Encoding);
            }
        }

        return new ModuleConversationRoutingResult(
            referencesFound,
            referencesUpdated,
            referencesAlreadyRouted,
            issues);
    }

    private static EncodedText ReadEncodedText(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var hasUtf8Bom = bytes.Length >= 3 &&
                         bytes[0] == 0xEF &&
                         bytes[1] == 0xBB &&
                         bytes[2] == 0xBF;
        var offset = hasUtf8Bom ? 3 : 0;

        try
        {
            var text = StrictUtf8.GetString(bytes, offset, bytes.Length - offset);
            return new EncodedText(text, new UTF8Encoding(hasUtf8Bom));
        }
        catch (DecoderFallbackException)
        {
            var windows1252 = Encoding.GetEncoding(
                1252,
                EncoderFallback.ExceptionFallback,
                DecoderFallback.ExceptionFallback);
            return new EncodedText(windows1252.GetString(bytes), windows1252);
        }
    }

    private sealed record EncodedText(string Text, Encoding Encoding);
}
