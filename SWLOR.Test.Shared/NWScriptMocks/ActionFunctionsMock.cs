using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Test.Shared.NWScriptMocks
{
    public partial class NWScriptServiceMock
    {
        // Mock data storage for actions
        private readonly Dictionary<uint, List<ActionRecord>> _actionQueues = new();
        private readonly List<DelayedAction> _delayedActions = new();
        private readonly List<ActionRecord> _actionHistory = new();
        private readonly Dictionary<uint, bool> _combatStates = new();
        private readonly Dictionary<uint, FollowData> _followData = new();
        private readonly Dictionary<uint, ConversationData> _conversationData = new();

        private class ActionRecord
        {
            public ActionType Type { get; set; }
            public uint Target { get; set; }
            public Location Destination { get; set; }
            public string Data { get; set; } = "";
            public float Value1 { get; set; }
            public float Value2 { get; set; }
            public float Value3 { get; set; }
            public bool Bool1 { get; set; }
            public bool Bool2 { get; set; }
            public bool Bool3 { get; set; }
            public int Int1 { get; set; }
            public int Int2 { get; set; }
            public int Int3 { get; set; }
        }

        private class DelayedAction
        {
            public float DelaySeconds { get; set; }
            public ActionRecord Action { get; set; }
            public float RemainingTime { get; set; }
        }

        private class FollowData
        {
            public uint Target { get; set; }
            public float Distance { get; set; }
        }

        private class ConversationData
        {
            public uint Target { get; set; }
            public string DialogResRef { get; set; } = "";
            public bool IsPrivate { get; set; }
            public bool PlayHello { get; set; }
            public bool IsPaused { get; set; }
        }

        public void AssignCommand(uint oActionSubject, Action aActionToAssign) 
        {
            if (!_actionQueues.ContainsKey(oActionSubject))
                _actionQueues[oActionSubject] = new List<ActionRecord>();
            
            // Note: Action delegate can't be easily stored, so we'll track the assignment
            _actionHistory.Add(new ActionRecord 
            { 
                Type = ActionType.MoveToPoint, 
                Target = oActionSubject 
            });
        }

        public void DelayCommand(float fSeconds, Action aActionToDelay) 
        {
            _delayedActions.Add(new DelayedAction
            {
                DelaySeconds = fSeconds,
                RemainingTime = fSeconds,
                Action = new ActionRecord { Type = ActionType.Wait }
            });
        }

        public void ActionDoCommand(Action aActionToDo) 
        {
            _actionHistory.Add(new ActionRecord { Type = ActionType.MoveToPoint });
        }

        public void ClearAllActions(bool nClearCombatState = false, uint oObject = OBJECT_INVALID) 
        {
            if (oObject == OBJECT_INVALID)
            {
                _actionQueues.Clear();
                if (nClearCombatState)
                    _combatStates.Clear();
            }
            else
            {
                _actionQueues.Remove(oObject);
                if (nClearCombatState)
                    _combatStates.Remove(oObject);
            }
        }

        public void ActionRandomWalk() 
        {
            _actionHistory.Add(new ActionRecord { Type = ActionType.RandomWalk });
        }

        public void ActionMoveToLocation(Location lDestination, bool bRun = false) 
        {
            _actionHistory.Add(new ActionRecord 
            { 
                Type = ActionType.MoveToPoint, 
                Destination = lDestination, 
                Bool1 = bRun 
            });
        }

        public void ActionMoveToObject(uint oMoveTo, bool bRun = false, float fRange = 1.0f) 
        {
            _actionHistory.Add(new ActionRecord 
            { 
                Type = ActionType.MoveToPoint, 
                Target = oMoveTo, 
                Bool1 = bRun, 
                Value1 = fRange 
            });
        }

        public void ActionMoveAwayFromObject(uint oFleeFrom, bool bRun = false, float fMoveAwayRange = 40.0f) 
        {
            _actionHistory.Add(new ActionRecord 
            { 
                Type = ActionType.MoveToPoint, 
                Target = oFleeFrom, 
                Bool1 = bRun, 
                Value1 = fMoveAwayRange 
            });
        }

        public void ActionPlayAnimation(AnimationType nAnimation, float fSpeed = 1.0f, float fDurationSeconds = 0.0f) 
        {
            _actionHistory.Add(new ActionRecord 
            { 
                Type = ActionType.MoveToPoint, 
                Int1 = (int)nAnimation, 
                Value1 = fSpeed, 
                Value2 = fDurationSeconds 
            });
        }

        public void ActionCastSpellAtObject(SpellType nSpell, uint oTarget, MetaMagicType nMetaMagic = MetaMagicType.Any,
            bool nCheat = false, int nDomainLevel = 0,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default, bool bInstantSpell = false) 
        {
            _actionHistory.Add(new ActionRecord 
            { 
                Type = ActionType.CastSpell, 
                Target = oTarget, 
                Int1 = (int)nSpell, 
                Int2 = (int)nMetaMagic, 
                Int3 = nDomainLevel, 
                Bool1 = nCheat, 
                Bool2 = bInstantSpell 
            });
        }

        public void ActionForceFollowObject(uint oFollow, float fFollowDistance = 0.0f) 
        {
            _followData[OBJECT_SELF] = new FollowData { Target = oFollow, Distance = fFollowDistance };
            _actionHistory.Add(new ActionRecord 
            { 
                Type = ActionType.Follow, 
                Target = oFollow, 
                Value1 = fFollowDistance 
            });
        }

        public void ActionSit(uint oChair) 
        {
            _actionHistory.Add(new ActionRecord { Type = ActionType.Sit, Target = oChair });
        }

        public void ActionJumpToObject(uint oToJumpTo, bool bWalkStraightLineToPoint = true) 
        {
            _actionHistory.Add(new ActionRecord 
            { 
                Type = ActionType.MoveToPoint, 
                Target = oToJumpTo, 
                Bool1 = bWalkStraightLineToPoint 
            });
        }

        public void ActionWait(float fSeconds) 
        {
            _actionHistory.Add(new ActionRecord { Type = ActionType.Wait, Value1 = fSeconds });
        }

        public void ActionStartConversation(uint oObjectToConverseWith, string sDialogResRef = "",
            bool bPrivateConversation = true, bool bPlayHello = true) 
        {
            _conversationData[OBJECT_SELF] = new ConversationData
            {
                Target = oObjectToConverseWith,
                DialogResRef = sDialogResRef,
                IsPrivate = bPrivateConversation,
                PlayHello = bPlayHello,
                IsPaused = false
            };
            _actionHistory.Add(new ActionRecord 
            { 
                Type = ActionType.DialogObject, 
                Target = oObjectToConverseWith, 
                Data = sDialogResRef, 
                Bool1 = bPrivateConversation, 
                Bool2 = bPlayHello 
            });
        }

        public void ActionPauseConversation() 
        {
            if (_conversationData.ContainsKey(OBJECT_SELF))
                _conversationData[OBJECT_SELF].IsPaused = true;
            _actionHistory.Add(new ActionRecord { Type = ActionType.Wait });
        }

        public void ActionResumeConversation() 
        {
            if (_conversationData.ContainsKey(OBJECT_SELF))
                _conversationData[OBJECT_SELF].IsPaused = false;
            _actionHistory.Add(new ActionRecord { Type = ActionType.Wait });
        }

        public void ActionSpeakStringByStrRef(int nStrRef, TalkVolumeType nTalkVolume = TalkVolumeType.Talk) 
        {
            _actionHistory.Add(new ActionRecord 
            { 
                Type = ActionType.MoveToPoint, 
                Int1 = nStrRef, 
                Int2 = (int)nTalkVolume 
            });
        }

        public void ActionUseFeat(FeatType nFeat, uint oTarget) 
        {
            _actionHistory.Add(new ActionRecord 
            { 
                Type = ActionType.MoveToPoint, 
                Target = oTarget, 
                Int1 = (int)nFeat 
            });
        }

        public void ActionUseSkill(NWNSkillType nSkill, uint oTarget, SubSkillType nSubSkill = SubSkillType.None,
            uint oItemUsed = OBJECT_INVALID) 
        {
            _actionHistory.Add(new ActionRecord 
            { 
                Type = ActionType.MoveToPoint, 
                Target = oTarget, 
                Int1 = (int)nSkill, 
                Int2 = (int)nSubSkill, 
                Int3 = (int)oItemUsed 
            });
        }

        public void ActionUseTalentOnObject(Talent tChosenTalent, uint oTarget) 
        {
            _actionHistory.Add(new ActionRecord 
            { 
                Type = ActionType.MoveToPoint, 
                Target = oTarget, 
                Data = tChosenTalent.ToString() 
            });
        }

        public void ActionUseTalentAtLocation(Talent tChosenTalent, Location lTargetLocation) 
        {
            _actionHistory.Add(new ActionRecord 
            { 
                Type = ActionType.MoveToPoint, 
                Destination = lTargetLocation, 
                Data = tChosenTalent.ToString() 
            });
        }

        public void JumpToLocation(Location lDestination) 
        {
            _actionHistory.Add(new ActionRecord 
            { 
                Type = ActionType.MoveToPoint, 
                Destination = lDestination 
            });
        }

        public void ActionUseItemOnObject(uint oItem, IntPtr ip, uint oTarget, int nSubPropertyIndex = 0, bool bDecrementCharges = true) 
        {
            _actionHistory.Add(new ActionRecord 
            { 
                Type = ActionType.UseObject, 
                Target = oTarget, 
                Int1 = (int)oItem, 
                Int2 = nSubPropertyIndex, 
                Bool1 = bDecrementCharges 
            });
        }

        public void ActionUseItemAtLocation(uint oItem, IntPtr ip, IntPtr lTarget, int nSubPropertyIndex = 0, bool bDecrementCharges = true) 
        {
            _actionHistory.Add(new ActionRecord 
            { 
                Type = ActionType.UseObject, 
                Int1 = (int)oItem, 
                Int2 = nSubPropertyIndex, 
                Bool1 = bDecrementCharges 
            });
        }

        // Action methods
        public void ActionExamine(uint oExamine) 
        { 
            // Mock implementation - no-op for testing
        }

        // Helper methods for testing
    }
}
