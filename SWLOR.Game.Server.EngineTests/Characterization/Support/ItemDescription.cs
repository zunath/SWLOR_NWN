using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.EngineTests.Characterization.Support;

/// <summary>Writes an item as one stable line: blueprint, stack size and its sorted item properties.</summary>
public static class ItemDescription
{
    public static string Describe(uint item)
    {
        if (!GetIsObjectValid(item))
            return "invalid item";

        return $"{GetResRef(item)} x{GetItemStackSize(item)}{DescribeProperties(item)}";
    }

    /// <summary>The item's properties as <c>[Type/Subtype/Cost/Param, ...]</c> sorted, or an empty string when it has none.</summary>
    public static string DescribeProperties(uint item)
    {
        var properties = new List<string>();
        for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
        {
            properties.Add($"{GetItemPropertyType(ip)}/{GetItemPropertySubType(ip)}/{GetItemPropertyCostTableValue(ip)}/{GetItemPropertyParam1Value(ip)}");
        }

        if (properties.Count == 0)
            return string.Empty;

        properties.Sort(StringComparer.Ordinal);
        return " [" + string.Join(", ", properties) + "]";
    }

    /// <summary>Every item the creature carries (not equipped), described and sorted, so order of creation never matters.</summary>
    public static List<string> DescribeInventory(uint creature)
    {
        var items = new List<string>();
        for (var item = GetFirstItemInInventory(creature); GetIsObjectValid(item); item = GetNextItemInInventory(creature))
            items.Add(Describe(item));

        items.Sort(StringComparer.Ordinal);
        return items;
    }
}
