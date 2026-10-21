using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Gives droid parts and controllers saved before the Unarmed and Twin Pistol droid skills the skill new copies
/// carry beside its group: Unarmed with Katar and Staff, Twin Pistol with Pistol, Rifle and Throwing. The added
/// skill takes the lowest total in its group, so a boost to one skill of the group is not copied. Controllers
/// keep a zero skill as assembly saves it. An item that already has the skill, or lacks part of the group, is
/// left alone, so the rule is idempotent. Runs only in release migrations, not when an item is acquired, so
/// boosts added to new parts later are not copied either.
/// </summary>
public static class DroidWeaponSkillMigration
{
    /// <summary>Each new skill and the existing skills whose lowest total it receives.</summary>
    public static IReadOnlyList<(DroidStatSubType Skill, DroidStatSubType[] Group)> Groups { get; } = new[]
    {
        (DroidStatSubType.Unarmed, new[] { DroidStatSubType.Katar, DroidStatSubType.Staff }),
        (DroidStatSubType.TwinPistol, new[] { DroidStatSubType.Pistol, DroidStatSubType.Rifle, DroidStatSubType.Throwing }),
    };

    /// <summary>The skills to add to an item with these permanent DroidStat properties, and their values.</summary>
    public static IEnumerable<(DroidStatSubType Skill, int Value)> GetMissingSkills(IReadOnlyCollection<(int Subtype, int Value)> droidStats)
    {
        foreach (var (skill, group) in Groups)
        {
            if (droidStats.Any(stat => stat.Subtype == (int)skill) ||
                !group.All(member => droidStats.Any(stat => stat.Subtype == (int)member)))
                continue;
            yield return (skill, group.Min(member => droidStats.Where(stat => stat.Subtype == (int)member).Sum(stat => stat.Value)));
        }
    }

    /// <summary>Adds the missing skills to a live item, as <see cref="StoredObjectData"/> does to saved data.</summary>
    internal static bool MigrateItem(uint item)
    {
        var droidStats = new List<(int Subtype, int Value)>();
        for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            if (GetItemPropertyType(ip) == ItemPropertyType.DroidStat && GetItemPropertyDurationType(ip) == DurationType.Permanent)
                droidStats.Add((GetItemPropertySubType(ip), GetItemPropertyCostTableValue(ip)));
        if (droidStats.Count == 0)
            return false;

        var changed = false;
        foreach (var (skill, value) in GetMissingSkills(droidStats).ToList())
        {
            MigrationObject.AddProperty(item, ItemPropertyCustom(ItemPropertyType.DroidStat, (int)skill, value), AddItemPropertyPolicy.IgnoreExisting);
            changed = true;
        }
        return changed;
    }
}
