using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.QuestContractService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    public static partial class MigrationEngineTests
    {
        [EngineTest("Force enhancement retirement preserves live and archived weapon damage", Category = "ForceEnhancement")]
        public static async Task RetireForceWeaponEnhancements(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var weapon = await CreateItemAsync(ctx, owner, "b_longsword", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                AddItemProperty(DurationType.Permanent,
                    ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Force), weapon);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var damage = PropertyValue(weapon, ItemPropertyType.DMG);
                var serialized = ObjectPlugin.Serialize(weapon);
                var migrated = ForceWeaponEnhancementMigration.MigrateSerializedObject(serialized);
                ctx.Assert(migrated != serialized, "The archived Force property is removed");
                var loaded = ObjectPlugin.Deserialize(migrated);
                ctx.Track(loaded);
                ctx.AssertEqual(damage, PropertyValue(loaded, ItemPropertyType.DMG), "Archived total DMG is preserved");
                ctx.AssertEqual(-1, PropertyValue(loaded, ItemPropertyType.WeaponDamageType), "Archived damage type reverts to physical");
                ctx.AssertEqual(migrated, ForceWeaponEnhancementMigration.MigrateSerializedObject(migrated), "Archive retry is unchanged");

                ctx.Assert(ForceWeaponEnhancementMigration.MigrateObject(owner), "Live inventory is migrated");
                ctx.AssertEqual(damage, PropertyValue(weapon, ItemPropertyType.DMG), "Live total DMG is preserved");
                ctx.AssertEqual(-1, PropertyValue(weapon, ItemPropertyType.WeaponDamageType), "Live Force property is removed immediately");
                ctx.AssertEqual(false, ForceWeaponEnhancementMigration.MigrateObject(owner), "Live retry is unchanged");
            });
        }

        [EngineTest("Force enhancement retirement preserves intentional NPC Force weapons", Category = "ForceEnhancement")]
        public static async Task PreserveNpcForceWeaponEnhancements(EngineTestContext ctx)
        {
            var owner = ctx.SpawnCreature("nw_rat001");
            await ctx.WaitFrameAsync();
            var weapon = await CreateItemAsync(ctx, owner, "b_longsword", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                SetLocalInt(weapon, Item.NoEconomyVariable, 1);
                AddItemProperty(DurationType.Permanent,
                    ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Force), weapon);
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var data = ObjectPlugin.Serialize(weapon);
                ctx.AssertEqual(data, ForceWeaponEnhancementMigration.MigrateSerializedObject(data), "NPC archive is unchanged");
                ctx.AssertEqual(false, ForceWeaponEnhancementMigration.MigrateObject(owner), "NPC live weapon is unchanged");
                AssertDamageType(ctx, weapon, CombatDamageType.Force);
            });
        }

    }
}
