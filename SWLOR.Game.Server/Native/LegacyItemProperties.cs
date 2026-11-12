using System;
using System.Collections.Generic;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Feature.MigrationDefinition;

namespace SWLOR.Game.Server.Native;

/// <summary>
/// Converts retired item-property rows in serialized GFF before the native item loader
/// validates them and discards properties that the current property tables no longer know.
/// </summary>
public static class LegacyItemProperties
{
    public const string ConvertedVariable = "LEGACY_ITEM_PROPERTIES";
    private static readonly Dictionary<ushort, int> DamageBonusAmountByRow = new();

    /// <summary>
    /// Fills the damage bonus table this build's conversions read, then enables the <c>CNWSItem::LoadItem</c> hook
    /// (<c>SWLOR.Game.NativeHooks.LegacyItemPropertiesTrampolines</c>), which installs once for the process. Called from
    /// module preload, before any native item load.
    /// </summary>
    public static void RegisterHook()
    {
        CacheDamageBonusAmounts();
        NativeHookAccess.Hooks.Enable(NativeHookId.LegacyItemProperties);
    }

    /// <summary>
    /// The DMG amount a saber's legacy Damage Bonus row converts to on load, for saved data converted outside
    /// the engine. Filled when the hook registers at module load.
    /// </summary>
    internal static bool TryGetLegacyDamageBonus(ushort costValue, out int damage) =>
        DamageBonusAmountByRow.TryGetValue(costValue, out damage);

    private static void CacheDamageBonusAmounts()
    {
        DamageBonusAmountByRow.Clear();
        for (var row = 0; row < Get2DARowCount("iprp_damagecost"); row++)
        {
            if (!int.TryParse(Get2DAString("iprp_damagecost", "NumDice", row), out var dice) ||
                !int.TryParse(Get2DAString("iprp_damagecost", "Die", row), out var die))
                continue;

            DamageBonusAmountByRow[(ushort)row] = Math.Min(100, SaberRecalibration.CalculateLegacyDamageBonus(dice, die));
        }
    }
}
