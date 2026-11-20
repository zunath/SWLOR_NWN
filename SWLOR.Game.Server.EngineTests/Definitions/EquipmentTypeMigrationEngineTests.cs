using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
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

}
