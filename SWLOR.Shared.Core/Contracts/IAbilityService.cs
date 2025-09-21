using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IAbilityService
    {
        void CacheData();
        void CacheAbilities();
        void CacheToggleActions();
        bool IsFeatRegistered(FeatType featType);
        AbilityDetail GetAbilityDetail(FeatType featType);
        bool CanUseAbility(uint activator, uint target, FeatType feat, int level, bool isConcentration);
        bool CanUseConcentration(uint activator, uint target, FeatType feat, int level);
        void ProcessConcentrationEffects();
        void StartConcentrationAbility(uint creature, uint target, FeatType feat, StatusEffectType statusEffectType);
        ActiveConcentrationAbility GetActiveConcentration(uint creature);
        void EndConcentrationAbility(uint creature);
        void ToggleAbility(uint player, AbilityToggleType toggleType, bool isToggled);
        bool IsAbilityToggled(uint player, AbilityToggleType toggleType);
        bool IsAbilityToggled(string playerId, AbilityToggleType toggleType);
        bool IsAnyAbilityToggled(uint player);
        void AddLeadershipCombatPoint();
        void ApplyAura(uint activator, StatusEffectType type, bool targetsSelf, bool targetsParty, bool targetsEnemies);
        bool ToggleAura(uint activator, StatusEffectType type);
        void ReapplyPlayerAuraAOE(uint player);
        void ApplyAuraAOE();
        void ClearAurasOnExit();
        void ClearAurasOnDeath();
        void ReapplyAuraOnRespawn();
        void ClearAurasOnSpaceEntry();
        void AuraEnter();
        void AuraExit();
        void ApplyTemporaryImmunity(uint target, float abilityDuration, ImmunityType immunity);
    }
}
