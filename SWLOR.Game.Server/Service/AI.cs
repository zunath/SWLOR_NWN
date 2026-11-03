using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Service.AIService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of creature AI. The logic and state live in <see cref="IAIService"/>; every member here forwards
    /// to the container's instance, and the event handlers stay here so their names in the handler order manifest do
    /// not change.
    /// </summary>
    public static class AI
    {
        private static IAIService Instance => FacadeAccessor.Resolve<IAIService>();

        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public static void CacheAIData()
        {
            Instance.CacheAIData();
        }

        [NWNEventHandler(ScriptName.OnDMToggleAIAfter)]
        public static void DMToggleAIAfter()
        {
            Instance.DMToggleAIAfter();
        }

        /// <summary>
        /// Entry point for creature heartbeat logic.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureHeartbeatAfter)]
        public static void CreatureHeartbeat()
        {
            Instance.CreatureHeartbeat();
        }

        /// <summary>
        /// Entry point for creature perception logic.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreaturePerceptionAfter)]
        public static void CreaturePerception()
        {
            Instance.CreaturePerception();
        }

        /// <summary>
        /// Entry point for creature combat round end logic.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureRoundEndAfter)]
        public static void CreatureCombatRoundEnd()
        {
            Instance.CreatureCombatRoundEnd();
        }

        /// <summary>
        /// Entry point for creature conversation logic.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureConversationAfter)]
        public static void CreatureConversation()
        {
            Instance.CreatureConversation();
        }

        /// <summary>
        /// Entry point for creature physical attacked logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureAttackAfter)]
        public static void CreaturePhysicalAttacked()
        {
            Instance.CreaturePhysicalAttacked();
        }

        /// <summary>
        /// Entry point for creature damaged logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
        public static void CreatureDamaged()
        {
            Instance.CreatureDamaged();
        }

        /// <summary>
        /// Entry point for creature death logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void CreatureDeath()
        {
            Instance.CreatureDeath();
        }

        /// <summary>
        /// Entry point for creature disturbed logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDisturbedAfter)]
        public static void CreatureDisturbed()
        {
            Instance.CreatureDisturbed();
        }

        /// <summary>
        /// Entry point for creature spawn logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureSpawnAfter)]
        public static void CreatureSpawn()
        {
            Instance.CreatureSpawn();
        }

        /// <summary>
        /// Entry point for creature rested logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureRestedAfter)]
        public static void CreatureRested()
        {
            Instance.CreatureRested();
        }

        /// <summary>
        /// Entry point for creature spell cast at logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureSpellCastAfter)]
        public static void CreatureSpellCastAt()
        {
            Instance.CreatureSpellCastAt();
        }

        /// <summary>
        /// Entry point for creature user defined logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureUserDefinedAfter)]
        public static void CreatureUserDefined()
        {
            Instance.CreatureUserDefined();
        }

        /// <summary>
        /// Entry point for creature blocked logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureBlockedAfter)]
        public static void CreatureBlocked()
        {
            Instance.CreatureBlocked();
        }

        /// <summary>
        /// When a creature enters the aggro aura of another creature, increase their enmity and start the aggro process.
        /// Invisible creatures do not trigger this.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureAggroEnter)]
        public static void CreatureAggroEnter()
        {
            Instance.CreatureAggroEnter();
        }

        /// <summary>
        /// When a creature exits the aggro aura of another creature,
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureAggroExit)]
        public static void CreatureAggroExit()
        {
            Instance.CreatureAggroExit();
        }

        public static bool ProcessTrigger(
            uint creature,
            AITriggerType trigger,
            uint eventTarget = OBJECT_INVALID,
            bool bypassDecisionThrottle = false)
        {
            return Instance.ProcessTrigger(creature, trigger, eventTarget, bypassDecisionThrottle);
        }

        public static void SetAIProfile(uint creature, AIProfileType profile)
        {
            Instance.SetAIProfile(creature, profile);
        }

        public static AIProfileType GetAIProfile(uint creature)
        {
            return Instance.GetAIProfile(creature);
        }

        /// <summary>
        /// When the creature dies or is destroyed, remove it from all caches.
        /// </summary>
        [NWNEventHandler(ScriptName.OnObjectDestroyed)]
        public static void RemoveFromAlliesCache()
        {
            Instance.RemoveFromAlliesCache();
        }

        /// <summary>
        /// Sets a set of AI flags onto a particular creature as a local variable.
        /// </summary>
        /// <param name="creature">The creature to set the flags onto.</param>
        /// <param name="flags">The flags to set.</param>
        public static void SetAIFlag(uint creature, AIFlag flags)
        {
            Instance.SetAIFlag(creature, flags);
        }

        /// <summary>
        /// Retrieves a set of AI flags from a particular creature. If <see cref="SetAIFlag"/> has not been called, this will return no flags.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>A set of AIFlags specified on a creature.</returns>
        public static AIFlag GetAIFlag(uint creature)
        {
            return Instance.GetAIFlag(creature);
        }

        public static bool IsLeashEvading(uint creature)
        {
            return Instance.IsLeashEvading(creature);
        }

        public static bool TryStartCombatLeashEvade(uint creature, uint target)
        {
            return Instance.TryStartCombatLeashEvade(creature, target);
        }

        public static bool IsInAggroRange(uint creature, uint target)
        {
            return Instance.IsInAggroRange(creature, target);
        }

        /// <summary>
        /// Re-enters the normal proximity-aggro path when a later native Spot check reveals a
        /// stealthed player who is still inside the observer's aggro aura. Non-AI observers and
        /// targets that no longer satisfy the ordinary aggro guards are ignored.
        /// </summary>
        public static void TryAcquireAggroAfterDetection(uint observer, uint target)
        {
            Instance.TryAcquireAggroAfterDetection(observer, target);
        }

        /// <summary>
        /// Stops a chase whose proximity threat has expired, using the normal return-home behavior.
        /// </summary>
        public static void StopCombatAfterProximityLoss(uint enemy)
        {
            Instance.StopCombatAfterProximityLoss(enemy);
        }

        public static bool IsCreatureAIEnabled(uint creature)
        {
            return Instance.IsCreatureAIEnabled(creature);
        }

        /// <summary>
        /// Forgets the AI state (phase, cooldowns, cached actions) of a creature.
        /// </summary>
        public static void ClearAIState(uint creature)
        {
            Instance.ClearAIState(creature);
        }

        public static bool TryUseBestTargetedSupportAbility(uint creature, uint target, out string abilityName)
        {
            return Instance.TryUseBestTargetedSupportAbility(creature, target, out abilityName);
        }
    }
}
