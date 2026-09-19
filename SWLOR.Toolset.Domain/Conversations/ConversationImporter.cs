using System.Text;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Domain.Conversations;

/// <summary>Imports one legacy file into a new graph without modifying existing authored content.</summary>
public static class ConversationImporter
{
    public static ConversationMigrationResult Import(string sourcePath, string destinationPath)
    {
        const string sourceSuffix = ".dlg.json";
        const string destinationSuffix = ".conversation.json";
        if (!sourcePath.EndsWith(sourceSuffix, StringComparison.OrdinalIgnoreCase) ||
            !destinationPath.EndsWith(destinationSuffix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Specify an input .dlg.json file and a new output .conversation.json file.");

        var id = Path.GetFileName(sourcePath)[..^sourceSuffix.Length];
        if (!Path.GetFileName(destinationPath)[..^destinationSuffix.Length].Equals(id, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The output filename must preserve the input conversation ID.");
        if (File.Exists(destinationPath))
            throw new IOException("The SWLOR conversation already exists. Edit it directly; importing cannot overwrite it.");

        var result = DlgConversationMigrator.Convert(id, DlgDocument.Load(sourcePath));
        var errors = result.Issues
            .Where(issue => issue.Severity == ConversationMigrationIssueSeverity.RequiresLegacyException)
            .Select(issue => $"{issue.Location}: {issue.Message}")
            .Concat(ConversationGraphValidator.Validate(result.Graph)).ToArray();
        if (errors.Length > 0)
            throw new InvalidDataException(string.Join(Environment.NewLine, errors));

        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result.Graph, Formatting.Indented));
        // CreateNew also protects against another author creating the graph during conversion.
        using var stream = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        stream.Write(bytes);
        return result;
    }
}
