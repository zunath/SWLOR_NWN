using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Determines whether oObject has any effects applied by nSpell
        ///   - nSpell: SPELL_*
        ///   - oObject
        ///   * The spell id on effects is only valid if the effect is created
        ///   when the spell script runs. If it is created in a delayed command
        ///   then the spell id on the effect will be invalid.
        /// </summary>
        public static bool GetHasSpellEffect(Spell nSpell, uint oObject = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetHasSpellEffect((int)nSpell, oObject) != 0;
        }

        /// <summary>
        ///   Get the spell (SPELL_*) that applied eSpellEffect.
        ///   * Returns -1 if eSpellEffect was applied outside a spell script.
        /// </summary>
        public static int GetEffectSpellId(Effect eSpellEffect)
        {
            return global::NWN.Core.NWScript.GetEffectSpellId(eSpellEffect);
        }

        /// <summary>
        ///   Use this in spell scripts to get nDamage adjusted by oTarget's reflex and
        ///   evasion saves.
        ///   - nDamage
        ///   - oTarget
        ///   - nDC: Difficulty check
        ///   - nSaveType: SAVING_THROW_TYPE_*
        ///   - oSaveVersus
        /// </summary>
        public static int GetReflexAdjustedDamage(int nDamage, uint oTarget, int nDC,
            SavingThrowType nSaveType = SavingThrowType.All, uint oSaveVersus = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetReflexAdjustedDamage(nDamage, oTarget, nDC, (int)nSaveType, oSaveVersus);
        }

        /// <summary>
        ///   Get the object at which the caller last cast a spell
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetSpellTargetObject()
        {
            return global::NWN.Core.NWScript.GetSpellTargetObject();
        }

        /// <summary>
        ///   Get the metamagic type (METAMAGIC_*) of the last spell cast by the caller
        ///   * Return value if the caster is not a valid object: -1
        /// </summary>
        public static int GetMetaMagicFeat()
        {
            return global::NWN.Core.NWScript.GetMetaMagicFeat();
        }

        /// <summary>
        ///   Get the DC to save against for a spell (10 + spell level + relevant ability
        ///   bonus).  This can be called by a creature or by an Area of Effect object.
        /// </summary>
        public static int GetSpellSaveDC()
        {
            return global::NWN.Core.NWScript.GetSpellSaveDC();
        }

        /// <summary>
        ///   Get the location of the caller's last spell target.
        /// </summary>
        public static Location GetSpellTargetLocation()
        {
            return global::NWN.Core.NWScript.GetSpellTargetLocation();
        }

        /// <summary>
        ///   Cast spell nSpell at lTargetLocation.
        ///   - nSpell: SPELL_*
        ///   - lTargetLocation
        ///   - nMetaMagic: METAMAGIC_*
        ///   - bCheat: If this is TRUE, then the executor of the action doesn't have to be
        ///   able to cast the spell.
        ///   - nProjectilePathType: PROJECTILE_PATH_TYPE_*
        ///   - bInstantSpell: If this is TRUE, the spell is cast immediately; this allows
        ///   the end-user to simulate
        ///   a high-level magic user having lots of advance warning of impending trouble.
        /// </summary>
        public static void ActionCastSpellAtLocation(Spell nSpell, Location lTargetLocation,
            MetaMagic nMetaMagic = MetaMagic.Any, bool bCheat = false,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default, bool bInstantSpell = false)
        {
            global::NWN.Core.NWScript.ActionCastSpellAtLocation((int)nSpell, lTargetLocation, (int)nMetaMagic, bCheat ? 1 : 0, (int)nProjectilePathType, bInstantSpell ? 1 : 0);
        }

        /// <summary>
        ///   Create an event which triggers the "SpellCastAt" script
        ///   Note: This only creates the event. The event wont actually trigger until SignalEvent()
        ///   is called using this created SpellCastAt event as an argument.
        ///   For example:
        ///   SignalEvent(oCreature, EventSpellCastAt(oCaster, SPELL_MAGIC_MISSILE, TRUE));
        ///   This function doesn't cast the spell specified, it only creates an event so that
        ///   when the event is signaled on an object, the object will use its OnSpellCastAt script
        ///   to react to the spell being cast.
        ///   To specify the OnSpellCastAt script that should run, view the Object's Properties
        ///   and click on the Scripts Tab. Then specify a script for the OnSpellCastAt event.
        ///   From inside the OnSpellCastAt script call:
        ///   GetLastSpellCaster() to get the object that cast the spell (oCaster).
        ///   GetLastSpell() to get the type of spell cast (nSpell)
        ///   GetLastSpellHarmful() to determine if the spell cast at the object was harmful.
        /// </summary>
        public static Event EventSpellCastAt(uint oCaster, Spell nSpell, bool bHarmful = true)
        {
            return global::NWN.Core.NWScript.EventSpellCastAt(oCaster, (int)nSpell, bHarmful ? 1 : 0);
        }

        /// <summary>
        ///   This is for use in a "Spell Cast" script, it gets who cast the spell.
        ///   The spell could have been cast by a creature, placeable or door.
        ///   * Returns OBJECT_INVALID if the caller is not a creature, placeable or door.
        /// </summary>
        public static uint GetLastSpellCaster()
        {
            return global::NWN.Core.NWScript.GetLastSpellCaster();
        }

        /// <summary>
        ///   This is for use in a "Spell Cast" script, it gets the ID of the spell that
        ///   was cast.
        /// </summary>
        public static int GetLastSpell()
        {
            return global::NWN.Core.NWScript.GetLastSpell();
        }

        /// <summary>
        ///   This is for use in a Spell script, it gets the ID of the spell that is being
        ///   cast (SPELL_*).
        /// </summary>
        public static int GetSpellId()
        {
            return global::NWN.Core.NWScript.GetSpellId();
        }

        /// <summary>
        ///   Use this in a SpellCast script to determine whether the spell was considered
        ///   harmful.
        ///   * Returns TRUE if the last spell cast was harmful.
        /// </summary>
        public static bool GetLastSpellHarmful()
        {
            return global::NWN.Core.NWScript.GetLastSpellHarmful() != 0;
        }

        /// <summary>
        ///   The action subject will fake casting a spell at oTarget; the conjure and cast
        ///   animations and visuals will occur, nothing else.
        ///   - nSpell
        ///   - oTarget
        ///   - nProjectilePathType: PROJECTILE_PATH_TYPE_*
        /// </summary>
        public static void ActionCastFakeSpellAtObject(Spell nSpell, uint oTarget,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default)
        {
            global::NWN.Core.NWScript.ActionCastFakeSpellAtObject((int)nSpell, oTarget, (int)nProjectilePathType);
        }

        /// <summary>
        ///   The action subject will fake casting a spell at lLocation; the conjure and
        ///   cast animations and visuals will occur, nothing else.
        ///   - nSpell
        ///   - lTarget
        ///   - nProjectilePathType: PROJECTILE_PATH_TYPE_*
        /// </summary>
        public static void ActionCastFakeSpellAtLocation(Spell nSpell, Location lTarget,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default)
        {
            global::NWN.Core.NWScript.ActionCastFakeSpellAtLocation((int)nSpell, lTarget, (int)nProjectilePathType);
        }

        /// <summary>
        ///   Counterspell oCounterSpellTarget.
        /// </summary>
        public static void ActionCounterSpell(uint oCounterSpellTarget)
        {
            global::NWN.Core.NWScript.ActionCounterSpell(oCounterSpellTarget);
        }

        /// <summary>
        ///   Get the target at which the caller attempted to cast a spell.
        ///   This value is set every time a spell is cast and is reset at the end of
        ///   combat.
        ///   * Returns OBJECT_INVALID if the caller is not a valid creature.
        /// </summary>
        public static uint GetAttemptedSpellTarget()
        {
            return global::NWN.Core.NWScript.GetAttemptedSpellTarget();
        }

        /// <summary>
        /// In the spell script returns the feat used, or -1 if no feat was used
        /// </summary>
        public static int GetSpellFeatId()
        {
            return global::NWN.Core.NWScript.GetSpellFeatId();
        }

        /// <summary>
        /// Returns TRUE if the last spell was cast spontaneously
        /// eg; a Cleric casting SPELL_CURE_LIGHT_WOUNDS when it is not prepared, using another level 1 slot
        /// </summary>
        public static bool GetSpellCastSpontaneously()
        {
            return global::NWN.Core.NWScript.GetSpellCastSpontaneously() != 0;
        }

        /// <summary>
        /// Returns the level of the last spell cast. This value is only valid in a Spell script.
        /// </summary>
        public static int GetLastSpellLevel()
        {
            return global::NWN.Core.NWScript.GetLastSpellLevel();
        }

        /// <summary>
        /// Gets the number of memorized spell slots for a given spell level.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellLevel: the spell level, 0-9.
        /// Returns: the number of spell slots.
        /// </summary>
        public static int GetMemorizedSpellCountByLevel(uint oCreature, ClassType nClassType, int nSpellLevel)
        {
            return global::NWN.Core.NWScript.GetMemorizedSpellCountByLevel(oCreature, (int)nClassType, nSpellLevel);
        }

        /// <summary>
        /// Gets the spell id of a memorized spell slot.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellLevel: the spell level, 0-9.
        /// - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()
        /// Returns: a SPELL_* constant or -1 if the slot is not set.
        /// </summary>
        public static int GetMemorizedSpellId(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            return global::NWN.Core.NWScript.GetMemorizedSpellId(oCreature, (int)nClassType, nSpellLevel, nIndex);
        }

        /// <summary>
        /// Gets the ready state of a memorized spell slot.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellLevel: the spell level, 0-9.
        /// - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()
        /// Returns: TRUE/FALSE or -1 if the slot is not set.
        /// </summary>
        public static int GetMemorizedSpellReady(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            return global::NWN.Core.NWScript.GetMemorizedSpellReady(oCreature, (int)nClassType, nSpellLevel, nIndex);
        }

        /// <summary>
        /// Gets the metamagic of a memorized spell slot.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellLevel: the spell level, 0-9.
        /// - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()
        /// Returns: a METAMAGIC_* constant or -1 if the slot is not set.
        /// </summary>
        public static int GetMemorizedSpellMetaMagic(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            return global::NWN.Core.NWScript.GetMemorizedSpellMetaMagic(oCreature, (int)nClassType, nSpellLevel, nIndex);
        }

        /// <summary>
        /// Gets if the memorized spell slot has a domain spell.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellLevel: the spell level, 0-9.
        /// - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()
        /// Returns: TRUE/FALSE or -1 if the slot is not set.
        /// </summary>
        public static int GetMemorizedSpellIsDomainSpell(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            return global::NWN.Core.NWScript.GetMemorizedSpellIsDomainSpell(oCreature, (int)nClassType, nSpellLevel, nIndex);
        }

        /// <summary>
        /// Set a memorized spell slot.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellLevel: the spell level, 0-9.
        /// - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()
        /// - nSpellId: a SPELL_* constant.
        /// - bReady: TRUE to mark the slot ready.
        /// - nMetaMagic: a METAMAGIC_* constant.
        /// - bIsDomainSpell: TRUE for a domain spell.
        /// </summary>
        public static void SetMemorizedSpell(
            uint oCreature,
            ClassType nClassType,
            int nSpellLevel,
            int nIndex,
            Spell nSpellId,
            bool bReady = true,
            MetaMagic nMetaMagic = MetaMagic.None,
            bool bIsDomainSpell = false)
        {
            global::NWN.Core.NWScript.SetMemorizedSpell(oCreature, (int)nClassType, nSpellLevel, nIndex, (int)nSpellId, bReady ? 1 : 0, (int)nMetaMagic, bIsDomainSpell ? 1 : 0);
        }

        /// <summary>
        /// Set the ready state of a memorized spell slot.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellLevel: the spell level, 0-9.
        /// - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()
        /// - bReady: TRUE to mark the slot ready.
        /// </summary>
        public static void SetMemorizedSpellReady(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex, bool bReady)
        {
            global::NWN.Core.NWScript.SetMemorizedSpellReady(oCreature, (int)nClassType, nSpellLevel, nIndex, bReady ? 1 : 0);
        }

        /// <summary>
        /// Clear a specific memorized spell slot.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellLevel: the spell level, 0-9.
        /// - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()
        /// </summary>
        public static void ClearMemorizedSpell(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            global::NWN.Core.NWScript.ClearMemorizedSpell(oCreature, (int)nClassType, nSpellLevel, nIndex);
        }

        /// <summary>
        /// Clear all memorized spell slots of a specific spell id, including metamagic'd ones.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellId: a SPELL_* constant.
        /// </summary>
        public static void ClearMemorizedSpellBySpellId(uint oCreature, ClassType nClassType, int nSpellId)
        {
            global::NWN.Core.NWScript.ClearMemorizedSpellBySpellId(oCreature, (int)nClassType, nSpellId);
        }

        /// <summary>
        ///  Gets the number of known spells for a given spell level.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a SpellBookRestricted class.
        /// - nSpellLevel: the spell level, 0-9.
        /// Returns: the number of known spells.
        /// </summary>
        public static int GetKnownSpellCount(uint oCreature, ClassType nClassType, int nSpellLevel)
        {
            return global::NWN.Core.NWScript.GetKnownSpellCount(oCreature, (int)nClassType, nSpellLevel);
        }

        /// <summary>
        /// Gets the spell id of a known spell.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a SpellBookRestricted class.
        /// - nSpellLevel: the spell level, 0-9.
        /// - nIndex: the index of the known spell. Bounds: 0 <= nIndex < GetKnownSpellCount()
        /// Returns: a SPELL_* constant or -1 on error.
        /// </summary>
        public static int GetKnownSpellId(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            return global::NWN.Core.NWScript.GetKnownSpellId(oCreature, (int)nClassType, nSpellLevel, nIndex);
        }

        /// <summary>
        /// Gets if a spell is in the known spell list.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a SpellBookRestricted class.
        /// - nSpellId: a SPELL_* constant.
        /// Returns: TRUE if the spell is in the known spell list.
        /// </summary>
        public static bool GetIsInKnownSpellList(uint oCreature, ClassType nClassType, Spell nSpellId)
        {
            return global::NWN.Core.NWScript.GetIsInKnownSpellList(oCreature, (int)nClassType, (int)nSpellId) != 0;
        }

        /// <summary>
        /// Gets the amount of uses a spell has left.
        /// - nClassType: a CLASS_TYPE_* constant.
        /// - nSpellid: a SPELL_* constant.
        /// - nMetaMagic: a METAMAGIC_* constant.
        /// - nDomainLevel: the domain level, if a domain spell.
        /// Returns: the amount of spell uses left.
        /// </summary>
        public static int GetSpellUsesLeft(
            uint oCreature,
            ClassType nClassType,
            Spell nSpellId,
            MetaMagic nMetaMagic = MetaMagic.None,
            int nDomainLevel = 0)
        {
            return global::NWN.Core.NWScript.GetSpellUsesLeft(oCreature, (int)nClassType, (int)nSpellId, (int)nMetaMagic, nDomainLevel);
        }

        /// <summary>
        /// Gets the spell level at which a class gets a spell.
        /// - nClassType: a CLASS_TYPE_* constant.
        /// - nSpellId: a SPELL_* constant.
        /// Returns: the spell level or -1 if the class does not get the spell.
        /// </summary>
        public static int GetSpellLevelByClass(ClassType nClassType, Spell nSpellId)
        {
            return global::NWN.Core.NWScript.GetSpellLevelByClass((int)nClassType, (int)nSpellId);
        }
    }
}