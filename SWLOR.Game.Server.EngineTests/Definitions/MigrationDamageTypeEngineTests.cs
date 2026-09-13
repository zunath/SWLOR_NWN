using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class MigrationEngineTests
    {
        [EngineTest("Migration preserves every nonphysical weapon damage type", Category = "MigrationDamageType")]
        public static async Task NonphysicalWeaponDamageTypes(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            foreach (var damageType in new[] { CombatDamageType.Force, CombatDamageType.Fire,
                         CombatDamageType.Poison, CombatDamageType.Electrical, CombatDamageType.Ice })
            {
                var weapon = await CreateItemAsync(ctx, owner, "b_longsword", owner);
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    // Use a saved legacy property: DMG no longer declares a subtype table.
                    var saved = LegacyPropertyFixture(ctx, weapon, ItemPropertyType.DMG, (int)damageType, 34, 6);
                    ctx.AssertEqual(6, PropertyValue(saved, ItemPropertyType.DMG, (int)damageType), "Legacy typed damage fixture");

                    var migrated = MigrateSerialized(ctx, saved);
                    ctx.AssertEqual(5, PropertyValue(migrated, ItemPropertyType.DMG), "Converted weapon damage");
                    AssertDamageType(ctx, migrated, damageType);
                    var retried = MigrateSerialized(ctx, migrated);
                    ctx.AssertEqual(5, PropertyValue(retried, ItemPropertyType.DMG), "Retry does not rescale damage");
                    AssertDamageType(ctx, retried, damageType);
                    ctx.AssertEqual(false, (bool)Invoke("SerializedItemWeaponDamageTypeMigration", "MigrateObject", retried),
                        "A canonical saved weapon needs no further damage-property conversion");
                });
                await ctx.WaitFrameAsync();
            }
        }

        [EngineTest("Migration preserves nonphysical enhancement and blueprint damage types", Category = "MigrationDamageType")]
        public static async Task NonphysicalEnhancementDamageTypes(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            foreach (var blueprint in new[] { false, true })
            foreach (var damageType in new[] { CombatDamageType.Force, CombatDamageType.Fire,
                         CombatDamageType.Poison, CombatDamageType.Electrical, CombatDamageType.Ice })
            {
                var item = await CreateItemAsync(ctx, owner, blueprint ? "blueprint" : "wen_dmg_phy1", owner);
                if (blueprint)
                {
                    // Empty blueprints have no saved property to use as a native template.
                    await ctx.ExecuteInCreatureContextAsync(owner, () =>
                        AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.WeaponEnhancement, (int)EnhancementSubType.DMG, 3), item));
                    await ctx.WaitFrameAsync();
                }
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    if (blueprint)
                        SetLocalInt(item, "BLUEPRINT_RECIPE_ID", 1);
                    var saved = LegacyPropertyFixture(ctx, item, ItemPropertyType.WeaponEnhancement, (int)damageType + 17, 45, 3);
                    ctx.AssertEqual(3, PropertyValue(saved, ItemPropertyType.WeaponEnhancement, (int)damageType + 17), "Saved legacy enhancement fixture");
                    var migrated = MigrateSerialized(ctx, saved);
                    ctx.AssertEqual(3, PropertyValue(migrated, ItemPropertyType.WeaponEnhancement, (int)EnhancementSubType.DMG), "Enhancement damage amount");
                    AssertDamageType(ctx, migrated, damageType);
                    var retried = MigrateSerialized(ctx, migrated);
                    ctx.AssertEqual(3, PropertyValue(retried, ItemPropertyType.WeaponEnhancement, (int)EnhancementSubType.DMG), "Retry enhancement damage amount");
                    AssertDamageType(ctx, retried, damageType);
                    ctx.AssertEqual(false, (bool)Invoke("SerializedItemWeaponDamageTypeMigration", "MigrateObject", retried),
                        "A canonical saved enhancement needs no further damage-property conversion");
                });
                await ctx.WaitFrameAsync();
            }
        }

        [EngineTest("Migration normalizes damage types without a damage amount", Category = "MigrationDamageType")]
        public static async Task DamageTypesWithoutDamageAmount(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var item = await CreateItemAsync(ctx, owner, "b_longsword", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () => ClearProperties(item));
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Force), item);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Fire), item);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var migrated = MigrateSerialized(ctx, item);
                AssertDamageType(ctx, migrated, CombatDamageType.Fire);
                ctx.AssertEqual(-1, PropertyValue(migrated, ItemPropertyType.DMG), "No damage amount is invented");
                AssertDamageType(ctx, MigrateSerialized(ctx, migrated), CombatDamageType.Fire);
            });
        }

        [EngineTest("Canonical physical enhancement bonuses are stable on retry", Category = "MigrationDamageType")]
        public static async Task PhysicalEnhancementRetry(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var item = await CreateItemAsync(ctx, owner, "wen_dmg_phy1", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var saved = LegacyPropertyFixture(ctx, item, ItemPropertyType.WeaponEnhancement,
                    (int)EnhancementSubType.DMG, 45, 1);
                var migrated = MigrateSerialized(ctx, saved);
                ctx.AssertEqual(2, PropertyValue(migrated, ItemPropertyType.WeaponEnhancement,
                    (int)EnhancementSubType.DMG), "The raw physical bonus is rebalanced once");
                ctx.AssertEqual(false, (bool)Invoke("SerializedItemWeaponDamageTypeMigration", "MigrateObject", migrated),
                    "The canonical physical bonus needs no further conversion");
            });
        }

        private static uint LegacyPropertyFixture(EngineTestContext ctx, uint item, ItemPropertyType type, int subtype, int table, int value)
        {
            var originalProperties = new List<SWLOR.NWN.API.Engine.ItemProperty>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                originalProperties.Add(ip);
            AddLegacyProperty(item, type, subtype, table, value);
            foreach (var property in originalProperties)
                Invoke("MigrationObject", "RemoveProperty", item, property);
            return Deserialize(ctx, ObjectPlugin.Serialize(item));
        }

        private static void AssertDamageType(EngineTestContext ctx, uint item, CombatDamageType expected)
        {
            var types = new List<int>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                if (GetItemPropertyType(ip) == ItemPropertyType.WeaponDamageType)
                    types.Add(GetItemPropertySubType(ip));
            ctx.Assert(types.SequenceEqual(new[] { (int)expected }),
                $"Expected exactly one {expected} damage-type property; found {string.Join(", ", types)}.");
        }
    }
}
