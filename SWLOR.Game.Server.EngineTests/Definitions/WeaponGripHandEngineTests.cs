using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core.Async;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static class WeaponGripHandEngineTests
{
    [EngineTest("Native weapon placement table loads every compact row", Category = "WeaponFit", TimeoutSeconds = 15f)]
    public static Task PlacementTableLoads(EngineTestContext ctx)
    {
        var type = typeof(WeaponFit).Assembly.GetType(
            "SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.WeaponGripPlacements")!;
        var catalog = type.GetField("_catalog", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var fits = (System.Collections.IDictionary)catalog.GetType()
            .GetField("_fits", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(catalog)!;
        var expected = Enumerable.Range(0, Get2DARowCount("weaponfits"))
            .Sum(row => Get2DAString("weaponfits", "MODEL", row).Split(',').Length);
        ctx.Assert(expected > 0, "The deployed weapon placement table is present");
        ctx.AssertEqual(expected, fits.Count, "Every native table placement was loaded");
        return Task.CompletedTask;
    }

    [EngineTest("Gun model changes update both grips and rifle support while preserving saved hands", Category = "WeaponFit", TimeoutSeconds = 90f)]
    public static async Task ModelSpecificGraspPackets(EngineTestContext ctx)
    {
        var field = typeof(WeaponGripHandRenderer).GetField("_catalog", BindingFlags.Static | BindingFlags.NonPublic)!;
        var previous = field.GetValue(null);
        var creature = ctx.SpawnCreature("civilian");
        await ctx.WaitUntilAsync(() => GetIsObjectValid(GetItemInSlot(InventorySlot.Chest, creature)), 10f, "fixture outfit");
        try
        {
            field.SetValue(null, new WeaponGripHandCatalog(new[]
            {
                new WeaponGripHandRow("pmh0_handr257", "pmh0_handr30000"),
                new WeaponGripHandRow("pmh0_handl257", "pmh0_handl30000"),
                new WeaponGripHandRow("pmh0_handr257", "pmh0_handr30002", BaseItem.Sling, 122),
                new WeaponGripHandRow("pmh0_handl257", "pmh0_handl30002", BaseItem.Sling, 122),
                new WeaponGripHandRow("pmh0_handl257", "pmh0_handl30121", BaseItem.Rifle)
            }));
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                SetCreatureAppearanceType(creature, AppearanceType.Human);
                SetGender(creature, Gender.Male);
                SetPhenoType(PhenoType.Normal, creature);
                var armor = GetItemInSlot(InventorySlot.Chest, creature);
                EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, 0);
                foreach (var part in new[] { AppearanceArmor.RightHand, AppearanceArmor.LeftHand })
                    EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)part, 257);
                EquippedItemAppearance.Refresh(creature, armor);
            });
            var right = await ctx.EquipItemAsync(creature, "b_twinpistol", InventorySlot.RightHand);
            await ctx.ExecuteInCreatureContextAsync(creature, () => TwinPistolEquipment.EnsurePair(creature));
            await ctx.WaitUntilAsync(() => GetIsObjectValid(GetItemInSlot(InventorySlot.LeftHand, creature)),
                10f, "production companion pistol fixture");
            var left = GetItemInSlot(InventorySlot.LeftHand, creature);
            await NwTask.NextFrame();
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                ctx.AssertEqual(left, GetItemInSlot(InventorySlot.LeftHand, creature), "Off-hand fixture is equipped");
                ctx.AssertEqual(BaseItem.CompanionPistol, GetBaseItemType(left), "Off-hand fixture uses the production companion base item");
                SetMiddle(right, 301);
                TwinPistolEquipment.RefreshAppearance(creature);
                VerifyPacket(ctx, creature, 30000, 30000);
                SetMiddle(right, 122);
                TwinPistolEquipment.RefreshAppearance(creature);
                VerifyPacket(ctx, creature, 30002, 30002);
                SetMiddle(right, 301);
                TwinPistolEquipment.RefreshAppearance(creature);
                VerifyPacket(ctx, creature, 30000, 30000);
                CreaturePlugin.RunUnequip(creature, right);
                if (GetIsObjectValid(left)) CreaturePlugin.RunUnequip(creature, left);
                VerifyPacket(ctx, creature, 257, 257);
            });
            var rifle = await ctx.EquipItemAsync(creature, "b_rifle", InventorySlot.RightHand);
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                VerifyPacket(ctx, creature, 30000, 30121);
                CreaturePlugin.RunUnequip(creature, rifle);
                VerifyPacket(ctx, creature, 257, 257);
            });
        }
        finally
        {
            field.SetValue(null, previous);
            if (GetIsObjectValid(creature)) WeaponGripHandRenderer.Apply(creature);
        }
    }

    private static void SetMiddle(uint item, int model)
    {
        EquippedItemAppearance.Set(item, ItemAppearanceType.WeaponModel, (int)AppearanceWeapon.Middle, model % 100);
        EquippedItemAppearance.Set(item, ItemAppearanceType.WeaponColor, (int)AppearanceWeapon.Middle, model / 100);
    }

    [EngineTest("Gun grasp packets preserve authored hands and dyes and restore empty hands", Category = "WeaponFit", TimeoutSeconds = 90f)]
    public static async Task EquippedGraspPackets(EngineTestContext ctx)
    {
        var catalogField = typeof(WeaponGripHandRenderer).GetField("_catalog", BindingFlags.Static | BindingFlags.NonPublic)!;
        var previous = catalogField.GetValue(null);
        var creature = ctx.SpawnCreature("civilian");
        await ctx.WaitUntilAsync(() => GetIsObjectValid(GetItemInSlot(InventorySlot.Chest, creature)), 10f, "fixture outfit");
        try
        {
            catalogField.SetValue(null, new WeaponGripHandCatalog(new[]
            {
                ("pmh0_handr257", "pmh0_handr30000"),
                ("pmh0_handl257", "pmh0_handl30000"),
                ("pmh0_handr002", "pmh0_handr30002")
            }));
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                SetCreatureAppearanceType(creature, AppearanceType.Human);
                SetGender(creature, Gender.Male);
                SetPhenoType(PhenoType.Normal, creature);
                SetCreatureBodyPart(CreaturePart.RightHand, 2, creature);
                var armor = GetItemInSlot(InventorySlot.Chest, creature);
                EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, 0);
                foreach (var part in new[] { AppearanceArmor.RightHand, AppearanceArmor.LeftHand })
                    EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)part, 257);
                EquippedItemAppearance.Refresh(creature, armor);
            });
            var pistol = await ctx.EquipItemAsync(creature, "b_pistol", InventorySlot.RightHand);
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                VerifyPacket(ctx, creature, 30000, 257);
                CreaturePlugin.RunUnequip(creature, pistol);
                VerifyPacket(ctx, creature, 257, 257);
            });
            var rifle = await ctx.EquipItemAsync(creature, "b_rifle", InventorySlot.RightHand);
            await ctx.ExecuteInCreatureContextAsync(creature, () =>
            {
                VerifyPacket(ctx, creature, 30000, 257);
                CreaturePlugin.RunUnequip(creature, rifle);
                VerifyPacket(ctx, creature, 257, 257);
                // Natural grasp changes only the native render cache, not stats or saves.
                var armor = GetItemInSlot(InventorySlot.Chest, creature);
                EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand, 0);
                CreaturePlugin.RunEquip(creature, pistol, InventorySlot.RightHand);
                VerifyPacket(ctx, creature, 0, 257);
                var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(creature);
                ctx.AssertEqual((ushort)30002, native.m_cAppearance.m_pPartVariation[(int)AppearanceArmor.RightHand],
                    "The natural gun hand uses its grasp in the render cache");
                ctx.AssertEqual((ushort)2, native.m_pStats.m_pPartVariation[(int)AppearanceArmor.RightHand],
                    "The saved natural hand remains authored");
                ctx.AssertEqual(2, GetCreatureBodyPart(CreaturePart.RightHand, creature), "Natural hand API remains authored");
                var copy = ObjectPlugin.Deserialize(ObjectPlugin.Serialize(creature));
                ctx.Track(copy);
                ctx.AssertEqual(2, GetCreatureBodyPart(CreaturePart.RightHand, copy),
                    "Serializing a grasp pose retains the authored natural hand");
                CreaturePlugin.RunUnequip(creature, pistol);
                WeaponGripHandRenderer.Apply(creature);
                ctx.AssertEqual((ushort)2, native.m_cAppearance.m_pPartVariation[(int)AppearanceArmor.RightHand],
                    "The empty natural hand restores its original pose");
            });
        }
        finally
        {
            catalogField.SetValue(null, previous);
            if (GetIsObjectValid(creature)) WeaponGripHandRenderer.Apply(creature);
        }
    }

    private static void VerifyPacket(EngineTestContext ctx, uint creature, ushort right, ushort left)
    {
        var armor = GetItemInSlot(InventorySlot.Chest, creature);
        var item = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(armor).AsNWSItem();
        var colors = Enumerable.Range(0, 120).Select(i => GetItemAppearance(armor, ItemAppearanceType.ArmorColor, i)).ToArray();
        var natural = new[] { GetCreatureBodyPart(CreaturePart.RightHand, creature), GetCreatureBodyPart(CreaturePart.LeftHand, creature) };
        var authored = new[] { GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand),
            GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand) };
        for (var repeat = 0; repeat < 3; repeat++) WeaponGripHandRenderer.Apply(creature);
        var packet = NeckAppearancePacket.Read(item);
        ctx.AssertEqual(right, packet.Parts[(int)AppearanceArmor.RightHand], "Right packet uses the current gun role");
        ctx.AssertEqual(left, packet.Parts[(int)AppearanceArmor.LeftHand], "Left support or empty hand retains its pose");
        ctx.Assert(colors.SequenceEqual(packet.Colors.Select(v => (int)v)), "All 120 dye fields remain authored");
        ctx.AssertEqual(authored[0], GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand), "Right saved style is unchanged");
        ctx.AssertEqual(authored[1], GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand), "Left saved style is unchanged");
        ctx.AssertEqual(natural[0], GetCreatureBodyPart(CreaturePart.RightHand, creature), "Right natural hand is unchanged");
        ctx.AssertEqual(natural[1], GetCreatureBodyPart(CreaturePart.LeftHand, creature), "Left natural hand is unchanged");
        ctx.AssertEqual(creature, item.m_oidPossessor, "Packet serialization retains outfit ownership");
    }
}
