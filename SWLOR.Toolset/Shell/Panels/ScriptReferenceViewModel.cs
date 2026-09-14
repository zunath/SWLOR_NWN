using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Script;
using SWLOR.Toolset.Domain.Script.Symbols;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>Which half of the reference is showing.</summary>
    public enum ScriptReferenceSection
    {
        Functions,
        Constants
    }

    /// <summary>One of the panel's two tabs, with its own total.</summary>
    public sealed partial class ScriptReferenceTabViewModel : ObservableObject
    {
        public ScriptReferenceTabViewModel(ScriptReferenceSection section, string label)
        {
            Section = section;
            Label = label;
        }

        public ScriptReferenceSection Section { get; }

        public string Label { get; }

        [ObservableProperty]
        private int _count;

        [ObservableProperty]
        private bool _isSelected;
    }

    /// <summary>A row in the reference tree: a group header, or a symbol under it.</summary>
    public sealed partial class ReferenceNodeViewModel : ObservableObject
    {
        public ReferenceNodeViewModel(string label, int count)
        {
            Label = label;
            Count = count;
            IsCategory = true;
        }

        public ReferenceNodeViewModel(ScriptFunction function)
        {
            Label = function.Name;
            Function = function;
            Detail = function.ReturnType;
        }

        public ReferenceNodeViewModel(ScriptConstant constant)
        {
            Label = constant.Name;
            Constant = constant;
            Detail = constant.Value;
        }

        public string Label { get; }

        public string? Detail { get; }

        public int Count { get; }

        public bool IsCategory { get; }

        public ScriptFunction? Function { get; }

        public ScriptConstant? Constant { get; }

        public bool IsSymbol => !IsCategory;

        [ObservableProperty]
        private bool _isExpanded;

        [ObservableProperty]
        private bool _isAutoExpanded;

        public bool IsEffectivelyExpanded => IsExpanded || IsAutoExpanded;

        public string Twisty => IsCategory ? IsEffectivelyExpanded ? "▾" : "▸" : string.Empty;

        partial void OnIsExpandedChanged(bool value)
        {
            OnPropertyChanged(nameof(IsEffectivelyExpanded));
            OnPropertyChanged(nameof(Twisty));
        }

        partial void OnIsAutoExpandedChanged(bool value)
        {
            OnPropertyChanged(nameof(IsEffectivelyExpanded));
            OnPropertyChanged(nameof(Twisty));
        }

        /// <summary>What Insert at cursor writes: a call skeleton, or the constant's name.</summary>
        public string InsertText => Function?.CallSkeleton ?? Constant?.Name ?? Label;

        public string Documentation =>
            Function != null
                ? string.Join("\n\n", new[] { Function.Signature, Function.Summary, Function.ReturnsOnError }
                    .Where(s => !string.IsNullOrWhiteSpace(s)))
                : Constant != null
                    ? $"{Constant.Type} {Constant.Name} = {Constant.Value}"
                    : Label;
    }

    /// <summary>
    /// The Aurora-parity reference browser: every engine function and constant, categorised,
    /// searchable, with a description pane, insert-at-cursor and a Lexicon link.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Functions and constants are separate tabs</b>, not one tree with a "Constants" branch at the
    /// bottom. They are different kinds of thing looked up for different reasons, and the counts are
    /// wildly lopsided — 1,187 functions against 6,201 constants — so a single list buried the
    /// functions under a branch that dwarfed everything above it. Tabs also let each half carry its
    /// own total, which is the one thing worth knowing about the half you are not looking at. Same
    /// pattern the Module Contents panel already uses for Areas/Dialogs/Scripts.
    /// </para>
    /// <para>
    /// Docks beside the Palette and takes focus when a script tab becomes active — the Palette lists
    /// the front <i>area's</i> tileset, so it has nothing to offer while a script is in front.
    /// </para>
    /// <para>
    /// Search filters <b>within</b> groups rather than flattening to a list. That keeps the Aurora
    /// mental model ("effects are over there") while being far faster than scrolling it, which is the
    /// actual reason people stopped using the original.
    /// </para>
    /// </remarks>
    public partial class ScriptReferenceViewModel : Tool
    {
        private readonly ScriptLanguageService _language;
        private readonly IExternalLinkService? _links;

        private List<ReferenceNodeViewModel> _functionCategories = new();
        private List<ReferenceNodeViewModel> _constantFamilies = new();
        private Dictionary<string, IReadOnlyList<ScriptConstant>> _constantsByFamily = new(StringComparer.Ordinal);
        private bool _built;

        public ScriptReferenceViewModel(ScriptLanguageService language, IExternalLinkService? links = null)
        {
            _language = language;
            _links = links;
            Id = "ScriptReference";
            Title = "Script Reference";

            Tabs.Add(new ScriptReferenceTabViewModel(ScriptReferenceSection.Functions, "Functions") { IsSelected = true });
            Tabs.Add(new ScriptReferenceTabViewModel(ScriptReferenceSection.Constants, "Constants"));
        }

        public ObservableCollection<ScriptReferenceTabViewModel> Tabs { get; } = new();

        public ObservableCollection<ReferenceNodeViewModel> Rows { get; } = new();

        [ObservableProperty]
        private ScriptReferenceSection _selectedSection = ScriptReferenceSection.Functions;

        [ObservableProperty]
        private string _filter = string.Empty;

        [ObservableProperty]
        private ReferenceNodeViewModel? _selectedRow;

        [ObservableProperty]
        private string _symbolCountLabel = string.Empty;

        /// <summary>Set by the shell to the active script editor's insertion point, or null.</summary>
        public Action<string>? InsertTarget { get; set; }

        public bool CanInsert => InsertTarget != null && SelectedRow?.IsSymbol == true;

        /// <summary>Whether the selection has a Lexicon page to open.</summary>
        public bool CanOpenLexicon => _links != null && ScriptLexicon.IsLinkableName(SelectedRow?.Label);

        public string Documentation => SelectedRow?.Documentation ?? string.Empty;

        /// <summary>Search watermark, which names what this tab actually searches.</summary>
        public string FilterWatermark =>
            SelectedSection == ScriptReferenceSection.Functions ? "Search functions..." : "Search constants...";

        /// <summary>Builds both trees on first show; the header parse is lazy behind this.</summary>
        public void EnsureBuilt()
        {
            if (_built)
                return;

            _built = true;
            var engine = _language.Engine;

            _functionCategories = engine.CategoryCounts()
                .Select(c => new ReferenceNodeViewModel(c.Category, c.Count))
                .ToList();

            _constantsByFamily = engine.Constants
                .GroupBy(engine.ConstantFamilyOf, StringComparer.Ordinal)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ScriptConstant>)g.ToList(), StringComparer.Ordinal);

            _constantFamilies = _constantsByFamily
                .Select(pair => new ReferenceNodeViewModel(pair.Key, pair.Value.Count))
                .ToList();

            Tabs[0].Count = engine.Functions.Count;
            Tabs[1].Count = engine.Constants.Count;

            Rebuild();
        }

        [RelayCommand]
        private void SelectTab(ScriptReferenceTabViewModel? tab)
        {
            if (tab == null || tab.Section == SelectedSection)
                return;

            SelectedSection = tab.Section;
        }

        partial void OnSelectedSectionChanged(ScriptReferenceSection value)
        {
            foreach (var tab in Tabs)
                tab.IsSelected = tab.Section == value;

            // The filter is per-section: a term that matched functions almost never matches constants,
            // and carrying it across makes a freshly-picked tab look empty.
            SelectedRow = null;
            OnPropertyChanged(nameof(FilterWatermark));
            Filter = string.Empty;
            Rebuild();
        }

        partial void OnFilterChanged(string value) => Rebuild();

        partial void OnSelectedRowChanged(ReferenceNodeViewModel? value)
        {
            OnPropertyChanged(nameof(Documentation));
            OnPropertyChanged(nameof(CanInsert));
            OnPropertyChanged(nameof(CanOpenLexicon));
            InsertCommand.NotifyCanExecuteChanged();
            OpenLexiconCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void Toggle(ReferenceNodeViewModel? row)
        {
            if (row is not { IsCategory: true })
                return;

            row.IsExpanded = !row.IsExpanded;
            Rebuild();
        }

        [RelayCommand(CanExecute = nameof(CanInsert))]
        private void Insert()
        {
            if (SelectedRow?.IsSymbol == true)
                InsertTarget?.Invoke(SelectedRow.InsertText);
        }

        /// <summary>
        /// Opens the community Lexicon's page for the selected symbol. Linked rather than bundled —
        /// the Lexicon is GFDL, so a copy would carry licence obligations and go stale.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanOpenLexicon))]
        private void OpenLexicon()
        {
            if (ScriptLexicon.UrlFor(SelectedRow?.Label) is { } url)
                _links?.Open(url);
        }

        /// <summary>Called by the shell when the active document changes.</summary>
        public void SetInsertTarget(Action<string>? target)
        {
            InsertTarget = target;
            OnPropertyChanged(nameof(CanInsert));
            InsertCommand.NotifyCanExecuteChanged();
        }

        private void Rebuild()
        {
            Rows.Clear();
            if (!_built)
                return;

            var filter = Filter?.Trim() ?? string.Empty;
            var filtering = filter.Length > 0;
            var groups = SelectedSection == ScriptReferenceSection.Functions
                ? _functionCategories
                : _constantFamilies;

            var shown = 0;

            foreach (var group in groups)
            {
                var children = ChildrenOf(group.Label, filter, filtering);

                // While filtering, a group with no surviving children is hidden entirely and the rest
                // auto-expand — otherwise a search looks like it found nothing.
                if (filtering && children.Count == 0)
                {
                    group.IsAutoExpanded = false;
                    continue;
                }

                group.IsAutoExpanded = filtering;
                Rows.Add(group);
                shown += children.Count;

                if (!group.IsEffectivelyExpanded)
                    continue;

                foreach (var child in children)
                    Rows.Add(child);
            }

            var total = SelectedSection == ScriptReferenceSection.Functions ? Tabs[0].Count : Tabs[1].Count;
            SymbolCountLabel = filtering
                ? $"{shown:N0} of {total:N0} shown"
                : $"{total:N0} total";
        }

        private IReadOnlyList<ReferenceNodeViewModel> ChildrenOf(string group, string filter, bool filtering)
        {
            if (SelectedSection == ScriptReferenceSection.Constants)
            {
                if (!_constantsByFamily.TryGetValue(group, out var constants))
                    return Array.Empty<ReferenceNodeViewModel>();

                return constants
                    .Where(c => !filtering || c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .Select(c => new ReferenceNodeViewModel(c))
                    .ToList();
            }

            return _language.Engine.Functions
                .Where(f => f.Category == group)
                .Where(f => !filtering || f.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .Select(f => new ReferenceNodeViewModel(f))
                .ToList();
        }

    }
}
