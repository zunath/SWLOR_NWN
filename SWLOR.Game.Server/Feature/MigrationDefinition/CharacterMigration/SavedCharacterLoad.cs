using System.Linq;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// The changes loading a character makes to its saved items, which the migrations were written to see, in load order:
/// the native conversion of retired property rows (<see cref="SavedLegacyItemProperties"/>); the repair of retired
/// basic weapons (<see cref="BasicVibrobladeCompatibility.Normalize"/>) and canonical base items for pistols and blaster
/// ammunition (<see cref="PistolBaseItemCompatibility.GetCanonicalBaseItem"/>) that the acquire event used to make;
/// then the module equip event for each worn item, which gives a weapon without a Delay property its default one
/// (<see cref="WeaponDelay.ApplyWeaponDelayFallback"/>). The upgrade applies them first so every migration sees
/// the items a loaded character holds. Login no longer repairs legacy items, so the upgrade is the only conversion.
/// </summary>
internal static class SavedCharacterLoad
{
    public static void Apply(SavedCharacter character)
    {
        SavedLegacyItemProperties.ConvertOnLoad(character);
        foreach (var item in character.Equipped.Concat(character.CarriedRecursive).ToArray())
        {
            SavedWeaponDamageTypeMigration.NormalizeBasicWeapon(item);
            var canonical = PistolBaseItemCompatibility.GetCanonicalBaseItem(item.BaseItem, item.ResRef);
            if (canonical != item.BaseItem)
                item.BaseItem = canonical;
        }
        foreach (var item in character.Equipped)
            ApplyDelayPropertyIfMissing(item);
    }

    /// <summary>Saved-data <see cref="WeaponDelay.ApplyDelayPropertyIfMissing"/>.</summary>
    private static void ApplyDelayPropertyIfMissing(SavedItem item)
    {
        var delay = WeaponDelay.GetWeaponDelay(item.BaseItem);
        if (!delay.HasValue || item.Properties.Any(property => property.Type == ItemPropertyType.Delay))
            return;
        item.AddProperty(ItemPropertyType.Delay, -1, (int)delay.Value, AddItemPropertyPolicy.IgnoreExisting);
    }
}
