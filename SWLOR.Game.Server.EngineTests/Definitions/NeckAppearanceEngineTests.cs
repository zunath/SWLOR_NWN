using System.Linq;
using System.Threading.Tasks;
using NWN.Native.API;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using AppearanceType = SWLOR.NWN.API.NWScript.Enum.AppearanceType;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using ItemAppearanceType = SWLOR.NWN.API.NWScript.Enum.Item.ItemAppearanceType;
using Gender = SWLOR.NWN.API.NWScript.Enum.Gender;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class NeckAppearanceEngineTests
{
    [EngineTest("Neck packets follow every playable body and preserve saved armor", Category = "Neck", TimeoutSeconds = 120f)]
    public static async Task EquippedNeckPackets(EngineTestContext ctx)
    {
        var creature = ctx.SpawnCreature("civilian");
        await ctx.WaitUntilAsync(() => GetIsObjectValid(GetItemInSlot(InventorySlot.Chest, creature)), 10f, "outfit equip");
        await ctx.DelaySecondsAsync(.5f);
        await ctx.ExecuteInCreatureContextAsync(creature, () =>
        {
            var armor = GetItemInSlot(InventorySlot.Chest, creature);
            var item = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(armor).AsNWSItem();
            var colors = Enumerable.Range(0, 120).Select(i => GetItemAppearance(armor, ItemAppearanceType.ArmorColor, i)).ToArray();
            SetCreatureAppearanceType(creature, AppearanceType.Gnome);
            SetGender(creature, Gender.Female);
            ctx.AssertEqual("pfg0_neck123", NeckAppearance.GetModel(creature, 123),
                "An authored neck shared through LOD remains selectable");
            foreach (var race in RacialAppearanceRegistry.GetAppearanceTypes())
            foreach (var gender in new[] { Gender.Female, Gender.Male })
            {
                SetCreatureAppearanceType(creature, race);
                SetGender(creature, gender);
                foreach (var neck in new[] { 257, 260, 264, 267, 159, 123 })
                {
                    if (string.IsNullOrEmpty(NeckAppearance.GetModel(creature, neck))) continue;
                    foreach (var robe in new[] { 187, 1, 0 })
                    {
                        EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck, neck);
                        EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, robe);
                        EquippedItemAppearance.Refresh(creature, armor);
                        var expected = NeckModelRenderer.GetItemProjection(item);
                        var packet = NeckAppearancePacket.Read(item);
                        ctx.AssertEqual(expected, packet.Parts[(int)AppearanceArmor.Neck], $"{race}/{gender}/{robe}/{neck} packet");
                        ctx.AssertEqual(neck, GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck), "Saved neck ID");
                        ctx.Assert(colors.SequenceEqual(packet.Colors.Select(v => (int)v)), "All dyes survive projection");
                        if (race == AppearanceType.Human && gender == Gender.Female && robe == 187)
                            ctx.Assert(expected >= 1000, "The photographed robe must use a corrected neck skin");
                        if (robe == 0)
                            ctx.AssertEqual((ushort)neck, expected, "Removing robe restores canonical neck");
                    }
                }
            }
        });
    }
}
