using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The skill XP a crafting guild hand-in gives (owner decision, October 2026, workstream 11). The Smithery, Engineering, Fabrication and
    /// Agriculture guilds pay <see cref="XpPercent"/> percent of <c>Skill.GetDeltaXP(task level - rank)</c> for every item handed in, in the
    /// guild's skill, through the same path as crafting XP so Social and food bonuses apply. The task's level is the level of the recipe that
    /// makes a crafted item, and the fish's level for a catch task. The Hunter's Guild pays no XP, and neither does a task whose level is unknown.
    /// Because the XP is the chart's, a hand-in five or more levels below the rank pays nothing, the same cutoff as crafting and quick craft.
    /// </summary>
    public static class GuildTaskXp
    {
        /// <summary>The share of the chart's XP a hand-in pays, per item, in percent.</summary>
        public const int XpPercent = 50;

        private static Dictionary<(SkillType Skill, string Resref), int> _recipeLevels;
        private static Dictionary<string, int> _fishLevels;

        /// <summary>The skill a guild's hand-ins give XP in; <see cref="SkillType.Invalid"/> when they give none.</summary>
        public static SkillType SkillOf(GuildType guild) => guild.GetAttribute<GuildType, GuildAttribute>().TaskXpSkill;

        public static bool GivesXp(GuildType guild) => SkillOf(guild) != SkillType.Invalid;

        /// <summary>
        /// The XP for handing in <paramref name="quantity"/> items of a task at <paramref name="taskLevel"/> to a player at <paramref name="skillRank"/>:
        /// <see cref="XpPercent"/> percent of the chart's XP for the level against the rank, per item, rounded down.
        /// </summary>
        public static int Amount(int taskLevel, int skillRank, int quantity) =>
            quantity <= 0 ? 0 : Skill.GetDeltaXP(taskLevel - skillRank) * quantity * XpPercent / 100;

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
            Func<string, int?> fishLevel)
        {
            var skill = SkillOf(guild);
            if (skill == SkillType.Invalid || string.IsNullOrEmpty(resref))
                return null;

            return playerProduced
                ? recipeLevel(skill, resref)
                : fishLevel(resref) ?? recipeLevel(skill, resref);
        }

        /// <summary>The task's level from the server's recipes and fish.</summary>
        public static int? TaskLevel(GuildType guild, string resref, bool playerProduced) =>
            TaskLevel(guild, resref, playerProduced, RecipeLevel, FishLevel);

        /// <summary>The XP a player would get now for handing in the task's items; 0 when the guild or the task gives none.</summary>
        public static int Calculate(uint player, GuildType guild, string resref, int quantity, bool playerProduced)
        {
            var skill = SkillOf(guild);
            if (skill == SkillType.Invalid)
                return 0;

            var level = TaskLevel(guild, resref, playerProduced);
            if (level == null)
                return 0;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            return dbPlayer != null && dbPlayer.Skills.TryGetValue(skill, out var dbSkill)
                ? Amount(level.Value, dbSkill.Rank, quantity)
                : 0;
        }

        /// <summary>Gives the XP of a hand-in the way crafting gives its XP: bonuses apply and no henchman penalty is taken.</summary>
        public static void Give(uint player, GuildType guild, string resref, int quantity, bool playerProduced)
        {
            var xp = Calculate(player, guild, resref, quantity, playerProduced);
            if (xp > 0)
                Skill.GiveSkillXP(player, SkillOf(guild), xp, false, false);
        }

        private static int? RecipeLevel(SkillType skill, string resref)
        {
            _recipeLevels ??= Craft.GetAllRecipes().Values
                .Where(recipe => recipe.IsActive && !string.IsNullOrEmpty(recipe.Resref))
                .GroupBy(recipe => (recipe.Skill, recipe.Resref.ToLowerInvariant()))
                .ToDictionary(group => group.Key, group => group.Min(recipe => recipe.Level));

            return _recipeLevels.TryGetValue((skill, resref.ToLowerInvariant()), out var level) ? level : null;
        }

        /// <summary>The level of the fish with the resref, from <see cref="FishType"/>; null when it is not a fish.</summary>
        public static int? FishLevel(string resref)
        {
            _fishLevels ??= Enum.GetValues<FishType>()
                .Where(fish => fish != FishType.Invalid)
                .Select(fish => fish.GetAttribute<FishType, FishAttribute>())
                .Where(fish => !string.IsNullOrEmpty(fish.Resref))
                .GroupBy(fish => fish.Resref.ToLowerInvariant())
                .ToDictionary(group => group.Key, group => group.First().Level);

            return _fishLevels.TryGetValue(resref.ToLowerInvariant(), out var level) ? level : null;
        }
    }
}
