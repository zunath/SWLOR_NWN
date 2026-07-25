using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Domain.Documents
{
    /// <summary>
    /// Builds the file content of a brand-new conversation or script, for Module Contents' "New
    /// Conversation…" / "New Script…" actions. Areas have their own writer (they also have a .git and
    /// a .gic to produce) and blueprints have <see cref="BlueprintTemplateFactory"/>; these two are
    /// what is left.
    /// </summary>
    /// <remarks>
    /// The conversation field set is the module corpus's minimum: the root fields every one of the 609
    /// .dlg files carries, which is exactly the shape of the smallest of them (tat_civ1) with one line
    /// of text. A new conversation gets that one entry rather than an empty EntryList, because
    /// StartingList has to point at something - a conversation with no entries is one the engine
    /// cannot start.
    /// </remarks>
    public static class ModuleResourceTemplateFactory
    {
        /// <summary>The line a new conversation opens with, so the file is startable as created.</summary>
        public const string PlaceholderEntryText = "<Enter dialogue here>";

        public static bool Supports(ResourceType type) => type is ResourceType.Dlg or ResourceType.Nss;

        /// <summary>
        /// The new file's bytes: GFF-JSON for a conversation, plain text for a script - see
        /// <see cref="ResourceTypeExtensions.IsJsonEncoded"/>, which .nss is the sole exception to.
        /// </summary>
        public static byte[] CreateFileContent(ResourceType type, string resRef, string displayName)
        {
            if (string.IsNullOrWhiteSpace(resRef))
                throw new ArgumentException("ResRef must be provided.", nameof(resRef));

            return type switch
            {
                ResourceType.Dlg => CreateConversation(),
                ResourceType.Nss => CreateScript(resRef, displayName),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(type), type, "There is no template for this resource type.")
            };
        }

        private static byte[] CreateConversation()
        {
            // The unpack pipeline writes an LF body terminated by a single CRLF; matching that keeps a
            // new conversation byte-shaped like every other file in Module\.
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

        private static byte[] CreateScript(string resRef, string displayName)
        {
            var title = string.IsNullOrWhiteSpace(displayName) ? resRef : displayName;

            // A compilable no-op rather than an empty file: an .nss with no main() does not compile,
            // and a script that fails to compile is worse than one that does nothing.
            var source =
                $"// {title}\r\n" +
                "void main()\r\n" +
                "{\r\n" +
                "}\r\n";

            return System.Text.Encoding.UTF8.GetBytes(source);
        }
    }
}
