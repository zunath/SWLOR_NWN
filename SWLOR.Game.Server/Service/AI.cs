using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Feature.AIDefinition;
using SWLOR.Game.Server.Service.AIService;

namespace SWLOR.Game.Server.Service
{
    public static class AI
    {
        private static readonly Dictionary<uint, HashSet<uint>> _creatureAllies = new();
        private static readonly Dictionary<AIDefinitionType, IAIDefinition> _aiDefinitions = new();

        private const string StickyTargetRounds = "AI_STICKY_TARGET_ROUNDS";

        [NWNEventHandler("mod_cache")]
        public static void CacheAIData()
        {
            _aiDefinitions[AIDefinitionType.Generic] = new GenericAIDefinition();
            _aiDefinitions[AIDefinitionType.Droid] = new DroidAIDefinition();
        }

        /// <summary>
        /// Entry point for creature heartbeat logic.
        /// </summary>
        [NWNEventHandler("crea_heartbeat")]
        public static void CreatureHeartbeat()
        {
            ExecuteScript("crea_hb_bef", OBJECT_SELF);
            Stat.RestoreNPCStats(true);
            ProcessFlags();
            AttackHighestEnmityTarget();
            ExecuteScript("cdef_c2_default1", OBJECT_SELF);
            ExecuteScript("crea_hb_aft", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature perception logic.
        /// </summary>
        [NWNEventHandler("crea_perception")]
        public static void CreaturePerception()
        {
            ExecuteScript("crea_perc_bef", OBJECT_SELF);
            // This is a stripped-down version of the default NWN perception event.
            // We handle most of our perception logic with the aggro aura effect.
            ProcessCreatureAllies();
            ExecuteScript("crea_perc_aft", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature combat round end logic.
        /// </summary>
        [NWNEventHandler("crea_roundend")]
        public static void CreatureCombatRoundEnd()
        {
            var creature = OBJECT_SELF;
            if (!Activity.IsBusy(creature))
            {
                ExecuteScript("crea_rndend_bef", creature);
                ProcessPerkAI(AIDefinitionType.Generic, creature, true);
                ExecuteScript("cdef_c2_default3", creature);
                ExecuteScript("crea_rndend_aft", creature);
            }
        }

        /// <summary>
        /// Entry point for creature conversation logic.
        /// </summary>
        [NWNEventHandler("crea_convo")]
        public static void CreatureConversation()
        {
            ExecuteScript("crea_convo_bef", OBJECT_SELF);
            ExecuteScript("cdef_c2_default4", OBJECT_SELF);

            var conversation = GetLocalString(OBJECT_SELF, "CONVERSATION");
            if (!string.IsNullOrWhiteSpace(conversation))
            {
                var talker = GetLastSpeaker();
                Dialog.StartConversation(talker, OBJECT_SELF, conversation);
            }
            ExecuteScript("crea_convo_aft", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature physical attacked logic
        /// </summary>
        [NWNEventHandler("crea_attacked")]
        public static void CreaturePhysicalAttacked()
        {
            ExecuteScript("crea_attack_bef", OBJECT_SELF);
            ExecuteScript("cdef_c2_default5", OBJECT_SELF);
            ExecuteScript("crea_attack_aft", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature damaged logic
        /// </summary>
        [NWNEventHandler("crea_damaged")]
        public static void CreatureDamaged()
        {
            ExecuteScript("crea_damaged_bef", OBJECT_SELF);
            ExecuteScript("cdef_c2_default6", OBJECT_SELF);
            ExecuteScript("crea_damaged_aft", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature death logic
        /// </summary>
        [NWNEventHandler("crea_death")]
        public static void CreatureDeath()
        {
            ExecuteScript("crea_death_bef", OBJECT_SELF);
            RemoveFromAlliesCache();
            ExecuteScript("cdef_c2_default7", OBJECT_SELF);
            ExecuteScript("crea_death_aft", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature disturbed logic
        /// </summary>
        [NWNEventHandler("crea_disturb")]
        public static void CreatureDisturbed()
        {
            ExecuteScript("crea_disturb_bef", OBJECT_SELF);
            ExecuteScript("cdef_c2_default8", OBJECT_SELF);
            ExecuteScript("crea_disturb_aft", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature spawn logic
        /// </summary>
        [NWNEventHandler("crea_spawn")]
        public static void CreatureSpawn()
        {
            ExecuteScript("crea_spawn_bef", OBJECT_SELF);
            Stat.LoadNPCStats();
            LoadAggroEffect();
            DoVFX();
            SetLocalLocation(OBJECT_SELF, "HOME_LOCATION", GetLocation(OBJECT_SELF));
            ExecuteScript("cdef_c2_default9", OBJECT_SELF);
            ExecuteScript("crea_spawn_aft", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature rested logic
        /// </summary>
        [NWNEventHandler("crea_rested")]
        public static void CreatureRested()
        {
            ExecuteScript("crea_rested_bef", OBJECT_SELF);
            ExecuteScript("cdef_c2_defaulta", OBJECT_SELF);
            ExecuteScript("crea_rested_aft", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature spell cast at logic
        /// </summary>
        [NWNEventHandler("crea_spellcastat")]
        public static void CreatureSpellCastAt()
        {
            ExecuteScript("crea_splcast_bef", OBJECT_SELF);
            ExecuteScript("cdef_c2_defaultb", OBJECT_SELF);
            ExecuteScript("crea_splcast_aft", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature user defined logic
        /// </summary>
        [NWNEventHandler("crea_userdef")]
        public static void CreatureUserDefined()
        {
            ExecuteScript("crea_userdef_bef", OBJECT_SELF);
            ExecuteScript("cdef_c2_defaultd", OBJECT_SELF);
            ExecuteScript("crea_userdef_aft", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature blocked logic
        /// </summary>
        [NWNEventHandler("crea_blocked")]
        public static void CreatureBlocked()
        {
            ExecuteScript("crea_block_bef", OBJECT_SELF);
            ExecuteScript("cdef_c2_defaulte", OBJECT_SELF);
            ExecuteScript("crea_block_aft", OBJECT_SELF);
        }

        /// <summary>
        /// When a creature enters the aggro aura of another creature, increase their enmity and start the aggro process.
        /// Invisible creatures do not trigger this.
        /// </summary>
        [NWNEventHandler("crea_aggro_enter")]
        public static void CreatureAggroEnter()
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
        /// When a creature exits the aggro aura of another creature, 
        /// </summary>
        [NWNEventHandler("crea_aggro_exit")]
        public static void CreatureAggroExit()
        {
        }

        /// <summary>
        /// When a creature's aggro aura heartbeat fires, 
        /// </summary>
        [NWNEventHandler("crea_aggro_hb")]
        public static void CreatureAggroHeartbeat()
        {
        }

        /// <summary>
        /// Fail-safe to ensure the creature attacks 
        /// </summary>
        private static void AttackHighestEnmityTarget()
        {
            var self = OBJECT_SELF;
            if (GetIsInCombat(self))
                return;

            var target = Enmity.GetHighestEnmityTarget(self);
            if (!GetIsObjectValid(target))
                return;

            var action = GetCurrentAction(self);
            if (action != ActionType.RandomWalk && 
                action != ActionType.Invalid)
                return;

            AssignCommand(self, () =>
            {
                ClearAllActions();
                ActionAttack(target);
            });
        }

        /// <summary>
        /// Handles custom perk usage
        /// </summary>
        public static void ProcessPerkAI(AIDefinitionType aiType, uint creature, bool usesEnmity)
        {
            // Petrified - do nothing else.
            if (GetHasEffect(creature, EffectTypeScript.Petrify)) 
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
                DeleteLocalInt(creature, StickyTargetRounds);
                ClearAllActions();
                ActionAttack(target);
            }
            // The AI should stick to their same target for 3 rounds before shifting to the next highest enmity target.
            else if (usesEnmity && target != GetAttackTarget(creature))
            {
                var rounds = GetLocalInt(creature, StickyTargetRounds) + 1;
                if (rounds > 3)
                {
                    DeleteLocalInt(creature, StickyTargetRounds);
                    ClearAllActions();
                    ActionAttack(target);
                }
                SetLocalInt(creature, StickyTargetRounds, rounds);
            }
            // Perk ability usage
            else
            {
                if (!_creatureAllies.TryGetValue(creature, out var allies))
                {
                    allies = new HashSet<uint>();
                }
                allies.Add(creature);

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
        /// Forces a creature to start attacking a different target, regardless of enmity level.
        /// This also resets their sticky targeting.
        /// If either the creature or the target is invalid, nothing will happen.
        /// </summary>
        /// <param name="creature">The creature to force a target swap upon.</param>
        /// <param name="target">The new target.</param>
        public static void ForceTargetSwap(uint creature, uint target)
        {
            if (!GetIsObjectValid(creature) || !GetIsObjectValid(target))
                return;

            DeleteLocalInt(creature, StickyTargetRounds);
            AssignCommand(creature, () =>
            {
                ClearAllActions();
                ActionAttack(target);
            });
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
            var effect = SupernaturalEffect(EffectAreaOfEffect(AreaOfEffect.CustomAoe, "crea_aggro_enter", "crea_aggro_hb", "crea_aggro_exit"));
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
                GetIsObjectValid(Enmity.GetHighestEnmityTarget(self)))
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
        [NWNEventHandler("object_destroyed")]
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
    }
}
