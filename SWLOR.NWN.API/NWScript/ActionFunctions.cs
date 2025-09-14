using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Assign aActionToAssign to oActionSubject.
        ///   * No return value, but if an error occurs, the log file will contain
        ///   "AssignCommand failed."
        ///   (If the object doesn't exist, nothing happens.)
        /// </summary>
        public static void AssignCommand(uint oActionSubject, Action aActionToAssign)
        {
            global::NWN.Core.NWScript.AssignCommand(oActionSubject, aActionToAssign);
        }

        /// <summary>
        ///   Delay aActionToDelay by fSeconds.
        ///   * No return value, but if an error occurs, the log file will contain
        ///   "DelayCommand failed.".
        ///   It is suggested that functions which create effects should not be used
        ///   as parameters to delayed actions.  Instead, the effect should be created in the
        ///   script and then passed into the action.  For example:
        ///   effect eDamage = EffectDamage(nDamage, DAMAGE_TYPE_MAGICAL);
        ///   DelayCommand(fDelay, ApplyEffectToObject(DURATION_TYPE_INSTANT, eDamage, oTarget);
        /// </summary>
        public static void DelayCommand(float fSeconds, Action aActionToDelay)
        {
            global::NWN.Core.NWScript.DelayCommand(fSeconds, aActionToDelay);
        }

        /// <summary>
        ///   Do aActionToDo.
        /// </summary>
        public static void ActionDoCommand(Action aActionToDo)
        {
            global::NWN.Core.NWScript.ActionDoCommand(aActionToDo);
        }

        /// <summary>
        ///   Clear all the actions of the caller.
        ///   * No return value, but if an error occurs, the log file will contain
        ///   "ClearAllActions failed.".
        ///   - nClearCombatState: if true, this will immediately clear the combat state
        ///   on a creature, which will stop the combat music and allow them to rest,
        ///   engage in dialog, or other actions that they would normally have to wait for.
        /// </summary>
        public static void ClearAllActions(bool nClearCombatState = false)
        {
            global::NWN.Core.NWScript.ClearAllActions(nClearCombatState ? 1 : 0);
        }

        /// <summary>
        ///   The action subject will generate a random location near its current location
        ///   and pathfind to it.  ActionRandomwalk never ends, which means it is neccessary
        ///   to call ClearAllActions in order to allow a creature to perform any other action
        ///   once ActionRandomWalk has been called.
        ///   * No return value, but if an error occurs the log file will contain
        ///   "ActionRandomWalk failed."
        /// </summary>
        public static void ActionRandomWalk()
        {
            global::NWN.Core.NWScript.ActionRandomWalk();
        }

        /// <summary>
        ///   The action subject will move to lDestination.
        ///   - lDestination: The object will move to this location.  If the location is
        ///   invalid or a path cannot be found to it, the command does nothing.
        ///   - bRun: If this is TRUE, the action subject will run rather than walk
        ///   * No return value, but if an error occurs the log file will contain
        ///   "MoveToPoint failed."
        /// </summary>
        public static void ActionMoveToLocation(Location lDestination, bool bRun = false)
        {
            global::NWN.Core.NWScript.ActionMoveToLocation(lDestination, bRun ? 1 : 0);
        }

        /// <summary>
        ///   Cause the action subject to move to a certain distance from oMoveTo.
        ///   If there is no path to oMoveTo, this command will do nothing.
        ///   - oMoveTo: This is the object we wish the action subject to move to
        ///   - bRun: If this is TRUE, the action subject will run rather than walk
        ///   - fRange: This is the desired distance between the action subject and oMoveTo
        ///   * No return value, but if an error occurs the log file will contain
        ///   "ActionMoveToObject failed."
        /// </summary>
        public static void ActionMoveToObject(uint oMoveTo, bool bRun = false, float fRange = 1.0f)
        {
            global::NWN.Core.NWScript.ActionMoveToObject(oMoveTo, bRun ? 1 : 0, fRange);
        }

        /// <summary>
        ///   Cause the action subject to move to a certain distance away from oFleeFrom.
        ///   - oFleeFrom: This is the object we wish the action subject to move away from.
        ///   If oFleeFrom is not in the same area as the action subject, nothing will
        ///   happen.
        ///   - bRun: If this is TRUE, the action subject will run rather than walk
        ///   - fMoveAwayRange: This is the distance we wish the action subject to put
        ///   between themselves and oFleeFrom
        ///   * No return value, but if an error occurs the log file will contain
        ///   "ActionMoveAwayFromObject failed."
        /// </summary>
        public static void ActionMoveAwayFromObject(uint oFleeFrom, bool bRun = false, float fMoveAwayRange = 40.0f)
        {
            global::NWN.Core.NWScript.ActionMoveAwayFromObject(oFleeFrom, bRun ? 1 : 0, fMoveAwayRange);
        }

        /// <summary>
        ///   Cause the action subject to play an animation
        ///   - nAnimation: ANIMATION_*
        ///   - fSpeed: Speed of the animation
        ///   - fDurationSeconds: Duration of the animation (this is not used for Fire and
        ///   Forget animations)
        /// </summary>
        public static void ActionPlayAnimation(Animation nAnimation, float fSpeed = 1.0f, float fDurationSeconds = 0.0f)
        {
            global::NWN.Core.NWScript.ActionPlayAnimation((int)nAnimation, fSpeed, fDurationSeconds);
        }

        /// <summary>
        ///   This action casts a spell at oTarget.
        ///   - nSpell: SPELL_*
        ///   - oTarget: Target for the spell
        ///   - nMetamagic: METAMAGIC_*
        ///   - bCheat: If this is TRUE, then the executor of the action doesn't have to be
        ///   able to cast the spell.
        ///   - nDomainLevel: TBD - SS
        ///   - nProjectilePathType: PROJECTILE_PATH_TYPE_*
        ///   - bInstantSpell: If this is TRUE, the spell is cast immediately. This allows
        ///   the end-user to simulate a high-level magic-user having lots of advance
        ///   warning of impending trouble
        /// </summary>
        public static void ActionCastSpellAtObject(Spell nSpell, uint oTarget, MetaMagic nMetaMagic = MetaMagic.Any,
            bool nCheat = false, int nDomainLevel = 0,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default, bool bInstantSpell = false)
        {
            global::NWN.Core.NWScript.ActionCastSpellAtObject((int)nSpell, oTarget, (int)nMetaMagic, nCheat ? 1 : 0, nDomainLevel, (int)nProjectilePathType, bInstantSpell ? 1 : 0);
        }

        /// <summary>
        ///   The action subject will follow oFollow until a ClearAllActions() is called.
        ///   - oFollow: this is the object to be followed
        ///   - fFollowDistance: follow distance in metres
        ///   * No return value
        /// </summary>
        public static void ActionForceFollowObject(uint oFollow, float fFollowDistance = 0.0f)
        {
            global::NWN.Core.NWScript.ActionForceFollowObject(oFollow, fFollowDistance);
        }

        /// <summary>
        ///   Sit in oChair.
        ///   Note: Not all creatures will be able to sit and not all
        ///   objects can be sat on.
        ///   The object oChair must also be marked as usable in the toolset.
        ///   For Example: To get a player to sit in oChair when they click on it,
        ///   place the following script in the OnUsed event for the object oChair.
        ///   void main()
        ///   {
        ///   object oChair = OBJECT_SELF;
        ///   AssignCommand(GetLastUsedBy(),ActionSit(oChair));
        ///   }
        /// </summary>
        public static void ActionSit(uint oChair)
        {
            global::NWN.Core.NWScript.ActionSit(oChair);
        }

        /// <summary>
        ///   Jump to an object ID, or as near to it as possible.
        /// </summary>
        public static void ActionJumpToObject(uint oToJumpTo, bool bWalkStraightLineToPoint = true)
        {
            global::NWN.Core.NWScript.ActionJumpToObject(oToJumpTo, bWalkStraightLineToPoint ? 1 : 0);
        }

        /// <summary>
        ///   Do nothing for fSeconds seconds.
        /// </summary>
        public static void ActionWait(float fSeconds)
        {
            global::NWN.Core.NWScript.ActionWait(fSeconds);
        }

        /// <summary>
        ///   Starts a conversation with oObjectToConverseWith - this will cause their
        ///   OnDialog event to fire.
        ///   - oObjectToConverseWith
        ///   - sDialogResRef: If this is blank, the creature's own dialogue file will be used
        ///   - bPrivateConversation
        ///   Turn off bPlayHello if you don't want the initial greeting to play
        /// </summary>
        public static void ActionStartConversation(uint oObjectToConverseWith, string sDialogResRef = "",
            bool bPrivateConversation = true, bool bPlayHello = true)
        {
            global::NWN.Core.NWScript.ActionStartConversation(oObjectToConverseWith, sDialogResRef, bPrivateConversation ? 1 : 0, bPlayHello ? 1 : 0);
        }

        /// <summary>
        ///   Pause the current conversation.
        /// </summary>
        public static void ActionPauseConversation()
        {
            global::NWN.Core.NWScript.ActionPauseConversation();
        }

        /// <summary>
        ///   Resume a conversation after it has been paused.
        /// </summary>
        public static void ActionResumeConversation()
        {
            global::NWN.Core.NWScript.ActionResumeConversation();
        }

        /// <summary>
        ///   Causes the creature to speak a translated string.
        ///   - nStrRef: Reference of the string in the talk table
        ///   - nTalkVolume: TALKVOLUME_*
        /// </summary>
        public static void ActionSpeakStringByStrRef(int nStrRef, TalkVolume nTalkVolume = TalkVolume.Talk)
        {
            global::NWN.Core.NWScript.ActionSpeakStringByStrRef(nStrRef, (int)nTalkVolume);
        }

        /// <summary>
        ///   Use nFeat on oTarget.
        ///   - nFeat: FEAT_*
        ///   - oTarget
        /// </summary>
        public static void ActionUseFeat(FeatType nFeat, uint oTarget)
        {
            global::NWN.Core.NWScript.ActionUseFeat((int)nFeat, oTarget);
        }

        /// <summary>
        ///   Runs the action "UseSkill" on the current creature
        ///   Use nSkill on oTarget.
        ///   - nSkill: SKILL_*
        ///   - oTarget
        ///   - nSubSkill: SUBSKILL_*
        ///   - oItemUsed: Item to use in conjunction with the skill
        /// </summary>
        public static void ActionUseSkill(NWNSkillType nSkill, uint oTarget, SubSkill nSubSkill = SubSkill.None,
            uint oItemUsed = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.ActionUseSkill((int)nSkill, oTarget, (int)nSubSkill, oItemUsed);
        }

        /// <summary>
        ///   Use tChosenTalent on oTarget.
        /// </summary>
        public static void ActionUseTalentOnObject(Talent tChosenTalent, uint oTarget)
        {
            global::NWN.Core.NWScript.ActionUseTalentOnObject(tChosenTalent, oTarget);
        }

        /// <summary>
        ///   Use tChosenTalent at lTargetLocation.
        /// </summary>
        public static void ActionUseTalentAtLocation(Talent tChosenTalent, Location lTargetLocation)
        {
            global::NWN.Core.NWScript.ActionUseTalentAtLocation(tChosenTalent, lTargetLocation);
        }

        /// <summary>
        ///   Jump to lDestination.  The action is added to the TOP of the action queue.
        /// </summary>
        public static void JumpToLocation(Location lDestination)
        {
            global::NWN.Core.NWScript.JumpToLocation(lDestination);
        }

        /// <summary>
        /// Queue an action to use an active item property.
        /// * oItem - item that has the item property to use
        /// * ip - item property to use
        /// * object oTarget - target
        /// * nSubPropertyIndex - specify if your itemproperty has subproperties (such as subradial spells)
        /// * bDecrementCharges - decrement charges if item property is limited
        /// </summary>
        public static void ActionUseItemOnObject(uint oItem, IntPtr ip, uint oTarget, int nSubPropertyIndex = 0, bool bDecrementCharges = true)
        {
            global::NWN.Core.NWScript.ActionUseItemOnObject(oItem, ip, oTarget, nSubPropertyIndex, bDecrementCharges ? 1 : 0);
        }

        /// <summary>
        /// Queue an action to use an active item property.
        /// * oItem - item that has the item property to use
        /// * ip - item property to use
        /// * location lTarget - target location (must be in the same area as item possessor)
        /// * nSubPropertyIndex - specify if your itemproperty has subproperties (such as subradial spells)
        /// * bDecrementCharges - decrement charges if item property is limited
        /// </summary>
        public static void ActionUseItemAtLocation(uint oItem, IntPtr ip, IntPtr lTarget, int nSubPropertyIndex = 0, bool bDecrementCharges = true)
        {
            global::NWN.Core.NWScript.ActionUseItemAtLocation(oItem, ip, lTarget, nSubPropertyIndex, bDecrementCharges ? 1 : 0);
        }
    }
}
