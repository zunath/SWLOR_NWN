using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Economy
{
    /// <summary>
    /// The skill XP a crafting guild hand-in gives. Static callers reach the
    /// container's instance of this interface through <c>GameServices.GuildTaskXp</c>.
    /// </summary>
    public interface IGuildTaskXpService
    {
        /// <summary>The skill a guild's hand-ins give XP in; <see cref="SkillType.Invalid"/> when they give none.</summary>
        SkillType SkillOf(GuildType guild);

        /// <summary>True when the guild's hand-ins give skill XP.</summary>
        bool GivesXp(GuildType guild);

        /// <summary>
        /// The XP for handing in <paramref name="quantity"/> items of a task at <paramref name="taskLevel"/> to a player at <paramref name="skillRank"/>:
        /// the XP percent of the chart's XP for the level against the rank, per item, rounded down.
        /// </summary>
        int Amount(int taskLevel, int skillRank, int quantity);

        /// <summary>
        /// The level that sets a task's XP. A crafted task (<paramref name="playerProduced"/>) uses the lowest level of an active recipe of the
        /// guild's skill that makes the item. Any other task uses the fish's level when the item is a fish, and otherwise the recipe's, as for a
        /// vendor good a recipe also makes. Null when neither is found.
        /// </summary>
        int? TaskLevel(
            GuildType guild,
            string resref,
            bool playerProduced,
            Func<SkillType, string, int?> recipeLevel,
            Func<string, int?> fishLevel);

        /// <summary>The task's level from the server's recipes and fish.</summary>
        int? TaskLevel(GuildType guild, string resref, bool playerProduced);

        /// <summary>The XP a player would get now for handing in the task's items; 0 when the guild or the task gives none.</summary>
        int Calculate(uint player, GuildType guild, string resref, int quantity, bool playerProduced);

        /// <summary>Gives the XP of a hand-in the way crafting gives its XP: bonuses apply and no henchman penalty is taken.</summary>
        void Give(uint player, GuildType guild, string resref, int quantity, bool playerProduced);

        /// <summary>The level of the fish with the resref, from the fish catalog; null when it is not a fish.</summary>
        int? FishLevel(string resref);
    }
}
