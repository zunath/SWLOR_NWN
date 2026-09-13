using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class MigrationEngineTests
    {
        [EngineTest("Migration removes historical droid instructions whose perk IDs were reused", Category = "MigrationInventoryTraversal")]
        public static async Task ReassignedDroidInstruction(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var controller = await CreateItemAsync(ctx, owner, Droid.DroidControlItemResref, owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat,
                    (int)DroidStatSubType.Tier, 5), controller);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DroidStat,
                    (int)DroidStatSubType.AISlots, 20), controller);
                SetLocalString(controller, "CONSTRUCTED_DROID", JsonConvert.SerializeObject(new ConstructedDroid
                {
                    // Historical ID 23 was Superior Weapon Focus, not Cryo Sprayer.
                    ActivePerks = new List<DroidPerk> { new((PerkType)23, 1), new(PerkType.MedKit, 1) },
                    LearnedPerks = new List<DroidPerk> { new((PerkType)23, 1), new(PerkType.MedKit, 1) }
                }));
                var migrated = MigrateSerialized(ctx, controller);
                var droid = Droid.LoadConstructedDroid(migrated);
                ctx.AssertEqual(1, droid.ActivePerks.Count, "Reused numeric IDs do not grant replacement abilities");
                ctx.AssertEqual(PerkType.MedKit, droid.ActivePerks.Single().Perk, "Unchanged instruction identity survives");
                ctx.AssertEqual(1, droid.LearnedPerks.Count, "Reassigned learned instructions are removed");
                ctx.AssertEqual(1, PropertyValue(migrated, ItemPropertyType.DroidInstruction, (int)PerkType.MedKit),
                    "The retained instruction has its inspection property");
                var saved = ObjectPlugin.Serialize(migrated);
                var retry = Invoke("ServerMigration.StoredItemDataMigration", "MigrateSerializedObject", saved);
                ctx.AssertEqual(false, (bool)retry.GetType().GetProperty("Changed").GetValue(retry),
                    "Normalized instructions need no further conversion");
            });
        }

        [EngineTest("Migration visits every inventory item when removing preceding items", Category = "MigrationInventoryTraversal")]
        public static async Task ConsecutiveObsoleteItems(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var bag = await CreateItemAsync(ctx, owner, "bag_ex", owner);
            foreach (var resref in new[] { "recipe_saberupg1", "recipe_staffupg1" })
                await CreateItemAsync(ctx, owner, resref, bag);
            var controller = await CreateItemAsync(ctx, owner, Droid.DroidControlItemResref, bag);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                SetLocalString(controller, "CONSTRUCTED_DROID", JsonConvert.SerializeObject(new ConstructedDroid
                {
                    ActivePerks = new List<DroidPerk> { new((PerkType)1, 5) },
                    LearnedPerks = new List<DroidPerk> { new((PerkType)1, 5) }
                }));
                var migrated = MigrateSerialized(ctx, bag);
                var contents = new List<uint>();
                for (var item = GetFirstItemInInventory(migrated); GetIsObjectValid(item); item = GetNextItemInInventory(migrated))
                    contents.Add(item);

                ctx.AssertEqual(1, contents.Count, "Every retired item is removed in the first pass");
                ctx.AssertEqual(Droid.DroidControlItemResref, GetResRef(contents.Single()), "The surviving controller is preserved");
                var droid = JsonConvert.DeserializeObject<ConstructedDroid>(GetLocalString(contents.Single(), "CONSTRUCTED_DROID"));
                ctx.AssertEqual(0, droid.ActivePerks.Count, "The controller after removed items is migrated in the same pass");
                ctx.AssertEqual(0, droid.LearnedPerks.Count, "Retired learned instructions are normalized immediately");
            });
        }
    }
}
