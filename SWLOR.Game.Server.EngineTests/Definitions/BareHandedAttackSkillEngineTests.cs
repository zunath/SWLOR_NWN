using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.SkillService;
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
}
