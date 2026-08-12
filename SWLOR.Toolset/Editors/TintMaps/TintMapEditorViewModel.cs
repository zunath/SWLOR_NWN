using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Editors.TintMaps
{
    /// <summary>RGB tint-map controls shared by item and creature blueprint editors.</summary>
    public sealed partial class TintMapEditorViewModel : ObservableObject
    {
        private readonly VarTable _variables;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<IDocumentEdit, string, Action, bool>? _runCoalescedEdit;
        private TintMapCatalog? _catalog;
        private readonly Func<RenderModel?>? _resolveModel;
        private readonly Action? _colorChanged;
        private ItemColorCarry? _pendingItemColorCarry;

        private readonly record struct ItemColorSource(string Key, int? SavedColor);

        private sealed record ItemColorCarry(
            IReadOnlyDictionary<TintMapLayerType, IReadOnlyList<ItemColorSource>> Sources,
            IDocumentEdit? OriginEdit);

        public ObservableCollection<TintMapColorRowViewModel> Colors { get; } = new();

        public bool HasColors => Colors.Count > 0;

        public TintMapEditorViewModel(
            VarTable variables,
            Func<string, Action, bool> runEdit,
            TintMapCatalog catalog,
            Func<RenderModel?>? resolveModel = null,
            Action? colorChanged = null,
            Func<IDocumentEdit, string, Action, bool>? runCoalescedEdit = null)
        {
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _resolveModel = resolveModel;
            _colorChanged = colorChanged;
            _runCoalescedEdit = runCoalescedEdit;

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
            bool carryItemCustomColorsAcrossMaterials = false,
            IDocumentEdit? coalesceOrigin = null)
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
            if (carryItemCustomColorsAcrossMaterials &&
                model == null &&
                _pendingItemColorCarry != null &&
                coalesceOrigin != null)
            {
                _pendingItemColorCarry = _pendingItemColorCarry with
                {
                    OriginEdit = coalesceOrigin
                };
            }
            if (!hasPendingReplacement &&
                currentKeys.SequenceEqual(wantedKeys, StringComparer.Ordinal))
            {
                for (var index = 0; index < Colors.Count; index++)
                {
                    var (material, layer) = wanted[index];
                    Colors[index].Reload(
                        ResolveStandardPaletteColorId(model, material.Resref, layer));
                }
                return;
            }

            ItemColorCarry? carry = null;
            if (carryItemCustomColorsAcrossMaterials)
            {
                carry = _pendingItemColorCarry ?? CaptureItemCustomColors(Colors, coalesceOrigin);
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
                    _colorChanged,
                    ResolveStandardPaletteColorId(model, material.Resref, layer)));
            }

            if (carryItemCustomColorsAcrossMaterials && model != null)
            {
                if (CarryItemCustomColors(carry, Colors))
                    _pendingItemColorCarry = null;
            }

            OnPropertyChanged(nameof(HasColors));
        }

        private static int ResolveStandardPaletteColorId(
            RenderModel? model,
            string materialResref,
            TintMapLayerType layer)
        {
            if (model == null)
                return 0;

            foreach (var mesh in model.Meshes.Where(mesh =>
                         string.Equals(mesh.MaterialName, materialResref, StringComparison.OrdinalIgnoreCase) ||
                         string.IsNullOrWhiteSpace(mesh.MaterialName) &&
                         string.Equals(mesh.TextureName, materialResref, StringComparison.OrdinalIgnoreCase)))
            {
                if (mesh.LayerColorIndices.TryGetValue((int)layer, out var meshColor))
                    return Math.Clamp(meshColor, 0, TintMapMaterialRegistry.PaletteColorCount - 1);
            }

            return model.LayerColorIndices.TryGetValue((int)layer, out var modelColor)
                ? Math.Clamp(modelColor, 0, TintMapMaterialRegistry.PaletteColorCount - 1)
                : 0;
        }

        /// <summary>
        /// Captures every material position for equipment layers that contain an explicit color.
        /// Palette values include material-level opt-outs from an inherited global RGB tint, so
        /// they must move with their material slot just like custom RGB values do.
        /// </summary>
        private ItemColorCarry CaptureItemCustomColors(
            IReadOnlyCollection<TintMapColorRowViewModel> rows,
            IDocumentEdit? originEdit)
        {
            var sources = new Dictionary<TintMapLayerType, IReadOnlyList<ItemColorSource>>();
            foreach (var group in rows
                         .Where(row => !TintMapVariable.IsCreatureColorLayer(row.Layer))
                         .GroupBy(row => row.Layer))
            {
                var entries = group
                    .GroupBy(row => row.Key, StringComparer.Ordinal)
                    .Select(keyGroup =>
                    {
                        var saved = _variables.GetInt(keyGroup.Key);
                        return new ItemColorSource(
                            keyGroup.Key,
                            saved.HasValue && IsStoredColorOverride(saved.Value)
                                ? saved.Value
                                : null);
                    })
                    .ToList();
                if (!entries.Any(entry => entry.SavedColor.HasValue))
                    continue;

                sources[group.Key] = entries;
            }

            return new ItemColorCarry(sources, originEdit);
        }

        /// <summary>
        /// Maps replaced source material positions to replacement positions when the layer has one
        /// unambiguous custom value, then removes every stale source key. Shared materials retain
        /// their existing variables and ambiguous per-material colors are cleaned up, not guessed.
        /// </summary>
        private bool CarryItemCustomColors(
            ItemColorCarry? carry,
            IReadOnlyCollection<TintMapColorRowViewModel> rows)
        {
            if (carry == null || carry.Sources.Count == 0)
                return true;
            var activeKeys = rows.Select(row => row.Key).ToHashSet(StringComparer.Ordinal);
            var assignments = new Dictionary<string, int>(StringComparer.Ordinal);
            var staleKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (layer, sources) in carry.Sources)
            {
                // A builder can change one tint layer while the replacement model is loading.
                // That newer choice invalidates only its own captured layer; unrelated layers must
                // still migrate to the replacement materials and clean up their obsolete keys.
                if (!CarrySourceValuesAreCurrent(sources))
                    continue;

                var sourceKeys = sources
                    .Select(source => source.Key)
                    .ToHashSet(StringComparer.Ordinal);
                var replacedSources = sources
                    .Where(source => !activeKeys.Contains(source.Key))
                    .ToList();
                var replacementDestinations = rows
                    .Where(row => row.Layer == layer && !sourceKeys.Contains(row.Key))
                    .GroupBy(row => row.Key, StringComparer.Ordinal)
                    .Select(group => group.First())
                    .ToList();

                // Prefer the equipment registry's wearer-variant material slots. Generated female,
                // male, race, and phenotype materials are not guaranteed to appear in the same
                // alphabetic order, so list position alone can move a custom tint to the wrong mesh.
                var semanticCandidates = new List<(
                    ItemColorSource Source,
                    TintMapColorRowViewModel Destination)>();
                foreach (var source in replacedSources.Where(source => source.SavedColor.HasValue))
                {
                    if (!TintMapVariable.TryParse(source.Key, out var sourceMaterial, out _))
                        continue;

                    foreach (var destination in replacementDestinations.Where(destination =>
                                 _catalog?.AreEquipmentMaterialSlotsEquivalent(
                                     sourceMaterial,
                                     destination.MaterialName,
                                     layer) == true))
                    {
                        semanticCandidates.Add((source, destination));
                    }
                }
                var matchedSourceKeys = new HashSet<string>(StringComparer.Ordinal);
                var matchedDestinationKeys = new HashSet<string>(StringComparer.Ordinal);
                foreach (var candidate in semanticCandidates)
                {
                    if (semanticCandidates.Count(entry =>
                            entry.Source.Key == candidate.Source.Key) != 1 ||
                        semanticCandidates.Count(entry =>
                            entry.Destination.Key == candidate.Destination.Key) != 1)
                    {
                        continue;
                    }

                    assignments[candidate.Destination.Key] = candidate.Source.SavedColor!.Value;
                    matchedSourceKeys.Add(candidate.Source.Key);
                    matchedDestinationKeys.Add(candidate.Destination.Key);
                }

                var unmatchedSources = replacedSources
                    .Where(source => !matchedSourceKeys.Contains(source.Key))
                    .ToList();
                var unmatchedDestinations = replacementDestinations
                    .Where(destination => !matchedDestinationKeys.Contains(destination.Key))
                    .ToList();
                var distinctCustomColors = unmatchedSources
                    .Where(source => source.SavedColor.HasValue)
                    .Select(source => source.SavedColor!.Value)
                    .Distinct()
                    .ToList();

                if (distinctCustomColors.Count == 1)
                {
                    for (var index = 0;
                         index < unmatchedSources.Count && index < unmatchedDestinations.Count;
                         index++)
                    {
                        if (unmatchedSources[index].SavedColor is int savedColor)
                            assignments[unmatchedDestinations[index].Key] = savedColor;
                    }
                }

                foreach (var source in sources)
                {
                    if (source.SavedColor.HasValue && !activeKeys.Contains(source.Key))
                        staleKeys.Add(source.Key);
                }
            }

            if (assignments.Count == 0 && staleKeys.Count == 0)
                return true;

            var description = "Carry custom item colors to replacement models";
            var mutation = () =>
            {
                foreach (var (key, savedColor) in assignments)
                    _variables.SetInt(key, savedColor);

                foreach (var staleKey in staleKeys)
                    _variables.Remove(staleKey);
            };
            var applied = carry.OriginEdit != null && _runCoalescedEdit != null
                ? _runCoalescedEdit(carry.OriginEdit, description, mutation)
                : _runEdit(description, mutation);
            if (!applied)
                return false;

            foreach (var row in rows)
                row.Reload();
            return true;
        }

        /// <summary>
        /// A model replacement resolves asynchronously. A later preset or custom-color edit is the
        /// builder's newer intent, so an older captured carry must not recreate the value that edit
        /// removed or replaced when the replacement model finally arrives.
        /// </summary>
        private bool CarrySourceValuesAreCurrent(IReadOnlyList<ItemColorSource> sources)
        {
            foreach (var source in sources)
            {
                var saved = _variables.GetInt(source.Key);
                int? current = saved.HasValue &&
                               IsStoredColorOverride(saved.Value)
                    ? saved.Value
                    : null;
                if (current != source.SavedColor)
                    return false;
            }

            return true;
        }

        private static bool IsStoredColorOverride(int savedColor)
        {
            return TintMapColor.TryFromStoredValue(savedColor, out _) ||
                   savedColor is > 0 and <= TintMapMaterialRegistry.PaletteColorCount;
        }

        public void ReloadCatalog(TintMapCatalog? catalog)
        {
            _catalog = catalog;
            Reload();
        }
    }

}
