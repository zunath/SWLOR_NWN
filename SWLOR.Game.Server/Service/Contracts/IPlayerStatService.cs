using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IPlayerStatService
    {
        int EffectiveArmorClass(NWPlayer player, NWItem ignoreItem);
        int EffectiveArmorsmithBonus(NWPlayer player);
        int EffectiveCastingSpeed(NWPlayer player);
        int EffectiveCookingBonus(NWPlayer player);
        int EffectiveDarkAbilityBonus(NWPlayer player);
        int EffectiveEngineeringBonus(NWPlayer player);
        float EffectiveEnmityRate(NWPlayer player);
        int EffectiveFabricationBonus(NWPlayer player);
        int EffectiveFPRegenBonus(NWPlayer player);
        int EffectiveHarvestingBonus(NWPlayer player);
        int EffectiveHPRegenBonus(NWPlayer player);
        int EffectiveLightAbilityBonus(NWPlayer player);
        int EffectiveLuckBonus(NWPlayer player);
        int EffectiveMaxFP(NWPlayer player, NWItem ignoreItem);
        int EffectiveMaxHitPoints(NWPlayer player, NWItem ignoreItem);
        int EffectiveMedicineBonus(NWPlayer player);
        int EffectiveMeditateBonus(NWPlayer player);
        int EffectiveRestBonus(NWPlayer player);
        int EffectiveSneakAttackBonus(NWPlayer player);
        int EffectiveWeaponsmithBonus(NWPlayer player);
        float EffectiveResidencyBonus(NWPlayer player);
        void ApplyStatChanges(NWPlayer player, NWItem ignoreItem, bool isInitialization = false);
    }
}