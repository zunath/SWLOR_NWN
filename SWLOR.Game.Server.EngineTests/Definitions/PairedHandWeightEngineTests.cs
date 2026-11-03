using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.EngineTests.Definitions;

/// <summary>
/// The engine adds an item's weight to its wearer on equip and subtracts the item's weight at removal time.
/// A generated hand must weigh the same at both moments, or every equip leaves weight the player cannot drop.
/// </summary>
public static class PairedHandWeightEngineTests
{
    private const int Cycles = 4;

    [EngineTest("Twin Pistol equip cycles leave the wearer's carried weight unchanged", Category = "PairedHandWeight", TimeoutSeconds = 90f)]
    public static async Task TwinPistolEquipCyclesKeepWeight(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record => record.Skills[SkillType.TwinPistol].Rank = 25);
        var actor = player.Creature;
        var main = await TwinPistolEquipmentEngineTests.CreateCarriedItem(ctx, actor, "b_twinpistol");
        var baseline = Measure(actor);
        ctx.Log($"Baseline: {baseline}, owned pistol weighs {GetWeight(main)}.");

        for (var cycle = 0; cycle < Cycles; cycle++)
        {
            await TwinPistolEquipmentEngineTests.EquipExisting(ctx, actor, main, InventorySlot.RightHand);
            var spare = await TwinPistolEquipmentEngineTests.WaitForPair(ctx, actor, main);
            if (cycle == 1)
            {
                // Login restore destroys the saved spare and builds a new one from the equipped owner.
                await ctx.ExecuteInCreatureContextAsync(actor, () => TwinPistolEquipment.ReleaseCompanions(actor));
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(spare), 6f, "logout cleanup to destroy the spare");
                await ctx.ExecuteInCreatureContextAsync(actor, () => TwinPistolEquipment.RestorePair(actor));
                spare = await TwinPistolEquipmentEngineTests.WaitForPair(ctx, actor, main);
            }
            AssertEquipped(ctx, actor, main, spare, baseline, cycle);

            var requested = cycle % 2 == 0 ? main : spare;
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ClearAllActions();
                ActionUnequipItem(requested);
            });
            await TwinPistolEquipmentEngineTests.WaitForRemovedPair(ctx, actor, main, spare);
            AssertRemoved(ctx, actor, baseline, cycle);
        }
        ctx.SetResultDetail($"{Cycles} Twin Pistol equip cycles kept carried weight at {baseline}.");
    }

    [EngineTest("Power Glove equip cycles leave the wearer's carried weight unchanged", Category = "PairedHandWeight", TimeoutSeconds = 90f)]
    public static async Task PowerGloveEquipCyclesKeepWeight(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record => record.Skills[SkillType.Unarmed].Rank = 50);
        var actor = player.Creature;
        var main = await PowerGlovePairEngineTests.CreateCarriedItem(ctx, actor, "t_pglove");
        var baseline = Measure(actor);
        ctx.Log($"Baseline: {baseline}, owned glove weighs {GetWeight(main)}.");

        for (var cycle = 0; cycle < Cycles; cycle++)
        {
            await PowerGlovePairEngineTests.EquipExisting(ctx, actor, main, InventorySlot.RightHand);
            var companion = await PowerGlovePairEngineTests.WaitForPair(ctx, actor, main);
            if (cycle == 1)
            {
                await ctx.ExecuteInCreatureContextAsync(actor, () => PowerGloveEquipment.ReleaseCompanions(actor));
                await ctx.WaitUntilAsync(() => !GetIsObjectValid(companion), 6f, "logout cleanup to destroy the cosmetic hand");
                await ctx.ExecuteInCreatureContextAsync(actor, () => PowerGloveEquipment.RestorePair(actor));
                companion = await PowerGlovePairEngineTests.WaitForPair(ctx, actor, main);
            }
            // Reconciliation re-sanitizes the equipped hand; that must not change what removal subtracts.
            await ctx.ExecuteInCreatureContextAsync(actor, () => PowerGloveEquipment.EnsurePair(actor));
            AssertEquipped(ctx, actor, main, companion, baseline, cycle);

            var requested = cycle % 2 == 0 ? main : companion;
            await ctx.ExecuteInCreatureContextAsync(actor, () =>
            {
                ClearAllActions();
                ActionUnequipItem(requested);
            });
            await PowerGlovePairEngineTests.WaitForRemovedPair(ctx, actor, main, companion);
            AssertRemoved(ctx, actor, baseline, cycle);
        }
        ctx.SetResultDetail($"{Cycles} Power Glove equip cycles kept carried weight at {baseline}.");
    }

    private static void AssertEquipped(EngineTestContext ctx, uint actor, uint main, uint companion, Weight baseline, int cycle)
    {
        var equipped = Measure(actor);
        ctx.Log($"Cycle {cycle} equipped: {equipped}, generated hand weighs {GetWeight(companion)}.");
        ctx.AssertEqual(0, GetWeight(companion), $"Cycle {cycle}: the generated hand weighs nothing");
        ctx.AssertEqual(baseline.Carried, equipped.Carried, $"Cycle {cycle}: equipping moves the owned item without adding weight");
        ctx.AssertEqual(baseline.Equipped + GetWeight(main), equipped.Equipped,
            $"Cycle {cycle}: only the owned item counts as equipped weight");
    }

    private static void AssertRemoved(EngineTestContext ctx, uint actor, Weight baseline, int cycle)
    {
        var removed = Measure(actor);
        ctx.Log($"Cycle {cycle} removed: {removed}.");
        ctx.AssertEqual(baseline.Equipped, removed.Equipped, $"Cycle {cycle}: removal leaves no ghost equipped weight");
        ctx.AssertEqual(baseline.Carried, removed.Carried, $"Cycle {cycle}: removal restores the carried weight");
    }

    private static Weight Measure(uint actor) => new(GetWeight(actor),
        NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature().m_nEquippedWeight);

    private readonly record struct Weight(int Carried, int Equipped);
}
