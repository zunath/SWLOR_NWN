using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>What converting the retired space items of one saved object found and changed.</summary>
public sealed class LegacySpaceItemTally
{
    /// <summary>Credits owed for configuration modules removed from it (and for the root, when it is one).</summary>
    public int Credits { get; set; }

    /// <summary>The saved object is itself a configuration module: the caller removes it and refunds it.</summary>
    public bool RootIsConfigurationModule { get; set; }

    /// <summary>The blueprint the root item now is, when the root was a retired hull or module.</summary>
    public LegacySpaceItemBlueprint RootReplacement { get; set; }

    /// <summary>The resref the root item now has, when it was converted.</summary>
    public string RootReplacementResref { get; set; }

    /// <summary>Hull Design keys (ship tag:appearance) that keep the look of each converted deed.</summary>
    public List<string> HullDesigns { get; } = new();

    /// <summary>Retired resref to the number of items converted or refunded.</summary>
    public Dictionary<string, int> Items { get; } = new(StringComparer.OrdinalIgnoreCase);

    public int ConfigurationModulesRefunded { get; set; }

    internal void Count(string resref, int count = 1) =>
        Items[resref] = (Items.TryGetValue(resref, out var current) ? current : 0) + count;

    /// <summary>Adds another tally's credits, counts and designs (not its root).</summary>
    internal void Add(LegacySpaceItemTally other)
    {
        Credits += other.Credits;
        ConfigurationModulesRefunded += other.ConfigurationModulesRefunded;
        foreach (var (resref, count) in other.Items)
            Count(resref, count);
        foreach (var key in other.HullDesigns.Where(key => !HullDesigns.Contains(key)))
            HullDesigns.Add(key);
    }
}
