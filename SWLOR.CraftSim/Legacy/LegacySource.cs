namespace SWLOR.CraftSim.Legacy
{
    /// <summary>Every game-server rule the legacy baseline ports, with its source location.</summary>
    public static class LegacySource
    {
        private const string Server = "SWLOR.Game.Server/";

        /// <summary>The old crafting window, deleted from the server. Its rules are frozen and cited to <see cref="CraftViewModelCommit"/>.</summary>
        public const string CraftViewModel = Server + "Feature/GuiDefinition/ViewModel/CraftViewModel.cs";

        /// <summary>The last commit that changed the deleted file. The cited lines are the committed reports', and a rule can sit a line below its citation.</summary>
        public const string CraftViewModelCommit = "ded6cfaf45b66b0a5c4ffe66447bf9c3e24a2ff7";

        /// <summary>The sentence a report prints above a rules table that cites the deleted window.</summary>
        public static readonly string RulesPortedNote =
            "Rules ported. `CraftViewModel.cs` was deleted from the game server after its rules were ported, so they are frozen and cited to commit `" +
            CraftViewModelCommit + "`; the others are checked against the source by `LegacySourceTests`:";

        public const string RecipeLevelChart = Server + "Service/CraftService/RecipeLevelChart.cs";
        public const string Craft = Server + "Service/Craft.cs";
        public const string Harvester = Server + "Feature/ItemDefinition/HarvesterItemDefinition.cs";
        public const string ProspectingNode = Server + "Service/CraftService/Minigame/Prospecting/ProspectingNode.cs";
        public const string GatheringLevels = Server + "Service/CraftService/GatheringLevels.cs";
        public const string Scavenge = Server + "Service/ScavengeService/ScavengeQuickSearch.cs";
        public const string ScavengeSite = Server + "Feature/ScavengePoint.cs";
        public const string Refinery = Server + "Feature/GuiDefinition/ViewModel/RefineryViewModel.cs";
        public const string FishingFight = Server + "Service/FishingService/FishingFightLifecycle.cs";
        public const string EquipmentStats = Server + "Feature/EquipmentStats.cs";

        public static readonly SourceReference ActionRanks = SourceReference.Frozen(
            "Craft action unlock ranks", CraftViewModel, 39, 48, CraftViewModelCommit);

        public static readonly SourceReference MaxCp = SourceReference.Frozen(
            "Craft points", CraftViewModel, 446, 454, CraftViewModelCommit);

        public static readonly SourceReference MaxDurability = SourceReference.Frozen(
            "Durability from the level chart", CraftViewModel, 456, 457, CraftViewModelCommit);

        public static readonly SourceReference MaxProgress = SourceReference.Frozen(
            "Maximum progress modifier", CraftViewModel, 460, 478, CraftViewModelCommit);

        public static readonly SourceReference MaxQuality = SourceReference.Frozen(
            "Maximum quality from the level chart", CraftViewModel, 481, 482, CraftViewModelCommit);

        public static readonly SourceReference LevelChart = new(
            "Recipe level chart", RecipeLevelChart, 13, 99,
            "_recipeLevels[1] = new RecipeLevelDetail(19, 312, 60);",
            "_recipeLevels[50] = new RecipeLevelDetail(186, 2641, 80);",
            "_recipeLevels[80] = new RecipeLevelDetail(3943, 18262, 80);");

        public static readonly SourceReference Progress = SourceReference.Frozen(
            "Progress per synthesis", CraftViewModel, 1354, 1367, CraftViewModelCommit);

        public static readonly SourceReference Quality = SourceReference.Frozen(
            "Quality per touch", CraftViewModel, 1369, 1384, CraftViewModelCommit);

        public static readonly SourceReference HandleAction = SourceReference.Frozen(
            "Action resolution order", CraftViewModel, 1672, 1725, CraftViewModelCommit);

        public static readonly SourceReference Actions = SourceReference.Frozen(
            "The 11 craft actions", CraftViewModel, 1727, 1864, CraftViewModelCommit);

        public static readonly SourceReference SessionReset = SourceReference.Frozen(
            "Flags and meters reset between crafts", CraftViewModel, 1225, 1232, CraftViewModelCommit);

        public static readonly SourceReference FailureLoss = SourceReference.Frozen(
            "Components and enhancements lost on failure", CraftViewModel, 1601, 1641, CraftViewModelCommit);

        public static readonly SourceReference EnhancementSlots = SourceReference.Frozen(
            "Enhancement slots", CraftViewModel, 397, 406, CraftViewModelCommit);

        public static readonly SourceReference EnhancementKinds = SourceReference.Frozen(
            "Enhancement kinds and level limit", CraftViewModel, 601, 666, CraftViewModelCommit);

        public static readonly SourceReference EnhancementProgressPenalty = SourceReference.Frozen(
            "Enhancement progress penalty", CraftViewModel, 695, 754, CraftViewModelCommit);

        public static readonly SourceReference EnhancementTransfer = SourceReference.Frozen(
            "Enhancement transfer chance", CraftViewModel, 1420, 1468, CraftViewModelCommit);

        public static readonly SourceReference RecipeUnlockOffset = new(
            "Recipe rank requirement", Craft, 327, 331,
            "var requiredRank = recipe.Level - RecipeSkillUnlockOffset;", "return Math.Max(requiredRank, 0);");

        public static readonly SourceReference GearStats = new(
            "Crafting gear stats", EquipmentStats, 929, 1075,
            "Stat.AdjustControl(dbPlayer, skillType, amount);", "Stat.AdjustCraftsmanship(dbPlayer, skillType, amount);",
            "Stat.AdjustCPBonus(dbPlayer, skillType, amount);", "GetItemPropertyCostTableValue(ip)");

        // The harvest rules live in Prospecting now. A node still holds a d4 pool of units, set when it is first worked, and Quick Harvest
        // takes all of it, a charge and a harvest delay for each; a unit still pays GetDeltaXP(10 x (tier - 1) + 5 - rank). The bonus item
        // chance (d100 <= 5 x Might modifier x 5) was removed with the attribute dependency, and is ported from HarvesterItemDefinition.cs
        // at commit 64b712fc8 without being checked against the source (HarvestModel.BonusItemChance).
        public static readonly SourceReference HarvestNodeCount = new(
            "Harvests per node", ProspectingNode, 67, 67,
            "random.Next(1, ProspectingRules.PoolDie + 1)");

        public static readonly SourceReference HarvestXp = new(
            "Harvest XP", GatheringLevels, 20, 23,
            "LevelsPerTier * (Math.Max(1, tier) - 1) + FirstTierLevel", "Skill.GetDeltaXP(ForTier(tier) - gatheringRank)");

        // The scavenge rules live in Quick Search now, which plays today's search at the parity reference (Perception modifier 0). The
        // legacy model still takes the modifier as a parameter, so the old baseline stays reproducible; the rules it ports are checked here.
        public static readonly SourceReference ScavengeChance = new(
            "Scavenge chances at the reference Perception modifier", Scavenge, 38, 49,
            "public const int ReferencePerceptionModifier = 0;", "public const int BaseDc = 6;",
            "Math.Clamp(hardLookLevel * 10 + ReferencePerceptionModifier * 5, 0, 100);",
            "Math.Clamp(21 - (dc - ReferencePerceptionModifier), 0, 20) / 20.0;");

        public static readonly SourceReference ScavengeAttempts = new(
            "Scavenge second attempt", Scavenge, 97, 99,
            "var attempts = 1;", "random.D100() <= SecondAttemptChance(hardLookLevel)", "attempts++;");

        public static readonly SourceReference ScavengeRoll = new(
            "Scavenge success roll and DC", Scavenge, 101, 108,
            "var dc = BaseDc;", "var roll = random.Next(1, 21);", "var success = roll + ReferencePerceptionModifier >= dc;",
            "success ? deltaXp : deltaXp / 4", "dc += random.Next(1, 4);");

        public static readonly SourceReference ScavengeXp = new(
            "Scavenge XP level", ScavengeSite, 93, 93,
            "Skill.GetDeltaXP(10 * requiredLevel - rank),");

        public static readonly SourceReference RefineryOres = new(
            "Ore tiers and XP per unit", Refinery, 35, 52,
            "{\"raw_veldite\", new OreDetail(1, \"ref_veldite\", 25)}", "{\"raw_scordspar\", new OreDetail(2, \"ref_scordspar\", 50)}",
            "{\"raw_plagionite\", new OreDetail(3, \"ref_plagionite\", 75)}", "{\"raw_keromber\", new OreDetail(4, \"ref_keromber\", 100)}",
            "{\"raw_jasioclase\", new OreDetail(5, \"ref_jasioclase\", 125)}", "{\"raw_arkoxit\", new OreDetail(5, \"ref_arkoxit\", 150)}");

        public static readonly SourceReference RefineryBatch = new(
            "Refinery batch size", Refinery, 61, 61,
            "private const int BaseItemsRefinedPerCore = 3;");

        public static readonly SourceReference RefineryCores = new(
            "Power cores per batch", Refinery, 172, 177,
            "BaseItemsRefinedPerCore + Math.Max(0, refineryManagementLevel)", "(int)Math.Ceiling(itemCount / (float)Math.Max(1, itemsPerCore))");

        // Refining pays each ore's flat XP times the rank's share of the curve (GatheringLevels.RefiningXp), not the flat XP alone.
        public static readonly SourceReference RefineryXp = new(
            "Refining XP", Refinery, 356, 360,
            "xp += GatheringLevels.RefiningXp(refinedItem.XPGranted, refinedItem.RequiredLevel, gatheringRank, stackSize);",
            "Skill.GiveSkillXP(Player, SkillType.Gathering, xp, false, false);");

        public static readonly SourceReference FishingXp = new(
            "Fishing XP per bite", FishingFight, 58, 68,
            "Skill.GetDeltaXP(bite.Fish.Level - bite.Rank)");

        public static readonly IReadOnlyList<SourceReference> All = new[]
        {
            ActionRanks, MaxCp, MaxDurability, MaxProgress, MaxQuality, LevelChart, Progress, Quality, HandleAction, Actions,
            SessionReset, FailureLoss, EnhancementSlots, EnhancementKinds, EnhancementProgressPenalty, EnhancementTransfer,
            RecipeUnlockOffset, GearStats, HarvestNodeCount, HarvestXp,
            ScavengeChance, ScavengeAttempts, ScavengeRoll, ScavengeXp, RefineryOres, RefineryBatch, RefineryCores, RefineryXp, FishingXp
        };
    }
}
