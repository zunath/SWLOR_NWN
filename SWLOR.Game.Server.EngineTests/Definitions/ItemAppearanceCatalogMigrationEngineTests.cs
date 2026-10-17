using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    private const string AppearanceCategory = "ItemAppearanceMigration";

    [EngineTest("Stored copies move onto catalog appearances and crafted Arkanian armor is revised in the combined stored-item pass",
        Category = AppearanceCategory, TimeoutSeconds = 150f)]
    public static async Task StoredAppearanceAndArkanianArmor(EngineTestContext ctx)
    {
        var owner = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var enhancements = new Dictionary<(ItemPropertyType, int), int>
        {
            [(ItemPropertyType.HPBonus, 0)] = 40,
            [(ItemPropertyType.Defense, (int)CombatDamageType.Physical)] = 3,
        };
        var legacy = new[] { "asc_greatsword", "asc_chn_tunic", "frontier_helmet", "redcircle_star", "proto_knife" };
        // Shipped breastplates: retired Ice Defense beside an Ice Resistance enhancement, the same copy after
        // losing the retired property, Ice Defense the combat replacement already converted, and no Ice at all.
        var arkanian = new (int Defense, int Resistance, int Expected)[] { (30, 5, 35), (0, 5, 35), (0, 30, 30), (0, 0, 30) };
        var expectedIce = new Dictionary<string, int>();
        var records = new List<InventoryItem>();
        var identities = new Dictionary<string, string>();
        var config = DB.Get<ServerConfiguration>("SWLOR_CONFIG");
        var originalVersion = config.MigrationVersion;
        DMCreature archive = null;
        int[] customized = null;
        try
        {
            foreach (var (resref, ice) in legacy.Select(resref => (resref, -1))
                         .Concat(Enumerable.Range(0, arkanian.Length).Select(index => ("ark_armor", index))))
            {
                var item = await CreateItemAsync(ctx, owner, resref, owner);
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    if (resref == "ark_armor")
                        StampLegacyArkanianArmor(item, enhancements, arkanian[ice].Defense, arkanian[ice].Resistance);
                    else
                        StampLegacyAppearance(ctx, item);
                    if (resref == "proto_knife")
                    {
                        // A different value in a part the blueprint changed marks the copy as customized.
                        ItemPlugin.SetItemAppearance(item, ItemAppearanceType.WeaponColor, (int)AppearanceWeapon.Middle, 2);
                        customized = AppearanceParts(item);
                    }
                    var identity = GetObjectUUID(item);
                    var record = new InventoryItem
                    {
                        Data = ObjectPlugin.Serialize(item), Name = resref, Quantity = GetItemStackSize(item),
                        IconResref = Item.GetIconResref(item),
                    };
                    DB.Set(record);
                    records.Add(record);
                    identities[record.Id] = identity;
                    if (resref == "ark_armor")
                        expectedIce[record.Id] = arkanian[ice].Expected;
                    Invoke("MigrationObject", "DestroyTemporaryObject", item);
                });
            }

            var weapon = await CreateItemAsync(ctx, owner, "raider_knife", owner);
            var tunic = await CreateItemAsync(ctx, owner, "gr_tunic", owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                StampLegacyAppearance(ctx, weapon);
                StampLegacyAppearance(ctx, tunic);
                RemoveRequirement(weapon);
            });
            var held = GetItemInSlot(InventorySlot.RightHand, owner);
            if (GetIsObjectValid(held)) await UnequipAsync(ctx, owner, held);
            await EquipAsync(ctx, owner, weapon, InventorySlot.RightHand);
            await ctx.ExecuteInCreatureContextAsync(owner, () =>
            {
                archive = new DMCreature("Appearance archive", "appearance_archive", ObjectPlugin.Serialize(owner));
                DB.Set(archive);
            });

            for (var cycle = 1; cycle <= 2; cycle++)
                await ctx.ExecuteInCreatureContextAsync(owner, () =>
                {
                    config.MigrationVersion = 27;
                    DB.Set(config);
                    var pending = (IEnumerable<IServerMigration>)typeof(Migration)
                        .GetMethod("GetMigrations", BindingFlags.NonPublic | BindingFlags.Static)
                        .Invoke(null, new object[] { MigrationExecutionType.PostCacheLoad });
                    ctx.Assert(pending.Select(migration => migration.Version).SequenceEqual(new[] { 28 }),
                        "The last released server checkpoint schedules exactly one combined stored-item migration");
                    Migration.AfterCacheLoaded();
                    ctx.AssertEqual(28, DB.Get<ServerConfiguration>("SWLOR_CONFIG").MigrationVersion,
                        "The stored-item pass advances the server checkpoint");

                    foreach (var record in records)
                    {
                        var saved = DB.Get<InventoryItem>(record.Id);
                        var restored = Deserialize(ctx, saved.Data);
                        if (record.Name == "ark_armor")
                            AssertRevisedArkanianArmor(ctx, restored, enhancements, expectedIce[record.Id]);
                        else if (record.Name == "proto_knife")
                            ctx.AssertEqual(string.Join(",", customized), string.Join(",", AppearanceParts(restored)),
                                "A customized copy keeps its chosen parts");
                        else
                            AssertCatalogAppearance(ctx, restored);
                        ctx.AssertEqual(Item.GetIconResref(restored), saved.IconResref, $"{record.Name} storage icon matches its appearance");
                        ctx.AssertEqual(record.Name, saved.Name, "Storage metadata survives migration");
                        ctx.AssertEqual(record.Quantity, saved.Quantity, "Storage quantity survives migration");
                        ctx.AssertEqual(record.Quantity, GetItemStackSize(restored), "Item quantity survives reload");
                        ctx.AssertEqual(identities[record.Id], GetObjectUUID(restored), $"{record.Name} identity survives reload");
                        ctx.AssertEqual(cycle - 1, GetLocalInt(restored, "APPEARANCE_TEST_CYCLE"), "Changed state survives the next reload");
                        ctx.Assert(!LootStatRevisionMigration.MigratePlayerItems(restored), "Reloaded items need no further migration");
                        SetLocalInt(restored, "APPEARANCE_TEST_CYCLE", cycle);
                        saved.Data = ObjectPlugin.Serialize(restored);
                        DB.Set(saved);
                        Invoke("MigrationObject", "DestroyTemporaryObject", restored);
                    }

                    archive = DB.Get<DMCreature>(archive.Id);
                    var creature = Deserialize(ctx, archive.Data);
                    var archivedWeapon = GetItemInSlot(InventorySlot.RightHand, creature);
                    ctx.AssertEqual("raider_knife", GetResRef(archivedWeapon), "The archived creature keeps its weapon equipped");
                    AssertCatalogAppearance(ctx, archivedWeapon);
                    var archivedTunic = FindHeadgear(creature, "gr_tunic");
                    ctx.AssertEqual(creature, GetItemPossessor(archivedTunic), "The archived creature keeps its carried armor");
                    AssertCatalogAppearance(ctx, archivedTunic);
                    ctx.AssertEqual(cycle - 1, GetLocalInt(archivedWeapon, "APPEARANCE_TEST_CYCLE"), "Archived changes survive the next reload");
                    SetLocalInt(archivedWeapon, "APPEARANCE_TEST_CYCLE", cycle);
                    archive.Data = ObjectPlugin.Serialize(creature);
                    DB.Set(archive);
                    DestroyObject(creature);
                });
        }
        finally
        {
            foreach (var record in records)
                DB.Delete<InventoryItem>(record.Id);
            if (archive != null)
                DB.Delete<DMCreature>(archive.Id);
            config.MigrationVersion = originalVersion;
            DB.Set(config);
        }
    }

    [EngineTest("Equipped and carried player copies move onto catalog appearances and Arkanian armor stats count once through reloads",
        Category = AppearanceCategory, TimeoutSeconds = 180f)]
    public static async Task PlayerAppearanceAndArkanianArmor(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        var player = fixture.Creature;
        fixture.Update(record => record.Skills[SkillType.Armor].Rank = 50);
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            // Keep the complete HP budget below the one-level fixture's native cap.
            var record = DB.Get<Player>(GetObjectUUID(player));
            Stat.AdjustPlayerMaxHP(record, player, 50 - record.MaxHP);
            DB.Set(record);
            SetCurrentHitPoints(player, 50);
        });
        var enhancements = new Dictionary<(ItemPropertyType, int), int> { [(ItemPropertyType.Defense, (int)CombatDamageType.Physical)] = 2 };
        var armor = await CreateItemAsync(ctx, player, "ark_armor", player);
        var weapon = await CreateItemAsync(ctx, player, "asc_knife", player);
        var stars = await CreateItemAsync(ctx, player, "redcircle_star", player);
        var bag = await CreateItemAsync(ctx, player, "bag_sm", player);
        var nested = await CreateItemAsync(ctx, player, "dancer_f_2", bag);
        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            // A copy that no longer has the retired Ice Defense keeps only its enhancement's Ice Resistance.
            StampLegacyArkanianArmor(armor, enhancements, 0, 5);
            foreach (var item in new[] { weapon, stars, nested })
                StampLegacyAppearance(ctx, item);
            RemoveRequirement(weapon);
        });
        var baseline = ReadArmorTotals(player);
        var baselineHp = GetMaxHitPoints(player);
        await EquipAsync(ctx, player, armor, InventorySlot.Chest);
        await EquipAsync(ctx, player, weapon, InventorySlot.RightHand);
        ctx.AssertEqual(baselineHp + 95, GetMaxHitPoints(player), "The fixture wears the shipped breastplate");
        ctx.AssertEqual(baseline.IceResistance + 5, ReadArmorTotals(player).IceResistance,
            "Only the enhancement's Ice Resistance counts before the revision");

        await ctx.ExecuteInCreatureContextAsync(player, () =>
        {
            var record = DB.Get<Player>(GetObjectUUID(player));
            record.Version = 22;
            DB.Set(record);
            SetLocalInt(player, "PLAYER_MIGRATION_VERSION", 22);
            var fileVersion = 22;
            var pending = typeof(_23_CorrectEquipmentTypes).Assembly.GetTypes()
                .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IPlayerMigration).IsAssignableFrom(type))
                .Select(type => (IPlayerMigration)Activator.CreateInstance(type))
                .Where(migration => migration.Version > Math.Min(record.Version, fileVersion)).ToArray();
            ctx.AssertEqual(23, pending.Single().Version, "The last released player checkpoint schedules exactly one combined item migration");
            var apply = typeof(Migration).GetMethod("ApplyPlayerMigration", BindingFlags.NonPublic | BindingFlags.Static);
            ctx.Assert((bool)apply.Invoke(null, new object[] { pending.Single(), player,
                (Func<Player>)(() => DB.Get<Player>(GetObjectUUID(player))), (Action<Player>)(updated => DB.Set(updated)),
                (Func<int>)(() => fileVersion), (Action<int>)(version =>
                {
                    SetLocalInt(player, "PLAYER_MIGRATION_VERSION", version);
                    fileVersion = version;
                }) }), "The actual player checkpoint path converts the legacy items");
            ctx.AssertEqual(23, DB.Get<Player>(GetObjectUUID(player)).Version, "The player database checkpoint advances");
            ctx.AssertEqual(armor, GetItemInSlot(InventorySlot.Chest, player), "The revised breastplate stays equipped");
            ctx.AssertEqual(weapon, GetItemInSlot(InventorySlot.RightHand, player), "The converted weapon stays equipped");
            AssertRevisedArkanianArmor(ctx, armor, enhancements, 35);
            foreach (var item in new[] { weapon, stars, nested })
                AssertCatalogAppearance(ctx, item);
            ctx.AssertEqual(1, GetItemStackSize(stars), "The converted throwing star keeps its quantity");
            ctx.Assert(Carried(bag).Contains(nested), "The nested armor stays in its bag");
            ctx.Assert(!LootStatRevisionMigration.MigratePlayerItems(player), "Retrying the combined traversal is stable");
        });
        AssertArmorTotals(ctx, baseline, ReadArmorTotals(player), enhancements, 35, "after migration");
        ctx.AssertEqual(baselineHp + 110, GetMaxHitPoints(player), "Equipped HP counts the revised breastplate once");

        for (var cycle = 1; cycle <= 2; cycle++)
        {
            await UnequipAsync(ctx, player, armor);
            await UnequipAsync(ctx, player, weapon);
            AssertArmorTotals(ctx, baseline, ReadArmorTotals(player), null, 0, "after unequip");
            ctx.AssertEqual(baselineHp, GetMaxHitPoints(player), "Unequipping removes exactly the revised HP");
            var saved = new Dictionary<string, (string Data, string Identity, int Quantity)>();
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                foreach (var item in new[] { armor, weapon, stars })
                {
                    SetLocalInt(item, "APPEARANCE_TEST_CYCLE", cycle);
                    var identity = GetObjectUUID(item);
                    saved[GetResRef(item)] = (ObjectPlugin.Serialize(item), identity, GetItemStackSize(item));
                    DestroyObject(item);
                }
            });
            await ctx.WaitFrameAsync();
            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                foreach (var data in saved.Values)
                    ctx.Assert(ObjectPlugin.AcquireItem(player, Deserialize(ctx, data.Data)), "The player reacquires the saved item");
            });
            await ctx.WaitFrameAsync();
            uint Reloaded(string resref)
            {
                var matching = Carried(player).Where(item => GetResRef(item) == resref).ToList();
                ctx.AssertEqual(1, matching.Count, $"Reload preserves exactly one {resref}");
                var item = matching.Single();
                ctx.Track(item);
                ctx.AssertEqual(player, GetItemPossessor(item), "Ownership survives reload");
                ctx.AssertEqual(saved[resref].Quantity, GetItemStackSize(item), "Quantity survives reload");
                ctx.AssertEqual(saved[resref].Identity, GetObjectUUID(item), "Identity survives reload");
                ctx.AssertEqual(cycle, GetLocalInt(item, "APPEARANCE_TEST_CYCLE"), "Changed state survives reload");
                return item;
            }
            armor = Reloaded("ark_armor");
            weapon = Reloaded("asc_knife");
            stars = Reloaded("redcircle_star");
            AssertRevisedArkanianArmor(ctx, armor, enhancements, 35);
            AssertCatalogAppearance(ctx, weapon);
            AssertCatalogAppearance(ctx, stars);
            await EquipAsync(ctx, player, armor, InventorySlot.Chest);
            await EquipAsync(ctx, player, weapon, InventorySlot.RightHand);
            AssertArmorTotals(ctx, baseline, ReadArmorTotals(player), enhancements, 35, $"after reload {cycle}");
            ctx.AssertEqual(baselineHp + 110, GetMaxHitPoints(player), "Reloaded HP counts once");
        }
        await UnequipAsync(ctx, player, armor);
        await UnequipAsync(ctx, player, weapon);
    }

    internal static void StampLegacyAppearance(EngineTestContext ctx, uint item)
    {
        var revision = ItemAppearanceCatalogMigration.Revisions[GetResRef(item)];
        ctx.AssertEqual(string.Join(",", revision.Current), string.Join(",", AppearanceParts(item)),
            $"{GetResRef(item)} blueprint reads as its catalog appearance");
        for (var index = 0; index < revision.Previous.Length; index++)
            switch (revision.Previous.Length)
            {
                case 3:
                    ItemPlugin.SetItemAppearance(item, ItemAppearanceType.WeaponModel, index, revision.Previous[index] / 10);
                    ItemPlugin.SetItemAppearance(item, ItemAppearanceType.WeaponColor, index, revision.Previous[index] % 10);
                    break;
                case (int)AppearanceArmor.Num:
                    ItemPlugin.SetItemAppearance(item, ItemAppearanceType.ArmorModel, index, revision.Previous[index]);
                    break;
                default:
                    ItemPlugin.SetItemAppearance(item, ItemAppearanceType.SimpleModel, index, revision.Previous[index]);
                    break;
            }
        ctx.AssertEqual(string.Join(",", revision.Previous), string.Join(",", AppearanceParts(item)),
            $"{GetResRef(item)} fixture carries the shipped appearance");
    }

    internal static void AssertCatalogAppearance(EngineTestContext ctx, uint item, bool everyPart = true)
    {
        var revision = ItemAppearanceCatalogMigration.Revisions[GetResRef(item)];
        var parts = AppearanceParts(item);
        var checkedParts = Enumerable.Range(0, parts.Length)
            .Where(index => everyPart || revision.Current[index] != revision.Previous[index]).ToArray();
        ctx.AssertEqual(string.Join(",", checkedParts.Select(index => revision.Current[index])),
            string.Join(",", checkedParts.Select(index => parts[index])), $"{GetResRef(item)} uses its catalog appearance");
    }

    private static int[] AppearanceParts(uint item) =>
        (int[])Invoke("ItemAppearanceCatalogMigration", "ReadParts", item, ItemAppearanceCatalogMigration.Revisions[GetResRef(item)].Current.Length);

    private static void StampLegacyArkanianArmor(uint item, Dictionary<(ItemPropertyType, int), int> enhancements,
        int iceDefense, int iceResistance)
    {
        var shipped = ArkanianArmorStatRevisionCatalog.Changes[GetResRef(item)]
            .Select(change => (change.Type, change.Subtype, change.Previous))
            .Append((ItemPropertyType.Defense, (int)CombatDamageType.Ice, 0))
            .Append((ItemPropertyType.Resistance, (int)ResistanceType.Ice, 0));
        foreach (var (type, subtype, previous) in shipped)
        {
            var matches = new List<SWLOR.NWN.API.Engine.ItemProperty>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                if (GetItemPropertyType(ip) == type && (!HasArkanianSubtype(type) || GetItemPropertySubType(ip) == subtype))
                    matches.Add(ip);
            foreach (var ip in matches)
                Invoke("MigrationObject", "RemoveProperty", item, ip);
            var legacy = previous + enhancements.GetValueOrDefault((type, subtype));
            if (legacy > 0)
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(type, HasArkanianSubtype(type) ? subtype : -1, legacy), item);
        }
        // Current constructors reject the retired Ice Defense subtype, so it is written natively.
        if (iceDefense > 0)
            AddLegacyProperty(item, ItemPropertyType.Defense, (int)CombatDamageType.Ice, 35, iceDefense);
        if (iceResistance > 0)
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Resistance, (int)ResistanceType.Ice,
                Resistance.EncodeItemPropertyCostTableValue(iceResistance)), item);
        DeleteLocalInt(item, ArkanianArmorStatRevisionCatalog.RevisionVariable);
    }

    private static void AssertRevisedArkanianArmor(EngineTestContext ctx, uint item,
        Dictionary<(ItemPropertyType, int), int> enhancements, int iceResistance)
    {
        foreach (var change in ArkanianArmorStatRevisionCatalog.Changes[GetResRef(item)])
            ctx.AssertEqual(change.Current + enhancements.GetValueOrDefault((change.Type, change.Subtype)),
                ArkanianTotal(item, change.Type, change.Subtype), $"ark_armor {change.Type} {change.Subtype} is revised");
        ctx.AssertEqual(0, ArkanianTotal(item, ItemPropertyType.Defense, (int)CombatDamageType.Ice),
            "ark_armor carries no retired Ice Defense");
        ctx.AssertEqual(iceResistance, ArkanianTotal(item, ItemPropertyType.Resistance, (int)ResistanceType.Ice),
            "ark_armor Ice Resistance is revised");
        ctx.AssertEqual(ArkanianArmorStatRevisionCatalog.CurrentRevision,
            GetLocalInt(item, ArkanianArmorStatRevisionCatalog.RevisionVariable), "ark_armor is marked as revised");
    }

    private static int ArkanianTotal(uint item, ItemPropertyType type, int subtype)
    {
        var total = 0;
        for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            if (GetItemPropertyType(ip) == type && (!HasArkanianSubtype(type) || GetItemPropertySubType(ip) == subtype))
                total += type == ItemPropertyType.Resistance
                    ? Resistance.DecodeItemPropertyCostTableValue(GetItemPropertyCostTableValue(ip))
                    : GetItemPropertyCostTableValue(ip);
        return total;
    }

    private static bool HasArkanianSubtype(ItemPropertyType type) =>
        type is ItemPropertyType.Defense or ItemPropertyType.Resistance or ItemPropertyType.RequiresSkill;

    private static (int MaxHP, int Physical, int Force, int IceDefense, int IceResistance, int StaminaRegen) ReadArmorTotals(uint player)
    {
        var record = DB.Get<Player>(GetObjectUUID(player));
        return (record.MaxHP, record.Defenses.GetValueOrDefault(CombatDamageType.Physical), record.Defenses.GetValueOrDefault(CombatDamageType.Force),
            record.Defenses.GetValueOrDefault(CombatDamageType.Ice), record.Resistances.GetValueOrDefault(ResistanceType.Ice), record.STMRegen);
    }

    private static void AssertArmorTotals(EngineTestContext ctx,
        (int MaxHP, int Physical, int Force, int IceDefense, int IceResistance, int StaminaRegen) baseline,
        (int MaxHP, int Physical, int Force, int IceDefense, int IceResistance, int StaminaRegen) actual,
        Dictionary<(ItemPropertyType, int), int> enhancements, int iceResistance, string when)
    {
        int Worn(ItemPropertyType type, int subtype) => enhancements == null ? 0 :
            ArkanianArmorStatRevisionCatalog.Changes["ark_armor"].Single(x => x.Type == type && x.Subtype == subtype).Current +
            enhancements.GetValueOrDefault((type, subtype));
        ctx.AssertEqual(baseline.MaxHP + Worn(ItemPropertyType.HPBonus, 0), actual.MaxHP, $"Persisted HP {when}");
        ctx.AssertEqual(baseline.Physical + Worn(ItemPropertyType.Defense, (int)CombatDamageType.Physical), actual.Physical, $"Physical Defense {when}");
        ctx.AssertEqual(baseline.Force + Worn(ItemPropertyType.Defense, (int)CombatDamageType.Force), actual.Force, $"Force Defense {when}");
        ctx.AssertEqual(baseline.IceDefense, actual.IceDefense, $"Retired Ice Defense {when}");
        ctx.AssertEqual(baseline.IceResistance + iceResistance, actual.IceResistance, $"Ice Resistance {when}");
        ctx.AssertEqual(baseline.StaminaRegen + Worn(ItemPropertyType.STMRegen, 0), actual.StaminaRegen, $"STM regen {when}");
    }

    private static IEnumerable<uint> Carried(uint creature)
    {
        for (var item = GetFirstItemInInventory(creature); GetIsObjectValid(item); item = GetNextItemInInventory(creature))
            yield return item;
    }
}
