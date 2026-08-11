using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Items;
using SWLOR.Toolset.Editors.TintMaps;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Real model-backed variants for segmented creature bodies.</summary>
    public sealed partial class CreatureBodyPartsViewModel : ObservableObject
    {
        private readonly Domain.Editors.Creatures.CreatureValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<IDocumentEdit?>? _captureCoalesceOrigin;
        private readonly Func<IDocumentEdit, string, Action, bool>? _runCoalescedEdit;
        private readonly Func<int, AppearanceRow?> _appearance;
        private readonly ArmorPartCatalog? _parts;
        private readonly ArmorDyeSwatchService? _colorPalettes;
        private readonly Action _changed;
        private readonly Action _colorChanged;
        private int _generation;
        private bool _loaded;
        private bool _partCatalogReady = true;
        private Task? _loadTask;
        private bool _loadingMirror;
        private IReadOnlyList<TintMapColorRowViewModel> _tintRows =
            Array.Empty<TintMapColorRowViewModel>();
        private IDocumentEdit? _pendingTintCarryOrigin;

        private static readonly IReadOnlyList<LimbDefinition> LimbDefinitions = new[]
        {
            new LimbDefinition(
                "Shoulder", "BodyPart_LShoul", "BodyPart_RShoul", "shol", "shor", AllowsNone: true),
            new LimbDefinition("Bicep", "BodyPart_LBicep", "BodyPart_RBicep", "bicepl", "bicepr"),
            new LimbDefinition("Forearm", "BodyPart_LFArm", "BodyPart_RFArm", "forel", "forer"),
            new LimbDefinition("Hand", "BodyPart_LHand", "BodyPart_RHand", "handl", "handr"),
            new LimbDefinition("Thigh", "BodyPart_LThigh", "BodyPart_RThigh", "legl", "legr"),
            new LimbDefinition("Shin", "BodyPart_LShin", "BodyPart_RShin", "shinl", "shinr"),
            // Creature UTCs really use ArmorPart_RFoot for this one field.
            new LimbDefinition("Foot", "BodyPart_LFoot", "ArmorPart_RFoot", "footl", "footr")
        };

        public ObservableCollection<ItemFieldCellViewModel> Structure { get; } = new();
        public ObservableCollection<BodyPartPairViewModel> Limbs { get; } = new();
        public ObservableCollection<CreatureBodyColorViewModel> Colors { get; } = new();

        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// Optional edit mode. It deliberately starts off even when the stored sides happen to
        /// match; creatures remain independently editable unless the builder explicitly opts in.
        /// </summary>
        [ObservableProperty]
        private bool _mirrorRightFromLeft;

        public bool IsLoaded => _loaded;

        public bool IsDynamic => string.Equals(CurrentAppearance?.ModelType, "P", StringComparison.OrdinalIgnoreCase);
        public bool IsFullBody => !IsDynamic;
        public bool HasEditableContent => IsDynamic || _tintRows.Count > 0;
        public string ModelSummary => CurrentAppearance == null
            ? "The selected model is not available in the loaded game data."
            : IsDynamic
                ? "Segmented model · left and right body parts can be edited independently."
                : $"Full-body model · phenotype, wings and tail are supplied by {CurrentAppearance.DisplayName}.";
        public string FullBodyDetails => IsFullBody
            ? $"Phenotype {_store.GetInteger(BehaviorFieldStorage.Field, "Phenotype") ?? 0} · " +
              $"Wings {_store.GetInteger(BehaviorFieldStorage.Field, "Wings_New") ?? 0} · " +
              $"Tail {_store.GetInteger(BehaviorFieldStorage.Field, "Tail_New") ?? 0}"
            : string.Empty;

        private AppearanceRow? CurrentAppearance => _appearance(
            (int)(_store.GetInteger(BehaviorFieldStorage.Field, "Appearance_Type") ?? -1));

        public CreatureBodyPartsViewModel(
            Domain.Editors.Creatures.CreatureValueStore store,
            Func<string, Action, bool> runEdit,
            Func<int, AppearanceRow?> appearance,
            ArmorPartCatalog? parts,
            ArmorDyeSwatchService? colorPalettes,
            Action changed,
            Action? colorChanged = null,
            Func<IDocumentEdit?>? captureCoalesceOrigin = null,
            Func<IDocumentEdit, string, Action, bool>? runCoalescedEdit = null)
        {
            _store = store;
            _runEdit = runEdit;
            _captureCoalesceOrigin = captureCoalesceOrigin;
            _runCoalescedEdit = runCoalescedEdit;
            _appearance = appearance;
            _parts = parts;
            _colorPalettes = colorPalettes;
            _changed = changed;
            _colorChanged = colorChanged ?? changed;
        }

        public void Reload()
        {
            _pendingTintCarryOrigin = null;
            // Never infer that mirroring was requested merely because values match. If an undo or
            // external reload makes explicitly mirrored values diverge, return to independent mode.
            if (MirrorRightFromLeft && !StoredPairsMatch())
            {
                _loadingMirror = true;
                try
                {
                    MirrorRightFromLeft = false;
                }
                finally
                {
                    _loadingMirror = false;
                }
            }

            _generation++;
            _loaded = false;
            Structure.Clear();
            Limbs.Clear();
            Colors.Clear();
            OnPropertyChanged(nameof(IsLoaded));
            OnPropertyChanged(nameof(IsDynamic));
            OnPropertyChanged(nameof(IsFullBody));
            OnPropertyChanged(nameof(HasEditableContent));
            OnPropertyChanged(nameof(ModelSummary));
            OnPropertyChanged(nameof(FullBodyDetails));
        }

        /// <summary>
        /// Loads body-part options only when the Body section is visited. The shared armor catalog
        /// is warmed on a worker thread using the same background-loading strategy as the placeable
        /// appearance editor, then the small observable rows are published on the caller's context.
        /// </summary>
        public Task EnsureLoadedAsync()
        {
            if (_loaded)
                return Task.CompletedTask;
            if (_loadTask != null)
                return _loadTask;

            IsLoading = true;
            _loadTask = LoadAsync();
            return _loadTask;
        }

        private async Task LoadAsync()
        {
            // Always yield once so EnsureLoadedAsync can publish the in-flight task before this
            // method reaches its finally block, even when the shared catalog was already built.
            await Task.Yield();
            try
            {
                while (!_loaded)
                {
                    var generation = _generation;
                    _partCatalogReady = true;
                    try
                    {
                        if (_parts != null)
                            await _parts.EnsureBuiltAsync();
                    }
                    catch (Exception)
                    {
                        // Empty option lists leave plain numeric controls rather than allowing a
                        // damaged resource index to take down the whole Appearance tab.
                        _partCatalogReady = false;
                    }

                    if (generation != _generation)
                        continue;

                    Build();
                    _loaded = true;
                    OnPropertyChanged(nameof(IsLoaded));
                }
            }
            finally
            {
                IsLoading = false;
                _loadTask = null;
            }
        }

        private void Build()
        {
            Structure.Clear();
            Limbs.Clear();
            Colors.Clear();
            if (IsDynamic)
            {
                Structure.Add(Single("Head", "Appearance_Head", "head"));
                Structure.Add(Single("Neck", "BodyPart_Neck", "neck"));
                Structure.Add(Single("Torso", "BodyPart_Torso", "chest"));
                Structure.Add(Single("Belt", "BodyPart_Belt", "belt", allowsNone: true));
                Structure.Add(Single("Pelvis", "BodyPart_Pelvis", "pelvis"));

                foreach (var definition in LimbDefinitions)
                    Limbs.Add(Pair(definition));
            }

            // Full-body appearances cannot change segmented geometry, but tint-map channels on
            // their model still use the same preset/custom semantic color controls.
            BuildColors();
        }

        public void SetTintMapRows(IEnumerable<TintMapColorRowViewModel>? rows)
        {
            var next = rows?.ToList() ?? new List<TintMapColorRowViewModel>();
            var changed = !_tintRows.Select(row => row.Key)
                .SequenceEqual(next.Select(row => row.Key), StringComparer.Ordinal);
            var carriedColors = changed
                ? CaptureSemanticCustomColors(_tintRows)
                : new Dictionary<TintMapLayerType, Color>();
            var carryOrigin = changed ? _pendingTintCarryOrigin : null;
            if (changed)
                _pendingTintCarryOrigin = null;
            _tintRows = next;
            if (changed)
                CarrySemanticCustomColors(next, carriedColors, carryOrigin);
            OnPropertyChanged(nameof(HasEditableContent));

            if (!_loaded)
                return;

            if (changed)
                BuildColors();
            else
                ReloadColors();
        }

        private void BuildColors()
        {
            Colors.Clear();
            var definitions = new (string Field, string Label,
                ArmorDyeSwatchService.DyeMaterial Palette, TintMapLayerType Layer)[]
            {
                ("Color_Skin", "Skin", ArmorDyeSwatchService.DyeMaterial.Skin, TintMapLayerType.Skin),
                ("Color_Hair", "Hair", ArmorDyeSwatchService.DyeMaterial.Hair, TintMapLayerType.Hair),
                ("Color_Tattoo1", "Body Color 1", ArmorDyeSwatchService.DyeMaterial.Tattoo, TintMapLayerType.Tattoo1),
                ("Color_Tattoo2", "Body Color 2", ArmorDyeSwatchService.DyeMaterial.Tattoo, TintMapLayerType.Tattoo2)
            };

            foreach (var definition in definitions)
            {
                var tintRows = _tintRows.Where(row => row.Layer == definition.Layer).ToList();
                Func<Color?>? readCustom = tintRows.Count == 0
                    ? null
                    : () => ReadCustomColor(tintRows);
                Func<Color, bool>? writeCustom = tintRows.Count == 0
                    ? null
                    : color => WriteCustomColor(
                        definition.Label, color, definition.Layer, tintRows);
                var palette = new ItemDyeCellViewModel(
                    definition.Label,
                    () => Read(definition.Field),
                    value => WriteStandardColor(
                        definition.Label, value, definition.Field, definition.Layer),
                    _colorPalettes?.GetPaletteColors(definition.Palette) ??
                    Array.Empty<(byte, byte, byte)>(),
                    allowsNumericFallback: false,
                    readCustom: readCustom,
                    writeCustom: writeCustom);
                Colors.Add(new CreatureBodyColorViewModel(
                    palette,
                    tintRows));
            }
        }

        private static Color? ReadCustomColor(
            IReadOnlyCollection<TintMapColorRowViewModel> tintRows)
        {
            var custom = tintRows.Where(row => row.IsCustom)
                .Select(row => row.Color)
                .Distinct()
                .ToList();
            return custom.Count == 1 && tintRows.All(row => row.IsCustom)
                ? custom[0]
                : null;
        }

        private Dictionary<TintMapLayerType, Color> CaptureSemanticCustomColors(
            IReadOnlyCollection<TintMapColorRowViewModel> tintRows)
        {
            var colors = new Dictionary<TintMapLayerType, Color>();
            foreach (var group in tintRows
                         .Where(row => TintMapVariable.IsCreatureColorLayer(row.Layer))
                         .GroupBy(row => row.Layer))
            {
                var custom = ReadCustomColor(group.ToList());
                if (!custom.HasValue)
                    continue;

                colors[group.Key] = custom.Value;
            }

            return colors;
        }

        private void CarrySemanticCustomColors(
            IReadOnlyCollection<TintMapColorRowViewModel> tintRows,
            IReadOnlyDictionary<TintMapLayerType, Color> colors,
            IDocumentEdit? originEdit)
        {
            var applicable = colors
                .Where(entry => tintRows.Any(row => row.Layer == entry.Key))
                .ToList();
            if (applicable.Count == 0)
                return;

            var description = "Carry custom body colors to replacement models";
            var mutation = () =>
            {
                foreach (var (layer, color) in applicable)
                {
                    var saved = new TintMapColor(color.R, color.G, color.B).ToStoredValue();
                    foreach (var variableName in GetSemanticVariableKeys(layer, tintRows))
                        _store.Locals.SetInt(variableName, saved);
                }
            };
            var applied = originEdit != null && _runCoalescedEdit != null
                ? _runCoalescedEdit(originEdit, description, mutation)
                : _runEdit(description, mutation);
            if (!applied)
                return;

            foreach (var row in tintRows)
                row.Reload();
        }

        private IReadOnlyCollection<string> GetSemanticVariableKeys(
            TintMapLayerType layer,
            IEnumerable<TintMapColorRowViewModel> tintRows)
        {
            var keys = _store.Locals
                .Where(entry =>
                    entry.Type == VarTable.TypeInt &&
                    TintMapVariable.TryGetLayer(entry.Name, out var variableLayer) &&
                    variableLayer == layer)
                .Select(entry => entry.Name)
                .ToHashSet(StringComparer.Ordinal);
            foreach (var row in tintRows.Where(row => row.Layer == layer))
                keys.Add(row.Key);

            return keys;
        }

        private bool WriteStandardColor(
            string label,
            int value,
            string field,
            TintMapLayerType layer)
        {
            var applied = _runEdit($"Change {label} preset", () =>
            {
                WriteFields(value, field);
                foreach (var variableName in GetSemanticVariableKeys(layer, _tintRows))
                    _store.Locals.Remove(variableName);
                _store.Locals.Remove(TintMapVariable.GetCreatureColorStateName(layer));
            });
            if (!applied)
                return false;

            CaptureInterveningTintCarryOrigin();
            ReloadColors();
            _colorChanged();
            return true;
        }

        private bool WriteCustomColor(
            string label,
            Color color,
            TintMapLayerType layer,
            IReadOnlyList<TintMapColorRowViewModel> tintRows)
        {
            if (tintRows.Count == 0)
                return false;

            var tint = new TintMapColor(color.R, color.G, color.B).ToStoredValue();
            var applied = _runEdit(
                $"Set {label} custom tint to #{color.R:X2}{color.G:X2}{color.B:X2}",
                () =>
                {
                    _store.Locals.SetInt(
                        TintMapVariable.GetCreatureColorStateName(layer),
                        tint);
                    foreach (var variableName in GetSemanticVariableKeys(layer, tintRows))
                        _store.Locals.SetInt(variableName, tint);
                });
            if (!applied)
                return false;

            CaptureInterveningTintCarryOrigin();
            ReloadColors();
            _colorChanged();
            return true;
        }

        /// <summary>
        /// A semantic tint edit made while a body-part model is rebuilding is newer than the part
        /// selection. Coalesce the derived material-key carry with that tint transaction so its Undo
        /// removes both the old- and replacement-material values together.
        /// </summary>
        private void CaptureInterveningTintCarryOrigin()
        {
            if (_pendingTintCarryOrigin != null)
                _pendingTintCarryOrigin = _captureCoalesceOrigin?.Invoke();
        }

        private void ReloadColors()
        {
            foreach (var color in Colors)
                color.Reload();
        }

        private ItemFieldCellViewModel Single(
            string label,
            string field,
            string part,
            bool allowsNone = false) =>
            new(
                label,
                () => Read(field),
                value => Write(label, value, field),
                0,
                byte.MaxValue,
                options: Options(part, allowsNone));

        private BodyPartPairViewModel Pair(LimbDefinition definition) =>
            new(
                definition.Label,
                () => Read(definition.LeftField),
                () => Read(definition.RightField),
                value => Write($"Left {definition.Label}", value, definition.LeftField),
                value => Write($"Right {definition.Label}", value, definition.RightField),
                value => Write(
                    $"Left {definition.Label}", value, definition.LeftField, definition.RightField),
                () => MirrorRightFromLeft,
                0,
                byte.MaxValue,
                Options(definition.LeftPart, definition.AllowsNone),
                Options(definition.RightPart, definition.AllowsNone));

        private int Read(string field) =>
            (int)(_store.GetInteger(BehaviorFieldStorage.Field, field) ?? 0);

        private bool Write(string label, int value, params string[] fields)
        {
            var applied = _runEdit($"Change {label}", () => WriteFields(value, fields));
            if (applied)
            {
                _pendingTintCarryOrigin = _captureCoalesceOrigin?.Invoke();
                _changed();
            }
            return applied;
        }

        private void WriteFields(int value, params string[] fields)
        {
            foreach (var field in fields)
            {
                _store.SetInteger(BehaviorFieldStorage.Field, field, GffFieldType.Byte, value);
                var twin = "x" + field;
                if (_store.Creature.Contains(twin))
                    _store.SetInteger(BehaviorFieldStorage.Field, twin, GffFieldType.Word, value);
            }
        }

        partial void OnMirrorRightFromLeftChanged(bool value)
        {
            if (_loadingMirror)
                return;

            ApplyPairState(value);
            if (!value)
                return;

            var applied = _runEdit("Mirror right from left", () =>
            {
                foreach (var definition in LimbDefinitions)
                    WriteFields(Read(definition.LeftField), definition.RightField);
            });

            if (!applied)
            {
                _loadingMirror = true;
                try
                {
                    MirrorRightFromLeft = false;
                }
                finally
                {
                    _loadingMirror = false;
                }

                ApplyPairState(false);
                ReloadPairs();
                return;
            }

            _pendingTintCarryOrigin = _captureCoalesceOrigin?.Invoke();
            _changed();
            ReloadPairs();
        }

        private void ApplyPairState(bool mirrored)
        {
            foreach (var pair in Limbs)
                pair.SetMirrored(mirrored);
        }

        private void ReloadPairs()
        {
            foreach (var pair in Limbs)
                pair.Reload();
        }

        private bool StoredPairsMatch() => LimbDefinitions.All(definition =>
            Read(definition.LeftField) == Read(definition.RightField));

        private IReadOnlyList<int> Options(string part, bool allowsNone = false)
        {
            var appearance = CurrentAppearance;
            if (appearance == null || string.IsNullOrWhiteSpace(appearance.Race) ||
                _parts == null || !_partCatalogReady)
            {
                return Array.Empty<int>();
            }

            var gender = (_store.GetInteger(BehaviorFieldStorage.Field, "Gender") ?? 0) == 1 ? 'f' : 'm';
            var phenotype = _store.GetInteger(BehaviorFieldStorage.Field, "Phenotype") ?? 0;
            var prefix = $"p{gender}{char.ToLowerInvariant(appearance.Race[0])}{phenotype}_{part}";
            var numbers = _parts.NumbersForModelPrefix(prefix);
            return allowsNone ? ArmorPartCatalog.WithNone(numbers) : numbers;
        }

        private sealed record LimbDefinition(
            string Label,
            string LeftField,
            string RightField,
            string LeftPart,
            string RightPart,
            bool AllowsNone = false);
    }
}
