using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Script.Symbols;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>A row in the reference tree: a category header, or a symbol under it.</summary>
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

        public string Twisty => IsCategory ? IsExpanded ? "▾" : "▸" : string.Empty;

        partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(Twisty));

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
    /// searchable, with a description pane and insert-at-cursor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Docks beside the Palette rather than replacing it, and takes focus when a script tab becomes
    /// active — the Palette lists the front <i>area's</i> tileset, so it has nothing to offer while a
    /// script is in front.
    /// </para>
    /// <para>
    /// Search filters <b>within</b> categories rather than flattening to a list. That keeps the
    /// Aurora mental model ("effects are over there") while being far faster than scrolling it, which
    /// is the actual reason people stopped using the original.
    /// </para>
    /// </remarks>
    public partial class ScriptReferenceViewModel : Tool
    {
        private readonly ScriptLanguageService _language;
        private List<ReferenceNodeViewModel> _categories = new();
        private bool _built;

        public ScriptReferenceViewModel(ScriptLanguageService language)
        {
            _language = language;
            Id = "ScriptReference";
            Title = "Script Reference";
        }

        public ObservableCollection<ReferenceNodeViewModel> Rows { get; } = new();

        [ObservableProperty]
        private string _filter = string.Empty;

        [ObservableProperty]
        private ReferenceNodeViewModel? _selectedRow;

        [ObservableProperty]
        private string _symbolCountLabel = string.Empty;

        /// <summary>Set by the shell to the active script editor's insertion point, or null.</summary>
        public Action<string>? InsertTarget { get; set; }

        public bool CanInsert => InsertTarget != null && SelectedRow?.IsSymbol == true;

        public string Documentation => SelectedRow?.Documentation ?? string.Empty;

        /// <summary>Builds the tree on first show; the header parse is lazy behind this.</summary>
        public void EnsureBuilt()
        {
            if (_built)
                return;

            _built = true;
            var engine = _language.Engine;

            _categories = engine.CategoryCounts()
                .Select(c => new ReferenceNodeViewModel(c.Category, c.Count))
                .ToList();

            // Constants are grouped by their FOO_* family rather than listed flat: 6,201 in one list
            // is not a browsable thing.
            var families = engine.Constants
                .GroupBy(FamilyOf, StringComparer.Ordinal)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToList();

            _categories.Add(new ReferenceNodeViewModel("Constants", engine.Constants.Count));
            _constantFamilies = families.ToDictionary(g => g.Key, g => (IReadOnlyList<ScriptConstant>)g.ToList(), StringComparer.Ordinal);

            SymbolCountLabel = $"{engine.Functions.Count:N0} functions · {engine.Constants.Count:N0} constants";
            Rebuild();
        }

        private Dictionary<string, IReadOnlyList<ScriptConstant>> _constantFamilies = new(StringComparer.Ordinal);

        partial void OnFilterChanged(string value) => Rebuild();

        partial void OnSelectedRowChanged(ReferenceNodeViewModel? value)
        {
            OnPropertyChanged(nameof(Documentation));
            OnPropertyChanged(nameof(CanInsert));
            InsertCommand.NotifyCanExecuteChanged();
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

            var engine = _language.Engine;
            var filter = Filter?.Trim() ?? string.Empty;
            var filtering = filter.Length > 0;

            foreach (var category in _categories)
            {
                IReadOnlyList<ReferenceNodeViewModel> children;

                if (category.Label == "Constants")
                {
                    children = _constantFamilies
                        .SelectMany(f => f.Value)
                        .Where(c => !filtering || c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                        .Take(filtering ? 200 : int.MaxValue)
                        .Select(c => new ReferenceNodeViewModel(c))
                        .ToList();
                }
                else
                {
                    children = engine.Functions
                        .Where(f => f.Category == category.Label)
                        .Where(f => !filtering || f.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                        .Select(f => new ReferenceNodeViewModel(f))
                        .ToList();
                }

                // While filtering, a category with no surviving children is hidden entirely and the
                // rest auto-expand — otherwise a search looks like it found nothing.
                if (filtering && children.Count == 0)
                    continue;

                Rows.Add(category);

                if (!filtering && !category.IsExpanded)
                    continue;

                if (filtering)
                    category.IsExpanded = true;

                foreach (var child in children)
                    Rows.Add(child);
            }
        }

        /// <summary>"CREATURE_TYPE_PLAYER_CHAR" → "CREATURE_TYPE_*" by its leading two segments.</summary>
        private static string FamilyOf(ScriptConstant constant)
        {
            var parts = constant.Name.Split('_');
            return parts.Length >= 2 ? $"{parts[0]}_{parts[1]}_*" : constant.Name;
        }
    }
}
