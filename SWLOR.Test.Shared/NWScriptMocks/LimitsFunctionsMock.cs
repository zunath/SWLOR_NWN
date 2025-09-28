namespace SWLOR.Test.Shared.NWScriptMocks
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for limits
        private int _attackBonusLimit = 20;
        private int _damageBonusLimit = 20;
        private int _savingThrowBonusLimit = 20;
        private int _abilityBonusLimit = 12;
        private int _abilityPenaltyLimit = -12;
        private int _skillBonusLimit = 50;

        public int GetAttackBonusLimit() => _attackBonusLimit;

        public int GetDamageBonusLimit() => _damageBonusLimit;

        public int GetSavingThrowBonusLimit() => _savingThrowBonusLimit;

        public int GetAbilityBonusLimit() => _abilityBonusLimit;

        public int GetAbilityPenaltyLimit() => _abilityPenaltyLimit;

        public int GetSkillBonusLimit() => _skillBonusLimit;

        public void SetAttackBonusLimit(int nNewLimit) => _attackBonusLimit = nNewLimit;

        public void SetDamageBonusLimit(int nNewLimit) => _damageBonusLimit = nNewLimit;

        public void SetSavingThrowBonusLimit(int nNewLimit) => _savingThrowBonusLimit = nNewLimit;

        public void SetAbilityBonusLimit(int nNewLimit) => _abilityBonusLimit = nNewLimit;

        public void SetAbilityPenaltyLimit(int nNewLimit) => _abilityPenaltyLimit = nNewLimit;

        public void SetSkillBonusLimit(int nNewLimit) => _skillBonusLimit = nNewLimit;

        // Helper methods for testing

    }
}
