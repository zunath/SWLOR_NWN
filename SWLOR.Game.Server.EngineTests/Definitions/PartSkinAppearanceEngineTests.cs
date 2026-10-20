using System.Linq;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using AppearanceType = SWLOR.NWN.API.NWScript.Enum.AppearanceType;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using ItemAppearanceType = SWLOR.NWN.API.NWScript.Enum.Item.ItemAppearanceType;
using Gender = SWLOR.NWN.API.NWScript.Enum.Gender;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class PartSkinAppearanceEngineTests
{
    [EngineTest("Non-modular creatures skip part skin projection", Category = "PartSkin", TimeoutSeconds = 30f)]
    public static async Task NonModularCreatureParts(EngineTestContext ctx)
    {
        var creature = ctx.SpawnCreature("nw_rat001");
        await ctx.ExecuteInCreatureContextAsync(creature, () =>
        {
            ctx.Assert(string.IsNullOrEmpty(NeckAppearance.GetBodyModel(creature)), "The fixture has no modular body skeleton");
            PartSkinModelRenderer.Apply(creature);
            ctx.Assert(GetIsObjectValid(creature), "Part skin projection leaves the non-modular creature intact");
        });
    }

    [EngineTest("Pelvis and hand packets follow robe skeletons and retain authored armor", Category = "PartSkin", TimeoutSeconds = 90f)]
    public static async Task EquippedPartPackets(EngineTestContext ctx)
    {
        var creature = ctx.SpawnCreature("civilian");
        await ctx.WaitUntilAsync(() => GetIsObjectValid(GetItemInSlot(InventorySlot.Chest, creature)), 10f, "outfit equip");
        await ctx.ExecuteInCreatureContextAsync(creature, () =>
        {
            SetCreatureAppearanceType(creature, AppearanceType.Human);
            var armor = GetItemInSlot(InventorySlot.Chest, creature);
            var item = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(armor).AsNWSItem();
            var colors = Enumerable.Range(0, 120).Select(i => GetItemAppearance(armor, ItemAppearanceType.ArmorColor, i)).ToArray();
            var parts = new[] { (AppearanceArmor.Pelvis, 10), (AppearanceArmor.LeftHand, 3), (AppearanceArmor.RightHand, 3) };
            var naked = GetCreatureBodyPart(CreaturePart.Pelvis, creature);
            foreach (var gender in new[] { Gender.Female, Gender.Male })
            for (var cycle = 0; cycle < 2; cycle++)
            foreach (var robe in new[] { 20, 21, 236, 0 })
            {
                SetGender(creature, gender);
                foreach (var (part, style) in parts)
                    EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)part, style);
                EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, robe);
                EquippedItemAppearance.Refresh(creature, armor);
                var body = NeckAppearance.GetBodyModel(creature);
                var packet = NeckAppearancePacket.Read(item);
                foreach (var (part, style) in parts)
                {
                    var projected = packet.Parts[(int)part];
                    ctx.AssertEqual(PartSkinModelRenderer.GetItemProjection(item, part, (ushort)style), projected,
                        $"Native {part} packet projection on {body}");
                    ctx.AssertEqual(style, GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)part),
                        "Packet writer restores the authored part ID");
                    if (robe == 0)
                        ctx.AssertEqual((ushort)style, projected, "Removing the robe restores the authored part");
                    else
                        ctx.Assert(projected >= 1000, $"Robe {robe} body {body} uses the remapped {part} skin");
                }
                ctx.Assert(colors.SequenceEqual(packet.Colors.Select(v => (int)v)), "All armor dyes survive projection");
                ctx.AssertEqual(naked, GetCreatureBodyPart(CreaturePart.Pelvis, creature), "Naked body parts keep authored IDs");
                ctx.AssertEqual(creature, item.m_oidPossessor, "Packet serialization preserves armor ownership");
            }
        });
    }
}
