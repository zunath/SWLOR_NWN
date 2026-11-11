using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using static SWLOR.Game.Server.EngineTests.Definitions.PowerGlovePairEngineTests;

namespace SWLOR.Game.Server.EngineTests.Definitions;

/// <summary>One Power Glove, with its generated other hand, must deal and grant exactly what two identical Katars do.</summary>
public static class PowerGloveKatarParityEngineTests
{
    private const int TestDMG = 24;
    private const int TestAttack = 5;
    private const int TestHP = 7;
    private const int TestStamina = 9;
    private const int TestAccuracy = 3;

    private sealed record Measurement(bool DualWield, int Attack, int MaxHP, int MaxStamina, int AbilityDMG,
        int MainAccuracy, int OffAccuracy, double MainDamage, double OffDamage, int AttackDelay);

    [EngineTest("One Power Glove deals and grants exactly what two identical Katars do", Category = "PowerGlovePair", TimeoutSeconds = 90f)]
    public static async Task OneGloveMatchesTwoKatars(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        player.Update(record =>
        {
            record.Skills[SkillType.Unarmed].Rank = 50;
            record.Skills[SkillType.Katar].Rank = 50;
        });
        var actor = player.Creature;
        var target = ctx.SpawnCreature("nw_rat001", 1f);
        var katars = new[] { await CreateCarriedItem(ctx, actor, "t_katar"), await CreateCarriedItem(ctx, actor, "t_katar") };
        var glove = await CreateCarriedItem(ctx, actor, "t_pglove");
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            foreach (var item in katars.Append(glove))
                Upgrade(item);
        });
        // The fixture's oversized STM pool is rebuilt from worn gear on the first equip; start from the rebuilt record.
        await ctx.ExecuteInCreatureContextAsync(actor, () => Stat.RefreshPlayerEquipmentStats(actor));
        var baseline = Totals(player);

        await EquipExisting(ctx, actor, katars[0], InventorySlot.RightHand);
        await EquipExisting(ctx, actor, katars[1], InventorySlot.LeftHand);
        var katarPair = await Measure(ctx, player, actor, target, katars[0]);
        ctx.Assert(katarPair.DualWield, "Two Katars strike with both hands");
        ctx.AssertEqual(baseline.Attack + 2 * TestAttack, katarPair.Attack, "Each Katar adds its Attack");
        ctx.AssertEqual(baseline.MaxHP + 2 * TestHP, katarPair.MaxHP, "Each Katar adds its HP");
        ctx.AssertEqual(baseline.MaxStamina + 2 * TestStamina, katarPair.MaxStamina, "Each Katar adds its STM");
        ctx.AssertEqual(2 * TestDMG, katarPair.AbilityDMG, "Unarmed abilities add both Katars' DMG");
        ctx.AssertEqual(katarPair.MainAccuracy, katarPair.OffAccuracy, "Each Katar rolls with its own identical Accuracy");
        ctx.AssertEqual(katarPair.MainDamage, katarPair.OffDamage, "Each Katar rolls the same damage");
        foreach (var katar in katars)
        {
            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionUnequipItem(katar));
            await ctx.WaitUntilAsync(() => Item.GetItemSlot(actor, katar) == InventorySlot.Invalid, 6f, "the Katar to return to inventory");
        }
        ctx.AssertEqual(baseline, Totals(player), "Removing both Katars restores the base record");

        for (var cycle = 0; cycle < 2; cycle++)
        {
            await EquipExisting(ctx, actor, glove, InventorySlot.RightHand);
            var companion = await WaitForPair(ctx, actor, glove);
            var gloves = await Measure(ctx, player, actor, target, glove);
            ctx.AssertEqual(katarPair.DualWield, gloves.DualWield, "The glove pair strikes with both hands like two Katars");
            ctx.AssertEqual(katarPair.Attack, gloves.Attack, "One glove adds the Attack of two Katars");
            ctx.AssertEqual(katarPair.MaxHP, gloves.MaxHP, "One glove adds the HP of two Katars");
            ctx.AssertEqual(katarPair.MaxStamina, gloves.MaxStamina, "One glove adds the STM of two Katars");
            ctx.AssertEqual(katarPair.AbilityDMG, gloves.AbilityDMG, "Unarmed abilities add the same DMG as with two Katars");
            ctx.AssertEqual(katarPair.MainAccuracy, gloves.MainAccuracy, "The main hand rolls with a Katar's Accuracy");
            ctx.AssertEqual(katarPair.OffAccuracy, gloves.OffAccuracy, "The other hand rolls with a Katar's Accuracy");
            ctx.AssertEqual(katarPair.MainDamage, gloves.MainDamage, "The main hand rolls a Katar's damage");
            ctx.AssertEqual(katarPair.OffDamage, gloves.OffDamage, "The other hand rolls a Katar's damage");
            ctx.AssertEqual(katarPair.AttackDelay, gloves.AttackDelay, "The pair attacks as often as two Katars");
            ctx.Log($"Cycle {cycle}: two Katars and one glove both measured {gloves}.");

            await ctx.ExecuteInCreatureContextAsync(actor, () => ActionUnequipItem(cycle == 0 ? glove : companion));
            await WaitForRemovedPair(ctx, actor, glove, companion);
            ctx.AssertEqual(baseline, Totals(player), "Removing the glove restores the base record without drift");
        }
    }

    private static void Upgrade(uint item)
    {
        var damage = new List<SWLOR.NWN.API.Engine.ItemProperty>();
        for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
            if (GetItemPropertyType(property) == ItemPropertyType.DMG) damage.Add(property);
        foreach (var property in damage) RemoveItemProperty(item, property);
        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, TestDMG), item);
        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Attack, -1, TestAttack), item);
        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.HPBonus, -1, TestHP), item);
        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Stamina, -1, TestStamina), item);
        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Accuracy, -1, TestAccuracy), item);
    }

    private static (int Attack, int MaxHP, int MaxStamina) Totals(PlayerAbilityFixture player)
    {
        var record = DB.Get<Player>(player.Id);
        return (record.Attack, record.MaxHP, record.MaxStamina);
    }

    private static async Task<Measurement> Measure(EngineTestContext ctx, PlayerAbilityFixture player, uint actor, uint target, uint main)
    {
        Measurement result = null;
        await ctx.WaitFrameAsync();
        await ctx.ExecuteInCreatureContextAsync(actor, () =>
        {
            var native = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(actor).AsNWSCreature();
            var defender = global::NWN.Native.API.NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(target).AsNWSObject();
            native.m_pcCombatRound.StartCombatRound(target);
            double Sample(int offHand)
            {
                ctx.SeedRandom(12345);
                return Enumerable.Range(0, 256).Average(_ => native.m_pStats.GetDamageRoll(defender, offHand, 0, 0, 0, 0));
            }
            var totals = Totals(player);
            result = new Measurement(EquipmentPredicates.HasDualWield(actor), totals.Attack, totals.MaxHP, totals.MaxStamina,
                WeaponDamage.GetAbilityDMG(actor, main, SkillType.Unarmed),
                Stat.GetAccuracyNative(native, WeaponAttackCycle.GetAttackWeapon(native, 1)),
                Stat.GetAccuracyNative(native, WeaponAttackCycle.GetAttackWeapon(native, 2)),
                Sample(0), Sample(1), Combat.CalculateAttackDelay(actor));
        });
        return result;
    }
}
