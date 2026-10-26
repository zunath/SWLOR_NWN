using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Retires the perks whose tiers now unlock with skill rank, and refunds the perks whose category and requirements
/// changed. Every owned rank returns its SP and the perk leaves the record and the character's feats. The release's
/// player migration (<c>_24_CorrectCarapaceBracers</c>) calls it and grants the single optional rebuild token.
/// </summary>
internal static class CraftingPerkConversion
{
    /// <summary>
    /// The prices players paid per rank, kept here because the retired definitions no longer exist.
    /// They are the combat-rebuild prices, not the older ones in the legacy refund table.
    /// </summary>
    private static readonly RefundedPerk[] RefundedPerks =
    {
        new(PerkType.Harvesting, "Harvesting", new[] { 1, 1, 2, 3, 4 },
            "was retired. Harvester tiers now unlock at Gathering ranks 0, 10, 20, 30 and 40.",
            FeatType.Harvesting1, FeatType.Harvesting2, FeatType.Harvesting3, FeatType.Harvesting4, FeatType.Harvesting5),
        new(PerkType.Refining, "Refining", new[] { 1, 1, 2, 3, 4 },
            "was retired. Ore tiers now unlock at Gathering ranks 0, 10, 20, 30 and 40.",
            FeatType.Refining1, FeatType.Refining2, FeatType.Refining3, FeatType.Refining4, FeatType.Refining5),
        new(PerkType.RefineryManagement, "Refinery Management", new[] { 1, 1, 2, 2, 3, 4 },
            "was retired. Power cores now refine one more item at Gathering ranks 0, 10, 20, 30, 40 and 45.",
            FeatType.RefineryManagement1, FeatType.RefineryManagement2, FeatType.RefineryManagement3,
            FeatType.RefineryManagement4, FeatType.RefineryManagement5, FeatType.RefineryManagement6),
        new(PerkType.Scavenging, "Scavenging", new[] { 1, 1, 2, 3, 4 },
            "was retired. Scavenge site tiers now unlock at Gathering ranks 0, 10, 20, 30 and 40.",
            FeatType.Scavenging1, FeatType.Scavenging2, FeatType.Scavenging3, FeatType.Scavenging4, FeatType.Scavenging5),
        new(PerkType.HardLook, "Hard Look", new[] { 1, 1, 2, 3, 3 },
            "was retired. The chance of a second search now grows with Gathering rank.",
            FeatType.HardLook1, FeatType.HardLook2, FeatType.HardLook3, FeatType.HardLook4, FeatType.HardLook5),
        new(PerkType.TreasureHunter, "Treasure Hunter", new[] { 3, 3, 4 },
            "moved to General and now requires Armor ranks 20, 40 and 50. Buy it again if you want it.",
            FeatType.TreasureHunterTrait),
        new(PerkType.CreditFinder, "Creditfinder", new[] { 3, 3, 4 },
            "moved to General and now requires Armor ranks 15, 30 and 45. Buy it again if you want it.",
            FeatType.CreditFinderTrait),
        new(PerkType.Research, "Research", new[] { 2, 2, 3, 3, 4 },
            "was retired. Research tiers now unlock at Fabrication ranks 10, 20, 30, 40 and 50.",
            FeatType.ResearchTrait),
        new(PerkType.ScientificNetworking, "Scientific Networking", new[] { 3, 4 },
            "was retired. New blueprints now gain a licensed run at Fabrication ranks 25 and 50.",
            FeatType.ScientificNetworkingTrait),
        new(PerkType.ResearchProjects, "Research Projects", new[] { 2, 3 },
            "was retired. You can now run one more research job at Fabrication ranks 25 and 50.",
            FeatType.ResearchProjectsTrait),
        new(PerkType.DroidAssembly, "Droid Assembly", new[] { 1, 1, 2, 3, 4 },
            "was retired. Droid tiers now unlock at Engineering ranks 0, 10, 20, 30 and 40.",
            FeatType.DroidAssemblyTrait),
    };

    /// <summary>Removes the feats the refunded perks granted, so none outlives its perk.</summary>
    public static void RemoveGrantedFeats(SavedCharacter character)
    {
        foreach (var feat in RefundedPerks.SelectMany(perk => perk.GrantedFeats))
            character.RemoveFeat(feat);
    }

    /// <summary>Refunds every owned rank of the retired perks and removes the perks from the record, with a notice each.</summary>
    public static void RefundRetiredPerks(Player player)
    {
        player.UpgradeNotices ??= new List<string>();

        foreach (var perk in RefundedPerks)
        {
            var owned = player.Perks.TryGetValue(perk.Type, out var level);
            player.Perks.Remove(perk.Type);
            player.UnlockedPerks.Remove(perk.Type);
            if (!owned || level <= 0)
                continue;

            var refund = perk.GetRefund(level);
            player.UnallocatedSP += refund;
            Log.Write(LogGroup.Migration, $"{player.Name} ({player.Id}) refunded {refund} SP for perk '{perk.Type}'.");
            player.UpgradeNotices.Add($"Perk '{perk.Name}' {perk.Change} You reclaimed {refund} SP.");
        }
    }

    private sealed record RefundedPerk(PerkType Type, string Name, int[] Prices, string Change, params FeatType[] GrantedFeats)
    {
        public int GetRefund(int level) => Prices.Take(level).Sum();
    }
}
