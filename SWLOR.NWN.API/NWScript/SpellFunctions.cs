using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Determines whether the object has any effects applied by the spell.
        /// The spell id on effects is only valid if the effect is created
        /// when the spell script runs. If it is created in a delayed command
        /// then the spell id on the effect will be invalid.
        /// </summary>
        /// <param name="nSpell">SPELL_* constant</param>
        /// <param name="oObject">The object to check (defaults to OBJECT_INVALID)</param>
        /// <returns>TRUE if the object has effects from the spell</returns>
        public static bool GetHasSpellEffect(Spell nSpell, uint oObject = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetHasSpellEffect((int)nSpell, oObject) != 0;
        }

        /// <summary>
        /// Gets the spell that applied the spell effect.
        /// </summary>
        /// <param name="eSpellEffect">The spell effect to check</param>
        /// <returns>The spell ID (SPELL_*), or -1 if the effect was applied outside a spell script</returns>
        public static int GetEffectSpellId(Effect eSpellEffect)
        {
            return global::NWN.Core.NWScript.GetEffectSpellId(eSpellEffect);
        }

        /// <summary>
        /// Use this in spell scripts to get damage adjusted by the target's reflex and
        /// evasion saves.
        /// </summary>
        /// <param name="nDamage">The base damage</param>
        /// <param name="oTarget">The target to check saves for</param>
        /// <param name="nDC">Difficulty check</param>
        /// <param name="nSaveType">SAVING_THROW_TYPE_* constant</param>
        /// <param name="oSaveVersus">The object to save versus (defaults to OBJECT_INVALID)</param>
        /// <returns>The adjusted damage after saves</returns>
        public static int GetReflexAdjustedDamage(int nDamage, uint oTarget, int nDC,
            SavingThrowType nSaveType = SavingThrowType.All, uint oSaveVersus = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetReflexAdjustedDamage(nDamage, oTarget, nDC, (int)nSaveType, oSaveVersus);
        }

        /// <summary>
        /// Gets the object at which the caller last cast a spell.
        /// </summary>
        /// <returns>The target object, or OBJECT_INVALID on error</returns>
        public static uint GetSpellTargetObject()
        {
            return global::NWN.Core.NWScript.GetSpellTargetObject();
        }

        /// <summary>
        /// Gets the metamagic type of the last spell cast by the caller.
        /// </summary>
        /// <returns>The metamagic type (METAMAGIC_*), or -1 if the caster is not a valid object</returns>
        public static int GetMetaMagicFeat()
        {
            return global::NWN.Core.NWScript.GetMetaMagicFeat();
        }

        /// <summary>
        /// Gets the DC to save against for a spell (10 + spell level + relevant ability bonus).
        /// This can be called by a creature or by an Area of Effect object.
        /// </summary>
        /// <returns>The spell save DC</returns>
        public static int GetSpellSaveDC()
        {
            return global::NWN.Core.NWScript.GetSpellSaveDC();
        }

        /// <summary>
        /// Gets the location of the caller's last spell target.
        /// </summary>
        /// <returns>The location of the last spell target</returns>
        public static Location GetSpellTargetLocation()
        {
            return global::NWN.Core.NWScript.GetSpellTargetLocation();
        }

        /// <summary>
        /// Casts a spell at the target location.
        /// If bCheat is TRUE, then the executor of the action doesn't have to be
        /// able to cast the spell.
        /// If bInstantSpell is TRUE, the spell is cast immediately; this allows
        /// the end-user to simulate a high-level magic user having lots of advance warning of impending trouble.
        /// </summary>
        /// <param name="nSpell">SPELL_* constant</param>
        /// <param name="lTargetLocation">The target location to cast the spell at</param>
        /// <param name="nMetaMagic">METAMAGIC_* constant</param>
        /// <param name="bCheat">If TRUE, the executor doesn't have to be able to cast the spell</param>
        /// <param name="nProjectilePathType">PROJECTILE_PATH_TYPE_* constant</param>
        /// <param name="bInstantSpell">If TRUE, the spell is cast immediately</param>
        public static void ActionCastSpellAtLocation(Spell nSpell, Location lTargetLocation,
            MetaMagic nMetaMagic = MetaMagic.Any, bool bCheat = false,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default, bool bInstantSpell = false)
        {
            global::NWN.Core.NWScript.ActionCastSpellAtLocation((int)nSpell, lTargetLocation, (int)nMetaMagic, bCheat ? 1 : 0, (int)nProjectilePathType, bInstantSpell ? 1 : 0);
        }

        /// <summary>
        /// Creates an event which triggers the "SpellCastAt" script.
        /// Note: This only creates the event. The event won't actually trigger until SignalEvent()
        /// is called using this created SpellCastAt event as an argument.
        /// For example:
        /// SignalEvent(oCreature, EventSpellCastAt(oCaster, SPELL_MAGIC_MISSILE, TRUE));
        /// This function doesn't cast the spell specified, it only creates an event so that
        /// when the event is signaled on an object, the object will use its OnSpellCastAt script
        /// to react to the spell being cast.
        /// To specify the OnSpellCastAt script that should run, view the Object's Properties
        /// and click on the Scripts Tab. Then specify a script for the OnSpellCastAt event.
        /// From inside the OnSpellCastAt script call:
        /// GetLastSpellCaster() to get the object that cast the spell (oCaster).
        /// GetLastSpell() to get the type of spell cast (nSpell)
        /// GetLastSpellHarmful() to determine if the spell cast at the object was harmful.
        /// </summary>
        /// <param name="oCaster">The object that cast the spell</param>
        /// <param name="nSpell">The spell that was cast (SPELL_*)</param>
        /// <param name="bHarmful">Whether the spell is harmful</param>
        /// <returns>The SpellCastAt event</returns>
        public static Event EventSpellCastAt(uint oCaster, Spell nSpell, bool bHarmful = true)
        {
            return global::NWN.Core.NWScript.EventSpellCastAt(oCaster, (int)nSpell, bHarmful ? 1 : 0);
        }

        /// <summary>
        /// This is for use in a "Spell Cast" script, it gets who cast the spell.
        /// The spell could have been cast by a creature, placeable or door.
        /// </summary>
        /// <returns>The object that cast the spell, or OBJECT_INVALID if the caller is not a creature, placeable or door</returns>
        public static uint GetLastSpellCaster()
        {
            return global::NWN.Core.NWScript.GetLastSpellCaster();
        }

        /// <summary>
        /// This is for use in a "Spell Cast" script, it gets the ID of the spell that was cast.
        /// </summary>
        /// <returns>The ID of the spell that was cast</returns>
        public static int GetLastSpell()
        {
            return global::NWN.Core.NWScript.GetLastSpell();
        }

        /// <summary>
        /// This is for use in a Spell script, it gets the ID of the spell that is being cast.
        /// </summary>
        /// <returns>The ID of the spell being cast (SPELL_*)</returns>
        public static int GetSpellId()
        {
            return global::NWN.Core.NWScript.GetSpellId();
        }

        /// <summary>
        /// Use this in a SpellCast script to determine whether the spell was considered harmful.
        /// </summary>
        /// <returns>TRUE if the last spell cast was harmful</returns>
        public static bool GetLastSpellHarmful()
        {
            return global::NWN.Core.NWScript.GetLastSpellHarmful() != 0;
        }

        /// <summary>
        /// The action subject will fake casting a spell at the target; the conjure and cast
        /// animations and visuals will occur, nothing else.
        /// </summary>
        /// <param name="nSpell">The spell to fake cast</param>
        /// <param name="oTarget">The target to fake cast at</param>
        /// <param name="nProjectilePathType">PROJECTILE_PATH_TYPE_* constant</param>
        public static void ActionCastFakeSpellAtObject(Spell nSpell, uint oTarget,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default)
        {
            global::NWN.Core.NWScript.ActionCastFakeSpellAtObject((int)nSpell, oTarget, (int)nProjectilePathType);
        }

        /// <summary>
        /// The action subject will fake casting a spell at the location; the conjure and
        /// cast animations and visuals will occur, nothing else.
        /// </summary>
        /// <param name="nSpell">The spell to fake cast</param>
        /// <param name="lTarget">The target location to fake cast at</param>
        /// <param name="nProjectilePathType">PROJECTILE_PATH_TYPE_* constant</param>
        public static void ActionCastFakeSpellAtLocation(Spell nSpell, Location lTarget,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default)
        {
            global::NWN.Core.NWScript.ActionCastFakeSpellAtLocation((int)nSpell, lTarget, (int)nProjectilePathType);
        }

        /// <summary>
        /// Counterspells the target.
        /// </summary>
        /// <param name="oCounterSpellTarget">The target to counterspell</param>
        public static void ActionCounterSpell(uint oCounterSpellTarget)
        {
            global::NWN.Core.NWScript.ActionCounterSpell(oCounterSpellTarget);
        }

        /// <summary>
        /// Gets the target at which the caller attempted to cast a spell.
        /// This value is set every time a spell is cast and is reset at the end of combat.
        /// </summary>
        /// <returns>The attempted spell target, or OBJECT_INVALID if the caller is not a valid creature</returns>
        public static uint GetAttemptedSpellTarget()
        {
            return global::NWN.Core.NWScript.GetAttemptedSpellTarget();
        }

        /// <summary>
        /// In the spell script returns the feat used, or -1 if no feat was used.
        /// </summary>
        /// <returns>The feat ID used, or -1 if no feat was used</returns>
        public static int GetSpellFeatId()
        {
            return global::NWN.Core.NWScript.GetSpellFeatId();
        }

        /// <summary>
        /// Returns TRUE if the last spell was cast spontaneously.
        /// e.g. a Cleric casting SPELL_CURE_LIGHT_WOUNDS when it is not prepared, using another level 1 slot.
        /// </summary>
        /// <returns>TRUE if the last spell was cast spontaneously</returns>
        public static bool GetSpellCastSpontaneously()
        {
            return global::NWN.Core.NWScript.GetSpellCastSpontaneously() != 0;
        }

        /// <summary>
        /// Returns the level of the last spell cast. This value is only valid in a Spell script.
        /// </summary>
        /// <returns>The level of the last spell cast</returns>
        public static int GetLastSpellLevel()
        {
            return global::NWN.Core.NWScript.GetLastSpellLevel();
        }

        /// <summary>
        /// Gets the number of memorized spell slots for a given spell level.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <returns>The number of spell slots</returns>
        public static int GetMemorizedSpellCountByLevel(uint oCreature, ClassType nClassType, int nSpellLevel)
        {
            return global::NWN.Core.NWScript.GetMemorizedSpellCountByLevel(oCreature, (int)nClassType, nSpellLevel);
        }

        /// <summary>
        /// Gets the spell ID of a memorized spell slot.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <param name="nIndex">The index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()</param>
        /// <returns>A SPELL_* constant or -1 if the slot is not set</returns>
        public static int GetMemorizedSpellId(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            return global::NWN.Core.NWScript.GetMemorizedSpellId(oCreature, (int)nClassType, nSpellLevel, nIndex);
        }

        /// <summary>
        /// Gets the ready state of a memorized spell slot.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <param name="nIndex">The index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()</param>
        /// <returns>TRUE/FALSE or -1 if the slot is not set</returns>
        public static int GetMemorizedSpellReady(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            return global::NWN.Core.NWScript.GetMemorizedSpellReady(oCreature, (int)nClassType, nSpellLevel, nIndex);
        }

        /// <summary>
        /// Gets the metamagic of a memorized spell slot.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <param name="nIndex">The index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()</param>
        /// <returns>A METAMAGIC_* constant or -1 if the slot is not set</returns>
        public static int GetMemorizedSpellMetaMagic(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            return global::NWN.Core.NWScript.GetMemorizedSpellMetaMagic(oCreature, (int)nClassType, nSpellLevel, nIndex);
        }

        /// <summary>
        /// Gets if the memorized spell slot has a domain spell.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <param name="nIndex">The index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()</param>
        /// <returns>TRUE/FALSE or -1 if the slot is not set</returns>
        public static int GetMemorizedSpellIsDomainSpell(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            return global::NWN.Core.NWScript.GetMemorizedSpellIsDomainSpell(oCreature, (int)nClassType, nSpellLevel, nIndex);
        }

        /// <summary>
        /// Sets a memorized spell slot.
        /// </summary>
        /// <param name="oCreature">The creature to set the spell for</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <param name="nIndex">The index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()</param>
        /// <param name="nSpellId">A SPELL_* constant</param>
        /// <param name="bReady">TRUE to mark the slot ready</param>
        /// <param name="nMetaMagic">A METAMAGIC_* constant</param>
        /// <param name="bIsDomainSpell">TRUE for a domain spell</param>
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
        /// Sets the ready state of a memorized spell slot.
        /// </summary>
        /// <param name="oCreature">The creature to set the spell for</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <param name="nIndex">The index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()</param>
        /// <param name="bReady">TRUE to mark the slot ready</param>
        public static void SetMemorizedSpellReady(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex, bool bReady)
        {
            global::NWN.Core.NWScript.SetMemorizedSpellReady(oCreature, (int)nClassType, nSpellLevel, nIndex, bReady ? 1 : 0);
        }

        /// <summary>
        /// Clears a specific memorized spell slot.
        /// </summary>
        /// <param name="oCreature">The creature to clear the spell for</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <param name="nIndex">The index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()</param>
        public static void ClearMemorizedSpell(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            global::NWN.Core.NWScript.ClearMemorizedSpell(oCreature, (int)nClassType, nSpellLevel, nIndex);
        }

        /// <summary>
        /// Clears all memorized spell slots of a specific spell ID, including metamagic'd ones.
        /// </summary>
        /// <param name="oCreature">The creature to clear the spells for</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellId">A SPELL_* constant</param>
        public static void ClearMemorizedSpellBySpellId(uint oCreature, ClassType nClassType, int nSpellId)
        {
            global::NWN.Core.NWScript.ClearMemorizedSpellBySpellId(oCreature, (int)nClassType, nSpellId);
        }

        /// <summary>
        /// Gets the number of known spells for a given spell level.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a SpellBookRestricted class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <returns>The number of known spells</returns>
        public static int GetKnownSpellCount(uint oCreature, ClassType nClassType, int nSpellLevel)
        {
            return global::NWN.Core.NWScript.GetKnownSpellCount(oCreature, (int)nClassType, nSpellLevel);
        }

        /// <summary>
        /// Gets the spell ID of a known spell.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a SpellBookRestricted class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <param name="nIndex">The index of the known spell. Bounds: 0 <= nIndex < GetKnownSpellCount()</param>
        /// <returns>A SPELL_* constant or -1 on error</returns>
        public static int GetKnownSpellId(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            return global::NWN.Core.NWScript.GetKnownSpellId(oCreature, (int)nClassType, nSpellLevel, nIndex);
        }

        /// <summary>
        /// Gets if a spell is in the known spell list.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a SpellBookRestricted class</param>
        /// <param name="nSpellId">A SPELL_* constant</param>
        /// <returns>TRUE if the spell is in the known spell list</returns>
        public static bool GetIsInKnownSpellList(uint oCreature, ClassType nClassType, Spell nSpellId)
        {
            return global::NWN.Core.NWScript.GetIsInKnownSpellList(oCreature, (int)nClassType, (int)nSpellId) != 0;
        }

        /// <summary>
        /// Gets the amount of uses a spell has left.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant</param>
        /// <param name="nSpellId">A SPELL_* constant</param>
        /// <param name="nMetaMagic">A METAMAGIC_* constant</param>
        /// <param name="nDomainLevel">The domain level, if a domain spell</param>
        /// <returns>The amount of spell uses left</returns>
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
        /// </summary>
        /// <param name="nClassType">A CLASS_TYPE_* constant</param>
        /// <param name="nSpellId">A SPELL_* constant</param>
        /// <returns>The spell level or -1 if the class does not get the spell</returns>
        public static int GetSpellLevelByClass(ClassType nClassType, Spell nSpellId)
        {
            return global::NWN.Core.NWScript.GetSpellLevelByClass((int)nClassType, (int)nSpellId);
        }
    }
}