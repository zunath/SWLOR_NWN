using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.NativeSinks;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWScript.Enum.Item;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Native;

/// <summary>
/// Converts retired item-property rows in serialized GFF before the native item loader
/// validates them and discards properties that the current property tables no longer know.
/// </summary>
public static unsafe class LegacyItemProperties
{
    public const string ConvertedVariable = "LEGACY_ITEM_PROPERTIES";
    private static readonly Dictionary<ushort, int> DamageBonusAmountByRow = new();
    private static FunctionHook* _loadItemHook;
    private static ILegacyItemPropertiesSink _sink;

    public static void RegisterHook()
    {
        if (_loadItemHook != null)
            return;

        _sink ??= new LegacyItemPropertiesSink();
        CacheDamageBonusAmounts();
        delegate* unmanaged<void*, void*, void*, int, int, int> loadItem = &LoadItem;
        _loadItemHook = NWNXAPI.RequestFunctionHook(
            NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
                "_ZN8CNWSItem8LoadItemEP7CResGFFP10CResStructii"),
            (IntPtr)loadItem,
            HookOrder.Early);
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

    [UnmanagedCallersOnly]
    private static int LoadItem(void* item, void* resource, void* structure, int firstFlag, int secondFlag)
        => _sink.LoadItem(item, resource, structure, firstFlag, secondFlag, (nint)_loadItemHook->m_trampoline);
}
