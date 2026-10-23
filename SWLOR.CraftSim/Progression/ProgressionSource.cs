using SWLOR.CraftSim.Legacy;

namespace SWLOR.CraftSim.Progression
{
    /// <summary>
    /// The game-server rules the progression model copies instead of calling, with their source locations. The rules it calls directly
    /// (<c>Skill.GetRequiredXP</c>, <c>GetDeltaXP</c>, <c>CraftOutcomeRules</c>, <c>CraftRankMilestones</c>, <c>Guild</c> thresholds,
    /// <c>QuickCraftRules</c>, <c>QuickCraftBatch</c>) need no entry. <c>ProgressionSourceTests</c> fails when an anchor moves away.
    /// The harvest, scavenge and refinery rules the model also uses are registered in <see cref="LegacySource"/>.
    /// </summary>
    public static class ProgressionSource
    {
        private const string Server = "SWLOR.Game.Server/";
        public const string RoleplayXp = Server + "Feature/RoleplayXP.cs";
        public const string Guild = Server + "Service/Guild.cs";
        public const string ProspectingRulesFile = Server + "Service/CraftService/Minigame/Prospecting/ProspectingRules.cs";
        public const string Scavenge = Server + "Feature/ScavengePoint.cs";
        public const string Refinery = Server + "Feature/GuiDefinition/ViewModel/RefineryViewModel.cs";
        public const string Craft = Server + "Service/Craft.cs";
        public const string ResearchViewModel = Server + "Feature/GuiDefinition/ViewModel/ResearchViewModel.cs";

        public static readonly SourceReference RoleplayXpRate = new(
            "RP XP per hour", RoleplayXp, 16, 93,
            "private const int RPRequiredTicks = 300;", "300 ticks * 6 seconds per HB = 1800 seconds = 30 minutes", "const int BaseXP = 1500;");

        public static readonly SourceReference GuildPointReward = new(
            "Guild points per hand-in", Guild, 62, 80,
            "var rankBonus = 0.25f * dbGuild.Rank;", "(rankBonus * baseAmount)", "return (int)amount;");

        public static readonly SourceReference GuildBoardSize = new(
            "Ten guild tasks per tier on the board", Guild, 124, 170,
            "if (potentialTasks.Count <= 10)", ".OrderBy(o => Random.Next())", ".Take(10)", "_activeGuildTasksByRank[type][rank] = tasks;");

        public static readonly SourceReference HarvestDelay = new(
            "Seconds per harvester use", ProspectingRulesFile, 71, 71,
            "public const double HarvestSeconds = 5.0;");

        public static readonly SourceReference ScavengeSearch = new(
            "Seconds per scavenge attempt", Scavenge, 55, 70,
            "ActionPlayAnimation(Animation.LoopingGetLow, 1.0f, 2.0f)");

        public static readonly SourceReference RefineDelay = new(
            "Seconds per refining operation", Refinery, 58, 64,
            "private const float RefiningDelaySeconds = 6f;");

        public static readonly SourceReference ResearchCost = new(
            "Research job length and credits", Craft, 1145, 1192,
            "var perkLevel = recipeDetail.Level / 10 + 1;", "if (perkLevel > 5)",
            "var price = baseConstant * (Math.Pow(perkLevel, 2.2f) * Math.Pow(blueprintLevel, 2f));",
            "price += price * recipeDetail.ResearchCostModifier;", "return CalculateResearchCost(recipe, blueprintLevel, 200, reductionBonus * 0.01f);");

        public static readonly SourceReference ResearchBlueprint = new(
            "A first research job creates the blueprint", ResearchViewModel, 387, 410,
            "if (string.IsNullOrWhiteSpace(dbJob.SerializedItem))", "item = CreateItemOnObject(\"blueprint\", Player);", "isNewBlueprint = true;");

        public static readonly IReadOnlyList<SourceReference> All = new[]
        {
            RoleplayXpRate, GuildPointReward, GuildBoardSize, HarvestDelay, ScavengeSearch, RefineDelay, ResearchCost, ResearchBlueprint
        };
    }
}
