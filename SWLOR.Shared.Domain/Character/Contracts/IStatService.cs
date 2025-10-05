namespace SWLOR.Shared.Domain.Character.Contracts
{
    public interface IStatServiceNew
    {
        int CalculateMaxHP(uint creature);
        int CalculateMaxFP(uint creature);
        int CalculateMaxSTM(uint creature);
        int CalculateHPRegen(uint creature);
        int CalculateFPRegen(uint creature);
        int CalculateSTMRegen(uint creature);
        float CalculateRecastReduction(uint creature);
        int CalculateDefense(uint creature);
        int CalculateEvasion(uint creature);
        int CalculateAccuracy(uint creature);
        int CalculateAttack(uint creature);
        int CalculateForceAttack(uint creature);
        int CalculateMight(uint creature);
        int CalculatePerception(uint creature);
        int CalculateVitality(uint creature);
        int CalculateAgility(uint creature);
        int CalculateWillpower(uint creature);
        int CalculateSocial(uint creature);
        int CalculateShieldDeflection(uint creature);
        int CalculateAttackDeflection(uint creature);
        int CalculateCriticalRate(uint creature);
        int CalculateEnmity(uint creature);
        int CalculateHaste(uint creature);
        int CalculateSlow(uint creature);
        int CalculateDamageReduction(uint creature);
        int CalculateForceDefense(uint creature);
        int CalculateQueuedDMGBonus(uint creature);
        int CalculateParalysis(uint creature);
        int CalculateAccuracyModifier(uint creature);
        int CalculateRecastReductionModifier(uint creature);
        int CalculateDefenseBypassModifier(uint creature);
        int CalculateHealingModifier(uint creature);
        int CalculateFPRestoreOnHit(uint creature);
        int CalculateDefenseModifier(uint creature);
        int CalculateForceDefenseModifier(uint creature);
        int CalculateExtraAttackModifier(uint creature);
        int CalculateAttackModifier(uint creature);
        int CalculateForceAttackModifier(uint creature);
        int CalculateEvasionModifier(uint creature);
        int CalculateXPModifier(uint creature);
        int CalculatePoisonResist(uint creature);
        int CalculateLevel(uint creature);
    }
}
