using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Dock.Model.Mvvm.Controls;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// The Properties panel: read-only key/value fields for whichever area or blueprint is
    /// currently selected (from Module Explorer or Search). Utc and Area get a curated summary
    /// (name/tag/appearance/portrait/faction, or name/tag/tileset/dimensions); every other type
    /// falls back to listing every root scalar field verbatim.
    /// </summary>
    public partial class PropertiesViewModel : Tool
    {
        private readonly WorkspaceContext _workspaceContext;
        private readonly AppearanceService? _appearanceService;
        private readonly PortraitService? _portraitService;
        private readonly TlkService? _tlkService;
        private readonly OutputLogService _log;
        private CatalogEntry? _selectedEntry;

        [ObservableProperty]
        private string _selectionTitle = "(nothing selected)";

        public ObservableCollection<PropertyRow> Rows { get; } = new();

        public PropertiesViewModel(
            WorkspaceContext workspaceContext,
            OutputLogService log,
            AppearanceService? appearanceService = null,
            PortraitService? portraitService = null,
            TlkService? tlkService = null)
        {
            _workspaceContext = workspaceContext ?? throw new ArgumentNullException(nameof(workspaceContext));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _appearanceService = appearanceService;
            _portraitService = portraitService;
            _tlkService = tlkService;
            Id = "Properties";
            Title = "Properties";
        }

        /// <summary>Loads and displays the fields of one catalog entry (area or blueprint).</summary>
        public void ShowEntry(CatalogEntry entry)
        {
            var workspace = _workspaceContext.Workspace;
            if (workspace == null)
                return;

            _selectedEntry = entry;

            SelectionTitle = $"{entry.ResourceTypeDisplayName} — {entry.ResRef}";
            Rows.Clear();

            try
            {
                if (entry.ResourceType == ResourceType.Area)
                {
                    var are = AreDocument.Load(
                        workspace.GetResourcePath(ResourceType.Area, entry.ResRef));
                    ShowAreaSummary(are);
                }
                else if (entry.ResourceType == ResourceType.Dlg)
                {
                    ShowConversationSummary(workspace, entry.ResRef);
                }
                else if (entry.ResourceType == ResourceType.Nss)
                {
                    ShowScriptSummary(workspace.GetResourcePath(ResourceType.Nss, entry.ResRef));
                }
                else
                {
                    var document = workspace.LoadBlueprint(entry.ResourceType, entry.ResRef);
                    if (document is UtcDocument utc)
                        ShowUtcSummary(utc);
                    else
                        ShowGenericFallback(document);
                }
            }
            catch (Exception ex)
            {
                Rows.Add(new PropertyRow("Error", ex.Message));
                _log.AppendLine($"Failed to load '{entry.ResRef}' ({entry.ResourceTypeDisplayName}): {ex.Message}");
            }
        }

        /// <summary>Rebuilds the current materialized rows after the active TLK generation changes.</summary>
        public void RefreshTlkLabels()
        {
            if (_selectedEntry != null)
                ShowEntry(_selectedEntry);
        }

        private void ShowConversationSummary(ModuleWorkspace workspace, string resRef)
        {
            var graphPath = workspace.GetConversationGraphPath(resRef);
            if (!File.Exists(graphPath))
            {
                ShowDialogSummary(workspace.GetResourcePath(ResourceType.Dlg, resRef));
                Rows.Add(new PropertyRow("Runtime", "Legacy DLG exception"));
                return;
            }

            var graph = JsonConvert.DeserializeObject<ConversationGraph>(File.ReadAllText(graphPath))
                        ?? throw new InvalidOperationException("The NUI conversation graph is empty.");
            Rows.Add(new PropertyRow("NPC lines", graph.Nodes.Count.ToString(CultureInfo.InvariantCulture)));
            Rows.Add(new PropertyRow("Player choices", graph.Choices.Count.ToString(CultureInfo.InvariantCulture)));
            Rows.Add(new PropertyRow("Opening checks", graph.EntryPoints.Count.ToString(CultureInfo.InvariantCulture)));
            Rows.Add(new PropertyRow("Runtime", "NUI conversation"));
        }

        private void ShowUtcSummary(UtcDocument utc)
        {
            var name = string.Join(" ", new[] {
                    ResolveLocString(utc.FirstName),
                    ResolveLocString(utc.LastName)
                }
                .Where(part => !string.IsNullOrEmpty(part)));

            Rows.Add(new PropertyRow("Name", string.IsNullOrEmpty(name) ? "(unnamed)" : name));
            Rows.Add(new PropertyRow("Tag", utc.Tag ?? string.Empty));
            Rows.Add(new PropertyRow("ResRef", utc.TemplateResRef ?? string.Empty));
            Rows.Add(new PropertyRow("Appearance", ResolveAppearance(utc.AppearanceType)));
            Rows.Add(new PropertyRow("Portrait", ResolvePortrait(utc.PortraitId)));
            Rows.Add(new PropertyRow("Faction", utc.FactionID?.ToString(CultureInfo.InvariantCulture) ?? string.Empty));
        }

        private void ShowAreaSummary(AreDocument are)
        {
            Rows.Add(new PropertyRow("Name", ResolveLocString(are.Name)));
            Rows.Add(new PropertyRow("Tag", are.Tag ?? string.Empty));
            Rows.Add(new PropertyRow("Tileset", are.Tileset ?? string.Empty));
            Rows.Add(new PropertyRow("Dimensions", $"{are.Width ?? 0} x {are.Height ?? 0}"));
        }

        /// <summary>
        /// A dialog's shape: how many lines it has, how many player replies, and where it starts.
        /// </summary>
        /// <remarks>
        /// Loaded directly rather than through <c>LoadBlueprint</c>, which only knows the blueprint
        /// types and threw "Not a blueprint resource type" for every dialog a builder clicked - the
        /// panel showed that exception where a summary belonged.
        /// </remarks>
        private void ShowDialogSummary(string path)
        {
            var root = JsonGffDocument.Load(path).Root;

            Rows.Add(new PropertyRow("Lines", CountOf(root, "EntryList")));
            Rows.Add(new PropertyRow("Player replies", CountOf(root, "ReplyList")));
            Rows.Add(new PropertyRow("Starts at", CountOf(root, "StartingList") + " entry point(s)"));

            // A dialog whose EndConversation script is set does something on exit, which is worth
            // seeing before opening it.
            var onEnd = root.GetOrNull("EndConversation")?.GetString();
            if (!string.IsNullOrWhiteSpace(onEnd))
                Rows.Add(new PropertyRow("On end", onEnd));
        }

        /// <summary>
        /// A script's shape. Scripts are the one module resource that is plain text, so there are no
        /// fields to list - length and the entry points it declares are what there is to say.
        /// </summary>
        private void ShowScriptSummary(string path)
        {
            var text = File.ReadAllText(path);
            var lines = text.Split(NewLine).Length;

            Rows.Add(new PropertyRow("Lines", lines.ToString(CultureInfo.InvariantCulture)));
            Rows.Add(new PropertyRow("Size", $"{text.Length:N0} characters"));

            var isInclude = !text.Contains("void main(", StringComparison.Ordinal) &&
                            !text.Contains("int StartingConditional(", StringComparison.Ordinal);
            Rows.Add(new PropertyRow("Kind", isInclude ? "Include (no entry point)" : "Executable"));
        }

        /// <summary>Line separator for the script line count; only the count matters, so LF alone is enough.</summary>
        private const char NewLine = (char)10;

        private static string CountOf(JsonGffStruct root, string listName) =>
            root.GetOrNull(listName)?.Elements?.Count.ToString(CultureInfo.InvariantCulture) ?? "0";

        private void ShowGenericFallback(GffDocumentBase document)
        {
            foreach (var (name, field) in document.Fields.Entries)
            {
                if (field.Type is GffFieldType.Struct or GffFieldType.List)
                    continue;

                Rows.Add(new PropertyRow(name, DescribeField(field)));
            }
        }

        private string ResolveAppearance(int? appearanceType)
        {
            if (appearanceType == null)
                return string.Empty;

            if (_appearanceService != null)
            {
                try
                {
                    return $"{_appearanceService.Get(appearanceType.Value).DisplayName} ({appearanceType.Value})";
                }
                catch (KeyNotFoundException)
                {
                    // Fall through to the raw id below.
                }
            }

            return appearanceType.Value.ToString(CultureInfo.InvariantCulture);
        }

        private string ResolvePortrait(int? portraitId)
        {
            if (portraitId == null)
                return string.Empty;

            if (_portraitService != null)
            {
                try
                {
                    return $"{_portraitService.Get(portraitId.Value).DisplayName} ({portraitId.Value})";
                }
                catch (KeyNotFoundException)
                {
                    // Fall through to the raw id below.
                }
            }

            return portraitId.Value.ToString(CultureInfo.InvariantCulture);
        }

        private string ResolveLocString(LocString value)
        {
            if (!string.IsNullOrEmpty(value.Text))
                return value.Text;

            return value.StrRef is { } strRef && strRef != uint.MaxValue
                ? _tlkService?.GetString(strRef) ?? string.Empty
                : string.Empty;
        }

        private string DescribeField(JsonGffField field)
        {
            try
            {
                if (field.Type is GffFieldType.CExoString or GffFieldType.ResRef or GffFieldType.Void)
                    return field.GetString();

                if (field.Type == GffFieldType.CExoLocString)
                {
                    var inlineText = field.LocStringEntries?
                        .FirstOrDefault(e => e.LanguageKey == "0")?
                        .GetText();
                    if (!string.IsNullOrEmpty(inlineText))
                        return inlineText;

                    return field.GetLocStringId() is { } strRef && strRef != uint.MaxValue
                        ? _tlkService?.GetString(strRef) ?? string.Empty
                        : string.Empty;
                }

                if (field.Type == GffFieldType.Float)
                    return field.GetSingle().ToString(CultureInfo.InvariantCulture);

                if (field.Type == GffFieldType.Double)
                    return field.GetDouble().ToString(CultureInfo.InvariantCulture);

                if (GffFieldTypeNames.IsNumeric(field.Type))
                    return field.RawValue == null ? string.Empty : Encoding.ASCII.GetString(field.RawValue);

                return string.Empty;
            }
            catch (Exception)
            {
                return "<unreadable>";
            }
        }
    }
}
