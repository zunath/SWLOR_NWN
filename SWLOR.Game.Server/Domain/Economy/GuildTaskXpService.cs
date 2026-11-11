using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Domain.World;

namespace SWLOR.Game.Server.Domain.Economy
{
    /// <summary>
    /// The skill XP a crafting guild hand-in gives: the logic that used to be static in <c>Service.GuildTaskXp</c>, moved
    /// here unchanged. The crafting guilds pay the XP percent of <c>Skill.GetDeltaXP(task level - rank)</c> for every item
    /// handed in, in the guild's skill, through the same path as crafting XP so Social and food bonuses apply. The hunting
    /// guild pays no XP, and neither does a task whose level is unknown. Static callers reach the container's
    /// instance through <c>GameServices.GuildTaskXp</c>.
    /// </summary>
    public class GuildTaskXpService : IGuildTaskXpService
    {
        /// <summary>The share of the chart's XP a hand-in pays, per item, in percent.</summary>
        public const int XpPercent = 50;

        private readonly IDBService _db;
        private readonly ICraftService _craft;
        private readonly ISkillService _skill;
        private Dictionary<(SkillType Skill, string Resref), int> _recipeLevels;
        private Dictionary<string, int> _fishLevels;

        public GuildTaskXpService(IDBService db, ICraftService craft, ISkillService skill)
        {
            _db = db;
            _craft = craft;
            _skill = skill;
        }

        public SkillType SkillOf(GuildType guild) => guild.GetAttribute<GuildType, GuildAttribute>().TaskXpSkill;

        public bool GivesXp(GuildType guild) => SkillOf(guild) != SkillType.Invalid;

        public int Amount(int taskLevel, int skillRank, int quantity) =>
            quantity <= 0 ? 0 : GetDeltaXP(taskLevel - skillRank) * quantity * XpPercent / 100;

        public int? TaskLevel(
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

        public int? TaskLevel(GuildType guild, string resref, bool playerProduced) =>
            TaskLevel(guild, resref, playerProduced, RecipeLevel, FishLevel);

        public int Calculate(uint player, GuildType guild, string resref, int quantity, bool playerProduced)
        {
            var skill = SkillOf(guild);
            if (skill == SkillType.Invalid)
                return 0;

            var level = TaskLevel(guild, resref, playerProduced);
            if (level == null)
                return 0;

            var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
            return dbPlayer != null && dbPlayer.Skills.TryGetValue(skill, out var dbSkill)
                ? Amount(level.Value, dbSkill.Rank, quantity)
                : 0;
        }

        public void Give(uint player, GuildType guild, string resref, int quantity, bool playerProduced)
        {
            var xp = Calculate(player, guild, resref, quantity, playerProduced);
            if (xp > 0)
                GiveSkillXP(player, SkillOf(guild), xp);
        }

        private int? RecipeLevel(SkillType skill, string resref)
        {
            _recipeLevels ??= GetAllRecipes()
                .Where(recipe => recipe.IsActive && !string.IsNullOrEmpty(recipe.Resref))
                .GroupBy(recipe => (recipe.Skill, recipe.Resref.ToLowerInvariant()))
                .ToDictionary(group => group.Key, group => group.Min(recipe => recipe.Level));

            return _recipeLevels.TryGetValue((skill, resref.ToLowerInvariant()), out var level) ? level : null;
        }

        public int? FishLevel(string resref)
        {
            _fishLevels ??= Enum.GetValues<FishType>()
                .Where(fish => fish != FishType.Invalid)
                .Select(fish => fish.GetAttribute<FishType, FishAttribute>())
                .Where(fish => !string.IsNullOrEmpty(fish.Resref))
                .GroupBy(fish => fish.Resref.ToLowerInvariant())
                .ToDictionary(group => group.Key, group => group.First().Level);

            return _fishLevels.TryGetValue(resref.ToLowerInvariant(), out var level) ? level : null;
        }

        /// <summary>The XP chart's pay for a level against a rank (the unconverted <c>Skill</c> service's table).</summary>
        protected virtual int GetDeltaXP(int delta) => _skill.GetDeltaXP(delta);

        /// <summary>Gives skill XP through the unconverted <c>Skill</c> service: bonuses apply, no henchman penalty.</summary>
        protected virtual void GiveSkillXP(uint player, SkillType skill, int xp) =>
            _skill.GiveSkillXP(player, skill, xp, false, false);

        /// <summary>Every recipe the unconverted <c>Craft</c> service knows.</summary>
        protected virtual IEnumerable<RecipeDetail> GetAllRecipes() => _craft.GetAllRecipes().Values;
    }
}
