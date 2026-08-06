using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Items;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Real model-backed variants for segmented creature bodies.</summary>
    public sealed partial class CreatureBodyPartsViewModel : ObservableObject
    {
        private readonly Domain.Editors.Creatures.CreatureValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Func<int, AppearanceRow?> _appearance;
        private readonly ArmorPartCatalog? _parts;
        private readonly ArmorDyeSwatchService? _colorPalettes;
        private readonly Action _changed;
        private int _generation;
        private bool _loaded;
        private bool _partCatalogReady = true;
        private Task? _loadTask;
        private bool _loadingMirror;

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
        public ObservableCollection<ItemDyeCellViewModel> Colors { get; } = new();

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
            Action changed)
        {
            _store = store;
            _runEdit = runEdit;
            _appearance = appearance;
            _parts = parts;
            _colorPalettes = colorPalettes;
            _changed = changed;
        }

        public void Reload()
        {
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
            if (!IsDynamic)
                return;

            Structure.Add(Single("Head", "Appearance_Head", "head"));
            Structure.Add(Single("Neck", "BodyPart_Neck", "neck"));
            Structure.Add(Single("Torso", "BodyPart_Torso", "chest"));
            Structure.Add(Single("Belt", "BodyPart_Belt", "belt", allowsNone: true));
            Structure.Add(Single("Pelvis", "BodyPart_Pelvis", "pelvis"));

            foreach (var definition in LimbDefinitions)
                Limbs.Add(Pair(definition));

            foreach (var (field, label, palette) in new[]
                     {
                         ("Color_Skin", "Skin", ArmorDyeSwatchService.DyeMaterial.Skin),
                         ("Color_Hair", "Hair", ArmorDyeSwatchService.DyeMaterial.Hair),
                         ("Color_Tattoo1", "Body Color 1", ArmorDyeSwatchService.DyeMaterial.Tattoo),
                         ("Color_Tattoo2", "Body Color 2", ArmorDyeSwatchService.DyeMaterial.Tattoo)
                     })
            {
                Colors.Add(new ItemDyeCellViewModel(
                    label,
                    () => Read(field),
                    value => Write(label, value, field),
                    _colorPalettes?.GetPaletteColors(palette) ?? Array.Empty<(byte, byte, byte)>(),
                    allowsNumericFallback: false));
            }
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
                _changed();
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
