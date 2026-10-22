using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// A player's stored equipment Defense, Evasion, max FP and max STM are kept by adding and removing each equipment
    /// change. A reported character showed Force DEF 13 below Physical DEF naked (Willpower 2 above Vitality) and FP 18
    /// below its base: both running sums had drifted below zero.
    /// </summary>
    public static class EquipmentRecordReconciliationEngineTests
    {
        private const string Category = "EquipmentRecordReconciliation";
        private const int DriftedPhysical = -32;
        private const int DriftedForce = -47;

        private static readonly (ItemPropertyType Type, int Subtype, int Amount)[] BeltProperties =
        {
            (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 11),
            (ItemPropertyType.Defense, (int)CombatDamageType.Force, 9),
            (ItemPropertyType.Evasion, -1, 10),
            // Retired Poison Defense never counts on the wearer.
            (ItemPropertyType.Defense, (int)CombatDamageType.Poison, 30),
        };

        private static readonly (ItemPropertyType Type, int Subtype, int Amount)[] RingProperties =
        {
            (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 10),
            (ItemPropertyType.Defense, (int)CombatDamageType.Force, 10),
            (ItemPropertyType.FP, -1, 8),
            (ItemPropertyType.Stamina, -1, 8),
        };

        [EngineTest("Equipping gear rebuilds drifted Defense, Evasion, FP and STM records", Category = Category, TimeoutSeconds = 60f)]
        public static async Task DriftedRecordIsRepairedByEquipChanges(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            ctx.SuppressNPCNaturalRegen(player);
            var belt = await CreateItemAsync(ctx, player, "advent_belt", BeltProperties);
            var ring = await CreateItemAsync(ctx, player, "absdef_l6", RingProperties);
            // Armor rank keeps the drifted Defense above the one-point floor, so the gap is visible.
            fixture.Update(record => record.Skills[SkillType.Armor].Rank = 50);
            Drift(fixture);

            var vitality = GetAbilityScore(player, AbilityType.Vitality);
            var willpower = GetAbilityScore(player, AbilityType.Willpower);
            ctx.AssertEqual(willpower - vitality + DriftedForce - DriftedPhysical, ForceGap(player),
                "the drifted record pulls naked Force DEF below Physical DEF (13 below for the reported character)");

            await EquipAsync(ctx, player, belt, InventorySlot.Belt);
            AssertRecord(ctx, player, physical: 11, force: 9, evasion: 10, fp: Stat.BaseFP - 18, stamina: Stat.BaseSTM,
                "the belt rebuilds Defense and Evasion; it carries no FP or STM, so those records wait for gear that does");

            await EquipAsync(ctx, player, ring, InventorySlot.RightRing);
            AssertRecord(ctx, player, physical: 21, force: 19, evasion: 10, fp: Stat.BaseFP + 8, stamina: Stat.BaseSTM + 8,
                "the ring's FP and STM rebuild those records from both worn pieces");

            await UnequipAsync(ctx, player, ring);
            await UnequipAsync(ctx, player, belt);
            AssertRecord(ctx, player, physical: 0, force: 0, evasion: 0, fp: Stat.BaseFP, stamina: Stat.BaseSTM,
                "unequipping returns every record to its base");
            ctx.AssertEqual(willpower - vitality, ForceGap(player), "naked Force DEF sits Willpower minus Vitality above Physical DEF");
        }

        [EngineTest("Login rebuilds drifted Defense, Evasion, FP and STM records from worn gear", Category = Category, TimeoutSeconds = 60f)]
        public static async Task LoginRepairsDriftedRecord(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            ctx.SuppressNPCNaturalRegen(player);
            var belt = await CreateItemAsync(ctx, player, "advent_belt", BeltProperties);
            var ring = await CreateItemAsync(ctx, player, "absdef_l6", RingProperties);
            await EquipAsync(ctx, player, belt, InventorySlot.Belt);
            await EquipAsync(ctx, player, ring, InventorySlot.RightRing);

            // Saved equipment emits no equip events at login, so only the login rebuild can repair it.
            Drift(fixture);
            fixture.Update(record =>
            {
                record.MaxFP = 10000;
                record.FP = 9999;
            });

            var corrected = EquipmentRecordStats.None;
            await ctx.ExecuteInCreatureContextAsync(player, () => corrected = Stat.RefreshPlayerEquipmentStats(player));
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(EquipmentRecordStats.All, corrected, "every drifted record is reported corrected");
            AssertRecord(ctx, player, physical: 21, force: 19, evasion: 10, fp: Stat.BaseFP + 8, stamina: Stat.BaseSTM + 8,
                "the records are rebuilt from the worn gear at login");
            ctx.AssertEqual(Stat.GetMaxFP(player), Record(player).FP, "current FP is capped at the corrected maximum");

            await ctx.ExecuteInCreatureContextAsync(player, () => corrected = Stat.RefreshPlayerEquipmentStats(player));
            ctx.AssertEqual(EquipmentRecordStats.None, corrected, "a matching record is left alone");
        }

        [EngineTest("A canceled equip into an occupied slot leaves the records on the worn gear", Category = Category, TimeoutSeconds = 60f)]
        public static async Task CanceledSwapKeepsRecordsConsistent(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            ctx.SuppressNPCNaturalRegen(player);
            var first = await CreateItemAsync(ctx, player, "absdef_l6", RingProperties);
            var second = await CreateItemAsync(ctx, player, "absdef_l6", BeltProperties);
            // The fixture seeds Evasion without gear, and the first ring carries none to rebuild it from.
            fixture.Update(record => record.Evasion = 0);
            await EquipAsync(ctx, player, first, InventorySlot.RightRing);
            AssertRecord(ctx, player, physical: 10, force: 10, evasion: 0, fp: Stat.BaseFP + 8, stamina: Stat.BaseSTM + 8, "the first ring");

            // The equip validator cancels an equip into an occupied slot before any stats move.
            await ctx.ExecuteInCreatureContextAsync(player, () => ActionEquipItem(second, InventorySlot.RightRing));
            for (var frame = 0; frame < 30; frame++)
                await ctx.WaitFrameAsync();
            ctx.AssertEqual(first, GetItemInSlot(InventorySlot.RightRing, player), "the occupied-slot equip is canceled");
            AssertRecord(ctx, player, physical: 10, force: 10, evasion: 0, fp: Stat.BaseFP + 8, stamina: Stat.BaseSTM + 8,
                "a canceled equip leaves the first ring's records");

            await UnequipAsync(ctx, player, first);
            AssertRecord(ctx, player, physical: 0, force: 0, evasion: 0, fp: Stat.BaseFP, stamina: Stat.BaseSTM,
                "nothing worn leaves every record at its base");
        }

        private static Player Record(uint player) => DB.Get<Player>(GetObjectUUID(player));

        private static void Drift(PlayerAbilityFixture fixture) => fixture.Update(record =>
        {
            record.Defenses[CombatDamageType.Physical] = DriftedPhysical;
            record.Defenses[CombatDamageType.Force] = DriftedForce;
            record.Evasion = 3;
            record.MaxFP = Stat.BaseFP - 18;
            record.FP = 0;
            record.MaxStamina = Stat.BaseSTM;
        });

        private static int ForceGap(uint player) =>
            Stat.GetDefense(player, CombatDamageType.Force, AbilityType.Willpower) -
            Stat.GetDefense(player, CombatDamageType.Physical, AbilityType.Vitality);

        private static void AssertRecord(EngineTestContext ctx, uint player, int physical, int force, int evasion, int fp, int stamina, string because)
        {
            var record = Record(player);
            ctx.AssertEqual(physical, record.Defenses[CombatDamageType.Physical], $"Physical Defense: {because}");
            ctx.AssertEqual(force, record.Defenses[CombatDamageType.Force], $"Force Defense: {because}");
            ctx.AssertEqual(evasion, record.Evasion, $"Evasion: {because}");
            ctx.AssertEqual(fp, record.MaxFP, $"max FP: {because}");
            ctx.AssertEqual(stamina, record.MaxStamina, $"max STM: {because}");
        }

        private static async Task<uint> CreateItemAsync(EngineTestContext ctx, uint player, string resref,
            (ItemPropertyType Type, int Subtype, int Amount)[] properties)
        {
            var item = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(player, () => item = CreateItemOnObject(resref, player));
            await ctx.WaitFrameAsync();
            ctx.Assert(GetIsObjectValid(item), $"create a real {resref}");
            ctx.Track(item);
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetFirstItemProperty(item))
                RemoveItemProperty(item, property);
            foreach (var (type, subtype, amount) in properties)
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(type, subtype, amount), item);
            return item;
        }

        private static async Task EquipAsync(EngineTestContext ctx, uint player, uint item, InventorySlot slot)
        {
            await ctx.ExecuteInCreatureContextAsync(player, () => ActionEquipItem(item, slot));
            await ctx.WaitUntilAsync(() => GetItemInSlot(slot, player) == item, 10f, $"equip {GetResRef(item)}");
            await ctx.WaitFrameAsync();
        }

        private static async Task UnequipAsync(EngineTestContext ctx, uint player, uint item)
        {
            var slot = Item.GetItemSlot(player, item);
            await ctx.ExecuteInCreatureContextAsync(player, () => ActionUnequipItem(item));
            await ctx.WaitUntilAsync(() => GetItemInSlot(slot, player) != item, 10f, $"unequip {GetResRef(item)}");
            await ctx.WaitFrameAsync();
        }
    }
}
