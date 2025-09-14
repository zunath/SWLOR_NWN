using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Gets the last attacker of the specified target.
        /// </summary>
        /// <param name="oAttackee">The target that was attacked (default: OBJECT_INVALID)</param>
        /// <returns>The last attacker. Returns OBJECT_INVALID on error</returns>
        /// <remarks>This should only be used in the OnAttacked events for creatures, placeables and doors.</remarks>
        public static uint GetLastAttacker(uint oAttackee = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetLastAttacker(oAttackee);
        }

        /// <summary>
        /// Makes the action subject attack the specified target.
        /// </summary>
        /// <param name="oAttackee">The target to attack</param>
        /// <param name="bPassive">If true, attack is in passive mode (default: false)</param>
        public static void ActionAttack(uint oAttackee, bool bPassive = false)
        {
            global::NWN.Core.NWScript.ActionAttack(oAttackee, bPassive ? 1 : 0);
        }

        /// <summary>
        /// Performs a Fortitude Save check for the given difficulty class.
        /// </summary>
        /// <param name="oCreature">The creature making the save</param>
        /// <param name="nDC">The difficulty class to beat</param>
        /// <param name="nSaveType">The type of saving throw (SAVING_THROW_TYPE_* constants) (default: SavingThrowType.All)</param>
        /// <param name="oSaveVersus">The object the save is against (default: OBJECT_INVALID)</param>
        /// <returns>0 if the saving throw roll failed, 1 if the saving throw roll succeeded, 2 if the target was immune to the save type specified</returns>
        /// <remarks>If used within an Area of Effect Object Script (On Enter, OnExit, OnHeartbeat), you MUST pass GetAreaOfEffectCreator() into oSaveVersus!!</remarks>
        public static SavingThrowResultType FortitudeSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All,
            uint oSaveVersus = OBJECT_INVALID)
        {
            return (SavingThrowResultType)global::NWN.Core.NWScript.FortitudeSave(oCreature, nDC, (int)nSaveType, oSaveVersus);
        }

        /// <summary>
        /// Performs a Reflex Save check for the given difficulty class.
        /// </summary>
        /// <param name="oCreature">The creature making the save</param>
        /// <param name="nDC">The difficulty class to beat</param>
        /// <param name="nSaveType">The type of saving throw (SAVING_THROW_TYPE_* constants) (default: SavingThrowType.All)</param>
        /// <param name="oSaveVersus">The object the save is against (default: OBJECT_INVALID)</param>
        /// <returns>0 if the saving throw roll failed, 1 if the saving throw roll succeeded, 2 if the target was immune to the save type specified</returns>
        /// <remarks>If used within an Area of Effect Object Script (On Enter, OnExit, OnHeartbeat), you MUST pass GetAreaOfEffectCreator() into oSaveVersus!!</remarks>
        public static SavingThrowResultType ReflexSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All,
            uint oSaveVersus = OBJECT_INVALID)
        {
            return (SavingThrowResultType)global::NWN.Core.NWScript.ReflexSave(oCreature, nDC, (int)nSaveType, oSaveVersus);
        }

        /// <summary>
        /// Performs a Will Save check for the given difficulty class.
        /// </summary>
        /// <param name="oCreature">The creature making the save</param>
        /// <param name="nDC">The difficulty class to beat</param>
        /// <param name="nSaveType">The type of saving throw (SAVING_THROW_TYPE_* constants) (default: SavingThrowType.All)</param>
        /// <param name="oSaveVersus">The object the save is against (default: OBJECT_INVALID)</param>
        /// <returns>0 if the saving throw roll failed, 1 if the saving throw roll succeeded, 2 if the target was immune to the save type specified</returns>
        /// <remarks>If used within an Area of Effect Object Script (On Enter, OnExit, OnHeartbeat), you MUST pass GetAreaOfEffectCreator() into oSaveVersus!!</remarks>
        public static SavingThrowResultType WillSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All,
            uint oSaveVersus = OBJECT_INVALID)
        {
            return (SavingThrowResultType)global::NWN.Core.NWScript.WillSave(oCreature, nDC, (int)nSaveType, oSaveVersus);
        }

        /// <summary>
        /// Performs a Spell Resistance check between the caster and target.
        /// </summary>
        /// <param name="oCaster">The spell caster</param>
        /// <param name="oTarget">The target of the spell</param>
        /// <returns>Return values: FALSE if oCaster or oTarget is an invalid object, -1 if spell cast is not a player spell, 1 if spell resisted, 2 if spell resisted via magic immunity, 3 if spell resisted via spell absorption</returns>
        public static int ResistSpell(uint oCaster, uint oTarget)
        {
            return global::NWN.Core.NWScript.ResistSpell(oCaster, oTarget);
        }

        /// <summary>
        /// Makes the caller perform a Melee Touch Attack on the target.
        /// </summary>
        /// <param name="oTarget">The target to attack</param>
        /// <param name="bDisplayFeedback">Whether to display feedback (default: true)</param>
        /// <returns>0 on a miss, 1 on a hit, and 2 on a critical hit</returns>
        /// <remarks>This is not an action, and it assumes the caller is already within range of the target.</remarks>
        public static TouchAttackReturn TouchAttackMelee(uint oTarget, bool bDisplayFeedback = true)
        {
            return (TouchAttackReturn)global::NWN.Core.NWScript.TouchAttackMelee(oTarget, bDisplayFeedback ? 1 : 0);
        }

        /// <summary>
        /// Makes the caller perform a Ranged Touch Attack on the target.
        /// </summary>
        /// <param name="oTarget">The target to attack</param>
        /// <param name="bDisplayFeedback">Whether to display feedback (default: true)</param>
        /// <returns>0 on a miss, 1 on a hit, and 2 on a critical hit</returns>
        public static TouchAttackReturn TouchAttackRanged(uint oTarget, bool bDisplayFeedback = true)
        {
            return (TouchAttackReturn)global::NWN.Core.NWScript.TouchAttackRanged(oTarget, bDisplayFeedback ? 1 : 0);
        }

        /// <summary>
        /// Gets the attack mode of the creature's last attack.
        /// </summary>
        /// <param name="oCreature">The creature to get the attack mode for (default: OBJECT_INVALID)</param>
        /// <returns>The attack mode (COMBAT_MODE_* constants)</returns>
        /// <remarks>This only works when the creature is in combat.</remarks>
        public static CombatMode GetLastAttackMode(uint oCreature = OBJECT_INVALID)
        {
            return (CombatMode)global::NWN.Core.NWScript.GetLastAttackMode(oCreature);
        }

        /// <summary>
        /// Gets the last weapon that the creature used in an attack.
        /// </summary>
        /// <param name="oCreature">The creature to get the last weapon for</param>
        /// <returns>The last weapon used. Returns OBJECT_INVALID if the creature did not attack or has no weapon equipped</returns>
        public static uint GetLastWeaponUsed(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetLastWeaponUsed(oCreature);
        }

        /// <summary>
        /// Gets the amount of damage of the specified type that has been dealt to the caller.
        /// </summary>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants)</param>
        /// <returns>The amount of damage dealt</returns>
        public static int GetDamageDealtByType(DamageType nDamageType)
        {
            return global::NWN.Core.NWScript.GetDamageDealtByType((int)nDamageType);
        }

        /// <summary>
        /// Gets the total amount of damage that has been dealt to the caller.
        /// </summary>
        /// <returns>The total amount of damage dealt</returns>
        public static int GetTotalDamageDealt()
        {
            return global::NWN.Core.NWScript.GetTotalDamageDealt();
        }

        /// <summary>
        /// Gets the last object that damaged the specified object.
        /// </summary>
        /// <param name="oObject">The object that was damaged (default: OBJECT_INVALID)</param>
        /// <returns>The last object that damaged the target. Returns OBJECT_INVALID if the passed in object is not a valid object</returns>
        public static uint GetLastDamager(uint oObject = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetLastDamager(oObject);
        }

        /// <summary>
        /// Gets the target that the caller attempted to attack.
        /// </summary>
        /// <returns>The attempted attack target. Returns OBJECT_INVALID if the caller is not a valid creature</returns>
        /// <remarks>This should be used in conjunction with GetAttackTarget(). This value is set every time an attack is made, and is reset at the end of combat.</remarks>
        public static uint GetAttemptedAttackTarget()
        {
            return global::NWN.Core.NWScript.GetAttemptedAttackTarget();
        }

        /// <summary>
        /// Returns true if the weapon equipped is capable of damaging the specified target.
        /// </summary>
        /// <param name="oVersus">The target to check effectiveness against (default: OBJECT_INVALID)</param>
        /// <param name="bOffHand">Whether to check the off-hand weapon (default: false)</param>
        /// <returns>True if the weapon is effective against the target</returns>
        public static bool GetIsWeaponEffective(uint oVersus = OBJECT_INVALID, bool bOffHand = false)
        {
            return global::NWN.Core.NWScript.GetIsWeaponEffective(oVersus, bOffHand ? 1 : 0) != 0;
        }

        /// <summary>
        /// Returns true if the object has effects on it originating from the specified feat.
        /// </summary>
        /// <param name="nFeat">The feat to check for (FEAT_* constants)</param>
        /// <param name="oObject">The object to check (default: OBJECT_INVALID)</param>
        /// <returns>True if the object has effects from the feat</returns>
        public static int GetHasFeatEffect(int nFeat, uint oObject = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetHasFeatEffect(nFeat, oObject);
        }

        /// <summary>
        /// Gets the number of remaining uses for the specified feat on the creature.
        /// </summary>
        /// <param name="nFeat">The feat to check (FEAT_* constants)</param>
        /// <param name="oCreature">The creature to check the feat for</param>
        /// <returns>The number of remaining uses left, or the maximum int value if the feat has unlimited uses (e.g., FEAT_KNOCKDOWN)</returns>
        /// <remarks>Only returns a value if the creature has the feat and it is usable.</remarks>
        public static int GetFeatRemainingUses(FeatType nFeat, uint oCreature)
        {
            return global::NWN.Core.NWScript.GetFeatRemainingUses((int)nFeat, oCreature);
        }
    }
}
