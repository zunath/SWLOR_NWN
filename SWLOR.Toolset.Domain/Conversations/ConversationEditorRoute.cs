using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Domain.Conversations;

public enum ConversationEditorRouteKind
{
    NuiGraph = 0,
    LegacyDialog = 1,
    LegacyException = 2,
    Missing = 3
}

/// <summary>
/// The document the toolset can honestly open for one conversation. Keeping this decision outside
/// the shell makes the entire module corpus testable without clicking hundreds of Explorer rows.
/// </summary>
public sealed record ConversationEditorRoute(
    ConversationEditorRouteKind Kind,
    string Path,
    string Reason,
    IReadOnlyList<string> Details)
{
    /// <summary>
    /// Every valid authored conversation is editable. Generated graphs use the NUI editor; the
    /// remaining route kinds preserve honest diagnostics for incomplete or external workspaces.
    /// </summary>
    public bool OpensEditor => Kind != ConversationEditorRouteKind.Missing;

    public static ConversationEditorRoute Resolve(
        string conversationId,
        string graphPath,
        string dialogPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conversationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(graphPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(dialogPath);

        if (File.Exists(graphPath))
        {
            return new ConversationEditorRoute(
                ConversationEditorRouteKind.NuiGraph,
                graphPath,
                string.Empty,
                Array.Empty<string>());
        }

        if (!File.Exists(dialogPath))
        {
            return new ConversationEditorRoute(
                ConversationEditorRouteKind.Missing,
                dialogPath,
                "Neither the NUI conversation graph nor the legacy DLG file exists.",
                new[] { graphPath, dialogPath });
        }

        var document = DlgDocument.Load(dialogPath);
        var support = ConversationCompatibility.Check(document);
        if (support.IsSupported)
        {
            return new ConversationEditorRoute(
                ConversationEditorRouteKind.LegacyDialog,
                dialogPath,
                string.Empty,
                Array.Empty<string>());
        }

        var migration = DlgConversationMigrator.Convert(conversationId, document);
        var details = migration.Issues
            .Where(issue => issue.Severity == ConversationMigrationIssueSeverity.RequiresLegacyException)
            .Select(issue => $"{issue.Location}: {issue.Message}")
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new ConversationEditorRoute(
            ConversationEditorRouteKind.LegacyException,
            dialogPath,
            support.Reason,
            details);
    }
}
