using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Stored equipment Defense, Evasion, max FP and max STM follow the gear actually worn.
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
            // Armor rank keeps the drifted Defense above the one-point floor.
            fixture.Update(record => record.Skills[SkillType.Armor].Rank = 50);
            Drift(fixture);

            var vitality = GetAbilityScore(player, AbilityType.Vitality);
            var willpower = GetAbilityScore(player, AbilityType.Willpower);
            ctx.AssertEqual(willpower - vitality + DriftedForce - DriftedPhysical, ForceGap(player),
                "the drifted record pulls naked Force DEF below Physical DEF");

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

        [EngineTest("A Defense-only equip leaves the FP and STM pools alone", Category = Category, TimeoutSeconds = 60f)]
        public static async Task DefenseOnlyEquipLeavesPools(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            ctx.SuppressNPCNaturalRegen(player);
            var plate = await CreateItemAsync(ctx, player, "advent_belt", new[] { (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 10) });
            // Drifted maximums below the current pools; only gear that feeds them may rebuild and cap them.
            fixture.Update(record =>
            {
                record.MaxFP = 0;
                record.FP = 500;
                record.MaxStamina = 0;
                record.Stamina = 500;
            });

            await EquipAsync(ctx, player, plate, InventorySlot.Belt);
            var record = Record(player);
            ctx.AssertEqual(10, record.Defenses[CombatDamageType.Physical], "the Defense record is rebuilt");
            ctx.AssertEqual(0, record.MaxFP, "max FP is not rebuilt by a Defense-only item");
            ctx.AssertEqual(500, record.FP, "current FP is not capped by an unrebuilt maximum");
            ctx.AssertEqual(500, record.Stamina, "current STM is not capped by an unrebuilt maximum");
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

        [EngineTest("Vanilla appearance crafting of worn armor keeps the records on the worn gear", Category = Category, TimeoutSeconds = 60f)]
        public static async Task AppearanceCraftingCopyIsCorrected(EngineTestContext ctx)
        {
            // The retired native Craft dialogue modified worn armor by copying it with the new part, destroying the
            // worn original and equipping the copy. Destroying a worn item raises the unequip event, so the records
            // follow the gear; the login rebuild is the backstop for any change that does not.
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            ctx.SuppressNPCNaturalRegen(player);
            var chest = GetItemInSlot(InventorySlot.Chest, player);
            if (GetIsObjectValid(chest)) await UnequipAsync(ctx, player, chest);
            var armor = await CreateItemAsync(ctx, player, "ec_armor", RingProperties);
            fixture.Update(record => record.Evasion = 0);
            await EquipAsync(ctx, player, armor, InventorySlot.Chest);
            AssertRecord(ctx, player, physical: 10, force: 10, evasion: 0, fp: Stat.BaseFP + 8, stamina: Stat.BaseSTM + 8, "the original armor");

            var copy = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(player, () =>
                copy = CopyItemAndModify(armor, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe, 1, true));
            await ctx.WaitFrameAsync();
            ctx.Assert(GetIsObjectValid(copy), "the appearance copy is created");
            ctx.Track(copy);
            await ctx.ExecuteInCreatureContextAsync(player, () => DestroyObject(armor));
            for (var frame = 0; frame < 10; frame++)
                await ctx.WaitFrameAsync();
            var afterDestroy = Record(player);
            ctx.Log($"After destroying the worn original: Physical {afterDestroy.Defenses[CombatDamageType.Physical]}, " +
                    $"Force {afterDestroy.Defenses[CombatDamageType.Force]}, max FP {afterDestroy.MaxFP}, max STM {afterDestroy.MaxStamina}.");

            await EquipAsync(ctx, player, copy, InventorySlot.Chest);
            AssertRecord(ctx, player, physical: 10, force: 10, evasion: 0, fp: Stat.BaseFP + 8, stamina: Stat.BaseSTM + 8,
                "equipping the copy rebuilds the records from the worn copy");

            // Destroying the worn copy leaves nothing worn.
            await ctx.ExecuteInCreatureContextAsync(player, () => DestroyObject(copy));
            for (var frame = 0; frame < 10; frame++)
                await ctx.WaitFrameAsync();
            var stale = Record(player);
            ctx.Log($"After destroying the worn copy: Physical {stale.Defenses[CombatDamageType.Physical]}, max FP {stale.MaxFP}.");
            await ctx.ExecuteInCreatureContextAsync(player, () => Stat.RefreshPlayerEquipmentStats(player));
            await ctx.WaitFrameAsync();
            AssertRecord(ctx, player, physical: 0, force: 0, evasion: 0, fp: Stat.BaseFP, stamina: Stat.BaseSTM,
                "the records match the gear actually worn after a login rebuild");
        }

        private static readonly (ItemPropertyType Type, int Subtype, int Amount)[] AttackRingProperties =
        {
            (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 10),
            (ItemPropertyType.FP, -1, 8),
            // Attack has no rebuild, so it shows every unmatched subtraction.
            (ItemPropertyType.Attack, -1, 20),
        };

        [EngineTest("An unequip call for an item that is not worn subtracts nothing", Category = Category, TimeoutSeconds = 60f)]
        public static async Task UnequipOfUnwornItemSubtractsNothing(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            ctx.SuppressNPCNaturalRegen(player);
            var ring = await CreateItemAsync(ctx, player, "absdef_l6", AttackRingProperties);
            var attack = Record(player).Attack;

            // The native unequip raises its before event even for an item it then refuses to unequip.
            await ctx.ExecuteInCreatureContextAsync(player, () => CreaturePlugin.RunUnequip(player, ring));
            for (var frame = 0; frame < 5; frame++)
                await ctx.WaitFrameAsync();
            ctx.AssertEqual(OBJECT_INVALID, GetItemInSlot(InventorySlot.RightRing, player), "the ring was never worn");
            ctx.AssertEqual(attack, Record(player).Attack, "Attack is unchanged by an unequip of an unworn item");
        }

        [EngineTest("Two queued unequips of one worn item subtract its stats once", Category = Category, TimeoutSeconds = 60f)]
        public static async Task RepeatedUnequipSubtractsOnce(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            ctx.SuppressNPCNaturalRegen(player);
            var ring = await CreateItemAsync(ctx, player, "absdef_l6", AttackRingProperties);
            var attack = Record(player).Attack;
            await EquipAsync(ctx, player, ring, InventorySlot.RightRing);
            ctx.AssertEqual(attack + 20, Record(player).Attack, "equipping adds the ring's Attack");

            await ctx.ExecuteInCreatureContextAsync(player, () =>
            {
                ActionUnequipItem(ring);
                ActionUnequipItem(ring);
            });
            for (var frame = 0; frame < 60; frame++)
                await ctx.WaitFrameAsync();
            ctx.AssertEqual(OBJECT_INVALID, GetItemInSlot(InventorySlot.RightRing, player), "the ring is unequipped");
            ctx.AssertEqual(attack, Record(player).Attack, "the ring's Attack is subtracted exactly once");
        }

        [EngineTest("Moving a worn weapon to the other hand is refused and adds nothing", Category = Category, TimeoutSeconds = 60f)]
        public static async Task MovedWeaponCountsOnce(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            ctx.SuppressNPCNaturalRegen(player);
            foreach (var hand in new[] { InventorySlot.RightHand, InventorySlot.LeftHand })
            {
                var held = GetItemInSlot(hand, player);
                if (GetIsObjectValid(held)) await UnequipAsync(ctx, player, held);
            }
            var blade = await CreateItemAsync(ctx, player, "b_longsword", AttackRingProperties);
            var attack = Record(player).Attack;
            await EquipAsync(ctx, player, blade, InventorySlot.RightHand);
            ctx.AssertEqual(attack + 20, Record(player).Attack, "the blade in the right hand counts once");

            // Equipping a worn item into another slot is refused, as moving a ring between ring slots always was.
            await ctx.ExecuteInCreatureContextAsync(player, () => ActionEquipItem(blade, InventorySlot.LeftHand));
            for (var frame = 0; frame < 60; frame++)
                await ctx.WaitFrameAsync();
            ctx.AssertEqual(blade, GetItemInSlot(InventorySlot.RightHand, player), "the blade stays in the right hand");
            ctx.AssertEqual(OBJECT_INVALID, GetItemInSlot(InventorySlot.LeftHand, player), "the left hand stays empty");
            ctx.AssertEqual(attack + 20, Record(player).Attack, "the refused move adds nothing");

            await UnequipAsync(ctx, player, blade);
            ctx.AssertEqual(attack, Record(player).Attack, "unequipping returns Attack to its base");
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
