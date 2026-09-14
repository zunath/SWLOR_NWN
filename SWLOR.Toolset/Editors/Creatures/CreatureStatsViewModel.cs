using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Creatures;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Creature attributes plus the linked stat skin.</summary>
    public sealed partial class CreatureStatsViewModel : ObservableObject
    {
        private readonly CreatureValueStore _creature;
        private readonly CreatureEquipmentSet _equipment;
        private readonly Func<string, Action, bool> _runEdit;

        public ObservableCollection<CreatureStatCellViewModel> Vitals { get; } = new();
        public ObservableCollection<CreatureStatCellViewModel> Offense { get; } = new();
        public ObservableCollection<CreatureStatCellViewModel> Defense { get; } = new();
        public ObservableCollection<CreatureStatCellViewModel> Resistances { get; } = new();
        public ObservableCollection<CreatureStatCellViewModel> Attributes { get; } = new();

        public bool HasStatSkin => _equipment.ForSlot(CreaturePropertyCatalog.StatSkinSlot) != null;

        public CreatureStatsViewModel(
            CreatureValueStore creature,
            CreatureEquipmentSet equipment,
            Func<string, Action, bool> runEdit)
        {
            _creature = creature;
            _equipment = equipment;
            _runEdit = runEdit;

            Vitals.Add(Skin("NPC Level", CreaturePropertyCatalog.Level, -1, 43, 0, 100));
            Vitals.Add(Skin("HP", CreaturePropertyCatalog.HitPoints, -1, 39, 0, 30000, combined: true));
            Vitals.Add(Skin("FP", CreaturePropertyCatalog.FocusPoints, -1, 38));
            Vitals.Add(Skin("Stamina", CreaturePropertyCatalog.Stamina, -1, 36));

            Offense.Add(Skin("Attack", CreaturePropertyCatalog.Attack, -1, 45));
            Offense.Add(Skin("Force Attack", CreaturePropertyCatalog.ForceAttack, -1, 45));
            Offense.Add(Skin("Combat Readiness", CreaturePropertyCatalog.CombatReadiness, -1, 42));

            Defense.Add(Skin("Physical", CreaturePropertyCatalog.Defense, 1, 35));
            Defense.Add(Skin("Force", CreaturePropertyCatalog.Defense, 2, 35));
            Defense.Add(Skin("Evasion", CreaturePropertyCatalog.Evasion, -1, 41));

            foreach (var (subtype, label) in new[]
                     {
                         (1, "Fire"), (2, "Poison"), (3, "Electrical"), (4, "Ice"),
                         (100, "Mind"), (101, "Mobility"), (102, "Trauma"), (103, "Disruption")
                     })
            {
                Resistances.Add(Skin(label, CreaturePropertyCatalog.Resistance, subtype, 54,
                    -100, 100, resistance: true));
            }

            foreach (var (field, label) in new[]
                     {
                         ("Str", "Might"), ("Dex", "Perception"), ("Con", "Vitality"),
                         ("Int", "Agility"), ("Wis", "Willpower"), ("Cha", "Social")
                     })
            {
                Attributes.Add(new CreatureStatCellViewModel(
                    label,
                    () => _creature.GetInteger(BehaviorFieldStorage.Field, field) is { } value ? (int)value : 0,
                    value => _runEdit($"Change {label}", () =>
                        _creature.SetInteger(BehaviorFieldStorage.Field, field, GffFieldType.Byte, value)),
                    0,
                    byte.MaxValue));
            }
        }

        public void Reload()
        {
            foreach (var cell in Vitals.Concat(Offense).Concat(Defense).Concat(Resistances).Concat(Attributes))
                cell.Reload();
            OnPropertyChanged(nameof(HasStatSkin));
        }

        private CreatureStatCellViewModel Skin(
            string label,
            int propertyId,
            int subtype,
            int costTable,
            int minimum = 0,
            int maximum = ushort.MaxValue,
            bool resistance = false,
            bool combined = false)
        {
            return new CreatureStatCellViewModel(
                label,
                () =>
                {
                    var skin = _equipment.ForSlot(CreaturePropertyCatalog.StatSkinSlot);
                    var stored = combined
                        ? skin?.Store.GetCombinedPropertyValue(propertyId, subtype) ?? 0
                        : skin?.Store.GetPropertyValue(propertyId, subtype) ?? 0;
                    return resistance ? CreaturePropertyCatalog.DecodeResistance(stored) : stored;
                },
                value => _runEdit($"Change {label}", () =>
                {
                    var skin = EnsureSkin();
                    var stored = resistance ? CreaturePropertyCatalog.EncodeResistance(value) : value;
                    int? optional = stored == 0 ? null : stored;
                    if (combined)
                        skin.Store.SetCombinedPropertyValue(propertyId, subtype, costTable, optional);
                    else
                        skin.Store.SetPropertyValue(propertyId, subtype, costTable, optional);
                }),
                minimum,
                maximum);
        }

        private CreatureEquipmentDocument EnsureSkin() => _equipment.Ensure(
            CreaturePropertyCatalog.StatSkinSlot,
            CreaturePropertyCatalog.StatSkinBaseItem,
            "_sk",
            "Creature Stat Skin");
    }
}
