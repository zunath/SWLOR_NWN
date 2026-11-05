using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The skill XP a crafting guild hand-in gives. The Smithery, Engineering, Fabrication and
    /// Agriculture guilds pay <see cref="XpPercent"/> percent of <c>Skill.GetDeltaXP(task level - rank)</c> for every item handed in, in the
    /// guild's skill, through the same path as crafting XP so Social and food bonuses apply. The task's level is the level of the recipe that
    /// makes a crafted item, and the fish's level for a catch task. The Hunter's Guild pays no XP, and neither does a task whose level is unknown.
    /// Because the XP is the chart's, a hand-in five or more levels below the rank pays nothing, the same cutoff as crafting and quick craft.
    /// The logic lives in <see cref="IGuildTaskXpService"/>; every member here forwards to the container's instance.
    /// </summary>
    public static class GuildTaskXp
    {
        /// <summary>The share of the chart's XP a hand-in pays, per item, in percent.</summary>
        public const int XpPercent = Domain.Economy.GuildTaskXpService.XpPercent;

        private static IGuildTaskXpService Instance => FacadeAccessor.Resolve<IGuildTaskXpService>();

        /// <summary>The skill a guild's hand-ins give XP in; <see cref="SkillType.Invalid"/> when they give none.</summary>
        public static SkillType SkillOf(GuildType guild) => Instance.SkillOf(guild);

        public static bool GivesXp(GuildType guild) => Instance.GivesXp(guild);

        /// <summary>
        /// The XP for handing in <paramref name="quantity"/> items of a task at <paramref name="taskLevel"/> to a player at <paramref name="skillRank"/>:
        /// <see cref="XpPercent"/> percent of the chart's XP for the level against the rank, per item, rounded down.
        /// </summary>
        public static int Amount(int taskLevel, int skillRank, int quantity) => Instance.Amount(taskLevel, skillRank, quantity);

        /// <summary>
        /// The level that sets a task's XP. A crafted task (<paramref name="playerProduced"/>) uses the lowest level of an active recipe of the
        /// guild's skill that makes the item. Any other task uses the fish's level when the item is a fish, and otherwise the recipe's, as for a
        /// vendor good a recipe also makes. Null when neither is found.
        /// </summary>
        public static int? TaskLevel(
            GuildType guild,
            string resref,
            bool playerProduced,
            Func<SkillType, string, int?> recipeLevel,
            Func<string, int?> fishLevel) =>
            Instance.TaskLevel(guild, resref, playerProduced, recipeLevel, fishLevel);

        /// <summary>The task's level from the server's recipes and fish.</summary>
        public static int? TaskLevel(GuildType guild, string resref, bool playerProduced) =>
            Instance.TaskLevel(guild, resref, playerProduced);

        /// <summary>The XP a player would get now for handing in the task's items; 0 when the guild or the task gives none.</summary>
        public static int Calculate(uint player, GuildType guild, string resref, int quantity, bool playerProduced) =>
            Instance.Calculate(player, guild, resref, quantity, playerProduced);

        /// <summary>Gives the XP of a hand-in the way crafting gives its XP: bonuses apply and no henchman penalty is taken.</summary>
        public static void Give(uint player, GuildType guild, string resref, int quantity, bool playerProduced) =>
            Instance.Give(player, guild, resref, quantity, playerProduced);

        /// <summary>The level of the fish with the resref, from the fish catalog; null when it is not a fish.</summary>
        public static int? FishLevel(string resref) => Instance.FishLevel(resref);
    }
}
