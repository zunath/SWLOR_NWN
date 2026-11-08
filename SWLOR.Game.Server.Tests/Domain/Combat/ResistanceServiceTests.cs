using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Core;
using FluentAssertions;
using NUnit.Framework;
using NWN.Native.API;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.ResistanceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The resistance instance: the type tables, how the stored, status effect, mimicry and gear values add up, the
/// player cap below immunity, damage and tick scaling, and the item property encoding. The engine is a recording fake,
/// the database an in-memory one, mimicry a scripted proxy, and the unconverted <c>Stat</c> and <c>StatusEffect</c>
/// reads and the native creature reads are answered by a subclass.
/// </summary>
[TestFixture]
public class ResistanceServiceTests
{
    private const uint Player = 1001;
    private const uint Npc = 2002;

    private readonly List<IDisposable> _scopes = new();
    private FakeNWScript _engine = null!;
    private InMemoryDBService _db = null!;
    private TestResistance _resistance = null!;
    private bool _isPlayer;
    private bool _isDm;
    private bool _isDmPossessed;

    [SetUp]
    public void SetUp()
    {
        _isPlayer = true;
        _isDm = false;
        _isDmPossessed = false;
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsPC", args => (uint)args[0]! == Player && _isPlayer);
        _engine.SetReturnHandler("GetIsDM", _ => _isDm);
        _engine.SetReturnHandler("GetIsDMPossessed", _ => _isDmPossessed);
        _engine.SetReturnHandler("GetObjectUUID", args => $"uuid-{(uint)args[0]!}");
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _db = new InMemoryDBService().With(new Player { Id = "uuid-1001" });
        _resistance = new TestResistance(_db);
        Stored.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
    }

    private Dictionary<ResistanceType, int> Stored => _db.Get<Player>("uuid-1001").Resistances;

    [Test]
    public void LoadResistanceTypes_ListsEveryTypeButInvalidAndReloadsCleanly()
    {
        _resistance.LoadResistanceTypes();
        _resistance.LoadResistanceTypes();

        var all = _resistance.GetAllResistanceTypes();
        all.Should().NotContain(ResistanceType.Invalid);
        all.Should().BeEquivalentTo(Enum.GetValues<ResistanceType>().Where(type => type != ResistanceType.Invalid));
        all.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void TheTypeTablesLoadOnFirstReadAndAreTheSameInstanceAfterwards()
    {
        var first = _resistance.GetAllResistanceTypes();

        _resistance.GetAllResistanceTypes().Should().BeSameAs(first);
        first.Should().NotBeEmpty();
    }

    [Test]
    public void IsValidResistanceType_RejectsInvalidAndUnknownValues()
    {
        _resistance.IsValidResistanceType(ResistanceType.Fire).Should().BeTrue();
        _resistance.IsValidResistanceType(ResistanceType.Invalid).Should().BeFalse();
        _resistance.IsValidResistanceType((ResistanceType)9999).Should().BeFalse();
    }

    [Test]
    public void CreateDefaultResistanceValues_HoldsTheDefaultForEveryType()
    {
        var values = _resistance.CreateDefaultResistanceValues(7);

        values.Keys.Should().BeEquivalentTo(_resistance.GetAllResistanceTypes());
        values.Values.Should().OnlyContain(value => value == 7);
        _resistance.CreateDefaultResistanceValues().Values.Should().OnlyContain(value => value == 0);
        _resistance.CreateDefaultResistanceValues().Should().BeEquivalentTo(ResistanceRules.CreateDefaultResistanceValues());
    }

    [Test]
    public void AdjustResistance_AddsToAnExistingAndToAMissingEntryAndIgnoresInvalidTypes()
    {
        var entity = new Player { Id = "p" };
        entity.Resistances.Remove(ResistanceType.Fire);

        _resistance.AdjustResistance(entity, ResistanceType.Fire, 5);
        _resistance.AdjustResistance(entity, ResistanceType.Fire, -2);
        _resistance.AdjustResistance(entity, ResistanceType.Invalid, 50);

        entity.Resistances[ResistanceType.Fire].Should().Be(3);
        entity.Resistances.ContainsKey(ResistanceType.Invalid).Should().BeFalse();
    }

    [Test]
    public void GetResistance_ReadsAPlayersStoredValueAndZeroForInvalidTypes()
    {
        Stored[ResistanceType.Fire] = 30;

        _resistance.GetResistance(Player, ResistanceType.Fire).Should().Be(30);
        _resistance.GetResistance(Player, ResistanceType.Ice).Should().Be(0);
        _resistance.GetResistance(Player, ResistanceType.Invalid).Should().Be(0);
    }

    [Test]
    public void GetResistance_IsZeroForAPlayerWithoutARecordOrATable()
    {
        _resistance.GetResistance(Player, ResistanceType.Fire).Should().Be(0);

        var noTable = new InMemoryDBService().With(new Player { Id = "uuid-1001", Resistances = null! });
        new TestResistance(noTable).GetResistance(Player, ResistanceType.Fire).Should().Be(0);
        new TestResistance(new InMemoryDBService()).GetResistance(Player, ResistanceType.Fire).Should().Be(0);
    }

    [Test]
    public void GetResistance_ReadsAnNpcsStatsAndTreatsADMAsAnNpc()
    {
        _resistance.NpcStats.Resistances[ResistanceType.Fire] = 40;
        _isPlayer = false;

        _resistance.GetResistance(Npc, ResistanceType.Fire).Should().Be(40);
        _resistance.GetResistance(Npc, ResistanceType.Ice).Should().Be(0);

        _isPlayer = true;
        _isDm = true;
        _resistance.GetResistance(Player, ResistanceType.Fire).Should().Be(40);

        _isDm = false;
        _isDmPossessed = true;
        _resistance.GetResistance(Player, ResistanceType.Fire).Should().Be(40);
    }

    [Test]
    public void GetResistance_AddsStatusEffectMimicryAndGearAdjustments()
    {
        Stored[ResistanceType.Fire] = 10;
        _resistance.StatusEffects.StatGroup.Resists[ResistanceType.Fire] = 5;
        _resistance.Mimicry[ResistanceType.Fire] = 7;
        _resistance.Adjustments[StatType.FireDefense] = 3;

        _resistance.GetResistance(Player, ResistanceType.Fire).Should().Be(25);
    }

    [TestCase(ResistanceType.Fire, StatType.FireDefense)]
    [TestCase(ResistanceType.Poison, StatType.PoisonDefense)]
    [TestCase(ResistanceType.Electrical, StatType.ElectricalDefense)]
    [TestCase(ResistanceType.Ice, StatType.IceDefense)]
    [TestCase(ResistanceType.Mind, StatType.MindResistance)]
    [TestCase(ResistanceType.Mobility, StatType.MobilityResistance)]
    [TestCase(ResistanceType.Trauma, StatType.TraumaResistance)]
    [TestCase(ResistanceType.Disruption, StatType.DisruptionResistance)]
    public void GetResistance_ReadsTheGearStatOfEachResistance(ResistanceType type, StatType stat)
    {
        _resistance.Adjustments[stat] = 12;

        _resistance.GetResistance(Player, type).Should().Be(12);
        _resistance.AdjustmentReads.Should().Contain(stat);
    }

    [Test]
    public void GetResistance_ReadsNoGearStatForTypesWithoutOne()
    {
        var withoutStat = _resistance.GetAllResistanceTypes()
            .Where(type => type is not (ResistanceType.Fire or ResistanceType.Poison or ResistanceType.Electrical
                or ResistanceType.Ice or ResistanceType.Mind or ResistanceType.Mobility or ResistanceType.Trauma
                or ResistanceType.Disruption))
            .ToList();
        if (withoutStat.Count == 0)
            Assert.Ignore("Every resistance type has a gear stat.");

        foreach (var type in withoutStat)
            _resistance.GetResistance(Player, type).Should().Be(0);

        _resistance.AdjustmentReads.Should().BeEmpty();
    }

    [Test]
    public void GetResistance_ClampsAnNpcToTheLimitsAndLetsItReachImmunity()
    {
        _isPlayer = false;
        _resistance.NpcStats.Resistances[ResistanceType.Fire] = 500;
        _resistance.GetResistance(Npc, ResistanceType.Fire).Should().Be(ResistanceService.MaximumResistance);

        _resistance.NpcStats.Resistances[ResistanceType.Fire] = -500;
        _resistance.GetResistance(Npc, ResistanceType.Fire).Should().Be(ResistanceService.MinimumResistance);
    }

    [Test]
    public void GetResistance_CapsAPlayerBelowImmunityWithoutATemporaryImmunity()
    {
        Stored[ResistanceType.Fire] = 150;

        _resistance.GetResistance(Player, ResistanceType.Fire).Should().Be(ResistanceService.MaximumResistance - 1);
        _resistance.HasImmunity(Player, ResistanceType.Fire).Should().BeFalse();
    }

    [Test]
    public void GetResistance_LetsATemporaryResistEffectGrantImmunity()
    {
        Stored[ResistanceType.Fire] = 150;
        var effect = _resistance.AddEffect(durationTicks: 5);
        effect.Resists[ResistanceType.Fire] = ResistanceService.MaximumResistance;

        _resistance.GetResistance(Player, ResistanceType.Fire).Should().Be(ResistanceService.MaximumResistance);
        _resistance.HasImmunity(Player, ResistanceType.Fire).Should().BeTrue();
    }

    [Test]
    public void GetResistance_LetsATemporaryStatEffectGrantImmunity()
    {
        Stored[ResistanceType.Fire] = 150;
        var effect = _resistance.AddEffect(durationTicks: 5);
        effect.Stats[StatType.FireDefense] = ResistanceService.MaximumResistance;

        _resistance.GetResistance(Player, ResistanceType.Fire).Should().Be(ResistanceService.MaximumResistance);
    }

    [Test]
    public void GetResistance_IgnoresAnImmunityEffectWithoutADuration()
    {
        Stored[ResistanceType.Fire] = 150;
        var effect = _resistance.AddEffect(durationTicks: 0);
        effect.Resists[ResistanceType.Fire] = ResistanceService.MaximumResistance;

        _resistance.GetResistance(Player, ResistanceType.Fire).Should().Be(ResistanceService.MaximumResistance - 1);
    }

    [Test]
    public void GetResistance_IgnoresAnEffectBelowImmunityAndOneForAnotherType()
    {
        Stored[ResistanceType.Fire] = 150;
        var small = _resistance.AddEffect(durationTicks: 5);
        small.Resists[ResistanceType.Fire] = 20;
        small.Stats[StatType.FireDefense] = 50;
        var other = _resistance.AddEffect(durationTicks: 5);
        other.Resists[ResistanceType.Ice] = ResistanceService.MaximumResistance;

        _resistance.GetResistance(Player, ResistanceType.Fire).Should().Be(ResistanceService.MaximumResistance - 1);
    }

    [Test]
    public void GetResistance_DoesNotCapADMOrAPossessedPlayer()
    {
        Stored[ResistanceType.Fire] = 150;
        _isDm = true;
        _resistance.NpcStats.Resistances[ResistanceType.Fire] = 150;

        _resistance.GetResistance(Player, ResistanceType.Fire).Should().Be(ResistanceService.MaximumResistance);
    }

    [Test]
    public void HasImmunity_NeedsAValidTypeAndFullResistance()
    {
        _isPlayer = false;
        _resistance.NpcStats.Resistances[ResistanceType.Fire] = 100;
        _resistance.NpcStats.Resistances[ResistanceType.Ice] = 99;

        _resistance.HasImmunity(Npc, ResistanceType.Fire).Should().BeTrue();
        _resistance.HasImmunity(Npc, ResistanceType.Ice).Should().BeFalse();
        _resistance.HasImmunity(Npc, ResistanceType.Invalid).Should().BeFalse();
    }

    [TestCase(0, 1f)]
    [TestCase(50, 0.5f)]
    [TestCase(-50, 1.5f)]
    [TestCase(100, 0f)]
    [TestCase(250, 0f)]
    [TestCase(-250, 2f)]
    public void CalculateResistanceDamageMultiplier_ScalesAndClamps(int resistance, float expected)
    {
        _resistance.CalculateResistanceDamageMultiplier(resistance).Should().BeApproximately(expected, 0.0001f);
    }

    [Test]
    public void CalculateResistanceDamageMultiplier_OfACreatureUsesItsTotalResistance()
    {
        Stored[ResistanceType.Fire] = 25;

        _resistance.CalculateResistanceDamageMultiplier(Player, ResistanceType.Fire).Should().BeApproximately(0.75f, 0.0001f);
    }

    [Test]
    public void ApplyResistanceToDamage_ByResistanceTypeRoundsAndNeverDropsALandedHitBelowOne()
    {
        Stored[ResistanceType.Fire] = 50;
        _resistance.ApplyResistanceToDamage(Player, ResistanceType.Fire, 11).Should().Be(6);
        _resistance.ApplyResistanceToDamage(Player, ResistanceType.Fire, 1).Should().Be(1);
        _resistance.ApplyResistanceToDamage(Player, ResistanceType.Fire, 0).Should().Be(0);
        _resistance.ApplyResistanceToDamage(Player, ResistanceType.Fire, -4).Should().Be(0);

        Stored[ResistanceType.Fire] = -100;
        _resistance.ApplyResistanceToDamage(Player, ResistanceType.Fire, 10).Should().Be(20);
    }

    [Test]
    public void ApplyResistanceToDamage_IsZeroAtImmunity()
    {
        _isPlayer = false;
        _resistance.NpcStats.Resistances[ResistanceType.Fire] = 100;

        _resistance.ApplyResistanceToDamage(Npc, ResistanceType.Fire, 40).Should().Be(0);
    }

    [Test]
    public void ApplyResistanceToDamage_ByDamageTypeUsesTheMatchingResistanceOrLeavesTheDamageAlone()
    {
        Stored[ResistanceType.Fire] = 50;
        var elemental = Enum.GetValues<CombatDamageType>()
            .First(type => type.TryGetElementalResistanceType(out var resistance) && resistance == ResistanceType.Fire);
        var neither = Enum.GetValues<CombatDamageType>()
            .First(type => !type.TryGetElementalResistanceType(out _) && !type.TryGetSourceResistanceType(out _));

        _resistance.ApplyResistanceToDamage(Player, elemental, 20).Should().Be(10);
        _resistance.ApplyResistanceToDamage(Player, neither, 20).Should().Be(20);
        _resistance.ApplyResistanceToDamage(Player, elemental, 0).Should().Be(0);
        _resistance.ApplyResistanceToDamage(Player, elemental, -3).Should().Be(0);
    }

    [Test]
    public void CalculateResistedTicks_ScalesByTheMultiplierWithAMinimumOfOne()
    {
        _isPlayer = false;
        var stats = _resistance.NpcStats.Resistances;

        _resistance.CalculateResistedTicks(Npc, ResistanceType.Fire, 10).Should().Be(10);
        stats[ResistanceType.Fire] = 50;
        _resistance.CalculateResistedTicks(Npc, ResistanceType.Fire, 10).Should().Be(5);
        stats[ResistanceType.Fire] = 95;
        _resistance.CalculateResistedTicks(Npc, ResistanceType.Fire, 2).Should().Be(1);
        stats[ResistanceType.Fire] = 100;
        _resistance.CalculateResistedTicks(Npc, ResistanceType.Fire, 10).Should().Be(0);
        stats[ResistanceType.Fire] = -100;
        _resistance.CalculateResistedTicks(Npc, ResistanceType.Fire, 10).Should().Be(20);
    }

    [Test]
    public void CalculateResistedTicks_LeavesNonPositiveTicksAndInvalidTypesAlone()
    {
        _resistance.CalculateResistedTicks(Player, ResistanceType.Fire, 0).Should().Be(0);
        _resistance.CalculateResistedTicks(Player, ResistanceType.Fire, -3).Should().Be(-3);
        _resistance.CalculateResistedTicks(Player, ResistanceType.Invalid, 9).Should().Be(9);
    }

    [TestCase(0, 0)]
    [TestCase(40, 40)]
    [TestCase(-40, 140)]
    [TestCase(-100, 200)]
    [TestCase(-300, 200)]
    [TestCase(300, 100)]
    public void EncodeItemPropertyCostTableValue_StoresVulnerabilityAboveTheOffset(int resistance, int expected)
    {
        _resistance.EncodeItemPropertyCostTableValue(resistance).Should().Be(expected);
        _resistance.EncodeItemPropertyCostTableValue(resistance).Should().Be(ResistanceRules.EncodeItemPropertyCostTableValue(resistance));
    }

    [TestCase(0, 0)]
    [TestCase(40, 40)]
    [TestCase(140, -40)]
    [TestCase(200, -100)]
    [TestCase(900, -100)]
    [TestCase(-5, -5)]
    [TestCase(-900, -100)]
    public void DecodeItemPropertyCostTableValue_ReversesTheEncodingAndClamps(int costTableValue, int expected)
    {
        _resistance.DecodeItemPropertyCostTableValue(costTableValue).Should().Be(expected);
    }

    [Test]
    public void EveryEncodedResistanceDecodesBack()
    {
        for (var resistance = ResistanceService.MinimumResistance; resistance <= ResistanceService.MaximumResistance; resistance++)
        {
            _resistance.DecodeItemPropertyCostTableValue(_resistance.EncodeItemPropertyCostTableValue(resistance))
                .Should().Be(resistance);
        }
    }

    [Test]
    public void NativeReads_UseTheNativeCreatureForPlayersAndNpcs()
    {
        var creature = (CNWSCreature)null!;
        _resistance.NativeSelf = Player;
        _resistance.NativeIsPlayer = true;
        _resistance.NativePlayerId = "uuid-1001";
        Stored[ResistanceType.Fire] = 50;

        _resistance.GetResistanceNative(creature, ResistanceType.Fire).Should().Be(50);
        _resistance.GetResistanceNative(creature, ResistanceType.Invalid).Should().Be(0);
        _resistance.CalculateResistanceDamageMultiplierNative(creature, ResistanceType.Fire).Should().BeApproximately(0.5f, 0.0001f);
        _resistance.ApplyResistanceToDamageNative(creature, ResistanceType.Fire, 20).Should().Be(10);
        _resistance.ApplyResistanceToDamageNative(creature, ResistanceType.Fire, 0).Should().Be(0);

        _resistance.NativeIsPlayer = false;
        _resistance.NpcStats.Resistances[ResistanceType.Fire] = 75;
        _resistance.GetResistanceNative(creature, ResistanceType.Fire).Should().Be(75);
        _resistance.GetResistanceNative(creature, ResistanceType.Ice).Should().Be(0);
    }

    [Test]
    public void NativeReads_TreatAPossessedPlayerAsAnNpcAndMissingRecordsAsZero()
    {
        var creature = (CNWSCreature)null!;
        _resistance.NativeSelf = Player;
        _resistance.NativeIsPlayer = true;
        _resistance.NativePlayerId = "uuid-1001";
        _resistance.NpcStats.Resistances[ResistanceType.Fire] = 60;
        _isDmPossessed = true;
        _isPlayer = false;

        _resistance.GetResistanceNative(creature, ResistanceType.Fire).Should().Be(60);

        _isDmPossessed = false;
        _resistance.NativePlayerId = "someone-else";
        _resistance.GetResistanceNative(creature, ResistanceType.Fire).Should().Be(0);
    }

    [Test]
    public void ApplyResistanceToDamageNative_ByDamageTypeMatchesTheCreatureOverload()
    {
        var creature = (CNWSCreature)null!;
        _resistance.NativeSelf = Player;
        _resistance.NativeIsPlayer = true;
        _resistance.NativePlayerId = "uuid-1001";
        Stored[ResistanceType.Fire] = 50;
        var elemental = Enum.GetValues<CombatDamageType>()
            .First(type => type.TryGetElementalResistanceType(out var resistance) && resistance == ResistanceType.Fire);
        var neither = Enum.GetValues<CombatDamageType>()
            .First(type => !type.TryGetElementalResistanceType(out _) && !type.TryGetSourceResistanceType(out _));

        _resistance.ApplyResistanceToDamageNative(creature, elemental, 20).Should().Be(10);
        _resistance.ApplyResistanceToDamageNative(creature, neither, 20).Should().Be(20);
        _resistance.ApplyResistanceToDamageNative(creature, elemental, 0).Should().Be(0);
    }

    [Test]
    public void TheTablesAreNeverRebuiltByAReadOnTheDamagePath()
    {
        var before = _resistance.GetAllResistanceTypes();
        Stored[ResistanceType.Fire] = 10;

        for (var index = 0; index < 100; index++)
        {
            _resistance.GetResistance(Player, ResistanceType.Fire);
            _resistance.IsValidResistanceType(ResistanceType.Fire);
        }

        _resistance.GetAllResistanceTypes().Should().BeSameAs(before);
    }

    [Test]
    public void ReadsOfTheTablesAndTheMultiplierAllocateNothing()
    {
        _resistance.LoadResistanceTypes();
        var sink = 0f;
        // Warm up so the JIT and the first call do not count.
        sink += _resistance.IsValidResistanceType(ResistanceType.Fire) ? 1 : 0;
        sink += _resistance.GetAllResistanceTypes().Count;
        sink += _resistance.CalculateResistanceDamageMultiplier(40);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 1000; index++)
        {
            sink += _resistance.IsValidResistanceType(ResistanceType.Fire) ? 1 : 0;
            sink += _resistance.GetAllResistanceTypes().Count;
            sink += _resistance.CalculateResistanceDamageMultiplier(index % 150);
        }

        (GC.GetAllocatedBytesForCurrentThread() - before).Should().Be(0);
        sink.Should().BeGreaterThan(0);
    }

    [Test]
    public void TheInstancesDoNotShareTheirTables()
    {
        var other = new TestResistance(_db);

        other.GetAllResistanceTypes().Should().NotBeSameAs(_resistance.GetAllResistanceTypes());
    }

    private sealed class TestResistance : ResistanceService
    {
        private readonly CreatureStatusEffect _statusEffects = new() { StatGroup = new StatGroup() };

        public TestResistance(IDBService db)
            : base(db, CreateMimicry(out var script), FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.LazyFor<IStatusEffectService>())
        {
            // `this.` names the property below: tools/Refactor/SWLOR.FacadeRemoval writes a bare Mimicry.X as GameServices.Mimicry.X.
            script.On("GetResistanceBonus", args => this.Mimicry.TryGetValue((ResistanceType)args[1]!, out var value) ? value : 0);
        }

        public Dictionary<ResistanceType, int> Mimicry { get; } = new();
        public Dictionary<StatType, int> Adjustments { get; } = new();
        public List<StatType> AdjustmentReads { get; } = new();
        public NPCStats NpcStats { get; } = new();
        public CreatureStatusEffect StatusEffects => _statusEffects;
        public uint NativeSelf { get; set; }
        public bool NativeIsPlayer { get; set; }
        public string NativePlayerId { get; set; } = string.Empty;

        public StatGroup AddEffect(int durationTicks)
        {
            var group = new StatGroup();
            var effect = ScriptedProxy.Create<IStatusEffect>(out var proxy);
            proxy.On("get_StatGroup", _ => group)
                .On("get_DurationTicks", _ => durationTicks)
                .On("get_ActivationType", _ => StatusEffectActivationType.Passive)
                .On("get_SourceType", _ => StatusEffectSourceType.Normal);
            _statusEffects.Add(effect);
            return group;
        }

        private static IMimicryService CreateMimicry(out ScriptedProxy script)
        {
            return ScriptedProxy.Create<IMimicryService>(out script);
        }

        protected override NPCStats GetNPCStats(uint npc) => NpcStats;

        protected override NPCStats GetNPCStatsNative(CNWSCreature npc) => NpcStats;

        protected override int GetStatAdjustment(uint creature, StatType stat)
        {
            AdjustmentReads.Add(stat);
            return Adjustments.TryGetValue(stat, out var value) ? value : 0;
        }

        protected override CreatureStatusEffect GetCreatureStatusEffects(uint creature) => _statusEffects;

        protected override uint GetNativeSelf(CNWSCreature creature) => NativeSelf;

        protected override bool IsNativePlayerCharacter(CNWSCreature creature) => NativeIsPlayer;

        protected override string GetNativePlayerId(CNWSCreature creature) => NativePlayerId;
    }
}
