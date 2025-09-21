using System;
using System.Collections.Generic;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IGuildService
    {
        int MaxRank { get; }
        DateTime? DateTasksLoaded { get; }
        void LoadData();
        GuildAttribute GetGuild(GuildType guild);
        int CalculateGPReward(uint player, GuildType guild, int baseAmount);
        void GiveGuildPoints(uint player, GuildType guild, int amount);
        void RefreshGuildTasks();
        List<QuestDetail> GetActiveGuildTasksByRank(GuildType guild, int rank);
        Dictionary<string, QuestDetail> GetAllActiveGuildTasks(GuildType guild);
        int GetGPRequiredForRank(int rank);
    }
}
