using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Creeping Terror's pulses hit hostile creatures inside the field's radius, not every creature in the area.
    /// </summary>
    public static class CreepingTerrorAreaEngineTests
    {
        private const string Resref = "nw_bandit001";
        private const float FieldRadius = 8f;

        [EngineTest("Creeping Terror pulses hit only hostile creatures inside the field", Category = "AreaPulse", TimeoutSeconds = 60f)]
        public static async Task PulsesStayInsideTheField(EngineTestContext ctx)
        {
            // Open ground with nothing placed, so line of sight and creature positions are what the test sets.
            var arena = await QuietArena.CreateAsync(ctx);
            var anchor = arena.Spawn(Resref, 8f, 90f);
            var isolated = new EngineTestContext(ctx.TestName, arena.Area, GetLocation(anchor));
            DestroyObject(anchor);
            try
            {
                await RunAsync(isolated);
            }
            finally
            {
                isolated.Cleanup();
            }
        }

        private static async Task RunAsync(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature(Resref, -2f, -3f);
            // Creeping Terror III centers its 8 m field on the targeted creature.
            var center = ctx.SpawnCreature(Resref, 3f, 0f);
            var inside = ctx.SpawnCreature(Resref, 3f, 5f);
            var friendlyInside = ctx.SpawnCreature(Resref, 3f, -4f);
            var outsideNear = ctx.SpawnCreature(Resref, -6f, 0f);
            var outsideFar = ctx.SpawnCreature(Resref, -6f, 6f);
            var hostiles = new[] { center, inside, outsideNear, outsideFar };
            // Spawn initialization resets NPC stats and pools after the first frame.
            await ctx.DelaySecondsAsync(1f);
            foreach (var hostile in hostiles)
            {
                ctx.MakeHostile(hostile);
                // A 12 HP bandit dies to the first pulse, which clears its statuses.
                Stat.SetNPCMaxHitPoints(hostile, 1000, true);
                SetImmortal(hostile, true);
                ctx.SuppressNPCNaturalRegen(hostile);
            }
            foreach (var creature in hostiles.Append(friendlyInside).Append(caster))
                SetAILevel(creature, AILevel.VeryLow);
            foreach (var creature in hostiles.Append(friendlyInside))
                ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), creature, 60f);

            ctx.Assert(GetDistanceBetween(center, inside) < FieldRadius - 1f, "the inside hostile stands well inside the field");
            ctx.Assert(GetDistanceBetween(center, friendlyInside) < FieldRadius - 1f, "the friendly creature stands inside the field");
            ctx.Assert(GetDistanceBetween(center, outsideNear) > FieldRadius + 0.5f, "the near hostile stands outside the field");
            ctx.Assert(GetDistanceBetween(center, outsideFar) > FieldRadius + 0.5f, "the far hostile stands outside the field");

            var ability = Ability.GetAbilityDetail(FeatType.CreepingTerror3);
            ctx.SetNPCPerkLevel(caster, ability.EffectiveLevelPerkType, ability.AbilityLevel);
            ctx.SetNPCResources(caster, 1000, 1000);
            CreaturePlugin.AddFeat(caster, FeatType.CreepingTerror3);

            var fpBefore = Stat.GetCurrentFP(caster);
            await ctx.ExecuteInCreatureContextAsync(caster, () =>
                ctx.Assert(UsePerkFeat.TryUseAbility(caster, center, FeatType.CreepingTerror3, GetLocation(center)),
                    $"Creeping Terror III activates: {Ability.GetLastActivationDenialReason()}"));

            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (!StatusEffect.HasStatusEffect<HobbleStatusEffect>(center) && DateTime.UtcNow < deadline)
                await ctx.WaitFrameAsync();
            ctx.Assert(StatusEffect.HasStatusEffect<HobbleStatusEffect>(center),
                $"the first pulse hobbles the targeted creature (caster FP {fpBefore} -> {Stat.GetCurrentFP(caster)}, busy {Activity.IsBusy(caster)}, " +
                $"action {GetCurrentAction(caster)}, hostiles in field {CombatAreaPulses.CountHostileCreatures(caster, GetLocation(center), FieldRadius)}, " +
                $"center HP {GetCurrentHitPoints(center)}/{GetMaxHitPoints(center)}, center hostile {GetIsReactionTypeHostile(center, caster)})");
            // Let a second pulse run so a creature outside the field has had every chance to be hit.
            await ctx.DelaySecondsAsync(3.5f);

            ctx.Assert(StatusEffect.HasStatusEffect<HobbleStatusEffect>(inside), "a hostile inside the field is hit");
            ctx.Assert(!StatusEffect.HasStatusEffect<HobbleStatusEffect>(outsideNear), "a hostile just outside the field is not hit");
            ctx.Assert(!StatusEffect.HasStatusEffect<HobbleStatusEffect>(outsideFar), "a hostile farther outside the field is not hit");
            ctx.Assert(!StatusEffect.HasStatusEffect<HobbleStatusEffect>(friendlyInside), "a non-hostile creature inside the field is not hit");
            ctx.AssertEqual(GetMaxHitPoints(outsideNear), GetCurrentHitPoints(outsideNear), "the hostile outside the field takes no pulse damage");
        }
    }
}
