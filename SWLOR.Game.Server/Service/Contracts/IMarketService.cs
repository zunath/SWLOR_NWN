using System;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IMarketService
    {
        int NumberOfItemsAllowedToBeSoldAtATime { get; }
        PCMarketData GetPlayerMarketData(NWPlayer player);
        void ClearPlayerMarketData(NWPlayer player);
        int GetMarketRegionID(NWPlaceable terminal);
        void GiveMarketGoldToPlayer(Guid playerID, int amount);
        void OnModuleEnter();
        void OnModuleNWNXChat();
        float CalculateFeePercentage(int days);
        int DetermineMarketCategory(NWItem item);
    }
}
