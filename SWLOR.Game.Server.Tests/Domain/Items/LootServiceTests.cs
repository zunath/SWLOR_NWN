using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.LootService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Area;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Domain.Items;

/// <summary>
/// Unit tests of the loot instance. The engine is a recording fake with real local variables, the loot tables are the
/// small ones of <see cref="SampleDefinition"/>, and the random draws are seeded: each test that depends on a draw finds a
/// seed whose draws (replayed with <see cref="System.Random"/> in the order the service makes them) give the outcome it
/// wants, so the order and the number of draws are part of what the tests pin.
/// </summary>
[TestFixture]
[NonParallelizable]
public class LootServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Source = 101;
    private const uint Receiver = 102;
    private const uint Area = 103;
    private const uint Container = 104;
    private const uint Looter = 105;
    private const uint Target = 106;
    private const uint Attacker = 107;
    private const uint Skin = 108;
    private const uint FirstCreatedItem = 5000;

    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private TestLootService _service = null!;
    private IDisposable _engineScope = null!;

    private readonly HashSet<uint> _valid = new();
    private uint _nextItem;

    [SetUp]
    public void SetUp()
    {
        _valid.Clear();
        _valid.UnionWith(new[] { Source, Receiver, Area, Container, Looter, Target, Attacker });
        _nextItem = FirstCreatedItem;
        SampleDefinition.SpawnedItems.Clear();

        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _service = new TestLootService { Definitions = new[] { typeof(SampleDefinition) } };

        _engine.SetReturnHandler("GetIsObjectValid", arguments => _valid.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("CreateItemOnObject", _ => NewObject());
        _engine.SetReturnHandler("CopyItem", _ => NewObject());

        _engineScope = NWScriptRuntime.Override(_engine);
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        _engineScope.Dispose();
    }

    // ---- helpers

    private uint NewObject()
    {
        var id = _nextItem++;
        _valid.Add(id);
        return id;
    }

    private void Register()
    {
        _service.RegisterLootTables();
    }

    /// <summary>The seed whose draws, replayed with <see cref="System.Random"/>, satisfy <paramref name="rolls"/>.</summary>
    private static int SeedWhere(Func<System.Random, bool> rolls)
    {
        for (var seed = 1; seed < 50000; seed++)
        {
            if (rolls(new System.Random(seed)))
                return seed;
        }

        throw new InvalidOperationException("No seed gives the wanted rolls.");
    }

    /// <summary>The next draw of the shared generator, which a test compares with the replay to prove how many draws were made.</summary>
    private static int NextDrawOf(System.Random replay) => replay.Next();

    private List<(string Resref, uint Target, int Quantity)> CreatedItems() =>
        _engine.CallsTo("CreateItemOnObject")
            .Select(call => ((string)call.Arguments[0]!, (uint)call.Arguments[1]!, (int)call.Arguments[2]!))
            .ToList();

    private static IReadOnlyList<Type> ServerDefinitionTypes() =>
        typeof(ILootTableDefinition).Assembly.GetTypes()
            .Where(type => typeof(ILootTableDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .ToArray();

    /// <summary>The registry with the search for loot table definitions and the calls into services that need a server answered by the test.</summary>
    private sealed class TestLootService : LootService
    {
        public IEnumerable<Type>? Definitions { get; set; }
        public Dictionary<(uint Creature, PerkType Perk), int> PerkLevels { get; } = new();
        public Dictionary<(uint Creature, StatType Stat), int> StatAdjustments { get; } = new();
        public Dictionary<BeastType, BeastDetail> Beasts { get; } = new();

        public IEnumerable<Type> DefaultDefinitions() => base.GetLootTableDefinitionTypes();

        protected override IEnumerable<Type> GetLootTableDefinitionTypes() => Definitions ?? base.GetLootTableDefinitionTypes();

        protected override int GetPerkLevel(uint creature, PerkType perkType) =>
            PerkLevels.GetValueOrDefault((creature, perkType));

        protected override int GetStatAdjustment(uint creature, StatType stat) =>
            StatAdjustments.GetValueOrDefault((creature, stat));

        protected override BeastDetail GetBeastDetail(BeastType type) => Beasts[type];
    }

    /// <summary>Small tables that cover a single item, gold, a mix of common and rare items, rare tables and a spawn action.</summary>
    public sealed class SampleDefinition : ILootTableDefinition
    {
        public static List<uint> SpawnedItems { get; } = new();

        public Dictionary<string, LootTable> BuildLootTables()
        {
            return new LootTableBuilder()
                .Create("SINGLE")
                .AddItem("item_single", 1, 5)
                .Create("GOLD")
                .AddGold(100, 1)
                .Create("MIXED")
                .AddItem("item_common", 10)
                .AddItem("item_rare", 1, 1, true)
                .Create("RARE_TABLE")
                .IsRare()
                .AddItem("item_rare_only", 1, 1, true)
                .Create("RARE_SCALED")
                .IsRare()
                .MaxRareChanceBonus(10)
                .AddItem("item_rare_scaled", 1, 1, true)
                .Create("ACTION")
                .AddItem("item_action", 1)
                .AddSpawnAction(item => SpawnedItems.Add(item))
                .Build();
        }
    }

    /// <summary>A table named like the variable that the steal loot handler reads, which is the name it looks up.</summary>
    public sealed class StealDefinition : ILootTableDefinition
    {
        public Dictionary<string, LootTable> BuildLootTables()
        {
            return new LootTableBuilder()
                .Create("STEAL_LOOT_TABLE_1")
                .AddItem("item_steal", 1)
                .Build();
        }
    }

    /// <summary>A definition that repeats the key "SINGLE" and also builds a table with a blank key.</summary>
    public sealed class ConflictingDefinition : ILootTableDefinition
    {
        public Dictionary<string, LootTable> BuildLootTables()
        {
            var duplicate = new LootTable();
            duplicate.Add(new LootTableItem("item_duplicate", 1, 1, false));
            var blank = new LootTable();
            blank.Add(new LootTableItem("item_blank", 1, 1, false));
            var unique = new LootTable();
            unique.Add(new LootTableItem("item_unique", 1, 1, false));

            return new Dictionary<string, LootTable>
            {
                ["SINGLE"] = duplicate,
                [" "] = blank,
                ["UNIQUE"] = unique
            };
        }
    }

    // ---- loot tables

    [Test]
    public void Tables_do_not_exist_before_they_are_registered()
    {
        _service.LootTableExists("SINGLE").Should().BeFalse();
    }

    [Test]
    public void RegisterLootTables_registers_every_table_of_the_definition()
    {
        Register();

        foreach (var name in new[] { "SINGLE", "GOLD", "MIXED", "RARE_TABLE", "RARE_SCALED", "ACTION" })
            _service.LootTableExists(name).Should().BeTrue(name);
        _service.GetLootTableByName("SINGLE").Single().Resref.Should().Be("item_single");
        _service.GetLootTableByName("RARE_SCALED").MaxRareChanceBonus.Should().Be(10);
    }

    [Test]
    public void RegisterLootTables_skips_blank_keys_and_keeps_the_first_table_of_a_repeated_key()
    {
        _service.Definitions = new[] { typeof(SampleDefinition), typeof(ConflictingDefinition) };

        Register();

        _service.GetLootTableByName("SINGLE").Single().Resref.Should().Be("item_single");
        _service.GetLootTableByName("UNIQUE").Single().Resref.Should().Be("item_unique");
        _service.LootTableExists(" ").Should().BeFalse();
    }

    [Test]
    public void RegisterLootTables_keeps_tables_across_a_second_registration_without_replacing_them()
    {
        Register();
        var first = _service.GetLootTableByName("GOLD");

        Register();

        _service.GetLootTableByName("GOLD").Should().BeSameAs(first);
    }

    [Test]
    public void GetLootTableByName_throws_for_an_unregistered_name()
    {
        Register();

        var read = () => _service.GetLootTableByName("MISSING");

        read.Should().Throw<Exception>().WithMessage("Loot table 'MISSING' is not registered. Did you enter the right name?");
    }

    [Test]
    public void The_default_search_finds_every_loot_table_definition_of_the_server()
    {
        var found = _service.DefaultDefinitions().ToList();

        found.Should().Contain(ServerDefinitionTypes());
        found.Should().NotContain(type => type.IsAbstract || type.IsInterface);
    }

    [Test]
    public void Every_table_of_every_server_definition_is_registered()
    {
        _service.Definitions = ServerDefinitionTypes();
        var keys = ServerDefinitionTypes()
            .SelectMany(type => ((ILootTableDefinition)Activator.CreateInstance(type)!).BuildLootTables().Keys)
            .Distinct()
            .ToList();

        Register();

        keys.Should().NotBeEmpty();
        keys.Where(key => !_service.LootTableExists(key)).Should().BeEmpty();
    }

    // ---- spawning loot

    [Test]
    public void SpawnLoot_creates_the_drawn_item_on_the_receiver_with_the_rolled_quantity_and_makes_exactly_three_draws()
    {
        Register();
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "SINGLE");
        Random.SetSeed(11);

        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");

        var replay = new System.Random(11);
        replay.Next(1, 101);
        replay.NextDouble();
        var quantity = replay.Next(5) + 1;
        CreatedItems().Should().Equal(("item_single", Receiver, quantity));
        Random.Next().Should().Be(NextDrawOf(replay));
    }

    [Test]
    public void SpawnLoot_without_a_chance_or_attempts_drops_once_for_certain()
    {
        Register();
        _engine.SetLocalString(Source, "LOOT_TABLE_1", " SINGLE ");

        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");

        CreatedItems().Should().ContainSingle().Which.Resref.Should().Be("item_single");
    }

    [Test]
    public void SpawnLoot_tries_as_many_times_as_the_attempts_say_and_draws_in_order_each_time()
    {
        Register();
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "SINGLE, 100, 3");
        Random.SetSeed(21);

        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");

        var replay = new System.Random(21);
        var expected = new List<(string, uint, int)>();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            replay.Next(1, 101);
            replay.NextDouble();
            expected.Add(("item_single", Receiver, replay.Next(5) + 1));
        }

        CreatedItems().Should().Equal(expected);
        Random.Next().Should().Be(NextDrawOf(replay));
    }

    [Test]
    public void SpawnLoot_with_a_chance_of_zero_only_rolls_the_chance_for_each_attempt()
    {
        Register();
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "SINGLE, 0, 2");
        Random.SetSeed(31);

        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");

        var replay = new System.Random(31);
        replay.Next(1, 101);
        replay.Next(1, 101);
        CreatedItems().Should().BeEmpty();
        Random.Next().Should().Be(NextDrawOf(replay));
    }

    [Test]
    public void SpawnLoot_skips_an_attempt_when_the_chance_roll_is_above_the_chance()
    {
        Register();
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "SINGLE, 40");
        var seed = SeedWhere(random => random.Next(1, 101) == 41);
        Random.SetSeed(seed);

        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");

        CreatedItems().Should().BeEmpty();
    }

    [Test]
    public void SpawnLoot_drops_when_the_chance_roll_equals_the_chance()
    {
        Register();
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "SINGLE, 40");
        var seed = SeedWhere(random => random.Next(1, 101) == 40);
        Random.SetSeed(seed);

        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");

        CreatedItems().Should().ContainSingle();
    }

    [TestCase("SINGLE, 500")]
    [TestCase("SINGLE,100,0")]
    [TestCase("SINGLE,100,-4")]
    [TestCase("SINGLE,100,not-a-number")]
    public void SpawnLoot_limits_a_chance_to_100_and_the_attempts_to_at_least_one(string entry)
    {
        Register();
        _engine.SetLocalString(Source, "LOOT_TABLE_1", entry);

        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");

        CreatedItems().Should().ContainSingle();
    }

    [Test]
    public void SpawnLoot_reads_an_unreadable_chance_as_zero_so_nothing_drops()
    {
        // An integer that cannot be parsed becomes 0, not the default of 100.
        Register();
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "SINGLE, often, 3");

        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");

        CreatedItems().Should().BeEmpty();
    }

    [Test]
    public void SpawnLoot_goes_through_the_numbered_tables_until_the_first_blank_one()
    {
        Register();
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "SINGLE");
        _engine.SetLocalString(Source, "LOOT_TABLE_2", "GOLD");
        _engine.SetLocalString(Source, "LOOT_TABLE_4", "ACTION");
        _engine.SetLocalString(Source, "OTHER_TABLE_1", "ACTION");

        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");

        CreatedItems().Select(item => item.Resref).Should().Equal("item_single", "nw_it_gold001");
    }

    [Test]
    public void SpawnLoot_with_no_tables_creates_nothing_and_draws_nothing()
    {
        Register();
        Random.SetSeed(41);

        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");

        CreatedItems().Should().BeEmpty();
        Random.Next().Should().Be(new System.Random(41).Next());
    }

    [Test]
    public void SpawnLoot_throws_for_a_table_that_is_not_registered()
    {
        Register();
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "MISSING");

        var spawn = () => _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");

        spawn.Should().Throw<Exception>().WithMessage("Loot table 'MISSING' is not registered*");
    }

    [Test]
    public void SpawnLoot_runs_the_spawn_action_of_the_item_on_the_item_created()
    {
        Register();
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "ACTION");

        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");

        SampleDefinition.SpawnedItems.Should().Equal(FirstCreatedItem);
    }

    [Test]
    public void SpawnLoot_raises_the_quantity_of_gold_by_20_percent_for_each_creditfinder_level()
    {
        Register();
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "GOLD");
        _engine.SetLocalInt(Source, "CREDITFINDER_LEVEL", 5);
        var seed = SeedWhere(random =>
        {
            random.Next(1, 101);
            random.NextDouble();
            return random.Next(100) == 9;
        });
        Random.SetSeed(seed);

        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");

        // A rolled quantity of 10 plus 100% of it.
        CreatedItems().Should().Equal(("nw_it_gold001", Receiver, 20));
    }

    [Test]
    public void SpawnLoot_leaves_the_quantity_of_other_items_alone_whatever_the_creditfinder_level()
    {
        Register();
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "SINGLE");
        _engine.SetLocalInt(Source, "CREDITFINDER_LEVEL", 5);
        var seed = SeedWhere(random =>
        {
            random.Next(1, 101);
            random.NextDouble();
            return random.Next(5) == 3;
        });
        Random.SetSeed(seed);

        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");

        CreatedItems().Should().Equal(("item_single", Receiver, 4));
    }

    [Test]
    public void SpawnLoot_adds_the_rare_find_bonus_to_the_weight_of_rare_items_only()
    {
        Register();
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "MIXED");
        // Common weight 10, rare weight 1: a draw in the middle of the range picks the common item, and the rare one
        // once the bonus of 30 makes its weight 31.
        var seed = SeedWhere(random =>
        {
            random.Next(1, 101);
            var weighted = random.NextDouble();
            return weighted is > 0.45 and < 0.55;
        });

        Random.SetSeed(seed);
        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");
        _engine.SetLocalInt(Source, "RARE_BONUS_CHANCE", 30);
        Random.SetSeed(seed);
        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");

        CreatedItems().Select(item => item.Resref).Should().Equal("item_common", "item_rare");
    }

    [Test]
    public void SpawnLoot_adds_the_unscaled_rare_find_bonus_to_the_chance_of_a_rare_table()
    {
        Register();
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "RARE_TABLE, 40");
        var seed = SeedWhere(random => random.Next(1, 101) == 60);

        Random.SetSeed(seed);
        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");
        CreatedItems().Should().BeEmpty("a roll of 60 misses a chance of 40");

        _engine.SetLocalInt(Source, "RARE_BONUS_CHANCE", 30);
        Random.SetSeed(seed);
        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");
        CreatedItems().Should().ContainSingle("the bonus of 30 raises the chance to 70");
    }

    [Test]
    public void SpawnLoot_scales_the_rare_find_bonus_by_the_maximum_bonus_of_the_table()
    {
        Register();
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "RARE_SCALED, 40");
        var seed = SeedWhere(random => random.Next(1, 101) == 45);

        // 12 * 10 / 30 = 4 points: the chance is 44 and a roll of 45 misses.
        _engine.SetLocalInt(Source, "RARE_BONUS_CHANCE", 12);
        Random.SetSeed(seed);
        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");
        CreatedItems().Should().BeEmpty();

        // A full bonus of 30 adds the 10 points of the table: the chance is 50.
        _engine.SetLocalInt(Source, "RARE_BONUS_CHANCE", 30);
        Random.SetSeed(seed);
        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");
        CreatedItems().Should().ContainSingle();
    }

    [Test]
    public void SpawnLoot_does_not_raise_the_chance_of_a_table_that_is_not_rare()
    {
        Register();
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "SINGLE, 40");
        _engine.SetLocalInt(Source, "RARE_BONUS_CHANCE", 30);
        Random.SetSeed(SeedWhere(random => random.Next(1, 101) == 60));

        _service.SpawnLoot(Source, Receiver, "LOOT_TABLE_");

        CreatedItems().Should().BeEmpty();
    }

    [Test]
    public void SpawnLootOnCreatureDeath_spawns_the_loot_tables_of_the_dying_creature_onto_itself()
    {
        Register();
        _engine.SetReturn("OBJECT_SELF", Source);
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "SINGLE");
        _engine.SetLocalString(Source, "STEAL_LOOT_TABLE_1", "GOLD");

        _service.SpawnLootOnCreatureDeath();

        var created = CreatedItems();
        created.Should().HaveCount(1);
        created[0].Resref.Should().Be("item_single");
        created[0].Target.Should().Be(Source);
    }

    [Test]
    public void SpawnStealLoot_creates_the_items_on_the_creature_and_marks_them_stealable_and_undroppable()
    {
        // Recorded defect: the handler passes the NAME of the variable ("STEAL_LOOT_TABLE_1"), not the table name stored in
        // it, to the table lookup (SpawnLoot(source, receiver, prefix) reads the value). A table named like the variable is
        // what it finds, so this test registers one. No creature in the module uses these variables.
        _service.Definitions = new[] { typeof(SampleDefinition), typeof(StealDefinition) };
        Register();
        _engine.SetReturn("OBJECT_SELF", Source);
        _engine.SetLocalString(Source, "STEAL_LOOT_TABLE_1", "SINGLE");
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "GOLD");

        _service.SpawnStealLoot();

        var created = CreatedItems();
        created.Should().HaveCount(1);
        created[0].Resref.Should().Be("item_steal");
        created[0].Target.Should().Be(Source);
        var item = FirstCreatedItem;
        _engine.CallsTo("SetItemCursedFlag").Should().ContainSingle(call => (uint)call.Arguments[0]! == item && (bool)call.Arguments[1]!);
        _engine.CallsTo("SetDroppableFlag").Should().ContainSingle(call => (uint)call.Arguments[0]! == item && !(bool)call.Arguments[1]!);
        _locals.Int(item, "STEAL_ITEM").Should().Be(1);
    }

    [Test]
    public void SpawnStealLoot_looks_up_the_variable_name_as_the_table_name()
    {
        // Recorded defect (see above): the table named in the variable is never looked up, so a creature set up as the
        // variable's documentation says makes the handler throw.
        Register();
        _engine.SetReturn("OBJECT_SELF", Source);
        _engine.SetLocalString(Source, "STEAL_LOOT_TABLE_1", "ACTION");
        _engine.SetLocalString(Source, "STEAL_LOOT_TABLE_2", "ACTION, 100, 2");

        var spawn = () => _service.SpawnStealLoot();

        spawn.Should().Throw<Exception>().WithMessage("Loot table 'STEAL_LOOT_TABLE_1' is not registered*");
        CreatedItems().Should().BeEmpty();
    }

    [Test]
    public void SpawnStealLoot_does_nothing_for_a_creature_without_steal_tables()
    {
        Register();
        _engine.SetReturn("OBJECT_SELF", Source);
        _engine.SetLocalString(Source, "LOOT_TABLE_1", "SINGLE");

        _service.SpawnStealLoot();

        CreatedItems().Should().BeEmpty();
        _engine.CallsTo("SetItemCursedFlag").Should().BeEmpty();
    }

    // ---- creature marks

    [Test]
    public void RecordWeaponHitFindBonuses_marks_the_target_with_the_perk_levels_of_the_attacker()
    {
        _service.PerkLevels[(Attacker, PerkType.CreditFinder)] = 2;
        _service.PerkLevels[(Attacker, PerkType.TreasureHunter)] = 3;

        _service.RecordWeaponHitFindBonuses(Attacker, Target);

        _locals.Int(Target, "CREDITFINDER_LEVEL").Should().Be(2);
        _locals.Int(Target, "RARE_BONUS_CHANCE").Should().Be(30);
    }

    [Test]
    public void RecordWeaponHitFindBonuses_takes_the_stat_bonus_when_it_beats_treasure_hunter()
    {
        _service.PerkLevels[(Attacker, PerkType.TreasureHunter)] = 1;
        _service.StatAdjustments[(Attacker, StatType.RareItemFindChance)] = 25;

        _service.RecordWeaponHitFindBonuses(Attacker, Target);

        _locals.Int(Target, "RARE_BONUS_CHANCE").Should().Be(25);
        _locals.Has(Target, "CREDITFINDER_LEVEL").Should().BeFalse("a level of 0 is not higher than the 0 already marked");
    }

    [Test]
    public void RecordWeaponHitFindBonuses_never_lowers_a_mark_the_target_already_has()
    {
        _engine.SetLocalInt(Target, "CREDITFINDER_LEVEL", 4);
        _engine.SetLocalInt(Target, "RARE_BONUS_CHANCE", 30);
        _service.PerkLevels[(Attacker, PerkType.CreditFinder)] = 4;
        _service.PerkLevels[(Attacker, PerkType.TreasureHunter)] = 2;
        _locals.ClearCalls();

        _service.RecordWeaponHitFindBonuses(Attacker, Target);

        _locals.Int(Target, "CREDITFINDER_LEVEL").Should().Be(4);
        _locals.Int(Target, "RARE_BONUS_CHANCE").Should().Be(30);
        _engine.CallsTo("SetLocalInt").Should().BeEmpty();
    }

    [Test]
    public void RecordWeaponHitFindBonuses_raises_a_lower_mark()
    {
        _engine.SetLocalInt(Target, "CREDITFINDER_LEVEL", 1);
        _engine.SetLocalInt(Target, "RARE_BONUS_CHANCE", 10);
        _service.PerkLevels[(Attacker, PerkType.CreditFinder)] = 3;
        _service.PerkLevels[(Attacker, PerkType.TreasureHunter)] = 2;

        _service.RecordWeaponHitFindBonuses(Attacker, Target);

        _locals.Int(Target, "CREDITFINDER_LEVEL").Should().Be(3);
        _locals.Int(Target, "RARE_BONUS_CHANCE").Should().Be(20);
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void RecordWeaponHitFindBonuses_never_marks_a_player_or_a_dm(bool isPlayer, bool isDm)
    {
        _engine.SetReturnHandler("GetIsPC", arguments => isPlayer && (uint)arguments[0]! == Target);
        _engine.SetReturnHandler("GetIsDM", arguments => isDm && (uint)arguments[0]! == Target);
        _service.PerkLevels[(Attacker, PerkType.CreditFinder)] = 3;
        _service.PerkLevels[(Attacker, PerkType.TreasureHunter)] = 3;

        _service.RecordWeaponHitFindBonuses(Attacker, Target);

        _engine.CallsTo("SetLocalInt").Should().BeEmpty();
    }

    [Test]
    public void MarkCreditfinderAndTreasureHunterOnTarget_marks_the_spell_target_for_the_creature_that_hit()
    {
        _engine.SetReturn("OBJECT_SELF", Attacker);
        _engine.SetReturn("GetSpellTargetObject", Target);
        _service.PerkLevels[(Attacker, PerkType.CreditFinder)] = 1;
        _service.PerkLevels[(Attacker, PerkType.TreasureHunter)] = 1;

        _service.MarkCreditfinderAndTreasureHunterOnTarget();

        _locals.Int(Target, "CREDITFINDER_LEVEL").Should().Be(1);
        _locals.Int(Target, "RARE_BONUS_CHANCE").Should().Be(10);
    }

    // ---- corpses

    private const uint Body = 201;
    private const uint HeadItem = 211;
    private const uint ChestItem = 212;
    private const uint HandItem = 213;
    private const uint CursedItem = 214;
    private const uint UndroppableItem = 215;
    private const uint CreatureWeapon = 216;
    private const uint CarriedItem = 221;
    private const uint CarriedCursedItem = 222;
    private const uint CarriedUndroppableItem = 223;

    private readonly Dictionary<InventorySlot, uint> _equipped = new();
    private readonly List<uint> _carried = new();
    private readonly HashSet<uint> _cursed = new();
    private readonly HashSet<uint> _undroppable = new();
    private int _carriedCursor;

    /// <summary>A body standing in an area of 300 by 300 meters with the given gear, ready for <c>ProcessCorpse</c>.</summary>
    private void ArrangeBody()
    {
        _valid.UnionWith(new[]
        {
            Body, HeadItem, ChestItem, HandItem, CursedItem, UndroppableItem, CreatureWeapon,
            CarriedItem, CarriedCursedItem, CarriedUndroppableItem, Skin
        });
        _equipped.Clear();
        _carried.Clear();
        _cursed.Clear();
        _undroppable.Clear();
        _cursed.UnionWith(new[] { CursedItem, CarriedCursedItem });
        _undroppable.UnionWith(new[] { UndroppableItem, CarriedUndroppableItem });
        _equipped[InventorySlot.Head] = HeadItem;
        _equipped[InventorySlot.Chest] = ChestItem;
        _equipped[InventorySlot.RightHand] = HandItem;
        _equipped[InventorySlot.LeftHand] = CursedItem;
        _equipped[InventorySlot.Boots] = UndroppableItem;
        _equipped[InventorySlot.CreatureLeft] = CreatureWeapon;
        _equipped[InventorySlot.CreatureArmor] = Skin;
        _carried.AddRange(new[] { CarriedItem, CarriedCursedItem, CarriedUndroppableItem });

        _engine.SetReturn("OBJECT_SELF", Body);
        _engine.SetReturn("GetArea", Area);
        _engine.SetReturn("GetFacing", 90f);
        _engine.SetReturn("GetPosition", new Vector3(50f, 60f, 2f));
        _engine.SetReturn("GetName", "Rancor");
        _engine.SetReturn("GetGold", 75);
        _engine.SetReturnHandler("GetAreaSize", arguments => (Dimension)arguments[0]! == Dimension.Width ? 30 : 40);
        _engine.SetReturnHandler("GetItemInSlot", arguments => _equipped.GetValueOrDefault((InventorySlot)arguments[0]!, ObjectInvalid));
        _engine.SetReturnHandler("GetItemCursedFlag", arguments => _cursed.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetDroppableFlag", arguments => !_undroppable.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetFirstItemInInventory", arguments => (uint)arguments[0]! == Body ? CarriedAt(_carriedCursor = 0) : ObjectInvalid);
        _engine.SetReturnHandler("GetNextItemInInventory", arguments => (uint)arguments[0]! == Body ? CarriedAt(++_carriedCursor) : ObjectInvalid);
        _engine.SetReturnHandler("CreateObject", _ => Container);

        // The stat skin of the creature: one NPC level property, then the end of the properties.
        _engine.SetReturnSequence("GetIsItemPropertyValid", true, false);
        _engine.SetReturn("GetItemPropertyType", ItemPropertyType.NPCLevel);
        _engine.SetReturn("GetItemPropertyCostTableValue", 12);
    }

    private uint CarriedAt(int index) => index < _carried.Count ? _carried[index] : ObjectInvalid;

    [Test]
    public void ProcessCorpse_creates_the_corpse_where_the_body_lies_and_names_it_after_the_creature()
    {
        ArrangeBody();
        _engine.SetLocalInt(Body, "BEAST_TYPE", 7);

        _service.ProcessCorpse();

        _engine.CallsTo("SetIsDestroyable").Single().Arguments[0].Should().Be(false);
        var locations = _engine.CallsTo("Location").Select(call => (call.Arguments[1], call.Arguments[2])).ToList();
        locations.Should().Contain((new Vector3(50f, 60f, 2f - CorpsePlacement.SinkDepth), 90f));
        var create = _engine.CallsTo("CreateObject").Single();
        create.Arguments[0].Should().Be(ObjectType.Placeable);
        create.Arguments[1].Should().Be("corpse");
        _locals.Object(Container, "CORPSE_BODY").Should().Be(Body);
        _engine.CallsTo("SetName").Should().ContainSingle(call => (uint)call.Arguments[0]! == Container && (string)call.Arguments[1]! == "Rancor's Corpse");
    }

    [Test]
    public void ProcessCorpse_moves_a_body_past_the_area_edge_back_inside_it()
    {
        ArrangeBody();
        _engine.SetReturn("GetPosition", new Vector3(-5f, 60f, 2f));
        _engine.SetReturn("GetGroundHeight", -6f);

        _service.ProcessCorpse();

        var expected = CorpsePlacement.GetLootPosition(new Vector3(-5f, 60f, 2f), 300f, 400f, _ => -6f);
        expected.X.Should().Be(CorpsePlacement.EdgeMargin);
        _engine.CallsTo("Location").Select(call => call.Arguments[1]).Should().Contain(expected);
    }

    [Test]
    public void ProcessCorpse_copies_the_beast_type_and_the_level_of_the_stat_skin_onto_the_corpse()
    {
        ArrangeBody();
        _engine.SetLocalInt(Body, "BEAST_TYPE", 7);

        _service.ProcessCorpse();

        _locals.Int(Container, "BEAST_TYPE").Should().Be(7);
        _locals.Int(Container, "BEAST_LEVEL").Should().Be(12);
    }

    [Test]
    public void ProcessCorpse_gives_a_creature_without_a_stat_skin_level_zero()
    {
        ArrangeBody();
        _equipped.Remove(InventorySlot.CreatureArmor);

        _service.ProcessCorpse();

        _locals.Int(Container, "BEAST_LEVEL").Should().Be(0);
        _locals.Int(Container, "BEAST_TYPE").Should().Be(0);
    }

    [Test]
    public void ProcessCorpse_takes_the_gold_of_the_body_when_the_corpse_runs_the_command()
    {
        ArrangeBody();

        _service.ProcessCorpse();

        var command = _engine.CallsTo("AssignCommand").Single(call => (uint)call.Arguments[0]! == Container);
        _engine.CallsTo("TakeGoldFromCreature").Should().BeEmpty("the gold is taken when the command runs");
        ((Action)command.Arguments[1]!)();
        var take = _engine.CallsTo("TakeGoldFromCreature").Single();
        take.Arguments[0].Should().Be(75);
        take.Arguments[1].Should().Be(Body);
    }

    [Test]
    public void ProcessCorpse_copies_droppable_gear_and_destroys_the_originals_except_head_and_chest()
    {
        ArrangeBody();

        _service.ProcessCorpse();

        var copies = _engine.CallsTo("CopyItem").Select(call => ((uint)call.Arguments[0]!, (uint)call.Arguments[1]!, (bool)call.Arguments[2]!)).ToList();
        copies.Should().Equal(
            (HeadItem, Container, true),
            (ChestItem, Container, true),
            (HandItem, Container, true),
            (CarriedItem, Container, true));
        _engine.CallsTo("DestroyObject").Select(call => (uint)call.Arguments[0]!).Should().Equal(HandItem, CarriedItem);
    }

    [Test]
    public void ProcessCorpse_remembers_the_original_of_a_head_or_chest_copy_so_taking_it_can_update_the_creature()
    {
        ArrangeBody();

        _service.ProcessCorpse();

        var headCopy = FirstCreatedItem;
        var chestCopy = FirstCreatedItem + 1;
        _locals.Object(headCopy, "CORPSE_ITEM_COPY").Should().Be(HeadItem);
        _locals.Object(chestCopy, "CORPSE_ITEM_COPY").Should().Be(ChestItem);
        _locals.Has(FirstCreatedItem + 2, "CORPSE_ITEM_COPY").Should().BeFalse();
    }

    [Test]
    public void ProcessCorpse_leaves_cursed_undroppable_and_natural_weapon_items_on_the_body()
    {
        ArrangeBody();

        _service.ProcessCorpse();

        var touched = _engine.CallsTo("CopyItem").Select(call => (uint)call.Arguments[0]!)
            .Concat(_engine.CallsTo("DestroyObject").Select(call => (uint)call.Arguments[0]!)).ToList();
        touched.Should().NotContain(new[]
        {
            CursedItem, UndroppableItem, CreatureWeapon, Skin, CarriedCursedItem, CarriedUndroppableItem
        });
    }

    [Test]
    public void ProcessCorpse_removes_the_corpse_and_the_body_after_six_minutes()
    {
        ArrangeBody();
        _service.ProcessCorpse();

        var delayed = _engine.CallsTo("DelayCommand").Single();
        delayed.Arguments[0].Should().Be(360f);
        _locals.ClearCalls();
        _engine.SetReturnHandler("GetFirstItemInInventory", arguments => (uint)arguments[0]! == Container ? CarriedItem : ObjectInvalid);
        _engine.SetReturnSequence("GetNextItemInInventory", ObjectInvalid, ObjectInvalid);

        ((Action)delayed.Arguments[1]!)();

        var destroyed = _engine.CallsTo("DestroyObject").Select(call => (uint)call.Arguments[0]!).ToList();
        destroyed.Should().Equal(Body, CarriedItem, Container);
        _engine.CallsTo("AssignCommand").Should().ContainSingle(call => (uint)call.Arguments[0]! == Body);
    }

    [Test]
    public void ProcessCorpse_cleanup_does_nothing_when_the_corpse_is_already_gone()
    {
        ArrangeBody();
        _service.ProcessCorpse();
        var delayed = _engine.CallsTo("DelayCommand").Single();
        _locals.ClearCalls();
        _valid.Remove(Container);

        ((Action)delayed.Arguments[1]!)();

        _engine.Calls.Select(call => call.Member).Should().NotContain(new[] { "DestroyObject", "AssignCommand" });
    }

    [Test]
    public void CloseCorpseContainer_leaves_a_corpse_that_still_holds_items()
    {
        _engine.SetReturn("OBJECT_SELF", Container);
        _engine.SetReturn("GetFirstItemInInventory", CarriedItem);
        _valid.Add(CarriedItem);

        _service.CloseCorpseContainer();

        _engine.CallsTo("DestroyObject").Should().BeEmpty();
        _engine.CallsTo("CreateObject").Should().BeEmpty();
    }

    [Test]
    public void CloseCorpseContainer_removes_an_empty_corpse_and_lets_the_body_go()
    {
        _engine.SetReturn("OBJECT_SELF", Container);
        _engine.SetLocalObject(Container, "CORPSE_BODY", Body);

        _service.CloseCorpseContainer();

        _engine.CallsTo("DestroyObject").Single().Arguments[0].Should().Be(Container);
        var release = _engine.CallsTo("AssignCommand").Single();
        release.Arguments[0].Should().Be(Body);
        _locals.ClearCalls();
        ((Action)release.Arguments[1]!)();
        _engine.CallsTo("SetIsDestroyable").Should().ContainSingle();
        _engine.CallsTo("CreateObject").Should().BeEmpty();
    }

    [Test]
    public void CloseCorpseContainer_replaces_an_empty_beast_corpse_with_a_dna_extraction_corpse()
    {
        const uint extract = 301;
        _engine.SetReturn("OBJECT_SELF", Container);
        _engine.SetLocalObject(Container, "CORPSE_BODY", Body);
        _engine.SetLocalInt(Container, "BEAST_TYPE", (int)BeastType.Aradile);
        _engine.SetLocalInt(Container, "BEAST_LEVEL", 9);
        _engine.SetReturn("CreateObject", extract);
        _valid.Add(extract);
        _service.Beasts[BeastType.Aradile] = new BeastDetail { Name = "Aradile" };

        _service.CloseCorpseContainer();

        _engine.CallsTo("DestroyObject").Single().Arguments[0].Should().Be(Container);
        var create = _engine.CallsTo("CreateObject").Single();
        create.Arguments[0].Should().Be(ObjectType.Placeable);
        create.Arguments[1].Should().Be("extract_corpse");
        _locals.Object(extract, "CORPSE_BODY").Should().Be(Body);
        _locals.Int(extract, "BEAST_TYPE").Should().Be((int)BeastType.Aradile);
        _locals.Int(extract, "BEAST_LEVEL").Should().Be(9);
        _engine.CallsTo("SetName").Should().ContainSingle(call => (uint)call.Arguments[0]! == extract && (string)call.Arguments[1]! == "Extract DNA: Aradile");
        _engine.CallsTo("SetIsDestroyable").Should().BeEmpty("the body is kept for the extraction");

        var schedule = _engine.CallsTo("AssignCommand").Single(call => (uint)call.Arguments[0]! == extract);
        _engine.CallsTo("DelayCommand").Should().BeEmpty();
        ((Action)schedule.Arguments[1]!)();
        _engine.CallsTo("DelayCommand").Single().Arguments[0].Should().Be(360f);
    }

    [Test]
    public void DisturbCorpseContainer_returns_an_item_a_player_puts_in_and_tells_them()
    {
        const uint dropped = 401;
        _engine.SetReturn("GetLastDisturbed", Looter);
        _engine.SetReturn("GetInventoryDisturbItem", dropped);
        _engine.SetReturn("GetInventoryDisturbType", DisturbType.Added);
        _valid.Add(dropped);

        _service.DisturbCorpseContainer();

        _engine.CallsTo("CopyItem").Should().ContainSingle(call => (uint)call.Arguments[0]! == dropped && (uint)call.Arguments[1]! == Looter);
        _engine.CallsTo("DestroyObject").Should().ContainSingle(call => (uint)call.Arguments[0]! == dropped);
        _engine.CallsTo("SendMessageToPC").Should().ContainSingle(call =>
            (uint)call.Arguments[0]! == Looter && (string)call.Arguments[1]! == "You cannot place items inside of corpses.");
        var animation = _engine.CallsTo("AssignCommand").Single();
        animation.Arguments[0].Should().Be(Looter);
        _locals.ClearCalls();
        ((Action)animation.Arguments[1]!)();
        var play = _engine.CallsTo("ActionPlayAnimation").Single();
        play.Arguments.Should().Equal(Animation.LoopingGetLow, 1.0f, 1.0f);
    }

    [Test]
    public void DisturbCorpseContainer_forgets_the_copy_link_of_an_item_taken_out_and_destroys_the_linked_copy()
    {
        const uint taken = 402;
        const uint linked = HeadItem;
        _engine.SetReturn("GetLastDisturbed", Looter);
        _engine.SetReturn("GetInventoryDisturbItem", taken);
        _engine.SetReturn("GetInventoryDisturbType", DisturbType.Removed);
        _valid.UnionWith(new[] { taken, linked });
        _engine.SetLocalObject(taken, "CORPSE_ITEM_COPY", linked);

        _service.DisturbCorpseContainer();

        _engine.CallsTo("DestroyObject").Should().ContainSingle().Which.Arguments[0].Should().Be(linked);
        _engine.CallsTo("DeleteLocalObject").Should().ContainSingle(call => (uint)call.Arguments[0]! == taken && (string)call.Arguments[1]! == "CORPSE_ITEM_COPY");
        _engine.CallsTo("SendMessageToPC").Should().BeEmpty();
    }

    [Test]
    public void DisturbCorpseContainer_taking_out_an_item_without_a_copy_link_destroys_nothing()
    {
        const uint taken = 403;
        _engine.SetReturn("GetLastDisturbed", Looter);
        _engine.SetReturn("GetInventoryDisturbItem", taken);
        _engine.SetReturn("GetInventoryDisturbType", DisturbType.Removed);
        _valid.Add(taken);

        _service.DisturbCorpseContainer();

        _engine.CallsTo("DestroyObject").Should().BeEmpty();
        _engine.CallsTo("DeleteLocalObject").Should().ContainSingle();
    }

    [Test]
    public void DisturbCorpseContainer_ignores_other_disturbances_besides_the_animation()
    {
        _engine.SetReturn("GetLastDisturbed", Looter);
        _engine.SetReturn("GetInventoryDisturbItem", 404u);
        _engine.SetReturn("GetInventoryDisturbType", DisturbType.Stolen);

        _service.DisturbCorpseContainer();

        _engine.Calls.Select(call => call.Member).Should().NotContain(new[] { "CopyItem", "DestroyObject", "SendMessageToPC", "DeleteLocalObject" });
        _engine.CallsTo("AssignCommand").Should().ContainSingle();
    }
}
