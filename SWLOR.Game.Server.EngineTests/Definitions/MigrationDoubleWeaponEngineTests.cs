using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Existing double weapons retain enhancements across stored migration and retry", Category = "DoubleWeaponMigration")]
    public static async Task ExistingDoubleWeaponEnhancements(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("nw_rat001");
        await ctx.WaitFrameAsync();
        foreach (var profile in new[]
        {
            (Resref: "h_twinblade_5", Tier: 6, OldDamage: 30, Damage: 26, Delay: 23),
            (Resref: "trn_saberstaff_3", Tier: 3, OldDamage: 20, Damage: 17, Delay: 24),
            (Resref: "chi_twinelec", Tier: 6, OldDamage: 37, Damage: 32, Delay: 24),
        })
        {
            var weapon = await CreateItemAsync(ctx, owner, profile.Resref, owner);
            string savedData = null;
            string migratedData = null;
            string identity = null;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                RemovePropertiesImmediately(weapon);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, profile.OldDamage), weapon);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Delay, -1, 29), weapon);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Accuracy, -1, 10), weapon);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.WeaponDamageType, (int)CombatDamageType.Ice), weapon);
                SetLocalInt(weapon, "SABER_TIER", profile.Tier);
                SetName(weapon, "Custom enhanced double weapon");
                SetLocalString(weapon, "DOUBLE_WEAPON_FIXTURE", "keep");
                identity = GetObjectUUID(weapon);
                savedData = ObjectPlugin.Serialize(weapon);
                DestroyObject(weapon);
            });
            await ctx.DelaySecondsAsync(0.3f);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var (changed, data) = MigrateStoredData(savedData);
                ctx.Assert(changed, "The stored weapon requires rebalance");
                ctx.Assert(ContainsIdentity(data, identity), "The stored payload retains its native identity");
                var migrated = Deserialize(ctx, data);
                ctx.AssertEqual(profile.Damage, Item.GetDMG(migrated), $"{profile.Resref}: damage enhancements survive base rebalance");
                ctx.AssertEqual(profile.Delay, PropertyValue(migrated, ItemPropertyType.Delay), "Per-end delay matches a paired single weapon");
                ctx.AssertEqual(10, PropertyValue(migrated, ItemPropertyType.Accuracy), "Both accuracy enhancements survive");
                AssertDamageType(ctx, migrated, CombatDamageType.Ice);
                ctx.AssertEqual(identity, GetObjectUUID(migrated), "Saved identity survives");
                ctx.AssertEqual("Custom enhanced double weapon", GetName(migrated), "Custom name survives");
                ctx.AssertEqual("keep", GetLocalString(migrated, "DOUBLE_WEAPON_FIXTURE"), "Unrelated local survives");
                migratedData = ObjectPlugin.Serialize(migrated);
                DestroyObject(migrated);
            });
            await ctx.DelaySecondsAsync(0.3f);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var (changed, data) = MigrateStoredData(migratedData);
                ctx.Assert(!changed, "The migrated stored payload needs no further changes");
                ctx.AssertEqual(migratedData, data, "Retry leaves the saved payload unchanged");
                var retried = Deserialize(ctx, data);
                ctx.AssertEqual(identity, GetObjectUUID(retried), "Retry preserves the saved identity");
                ctx.AssertEqual(profile.Damage, Item.GetDMG(retried), "Retry cannot subtract the rating adjustment twice");
                ctx.AssertEqual(profile.Delay, PropertyValue(retried, ItemPropertyType.Delay), "Retry delay is stable");
                ctx.AssertEqual(10, PropertyValue(retried, ItemPropertyType.Accuracy), "Retry accuracy is stable");
            });
        }
    }

    [EngineTest("In-place saberstaff upgrades retain their kit tier and bonuses through migration and retry", Category = "DoubleWeaponMigration")]
    public static async Task UpgradedSaberstaffKitBudgets(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("nw_rat001");
        await ctx.WaitFrameAsync();
        foreach (var profile in new[]
        {
            (Resref: "ss_custom", Tier: 2, OldDamage: 15, Damage: 13),
            (Resref: "ss_custom", Tier: 4, OldDamage: 23, Damage: 21),
            (Resref: "ss_custom", Tier: 6, OldDamage: 33, Damage: 28),
            (Resref: "trn_saberstaff_3", Tier: 6, OldDamage: 34, Damage: 28),
        })
        {
            var weapon = await CreateItemAsync(ctx, owner, profile.Resref, owner);
            string savedData = null;
            string migratedData = null;
            string identity = null;
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                RemovePropertiesImmediately(weapon);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, profile.OldDamage), weapon);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Delay, -1, 29), weapon);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Accuracy, -1, 5), weapon);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.RequiresSkill, 42, (profile.Tier - 1) * 10), weapon);
                SetLocalInt(weapon, "SABER_TIER", profile.Tier);
                SetName(weapon, "Upgraded custom saberstaff");
                identity = GetObjectUUID(weapon);
                savedData = ObjectPlugin.Serialize(weapon);
                DestroyObject(weapon);
            });
            // Native loading assigns a new UUID if the original instance is still alive.
            await ctx.DelaySecondsAsync(0.3f);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                // The resref still describes the item's original tier. Its persisted
                // tier records the completed in-place kits, plus four added DMG.
                var (changed, data) = MigrateStoredData(savedData);
                ctx.Assert(changed, "The upgraded stored weapon requires rebalance");
                ctx.Assert(ContainsIdentity(data, identity), "The stored payload retains its native identity");
                var migrated = Deserialize(ctx, data);
                ctx.AssertEqual(profile.Damage, Item.GetDMG(migrated),
                    $"{profile.Resref} tier {profile.Tier}: kit baseline and added damage migrate together");
                ctx.AssertEqual(profile.Tier, GetLocalInt(migrated, "SABER_TIER"), "Migration retains the attained kit tier");
                ctx.AssertEqual(24, PropertyValue(migrated, ItemPropertyType.Delay), "Both ends use the current saberstaff delay");
                ctx.AssertEqual(5, PropertyValue(migrated, ItemPropertyType.Accuracy), "The accuracy enhancement survives");
                ctx.AssertEqual((profile.Tier - 1) * 10, PropertyValue(migrated, ItemPropertyType.RequiresSkill, 42),
                    "The attained kit tier keeps its skill requirement");
                ctx.AssertEqual(identity, GetObjectUUID(migrated), "Saved native identity survives");
                ctx.AssertEqual("Upgraded custom saberstaff", GetName(migrated), "Custom name survives");
                ctx.Assert(!(bool)Invoke("SerializedItemWeaponDamageTypeMigration", "MigrateDoubleWeapons", migrated),
                    "Login/acquisition compatibility cannot subtract the kit adjustment again");
                migratedData = ObjectPlugin.Serialize(migrated);
                DestroyObject(migrated);
            });
            await ctx.DelaySecondsAsync(0.3f);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var (changed, retriedData) = MigrateStoredData(migratedData);
                ctx.Assert(!changed, "Retry requires no further normalization");
                ctx.AssertEqual(migratedData, retriedData, "Retry leaves the stored payload unchanged");
                ctx.Assert(ContainsIdentity(retriedData, identity), "Retry retains the stored native identity");
            });
        }
    }

    [EngineTest("Raw legacy double weapons use balanced end ratings through the existing migration", Category = "DoubleWeaponMigration")]
    public static async Task RawLegacyDoubleWeaponMigration(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("nw_rat001");
        await ctx.WaitFrameAsync();
        foreach (var profile in new[] { (Resref: "oph_twinblade", Delay: 23), (Resref: "trn_saberstaff_5", Delay: 24) })
        {
            var weapon = await CreateItemAsync(ctx, owner, profile.Resref, owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                var original = new System.Collections.Generic.List<SWLOR.NWN.API.Engine.ItemProperty>();
                for (var ip = GetFirstItemProperty(weapon); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(weapon))
                    original.Add(ip);
                AddLegacyProperty(weapon, ItemPropertyType.DMG, (int)CombatDamageType.Physical, 34, 27);
                foreach (var ip in original)
                    Invoke("MigrationObject", "RemoveProperty", weapon, ip);
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Accuracy, -1, 5), weapon);
                var migrated = MigrateSerialized(ctx, weapon);
                ctx.AssertEqual(21, Item.GetDMG(migrated), "Legacy tier-five damage matches the current end rating");
                ctx.AssertEqual(profile.Delay, PropertyValue(migrated, ItemPropertyType.Delay), "Legacy delay uses the current end budget");
                ctx.AssertEqual(5, PropertyValue(migrated, ItemPropertyType.Accuracy), "Legacy accuracy is retained");
                ctx.AssertEqual(21, Item.GetDMG(MigrateSerialized(ctx, migrated)), "Raw legacy migration retry is stable");
            });
        }
    }

    [EngineTest("Double weapon compatibility updates player gear and retains NPC budgets", Category = "DoubleWeaponMigration")]
    public static async Task DoubleWeaponCompatibilityAndNpcExclusion(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("nw_rat001");
        await ctx.WaitFrameAsync();
        var weapon = await CreateItemAsync(ctx, owner, "b_twinblade", owner);
        var npcWeapon = await CreateItemAsync(ctx, owner, "vnpcmsentguard", owner);
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            RemovePropertiesImmediately(weapon);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.DMG, -1, 11), weapon);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Delay, -1, 29), weapon);
            ctx.Assert((bool)Invoke("SerializedItemWeaponDamageTypeMigration", "MigrateDoubleWeapons", owner), "The login/acquisition compatibility traversal finds the old item");
            ctx.AssertEqual(9, Item.GetDMG(weapon), "Compatibility retains the basic weapon's added damage");
            ctx.AssertEqual(23, PropertyValue(weapon, ItemPropertyType.Delay), "Compatibility updates its end delay");
            ctx.Assert(!(bool)Invoke("SerializedItemWeaponDamageTypeMigration", "MigrateDoubleWeapons", owner), "Compatibility is idempotent");
            ctx.AssertEqual(53, Item.GetDMG(npcWeapon), "Authored NPC damage remains intact");
            ctx.AssertEqual(29, PropertyValue(npcWeapon, ItemPropertyType.Delay), "Authored NPC end delay remains intact");
            var migratedNpc = MigrateSerialized(ctx, npcWeapon);
            ctx.AssertEqual(53, Item.GetDMG(migratedNpc), "Persisted NPC damage remains intact");
            ctx.AssertEqual(29, PropertyValue(migratedNpc, ItemPropertyType.Delay), "Persisted NPC delay remains intact");
        });
    }
}
