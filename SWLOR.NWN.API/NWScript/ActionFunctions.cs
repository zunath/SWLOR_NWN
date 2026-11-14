using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Assigns an action to the specified action subject.
        /// </summary>
        /// <param name="oActionSubject">The object to assign the action to</param>
        /// <param name="aActionToAssign">The action to assign</param>
        /// <remarks>No return value, but if an error occurs, the log file will contain "AssignCommand failed." (If the object doesn't exist, nothing happens.)</remarks>
        public static void AssignCommand(uint oActionSubject, Action aActionToAssign)
        {
            NWScriptRuntime.Current.AssignCommand(oActionSubject, aActionToAssign);
        }

        /// <summary>
        /// Delays an action by the specified number of seconds.
        /// </summary>
        /// <param name="fSeconds">Number of seconds to delay the action</param>
        /// <param name="aActionToDelay">The action to delay</param>
        /// <remarks>No return value, but if an error occurs, the log file will contain "DelayCommand failed." It is suggested that functions which create effects should not be used as parameters to delayed actions. Instead, the effect should be created in the script and then passed into the action.</remarks>
        public static void DelayCommand(float fSeconds, Action aActionToDelay)
        {
            NWScriptRuntime.Current.DelayCommand(fSeconds, aActionToDelay);
        }

        /// <summary>
        /// Executes the specified action immediately.
        /// </summary>
        /// <param name="aActionToDo">The action to execute</param>
        public static void ActionDoCommand(Action aActionToDo)
        {
            NWScriptRuntime.Current.ActionDoCommand(aActionToDo);
        }

        /// <summary>
        /// Clears all actions of the target object.
        /// </summary>
        /// <param name="nClearCombatState">If true, immediately clears the combat state on a creature, stopping combat music and allowing rest, dialog, or other actions</param>
        /// <param name="oObject">The target object (defaults to OBJECT_SELF)</param>
        /// <remarks>No return value, but if an error occurs, the log file will contain "ClearAllActions failed."</remarks>
        public static void ClearAllActions(bool nClearCombatState = false, uint oObject = OBJECT_INVALID)
        {
            NWScriptRuntime.Current.ClearAllActions(nClearCombatState, oObject);
        }

        /// <summary>
        /// Makes the action subject generate a random location near its current location and pathfind to it.
        /// </summary>
        /// <remarks>ActionRandomWalk never ends, which means it is necessary to call ClearAllActions in order to allow a creature to perform any other action once ActionRandomWalk has been called. No return value, but if an error occurs the log file will contain "ActionRandomWalk failed."</remarks>
        public static void ActionRandomWalk()
        {
            NWScriptRuntime.Current.ActionRandomWalk();
        }

        /// <summary>
        /// Makes the action subject move to the specified destination.
        /// </summary>
        /// <param name="lDestination">The location to move to. If the location is invalid or a path cannot be found, the command does nothing</param>
        /// <param name="bRun">If true, the action subject will run rather than walk (default: false)</param>
        /// <remarks>No return value, but if an error occurs the log file will contain "MoveToPoint failed."</remarks>
        public static void ActionMoveToLocation(Location lDestination, bool bRun = false)
        {
            NWScriptRuntime.Current.ActionMoveToLocation(lDestination, bRun);
        }

        /// <summary>
        /// Makes the action subject move to a certain distance from the specified object.
        /// </summary>
        /// <param name="oMoveTo">The object to move to. If there is no path to this object, this command will do nothing</param>
        /// <param name="bRun">If true, the action subject will run rather than walk (default: false)</param>
        /// <param name="fRange">The desired distance between the action subject and the target object (default: 1.0)</param>
        /// <remarks>No return value, but if an error occurs the log file will contain "ActionMoveToObject failed."</remarks>
        public static void ActionMoveToObject(uint oMoveTo, bool bRun = false, float fRange = 1.0f)
        {
            NWScriptRuntime.Current.ActionMoveToObject(oMoveTo, bRun, fRange);
        }

        /// <summary>
        /// Makes the action subject move away from the specified object to a certain distance.
        /// </summary>
        /// <param name="oFleeFrom">The object to move away from. If this object is not in the same area as the action subject, nothing will happen</param>
        /// <param name="bRun">If true, the action subject will run rather than walk (default: false)</param>
        /// <param name="fMoveAwayRange">The distance to put between the action subject and the target object (default: 40.0)</param>
        /// <remarks>No return value, but if an error occurs the log file will contain "ActionMoveAwayFromObject failed."</remarks>
        public static void ActionMoveAwayFromObject(uint oFleeFrom, bool bRun = false, float fMoveAwayRange = 40.0f)
        {
            NWScriptRuntime.Current.ActionMoveAwayFromObject(oFleeFrom, bRun, fMoveAwayRange);
        }

        /// <summary>
        /// Makes the action subject play the specified animation.
        /// </summary>
        /// <param name="nAnimation">The animation to play (ANIMATION_* constant)</param>
        /// <param name="fSpeed">Speed of the animation (default: 1.0)</param>
        /// <param name="fDurationSeconds">Duration of the animation in seconds. This is not used for Fire and Forget animations (default: 0.0)</param>
        public static void ActionPlayAnimation(Animation nAnimation, float fSpeed = 1.0f, float fDurationSeconds = 0.0f)
        {
            NWScriptRuntime.Current.ActionPlayAnimation(nAnimation, fSpeed, fDurationSeconds);
        }

        /// <summary>
        /// Makes the action subject cast a spell at the specified target.
        /// </summary>
        /// <param name="nSpell">The spell to cast (SPELL_* constant)</param>
        /// <param name="oTarget">The target for the spell</param>
        /// <param name="nMetaMagic">The metamagic to apply (default: MetaMagic.Any)</param>
        /// <param name="nCheat">If true, the executor doesn't need to be able to cast the spell (default: false)</param>
        /// <param name="nDomainLevel">Domain level (default: 0)</param>
        /// <param name="nProjectilePathType">The projectile path type (default: ProjectilePathType.Default)</param>
        /// <param name="bInstantSpell">If true, the spell is cast immediately, allowing simulation of high-level magic users with advance warning (default: false)</param>
        public static void ActionCastSpellAtObject(Spell nSpell, uint oTarget, MetaMagic nMetaMagic = MetaMagic.Any,
            bool nCheat = false, int nDomainLevel = 0,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default, bool bInstantSpell = false)
        {
            NWScriptRuntime.Current.ActionCastSpellAtObject(nSpell, oTarget, nMetaMagic, nCheat, nDomainLevel, nProjectilePathType, bInstantSpell);
        }

        /// <summary>
        /// Makes the action subject follow the specified object until ClearAllActions() is called.
        /// </summary>
        /// <param name="oFollow">The object to follow</param>
        /// <param name="fFollowDistance">Follow distance in meters (default: 0.0)</param>
        public static void ActionForceFollowObject(uint oFollow, float fFollowDistance = 0.0f)
        {
            NWScriptRuntime.Current.ActionForceFollowObject(oFollow, fFollowDistance);
        }

        /// <summary>
        /// Makes the action subject sit in the specified chair.
        /// </summary>
        /// <param name="oChair">The chair to sit in. The object must be marked as usable in the toolset</param>
        /// <remarks>Not all creatures will be able to sit and not all objects can be sat on. To get a player to sit when they click on a chair, place the following script in the OnUsed event: void main() { object oChair = OBJECT_SELF; AssignCommand(GetLastUsedBy(),ActionSit(oChair)); }</remarks>
        public static void ActionSit(uint oChair)
        {
            NWScriptRuntime.Current.ActionSit(oChair);
        }

        /// <summary>
        /// Makes the action subject jump to the specified object, or as near to it as possible.
        /// </summary>
        /// <param name="oToJumpTo">The object to jump to</param>
        /// <param name="bWalkStraightLineToPoint">If true, walks in a straight line to the point (default: true)</param>
        public static void ActionJumpToObject(uint oToJumpTo, bool bWalkStraightLineToPoint = true)
        {
            NWScriptRuntime.Current.ActionJumpToObject(oToJumpTo, bWalkStraightLineToPoint);
        }

        /// <summary>
        /// Makes the action subject wait for the specified number of seconds.
        /// </summary>
        /// <param name="fSeconds">Number of seconds to wait</param>
        public static void ActionWait(float fSeconds)
        {
            NWScriptRuntime.Current.ActionWait(fSeconds);
        }

        /// <summary>
        /// Starts a conversation with the specified object, causing their OnDialog event to fire.
        /// </summary>
        /// <param name="oObjectToConverseWith">The object to start a conversation with</param>
        /// <param name="sDialogResRef">If blank, the creature's own dialogue file will be used (default: empty string)</param>
        /// <param name="bPrivateConversation">Whether the conversation is private (default: true)</param>
        /// <param name="bPlayHello">If false, the initial greeting will not play (default: true)</param>
        public static void ActionStartConversation(uint oObjectToConverseWith, string sDialogResRef = "",
            bool bPrivateConversation = true, bool bPlayHello = true)
        {
            NWScriptRuntime.Current.ActionStartConversation(oObjectToConverseWith, sDialogResRef, bPrivateConversation, bPlayHello);
        }

        /// <summary>
        /// Pauses the current conversation.
        /// </summary>
        public static void ActionPauseConversation()
        {
            NWScriptRuntime.Current.ActionPauseConversation();
        }

        /// <summary>
        /// Resumes a conversation after it has been paused.
        /// </summary>
        public static void ActionResumeConversation()
        {
            NWScriptRuntime.Current.ActionResumeConversation();
        }

        /// <summary>
        /// Makes the creature speak a translated string.
        /// </summary>
        /// <param name="nStrRef">Reference of the string in the talk table</param>
        /// <param name="nTalkVolume">The talk volume (TALKVOLUME_* constant) (default: TalkVolume.Talk)</param>
        public static void ActionSpeakStringByStrRef(int nStrRef, TalkVolume nTalkVolume = TalkVolume.Talk)
        {
            NWScriptRuntime.Current.ActionSpeakStringByStrRef(nStrRef, nTalkVolume);
        }

        /// <summary>
        /// Makes the action subject use the specified feat on the target.
        /// </summary>
        /// <param name="nFeat">The feat to use (FEAT_* constant)</param>
        /// <param name="oTarget">The target to use the feat on</param>
        public static void ActionUseFeat(FeatType nFeat, uint oTarget)
        {
            NWScriptRuntime.Current.ActionUseFeat(nFeat, oTarget);
        }

        /// <summary>
        /// Makes the action subject use the specified skill on the target.
        /// </summary>
        /// <param name="nSkill">The skill to use (SKILL_* constant)</param>
        /// <param name="oTarget">The target to use the skill on</param>
        /// <param name="nSubSkill">The subskill to use (SUBSKILL_* constant) (default: SubSkill.None)</param>
        /// <param name="oItemUsed">Item to use in conjunction with the skill (default: OBJECT_INVALID)</param>
        public static void ActionUseSkill(NWNSkillType nSkill, uint oTarget, SubSkill nSubSkill = SubSkill.None,
            uint oItemUsed = OBJECT_INVALID)
        {
            NWScriptRuntime.Current.ActionUseSkill(nSkill, oTarget, nSubSkill, oItemUsed);
        }

        /// <summary>
        /// Makes the action subject use the specified talent on the target object.
        /// </summary>
        /// <param name="tChosenTalent">The talent to use</param>
        /// <param name="oTarget">The target object to use the talent on</param>
        public static void ActionUseTalentOnObject(Talent tChosenTalent, uint oTarget)
        {
            NWScriptRuntime.Current.ActionUseTalentOnObject(tChosenTalent, oTarget);
        }

        /// <summary>
        /// Makes the action subject use the specified talent at the target location.
        /// </summary>
        /// <param name="tChosenTalent">The talent to use</param>
        /// <param name="lTargetLocation">The target location to use the talent at</param>
        public static void ActionUseTalentAtLocation(Talent tChosenTalent, Location lTargetLocation)
        {
            NWScriptRuntime.Current.ActionUseTalentAtLocation(tChosenTalent, lTargetLocation);
        }

        /// <summary>
        /// Makes the action subject jump to the specified destination. The action is added to the top of the action queue.
        /// </summary>
        /// <param name="lDestination">The destination location to jump to</param>
        public static void JumpToLocation(Location lDestination)
        {
            NWScriptRuntime.Current.JumpToLocation(lDestination);
        }

        /// <summary>
        /// Queues an action to use an active item property on an object.
        /// </summary>
        /// <param name="oItem">The item that has the item property to use</param>
        /// <param name="ip">The item property to use</param>
        /// <param name="oTarget">The target object</param>
        /// <param name="nSubPropertyIndex">Specify if your item property has subproperties (such as subradial spells) (default: 0)</param>
        /// <param name="bDecrementCharges">Whether to decrement charges if the item property is limited (default: true)</param>
        public static void ActionUseItemOnObject(uint oItem, IntPtr ip, uint oTarget, int nSubPropertyIndex = 0, bool bDecrementCharges = true)
        {
            NWScriptRuntime.Current.ActionUseItemOnObject(oItem, ip, oTarget, nSubPropertyIndex, bDecrementCharges);
        }

        /// <summary>
        /// Queues an action to use an active item property at a location.
        /// </summary>
        /// <param name="oItem">The item that has the item property to use</param>
        /// <param name="ip">The item property to use</param>
        /// <param name="lTarget">The target location (must be in the same area as item possessor)</param>
        /// <param name="nSubPropertyIndex">Specify if your item property has subproperties (such as subradial spells) (default: 0)</param>
        /// <param name="bDecrementCharges">Whether to decrement charges if the item property is limited (default: true)</param>
        public static void ActionUseItemAtLocation(uint oItem, IntPtr ip, IntPtr lTarget, int nSubPropertyIndex = 0, bool bDecrementCharges = true)
        {
            NWScriptRuntime.Current.ActionUseItemAtLocation(oItem, ip, lTarget, nSubPropertyIndex, bDecrementCharges);
        }
    }
}
