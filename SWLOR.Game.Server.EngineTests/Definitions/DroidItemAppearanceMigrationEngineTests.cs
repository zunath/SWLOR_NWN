using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class DroidInventoryEngineTests
{
    [EngineTest("Migration moves saved droid armor and carried weapons onto catalog appearances through two resummons",
        Category = "DroidInventory", TimeoutSeconds = 120)]
    public static async Task AppearanceSavedController(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var controller = await CreateController(ctx, owner);
        var armor = await CreateEquipment(ctx, owner, "dan_heavy_armor");
        var pistol = await CreateEquipment(ctx, owner, "raider_pistol");
        var armorId = GetObjectUUID(armor);
        var pistolId = GetObjectUUID(pistol);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            MigrationEngineTests.StampLegacyAppearance(ctx, armor);
            MigrationEngineTests.StampLegacyAppearance(ctx, pistol);
            var archive = new ConstructedDroid
            {
                Name = "Appearance fixture",
                EquippedItems = { [InventorySlot.Chest] = ObjectPlugin.Serialize(armor) },
                Inventory = { ["carried-pistol"] = ObjectPlugin.Serialize(pistol) },
            };
            Droid.SaveConstructedDroid(controller, archive);
            DestroyObject(armor);
            DestroyObject(pistol);
            ctx.Assert(LootStatRevisionMigration.MigratePlayerItems(controller), "The combined item traversal converts the saved droid items");
            ctx.Assert(!LootStatRevisionMigration.MigratePlayerItems(controller), "Saved-controller retry is stable");
            var saved = ObjectPlugin.Deserialize(Droid.LoadConstructedDroid(controller).EquippedItems[InventorySlot.Chest]);
            ctx.Track(saved);
            MigrationEngineTests.AssertCatalogAppearance(ctx, saved);
            DestroyObject(saved);
        });
        await ctx.DelaySecondsAsync(0.3f);
        var droid = await Spawn(ctx, owner, controller);
        try
        {
            for (var cycle = 0; cycle <= 2; cycle++)
            {
                var worn = GetItemInSlot(InventorySlot.Chest, droid);
                ctx.AssertEqual("dan_heavy_armor", GetResRef(worn), "Each summon restores the converted armor to its slot");
                // A droid body cannot show the armor's neck or robe, so summoning clears those parts.
                MigrationEngineTests.AssertCatalogAppearance(ctx, worn, everyPart: false);
                ctx.AssertEqual(armorId, GetObjectUUID(worn), "Repeated save/reload preserves the armor's identity");
                var carried = Items(droid).Where(item => GetResRef(item) == "raider_pistol").ToList();
                ctx.AssertEqual(1, carried.Count, "Repeated save/reload keeps exactly one carried pistol");
                MigrationEngineTests.AssertCatalogAppearance(ctx, carried.Single());
                ctx.AssertEqual(pistolId, GetObjectUUID(carried.Single()), "Repeated save/reload preserves the pistol's identity");
                ctx.AssertEqual(droid, GetItemPossessor(carried.Single()), "Repeated save/reload preserves ownership");
                await Unequip(ctx, droid, worn);
                await Equip(ctx, droid, worn, InventorySlot.Chest);
                await ctx.ExecuteInCreatureContextAsync(droid, () =>
                    ctx.Assert(!LootStatRevisionMigration.MigratePlayerItems(droid), "Live conversion remains idempotent after use"));
                if (cycle == 2) break;
                await Dismiss(ctx, owner, droid);
                droid = await Spawn(ctx, owner, controller);
            }
        }
        finally
        {
            if (GetIsObjectValid(droid)) await Dismiss(ctx, owner, droid);
        }
    }
}
