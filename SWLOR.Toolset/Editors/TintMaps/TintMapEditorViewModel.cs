using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Editors.TintMaps
{
    /// <summary>RGB tint-map controls shared by item and creature blueprint editors.</summary>
    public sealed partial class TintMapEditorViewModel : ObservableObject
    {
        private readonly VarTable _variables;
        private readonly Func<string, Action, bool> _runEdit;
        private TintMapCatalog? _catalog;
        private readonly Func<RenderModel?>? _resolveModel;
        private readonly Action? _colorChanged;
        private ItemColorCarry? _pendingItemColorCarry;

        private sealed record ItemColorCarry(
            IReadOnlySet<string> ActiveKeys,
            IReadOnlyDictionary<TintMapLayerType, int> Colors,
            IReadOnlyDictionary<TintMapLayerType, IReadOnlyList<string>> SourceKeys);

        public ObservableCollection<TintMapColorRowViewModel> Colors { get; } = new();

        public bool HasColors => Colors.Count > 0;

        public TintMapEditorViewModel(
            VarTable variables,
            Func<string, Action, bool> runEdit,
            TintMapCatalog catalog,
            Func<RenderModel?>? resolveModel = null,
            Action? colorChanged = null)
        {
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _resolveModel = resolveModel;
            _colorChanged = colorChanged;

            if (_resolveModel != null)
                Reload();
        }

        public void Reload()
        {
            Reload(_resolveModel?.Invoke());
        }

        public void Reload(
            RenderModel? model,
            bool includeItemOwnedMaterials = true,
            bool includeNonItemOwnedMaterials = true,
            bool includeCreatureLayersFromItemOwnedMaterials = false,
            bool carryItemCustomColorsAcrossMaterials = false)
        {
            if (_catalog == null)
            {
                Colors.Clear();
                OnPropertyChanged(nameof(HasColors));
                return;
            }

            var wanted = _catalog.FindMaterials(
                    model,
                    includeItemOwnedMaterials,
                    includeNonItemOwnedMaterials,
                    includeCreatureLayersFromItemOwnedMaterials)
                .SelectMany(material => material.Layers.Select(layer => (material, layer)))
                .ToList();
            var currentKeys = Colors.Select(row => row.Key);
            var wantedKeys = wanted.Select(entry =>
                TintMapVariable.GetName(entry.material.Resref, entry.layer));

            var hasPendingReplacement =
                carryItemCustomColorsAcrossMaterials &&
                model != null &&
                _pendingItemColorCarry != null;
            if (!hasPendingReplacement &&
                currentKeys.SequenceEqual(wantedKeys, StringComparer.Ordinal))
            {
                foreach (var row in Colors)
                    row.Reload();
                return;
            }

            ItemColorCarry? carry = null;
            if (carryItemCustomColorsAcrossMaterials)
            {
                carry = _pendingItemColorCarry ?? CaptureItemCustomColors(Colors);
                if (model == null)
                    _pendingItemColorCarry = carry;
            }

            Colors.Clear();
            foreach (var (material, layer) in wanted)
            {
                Colors.Add(new TintMapColorRowViewModel(
                    material.Resref,
                    layer,
                    _variables,
                    _runEdit,
                    _colorChanged));
            }

            if (carryItemCustomColorsAcrossMaterials && model != null)
            {
                if (CarryItemCustomColors(carry, Colors))
                    _pendingItemColorCarry = null;
            }

            OnPropertyChanged(nameof(HasColors));
        }

        /// <summary>
        /// Captures only equipment layers with one clear custom RGB value. Different colors on the
        /// same layer are intentional per-material edits and cannot safely be mapped to a new model.
        /// </summary>
        private ItemColorCarry CaptureItemCustomColors(
            IReadOnlyCollection<TintMapColorRowViewModel> rows)
        {
            var activeKeys = rows.Select(row => row.Key).ToHashSet(StringComparer.Ordinal);
            var colors = new Dictionary<TintMapLayerType, int>();
            var sourceKeys = new Dictionary<TintMapLayerType, IReadOnlyList<string>>();
            foreach (var group in rows
                         .Where(row => !TintMapVariable.IsCreatureColorLayer(row.Layer))
                         .GroupBy(row => row.Layer))
            {
                var custom = group
                    .Select(row => (row.Key, Saved: _variables.GetInt(row.Key) ?? 0))
                    .Where(entry => TintMapColor.TryFromStoredValue(entry.Saved, out _))
                    .ToList();
                var distinct = custom.Select(entry => entry.Saved).Distinct().ToList();
                if (distinct.Count != 1)
                    continue;

                colors[group.Key] = distinct[0];
                sourceKeys[group.Key] = custom
                    .Select(entry => entry.Key)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
            }

            return new ItemColorCarry(activeKeys, colors, sourceKeys);
        }

        /// <summary>
        /// Applies captured colors only to material keys introduced by the replacement model, then
        /// removes source keys that no active mesh still uses so switching back cannot revive them.
        /// </summary>
        private bool CarryItemCustomColors(
            ItemColorCarry? carry,
            IReadOnlyCollection<TintMapColorRowViewModel> rows)
        {
            if (carry == null || carry.Colors.Count == 0)
                return true;

            var activeKeys = rows.Select(row => row.Key).ToHashSet(StringComparer.Ordinal);
            var destinations = carry.Colors
                .Select(entry => (
                    entry.Key,
                    entry.Value,
                    Keys: rows
                        .Where(row =>
                            row.Layer == entry.Key &&
                            !carry.ActiveKeys.Contains(row.Key))
                        .Select(row => row.Key)
                        .Distinct(StringComparer.Ordinal)
                        .ToList()))
                .ToList();
            var hasChanges = destinations.Any(entry =>
                entry.Keys.Count > 0 ||
                carry.SourceKeys.TryGetValue(entry.Key, out var sources) &&
                sources.Any(source => !activeKeys.Contains(source)));
            if (!hasChanges)
                return true;

            var applied = _runEdit("Carry custom item colors to replacement models", () =>
            {
                foreach (var (layer, saved, keys) in destinations)
                {
                    foreach (var key in keys)
                        _variables.SetInt(key, saved);

                    if (!carry.SourceKeys.TryGetValue(layer, out var sources))
                        continue;

                    foreach (var source in sources)
                    {
                        if (!activeKeys.Contains(source))
                            _variables.Remove(source);
                    }
                }
            });
            if (!applied)
                return false;

            foreach (var row in rows)
                row.Reload();
            return true;
        }

        public void ReloadCatalog(TintMapCatalog? catalog)
        {
            _catalog = catalog;
            Reload();
        }
    }

}
