using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Feature.AIDefinition;
using SWLOR.Game.Server.Service.AIService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class AI
    {
        private static readonly Dictionary<uint, HashSet<uint>> _creatureAllies = new Dictionary<uint, HashSet<uint>>();

        /// <summary>
        /// Entry point for creature heartbeat logic.
        /// </summary>
        [NWNEventHandler("crea_heartbeat")]
        public static void CreatureHeartbeat()
        {
            RestoreCreatureStats();
            ProcessRandomWalkFlag();
            ExecuteScript("cdef_c2_default1", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature perception logic.
        /// </summary>
        [NWNEventHandler("crea_perception")]
        public static void CreaturePerception()
        {
            // This is a stripped-down version of the default NWN perception event.
            // We handle most of our perception logic with the aggro aura effect.
            ProcessCreatureAllies();
        }

        /// <summary>
        /// Entry point for creature combat round end logic.
        /// </summary>
        [NWNEventHandler("crea_roundend")]
        public static void CreatureCombatRoundEnd()
        {
            ProcessPerkAI();
            ExecuteScript("cdef_c2_default3", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature conversation logic.
        /// </summary>
        [NWNEventHandler("crea_convo")]
        public static void CreatureConversation()
        {
            ExecuteScript("cdef_c2_default4", OBJECT_SELF);

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
        [NWNEventHandler("crea_attacked")]
        public static void CreaturePhysicalAttacked()
        {
            ExecuteScript("cdef_c2_default5", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature damaged logic
        /// </summary>
        [NWNEventHandler("crea_damaged")]
        public static void CreatureDamaged()
        {
            ExecuteScript("cdef_c2_default6", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature death logic
        /// </summary>
        [NWNEventHandler("crea_death")]
        public static void CreatureDeath()
        {
            RemoveFromAlliesCache();
            ExecuteScript("cdef_c2_default7", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature disturbed logic
        /// </summary>
        [NWNEventHandler("crea_disturb")]
        public static void CreatureDisturbed()
        {
            ExecuteScript("cdef_c2_default8", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature spawn logic
        /// </summary>
        [NWNEventHandler("crea_spawn")]
        public static void CreatureSpawn()
        {
            LoadCreatureStats();
            LoadAggroEffect();
            ExecuteScript("cdef_c2_default9", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature rested logic
        /// </summary>
        [NWNEventHandler("crea_rested")]
        public static void CreatureRested()
        {
            ExecuteScript("cdef_c2_defaulta", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature spell cast at logic
        /// </summary>
        [NWNEventHandler("crea_spellcastat")]
        public static void CreatureSpellCastAt()
        {
            ExecuteScript("cdef_c2_defaultb", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature user defined logic
        /// </summary>
        [NWNEventHandler("crea_userdef")]
        public static void CreatureUserDefined()
        {
            ExecuteScript("cdef_c2_defaultd", OBJECT_SELF);
        }

        /// <summary>
        /// Entry point for creature blocked logic
        /// </summary>
        [NWNEventHandler("crea_blocked")]
        public static void CreatureBlocked()
        {
            ExecuteScript("cdef_c2_defaulte", OBJECT_SELF);
        }

        /// <summary>
        /// When a creature enters the aggro aura of another creature, increase their enmity and start the aggro process.
        /// </summary>
        [NWNEventHandler("crea_aggro_enter")]
        public static void CreatureAggroEnter()
        {
            var entering = GetEnteringObject();
            var self = GetAreaOfEffectCreator(OBJECT_SELF);
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
        /// Handles custom perk usage
        /// </summary>
        private static void ProcessPerkAI()
        {
            var self = OBJECT_SELF;

            // Petrified - do nothing else.
            if (GetHasEffect(EffectTypeScript.Petrify, self)) return;

            // Attempt to target the highest enmity creature.
            // If no target can be determined, exit early.
            var target = GetTarget();
            if (!GetIsObjectValid(target)) return;

            // If currently randomly walking, clear all actions.
            if (GetCurrentAction(self) == ActionType.RandomWalk)
            {
                ClearAllActions();
            }

            // Switch targets if necessary
            if (target != GetAttackTarget(self) ||
                GetCurrentAction(self) == ActionType.Invalid)
            {
                ClearAllActions();
                ActionAttack(target);
            }
            // Perk ability usage
            else
            {
                if (!_creatureAllies.TryGetValue(self, out var allies))
                {
                    allies = new HashSet<uint>();
                }
                allies.Add(self);

                var (feat, featTarget) = GenericAIDefinition.DeterminePerkAbility(self, target, allies);
                if (feat != FeatType.Invalid && GetIsObjectValid(featTarget))
                {
                    Console.WriteLine($"{GetName(self)}: Feat = {feat}"); // todo debug

                    ClearAllActions();
                    ActionUseFeat(feat, featTarget);
                }
            }
        }

        /// <summary>
        /// Returns the creature with the highest enmity on this enemy's enmity table.
        /// If no target can be determined, OBJECT_INVALID will be returned.
        /// </summary>
        /// <returns>The creature with the highest enmity, or OBJECT_INVALID if it cannot be determined.</returns>
        private static uint GetTarget()
        {
            var self = OBJECT_SELF;
            var enmityTable = Enmity.GetEnmityTable(self);
            if (enmityTable.Count <= 0) return OBJECT_INVALID;

            var highest = enmityTable.OrderByDescending(o => o.Value).First();
            return highest.Key;
        }

        /// <summary>
        /// Returns whether a creature has an effect.
        /// </summary>
        /// <param name="effectType">The type of effect to look for.</param>
        /// <param name="creature">The creature to check</param>
        /// <returns>true if creature has the effect, false otherwise</returns>
        private static bool GetHasEffect(EffectTypeScript effectType, uint creature)
        {
            var effect = GetFirstEffect(creature);
            while (GetIsEffectValid(effect))
            {
                if (GetEffectType(effect) == effectType)
                {
                    return true;
                }
                effect = GetNextEffect(creature);
            }

            return false;
        }

        /// <summary>
        /// When a creature spawns, store their STM and FP as local variables.
        /// </summary>
        private static void LoadCreatureStats()
        {
            var self = OBJECT_SELF;
            
            SetLocalInt(self, "FP", Stat.GetMaxFP(self));
            SetLocalInt(self, "STAMINA", Stat.GetMaxStamina(self));
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

        /// <summary>
        /// When a creature's heartbeat fires, restore their STM and FP.
        /// </summary>
        private static void RestoreCreatureStats()
        {
            var self = OBJECT_SELF;
            var maxFP = Stat.GetMaxFP(self);
            var maxSTM = Stat.GetMaxStamina(self);
            var fp = GetLocalInt(self, "FP") + 1;
            var stm = GetLocalInt(self, "STAMINA") + 1;

            if (fp > maxFP)
                fp = maxFP;
            if (stm > maxSTM)
                stm = maxSTM;

            SetLocalInt(self, "FP", fp);
            SetLocalInt(self, "STAMINA", stm);
        }

        /// <summary>
        /// When a creature's heartbeat fires, if they have the RandomWalk AI flag,
        /// and they are not currently preoccupied (combat, talking, etc.) force them to randomly walk.
        /// </summary>
        private static void ProcessRandomWalkFlag()
        {
            var self = OBJECT_SELF;
            var aiFlags = GetAIFlag(self);
            if (!aiFlags.HasFlag(AIFlag.RandomWalk) ||
                IsInConversation(self) ||
                GetIsInCombat(self) ||
                GetCurrentAction(self) == ActionType.RandomWalk)
                return;

            if (Random.D100(1) <= 40)
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

            if (GetIsPC(lastPerceived)) return;
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

            var allies = _creatureAllies[self];

            //foreach (var ally in allies)
            for(var index = allies.Count-1; index >= 0; index--)
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
