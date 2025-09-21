using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IStatService
    {
        void ApplyPlayerStats();
        int GetMaxFP(uint creature, Player dbPlayer = null);
        int GetMaxFP(int baseFP, int modifier, int bonus);
        int GetCurrentFP(uint creature, Player dbPlayer = null);
        int GetMaxStamina(uint creature, Player dbPlayer = null);
        int GetMaxStamina(int baseFP, int modifier, int bonus);
        int GetCurrentStamina(uint creature, Player dbPlayer = null);
        void RestoreFP(uint creature, int amount, Player dbPlayer = null);
        void ReduceFP(uint creature, int reduceBy, Player dbPlayer = null);
        void RestoreStamina(uint creature, int amount, Player dbPlayer = null);
        void ReduceStamina(uint creature, int reduceBy, Player dbPlayer = null);
        void ReapplyFoodHP();
        void AdjustPlayerMaxHP(Player entity, uint player, int adjustBy);
        void AdjustPlayerMaxFP(Player entity, int adjustBy, uint player);
        void AdjustPlayerMaxSTM(Player entity, int adjustBy, uint player);
        void ApplyPlayerMovementRate(uint player);
        void ApplyPlayerStat(Player entity, uint player, AbilityType ability);
        void AdjustPlayerRecastReduction(Player entity, int adjustBy);
        void AdjustHPRegen(Player entity, int adjustBy);
        void AdjustFPRegen(Player entity, int adjustBy);
        void AdjustSTMRegen(Player entity, int adjustBy);
        void AdjustDefense(Player entity, CombatDamageType type, int adjustBy);
        void AdjustEvasion(Player entity, int adjustBy);
        void AdjustAttack(Player entity, int adjustBy);
        void AdjustForceAttack(Player entity, int adjustBy);
        void AdjustControl(Player entity, SkillType skillType, int adjustBy);
        void AdjustCraftsmanship(Player entity, SkillType skillType, int adjustBy);
        void AdjustCPBonus(Player entity, SkillType skillType, int adjustBy);
        int GetAttack(uint creature, AbilityType abilityType, SkillType skillType, int attackBonusOverride = 0);
        int GetAttackNative(CNWSCreature creature, BaseItem itemType);
        int GetAttack(int level, int stat, int bonus);
        int GetDefense(uint creature, CombatDamageType type, AbilityType abilityType, int defenseBonusOverride = 0);
        int CalculateDefense(int defenderStat, int skillLevel, int defenseBonus);
        int GetStatValueNative(CNWSCreature creature, AbilityType statType);
        int GetDefenseNative(CNWSCreature creature, CombatDamageType type, AbilityType abilityType);
        int GetAccuracy(uint creature, uint weapon, AbilityType statOverride, SkillType skillOverride);
        int GetAccuracyNative(CNWSCreature creature, CNWSItem weapon, AbilityType statOverride);
        int GetAccuracy(int level, int stat, int bonus);
        int GetEvasion(uint creature, SkillType skillOverride);
        int GetEvasionNative(CNWSCreature creature);
        int GetEvasion(int level, int stat, int bonus);
        NPCStats GetNPCStats(uint npc);
        void ApplyAttacksPerRound(uint creature, uint rightHandWeapon, uint offHandItem = OBJECT_INVALID);
        void ApplyCritModifier(uint player, uint rightHandWeapon);
        string GetAbilityNameShort(AbilityType type);
        int CalculateControl(uint player, SkillType craftingSkillType);
        int CalculateCraftsmanship(uint player, SkillType craftingSkillType);
        int CalculateBaseSavingThrow(uint player, SavingThrow type, uint offHandItem = OBJECT_INVALID);
        void LoadNPCStats();
        void RestoreNPCStats(bool outOfCombatRegen);
    }
}
