﻿using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Quest.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Quest.Reward
{
    public class QuestGPReward: IQuestReward
    {
        public GuildType Guild { get; }
        public int Amount { get; }

        public QuestGPReward(GuildType guild, int amount, bool isSelectable)
        {
            Guild = guild;
            Amount = amount;
            IsSelectable = isSelectable;

            var dbGuild = DataService.Guild.GetByID((int) guild);
            MenuName = Amount + " " + dbGuild.Name + " GP";
        }

        public bool IsSelectable { get; }
        public string MenuName { get; }

        public void GiveReward(NWPlayer player)
        {
            var pcGP = DataService.PCGuildPoint.GetByPlayerIDAndGuildID(player.GlobalID, (int)Guild);
            float rankBonus = 0.25f * pcGP.Rank;
            int gp = Amount + (int)(Amount * rankBonus);
            GuildService.GiveGuildPoints(player, Guild, gp);
        }
    }
}
