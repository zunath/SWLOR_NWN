using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.GameData.Lookups;
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

        public ObservableCollection<ItemFieldCellViewModel> Structure { get; } = new();
        public ObservableCollection<ItemFieldCellViewModel> Limbs { get; } = new();
        public ObservableCollection<ItemFieldCellViewModel> Colors { get; } = new();

        public bool IsDynamic => string.Equals(CurrentAppearance?.ModelType, "P", StringComparison.OrdinalIgnoreCase);
        public bool IsFullBody => !IsDynamic;
        public string ModelSummary => CurrentAppearance == null
            ? "The selected model is not available in the loaded game data."
            : IsDynamic
                ? "Segmented model · body parts can be changed below. Paired limbs stay mirrored."
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
            ArmorPartCatalog? parts)
        {
            _store = store;
            _runEdit = runEdit;
            _appearance = appearance;
            _parts = parts;
            Build();
        }

        public void Reload()
        {
            Build();
            OnPropertyChanged(nameof(IsDynamic));
            OnPropertyChanged(nameof(IsFullBody));
            OnPropertyChanged(nameof(ModelSummary));
            OnPropertyChanged(nameof(FullBodyDetails));
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
            Structure.Add(Single("Pelvis", "BodyPart_Pelvis", "pelvis"));

            Limbs.Add(Pair("Bicep", "BodyPart_LBicep", "BodyPart_RBicep", "bicepl", "bicepr"));
            Limbs.Add(Pair("Forearm", "BodyPart_LFArm", "BodyPart_RFArm", "forel", "forer"));
            Limbs.Add(Pair("Hand", "BodyPart_LHand", "BodyPart_RHand", "handl", "handr"));
            Limbs.Add(Pair("Thigh", "BodyPart_LThigh", "BodyPart_RThigh", "legl", "legr"));
            Limbs.Add(Pair("Shin", "BodyPart_LShin", "BodyPart_RShin", "shinl", "shinr"));
            Limbs.Add(Pair("Foot", "BodyPart_LFoot", "ArmorPart_RFoot", "footl", "footr"));

            foreach (var (field, label) in new[]
                     {
                         ("Color_Skin", "Skin"), ("Color_Hair", "Hair"),
                         ("Color_Tattoo1", "Body Color 1"), ("Color_Tattoo2", "Body Color 2")
                     })
            {
                Colors.Add(new ItemFieldCellViewModel(
                    label,
                    () => Read(field),
                    value => Write(label, value, field),
                    0,
                    byte.MaxValue));
            }
        }

        private ItemFieldCellViewModel Single(string label, string field, string part)
        {
            return new ItemFieldCellViewModel(
                label,
                () => Read(field),
                value => Write(label, value, field),
                0,
                byte.MaxValue,
                options: Options(part));
        }

        private ItemFieldCellViewModel Pair(
            string label,
            string leftField,
            string rightField,
            string leftPart,
            string rightPart)
        {
            var left = Options(leftPart);
            var right = Options(rightPart);
            var options = left.Count == 0 || right.Count == 0
                ? left.Concat(right).Distinct().Order().ToList()
                : left.Intersect(right).Order().ToList();
            return new ItemFieldCellViewModel(
                label,
                () => Read(leftField),
                value => Write(label, value, leftField, rightField),
                0,
                byte.MaxValue,
                options: options);
        }

        private int Read(string field) =>
            (int)(_store.GetInteger(BehaviorFieldStorage.Field, field) ?? 0);

        private bool Write(string label, int value, params string[] fields) =>
            _runEdit($"Change {label}", () =>
            {
                foreach (var field in fields)
                {
                    _store.SetInteger(BehaviorFieldStorage.Field, field, GffFieldType.Byte, value);
                    var twin = "x" + field;
                    if (_store.Creature.Contains(twin))
                        _store.SetInteger(BehaviorFieldStorage.Field, twin, GffFieldType.Word, value);
                }
            });

        private IReadOnlyList<int> Options(string part)
        {
            var appearance = CurrentAppearance;
            if (appearance == null || string.IsNullOrWhiteSpace(appearance.Race) || _parts == null)
                return Array.Empty<int>();
            var gender = (_store.GetInteger(BehaviorFieldStorage.Field, "Gender") ?? 0) == 1 ? 'f' : 'm';
            var phenotype = _store.GetInteger(BehaviorFieldStorage.Field, "Phenotype") ?? 0;
            var prefix = $"p{gender}{char.ToLowerInvariant(appearance.Race[0])}{phenotype}_{part}";
            return _parts.NumbersForModelPrefix(prefix);
        }
    }
}
