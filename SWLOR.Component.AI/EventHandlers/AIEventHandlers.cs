using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.AI.Contracts;
using SWLOR.Shared.Domain.Combat.Events;
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

        public AIEventHandlers(
            IAIService aiService,
            IEventAggregator eventAggregator)
        {
            _aiService = aiService;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleCacheBefore>(e => CacheAIData());
            eventAggregator.Subscribe<OnCreatureHeartbeatAfter>(e => CreatureHeartbeat());
            eventAggregator.Subscribe<OnCreaturePerceptionAfter>(e => CreaturePerception());
            eventAggregator.Subscribe<OnCreatureRoundEndAfter>(e => CreatureCombatRoundEnd());
            eventAggregator.Subscribe<OnCreatureConversationAfter>(e => CreatureConversation());
            eventAggregator.Subscribe<OnCreatureAttackAfter>(e => CreaturePhysicalAttacked());
            eventAggregator.Subscribe<OnCreatureDamagedAfter>(e => CreatureDamaged());
            eventAggregator.Subscribe<OnCreatureDeathAfter>(e => CreatureDeath());
            eventAggregator.Subscribe<OnCreatureDisturbedAfter>(e => CreatureDisturbed());
            eventAggregator.Subscribe<OnCreatureSpawnAfter>(e => CreatureSpawn());
            eventAggregator.Subscribe<OnCreatureRestedAfter>(e => CreatureRested());
            eventAggregator.Subscribe<OnCreatureSpellCastAfter>(e => CreatureSpellCastAt());
            eventAggregator.Subscribe<OnCreatureUserDefinedAfter>(e => CreatureUserDefined());
            eventAggregator.Subscribe<OnCreatureBlockedAfter>(e => CreatureBlocked());
            eventAggregator.Subscribe<OnCreatureAggroEnter>(e => CreatureAggroEnter());
            eventAggregator.Subscribe<OnCreatureAggroExit>(e => CreatureAggroExit());
            eventAggregator.Subscribe<OnObjectDestroyed>(e => RemoveFromAlliesCache());
        }

        public void CacheAIData()
        {
            _aiService.CacheAIData();
        }

        /// <summary>
        /// Entry point for creature heartbeat logic.
        /// </summary>
        public void CreatureHeartbeat()
        {
            _aiService.ProcessCreatureHeartbeat(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature perception logic.
        /// </summary>
        public void CreaturePerception()
        {
            _aiService.ProcessCreaturePerception(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature combat round end logic.
        /// </summary>
        public void CreatureCombatRoundEnd()
        {
            _aiService.ProcessCreatureCombatRoundEnd(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature conversation logic.
        /// </summary>
        public void CreatureConversation()
        {
            _aiService.ProcessCreatureConversation(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature physical attacked logic
        /// </summary>
        public void CreaturePhysicalAttacked()
        {
            _aiService.ProcessCreaturePhysicalAttacked(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature damaged logic
        /// </summary>
        public void CreatureDamaged()
        {
            _aiService.ProcessCreatureDamaged(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature death logic
        /// </summary>
        public void CreatureDeath()
        {
            _aiService.ProcessCreatureDeath(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature disturbed logic
        /// </summary>
        public void CreatureDisturbed()
        {
            _aiService.ProcessCreatureDisturbed(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature spawn logic
        /// </summary>
        public void CreatureSpawn()
        {
            _aiService.ProcessCreatureSpawn(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature rested logic
        /// </summary>
        public void CreatureRested()
        {
            _aiService.ProcessCreatureRested(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature spell cast at logic
        /// </summary>
        public void CreatureSpellCastAt()
        {
            _aiService.ProcessCreatureSpellCastAt(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature user defined logic
        /// </summary>
        public void CreatureUserDefined()
        {
            _aiService.ProcessCreatureUserDefined(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature blocked logic
        /// </summary>
        public void CreatureBlocked()
        {
            _aiService.ProcessCreatureBlocked(OBJECT_SELF);
        }

        /// <summary>
        /// When a creature enters the aggro aura of another creature, increase their enmity and start the aggro process.
        /// Invisible creatures do not trigger this.
        /// </summary>
        public void CreatureAggroEnter()
        {
            _aiService.ProcessCreatureAggroEnter(OBJECT_SELF);
        }

        /// <summary>
        /// When a creature exits the aggro aura of another creature, 
        /// </summary>
        public void CreatureAggroExit()
        {
            _aiService.ProcessCreatureAggroExit(OBJECT_SELF);
        }

        /// <summary>
        /// When the creature dies or is destroyed, remove it from all caches.
        /// </summary>
        public void RemoveFromAlliesCache()
        {
            _aiService.RemoveFromAlliesCache(OBJECT_SELF);
        }
    }
}
