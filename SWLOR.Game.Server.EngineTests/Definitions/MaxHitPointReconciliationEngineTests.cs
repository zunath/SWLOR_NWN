using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// A player's stored maximum HP is kept by adding and removing each equipment and food change. A record that
    /// drifted below the native floor of one HP per level hid gear HP: 18 Vitality, 200 HP naked, a 125 HP piece
    /// left 200, and a further 120 HP piece showed 315 instead of 445.
    /// </summary>
    public static class MaxHitPointReconciliationEngineTests
    {
        private const string Category = "MaxHitPointReconciliation";
        private const int ReportedVitality = 18;
        private const int DriftedRecordMaxHP = -90;
        private const int BeltHitPoints = 125;
        private const int RingHitPoints = 120;

        [EngineTest("A drifted max HP record no longer hides the first gear HP", Category = Category, TimeoutSeconds = 60f)]
        public static async Task DriftedRecordIsRepairedByHitPointGear(EngineTestContext ctx)
        {
            using var fixture = await PreparePlayer(ctx);
            var player = fixture.Creature;
            await SetRecordAsync(ctx, player, DriftedRecordMaxHP);
            ctx.AssertEqual(200, GetMaxHitPoints(player), "the reported naked maximum: 40 levels at the one HP floor, plus 4 per level");

            var belt = await CreateHitPointItemAsync(ctx, player, "advent_belt", BeltHitPoints);
            var ring = await CreateHitPointItemAsync(ctx, player, "absdef_l6", RingHitPoints);

            await EquipAsync(ctx, player, belt, InventorySlot.Belt);
            ctx.AssertEqual(Stat.BaseHP + BeltHitPoints, Record(player).MaxHP, "the record is rebuilt from its sources");
            ctx.AssertEqual(355, GetMaxHitPoints(player), "the 125 HP belt adds all of its HP to the 230 HP naked maximum");

            await EquipAsync(ctx, player, ring, InventorySlot.RightRing);
            ctx.AssertEqual(475, GetMaxHitPoints(player), "the 120 HP ring adds all of its HP");

            await UnequipAsync(ctx, player, ring);
            await UnequipAsync(ctx, player, belt);
            ctx.AssertEqual(Stat.BaseHP, Record(player).MaxHP, "unequipping returns the record to the base");
            ctx.AssertEqual(230, GetMaxHitPoints(player), "the naked maximum includes the 70 HP base");
        }

        [EngineTest("Login rebuilds a drifted max HP record from worn gear and food", Category = Category, TimeoutSeconds = 60f)]
        public static async Task LoginRepairsDriftedRecord(EngineTestContext ctx)
        {
            using var fixture = await PreparePlayer(ctx);
            var player = fixture.Creature;
            await SetRecordAsync(ctx, player, Stat.BaseHP);
            var belt = await CreateHitPointItemAsync(ctx, player, "advent_belt", BeltHitPoints);
            var ring = await CreateHitPointItemAsync(ctx, player, "absdef_l6", RingHitPoints);
            await EquipAsync(ctx, player, belt, InventorySlot.Belt);
            await EquipAsync(ctx, player, ring, InventorySlot.RightRing);

            // The record drifts with the gear worn; food HP is a source the record keeps.
            const int foodHitPoints = 30;
            fixture.Update(record => record.TemporaryFoodHP = foodHitPoints);
            await SetRecordAsync(ctx, player, DriftedRecordMaxHP);
            ctx.AssertEqual(315, GetMaxHitPoints(player), "the drifted record hides 130 of the 245 gear HP");

            await ctx.ExecuteInCreatureContextAsync(player, () => Stat.RefreshPlayerMaxHP(player));
            await ctx.WaitFrameAsync();
            ctx.AssertEqual(Stat.BaseHP + BeltHitPoints + RingHitPoints + foodHitPoints, Record(player).MaxHP, "the record is rebuilt at login");
            ctx.AssertEqual(505, GetMaxHitPoints(player), "every source counts in full");
        }

        private static Player Record(uint player) => DB.Get<Player>(GetObjectUUID(player));

        private static async Task SetRecordAsync(EngineTestContext ctx, uint player, int maxHP)
        {
            var record = Record(player);
            record.MaxHP = maxHP;
            await ctx.ExecuteInCreatureContextAsync(player, () => Stat.AdjustPlayerMaxHP(record, player, 0));
            DB.Set(record);
            ObjectPlugin.SetCurrentHitPoints(player, GetMaxHitPoints(player));
            await ctx.WaitFrameAsync();
        }

        private static async Task<uint> CreateHitPointItemAsync(EngineTestContext ctx, uint player, string resref, int hitPoints)
        {
            var item = OBJECT_INVALID;
            await ctx.ExecuteInCreatureContextAsync(player, () => item = CreateItemOnObject(resref, player));
            await ctx.WaitFrameAsync();
            ctx.Assert(GetIsObjectValid(item), $"create a real {resref}");
            ctx.Track(item);
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetFirstItemProperty(item))
                RemoveItemProperty(item, property);
            AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.HPBonus, -1, hitPoints), item);
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

        private static async Task<PlayerAbilityFixture> PreparePlayer(EngineTestContext ctx)
        {
            var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            try
            {
                var player = fixture.Creature;
                SetPlotFlag(player, false);
                CreaturePlugin.SetClassByPosition(player, 0, SWLOR.NWN.API.NWScript.Enum.ClassType.Standard);
                await ctx.ExecuteInCreatureContextAsync(player, () =>
                    typeof(PlayerInitialization).GetMethod("EnsureNativeLevels", BindingFlags.Static | BindingFlags.NonPublic)
                        .Invoke(null, new object[] { player }));
                ctx.AssertEqual(40, GetHitDice(player), "fixture has normal SWLOR native levels");
                // Racial and other bonuses ride on the raw score; set it so the effective score is the reported one.
                CreaturePlugin.SetRawAbilityScore(player, AbilityType.Vitality, 10);
                var bonus = GetAbilityScore(player, AbilityType.Vitality) - 10;
                CreaturePlugin.SetRawAbilityScore(player, AbilityType.Vitality, ReportedVitality - bonus);
                ctx.AssertEqual(ReportedVitality, GetAbilityScore(player, AbilityType.Vitality), "fixture has the reported Vitality");
                fixture.Update(record => record.TemporaryFoodHP = 0);
                ctx.SuppressNPCNaturalRegen(player);
                return fixture;
            }
            catch
            {
                fixture.Dispose();
                throw;
            }
        }
    }
}
