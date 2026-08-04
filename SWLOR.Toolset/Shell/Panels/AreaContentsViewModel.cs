using System.Collections.ObjectModel;
using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Model.Mvvm.Controls;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Shell.Panels
{
    /// <summary>
    /// Area Contents: everything placed in the area that is in front, as a tree.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately not part of Module Contents. Aurora hung an area's creatures, doors and
    /// placeables off the same tree as the areas themselves, which is what made finding anything in
    /// it miserable - the 443 areas and the 1,599 objects inside one of them were competing for the
    /// same scroll. This is a sibling panel that follows the front area tab instead.
    /// </para>
    /// <para>
    /// The middle level of the tree is a <see cref="AreaContentsGrouping">group</see>, not an
    /// instance, because a row per instance is unreadable at real sizes: veles_exterior holds 1,599
    /// placeables of which 648 are copies of one object. Grouped by name that branch is 310 rows,
    /// and anything placed exactly once renders as a leaf rather than a group of one.
    /// </para>
    /// </remarks>
    public partial class AreaContentsViewModel : Tool
    {
        /// <summary>
        /// How many members of one group are realised before the rest collapse into a single
        /// "... n more" row.
        /// </summary>
        /// <remarks>
        /// A cap rather than virtualisation inside the group, because the tree publishes one flat
        /// row list and expanding the 648-copy rug would otherwise build 648 view models on the UI
        /// thread for a list nobody reads to the end of. The tail row says how many were dropped, so
        /// the panel never quietly implies a group is smaller than it is.
        /// </remarks>
        private const int MaxRealizedGroupMembers = 200;

        private readonly IEditorPromptService? _prompts;

        private readonly List<AreaContentsNodeViewModel> _roots = new();

        private AreaEditorViewModel? _editor;
        private (ResourceType Type, int Index)? _forcedVisibleInstance;
        private AreaContentsNodeViewModel? _pendingRowReveal;
        private bool _syncingSelection;

        /// <summary>
        /// Raised after a Go To has made its exact instance row visible. The view consumes the
        /// retained row after layout and scrolls it into view; retaining it matters when the Area
        /// Contents tool tab is activated in the same dispatcher turn as the request.
        /// </summary>
        public event Action? RowRevealRequested;

        /// <summary>The visible rows: every node whose ancestors are all expanded.</summary>
        public ObservableCollection<AreaContentsNodeViewModel> Rows { get; } = new();

        public IReadOnlyList<AreaContentsGroupingOption> GroupingOptions { get; } = new[]
        {
            new AreaContentsGroupingOption(
                AreaContentsGrouping.Name, "Name",
                "One row per name. Two objects named differently are two rows, however they were built."),
            new AreaContentsGroupingOption(
                AreaContentsGrouping.Blueprint, "Blueprint",
                "One row per blueprint - what a change to that blueprint would touch. Objects here often share a blueprint and nothing else."),
            new AreaContentsGroupingOption(
                AreaContentsGrouping.Tag, "Tag",
                "One row per tag - what scripts and spawn tables address."),
            new AreaContentsGroupingOption(
                AreaContentsGrouping.Flat, "No grouping",
                "Every placement on its own row.")
        };

        [ObservableProperty]
        private AreaContentsNodeViewModel? _selectedRow;

        [ObservableProperty]
        private string _filter = string.Empty;

        [ObservableProperty]
        private AreaContentsGroupingOption? _selectedGrouping;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        /// <summary>The area this panel is showing, or empty when no area is open.</summary>
        [ObservableProperty]
        private string _areaResRef = string.Empty;

        public AreaContentsViewModel(IEditorPromptService? prompts = null)
        {
            _prompts = prompts;

            Id = "AreaContents";
            Title = "Area Contents";

            // Assigned rather than set through the property: the change handler rebuilds a tree that
            // has no editor yet.
            _selectedGrouping = GroupingOptions[0];
            StatusMessage = "Open an area to see what is in it.";
        }

        public bool HasArea => _editor != null;

        /// <summary>
        /// Points the panel at the area document in front, or at nothing when a blueprint, script or
        /// conversation is. Called from the shell's active-document hook.
        /// </summary>
        public void SetEditor(AreaEditorViewModel? editor)
        {
            if (ReferenceEquals(editor, _editor))
                return;

            if (_editor != null)
            {
                _editor.ContentsChanged -= Rebuild;
                _editor.AreaContentsRevealRequested -= OnAreaContentsRevealRequested;
            }

            _editor = editor;

            if (_editor != null)
            {
                _editor.ContentsChanged += Rebuild;
                _editor.AreaContentsRevealRequested += OnAreaContentsRevealRequested;
            }

            AreaResRef = _editor?.AreaResRef ?? string.Empty;
            _forcedVisibleInstance = null;
            _pendingRowReveal = null;
            OnPropertyChanged(nameof(HasArea));
            SelectedRow = null;
            Rebuild();

            ConsumePendingEditorReveal();
        }

        private void OnAreaContentsRevealRequested(ResourceType _, int __) =>
            ConsumePendingEditorReveal();

        private void ConsumePendingEditorReveal()
        {
            if (_editor?.TryTakePendingAreaContentsReveal(out var type, out var index) == true)
                RevealInstanceRow(type, index);
        }

        /// <summary>Lets the attached view take exactly one retained scroll request.</summary>
        public bool TryTakePendingRowReveal(out AreaContentsNodeViewModel row)
        {
            if (_pendingRowReveal == null)
            {
                row = null!;
                return false;
            }

            row = _pendingRowReveal;
            _pendingRowReveal = null;
            return true;
        }

        partial void OnFilterChanged(string value) => Rebuild();

        partial void OnSelectedGroupingChanged(AreaContentsGroupingOption? value) => Rebuild();

        private AreaContentsGrouping Grouping => SelectedGrouping?.Value ?? AreaContentsGrouping.Name;

        // ----- building the tree -----

        private void Rebuild()
        {
            // Which branches were open, so a rebuild after an edit does not collapse the tree the
            // builder had arranged. Keyed by the row's own label, which survives the rebuild for
            // everything except the group that was just renamed out from under it.
            var expanded = _roots
                .SelectMany(Flatten)
                .Where(node => node.IsExpanded)
                .Select(NodeKey)
                .ToHashSet(StringComparer.Ordinal);

            var reselect = SelectedRow == null ? null : NodeKey(SelectedRow);

            _roots.Clear();

            if (_editor == null)
            {
                PublishVisibleRows();
                StatusMessage = "Open an area to see what is in it.";
                return;
            }

            var matched = 0;
            var total = 0;

            foreach (var section in _editor.Sections)
            {
                total += section.Rows.Count;

                var rows = Matching(section).ToList();
                matched += rows.Count;

                _roots.Add(BuildKindNode(section, rows));
            }

            foreach (var node in _roots.SelectMany(Flatten))
            {
                // A first look at an area opens the kinds and leaves the groups shut: the kinds are
                // the shape of the area, and every group open at once is the flat list this exists
                // to avoid.
                node.IsExpanded = expanded.Count == 0
                    ? node.Kind == AreaContentsNodeKind.Kind
                    : expanded.Contains(NodeKey(node));
            }

            PublishVisibleRows();

            if (reselect != null)
                SelectedRow = Rows.FirstOrDefault(row => NodeKey(row) == reselect);

            // The area leads, because the tab above can only say which panel this is - which area it
            // is following is the thing you cannot read anywhere else in the rail.
            StatusMessage = HasFilter
                ? $"{AreaResRef} — {matched} of {total} match “{Filter.Trim()}”"
                : $"{AreaResRef} — {total} objects";
        }

        private bool HasFilter => !string.IsNullOrWhiteSpace(Filter);

        /// <summary>
        /// The section's rows that survive the filter. Matching runs over the name, the resref and
        /// the tag together, because which of the three a builder has in hand varies by why they are
        /// looking - a name from the map, a resref from a blueprint, a tag from a script.
        /// </summary>
        private IEnumerable<InstanceRow> Matching(InstanceListSectionViewModel section)
        {
            if (!HasFilter)
                return section.Rows;

            var needle = Filter.Trim();
            return section.Rows.Where(row =>
                Contains(_editor!.ResolveInstanceName(section.BlueprintType, row), needle) ||
                Contains(row.TemplateResRef, needle) ||
                Contains(row.Tag, needle));
        }

        private static bool Contains(string? haystack, string needle) =>
            haystack != null && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

        private AreaContentsNodeViewModel BuildKindNode(
            InstanceListSectionViewModel section, IReadOnlyList<InstanceRow> rows)
        {
            var type = section.BlueprintType;

            var node = new AreaContentsNodeViewModel(
                AreaContentsNodeKind.Kind, type, section.Title, depth: 0)
            {
                Detail = KindDetail(section, rows)
            };

            if (Grouping == AreaContentsGrouping.Flat)
            {
                var realized = RealizedRows(section.BlueprintType, rows);
                foreach (var row in realized)
                    node.Children.Add(BuildInstanceNode(section, row, depth: 1, leadWithName: true));

                if (rows.Count > realized.Count)
                    node.Children.Add(BuildOverflowNode(type, rows.Count - realized.Count, depth: 1));

                return node;
            }

            var groups = rows
                .GroupBy(row => GroupKey(section, row), StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                var members = group.ToList();

                // A group of one is a click that buys nothing, so a unique object stays a leaf and
                // keeps its position on the row rather than hiding behind a "x1".
                if (members.Count == 1)
                {
                    node.Children.Add(BuildInstanceNode(section, members[0], depth: 1, leadWithName: true));
                    continue;
                }

                node.Children.Add(BuildGroupNode(section, group.Key, members));
            }

            return node;
        }

        private AreaContentsNodeViewModel BuildGroupNode(
            InstanceListSectionViewModel section, string label, IReadOnlyList<InstanceRow> members)
        {
            var node = new AreaContentsNodeViewModel(
                AreaContentsNodeKind.Group, section.BlueprintType, label, depth: 1)
            {
                Detail = $"×{members.Count}",
                Indices = members.Select(row => row.Index).ToList()
            };

            var realized = RealizedRows(section.BlueprintType, members);
            foreach (var row in realized)
            {
                // Inside a group the members share a name by definition, so the position leads and
                // the resref trails - a column of the same word repeated tells you nothing.
                node.Children.Add(BuildInstanceNode(section, row, depth: 2, leadWithName: false));
            }

            if (members.Count > realized.Count)
            {
                node.Children.Add(BuildOverflowNode(
                    section.BlueprintType, members.Count - realized.Count, depth: 2));
            }

            return node;
        }

        /// <summary>
        /// Keeps the normal 200-row performance cap while ensuring an explicitly requested source
        /// instance is realised even when it is the 648th copy in a large group.
        /// </summary>
        private IReadOnlyList<InstanceRow> RealizedRows(
            ResourceType type, IReadOnlyList<InstanceRow> rows)
        {
            var realized = rows.Take(MaxRealizedGroupMembers).ToList();
            if (_forcedVisibleInstance is not { } forced || forced.Type != type ||
                realized.Any(row => row.Index == forced.Index))
                return realized;

            var requested = rows.FirstOrDefault(row => row.Index == forced.Index);
            if (requested != null)
                realized.Add(requested);

            return realized;
        }

        private AreaContentsNodeViewModel BuildInstanceNode(
            InstanceListSectionViewModel section, InstanceRow row, int depth, bool leadWithName)
        {
            var name = _editor!.ResolveInstanceName(section.BlueprintType, row);
            var position = FormatPosition(row);

            return new AreaContentsNodeViewModel(
                AreaContentsNodeKind.Instance,
                section.BlueprintType,
                leadWithName ? name : position,
                depth)
            {
                Detail = leadWithName ? position : row.TemplateResRef,
                Indices = new[] { row.Index },
                Position = new Vector3(row.X, row.Y, row.Z)
            };
        }

        private static AreaContentsNodeViewModel BuildOverflowNode(ResourceType type, int remaining, int depth) =>
            new(AreaContentsNodeKind.Overflow, type, $"… {remaining} more", depth);

        private string KindDetail(InstanceListSectionViewModel section, IReadOnlyList<InstanceRow> rows)
        {
            var total = section.Rows.Count;

            if (HasFilter)
                return $"{rows.Count} of {total}";

            if (Grouping == AreaContentsGrouping.Flat || total == 0)
                return total.ToString();

            var groupCount = rows
                .Select(row => GroupKey(section, row))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            // Groups first, then instances: the first number is how many rows opening this costs,
            // which is the one worth knowing before you open it.
            return $"{groupCount} · {total}";
        }

        private string GroupKey(InstanceListSectionViewModel section, InstanceRow row)
        {
            return Grouping switch
            {
                AreaContentsGrouping.Blueprint => string.IsNullOrWhiteSpace(row.TemplateResRef)
                    ? "(no blueprint)"
                    : row.TemplateResRef,
                AreaContentsGrouping.Tag => string.IsNullOrWhiteSpace(row.Tag)
                    ? "(no tag)"
                    : row.Tag,
                _ => _editor!.ResolveInstanceName(section.BlueprintType, row)
            };
        }

        private static string FormatPosition(InstanceRow row) => $"{row.X:0.0}, {row.Y:0.0}";

        /// <summary>
        /// Identifies a row across a rebuild, for restoring what was open and what was selected.
        /// </summary>
        /// <remarks>
        /// Built from kind, depth and label rather than from the list index, because an index is
        /// exactly what a delete invalidates - and a rebuild after a delete is the one where
        /// restoring the wrong row matters most.
        /// </remarks>
        private static string NodeKey(AreaContentsNodeViewModel node) =>
            $"{node.BlueprintType}|{node.Kind}|{node.Depth}|{node.Name}|{node.Detail}";

        private static IEnumerable<AreaContentsNodeViewModel> Flatten(AreaContentsNodeViewModel node) =>
            new[] { node }.Concat(node.Children.SelectMany(Flatten));

        private void PublishVisibleRows()
        {
            Rows.Clear();
            foreach (var root in _roots)
                Publish(root);
        }

        private void Publish(AreaContentsNodeViewModel node)
        {
            Rows.Add(node);
            if (!node.IsExpanded)
                return;

            foreach (var child in node.Children)
                Publish(child);
        }

        /// <summary>
        /// Clears any filter hiding the target, realises it past the large-group cap, opens every
        /// ancestor, selects it, and retains a scroll request for the view.
        /// </summary>
        private void RevealInstanceRow(ResourceType type, int index)
        {
            _forcedVisibleInstance = (type, index);
            _syncingSelection = true;
            try
            {
                if (HasFilter)
                    Filter = string.Empty;
                else
                    Rebuild();

                var path = new List<AreaContentsNodeViewModel>();
                var found = _roots
                    .Where(root => root.BlueprintType == type)
                    .Any(root => TryFindPath(
                        root,
                        node => node.Kind == AreaContentsNodeKind.Instance &&
                                node.Indices.Count == 1 && node.Indices[0] == index,
                        path));
                if (!found || path.Count == 0)
                    return;

                foreach (var ancestor in path.Take(path.Count - 1))
                    ancestor.IsExpanded = true;

                PublishVisibleRows();
                SelectedRow = path[^1];
                _pendingRowReveal = SelectedRow;
            }
            finally
            {
                _syncingSelection = false;
            }

            RowRevealRequested?.Invoke();
        }

        private static bool TryFindPath(
            AreaContentsNodeViewModel node,
            Func<AreaContentsNodeViewModel, bool> predicate,
            List<AreaContentsNodeViewModel> path)
        {
            path.Add(node);
            if (predicate(node))
                return true;

            foreach (var child in node.Children)
            {
                if (TryFindPath(child, predicate, path))
                    return true;
            }

            path.RemoveAt(path.Count - 1);
            return false;
        }

        // ----- what the rows do -----

        [RelayCommand]
        private void Toggle(AreaContentsNodeViewModel? node)
        {
            if (node == null || node.Children.Count == 0)
                return;

            node.IsExpanded = !node.IsExpanded;
            PublishVisibleRows();
        }

        /// <summary>
        /// A single click selects the object in the map without moving the camera; opening the row
        /// (double click, or Enter) sends the camera to it.
        /// </summary>
        partial void OnSelectedRowChanged(AreaContentsNodeViewModel? value)
        {
            if (_syncingSelection || _editor == null ||
                value is not { Kind: AreaContentsNodeKind.Instance } instance)
                return;

            _editor.RevealInstance(instance.BlueprintType, instance.Indices[0], frameCamera: false);
        }

        /// <summary>Double-click or Enter: select the object and fly the camera to it.</summary>
        [RelayCommand]
        private void Open(AreaContentsNodeViewModel? node)
        {
            node ??= SelectedRow;
            if (_editor == null || node == null)
                return;

            if (node.Kind != AreaContentsNodeKind.Instance)
            {
                Toggle(node);
                return;
            }

            _editor.RevealInstance(node.BlueprintType, node.Indices[0], frameCamera: true);
        }

        /// <summary>Opens the selected placement's editable details in the owning area tab.</summary>
        [RelayCommand]
        private void OpenProperties(AreaContentsNodeViewModel? node)
        {
            node ??= SelectedRow;
            if (_editor == null || node is not { CanOpenProperties: true })
                return;

            _editor.OpenInstanceProperties(node.BlueprintType, node.Indices[0]);
        }

        /// <summary>
        /// Delete: removes the selected row's objects from the area.
        /// </summary>
        /// <remarks>
        /// A group row deletes every object in it, as one undo entry - which is the useful half of
        /// grouping and also the half that can take 648 objects off the map from one keypress, so
        /// anything past a single object is confirmed first.
        /// </remarks>
        [RelayCommand]
        private async Task DeleteSelectedAsync()
        {
            if (_editor == null || SelectedRow is not { } node || !node.IsDeletable)
                return;

            var count = node.Indices.Count;
            if (count > 1)
            {
                if (_prompts == null)
                    return;

                var confirmed = await _prompts.ConfirmDestructiveAsync(
                    $"Delete {count} objects from {AreaResRef}?",
                    $"Every object under “{node.Name}” is removed from this area. " +
                    "The area is not saved by this, so Undo takes it back; saving afterwards does not.",
                    $"Delete {count}").ConfigureAwait(true);

                if (!confirmed)
                    return;
            }

            var deleted = _editor.DeleteInstances(node.BlueprintType, node.Indices);
            if (deleted)
                SelectedRow = null;
        }
    }
}
