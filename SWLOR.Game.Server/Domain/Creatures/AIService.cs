using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.CompanionControlService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// Creature AI: the creature event logic, the combat leash, the aggro aura, the ally cache and the profile decision
    /// engine (profiles, per-creature state and action selection) that used to be static in <c>Service.AI</c> and
    /// <c>Service.AIService.NPCAI</c>, moved here unchanged. The facade still carries the event handlers and forwards
    /// every call to the container's instance.
    /// </summary>
    public class AIService : IAIService
    {
        public const string ProfileLocalVariable = "AI_PROFILE";
        public const string ProfileIdLocalVariable = "AI_PROFILE_ID";

        private const float AggroRadius = 8.5f;
        private const float ReturnHomeRadius = 15f;
        private const float CombatLeashRadius = 45f;
        private const float LeashEvadeMovementRateFactor = 3.0f;
        private const int ProximityEnmityAmount = 1;
        private const string LeashEvadeActiveVariable = "AI_LEASH_EVADE_ACTIVE";
        private const string LeashEvadeRestorePlotFlagVariable = "AI_LEASH_EVADE_RESTORE_PLOT_FLAG";
        private const string LeashEvadeRestoreMovementRateVariable = "AI_LEASH_EVADE_RESTORE_MOVEMENT_RATE";
        private const string LeashEvadeReturnQueuedVariable = "AI_LEASH_EVADE_RETURN_QUEUED";

        private readonly IPartyService _party;
        private readonly IBeastMasteryService _beastMastery;
        private readonly IDMPresenceService _dmPresence;

        private readonly Dictionary<uint, HashSet<uint>> _creatureAllies = new();
        private readonly Dictionary<AIProfileType, AIProfile> _profiles = new();
        private readonly Dictionary<uint, AIState> _states = new();
        private readonly Dictionary<FeatType, AITargetSelector> _defaultTargetSelectors = new();

        public AIService(IPartyService party, IBeastMasteryService beastMastery, IDMPresenceService dmPresence)
        {
            _party = party;
            _beastMastery = beastMastery;
            _dmPresence = dmPresence;
        }

        public void CacheAIData()
        {
            CacheProfiles();
            ValidateProfiles();
        }

        public void DMToggleAIAfter()
        {
            var creature = StringToObject(EventsPlugin.GetEventData("OBJECT"));
            if (!GetIsObjectValid(creature) ||
                GetObjectType(creature) != ObjectType.Creature)
            {
                return;
            }

            if (!IsAIEnabled(creature))
            {
                ClearAIState(creature);
            }
        }

        /// <summary>
        /// Entry point for creature heartbeat logic.
        /// </summary>
        public void CreatureHeartbeat()
        {
            if (!IsAIEnabled(OBJECT_SELF))
                return;

            RestoreNPCStats(true);
            ProcessFlags();
            AttackHighestEnmityTarget(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature perception logic.
        /// </summary>
        public void CreaturePerception()
        {
            if (!IsAIEnabled(OBJECT_SELF))
                return;

            // This is a stripped-down version of the default NWN perception event.
            // We handle most of our perception logic with the aggro aura effect.
            ProcessCreatureAllies();
        }

        /// <summary>
        /// Entry point for creature combat round end logic.
        /// </summary>
        public void CreatureCombatRoundEnd()
        {
            var creature = OBJECT_SELF;
            if (!IsAIEnabled(creature))
                return;

            var handled = false;
            if (!Activity.IsBusy(creature))
            {
                if (TryStartLeashEvade(creature, GetHighestEnmityTarget(creature)))
                    return;

                handled = ProcessTrigger(creature, AITriggerType.CombatRound);
            }

            if (!handled)
                AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Entry point for creature conversation logic.
        /// </summary>
        public void CreatureConversation()
        {
            var owner = OBJECT_SELF;
            var conversation = GetLocalString(owner, "CONVERSATION");
            if (!string.IsNullOrWhiteSpace(conversation))
            {
                var talker = GetLastSpeaker();
                if (Conversation.TryGetGraph(conversation, out _))
                    Conversation.Start(talker, owner, conversation);
                else if (!ConversationMenu.TryStart(talker, owner, conversation))
                    AssignCommand(talker, () => ActionStartConversation(owner, conversation, true, false));
            }
        }

        /// <summary>
        /// Entry point for creature physical attacked logic
        /// </summary>
        public void CreaturePhysicalAttacked()
        {
            var creature = OBJECT_SELF;
            if (!IsAIEnabled(creature))
                return;

            if (IsLeashEvading(creature))
                return;

            if (TryStartLeashEvade(creature, GetHighestOrEventTarget(creature, GetLastAttacker(creature))))
                return;

            if (!ProcessTrigger(creature, AITriggerType.Attacked, GetLastAttacker(creature)))
                AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Entry point for creature damaged logic
        /// </summary>
        public void CreatureDamaged()
        {
            var creature = OBJECT_SELF;
            if (!IsAIEnabled(creature))
                return;

            if (IsLeashEvading(creature))
                return;

            if (TryStartLeashEvade(creature, GetHighestOrEventTarget(creature, GetLastDamager(creature))))
                return;

            if (!ProcessTrigger(creature, AITriggerType.Damaged, GetLastDamager(creature)))
                AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Entry point for creature death logic
        /// </summary>
        public void CreatureDeath()
        {
            ProcessTrigger(OBJECT_SELF, AITriggerType.Death);
            RemoveFromAlliesCache();
        }

        /// <summary>
        /// Entry point for creature disturbed logic
        /// </summary>
        public void CreatureDisturbed()
        {
            var creature = OBJECT_SELF;
            if (!IsAIEnabled(creature))
                return;

            if (!ProcessTrigger(creature, AITriggerType.Disturbed))
                AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Entry point for creature spawn logic
        /// </summary>
        public void CreatureSpawn()
        {
            var creature = OBJECT_SELF;

            SetLocalString(creature, "X2_SPECIAL_COMBAT_AI_SCRIPT", "xxx");
            LoadNPCStats();
            ApplyCreatureMovementRate(creature);
            LoadAggroEffect();
            DoVFX();
            SetLocalLocation(creature, "HOME_LOCATION", GetLocation(creature));
            ProcessTrigger(creature, AITriggerType.Spawn);
        }

        /// <summary>
        /// Entry point for creature rested logic
        /// </summary>
        public void CreatureRested()
        {
        }

        /// <summary>
        /// Entry point for creature spell cast at logic
        /// </summary>
        public void CreatureSpellCastAt()
        {
        }

        /// <summary>
        /// Entry point for creature user defined logic
        /// </summary>
        public void CreatureUserDefined()
        {
        }

        /// <summary>
        /// Entry point for creature blocked logic
        /// </summary>
        public void CreatureBlocked()
        {
            var creature = OBJECT_SELF;
            var blocker = GetBlockingDoor();
            if (!IsAIEnabled(creature) ||
                IsLeashEvading(creature) ||
                GetObjectType(blocker) != ObjectType.Creature)
            {
                return;
            }

            ResumeAttackAfterActionsCleared(creature);
        }

        /// <summary>
        /// When a creature enters the aggro aura of another creature, increase their enmity and start the aggro process.
        /// Invisible creatures do not trigger this.
        /// </summary>
        public void CreatureAggroEnter()
        {
            var entering = GetEnteringObject();
            var self = GetAreaOfEffectCreator(OBJECT_SELF);
            if (!IsAIEnabled(self))
                return;

            if (IsLeashEvading(self))
                return;

            // Target is invisible
            if (GetHasEffect(entering, EffectTypeScript.Invisibility, EffectTypeScript.ImprovedInvisibility))
            {
                return;
            }

            if (!IsInAggroRange(self, entering))
                return;

            if (!GetIsEnemy(entering, self))
            {
                var attackTarget = GetHighestEnmityTarget(entering);
                // Non-enemy entered aggro range. If they're the same faction and fighting someone, help them out!
                if (GetFactionEqual(entering, self) &&
                    GetIsEnemy(attackTarget, self) &&
                    IsInAggroRange(self, attackTarget))
                {
                    TryAddProximityEnmity(attackTarget, self);
                }

                return;
            }

            TryBeginInfiltration(entering, self);
            if (!CanAcquireAggro(self, entering))
                return;

            TryAcquireAggro(self, entering);
        }

        /// <summary>
        /// When a creature exits the aggro aura of another creature,
        /// </summary>
        public void CreatureAggroExit()
        {
            var exiting = GetExitingObject();
            var self = GetAreaOfEffectCreator(OBJECT_SELF);
            if (!IsAIEnabled(self) || !GetIsObjectValid(exiting))
                return;

            CompleteInfiltration(exiting, self);
            RemoveProximityEnmity(exiting, self);

            if (!_creatureAllies.TryGetValue(self, out var allies))
                return;

            foreach (var ally in allies)
            {
                if (!IsAIEnabled(ally) || IsInAggroRange(ally, exiting))
                    continue;

                RemoveProximityEnmity(exiting, ally);
            }
        }

        public bool ProcessTrigger(
            uint creature,
            AITriggerType trigger,
            uint eventTarget = OBJECT_INVALID,
            bool bypassDecisionThrottle = false)
        {
            if (trigger != AITriggerType.Death && !IsAIEnabled(creature))
                return false;

            return ProcessProfileTrigger(
                creature,
                trigger,
                eventTarget,
                BuildAllies(creature),
                bypassDecisionThrottle);
        }

        public void SetAIProfile(uint creature, AIProfileType profile)
        {
            SetProfile(creature, profile);
        }

        public AIProfileType GetAIProfile(uint creature)
        {
            return GetProfileType(creature);
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
            var aiFlags = GetAIFlag(self);
            var homeLocation = GetLocalLocation(self, "HOME_LOCATION");
            var isOutsideHomeRadius = aiFlags.HasFlag(AIFlag.ReturnHome) &&
                                      IsOutsideHomeRadius(self, homeLocation);
            var highestEnmityTarget = GetHighestEnmityTarget(self);

            if (IsLeashEvading(self))
            {
                if (!TryEndLeashEvadeAtHome(self, homeLocation))
                    ContinueLeashEvadeReturn(self, homeLocation);

                return;
            }

            var hasCombatState = GetIsInCombat(self) || GetIsObjectValid(highestEnmityTarget);
            if (hasCombatState &&
                TryStartLeashEvade(self, highestEnmityTarget))
            {
                return;
            }

            // Certain effects should interrupt the random walk process.
            var effects = new[] {EffectTypeScript.Dazed, EffectTypeScript.Petrify};
            for (var effect = GetFirstEffect(self); GetIsEffectValid(effect); effect = GetNextEffect(self))
            {
                if (effects.Contains(GetEffectType(effect)))
                {
                    return;
                }
            }

            if (GetIsObjectValid(highestEnmityTarget))
            {
                if (!IsInConversation(self))
                    AttackHighestEnmityTarget(self);

                return;
            }

            var currentAction = GetCurrentAction(self);
            if (IsInConversation(self) ||
                GetIsInCombat(self) ||
                (currentAction == ActionType.RandomWalk && !isOutsideHomeRadius) ||
                currentAction == ActionType.MoveToPoint)
                return;

            // Return Home flag
            if (isOutsideHomeRadius)
            {
                AssignCommand(self, () =>
                {
                    if (currentAction == ActionType.RandomWalk)
                        ClearAllActions();

                    ActionForceMoveToLocation(homeLocation);
                });
            }
            // Randomly walk flag
            else if(aiFlags.HasFlag(AIFlag.RandomWalk) &&
                Random.D100(1) <= 40)
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

        private IReadOnlyList<uint> BuildAllies(uint creature)
        {
            var master = GetMaster(creature);
            var hasPCMaster = GetIsObjectValid(master) && GetIsPC(master);

            if (hasPCMaster)
            {
                return _party.GetAllPartyMembers(creature);
            }

            var allies = _creatureAllies.TryGetValue(creature, out var cachedAllies)
                ? cachedAllies.ToList()
                : new List<uint>();

            if (!allies.Contains(creature))
                allies.Add(creature);

            return allies;
        }

        private bool ShouldStartCombatLeashEvade(uint creature, uint target, Location homeLocation)
        {
            return ShouldLeashCombatTarget(creature, target, homeLocation);
        }

        private bool ShouldLeashCombatTarget(uint creature, uint target, Location homeLocation)
        {
            if (!ShouldUseCombatLeash(creature))
                return false;

            return IsTargetBeyondLeash(creature, target, homeLocation);
        }

        private bool IsTargetBeyondLeash(uint creature, uint target, Location homeLocation)
        {
            var leashRadius = GetCombatLeashRadius(creature, target);
            var creatureOutsideLeashRadius = IsOutsideHomeRadius(creature, homeLocation, leashRadius);

            if (!GetIsObjectValid(target))
                return creatureOutsideLeashRadius;

            if (!IsOutsideHomeRadius(target, homeLocation, leashRadius))
                return false;

            var targetMaster = GetMaster(target);
            if (GetIsObjectValid(targetMaster) &&
                GetIsPC(targetMaster) &&
                !IsOutsideHomeRadius(targetMaster, homeLocation, leashRadius) &&
                !creatureOutsideLeashRadius)
            {
                return false;
            }

            return true;
        }

        private bool ShouldUseCombatLeash(uint creature)
        {
            if (!GetAIFlag(creature).HasFlag(AIFlag.ReturnHome))
                return false;

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetArea(player) == GetArea(creature) &&
                    GetIsEnemy(player, creature) &&
                    !_dmPresence.IsControlledByDM(player))
                {
                    return true;
                }
            }

            return false;
        }

        private float GetCombatLeashRadius(uint creature, uint target)
        {
            return CombatLeashRadius + GetHitDistance(creature) + GetHitDistance(target);
        }

        private float GetHitDistance(uint creature)
        {
            return GetIsObjectValid(creature)
                ? CreaturePlugin.GetHitDistance(creature)
                : 0f;
        }

        public bool IsLeashEvading(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   GetLocalBool(creature, LeashEvadeActiveVariable);
        }

        private void StartLeashEvade(uint creature, Location homeLocation)
        {
            if (IsLeashEvading(creature))
                return;

            SetLocalBool(creature, LeashEvadeActiveVariable, true);
            SetLocalBool(creature, LeashEvadeRestorePlotFlagVariable, GetPlotFlag(creature));
            SetLocalInt(creature, LeashEvadeRestoreMovementRateVariable, GetMovementRate(creature));
            SetPlotFlag(creature, true);

            RemoveEnemySourcedStatusEffects(creature);
            SetCurrentHitPoints(creature, GetMaxHitPoints(creature));
            ClearEnmityTable(creature);
            ClearAIState(creature);
            ApplyLeashEvadeMovementRate(creature);
            ExecuteScript(ScriptName.OnCreatureLeashStarted, creature);
            DelayCommand(0.2f, () =>
            {
                if (IsLeashEvading(creature))
                    ApplyLeashEvadeMovementRate(creature);
            });
            ContinueLeashEvadeReturn(creature, homeLocation);
        }

        private void ContinueLeashEvadeReturn(uint creature, Location homeLocation)
        {
            ApplyLeashEvadeMovementRate(creature);

            if (GetLocalBool(creature, LeashEvadeReturnQueuedVariable))
            {
                if (GetCurrentAction(creature) == ActionType.MoveToPoint)
                    return;

                DeleteLocalBool(creature, LeashEvadeReturnQueuedVariable);
            }

            SetLocalBool(creature, LeashEvadeReturnQueuedVariable, true);

            AssignCommand(creature, () =>
            {
                ClearAllActions(true);
                ActionForceMoveToLocation(homeLocation, true, 60f);
                ActionDoCommand(() => CompleteLeashEvadeReturn(creature, homeLocation));
            });
        }

        private bool TryEndLeashEvadeAtHome(uint creature, Location homeLocation)
        {
            if (!IsLeashEvading(creature) ||
                IsOutsideHomeRadius(creature, homeLocation))
            {
                return false;
            }

            EndLeashEvade(creature);
            return true;
        }

        private void CompleteLeashEvadeReturn(uint creature, Location homeLocation)
        {
            DeleteLocalBool(creature, LeashEvadeReturnQueuedVariable);

            if (!IsLeashEvading(creature))
                return;

            if (IsOutsideHomeRadius(creature, homeLocation))
            {
                if (!GetIsObjectValid(GetAreaFromLocation(homeLocation)))
                    return;

                ActionJumpToLocation(homeLocation);
                ActionDoCommand(() => EndLeashEvade(creature));
                return;
            }

            EndLeashEvade(creature);
        }

        private void EndLeashEvade(uint creature)
        {
            if (!IsLeashEvading(creature))
                return;

            SetCurrentHitPoints(creature, GetMaxHitPoints(creature));
            SetPlotFlag(creature, GetLocalBool(creature, LeashEvadeRestorePlotFlagVariable));
            DeleteLocalBool(creature, LeashEvadeRestorePlotFlagVariable);
            DeleteLocalBool(creature, LeashEvadeActiveVariable);
            DeleteLocalBool(creature, LeashEvadeReturnQueuedVariable);
            RestoreLeashEvadeMovementRate(creature);
            ExecuteScript(ScriptName.OnCreatureLeashEnded, creature);
        }

        public bool TryStartCombatLeashEvade(uint creature, uint target)
        {
            return TryStartLeashEvade(creature, target);
        }

        private bool TryStartLeashEvade(uint creature, uint target)
        {
            // Removing control effects can request another attack before evade cleanup finishes.
            // Report it as handled so callers neither restart cleanup nor resume combat.
            if (IsLeashEvading(creature))
                return true;

            var homeLocation = GetLocalLocation(creature, "HOME_LOCATION");
            if (!ShouldStartCombatLeashEvade(creature, target, homeLocation))
                return false;

            if (TryKeepFightingWithinLeash(creature, homeLocation))
                return true;

            StartLeashEvade(creature, homeLocation);
            return true;
        }

        /// <summary>
        /// One creature leaving the leash, or the area (a party member respawning), must not reset an enemy
        /// that is still fighting someone inside its leash. Drops the targets that left and attacks the rest.
        /// </summary>
        /// <returns>true if the enemy kept fighting, false if nobody it is fighting remains inside its leash.</returns>
        private bool TryKeepFightingWithinLeash(uint creature, Location homeLocation)
        {
            var targets = GetEnmityTable(creature).Keys.ToArray();
            var departed = targets
                .Where(target => !IsEngagedWithinLeash(creature, target, homeLocation))
                .ToArray();
            if (departed.Length == targets.Length)
                return false;

            foreach (var target in departed)
            {
                RemoveEnmity(target, creature);
            }

            AttackHighestEnmityTarget(creature);
            return true;
        }

        private bool IsEngagedWithinLeash(uint creature, uint target, Location homeLocation)
        {
            return GetIsObjectValid(target) &&
                   !GetIsDead(target) &&
                   GetArea(target) == GetArea(creature) &&
                   !IsTargetBeyondLeash(creature, target, homeLocation);
        }

        private bool TryReturnHomeAfterCombat(uint enemy)
        {
            var homeLocation = GetLocalLocation(enemy, "HOME_LOCATION");
            if (!GetAIFlag(enemy).HasFlag(AIFlag.ReturnHome) ||
                !IsOutsideHomeRadius(enemy, homeLocation))
            {
                return false;
            }

            StartLeashEvade(enemy, homeLocation);
            return true;
        }

        private uint GetHighestOrEventTarget(uint creature, uint eventTarget)
        {
            var highestEnmityTarget = GetHighestEnmityTarget(creature);
            return GetIsObjectValid(highestEnmityTarget)
                ? highestEnmityTarget
                : eventTarget;
        }

        private void ApplyLeashEvadeMovementRate(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            CreaturePlugin.SetMovementRate(creature, MovementRate.DMFast);
            CreaturePlugin.SetMovementRateFactor(creature, LeashEvadeMovementRateFactor);
        }

        private void RestoreLeashEvadeMovementRate(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            CreaturePlugin.SetMovementRate(
                creature,
                (MovementRate)GetLocalInt(creature, LeashEvadeRestoreMovementRateVariable));
            DeleteLocalInt(creature, LeashEvadeRestoreMovementRateVariable);
            ApplyCreatureMovementRate(creature);
        }

        private void RemoveEnemySourcedStatusEffects(uint creature)
        {
            var effects = StatusEffect.GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .Where(effect => GetIsObjectValid(effect.Source) &&
                                 GetIsEnemy(effect.Source, creature))
                .ToArray();

            foreach (var effect in effects)
            {
                StatusEffect.RemoveStatusEffect(creature, effect.GetType(), effect.Source, false);
            }
        }

        private bool IsOutsideHomeRadius(uint creature, Location homeLocation, float radius = ReturnHomeRadius)
        {
            return GetIsObjectValid(GetAreaFromLocation(homeLocation)) &&
                   (GetAreaFromLocation(homeLocation) != GetArea(creature) ||
                    GetDistanceBetweenLocations(GetLocation(creature), homeLocation) > radius);
        }

        public bool IsInAggroRange(uint creature, uint target)
        {
            return GetIsObjectValid(target) &&
                   GetArea(creature) == GetArea(target) &&
                   GetDistanceBetween(creature, target) <= AggroRadius &&
                   LineOfSightObject(target, creature);
        }

        /// <summary>
        /// Re-enters the normal proximity-aggro path when a later native Spot check reveals a
        /// stealthed player who is still inside the observer's aggro aura. Non-AI observers and
        /// targets that no longer satisfy the ordinary aggro guards are ignored.
        /// </summary>
        public void TryAcquireAggroAfterDetection(uint observer, uint target)
        {
            if (!IsAIEnabled(observer))
                return;

            if (!TryAcquireAggro(observer, target))
                return;

            Log.WriteStructured(
                LogGroup.AI,
                "Stealth detection acquired normal proximity aggro: Observer={Observer} Target={Target}",
                observer,
                target);
        }

        private bool TryAcquireAggro(uint self, uint target)
        {
            if (self == target ||
                !GetIsObjectValid(target) ||
                GetIsDead(target) ||
                GetHasEffect(target, EffectTypeScript.Invisibility, EffectTypeScript.ImprovedInvisibility) ||
                !IsInAggroRange(self, target) ||
                !GetIsEnemy(target, self) ||
                TryStartLeashEvade(self, target) ||
                !TryAddProximityEnmity(target, self))
            {
                return false;
            }

            ProcessTrigger(self, AITriggerType.Aggro, target);
            AddNearbyAllyProximityEnmity(self, target);
            return true;
        }

        private void AddNearbyAllyProximityEnmity(uint self, uint target)
        {
            if (!_creatureAllies.TryGetValue(self, out var allies))
                return;

            foreach (var ally in allies)
            {
                if (!IsAIEnabled(ally)) continue;
                if (!GetIsEnemy(target, ally)) continue;
                if (GetDistanceBetween(self, ally) > 5f) continue;
                if (!LineOfSightObject(target, ally)) continue;

                TryAddProximityEnmity(target, ally);
            }
        }

        private bool TryAddProximityEnmity(uint target, uint enemy)
        {
            if (HasProximityEnmity(target, enemy))
                return false;

            ModifyProximityEnmity(target, enemy, ProximityEnmityAmount);
            return true;
        }

        private void RemoveProximityEnmity(uint target, uint enemy)
        {
            if (TryStartLeashEvade(enemy, target))
                return;

            if (!RemoveTrackedProximityEnmity(target, enemy))
                return;

            var nextTarget = GetHighestEnmityTarget(enemy);
            if (GetIsObjectValid(nextTarget))
            {
                AttackHighestEnmityTarget(enemy);
                return;
            }

            StopCombatAfterProximityLoss(enemy);
        }

        /// <summary>
        /// Stops a chase whose proximity threat has expired, using the normal return-home behavior.
        /// </summary>
        public void StopCombatAfterProximityLoss(uint enemy)
        {
            ClearAIState(enemy);
            if (TryReturnHomeAfterCombat(enemy))
                return;

            AssignCommand(enemy, () => ClearAllActions());
        }

        public bool IsCreatureAIEnabled(uint creature)
        {
            return IsAIEnabled(creature);
        }

        private bool IsAIEnabled(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   GetAILevel(creature) != AILevel.VeryLow;
        }

        // ---- Profile decision engine (formerly Service.AIService.NPCAI) ----

        private sealed class ActionEvaluation
        {
            public AIActionDefinition Action { get; set; }
            public uint Target { get; set; }
            public int Score { get; set; }
            public int Priority { get; set; }
            public int Index { get; set; }
        }

        public IReadOnlyDictionary<AIProfileType, AIProfile> Profiles => _profiles;

        /// <summary>
        /// Registers the target selector an ability uses when its action does not name one.
        /// </summary>
        public void RegisterDefaultTargetSelector(FeatType feat, AITargetSelector selector)
        {
            _defaultTargetSelectors[feat] = selector;
        }

        public bool TryGetDefaultTargetSelector(FeatType feat, out AITargetSelector selector)
        {
            return _defaultTargetSelectors.TryGetValue(feat, out selector);
        }

        public void CacheProfiles()
        {
            _profiles.Clear();

            var types = GetProfileDefinitionTypes();

            foreach (var type in types)
            {
                var instance = (IAIProfileDefinition)Activator.CreateInstance(type);
                var profiles = instance.BuildProfiles();

                foreach (var (profileType, profile) in profiles)
                {
                    if (_profiles.ContainsKey(profileType))
                    {
                        throw new InvalidOperationException($"AI profile '{profileType}' is already registered.");
                    }

                    _profiles[profileType] = profile;
                }
            }

            Console.WriteLine($"Loaded {_profiles.Count} AI profiles.");
        }

        public void ValidateProfiles()
        {
            if (!_profiles.ContainsKey(AIProfileType.Generic))
                throw new InvalidOperationException("AI profile 'Generic' must be registered.");

            if (!_profiles.ContainsKey(AIProfileType.BeastCompanion))
                throw new InvalidOperationException("AI profile 'BeastCompanion' must be registered.");

            if (!_profiles.ContainsKey(AIProfileType.DroidCompanion))
                throw new InvalidOperationException("AI profile 'DroidCompanion' must be registered.");

            foreach (var profile in _profiles.Values)
            {
                ValidateActions(profile, profile.Actions);

                foreach (var phaseId in profile.PhaseOrder)
                {
                    var phase = profile.Phases[phaseId];
                    if (phase.EnterCondition == null)
                    {
                        throw new InvalidOperationException($"AI profile '{profile.Type}' phase '{phase.Id}' has no EnterWhen condition.");
                    }

                    ValidateActions(profile, phase.Actions);
                }
            }
        }

        private void SetProfile(uint creature, AIProfileType profile)
        {
            SetLocalString(creature, ProfileLocalVariable, profile.ToString());
            SetLocalInt(creature, ProfileIdLocalVariable, (int)profile);
        }

        private AIProfileType GetProfileType(uint creature)
        {
            var profileName = GetLocalString(creature, ProfileLocalVariable);
            if (!string.IsNullOrWhiteSpace(profileName) &&
                Enum.TryParse(profileName, true, out AIProfileType namedProfile) &&
                namedProfile != AIProfileType.Invalid)
            {
                return namedProfile;
            }

            var profileId = GetLocalInt(creature, ProfileIdLocalVariable);
            if (profileId > 0 && Enum.IsDefined(typeof(AIProfileType), profileId))
                return (AIProfileType)profileId;

            if (_beastMastery.IsPlayerBeast(creature))
                return AIProfileType.BeastCompanion;

            if (Droid.IsDroid(creature))
                return AIProfileType.DroidCompanion;

            return AIProfileType.Generic;
        }

        public void ClearAIState(uint creature)
        {
            _states.Remove(creature);
        }

        private bool ProcessProfileTrigger(
            uint creature,
            AITriggerType trigger,
            uint eventTarget,
            IReadOnlyList<uint> allies,
            bool bypassDecisionThrottle = false)
        {
            if (!GetIsObjectValid(creature) ||
                GetIsPC(creature) ||
                GetIsDM(creature) ||
                GetIsDMPossessed(creature))
            {
                return false;
            }

            if (trigger == AITriggerType.Death ||
                GetCurrentHitPoints(creature) <= 0)
            {
                ClearAIState(creature);
                return false;
            }

            if (GetAILevel(creature) == AILevel.VeryLow)
                return false;

            if (HasEffectType(creature, EffectTypeScript.Petrify))
                return false;

            var profileType = GetProfileType(creature);
            if (!_profiles.TryGetValue(profileType, out var profile))
            {
                profile = _profiles[AIProfileType.Generic];
            }

            var state = GetState(creature, profileType);
            RefreshActionCache(creature, profile, state);
            var context = new AIContext(creature, trigger, eventTarget, profile, state, allies);
            UseAttackableEnmityTarget(context);

            if (GetIsObjectValid(context.CurrentEnmityTarget) &&
                TryStartCombatLeashEvade(creature, context.CurrentEnmityTarget))
            {
                return false;
            }

            if (GetIsObjectValid(context.CurrentEnmityTarget) && state.CombatStartedTime == default)
                state.CombatStartedTime = Clock.UtcNow;

            UpdateActivePhase(context);

            if (Activity.IsBusy(creature))
            {
                ScheduleBossTimer(context);
                return false;
            }

            if (!bypassDecisionThrottle && ShouldThrottle(profile, state, trigger))
                return false;

            state.LastDecisionTime = Clock.UtcNow;

            ProfilerPlugin.PushPerfScope(creature, "NpcAI.Decide");
            try
            {
                var selected = SelectAction(context);
                if (selected == null)
                {
                    AttackHighestEnmityTarget(creature);
                    return false;
                }

                ExecuteAction(context, selected);
                return true;
            }
            finally
            {
                ProfilerPlugin.PopPerfScope();
                ScheduleBossTimer(context);
            }
        }

        private AIState GetState(uint creature, AIProfileType profileType)
        {
            if (!_states.TryGetValue(creature, out var state))
            {
                state = new AIState();
                _states[creature] = state;
            }

            if (state.Profile != profileType)
            {
                state.Profile = profileType;
                state.ActivePhase = AIPhaseId.Invalid;
                state.EnteredPhases.Clear();
                state.CompletedOnceActions.Clear();
                state.Cooldowns.Clear();
                state.LastDecisionTime = default;
                state.CombatStartedTime = default;
                state.ClearActionCache();
            }

            return state;
        }

        private bool ShouldThrottle(AIProfile profile, AIState state, AITriggerType trigger)
        {
            if (trigger is AITriggerType.Spawn or AITriggerType.Death)
                return false;

            if (profile.DecisionThrottleSeconds <= 0f || state.LastDecisionTime == default)
                return false;

            return (Clock.UtcNow - state.LastDecisionTime).TotalSeconds < profile.DecisionThrottleSeconds;
        }

        private void UpdateActivePhase(AIContext context)
        {
            if (context.Profile.PhaseOrder.Count <= 0)
                return;

            var selected = AIPhaseId.Invalid;
            foreach (var phaseId in context.Profile.PhaseOrder)
            {
                var phase = context.Profile.Phases[phaseId];
                if (phase.EnterCondition?.Invoke(context) == true)
                {
                    selected = phaseId;
                }
            }

            if (selected == AIPhaseId.Invalid || selected == context.State.ActivePhase)
                return;

            context.State.ActivePhase = selected;
            context.State.EnteredPhases.Add(selected);
        }

        private ActionEvaluation SelectAction(AIContext context)
        {
            var candidates = context.State.CachedActions.AsEnumerable();
            if (context.State.ActivePhase != AIPhaseId.Invalid &&
                context.State.CachedPhaseActions.TryGetValue(context.State.ActivePhase, out var phaseActions))
            {
                candidates = candidates.Concat(phaseActions);
            }

            ActionEvaluation best = null;
            var index = 0;
            foreach (var action in candidates.Take(context.Profile.MaxCandidateActions))
            {
                var evaluation = EvaluateAction(context, action, index);
                index++;

                if (evaluation == null)
                    continue;

                if (best == null ||
                    evaluation.Score > best.Score ||
                    evaluation.Score == best.Score && evaluation.Priority < best.Priority ||
                    evaluation.Score == best.Score && evaluation.Priority == best.Priority && evaluation.Index < best.Index)
                {
                    best = evaluation;
                }
            }

            return best;
        }

        private void RefreshActionCache(uint creature, AIProfile profile, AIState state)
        {
            var knownFeats = GetKnownFeats(creature, out var featCount, out var featChecksum);
            if (state.ActionCacheFeatCount == featCount &&
                state.ActionCacheFeatChecksum == featChecksum &&
                state.CachedActions.Count > 0)
            {
                return;
            }

            state.ActionCacheFeatCount = featCount;
            state.ActionCacheFeatChecksum = featChecksum;
            state.CachedActions.Clear();
            state.CachedActions.AddRange(FilterKnownActions(profile.Actions, knownFeats));
            state.CachedPhaseActions.Clear();

            foreach (var (phaseId, phase) in profile.Phases)
            {
                state.CachedPhaseActions[phaseId] = FilterKnownActions(phase.Actions, knownFeats).ToList();
            }
        }

        private IEnumerable<AIActionDefinition> FilterKnownActions(
            IEnumerable<AIActionDefinition> actions,
            HashSet<FeatType> knownFeats)
        {
            foreach (var action in actions)
            {
                if (action.Type != AIActionType.Ability ||
                    knownFeats.Contains(action.Feat))
                {
                    yield return action;
                }
            }
        }

        private HashSet<FeatType> GetKnownFeats(uint creature, out int featCount, out int featChecksum)
        {
            var knownFeats = new HashSet<FeatType>();
            featCount = CreaturePlugin.GetFeatCount(creature);
            featChecksum = 17;

            unchecked
            {
                for (var index = 0; index < featCount; index++)
                {
                    var feat = CreaturePlugin.GetFeatByIndex(creature, index);
                    knownFeats.Add(feat);
                    featChecksum = featChecksum * 31 + (int)feat;
                }
            }

            return knownFeats;
        }

        private ActionEvaluation EvaluateAction(AIContext context, AIActionDefinition action, int index)
        {
            if (action.OncePerPhase && HasCompletedOncePerPhaseAction(context, action))
                return null;

            if (IsOnCooldown(context, action))
                return null;

            foreach (var guard in action.Guards)
            {
                if (!guard(context))
                    return null;
            }

            var target = ResolveTarget(context, action);
            context.SetEvaluatedTarget(target);

            if (!CanExecuteAction(context, action, target))
                return null;

            var score = action.Score?.Invoke(context) ?? 0;
            if (score <= 0)
                return null;

            return new ActionEvaluation
            {
                Action = action,
                Target = target,
                Score = score,
                Priority = action.Priority,
                Index = index
            };
        }

        private uint ResolveTarget(AIContext context, AIActionDefinition action)
        {
            if (action.Type == AIActionType.AttackHighestEnmity)
                return context.CurrentEnmityTarget;

            if (action.Type == AIActionType.Ability)
            {
                var ability = Ability.GetAbilityDetail(action.Feat);
                AITargetSelector selector;
                if (action.TargetSelector != null)
                    selector = action.TargetSelector;
                else if (TryGetDefaultTargetSelector(action.Feat, out var defaultSelector))
                    selector = defaultSelector;
                else
                    selector = AITarget.InferDefault(action.Feat, ability);

                var selectedTarget = selector(context);
                if (ability.IsHostileAbility && IsControlledCompanion(context.Self))
                {
                    return ResolveHostileAbilityTarget(
                        context.Self,
                        ability,
                        context.CurrentEnmityTarget,
                        selectedTarget);
                }

                return selectedTarget;
            }

            if (action.TargetSelector != null)
                return action.TargetSelector(context);

            return context.Self;
        }

        private bool CanExecuteAction(AIContext context, AIActionDefinition action, uint target)
        {
            var isControlledHostileIntent = IsControlledCompanion(context.Self) &&
                                            IsHostileIntent(action);
            if (isControlledHostileIntent &&
                (!GetIsObjectValid(context.CurrentEnmityTarget) ||
                 action.Type == AIActionType.Ability && !GetIsObjectValid(target)))
            {
                return false;
            }

            switch (action.Type)
            {
                case AIActionType.Ability:
                    return CanUseAbility(context, action, target);
                case AIActionType.AttackHighestEnmity:
                case AIActionType.MoveToTarget:
                case AIActionType.Flee:
                case AIActionType.CallAllies:
                    return GetIsObjectValid(target);
                case AIActionType.ReturnHome:
                    return context.IsOutsideHomeRadius;
                case AIActionType.RandomWalk:
                    return !GetIsInCombat(context.Self) && GetCurrentAction(context.Self) == ActionType.Invalid;
                case AIActionType.Wait:
                case AIActionType.Speak:
                case AIActionType.Script:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsHostileIntent(AIActionDefinition action)
        {
            if (action.Type == AIActionType.AttackHighestEnmity)
                return true;

            return action.Type == AIActionType.Ability &&
                   action.Feat != FeatType.Invalid &&
                   Ability.IsFeatRegistered(action.Feat) &&
                   Ability.GetAbilityDetail(action.Feat).IsHostileAbility;
        }

        private bool CanUseAbility(AIContext context, AIActionDefinition action, uint target)
        {
            if (action.Feat == FeatType.Invalid || !Ability.IsFeatRegistered(action.Feat))
                return false;

            if (IsControlledCompanion(context.Self) &&
                !AreCompanionAbilitiesEnabled(context.Self))
            {
                return false;
            }

            if (!GetHasFeat(action.Feat, context.Self))
                return false;

            var ability = Ability.GetAbilityDetail(action.Feat);
            if (ability.IsHostileAbility &&
                ability.IsSingleTargetAbility &&
                (!GetIsObjectValid(target) ||
                 target == context.Self ||
                 !GetIsReactionTypeHostile(target, context.Self)))
            {
                return false;
            }

            // Every hostile ability's selector returns no target when there is no enemy to use it on;
            // a self-centered area ability returns the creature itself only when it sees enough
            // enemies nearby. Falling back to the creature here would fire it at nothing it can see.
            if ((ability.RequiresTarget || ability.IsHostileAbility) && !GetIsObjectValid(target))
                return false;

            if (!GetIsObjectValid(target))
                target = context.Self;

            var targetLocation = GetLocation(target);
            var effectiveLevel = ability.EffectiveLevelPerkType == PerkType.Invalid
                ? 1
                : GetPerkLevel(context.Self, ability.EffectiveLevelPerkType);

            return CanActivateAbility(context.Self, target, action.Feat, effectiveLevel, targetLocation);
        }

        private void ExecuteAction(AIContext context, ActionEvaluation evaluation)
        {
            var action = evaluation.Action;
            ProfilerPlugin.PushPerfScope(context.Self, "NpcAI.Execute");
            try
            {
                switch (action.Type)
                {
                    case AIActionType.Ability:
                        ExecuteAbility(
                            context.Self,
                            action,
                            evaluation.Target,
                            context.CurrentEnmityTarget);
                        break;
                    case AIActionType.AttackHighestEnmity:
                        AttackHighestEnmityTarget(context.Self);
                        break;
                    case AIActionType.MoveToTarget:
                        AssignCommand(context.Self, () =>
                        {
                            ClearAllActions();
                            ActionMoveToObject(evaluation.Target, true);
                        });
                        break;
                    case AIActionType.Flee:
                        AssignCommand(context.Self, () =>
                        {
                            ClearAllActions();
                            ActionForceMoveToLocation(context.HomeLocation);
                        });
                        break;
                    case AIActionType.ReturnHome:
                        AssignCommand(context.Self, () =>
                        {
                            ClearAllActions();
                            ActionForceMoveToLocation(context.HomeLocation);
                        });
                        break;
                    case AIActionType.RandomWalk:
                        AssignCommand(context.Self, () =>
                        {
                            ClearAllActions();
                            ActionRandomWalk();
                        });
                        break;
                    case AIActionType.Wait:
                        AssignCommand(context.Self, () => ActionWait(action.FloatValue));
                        break;
                    case AIActionType.Speak:
                        AssignCommand(context.Self, () => SpeakString(action.Text));
                        break;
                    case AIActionType.Script:
                        ExecuteScript(action.ScriptName, context.Self);
                        break;
                    case AIActionType.CallAllies:
                        CallAllies(context, evaluation.Target, action.FloatValue);
                        break;
                }

                ApplyCooldown(context, action);
                MarkOncePerPhaseAction(context, action);

                Log.Write(LogGroup.AI, $"{GetName(context.Self)} [{context.Profile.Type}] executed {action.DebugName} with score {evaluation.Score}.");
            }
            finally
            {
                ProfilerPlugin.PopPerfScope();
            }
        }

        private void ExecuteAbility(
            uint creature,
            AIActionDefinition action,
            uint target,
            uint authorizedAttackTarget)
        {
            var isControlledCompanion = IsControlledCompanion(creature);
            var isControlledHostileAbility = isControlledCompanion && IsHostileIntent(action);
            if (isControlledHostileAbility && !GetIsObjectValid(target))
                return;

            if (!GetIsObjectValid(target))
                target = creature;

            var targetLocation = GetLocation(target);
            if (TryUseAbility(creature, target, action.Feat, targetLocation))
                return;

            if (isControlledCompanion)
            {
                target = authorizedAttackTarget;
            }
            else if (!GetIsObjectValid(target) || target == creature)
            {
                target = GetHighestEnmityAttackTarget(creature);
            }

            if (!GetIsObjectValid(target))
                return;

            IssueAttackCommand(creature, target);
        }

        public bool TryUseBestTargetedSupportAbility(uint creature, uint target, out string abilityName)
        {
            abilityName = string.Empty;
            if (!GetIsObjectValid(creature) ||
                !GetIsObjectValid(target) ||
                GetCurrentHitPoints(target) >= GetMaxHitPoints(target))
            {
                return false;
            }

            var candidates = Ability.GetAllAbilityDetails()
                .Where(x => x.Value.IsHealingAbility &&
                            !x.Value.IsHostileAbility &&
                            x.Value.RequiresTarget &&
                            x.Value.IsSingleTargetAbility &&
                            GetHasFeat(x.Key, creature))
                .OrderByDescending(x => x.Value.AbilityLevel)
                .ThenBy(x => x.Value.Name)
                .ToList();

            foreach (var (feat, ability) in candidates)
            {
                var targetLocation = GetLocation(target);
                if (!TryUseAbility(creature, target, feat, targetLocation))
                    continue;

                abilityName = ability.Name;
                return true;
            }

            return false;
        }

        private bool IsOnCooldown(AIContext context, AIActionDefinition action)
        {
            if (string.IsNullOrWhiteSpace(action.CooldownId))
                return false;

            return context.State.Cooldowns.TryGetValue(action.CooldownId, out var availableAt) &&
                   availableAt > Clock.UtcNow;
        }

        private void ApplyCooldown(AIContext context, AIActionDefinition action)
        {
            if (string.IsNullOrWhiteSpace(action.CooldownId) || action.CooldownSeconds <= 0f)
                return;

            context.State.Cooldowns[action.CooldownId] = Clock.UtcNow.AddSeconds(action.CooldownSeconds);
        }

        private bool HasCompletedOncePerPhaseAction(AIContext context, AIActionDefinition action)
        {
            if (context.State.ActivePhase == AIPhaseId.Invalid)
                return false;

            return context.State.CompletedOnceActions.Contains(GetOncePerPhaseKey(context, action));
        }

        private void MarkOncePerPhaseAction(AIContext context, AIActionDefinition action)
        {
            if (!action.OncePerPhase || context.State.ActivePhase == AIPhaseId.Invalid)
                return;

            context.State.CompletedOnceActions.Add(GetOncePerPhaseKey(context, action));
        }

        private string GetOncePerPhaseKey(AIContext context, AIActionDefinition action)
        {
            return $"{context.State.ActivePhase}:{action.Type}:{action.DebugName}:{action.Feat}";
        }

        private void CallAllies(AIContext context, uint target, float radius)
        {
            foreach (var ally in context.Allies)
            {
                if (!GetIsObjectValid(ally) || ally == context.Self)
                    continue;

                if (radius > 0f && GetDistanceBetween(context.Self, ally) > radius)
                    continue;

                if (!GetIsEnemy(target, ally))
                    continue;

                ModifyEnmity(target, ally, 1);
            }
        }

        private void ScheduleBossTimer(AIContext context)
        {
            if (!context.Profile.IsBoss ||
                context.State.BossTimerScheduled ||
                !GetIsObjectValid(context.CurrentEnmityTarget))
            {
                return;
            }

            var creature = context.Self;
            context.State.BossTimerScheduled = true;

            DelayCommand(1.0f, () =>
            {
                if (!_states.TryGetValue(creature, out var state))
                    return;

                state.BossTimerScheduled = false;

                if (!GetIsObjectValid(creature) || GetCurrentHitPoints(creature) <= 0)
                {
                    ClearAIState(creature);
                    return;
                }

                var target = GetHighestEnmityTarget(creature);
                if (!GetIsObjectValid(target) && !GetIsInCombat(creature))
                {
                    state.CombatStartedTime = default;
                    return;
                }

                ProcessTrigger(creature, AITriggerType.BossTimer, target);
            });
        }

        private void ValidateActions(AIProfile profile, IEnumerable<AIActionDefinition> actions)
        {
            foreach (var action in actions)
            {
                if (action.Type == AIActionType.Ability)
                {
                    if (action.Feat == FeatType.Invalid)
                    {
                        throw new InvalidOperationException($"AI profile '{profile.Type}' has an ability action with an invalid feat.");
                    }

                    if (!Ability.IsFeatRegistered(action.Feat))
                    {
                        throw new InvalidOperationException($"AI profile '{profile.Type}' references feat '{action.Feat}', but it is not registered as an ability.");
                    }
                }

                if (action.Score == null)
                {
                    throw new InvalidOperationException($"AI profile '{profile.Type}' action '{action.DebugName}' has no score.");
                }
            }
        }

        private bool HasEffectType(uint creature, EffectTypeScript effectType)
        {
            if (!MayHaveScriptType(creature, effectType))
                return false;

            var effect = GetFirstEffect(creature);
            while (GetIsEffectValid(effect))
            {
                if (GetEffectType(effect) == effectType)
                    return true;

                effect = GetNextEffect(creature);
            }

            return false;
        }

        // ---- Seams into services that are not converted yet ----
        // Each protected virtual method below has the original call as its body, so the server behaves as it did.
        // A unit test subclass answers them, because the callees read the static enmity tables, the database, the
        // native engine or the other services' statics for real creatures.

        /// <summary>
        /// The types that define AI profiles: every concrete class of a loaded assembly that implements
        /// <see cref="IAIProfileDefinition"/>. Lazy, as the original search was, so each definition is built when the
        /// loop reaches it.
        /// </summary>
        protected virtual IEnumerable<Type> GetProfileDefinitionTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IAIProfileDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);
        }

        protected virtual uint GetHighestEnmityTarget(uint enemy) => Enmity.GetHighestEnmityTarget(enemy);

        protected virtual uint GetHighestEnmityAttackTarget(uint enemy) => Enmity.GetHighestEnmityAttackTarget(enemy);

        protected virtual Dictionary<uint, int> GetEnmityTable(uint enemy) => Enmity.GetEnmityTable(enemy);

        protected virtual void AttackHighestEnmityTarget(uint creature) => Enmity.AttackHighestEnmityTarget(creature);

        protected virtual void ResumeAttackAfterActionsCleared(uint creature) => Enmity.ResumeAttackAfterActionsCleared(creature);

        protected virtual void IssueAttackCommand(uint creature, uint target) => Enmity.IssueAttackCommand(creature, target);

        protected virtual void ClearEnmityTable(uint enemy) => Enmity.ClearEnmityTable(enemy);

        protected virtual void RemoveEnmity(uint creature, uint enemy) => Enmity.RemoveEnmity(creature, enemy);

        protected virtual void ModifyEnmity(uint creature, uint enemy, int amount) => Enmity.ModifyEnmity(creature, enemy, amount);

        protected virtual bool HasProximityEnmity(uint creature, uint enemy) => Enmity.HasProximityEnmity(creature, enemy);

        protected virtual void ModifyProximityEnmity(uint creature, uint enemy, int amount) => Enmity.ModifyProximityEnmity(creature, enemy, amount);

        protected virtual bool RemoveTrackedProximityEnmity(uint creature, uint enemy) => Enmity.RemoveProximityEnmity(creature, enemy);

        protected virtual void RestoreNPCStats(bool outOfCombatRegen) => Stat.RestoreNPCStats(outOfCombatRegen);

        protected virtual void LoadNPCStats() => Stat.LoadNPCStats();

        protected virtual void ApplyCreatureMovementRate(uint creature) => Stat.ApplyCreatureMovementRate(creature);

        protected virtual bool TryBeginInfiltration(uint player, uint npc) => EspionageInfiltration.TryBegin(player, npc);

        protected virtual void CompleteInfiltration(uint player, uint npc) => EspionageInfiltration.Complete(player, npc);

        protected virtual bool CanAcquireAggro(uint observer, uint target) => Stealth.CanAcquireAggro(observer, target);

        protected virtual bool IsControlledCompanion(uint creature) => CompanionControl.IsControlledCompanion(creature);

        protected virtual bool AreCompanionAbilitiesEnabled(uint companion) => CompanionControl.AreAbilitiesEnabled(companion);

        protected virtual uint ResolveHostileAbilityTarget(uint companion, AbilityDetail ability, uint authorizedTarget, uint selectedTarget) =>
            CompanionControl.ResolveHostileAbilityTarget(companion, ability, authorizedTarget, selectedTarget);

        protected virtual int GetPerkLevel(uint creature, PerkType perkType) => Perk.GetPerkLevel(creature, perkType);

        protected virtual bool CanActivateAbility(uint activator, uint target, FeatType feat, int effectiveLevel, Location targetLocation) =>
            Ability.CanUseAbility(activator, target, feat, effectiveLevel, targetLocation);

        protected virtual bool TryUseAbility(uint creature, uint target, FeatType feat, Location targetLocation) =>
            UsePerkFeat.TryUseAbility(creature, target, feat, targetLocation);

        protected virtual bool MayHaveScriptType(uint creature, EffectTypeScript effectType) => AppliedEffects.MayHaveScriptType(creature, effectType);

        /// <summary>
        /// Points the context at the highest-enmity creature the NPC can attack, skipping creatures hidden by
        /// invisibility. The body is the original call; a unit test answers it because the context reads the static enmity
        /// tables and the native effect list.
        /// </summary>
        protected virtual void UseAttackableEnmityTarget(AIContext context) => context.UseAttackableEnmityTarget();
    }
}
