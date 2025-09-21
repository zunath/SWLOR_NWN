using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Contracts
{
    public interface ICombatService
    {
        void LoadDamageTypes();
        void AddDamageTypeDefenses();
        List<CombatDamageType> GetAllDamageTypes();
        (int, int) CalculateDamageRange(int attackerAttack, int defenderDefense, int weaponDMG, int criticalModifier);
        int CalculateHitRate(int attackerAccuracy, int defenderDefense, int attackerLevel, int defenderLevel);
        int CalculateCriticalRate(int attackerPER, int defenderMGT, int criticalModifier);
        int CalculateDamage(int attackerAttack, int defenderDefense, int weaponDMG, int criticalModifier, bool isCritical);
        int GetAbilityDamageBonus(uint creature, SkillType skill);
        void ClearCombatState();
        string BuildCombatLogMessage(uint attacker, uint target, int damage, CombatDamageType damageType, bool isCritical);
        string BuildCombatLogMessageNative(CNWSCreature attacker, CNWSCreature target, int damage, CombatDamageType damageType, bool isCritical);
        int GetPerkAdjustedAbilityScore(uint attacker);
        int GetMiscDMGBonus(uint attacker, BaseItem weaponType);
        int GetMightDMGBonus(uint attacker, BaseItem weaponType);
        int GetDoublehandDMGBonus(uint attacker);
        int GetPowerAttackDMGBonus(uint attacker);
        int GetDoublehandDMGBonusNative(CNWSCreature attacker);
        int CalculateSavingThrowDC(uint attacker, int baseDC, int attackerLevel, int defenderLevel);
    }
}
