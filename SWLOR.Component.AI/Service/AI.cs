using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.AIDefinition;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Dialog.Service;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Game.Server.Service
{
    public class AI : IAI
    {
        private readonly IRandomService _random;
        private readonly IStatService _statService;
        private readonly IEnmityService _enmity;
        private readonly IAbilityService _abilityService;
        private readonly IPerkService _perkService;
        private readonly IStatusEffectService _statusEffectService;
        private readonly IPartyService _partyService;
        private readonly IActivityService _activityService;
        private readonly Dictionary<uint, HashSet<uint>> _creatureAllies = new();
        private readonly Dictionary<AIDefinitionType, IAIDefinition> _aiDefinitions = new();

        public AI(
            IRandomService random, 
            IStatService statService,
            IEnmityService enmity, 
            IAbilityService abilityService, 
            IPerkService perkService, 
            IStatusEffectService statusEffectService, 
            IPartyService partyService, 
            IActivityService activityService)
        {
            _random = random;
            _statService = statService;
            _enmity = enmity;
            _abilityService = abilityService;
            _perkService = perkService;
            _statusEffectService = statusEffectService;
            _partyService = partyService;
            _activityService = activityService;
        }

        [ScriptHandler<OnModuleCacheBefore>]
        public void CacheAIData()
        {
            _aiDefinitions[AIDefinitionType.Generic] = new GenericAIDefinition(_abilityService, _perkService, _statusEffectService);
            _aiDefinitions[AIDefinitionType.Droid] = new DroidAIDefinition(_abilityService, _perkService, _statusEffectService);
            _aiDefinitions[AIDefinitionType.Beast] = new BeastAIDefinition(_abilityService, _perkService, _statusEffectService);
        }

        /// <summary>
        /// Entry point for creature heartbeat logic.
        /// </summary>
        [ScriptHandler<OnCreatureHeartbeatAfter>]
        public void CreatureHeartbeat()
        {
            if (GetAILevel(OBJECT_SELF) == AILevel.VeryLow)
                return;

            _statService.RestoreNPCStats(true);
            ProcessFlags();
            _enmity.AttackHighestEnmityTarget(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature perception logic.
        /// </summary>
        [ScriptHandler<OnCreaturePerceptionAfter>]
        public void CreaturePerception()
        {
            // This is a stripped-down version of the default NWN perception event.
            // We handle most of our perception logic with the aggro aura effect.
            ProcessCreatureAllies();
        }

        /// <summary>
        /// Entry point for creature combat round end logic.
        /// </summary>
        [ScriptHandler<OnCreatureRoundEndAfter>]
        public void CreatureCombatRoundEnd()
        {
            var creature = OBJECT_SELF;
            if (!_activityService.IsBusy(creature))
            {
                ProcessPerkAI(AIDefinitionType.Generic, creature, true);
            }

            _enmity.AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Entry point for creature conversation logic.
        /// </summary>
        [ScriptHandler<OnCreatureConversationAfter>]
        public void CreatureConversation()
        {
            var conversation = GetLocalString(OBJECT_SELF, "CONVERSATION");
            if (!string.IsNullOrWhiteSpace(conversation))
            {
                var talker = GetLastSpeaker();
                Dialog.StartConversation(talker, OBJECT_SELF, conversation);
            }
        }

        /// <summary>
        /// Entry point for creature physical attacked logic
        /// </summary>
        [ScriptHandler<OnCreatureAttackAfter>]
        public void CreaturePhysicalAttacked()
        {
            _enmity.AttackHighestEnmityTarget(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature damaged logic
        /// </summary>
        [ScriptHandler<OnCreatureDamagedAfter>]
        public void CreatureDamaged()
        {
            _enmity.AttackHighestEnmityTarget(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature death logic
        /// </summary>
        [ScriptHandler<OnCreatureDeathAfter>]
        public void CreatureDeath()
        {
            RemoveFromAlliesCache();
        }

        /// <summary>
        /// Entry point for creature disturbed logic
        /// </summary>
        [ScriptHandler<OnCreatureDisturbedAfter>]
        public void CreatureDisturbed()
        {
            _enmity.AttackHighestEnmityTarget(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature spawn logic
        /// </summary>
        [ScriptHandler<OnCreatureSpawnAfter>]
        public void CreatureSpawn()
        {
            SetLocalString(OBJECT_SELF, "X2_SPECIAL_COMBAT_AI_SCRIPT", "xxx");

            _statService.LoadNPCStats();
            LoadAggroEffect();
            DoVFX();
            SetLocalLocation(OBJECT_SELF, "HOME_LOCATION", GetLocation(OBJECT_SELF));
        }

        /// <summary>
        /// Entry point for creature rested logic
        /// </summary>
        [ScriptHandler<OnCreatureRestedAfter>]
        public void CreatureRested()
        {
        }

        /// <summary>
        /// Entry point for creature spell cast at logic
        /// </summary>
        [ScriptHandler<OnCreatureSpellCastAfter>]
        public void CreatureSpellCastAt()
        {
        }

        /// <summary>
        /// Entry point for creature user defined logic
        /// </summary>
        [ScriptHandler<OnCreatureUserDefinedAfter>]
        public void CreatureUserDefined()
        {
        }

        /// <summary>
        /// Entry point for creature blocked logic
        /// </summary>
        [ScriptHandler<OnCreatureBlockedAfter>]
        public void CreatureBlocked()
        {
        }

        /// <summary>
        /// When a creature enters the aggro aura of another creature, increase their enmity and start the aggro process.
        /// Invisible creatures do not trigger this.
        /// </summary>
        [ScriptHandler<OnCreatureAggroEnter>]
        public void CreatureAggroEnter()
        {
            var entering = GetEnteringObject();
            var self = GetAreaOfEffectCreator(OBJECT_SELF);

            // Target is invisible
            if (GetHasEffect(entering, EffectTypeScript.Invisibility, EffectTypeScript.ImprovedInvisibility))
            {
                return;
            }

            // Must have line of sight to AOE creator
            if (!LineOfSightObject(entering, self))
                return;

            if (!GetIsEnemy(entering, self))
            {
                var attackTarget = _enmity.GetHighestEnmityTarget(entering);
                // Non-enemy entered aggro range. If they're the same faction and fighting someone, help them out!
                if (GetFactionEqual(entering, self) &&
                    GetIsEnemy(attackTarget, self))
                {
                    _enmity.ModifyEnmity(attackTarget, self, 1);
                }

                return;
            }

            _enmity.ModifyEnmity(entering, self, 1);

            // All allies within 5m should also aggro the player if they're not already in combat.
            if (_creatureAllies.TryGetValue(self, out var allies))
            {
                foreach (var ally in allies)
                {
                    if (!GetIsEnemy(entering, ally)) continue;
                    if (GetDistanceBetween(self, ally) > 5f) continue;

                    _enmity.ModifyEnmity(entering, ally, 1);
                }
            }

        }

        /// <summary>
        /// When a creature exits the aggro aura of another creature, 
        /// </summary>
        [ScriptHandler<OnCreatureAggroExit>]
        public void CreatureAggroExit()
        {
        }

        /// <summary>
        /// Handles custom perk usage
        /// </summary>
        public void ProcessPerkAI(AIDefinitionType aiType, uint creature, bool usesEnmity)
        {
            // Petrified - do nothing else.
            if (GetHasEffect(creature, EffectTypeScript.Petrify)) 
                return;

            // Attempt to target the highest enmity creature.
            // If no target can be determined, exit early.
            var target = _enmity.GetHighestEnmityTarget(creature);
            if (usesEnmity && !GetIsObjectValid(target))
            {
                ClearAllActions();
                return;
            }

            // If currently randomly walking, clear all actions.
            if (GetCurrentAction(creature) == ActionType.RandomWalk)
            {
                ClearAllActions();
            }

            // Not currently fighting - attack target
            if (GetCurrentAction(creature) == ActionType.Invalid)
            {
                ClearAllActions();
                ActionAttack(target);
            }
            // Perk ability usage
            else
            {
                var master = GetMaster(creature);
                var hasPCMaster = GetIsObjectValid(master) && GetIsPC(master);
                var allies = new List<uint>();

                if (hasPCMaster)
                {
                    allies = _partyService.GetAllPartyMembers(creature);
                }
                else
                {
                    if (_creatureAllies.ContainsKey(creature))
                    {
                        allies = _creatureAllies[creature].ToList();
                    }

                    allies.Add(creature);
                }

                if(!GetIsObjectValid(target))
                    target = GetAttemptedAttackTarget();

                var aiDefinition = _aiDefinitions[aiType];
                aiDefinition.PreProcessAI(creature, target, allies);
                var (feat, featTarget) = aiDefinition.DeterminePerkAbility();
                if (feat != FeatType.Invalid && GetIsObjectValid(featTarget))
                {
                    ClearAllActions();
                    ActionUseFeat(feat, featTarget);
                }
            }
        }

        /// <summary>
        /// Returns whether a creature has an effect.
        /// </summary>
        /// <param name="effectType">The type of effect to look for.</param>
        /// <param name="creature">The creature to check</param>
        /// <returns>true if creature has the effect, false otherwise</returns>
        private bool GetHasEffect(uint creature, EffectTypeScript effectType, params EffectTypeScript[] otherEffectTypes)
        {
            var effect = GetFirstEffect(creature);
            while (GetIsEffectValid(effect))
            {
                var type = GetEffectType(effect);

                if (type == effectType || otherEffectTypes.Contains(type))
                {
                    return true;
                }
                effect = GetNextEffect(creature);
            }

            return false;
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

        /// <summary>
        /// When a creature's heartbeat fires, if they have the RandomWalk or ReturnHome AI flag,
        /// and they are not currently preoccupied (combat, talking, etc.) force them to randomly walk or return home if they are too far away.
        /// </summary>
        private void ProcessFlags()
        {
            var self = OBJECT_SELF;

            // Certain effects should interrupt the random walk process.
            var effects = new[] {EffectTypeScript.Dazed, EffectTypeScript.Petrify};
            for (var effect = GetFirstEffect(self); GetIsEffectValid(effect); effect = GetNextEffect(self))
            {
                if (effects.Contains(GetEffectType(effect)))
                {
                    return;
                }
            }

            var aiFlags = GetAIFlag(self);
            if (IsInConversation(self) ||
                GetIsInCombat(self) ||
                GetCurrentAction(self) == ActionType.RandomWalk ||
                GetCurrentAction(self) == ActionType.MoveToPoint ||
                GetIsObjectValid(_enmity.GetHighestEnmityTarget(self)))
                return;

            // Return Home flag
            var homeLocation = GetLocalLocation(self, "HOME_LOCATION");
            if (aiFlags.HasFlag(AIFlag.ReturnHome) &&
                (GetAreaFromLocation(homeLocation) != GetArea(self) ||
                 GetDistanceBetweenLocations(GetLocation(self), homeLocation) > 15f))
            {
                AssignCommand(self, () => ActionForceMoveToLocation(homeLocation));
            }
            // Randomly walk flag
            else if(aiFlags.HasFlag(AIFlag.RandomWalk) &&
                _random.D100(1) <= 40)
            {
                AssignCommand(self, ActionRandomWalk);
            }
        }

        /// <summary>
        /// When a creature perceives another creature, if the creature is part of the same faction add or remove it from their cache.
        /// Creatures in this cache will be used for AI decisions.
        /// </summary>
        private void ProcessCreatureAllies()
        {
            var self = OBJECT_SELF;
            var lastPerceived = GetLastPerceived();
            if (self == lastPerceived) return;

            var isSeen = GetLastPerceptionSeen();
            var isVanished = GetLastPerceptionVanished();

            if (GetIsPC(lastPerceived) || GetIsDead(lastPerceived)) return;
            var isSameFaction = GetFactionEqual(self, lastPerceived);
            if (!isSameFaction) return;

            if (!_creatureAllies.ContainsKey(self))
                _creatureAllies[self] = new HashSet<uint>();

            // Only make adjustments if the perceived creature is seen or vanished, as opposed to heard or inaudible.
            if (isSeen)
            {
                if (!_creatureAllies[self].Contains(lastPerceived))
                    _creatureAllies[self].Add(lastPerceived);
            }
            else if (isVanished)
            {
                if (_creatureAllies[self].Contains(lastPerceived))
                    _creatureAllies[self].Remove(lastPerceived);
            }
        }

        /// <summary>
        /// When the creature dies or is destroyed, remove it from all caches.
        /// </summary>
        [ScriptHandler(ScriptName.OnObjectDestroyed)]
        public void RemoveFromAlliesCache()
        {
            var self = OBJECT_SELF;
            if (!_creatureAllies.ContainsKey(self)) return;

            for(var index = _creatureAllies.Count-1; index >= 0; index--)
            {
                var ally = _creatureAllies.ElementAt(index).Key;
                if (_creatureAllies.ContainsKey(ally))
                {
                    if (_creatureAllies[ally].Contains(self))
                        _creatureAllies[ally].Remove(self);
                }
            }

            _creatureAllies.Remove(self);
        }

        /// <summary>
        /// Sets a set of AI flags onto a particular creature as a local variable.
        /// </summary>
        /// <param name="creature">The creature to set the flags onto.</param>
        /// <param name="flags">The flags to set.</param>
        public void SetAIFlag(uint creature, AIFlag flags)
        {
            var flagValue = (int) flags;
            SetLocalInt(creature, "AI_FLAGS", flagValue);
        }

        /// <summary>
        /// Retrieves a set of AI flags from a particular creature. If <see cref="SetAIFlag"/> has not been called, this will return no flags.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>A set of AIFlags specified on a creature.</returns>
        public AIFlag GetAIFlag(uint creature)
        {
            var flagValue = GetLocalInt(creature, "AI_FLAGS");
            return (AIFlag) flagValue;
        }
    }
}
