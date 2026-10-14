using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Effects;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Report
{
    /// <summary>
    /// Upper bounds on the work one run of a scenario can do, so an impractical batch is refused before it starts.
    /// Values the simulation rejects count as no work here; the simulation reports them.
    /// </summary>
    internal static class BatchWork
    {
        /// <summary>
        /// Every event one run can process, however coarse its time step: each hit on each target an attack or ability
        /// reaches, each ally a heal, shield or zone pulse reaches, and each combatant's heartbeat and Convergence tick.
        /// </summary>
        public static double EventsPerRun(ScenarioSpec scenario, RuleSet rules, AbilityCatalog catalog)
        {
            var duration = (double)scenario.DurationSeconds;
            var step = (double)scenario.TimeStepSeconds;
            if (!double.IsFinite(duration) || duration <= 0 || !double.IsFinite(step) || step <= 0 || scenario.Teams == null)
                return 0;

            var teamSizes = scenario.Teams.Select(team => team?.Members?.Sum(member => (double)Math.Max(1, member?.Count ?? 1)) ?? 0).ToList();
            var everyone = teamSizes.Sum();
            double events = 0;
            for (var teamIndex = 0; teamIndex < scenario.Teams.Count; teamIndex++)
            {
                var allies = teamSizes[teamIndex];
                var enemies = everyone - allies;
                foreach (var member in scenario.Teams[teamIndex]?.Members ?? new List<CombatantSpec>())
                {
                    if (member == null)
                        continue;

                    var count = Math.Max(1, member.Count);
                    // Recurring events catch up inside one step, so they count by the duration, not the steps.
                    events += count * duration * (1 / Simulation.HeartbeatSeconds + 1 / Simulation.ConvergenceTickSeconds);

                    var attack = member.AutoAttack;
                    if (attack != null && float.IsFinite(attack.AttacksPerSecond) && attack.AttacksPerSecond > 0)
                        events += count * attack.AttacksPerSecond * duration * RollsPerUse(attack.Damage, enemies);

                    foreach (var use in member.Abilities ?? new List<AbilityUseSpec>())
                    {
                        if (use == null)
                            continue;

                        var secondsBetweenUses = SecondsBetweenUses(member, use, step, rules, catalog);
                        var perUse = RollsPerUse(use.Damage, enemies) + ApplicationsPerUse(use, allies);
                        if (perUse > 0)
                            events += count * duration / secondsBetweenUses * perUse;
                        events += count * ZoneScansPerRun(use, duration, step, secondsBetweenUses);
                    }
                }
            }

            return events;
        }

        /// <summary>Priority-list entries one run can check: while nothing can be cast, a combatant checks its whole list every step.</summary>
        public static double PriorityChecksPerRun(ScenarioSpec scenario)
        {
            var duration = (double)scenario.DurationSeconds;
            var step = (double)scenario.TimeStepSeconds;
            if (!double.IsFinite(duration) || duration <= 0 || !double.IsFinite(step) || step <= 0 || scenario.Teams == null)
                return 0;

            return scenario.Teams
                .SelectMany(team => team?.Members ?? new List<CombatantSpec>())
                .Where(member => member != null)
                .Sum(member => Math.Max(1, member.Count) * (double)(member.Abilities?.Count ?? 0) * (duration / step));
        }

        // Each zone cast keeps its own pulse schedule until its last tick, and every step looks at every live one.
        private static double ZoneScansPerRun(AbilityUseSpec use, double duration, double step, double secondsBetweenUses)
        {
            if (!Enum.TryParse<FeatType>(use.Feat, out var feat) || !EffectCatalog.TryGet(feat, out var effect) ||
                effect.Kind != EffectKind.PartyHealOverTime || effect.TickSeconds <= 0)
                return 0;

            var lifetime = Math.Min(duration, Math.Max(1, effect.Ticks) * (double)effect.TickSeconds);
            return duration / step * Math.Max(1, lifetime / secondsBetweenUses);
        }

        // A party heal or shield reaches every ally up to MaxTargets, and a zone does so on each of its pulses.
        private static double ApplicationsPerUse(AbilityUseSpec use, double allies)
        {
            if (!Enum.TryParse<FeatType>(use.Feat, out var feat) || !EffectCatalog.TryGet(feat, out var effect))
                return 0;

            var reach = use.MaxTargets.HasValue ? Math.Min(Math.Max(1, use.MaxTargets.Value), allies) : allies;
            return effect.Kind switch
            {
                EffectKind.PartyTemporaryHp => reach,
                EffectKind.PartyHealOverTime => reach * Math.Max(1, effect.Ticks),
                EffectKind.HealOverTime => Math.Max(1, effect.Ticks),
                _ => 1
            };
        }

        private static double RollsPerUse(DamageSpec damage, double enemies)
        {
            if (damage == null)
                return 0;

            var targets = damage.Area ? enemies : Math.Min(Math.Max(1, damage.Targets), enemies);
            return Math.Max(1, damage.Hits) * targets;
        }

        // A combatant decides at most once a step and once per reaction time, and a known feat waits out its recast.
        private static double SecondsBetweenUses(CombatantSpec member, AbilityUseSpec use, double step, RuleSet rules, AbilityCatalog catalog)
        {
            var seconds = step;
            if (float.IsFinite(member.ReactionSeconds))
                seconds = Math.Max(seconds, member.ReactionSeconds);
            if (float.IsFinite(use.MinIntervalSeconds))
                seconds = Math.Max(seconds, use.MinIntervalSeconds);
            if (Enum.TryParse<FeatType>(use.Feat, out var feat) && catalog.Abilities.TryGetValue(feat, out var info))
                seconds = Math.Max(seconds, rules.GetRecastSeconds(info));
            return seconds;
        }
    }
}
