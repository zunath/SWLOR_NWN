using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// The space revamp's character upgrade (plan section 15). Carried ship deeds and modules, including those in bags and
/// droids, become their new items (<see cref="LegacySpaceItemCatalog"/>), and configuration modules are refunded in
/// credits. The record refunds the retired Piloting perks as SP, keeps unlocked hull recipes, unlocks the Hull Design
/// that keeps each carried deed's look, gains a Rebuild Token and gets the login notice. Piloting ranks keep their
/// meaning, so no rebuild is forced. Registered ships and stored items are converted by <c>_30_ConvertSpaceItems</c>.
/// </summary>
internal sealed class _26_ConvertSpaceItems : ICharacterMigration
{
    /// <summary>The retired Piloting perks and the prices players paid per level.</summary>
    internal static readonly (PerkType Type, string Name, int[] Prices, FeatType Feat)[] RetiredPerks =
    {
        (PerkType.Starships, "Starships", new[] { 1, 1, 2, 3, 4 }, FeatType.StarshipsTrait),
        (PerkType.DefensiveModules, "Defensive Modules", new[] { 1, 1, 2, 3, 4 }, FeatType.DefensiveModulesTrait),
        (PerkType.OffensiveModules, "Offensive Modules", new[] { 1, 1, 2, 3, 4 }, FeatType.OffensiveModulesTrait),
        (PerkType.MiningModules, "Mining Modules", new[] { 1, 1, 2, 3, 4 }, FeatType.MiningModulesTrait),
        (PerkType.EnergyManagement, "Energy Management", new[] { 5, 5 }, FeatType.EnergyManagementTrait),
        (PerkType.StarshipMining, "Starship Mining", new[] { 5, 5 }, FeatType.StarshipMiningTrait),
        (PerkType.IntuitivePiloting, "Intuitive Piloting", new[] { 3 }, FeatType.IntuitivePilotingTrait),
    };

    /// <summary>Unlocked retired hull recipes and the revamp recipes they become (their Ship Schematics).</summary>
    internal static readonly IReadOnlyDictionary<RecipeType, RecipeType> UnlockedRecipes = new Dictionary<RecipeType, RecipeType>
    {
        [RecipeType.AurekStrikefighter] = RecipeType.ShipDeedAurekStrikefighter,
        [RecipeType.SithFighter] = RecipeType.ShipDeedSithFighter,
        [RecipeType.BasiliskWarDroid] = RecipeType.ShipDeedBasiliskWarDroid,
    };

    internal const string LoginNotice =
        "Space update: Piloting now covers every space hull and module, so the ships and modules you could fit before " +
        "still fit; they were converted to their new equivalents. Space mining, gas harvesting and salvage moved to " +
        "Gathering, so mining gear now needs Gathering ranks. The old Piloting perks were refunded as SP to spend on " +
        "the four new Piloting styles: Ace, Ordnance, Bulwark and Command. You also received a free Rebuild Token to " +
        "move skill ranks; you can set skill rank limits in the Skills window before you spend it.";

    public int Version => 26;

    public void Migrate(SavedCharacter character)
    {
        foreach (var perk in RetiredPerks)
            character.RemoveFeat(perk.Feat);
        ConvertItems(character);
    }

    /// <summary>Converts carried retired items and pays carried configuration modules into the character's credits.</summary>
    internal static LegacySpaceItemTally ConvertItems(SavedCharacter character)
    {
        var tally = new LegacySpaceItemTally();
        foreach (var item in character.CarriedRecursive.ToArray())
        {
            if (LegacySpaceItemCatalog.TryGetConfigurationModule(item.ResRef, out var credits))
            {
                tally.Credits += credits;
                tally.ConfigurationModulesRefunded++;
                tally.Count(item.ResRef);
                character.Destroy(item);
                if (item.Container != null)
                    StoredObjectData.NumberListElements(item.List);
                continue;
            }
            StoredObjectData.ConvertLegacySpaceItem(item.Node, tally, out _);
            StoredObjectData.ConvertLegacySpaceDroid(item.Node, tally);
        }

        if (tally.Credits > 0)
        {
            character.WriteInteger("Gold", character.ReadInteger("Gold") + tally.Credits, 4);
            character.Notices.Add("Configuration modules were retired, since ship roles are now built into each hull. " +
                $"You received {tally.Credits} credits for the ones you carried.");
        }
        if (character.AppliesRecordEffects && tally.HullDesigns.Count > 0)
        {
            character.Record.UnlockedHullDesigns ??= new Dictionary<string, System.DateTime>();
            foreach (var key in tally.HullDesigns)
                character.Record.UnlockedHullDesigns.TryAdd(key, System.DateTime.UtcNow);
        }
        return tally;
    }

    public void MigratePlayerData(Player player)
    {
        player.UpgradeNotices ??= new List<string>();
        player.UpgradeNotices.Add(LoginNotice);
        foreach (var (type, name, prices, _) in RetiredPerks)
        {
            CharacterRecalculation.RefundPerk(player, player.UpgradeNotices, type, name, prices);
            player.UnlockedPerks?.Remove(type);
        }

        if (player.UnlockedRecipes != null)
            foreach (var (retired, replacement) in UnlockedRecipes)
                if (player.UnlockedRecipes.TryGetValue(retired, out var unlocked))
                    player.UnlockedRecipes.TryAdd(replacement, unlocked);

        RebuildTokenGrant.Grant(player);
    }
}
