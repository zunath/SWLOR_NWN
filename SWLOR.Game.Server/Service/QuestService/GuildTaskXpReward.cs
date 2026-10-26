using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.QuestService
{
    /// <summary>
    /// The skill XP a crafting guild hand-in gives (<see cref="Service.GuildTaskXp"/>): a share of the XP chart's pay for the task's level against
    /// the player's rank in the guild's skill, for every item handed in. It goes through the same XP path as crafting, so Social and food bonuses apply.
    /// </summary>
    public class GuildTaskXpReward : IQuestReward
    {
        public bool IsSelectable => false;
        public string MenuName => "Skill XP";
        public GuildType Guild { get; }
        public string Resref { get; }
        public int Quantity { get; }
        public bool PlayerProduced { get; }

        public GuildTaskXpReward(GuildType guild, string resref, int quantity, bool playerProduced)
        {
            Guild = guild;
            Resref = resref;
            Quantity = quantity;
            PlayerProduced = playerProduced;
        }

        /// <summary>The XP the player would get now, or 0 when the guild gives none or the task's level is unknown.</summary>
        public int Preview(uint player) => Service.GuildTaskXp.Calculate(player, Guild, Resref, Quantity, PlayerProduced);

        public void GiveReward(uint player) => Service.GuildTaskXp.Give(player, Guild, Resref, Quantity, PlayerProduced);
    }
}
