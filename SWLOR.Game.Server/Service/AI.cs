using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    public static class AI
    {
        private const float AggroRadius = 8.5f;
        private const float ReturnHomeRadius = 15f;
        private const float CombatLeashRadius = 45f;
        private const float LeashEvadeMovementRateFactor = 3.0f;
        private const int ProximityEnmityAmount = 1;
        private const string LeashEvadeActiveVariable = "AI_LEASH_EVADE_ACTIVE";
        private const string LeashEvadeRestorePlotFlagVariable = "AI_LEASH_EVADE_RESTORE_PLOT_FLAG";
        private const string LeashEvadeRestoreMovementRateVariable = "AI_LEASH_EVADE_RESTORE_MOVEMENT_RATE";
        private const string LeashEvadeReturnQueuedVariable = "AI_LEASH_EVADE_RETURN_QUEUED";
        private static readonly Dictionary<uint, HashSet<uint>> _creatureAllies = new();

        [NWNEventHandler(ScriptName.OnModuleCacheAfter)]
        public static void CacheAIData()
        {
            NPCAI.CacheProfiles();
            NPCAI.ValidateProfiles();
        }

        [NWNEventHandler(ScriptName.OnDMToggleAIAfter)]
        public static void DMToggleAIAfter()
        {
            var creature = StringToObject(EventsPlugin.GetEventData("OBJECT"));
            if (!GetIsObjectValid(creature) ||
                GetObjectType(creature) != ObjectType.Creature)
            {
                return;
            }

            if (!IsAIEnabled(creature))
            {
                NPCAI.ClearState(creature);
            }
        }

        /// <summary>
        /// Entry point for creature heartbeat logic.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureHeartbeatAfter)]
        public static void CreatureHeartbeat()
        {
            if (!IsAIEnabled(OBJECT_SELF))
                return;

            Stat.RestoreNPCStats(true);
            ProcessFlags();
            Enmity.AttackHighestEnmityTarget(OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature perception logic.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreaturePerceptionAfter)]
        public static void CreaturePerception()
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
        [NWNEventHandler(ScriptName.OnCreatureRoundEndAfter)]
        public static void CreatureCombatRoundEnd()
        {
            var creature = OBJECT_SELF;
            if (!IsAIEnabled(creature))
                return;

            var handled = false;
            if (!Activity.IsBusy(creature))
            {
                if (TryStartLeashEvade(creature, Enmity.GetHighestEnmityTarget(creature)))
                    return;

                handled = ProcessTrigger(creature, AITriggerType.CombatRound);
            }

            if (!handled)
                Enmity.AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Entry point for creature conversation logic.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureConversationAfter)]
        public static void CreatureConversation()
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
        [NWNEventHandler(ScriptName.OnCreatureAttackAfter)]
        public static void CreaturePhysicalAttacked()
        {
            var creature = OBJECT_SELF;
            if (!IsAIEnabled(creature))
                return;

            if (IsLeashEvading(creature))
                return;

            if (TryStartLeashEvade(creature, GetHighestOrEventTarget(creature, GetLastAttacker(creature))))
                return;

            if (!ProcessTrigger(creature, AITriggerType.Attacked, GetLastAttacker(creature)))
                Enmity.AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Entry point for creature damaged logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
        public static void CreatureDamaged()
        {
            var creature = OBJECT_SELF;
            if (!IsAIEnabled(creature))
                return;

            if (IsLeashEvading(creature))
                return;

            if (TryStartLeashEvade(creature, GetHighestOrEventTarget(creature, GetLastDamager(creature))))
                return;

            if (!ProcessTrigger(creature, AITriggerType.Damaged, GetLastDamager(creature)))
                Enmity.AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Entry point for creature death logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void CreatureDeath()
        {
            ProcessTrigger(OBJECT_SELF, AITriggerType.Death);
            RemoveFromAlliesCache();
        }

        /// <summary>
        /// Entry point for creature disturbed logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureDisturbedAfter)]
        public static void CreatureDisturbed()
        {
            var creature = OBJECT_SELF;
            if (!IsAIEnabled(creature))
                return;

            if (!ProcessTrigger(creature, AITriggerType.Disturbed))
                Enmity.AttackHighestEnmityTarget(creature);
        }

        /// <summary>
        /// Entry point for creature spawn logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureSpawnAfter)]
        public static void CreatureSpawn()
        {
            var creature = OBJECT_SELF;

            SetLocalString(creature, "X2_SPECIAL_COMBAT_AI_SCRIPT", "xxx");
            Stat.LoadNPCStats();
            Stat.ApplyCreatureMovementRate(creature);
            LoadAggroEffect();
            DoVFX();
            SetLocalLocation(creature, "HOME_LOCATION", GetLocation(creature));
            ProcessTrigger(creature, AITriggerType.Spawn);
        }

        /// <summary>
        /// Entry point for creature rested logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureRestedAfter)]
        public static void CreatureRested()
        {
        }

        /// <summary>
        /// Entry point for creature spell cast at logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureSpellCastAfter)]
        public static void CreatureSpellCastAt()
        {
        }

        /// <summary>
        /// Entry point for creature user defined logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureUserDefinedAfter)]
        public static void CreatureUserDefined()
        {
        }

        /// <summary>
        /// Entry point for creature blocked logic
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureBlockedAfter)]
        public static void CreatureBlocked()
        {
        }

        /// <summary>
        /// When a creature enters the aggro aura of another creature, increase their enmity and start the aggro process.
        /// Invisible creatures do not trigger this.
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureAggroEnter)]
        public static void CreatureAggroEnter()
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
                var attackTarget = Enmity.GetHighestEnmityTarget(entering);
                // Non-enemy entered aggro range. If they're the same faction and fighting someone, help them out!
                if (GetFactionEqual(entering, self) &&
                    GetIsEnemy(attackTarget, self) &&
                    IsInAggroRange(self, attackTarget))
                {
                    TryAddProximityEnmity(attackTarget, self);
                }

                return;
            }

            EspionageInfiltration.TryBegin(entering, self);
            if (!Stealth.CanAcquireAggro(self, entering))
                return;

            TryAcquireAggro(self, entering);
        }

        /// <summary>
        /// When a creature exits the aggro aura of another creature,
        /// </summary>
        [NWNEventHandler(ScriptName.OnCreatureAggroExit)]
        public static void CreatureAggroExit()
        {
            var exiting = GetExitingObject();
            var self = GetAreaOfEffectCreator(OBJECT_SELF);
            if (!IsAIEnabled(self) || !GetIsObjectValid(exiting))
                return;

            EspionageInfiltration.Complete(exiting, self);
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

        public static bool ProcessTrigger(
            uint creature,
            AITriggerType trigger,
            uint eventTarget = OBJECT_INVALID)
        {
            if (trigger != AITriggerType.Death && !IsAIEnabled(creature))
                return false;

            return NPCAI.ProcessTrigger(creature, trigger, eventTarget, BuildAllies(creature));
        }

        public static void SetAIProfile(uint creature, AIProfileType profile)
        {
            NPCAI.SetProfile(creature, profile);
        }

        public static AIProfileType GetAIProfile(uint creature)
        {
            return NPCAI.GetProfileType(creature);
        }

        /// <summary>
        /// Returns whether a creature has an effect.
        /// </summary>
        /// <param name="effectType">The type of effect to look for.</param>
        /// <param name="creature">The creature to check</param>
        /// <returns>true if creature has the effect, false otherwise</returns>
        private static bool GetHasEffect(uint creature, EffectTypeScript effectType, params EffectTypeScript[] otherEffectTypes)
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
        private static void LoadAggroEffect()
        {
            var effect = SupernaturalEffect(EffectAreaOfEffect(AreaOfEffect.CustomAoe, "crea_aggro_enter", string.Empty, "crea_aggro_exit"));
            effect = TagEffect(effect, "AGGRO_AOE");
            ApplyEffectToObject(DurationType.Permanent, effect, OBJECT_SELF);
        }

        private static void DoVFX()
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
        private static void ProcessFlags()
        {
            var self = OBJECT_SELF;
            var aiFlags = GetAIFlag(self);
            var homeLocation = GetLocalLocation(self, "HOME_LOCATION");
            var isOutsideHomeRadius = aiFlags.HasFlag(AIFlag.ReturnHome) &&
                                      IsOutsideHomeRadius(self, homeLocation);
            var highestEnmityTarget = Enmity.GetHighestEnmityTarget(self);

            if (IsLeashEvading(self))
            {
                if (!TryEndLeashEvadeAtHome(self, homeLocation))
                    ContinueLeashEvadeReturn(self, homeLocation);

                return;
            }

            var hasCombatState = GetIsInCombat(self) || GetIsObjectValid(highestEnmityTarget);
            if (hasCombatState &&
                ShouldStartCombatLeashEvade(self, highestEnmityTarget, homeLocation))
            {
                StartLeashEvade(self, homeLocation);
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
                    Enmity.AttackHighestEnmityTarget(self);

                return;
            }

            if (IsInConversation(self) ||
                GetIsInCombat(self) ||
                GetCurrentAction(self) == ActionType.RandomWalk ||
                GetCurrentAction(self) == ActionType.MoveToPoint)
                return;

            // Return Home flag
            if (isOutsideHomeRadius)
            {
                AssignCommand(self, () => ActionForceMoveToLocation(homeLocation));
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
        private static void ProcessCreatureAllies()
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
        public static void RemoveFromAlliesCache()
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
        public static void SetAIFlag(uint creature, AIFlag flags)
        {
            var flagValue = (int) flags;
            SetLocalInt(creature, "AI_FLAGS", flagValue);
        }

        /// <summary>
        /// Retrieves a set of AI flags from a particular creature. If <see cref="SetAIFlag"/> has not been called, this will return no flags.
        /// </summary>
        /// <param name="creature">The creature to retrieve from.</param>
        /// <returns>A set of AIFlags specified on a creature.</returns>
        public static AIFlag GetAIFlag(uint creature)
        {
            var flagValue = GetLocalInt(creature, "AI_FLAGS");
            return (AIFlag) flagValue;
        }

        private static IReadOnlyList<uint> BuildAllies(uint creature)
        {
            var master = GetMaster(creature);
            var hasPCMaster = GetIsObjectValid(master) && GetIsPC(master);

            if (hasPCMaster)
            {
                return Party.GetAllPartyMembers(creature);
            }

            var allies = _creatureAllies.TryGetValue(creature, out var cachedAllies)
                ? cachedAllies.ToList()
                : new List<uint>();

            if (!allies.Contains(creature))
                allies.Add(creature);

            return allies;
        }

        private static bool ShouldStartCombatLeashEvade(uint creature, uint target, Location homeLocation)
        {
            return ShouldLeashCombatTarget(creature, target, homeLocation);
        }

        private static bool ShouldLeashCombatTarget(uint creature, uint target, Location homeLocation)
        {
            if (!ShouldUseCombatLeash(creature))
                return false;

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

        private static bool ShouldUseCombatLeash(uint creature)
        {
            if (!GetAIFlag(creature).HasFlag(AIFlag.ReturnHome))
                return false;

            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetIsDM(player))
                    continue;

                if (GetArea(player) == GetArea(creature) &&
                    GetIsEnemy(player, creature))
                {
                    return true;
                }
            }

            return false;
        }

        private static float GetCombatLeashRadius(uint creature, uint target)
        {
            return CombatLeashRadius + GetHitDistance(creature) + GetHitDistance(target);
        }

        private static float GetHitDistance(uint creature)
        {
            return GetIsObjectValid(creature)
                ? CreaturePlugin.GetHitDistance(creature)
                : 0f;
        }

        public static bool IsLeashEvading(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   GetLocalBool(creature, LeashEvadeActiveVariable);
        }

        private static void StartLeashEvade(uint creature, Location homeLocation)
        {
            if (!IsLeashEvading(creature))
            {
                SetLocalBool(creature, LeashEvadeActiveVariable, true);
                SetLocalBool(creature, LeashEvadeRestorePlotFlagVariable, GetPlotFlag(creature));
                SetLocalInt(creature, LeashEvadeRestoreMovementRateVariable, GetMovementRate(creature));
                SetPlotFlag(creature, true);
            }

            RemoveEnemySourcedStatusEffects(creature);
            SetCurrentHitPoints(creature, GetMaxHitPoints(creature));
            Enmity.ClearEnmityTable(creature);
            NPCAI.ClearState(creature);
            ApplyLeashEvadeMovementRate(creature);
            DelayCommand(0.2f, () =>
            {
                if (IsLeashEvading(creature))
                    ApplyLeashEvadeMovementRate(creature);
            });
            ContinueLeashEvadeReturn(creature, homeLocation);
        }

        private static void ContinueLeashEvadeReturn(uint creature, Location homeLocation)
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

        private static bool TryEndLeashEvadeAtHome(uint creature, Location homeLocation)
        {
            if (!IsLeashEvading(creature) ||
                IsOutsideHomeRadius(creature, homeLocation))
            {
                return false;
            }

            EndLeashEvade(creature);
            return true;
        }

        private static void CompleteLeashEvadeReturn(uint creature, Location homeLocation)
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

        private static void EndLeashEvade(uint creature)
        {
            if (!IsLeashEvading(creature))
                return;

            SetCurrentHitPoints(creature, GetMaxHitPoints(creature));
            SetPlotFlag(creature, GetLocalBool(creature, LeashEvadeRestorePlotFlagVariable));
            DeleteLocalBool(creature, LeashEvadeRestorePlotFlagVariable);
            DeleteLocalBool(creature, LeashEvadeActiveVariable);
            DeleteLocalBool(creature, LeashEvadeReturnQueuedVariable);
            RestoreLeashEvadeMovementRate(creature);
        }

        public static bool TryStartCombatLeashEvade(uint creature, uint target)
        {
            return TryStartLeashEvade(creature, target);
        }

        private static bool TryStartLeashEvade(uint creature, uint target)
        {
            var homeLocation = GetLocalLocation(creature, "HOME_LOCATION");
            if (!ShouldStartCombatLeashEvade(creature, target, homeLocation))
                return false;

            StartLeashEvade(creature, homeLocation);
            return true;
        }

        private static bool TryReturnHomeAfterCombat(uint enemy)
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

        private static uint GetHighestOrEventTarget(uint creature, uint eventTarget)
        {
            var highestEnmityTarget = Enmity.GetHighestEnmityTarget(creature);
            return GetIsObjectValid(highestEnmityTarget)
                ? highestEnmityTarget
                : eventTarget;
        }

        private static void ApplyLeashEvadeMovementRate(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            CreaturePlugin.SetMovementRate(creature, MovementRate.DMFast);
            CreaturePlugin.SetMovementRateFactor(creature, LeashEvadeMovementRateFactor);
        }

        private static void RestoreLeashEvadeMovementRate(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            CreaturePlugin.SetMovementRate(
                creature,
                (MovementRate)GetLocalInt(creature, LeashEvadeRestoreMovementRateVariable));
            DeleteLocalInt(creature, LeashEvadeRestoreMovementRateVariable);
            Stat.ApplyCreatureMovementRate(creature);
        }

        private static void RemoveEnemySourcedStatusEffects(uint creature)
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

        private static bool IsOutsideHomeRadius(uint creature, Location homeLocation, float radius = ReturnHomeRadius)
        {
            return GetIsObjectValid(GetAreaFromLocation(homeLocation)) &&
                   (GetAreaFromLocation(homeLocation) != GetArea(creature) ||
                    GetDistanceBetweenLocations(GetLocation(creature), homeLocation) > radius);
        }

        public static bool IsInAggroRange(uint creature, uint target)
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
        public static void TryAcquireAggroAfterDetection(uint observer, uint target)
        {
            if (!IsAIEnabled(observer))
                return;

            Log.WriteStructured(
                LogGroup.AI,
                "Stealth detection handed off to normal aggro acquisition: Observer={Observer} Target={Target}",
                observer,
                target);

            TryAcquireAggro(observer, target);
        }

        private static void TryAcquireAggro(uint self, uint target)
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
                return;
            }

            ProcessTrigger(self, AITriggerType.Aggro, target);
            AddNearbyAllyProximityEnmity(self, target);
        }

        private static void AddNearbyAllyProximityEnmity(uint self, uint target)
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

        private static bool TryAddProximityEnmity(uint target, uint enemy)
        {
            if (Enmity.HasProximityEnmity(target, enemy))
                return false;

            Enmity.ModifyProximityEnmity(target, enemy, ProximityEnmityAmount);
            return true;
        }

        private static void RemoveProximityEnmity(uint target, uint enemy)
        {
            if (TryStartLeashEvade(enemy, target))
                return;

            if (!Enmity.RemoveProximityEnmity(target, enemy))
                return;

            var nextTarget = Enmity.GetHighestEnmityTarget(enemy);
            if (GetIsObjectValid(nextTarget))
            {
                Enmity.AttackHighestEnmityTarget(enemy);
                return;
            }

            NPCAI.ClearState(enemy);
            if (TryReturnHomeAfterCombat(enemy))
                return;

            AssignCommand(enemy, () => ClearAllActions());
        }

        public static bool IsCreatureAIEnabled(uint creature)
        {
            return IsAIEnabled(creature);
        }

        private static bool IsAIEnabled(uint creature)
        {
            return GetIsObjectValid(creature) &&
                   GetAILevel(creature) != AILevel.VeryLow;
        }
    }
}
