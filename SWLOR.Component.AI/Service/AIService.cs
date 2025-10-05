using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.AI.Contracts;
using SWLOR.Component.AI.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.AI.Contracts;
using SWLOR.Shared.Domain.AI.Enums;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;

namespace SWLOR.Component.AI.Service
{
    public class AIService : IAIService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<uint, HashSet<uint>> _creatureAllies = new();
        private readonly Dictionary<AIDefinitionType, IAIDefinition> _aiDefinitions = new();

        public AIService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            // Initialize lazy services
            _random = new Lazy<IRandomService>(() => _serviceProvider.GetRequiredService<IRandomService>());
            _enmity = new Lazy<IEnmityService>(() => _serviceProvider.GetRequiredService<IEnmityService>());
            _partyService = new Lazy<IPartyService>(() => _serviceProvider.GetRequiredService<IPartyService>());
            _activityService = new Lazy<IActivityService>(() => _serviceProvider.GetRequiredService<IActivityService>());
            _dialogService = new Lazy<IDialogService>(() => _serviceProvider.GetRequiredService<IDialogService>());
            _statService = new Lazy<IStatService>(() => _serviceProvider.GetRequiredService<IStatService>());
            _abilityService = new Lazy<IAbilityService>(() => _serviceProvider.GetRequiredService<IAbilityService>());
            _perkService = new Lazy<IPerkService>(() => _serviceProvider.GetRequiredService<IPerkService>());
        }
        
        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IRandomService> _random;
        private readonly Lazy<IEnmityService> _enmity;
        private readonly Lazy<IPartyService> _partyService;
        private readonly Lazy<IActivityService> _activityService;
        private readonly Lazy<IDialogService> _dialogService;
        private readonly Lazy<IStatService> _statService;
        private readonly Lazy<IAbilityService> _abilityService;
        private readonly Lazy<IPerkService> _perkService;
        
        private IRandomService Random => _random.Value;
        private IEnmityService Enmity => _enmity.Value;
        private IPartyService PartyService => _partyService.Value;
        private IActivityService ActivityService => _activityService.Value;
        private IDialogService DialogService => _dialogService.Value;
        private IStatService StatService => _statService.Value;
        private IAbilityService AbilityService => _abilityService.Value;
        private IPerkService PerkService => _perkService.Value;

        public void CacheAIData()
        {
            _aiDefinitions[AIDefinitionType.Generic] = new GenericAIDefinition(_serviceProvider);
            _aiDefinitions[AIDefinitionType.Droid] = new DroidAIDefinition(_serviceProvider);
            _aiDefinitions[AIDefinitionType.Beast] = new BeastAIDefinition(_serviceProvider);
        }

        /// <summary>
        /// Processes creature heartbeat logic.
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreatureHeartbeat(uint creature)
        {
            if (GetAILevel(creature) == AILevelType.VeryLow)
                return;

            StatService.RestoreNPCStats(true);
            ProcessFlags(creature);
            Enmity.AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Processes creature perception logic.
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreaturePerception(uint creature)
        {
            // This is a stripped-down version of the default NWN perception event.
            // We handle most of our perception logic with the aggro aura effect.
            ProcessCreatureAllies(creature);
        }

        /// <summary>
        /// Processes creature combat round end logic.
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreatureCombatRoundEnd(uint creature)
        {
            if (!ActivityService.IsBusy(creature))
            {
                ProcessPerkAI(AIDefinitionType.Generic, creature, true);
            }

            Enmity.AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Processes creature conversation logic.
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreatureConversation(uint creature)
        {
            var conversation = GetLocalString(creature, "CONVERSATION");
            if (!string.IsNullOrWhiteSpace(conversation))
            {
                var talker = GetLastSpeaker();
                DialogService.StartConversation(talker, creature, conversation);
            }
        }

        /// <summary>
        /// Processes creature physical attacked logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreaturePhysicalAttacked(uint creature)
        {
            Enmity.AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Processes creature damaged logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreatureDamaged(uint creature)
        {
            Enmity.AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Processes creature death logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreatureDeath(uint creature)
        {
            RemoveFromAlliesCache(creature);
        }

        /// <summary>
        /// Processes creature disturbed logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreatureDisturbed(uint creature)
        {
            Enmity.AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Processes creature spawn logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreatureSpawn(uint creature)
        {
            SetLocalString(creature, "X2_SPECIAL_COMBAT_AI_SCRIPT", "xxx");

            StatService.LoadNPCStats();
            LoadAggroEffect(creature);
            DoVFX(creature);
            SetLocalLocation(creature, "HOME_LOCATION", GetLocation(creature));
        }

        /// <summary>
        /// Processes creature rested logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreatureRested(uint creature)
        {
        }

        /// <summary>
        /// Processes creature spell cast at logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreatureSpellCastAt(uint creature)
        {
        }

        /// <summary>
        /// Processes creature user defined logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreatureUserDefined(uint creature)
        {
        }

        /// <summary>
        /// Processes creature blocked logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreatureBlocked(uint creature)
        {
        }

        /// <summary>
        /// Processes when a creature enters the aggro aura of another creature, increase their enmity and start the aggro process.
        /// Invisible creatures do not trigger this.
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreatureAggroEnter(uint creature)
        {
            var entering = GetEnteringObject();
            var self = GetAreaOfEffectCreator(creature);

            // Target is invisible
            if (GetHasEffect(entering, EffectScriptType.Invisibility, EffectScriptType.ImprovedInvisibility))
            {
                return;
            }

            // Must have line of sight to AOE creator
            if (!LineOfSightObject(entering, self))
                return;

            if (!GetIsEnemy(entering, self))
            {
                var attackTarget = Enmity.GetHighestEnmityTarget(entering);
                // Non-enemy entered aggro range. If they're the same faction and fighting someone, help them out!
                if (GetFactionEqual(entering, self) &&
                    GetIsEnemy(attackTarget, self))
                {
                    Enmity.ModifyEnmity(attackTarget, self, 1);
                }

                return;
            }

            Enmity.ModifyEnmity(entering, self, 1);

            // All allies within 5m should also aggro the player if they're not already in combat.
            if (_creatureAllies.TryGetValue(self, out var allies))
            {
                foreach (var ally in allies)
                {
                    if (!GetIsEnemy(entering, ally)) continue;
                    if (GetDistanceBetween(self, ally) > 5f) continue;

                    Enmity.ModifyEnmity(entering, ally, 1);
                }
            }
        }

        /// <summary>
        /// Processes when a creature exits the aggro aura of another creature
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreatureAggroExit(uint creature)
        {
        }

        /// <summary>
        /// Handles custom perk usage
        /// </summary>
        public void ProcessPerkAI(AIDefinitionType aiType, uint creature, bool usesEnmity)
        {
            // Petrified - do nothing else.
            if (GetHasEffect(creature, EffectScriptType.Petrify)) 
                return;

            // Attempt to target the highest enmity creature.
            // If no target can be determined, exit early.
            var target = Enmity.GetHighestEnmityTarget(creature);
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
                    allies = PartyService.GetAllPartyMembers(creature);
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
        private bool GetHasEffect(uint creature, EffectScriptType effectType, params EffectScriptType[] otherEffectTypes)
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
        /// <param name="creature">The creature to add the effect to</param>
        private void LoadAggroEffect(uint creature)
        {
            var effect = SupernaturalEffect(EffectAreaOfEffect(AreaOfEffectType.CustomAoe, "crea_aggro_enter", string.Empty, "crea_aggro_exit"));
            effect = TagEffect(effect, "AGGRO_AOE");
            ApplyEffectToObject(DurationType.Permanent, effect, creature);
        }

        /// <summary>
        /// Applies visual effects to a creature based on local variables.
        /// </summary>
        /// <param name="creature">The creature to apply effects to</param>
        private void DoVFX(uint creature)
        {
            // Allow builders to put permanent effects on creatures - e.g. to make them statues, or make them glow.
            // Index of standard VFX effects here: https://nwnlexicon.com/index.php?title=Vfx_dur
            var vfx = GetLocalInt(creature, "PERMANENT_VFX_ID");
            if (vfx > 0) 
                ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect((VisualEffectType)vfx), creature);

            // Cutscene paralysis - for statues.
            var paralyze = GetLocalInt(creature, "PARALYZE");
            if (paralyze > 0) 
                ApplyEffectToObject(DurationType.Permanent, SupernaturalEffect(EffectCutsceneParalyze()), creature);

            // Daze - for creatures that should not be able to attack.
            var daze = GetLocalInt(creature, "DAZE");
            if (daze > 0) 
                ApplyEffectToObject(DurationType.Permanent, SupernaturalEffect(EffectDazed()), creature);
        }

        /// <summary>
        /// When a creature's heartbeat fires, if they have the RandomWalk or ReturnHome AI flag,
        /// and they are not currently preoccupied (combat, talking, etc.) force them to randomly walk or return home if they are too far away.
        /// </summary>
        /// <param name="creature">The creature to process</param>
        private void ProcessFlags(uint creature)
        {
            // Certain effects should interrupt the random walk process.
            var effects = new[] {EffectScriptType.Dazed, EffectScriptType.Petrify};
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                if (effects.Contains(GetEffectType(effect)))
                {
                    return;
                }
            }

            var aiFlags = GetAIFlag(creature);
            if (IsInConversation(creature) ||
                GetIsInCombat(creature) ||
                GetCurrentAction(creature) == ActionType.RandomWalk ||
                GetCurrentAction(creature) == ActionType.MoveToPoint ||
                GetIsObjectValid(Enmity.GetHighestEnmityTarget(creature)))
                return;

            // Return Home flag
            var homeLocation = GetLocalLocation(creature, "HOME_LOCATION");
            if (aiFlags.HasFlag(AIFlagType.ReturnHome) &&
                (GetAreaFromLocation(homeLocation) != GetArea(creature) ||
                 GetDistanceBetweenLocations(GetLocation(creature), homeLocation) > 15f))
            {
                AssignCommand(creature, () => ActionForceMoveToLocation(homeLocation));
            }
            // Randomly walk flag
            else if(aiFlags.HasFlag(AIFlagType.RandomWalk) &&
                Random.D100(1) <= 40)
            {
                AssignCommand(creature, ActionRandomWalk);
            }
        }

        /// <summary>
        /// When a creature perceives another creature, if the creature is part of the same faction add or remove it from their cache.
        /// Creatures in this cache will be used for AI decisions.
        /// </summary>
        /// <param name="creature">The creature to process</param>
        private void ProcessCreatureAllies(uint creature)
        {
            var lastPerceived = GetLastPerceived();
            if (creature == lastPerceived) return;

            var isSeen = GetLastPerceptionSeen();
            var isVanished = GetLastPerceptionVanished();

            if (GetIsPC(lastPerceived) || GetIsDead(lastPerceived)) return;
            var isSameFaction = GetFactionEqual(creature, lastPerceived);
            if (!isSameFaction) return;

            if (!_creatureAllies.ContainsKey(creature))
                _creatureAllies[creature] = new HashSet<uint>();

            // Only make adjustments if the perceived creature is seen or vanished, as opposed to heard or inaudible.
            if (isSeen)
            {
                if (!_creatureAllies[creature].Contains(lastPerceived))
                    _creatureAllies[creature].Add(lastPerceived);
            }
            else if (isVanished)
            {
                if (_creatureAllies[creature].Contains(lastPerceived))
                    _creatureAllies[creature].Remove(lastPerceived);
            }
        }

        /// <summary>
        /// When the creature dies or is destroyed, remove it from all caches.
        /// </summary>
        /// <param name="creature">The creature to remove from caches</param>
        public void RemoveFromAlliesCache(uint creature)
        {
            if (!_creatureAllies.ContainsKey(creature)) return;

            for(var index = _creatureAllies.Count-1; index >= 0; index--)
            {
                var ally = _creatureAllies.ElementAt(index).Key;
                if (_creatureAllies.ContainsKey(ally))
                {
                    if (_creatureAllies[ally].Contains(creature))
                        _creatureAllies[ally].Remove(creature);
                }
            }

            _creatureAllies.Remove(creature);
        }

        /// <summary>
        /// Sets a set of AI flags onto a particular creature as a local variable.
        /// </summary>
        /// <param name="creature">The creature to set the flags onto.</param>
        /// <param name="flags">The flags to set.</param>
        public void SetAIFlag(uint creature, AIFlagType flags)
        {
            var flagValue = (int) flags;
            SetLocalInt(creature, "AI_FLAGS", flagValue);
        }

        /// <summary>
        /// Retrieves a set of AI flags from a particular creature. If <see cref="SetAIFlag"/> has not been called, this will return no flags.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>A set of AIFlags specified on a creature.</returns>
        public AIFlagType GetAIFlag(uint creature)
        {
            var flagValue = GetLocalInt(creature, "AI_FLAGS");
            return (AIFlagType) flagValue;
        }
    }
}
