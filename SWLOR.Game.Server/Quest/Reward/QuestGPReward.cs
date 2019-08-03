using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Quest.Reward
{
    public class QuestGPReward: IQuestReward
    {
        private readonly GuildType _guild;
        private readonly int _amount;

        public QuestGPReward(GuildType guild, int amount)
        {
            _guild = guild;
            _amount = amount;
        }

        public void GiveReward(NWPlayer player)
        {
            var pcGP = DataService.PCGuildPoint.GetByPlayerIDAndGuildID(player.GlobalID, (int)_guild);
            float rankBonus = 0.25f * pcGP.Rank;
            int gp = _amount + (int)(_amount * rankBonus);
            GuildService.GiveGuildPoints(player, _guild, gp);
        }
    }
}
