using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IPlayerStatService
    {
        float EffectiveResidencyBonus(NWPlayer player);
        void ApplyStatChanges(NWPlayer player, NWItem ignoreItem, bool isInitialization = false);
        EffectiveItemStats GetPlayerItemEffectiveStats(NWPlayer player, NWItem ignoreItem = null);
    }
}