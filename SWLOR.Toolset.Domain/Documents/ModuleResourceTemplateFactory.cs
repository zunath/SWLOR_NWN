using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Documents
{
    public sealed record ScriptTemplateDefinition(
        string Id,
        string DisplayName,
        string Description,
        string Body);

    /// <summary>
    /// Builds the file content of a brand-new dialog or script, for Module Contents' "New
    /// Dialog…" / "New Script…" actions. Areas have their own writer (they also have a .git and
    /// a .gic to produce) and blueprints have <see cref="BlueprintTemplateFactory"/>; these two are
    /// what is left.
    /// </summary>
    /// <remarks>
    /// The dialog field set is the module corpus's minimum: the root fields every one of the 609
    /// .dlg files carries, which is exactly the shape of the smallest of them (tat_civ1) with one line
    /// of text. A new dialog gets that one entry rather than an empty EntryList, because
    /// StartingList has to point at something - a dialog with no entries is one the engine
    /// cannot start.
    /// </remarks>
    public static class ModuleResourceTemplateFactory
    {
        /// <summary>The line a new dialog opens with, so the file is startable as created.</summary>
        public const string PlaceholderEntryText = "<Enter dialogue here>";

        private const string EmptyTemplateId = "empty";

        public static IReadOnlyList<ScriptTemplateDefinition> ScriptTemplates { get; } =
            new[]
            {
                new ScriptTemplateDefinition(
                    EmptyTemplateId,
                    "Empty",
                    "No-op action script with void main().",
                    "void main()\n{\n}"),
                new ScriptTemplateDefinition(
                    "starting_conditional",
                    "Starting Conditional",
                    "Conversation condition that starts true.",
                    "int StartingConditional()\n{\n    return TRUE;\n}"),
                new ScriptTemplateDefinition(
                    "on_spawn",
                    "OnSpawn",
                    "Creature spawn event stub with the standard NWN AI include.",
                    "#include \"nw_i0_generic\"\n\nvoid main()\n{\n}"),
                new ScriptTemplateDefinition(
                    "on_used",
                    "OnUsed",
                    "Placeable OnUsed event stub with the activating object ready.",
                    "void main()\n{\n    object oUser = GetLastUsedBy();\n    object oSelf = OBJECT_SELF;\n}"),
                new ScriptTemplateDefinition(
                    "on_heartbeat",
                    "OnHeartbeat",
                    "Periodic event stub for module, creature or placeable heartbeat hooks.",
                    "void main()\n{\n    object oSelf = OBJECT_SELF;\n}")
            };

        public static bool Supports(ResourceType type) => type is ResourceType.Dlg or ResourceType.Nss;

        /// <summary>
        /// Converts a display name into an NWN resref: lowercase ASCII alphanumeric/underscore,
        /// trimmed and capped at the engine's 16-character resource-name limit.
        /// </summary>
        public static string ToResRef(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            var characters = name.Trim().ToLowerInvariant()
                .Select(character => char.IsAsciiLetterOrDigit(character) ? character : '_')
                .ToArray();

            var cleaned = string.Join(
                '_',
                new string(characters).Split('_', StringSplitOptions.RemoveEmptyEntries));
            return cleaned.Length > 0
                ? cleaned[..Math.Min(NwnResRef.MaxLength, cleaned.Length)]
                : string.Empty;
        }

        /// <summary>
        /// The new file's bytes: GFF-JSON for a dialog, plain text for a script - see
        /// <see cref="ResourceTypeExtensions.IsJsonEncoded"/>, which .nss is the sole exception to.
        /// </summary>
        public static byte[] CreateFileContent(ResourceType type, string resRef, string displayName) =>
            CreateFileContent(type, resRef, displayName, scriptTemplateId: null);

        public static byte[] CreateFileContent(
            ResourceType type,
            string resRef,
            string displayName,
            string? scriptTemplateId)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                throw new ArgumentException("ResRef must be provided.", nameof(resRef));

            return type switch
            {
                ResourceType.Dlg => CreateDialog(),
                ResourceType.Nss => CreateScript(resRef, displayName, scriptTemplateId),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(type), type, "There is no template for this resource type.")
            };
        }

        private static byte[] CreateDialog()
        {
            // A brand-new dialog is on nobody's undo stack, but the guard is ambient per call
            // context, so with an editor open every field added below would otherwise throw.
            using var construction = Editing.EditScope.EnterConstruction();

            // The unpack pipeline writes an LF body terminated by a single CRLF; matching that keeps a
            // new dialog byte-shaped like every other file in Module\.
            var document = new JsonGffDocument("DLG ", new JsonGffStruct())
            {
                UsesCrLf = false,
                HasTrailingNewline = true,
                TrailingNewlineUsesCrLf = true
            };

            var root = document.Root;
            root.SetUInt("DelayEntry", GffFieldType.Dword, 0);
            root.SetUInt("DelayReply", GffFieldType.Dword, 0);
            root.SetString("EndConverAbort", GffFieldType.ResRef, string.Empty);
            root.SetString("EndConversation", GffFieldType.ResRef, string.Empty);

            var entries = root.GetOrAddList("EntryList");
            var entry = JsonGffField.CreateStruct(0).Struct!;
            entry.SetUInt("Animation", GffFieldType.Dword, 0);
            entry.SetInt("AnimLoop", GffFieldType.Byte, 1);
            entry.SetString("Comment", GffFieldType.CExoString, string.Empty);

            // 0xFFFFFFFF, which is what the corpus uses for "no delay". A real 0 is a zero-second
            // delay, and the engine advances such a line before it can be read.
            entry.SetUInt("Delay", GffFieldType.Dword, uint.MaxValue);
            entry.SetString("Quest", GffFieldType.CExoString, string.Empty);
            entry.GetOrAddList("RepliesList");
            entry.SetString("Script", GffFieldType.ResRef, string.Empty);
            entry.SetString("Sound", GffFieldType.ResRef, string.Empty);
            entry.SetString("Speaker", GffFieldType.CExoString, string.Empty);
            entry.GetOrAddLocString("Text").Text = PlaceholderEntryText;
            entries.Add(entry);

            root.SetUInt("NumWords", GffFieldType.Dword,
                (uint)PlaceholderEntryText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
            root.SetInt("PreventZoomIn", GffFieldType.Byte, 0);
            root.GetOrAddList("ReplyList");

            var starts = root.GetOrAddList("StartingList");
            var start = JsonGffField.CreateStruct(0).Struct!;
            start.SetString("Active", GffFieldType.ResRef, string.Empty);
            start.SetUInt("Index", GffFieldType.Dword, 0);
            starts.Add(start);

            return document.ToBytes();
        }

        public static ScriptTemplateDefinition ScriptTemplateById(string? templateId)
        {
            if (string.IsNullOrWhiteSpace(templateId))
                return ScriptTemplates.Single(t => t.Id == EmptyTemplateId);

            return ScriptTemplates.SingleOrDefault(t =>
                    t.Id.Equals(templateId, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"Unknown script template '{templateId}'.", nameof(templateId));
        }

        private static byte[] CreateScript(string resRef, string displayName, string? templateId)
        {
            var title = string.IsNullOrWhiteSpace(displayName) ? resRef : displayName;
            var template = ScriptTemplateById(templateId);

            // A compilable no-op rather than an empty file: an .nss with no main() does not compile,
            // and a script that fails to compile is worse than one that does nothing.
            return ScriptTextDocument.NewScript(title, template.Body).ToBytes();
        }
    }
}
