using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using DurationType = SWLOR.NWN.API.NWScript.Enum.DurationType;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Measures the real interval between landed auto-attacks with and without Haste. The engine
    /// adds a fixed combat-round overhead to every cycle, so the tests assert how much Haste removes
    /// from the measured interval: X% Haste must remove X% of the weapon's raw delay.
    /// </summary>
    public static class HasteAttackTimingEngineTests
    {
        private const float ToleranceMilliseconds = 250f;

        private static uint _observedAttacker = OBJECT_INVALID;
        private static readonly List<DateTime> _hits = new();

        [EngineTest("Haste shortens the measured Heavy Vibroblade attack interval", Category = "Haste", TimeoutSeconds = 120f)]
        public static async Task HasteShortensMeasuredAttackInterval(EngineTestContext ctx)
        {
            var (attacker, target) = await SpawnFixtureAsync(ctx, "b_greatsword");
            var rawDelay = 0;
            var hastedRawDelay = 0;
            try
            {
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                {
                    ctx.AssertEqual(SkillType.HeavyVibroblade, Combat.GetEquippedWeaponSkillType(attacker),
                        "The fixture swings a Heavy Vibroblade");
                    ctx.AssertEqual(0, Combat.CalculateAttackDelayReduction(attacker), "The fixture starts without Haste");
                    rawDelay = Combat.CalculateAttackDelay(attacker);
                    ctx.AssertEqual(5000, rawDelay, "A Heavy Vibroblade (delay 300) has a 5000ms raw delay");

                    TemporaryStatModifier.Add(attacker, StatType.AttackDelayReductionPercent, 25, 1f);
                    hastedRawDelay = Combat.CalculateAttackDelay(attacker);
                    ctx.AssertEqual(25, Combat.CalculateAttackDelayReduction(attacker),
                        "The character sheet's Haste line reads temporary Haste");
                    TemporaryStatModifier.Clear(attacker);
                    ctx.AssertEqual(3750, hastedRawDelay, "25% Haste removes a quarter of the raw delay");
                    ctx.Assert(Combat.CalculateEffectiveAttackDelay(hastedRawDelay) > Combat.BaseAttackDelayMilliseconds,
                        "25% Haste keeps a Heavy Vibroblade above the swing animation floor");
                });
                var expectedCut = rawDelay - hastedRawDelay;

                StartAttacking(attacker, target);
                var unhasted = await MeasureIntervalsAsync(ctx, 3, "unhasted");

                // Temporary Haste (ability and perk riders) lands mid-attack and must reach the very
                // next scheduled swing.
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                    TemporaryStatModifier.Add(attacker, StatType.AttackDelayReductionPercent, 25, 100f));
                var temporary = await MeasureIntervalsAsync(ctx, 3, "temporary Haste");

                // Status-effect Haste (Hasten II) feeds the same stat.
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                {
                    TemporaryStatModifier.Clear(attacker);
                    StatusEffect.ApplyStatusEffect<Hasten2StatusEffect>(attacker, attacker, 100f);
                    ctx.AssertEqual(25, Combat.CalculateAttackDelayReduction(attacker), "Hasten II grants 25% Haste");
                });
                var status = await MeasureIntervalsAsync(ctx, 3, "Hasten II");

                // Skill-scoped limited Haste is supplied per swing instead of through the stat.
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                {
                    StatusEffect.RemoveStatusEffectsWithStat(attacker, StatType.AttackDelayReductionPercent, false);
                    StatusEffect.ApplyStatusEffect(attacker, attacker,
                        new LimitedHasteStatusEffect(25, 10, SkillType.HeavyVibroblade, EffectIconType.Haste, null), 100f);
                    ctx.AssertEqual(0, Combat.CalculateAttackDelayReduction(attacker),
                        "Limited Haste does not inflate the creature-wide Haste line");
                });
                var limited = await MeasureIntervalsAsync(ctx, 3, "limited Haste");

                // Stacked sources clamp at the 50% cap. The capped delay falls below the swing floor,
                // so measure attacks over a window instead of swing gaps.
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                {
                    StatusEffect.RemoveStatusEffect(attacker, typeof(LimitedHasteStatusEffect), false);
                    StatusEffect.ApplyStatusEffect<Hasten2StatusEffect>(attacker, attacker, 100f);
                    TemporaryStatModifier.Add(attacker, StatType.AttackDelayReductionPercent, 40, 100f);
                    ctx.AssertEqual(Combat.MaximumAttackDelayAdjustmentPercent, Combat.CalculateAttackDelayReduction(attacker),
                        "25% + 40% Haste clamps at the 50% cap");
                    ctx.AssertEqual(2500, Combat.CalculateAttackDelay(attacker), "Capped Haste halves the raw delay");
                });
                var capped = await MeasureAttackRateAsync(ctx, 8f);

                var baseline = Median(unhasted);
                Report(ctx, $"Heavy Vibroblade attack interval: unhasted {Format(unhasted)}; 25% temporary " +
                            $"{Format(temporary)}; Hasten II {Format(status)}; limited {Format(limited)}; capped 50% " +
                            $"{capped:0}ms per attack. Designed cut for 25% Haste: {expectedCut}ms of the {rawDelay}ms raw delay.");
                AssertCut(ctx, baseline, temporary, expectedCut, "25% temporary Haste");
                AssertCut(ctx, baseline, status, expectedCut, "Hasten II");
                AssertCut(ctx, baseline, limited, expectedCut, "25% limited Haste");
                ctx.Assert(capped < Median(temporary) - ToleranceMilliseconds,
                    $"Capped Haste attacks faster than 25% Haste (observed {capped:0}ms per attack)");
            }
            finally
            {
                StopAttacking(attacker);
            }
        }

        [EngineTest("Haste below the swing floor adds Vibroblade attacks instead of faster swings", Category = "Haste", TimeoutSeconds = 120f)]
        public static async Task HasteBelowSwingFloorAddsAttacks(EngineTestContext ctx)
        {
            var (attacker, target) = await SpawnFixtureAsync(ctx, "b_longsword");
            try
            {
                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                {
                    ctx.AssertEqual(SkillType.Vibroblade, Combat.GetEquippedWeaponSkillType(attacker),
                        "The fixture swings a Vibroblade");
                    var baseGate = Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelay(attacker));
                    TemporaryStatModifier.Add(attacker, StatType.AttackDelayReductionPercent, 15, 1f);
                    var hastedGate = Combat.CalculateEffectiveAttackDelay(Combat.CalculateAttackDelay(attacker));
                    TemporaryStatModifier.Clear(attacker);
                    ctx.Assert(baseGate > Combat.BaseAttackDelayMilliseconds,
                        $"An unhasted Vibroblade swings slower than the animation floor ({baseGate}ms)");
                    ctx.Assert(hastedGate < Combat.BaseAttackDelayMilliseconds,
                        $"15% Haste (Berserker Stance) takes a Vibroblade below the animation floor ({hastedGate}ms)");
                });

                StartAttacking(attacker, target);
                var unhastedSwings = await MeasureIntervalsAsync(ctx, 3, "unhasted");
                var unhasted = Median(unhastedSwings);

                await ctx.ExecuteInCreatureContextAsync(attacker, () =>
                    TemporaryStatModifier.Add(attacker, StatType.AttackDelayReductionPercent, 15, 100f));
                var hastedSwings = await MeasureIntervalsAsync(ctx, 3, "15% Haste");
                var hastedRate = await MeasureAttackRateAsync(ctx, 24f);

                Report(ctx, $"Vibroblade: unhasted swings {Format(unhastedSwings)}; 15% Haste swings " +
                            $"{Format(hastedSwings)} at the {Combat.BaseAttackDelayMilliseconds}ms floor, averaging " +
                            $"{hastedRate:0}ms per attack over 24s with extra rolls inside some swings.");
                ctx.Assert(Median(hastedSwings) < unhasted - ToleranceMilliseconds,
                    "Haste shortens the swing cadence down to the animation floor");
                ctx.Assert(hastedRate < Median(hastedSwings) - 100,
                    "Haste below the floor resolves extra attacks inside some swings");
            }
            finally
            {
                StopAttacking(attacker);
            }
        }

        private static async Task<(uint Attacker, uint Target)> SpawnFixtureAsync(EngineTestContext ctx, string weaponResref)
        {
            var attacker = ctx.SpawnCreature("nw_bandit001", -0.5f);
            var target = ctx.SpawnCreature("nw_rat001", 1f);
            await ctx.WaitFrameAsync();
            await ctx.EquipItemAsync(attacker, weaponResref, InventorySlot.RightHand);
            await ctx.ExecuteInCreatureContextAsync(attacker, () =>
            {
                ctx.SuppressNPCNaturalRegen(target);
                Stat.SetNPCMaxHitPoints(target, 20000, true);
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 110f);
                ctx.MakeHostile(target);
                TemporaryStatModifier.Clear(attacker);
                StatusEffect.RemoveStatusEffectsWithStat(attacker, StatType.AttackDelayReductionPercent, false);
            });
            return (attacker, target);
        }

        private static void StartAttacking(uint attacker, uint target)
        {
            _observedAttacker = attacker;
            _hits.Clear();
            Combat.SetAutoAttackHitResolutionOverride(true);
            AssignCommand(attacker, () => ActionAttack(target));
        }

        private static void StopAttacking(uint attacker)
        {
            AssignCommand(attacker, () => ClearAllActions());
            Combat.SetAutoAttackHitResolutionOverride(null);
            _observedAttacker = OBJECT_INVALID;
            _hits.Clear();
        }

        private static async Task<List<double>> MeasureIntervalsAsync(EngineTestContext ctx, int count, string label)
        {
            // Discard the cycle already in flight when the Haste state changed.
            var start = _hits.Count;
            await ctx.WaitUntilAsync(() => _hits.Count > start, 8f, $"the first {label} attack");
            var first = _hits.Count - 1;
            await ctx.WaitUntilAsync(() => _hits.Count > first + count, 8f * count, $"{count} {label} attack intervals");
            return Enumerable.Range(first, count)
                .Select(index => (_hits[index + 1] - _hits[index]).TotalMilliseconds)
                .ToList();
        }

        private static async Task<double> MeasureAttackRateAsync(EngineTestContext ctx, float seconds)
        {
            var start = _hits.Count;
            await ctx.WaitUntilAsync(() => _hits.Count > start, 8f, "the first measured attack");
            var first = _hits.Count - 1;
            var firstTime = _hits[first];
            await ctx.DelaySecondsAsync(seconds);
            var last = _hits.Count - 1;
            ctx.Assert(last > first, "The attacker keeps attacking");
            return (_hits[last] - firstTime).TotalMilliseconds / (last - first);
        }

        private static void AssertCut(EngineTestContext ctx, double baseline, List<double> hasted, int expectedCut, string label)
        {
            var cut = baseline - Median(hasted);
            ctx.Assert(Math.Abs(cut - expectedCut) <= ToleranceMilliseconds,
                $"{label} removes {expectedCut}ms from each measured attack (observed {cut:0}ms: {Format(hasted)} vs {baseline:0}ms)");
        }

        private static void Report(EngineTestContext ctx, string message)
        {
            ctx.Log(message);
            ctx.SetResultDetail(message);
            Console.WriteLine($"[ENGINE_TESTS] [{ctx.TestName}] {message}");
        }

        // A cycle occasionally waits for an extra engine round start; the median ignores one such outlier.
        private static double Median(List<double> intervals) =>
            intervals.OrderBy(value => value).ElementAt(intervals.Count / 2);

        private static string Format(List<double> intervals) =>
            $"{Median(intervals):0}ms median [{string.Join(", ", intervals.Select(value => value.ToString("0")))}]";

        [NWNEventHandler(ScriptName.OnSWLORDamage)]
        public static void ObserveAttackDamage()
        {
            if (OBJECT_SELF != _observedAttacker)
                return;
            _hits.Add(DateTime.UtcNow);
        }
    }
}
