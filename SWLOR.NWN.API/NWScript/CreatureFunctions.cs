using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   returns the footstep type of the creature specified.
        ///   The footstep type determines what the creature's footsteps sound
        ///   like when ever they take a step.
        ///   returns FOOTSTEP_TYPE_INVALID if used on a non-creature object, or if
        ///   used on creature that has no footstep sounds by default (e.g. Will-O'-Wisp).
        /// </summary>
        public static FootstepType GetFootstepType(uint oCreature = OBJECT_INVALID)
        {
            return (FootstepType)global::NWN.Core.NWScript.GetFootstepType(oCreature);
        }

        /// <summary>
        ///   Sets the footstep type of the creature specified.
        ///   Changing a creature's footstep type will change the sound that
        ///   its feet make when ever the creature makes takes a step.
        ///   By default a creature's footsteps are detemined by the appearance
        ///   type of the creature. SetFootstepType() allows you to make a
        ///   creature use a difference footstep type than it would use by default
        ///   for its given appearance.
        ///   - nFootstepType (FOOTSTEP_TYPE_*):
        ///   FOOTSTEP_TYPE_NORMAL
        ///   FOOTSTEP_TYPE_LARGE
        ///   FOOTSTEP_TYPE_DRAGON
        ///   FOOTSTEP_TYPE_SoFT
        ///   FOOTSTEP_TYPE_HOOF
        ///   FOOTSTEP_TYPE_HOOF_LARGE
        ///   FOOTSTEP_TYPE_BEETLE
        ///   FOOTSTEP_TYPE_SPIDER
        ///   FOOTSTEP_TYPE_SKELETON
        ///   FOOTSTEP_TYPE_LEATHER_WING
        ///   FOOTSTEP_TYPE_FEATHER_WING
        ///   FOOTSTEP_TYPE_DEFAULT - Makes the creature use its original default footstep sounds.
        ///   FOOTSTEP_TYPE_NONE
        ///   - oCreature: the creature to change the footstep sound for.
        /// </summary>
        public static void SetFootstepType(FootstepType nFootstepType, uint oCreature = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetFootstepType((int)nFootstepType, oCreature);
        }

        /// <summary>
        ///   returns the Wing type of the creature specified.
        ///   CREATURE_WING_TYPE_NONE
        ///   CREATURE_WING_TYPE_DEMON
        ///   CREATURE_WING_TYPE_ANGEL
        ///   CREATURE_WING_TYPE_BAT
        ///   CREATURE_WING_TYPE_DRAGON
        ///   CREATURE_WING_TYPE_BUTTERFLY
        ///   CREATURE_WING_TYPE_BIRD
        ///   returns CREATURE_WING_TYPE_NONE if used on a non-creature object,
        ///   if the creature has no wings, or if the creature can not have its
        ///   wing type changed in the toolset.
        /// </summary>
        public static WingType GetCreatureWingType(uint oCreature = OBJECT_INVALID)
        {
            return (WingType)global::NWN.Core.NWScript.GetCreatureWingType(oCreature);
        }

        /// <summary>
        ///   Sets the Wing type of the creature specified.
        ///   - nWingType (CREATURE_WING_TYPE_*)
        ///   CREATURE_WING_TYPE_NONE
        ///   CREATURE_WING_TYPE_DEMON
        ///   CREATURE_WING_TYPE_ANGEL
        ///   CREATURE_WING_TYPE_BAT
        ///   CREATURE_WING_TYPE_DRAGON
        ///   CREATURE_WING_TYPE_BUTTERFLY
        ///   CREATURE_WING_TYPE_BIRD
        ///   - oCreature: the creature to change the wing type for.
        ///   Note: Only two creature model types will support wings.
        ///   The MODELTYPE for the part based (playable races) 'P'
        ///   and MODELTYPE 'W'in the appearance.2da
        /// </summary>
        public static void SetCreatureWingType(WingType nWingType, uint oCreature = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCreatureWingType((int)nWingType, oCreature);
        }

        /// <summary>
        ///   returns the model number being used for the body part and creature specified
        ///   The model number returned is for the body part when the creature is not wearing
        ///   armor (i.e. whether or not the creature is wearing armor does not affect
        ///   the return value).
        ///   Note: Only works on part based creatures, which is typically restricted to
        ///   the playable races (unless some new part based custom content has been
        ///   added to the module).
        ///   returns CREATURE_PART_INVALID if used on a non-creature object,
        ///   or if the creature does not use a part based model.
        ///   - nPart (CREATURE_PART_*)
        ///   CREATURE_PART_RIGHT_FOOT
        ///   CREATURE_PART_LEFT_FOOT
        ///   CREATURE_PART_RIGHT_SHIN
        ///   CREATURE_PART_LEFT_SHIN
        ///   CREATURE_PART_RIGHT_THIGH
        ///   CREATURE_PART_LEFT_THIGH
        ///   CREATURE_PART_PELVIS
        ///   CREATURE_PART_TORSO
        ///   CREATURE_PART_BELT
        ///   CREATURE_PART_NECK
        ///   CREATURE_PART_RIGHT_FOREARM
        ///   CREATURE_PART_LEFT_FOREARM
        ///   CREATURE_PART_RIGHT_BICEP
        ///   CREATURE_PART_LEFT_BICEP
        ///   CREATURE_PART_RIGHT_SHOULDER
        ///   CREATURE_PART_LEFT_SHOULDER
        ///   CREATURE_PART_RIGHT_HAND
        ///   CREATURE_PART_LEFT_HAND
        ///   CREATURE_PART_HEAD
        /// </summary>
        public static int GetCreatureBodyPart(CreaturePart nPart, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCreatureBodyPart((int)nPart, oCreature);
        }

        /// <summary>
        ///   Sets the body part model to be used on the creature specified.
        ///   The model names for parts need to be in the following format:
        ///   p
        ///   <m/ f>
        ///     <race letter>
        ///       <phenotype>
        ///         _
        ///         <body part>
        ///           <model number>
        ///             .mdl
        ///             - nPart (CREATURE_PART_*)
        ///             CREATURE_PART_RIGHT_FOOT
        ///             CREATURE_PART_LEFT_FOOT
        ///             CREATURE_PART_RIGHT_SHIN
        ///             CREATURE_PART_LEFT_SHIN
        ///             CREATURE_PART_RIGHT_THIGH
        ///             CREATURE_PART_LEFT_THIGH
        ///             CREATURE_PART_PELVIS
        ///             CREATURE_PART_TORSO
        ///             CREATURE_PART_BELT
        ///             CREATURE_PART_NECK
        ///             CREATURE_PART_RIGHT_FOREARM
        ///             CREATURE_PART_LEFT_FOREARM
        ///             CREATURE_PART_RIGHT_BICEP
        ///             CREATURE_PART_LEFT_BICEP
        ///             CREATURE_PART_RIGHT_SHOULDER
        ///             CREATURE_PART_LEFT_SHOULDER
        ///             CREATURE_PART_RIGHT_HAND
        ///             CREATURE_PART_LEFT_HAND
        ///             CREATURE_PART_HEAD
        ///             - nModelNumber: CREATURE_MODEL_TYPE_*
        ///             CREATURE_MODEL_TYPE_NONE
        ///             CREATURE_MODEL_TYPE_SKIN (not for use on shoulders, pelvis or head).
        ///             CREATURE_MODEL_TYPE_TATTOO (for body parts that support tattoos, i.e. not heads/feet/hands).
        ///             CREATURE_MODEL_TYPE_UNDEAD (undead model only exists for the right arm parts).
        ///             - oCreature: the creature to change the body part for.
        ///             Note: Only part based creature appearance types are supported.
        ///             i.e. The model types for the playable races ('P') in the appearance.2da
        /// </summary>
        public static void SetCreatureBodyPart(CreaturePart nPart, int nModelNumber, uint oCreature = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCreatureBodyPart((int)nPart, nModelNumber, oCreature);
        }

        /// <summary>
        ///   returns the Tail type of the creature specified.
        ///   CREATURE_TAIL_TYPE_NONE
        ///   CREATURE_TAIL_TYPE_LIZARD
        ///   CREATURE_TAIL_TYPE_BONE
        ///   CREATURE_TAIL_TYPE_DEVIL
        ///   returns CREATURE_TAIL_TYPE_NONE if used on a non-creature object,
        ///   if the creature has no Tail, or if the creature can not have its
        ///   Tail type changed in the toolset.
        /// </summary>
        public static TailType GetCreatureTailType(uint oCreature = OBJECT_INVALID)
        {
            return (TailType)global::NWN.Core.NWScript.GetCreatureTailType(oCreature);
        }

        /// <summary>
        ///   Sets the Tail type of the creature specified.
        ///   - nTailType (CREATURE_TAIL_TYPE_*)
        ///   CREATURE_TAIL_TYPE_NONE
        ///   CREATURE_TAIL_TYPE_LIZARD
        ///   CREATURE_TAIL_TYPE_BONE
        ///   CREATURE_TAIL_TYPE_DEVIL
        ///   - oCreature: the creature to change the Tail type for.
        ///   Note: Only two creature model types will support Tails.
        ///   The MODELTYPE for the part based (playable) races 'P'
        ///   and MODELTYPE 'T'in the appearance.2da
        /// </summary>
        public static void SetCreatureTailType(TailType nTailType, uint oCreature = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCreatureTailType((int)nTailType, oCreature);
        }

        /// <summary>
        ///   Returns the creature's currently set PhenoType (body type).
        /// </summary>
        public static PhenoType GetPhenoType(uint oCreature)
        {
            return (PhenoType)global::NWN.Core.NWScript.GetPhenoType(oCreature);
        }

        /// <summary>
        ///   Sets the creature's PhenoType (body type) to the type specified.
        ///   nPhenoType = PHENOTYPE_NORMAL
        ///   nPhenoType = PHENOTYPE_BIG
        ///   nPhenoType = PHENOTYPE_CUSTOM* - The custom PhenoTypes should only ever
        ///   be used if you have specifically created your own custom content that
        ///   requires the use of a new PhenoType and you have specified the appropriate
        ///   custom PhenoType in your custom content.
        ///   SetPhenoType will only work on part based creature (i.e. the starting
        ///   default playable races).
        /// </summary>
        public static void SetPhenoType(PhenoType nPhenoType, uint oCreature = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetPhenoType((int)nPhenoType, oCreature);
        }

        /// <summary>
        ///   Is this creature able to be disarmed? (checks disarm flag on creature, and if
        ///   the creature actually has a weapon equipped in their right hand that is droppable)
        /// </summary>
        public static bool GetIsCreatureDisarmable(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetIsCreatureDisarmable(oCreature) != 0;
        }

        /// <summary>
        ///   Returns the class that the spellcaster cast the
        ///   spell as.
        ///   - Returns CLASS_TYPE_INVALID if the caster has
        ///   no valid class (placeables, etc...)
        /// </summary>
        public static ClassType GetLastSpellCastClass()
        {
            return (ClassType)global::NWN.Core.NWScript.GetLastSpellCastClass();
        }

        /// <summary>
        ///   Sets the number of base attacks for the specified
        ///   creatures. The range of values accepted are from
        ///   1 to 6
        ///   Note: This function does not work on Player Characters
        /// </summary>
        public static void SetBaseAttackBonus(int nBaseAttackBonus, uint oCreature = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetBaseAttackBonus(nBaseAttackBonus, oCreature);
        }

        /// <summary>
        ///   Restores the number of base attacks back to it's
        ///   original state.
        /// </summary>
        public static void RestoreBaseAttackBonus(uint oCreature = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.RestoreBaseAttackBonus(oCreature);
        }


        /// <summary>
        ///   Sets the creature's appearance type to the value specified (uses the APPEARANCE_TYPE_XXX constants)
        /// </summary>
        public static void SetCreatureAppearanceType(uint oCreature, AppearanceType nAppearanceType)
        {
            global::NWN.Core.NWScript.SetCreatureAppearanceType(oCreature, (int)nAppearanceType);
        }

        /// <summary>
        ///   Returns the default package selected for this creature to level up with
        ///   - returns PACKAGE_INVALID if error occurs
        /// </summary>
        public static int GetCreatureStartingPackage(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetCreatureStartingPackage(oCreature);
        }

        /// <summary>
        ///   Returns the spell resistance of the specified creature.
        ///   - Returns 0 if the creature has no spell resistance or an invalid
        ///   creature is passed in.
        /// </summary>
        public static int GetSpellResistance(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetSpellResistance(oCreature);
        }

        /// <summary>
        ///   Sets the lootable state of a *living* NPC creature.
        ///   This function will *not* work on players or dead creatures.
        /// </summary>
        public static void SetLootable(uint oCreature, bool bLootable)
        {
            global::NWN.Core.NWScript.SetLootable(oCreature, bLootable ? 1 : 0);
        }

        /// <summary>
        ///   Returns the lootable state of a creature.
        /// </summary>
        public static bool GetLootable(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetLootable(oCreature) != 0;
        }

        /// <summary>
        ///   Gets the status of ACTION_MODE_* modes on a creature.
        /// </summary>
        public static bool GetActionMode(uint oCreature, ActionMode nMode)
        {
            return global::NWN.Core.NWScript.GetActionMode(oCreature, (int)nMode) == 1;
        }

        /// <summary>
        ///   Sets the status of modes ACTION_MODE_* on a creature.
        /// </summary>
        public static void SetActionMode(uint oCreature, ActionMode nMode, bool nStatus)
        {
            global::NWN.Core.NWScript.SetActionMode(oCreature, (int)nMode, nStatus ? 1 : 0);
        }

        /// <summary>
        ///   Returns the current arcane spell failure factor of a creature
        /// </summary>
        public static int GetArcaneSpellFailure(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetArcaneSpellFailure(oCreature);
        }

        /// <summary>
        ///   Set the name of oCreature's sub race to sSubRace.
        /// </summary>
        public static void SetSubRace(uint oCreature, string sSubRace)
        {
            global::NWN.Core.NWScript.SetSubRace(oCreature, sSubRace);
        }

        /// <summary>
        ///   Set the name of oCreature's Deity to sDeity.
        /// </summary>
        public static void SetDeity(uint oCreature, string sDeity)
        {
            global::NWN.Core.NWScript.SetDeity(oCreature, sDeity);
        }

        /// <summary>
        ///   Returns TRUE if the creature oCreature is currently possessed by a DM character.
        ///   Returns FALSE otherwise.
        ///   Note: GetIsDMPossessed() will return FALSE if oCreature is the DM character.
        ///   To determine if oCreature is a DM character use GetIsDM()
        /// </summary>
        public static bool GetIsDMPossessed(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetIsDMPossessed(oCreature) != 0;
        }

        /// <summary>
        ///   Increment the remaining uses per day for this creature by one.
        ///   Total number of feats per day can not exceed the maximum.
        ///   - oCreature: creature to modify
        ///   - nFeat: constant FEAT_*
        /// </summary>
        public static void IncrementRemainingFeatUses(uint oCreature, FeatType nFeat)
        {
            global::NWN.Core.NWScript.IncrementRemainingFeatUses(oCreature, (int)nFeat);
        }

        /// <summary>
        ///   Gets the current AI Level that the creature is running at.
        ///   Returns one of the following:
        ///   AI_LEVEL_INVALID, AI_LEVEL_VERY_LOW, AI_LEVEL_LOW, AI_LEVEL_NORMAL, AI_LEVEL_HIGH, AI_LEVEL_VERY_HIGH
        /// </summary>
        public static AILevel GetAILevel(uint oTarget = OBJECT_INVALID)
        {
            return (AILevel)global::NWN.Core.NWScript.GetAILevel(oTarget);
        }

        /// <summary>
        ///   Sets the current AI Level of the creature to the value specified. Does not work on Players.
        ///   The game by default will choose an appropriate AI level for
        ///   creatures based on the circumstances that the creature is in.
        ///   Explicitly setting an AI level will over ride the game AI settings.
        ///   The new setting will last until SetAILevel is called again with the argument AI_LEVEL_DEFAULT.
        ///   AI_LEVEL_DEFAULT  - Default setting. The game will take over seting the appropriate AI level when required.
        ///   AI_LEVEL_VERY_LOW - Very Low priority, very stupid, but low CPU usage for AI. Typically used when no players are in
        ///   the area.
        ///   AI_LEVEL_LOW      - Low priority, mildly stupid, but slightly more CPU usage for AI. Typically used when not in
        ///   combat, but a player is in the area.
        ///   AI_LEVEL_NORMAL   - Normal priority, average AI, but more CPU usage required for AI. Typically used when creature is
        ///   in combat.
        ///   AI_LEVEL_HIGH     - High priority, smartest AI, but extremely high CPU usage required for AI. Avoid using this. It is
        ///   most likely only ever needed for cutscenes.
        /// </summary>
        public static void SetAILevel(uint oTarget, AILevel nAILevel)
        {
            global::NWN.Core.NWScript.SetAILevel(oTarget, (int)nAILevel);
        }

        /// <summary>
        ///   This will return TRUE if the creature running the script is a familiar currently
        ///   possessed by his master.
        ///   returns FALSE if not or if the creature object is invalid
        /// </summary>
        public static bool GetIsPossessedFamiliar(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetIsPossessedFamiliar(oCreature) == 1;
        }

        /// <summary>
        ///   This will cause a Player Creature to unpossess his/her familiar.  It will work if run
        ///   on the player creature or the possessed familiar.  It does not work in conjunction with
        ///   any DM possession.
        /// </summary>
        public static void UnpossessFamiliar(uint oCreature)
        {
            global::NWN.Core.NWScript.UnpossessFamiliar(oCreature);
        }

        /// <summary>
        ///   Get the immortal flag on a creature
        /// </summary>
        public static bool GetImmortal(uint oTarget = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetImmortal(oTarget) != 0;
        }

        /// <summary>
        ///   Does a single attack on every hostile creature within 10ft. of the attacker
        ///   and determines damage accordingly.  If the attacker has a ranged weapon
        ///   equipped, this will have no effect.
        ///   ** NOTE ** This is meant to be called inside the spell script for whirlwind
        ///   attack, it is not meant to be used to queue up a new whirlwind attack.  To do
        ///   that you need to call ActionUseFeat(FEAT_WHIRLWIND_ATTACK, oEnemy)
        ///   - int bDisplayFeedback: TRUE or FALSE, whether or not feedback should be
        ///   displayed
        ///   - int bImproved: If TRUE, the improved version of whirlwind is used
        /// </summary>
        public static void DoWhirlwindAttack(bool bDisplayFeedback = true, bool bImproved = false)
        {
            global::NWN.Core.NWScript.DoWhirlwindAttack(bDisplayFeedback ? 1 : 0, bImproved ? 1 : 0);
        }

        /// <summary>
        ///   Returns the base attack bonus for the given creature.
        /// </summary>
        public static int GetBaseAttackBonus(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetBaseAttackBonus(oCreature);
        }

        /// <summary>
        ///   Set a creature's immortality flag.
        ///   -oCreature: creature affected
        ///   -bImmortal: TRUE = creature is immortal and cannot be killed (but still takes damage)
        ///   FALSE = creature is not immortal and is damaged normally.
        ///   This scripting command only works on Creature objects.
        /// </summary>
        public static void SetImmortal(uint oCreature, bool bImmortal)
        {
            global::NWN.Core.NWScript.SetImmortal(oCreature, bImmortal ? 1 : 0);
        }

        /// <summary>
        ///   Returns true if 1d20 roll + skill rank is greater than or equal to difficulty
        ///   - oTarget: the creature using the skill
        ///   - nSkill: the skill being used
        ///   - nDifficulty: Difficulty class of skill
        /// </summary>
        public static bool GetIsSkillSuccessful(uint oTarget, NWNSkillType nSkill, int nDifficulty)
        {
            return global::NWN.Core.NWScript.GetIsSkillSuccessful(oTarget, (int)nSkill, nDifficulty) != 0;
        }

        /// <summary>
        ///   Decrement the remaining uses per day for this creature by one.
        ///   - oCreature: creature to modify
        ///   - nFeat: constant FEAT_*
        /// </summary>
        public static void DecrementRemainingFeatUses(uint oCreature, int nFeat)
        {
            global::NWN.Core.NWScript.DecrementRemainingFeatUses(oCreature, nFeat);
        }

        /// <summary>
        ///   Decrement the remaining uses per day for this creature by one.
        ///   - oCreature: creature to modify
        ///   - nSpell: constant SPELL_*
        /// </summary>
        public static void DecrementRemainingSpellUses(uint oCreature, int nSpell)
        {
            global::NWN.Core.NWScript.DecrementRemainingSpellUses(oCreature, nSpell);
        }

        /// <summary>
        ///   Returns the stealth mode of the specified creature.
        ///   - oCreature
        ///   * Returns a constant STEALTH_MODE_*
        /// </summary>
        public static StealthMode GetStealthMode(uint oCreature)
        {
            return (StealthMode)global::NWN.Core.NWScript.GetStealthMode(oCreature);
        }

        /// <summary>
        ///   Returns the detection mode of the specified creature.
        ///   - oCreature
        ///   * Returns a constant DETECT_MODE_*
        /// </summary>
        public static DetectMode GetDetectMode(uint oCreature)
        {
            return (DetectMode)global::NWN.Core.NWScript.GetDetectMode(oCreature);
        }

        /// <summary>
        ///   Returns the defensive casting mode of the specified creature.
        ///   - oCreature
        ///   * Returns a constant DEFENSIVE_CASTING_MODE_*
        /// </summary>
        public static CastingMode GetDefensiveCastingMode(uint oCreature)
        {
            return (CastingMode)global::NWN.Core.NWScript.GetDefensiveCastingMode(oCreature);
        }

        /// <summary>
        ///   returns the appearance type of the specified creature.
        ///   * returns a constant APPEARANCE_TYPE_* for valid creatures
        ///   * returns APPEARANCE_TYPE_INVALID for non creatures/invalid creatures
        /// </summary>
        public static AppearanceType GetAppearanceType(uint oCreature)
        {
            return (AppearanceType)global::NWN.Core.NWScript.GetAppearanceType(oCreature);
        }

        /// <summary>
        ///   Get the last object that was sent as a GetLastAttacker(), GetLastDamager(),
        ///   GetLastSpellCaster() (for a hostile spell), or GetLastDisturbed() (when a
        ///   creature is pickpocketed).
        ///   Note: Return values may only ever be:
        ///   1) A Creature
        ///   2) Plot Characters will never have this value set
        ///   3) Area of Effect Objects will return the AOE creator if they are registered
        ///   as this value, otherwise they will return INVALID_OBJECT_ID
        ///   4) Traps will not return the creature that set the trap.
        ///   5) This value will never be overwritten by another non-creature object.
        ///   6) This value will never be a dead/destroyed creature
        /// </summary>
        public static uint GetLastHostileActor(uint oVictim = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetLastHostileActor(oVictim);
        }

        /// <summary>
        ///   Get the number of Hitdice worth of Turn Resistance that oUndead may have.
        ///   This will only work on undead creatures.
        /// </summary>
        public static int GetTurnResistanceHD(uint oUndead = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetTurnResistanceHD(oUndead);
        }

        /// <summary>
        ///   Get the size (CREATURE_SIZE_*) of oCreature.
        /// </summary>
        public static CreatureSize GetCreatureSize(uint oCreature)
        {
            return (CreatureSize)global::NWN.Core.NWScript.GetCreatureSize(oCreature);
        }

        /// <summary>
        ///   Use this on an NPC to cause all creatures within a 10-metre radius to stop
        ///   what they are doing and sets the NPC's enemies within this range to be
        ///   neutral towards the NPC for roughly 3 minutes. If this command is run on a PC
        ///   or an object that is not a creature, nothing will happen.
        /// </summary>
        public static void SurrenderToEnemies()
        {
            global::NWN.Core.NWScript.SurrenderToEnemies();
        }

        /// <summary>
        ///   Determine whether oSource has a friendly reaction towards oTarget, depending
        ///   on the reputation, PVP setting and (if both oSource and oTarget are PCs),
        ///   oSource's Like/Dislike setting for oTarget.
        ///   Note: If you just want to know how two objects feel about each other in terms
        ///   of faction and personal reputation, use GetIsFriend() instead.
        ///   * Returns TRUE if oSource has a friendly reaction towards oTarget
        /// </summary>
        public static int GetIsReactionTypeFriendly(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsReactionTypeFriendly(oTarget, oSource);
        }

        /// <summary>
        ///   Determine whether oSource has a neutral reaction towards oTarget, depending
        ///   on the reputation, PVP setting and (if both oSource and oTarget are PCs),
        ///   oSource's Like/Dislike setting for oTarget.
        ///   Note: If you just want to know how two objects feel about each other in terms
        ///   of faction and personal reputation, use GetIsNeutral() instead.
        ///   * Returns TRUE if oSource has a neutral reaction towards oTarget
        /// </summary>
        public static int GetIsReactionTypeNeutral(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsReactionTypeNeutral(oTarget, oSource);
        }

        /// <summary>
        ///   Determine whether oSource has a Hostile reaction towards oTarget, depending
        ///   on the reputation, PVP setting and (if both oSource and oTarget are PCs),
        ///   oSource's Like/Dislike setting for oTarget.
        ///   Note: If you just want to know how two objects feel about each other in terms
        ///   of faction and personal reputation, use GetIsEnemy() instead.
        ///   * Returns TRUE if oSource has a hostile reaction towards oTarget
        /// </summary>
        public static bool GetIsReactionTypeHostile(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsReactionTypeHostile(oTarget, oSource) == 1;
        }

        /// <summary>
        ///   Take nAmount of gold from oCreatureToTakeFrom.
        ///   - nAmount
        ///   - oCreatureToTakeFrom: If this is not a valid creature, nothing will happen.
        ///   - bDestroy: If this is TRUE, the caller will not get the gold.  Instead, the
        ///   gold will be destroyed and will vanish from the game.
        /// </summary>
        public static void TakeGoldFromCreature(int nAmount, uint oCreatureToTakeFrom, bool bDestroy = false)
        {
            global::NWN.Core.NWScript.TakeGoldFromCreature(nAmount, oCreatureToTakeFrom, bDestroy ? 1 : 0);
        }

        /// <summary>
        ///   Get the object that killed the caller.
        /// </summary>
        public static uint GetLastKiller()
        {
            return global::NWN.Core.NWScript.GetLastKiller();
        }

        /// <summary>
        ///   * Returns TRUE if oCreature is the Dungeon Master.
        ///   Note: This will return FALSE if oCreature is a DM Possessed creature.
        ///   To determine if oCreature is a DM Possessed creature, use GetIsDMPossessed()
        /// </summary>
        public static bool GetIsDM(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetIsDM(oCreature) != 0;
        }

        /// <summary>
        ///   Use this in an OnRespawnButtonPressed module script to get the object id of
        ///   the player who last pressed the respawn button.
        /// </summary>
        public static uint GetLastRespawnButtonPresser()
        {
            return global::NWN.Core.NWScript.GetLastRespawnButtonPresser();
        }

        /// <summary>
        ///   The creature will equip the armour in its possession that has the highest
        ///   armour class.
        /// </summary>
        public static void ActionEquipMostEffectiveArmor()
        {
            global::NWN.Core.NWScript.ActionEquipMostEffectiveArmor();
        }

        /// <summary>
        ///   * Returns TRUE if oCreature was spawned from an encounter.
        /// </summary>
        public static int GetIsEncounterCreature(uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsEncounterCreature(oCreature);
        }

        /// <summary>
        ///   The creature will equip the melee weapon in its possession that can do the
        ///   most damage. If no valid melee weapon is found, it will equip the most
        ///   damaging range weapon. This function should only ever be called in the
        ///   EndOfCombatRound scripts, because otherwise it would have to stop the combat
        ///   round to run simulation.
        ///   - oVersus: You can try to get the most damaging weapon against oVersus
        ///   - bOffHand
        /// </summary>
        public static void ActionEquipMostDamagingMelee(uint oVersus = OBJECT_INVALID, bool bOffHand = false)
        {
            global::NWN.Core.NWScript.ActionEquipMostDamagingMelee(oVersus, bOffHand ? 1 : 0);
        }

        /// <summary>
        ///   The creature will equip the range weapon in its possession that can do the
        ///   most damage.
        ///   If no valid range weapon can be found, it will equip the most damaging melee
        ///   weapon.
        ///   - oVersus: You can try to get the most damaging weapon against oVersus
        /// </summary>
        public static void ActionEquipMostDamagingRanged(uint oVersus = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.ActionEquipMostDamagingRanged(oVersus);
        }

        /// <summary>
        ///   Gives nXpAmount to oCreature.
        /// </summary>
        public static void GiveXPToCreature(uint oCreature, int nXpAmount)
        {
            global::NWN.Core.NWScript.GiveXPToCreature(oCreature, nXpAmount);
        }

        /// <summary>
        ///   Sets oCreature's experience to nXpAmount.
        /// </summary>
        public static void SetXP(uint oCreature, int nXpAmount)
        {
            global::NWN.Core.NWScript.SetXP(oCreature, nXpAmount);
        }

        /// <summary>
        ///   Get oCreature's experience.
        /// </summary>
        public static int GetXP(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetXP(oCreature);
        }

        /// <summary>
        ///   Force the action subject to move to lDestination.
        /// </summary>
        public static void ActionForceMoveToLocation(Location lDestination, bool bRun = false, float fTimeout = 30.0f)
        {
            global::NWN.Core.NWScript.ActionForceMoveToLocation(lDestination, bRun ? 1 : 0, fTimeout);
        }

        /// <summary>
        ///   Force the action subject to move to oMoveTo.
        /// </summary>
        public static void ActionForceMoveToObject(uint oMoveTo, bool bRun = false, float fRange = 1.0f,
            float fTimeout = 30.0f)
        {
            global::NWN.Core.NWScript.ActionForceMoveToObject(oMoveTo, bRun ? 1 : 0, fRange, fTimeout);
        }

        /// <summary>
        ///   Get the last creature that opened the caller.
        ///   * Returns OBJECT_INVALID if the caller is not a valid door, placeable or store.
        /// </summary>
        public static uint GetLastOpenedBy()
        {
            return global::NWN.Core.NWScript.GetLastOpenedBy();
        }

        /// <summary>
        ///   Determines the number of times that oCreature has nSpell memorised.
        ///   - nSpell: SPELL_*
        ///   - oCreature
        /// </summary>
        public static int GetHasSpell(Spell nSpell, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetHasSpell((int)nSpell, oCreature);
        }

        /// <summary>
        ///   Get the gender of oCreature.
        /// </summary>
        public static Gender GetGender(uint oCreature)
        {
            return (Gender)global::NWN.Core.NWScript.GetGender(oCreature);
        }

        /// <summary>
        ///   Get the type of disturbance (INVENTORY_DISTURB_*) that caused the caller's
        ///   OnInventoryDisturbed script to fire.  This will only work for creatures and
        ///   placeables.
        /// </summary>
        public static DisturbType GetInventoryDisturbType()
        {
            return (DisturbType)global::NWN.Core.NWScript.GetInventoryDisturbType();
        }

        /// <summary>
        ///   get the item that caused the caller's OnInventoryDisturbed script to fire.
        ///   * Returns OBJECT_INVALID if the caller is not a valid object.
        /// </summary>
        public static uint GetInventoryDisturbItem()
        {
            return global::NWN.Core.NWScript.GetInventoryDisturbItem();
        }

        /// <summary>
        ///   A creature can have up to three classes.  This function determines the
        ///   creature's class (CLASS_TYPE_*) based on nClassPosition.
        ///   - nClassPosition: 1, 2 or 3
        ///   - oCreature
        ///   * Returns CLASS_TYPE_INVALID if the oCreature does not have a class in
        ///   nClassPosition (i.e. a single-class creature will only have a value in
        ///   nClassLocation=1) or if oCreature is not a valid creature.
        /// </summary>
        public static ClassType GetClassByPosition(int nClassPosition, uint oCreature = OBJECT_INVALID)
        {
            return (ClassType)global::NWN.Core.NWScript.GetClassByPosition(nClassPosition, oCreature);
        }

        /// <summary>
        ///   A creature can have up to three classes.  This function determines the
        ///   creature's class level based on nClass Position.
        ///   - nClassPosition: 1, 2 or 3
        ///   - oCreature
        ///   * Returns 0 if oCreature does not have a class in nClassPosition
        ///   (i.e. a single-class creature will only have a value in nClassLocation=1)
        ///   or if oCreature is not a valid creature.
        /// </summary>
        public static int GetLevelByPosition(int nClassPosition, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetLevelByPosition(nClassPosition, oCreature);
        }

        /// <summary>
        ///   Determine the levels that oCreature holds in nClassType.
        ///   - nClassType: CLASS_TYPE_*
        ///   - oCreature
        /// </summary>
        public static int GetLevelByClass(ClassType nClassType, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetLevelByClass((int)nClassType, oCreature);
        }

        /// <summary>
        ///   Returns the ability modifier for the specified ability
        ///   Get oCreature's ability modifier for nAbility.
        ///   - nAbility: ABILITY_*
        ///   - oCreature
        /// </summary>
        public static int GetAbilityModifier(AbilityType nAbility, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetAbilityModifier((int)nAbility, oCreature);
        }

        /// <summary>
        ///   * Returns TRUE if oCreature is in combat.
        /// </summary>
        public static bool GetIsInCombat(uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsInCombat(oCreature) != 0;
        }

        /// <summary>
        ///   Give nGP gold to oCreature.
        /// </summary>
        public static void GiveGoldToCreature(uint oCreature, int nGP)
        {
            global::NWN.Core.NWScript.GiveGoldToCreature(oCreature, nGP);
        }

        /// <summary>
        ///   Get the creature nearest to lLocation, subject to all the criteria specified.
        ///   - nFirstCriteriaType: CREATURE_TYPE_*
        ///   - nFirstCriteriaValue:
        ///   -> CLASS_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_CLASS
        ///   -> SPELL_* if nFirstCriteriaType was CREATURE_TYPE_DOES_NOT_HAVE_SPELL_EFFECT
        ///   or CREATURE_TYPE_HAS_SPELL_EFFECT
        ///   -> TRUE or FALSE if nFirstCriteriaType was CREATURE_TYPE_IS_ALIVE
        ///   -> PERCEPTION_* if nFirstCriteriaType was CREATURE_TYPE_PERCEPTION
        ///   -> PLAYER_CHAR_IS_PC or PLAYER_CHAR_NOT_PC if nFirstCriteriaType was
        ///   CREATURE_TYPE_PLAYER_CHAR
        ///   -> RACIAL_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_RACIAL_TYPE
        ///   -> REPUTATION_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_REPUTATION
        ///   For example, to get the nearest PC, use
        ///   (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC)
        ///   - lLocation: We're trying to find the creature of the specified type that is
        ///   nearest to lLocation
        ///   - nNth: We don't have to find the first nearest: we can find the Nth nearest....
        ///   - nSecondCriteriaType: This is used in the same way as nFirstCriteriaType to
        ///   further specify the type of creature that we are looking for.
        ///   - nSecondCriteriaValue: This is used in the same way as nFirstCriteriaValue
        ///   to further specify the type of creature that we are looking for.
        ///   - nThirdCriteriaType: This is used in the same way as nFirstCriteriaType to
        ///   further specify the type of creature that we are looking for.
        ///   - nThirdCriteriaValue: This is used in the same way as nFirstCriteriaValue to
        ///   further specify the type of creature that we are looking for.
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetNearestCreatureToLocation(CreatureType nFirstCriteriaType, bool nFirstCriteriaValue,
            Location lLocation, int nNth = 1, int nSecondCriteriaType = -1, int nSecondCriteriaValue = -1,
            int nThirdCriteriaType = -1, int nThirdCriteriaValue = -1)
        {
            return global::NWN.Core.NWScript.GetNearestCreatureToLocation((int)nFirstCriteriaType, nFirstCriteriaValue ? 1 : 0, lLocation, nNth, nSecondCriteriaType, nSecondCriteriaValue, nThirdCriteriaType, nThirdCriteriaValue);
        }

        /// <summary>
        ///   Get the level at which this creature cast it's last spell (or spell-like ability)
        ///   * Return value on error, or if oCreature has not yet cast a spell: 0;
        /// </summary>
        public static int GetCasterLevel(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetCasterLevel(oCreature);
        }

        /// <summary>
        ///   Get the racial type (RACIAL_TYPE_*) of oCreature
        ///   * Return value if oCreature is not a valid creature: RACIAL_TYPE_INVALID
        /// </summary>
        public static RacialType GetRacialType(uint oCreature)
        {
            return (RacialType)global::NWN.Core.NWScript.GetRacialType(oCreature);
        }

        /// <summary>
        ///   Get the creature nearest to oTarget, subject to all the criteria specified.
        ///   - nFirstCriteriaType: CREATURE_TYPE_*
        ///   - nFirstCriteriaValue:
        ///   -> CLASS_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_CLASS
        ///   -> SPELL_* if nFirstCriteriaType was CREATURE_TYPE_DOES_NOT_HAVE_SPELL_EFFECT
        ///   or CREATURE_TYPE_HAS_SPELL_EFFECT
        ///   -> TRUE or FALSE if nFirstCriteriaType was CREATURE_TYPE_IS_ALIVE
        ///   -> PERCEPTION_* if nFirstCriteriaType was CREATURE_TYPE_PERCEPTION
        ///   -> PLAYER_CHAR_IS_PC or PLAYER_CHAR_NOT_PC if nFirstCriteriaType was
        ///   CREATURE_TYPE_PLAYER_CHAR
        ///   -> RACIAL_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_RACIAL_TYPE
        ///   -> REPUTATION_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_REPUTATION
        ///   For example, to get the nearest PC, use:
        ///   (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC)
        ///   - oTarget: We're trying to find the creature of the specified type that is
        ///   nearest to oTarget
        ///   - nNth: We don't have to find the first nearest: we can find the Nth nearest...
        ///   - nSecondCriteriaType: This is used in the same way as nFirstCriteriaType to
        ///   further specify the type of creature that we are looking for.
        ///   - nSecondCriteriaValue: This is used in the same way as nFirstCriteriaValue
        ///   to further specify the type of creature that we are looking for.
        ///   - nThirdCriteriaType: This is used in the same way as nFirstCriteriaType to
        ///   further specify the type of creature that we are looking for.
        ///   - nThirdCriteriaValue: This is used in the same way as nFirstCriteriaValue to
        ///   further specify the type of creature that we are looking for.
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetNearestCreature(CreatureType nFirstCriteriaType, int nFirstCriteriaValue,
            uint oTarget = OBJECT_INVALID, int nNth = 1, int nSecondCriteriaType = -1, int nSecondCriteriaValue = -1,
            int nThirdCriteriaType = -1, int nThirdCriteriaValue = -1)
        {
            return global::NWN.Core.NWScript.GetNearestCreature((int)nFirstCriteriaType, nFirstCriteriaValue, oTarget, nNth, nSecondCriteriaType, nSecondCriteriaValue, nThirdCriteriaType, nThirdCriteriaValue);
        }

        /// <summary>
        ///   Get the ability score of type nAbility for a creature (otherwise 0)
        ///   - oCreature: the creature whose ability score we wish to find out
        ///   - nAbilityType: ABILITY_*
        ///   - nBaseAbilityScore: if set to true will return the base ability score without
        ///   bonuses (e.g. ability bonuses granted from equipped items).
        ///   Return value on error: 0
        /// </summary>
        public static int GetAbilityScore(uint oCreature, AbilityType nAbilityType, bool nBaseAbilityScore = false)
        {
            return global::NWN.Core.NWScript.GetAbilityScore(oCreature, (int)nAbilityType, nBaseAbilityScore ? 1 : 0);
        }

        /// <summary>
        ///   * Returns TRUE if oCreature is a dead NPC, dead PC or a dying PC.
        /// </summary>
        public static bool GetIsDead(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetIsDead(oCreature) != 0;
        }

        /// <summary>
        ///   Get the number of hitdice for oCreature.
        ///   * Return value if oCreature is not a valid creature: 0
        /// </summary>
        public static int GetHitDice(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetHitDice(oCreature);
        }

        /// <summary>
        ///   Get the creature that is going to attack oTarget.
        ///   Note: This value is cleared out at the end of every combat round and should
        ///   not be used in any case except when getting a "going to be attacked" shout
        ///   from the master creature (and this creature is a henchman)
        ///   * Returns OBJECT_INVALID if oTarget is not a valid creature.
        /// </summary>
        public static uint GetGoingToBeAttackedBy(uint oTarget)
        {
            return global::NWN.Core.NWScript.GetGoingToBeAttackedBy(oTarget);
        }

        /// <summary>
        ///   * Returns TRUE if oCreature is a Player Controlled character.
        /// </summary>
        public static bool GetIsPC(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetIsPC(oCreature) != 0;
        }

        /// <summary>
        ///   - oCreature
        ///   - nImmunityType: IMMUNITY_TYPE_*
        ///   - oVersus: if this is specified, then we also check for the race and
        ///   alignment of oVersus
        ///   * Returns TRUE if oCreature has immunity of type nImmunity versus oVersus.
        /// </summary>
        public static bool GetIsImmune(uint oCreature, ImmunityType nImmunityType, uint oVersus = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsImmune(oCreature, (int)nImmunityType, oVersus) == 1;
        }

        /// <summary>
        ///   Determine whether oCreature has nFeat, and nFeat is useable.
        ///   - nFeat: FEAT_*
        ///   - oCreature
        /// </summary>
        public static bool GetHasFeat(FeatType nFeat, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetHasFeat((int)nFeat, oCreature) != 0;
        }

        /// <summary>
        ///   Determine whether oCreature has nSkill, and nSkill is useable.
        ///   - nSkill: SKILL_*
        ///   - oCreature
        /// </summary>
        public static bool GetHasSkill(NWNSkillType nSkill, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetHasSkill((int)nSkill, oCreature) != 0;
        }

        /// <summary>
        ///   Determine whether oSource sees oTarget.
        ///   NOTE: This *only* works on creatures, as visibility lists are not
        ///   maintained for non-creature objects.
        /// </summary>
        public static bool GetObjectSeen(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetObjectSeen(oTarget, oSource) != 0;
        }

        /// <summary>
        ///   Determine whether oSource hears oTarget.
        ///   NOTE: This *only* works on creatures, as visibility lists are not
        ///   maintained for non-creature objects.
        /// </summary>
        public static bool GetObjectHeard(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetObjectHeard(oTarget, oSource) != 0;
        }

        /// <summary>
        ///   * Returns TRUE if oCreature is of a playable racial type.
        /// </summary>
        public static bool GetIsPlayableRacialType(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetIsPlayableRacialType(oCreature) != 0;
        }

        /// <summary>
        ///   Get the number of ranks that oTarget has in nSkill.
        ///   - nSkill: SKILL_*
        ///   - oTarget
        ///   - nBaseSkillRank: if set to true returns the number of base skill ranks the target
        ///   has (i.e. not including any bonuses from ability scores, feats, etc).
        ///   * Returns -1 if oTarget doesn't have nSkill.
        ///   * Returns 0 if nSkill is untrained.
        /// </summary>
        public static int GetSkillRank(NWNSkillType nSkill, uint oTarget = OBJECT_INVALID, bool nBaseSkillRank = false)
        {
            return global::NWN.Core.NWScript.GetSkillRank((int)nSkill, oTarget, nBaseSkillRank ? 1 : 0);
        }

        /// <summary>
        ///   Get the attack target of oCreature.
        ///   This only works when oCreature is in combat.
        /// </summary>
        public static uint GetAttackTarget(uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetAttackTarget(oCreature);
        }

        /// <summary>
        ///   Get the attack type (SPECIAL_ATTACK_*) of oCreature's last attack.
        ///   This only works when oCreature is in combat.
        /// </summary>
        public static SpecialAttack GetLastAttackType(uint oCreature = OBJECT_INVALID)
        {
            return (SpecialAttack)global::NWN.Core.NWScript.GetLastAttackType(oCreature);
        }

        /// <summary>
        /// Set the gender of oCreature.
        /// - nGender: a GENDER_* constant.
        /// </summary>
        public static void SetGender(uint oCreature, Gender nGender)
        {
            global::NWN.Core.NWScript.SetGender(oCreature, (int)nGender);
        }

        /// <summary>
        /// Get the soundset of oCreature.
        /// Returns -1 on error.
        /// </summary>
        public static int GetSoundset(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetSoundset(oCreature);
        }

        /// <summary>
        /// Set the soundset of oCreature, see soundset.2da for possible values.
        /// </summary>
        public static void SetSoundset(uint oCreature, int nSoundset)
        {
            global::NWN.Core.NWScript.SetSoundset(oCreature, nSoundset);
        }

        /// <summary>
        /// Ready a spell level for oCreature.
        /// - nSpellLevel: 0-9
        /// - nClassType: a CLASS_TYPE_* constant or CLASS_TYPE_INVALID to ready the spell level for all classes.
        /// </summary>
        public static void ReadySpellLevel(uint oCreature, int nSpellLevel, ClassType nClassType = ClassType.Invalid)
        {
            global::NWN.Core.NWScript.ReadySpellLevel(oCreature, nSpellLevel, (int)nClassType);
        }

        /// <summary>
        /// Makes oCreature controllable by oPlayer, if player party control is enabled
        /// Setting oPlayer=OBJECT_INVALID removes the override and reverts to regular party control behavior
        /// NB: A creature is only controllable by one player, so if you set oPlayer to a non-Player object
        ///    (e.g. the module) it will disable regular party control for this creature
        /// </summary>
        public static void SetCommandingPlayer(uint oCreature, uint oPlayer)
        {
            global::NWN.Core.NWScript.SetCommandingPlayer(oCreature, oPlayer);
        }

        /// <summary>
        /// Get the current discoverability mask of oObject.
        /// Returns -1 if oObject cannot have a discovery mask.
        /// </summary>
        public static int GetObjectUiDiscoveryMask(uint oObject)
        {
            return global::NWN.Core.NWScript.GetObjectUiDiscoveryMask(oObject);
        }

        /// <summary>
        /// Sets the discoverability mask on oObject.
        /// This allows toggling areahilite (TAB key by default) and mouseover discovery in the area view.
        /// * nMask is a mask of OBJECT_UI_DISCOVERY_MODE_*
        /// Will currently only work on Creatures, Doors (Hilite only), Items and Useable Placeables.
        /// Does not affect inventory items.
        /// </summary>
        public static void SetObjectUiDiscoveryMask(uint oObject, ObjectUIDiscoveryType nMask = ObjectUIDiscoveryType.Default)
        {
            global::NWN.Core.NWScript.SetObjectUiDiscoveryMask(oObject, (int)nMask);
        }

        /// <summary>
        /// Sets a text override for the mouseover/tab-highlight text bubble of oObject.
        /// Will currently only work on Creatures, Items and Useable Placeables.
        /// * nMode is one of OBJECT_UI_TEXT_BUBBLE_OVERRIDE_*.
        /// </summary>
        public static void SetObjectTextBubbleOverride(uint oObject, ObjectUITextBubbleOverrideType nMode, string sText)
        {
            global::NWN.Core.NWScript.SetObjectTextBubbleOverride(oObject, (int)nMode, sText);
        }
    }
}