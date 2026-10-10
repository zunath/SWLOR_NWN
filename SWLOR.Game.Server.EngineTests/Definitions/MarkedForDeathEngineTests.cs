using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Marked for Death amplifies the marker's next three damaging hits by a percentage of each
    /// hit. A flat per-hit bonus used to triple low-level enemies' auto-attacks.
    /// </summary>
    public static class MarkedForDeathEngineTests
    {
        private const int BaseDamage = 20;

        [EngineTest("Marked for Death amplifies only the marker's next three hits by half", Category = "StatusEffect", TimeoutSeconds = 30f)]
        public static async Task MarkAmplifiesMarkersNextThreeHits(EngineTestContext ctx)
        {
            var marker = ctx.SpawnCreature("nw_bandit001");
            var bystander = ctx.SpawnCreature("nw_bandit001", 1f);
            var target = ctx.SpawnCreature("nw_rat001", 2f);
            await ctx.WaitFrameAsync();
            Prepare(ctx, marker);
            Prepare(ctx, bystander);
            Prepare(ctx, target);
            ctx.MakeHostile(target);

            var unmarked = Hit(marker, target);
            ctx.Assert(unmarked > 0, "the baseline hit must deal damage");

            ctx.Assert(StatusEffect.ApplyStatusEffect(marker, target, typeof(MarkedForDeathStatusEffect), 60f, CombatDamageType.Physical),
                "Marked for Death applies");
            var mark = (MarkedForDeathStatusEffect)StatusEffect.GetStatusEffect(target, typeof(MarkedForDeathStatusEffect));

            ctx.AssertEqual(unmarked, Hit(bystander, target), "another attacker's hit is not amplified");
            ctx.AssertEqual(MarkedForDeathStatusEffect.AttackLimit, mark.RemainingAttacks,
                "another attacker's hit does not spend a charge");

            var expectedMarked = unmarked * (100 + MarkedForDeathStatusEffect.DamageTakenFromSourcePercent) / 100f;
            for (var hit = 1; hit <= MarkedForDeathStatusEffect.AttackLimit; hit++)
            {
                var marked = Hit(marker, target);
                ctx.Assert(Math.Abs(marked - expectedMarked) <= 1f,
                    $"marked hit {hit} should deal about {expectedMarked} (unmarked {unmarked}), dealt {marked}");
            }

            ctx.AssertEqual(0, mark.RemainingAttacks, "three marker hits spend every charge");
            await ctx.WaitUntilAsync(() => !StatusEffect.HasStatusEffect(target, typeof(MarkedForDeathStatusEffect)),
                5f, "the spent mark to be removed");
            ctx.AssertEqual(unmarked, Hit(marker, target), "hits after the mark is spent are not amplified");
            ctx.Log($"Marked for Death: unmarked {unmarked}, marked {expectedMarked:0.#} per hit.");
        }

        private static int Hit(uint attacker, uint target)
        {
            return Ability.ApplyCombatImpact(
                attacker, target, GetLocation(target), SkillType.Rifle, BaseDamage, 0, null, false,
                damageType: CombatDamageType.Physical,
                resolvesHit: false, canCritical: false, useUnscaledDamage: true);
        }

        private static void Prepare(EngineTestContext ctx, uint creature)
        {
            ctx.SuppressNPCNaturalRegen(creature);
            Stat.SetNPCMaxHitPoints(creature, 1000, true);
            SetAILevel(creature, AILevel.VeryLow);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), creature, 120f);
        }
    }
}
