using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Throwing;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class PiercingTossEngineTests
    {
        [EngineTest("All Piercing Toss ranks retain Bleed through hits, extensions, and expiration", Category = "StatusEffect", TimeoutSeconds = 100f)]
        public static async Task BleedSurvivesDamageAndDurationExtensions(EngineTestContext ctx)
        {
            var ranks = new[]
            {
                (FeatType.PiercingToss1, 1, 30f),
                (FeatType.PiercingToss2, 2, 36f),
                (FeatType.PiercingToss3, 3, 45f),
                (FeatType.PiercingToss4, 4, 60f)
            };
            var abilities = new PiercingTossAbilityDefinition().BuildAbilities();
            var cases = new List<(uint Target, BleedStatusEffect Bleed, int Ticks, DateTime AppliedAt, string Label, int PostHitHP)>();

            Combat.SetAbilityHitResolutionOverride(true);
            Combat.SetAutoAttackHitResolutionOverride(false);
            try
            {
                foreach (var extensionSeconds in new[] { 0, 6 })
                {
                    var source = ctx.SpawnCreature("nw_bandit001");
                    await ctx.WaitFrameAsync();
                    SetCommandable(false, source);

                    // Flurry Bleed's stat-driven rider renews the native timer in the same
                    // impact that applies Bleed, before NWN delivers removal callbacks.
                    if (extensionSeconds > 0)
                    {
                        TemporaryStatModifier.Add(source, StatType.AbilityDamageToBleedingTargetSkillType, (int)SkillType.Throwing, 120f);
                        TemporaryStatModifier.Add(source, StatType.BleedingTargetAbilityBleedDurationExtensionSeconds, extensionSeconds, 120f);
                    }

                    foreach (var (feat, rank, duration) in ranks)
                    {
                        var target = ctx.SpawnCreature("nw_rat001", 2f + rank);
                        await ctx.WaitFrameAsync();
                        SetCommandable(false, target);
                        ctx.SuppressNPCNaturalRegen(target);
                        ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(1000), target, 120f);
                        ctx.MakeHostile(target);

                        var label = $"{feat} (extension {extensionSeconds}s)";
                        var ability = abilities[feat];
                        var appliedAt = DateTime.UtcNow;
                        Ability.BeginAbilityImpact(source, ability);
                        try
                        {
                            ability.ImpactAction(source, target, rank, GetLocation(target));
                        }
                        finally
                        {
                            Ability.EndAbilityImpact(source);
                        }

                        var bleed = StatusEffect.GetStatusEffect(target, typeof(BleedStatusEffect)) as BleedStatusEffect;
                        ctx.Assert(bleed != null, $"{label}: Bleed must remain after the impact.");
                        var ticks = (int)Math.Ceiling(duration / bleed.Frequency) + extensionSeconds / 6;
                        ctx.Assert(bleed.DurationTicks == ticks, $"{label}: Bleed must have {ticks} damage ticks.");
                        AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDamage(1), target));
                        cases.Add((target, bleed, ticks, appliedAt, label, GetCurrentHitPoints(target)));
                    }
                }

                await ctx.DelaySecondsAsync(1f);
                foreach (var test in cases)
                {
                    ctx.Assert(StatusEffect.GetStatusEffect(test.Target, typeof(BleedStatusEffect)) == test.Bleed,
                        $"{test.Label}: hits and old timer callbacks must preserve Bleed.");
                    ctx.Assert(HasEffectByTag(test.Target, test.Bleed.Id), $"{test.Label}: Bleed must retain its native timer.");
                }

                await ctx.WaitUntilAsync(() => cases.All(test => test.Bleed.DurationTicks < test.Ticks), 10f, "each rank's first Bleed tick");
                foreach (var test in cases)
                    ctx.Assert(GetCurrentHitPoints(test.Target) < test.PostHitHP, $"{test.Label}: Bleed must deal periodic damage.");

                await ctx.WaitUntilAsync(() =>
                {
                    foreach (var test in cases)
                    {
                        if ((DateTime.UtcNow - test.AppliedAt).TotalSeconds < test.Ticks * test.Bleed.Frequency)
                        {
                            ctx.Assert(StatusEffect.HasStatusEffect<BleedStatusEffect>(test.Target),
                                $"{test.Label}: Bleed must not disappear before its duration ends.");
                        }
                    }

                    return cases.All(test => !StatusEffect.HasStatusEffect<BleedStatusEffect>(test.Target));
                }, 75f, "all Bleeds to finish their full durations");

                foreach (var test in cases)
                {
                    ctx.Assert(test.Bleed.WasNaturallyExpired && test.Bleed.DurationTicks == 0,
                        $"{test.Label}: Bleed must deliver its final tick before expiring.");
                }
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
                Combat.SetAutoAttackHitResolutionOverride(null);
            }
        }
    }
}
