using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Contracts
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
