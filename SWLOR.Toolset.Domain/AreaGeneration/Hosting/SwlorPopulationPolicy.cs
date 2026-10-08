using Nwn.Authoring.Areas.Generation.Composition;
using Nwn.Authoring.Areas.Generation.Hosting;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;

namespace SWLOR.Toolset.Domain.AreaGeneration.Hosting;

/// <summary>SWLOR's rules for generated objects: loot containers, exit placeables and safe encounter creatures.</summary>
public sealed class SwlorPopulationPolicy : IGeneratedAreaPopulationPolicy
{
    private const string GeneratedTreasureOpenScript = "proc_loot_open";

    public void ConfigureExitPlaceable(JsonGffStruct instance)
    {
        instance.SetString("OnUsed", GffFieldType.ResRef, string.Empty);
        new VarTable(instance).Remove("Destination");
    }

    public void ConfigureTreasure(JsonGffStruct instance, DungeonTierDetail tier)
    {
        if (string.IsNullOrWhiteSpace(tier.TreasureLootTableId) || tier.TreasureItemCount < 1)
            throw new InvalidOperationException($"Tier {tier.Tier} has invalid treasure settings.");

        instance.SetInt("Useable", GffFieldType.Byte, 1);
        instance.SetInt("HasInventory", GffFieldType.Byte, 1);
        instance.SetInt("Static", GffFieldType.Byte, 0);
        instance.SetString("OnOpen", GffFieldType.ResRef, GeneratedTreasureOpenScript);
        instance.SetString("OnClosed", GffFieldType.ResRef, string.Empty);
        instance.SetString("OnInvDisturbed", GffFieldType.ResRef, string.Empty);

        var variables = new VarTable(instance);
        variables.Remove("SCAVENGE_POINT_LEVEL");
        variables.Remove("SCAVENGE_POINT_LOOT_TABLE_NAME");
        variables.SetString(
            "LOOT_TABLE_1",
            $"{tier.TreasureLootTableId},100,{tier.TreasureItemCount}");
    }

    /// <summary>Drops inherited quest locals and progression-key loot so generated creatures cannot grant them.</summary>
    public void ConfigureCreature(JsonGffStruct instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var variables = new VarTable(instance);
        foreach (var inheritedLocal in variables
                     .Where(entry =>
                         entry.Name.StartsWith("QUEST_", StringComparison.Ordinal) ||
                         IsProgressionKeyLoot(entry))
                     .Select(entry => entry.Name)
                     .ToList())
        {
            variables.Remove(inheritedLocal);
        }
    }

    private static bool IsProgressionKeyLoot(VarTableEntry entry)
    {
        if (!entry.Name.StartsWith("LOOT_TABLE_", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(entry.StringValue))
        {
            return false;
        }

        var separator = entry.StringValue.IndexOf(',');
        var tableId = (separator < 0 ? entry.StringValue : entry.StringValue[..separator]).Trim();
        return tableId.EndsWith("_KEY", StringComparison.OrdinalIgnoreCase);
    }
}
