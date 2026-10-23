namespace SWLOR.CraftSim.Legacy
{
    /// <summary>Every game-server rule the legacy baseline ports, with its source location.</summary>
    public static class LegacySource
    {
        private const string Server = "SWLOR.Game.Server/";
        public const string CraftViewModel = Server + "Feature/GuiDefinition/ViewModel/CraftViewModel.cs";
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

        public static readonly SourceReference ActionRanks = new(
            "Craft action unlock ranks", CraftViewModel, 39, 48,
            "RapidSynthesisRequiredSkillRank = 10", "CarefulSynthesisRequiredSkillRank = 30",
            "BasicTouchRequiredSkillRank = 5", "StandardTouchRequiredSkillRank = 15", "PreciseTouchRequiredSkillRank = 35",
            "MastersMendRequiredSkillRank = 10", "SteadyHandRequiredSkillRank = 20", "MuscleMemoryRequiredSkillRank = 40",
            "VenerationRequiredSkillRank = 25", "WasteNotRequiredSkillRank = 8");

        public static readonly SourceReference MaxCp = new(
            "Craft points", CraftViewModel, 446, 454,
            "dbPlayer.CPBonus[recipe.Skill]", "_maxCP = (int)(cp + skill * 0.75f);",
            "if (skill >= VenerationRequiredSkillRank)", "_maxCP += 31;", "_cp = _maxCP;");

        public static readonly SourceReference MaxDurability = new(
            "Durability from the level chart", CraftViewModel, 456, 457,
            "_maxDurability = levelDetail.Durability;", "_durability = _maxDurability;");

        public static readonly SourceReference MaxProgress = new(
            "Maximum progress modifier", CraftViewModel, 460, 478,
            "if (_levelDifference < 0)", "var adjustment = _levelDifference * 0.25f;", "progressModifier = -adjustment;",
            "else if (_levelDifference > 0)", "var adjustment = _levelDifference * 0.05f;", "if (adjustment > 0.25f)",
            "progressModifier = adjustment;", "_maxProgress = (int)(levelDetail.Progress + levelDetail.Progress * progressModifier);");

        public static readonly SourceReference MaxQuality = new(
            "Maximum quality from the level chart", CraftViewModel, 481, 482,
            "_maxQuality = levelDetail.Quality;", "_quality = 0;");

        public static readonly SourceReference LevelChart = new(
            "Recipe level chart", RecipeLevelChart, 13, 99,
            "_recipeLevels[1] = new RecipeLevelDetail(19, 312, 60, 0.0025f);",
            "_recipeLevels[50] = new RecipeLevelDetail(186, 2641, 80, 0.018f);",
            "_recipeLevels[80] = new RecipeLevelDetail(3943, 18262, 80, 0.018f);");

        public static readonly SourceReference Progress = new(
            "Progress per synthesis", CraftViewModel, 1354, 1367,
            "var recipeDiff = 1 + 0.05f * delta;", "dbPlayer.Skills[recipe.Skill].Rank >= SteadyHandRequiredSkillRank ? 21 : 0",
            "(int)((baseProgress + steadyHandBonus + craftsmanship * 0.65f) * recipeDiff)");

        public static readonly SourceReference Quality = new(
            "Quality per touch", CraftViewModel, 1369, 1384,
            "? 1 + 0.05f * delta", ": 1;", "dbPlayer.Skills[recipe.Skill].Rank >= MuscleMemoryRequiredSkillRank ? 115 : 0",
            "(int)((baseQuality + muscleMemoryBonus + control * 0.75f) * recipeDiff)");

        public static readonly SourceReference HandleAction = new(
            "Action resolution order", CraftViewModel, 1672, 1725,
            "if (_cp < cpCost)", "if (_wasteNotStepsRemaining > 0)", "_wasteNotStepsRemaining--;", "durabilityLoss /= 2;",
            "_durability -= durabilityLoss;", "_cp -= cpCost;", "if (Random.D100(1) <= chance)",
            "if (_progress >= _maxProgress)", "ProcessSuccess();", "else if (_durability <= 0)", "ProcessFailure();");

        public static readonly SourceReference Actions = new(
            "The 11 craft actions", CraftViewModel, 1727, 1864,
            "var chance = _isSteadyHandActive ? 100 : 90;", "var chance = _isSteadyHandActive ? 100 : 75;",
            "var chance = _isSteadyHandActive ? 100 : 50;", "var chance = _isMuscleMemoryActive ? 100 : 90;",
            "var chance = _isMuscleMemoryActive ? 100 : 75;", "var chance = _isMuscleMemoryActive ? 100 : 50;",
            "HandleAction(\"Basic Synthesis\", chance, 0, 10,", "var cpCost = 6;", "var cpCost = 10;",
            "_venerationStepsRemaining--;", "cpCost /= 2;", "CalculateProgress(10)", "CalculateProgress(30)", "CalculateProgress(80)",
            "HandleAction(\"Basic Touch\", chance, 3, 10,", "HandleAction(\"Standard Touch\", chance, 6, 10,",
            "HandleAction(\"Precise Touch\",  chance, 10, 10,", "CalculateQuality(10)", "CalculateQuality(30)", "CalculateQuality(80)",
            "HandleAction(\"Master's Mend\", 100, 10, 0,", "_durability += 30;", "HandleAction(\"Steady Hand\", 100, 12, 0,",
            "HandleAction(\"Muscle Memory\", 100, 12, 0,", "HandleAction(\"Veneration\", 100, 8, 10,", "_venerationStepsRemaining = 4;",
            "HandleAction(\"Waste Not\", 100, 4, 0,", "_wasteNotStepsRemaining = 4;");

        public static readonly SourceReference SessionReset = new(
            "Flags and meters reset between crafts", CraftViewModel, 1225, 1232,
            "_isMuscleMemoryActive = false;", "_isSteadyHandActive = false;", "_venerationStepsRemaining = 0;",
            "_wasteNotStepsRemaining = 0;", "_durability = _maxDurability;", "_cp = _maxCP;");

        public static readonly SourceReference FailureLoss = new(
            "Components and enhancements lost on failure", CraftViewModel, 1601, 1641,
            "const int ChanceToLoseItem = 65;", "Random.D100(1) > ChanceToLoseItem", "foreach (var serialized in _components)");

        public static readonly SourceReference EnhancementSlots = new(
            "Enhancement slots", CraftViewModel, 397, 406,
            "var enhancementSlots = recipe.EnhancementSlots + blueprint.EnhancementSlots;", "IsEnhancement3Visible = enhancementSlots >= 3;");

        public static readonly SourceReference EnhancementKinds = new(
            "Enhancement kinds and level limit", CraftViewModel, 601, 666,
            "typeIP = ItemPropertyType.ArmorEnhancement;", "typeIP = ItemPropertyType.WeaponEnhancement;",
            "typeIP = ItemPropertyType.StructureEnhancement;", "typeIP = ItemPropertyType.FoodEnhancement;",
            "typeIP = ItemPropertyType.StarshipEnhancement;", "typeIP = ItemPropertyType.ModuleEnhancement;",
            "typeIP = ItemPropertyType.DroidEnhancement;", "enhancementLevel = GetItemPropertyCostTableValue(ip);",
            "var levelDiff = enhancementLevel - recipe.Level;", "if (levelDiff > 5)");

        public static readonly SourceReference EnhancementProgressPenalty = new(
            "Enhancement progress penalty", CraftViewModel, 695, 754,
            "if (type == ItemPropertyType.ProgressPenalty)", "progressPenalty += GetItemPropertyCostTableValue(ip);",
            "var progressPenalty = CalculateProgressPenaltyAndProcessItemProperties(item, _itemPropertiesEnhancement1);",
            "_maxProgress += progressPenalty;");

        public static readonly SourceReference EnhancementTransfer = new(
            "Enhancement transfer chance", CraftViewModel, 1420, 1468,
            "var propertyTransferChance = (int)(((float)_quality / (float)_maxQuality) * 100);",
            "GetItemPropertyType(allProperties[index]) == ItemPropertyType.DMG", "if (Random.D100(1) <= propertyTransferChance)",
            "ApplyProperty(item, property);");

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
            "Harvests per node", ProspectingNode, 62, 62,
            "var pool = random.Next(1, ProspectingRules.PoolDie + 1);");

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
            "Scavenge XP level", ScavengeSite, 89, 89,
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
            "Power cores per batch", Refinery, 173, 174,
            "var itemsPerCore = BaseItemsRefinedPerCore + refineryManagement;", "(int)Math.Ceiling(ItemCount / (float)itemsPerCore)");

        // Refining pays each ore's flat XP times the rank's share of the curve (GatheringLevels.RefiningXp), not the flat XP alone.
        public static readonly SourceReference RefineryXp = new(
            "Refining XP", Refinery, 356, 360,
            "xp += GatheringLevels.RefiningXp(refinedItem.XPGranted, refinedItem.RequiredLevel, gatheringRank, stackSize);",
            "Skill.GiveSkillXP(Player, SkillType.Gathering, xp, false, false);");

        public static readonly SourceReference FishingXp = new(
            "Fishing XP per bite", FishingFight, 52, 62,
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
