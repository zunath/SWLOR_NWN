using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class DroidStatusImmunityEngineTests
    {
        [EngineTest("Enemy droids refuse Bleed but take Sunder", Category = "StatusEffect", TimeoutSeconds = 30f)]
        public static async Task EnemyDroidsRefuseBleedButTakeSunder(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001");
            // Probe Droid: a Robot-race World NPC whose skin carries its Bible Trauma package, not immunity.
            var droid = ctx.SpawnCreature("malspiderdroid", 2f);
            var rat = ctx.SpawnCreature("nw_rat001", -2f);
            await ctx.WaitFrameAsync();
            foreach (var target in new[] { droid, rat })
            {
                SetCommandable(false, target);
                ctx.SuppressNPCNaturalRegen(target);
                ctx.MakeHostile(target);
            }

            ctx.Assert(!StatusEffect.ApplyStatusEffect(caster, droid, typeof(BleedStatusEffect), 30f),
                "a droid must refuse Bleed");
            ctx.Assert(!StatusEffect.ApplyStatusEffect(caster, droid, typeof(HemorrhageStatusEffect), 30f),
                "a droid must refuse Hemorrhage");
            ctx.Assert(StatusEffect.ApplyStatusEffect(caster, droid, typeof(SunderStatusEffect), 30f),
                "a droid must take Sunder");
            ctx.Assert(StatusEffect.HasStatusEffect(droid, typeof(SunderStatusEffect)), "Sunder must remain on the droid");
            ctx.Assert(StatusEffect.ApplyStatusEffect(caster, rat, typeof(BleedStatusEffect), 30f),
                "a living enemy must still bleed");
        }
    }
}
