using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Previously revised shawls lose only three Attack while preserving crafted additions", Category = "EquipmentTypeMigration", TimeoutSeconds = 60)]
    public static async Task PreviouslyRevisedShawlKeepsEnhancements(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        var item = await CreateItemAsync(ctx, owner, "hexweave", owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            ItemPlugin.SetBaseItemType(item, BaseItem.Bracer);
            DeleteLocalInt(item, EquipmentTypeStatRevisionCatalog.RevisionVariable);
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                if (GetItemPropertyType(ip) == ItemPropertyType.Attack) { RemoveItemProperty(item, ip); break; }
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Attack, -1, 16), item);
            ctx.AssertEqual(1, GetLocalInt(item, RareLootStatRevisionCatalog.RevisionVariable), "Fixture has already received the old rare revision");
            ctx.Assert(LootStatRevisionMigration.MigrateObject(item), "The actual conversion repairs a previously revised shawl");
            var attack = 0;
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                if (GetItemPropertyType(ip) == ItemPropertyType.Attack) attack += GetItemPropertyCostTableValue(ip);
            ctx.AssertEqual(13, attack, "The final cloak Attack 8 retains the added Attack 5");
            ctx.AssertEqual(BaseItem.Cloak, GetBaseItemType(item), "The same item becomes a cloak");
            ctx.Assert(!LootStatRevisionMigration.MigrateObject(item), "Retry does not subtract Attack again");
        });
    }

    [EngineTest("Every legacy wearable type converts in place without property or identity loss", Category = "EquipmentTypeMigration", TimeoutSeconds = 120)]
    public static async Task AllLegacyWearableTypes(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        foreach (var (resref, correction) in EquipmentTypeMigration.Corrections)
        {
            var item = await CreateItemAsync(ctx, owner, resref, owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ItemPlugin.SetBaseItemType(item, correction.LegacyType);
                ItemPlugin.SetItemAppearance(item, ItemAppearanceType.SimpleModel, 0, correction.LegacyType == BaseItem.Gloves ? 198 : 176);
                var uuid = GetObjectUUID(item);
                var properties = DescribeProperties(item);
                ctx.AssertEqual(correction.LegacyType, GetBaseItemType(item), "The fixture starts with its actual legacy type");
                ctx.Assert(LootStatRevisionMigration.MigrateObject(item), resref + " converts through the acquisition path");
                ctx.AssertEqual(correction.Type, GetBaseItemType(item), resref + " has its corrected type");
                ctx.AssertEqual(correction.Model, GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0), resref + " has a matching model");
                ctx.AssertEqual(uuid, GetObjectUUID(item), resref + " preserves its identity");
                ctx.AssertEqual(owner, GetItemPossessor(item), resref + " preserves ownership");
                ctx.AssertEqual(properties, DescribeProperties(item), resref + " preserves properties");
                ctx.Assert(!LootStatRevisionMigration.MigrateObject(item), resref + " is stable on retry");
            });
        }
    }

    [EngineTest("Legacy shawl equips as a cloak and survives two bank use/save/reload cycles", Category = "EquipmentTypeMigration", TimeoutSeconds = 120)]
    public static Task LegacyShawlLifecycle(EngineTestContext ctx) => VerifyWearableTypeLifecycle(ctx, "hexweave", false);

    [EngineTest("An occupied cloak slot retains its item while a legacy shawl is safely stowed", Category = "EquipmentTypeMigration", TimeoutSeconds = 120)]
    public static Task LegacyShawlOccupiedCloak(EngineTestContext ctx) => VerifyWearableTypeLifecycle(ctx, "hexweave", true);

    [EngineTest("Legacy cowl equips on the head and survives two bank use/save/reload cycles", Category = "EquipmentTypeMigration", TimeoutSeconds = 120)]
    public static Task LegacyCowlLifecycle(EngineTestContext ctx) => VerifyWearableTypeLifecycle(ctx, "absdef_l3", false);

    [EngineTest("An occupied head slot retains its item while a legacy cowl is safely stowed", Category = "EquipmentTypeMigration", TimeoutSeconds = 120)]
    public static Task LegacyCowlOccupiedHead(EngineTestContext ctx) => VerifyWearableTypeLifecycle(ctx, "absdef_l3", true);

    [EngineTest("Legacy bracer hand wraps remain equipped as gloves through two bank cycles", Category = "EquipmentTypeMigration", TimeoutSeconds = 120)]
    public static Task LegacyHandwrapLifecycle(EngineTestContext ctx) => VerifyWearableTypeLifecycle(ctx, "brawl_wraps", false);

    [EngineTest("Legacy body-armor gauntlets move to arms and survive two bank use/save/reload cycles", Category = "EquipmentTypeMigration", TimeoutSeconds = 120)]
    public static Task LegacyBodyGauntletLifecycle(EngineTestContext ctx) => VerifyWearableTypeLifecycle(ctx, "piston_gaunt", false);

    private static async Task VerifyWearableTypeLifecycle(EngineTestContext ctx, string resref, bool occupied)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var owner = fixture.Creature;
        var correction = EquipmentTypeMigration.Corrections[resref];
        var previousSlot = correction.LegacyType == BaseItem.Armor ? InventorySlot.Chest : InventorySlot.Arms;
        foreach (var slot in new[] { previousSlot, correction.Slot }.Distinct())
        {
            var current = GetItemInSlot(slot, owner);
            if (GetIsObjectValid(current)) await UnequipAsync(ctx, owner, current);
        }
        var existing = OBJECT_INVALID;
        if (occupied)
        {
            existing = await CreateItemAsync(ctx, owner, correction.Slot == InventorySlot.Head ? "charm_cowl" : "advent_cloak", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () => RemoveRequirement(existing));
            await EquipAsync(ctx, owner, existing, correction.Slot);
        }
        var baseline = ReadEquipmentTotals(owner).Attack;
        var item = await CreateItemAsync(ctx, owner, resref, owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            RemoveRequirement(item);
            ItemPlugin.SetBaseItemType(item, correction.LegacyType);
            ItemPlugin.SetItemAppearance(item, ItemAppearanceType.SimpleModel, 0, correction.LegacyType == BaseItem.Gloves ? 198 : 176);
            SetLocalString(item, "CRAFTED_BY", "equipment-type-fixture");
        });
        await EquipAsync(ctx, owner, item, previousSlot);
        var uuid = GetObjectUUID(item);
        var properties = DescribeStoredEquipmentProperties(item);
        var wornAttack = ReadEquipmentTotals(owner).Attack;
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            ctx.AssertEqual(correction.LegacyType, GetBaseItemType(item), "The equipped fixture starts with its legacy type");
            new _22_ReviseLootStats().Migrate(owner);
            ctx.AssertEqual(correction.Type, GetBaseItemType(item), "Login conversion corrects the type");
            ctx.AssertEqual(occupied ? existing : item, GetItemInSlot(correction.Slot, owner), "An existing destination item is never replaced");
            ctx.AssertEqual(owner, GetItemPossessor(item), "Conversion retains ownership");
            ctx.AssertEqual(occupied ? baseline : wornAttack, ReadEquipmentTotals(owner).Attack, "Equipment stats are counted exactly once");
            ctx.AssertEqual(properties, DescribeStoredEquipmentProperties(item), "Conversion preserves authored and crafted properties");
            ctx.Assert(!LootStatRevisionMigration.MigrateObject(owner), "Login retry makes no further changes");
        });
        for (var cycle = 0; cycle < 2; cycle++)
        {
            if (GetItemInSlot(correction.Slot, owner) == item) await UnequipAsync(ctx, owner, item);
            ctx.AssertEqual(baseline, ReadEquipmentTotals(owner).Attack, "Unequip removes the corrected contribution exactly once");
            InventoryItem stored = null;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                stored = Bank.DepositItem(owner, item);
                ctx.AssertEqual(fixture.Id, stored.PlayerId, "Bank save retains ownership");
                ctx.AssertEqual(1, stored.Quantity, "Bank save retains one item");
            });
            await ctx.WaitUntilAsync(() => !GetIsObjectValid(item), 5f, "the deposited item to leave the inventory");
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                new _27_UpdateStoredItems().Migrate();
                Bank.WithdrawItem(owner, stored.Id);
            });
            await ctx.WaitFrameAsync();
            item = FindHeadgear(owner, resref);
            ctx.Assert(GetIsObjectValid(item), "Withdrawal restores the corrected item");
            ctx.AssertEqual(1, CountHeadgear(owner, resref), "Save/reload creates no duplicate");
            ctx.AssertEqual(uuid, GetObjectUUID(item), "Save/reload preserves identity");
            ctx.AssertEqual(correction.Type, GetBaseItemType(item), "Save/reload preserves corrected type");
            ctx.AssertEqual(properties, DescribeStoredEquipmentProperties(item), "Save/reload preserves authored and crafted properties");
            ctx.AssertEqual("equipment-type-fixture", GetLocalString(item, "CRAFTED_BY"), "Save/reload preserves customization");
            ctx.AssertEqual(owner, GetItemPossessor(item), "Reload retains ownership");
            ctx.AssertEqual(1, GetItemStackSize(item), "Reload retains quantity");
            if (!occupied) await EquipAsync(ctx, owner, item, correction.Slot);
            ctx.AssertEqual(occupied ? baseline : wornAttack, ReadEquipmentTotals(owner).Attack, "The restored item remains usable without stat drift");
        }
    }

    private static string DescribeStoredEquipmentProperties(uint item)
    {
        var properties = new List<string>();
        for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
        {
            // StandardItemConfigurations adds this engine combat hook on equip;
            // it is regenerated after storage and is not an authored item stat.
            if (GetItemPropertyType(ip) == ItemPropertyType.OnHitCastSpell &&
                GetItemPropertySubType(ip) == (int)OnHitCastSpell.ONHIT_UNIQUEPOWER)
                continue;
            properties.Add($"{GetItemPropertyType(ip)}:{GetItemPropertySubType(ip)}:{GetItemPropertyCostTableValue(ip)}");
        }
        return string.Join(", ", properties);
    }
}
