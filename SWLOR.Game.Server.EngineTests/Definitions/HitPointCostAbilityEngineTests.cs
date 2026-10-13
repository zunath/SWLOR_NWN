using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.PerkService;
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
    }
}
