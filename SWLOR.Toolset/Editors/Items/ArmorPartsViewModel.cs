using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Editors.Items
{
    /// <summary>
    /// The Appearance tab's surface for a ModelType 3 (armor) base item: the five unpaired body-part
    /// fields, the seven left/right pairs, and the six dye channels.
    /// </summary>
    /// <remarks>
    /// A pair defaults to mirrored when the document already carries matching left/right values -
    /// which is every shipped armor blueprint, since nothing before this editor could set them apart.
    /// While mirrored, editing the left cell writes both sides and keeps each side's extended
    /// companion synchronized; the right cell stays visible but read-only, always showing the left
    /// side's current value. Turning mirroring off makes the right cells editable again so they can
    /// diverge; turning it back on immediately writes the right side from the left (the mockup's "the
    /// mirror check writes the right side from the left"), as one undoable edit.
    /// </remarks>
    public sealed partial class ArmorPartsViewModel : ObservableObject
    {
        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action? _appearanceChanged;
        private readonly ArmorDyeSwatchService? _dyes;
        private readonly ArmorPartCatalog? _partModels;
        private readonly List<ItemFieldCellViewModel> _allCells = new();
        private readonly List<ItemDyeCellViewModel> _dyeCells = new();
        private readonly List<BodyPartPairViewModel> _pairBindings = new();
        private bool _loadingMirror;

        public ItemFieldCellViewModel Neck { get; }
        public ItemFieldCellViewModel Torso { get; }
        public ItemFieldCellViewModel Belt { get; }
        public ItemFieldCellViewModel Pelvis { get; }
        public ItemFieldCellViewModel Robe { get; }

        public ItemFieldCellViewModel LeftShoulder { get; }
        public ItemFieldCellViewModel RightShoulder { get; }
        public ItemFieldCellViewModel LeftBicep { get; }
        public ItemFieldCellViewModel RightBicep { get; }
        public ItemFieldCellViewModel LeftForearm { get; }
        public ItemFieldCellViewModel RightForearm { get; }
        public ItemFieldCellViewModel LeftHand { get; }
        public ItemFieldCellViewModel RightHand { get; }
        public ItemFieldCellViewModel LeftThigh { get; }
        public ItemFieldCellViewModel RightThigh { get; }
        public ItemFieldCellViewModel LeftShin { get; }
        public ItemFieldCellViewModel RightShin { get; }
        public ItemFieldCellViewModel LeftFoot { get; }
        public ItemFieldCellViewModel RightFoot { get; }

        public ItemDyeCellViewModel Cloth1 { get; }
        public ItemDyeCellViewModel Cloth2 { get; }
        public ItemDyeCellViewModel Leather1 { get; }
        public ItemDyeCellViewModel Leather2 { get; }
        public ItemDyeCellViewModel Metal1 { get; }
        public ItemDyeCellViewModel Metal2 { get; }

        /// <summary>
        /// Whether the seven left/right pairs are edited together. Defaults to whatever the document
        /// already shows: true only when every pair's stored values already match.
        /// </summary>
        [ObservableProperty]
        private bool _mirrorRightFromLeft;

        public ArmorPartsViewModel(
            ItemValueStore store,
            Func<string, Action, bool> runEdit,
            Action? appearanceChanged = null,
            ArmorDyeSwatchService? dyes = null,
            ArmorPartCatalog? partModels = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _appearanceChanged = appearanceChanged;
            _dyes = dyes;
            _partModels = partModels;

            _mirrorRightFromLeft = DetectMirror();

            Neck = CreateSingle("Neck", ItemAppearanceFieldNames.Neck, "neck");
            Torso = CreateSingle("Torso", ItemAppearanceFieldNames.Torso, "chest");
            Belt = CreateSingle("Belt", ItemAppearanceFieldNames.Belt, "belt");
            Pelvis = CreateSingle("Pelvis", ItemAppearanceFieldNames.Pelvis, "pelvis");
            Robe = CreateSingle("Robe", ItemAppearanceFieldNames.Robe, "robe");

            (LeftShoulder, RightShoulder) = Cells(CreatePair(ItemAppearanceFieldNames.Shoulder, "shol", "shor"));
            (LeftBicep, RightBicep) = Cells(CreatePair(ItemAppearanceFieldNames.Bicep, "bicepl", "bicepr"));
            (LeftForearm, RightForearm) = Cells(CreatePair(ItemAppearanceFieldNames.Forearm, "forel", "forer"));
            (LeftHand, RightHand) = Cells(CreatePair(ItemAppearanceFieldNames.Hand, "handl", "handr"));
            (LeftThigh, RightThigh) = Cells(CreatePair(ItemAppearanceFieldNames.Thigh, "legl", "legr"));
            (LeftShin, RightShin) = Cells(CreatePair(ItemAppearanceFieldNames.Shin, "shinl", "shinr"));
            (LeftFoot, RightFoot) = Cells(CreatePair(ItemAppearanceFieldNames.Foot, "footl", "footr"));

            Cloth1 = CreateDye("Cloth 1", ItemAppearanceFieldNames.Cloth1Color,
                ArmorDyeSwatchService.DyeMaterial.Cloth, TintMapLayerType.Cloth1);
            Cloth2 = CreateDye("Cloth 2", ItemAppearanceFieldNames.Cloth2Color,
                ArmorDyeSwatchService.DyeMaterial.Cloth, TintMapLayerType.Cloth2);
            Leather1 = CreateDye("Leather 1", ItemAppearanceFieldNames.Leather1Color,
                ArmorDyeSwatchService.DyeMaterial.Leather, TintMapLayerType.Leather1);
            Leather2 = CreateDye("Leather 2", ItemAppearanceFieldNames.Leather2Color,
                ArmorDyeSwatchService.DyeMaterial.Leather, TintMapLayerType.Leather2);
            Metal1 = CreateDye("Metal 1", ItemAppearanceFieldNames.Metal1Color,
                ArmorDyeSwatchService.DyeMaterial.Metal1, TintMapLayerType.Metal1);
            Metal2 = CreateDye("Metal 2", ItemAppearanceFieldNames.Metal2Color,
                ArmorDyeSwatchService.DyeMaterial.Metal2, TintMapLayerType.Metal2);

            _allCells.AddRange(new[]
            {
                Neck, Torso, Belt, Pelvis, Robe,
                LeftShoulder, RightShoulder, LeftBicep, RightBicep, LeftForearm, RightForearm,
                LeftHand, RightHand, LeftThigh, RightThigh, LeftShin, RightShin, LeftFoot, RightFoot
            });
            _dyeCells.AddRange(new[] { Cloth1, Cloth2, Leather1, Leather2, Metal1, Metal2 });
        }

        /// <summary>Re-reads every cell after an undo, redo, or external reload.</summary>
        public void ReloadFromDocument()
        {
            var mirrored = DetectMirror();
            _loadingMirror = true;
            try
            {
                MirrorRightFromLeft = mirrored;
            }
            finally
            {
                _loadingMirror = false;
            }

            ApplyPairState(mirrored);

            foreach (var cell in _allCells)
                cell.Reload();
            foreach (var dye in _dyeCells)
                dye.Reload();
        }

        /// <summary>
        /// Fills every field the document does not carry yet with the plain-body baseline - part 1
        /// for the body pieces, 0 for shoulder/belt/robe (none) and the dye channels - as one
        /// undoable edit, so a base-type swap to armor starts from a dressed body instead of a sea
        /// of empty boxes. Fields the document already stores keep their values untouched.
        /// </summary>
        public void EnsureDefaults()
        {
            var missing = new List<(string Field, int Value, bool IsDye)>();

            void Single(string field, int value)
            {
                if (ItemAppearanceValues.Read(_store.Item, field) is null)
                    missing.Add((field, value, false));
            }

            Single(ItemAppearanceFieldNames.Neck, 1);
            Single(ItemAppearanceFieldNames.Torso, 1);
            Single(ItemAppearanceFieldNames.Belt, 0);
            Single(ItemAppearanceFieldNames.Pelvis, 1);
            Single(ItemAppearanceFieldNames.Robe, 0);

            foreach (var pair in ItemAppearanceFieldNames.Pairs)
            {
                // Shoulders default to "none" like belts and robes; every other limb piece has a
                // plain part 1 model to stand in.
                var value = pair.Label == "Shoulder" ? 0 : 1;
                Single(pair.LeftField, value);
                Single(pair.RightField, value);
            }

            foreach (var dye in new[]
                     {
                         ItemAppearanceFieldNames.Cloth1Color, ItemAppearanceFieldNames.Cloth2Color,
                         ItemAppearanceFieldNames.Leather1Color, ItemAppearanceFieldNames.Leather2Color,
                         ItemAppearanceFieldNames.Metal1Color, ItemAppearanceFieldNames.Metal2Color
                     })
            {
                if (_store.GetInteger(BehaviorFieldStorage.Field, dye) is null)
                    missing.Add((dye, 0, true));
            }

            if (missing.Count == 0)
                return;

            var applied = _runEdit("Set armor defaults", () =>
            {
                foreach (var (field, value, isDye) in missing)
                {
                    if (isDye)
                        _store.SetInteger(BehaviorFieldStorage.Field, field, GffFieldType.Byte, value);
                    else
                        WriteArmorField(field, value);
                }
            });

            if (!applied)
                return;

            _appearanceChanged?.Invoke();
            ReloadFromDocument();
        }

        /// <summary>
        /// Turning mirroring off just makes the right cells editable again - whatever they already
        /// stored stands. Turning it on is the mockup's "the mirror check writes the right side from
        /// the left": every pair's right field (and its "x" twin) is written from the left field's
        /// current stored value, as one undoable edit, and every right cell reloads to show it
        /// immediately rather than waiting for the next left-side edit.
        /// </summary>
        partial void OnMirrorRightFromLeftChanged(bool value)
        {
            if (_loadingMirror)
                return;

            ApplyPairState(value);
            if (!value)
                return;

            var applied = _runEdit("Mirror right from left", () =>
            {
                foreach (var pair in ItemAppearanceFieldNames.Pairs)
                {
                    var leftValue = ItemAppearanceValues.Read(_store.Item, pair.LeftField) ?? 0;
                    WriteArmorField(pair.RightField, leftValue);
                }
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
                ReloadCells();
                return;
            }

            _appearanceChanged?.Invoke();
            ReloadCells();
        }

        /// <summary>True only when every pair's left and right value already match.</summary>
        private bool DetectMirror()
        {
            foreach (var pair in ItemAppearanceFieldNames.Pairs)
            {
                var left = ItemAppearanceValues.Read(_store.Item, pair.LeftField) ?? 0;
                var right = ItemAppearanceValues.Read(_store.Item, pair.RightField) ?? 0;
                if (left != right)
                    return false;
            }

            return true;
        }

        private ItemFieldCellViewModel CreateSingle(
            string label, string field, string partType) =>
            new(
                label,
                () => ItemAppearanceValues.Read(_store.Item, field),
                value => Apply(label, () => WriteArmorField(field, value)),
                0, ushort.MaxValue,
                options: PartNumbers(partType));

        /// <summary>The variants that exist for a part, plus 0 - "this armor covers nothing here".</summary>
        private IReadOnlyList<int> PartNumbers(string partType)
        {
            var numbers = _partModels?.Numbers(partType) ?? Array.Empty<int>();
            return ArmorPartCatalog.WithNone(numbers);
        }

        private ItemDyeCellViewModel CreateDye(
            string label,
            string field,
            ArmorDyeSwatchService.DyeMaterial material,
            TintMapLayerType layer) =>
            new(
                label,
                () => (int?)_store.GetInteger(BehaviorFieldStorage.Field, field),
                value => Apply(
                    label, () =>
                    {
                        _store.SetInteger(BehaviorFieldStorage.Field, field, GffFieldType.Byte, value);
                        ClearGlobalCustomTint(layer);
                    }),
                _dyes?.GetPaletteColors(material) ?? Array.Empty<(byte, byte, byte)>(),
                hasExternalOverride: () => GetTintVariableKeys(layer).Count > 0);

        /// <summary>
        /// Restores the selected global dye channel to its palette value without discarding an
        /// independently customized material in the same channel. The explicit global-intent
        /// marker identifies which material overrides were written by the previous global custom
        /// color. Older blueprints have no marker, so a complete uniform set of custom values is
        /// the only legacy state that is safe to interpret as one global color.
        /// </summary>
        private void ClearGlobalCustomTint(TintMapLayerType layer)
        {
            var stateVariable = TintMapVariable.GetItemGlobalColorStateName(layer);
            var inheritanceStateVariable = TintMapVariable.GetItemGlobalInheritanceStateName(layer);
            var usesExplicitInheritance = _store.Locals.GetInt(inheritanceStateVariable) != null;
            var savedGlobalColor = _store.Locals.GetInt(stateVariable);
            var tintVariableKeys = GetTintVariableKeys(layer);
            int? globalColor = savedGlobalColor.HasValue &&
                               TintMapColor.TryFromStoredValue(savedGlobalColor.Value, out _)
                ? savedGlobalColor.Value
                : null;

            if (!globalColor.HasValue && tintVariableKeys.Count > 1)
            {
                var legacyColors = tintVariableKeys
                    .Select(key => _store.Locals.GetInt(key))
                    .ToList();
                var distinctCustomColors = legacyColors
                    .Where(value => value.HasValue &&
                                    TintMapColor.TryFromStoredValue(value.Value, out _))
                    .Select(value => value!.Value)
                    .Distinct()
                    .ToList();
                if (legacyColors.All(value => value.HasValue &&
                                              TintMapColor.TryFromStoredValue(value.Value, out _)) &&
                    distinctCustomColors.Count == 1)
                {
                    globalColor = distinctCustomColors[0];
                }
            }

            if (globalColor.HasValue && !usesExplicitInheritance)
            {
                foreach (var key in tintVariableKeys)
                {
                    if (_store.Locals.GetInt(key) == globalColor)
                        _store.Locals.Remove(key);
                }
            }

            _store.Locals.Remove(stateVariable);
            _store.Locals.Remove(inheritanceStateVariable);
        }

        private IReadOnlyList<string> GetTintVariableKeys(TintMapLayerType layer) =>
            _store.Locals
                .Where(entry =>
                    entry.Type == SWLOR.Toolset.Domain.Documents.VarTable.TypeInt &&
                    TintMapVariable.TryGetLayer(entry.Name, out var variableLayer) &&
                    variableLayer == layer)
                .Select(entry => entry.Name)
                .ToList();

        /// <summary>
        /// Builds a mirrored pair. The left cell's write closure decides at write time - not at
        /// construction - whether mirroring is on, so toggling <see cref="MirrorRightFromLeft"/>
        /// changes what the very next left-side edit does without rebuilding either cell.
        /// </summary>
        private BodyPartPairViewModel CreatePair(
            ItemArmorPartFieldPair pair, string leftPartType, string rightPartType)
        {
            var leftOptions = PartNumbers(leftPartType);
            var rightOptions = PartNumbers(rightPartType);
            var binding = new BodyPartPairViewModel(
                pair.Label,
                () => ItemAppearanceValues.Read(_store.Item, pair.LeftField),
                () => ItemAppearanceValues.Read(_store.Item, pair.RightField),
                value => Apply($"Left {pair.Label}", () => WriteArmorField(pair.LeftField, value)),
                value => Apply($"Right {pair.Label}", () => WriteArmorField(pair.RightField, value)),
                value => Apply($"Left {pair.Label}", () =>
                {
                    WriteArmorField(pair.LeftField, value);
                    WriteArmorField(pair.RightField, value);
                }),
                () => MirrorRightFromLeft,
                0,
                ushort.MaxValue,
                leftOptions,
                rightOptions);
            _pairBindings.Add(binding);
            return binding;
        }

        private static (ItemFieldCellViewModel Left, ItemFieldCellViewModel Right) Cells(
            BodyPartPairViewModel pair) => (pair.Left, pair.Right);

        /// <summary>
        /// Writes the legacy byte field and keeps its word-sized NWN:EE companion synchronized.
        /// </summary>
        private void WriteArmorField(string field, int value)
        {
            ItemAppearanceValues.Write(_store, field, value);
        }

        private void ApplyPairState(bool mirrored)
        {
            foreach (var binding in _pairBindings)
                binding.SetMirrored(mirrored);
        }

        private void ReloadCells()
        {
            foreach (var cell in _allCells)
                cell.Reload();
        }

        private bool Apply(string label, Action mutation)
        {
            var applied = _runEdit($"Set {label}", mutation);
            if (applied)
                _appearanceChanged?.Invoke();
            return applied;
        }

    }
}
