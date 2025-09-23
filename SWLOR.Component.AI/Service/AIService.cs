using SWLOR.Component.AI.Contracts;
using SWLOR.Component.AI.Enums;
using SWLOR.Component.AI.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Dialog.Contracts;
using SWLOR.Shared.Dialog.Service;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.Events.Events.Creature;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.AI.Service
{
    public class AIService : IAIService
    {
        private readonly IRandomService _random;
        private readonly IStatService _statService;
        private readonly IEnmityService _enmity;
        private readonly IAbilityService _abilityService;
        private readonly IPerkService _perkService;
        private readonly IStatusEffectService _statusEffectService;
        private readonly IPartyService _partyService;
        private readonly IActivityService _activityService;
        private readonly IDialogService _dialogService;
        private readonly Dictionary<uint, HashSet<uint>> _creatureAllies = new();
        private readonly Dictionary<AIDefinitionType, IAIDefinition> _aiDefinitions = new();

        public AIService(
            IRandomService random, 
            IStatService statService,
            IEnmityService enmity, 
            IAbilityService abilityService, 
            IPerkService perkService, 
            IStatusEffectService statusEffectService, 
            IPartyService partyService, 
            IActivityService activityService,
            IDialogService dialogService)
        {
            _random = random;
            _statService = statService;
            _enmity = enmity;
            _abilityService = abilityService;
            _perkService = perkService;
            _statusEffectService = statusEffectService;
            _partyService = partyService;
            _activityService = activityService;
            _dialogService = dialogService;
        }

        public void CacheAIData()
        {
            _aiDefinitions[AIDefinitionType.Generic] = new GenericAIDefinition(_abilityService, _perkService, _statusEffectService);
            _aiDefinitions[AIDefinitionType.Droid] = new DroidAIDefinition(_abilityService, _perkService, _statusEffectService);
            _aiDefinitions[AIDefinitionType.Beast] = new BeastAIDefinition(_abilityService, _perkService, _statusEffectService);
        }

        /// <summary>
        /// Processes creature heartbeat logic.
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreatureHeartbeat(uint creature)
        {
            if (GetAILevel(creature) == AILevel.VeryLow)
                return;

            _statService.RestoreNPCStats(true);
            ProcessFlags(creature);
            _enmity.AttackHighestEnmityTarget(creature);
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
            if (!_activityService.IsBusy(creature))
            {
                ProcessPerkAI(AIDefinitionType.Generic, creature, true);
            }

            _enmity.AttackHighestEnmityTarget(creature);
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
                _dialogService.StartConversation(talker, creature, conversation);
            }
        }

        /// <summary>
        /// Processes creature physical attacked logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreaturePhysicalAttacked(uint creature)
        {
            _enmity.AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Processes creature damaged logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreatureDamaged(uint creature)
        {
            _enmity.AttackHighestEnmityTarget(creature);
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
            _enmity.AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Processes creature spawn logic
        /// </summary>
        /// <param name="creature">The creature to process</param>
        public void ProcessCreatureSpawn(uint creature)
        {
            SetLocalString(creature, "X2_SPECIAL_COMBAT_AI_SCRIPT", "xxx");

            _statService.LoadNPCStats();
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
        /// <param name="creature">The creature to add the effect to</param>
        private void LoadAggroEffect(uint creature)
        {
            var effect = SupernaturalEffect(EffectAreaOfEffect(AreaOfEffect.CustomAoe, "crea_aggro_enter", string.Empty, "crea_aggro_exit"));
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
                ApplyEffectToObject(DurationType.Permanent, EffectVisualEffect((VisualEffect)vfx), creature);

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
            var effects = new[] {EffectTypeScript.Dazed, EffectTypeScript.Petrify};
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
                GetIsObjectValid(_enmity.GetHighestEnmityTarget(creature)))
                return;

            // Return Home flag
            var homeLocation = GetLocalLocation(creature, "HOME_LOCATION");
            if (aiFlags.HasFlag(AIFlag.ReturnHome) &&
                (GetAreaFromLocation(homeLocation) != GetArea(creature) ||
                 GetDistanceBetweenLocations(GetLocation(creature), homeLocation) > 15f))
            {
                AssignCommand(creature, () => ActionForceMoveToLocation(homeLocation));
            }
            // Randomly walk flag
            else if(aiFlags.HasFlag(AIFlag.RandomWalk) &&
                _random.D100(1) <= 40)
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
