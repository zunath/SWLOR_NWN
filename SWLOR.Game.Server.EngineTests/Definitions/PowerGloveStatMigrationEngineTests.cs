using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Migration 25 adds the other hand's share of a worn Power Glove's stats once and survives two equip cycles",
        Category = "PowerGloveMigration", TimeoutSeconds = 120)]
    public static async Task PowerGloveStatsCountPerHand(EngineTestContext ctx)
    {
        using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
        fixture.Update(record =>
        {
            record.Skills[SkillType.Unarmed].Rank = 50;
            // The fixture's food HP holds 1000 maximum HP, more than its one level's hit die can store.
            record.TemporaryFoodHP = 0;
        });
        var owner = fixture.Creature;
        // Rebuild the fixture's oversized HP and STM pools from worn gear, so the file's level hit die holds the maximum.
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            Stat.RefreshPlayerEquipmentStats(owner);
            Stat.RefreshPlayerMaxHP(owner);
        });
        var baseline = ReadGloveTotals(DB.Get<Player>(fixture.Id));
        var glove = await PowerGlovePairEngineTests.CreateCarriedItem(ctx, owner, "t_pglove");
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Attack, -1, 5), glove);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.HPBonus, -1, 7), glove);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Stamina, -1, 9), glove);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.CombatReadiness, -1, 2), glove);
        });
        await PowerGlovePairEngineTests.EquipExisting(ctx, owner, glove, InventorySlot.RightHand);
        await PowerGlovePairEngineTests.WaitForPair(ctx, owner, glove);
        var worn = ReadGloveTotals(DB.Get<Player>(fixture.Id));
        ctx.AssertEqual((baseline.Attack + 10, baseline.MaxHP + 14, baseline.MaxStamina + 18, baseline.CombatReadiness + 4), worn,
            "A worn glove counts its stats once per hand");

        SavedCharacterMigration.Upgraded upgraded = null;
        await ctx.ExecuteInCreatureContextAsync(owner, () =>
        {
            // Before migration 25 the record and the file's levels counted the worn glove's HP once.
            var player = DB.Get<Player>(fixture.Id);
            Stat.AdjustPlayerMaxHP(player, owner, -7);
            DB.Set(player);
            var legacy = SavedCharacterMigration.CopyRecord(player);
            legacy.Attack -= 5;
            legacy.MaxStamina -= 9;
            legacy.CombatReadiness -= 2;
            legacy.Version = 24;
            legacy.CharacterFileVersion = 0;
            var file = SavedCharacterMigration.WithFileVersion(SavedCharacterMigration.SaveFile(owner), 24);
            var legacyMaxHP = GetMaxHitPoints(SavedCharacterMigration.Load(ctx,
                new SavedCharacterMigration.Upgraded(file, legacy,
                    new List<SavedCharacterMigration.UpgradedOverflowItem>(), new List<string>())));

            upgraded = SavedCharacterMigration.Upgrade(file, legacy);
            ctx.Assert(upgraded != null, "A character from before migration 25 is upgraded");
            ctx.AssertEqual(worn, ReadGloveTotals(upgraded.Record), "The upgrade adds the other hand's share once");
            var character = SavedCharacterMigration.Load(ctx, upgraded);
            ctx.AssertEqual(SavedCharacterMigration.FindItem(character, "t_pglove"), GetItemInSlot(InventorySlot.RightHand, character),
                "The glove stays worn");
            ctx.AssertEqual(legacyMaxHP + 7, GetMaxHitPoints(character), "The upgraded file's levels hold the other hand's HP");
            ctx.Assert(SavedCharacterMigration.Upgrade(upgraded.File, upgraded.Record) == null, "An upgraded character is current");
            var restored = SavedCharacterMigration.Upgrade(file, upgraded.Record);
            ctx.Assert(restored != null, "An older file restored beside the upgraded record is upgraded");
            ctx.AssertEqual(worn, ReadGloveTotals(restored.Record), "The upgraded record does not gain the share again");
            ctx.AssertEqual(legacyMaxHP + 7, GetMaxHitPoints(SavedCharacterMigration.Load(ctx, restored)),
                "The restored file's levels match the upgraded record's maximum HP");
        });

        // The player logs in wearing the glove and swaps it off and on twice without drift.
        using var login = await PlayerAbilityFixture.CreateFromCharacterAsync(ctx, upgraded.File, upgraded.Record, 3f);
        owner = login.Creature;
        glove = SavedCharacterMigration.FindItem(owner, "t_pglove");
        ctx.AssertEqual(glove, GetItemInSlot(InventorySlot.RightHand, owner), "The upgraded character logs in wearing the glove");
        // A headless login raises no enter event, so rebuild the generated hand as entering the module does.
        await ctx.ExecuteInCreatureContextAsync(owner, () => PowerGloveEquipment.RestorePair(owner));
        await PowerGlovePairEngineTests.WaitForPair(ctx, owner, glove);
        ctx.AssertEqual(worn, ReadGloveTotals(DB.Get<Player>(login.Id)), "Login keeps the upgraded totals");
        for (var cycle = 0; cycle < 2; cycle++)
        {
            var companion = GetItemInSlot(InventorySlot.LeftHand, owner);
            await ctx.ExecuteInCreatureContextAsync(owner, () => ActionUnequipItem(glove));
            await PowerGlovePairEngineTests.WaitForRemovedPair(ctx, owner, glove, companion);
            ctx.AssertEqual(baseline, ReadGloveTotals(DB.Get<Player>(login.Id)), "Removing the glove returns exactly to the base totals");
            await PowerGlovePairEngineTests.EquipExisting(ctx, owner, glove, InventorySlot.RightHand);
            await PowerGlovePairEngineTests.WaitForPair(ctx, owner, glove);
            ctx.AssertEqual(worn, ReadGloveTotals(DB.Get<Player>(login.Id)), "Re-equipping counts both hands again");
        }
    }

    private static (int Attack, int MaxHP, int MaxStamina, int CombatReadiness) ReadGloveTotals(Player record) =>
        (record.Attack, record.MaxHP, record.MaxStamina, record.CombatReadiness);
}
