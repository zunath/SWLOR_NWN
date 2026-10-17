using System.Linq;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using AppearanceType = SWLOR.NWN.API.NWScript.Enum.AppearanceType;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using ItemAppearanceType = SWLOR.NWN.API.NWScript.Enum.Item.ItemAppearanceType;
using Gender = SWLOR.NWN.API.NWScript.Enum.Gender;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class ShoulderAppearanceEngineTests
{
    [EngineTest("Shoulder packets follow robe skeletons and retain authored armor", Category = "Shoulder", TimeoutSeconds = 60f)]
    public static async Task EquippedShoulderPackets(EngineTestContext ctx)
    {
        var creature = ctx.SpawnCreature("civilian");
        await ctx.WaitUntilAsync(() => GetIsObjectValid(GetItemInSlot(InventorySlot.Chest, creature)), 10f, "outfit equip");
        await ctx.ExecuteInCreatureContextAsync(creature, () =>
        {
            SetCreatureAppearanceType(creature, AppearanceType.Human);
            SetGender(creature, Gender.Male);
            var armor = GetItemInSlot(InventorySlot.Chest, creature);
            var item = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(armor).AsNWSItem();
            var colors = Enumerable.Range(0, 120).Select(i => GetItemAppearance(armor, ItemAppearanceType.ArmorColor, i)).ToArray();
            for (var cycle = 0; cycle < 2; cycle++)
            foreach (var style in new[] { 13, 140, 141, 185, 256, 260 })
            foreach (var robe in new[] { 200, 1, 0 })
            {
                EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder, style);
                EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder, style);
                EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, robe);
                EquippedItemAppearance.Refresh(creature, armor);
                var packet = NeckAppearancePacket.Read(item);
                foreach (var part in new[] { AppearanceArmor.LeftShoulder, AppearanceArmor.RightShoulder })
                {
                    var projected = packet.Parts[(int)part];
                    ctx.AssertEqual(ShoulderModelRenderer.GetItemProjection(item, part), projected, "Native shoulder packet projection");
                    ctx.AssertEqual(style, GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)part), "Packet writer restores authored shoulder ID");
                    if (robe == 0)
                        ctx.AssertEqual((ushort)style, projected, "Removing the robe restores the base shoulder");
                    else
                        ctx.Assert(projected >= 1000, "A robe skeleton uses its remapped shoulder skin");
                }
                ctx.Assert(colors.SequenceEqual(packet.Colors.Select(v => (int)v)), "All armor dyes survive projection");
                ctx.AssertEqual(creature, item.m_oidPossessor, "Packet serialization preserves armor ownership");
            }
        });
    }
}
