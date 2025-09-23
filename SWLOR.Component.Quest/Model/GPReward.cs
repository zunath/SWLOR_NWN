using SWLOR.Component.Quest.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.Quest.Model
{
    public class GPReward: IQuestReward
    {
        private readonly IGuildService _guildService;
        public bool IsSelectable { get; }
        public string MenuName { get; }
        public GuildType Guild { get; }
        public int Amount { get; }

        public GPReward(IGuildService guildService, GuildType guild, int amount, bool isSelectable)
        {
            _guildService = guildService;
            IsSelectable = isSelectable;
            Guild = guild;
            Amount = amount;

            var guildDetail = _guildService.GetGuild(guild);
            MenuName = amount + " " + guildDetail.Name + " GP";
        }

        public void GiveReward(uint player)
        {
            var reward = _guildService.CalculateGPReward(player, Guild, Amount);
            _guildService.GiveGuildPoints(player, Guild, reward);
        }
    }
}
