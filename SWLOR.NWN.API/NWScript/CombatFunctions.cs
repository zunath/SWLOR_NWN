using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Get the last attacker of oAttackee.  This should only be used ONLY in the
        ///   OnAttacked events for creatures, placeables and doors.
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetLastAttacker(uint oAttackee = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetLastAttacker(oAttackee);
        }

        /// <summary>
        ///   Attack oAttackee.
        ///   - bPassive: If this is TRUE, attack is in passive mode.
        /// </summary>
        public static void ActionAttack(uint oAttackee, bool bPassive = false)
        {
            global::NWN.Core.NWScript.ActionAttack(oAttackee, bPassive ? 1 : 0);
        }

        /// <summary>
        ///   Do a Fortitude Save check for the given DC
        ///   - oCreature
        ///   - nDC: Difficulty check
        ///   - nSaveType: SAVING_THROW_TYPE_*
        ///   - oSaveVersus
        ///   Returns: 0 if the saving throw roll failed
        ///   Returns: 1 if the saving throw roll succeeded
        ///   Returns: 2 if the target was immune to the save type specified
        ///   Note: If used within an Area of Effect Object Script (On Enter, OnExit, OnHeartbeat), you MUST pass
        ///   GetAreaOfEffectCreator() into oSaveVersus!!
        /// </summary>
        public static SavingThrowResultType FortitudeSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All,
            uint oSaveVersus = OBJECT_INVALID)
        {
            return (SavingThrowResultType)global::NWN.Core.NWScript.FortitudeSave(oCreature, nDC, (int)nSaveType, oSaveVersus);
        }

        /// <summary>
        ///   Does a Reflex Save check for the given DC
        ///   - oCreature
        ///   - nDC: Difficulty check
        ///   - nSaveType: SAVING_THROW_TYPE_*
        ///   - oSaveVersus
        ///   Returns: 0 if the saving throw roll failed
        ///   Returns: 1 if the saving throw roll succeeded
        ///   Returns: 2 if the target was immune to the save type specified
        ///   Note: If used within an Area of Effect Object Script (On Enter, OnExit, OnHeartbeat), you MUST pass
        ///   GetAreaOfEffectCreator() into oSaveVersus!!
        /// </summary>
        public static SavingThrowResultType ReflexSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All,
            uint oSaveVersus = OBJECT_INVALID)
        {
            return (SavingThrowResultType)global::NWN.Core.NWScript.ReflexSave(oCreature, nDC, (int)nSaveType, oSaveVersus);
        }

        /// <summary>
        ///   Does a Will Save check for the given DC
        ///   - oCreature
        ///   - nDC: Difficulty check
        ///   - nSaveType: SAVING_THROW_TYPE_*
        ///   - oSaveVersus
        ///   Returns: 0 if the saving throw roll failed
        ///   Returns: 1 if the saving throw roll succeeded
        ///   Returns: 2 if the target was immune to the save type specified
        ///   Note: If used within an Area of Effect Object Script (On Enter, OnExit, OnHeartbeat), you MUST pass
        ///   GetAreaOfEffectCreator() into oSaveVersus!!
        /// </summary>
        public static SavingThrowResultType WillSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All,
            uint oSaveVersus = OBJECT_INVALID)
        {
            return (SavingThrowResultType)global::NWN.Core.NWScript.WillSave(oCreature, nDC, (int)nSaveType, oSaveVersus);
        }

        /// <summary>
        ///   Do a Spell Resistance check between oCaster and oTarget, returning TRUE if
        ///   the spell was resisted.
        ///   * Return value if oCaster or oTarget is an invalid object: FALSE
        ///   * Return value if spell cast is not a player spell: - 1
        ///   * Return value if spell resisted: 1
        ///   * Return value if spell resisted via magic immunity: 2
        ///   * Return value if spell resisted via spell absorption: 3
        /// </summary>
        public static int ResistSpell(uint oCaster, uint oTarget)
        {
            return global::NWN.Core.NWScript.ResistSpell(oCaster, oTarget);
        }

        /// <summary>
        ///   The caller will perform a Melee Touch Attack on oTarget
        ///   This is not an action, and it assumes the caller is already within range of
        ///   oTarget
        ///   * Returns 0 on a miss, 1 on a hit and 2 on a critical hit
        /// </summary>
        public static TouchAttackReturn TouchAttackMelee(uint oTarget, bool bDisplayFeedback = true)
        {
            return (TouchAttackReturn)global::NWN.Core.NWScript.TouchAttackMelee(oTarget, bDisplayFeedback ? 1 : 0);
        }

        /// <summary>
        ///   The caller will perform a Ranged Touch Attack on oTarget
        ///   * Returns 0 on a miss, 1 on a hit and 2 on a critical hit
        /// </summary>
        public static TouchAttackReturn TouchAttackRanged(uint oTarget, bool bDisplayFeedback = true)
        {
            return (TouchAttackReturn)global::NWN.Core.NWScript.TouchAttackRanged(oTarget, bDisplayFeedback ? 1 : 0);
        }

        /// <summary>
        ///   Get the attack mode (COMBAT_MODE_*) of oCreature's last attack.
        ///   This only works when oCreature is in combat.
        /// </summary>
        public static CombatMode GetLastAttackMode(uint oCreature = OBJECT_INVALID)
        {
            return (CombatMode)global::NWN.Core.NWScript.GetLastAttackMode(oCreature);
        }

        /// <summary>
        ///   Get the last weapon that oCreature used in an attack.
        ///   * Returns OBJECT_INVALID if oCreature did not attack, or has no weapon equipped.
        /// </summary>
        public static uint GetLastWeaponUsed(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetLastWeaponUsed(oCreature);
        }

        /// <summary>
        ///   Get the amount of damage of type nDamageType that has been dealt to the caller.
        ///   - nDamageType: DAMAGE_TYPE_*
        /// </summary>
        public static int GetDamageDealtByType(DamageType nDamageType)
        {
            return global::NWN.Core.NWScript.GetDamageDealtByType((int)nDamageType);
        }

        /// <summary>
        ///   Get the total amount of damage that has been dealt to the caller.
        /// </summary>
        public static int GetTotalDamageDealt()
        {
            return global::NWN.Core.NWScript.GetTotalDamageDealt();
        }

        /// <summary>
        ///   Get the last object that damaged oObject
        ///   * Returns OBJECT_INVALID if the passed in object is not a valid object.
        /// </summary>
        public static uint GetLastDamager(uint oObject = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetLastDamager(oObject);
        }

        /// <summary>
        ///   Get the target that the caller attempted to attack - this should be used in
        ///   conjunction with GetAttackTarget(). This value is set every time an attack is
        ///   made, and is reset at the end of combat.
        ///   * Returns OBJECT_INVALID if the caller is not a valid creature.
        /// </summary>
        public static uint GetAttemptedAttackTarget()
        {
            return global::NWN.Core.NWScript.GetAttemptedAttackTarget();
        }

        /// <summary>
        ///   * Returns TRUE if the weapon equipped is capable of damaging oVersus.
        /// </summary>
        public static bool GetIsWeaponEffective(uint oVersus = OBJECT_INVALID, bool bOffHand = false)
        {
            return global::NWN.Core.NWScript.GetIsWeaponEffective(oVersus, bOffHand ? 1 : 0) != 0;
        }

        /// <summary>
        ///   - nFeat: FEAT_*
        ///   - oObject
        ///   * Returns TRUE if oObject has effects on it originating from nFeat.
        /// </summary>
        public static int GetHasFeatEffect(int nFeat, uint oObject = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetHasFeatEffect(nFeat, oObject);
        }

        /// <summary>
        /// If oCreature has nFeat, and nFeat is useable, returns the number of remaining uses left
        /// or the maximum int value if the feat has unlimited uses (eg FEAT_KNOCKDOWN)
        /// - nFeat: FEAT_*
        /// - oCreature: Creature to check the feat of
        /// </summary>
        public static int GetFeatRemainingUses(FeatType nFeat, uint oCreature)
        {
            return global::NWN.Core.NWScript.GetFeatRemainingUses((int)nFeat, oCreature);
        }
    }
}
