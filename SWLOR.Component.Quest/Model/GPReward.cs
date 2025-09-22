using SWLOR.Component.Quest.Contracts;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Component.Quest.Model
{
    public class GPReward: IQuestReward
    {
        public bool IsSelectable { get; }
        public string MenuName { get; }
        public GuildType Guild { get; }
        public int Amount { get; }

        public GPReward(GuildType guild, int amount, bool isSelectable)
        {
            IsSelectable = isSelectable;
            Guild = guild;
            Amount = amount;

            var guildDetail = Service.Guild.GetGuild(guild);
            MenuName = amount + " " + guildDetail.Name + " GP";
        }

        public void GiveReward(uint player)
        {
            var reward = Service.Guild.CalculateGPReward(player, Guild, Amount);
            Service.Guild.GiveGuildPoints(player, Guild, reward);
        }
    }
}
