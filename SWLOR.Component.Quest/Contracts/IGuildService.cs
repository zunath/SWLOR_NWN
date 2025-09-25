using SWLOR.Shared.Domain.Communication.Enums;

namespace SWLOR.Component.Quest.Contracts
{
    public interface IGuildService
    {
        int MaxRank { get; }
        void LoadData();
        GuildAttribute GetGuild(GuildType guild);
        int CalculateGPReward(uint player, GuildType guild, int baseAmount);
        void GiveGuildPoints(uint player, GuildType guild, int amount);
        int GetGPRequiredForRank(int rank);
    }
}
