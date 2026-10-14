using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static class HitPointCostAbilityEngineTests
    {
        [EngineTest("HP-cost abilities cannot be used at 1 HP, where nothing is left to pay", Category = "Ability", TimeoutSeconds = 30f)]
        public static async Task RefusedAtOneHitPoint(EngineTestContext ctx)
        {
            var caster = ctx.SpawnCreature("nw_bandit001", -0.5f);
            var target = ctx.SpawnCreature("nw_rat001", 1.5f);
            SetAILevel(caster, AILevel.VeryLow);
            SetAILevel(target, AILevel.VeryLow);
            ctx.MakeHostile(target);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 60f);
            await ctx.WaitFrameAsync();

            ctx.SetResources(caster, 9999, 9999);
            ctx.SuppressNPCNaturalRegen(caster);
            ctx.SetNPCPerkLevel(caster, PerkType.SacrificialBlade, 1);
            CreaturePlugin.AddFeat(caster, FeatType.SacrificialBlade1);
            await ctx.EquipItemAsync(caster, "nw_wswgs001", InventorySlot.RightHand);
            ObjectPlugin.SetCurrentHitPoints(caster, 1);

            var stamina = Stat.GetCurrentStamina(caster);
            var activated = true;
            var denial = string.Empty;
            await ctx.ExecuteInCreatureContextAsync(caster, () =>
            {
                activated = UsePerkFeat.TryUseAbility(caster, target, FeatType.SacrificialBlade1, GetLocation(target));
                denial = Ability.GetLastActivationDenialReason();
            });

            ctx.Assert(!activated, "Sacrificial Blade is refused at 1 HP");
            ctx.AssertEqual("You need more than 1 HP to use this ability.", denial, "denial reason");
            ctx.AssertEqual(stamina, Stat.GetCurrentStamina(caster), "a refused activation spends no STM");
            ctx.AssertEqual(1, GetCurrentHitPoints(caster), "a refused activation spends no HP");
        }

        [EngineTest("An HP cost absorbed by temporary HP spends no HP, so HP-spend riders don't fire", Category = "Ability", TimeoutSeconds = 60f)]
        public static async Task ShieldedPaymentTriggersNoRiders(EngineTestContext ctx)
        {
            Combat.SetAbilityHitResolutionOverride(true);
            try
            {
                var (unshieldedChange, maxStamina) = await StaminaChangeFromSoulBurstAsync(ctx, shielded: false, -2f);
                var (shieldedChange, _) = await StaminaChangeFromSoulBurstAsync(ctx, shielded: true, 4f);

                ctx.AssertEqual(GameMath.PercentOf(maxStamina, BloodlustPercent), unshieldedChange - shieldedChange,
                    "only the payment that reached HP restores stamina");
            }
            finally
            {
                Combat.SetAbilityHitResolutionOverride(null);
            }
        }

        private const int BloodlustPercent = 15;

        private static async Task<(int Change, int MaxStamina)> StaminaChangeFromSoulBurstAsync(EngineTestContext ctx, bool shielded, float xOffset)
        {
            var caster = ctx.SpawnCreature("nw_bandit001", xOffset);
            var target = ctx.SpawnCreature("nw_rat001", xOffset + 1.5f);
            SetAILevel(caster, AILevel.VeryLow);
            SetAILevel(target, AILevel.VeryLow);
            ctx.MakeHostile(target);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), target, 60f);
            ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(10000), target, 60f);
            await ctx.DelaySecondsAsync(1f);

            Stat.SetNPCMaxHitPoints(caster, 1000, true);
            ctx.SetResources(caster, 1000, 1000);
            ctx.SuppressNPCNaturalRegen(caster);
            ctx.SetNPCPerkLevel(caster, PerkType.SoulBurst, 1);
            CreaturePlugin.AddFeat(caster, FeatType.SoulBurst1);
            await ctx.EquipItemAsync(caster, "nw_wswgs001", InventorySlot.RightHand);
            TemporaryStatModifier.Add(caster, StatType.HeavyVibrobladeOffenseHitPointSpendStaminaRestoreBasePercent, BloodlustPercent, 60f);
            Stat.ReduceStamina(caster, Stat.GetMaxStamina(caster) / 2);
            if (shielded)
                TemporaryHitPointEffects.ApplyFlat(caster, "TEST_SHIELD", 500, 60f);

            var before = Stat.GetCurrentStamina(caster);
            var activated = false;
            var denial = string.Empty;
            await ctx.ExecuteInCreatureContextAsync(caster, () =>
            {
                activated = UsePerkFeat.TryUseAbility(caster, target, FeatType.SoulBurst1, GetLocation(target));
                denial = Ability.GetLastActivationDenialReason();
            });
            ctx.Assert(activated, $"Soul Burst activates: {denial}");
            await ctx.DelaySecondsAsync(1f);
            if (shielded)
                ctx.AssertEqual(1000, ObjectPlugin.GetCurrentHitPoints(caster), "the shield took the whole payment");

            return (Stat.GetCurrentStamina(caster) - before, Stat.GetMaxStamina(caster));
        }
    }
}
