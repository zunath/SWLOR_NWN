using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NWN.Native.API;
using NWNX.NET;
using NWNX.NET.Native;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service;
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
    private const int AccuracyCostTable = 45;
    private const int DamageCostTable = 34;
    private const int MaximumAccuracy = 100;
    private const uint LightsaberBaseItem = (uint)BaseItem.Lightsaber;
    private const uint SaberstaffBaseItem = (uint)BaseItem.Saberstaff;

    private static readonly byte[] PropertiesListLabel = System.Text.Encoding.ASCII.GetBytes("PropertiesList\0");
    private static readonly byte[] BaseItemLabel = System.Text.Encoding.ASCII.GetBytes("BaseItem\0");
    private static readonly byte[] PropertyNameLabel = System.Text.Encoding.ASCII.GetBytes("PropertyName\0");
    private static readonly byte[] CostTableLabel = System.Text.Encoding.ASCII.GetBytes("CostTable\0");
    private static readonly byte[] CostValueLabel = System.Text.Encoding.ASCII.GetBytes("CostValue\0");
    private static readonly byte[] SubtypeLabel = System.Text.Encoding.ASCII.GetBytes("Subtype\0");
    private static readonly Dictionary<ushort, int> DamageBonusAmountByRow = new();
    private static FunctionHook* _loadItemHook;

    public static void RegisterHook()
    {
        if (_loadItemHook != null)
            return;

        CacheDamageBonusAmounts();
        delegate* unmanaged<void*, void*, void*, int, int, int> loadItem = &LoadItem;
        _loadItemHook = NWNXAPI.RequestFunctionHook(
            NativeLibrary.GetExport(NativeLibrary.GetMainProgramHandle(),
                "_ZN8CNWSItem8LoadItemEP7CResGFFP10CResStructii"),
            (IntPtr)loadItem,
            HookOrder.Early);
    }

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
    {
        var original = (delegate* unmanaged<void*, void*, void*, int, int, int>)_loadItemHook->m_trampoline;
        try
        {
            var converted = ConvertLegacyProperties(CResGFF.FromPointer(resource), CResStruct.FromPointer(structure));
            var loaded = original(item, resource, structure, firstFlag, secondFlag);
            if (loaded != 0 && converted)
            {
                using var marker = new CExoString(ConvertedVariable);
                CNWSItem.FromPointer(item).m_ScriptVars.SetInt(marker, 1);
            }
            return loaded;
        }
        catch (Exception exception)
        {
            Log.WriteError(exception, "Could not convert legacy item properties before native loading");
            return 0;
        }
    }

    private static bool ConvertLegacyProperties(CResGFF file, CResStruct root)
    {
        using var properties = new CResList();
        fixed (byte* label = PropertiesListLabel)
            if (file.GetList(properties, root, label) == 0)
                return false;

        var baseItem = ReadBaseItem(file, root);
        var propertyCount = file.GetListCount(properties);
        var firstAccuracyIndex = uint.MaxValue;
        var accuracyAmount = 0;
        var converted = false;
        for (uint index = 0; index < propertyCount; index++)
        {
            using var property = new CResStruct();
            if (file.GetListElement(property, properties, index) == 0 ||
                !TryReadWord(file, property, PropertyNameLabel, out var propertyName) ||
                !IsLegacyAccuracyProperty(propertyName) ||
                !TryReadWord(file, property, CostValueLabel, out var costValue) ||
                !TryReadByte(file, property, CostTableLabel, out _) ||
                !TryReadWord(file, property, SubtypeLabel, out _))
                continue;

            if (firstAccuracyIndex == uint.MaxValue)
                firstAccuracyIndex = index;
            accuracyAmount = (int)Math.Min(MaximumAccuracy, (long)accuracyAmount + costValue);
        }

        for (uint index = 0; index < propertyCount; index++)
        {
            using var property = new CResStruct();
            if (file.GetListElement(property, properties, index) == 0)
                continue;

            if (!TryReadWord(file, property, PropertyNameLabel, out var propertyName) ||
                !TryReadWord(file, property, CostValueLabel, out var costValue) ||
                !TryReadByte(file, property, CostTableLabel, out _))
                continue;

            if (IsLegacyAccuracyProperty(propertyName))
            {
                if (!TryReadWord(file, property, SubtypeLabel, out _))
                    continue;

                RewriteProperty(file, property,
                    (ushort)ItemPropertyType.Accuracy,
                    AccuracyCostTable,
                    index == firstAccuracyIndex ? (ushort)accuracyAmount : (ushort)0,
                    ushort.MaxValue);
                converted = true;
                continue;
            }

            if (propertyName != (ushort)ItemPropertyType.DamageBonus ||
                (baseItem != LightsaberBaseItem && baseItem != SaberstaffBaseItem) ||
                !DamageBonusAmountByRow.TryGetValue(costValue, out var damage) ||
                !TryReadWord(file, property, SubtypeLabel, out _))
                continue;

            RewriteProperty(file, property,
                (ushort)ItemPropertyType.DMG,
                DamageCostTable,
                (ushort)Math.Clamp(damage, 0, ushort.MaxValue),
                0);
            converted = true;
        }
        return converted;
    }

    private static uint ReadBaseItem(CResGFF file, CResStruct root)
    {
        var found = 0;
        fixed (byte* label = BaseItemLabel)
        {
            var value = file.ReadFieldINT(root, label, &found, -1);
            return found == 0 ? uint.MaxValue : unchecked((uint)value);
        }
    }

    private static bool TryReadWord(CResGFF file, CResStruct structure, byte[] fieldName, out ushort value)
    {
        var found = 0;
        fixed (byte* label = fieldName)
            value = file.ReadFieldWORD(structure, label, &found, 0);
        return found != 0;
    }

    private static bool TryReadByte(CResGFF file, CResStruct structure, byte[] fieldName, out byte value)
    {
        var found = 0;
        fixed (byte* label = fieldName)
            value = file.ReadFieldBYTE(structure, label, &found, 0);
        return found != 0;
    }

    private static void RewriteProperty(
        CResGFF file,
        CResStruct property,
        ushort propertyName,
        byte costTable,
        ushort costValue,
        ushort subtype)
    {
        fixed (byte* label = PropertyNameLabel)
            EnsureWritten(file.WriteFieldWORD(property, propertyName, label));
        fixed (byte* label = CostTableLabel)
            EnsureWritten(file.WriteFieldBYTE(property, costTable, label));
        fixed (byte* label = CostValueLabel)
            EnsureWritten(file.WriteFieldWORD(property, costValue, label));
        fixed (byte* label = SubtypeLabel)
            EnsureWritten(file.WriteFieldWORD(property, subtype, label));
    }

    private static bool IsLegacyAccuracyProperty(ushort propertyName) =>
        propertyName == (ushort)ItemPropertyType.EnhancementBonus ||
        propertyName == (ushort)ItemPropertyType.AccuracyBonus;

    private static void EnsureWritten(int result)
    {
        if (result == 0)
            throw new InvalidOperationException("A legacy item-property field could not be rewritten.");
    }
}
