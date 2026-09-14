using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Toolset.Domain.Editors.Creatures;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SWLOR.Toolset.Editors.Creatures
{
    /// <summary>Stats stored on one creature weapon slot.</summary>
    public sealed partial class CreatureWeaponViewModel : ObservableObject
    {
        private readonly int _slot;
        private readonly CreatureEquipmentSet _equipment;
        private readonly Func<string, Action, bool> _runEdit;
        private string? _lastResRef;

        public string Label { get; }
        public CreatureStatCellViewModel Damage { get; }
        public CreatureStatCellViewModel Delay { get; }
        public CreatureOptionCellViewModel DamageType { get; }
        public CreatureOptionCellViewModel DamageStat { get; }
        public bool Exists => !string.IsNullOrWhiteSpace(_equipment.EquippedResRef(_slot));
        public bool IsEnabled
        {
            get => Exists;
            set
            {
                if (value == Exists)
                    return;

                if (value)
                {
                    var restore = _lastResRef;
                    if (_runEdit($"Enable {Label.ToLowerInvariant()}", () =>
                        {
                            if (!string.IsNullOrWhiteSpace(restore))
                                _equipment.SetEquippedResRef(_slot, restore);
                            else
                                EnsureWeapon();
                        }))
                    {
                        _lastResRef = _equipment.EquippedResRef(_slot);
                    }
                }
                else
                {
                    var current = _equipment.EquippedResRef(_slot);
                    if (!string.IsNullOrWhiteSpace(current) &&
                        _runEdit($"Disable {Label.ToLowerInvariant()}", () =>
                            _equipment.SetEquippedResRef(_slot, null)))
                    {
                        _lastResRef = current;
                    }
                }

                Reload();
            }
        }

        public CreatureWeaponViewModel(
            string label,
            int slot,
            CreatureEquipmentSet equipment,
            Func<string, Action, bool> runEdit)
        {
            Label = label;
            _slot = slot;
            _equipment = equipment;
            _runEdit = runEdit;

            Damage = Numeric("DMG", CreaturePropertyCatalog.Damage, 34, 0);
            Delay = Numeric("Delay", CreaturePropertyCatalog.Delay, 52, 24);
            DamageType = Exclusive(
                "Damage Type",
                Enum.GetValues<CombatDamageType>()
                    .Where(value => value != CombatDamageType.Invalid && (int)value < 20)
                    .Select(value => new CreatureOption((int)value, value.ToString()))
                    .ToList(),
                CreaturePropertyCatalog.WeaponDamageType);
            DamageStat = Exclusive(
                "Damage Stat",
                Enum.GetValues<AbilityType>()
                    .Where(value => value != AbilityType.Invalid)
                    .Select(value => new CreatureOption((int)value, Humanize(value.ToString())))
                    .ToList(),
                CreaturePropertyCatalog.DamageStat);
        }

        public void Reload()
        {
            Damage.Reload();
            Delay.Reload();
            DamageType.Reload();
            DamageStat.Reload();
            OnPropertyChanged(nameof(Exists));
            OnPropertyChanged(nameof(IsEnabled));
        }

        private CreatureStatCellViewModel Numeric(
            string label,
            int propertyId,
            int costTable,
            int fallback)
        {
            return new CreatureStatCellViewModel(
                label,
                () => _equipment.ForSlot(_slot)?.Store.GetPropertyValue(propertyId, -1) ?? fallback,
                value => _runEdit($"Change {Label} {label}", () =>
                {
                    var weapon = EnsureWeapon();
                    weapon.Store.SetPropertyValue(propertyId, -1, costTable,
                        value == fallback && propertyId != CreaturePropertyCatalog.Delay ? null : value);
                }));
        }

        private CreatureOptionCellViewModel Exclusive(
            string label,
            IReadOnlyList<CreatureOption> options,
            int propertyId)
        {
            return new CreatureOptionCellViewModel(
                label,
                options,
                () => _equipment.ForSlot(_slot)?.Store.Properties
                    .Where(property => property.PropertyId == propertyId)
                    .Select(property => (int?)property.SubtypeId)
                    .FirstOrDefault(),
                value => _runEdit($"Change {Label} {label}", () =>
                {
                    var weapon = EnsureWeapon();
                    if (value.HasValue)
                        weapon.Store.SetExclusiveProperty(propertyId, value.Value, 0);
                    else
                        weapon.Store.ClearProperty(propertyId);
                }));
        }

        private CreatureEquipmentDocument EnsureWeapon() => _equipment.Ensure(
            _slot,
            69,
            _slot switch
            {
                CreaturePropertyCatalog.MainWeaponSlot => "_w1",
                CreaturePropertyCatalog.OffWeaponSlot => "_w2",
                _ => "_w3"
            },
            Label);

        private static string Humanize(string value) =>
            System.Text.RegularExpressions.Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2");
    }
}
