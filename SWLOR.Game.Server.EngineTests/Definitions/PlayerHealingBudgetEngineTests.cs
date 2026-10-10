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
    public static class PlayerHealingBudgetEngineTests
    {
        [EngineTest("Player Med Kit healing uses finite stamina and consumes medical supplies", Category = "PlayerHealing", TimeoutSeconds = 30f)]
        public static async Task MedKitBudget(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            var target = ctx.SpawnCreature("civilian", 1f);
            await ctx.DelaySecondsAsync(1f);
            ctx.SuppressNPCNaturalRegen(target);
            Stat.SetNPCMaxHitPoints(target, 1000, true);
            ObjectPlugin.SetCurrentHitPoints(target, 1);
            CreaturePlugin.SetRawAbilityScore(player, AbilityType.Willpower, 26);
            fixture.Update(record =>
            {
                record.BaseStats[AbilityType.Willpower] = 26;
                record.Perks[PerkType.MedKit] = 4;
                record.Perks[PerkType.MedicalInjectorRig] = 2;
            });
            TemporaryStatModifier.Add(target, StatType.HealingReceivedPercentAdjustment, 35, 60f);
            CreaturePlugin.AddFeat(player, FeatType.MedKit4);
            ctx.SetResources(player, 100, 8);
            uint supplies = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                supplies = CreateItemOnObject("med_supplies_50", player);
                SetItemStackSize(supplies, 1);
            });
            ctx.Assert(GetIsObjectValid(supplies), "one medical supply exists");
            await ctx.WaitUntilAsync(() => GetItemPossessor(supplies) == player, 5f,
                "medical supplies to enter the player's inventory");
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                ctx.AssertEqual(supplies, GetItemPossessedBy(player, "med_supplies"), "native inventory tag lookup"));
            var before = GetCurrentHitPoints(target);
            var activated = false;
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                activated = UsePerkFeat.TryUseAbility(player, target, FeatType.MedKit4, GetLocation(target)));
            ctx.Assert(activated, "Med Kit IV activates with exactly enough stamina and one supply");
            ctx.AssertEqual(before, GetCurrentHitPoints(target), "healing waits for the cast to complete");
            await ctx.WaitUntilAsync(() => GetCurrentHitPoints(target) > before, 5f, "the player's direct heal");
            var expected = AbilityEffectScaling.ScaleDirectEffect(240, 26);
            expected = Stat.ApplyOutgoingAbilityHealingAdjustment(player, expected);
            expected = Stat.ApplyHealingReceivedAdjustment(target, expected, false);
            var observed = GetCurrentHitPoints(target) - before;
            ctx.AssertEqual(expected, observed, "healing includes ordinary WIL, Injector Rig and received-healing bonuses");
            ctx.Assert(observed < 500, "this legal combination restores less than half a health bar per cast");
            ctx.AssertEqual(0, Stat.GetCurrentStamina(player), "one rank-IV heal consumes the eight-STM budget");
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(supplies), 3f, "the medical supply to be consumed");
            ctx.Assert(!Ability.CanUseAbility(player, target, FeatType.MedKit4, 4, GetLocation(target)), "healing cannot be spammed during recast");
            await ctx.DelaySecondsAsync(7f);
            ctx.Assert(!Ability.CanUseAbility(player, target, FeatType.MedKit4, 4, GetLocation(target)), "an exhausted medic cannot heal after recast");
            ctx.SetResources(player, 100, 8);
            ctx.Assert(!Ability.CanUseAbility(player, target, FeatType.MedKit4, 4, GetLocation(target)), "restoring stamina does not replace the consumed supply");
            ctx.Log($"Med Kit IV: {observed}/1000 HP restored, 8 STM and one supply consumed.");
        }
    }
}
