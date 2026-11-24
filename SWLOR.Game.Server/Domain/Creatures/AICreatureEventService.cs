using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The creature event entry points: heartbeat, combat round end, conversation, attacked, damaged, death, disturbed,
    /// spawn, blocked and the DM AI toggle. Moved here unchanged from <see cref="AIService"/>; each calls the core, the leash
    /// and the decision engine through <see cref="IAIService"/>.
    /// </summary>
    public class AICreatureEventService : IAICreatureEventService
    {
        private readonly IActivityService _activity;
        private readonly IAIService _ai;
        private readonly IConversationMenuService _conversationMenu;
        private readonly IConversationService _conversation;
        private readonly IEnmityService _enmity;
        private readonly ICreatureStatService _stat;

        public AICreatureEventService(
            IActivityService activity,
            IAIService ai,
            IConversationMenuService conversationMenu,
            IConversationService conversation,
            IEnmityService enmity,
            ICreatureStatService stat)
        {
            _activity = activity;
            _ai = ai;
            _conversationMenu = conversationMenu;
            _conversation = conversation;
            _enmity = enmity;
            _stat = stat;
        }

        [NWNEventHandler(ScriptName.OnDMToggleAIAfter)]
        public void DMToggleAIAfter()
        {
            var creature = StringToObject(EventsPlugin.GetEventData("OBJECT"));
            if (!GetIsObjectValid(creature) ||
                GetObjectType(creature) != ObjectType.Creature)
            {
                return;
            }

            if (!CreatureAIRules.IsCreatureAIEnabled(creature))
            {
                _ai.ClearAIState(creature);
            }
        }

        /// <summary>
        /// Entry point for creature heartbeat logic.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureHeartbeatAfter)]
        public void CreatureHeartbeat()
        {
            if (!CreatureAIRules.IsCreatureAIEnabled(OBJECT_SELF))
                return;

            RestoreNPCStats(true);
            _ai.ProcessFlags();
            AttackHighestEnmityTarget(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature combat round end logic.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureRoundEndAfter)]
        public void CreatureCombatRoundEnd()
        {
            var creature = OBJECT_SELF;
            if (!CreatureAIRules.IsCreatureAIEnabled(creature))
                return;

            var handled = false;
            if (!_activity.IsBusy(creature))
            {
                if (_ai.TryStartLeashEvade(creature, GetHighestEnmityTarget(creature)))
                    return;

                handled = _ai.ProcessTrigger(creature, AITriggerType.CombatRound);
            }

            if (!handled)
                AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Entry point for creature conversation logic.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureConversationAfter)]
        public void CreatureConversation()
        {
            var owner = OBJECT_SELF;
            var conversation = GetLocalString(owner, "CONVERSATION");
            if (!string.IsNullOrWhiteSpace(conversation))
            {
                var talker = GetLastSpeaker();
                if (_conversation.TryGetGraph(conversation, out _))
                    _conversation.Start(talker, owner, conversation);
                else if (!_conversationMenu.TryStart(talker, owner, conversation))
                    AssignCommand(talker, () => ActionStartConversation(owner, conversation, true, false));
            }
        }

        /// <summary>
        /// Entry point for creature physical attacked logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureAttackAfter)]
        public void CreaturePhysicalAttacked()
        {
            var creature = OBJECT_SELF;
            if (!CreatureAIRules.IsCreatureAIEnabled(creature))
                return;

            if (CreatureAIRules.IsLeashEvading(creature))
                return;

            if (_ai.TryStartLeashEvade(creature, GetHighestOrEventTarget(creature, GetLastAttacker(creature))))
                return;

            if (!_ai.ProcessTrigger(creature, AITriggerType.Attacked, GetLastAttacker(creature)))
                AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Entry point for creature damaged logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
        public void CreatureDamaged()
        {
            var creature = OBJECT_SELF;
            if (!CreatureAIRules.IsCreatureAIEnabled(creature))
                return;

            if (CreatureAIRules.IsLeashEvading(creature))
                return;

            if (_ai.TryStartLeashEvade(creature, GetHighestOrEventTarget(creature, GetLastDamager(creature))))
                return;

            if (!_ai.ProcessTrigger(creature, AITriggerType.Damaged, GetLastDamager(creature)))
                AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Entry point for creature death logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public void CreatureDeath()
        {
            _ai.ProcessTrigger(OBJECT_SELF, AITriggerType.Death);
            _ai.RemoveFromAlliesCache();
        }

        /// <summary>
        /// Entry point for creature disturbed logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDisturbedAfter)]
        public void CreatureDisturbed()
        {
            var creature = OBJECT_SELF;
            if (!CreatureAIRules.IsCreatureAIEnabled(creature))
                return;

            if (!_ai.ProcessTrigger(creature, AITriggerType.Disturbed))
                AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Entry point for creature spawn logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureSpawnAfter)]
        public void CreatureSpawn()
        {
            var creature = OBJECT_SELF;

            SetLocalString(creature, "X2_SPECIAL_COMBAT_AI_SCRIPT", "xxx");
            LoadNPCStats();
            ApplyCreatureMovementRate(creature);
            LoadAggroEffect();
            DoVFX();
            SetLocalLocation(creature, "HOME_LOCATION", GetLocation(creature));
            _ai.ProcessTrigger(creature, AITriggerType.Spawn);
        }

        /// <summary>
        /// Entry point for creature rested logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureRestedAfter)]
        public void CreatureRested()
        {
        }

        /// <summary>
        /// Entry point for creature spell cast at logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureSpellCastAfter)]
        public void CreatureSpellCastAt()
        {
        }

        /// <summary>
        /// Entry point for creature user defined logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureUserDefinedAfter)]
        public void CreatureUserDefined()
        {
        }

        /// <summary>
        /// Entry point for creature blocked logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureBlockedAfter)]
        public void CreatureBlocked()
        {
            var creature = OBJECT_SELF;
            var blocker = GetBlockingDoor();
            if (!CreatureAIRules.IsCreatureAIEnabled(creature) ||
                CreatureAIRules.IsLeashEvading(creature) ||
                GetObjectType(blocker) != ObjectType.Creature)
            {
                return;
            }

            ResumeAttackAfterActionsCleared(creature);
        }

        /// <summary>
        /// When the creature spawns, add an AOE effect to the creature which will be used to process aggro ranges.
        /// </summary>
        private void LoadAggroEffect()
        {
            var effect = SupernaturalEffect(EffectAreaOfEffect(AreaOfEffect.CustomAoe, "crea_aggro_enter", string.Empty, "crea_aggro_exit"));
            effect = TagEffect(effect, "AGGRO_AOE");
            ApplyEffectToObject(DurationType.Permanent, effect, OBJECT_SELF);
        }

        private void DoVFX()
        {
            // Allow builders to put permanent effects on creatures - e.g. to make them statues, or make them glow.
            // Index of standard VFX effects here: https://nwnlexicon.com/index.php?title=Vfx_dur
            var vfx = GetLocalInt(OBJECT_SELF, "PERMANENT_VFX_ID");
            if (vfx > 0)
                ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect((VisualEffect)vfx), OBJECT_SELF);

            // Cutscene paralysis - for statues.
            var paralyze = GetLocalInt(OBJECT_SELF, "PARALYZE");
            if (paralyze > 0)
                ApplyEffectToObject(DurationType.Permanent, SupernaturalEffect(EffectCutsceneParalyze()), OBJECT_SELF);

            // Daze - for creatures that should not be able to attack.
            var daze = GetLocalInt(OBJECT_SELF, "DAZE");
            if (daze > 0)
                ApplyEffectToObject(DurationType.Permanent, SupernaturalEffect(EffectDazed()), OBJECT_SELF);
        }

        private uint GetHighestOrEventTarget(uint creature, uint eventTarget)
        {
            var highestEnmityTarget = GetHighestEnmityTarget(creature);
            return GetIsObjectValid(highestEnmityTarget)
                ? highestEnmityTarget
                : eventTarget;
        }

        // ---- Seams into services that are not converted yet ----
        // Each protected virtual method below has the original call as its body, so the server behaves as it did.

        protected virtual uint GetHighestEnmityTarget(uint enemy) => _enmity.GetHighestEnmityTarget(enemy);

        protected virtual void AttackHighestEnmityTarget(uint creature) => _enmity.AttackHighestEnmityTarget(creature);

        protected virtual void ResumeAttackAfterActionsCleared(uint creature) => _enmity.ResumeAttackAfterActionsCleared(creature);

        protected virtual void RestoreNPCStats(bool outOfCombatRegen) => _stat.RestoreNPCStats(outOfCombatRegen, _enmity);

        protected virtual void LoadNPCStats() => _stat.LoadNPCStats();

        protected virtual void ApplyCreatureMovementRate(uint creature) => _stat.ApplyCreatureMovementRate(creature);
    }
}
