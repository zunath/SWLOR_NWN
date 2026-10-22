using System;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Stances whose descriptions mention a timed rider ("+5 Ranged Deflection for 30 seconds")
    /// once passed that rider's duration as the stance's own duration, so the stance switched
    /// itself off after 30 seconds. These tests keep each stance on through and after its rider.
    /// </summary>
    public static class StancePersistenceEngineTests
    {
        private const string CasterResref = "nw_bandit001";
        private const string TargetResref = "nw_rat001";
        private const string SaberstaffResref = "vet_trnsabstaff";
        private const string SpearResref = "nw_wplhb001";
        private const string VibroknifeResref = "nw_wswdg001";
        private const int ResourcePool = 200;

        [EngineTest("Tempest Stance stays on after its 30s area-ability Ranged Deflection rider expires",
            Category = "Ability", TimeoutSeconds = 120f)]
        public static async Task TempestStanceOutlastsRangedDeflectionRider(EngineTestContext ctx)
        {
            var caster = await ActivateStanceAsync(ctx, FeatType.TempestStance1, SaberstaffResref,
                typeof(TempestStanceStatusEffect));
            var target = ctx.SpawnCreature(TargetResref, 1.5f, 0f);
            ctx.MakeHostile(target);

            await ctx.ExecuteInCreatureContextAsync(caster, () => TriggerHostileAbilityRiders(caster, target, SkillType.Saberstaff, isArea: true));
            ctx.AssertEqual(5, TemporaryStatModifier.GetStatAdjustment(caster, StatType.RangedDeflection),
                "Ranged Deflection granted by Tempest Stance's area-ability rider");

            await ctx.WaitUntilAsync(
                () => TemporaryStatModifier.GetStatAdjustment(caster, StatType.RangedDeflection) == 0,
                45f,
                "Tempest Stance's 30s Ranged Deflection rider to expire");
            await AssertStanceStillOnAsync(ctx, caster, typeof(TempestStanceStatusEffect));
        }

        [EngineTest("Vigor Stance stays on after its 30s hostile-ability Evasion rider expires",
            Category = "Ability", TimeoutSeconds = 120f)]
        public static async Task VigorStanceOutlastsEvasionRider(EngineTestContext ctx)
        {
            var caster = await ActivateStanceAsync(ctx, FeatType.VigorStance1, SpearResref,
                typeof(VigorStanceStatusEffect));
            var target = ctx.SpawnCreature(TargetResref, 1.5f, 0f);
            ctx.MakeHostile(target);

            await ctx.ExecuteInCreatureContextAsync(caster, () => TriggerHostileAbilityRiders(caster, target, SkillType.Spear, isArea: false));
            ctx.AssertEqual(8, TemporaryStatModifier.GetStatAdjustment(caster, StatType.EvasionPercentAdjustment),
                "Evasion granted by Vigor Stance's hostile-ability rider");

            await ctx.WaitUntilAsync(
                () => TemporaryStatModifier.GetStatAdjustment(caster, StatType.EvasionPercentAdjustment) == 0,
                45f,
                "Vigor Stance's 30s Evasion rider to expire");
            await AssertStanceStillOnAsync(ctx, caster, typeof(VigorStanceStatusEffect));
        }

        [EngineTest("Shadowflow stays on past the 18s duration of the Hamstring it inflicts",
            Category = "Ability", TimeoutSeconds = 90f)]
        public static async Task ShadowflowOutlastsHamstringDuration(EngineTestContext ctx)
        {
            var caster = await ActivateStanceAsync(ctx, FeatType.ShadowflowStance1, VibroknifeResref,
                typeof(ShadowflowStatusEffect));

            // The old timed stance expired on the first status tick after 18 seconds.
            await ctx.DelaySecondsAsync(28f);
            await AssertStanceStillOnAsync(ctx, caster, typeof(ShadowflowStatusEffect));
        }

        private static async Task<uint> ActivateStanceAsync(
            EngineTestContext ctx,
            FeatType feat,
            string weaponResref,
            Type stanceType)
        {
            var caster = ctx.SpawnCreature(CasterResref, -0.5f, 0f);
            SetAILevel(caster, AILevel.VeryLow);
            await NwTask.NextFrame();
            ctx.SetResources(caster, ResourcePool, ResourcePool);
            ctx.SuppressNPCNaturalRegen(caster);
            CreaturePlugin.AddFeat(caster, feat);
            await ctx.EquipItemAsync(caster, weaponResref, InventorySlot.RightHand);

            await ctx.WaitUntilAsync(() => GetCommandable(caster), 5f, "the caster to become commandable");
            var used = false;
            var denial = string.Empty;
            await ctx.ExecuteInCreatureContextAsync(caster, () =>
            {
                used = UsePerkFeat.TryUseAbility(caster, caster, feat, GetLocation(caster));
                denial = Ability.GetLastActivationDenialReason();
            });
            ctx.Assert(used, $"{feat} should activate on its caster: {denial}");
            await ctx.WaitUntilAsync(() => StatusEffect.HasStatusEffect(caster, stanceType), 10f,
                $"{stanceType.Name} to appear on the caster");

            AssertPermanent(ctx, caster, stanceType);
            return caster;
        }

        private static void TriggerHostileAbilityRiders(uint caster, uint target, SkillType skill, bool isArea)
        {
            var ability = new AbilityDetail
            {
                IsHostileAbility = true,
                IsAreaAbility = isArea,
                IsSingleTargetAbility = !isArea,
                SkillType = skill
            };
            Combat.ApplyAbilityActivatedEffects(caster, target, FeatType.Invalid, ability, new AbilityImpactSummary
            {
                Ability = ability,
                SkillType = skill,
                IsAreaAbility = isArea,
                IsSingleTargetAbility = !isArea,
                ImpactedTargetCount = 1,
                ImpactedTargets = new[] { target }
            });
        }

        private static async Task AssertStanceStillOnAsync(EngineTestContext ctx, uint caster, Type stanceType)
        {
            // Status effects tick about every 6 seconds; give a timed stance two ticks to lapse.
            await ctx.DelaySecondsAsync(13f);
            ctx.Assert(StatusEffect.HasStatusEffect(caster, stanceType),
                $"{stanceType.Name} should stay on until toggled off.");
            AssertPermanent(ctx, caster, stanceType);
        }

        private static void AssertPermanent(EngineTestContext ctx, uint caster, Type stanceType)
        {
            var remaining = StatusEffect.GetStatusEffect(caster, stanceType).GetRemainingDurationSeconds(DateTime.UtcNow);
            ctx.Assert(remaining < 0f,
                $"{stanceType.Name} should have no duration, but has {remaining:0.0}s remaining.");
        }
    }
}
