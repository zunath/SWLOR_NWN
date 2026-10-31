using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class BareHandedAttackSkillEngineTests
{
    /// <summary>
    /// The engine attacks with the arms-slot item when both hands are empty, and that item's on-hit
    /// event awards the combat point, so its attack identity decides which skill a punch trains.
    /// </summary>
    [EngineTest("Bare-handed attacks train Unarmed while katars and power gloves keep their own skill", Category = "BareHandedAttackSkill", TimeoutSeconds = 30f)]
    public static async Task EquippedItemsResolveTheirAttackSkill(EngineTestContext ctx)
    {
        using var player = await PlayerAbilityFixture.CreateAsync(ctx);
        var actor = player.Creature;

        var gloves = await ctx.EquipItemAsync(actor, "com_gloves", InventorySlot.Arms);
        ctx.Assert(!GetIsObjectValid(GetItemInSlot(InventorySlot.RightHand, actor)) &&
                   !GetIsObjectValid(GetItemInSlot(InventorySlot.LeftHand, actor)), "Both hands are empty");
        ctx.AssertEqual(gloves, GetItemInSlot(InventorySlot.Arms, actor), "The gloves are in the arms slot");
        ctx.AssertEqual(SkillType.Unarmed, Skill.GetWeaponSkill(actor, gloves), "Bare-handed attacks with gloves train Unarmed");

        var bracer = await ctx.EquipItemAsync(actor, "bm_bracer", InventorySlot.Arms);
        ctx.AssertEqual(bracer, GetItemInSlot(InventorySlot.Arms, actor), "The bracer is in the arms slot");
        ctx.AssertEqual(SkillType.Unarmed, Skill.GetWeaponSkill(actor, bracer), "Bare-handed attacks with a bracer train Unarmed");

        var katar = await ctx.EquipItemAsync(actor, "t_katar", InventorySlot.RightHand);
        ctx.AssertEqual(SkillType.Katar, Skill.GetWeaponSkill(actor, katar), "A katar keeps training Katar");

        await ctx.ExecuteInCreatureContextAsync(actor, () => ActionUnequipItem(katar));
        await ctx.WaitUntilAsync(() => !GetIsObjectValid(GetItemInSlot(InventorySlot.RightHand, actor)), 5f,
            "the katar to leave the main hand");
        var powerGlove = await ctx.EquipItemAsync(actor, "t_pglove", InventorySlot.RightHand);
        ctx.AssertEqual(SkillType.Unarmed, Skill.GetWeaponSkill(actor, powerGlove), "A power glove keeps training Unarmed");
    }

    /// <summary>
    /// A landed punch reaches XP through the arms item's on-hit handler: the handler records the
    /// combat point under the item's attack skill and the kill pays that skill out.
    /// </summary>
    [EngineTest("Landed bare-handed hits with gloves or a bracer pay kill XP to Unarmed, not Katar", Category = "BareHandedAttackSkill", TimeoutSeconds = 60f)]
    public static async Task LandedBareHandedHitsPayUnarmedXP(EngineTestContext ctx)
    {
        foreach (var armsResref in new[] { "com_gloves", "bm_bracer" })
        {
            using var player = await PlayerAbilityFixture.CreateAsync(ctx);
            var actor = player.Creature;
            player.Update(record =>
            {
                foreach (var skill in new[] { SkillType.Unarmed, SkillType.Katar })
                {
                    record.Skills[skill].Rank = 0;
                    record.Skills[skill].XP = 0;
                }
                record.XPDebt = 0;
                record.DMXPBonus = 0;
            });
            var armsItem = await ctx.EquipItemAsync(actor, armsResref, InventorySlot.Arms);
            ctx.Assert(!GetIsObjectValid(GetItemInSlot(InventorySlot.RightHand, actor)) &&
                       !GetIsObjectValid(GetItemInSlot(InventorySlot.LeftHand, actor)), $"{armsResref}: both hands are empty");

            var enemy = ctx.SpawnCreature("nw_bandit001", 2f);
            await ctx.WaitFrameAsync();
            ctx.SuppressNPCNaturalRegen(enemy);
            Stat.SetNPCMaxHitPoints(enemy, 1000, true);
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), enemy, 60f);

            CombatPoint.RecordItemHitPoint(actor, enemy, armsItem);
            ctx.AssertEqual(1, Points(actor, enemy, SkillType.Unarmed), $"{armsResref}: the landed hit records an Unarmed combat point");
            ctx.AssertEqual(0, Points(actor, enemy, SkillType.Katar), $"{armsResref}: the landed hit records no Katar combat point");

            await ctx.ExecuteInCreatureContextAsync(enemy, () => ApplyEffectToObject(DurationType.Instant, EffectDeath(), enemy));
            await ctx.WaitUntilAsync(() => !CombatPoint.HasCombatPoints(actor, enemy), 5f, $"{armsResref}: kill XP payout and cleanup");

            var record = DB.Get<Player>(player.Id);
            var unarmed = record.Skills[SkillType.Unarmed];
            var katar = record.Skills[SkillType.Katar];
            ctx.Assert(unarmed.Rank > 0 || unarmed.XP > 0, $"{armsResref}: the kill pays Unarmed XP");
            ctx.Assert(katar.Rank == 0 && katar.XP == 0, $"{armsResref}: the kill pays no Katar XP");
        }
    }

    private static int Points(uint player, uint creature, SkillType skill)
    {
        var tracker = (Dictionary<uint, Dictionary<uint, Dictionary<SkillType, int>>>)typeof(CombatPoint)
            .GetField("_creatureCombatPointTracker", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null);
        return tracker!.TryGetValue(creature, out var byPlayer) &&
               byPlayer.TryGetValue(player, out var bySkill) &&
               bySkill.TryGetValue(skill, out var points)
            ? points
            : 0;
    }
}
