using SWLOR.Component.AI.Contracts;
using SWLOR.Component.AI.Enums;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.AI.EventHandlers
{
    /// <summary>
    /// Event handlers for AI-related game events.
    /// This class handles the infrastructure layer of receiving game events and delegating to the AI service.
    /// </summary>
    public class AIEventHandlers
    {
        private readonly IAIService _aiService;

        public AIEventHandlers(IAIService aiService)
        {
            _aiService = aiService;
        }

        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheAIData()
        {
            _aiService.CacheAIData();
        }

        /// <summary>
        /// Entry point for creature heartbeat logic.
        /// </summary>
        [ScriptHandler<OnCreatureHeartbeatAfter>]
        public void CreatureHeartbeat()
        {
            _aiService.ProcessCreatureHeartbeat(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature perception logic.
        /// </summary>
        [ScriptHandler<OnCreaturePerceptionAfter>]
        public void CreaturePerception()
        {
            _aiService.ProcessCreaturePerception(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature combat round end logic.
        /// </summary>
        [ScriptHandler<OnCreatureRoundEndAfter>]
        public void CreatureCombatRoundEnd()
        {
            _aiService.ProcessCreatureCombatRoundEnd(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature conversation logic.
        /// </summary>
        [ScriptHandler<OnCreatureConversationAfter>]
        public void CreatureConversation()
        {
            _aiService.ProcessCreatureConversation(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature physical attacked logic
        /// </summary>
        [ScriptHandler<OnCreatureAttackAfter>]
        public void CreaturePhysicalAttacked()
        {
            _aiService.ProcessCreaturePhysicalAttacked(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature damaged logic
        /// </summary>
        [ScriptHandler<OnCreatureDamagedAfter>]
        public void CreatureDamaged()
        {
            _aiService.ProcessCreatureDamaged(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature death logic
        /// </summary>
        [ScriptHandler<OnCreatureDeathAfter>]
        public void CreatureDeath()
        {
            _aiService.ProcessCreatureDeath(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature disturbed logic
        /// </summary>
        [ScriptHandler<OnCreatureDisturbedAfter>]
        public void CreatureDisturbed()
        {
            _aiService.ProcessCreatureDisturbed(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature spawn logic
        /// </summary>
        [ScriptHandler<OnCreatureSpawnAfter>]
        public void CreatureSpawn()
        {
            _aiService.ProcessCreatureSpawn(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature rested logic
        /// </summary>
        [ScriptHandler<OnCreatureRestedAfter>]
        public void CreatureRested()
        {
            _aiService.ProcessCreatureRested(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature spell cast at logic
        /// </summary>
        [ScriptHandler<OnCreatureSpellCastAfter>]
        public void CreatureSpellCastAt()
        {
            _aiService.ProcessCreatureSpellCastAt(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature user defined logic
        /// </summary>
        [ScriptHandler<OnCreatureUserDefinedAfter>]
        public void CreatureUserDefined()
        {
            _aiService.ProcessCreatureUserDefined(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature blocked logic
        /// </summary>
        [ScriptHandler<OnCreatureBlockedAfter>]
        public void CreatureBlocked()
        {
            _aiService.ProcessCreatureBlocked(OBJECT_SELF);
        }

        /// <summary>
        /// When a creature enters the aggro aura of another creature, increase their enmity and start the aggro process.
        /// Invisible creatures do not trigger this.
        /// </summary>
        [ScriptHandler<OnCreatureAggroEnter>]
        public void CreatureAggroEnter()
        {
            _aiService.ProcessCreatureAggroEnter(OBJECT_SELF);
        }

        /// <summary>
        /// When a creature exits the aggro aura of another creature, 
        /// </summary>
        [ScriptHandler<OnCreatureAggroExit>]
        public void CreatureAggroExit()
        {
            _aiService.ProcessCreatureAggroExit(OBJECT_SELF);
        }

        /// <summary>
        /// When the creature dies or is destroyed, remove it from all caches.
        /// </summary>
        [ScriptHandler(ScriptName.OnObjectDestroyed)]
        public void RemoveFromAlliesCache()
        {
            _aiService.RemoveFromAlliesCache(OBJECT_SELF);
        }
    }
}
