using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Service.QuestService
{
    /// <summary>
    /// The skill XP a crafting guild hand-in gives (<see cref="Domain.Economy.GuildTaskXpService"/>): a share of the XP chart's pay for the task's level against
    /// the player's rank in the guild's skill, for every item handed in. It goes through the same XP path as crafting, so Social and food bonuses apply.
    /// The XP is recorded as waiting craft XP (<see cref="Player.PendingCraftXp"/>) in the save that completes the task, then given from there,
    /// so a stop between the hand-in and the award cannot lose it.
    /// </summary>
    public class GuildTaskXpReward : IQuestRewardSavedWithCompletion
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
        public int Preview(uint player) => GuildTaskXp.Calculate(player, Guild, Resref, Quantity, PlayerProduced);

        public void SaveWithCompletion(uint player, Player dbPlayer)
        {
            var xp = Preview(player);
            if (xp <= 0)
                return;

            var skill = GuildTaskXp.SkillOf(Guild);
            dbPlayer.PendingCraftXp ??= new Dictionary<SkillService.SkillType, int>();
            dbPlayer.PendingCraftXp[skill] = dbPlayer.PendingCraftXp.GetValueOrDefault(skill) + xp;
        }

        /// <summary>Gives the XP <see cref="SaveWithCompletion"/> recorded, once, through the craft XP path.</summary>
        public void GiveReward(uint player) => CraftSessions.GrantPendingXp(player);
    }
}
