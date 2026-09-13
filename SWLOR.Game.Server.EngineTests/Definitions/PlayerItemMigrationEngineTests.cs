using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class MigrationEngineTests
    {
        [EngineTest("Sequential saber property conversion survives engine updates", Category = "PlayerItemMigration")]
        public static async Task SequentialSaberPropertiesSurviveUpdates(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var item = await CreateItemAsync(ctx, owner, "valcl4", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ClearProperties(item);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Physical, 20), item);
                AddLegacyProperty(item, ItemPropertyType.UseLimitationPerk, 18, 33, 5);
            });
            await ctx.WaitFrameAsync();
            string expected = null;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                Invoke("EquipmentRequirementMigration", "MigrateObject", owner);
                Invoke("SerializedItemResistanceMigration", "MigrateObject", owner);
                Invoke("SerializedItemWeaponDamageTypeMigration", "MigrateObject", owner);
                Invoke("LegacySaberMigration", "MigratePlayer", owner);
                ctx.AssertEqual(21, PropertyValue(item, ItemPropertyType.DMG), "Tier 5 saber damage is present");
                ctx.AssertEqual(40, PropertyValue(item, ItemPropertyType.RequiresSkill), "Tier 5 skill requirement is present");
                expected = DescribeProperties(item);
                ctx.Log("Immediate saber properties: " + expected);
            });
            await ctx.DelaySecondsAsync(0.3f);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ctx.Log("Settled saber properties: " + DescribeProperties(item));
                ctx.AssertEqual(expected, DescribeProperties(item), "Later engine updates must retain every replacement property");
            });
        }

        /// <summary>Checks the same-script save cannot retain items or properties removed by the original player migration.</summary>
        [EngineTest("Legacy player items are removed before the character checkpoint", Category = "PlayerItemMigration")]
        public static async Task LegacyPlayerItemsAreSavedImmediately(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            await CreateItemAsync(ctx, owner, "xp_tome_1", owner);
            await CreateItemAsync(ctx, owner, "refund_tome", owner);
            var retained = await CreateItemAsync(ctx, owner, "b_longsword", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
                AddItemProperty(DurationType.Permanent, ItemPropertyAbilityBonus(AbilityType.Might, 4), retained));
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                typeof(_1_LegacyPlayerMigration).GetMethod("MigrateItems", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(new _1_LegacyPlayerMigration(), new object[] { owner });
                var restored = Deserialize(ctx, ObjectPlugin.Serialize(owner));
                ctx.AssertEqual(1, Item.GetInventoryItemCount(restored), "The saved file excludes both retired items immediately");
                var item = GetFirstItemInInventory(restored);
                ctx.AssertEqual("b_longsword", GetResRef(item), "The retained item survives");
                ctx.Assert(!GetIsItemPropertyValid(GetFirstItemProperty(item)), "No wiped property survives the immediate save");
            });
        }

        /// <summary>Checks interrupted historical damage conversion neither saves old properties nor increments damage twice.</summary>
        [EngineTest("Historical weapon damage conversion survives save and retry", Category = "PlayerItemMigration")]
        public static async Task HistoricalWeaponDamageSaveAndRetry(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var bag = await CreateItemAsync(ctx, owner, "bag_sm", owner);
            var item = await CreateItemAsync(ctx, owner, "tit_rifle", bag);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                ClearProperties(item);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, (int)CombatDamageType.Physical, 12), item);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                new _8_UpgradeWeapons().Migrate(owner);
                item = Deserialize(ctx, ObjectPlugin.Serialize(item));
                ctx.AssertEqual(15, PropertyValue(item, ItemPropertyType.DMG), "The historical +3 adjustment reaches weapons in bags and is persisted immediately");
                var saved = DescribeProperties(item);
                var update = typeof(_8_UpgradeWeapons).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
                update.Invoke(new _8_UpgradeWeapons(), new object[] { item });
                ctx.AssertEqual(saved, DescribeProperties(item), "Retry after a later failure must not add damage again");
            });
        }
    }
}
