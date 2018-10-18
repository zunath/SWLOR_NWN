using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IPlayerStatService
    {
        int EffectiveArmorClass(NWPlayer player, NWItem ignoreItem);
        int EffectiveMaxFP(NWPlayer player, NWItem ignoreItem);
        int EffectiveMaxHitPoints(NWPlayer player, NWItem ignoreItem);
        float EffectiveResidencyBonus(NWPlayer player);
        void ApplyStatChanges(NWPlayer player, NWItem ignoreItem, bool isInitialization = false);
        EffectiveItemStats GetPlayerItemEffectiveStats(NWPlayer player, NWItem ignoreItem = null);
    }
}