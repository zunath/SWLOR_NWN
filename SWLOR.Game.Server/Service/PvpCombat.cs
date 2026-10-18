using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.PvpCombatService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Player-vs-player combat. A player is in PvP combat from the first hit they deal to, or take
    /// from, another player or a player's pet until <see cref="PvpCombatRules.CombatWindowSeconds"/>
    /// pass without one. The longer it runs, the less healing they receive and the more damage
    /// players deal to them; see <see cref="PvpCombatRules"/> for the steps.
    /// </summary>
    public static class PvpCombat
    {
        private static readonly Dictionary<uint, PvpCombatState> _states = new();
        private static IDisposable _updateSchedule;

        // Indexed by healing-reduction step; the countdown before the first step is shown separately.
        private static readonly Type[] _healingStepStatusEffects =
        {
            typeof(PvpHealingReduction1StatusEffect),
            typeof(PvpHealingReduction2StatusEffect),
            typeof(PvpHealingReduction3StatusEffect),
            typeof(PvpHealingReduction4StatusEffect),
            typeof(PvpHealingReduction5StatusEffect)
        };

        private static readonly Type[] _allStatusEffects =
            new[] { typeof(PvpCombatStatusEffect) }.Concat(_healingStepStatusEffects).ToArray();

        public static bool IsPlayer(uint creature)
        {
            return GetIsObjectValid(creature) && GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);
        }

        public static bool IsPlayerControlled(uint creature)
        {
            return GetIsObjectValid(GetControllingPlayer(creature));
        }

        /// <summary>The player a creature fights for: the player itself, or the master of a player's pet.</summary>
        public static uint GetControllingPlayer(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return OBJECT_INVALID;
            if (IsPlayer(creature))
                return creature;

            var master = GetMaster(creature);
            return IsPlayer(master) ? master : OBJECT_INVALID;
        }

        /// <summary>Whether a player or a player's pet is acting on a different player.</summary>
        public static bool IsPlayerAgainstPlayer(uint source, uint target)
        {
            if (!IsPlayer(target))
                return false;

            var player = GetControllingPlayer(source);
            return GetIsObjectValid(player) && player != target;
        }

        public static bool IsInPvpCombat(uint player)
        {
            return _states.TryGetValue(player, out var state) && !IsLapsed(state, DateTime.UtcNow);
        }

        /// <summary>How long the player's current PvP combat has run, or 0 when they are not in one.</summary>
        public static double GetCombatSeconds(uint player)
        {
            var now = DateTime.UtcNow;
            return _states.TryGetValue(player, out var state) && !IsLapsed(state, now)
                ? (now - state.StartedAt).TotalSeconds
                : 0;
        }

        public static int GetHealingReductionPercent(uint player)
        {
            return IsInPvpCombat(player)
                ? PvpCombatRules.GetStepPercent(PvpCombatRules.HealingReductionSteps, GetCombatSeconds(player))
                : 0;
        }

        public static int GetDamageIncreasePercent(uint player)
        {
            return IsInPvpCombat(player)
                ? PvpCombatRules.GetStepPercent(PvpCombatRules.DamageIncreaseSteps, GetCombatSeconds(player))
                : 0;
        }

        /// <summary>
        /// Keeps both sides of a hit in PvP combat when a player or a player's pet hits another
        /// player or a player's pet.
        /// </summary>
        public static void RecordHit(uint attacker, uint target)
        {
            var attackingPlayer = GetControllingPlayer(attacker);
            if (!GetIsObjectValid(attackingPlayer))
                return;

            var targetPlayer = GetControllingPlayer(target);
            if (!GetIsObjectValid(targetPlayer) || targetPlayer == attackingPlayer)
                return;

            var now = DateTime.UtcNow;
            MarkActivity(attackingPlayer, now);
            MarkActivity(targetPlayer, now);
        }

        /// <summary>
        /// Raises damage a player or a player's pet deals to another player by that player's damage
        /// ramp. The increase belongs to the player being hit, so it can't be carried over to NPCs or
        /// to players who just joined the fight. A landed hit also keeps both sides in PvP combat.
        /// </summary>
        public static int ApplyDamageIncrease(uint defender, uint attacker, int damage, bool isLandedAttack)
        {
            if (damage <= 0)
                return damage;

            if (isLandedAttack)
                RecordHit(attacker, defender);

            return IsPlayerAgainstPlayer(attacker, defender)
                ? PvpCombatRules.IncreaseDamage(damage, GetDamageIncreasePercent(defender))
                : damage;
        }

        /// <summary>
        /// Cuts every heal a player receives during a long PvP fight: ability heals, heals over time,
        /// lifesteal, consumables, rest and natural regeneration. FP and STM are not heals and are
        /// never touched.
        /// </summary>
        [NWNEventHandler(ScriptName.OnHealBefore)]
        public static void ReduceHealing()
        {
            var target = StringToObject(EventsPlugin.GetEventData("TARGET_OBJECT_ID"));
            if (!IsPlayer(target) || GetIsDM(OBJECT_SELF))
                return;

            var percent = GetHealingReductionPercent(target);
            if (percent <= 0 ||
                !int.TryParse(EventsPlugin.GetEventData("HEAL_AMOUNT"), out var amount) ||
                amount <= 0)
            {
                return;
            }

            var reduced = PvpCombatRules.ReduceHealing(amount, percent);
            // NWNX applies the result only to a skipped heal, which it still applies with the new amount.
            EventsPlugin.SkipEvent();
            EventsPlugin.SetEventResult(reduced.ToString());
            if (CombatRecorder.IsRecording)
                CombatRecorder.RecordPvpHealingReduced(target, amount, reduced);
        }

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void EndOnDeath()
        {
            End(GetLastPlayerDied(), false);
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ForgetExitingPlayer()
        {
            _states.Remove(GetExitingObject());
        }

        /// <summary>
        /// Starts or moves the player's PvP combat clock to <paramref name="seconds"/> as of now, as if
        /// they had been fighting that long, with their last PvP hit <paramref name="secondsSinceLastHit"/>
        /// ago. For tests only.
        /// </summary>
        public static void SetCombatSecondsForTesting(uint player, double seconds, double secondsSinceLastHit = 0)
        {
            var now = DateTime.UtcNow;
            MarkActivity(player, now);
            var state = _states[player];
            state.StartedAt = now - TimeSpan.FromSeconds(seconds);
            state.LastActivityAt = now - TimeSpan.FromSeconds(secondsSinceLastHit);
            Update(player, now);
        }

        /// <summary>Ends the player's PvP combat at once. For tests only.</summary>
        public static void EndForTesting(uint player)
        {
            End(player, false);
        }

        private static bool IsLapsed(PvpCombatState state, DateTime now)
        {
            return (now - state.LastActivityAt).TotalSeconds > PvpCombatRules.CombatWindowSeconds;
        }

        // Runs while damage is being calculated, sometimes inside a native hook, so it only records
        // the time; status icons and messages change in the next update.
        private static void MarkActivity(uint player, DateTime now)
        {
            if (_states.TryGetValue(player, out var state) && !IsLapsed(state, now))
            {
                state.LastActivityAt = now;
                return;
            }

            // A fight that lapsed before the update noticed starts over; the update swaps the icon.
            _states[player] = new PvpCombatState(now);
            _updateSchedule ??= Scheduler.ScheduleRepeating(UpdateAll, TimeSpan.FromSeconds(1));
        }

        private static void UpdateAll()
        {
            var now = DateTime.UtcNow;
            foreach (var player in _states.Keys.ToList())
                Update(player, now);

            if (_states.Count == 0)
            {
                _updateSchedule?.Dispose();
                _updateSchedule = null;
            }
        }

        private static void Update(uint player, DateTime now)
        {
            if (!_states.TryGetValue(player, out var state))
                return;

            if (!GetIsObjectValid(player))
            {
                _states.Remove(player);
                return;
            }

            if (IsLapsed(state, now))
            {
                End(player, true);
                return;
            }

            var seconds = (now - state.StartedAt).TotalSeconds;
            var healingStep = PvpCombatRules.GetStepIndex(PvpCombatRules.HealingReductionSteps, seconds);
            var statusEffect = GetStepStatusEffect(healingStep);
            if (healingStep != state.ShownHealingStep || !StatusEffect.HasStatusEffect(player, statusEffect))
            {
                ShowHealingStep(player, healingStep, statusEffect, seconds);
                if (healingStep != state.ShownHealingStep)
                {
                    SendMessageToPC(player, ColorToken.Combat(healingStep < 0
                        ? "You are in PvP combat. Healing you receive starts dropping at 2:00 and damage players deal to you starts rising at 3:00. It ends after 30 seconds without PvP damage."
                        : $"PvP combat {FormatTime(PvpCombatRules.HealingReductionSteps[healingStep].AtSeconds)}: healing you receive is reduced by {PvpCombatRules.HealingReductionSteps[healingStep].Percent}%."));
                }

                state.ShownHealingStep = healingStep;
            }

            var damageStep = PvpCombatRules.GetStepIndex(PvpCombatRules.DamageIncreaseSteps, seconds);
            if (damageStep > state.AnnouncedDamageStep)
            {
                var step = PvpCombatRules.DamageIncreaseSteps[damageStep];
                SendMessageToPC(player, ColorToken.Combat(
                    $"PvP combat {FormatTime(step.AtSeconds)}: damage players deal to you is increased by {step.Percent}%."));
                state.AnnouncedDamageStep = damageStep;
            }
        }

        private static Type GetStepStatusEffect(int healingStep)
        {
            return healingStep < 0 ? typeof(PvpCombatStatusEffect) : _healingStepStatusEffects[healingStep];
        }

        // Each icon's timer counts down to the next step; the last step lasts until the fight ends.
        private static void ShowHealingStep(uint player, int healingStep, Type statusEffect, double seconds)
        {
            foreach (var other in _allStatusEffects.Where(type => type != statusEffect))
                StatusEffect.RemoveStatusEffect(player, other, false);

            var steps = PvpCombatRules.HealingReductionSteps;
            var duration = healingStep + 1 < steps.Count
                ? (float)Math.Max(1, steps[healingStep + 1].AtSeconds - seconds) + 1f
                : -1f;
            StatusEffect.RemoveStatusEffect(player, statusEffect, false);
            StatusEffect.ApplyStatusEffect(player, player, statusEffect, duration);
        }

        private static void End(uint player, bool sendMessage)
        {
            if (!_states.Remove(player) || !GetIsObjectValid(player))
                return;

            foreach (var statusEffect in _allStatusEffects)
                StatusEffect.RemoveStatusEffect(player, statusEffect, false);

            if (sendMessage)
                SendMessageToPC(player, ColorToken.Combat("PvP combat has ended. Healing and damage are back to normal."));
        }

        private static string FormatTime(float seconds)
        {
            return $"{(int)seconds / 60}:{(int)seconds % 60:00}";
        }
    }
}
