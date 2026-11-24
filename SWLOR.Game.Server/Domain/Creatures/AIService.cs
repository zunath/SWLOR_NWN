using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// Creature AI: the profile tables, the per-creature state and flags, the ally cache, the combat leash, which used to be static in <c>Service.AI</c> and
    /// <c>SWLOR.Game.Server.Domain.Creatures.NPCAI</c>, moved here unchanged. The creature event handlers are on
    /// <see cref="AICreatureEventService"/>, the aggro aura on <see cref="AIAggroService"/> and the decision engine on
    /// <see cref="AIDecisionService"/> and <see cref="AIActionService"/>; static callers reach this instance through
    /// <c>GameServices.AI</c>.
    /// </summary>
    public class AIService : IAIService
    {
        public const string ProfileLocalVariable = "AI_PROFILE";
        public const string ProfileIdLocalVariable = "AI_PROFILE_ID";

        private const float CombatLeashRadius = 45f;
        private const float LeashEvadeMovementRateFactor = 3.0f;
        private const string LeashEvadeRestorePlotFlagVariable = "AI_LEASH_EVADE_RESTORE_PLOT_FLAG";
        private const string LeashEvadeRestoreMovementRateVariable = "AI_LEASH_EVADE_RESTORE_MOVEMENT_RATE";
        private const string LeashEvadeReturnQueuedVariable = "AI_LEASH_EVADE_RETURN_QUEUED";

        private readonly IPartyRoster _party;
        private readonly ICompanionLookup _companions;
        private readonly IDMPresenceService _dmPresence;
        private readonly IAbilityCatalog _catalog;
        private readonly Lazy<IEnmityService> _enmity;
        private readonly ICreatureStatService _stat;
        private readonly IStatusEffectService _statusEffect;

        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, HashSet<uint>> _creatureAllies = new();
        private readonly Dictionary<AIProfileType, AIProfile> _profiles = new();
        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, AIState> _states = new();
        private readonly Dictionary<FeatType, AITargetSelector> _defaultTargetSelectors = new();

        public AIService(
            IPartyRoster party,
            ICompanionLookup companions,
            IDMPresenceService dmPresence,
            IAbilityCatalog catalog,
            Lazy<IEnmityService> enmity,
            ICreatureStatService stat,
            IStatusEffectService statusEffect)
        {
            _party = party;
            _companions = companions;
            _dmPresence = dmPresence;
            _catalog = catalog;
            _enmity = enmity;
            _stat = stat;
            _statusEffect = statusEffect;
        }

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public void CacheAIData()
        {
            CacheProfiles();
            ValidateProfiles();
        }

        /// <summary>
        /// Entry point for creature perception logic.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreaturePerceptionAfter)]
        public void CreaturePerception()
        {
            if (!CreatureAIRules.IsCreatureAIEnabled(OBJECT_SELF))
                return;

            // This is a stripped-down version of the default NWN perception event.
            // We handle most of our perception logic with the aggro aura effect.
            ProcessCreatureAllies();
        }

        public void SetAIProfile(uint creature, AIProfileType profile)
        {
            SetProfile(creature, profile);
        }

        public AIProfileType GetAIProfile(uint creature)
        {
            return GetProfileType(creature);
        }

        public string DescribeAIProfile(uint creature)
        {
            return GetAIProfile(creature).ToString();
        }

        /// <summary>
        /// When a creature's heartbeat fires, if they have the RandomWalk or ReturnHome AI flag,
        /// and they are not currently preoccupied (combat, talking, etc.) force them to randomly walk or return home if they are too far away.
        /// </summary>
        public void ProcessFlags()
        {
            var self = OBJECT_SELF;
            var aiFlags = GetAIFlag(self);
            var homeLocation = GetLocalLocation(self, "HOME_LOCATION");
            var isOutsideHomeRadius = aiFlags.HasFlag(AIFlag.ReturnHome) &&
                                      CreatureAIRules.IsOutsideHomeRadius(self, homeLocation);
            var highestEnmityTarget = GetHighestEnmityTarget(self);

            if (CreatureAIRules.IsLeashEvading(self))
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
        [NWNEventHandler(ScriptName.OnObjectDestroyed)]
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
        /// The creatures a creature has seen of its own faction, for the aggro handlers that call allies in.
        /// </summary>
        public bool TryGetAllies(uint creature, out HashSet<uint> allies)
        {
            return _creatureAllies.TryGetValue(creature, out allies);
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

        public IReadOnlyList<uint> BuildAllies(uint creature)
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
            var creatureOutsideLeashRadius = CreatureAIRules.IsOutsideHomeRadius(creature, homeLocation, leashRadius);

            if (!GetIsObjectValid(target))
                return creatureOutsideLeashRadius;

            if (!CreatureAIRules.IsOutsideHomeRadius(target, homeLocation, leashRadius))
                return false;

            var targetMaster = GetMaster(target);
            if (GetIsObjectValid(targetMaster) &&
                GetIsPC(targetMaster) &&
                !CreatureAIRules.IsOutsideHomeRadius(targetMaster, homeLocation, leashRadius) &&
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

        private void StartLeashEvade(uint creature, Location homeLocation)
        {
            if (CreatureAIRules.IsLeashEvading(creature))
                return;

            SetLocalBool(creature, CreatureAIRules.LeashEvadeActiveVariable, true);
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
                if (CreatureAIRules.IsLeashEvading(creature))
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
            if (!CreatureAIRules.IsLeashEvading(creature) ||
                CreatureAIRules.IsOutsideHomeRadius(creature, homeLocation))
            {
                return false;
            }

            EndLeashEvade(creature);
            return true;
        }

        private void CompleteLeashEvadeReturn(uint creature, Location homeLocation)
        {
            DeleteLocalBool(creature, LeashEvadeReturnQueuedVariable);

            if (!CreatureAIRules.IsLeashEvading(creature))
                return;

            if (CreatureAIRules.IsOutsideHomeRadius(creature, homeLocation))
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
            if (!CreatureAIRules.IsLeashEvading(creature))
                return;

            SetCurrentHitPoints(creature, GetMaxHitPoints(creature));
            SetPlotFlag(creature, GetLocalBool(creature, LeashEvadeRestorePlotFlagVariable));
            DeleteLocalBool(creature, LeashEvadeRestorePlotFlagVariable);
            DeleteLocalBool(creature, CreatureAIRules.LeashEvadeActiveVariable);
            DeleteLocalBool(creature, LeashEvadeReturnQueuedVariable);
            RestoreLeashEvadeMovementRate(creature);
            ExecuteScript(ScriptName.OnCreatureLeashEnded, creature);
        }

        public bool TryStartCombatLeashEvade(uint creature, uint target)
        {
            return TryStartLeashEvade(creature, target);
        }

        public bool TryStartLeashEvade(uint creature, uint target)
        {
            // Removing control effects can request another attack before evade cleanup finishes.
            // Report it as handled so callers neither restart cleanup nor resume combat.
            if (CreatureAIRules.IsLeashEvading(creature))
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
                !CreatureAIRules.IsOutsideHomeRadius(enemy, homeLocation))
            {
                return false;
            }

            StartLeashEvade(enemy, homeLocation);
            return true;
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
            var effects = _statusEffect.GetCreatureStatusEffects(creature)
                .GetAllEffects()
                .Where(effect => GetIsObjectValid(effect.Source) &&
                                 GetIsEnemy(effect.Source, creature))
                .ToArray();

            foreach (var effect in effects)
            {
                _statusEffect.RemoveStatusEffect(creature, effect.GetType(), effect.Source, false);
            }
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

        // ---- Profiles and per-creature state (formerly SWLOR.Game.Server.Domain.Creatures.NPCAI) ----

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

        public AIProfileType GetProfileType(uint creature)
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

            if (_companions.IsPlayerBeast(creature))
                return AIProfileType.BeastCompanion;

            if (_companions.IsDroid(creature))
                return AIProfileType.DroidCompanion;

            return AIProfileType.Generic;
        }

        public void ClearAIState(uint creature)
        {
            _states.Remove(creature);
        }

        /// <summary>
        /// The state of a creature the engine has already made one for; false when it has none.
        /// </summary>
        public bool TryGetState(uint creature, out AIState state)
        {
            return _states.TryGetValue(creature, out state);
        }

        public AIState GetState(uint creature, AIProfileType profileType)
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

                    if (!_catalog.IsFeatRegistered(action.Feat))
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
            return GameAssemblies.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IAIProfileDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);
        }

        protected virtual uint GetHighestEnmityTarget(uint enemy) => _enmity.Value.GetHighestEnmityTarget(enemy);

        protected virtual Dictionary<uint, int> GetEnmityTable(uint enemy) => _enmity.Value.GetEnmityTable(enemy);

        protected virtual void AttackHighestEnmityTarget(uint creature) => _enmity.Value.AttackHighestEnmityTarget(creature);

        protected virtual void ClearEnmityTable(uint enemy) => _enmity.Value.ClearEnmityTable(enemy);

        protected virtual void RemoveEnmity(uint creature, uint enemy) => _enmity.Value.RemoveEnmity(creature, enemy);

        protected virtual void ApplyCreatureMovementRate(uint creature) => _stat.ApplyCreatureMovementRate(creature);

    }
}
