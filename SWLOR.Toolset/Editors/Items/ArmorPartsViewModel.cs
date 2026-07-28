using CommunityToolkit.Mvvm.ComponentModel;
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
    /// While mirrored, editing the left cell writes both sides (and each side's "x" twin, when the
    /// document carries one); turning mirroring off exposes the right cells so they can diverge, and
    /// editing either side then writes only that side.
    /// </remarks>
    public sealed partial class ArmorPartsViewModel : ObservableObject
    {
        private readonly ItemValueStore _store;
        private readonly Func<string, Action, bool> _runEdit;
        private readonly Action? _appearanceChanged;
        private readonly List<ItemFieldCellViewModel> _allCells = new();

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

        public ItemFieldCellViewModel Cloth1 { get; }
        public ItemFieldCellViewModel Cloth2 { get; }
        public ItemFieldCellViewModel Leather1 { get; }
        public ItemFieldCellViewModel Leather2 { get; }
        public ItemFieldCellViewModel Metal1 { get; }
        public ItemFieldCellViewModel Metal2 { get; }

        /// <summary>
        /// Whether the seven left/right pairs are edited together. Defaults to whatever the document
        /// already shows: true only when every pair's stored values already match.
        /// </summary>
        [ObservableProperty]
        private bool _mirrorRightFromLeft;

        /// <summary>Whether the right-side cells should be shown at all - only once mirroring is off.</summary>
        public bool ShowsRightCells => !MirrorRightFromLeft;

        public ArmorPartsViewModel(
            ItemValueStore store, Func<string, Action, bool> runEdit, Action? appearanceChanged = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _runEdit = runEdit ?? throw new ArgumentNullException(nameof(runEdit));
            _appearanceChanged = appearanceChanged;

            _mirrorRightFromLeft = DetectMirror();

            Neck = CreateSingle("Neck", ItemAppearanceFieldNames.Neck, ItemAppearanceFieldNames.NeckTwin);
            Torso = CreateSingle("Torso", ItemAppearanceFieldNames.Torso, ItemAppearanceFieldNames.TorsoTwin);
            Belt = CreateSingle("Belt", ItemAppearanceFieldNames.Belt, ItemAppearanceFieldNames.BeltTwin);
            Pelvis = CreateSingle("Pelvis", ItemAppearanceFieldNames.Pelvis, ItemAppearanceFieldNames.PelvisTwin);
            Robe = CreateSingle("Robe", ItemAppearanceFieldNames.Robe, ItemAppearanceFieldNames.RobeTwin);

            (LeftShoulder, RightShoulder) = CreatePair(ItemAppearanceFieldNames.Shoulder);
            (LeftBicep, RightBicep) = CreatePair(ItemAppearanceFieldNames.Bicep);
            (LeftForearm, RightForearm) = CreatePair(ItemAppearanceFieldNames.Forearm);
            (LeftHand, RightHand) = CreatePair(ItemAppearanceFieldNames.Hand);
            (LeftThigh, RightThigh) = CreatePair(ItemAppearanceFieldNames.Thigh);
            (LeftShin, RightShin) = CreatePair(ItemAppearanceFieldNames.Shin);
            (LeftFoot, RightFoot) = CreatePair(ItemAppearanceFieldNames.Foot);

            Cloth1 = CreateDye("Cloth 1", ItemAppearanceFieldNames.Cloth1Color);
            Cloth2 = CreateDye("Cloth 2", ItemAppearanceFieldNames.Cloth2Color);
            Leather1 = CreateDye("Leather 1", ItemAppearanceFieldNames.Leather1Color);
            Leather2 = CreateDye("Leather 2", ItemAppearanceFieldNames.Leather2Color);
            Metal1 = CreateDye("Metal 1", ItemAppearanceFieldNames.Metal1Color);
            Metal2 = CreateDye("Metal 2", ItemAppearanceFieldNames.Metal2Color);

            _allCells.AddRange(new[]
            {
                Neck, Torso, Belt, Pelvis, Robe,
                LeftShoulder, RightShoulder, LeftBicep, RightBicep, LeftForearm, RightForearm,
                LeftHand, RightHand, LeftThigh, RightThigh, LeftShin, RightShin, LeftFoot, RightFoot,
                Cloth1, Cloth2, Leather1, Leather2, Metal1, Metal2
            });
        }

        /// <summary>Re-reads every cell after an undo, redo, or external reload.</summary>
        public void ReloadFromDocument()
        {
            foreach (var cell in _allCells)
                cell.Reload();
        }

        partial void OnMirrorRightFromLeftChanged(bool value) => OnPropertyChanged(nameof(ShowsRightCells));

        /// <summary>True only when every pair's left and right value already match.</summary>
        private bool DetectMirror()
        {
            foreach (var pair in ItemAppearanceFieldNames.Pairs)
            {
                var left = _store.GetInteger(BehaviorFieldStorage.Field, pair.LeftField) ?? 0;
                var right = _store.GetInteger(BehaviorFieldStorage.Field, pair.RightField) ?? 0;
                if (left != right)
                    return false;
            }

            return true;
        }

        private ItemFieldCellViewModel CreateSingle(string label, string field, string? twinField) =>
            new(
                label,
                () => (int?)_store.GetInteger(BehaviorFieldStorage.Field, field),
                value => Apply(label, () => WriteArmorField(field, twinField, value)),
                0, 255);

        private ItemFieldCellViewModel CreateDye(string label, string field) =>
            new(
                label,
                () => (int?)_store.GetInteger(BehaviorFieldStorage.Field, field),
                value => Apply(
                    label, () => _store.SetInteger(BehaviorFieldStorage.Field, field, GffFieldType.Byte, value)),
                0, 175);

        /// <summary>
        /// Builds a mirrored pair. The left cell's write closure decides at write time - not at
        /// construction - whether mirroring is on, so toggling <see cref="MirrorRightFromLeft"/>
        /// changes what the very next left-side edit does without rebuilding either cell.
        /// </summary>
        private (ItemFieldCellViewModel Left, ItemFieldCellViewModel Right) CreatePair(ItemArmorPartFieldPair pair)
        {
            ItemFieldCellViewModel? right = null;

            var left = new ItemFieldCellViewModel(
                $"Left {pair.Label}",
                () => (int?)_store.GetInteger(BehaviorFieldStorage.Field, pair.LeftField),
                value =>
                {
                    var applied = Apply($"Left {pair.Label}", () =>
                    {
                        WriteArmorField(pair.LeftField, pair.LeftTwinField, value);
                        if (MirrorRightFromLeft)
                            WriteArmorField(pair.RightField, pair.RightTwinField, value);
                    });

                    // The right cell's own OnValueChanged never fired for this edit, so its display
                    // has to be told directly that mirroring just changed what it shows.
                    if (applied && MirrorRightFromLeft)
                        right?.Reload();

                    return applied;
                },
                0, 255);

            right = new ItemFieldCellViewModel(
                $"Right {pair.Label}",
                () => (int?)_store.GetInteger(BehaviorFieldStorage.Field, pair.RightField),
                value => Apply(
                    $"Right {pair.Label}", () => WriteArmorField(pair.RightField, pair.RightTwinField, value)),
                0, 255);

            return (left, right);
        }

        /// <summary>
        /// Writes a byte-typed ArmorPart_* field, and its word-typed "x" twin too when the document
        /// already carries one - never adding a twin that was not already there.
        /// </summary>
        private void WriteArmorField(string field, string? twinField, int value)
        {
            _store.SetInteger(BehaviorFieldStorage.Field, field, GffFieldType.Byte, value);
            if (twinField != null && _store.Item.Contains(twinField))
                _store.SetInteger(BehaviorFieldStorage.Field, twinField, GffFieldType.Word, value);
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
