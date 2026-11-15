using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// The reads of the game's rules that <see cref="SavedCharacterDataStore.Load"/> makes on the main thread before any
/// character is upgraded. They name the Character, Combat and Items services, so they are content that the store
/// (Foundation) calls.
/// </summary>
internal static class SavedCharacterGameRules
{
    /// <summary>These caches fill on first use; fill them before worker threads read them.</summary>
    public static void FillCaches()
    {
        Resistance.IsValidResistanceType(ResistanceType.Fire);
        Resistance.GetAllResistanceTypes();
        CombatDamageCatalog.GetDefenseDamageTypes();
        Perk.GetAllActiveAbilityFeats(PerkType.Invalid);
    }

    /// <summary>
    /// <c>Item.HasInventoryIcon</c> for every attack weapon base item and simple model: whether
    /// <c>Item.ResolveIconResref</c> finds a real icon, which <c>Item.IsEconomyRestricted</c> reads.
    /// Weapons are neither cloaks nor scrolls, so the simple-model and default-icon rules decide it.
    /// </summary>
    public static IReadOnlyDictionary<(int BaseItem, int Model), bool> LoadWeaponIcons(IEnumerable<int> baseItems)
    {
        static bool Exists(string resref) => !string.IsNullOrWhiteSpace(resref) &&
            (ResManGetAliasFor(resref, ResType.TGA) != string.Empty || ResManGetAliasFor(resref, ResType.DDS) != string.Empty);
        var icons = new Dictionary<(int, int), bool>();
        foreach (var row in baseItems)
        {
            if (!Item.IsAttackWeaponType((BaseItem)row))
                continue;
            var defaultIcon = Get2DAString("baseitems", "DefaultIcon", row);
            var hasDefaultIcon = !defaultIcon.StartsWith("iinvalid") && (Exists(defaultIcon) || Exists(defaultIcon + "_001"));
            if (Get2DAString("baseitems", "ModelType", row) != "0")
            {
                icons[(row, -1)] = hasDefaultIcon;
                continue;
            }
            var itemClass = Get2DAString("baseitems", "ItemClass", row);
            var stripped = defaultIcon.Length >= 4 && defaultIcon[^4] == '_' ? defaultIcon[..^4] : defaultIcon;
            for (var model = 0; model < MaximumSimpleModel; model++)
            {
                var id = model.ToString().PadLeft(3, '0');
                icons[(row, model)] = Exists(("i" + itemClass + "_" + id).ToLower()) || Exists(stripped + "_" + id) || hasDefaultIcon;
            }
        }
        return icons;
    }

    private const int MaximumSimpleModel = 1024;
}
