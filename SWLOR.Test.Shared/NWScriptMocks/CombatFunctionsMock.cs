using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScript
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for combat
        private readonly Dictionary<uint, uint> _lastAttackers = new();
        private readonly Dictionary<uint, CombatModeType> _lastAttackModes = new();
        private readonly Dictionary<uint, uint> _lastWeaponsUsed = new();
        private readonly Dictionary<DamageType, int> _damageDealtByType = new();
        private int _totalDamageDealt = 0;
        private uint _lastDamager = OBJECT_INVALID;
        private uint _attemptedAttackTarget = OBJECT_INVALID;
        private readonly Dictionary<uint, Dictionary<uint, bool>> _weaponEffectiveness = new();
        private readonly Dictionary<uint, Dictionary<int, int>> _featEffects = new();
        private readonly Dictionary<uint, Dictionary<FeatType, int>> _featRemainingUses = new();
        private readonly Dictionary<uint, Dictionary<uint, SavingThrowResultType>> _savingThrows = new();
        private readonly Dictionary<uint, int> _spellResistance = new();
        private readonly Dictionary<uint, TouchAttackReturnType> _touchAttackResults = new();

        public uint GetLastAttacker(uint oAttackee = OBJECT_INVALID) => 
            _lastAttackers.GetValueOrDefault(oAttackee, OBJECT_INVALID);

        public void ActionAttack(uint oAttackee, bool bPassive = false) 
        {
            _lastAttackers[OBJECT_SELF] = oAttackee;
            _attemptedAttackTarget = oAttackee;
        }

        public SavingThrowResultType FortitudeSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All, uint oSaveVersus = OBJECT_INVALID) 
        {
            var key = $"{oCreature}|{oSaveVersus}|{(int)nSaveType}";
            var result = _savingThrows.GetValueOrDefault(oCreature, new Dictionary<uint, SavingThrowResultType>())
                .GetValueOrDefault(oSaveVersus, SavingThrowResultType.Failed);
            return result;
        }

        public SavingThrowResultType ReflexSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All, uint oSaveVersus = OBJECT_INVALID) 
        {
            var key = $"{oCreature}|{oSaveVersus}|{(int)nSaveType}";
            var result = _savingThrows.GetValueOrDefault(oCreature, new Dictionary<uint, SavingThrowResultType>())
                .GetValueOrDefault(oSaveVersus, SavingThrowResultType.Failed);
            return result;
        }

        public SavingThrowResultType WillSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All, uint oSaveVersus = OBJECT_INVALID) 
        {
            var key = $"{oCreature}|{oSaveVersus}|{(int)nSaveType}";
            var result = _savingThrows.GetValueOrDefault(oCreature, new Dictionary<uint, SavingThrowResultType>())
                .GetValueOrDefault(oSaveVersus, SavingThrowResultType.Failed);
            return result;
        }

        public int ResistSpell(uint oCaster, uint oTarget) => 
            _spellResistance.GetValueOrDefault(oTarget, 0);

        public TouchAttackReturnType TouchAttackMelee(uint oTarget, bool bDisplayFeedback = true, uint oAttacker = OBJECT_INVALID) 
        {
            var result = _touchAttackResults.GetValueOrDefault(oTarget, TouchAttackReturnType.Miss);
            return result;
        }

        public TouchAttackReturnType TouchAttackRanged(uint oTarget, bool bDisplayFeedback = true, uint oAttacker = OBJECT_INVALID) 
        {
            var result = _touchAttackResults.GetValueOrDefault(oTarget, TouchAttackReturnType.Miss);
            return result;
        }

        public CombatModeType GetLastAttackMode(uint oCreature = OBJECT_INVALID) => 
            _lastAttackModes.GetValueOrDefault(oCreature, CombatModeType.Invalid);

        public uint GetLastWeaponUsed(uint oCreature) => 
            _lastWeaponsUsed.GetValueOrDefault(oCreature, OBJECT_INVALID);

        public int GetDamageDealtByType(DamageType nDamageType) => 
            _damageDealtByType.GetValueOrDefault(nDamageType, 0);

        public int GetTotalDamageDealt() => _totalDamageDealt;

        public uint GetLastDamager(uint oObject = OBJECT_INVALID) => _lastDamager;

        public uint GetAttemptedAttackTarget() => _attemptedAttackTarget;

        public bool GetIsWeaponEffective(uint oVersus = OBJECT_INVALID, bool bOffHand = false) => 
            _weaponEffectiveness.GetValueOrDefault(OBJECT_SELF, new Dictionary<uint, bool>())
                .GetValueOrDefault(oVersus, false);

        public int GetHasFeatEffect(int nFeat, uint oObject = OBJECT_INVALID) => 
            _featEffects.GetValueOrDefault(oObject, new Dictionary<int, int>())
                .GetValueOrDefault(nFeat, 0);

        public int GetFeatRemainingUses(FeatType nFeat, uint oCreature = OBJECT_INVALID) => 
            _featRemainingUses.GetValueOrDefault(oCreature, new Dictionary<FeatType, int>())
                .GetValueOrDefault(nFeat, 0);

        // Helper methods for testing




    }
}
