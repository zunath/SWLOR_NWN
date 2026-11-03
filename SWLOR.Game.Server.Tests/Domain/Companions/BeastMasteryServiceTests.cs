using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>
/// Unit tests of the beast mastery instance. The engine and the events plugin are recording fakes with real local
/// variables, the database is an in-memory one that answers searches, the beast catalog is the small one of
/// <see cref="SampleBeastDefinition"/>, and the random rolls are seeded: each test that depends on a roll finds a seed
/// whose draws (replayed with <see cref="System.Random"/> in the order the service makes them) give the outcome it
/// wants, so the order and the number of draws are part of what the tests pin. The calls into services that are not
/// converted yet (perks, stats, the companion control, the windows) are answered by <see cref="TestBeastMastery"/>.
/// </summary>
[TestFixture]
[NonParallelizable]
public class BeastMasteryServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Player = 101;
    private const uint BeastObject = 102;
    private const uint Skin = 103;
    private const uint Claw = 104;
    private const uint Npc = 105;
    private const uint Area = 106;
    private const uint ItemObject = 107;
    private const uint Module = 108;
    private const uint Incubator = 109;
    private const uint Killer = 110;
    private const uint Damager = 111;
    private const uint FirstCreatedObject = 5000;
    private const string PlayerUuid = "player-uuid-1";
    private const string BeastId = "beast-1";

    private FakeNWScript _engine = null!;
    private FakeEventsPlugin _events = null!;
    private FakeCreaturePlugin _creature = null!;
    private FakeItemPlugin _itemPlugin = null!;
    private LocalVariables _locals = null!;
    private QueryableDBService _db = null!;
    private RecordingFieldNotes _fieldNotes = null!;
    private PropertyIdStub _propertyIds = null!;
    private TestBeastMastery _service = null!;
    private readonly List<IDisposable> _scopes = new();
    private readonly HashSet<uint> _valid = new();
    private readonly Dictionary<nint, (ItemPropertyType Type, int SubType, int Cost)> _properties = new();
    private nint _nextHandle;
    private uint _nextObject;

    [SetUp]
    public void SetUp()
    {
        _valid.Clear();
        _valid.UnionWith(new[] { Player, BeastObject, Skin, Claw, Npc, Area, ItemObject, Module, Incubator, Killer, Damager });
        _properties.Clear();
        _nextHandle = 1000;
        _nextObject = FirstCreatedObject;

        _engine = new FakeNWScript();
        _events = new FakeEventsPlugin();
        _creature = new FakeCreaturePlugin();
        _itemPlugin = new FakeItemPlugin();
        _locals = new LocalVariables(_engine);
        _db = new QueryableDBService();
        _fieldNotes = new RecordingFieldNotes();
        var property = DispatchProxy.Create<IPropertyService, PropertyIdStub>();
        _propertyIds = (PropertyIdStub)(object)property;
        _service = new TestBeastMastery(_db, _fieldNotes, property) { Definitions = new[] { typeof(SampleBeastDefinition) } };

        _engine.SetReturnHandler("GetIsObjectValid", arguments => _valid.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! == Player);
        _engine.SetReturnHandler("GetIsDM", _ => _isDM);
        _engine.SetReturnHandler("GetIsDMPossessed", _ => _isPossessed);
        _engine.SetReturnHandler("GetObjectUUID", arguments => (uint)arguments[0]! == Player ? PlayerUuid : string.Empty);
        _engine.SetReturnHandler("GetMaster", arguments => (uint)arguments[0]! == BeastObject ? Player : ObjectInvalid);
        _engine.SetReturnHandler("GetAssociate", arguments =>
            (AssociateType)arguments[0]! == AssociateType.Henchman && (uint)arguments[1]! == Player && _henchman != 0
                ? _henchman
                : ObjectInvalid);
        _engine.SetReturnHandler("GetItemInSlot", arguments =>
        {
            var slot = (InventorySlot)arguments[0]!;
            return slot switch
            {
                InventorySlot.CreatureArmor => Skin,
                InventorySlot.CreatureLeft => Claw,
                _ => ObjectInvalid
            };
        });
        _engine.SetReturnHandler("ItemPropertyCustom", arguments =>
        {
            var handle = _nextHandle++;
            _properties[handle] = ((ItemPropertyType)arguments[0]!, (int)arguments[1]!, (int)arguments[2]!);
            return EngineStructures.Create<ItemProperty>(handle);
        });
        _engine.SetReturnHandler("CreateObject", _ => NewObject());
        _engine.SetReturnHandler("CreateItemOnObject", _ => NewObject());
        _engine.SetReturnHandler("StringToObject", arguments => (string)arguments[0]! == "npc-hex" ? Npc : ObjectInvalid);
        _engine.SetReturnHandler("GetArea", _ => Area);
        _engine.SetReturnHandler("GetModule", _ => Module);
        _engine.SetReturnHandler("GetLocation", _ => EngineStructures.Create<Location>());
        _engine.SetReturnHandler("Get2DARowCount", _ => 3);
        _engine.SetReturnHandler("Get2DAString", arguments => (int)arguments[2]! switch
        {
            1 => "5",
            2 => "not a number",
            _ => "10"
        });
        _events.SetReturnHandler("GetEventData", arguments => (string)arguments[0]! == "NPC" ? "npc-hex" : "incubator1");
        _engine.SetReturn("OBJECT_SELF", Player);

        _henchman = 0;
        _isDM = false;
        _isPossessed = false;
        _scopes.Add(NWScriptRuntime.Override(_engine));
        _scopes.Add(EventsPluginRuntime.Override(_events));
        _scopes.Add(CreaturePluginRuntime.Override(_creature));
        _scopes.Add(ItemPluginRuntime.Override(_itemPlugin));

        _service.CacheData();
        _engine.ClearCalls();
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
    }

    private uint _henchman;
    private bool _isDM;
    private bool _isPossessed;

    // ---- helpers

    private uint NewObject()
    {
        var id = _nextObject++;
        _valid.Add(id);
        return id;
    }

    private IReadOnlyList<string> Messages() =>
        _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).ToList();

    private List<(ItemPropertyType Type, int SubType, int Cost)> Added(uint item) =>
        _engine.CallsTo("AddItemProperty")
            .Where(call => (uint)call.Arguments[2]! == item)
            .Select(call => _properties[((ItemProperty)call.Arguments[1]!).Handle])
            .ToList();

    /// <summary>Makes <see cref="Beast"/> a spawned beast of a type that belongs to the player and stores its entity.</summary>
    private Beast ActiveBeast(int level = 1, int xp = 0, BeastType type = BeastType.Aradile)
    {
        _service.SetBeastId(BeastObject, BeastId);
        _service.SetBeastType(BeastObject, type);
        _henchman = BeastObject;
        return StoredBeast(level, xp, type);
    }

    private Beast StoredBeast(int level = 1, int xp = 0, BeastType type = BeastType.Aradile, string id = BeastId)
    {
        var beast = new Beast
        {
            Id = id,
            Name = "Rex",
            OwnerPlayerId = PlayerUuid,
            Type = type,
            Level = level,
            XP = xp
        };
        _db.With(beast);
        return beast;
    }

    private Beast Saved() => _db.Store.Saved.OfType<Beast>().Last();

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

    private static IncubationJob Job(int mutationChance = 0, BeastType type = BeastType.Aradile)
    {
        return new IncubationJob
        {
            Id = "job-1",
            ParentPropertyId = "incubator1",
            PlayerId = PlayerUuid,
            BeastDNAType = type,
            MutationChance = mutationChance,
            AttackPurity = 400,
            AccuracyPurity = 300,
            EvasionPurity = 200,
            LearningPurity = 100,
            XPPenalty = 100
        };
    }

    /// <summary>Runs the actions the service handed to <c>AssignCommand</c>, and the ones those hand to <c>DelayCommand</c>.</summary>
    private void RunScheduledCommands()
    {
        foreach (var call in _engine.CallsTo("AssignCommand"))
            ((Action)call.Arguments[1]!).Invoke();
        foreach (var call in _engine.CallsTo("DelayCommand"))
            ((Action)call.Arguments[1]!).Invoke();
    }

    /// <summary>The beast service with the calls into services that are not converted yet answered by the test.</summary>
    private sealed class TestBeastMastery : BeastMasteryService
    {
        public TestBeastMastery(IDBService db, IIncubationFieldNoteService notes, IPropertyService property)
            : base(db, notes, property)
        {
        }

        public IEnumerable<Type>? Definitions { get; set; }
        public Dictionary<(uint Creature, PerkType Perk), int> PerkLevels { get; } = new();
        public Dictionary<(uint Creature, StatType Stat), int> StatAdjustments { get; } = new();
        public Dictionary<uint, NPCStats> NpcStats { get; } = new();
        public bool ConversationHandled { get; set; }
        public List<string> Calls { get; } = new();

        public IEnumerable<Type> DefaultDefinitions() => base.GetBeastListDefinitionTypes();

        protected override IEnumerable<Type> GetBeastListDefinitionTypes() => Definitions ?? base.GetBeastListDefinitionTypes();

        protected override int GetPerkLevel(uint creature, PerkType perkType) =>
            PerkLevels.GetValueOrDefault((creature, perkType));

        protected override int GetStatAdjustment(uint creature, StatType stat) =>
            StatAdjustments.GetValueOrDefault((creature, stat));

        protected override void PublishBeastGainXPRefresh(uint player) => Calls.Add($"Refresh {player}");

        protected override void SyncGrantedFeats(uint creature, PerkType perkType, int perkLevel, bool addByLevel) =>
            Calls.Add($"SyncGrantedFeats {creature} {perkType} {perkLevel} {addByLevel}");

        protected override void SetNPCMaxHitPoints(uint creature, int desiredMaxHitPoints) =>
            Calls.Add($"SetNPCMaxHitPoints {creature} {desiredMaxHitPoints}");

        protected override NPCStats GetNPCStats(uint npc) => NpcStats.GetValueOrDefault(npc) ?? new NPCStats();

        protected override void ClearCompanionControl(uint companion) => Calls.Add($"Clear {companion}");

        protected override void InitializeCompanionControl(uint companion) => Calls.Add($"Initialize {companion}");

        protected override void ProcessCompanionCombatRound(uint companion) => Calls.Add($"CombatRound {companion}");

        protected override void ProcessCompanionHeartbeat(uint companion) => Calls.Add($"Heartbeat {companion}");

        protected override bool HandleCompanionConversation(uint companion)
        {
            Calls.Add($"Conversation {companion}");
            return ConversationHandled;
        }

        protected override void RegisterDefensiveThreat(uint companion, uint threat) =>
            Calls.Add($"Threat {companion} {threat}");

        protected override void RecordBeastDeath(uint master, string beastId, uint killer) =>
            Calls.Add($"RecordDeath {master} {beastId} {killer}");

        protected override void RestoreBeastStats() => Calls.Add("RestoreBeastStats");

        protected override void LoadNPCStats() => Calls.Add("LoadNPCStats");

        protected override void ApplyCreatureMovementRate(uint creature) => Calls.Add($"MovementRate {creature}");

        protected override void ApplyRestStatusEffect(uint beast) => Calls.Add($"Rest {beast}");

        protected override void ReturnItem(uint owner, uint item) => Calls.Add($"ReturnItem {owner} {item}");

        protected override void TogglePlayerWindow(uint player, GuiWindowType windowType, GuiPayloadBase payload, uint tetherObject)
        {
            Calls.Add($"Window {player} {windowType} {tetherObject}");
            Payload = payload;
        }

        public GuiPayloadBase? Payload { get; private set; }
    }

    /// <summary>Records the field notes the service grants.</summary>
    private sealed class RecordingFieldNotes : IIncubationFieldNoteService
    {
        public List<(uint Player, BeastType Beast)> Granted { get; } = new();

        public void EnsureRegistered()
        {
        }

        public void LoadDisplayText()
        {
        }

        public void GrantDiscoveredNote(uint player, BeastType producedBeast) => Granted.Add((player, producedBeast));

        public IReadOnlyCollection<FieldNoteDetail> GetAllNotes() => Array.Empty<FieldNoteDetail>();

        public bool TryGetNoteForTarget(BeastType target, out FieldNoteDetail detail)
        {
            detail = null!;
            return false;
        }

        public bool TryGetNoteForKeyItem(KeyItemType keyItem, out FieldNoteDetail detail)
        {
            detail = null!;
            return false;
        }
    }

    /// <summary>Answers <see cref="IPropertyService.GetPropertyId"/>, the only member the beast service calls.</summary>
    public class PropertyIdStub : DispatchProxy
    {
        public Dictionary<uint, string> Ids { get; } = new();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name == nameof(IPropertyService.GetPropertyId))
                return Ids.GetValueOrDefault((uint)args![0]!, string.Empty);

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    /// <summary>
    /// Three beasts: Aradile (50 levels, two mutations: Bearbug needs nothing and weighs 10, Wraid needs a blue lyase
    /// and weighs 50), Bearbug and Wraid (one level each).
    /// </summary>
    public sealed class SampleBeastDefinition : IBeastListDefinition
    {
        public Dictionary<BeastType, BeastDetail> Build()
        {
            var builder = new BeastBuilder();
            builder.Create(BeastType.Aradile)
                .Name("Aradile")
                .Appearance(AppearanceType.LizardGiantBrownCCP)
                .AppearanceScale(1.5f)
                .SoundSetId(442)
                .PortraitId(4279)
                .CombatStats(AbilityType.Vitality, AbilityType.Might)
                .Role(BeastRoleType.Bruiser)
                .CanMutateInto(BeastType.Bearbug)
                .MutationWeight(10)
                .CanMutateInto(BeastType.Wraid)
                .MutationWeight(50)
                .MutationRequiresLyaseColor(EnzymeColorType.Blue, 1);

            for (var level = 1; level <= 50; level++)
            {
                AddLevel(builder, level);
            }

            builder.Create(BeastType.Bearbug)
                .Name("Bearbug")
                .Appearance(AppearanceType.LizardGiantBrownCCP)
                .CombatStats(AbilityType.Vitality, AbilityType.Might)
                .Role(BeastRoleType.Force);
            AddLevel(builder, 1);

            builder.Create(BeastType.Wraid)
                .Name("Wraid")
                .Appearance(AppearanceType.LizardGiantBrownCCP)
                .CombatStats(AbilityType.Vitality, AbilityType.Might)
                .Role(BeastRoleType.Evasion);
            AddLevel(builder, 1);

            return builder.Build();
        }

        private static void AddLevel(BeastBuilder builder, int level)
        {
            builder.AddLevel()
                .HP(100 * level)
                .FP(2 * level)
                .STM(3 * level)
                .DMG(level)
                .AttackDelay(ItemPropertyAttackDelay.Delay230)
                .Stat(AbilityType.Might, 10 + level)
                .Stat(AbilityType.Perception, 20 + level)
                .Stat(AbilityType.Vitality, 30 + level)
                .Stat(AbilityType.Willpower, 40 + level)
                .Stat(AbilityType.Agility, 50 + level)
                .Stat(AbilityType.Social, 60 + level)
                .MaxAttackBonus(100)
                .MaxAccuracyBonus(200)
                .MaxEvasionBonus(300);
        }
    }

    // ---- the cache the module load fills

    [Test]
    public void CacheData_loads_the_catalog_the_roles_the_foods_and_the_incubation_percentages()
    {
        _service.GetAllBeastTypes().Should().BeEquivalentTo(new[] { BeastType.Aradile, BeastType.Bearbug, BeastType.Wraid });
        var aradile = _service.GetBeastDetail(BeastType.Aradile);
        aradile.Name.Should().Be("Aradile");
        aradile.Levels.Should().HaveCount(50);
        aradile.PossibleMutations.Select(x => x.Type).Should().Equal(BeastType.Bearbug, BeastType.Wraid);

        _service.GetBeastRoleDetail(BeastRoleType.Bruiser).Name.Should().Be("Bruiser");
        _service.GetBeastRoleDetail(BeastRoleType.Invalid).IsActive.Should().BeFalse();

        _service.GetIncubationPercentageById(1).Should().Be(5f);
        _service.GetIncubationPercentageById(3).Should().Be(10f);
    }

    [Test]
    public void The_incubation_percentage_of_an_unknown_or_unparsable_row_is_zero()
    {
        _service.GetIncubationPercentageById(2).Should().Be(0f, "the label of the row is not a number");
        _service.GetIncubationPercentageById(99).Should().Be(0f);
    }

    [Test]
    public void CacheData_reads_the_incubation_table_rows_from_the_first()
    {
        _engine.CountOf("Get2DARowCount").Should().Be(0, "the setup cleared the calls");
        _service.CacheData();

        _engine.CallsTo("Get2DARowCount").Select(call => (string)call.Arguments[0]!).Should().Equal("iprp_incubonus");
        _engine.CallsTo("Get2DAString").Select(call => (int)call.Arguments[2]!).Should().Equal(1, 2, 3);
    }

    [Test]
    public void CacheData_can_run_again_without_duplicating_the_catalog()
    {
        _service.CacheData();

        _service.GetAllBeastTypes().Should().HaveCount(3);
    }

    [Test]
    public void The_catalog_is_empty_until_the_module_cache_runs()
    {
        var fresh = new TestBeastMastery(_db, _fieldNotes, DispatchProxy.Create<IPropertyService, PropertyIdStub>());

        fresh.GetAllBeastTypes().Should().BeEmpty();
    }

    [Test]
    public void The_default_definition_search_finds_every_beast_list_definition_of_the_server_assembly()
    {
        var expected = typeof(IBeastListDefinition).Assembly.GetTypes()
            .Where(type => typeof(IBeastListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .ToList();

        _service.DefaultDefinitions()
            .Where(type => type.Assembly == typeof(IBeastListDefinition).Assembly)
            .Should().BeEquivalentTo(expected);
        _service.DefaultDefinitions().Should().Contain(typeof(SampleBeastDefinition),
            "the search covers every loaded assembly, the test assembly included");
        expected.Should().NotBeEmpty();
    }

    [Test]
    public void CacheData_loads_every_beast_the_server_defines()
    {
        var service = new TestBeastMastery(_db, _fieldNotes, DispatchProxy.Create<IPropertyService, PropertyIdStub>());

        service.CacheData();

        service.GetAllBeastTypes().Should().HaveCountGreaterThan(50);
        service.GetAllBeastTypes().Should().OnlyHaveUniqueItems();
    }

    // ---- what is stored on a spawned beast

    [Test]
    public void The_beast_id_and_type_are_stored_as_local_variables()
    {
        _service.SetBeastId(BeastObject, BeastId);
        _service.SetBeastType(BeastObject, BeastType.Wraid);

        _service.GetBeastId(BeastObject).Should().Be(BeastId);
        _service.GetBeastType(BeastObject).Should().Be(BeastType.Wraid);
        _locals.String(BeastObject, "BEAST_ID").Should().Be(BeastId);
        _locals.Int(BeastObject, "BEAST_TYPE").Should().Be((int)BeastType.Wraid);
    }

    [Test]
    public void A_creature_without_a_beast_type_is_not_a_beast()
    {
        _service.GetBeastType(BeastObject).Should().Be(BeastType.Invalid);
        _service.IsPlayerBeast(BeastObject).Should().BeFalse();
    }

    [Test]
    public void A_beast_belongs_to_a_player_when_its_master_is_a_player()
    {
        _service.SetBeastType(BeastObject, BeastType.Aradile);

        _service.IsPlayerBeast(BeastObject).Should().BeTrue();
    }

    [Test]
    public void A_beast_without_a_valid_master_is_not_a_player_beast()
    {
        _service.SetBeastType(Npc, BeastType.Aradile);

        _service.IsPlayerBeast(Npc).Should().BeFalse("GetMaster answers the invalid object");
    }

    [Test]
    public void A_beast_whose_master_is_not_a_player_is_not_a_player_beast()
    {
        _valid.Add(Killer);
        _engine.SetReturnHandler("GetMaster", _ => Killer);
        _service.SetBeastType(BeastObject, BeastType.Aradile);

        _service.IsPlayerBeast(BeastObject).Should().BeFalse();
    }

    // ---- XP

    [Test]
    public void The_level_cap_is_ten_levels_per_tame_rank_up_to_the_maximum()
    {
        _service.GetBeastLevelCap(0).Should().Be(0);
        _service.GetBeastLevelCap(3).Should().Be(30);
        _service.GetBeastLevelCap(5).Should().Be(50);
        _service.GetBeastLevelCap(9).Should().Be(BeastMasteryService.MaxLevel);
    }

    [Test]
    public void The_required_xp_adds_the_penalty_percent_to_the_table_value()
    {
        _service.GetRequiredXP(0, 0).Should().Be(1100);
        _service.GetRequiredXP(10, 0).Should().Be(6160);
        _service.GetRequiredXP(10, 50).Should().Be(6160 + (int)(6160 * (50 * 0.01f)));
        _service.GetRequiredXP(100, 0).Should().Be(800000);
    }

    [Test]
    public void The_distributable_xp_fills_each_level_up_to_the_cap_less_what_the_beast_has()
    {
        var beast = new Beast { Level = 5, XP = 300, XPPenaltyPercent = 25 };

        var expected = Enumerable.Range(5, 5).Sum(level => _service.GetRequiredXP(level, 25)) - 300;

        _service.GetMaxDistributableXP(beast, 10).Should().Be(expected);
    }

    [Test]
    public void The_distributable_xp_is_zero_at_or_above_the_cap_and_never_passes_the_maximum_level()
    {
        _service.GetMaxDistributableXP(new Beast { Level = 30, XP = 100 }, 30).Should().Be(0);
        _service.GetMaxDistributableXP(new Beast { Level = 35, XP = 100 }, 30).Should().Be(0);
        _service.GetMaxDistributableXP(new Beast { Level = 49, XP = 0 }, 80).Should().Be(_service.GetRequiredXP(49, 0));
    }

    [Test]
    public void A_beast_that_gains_xp_below_the_next_level_keeps_it_and_the_player_is_told()
    {
        ActiveBeast(level: 1);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.GiveBeastXP(BeastObject, 1000, true);

        var saved = Saved();
        saved.XP.Should().Be(1000);
        saved.Level.Should().Be(1);
        saved.UnallocatedSP.Should().Be(0);
        Messages().Should().Equal("Rex earned 1000 XP.");
        _service.Calls.Should().Contain($"Refresh {Player}");
    }

    [Test]
    public void A_beast_that_gains_enough_xp_levels_up_and_earns_a_skill_point()
    {
        ActiveBeast(level: 1);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.GiveBeastXP(BeastObject, 2000, true);

        var saved = Saved();
        saved.Level.Should().Be(2);
        saved.XP.Should().Be(2000 - 1650);
        saved.UnallocatedSP.Should().Be(1);
        Messages().Should().Equal("Rex earned 2000 XP.", "Rex reaches level 2!");
    }

    [Test]
    public void A_beast_can_gain_several_levels_at_once()
    {
        ActiveBeast(level: 1);
        _service.PerkLevels[(Player, PerkType.Tame)] = 2;

        _service.GiveBeastXP(BeastObject, 1650 + 2200 + 100, true);

        var saved = Saved();
        saved.Level.Should().Be(3);
        saved.XP.Should().Be(100);
        saved.UnallocatedSP.Should().Be(2);
        Messages().Should().Equal("Rex earned 3950 XP.", "Rex reaches level 2!", "Rex reaches level 3!");
    }

    [Test]
    public void A_beast_at_the_cap_of_the_tame_perk_holds_one_xp_short_of_the_next_level()
    {
        ActiveBeast(level: 10);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.GiveBeastXP(BeastObject, 10000, true);

        var saved = Saved();
        saved.Level.Should().Be(10);
        saved.XP.Should().Be(_service.GetRequiredXP(10, 0) - 1);
        saved.UnallocatedSP.Should().Be(0);
    }

    [Test]
    public void The_xp_bonuses_add_up_status_social_and_learning_purity_unless_they_are_ignored()
    {
        var beast = ActiveBeast(level: 1);
        beast.LearningPurity = 5;
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;
        _service.StatAdjustments[(BeastObject, StatType.ExperiencePercentAdjustment)] = 20;
        _engine.SetReturnHandler("GetAbilityScore", arguments => (AbilityType)arguments[1]! == AbilityType.Social ? 4 : 0);

        _service.GiveBeastXP(BeastObject, 1000, false);

        var bonus = 0f;
        bonus += 20 * 0.01f;
        bonus += 4 * 0.025f;
        bonus += (5 * 2) * 0.01f;
        Saved().XP.Should().Be(1000 + (int)(1000 * bonus));

        beast.XP = 0;
        _service.GiveBeastXP(BeastObject, 1000, true);
        Saved().XP.Should().Be(1000, "ignoring the bonuses gives exactly the XP asked for");
    }

    [Test]
    public void A_beast_with_no_social_score_gets_no_social_bonus()
    {
        ActiveBeast(level: 1);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.GiveBeastXP(BeastObject, 1000, false);

        Saved().XP.Should().Be(1000);
    }

    [Test]
    public void A_beast_at_the_maximum_level_earns_no_xp_and_no_message()
    {
        ActiveBeast(level: 50);
        _service.PerkLevels[(Player, PerkType.Tame)] = 5;

        _service.GiveBeastXP(BeastObject, 5000, true);

        Saved().XP.Should().Be(0);
        Messages().Should().BeEmpty();
    }

    [Test]
    public void A_beast_that_reaches_the_maximum_level_starts_it_with_no_xp()
    {
        ActiveBeast(level: 49);
        _service.PerkLevels[(Player, PerkType.Tame)] = 5;

        _service.GiveBeastXP(BeastObject, 40000, true);

        var saved = Saved();
        saved.Level.Should().Be(50);
        saved.XP.Should().Be(0);
        saved.UnallocatedSP.Should().Be(1);
        Messages().Should().Equal("Rex earned 40000 XP.", "Rex reaches level 50!");
    }

    [Test]
    public void Gaining_xp_applies_the_stats_of_the_new_level_to_the_beast()
    {
        ActiveBeast(level: 1);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.GiveBeastXP(BeastObject, 2000, true);

        Added(Skin).Should().Contain((ItemPropertyType.NPCLevel, -1, 2));
        Added(Skin).Should().Contain((ItemPropertyType.Stamina, -1, 6));
        Added(Skin).Should().Contain((ItemPropertyType.FP, -1, 4));
        Added(Claw).Should().Contain((ItemPropertyType.DMG, -1, 2));
    }

    // ---- RP XP

    [Test]
    public void RP_xp_for_an_unknown_beast_is_refused()
    {
        _service.GiveBeastRPXP(Player, "missing", 500).Should().Be(0);

        _db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void RP_xp_for_another_players_beast_is_refused()
    {
        StoredBeast().OwnerPlayerId = "someone-else";

        _service.GiveBeastRPXP(Player, BeastId, 500).Should().Be(0);
    }

    [Test]
    public void RP_xp_is_capped_so_none_is_lost()
    {
        StoredBeast(level: 9, xp: 0);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;
        var room = _service.GetRequiredXP(9, 0);

        _service.GiveBeastRPXP(Player, BeastId, room + 5000).Should().Be(room);
    }

    [Test]
    public void RP_xp_is_refused_when_the_beast_is_at_its_cap()
    {
        StoredBeast(level: 10);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.GiveBeastRPXP(Player, BeastId, 500).Should().Be(0);
        _db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void RP_xp_for_a_beast_in_the_stables_is_saved_without_the_xp_bonuses()
    {
        StoredBeast(level: 1);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;
        _service.StatAdjustments[(BeastObject, StatType.ExperiencePercentAdjustment)] = 50;

        _service.GiveBeastRPXP(Player, BeastId, 1000).Should().Be(1000);

        Saved().XP.Should().Be(1000);
        _service.Calls.Should().Equal($"Refresh {Player}");
        _engine.CallsTo("ItemPropertyCustom").Should().BeEmpty("a beast that is not out has no stats to apply");
    }

    [Test]
    public void RP_xp_for_the_beast_that_is_out_goes_through_the_beast_and_applies_its_stats()
    {
        ActiveBeast(level: 1);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;
        _service.SetBeastType(BeastObject, BeastType.Aradile);
        _service.StatAdjustments[(BeastObject, StatType.ExperiencePercentAdjustment)] = 50;

        _service.GiveBeastRPXP(Player, BeastId, 2000).Should().Be(2000);

        Saved().Level.Should().Be(2);
        Saved().XP.Should().Be(350, "the XP bonuses do not apply to RP XP");
        Added(Skin).Should().Contain((ItemPropertyType.NPCLevel, -1, 2));
    }

    [Test]
    public void RP_xp_for_a_beast_in_the_stables_while_another_is_out_is_saved_directly()
    {
        ActiveBeast(level: 1);
        StoredBeast(level: 1, id: "beast-2");
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.GiveBeastRPXP(Player, "beast-2", 100).Should().Be(100);

        _db.Store.Saved.OfType<Beast>().Single().Id.Should().Be("beast-2");
        _engine.CallsTo("ItemPropertyCustom").Should().BeEmpty();
    }

    // ---- the XP a kill gives

    private void KillWith(int npcLevel, int beastLevel = 5, int areaBonusPercent = 0)
    {
        ActiveBeast(level: beastLevel);
        _service.PerkLevels[(Player, PerkType.Tame)] = 5;
        _service.NpcStats[Npc] = new NPCStats { Level = npcLevel };
        _engine.SetLocalInt(Area, "AREA_XP_BONUS_PERCENTAGE", areaBonusPercent);
        _engine.SetReturn("OBJECT_SELF", Player);

        _service.CombatPointXPDistributed();
    }

    [TestCase(5, 5, 600)]
    [TestCase(8, 5, 900)]
    [TestCase(11, 5, 1200)]
    [TestCase(40, 5, 1200)]
    [TestCase(4, 5, 450)]
    [TestCase(1, 5, 76)]
    public void A_kill_gives_the_beast_xp_by_the_level_difference_capped_at_the_highest_delta(int npcLevel, int beastLevel, int expected)
    {
        KillWith(npcLevel, beastLevel);

        Saved().XP.Should().Be(expected);
    }

    [Test]
    public void A_kill_far_below_the_beast_gives_no_xp()
    {
        KillWith(npcLevel: 1, beastLevel: 10);

        _db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void The_area_xp_bonus_is_added_to_a_kill_before_the_xp_is_given()
    {
        KillWith(npcLevel: 8, beastLevel: 5, areaBonusPercent: 50);

        Saved().XP.Should().Be(900 + (int)(900 * (50 * 0.01f)));
    }

    [Test]
    public void The_xp_bonuses_of_the_beast_apply_to_the_xp_of_a_kill()
    {
        ActiveBeast(level: 5);
        _service.PerkLevels[(Player, PerkType.Tame)] = 5;
        _service.NpcStats[Npc] = new NPCStats { Level = 5 };
        _service.StatAdjustments[(BeastObject, StatType.ExperiencePercentAdjustment)] = 10;

        _service.CombatPointXPDistributed();

        Saved().XP.Should().Be(600 + (int)(600 * (10 * 0.01f)));
    }

    [Test]
    public void A_kill_gives_no_xp_when_the_player_has_no_beast_out()
    {
        ActiveBeast();
        _henchman = 0;

        _service.CombatPointXPDistributed();

        _db.Store.Saved.Should().BeEmpty();
    }

    // ---- companion slot

    [Test]
    public void A_player_without_a_companion_may_spawn_one()
    {
        _service.GetCompanionSlotValidationError(Player).Should().BeEmpty();
    }

    [Test]
    public void A_player_with_a_companion_may_not_spawn_another()
    {
        _henchman = BeastObject;

        _service.GetCompanionSlotValidationError(Player).Should().Be("Only one companion may be active at a time.");
    }

    // ---- spawning

    [Test]
    public void Spawning_a_beast_is_refused_while_a_companion_is_out()
    {
        _henchman = BeastObject;

        _service.SpawnBeast(Player, BeastId, 100);

        Messages().Should().Equal("Only one companion may be active at a time.");
        _engine.CallsTo("CreateObject").Should().BeEmpty();
    }

    [Test]
    public void Spawning_a_beast_that_is_not_in_the_database_tells_the_player()
    {
        _service.SpawnBeast(Player, "missing", 100);

        Messages().Should().Equal("Unable to locate beast in DB. Notify an admin.");
        _engine.CallsTo("CreateObject").Should().BeEmpty();
    }

    [Test]
    public void Spawning_a_beast_creates_it_names_it_and_makes_it_the_henchman()
    {
        StoredBeast(level: 2);
        _engine.SetReturnHandler("GetEventScript", _ => "beast_spawn");

        _service.SpawnBeast(Player, BeastId, 100);

        var spawn = _engine.CallsTo("CreateObject").Single();
        spawn.Arguments[0].Should().Be(ObjectType.Creature);
        spawn.Arguments[1].Should().Be("pc_beast");
        var beast = FirstCreatedObject;
        _engine.CallsTo("SetName").Single().Arguments.Should().Equal(beast, "Rex");
        _locals.String(beast, "BEAST_ID").Should().Be(BeastId);
        _locals.Int(beast, "BEAST_TYPE").Should().Be((int)BeastType.Aradile);
        _engine.CallsTo("SetCreatureAppearanceType").Single().Arguments.Should().Equal(beast, AppearanceType.LizardGiantBrownCCP);
        _engine.CallsTo("SetObjectVisualTransform").Single().Arguments.Take(3).Should().Equal(beast, ObjectVisualTransform.Scale, 1.5f);
        _engine.CallsTo("AddHenchman").Single().Arguments.Should().Equal(Player, beast);
    }

    [Test]
    public void A_beast_uses_the_portrait_and_sound_set_of_its_species_unless_its_owner_picked_others()
    {
        StoredBeast(level: 1);
        _service.SpawnBeast(Player, BeastId, 100);
        _engine.CallsTo("SetPortraitId").Single().Arguments[1].Should().Be(4279);
        _engine.CallsTo("SetSoundset").Single().Arguments[1].Should().Be(442);

        _locals.ClearCalls();
        _henchman = 0;
        var custom = StoredBeast(level: 1);
        custom.PortraitId = 12;
        custom.SoundSetId = 13;
        _service.SpawnBeast(Player, BeastId, 100);

        _engine.CallsTo("SetPortraitId").Single().Arguments[1].Should().Be(12);
        _engine.CallsTo("SetSoundset").Single().Arguments[1].Should().Be(13);
    }

    [Test]
    public void A_spawned_beast_gets_its_stats_its_ai_profile_and_the_feats_of_its_perks()
    {
        var stored = StoredBeast(level: 2);
        stored.Perks[PerkType.Tame] = 3;
        stored.Perks[PerkType.DNAManipulation] = 1;

        _service.SpawnBeast(Player, BeastId, 100);

        var beast = FirstCreatedObject;
        Added(Skin).Should().Contain((ItemPropertyType.NPCLevel, -1, 2));
        _service.Calls.Should().Contain($"SetNPCMaxHitPoints {beast} 200");
        _service.Calls.Should().Contain($"SyncGrantedFeats {beast} {PerkType.Tame} 3 False");
        _service.Calls.Should().Contain($"SyncGrantedFeats {beast} {PerkType.DNAManipulation} 1 False");
        _locals.String(beast, "AI_PROFILE").Should().NotBeEmpty();
    }

    [Test]
    public void A_spawned_beast_runs_the_beast_scripts_and_the_spawn_script_runs_once()
    {
        StoredBeast(level: 1);
        _engine.SetReturnHandler("GetEventScript", _ => "beast_spawn_script");

        _service.SpawnBeast(Player, BeastId, 100);

        var beast = FirstCreatedObject;
        var scripts = _engine.CallsTo("SetEventScript").Select(call => (EventScript)call.Arguments[1]!).ToList();
        scripts.Should().BeEquivalentTo(new[]
        {
            EventScript.Creature_OnBlockedByDoor, EventScript.Creature_OnEndCombatRound, EventScript.Creature_OnDialogue,
            EventScript.Creature_OnDamaged, EventScript.Creature_OnDeath, EventScript.Creature_OnDisturbed,
            EventScript.Creature_OnHeartbeat, EventScript.Creature_OnMeleeAttacked, EventScript.Creature_OnRested,
            EventScript.Creature_OnSpawnIn, EventScript.Creature_OnSpellCastAt, EventScript.Creature_OnUserDefined
        });
        _engine.CallsTo("SetEventScript").Select(call => (string)call.Arguments[2]!).Should().Contain(new[]
        {
            ScriptName.OnBeastBlocked, ScriptName.OnBeastRoundEnd, ScriptName.OnBeastConversation, ScriptName.OnBeastDamaged,
            ScriptName.OnBeastDeath, ScriptName.OnBeastDisturbed, ScriptName.OnBeastHeartbeat, ScriptName.OnBeastAttacked,
            ScriptName.OnBeastRest, ScriptName.OnBeastSpawn, ScriptName.OnBeastSpellCast, ScriptName.OnBeastUserDefined
        });
        _engine.CallsTo("ExecuteScript").Single().Arguments.Should().Equal("beast_spawn_script", beast);
    }

    [Test]
    public void A_beast_that_is_called_at_full_health_is_restored_before_the_delayed_correction()
    {
        StoredBeast(level: 1);
        _engine.SetReturn("GetMaxHitPoints", 150);

        _service.SpawnBeast(Player, BeastId, 100);

        var beast = FirstCreatedObject;
        var calls = _engine.Calls.Select(call => call.Member).ToList();
        calls.Should().Contain("SetCurrentHitPoints");
        _engine.CallsTo("SetCurrentHitPoints").Single().Arguments.Should().Equal(beast, 150);
        calls.IndexOf("SetCurrentHitPoints").Should().BeLessThan(calls.IndexOf("AssignCommand"),
            "the full-health branch restores the beast at once, before the delayed correction is scheduled");
        _engine.CallsTo("DelayCommand").Should().BeEmpty("the delay is scheduled from inside the command assigned to the module");
        _engine.CallsTo("AssignCommand").Single().Arguments[0].Should().Be(Module);
    }

    [Test]
    public void A_beast_called_below_full_health_waits_for_the_delayed_correction()
    {
        StoredBeast(level: 1);
        _engine.SetReturn("GetMaxHitPoints", 150);

        _service.SpawnBeast(Player, BeastId, 50);

        _engine.CallsTo("SetCurrentHitPoints").Should().BeEmpty("only a full-health spawn is restored at once");
        RunScheduledCommands();
        _engine.CallsTo("DelayCommand").Single().Arguments[0].Should().Be(4f);
        _engine.CallsTo("SetCurrentHitPoints").Single().Arguments.Should().Equal(FirstCreatedObject, 1);
        _engine.CallsTo("EffectHeal").Single().Arguments.Should().Equal((int)(150 * (50 * 0.01f)));
        var heal = _engine.CallsTo("ApplyEffectToObject").Single();
        heal.Arguments[0].Should().Be(DurationType.Instant);
        heal.Arguments[2].Should().Be(FirstCreatedObject);
    }

    [Test]
    public void A_beast_spawned_with_no_heal_is_left_at_one_hit_point_after_the_delay()
    {
        StoredBeast(level: 1);
        _engine.SetReturn("GetMaxHitPoints", 150);

        _service.SpawnBeast(Player, BeastId, 0);
        RunScheduledCommands();

        _engine.CallsTo("SetCurrentHitPoints").Single().Arguments.Should().Equal(FirstCreatedObject, 1);
        _engine.CallsTo("EffectHeal").Should().BeEmpty();
        _engine.CallsTo("ApplyEffectToObject").Should().BeEmpty();
    }

    // ---- the stats of a beast

    [Test]
    public void The_stats_of_a_beast_come_from_its_level_and_its_purities()
    {
        var stored = ActiveBeast(level: 3);
        stored.AttackPurity = 50;
        stored.AccuracyPurity = 25;
        stored.EvasionPurity = 10;
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.GiveBeastXP(BeastObject, 1, true);

        var skin = Added(Skin);
        skin.Should().Contain((ItemPropertyType.NPCLevel, -1, 3));
        skin.Should().Contain((ItemPropertyType.Stamina, -1, 9));
        skin.Should().Contain((ItemPropertyType.FP, -1, 6));
        skin.Should().Contain((ItemPropertyType.Attack, -1, (int)(100 * (50 * 0.01f))));
        skin.Should().Contain((ItemPropertyType.Accuracy, -1, (int)(200 * (25 * 0.01f))));
        skin.Should().Contain((ItemPropertyType.Evasion, -1, (int)(300 * (10 * 0.01f))));
        _service.Calls.Should().Contain($"SetNPCMaxHitPoints {BeastObject} 300");
    }

    [Test]
    public void The_natural_weapon_gets_the_damage_the_delay_and_the_stats_of_the_species()
    {
        ActiveBeast(level: 3);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.GiveBeastXP(BeastObject, 1, true);

        var claw = Added(Claw);
        claw.Should().Contain((ItemPropertyType.DMG, -1, 3));
        claw.Should().Contain((ItemPropertyType.Delay, -1, (int)ItemPropertyAttackDelay.Delay230),
            "the generated species cadence replaces the generic beast claw delay at runtime");
        claw.Should().Contain((ItemPropertyType.DamageStat, (int)AbilityType.Might, -1));
        claw.Should().Contain((ItemPropertyType.AccuracyStat, (int)AbilityType.Vitality, -1));
    }

    [Test]
    public void The_accuracy_of_a_beast_is_the_custom_accuracy_property_not_the_native_bonus()
    {
        ActiveBeast(level: 3);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.GiveBeastXP(BeastObject, 1, true);

        Added(Skin).Should().Contain(property => property.Type == ItemPropertyType.Accuracy);
        Added(Skin).Should().NotContain(property => property.Type == ItemPropertyType.AccuracyBonus);
        _engine.CallsTo("AddItemProperty").Should().NotBeEmpty();
    }

    [Test]
    public void The_ability_scores_of_a_beast_are_set_from_its_level()
    {
        ActiveBeast(level: 3);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.GiveBeastXP(BeastObject, 1, true);

        var scores = _creature.CallsTo("SetRawAbilityScore")
            .ToDictionary(call => (AbilityType)call.Arguments[1]!, call => (int)call.Arguments[2]!);
        scores.Should().Equal(new Dictionary<AbilityType, int>
        {
            [AbilityType.Might] = 13,
            [AbilityType.Perception] = 23,
            [AbilityType.Vitality] = 33,
            [AbilityType.Willpower] = 43,
            [AbilityType.Agility] = 53,
            [AbilityType.Social] = 63
        });
    }

    [Test]
    public void A_beast_gets_a_defense_and_a_resistance_property_for_every_type()
    {
        ActiveBeast(level: 3);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.GiveBeastXP(BeastObject, 1, true);

        var skin = Added(Skin);
        skin.Where(property => property.Type == ItemPropertyType.Defense).Select(property => property.SubType)
            .Should().Equal(Combat.GetDefenseDamageTypes().Select(type => (int)type));
        skin.Where(property => property.Type == ItemPropertyType.Resistance).Select(property => property.SubType)
            .Should().Equal(Resistance.GetAllResistanceTypes().Select(type => (int)type));
    }

    // ---- food

    [Test]
    public void The_liked_and_the_hated_food_are_two_different_foods_drawn_in_order()
    {
        var foods = Enum.GetValues<BeastFoodType>().Where(food => food != BeastFoodType.Invalid).ToList();
        // The second draw of a service that forgot to take the liked food out of the list would pick the liked food again.
        var seed = SeedWhere(random =>
        {
            var liked = random.Next(foods.Count);
            return random.Next(foods.Count) == liked;
        });
        Random.SetSeed(seed);
        var replay = new System.Random(seed);
        var likedIndex = replay.Next(foods.Count);
        var left = foods.ToList();
        var liked = left[likedIndex];
        left.Remove(liked);
        var hated = left[replay.Next(left.Count)];

        var (likedFood, hatedFood) = _service.GetLikedAndHatedFood();

        likedFood.Should().Be(liked);
        hatedFood.Should().Be(hated);
        likedFood.Should().NotBe(hatedFood);
        Random.Next().Should().Be(replay.Next(), "the service drew exactly twice");
    }

    // ---- creature event handlers

    [Test]
    public void Leaving_the_party_or_entering_space_despawns_the_beast()
    {
        _henchman = BeastObject;
        _engine.SetReturn("OBJECT_SELF", Player);

        _service.RemoveAssociate();

        _service.Calls.Should().Equal($"Clear {BeastObject}");
        _engine.CallsTo("DestroyObject").Single().Arguments[0].Should().Be(BeastObject);
    }

    [Test]
    public void A_beast_may_pick_up_its_own_natural_weapons()
    {
        ActiveBeast();
        _engine.SetReturn("GetModuleItemAcquiredBy", BeastObject);
        _engine.SetReturn("GetModuleItemAcquired", ItemObject);

        foreach (var type in new[]
                 {
                     BaseItem.CreatureBludgeonWeapon, BaseItem.CreaturePierceWeapon, BaseItem.CreatureSlashPierceWeapon,
                     BaseItem.CreatureSlashWeapon, BaseItem.CreatureItem
                 })
        {
            _engine.SetReturn("GetBaseItemType", type);

            _service.OnAcquireItem();
        }

        Messages().Should().BeEmpty();
        _service.Calls.Should().BeEmpty();
    }

    [Test]
    public void A_beast_that_picks_up_another_item_gives_it_back_to_its_master()
    {
        ActiveBeast();
        _engine.SetReturn("GetModuleItemAcquiredBy", BeastObject);
        _engine.SetReturn("GetModuleItemAcquired", ItemObject);
        _engine.SetReturn("GetBaseItemType", BaseItem.Longsword);

        _service.OnAcquireItem();

        Messages().Should().Equal("Beasts cannot hold items.");
        _service.Calls.Should().Equal($"ReturnItem {Player} {ItemObject}");
        _engine.CallsTo("AssignCommand").Single().Arguments[0].Should().Be(BeastObject);
        RunScheduledCommands();
        _engine.CountOf("ClearAllActions").Should().Be(1);
    }

    [Test]
    public void A_creature_that_is_not_a_player_beast_may_acquire_anything()
    {
        _engine.SetReturn("GetModuleItemAcquiredBy", Player);
        _engine.SetReturn("GetModuleItemAcquired", ItemObject);
        _engine.SetReturn("GetBaseItemType", BaseItem.Longsword);

        _service.OnAcquireItem();

        Messages().Should().BeEmpty();
        _service.Calls.Should().BeEmpty();
    }

    [Test]
    public void A_beast_blocked_by_a_door_runs_the_henchman_block_script()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.BeastOnBlocked();

        _engine.CallsTo("ExecuteScript").Single().Arguments.Should().Equal("x0_ch_hen_block", BeastObject);
    }

    [Test]
    public void A_beast_runs_the_other_henchman_scripts_for_the_events_the_companion_control_does_not_own()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.BeastOnDisturbed();
        _service.BeastOnUserDefined();

        _engine.CallsTo("ExecuteScript").Select(call => (string)call.Arguments[0]!)
            .Should().Equal("x0_ch_hen_distrb", "x0_ch_hen_usrdef");
    }

    [Test]
    public void A_beast_ending_a_combat_round_lets_the_companion_control_run_it()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.BeastOnEndCombatRound();

        _service.Calls.Should().Equal($"CombatRound {BeastObject}");
    }

    [Test]
    public void A_beast_falls_back_to_the_henchman_conversation_when_the_companion_control_does_not_handle_it()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.ConversationHandled = true;
        _service.BeastOnConversation();
        _engine.CallsTo("ExecuteScript").Should().BeEmpty();

        _service.ConversationHandled = false;
        _service.BeastOnConversation();
        _engine.CallsTo("ExecuteScript").Single().Arguments.Should().Equal("x0_ch_hen_conv", BeastObject);
        _service.Calls.Should().Equal($"Conversation {BeastObject}", $"Conversation {BeastObject}");
    }

    [Test]
    public void A_damaged_or_attacked_beast_registers_the_threat()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);
        _engine.SetReturnHandler("GetLastDamager", _ => Damager);
        _engine.SetReturnHandler("GetLastAttacker", _ => Killer);

        _service.BeastOnDamaged();
        _service.BeastOnPhysicalAttacked();

        _service.Calls.Should().Equal($"Threat {BeastObject} {Damager}", $"Threat {BeastObject} {Killer}");
    }

    [Test]
    public void A_beast_only_defends_against_harmful_spells()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);
        _engine.SetReturn("GetLastSpellCaster", Damager);

        _engine.SetReturn("GetLastSpellHarmful", false);
        _service.BeastOnSpellCastAt();
        _service.Calls.Should().BeEmpty();

        _engine.SetReturn("GetLastSpellHarmful", true);
        _service.BeastOnSpellCastAt();
        _service.Calls.Should().Equal($"Threat {BeastObject} {Damager}");
    }

    [Test]
    public void A_beast_heartbeat_restores_the_beast_stats_then_runs_the_companion_control()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.BeastOnHeartbeat();

        _service.Calls.Should().Equal("RestoreBeastStats", $"Heartbeat {BeastObject}");
    }

    [Test]
    public void A_resting_beast_stops_what_it_is_doing_and_gets_the_rest_effect()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.BeastOnRested();

        _engine.CallsTo("ExecuteScript").Single().Arguments.Should().Equal("x0_ch_hen_rest", BeastObject);
        _service.Calls.Should().Equal($"Rest {BeastObject}");
        _engine.CallsTo("AssignCommand").Single().Arguments[0].Should().Be(BeastObject);
    }

    [Test]
    public void A_beast_that_spawns_is_made_destroyable_and_handed_to_the_companion_control()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.BeastOnSpawn();
        RunScheduledCommands();

        _service.Calls.Should().Equal("LoadNPCStats", $"MovementRate {BeastObject}", $"Initialize {BeastObject}");
        _engine.CallsTo("SetIsDestroyable").Single().Arguments.Take(3).Should().Equal(true, false, false);
    }

    [Test]
    public void A_dying_beast_is_recorded_on_its_master_and_marked_dead()
    {
        var stored = ActiveBeast();
        _engine.SetReturn("OBJECT_SELF", BeastObject);
        _engine.SetReturn("GetLastKiller", Killer);

        _service.BeastOnDeath();

        _service.Calls.Should().Equal($"RecordDeath {Player} {BeastId} {Killer}", $"Clear {BeastObject}");
        _engine.CallsTo("ExecuteScript").Single().Arguments.Should().Equal("x2_hen_death", BeastObject);
        stored.IsDead.Should().BeTrue();
        _db.Store.Saved.Should().ContainSingle().Which.Should().BeSameAs(stored);
    }

    [Test]
    public void A_beast_with_no_killer_is_recorded_against_the_last_hostile_actor()
    {
        ActiveBeast();
        _engine.SetReturn("OBJECT_SELF", BeastObject);
        _engine.SetReturn("GetLastKiller", ObjectInvalid);
        _engine.SetReturnHandler("GetLastHostileActor", _ => Damager);

        _service.BeastOnDeath();

        _service.Calls[0].Should().Be($"RecordDeath {Player} {BeastId} {Damager}");
    }

    [Test]
    public void A_dying_beast_with_no_entity_leaves_the_database_alone()
    {
        _service.SetBeastId(BeastObject, "missing");
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.BeastOnDeath();

        _service.Calls.Should().Contain($"Clear {BeastObject}");
        _db.Store.Saved.Should().BeEmpty();
    }

    // ---- terminals

    [Test]
    public void A_player_using_the_stables_terminal_opens_the_stables_window()
    {
        _engine.SetReturn("GetLastUsedBy", Player);
        _engine.SetReturn("OBJECT_SELF", ItemObject);

        _service.OpenStablesMenu();

        _service.Calls.Should().Equal($"Window {Player} {GuiWindowType.Stables} {ItemObject}");
        _service.Payload.Should().BeNull();
        Messages().Should().BeEmpty();
    }

    [Test]
    public void The_stables_terminal_is_for_players_only()
    {
        _engine.SetReturn("GetLastUsedBy", Npc);

        _service.OpenStablesMenu();

        Messages().Should().ContainSingle().Which.Should().Contain("Only players may use this terminal.");
        _service.Calls.Should().BeEmpty();
    }

    [Test]
    public void A_dm_or_a_possessing_dm_may_not_use_the_stables_terminal()
    {
        _engine.SetReturn("GetLastUsedBy", Player);
        _isDM = true;
        _service.OpenStablesMenu();

        _isDM = false;
        _isPossessed = true;
        _service.OpenStablesMenu();

        Messages().Should().HaveCount(2);
        _service.Calls.Should().BeEmpty();
    }

    [Test]
    public void Using_a_dna_extract_object_tells_the_player_to_use_the_extractor()
    {
        _engine.SetReturn("GetLastUsedBy", Player);

        _service.UseExtractDNAObject();

        Messages().Should().ContainSingle().Which.Should().Contain("Use a DNA Extractor on this corpse to retrieve its DNA.");
    }

    // ---- the incubator

    private void UseIncubator(int dnaLevel = 1, string propertyId = "incubator1")
    {
        _engine.SetReturn("GetLastUsedBy", Player);
        _engine.SetReturn("OBJECT_SELF", Incubator);
        _service.PerkLevels[(Player, PerkType.DNAManipulation)] = dnaLevel;
        _propertyIds.Ids[Incubator] = propertyId;

        _service.UseIncubator();
    }

    [Test]
    public void The_incubator_needs_the_dna_manipulation_perk()
    {
        UseIncubator(dnaLevel: 0);

        Messages().Should().Equal("Perk 'DNA Manipulation I' is required to use incubators.");
        _service.Calls.Should().BeEmpty();
        _db.Store.Queries.Should().BeEmpty();
    }

    [Test]
    public void An_incubator_that_is_not_part_of_a_property_cannot_be_used()
    {
        UseIncubator(propertyId: " ");

        Messages().Should().Equal("This incubator cannot be used.");
        _service.Calls.Should().BeEmpty();
    }

    [Test]
    public void An_incubator_with_no_job_opens_the_window_with_no_job()
    {
        UseIncubator();

        _service.Calls.Should().Equal($"Window {Player} {GuiWindowType.Incubator} {Player}");
        var payload = _service.Payload.Should().BeOfType<IncubatorPayload>().Subject;
        payload.PropertyId.Should().Be("incubator1");
        payload.IncubationJobId.Should().BeEmpty();
        var query = _db.Store.Queries.OfType<DBQuery<IncubationJob>>().Single().BuildQuery().QueryString;
        query.Should().Contain("ParentPropertyId");
    }

    [Test]
    public void An_incubator_with_the_players_own_job_opens_the_window_with_that_job()
    {
        _db.With(Job());

        UseIncubator();

        _service.Calls.Should().Equal($"Window {Player} {GuiWindowType.Incubator} {Player}");
        ((IncubatorPayload)_service.Payload!).IncubationJobId.Should().Be("job-1");
    }

    [Test]
    public void Another_players_running_job_blocks_the_incubator_and_says_when_it_completes()
    {
        var job = Job();
        job.PlayerId = "someone-else";
        job.DateCompleted = DateTime.UtcNow.AddHours(3);
        _db.With(job);

        UseIncubator();

        Messages().Should().ContainSingle().Which.Should().StartWith("Another player's incubation job is active. This job will complete in: ");
        _service.Calls.Should().BeEmpty();
    }

    [Test]
    public void Another_players_finished_job_blocks_the_incubator_too()
    {
        var job = Job();
        job.PlayerId = "someone-else";
        job.DateCompleted = DateTime.UtcNow.AddHours(-3);
        _db.With(job);

        UseIncubator();

        Messages().Should().Equal("Another player's incubation job is active. This job has completed.");
        _service.Calls.Should().BeEmpty();
    }

    [Test]
    public void Removing_a_property_removes_its_incubation_jobs()
    {
        _db.With(Job());

        _service.OnRemoveProperty();

        _db.Store.Deleted.Should().Equal("job-1");
        _db.Store.Queries.OfType<DBQuery<IncubationJob>>().Single().BuildQuery().QueryString.Should().Contain("ParentPropertyId");
    }

    [Test]
    public void Removing_a_property_with_no_jobs_deletes_nothing()
    {
        _service.OnRemoveProperty();

        _db.Store.Deleted.Should().BeEmpty();
    }

    // ---- eggs

    [Test]
    public void A_beast_egg_holds_the_dna_type_and_is_named_for_the_beast()
    {
        var egg = _service.CreateBeastEgg(BeastType.Wraid, Player);

        egg.Should().Be(FirstCreatedObject);
        var created = _engine.CallsTo("CreateItemOnObject").Single();
        created.Arguments[0].Should().Be("beast_egg");
        created.Arguments[1].Should().Be(Player);
        Added(egg).Should().Equal((ItemPropertyType.DNAType, (int)BeastType.Wraid, -1));
        _engine.CallsTo("SetName").Single().Arguments.Should().Equal(egg, "Beast Egg: Wraid");
    }

    /// <summary>The seed whose first roll fails (above the chance) so no mutation is looked for.</summary>
    private static int SeedForNoMutation(int chance) => SeedWhere(random => random.Next(1000) > chance);

    [Test]
    public void A_job_that_does_not_mutate_makes_an_egg_of_its_own_beast_and_ends_the_job()
    {
        var job = Job(mutationChance: 100);
        _db.With(job);
        var seed = SeedForNoMutation(100);
        Random.SetSeed(seed);
        var replay = new System.Random(seed);
        replay.Next(1000);

        _service.CreateBeastEgg(job, Player);

        _engine.CallsTo("SetName").Single().Arguments[1].Should().Be("Beast Egg: Aradile");
        _fieldNotes.Granted.Should().BeEmpty("no mutation, no field note");
        _db.Store.Deleted.Should().Equal("job-1");
        Random.Next().Should().Be(replay.Next(), "the mutation roll is one draw");
    }

    [Test]
    public void The_purities_of_the_job_become_incubation_properties_of_the_egg()
    {
        var job = Job(mutationChance: 100);
        job.DefensePurities[Combat.GetDefenseDamageTypes()[0]] = 77;
        job.ResistancePurities[Resistance.GetAllResistanceTypes()[0]] = 88;
        Random.SetSeed(SeedForNoMutation(100));

        _service.CreateBeastEgg(job, Player);

        var egg = FirstCreatedObject;
        var incubation = Added(egg).Where(property => property.Type == ItemPropertyType.Incubation).ToList();
        incubation.Should().Contain((ItemPropertyType.Incubation, (int)IncubationStatType.AttackPurity, 400));
        incubation.Should().Contain((ItemPropertyType.Incubation, (int)IncubationStatType.AccuracyPurity, 300));
        incubation.Should().Contain((ItemPropertyType.Incubation, (int)IncubationStatType.EvasionPurity, 200));
        incubation.Should().Contain((ItemPropertyType.Incubation, (int)IncubationStatType.LearningPurity, 100));
        incubation.Should().Contain((ItemPropertyType.Incubation, (int)IncubationStatType.XPPenalty, 100));
        BeastResistanceCalculator.TryGetDefensePurityIncubationStatType(Combat.GetDefenseDamageTypes()[0], out var defenseStat)
            .Should().BeTrue();
        incubation.Should().Contain((ItemPropertyType.Incubation, (int)defenseStat, 77));
        BeastResistanceCalculator.TryGetResistancePurityIncubationStatType(Resistance.GetAllResistanceTypes()[0], out var resistanceStat)
            .Should().BeTrue();
        incubation.Should().Contain((ItemPropertyType.Incubation, (int)resistanceStat, 88));
    }

    [Test]
    public void A_job_without_resistance_purities_gives_the_egg_zero_for_them()
    {
        var job = Job(mutationChance: 100);
        job.ResistancePurities = null!;
        Random.SetSeed(SeedForNoMutation(100));

        _service.CreateBeastEgg(job, Player);

        BeastResistanceCalculator.TryGetResistancePurityIncubationStatType(Resistance.GetAllResistanceTypes()[0], out var stat)
            .Should().BeTrue();
        Added(FirstCreatedObject).Should().Contain((ItemPropertyType.Incubation, (int)stat, 0));
    }

    [Test]
    public void The_egg_adds_a_vendor_value_for_its_quality_and_xp_penalty()
    {
        var job = Job(mutationChance: 100);
        Random.SetSeed(SeedForNoMutation(100));
        var purities = new List<int> { job.AttackPurity, job.AccuracyPurity, job.EvasionPurity, job.LearningPurity };
        purities.AddRange(Combat.GetDefenseDamageTypes().Select(type => BeastResistanceCalculator.GetDefensePurity(job, type)));
        purities.AddRange(Resistance.GetAllResistanceTypes().Select(_ => 0));
        var quality = purities.Average() / 1000f;
        var expected = Math.Max(180, (int)Math.Round(180 + (1170 * quality * (1f - (job.XPPenalty / 1000f)))));

        _service.CreateBeastEgg(job, Player);

        _itemPlugin.CallsTo("SetAddGoldPieceValue").Single().Arguments.Should().Equal(FirstCreatedObject, expected);
    }

    [Test]
    public void The_egg_vendor_value_is_never_below_the_base_value()
    {
        var job = new IncubationJob { Id = "job-1", BeastDNAType = BeastType.Aradile, XPPenalty = 1000 };
        Random.SetSeed(SeedForNoMutation(0));

        _service.CreateBeastEgg(job, Player);

        _itemPlugin.CallsTo("SetAddGoldPieceValue").Single().Arguments[1].Should().Be(180);
    }

    // ---- mutation

    [Test]
    public void A_mutation_needs_the_roll_to_be_within_the_chance_and_a_mutation_whose_requirements_are_met()
    {
        // Only Bearbug is possible (Wraid needs a lyase the job lacks); the roll succeeds, then the weighted pick draws once.
        var job = Job(mutationChance: 500);
        var seed = SeedWhere(random => random.Next(1000) <= 500);
        Random.SetSeed(seed);
        var replay = new System.Random(seed);
        replay.Next(1000);
        replay.NextDouble();

        _service.CreateBeastEgg(job, Player);

        _engine.CallsTo("SetName").Single().Arguments[1].Should().Be("Beast Egg: Bearbug");
        _fieldNotes.Granted.Should().Equal((Player, BeastType.Bearbug));
        Random.Next().Should().Be(replay.Next(), "one roll for the chance and one for the weighted pick");
    }

    [Test]
    public void A_mutation_picks_between_the_possible_beasts_by_weight()
    {
        // Bearbug weighs 10 and Wraid 50: a draw below 10/60 picks Bearbug, anything above picks Wraid.
        var job = Job(mutationChance: 1000);
        job.LyaseColors[EnzymeColorType.Blue] = 1;
        var wraidSeed = SeedWhere(random =>
        {
            random.Next(1000);
            return random.NextDouble() * 60 > 10;
        });
        Random.SetSeed(wraidSeed);

        _service.CreateBeastEgg(job, Player);

        _engine.CallsTo("SetName").Single().Arguments[1].Should().Be("Beast Egg: Wraid");
        _fieldNotes.Granted.Should().Equal((Player, BeastType.Wraid));
    }

    [Test]
    public void A_mutation_with_the_lighter_weight_can_win()
    {
        var job = Job(mutationChance: 1000);
        job.LyaseColors[EnzymeColorType.Blue] = 1;
        var bearbugSeed = SeedWhere(random =>
        {
            random.Next(1000);
            return random.NextDouble() * 60 <= 10;
        });
        Random.SetSeed(bearbugSeed);

        _service.CreateBeastEgg(job, Player);

        _engine.CallsTo("SetName").Single().Arguments[1].Should().Be("Beast Egg: Bearbug");
    }

    [Test]
    public void A_mutation_roll_equal_to_the_chance_still_mutates()
    {
        Random.SetSeed(7);
        var roll = new System.Random(7).Next(1000);
        var job = Job(mutationChance: roll);

        _service.CreateBeastEgg(job, Player);

        _engine.CallsTo("SetName").Single().Arguments[1].Should().Be("Beast Egg: Bearbug");
    }

    [Test]
    public void A_failed_mutation_roll_draws_once_and_leaves_the_beast_as_it_was()
    {
        var job = Job(mutationChance: 10);
        var seed = SeedForNoMutation(10);
        Random.SetSeed(seed);
        var replay = new System.Random(seed);
        replay.Next(1000);

        _service.CreateBeastEgg(job, Player);

        _engine.CallsTo("SetName").Single().Arguments[1].Should().Be("Beast Egg: Aradile");
        Random.Next().Should().Be(replay.Next());
    }

    [Test]
    public void A_beast_with_no_possible_mutation_draws_once_and_stays_the_same()
    {
        var job = Job(mutationChance: 1000, type: BeastType.Bearbug);
        var seed = SeedWhere(random => random.Next(1000) <= 1000);
        Random.SetSeed(seed);
        var replay = new System.Random(seed);
        replay.Next(1000);

        _service.CreateBeastEgg(job, Player);

        _engine.CallsTo("SetName").Single().Arguments[1].Should().Be("Beast Egg: Bearbug");
        _fieldNotes.Granted.Should().BeEmpty();
        Random.Next().Should().Be(replay.Next(), "no mutation is possible, so there is no weighted pick");
    }

    // ---- items

    [Test]
    public void Enzymes_and_dna_are_incubation_crafting_items_and_eggs_are_not()
    {
        _engine.SetReturnHandler("GetTag", arguments => (uint)arguments[0]! == 1 ? "INCUBATION_ENZYME" : "other");
        _engine.SetReturnHandler("GetResRef", arguments => (uint)arguments[0]! switch
        {
            2 => "beast_dna",
            3 => "beast_egg",
            _ => "other"
        });

        _service.IsIncubationCraftingItem(1).Should().BeTrue();
        _service.IsIncubationCraftingItem(2).Should().BeTrue();
        _service.IsIncubationCraftingItem(3).Should().BeFalse();
        _service.IsIncubationCraftingItem(4).Should().BeFalse();
    }

    [Test]
    public void Only_the_beast_egg_resref_is_a_beast_egg()
    {
        _engine.SetReturnHandler("GetResRef", arguments => (uint)arguments[0]! == 3 ? "beast_egg" : "beast_dna");

        _service.IsBeastEgg(3).Should().BeTrue();
        _service.IsBeastEgg(2).Should().BeFalse();
    }

    // ---- the constants the facade repeats

    [Test]
    public void The_facade_repeats_the_constants_the_logic_uses()
    {
        BeastMastery.MaxLevel.Should().Be(BeastMasteryService.MaxLevel);
        BeastMastery.DNAResref.Should().Be(BeastMasteryService.DNAResref);
        BeastMastery.BeastEggResref.Should().Be(BeastMasteryService.BeastEggResref);
        BeastMastery.EnzymeTag.Should().Be(BeastMasteryService.EnzymeTag);
    }

    [Test]
    public void The_container_builds_the_service_from_the_converted_services_it_needs()
    {
        using var container = new FacadeContainer();

        container.Get<IBeastMasteryService>().Should().BeOfType<BeastMasteryService>();
    }
}
