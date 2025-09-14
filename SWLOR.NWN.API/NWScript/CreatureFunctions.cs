using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Returns the footstep type of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the footstep type for (default: OBJECT_INVALID)</param>
        /// <returns>The footstep type. Returns FOOTSTEP_TYPE_INVALID if used on a non-creature object, or if used on creature that has no footstep sounds by default (e.g., Will-O'-Wisp)</returns>
        /// <remarks>The footstep type determines what the creature's footsteps sound like whenever they take a step.</remarks>
        public static FootstepType GetFootstepType(uint oCreature = OBJECT_INVALID)
        {
            return (FootstepType)global::NWN.Core.NWScript.GetFootstepType(oCreature);
        }

        /// <summary>
        /// Sets the footstep type of the specified creature.
        /// </summary>
        /// <param name="nFootstepType">The footstep type (FOOTSTEP_TYPE_* constants)</param>
        /// <param name="oCreature">The creature to change the footstep sound for (default: OBJECT_INVALID)</param>
        /// <remarks>Changing a creature's footstep type will change the sound that its feet make whenever the creature takes a step. By default a creature's footsteps are determined by the appearance type of the creature. SetFootstepType() allows you to make a creature use a different footstep type than it would use by default for its given appearance. Possible values: FOOTSTEP_TYPE_NORMAL, FOOTSTEP_TYPE_LARGE, FOOTSTEP_TYPE_DRAGON, FOOTSTEP_TYPE_SOFT, FOOTSTEP_TYPE_HOOF, FOOTSTEP_TYPE_HOOF_LARGE, FOOTSTEP_TYPE_BEETLE, FOOTSTEP_TYPE_SPIDER, FOOTSTEP_TYPE_SKELETON, FOOTSTEP_TYPE_LEATHER_WING, FOOTSTEP_TYPE_FEATHER_WING, FOOTSTEP_TYPE_DEFAULT (makes the creature use its original default footstep sounds), FOOTSTEP_TYPE_NONE</remarks>
        public static void SetFootstepType(FootstepType nFootstepType, uint oCreature = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetFootstepType((int)nFootstepType, oCreature);
        }

        /// <summary>
        /// Returns the wing type of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the wing type for (default: OBJECT_INVALID)</param>
        /// <returns>The wing type. Returns CREATURE_WING_TYPE_NONE if used on a non-creature object, if the creature has no wings, or if the creature cannot have its wing type changed in the toolset</returns>
        /// <remarks>Possible values: CREATURE_WING_TYPE_NONE, CREATURE_WING_TYPE_DEMON, CREATURE_WING_TYPE_ANGEL, CREATURE_WING_TYPE_BAT, CREATURE_WING_TYPE_DRAGON, CREATURE_WING_TYPE_BUTTERFLY, CREATURE_WING_TYPE_BIRD</remarks>
        public static WingType GetCreatureWingType(uint oCreature = OBJECT_INVALID)
        {
            return (WingType)global::NWN.Core.NWScript.GetCreatureWingType(oCreature);
        }

        /// <summary>
        /// Sets the wing type of the specified creature.
        /// </summary>
        /// <param name="nWingType">The wing type (CREATURE_WING_TYPE_* constants)</param>
        /// <param name="oCreature">The creature to change the wing type for (default: OBJECT_INVALID)</param>
        /// <remarks>Only two creature model types will support wings. The MODELTYPE for the part based (playable races) 'P' and MODELTYPE 'W' in the appearance.2da. Possible values: CREATURE_WING_TYPE_NONE, CREATURE_WING_TYPE_DEMON, CREATURE_WING_TYPE_ANGEL, CREATURE_WING_TYPE_BAT, CREATURE_WING_TYPE_DRAGON, CREATURE_WING_TYPE_BUTTERFLY, CREATURE_WING_TYPE_BIRD</remarks>
        public static void SetCreatureWingType(WingType nWingType, uint oCreature = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCreatureWingType((int)nWingType, oCreature);
        }

        /// <summary>
        /// Returns the model number being used for the specified body part and creature.
        /// </summary>
        /// <param name="nPart">The body part (CREATURE_PART_* constants)</param>
        /// <param name="oCreature">The creature to get the body part for (default: OBJECT_INVALID)</param>
        /// <returns>The model number for the body part. Returns CREATURE_PART_INVALID if used on a non-creature object, or if the creature does not use a part based model</returns>
        /// <remarks>The model number returned is for the body part when the creature is not wearing armor (i.e. whether or not the creature is wearing armor does not affect the return value). Only works on part based creatures, which is typically restricted to the playable races (unless some new part based custom content has been added to the module). Possible body parts: CREATURE_PART_RIGHT_FOOT, CREATURE_PART_LEFT_FOOT, CREATURE_PART_RIGHT_SHIN, CREATURE_PART_LEFT_SHIN, CREATURE_PART_RIGHT_THIGH, CREATURE_PART_LEFT_THIGH, CREATURE_PART_PELVIS, CREATURE_PART_TORSO, CREATURE_PART_BELT, CREATURE_PART_NECK, CREATURE_PART_RIGHT_FOREARM, CREATURE_PART_LEFT_FOREARM, CREATURE_PART_RIGHT_BICEP, CREATURE_PART_LEFT_BICEP, CREATURE_PART_RIGHT_SHOULDER, CREATURE_PART_LEFT_SHOULDER, CREATURE_PART_RIGHT_HAND, CREATURE_PART_LEFT_HAND, CREATURE_PART_HEAD</remarks>
        public static int GetCreatureBodyPart(CreaturePart nPart, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCreatureBodyPart((int)nPart, oCreature);
        }

        /// <summary>
        /// Sets the body part model to be used on the specified creature.
        /// </summary>
        /// <param name="nPart">The body part (CREATURE_PART_* constants)</param>
        /// <param name="nModelNumber">The model number (CREATURE_MODEL_TYPE_* constants)</param>
        /// <param name="oCreature">The creature to change the body part for (default: OBJECT_INVALID)</param>
        /// <remarks>The model names for parts need to be in the following format: p&lt;m/f&gt;&lt;race letter&gt;&lt;phenotype&gt;_&lt;body part&gt;&lt;model number&gt;.mdl. Only part based creature appearance types are supported (i.e. The model types for the playable races ('P') in the appearance.2da). Possible body parts: CREATURE_PART_RIGHT_FOOT, CREATURE_PART_LEFT_FOOT, CREATURE_PART_RIGHT_SHIN, CREATURE_PART_LEFT_SHIN, CREATURE_PART_RIGHT_THIGH, CREATURE_PART_LEFT_THIGH, CREATURE_PART_PELVIS, CREATURE_PART_TORSO, CREATURE_PART_BELT, CREATURE_PART_NECK, CREATURE_PART_RIGHT_FOREARM, CREATURE_PART_LEFT_FOREARM, CREATURE_PART_RIGHT_BICEP, CREATURE_PART_LEFT_BICEP, CREATURE_PART_RIGHT_SHOULDER, CREATURE_PART_LEFT_SHOULDER, CREATURE_PART_RIGHT_HAND, CREATURE_PART_LEFT_HAND, CREATURE_PART_HEAD. Possible model types: CREATURE_MODEL_TYPE_NONE, CREATURE_MODEL_TYPE_SKIN (not for use on shoulders, pelvis or head), CREATURE_MODEL_TYPE_TATTOO (for body parts that support tattoos, i.e. not heads/feet/hands), CREATURE_MODEL_TYPE_UNDEAD (undead model only exists for the right arm parts).</remarks>
        public static void SetCreatureBodyPart(CreaturePart nPart, int nModelNumber, uint oCreature = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCreatureBodyPart((int)nPart, nModelNumber, oCreature);
        }

        /// <summary>
        /// Returns the tail type of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the tail type for (default: OBJECT_INVALID)</param>
        /// <returns>The tail type. Returns CREATURE_TAIL_TYPE_NONE if used on a non-creature object, if the creature has no tail, or if the creature cannot have its tail type changed in the toolset</returns>
        /// <remarks>Possible values: CREATURE_TAIL_TYPE_NONE, CREATURE_TAIL_TYPE_LIZARD, CREATURE_TAIL_TYPE_BONE, CREATURE_TAIL_TYPE_DEVIL</remarks>
        public static TailType GetCreatureTailType(uint oCreature = OBJECT_INVALID)
        {
            return (TailType)global::NWN.Core.NWScript.GetCreatureTailType(oCreature);
        }

        /// <summary>
        /// Sets the tail type of the specified creature.
        /// </summary>
        /// <param name="nTailType">The tail type (CREATURE_TAIL_TYPE_* constants)</param>
        /// <param name="oCreature">The creature to change the tail type for (default: OBJECT_INVALID)</param>
        /// <remarks>Only two creature model types will support tails. The MODELTYPE for the part based (playable) races 'P' and MODELTYPE 'T' in the appearance.2da. Possible values: CREATURE_TAIL_TYPE_NONE, CREATURE_TAIL_TYPE_LIZARD, CREATURE_TAIL_TYPE_BONE, CREATURE_TAIL_TYPE_DEVIL</remarks>
        public static void SetCreatureTailType(TailType nTailType, uint oCreature = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetCreatureTailType((int)nTailType, oCreature);
        }

        /// <summary>
        /// Returns the creature's currently set phenotype (body type).
        /// </summary>
        /// <param name="oCreature">The creature to get the phenotype for</param>
        /// <returns>The creature's phenotype</returns>
        public static PhenoType GetPhenoType(uint oCreature)
        {
            return (PhenoType)global::NWN.Core.NWScript.GetPhenoType(oCreature);
        }

        /// <summary>
        /// Sets the creature's phenotype (body type) to the specified type.
        /// </summary>
        /// <param name="nPhenoType">The phenotype type to set</param>
        /// <param name="oCreature">The creature to change the phenotype for (default: OBJECT_INVALID)</param>
        /// <remarks>SetPhenoType will only work on part based creatures (i.e. the starting default playable races). Possible values: PHENOTYPE_NORMAL, PHENOTYPE_BIG, PHENOTYPE_CUSTOM* (custom phenotypes should only be used if you have specifically created your own custom content that requires the use of a new phenotype and you have specified the appropriate custom phenotype in your custom content)</remarks>
        public static void SetPhenoType(PhenoType nPhenoType, uint oCreature = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetPhenoType((int)nPhenoType, oCreature);
        }

        /// <summary>
        /// Returns whether this creature is able to be disarmed.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>True if the creature can be disarmed, false otherwise</returns>
        /// <remarks>Checks disarm flag on creature, and if the creature actually has a weapon equipped in their right hand that is droppable</remarks>
        public static bool GetIsCreatureDisarmable(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetIsCreatureDisarmable(oCreature) != 0;
        }

        /// <summary>
        /// Returns the class that the spellcaster cast the spell as.
        /// </summary>
        /// <returns>The class type. Returns CLASS_TYPE_INVALID if the caster has no valid class (placeables, etc.)</returns>
        public static ClassType GetLastSpellCastClass()
        {
            return (ClassType)global::NWN.Core.NWScript.GetLastSpellCastClass();
        }

        /// <summary>
        /// Sets the number of base attacks for the specified creature.
        /// </summary>
        /// <param name="nBaseAttackBonus">The number of base attacks (range: 1 to 6)</param>
        /// <param name="oCreature">The creature to set the base attack bonus for (default: OBJECT_INVALID)</param>
        /// <remarks>This function does not work on Player Characters.</remarks>
        public static void SetBaseAttackBonus(int nBaseAttackBonus, uint oCreature = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetBaseAttackBonus(nBaseAttackBonus, oCreature);
        }

        /// <summary>
        /// Restores the number of base attacks back to its original state.
        /// </summary>
        /// <param name="oCreature">The creature to restore the base attack bonus for (default: OBJECT_INVALID)</param>
        public static void RestoreBaseAttackBonus(uint oCreature = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.RestoreBaseAttackBonus(oCreature);
        }


        /// <summary>
        /// Sets the creature's appearance type to the specified value.
        /// </summary>
        /// <param name="oCreature">The creature to change the appearance type for</param>
        /// <param name="nAppearanceType">The appearance type (APPEARANCE_TYPE_* constants)</param>
        public static void SetCreatureAppearanceType(uint oCreature, AppearanceType nAppearanceType)
        {
            global::NWN.Core.NWScript.SetCreatureAppearanceType(oCreature, (int)nAppearanceType);
        }

        /// <summary>
        /// Returns the default package selected for this creature to level up with.
        /// </summary>
        /// <param name="oCreature">The creature to get the starting package for</param>
        /// <returns>The starting package. Returns PACKAGE_INVALID if error occurs</returns>
        public static int GetCreatureStartingPackage(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetCreatureStartingPackage(oCreature);
        }

        /// <summary>
        /// Returns the spell resistance of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get spell resistance for</param>
        /// <returns>The spell resistance value. Returns 0 if the creature has no spell resistance or an invalid creature is passed in</returns>
        public static int GetSpellResistance(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetSpellResistance(oCreature);
        }

        /// <summary>
        /// Sets the lootable state of a living NPC creature.
        /// </summary>
        /// <param name="oCreature">The creature to set the lootable state for</param>
        /// <param name="bLootable">Whether the creature is lootable</param>
        /// <remarks>This function will not work on players or dead creatures.</remarks>
        public static void SetLootable(uint oCreature, bool bLootable)
        {
            global::NWN.Core.NWScript.SetLootable(oCreature, bLootable ? 1 : 0);
        }

        /// <summary>
        /// Returns the lootable state of a creature.
        /// </summary>
        /// <param name="oCreature">The creature to check the lootable state for</param>
        /// <returns>True if the creature is lootable, false otherwise</returns>
        public static bool GetLootable(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetLootable(oCreature) != 0;
        }

        /// <summary>
        /// Gets the status of the specified action mode on a creature.
        /// </summary>
        /// <param name="oCreature">The creature to check the action mode for</param>
        /// <param name="nMode">The action mode to check (ACTION_MODE_* constants)</param>
        /// <returns>True if the action mode is active, false otherwise</returns>
        public static bool GetActionMode(uint oCreature, ActionMode nMode)
        {
            return global::NWN.Core.NWScript.GetActionMode(oCreature, (int)nMode) == 1;
        }

        /// <summary>
        /// Sets the status of the specified action mode on a creature.
        /// </summary>
        /// <param name="oCreature">The creature to set the action mode for</param>
        /// <param name="nMode">The action mode to set (ACTION_MODE_* constants)</param>
        /// <param name="nStatus">The status to set (true/false)</param>
        public static void SetActionMode(uint oCreature, ActionMode nMode, bool nStatus)
        {
            global::NWN.Core.NWScript.SetActionMode(oCreature, (int)nMode, nStatus ? 1 : 0);
        }

        /// <summary>
        /// Returns the current arcane spell failure factor of a creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the arcane spell failure for</param>
        /// <returns>The arcane spell failure factor</returns>
        public static int GetArcaneSpellFailure(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetArcaneSpellFailure(oCreature);
        }

        /// <summary>
        /// Sets the name of the creature's sub race.
        /// </summary>
        /// <param name="oCreature">The creature to set the sub race for</param>
        /// <param name="sSubRace">The sub race name to set</param>
        public static void SetSubRace(uint oCreature, string sSubRace)
        {
            global::NWN.Core.NWScript.SetSubRace(oCreature, sSubRace);
        }

        /// <summary>
        /// Sets the name of the creature's deity.
        /// </summary>
        /// <param name="oCreature">The creature to set the deity for</param>
        /// <param name="sDeity">The deity name to set</param>
        public static void SetDeity(uint oCreature, string sDeity)
        {
            global::NWN.Core.NWScript.SetDeity(oCreature, sDeity);
        }

        /// <summary>
        /// Returns true if the creature is currently possessed by a DM character.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>True if the creature is possessed by a DM, false otherwise</returns>
        /// <remarks>GetIsDMPossessed() will return false if oCreature is the DM character. To determine if oCreature is a DM character use GetIsDM()</remarks>
        public static bool GetIsDMPossessed(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetIsDMPossessed(oCreature) != 0;
        }

        /// <summary>
        /// Increments the remaining uses per day for the specified feat on the creature by one.
        /// </summary>
        /// <param name="oCreature">The creature to modify</param>
        /// <param name="nFeat">The feat constant (FEAT_* constants)</param>
        /// <remarks>Total number of feats per day cannot exceed the maximum.</remarks>
        public static void IncrementRemainingFeatUses(uint oCreature, FeatType nFeat)
        {
            global::NWN.Core.NWScript.IncrementRemainingFeatUses(oCreature, (int)nFeat);
        }

        /// <summary>
        /// Gets the current AI level that the creature is running at.
        /// </summary>
        /// <param name="oTarget">The creature to get the AI level for (default: OBJECT_INVALID)</param>
        /// <returns>One of the following: AI_LEVEL_INVALID, AI_LEVEL_VERY_LOW, AI_LEVEL_LOW, AI_LEVEL_NORMAL, AI_LEVEL_HIGH, AI_LEVEL_VERY_HIGH</returns>
        public static AILevel GetAILevel(uint oTarget = OBJECT_INVALID)
        {
            return (AILevel)global::NWN.Core.NWScript.GetAILevel(oTarget);
        }

        /// <summary>
        /// Sets the current AI level of the creature to the specified value.
        /// </summary>
        /// <param name="oTarget">The creature to set the AI level for</param>
        /// <param name="nAILevel">The AI level to set</param>
        /// <remarks>Does not work on Players. The game by default will choose an appropriate AI level for creatures based on the circumstances that the creature is in. Explicitly setting an AI level will override the game AI settings. The new setting will last until SetAILevel is called again with the argument AI_LEVEL_DEFAULT. AI_LEVEL_DEFAULT - Default setting. The game will take over setting the appropriate AI level when required. AI_LEVEL_VERY_LOW - Very Low priority, very stupid, but low CPU usage for AI. Typically used when no players are in the area. AI_LEVEL_LOW - Low priority, mildly stupid, but slightly more CPU usage for AI. Typically used when not in combat, but a player is in the area. AI_LEVEL_NORMAL - Normal priority, average AI, but more CPU usage required for AI. Typically used when creature is in combat. AI_LEVEL_HIGH - High priority, smartest AI, but extremely high CPU usage required for AI. Avoid using this. It is most likely only ever needed for cutscenes.</remarks>
        public static void SetAILevel(uint oTarget, AILevel nAILevel)
        {
            global::NWN.Core.NWScript.SetAILevel(oTarget, (int)nAILevel);
        }

        /// <summary>
        /// Returns true if the creature is a familiar currently possessed by its master.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>True if the creature is a possessed familiar, false if not or if the creature object is invalid</returns>
        public static bool GetIsPossessedFamiliar(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetIsPossessedFamiliar(oCreature) == 1;
        }

        /// <summary>
        /// Causes a Player Creature to unpossess their familiar.
        /// </summary>
        /// <param name="oCreature">The creature to unpossess the familiar for</param>
        /// <remarks>It will work if run on the player creature or the possessed familiar. It does not work in conjunction with any DM possession.</remarks>
        public static void UnpossessFamiliar(uint oCreature)
        {
            global::NWN.Core.NWScript.UnpossessFamiliar(oCreature);
        }

        /// <summary>
        /// Gets the immortal flag on a creature.
        /// </summary>
        /// <param name="oTarget">The creature to check the immortal flag for (default: OBJECT_INVALID)</param>
        /// <returns>True if the creature is immortal, false otherwise</returns>
        public static bool GetImmortal(uint oTarget = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetImmortal(oTarget) != 0;
        }

        /// <summary>
        /// Performs a single attack on every hostile creature within 10ft of the attacker and determines damage accordingly.
        /// </summary>
        /// <param name="bDisplayFeedback">Whether or not feedback should be displayed (default: true)</param>
        /// <param name="bImproved">If true, the improved version of whirlwind is used (default: false)</param>
        /// <remarks>If the attacker has a ranged weapon equipped, this will have no effect. This is meant to be called inside the spell script for whirlwind attack, it is not meant to be used to queue up a new whirlwind attack. To do that you need to call ActionUseFeat(FEAT_WHIRLWIND_ATTACK, oEnemy)</remarks>
        public static void DoWhirlwindAttack(bool bDisplayFeedback = true, bool bImproved = false)
        {
            global::NWN.Core.NWScript.DoWhirlwindAttack(bDisplayFeedback ? 1 : 0, bImproved ? 1 : 0);
        }

        /// <summary>
        /// Returns the base attack bonus for the given creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the base attack bonus for</param>
        /// <returns>The base attack bonus</returns>
        public static int GetBaseAttackBonus(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetBaseAttackBonus(oCreature);
        }

        /// <summary>
        /// Sets a creature's immortality flag.
        /// </summary>
        /// <param name="oCreature">The creature to set the immortality flag for</param>
        /// <param name="bImmortal">True = creature is immortal and cannot be killed (but still takes damage), False = creature is not immortal and is damaged normally</param>
        /// <remarks>This scripting command only works on Creature objects.</remarks>
        public static void SetImmortal(uint oCreature, bool bImmortal)
        {
            global::NWN.Core.NWScript.SetImmortal(oCreature, bImmortal ? 1 : 0);
        }

        /// <summary>
        /// Returns true if 1d20 roll + skill rank is greater than or equal to difficulty.
        /// </summary>
        /// <param name="oTarget">The creature using the skill</param>
        /// <param name="nSkill">The skill being used (SKILL_* constants)</param>
        /// <param name="nDifficulty">The difficulty class of the skill</param>
        /// <returns>True if the skill check succeeds, false otherwise</returns>
        public static bool GetIsSkillSuccessful(uint oTarget, NWNSkillType nSkill, int nDifficulty)
        {
            return global::NWN.Core.NWScript.GetIsSkillSuccessful(oTarget, (int)nSkill, nDifficulty) != 0;
        }

        /// <summary>
        /// Decrements the remaining uses per day for the specified feat on the creature by one.
        /// </summary>
        /// <param name="oCreature">The creature to modify</param>
        /// <param name="nFeat">The feat constant (FEAT_* constants)</param>
        public static void DecrementRemainingFeatUses(uint oCreature, int nFeat)
        {
            global::NWN.Core.NWScript.DecrementRemainingFeatUses(oCreature, nFeat);
        }

        /// <summary>
        /// Decrements the remaining uses per day for the specified spell on the creature by one.
        /// </summary>
        /// <param name="oCreature">The creature to modify</param>
        /// <param name="nSpell">The spell constant (SPELL_* constants)</param>
        public static void DecrementRemainingSpellUses(uint oCreature, int nSpell)
        {
            global::NWN.Core.NWScript.DecrementRemainingSpellUses(oCreature, nSpell);
        }

        /// <summary>
        /// Returns the stealth mode of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the stealth mode for</param>
        /// <returns>A constant STEALTH_MODE_*</returns>
        public static StealthMode GetStealthMode(uint oCreature)
        {
            return (StealthMode)global::NWN.Core.NWScript.GetStealthMode(oCreature);
        }

        /// <summary>
        /// Returns the detection mode of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the detection mode for</param>
        /// <returns>A constant DETECT_MODE_*</returns>
        public static DetectMode GetDetectMode(uint oCreature)
        {
            return (DetectMode)global::NWN.Core.NWScript.GetDetectMode(oCreature);
        }

        /// <summary>
        /// Returns the defensive casting mode of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the defensive casting mode for</param>
        /// <returns>A constant DEFENSIVE_CASTING_MODE_*</returns>
        public static CastingMode GetDefensiveCastingMode(uint oCreature)
        {
            return (CastingMode)global::NWN.Core.NWScript.GetDefensiveCastingMode(oCreature);
        }

        /// <summary>
        /// Returns the appearance type of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the appearance type for</param>
        /// <returns>A constant APPEARANCE_TYPE_* for valid creatures, APPEARANCE_TYPE_INVALID for non-creatures/invalid creatures</returns>
        public static AppearanceType GetAppearanceType(uint oCreature)
        {
            return (AppearanceType)global::NWN.Core.NWScript.GetAppearanceType(oCreature);
        }

        /// <summary>
        /// Gets the last object that was sent as a GetLastAttacker(), GetLastDamager(), GetLastSpellCaster() (for a hostile spell), or GetLastDisturbed() (when a creature is pickpocketed).
        /// </summary>
        /// <param name="oVictim">The victim object (default: OBJECT_INVALID)</param>
        /// <returns>The last hostile actor</returns>
        /// <remarks>Return values may only ever be: 1) A Creature, 2) Plot Characters will never have this value set, 3) Area of Effect Objects will return the AOE creator if they are registered as this value, otherwise they will return INVALID_OBJECT_ID, 4) Traps will not return the creature that set the trap, 5) This value will never be overwritten by another non-creature object, 6) This value will never be a dead/destroyed creature</remarks>
        public static uint GetLastHostileActor(uint oVictim = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetLastHostileActor(oVictim);
        }

        /// <summary>
        /// Gets the number of hit dice worth of Turn Resistance that the undead creature may have.
        /// </summary>
        /// <param name="oUndead">The undead creature to check (default: OBJECT_INVALID)</param>
        /// <returns>The number of hit dice of turn resistance</returns>
        /// <remarks>This will only work on undead creatures.</remarks>
        public static int GetTurnResistanceHD(uint oUndead = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetTurnResistanceHD(oUndead);
        }

        /// <summary>
        /// Gets the size of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the size for</param>
        /// <returns>The creature size (CREATURE_SIZE_* constants)</returns>
        public static CreatureSize GetCreatureSize(uint oCreature)
        {
            return (CreatureSize)global::NWN.Core.NWScript.GetCreatureSize(oCreature);
        }

        /// <summary>
        /// Causes all creatures within a 10-metre radius to stop what they are doing and sets the NPC's enemies within this range to be neutral towards the NPC for roughly 3 minutes.
        /// </summary>
        /// <remarks>Use this on an NPC. If this command is run on a PC or an object that is not a creature, nothing will happen.</remarks>
        public static void SurrenderToEnemies()
        {
            global::NWN.Core.NWScript.SurrenderToEnemies();
        }

        /// <summary>
        /// Determines whether the source has a friendly reaction towards the target.
        /// </summary>
        /// <param name="oTarget">The target to check the reaction for</param>
        /// <param name="oSource">The source to check the reaction from (default: OBJECT_INVALID)</param>
        /// <returns>True if the source has a friendly reaction towards the target</returns>
        /// <remarks>This depends on the reputation, PVP setting and (if both oSource and oTarget are PCs), oSource's Like/Dislike setting for oTarget. If you just want to know how two objects feel about each other in terms of faction and personal reputation, use GetIsFriend() instead.</remarks>
        public static int GetIsReactionTypeFriendly(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsReactionTypeFriendly(oTarget, oSource);
        }

        /// <summary>
        /// Determines whether the source has a neutral reaction towards the target.
        /// </summary>
        /// <param name="oTarget">The target to check the reaction for</param>
        /// <param name="oSource">The source to check the reaction from (default: OBJECT_INVALID)</param>
        /// <returns>True if the source has a neutral reaction towards the target</returns>
        /// <remarks>This depends on the reputation, PVP setting and (if both oSource and oTarget are PCs), oSource's Like/Dislike setting for oTarget. If you just want to know how two objects feel about each other in terms of faction and personal reputation, use GetIsNeutral() instead.</remarks>
        public static int GetIsReactionTypeNeutral(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsReactionTypeNeutral(oTarget, oSource);
        }

        /// <summary>
        /// Determines whether the source has a hostile reaction towards the target.
        /// </summary>
        /// <param name="oTarget">The target to check the reaction for</param>
        /// <param name="oSource">The source to check the reaction from (default: OBJECT_INVALID)</param>
        /// <returns>True if the source has a hostile reaction towards the target</returns>
        /// <remarks>This depends on the reputation, PVP setting and (if both oSource and oTarget are PCs), oSource's Like/Dislike setting for oTarget. If you just want to know how two objects feel about each other in terms of faction and personal reputation, use GetIsEnemy() instead.</remarks>
        public static bool GetIsReactionTypeHostile(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsReactionTypeHostile(oTarget, oSource) == 1;
        }

        /// <summary>
        /// Takes the specified amount of gold from the creature.
        /// </summary>
        /// <param name="nAmount">The amount of gold to take</param>
        /// <param name="oCreatureToTakeFrom">The creature to take gold from. If this is not a valid creature, nothing will happen</param>
        /// <param name="bDestroy">If true, the caller will not get the gold. Instead, the gold will be destroyed and will vanish from the game (default: false)</param>
        public static void TakeGoldFromCreature(int nAmount, uint oCreatureToTakeFrom, bool bDestroy = false)
        {
            global::NWN.Core.NWScript.TakeGoldFromCreature(nAmount, oCreatureToTakeFrom, bDestroy ? 1 : 0);
        }

        /// <summary>
        /// Gets the object that killed the caller.
        /// </summary>
        /// <returns>The object that killed the caller</returns>
        public static uint GetLastKiller()
        {
            return global::NWN.Core.NWScript.GetLastKiller();
        }

        /// <summary>
        /// Returns true if the creature is the Dungeon Master.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>True if the creature is the Dungeon Master, false otherwise</returns>
        /// <remarks>This will return false if oCreature is a DM Possessed creature. To determine if oCreature is a DM Possessed creature, use GetIsDMPossessed()</remarks>
        public static bool GetIsDM(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetIsDM(oCreature) != 0;
        }

        /// <summary>
        /// Gets the object ID of the player who last pressed the respawn button.
        /// </summary>
        /// <returns>The object ID of the player who last pressed the respawn button</returns>
        /// <remarks>Use this in an OnRespawnButtonPressed module script.</remarks>
        public static uint GetLastRespawnButtonPresser()
        {
            return global::NWN.Core.NWScript.GetLastRespawnButtonPresser();
        }

        /// <summary>
        /// Makes the creature equip the armor in its possession that has the highest armor class.
        /// </summary>
        public static void ActionEquipMostEffectiveArmor()
        {
            global::NWN.Core.NWScript.ActionEquipMostEffectiveArmor();
        }

        /// <summary>
        /// Returns true if the creature was spawned from an encounter.
        /// </summary>
        /// <param name="oCreature">The creature to check (default: OBJECT_INVALID)</param>
        /// <returns>True if the creature was spawned from an encounter, false otherwise</returns>
        public static int GetIsEncounterCreature(uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsEncounterCreature(oCreature);
        }

        /// <summary>
        /// Makes the creature equip the melee weapon in its possession that can do the most damage.
        /// </summary>
        /// <param name="oVersus">You can try to get the most damaging weapon against this target (default: OBJECT_INVALID)</param>
        /// <param name="bOffHand">Whether to equip in the off-hand (default: false)</param>
        /// <remarks>If no valid melee weapon is found, it will equip the most damaging range weapon. This function should only ever be called in the EndOfCombatRound scripts, because otherwise it would have to stop the combat round to run simulation.</remarks>
        public static void ActionEquipMostDamagingMelee(uint oVersus = OBJECT_INVALID, bool bOffHand = false)
        {
            global::NWN.Core.NWScript.ActionEquipMostDamagingMelee(oVersus, bOffHand ? 1 : 0);
        }

        /// <summary>
        /// Makes the creature equip the range weapon in its possession that can do the most damage.
        /// </summary>
        /// <param name="oVersus">You can try to get the most damaging weapon against this target (default: OBJECT_INVALID)</param>
        /// <remarks>If no valid range weapon can be found, it will equip the most damaging melee weapon.</remarks>
        public static void ActionEquipMostDamagingRanged(uint oVersus = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.ActionEquipMostDamagingRanged(oVersus);
        }

        /// <summary>
        /// Gives the specified amount of experience points to the creature.
        /// </summary>
        /// <param name="oCreature">The creature to give experience to</param>
        /// <param name="nXpAmount">The amount of experience points to give</param>
        public static void GiveXPToCreature(uint oCreature, int nXpAmount)
        {
            global::NWN.Core.NWScript.GiveXPToCreature(oCreature, nXpAmount);
        }

        /// <summary>
        /// Sets the creature's experience to the specified amount.
        /// </summary>
        /// <param name="oCreature">The creature to set the experience for</param>
        /// <param name="nXpAmount">The amount of experience points to set</param>
        public static void SetXP(uint oCreature, int nXpAmount)
        {
            global::NWN.Core.NWScript.SetXP(oCreature, nXpAmount);
        }

        /// <summary>
        /// Gets the creature's experience points.
        /// </summary>
        /// <param name="oCreature">The creature to get the experience for</param>
        /// <returns>The creature's experience points</returns>
        public static int GetXP(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetXP(oCreature);
        }

        /// <summary>
        /// Forces the action subject to move to the specified location.
        /// </summary>
        /// <param name="lDestination">The destination location to move to</param>
        /// <param name="bRun">Whether to run to the destination (default: false)</param>
        /// <param name="fTimeout">The timeout in seconds (default: 30.0f)</param>
        public static void ActionForceMoveToLocation(Location lDestination, bool bRun = false, float fTimeout = 30.0f)
        {
            global::NWN.Core.NWScript.ActionForceMoveToLocation(lDestination, bRun ? 1 : 0, fTimeout);
        }

        /// <summary>
        /// Forces the action subject to move to the specified object.
        /// </summary>
        /// <param name="oMoveTo">The object to move to</param>
        /// <param name="bRun">Whether to run to the object (default: false)</param>
        /// <param name="fRange">The range to stop at (default: 1.0f)</param>
        /// <param name="fTimeout">The timeout in seconds (default: 30.0f)</param>
        public static void ActionForceMoveToObject(uint oMoveTo, bool bRun = false, float fRange = 1.0f,
            float fTimeout = 30.0f)
        {
            global::NWN.Core.NWScript.ActionForceMoveToObject(oMoveTo, bRun ? 1 : 0, fRange, fTimeout);
        }

        /// <summary>
        /// Gets the last creature that opened the caller.
        /// </summary>
        /// <returns>The last creature that opened the caller. Returns OBJECT_INVALID if the caller is not a valid door, placeable or store</returns>
        public static uint GetLastOpenedBy()
        {
            return global::NWN.Core.NWScript.GetLastOpenedBy();
        }

        /// <summary>
        /// Determines the number of times that the creature has the specified spell memorized.
        /// </summary>
        /// <param name="nSpell">The spell to check (SPELL_* constants)</param>
        /// <param name="oCreature">The creature to check the spell for (default: OBJECT_INVALID)</param>
        /// <returns>The number of times the creature has the spell memorized</returns>
        public static int GetHasSpell(Spell nSpell, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetHasSpell((int)nSpell, oCreature);
        }

        /// <summary>
        /// Gets the gender of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the gender for</param>
        /// <returns>The creature's gender</returns>
        public static Gender GetGender(uint oCreature)
        {
            return (Gender)global::NWN.Core.NWScript.GetGender(oCreature);
        }

        /// <summary>
        /// Gets the type of disturbance that caused the caller's OnInventoryDisturbed script to fire.
        /// </summary>
        /// <returns>The type of disturbance (INVENTORY_DISTURB_* constants)</returns>
        /// <remarks>This will only work for creatures and placeables.</remarks>
        public static DisturbType GetInventoryDisturbType()
        {
            return (DisturbType)global::NWN.Core.NWScript.GetInventoryDisturbType();
        }

        /// <summary>
        /// Gets the item that caused the caller's OnInventoryDisturbed script to fire.
        /// </summary>
        /// <returns>The item that caused the disturbance. Returns OBJECT_INVALID if the caller is not a valid object</returns>
        public static uint GetInventoryDisturbItem()
        {
            return global::NWN.Core.NWScript.GetInventoryDisturbItem();
        }

        /// <summary>
        /// Determines the creature's class based on the class position.
        /// </summary>
        /// <param name="nClassPosition">The class position (1, 2, or 3)</param>
        /// <param name="oCreature">The creature to get the class for (default: OBJECT_INVALID)</param>
        /// <returns>The creature's class (CLASS_TYPE_* constants). Returns CLASS_TYPE_INVALID if the creature does not have a class in the specified position or if the creature is not valid</returns>
        /// <remarks>A creature can have up to three classes. A single-class creature will only have a value in nClassPosition=1.</remarks>
        public static ClassType GetClassByPosition(int nClassPosition, uint oCreature = OBJECT_INVALID)
        {
            return (ClassType)global::NWN.Core.NWScript.GetClassByPosition(nClassPosition, oCreature);
        }

        /// <summary>
        /// Determines the creature's class level based on the class position.
        /// </summary>
        /// <param name="nClassPosition">The class position (1, 2, or 3)</param>
        /// <param name="oCreature">The creature to get the class level for (default: OBJECT_INVALID)</param>
        /// <returns>The creature's class level. Returns 0 if the creature does not have a class in the specified position or if the creature is not valid</returns>
        /// <remarks>A creature can have up to three classes. A single-class creature will only have a value in nClassPosition=1.</remarks>
        public static int GetLevelByPosition(int nClassPosition, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetLevelByPosition(nClassPosition, oCreature);
        }

        /// <summary>
        /// Determines the levels that the creature holds in the specified class type.
        /// </summary>
        /// <param name="nClassType">The class type (CLASS_TYPE_* constants)</param>
        /// <param name="oCreature">The creature to get the class level for (default: OBJECT_INVALID)</param>
        /// <returns>The number of levels the creature has in the specified class</returns>
        public static int GetLevelByClass(ClassType nClassType, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetLevelByClass((int)nClassType, oCreature);
        }

        /// <summary>
        /// Returns the ability modifier for the specified ability.
        /// </summary>
        /// <param name="nAbility">The ability type (ABILITY_* constants)</param>
        /// <param name="oCreature">The creature to get the ability modifier for (default: OBJECT_INVALID)</param>
        /// <returns>The ability modifier for the specified ability</returns>
        public static int GetAbilityModifier(AbilityType nAbility, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetAbilityModifier((int)nAbility, oCreature);
        }

        /// <summary>
        /// Returns true if the creature is in combat.
        /// </summary>
        /// <param name="oCreature">The creature to check (default: OBJECT_INVALID)</param>
        /// <returns>True if the creature is in combat, false otherwise</returns>
        public static bool GetIsInCombat(uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsInCombat(oCreature) != 0;
        }

        /// <summary>
        /// Gives the specified amount of gold to the creature.
        /// </summary>
        /// <param name="oCreature">The creature to give gold to</param>
        /// <param name="nGP">The amount of gold to give</param>
        public static void GiveGoldToCreature(uint oCreature, int nGP)
        {
            global::NWN.Core.NWScript.GiveGoldToCreature(oCreature, nGP);
        }

        /// <summary>
        /// Gets the creature nearest to the specified location, subject to all the criteria specified.
        /// </summary>
        /// <param name="nFirstCriteriaType">The first criteria type (CREATURE_TYPE_* constants)</param>
        /// <param name="nFirstCriteriaValue">The first criteria value</param>
        /// <param name="lLocation">The location to find the nearest creature to</param>
        /// <param name="nNth">The Nth nearest creature to find (default: 1)</param>
        /// <param name="nSecondCriteriaType">The second criteria type (default: -1)</param>
        /// <param name="nSecondCriteriaValue">The second criteria value (default: -1)</param>
        /// <param name="nThirdCriteriaType">The third criteria type (default: -1)</param>
        /// <param name="nThirdCriteriaValue">The third criteria value (default: -1)</param>
        /// <returns>The nearest creature. Returns OBJECT_INVALID on error</returns>
        /// <remarks>Criteria values: CLASS_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_CLASS, SPELL_* if nFirstCriteriaType was CREATURE_TYPE_DOES_NOT_HAVE_SPELL_EFFECT or CREATURE_TYPE_HAS_SPELL_EFFECT, TRUE or FALSE if nFirstCriteriaType was CREATURE_TYPE_IS_ALIVE, PERCEPTION_* if nFirstCriteriaType was CREATURE_TYPE_PERCEPTION, PLAYER_CHAR_IS_PC or PLAYER_CHAR_NOT_PC if nFirstCriteriaType was CREATURE_TYPE_PLAYER_CHAR, RACIAL_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_RACIAL_TYPE, REPUTATION_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_REPUTATION. For example, to get the nearest PC, use (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC).</remarks>
        public static uint GetNearestCreatureToLocation(CreatureType nFirstCriteriaType, bool nFirstCriteriaValue,
            Location lLocation, int nNth = 1, int nSecondCriteriaType = -1, int nSecondCriteriaValue = -1,
            int nThirdCriteriaType = -1, int nThirdCriteriaValue = -1)
        {
            return global::NWN.Core.NWScript.GetNearestCreatureToLocation((int)nFirstCriteriaType, nFirstCriteriaValue ? 1 : 0, lLocation, nNth, nSecondCriteriaType, nSecondCriteriaValue, nThirdCriteriaType, nThirdCriteriaValue);
        }

        /// <summary>
        /// Gets the level at which the creature cast its last spell (or spell-like ability).
        /// </summary>
        /// <param name="oCreature">The creature to get the caster level for</param>
        /// <returns>The caster level. Returns 0 on error, or if the creature has not yet cast a spell</returns>
        public static int GetCasterLevel(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetCasterLevel(oCreature);
        }

        /// <summary>
        /// Gets the racial type of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the racial type for</param>
        /// <returns>The racial type (RACIAL_TYPE_* constants). Returns RACIAL_TYPE_INVALID if the creature is not valid</returns>
        public static RacialType GetRacialType(uint oCreature)
        {
            return (RacialType)global::NWN.Core.NWScript.GetRacialType(oCreature);
        }

        /// <summary>
        /// Gets the creature nearest to the specified target, subject to all the criteria specified.
        /// </summary>
        /// <param name="nFirstCriteriaType">The first criteria type (CREATURE_TYPE_* constants)</param>
        /// <param name="nFirstCriteriaValue">The first criteria value</param>
        /// <param name="oTarget">The target to find the nearest creature to (default: OBJECT_INVALID)</param>
        /// <param name="nNth">The Nth nearest creature to find (default: 1)</param>
        /// <param name="nSecondCriteriaType">The second criteria type (default: -1)</param>
        /// <param name="nSecondCriteriaValue">The second criteria value (default: -1)</param>
        /// <param name="nThirdCriteriaType">The third criteria type (default: -1)</param>
        /// <param name="nThirdCriteriaValue">The third criteria value (default: -1)</param>
        /// <returns>The nearest creature. Returns OBJECT_INVALID on error</returns>
        /// <remarks>Criteria values: CLASS_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_CLASS, SPELL_* if nFirstCriteriaType was CREATURE_TYPE_DOES_NOT_HAVE_SPELL_EFFECT or CREATURE_TYPE_HAS_SPELL_EFFECT, TRUE or FALSE if nFirstCriteriaType was CREATURE_TYPE_IS_ALIVE, PERCEPTION_* if nFirstCriteriaType was CREATURE_TYPE_PERCEPTION, PLAYER_CHAR_IS_PC or PLAYER_CHAR_NOT_PC if nFirstCriteriaType was CREATURE_TYPE_PLAYER_CHAR, RACIAL_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_RACIAL_TYPE, REPUTATION_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_REPUTATION. For example, to get the nearest PC, use: (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC).</remarks>
        public static uint GetNearestCreature(CreatureType nFirstCriteriaType, int nFirstCriteriaValue,
            uint oTarget = OBJECT_INVALID, int nNth = 1, int nSecondCriteriaType = -1, int nSecondCriteriaValue = -1,
            int nThirdCriteriaType = -1, int nThirdCriteriaValue = -1)
        {
            return global::NWN.Core.NWScript.GetNearestCreature((int)nFirstCriteriaType, nFirstCriteriaValue, oTarget, nNth, nSecondCriteriaType, nSecondCriteriaValue, nThirdCriteriaType, nThirdCriteriaValue);
        }

        /// <summary>
        /// Gets the ability score of the specified type for a creature.
        /// </summary>
        /// <param name="oCreature">The creature whose ability score to find out</param>
        /// <param name="nAbilityType">The ability type (ABILITY_* constants)</param>
        /// <param name="nBaseAbilityScore">If set to true, will return the base ability score without bonuses (e.g., ability bonuses granted from equipped items) (default: false)</param>
        /// <returns>The ability score. Returns 0 on error</returns>
        public static int GetAbilityScore(uint oCreature, AbilityType nAbilityType, bool nBaseAbilityScore = false)
        {
            return global::NWN.Core.NWScript.GetAbilityScore(oCreature, (int)nAbilityType, nBaseAbilityScore ? 1 : 0);
        }

        /// <summary>
        /// Returns true if the creature is a dead NPC, dead PC or a dying PC.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>True if the creature is dead or dying, false otherwise</returns>
        public static bool GetIsDead(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetIsDead(oCreature) != 0;
        }

        /// <summary>
        /// Gets the number of hit dice for the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the hit dice for</param>
        /// <returns>The number of hit dice. Returns 0 if the creature is not valid</returns>
        public static int GetHitDice(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetHitDice(oCreature);
        }

        /// <summary>
        /// Gets the creature that is going to attack the specified target.
        /// </summary>
        /// <param name="oTarget">The target to check</param>
        /// <returns>The creature that is going to attack the target. Returns OBJECT_INVALID if the target is not a valid creature</returns>
        /// <remarks>This value is cleared out at the end of every combat round and should not be used in any case except when getting a "going to be attacked" shout from the master creature (and this creature is a henchman).</remarks>
        public static uint GetGoingToBeAttackedBy(uint oTarget)
        {
            return global::NWN.Core.NWScript.GetGoingToBeAttackedBy(oTarget);
        }

        /// <summary>
        /// Returns true if the creature is a Player Controlled character.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>True if the creature is a PC, false otherwise</returns>
        public static bool GetIsPC(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetIsPC(oCreature) != 0;
        }

        /// <summary>
        /// Returns true if the creature has immunity of the specified type.
        /// </summary>
        /// <param name="oCreature">The creature to check immunity for</param>
        /// <param name="nImmunityType">The immunity type (IMMUNITY_TYPE_* constants)</param>
        /// <param name="oVersus">If specified, also checks for the race and alignment of this target (default: OBJECT_INVALID)</param>
        /// <returns>True if the creature has immunity of the specified type</returns>
        public static bool GetIsImmune(uint oCreature, ImmunityType nImmunityType, uint oVersus = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetIsImmune(oCreature, (int)nImmunityType, oVersus) == 1;
        }

        /// <summary>
        /// Determines whether the creature has the specified feat and it is usable.
        /// </summary>
        /// <param name="nFeat">The feat to check (FEAT_* constants)</param>
        /// <param name="oCreature">The creature to check the feat for (default: OBJECT_INVALID)</param>
        /// <returns>True if the creature has the feat and it is usable, false otherwise</returns>
        public static bool GetHasFeat(FeatType nFeat, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetHasFeat((int)nFeat, oCreature) != 0;
        }

        /// <summary>
        /// Determines whether the creature has the specified skill and it is usable.
        /// </summary>
        /// <param name="nSkill">The skill to check (SKILL_* constants)</param>
        /// <param name="oCreature">The creature to check the skill for (default: OBJECT_INVALID)</param>
        /// <returns>True if the creature has the skill and it is usable, false otherwise</returns>
        public static bool GetHasSkill(NWNSkillType nSkill, uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetHasSkill((int)nSkill, oCreature) != 0;
        }

        /// <summary>
        /// Determines whether the source sees the target.
        /// </summary>
        /// <param name="oTarget">The target to check visibility for</param>
        /// <param name="oSource">The source to check visibility from (default: OBJECT_INVALID)</param>
        /// <returns>True if the source sees the target, false otherwise</returns>
        /// <remarks>This only works on creatures, as visibility lists are not maintained for non-creature objects.</remarks>
        public static bool GetObjectSeen(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetObjectSeen(oTarget, oSource) != 0;
        }

        /// <summary>
        /// Determines whether the source hears the target.
        /// </summary>
        /// <param name="oTarget">The target to check hearing for</param>
        /// <param name="oSource">The source to check hearing from (default: OBJECT_INVALID)</param>
        /// <returns>True if the source hears the target, false otherwise</returns>
        /// <remarks>This only works on creatures, as visibility lists are not maintained for non-creature objects.</remarks>
        public static bool GetObjectHeard(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetObjectHeard(oTarget, oSource) != 0;
        }

        /// <summary>
        /// Returns true if the creature is of a playable racial type.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>True if the creature is of a playable racial type, false otherwise</returns>
        public static bool GetIsPlayableRacialType(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetIsPlayableRacialType(oCreature) != 0;
        }

        /// <summary>
        /// Gets the number of ranks that the target has in the specified skill.
        /// </summary>
        /// <param name="nSkill">The skill to check (SKILL_* constants)</param>
        /// <param name="oTarget">The target to get the skill rank for (default: OBJECT_INVALID)</param>
        /// <param name="nBaseSkillRank">If set to true, returns the number of base skill ranks the target has (i.e., not including any bonuses from ability scores, feats, etc.) (default: false)</param>
        /// <returns>The number of skill ranks. Returns -1 if the target doesn't have the skill, 0 if the skill is untrained</returns>
        public static int GetSkillRank(NWNSkillType nSkill, uint oTarget = OBJECT_INVALID, bool nBaseSkillRank = false)
        {
            return global::NWN.Core.NWScript.GetSkillRank((int)nSkill, oTarget, nBaseSkillRank ? 1 : 0);
        }

        /// <summary>
        /// Gets the attack target of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the attack target for (default: OBJECT_INVALID)</param>
        /// <returns>The attack target of the creature</returns>
        /// <remarks>This only works when the creature is in combat.</remarks>
        public static uint GetAttackTarget(uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetAttackTarget(oCreature);
        }

        /// <summary>
        /// Gets the attack type of the creature's last attack.
        /// </summary>
        /// <param name="oCreature">The creature to get the last attack type for (default: OBJECT_INVALID)</param>
        /// <returns>The attack type (SPECIAL_ATTACK_* constants)</returns>
        /// <remarks>This only works when the creature is in combat.</remarks>
        public static SpecialAttack GetLastAttackType(uint oCreature = OBJECT_INVALID)
        {
            return (SpecialAttack)global::NWN.Core.NWScript.GetLastAttackType(oCreature);
        }

        /// <summary>
        /// Sets the gender of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to set the gender for</param>
        /// <param name="nGender">The gender to set (GENDER_* constants)</param>
        public static void SetGender(uint oCreature, Gender nGender)
        {
            global::NWN.Core.NWScript.SetGender(oCreature, (int)nGender);
        }

        /// <summary>
        /// Gets the soundset of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the soundset for</param>
        /// <returns>The soundset. Returns -1 on error</returns>
        public static int GetSoundset(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetSoundset(oCreature);
        }

        /// <summary>
        /// Sets the soundset of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to set the soundset for</param>
        /// <param name="nSoundset">The soundset to set (see soundset.2da for possible values)</param>
        public static void SetSoundset(uint oCreature, int nSoundset)
        {
            global::NWN.Core.NWScript.SetSoundset(oCreature, nSoundset);
        }

        /// <summary>
        /// Readies a spell level for the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to ready the spell level for</param>
        /// <param name="nSpellLevel">The spell level to ready (0-9)</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant or CLASS_TYPE_INVALID to ready the spell level for all classes (default: ClassType.Invalid)</param>
        public static void ReadySpellLevel(uint oCreature, int nSpellLevel, ClassType nClassType = ClassType.Invalid)
        {
            global::NWN.Core.NWScript.ReadySpellLevel(oCreature, nSpellLevel, (int)nClassType);
        }

        /// <summary>
        /// Makes the creature controllable by the specified player, if player party control is enabled.
        /// </summary>
        /// <param name="oCreature">The creature to set the commanding player for</param>
        /// <param name="oPlayer">The player to make the creature controllable by. Setting to OBJECT_INVALID removes the override and reverts to regular party control behavior</param>
        /// <remarks>A creature is only controllable by one player, so if you set oPlayer to a non-Player object (e.g., the module) it will disable regular party control for this creature.</remarks>
        public static void SetCommandingPlayer(uint oCreature, uint oPlayer)
        {
            global::NWN.Core.NWScript.SetCommandingPlayer(oCreature, oPlayer);
        }

        /// <summary>
        /// Gets the current discoverability mask of the specified object.
        /// </summary>
        /// <param name="oObject">The object to get the discovery mask for</param>
        /// <returns>The discoverability mask. Returns -1 if the object cannot have a discovery mask</returns>
        public static int GetObjectUiDiscoveryMask(uint oObject)
        {
            return global::NWN.Core.NWScript.GetObjectUiDiscoveryMask(oObject);
        }

        /// <summary>
        /// Sets the discoverability mask on the specified object.
        /// </summary>
        /// <param name="oObject">The object to set the discovery mask for</param>
        /// <param name="nMask">A mask of OBJECT_UI_DISCOVERY_MODE_* constants (default: ObjectUIDiscoveryType.Default)</param>
        /// <remarks>This allows toggling areahilite (TAB key by default) and mouseover discovery in the area view. Will currently only work on Creatures, Doors (Hilite only), Items and Useable Placeables. Does not affect inventory items.</remarks>
        public static void SetObjectUiDiscoveryMask(uint oObject, ObjectUIDiscoveryType nMask = ObjectUIDiscoveryType.Default)
        {
            global::NWN.Core.NWScript.SetObjectUiDiscoveryMask(oObject, (int)nMask);
        }

        /// <summary>
        /// Sets a text override for the mouseover/tab-highlight text bubble of the specified object.
        /// </summary>
        /// <param name="oObject">The object to set the text bubble override for</param>
        /// <param name="nMode">One of OBJECT_UI_TEXT_BUBBLE_OVERRIDE_* constants</param>
        /// <param name="sText">The text to display in the bubble</param>
        /// <remarks>Will currently only work on Creatures, Items and Useable Placeables.</remarks>
        public static void SetObjectTextBubbleOverride(uint oObject, ObjectUITextBubbleOverrideType nMode, string sText)
        {
            global::NWN.Core.NWScript.SetObjectTextBubbleOverride(oObject, (int)nMode, sText);
        }
    }
}