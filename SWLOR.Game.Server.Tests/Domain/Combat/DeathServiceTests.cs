using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MessagingService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The death instance: dying, the death handler (reputation reset, subdual, the respawn panel, the debt waiver),
/// respawning with the XP debt penalty, the respawn point and where a player respawns. The engine is a recording
/// fake with real local variables, the database an in-memory one, the neighbours scripted proxies, and the unconverted
/// combat, ability, status effect, recorder, log and feature calls are answered by a subclass.
/// </summary>
[TestFixture]
public class DeathServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Player = 1001;
    private const uint Hostile = 2002;
    private const uint Area = 500;
    private const uint Waypoint = 600;
    private const uint DefaultArea = 700;

    private readonly List<IDisposable> _scopes = new();
    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private InMemoryDBService _db = null!;
    private TestDeath _death = null!;
    private INamedAnimationService _animationService = null!;
    private IMessagingService _messagingService = null!;
    private IPlayerNameService _namesService = null!;
    private IPropertyService _propertyService = null!;
    private IAreaService _areasService = null!;
    private ScriptedProxy _animation = null!;
    private ScriptedProxy _messaging = null!;
    private ScriptedProxy _names = null!;
    private ScriptedProxy _property = null!;
    private ScriptedProxy _areas = null!;
    private bool _playerIsPc = true;
    private bool _playerIsDm;
    private bool _hostileIsPlayer;
    private bool _hostileIsDm;
    private bool _hostilePossessed;
    private bool _areaValid;
    private string _walkable = "1";

    [SetUp]
    public void SetUp()
    {
        _playerIsPc = true;
        _playerIsDm = false;
        _hostileIsPlayer = false;
        _hostileIsDm = false;
        _hostilePossessed = false;
        _areaValid = true;
        _walkable = "1";

        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid);
        _engine.SetReturnHandler("GetIsPC", args => ((uint)args[0]! == Player && _playerIsPc) || ((uint)args[0]! == Hostile && _hostileIsPlayer));
        _engine.SetReturnHandler("GetIsDM", args => ((uint)args[0]! == Hostile && _hostileIsDm) || ((uint)args[0]! == Player && _playerIsDm));
        _engine.SetReturnHandler("GetIsDMPossessed", args => (uint)args[0]! == Hostile && _hostilePossessed);
        _engine.SetReturnHandler("GetObjectUUID", args => $"uuid-{(uint)args[0]!}");
        _engine.SetReturn("GetLastPlayerDying", Player);
        _engine.SetReturn("GetLastPlayerDied", Player);
        _engine.SetReturn("GetLastRespawnButtonPresser", Player);
        _engine.SetReturn("GetEnteringObject", Player);
        _engine.SetReturnHandler("GetLastHostileActor", _ => Hostile);
        _engine.SetReturn("GetMaxHitPoints", 90);
        _engine.SetReturn("GetWaypointByTag", Waypoint);
        _engine.SetReturnHandler("GetArea", args => (uint)args[0]! == Waypoint ? DefaultArea : Area);
        _engine.SetReturnHandler("GetResRef", args => (uint)args[0]! == DefaultArea ? "default_area" : "other");
        _engine.SetReturn("GetSurfaceMaterial", 3);
        _engine.SetReturnHandler("Get2DAString", _ => _walkable);
        _engine.SetReturn("GetName", "Area Name");
        _engine.SetReturn("GetTag", "area_tag");
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _db = new InMemoryDBService()
            .With(new Player { Id = "uuid-1001", TotalSPAcquired = 10, Settings = new PlayerSettings() })
            .With(new Player { Id = "uuid-2002", Settings = new PlayerSettings() });

        _animationService = ScriptedProxy.Create<INamedAnimationService>(out _animation);
        _animation.On("ClearOnDeath", _ => null);
        _messagingService = ScriptedProxy.Create<IMessagingService>(out _messaging);
        _messaging.On("SendMessageNearbyToPlayers", _ => null);
        _namesService = ScriptedProxy.Create<IPlayerNameService>(out _names);
        _names.On("GetDisplayName", args => $"name{(uint)args[0]!}-{(uint)args[1]!}")
            .On("GetAuditName", args => $"audit-{(uint)args[0]!}");
        _propertyService = ScriptedProxy.Create<IPropertyService>(out _property);
        _property.On("GetEffectiveUpgradeLevel", _ => 0);
        _areasService = ScriptedProxy.Create<IAreaService>(out _areas);
        _areas.On("GetAreaByResref", _ => _areaValid ? Area : ObjectInvalid);

        _death = new TestDeath(_db, _animationService, _messagingService, _namesService, _propertyService, _areasService);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
    }

    private Player Record => _db.Get<Player>("uuid-1001");

    [Test]
    public void OnPlayerDying_KillsThePlayerWhenNothingPreventsIt()
    {
        _death.OnPlayerDying();

        _death.Calls.Should().Contain("TryPreventFatalDamage:1001:0:True");
        _engine.CallsTo("EffectDeath").Should().HaveCount(1);
        _engine.CallsTo("ApplyEffectToObject").Should().HaveCount(1);
        _engine.CallsTo("ApplyEffectToObject")[0].Arguments[2].Should().Be(Player);
    }

    [Test]
    public void OnPlayerDying_DoesNothingWhenFatalDamageIsPrevented()
    {
        _death.PreventFatal = true;

        _death.OnPlayerDying();

        _engine.CountOf("EffectDeath").Should().Be(0);
        _engine.CountOf("ApplyEffectToObject").Should().Be(0);
    }

    [Test]
    public void OnPlayerDeath_ByAnNpcShowsTheRespawnPanelAndWritesTheAudit()
    {
        _death.OnPlayerDeath();

        _animation.Calls.Should().ContainSingle(call => call.Member == "ClearOnDeath" && (uint)call.Arguments[0]! == Player);
        _death.Calls.Should().ContainInOrder("ClearQueuedAbility:1001", "RecordPlayerDeath:1001:2002", "ClearStatusEffectsOnDeath:1001");
        _engine.CallsTo("SetStandardFactionReputation").Select(call => (StandardFaction)call.Arguments[0]!)
            .Should().Equal(StandardFaction.Commoner, StandardFaction.Merchant, StandardFaction.Defender);
        _engine.CallsTo("SetStandardFactionReputation").Should().OnlyContain(call => (int)call.Arguments[1]! == 100);
        var panel = _engine.CallsTo("PopUpDeathGUIPanel").Single();
        panel.Arguments[0].Should().Be(Player);
        ((string)panel.Arguments[4]!).Should().StartWith("You have died.");
        _death.Logs.Should().ContainSingle()
            .Which.Should().Be((LogGroup.Death, "DEATH: audit-1001 - Area Name - area_tag - other Killed by: audit-2002"));
        _engine.CountOf("SendMessageToPC").Should().Be(0);
    }

    [Test]
    public void OnPlayerDeath_ClearsThePersonalReputationOfEveryHostileFactionMember()
    {
        _engine.SetReturnSequence("GetFirstFactionMember", 31u);
        _engine.SetReturnSequence("GetNextFactionMember", 32u, 33u);

        _death.OnPlayerDeath();

        _engine.CallsTo("ClearPersonalReputation").Select(call => (uint)call.Arguments[1]!).Should().Equal(31u, 32u, 33u);
    }

    [Test]
    public void OnPlayerDeath_ClaimsTheFirstDebtWaiverThatCanBeConsumed()
    {
        var spent = new TestWaiver(consumes: false);
        var good = new TestWaiver(consumes: true);
        var unused = new TestWaiver(consumes: true);
        _death.Waivers.AddRange(new[] { spent, good, unused });

        _death.OnPlayerDeath();

        spent.Asked.Should().BeTrue();
        good.Asked.Should().BeTrue();
        unused.Asked.Should().BeFalse();
        _locals.Int(Player, "DEATH_DEBT_WAIVED").Should().Be(1);
    }

    [Test]
    public void OnPlayerDeath_ReplacesAPreviousClaimWhenNoWaiverIsLeft()
    {
        _engine.SetLocalInt(Player, "DEATH_DEBT_WAIVED", 1);

        _death.OnPlayerDeath();

        _engine.CallsTo("DeleteLocalInt").Should().Contain(call => (string)call.Arguments[1]! == "DEATH_DEBT_WAIVED");
        _locals.Int(Player, "DEATH_DEBT_WAIVED").Should().Be(0);
    }

    [Test]
    public void OnPlayerDeath_ByAPlayerWithoutSubdualModeHasNoPanelAndNoMessages()
    {
        _hostileIsPlayer = true;

        _death.OnPlayerDeath();

        _engine.CountOf("PopUpDeathGUIPanel").Should().Be(0);
        _engine.CountOf("SendMessageToPC").Should().Be(0);
        _death.Logs.Should().BeEmpty();
        _death.Calls.Should().NotContain(call => call.StartsWith("ApplyStatusEffect"));
    }

    [Test]
    public void OnPlayerDeath_ByADMOrAPossessedPlayerIsAnOrdinaryDeath()
    {
        _hostileIsPlayer = true;
        _hostileIsDm = true;
        _death.OnPlayerDeath();
        _engine.CountOf("PopUpDeathGUIPanel").Should().Be(1);

        _engine.ClearCalls();
        _hostileIsDm = false;
        _hostilePossessed = true;
        _death.OnPlayerDeath();
        _engine.CountOf("PopUpDeathGUIPanel").Should().Be(1);
    }

    [Test]
    public void OnPlayerDeath_ASubduedPlayerIsRevivedAndPenalizedWithoutDebtOrPanel()
    {
        _hostileIsPlayer = true;
        _db.Get<Player>("uuid-2002").Settings.IsSubdualModeEnabled = true;
        _death.Recording = true;

        _death.OnPlayerDeath();

        _death.Calls.Should().Contain("RecordSubdual:1001:2002");
        _engine.CallsTo("SendMessageToPC").Single().Arguments[1].Should().Be("You have been subdued.");
        var nearby = _messaging.Calls.Single(call => call.Member == "SendMessageNearbyToPlayers");
        ((uint)nearby.Arguments[0]!).Should().Be(Player);
        var build = (BuildMessageDelegate)nearby.Arguments[1]!;
        build(77).Should().Be("name77-1001 has been subdued by name77-2002.");
        _engine.CountOf("EffectResurrection").Should().Be(1);
        _death.Calls.Should().Contain("RestoreGhostAfterRevive:1001");
        _engine.CallsTo("DelayCommand").Single().Arguments[0].Should().Be(0.1f);
        ((Action)_engine.CallsTo("DelayCommand").Single().Arguments[1]!)();
        _death.Calls.Should().Contain("ReapplyAura:1001");
        _death.Calls.Should().Contain($"ApplyStatusEffect:1001:1001:{nameof(KnockdownStatusEffect)}:60");
        _death.Calls.Should().Contain($"ApplyStatusEffect:1001:1001:{nameof(SubdualPenaltyStatusEffect)}:300");
        _engine.CountOf("PopUpDeathGUIPanel").Should().Be(0);
        _death.Calls.Should().NotContain("RecordDeathDebtWaiver");
    }

    [Test]
    public void OnPlayerDeath_ASubduedPlayerDoesNotRecordWhenNotRecordingAndForgetsAnOldWaiver()
    {
        _hostileIsPlayer = true;
        _db.Get<Player>("uuid-2002").Settings.IsSubdualModeEnabled = true;
        _death.Waivers.Add(new TestWaiver(consumes: true));

        _death.OnPlayerDeath();

        _death.Calls.Should().NotContain(call => call.StartsWith("RecordSubdual"));
        _death.Waivers[0].Asked.Should().BeFalse();
        _locals.Int(Player, "DEATH_DEBT_WAIVED").Should().Be(0);
    }

    [Test]
    public void OnPlayerRespawn_HealsHalfOfMaximumAndSendsThePlayerHomeWithThePenalty()
    {
        Record.RespawnAreaResref = "med_center";
        Record.RespawnLocationX = 1f;
        Record.RespawnLocationY = 2f;
        Record.RespawnLocationZ = 3f;
        _death.Recording = true;

        _death.OnPlayerRespawn();

        _engine.CallsTo("EffectHeal").Single().Arguments[0].Should().Be(45);
        _engine.CountOf("EffectResurrection").Should().Be(1);
        _death.Calls.Should().ContainInOrder("RestoreGhostAfterRevive:1001", "RecordRespawn:1001", "BeginRespawnAttribution", "EndRespawnAttribution");
        var assign = _engine.CallsTo("AssignCommand").Single();
        assign.Arguments[0].Should().Be(Player);
        _engine.ClearCalls();
        ((Action)assign.Arguments[1]!)();
        _engine.CallsTo("ClearAllActions").Single().Arguments[0].Should().Be(true);
        _engine.CountOf("JumpToLocation").Should().Be(1);
        _death.Logs.Should().ContainSingle().Which.Should().Be((LogGroup.Death, "RESPAWN - audit-1001 - 150 XP lost"));
    }

    [Test]
    public void OnPlayerRespawn_DoesNotRecordWhenNotRecording()
    {
        _death.OnPlayerRespawn();

        _death.Calls.Should().NotContain(call => call.StartsWith("RecordRespawn"));
        _death.Calls.Should().Contain("BeginRespawnAttribution");
    }

    [TestCase(0, 15)]
    [TestCase(49, 15)]
    [TestCase(50, 25)]
    [TestCase(199, 25)]
    [TestCase(200, 35)]
    [TestCase(299, 35)]
    [TestCase(300, 45)]
    [TestCase(900, 45)]
    public void OnPlayerRespawn_ChargesDebtByTheSkillPointsAcquired(int skillPoints, int multiplier)
    {
        Record.TotalSPAcquired = skillPoints;
        _engine.SetReturn("GetAbilityScore", 10);

        _death.OnPlayerRespawn();

        Record.XPDebt.Should().Be(skillPoints * multiplier);
        _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!)
            .Should().Contain($"{skillPoints * multiplier} XP added to your debt. (Total: {skillPoints * multiplier} XP)");
    }

    [Test]
    public void OnPlayerRespawn_ReducesTheDebtByMedicalCenterLevelAndSocial()
    {
        Record.TotalSPAcquired = 100;
        Record.CitizenPropertyId = "city-1";
        _property.On("GetEffectiveUpgradeLevel", args =>
        {
            args[0].Should().Be("city-1");
            args[1].Should().Be(PropertyUpgradeType.MedicalCenterLevel);
            return 2;
        });
        _engine.SetReturn("GetAbilityScore", 15);

        _death.OnPlayerRespawn();

        // 100 * 25 = 2500; reduction 2 * 5% + 5 * 3% = 25%.
        Record.XPDebt.Should().Be(2500 - (int)(2500 * (2 * 0.05f + 5 * 0.03f)));
    }

    [Test]
    public void OnPlayerRespawn_CapsTheReductionAtEightyPercentAndTheDebtAtItsMaximum()
    {
        Record.TotalSPAcquired = 100;
        _property.On("GetEffectiveUpgradeLevel", _ => 10);
        _engine.SetReturn("GetAbilityScore", 40);
        _death.OnPlayerRespawn();
        Record.XPDebt.Should().Be(2500 - (int)(2500 * 0.8f));

        Record.XPDebt = 9999990;
        Record.TotalSPAcquired = 400;
        _property.On("GetEffectiveUpgradeLevel", _ => 0);
        _engine.SetReturn("GetAbilityScore", 5);
        _death.OnPlayerRespawn();
        Record.XPDebt.Should().Be(9999999);
    }

    [Test]
    public void OnPlayerRespawn_AClaimedWaiverSparesTheDebtOnce()
    {
        Record.XPDebt = 500;
        _engine.SetLocalInt(Player, "DEATH_DEBT_WAIVED", 1);

        _death.OnPlayerRespawn();

        Record.XPDebt.Should().Be(500);
        _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).Should().Contain("Requiem spares you any XP debt.");
        _locals.Int(Player, "DEATH_DEBT_WAIVED").Should().Be(0);
        _death.Logs.Should().ContainSingle().Which.Should().Be((LogGroup.Death, "RESPAWN - audit-1001 - 500 XP lost"));
    }

    [Test]
    public void InitializeRespawnPoint_SavesTheDefaultWaypointForAPlayerWithoutOne()
    {
        _engine.SetReturn("GetPosition", Vector3Of(4f, 5f, 6f));
        _engine.SetReturn("GetFacing", 90f);

        _death.InitializeRespawnPoint();

        var saved = _db.Saved.OfType<Player>().Last();
        saved.RespawnLocationX.Should().Be(4f);
        saved.RespawnLocationY.Should().Be(5f);
        saved.RespawnLocationZ.Should().Be(6f);
        saved.RespawnAreaResref.Should().Be("default_area");
        saved.RespawnLocationOrientation.Should().Be(90f);
    }

    [Test]
    public void InitializeRespawnPoint_CreatesARecordForAPlayerWithoutOne()
    {
        _db = new InMemoryDBService();
        _death = new TestDeath(_db, _animationService, _messagingService, _namesService, _propertyService, _areasService);

        _death.InitializeRespawnPoint();

        _db.Saved.OfType<Player>().Single().Id.Should().Be("uuid-1001");
    }

    [Test]
    public void InitializeRespawnPoint_KeepsAnExistingPointAndSkipsDMsAndNonPlayers()
    {
        Record.RespawnAreaResref = "med_center";
        _death.InitializeRespawnPoint();
        _db.Saved.Should().BeEmpty();

        Record.RespawnAreaResref = string.Empty;
        _playerIsDm = true;
        _death.InitializeRespawnPoint();
        _db.Saved.Should().BeEmpty();

        _playerIsDm = false;
        _playerIsPc = false;
        _death.InitializeRespawnPoint();
        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void GetRespawnLocation_UsesTheMedicalCenterWhenItsAreaExistsAndIsWalkable()
    {
        var dbPlayer = new Player { RespawnAreaResref = "med_center", RespawnLocationX = 1f, RespawnLocationY = 2f, RespawnLocationZ = 3f };

        _death.GetRespawnLocation(dbPlayer);

        _engine.CountOf("Location").Should().Be(1);
        _engine.CountOf("GetWaypointByTag").Should().Be(0);
        _areas.Calls.Should().ContainSingle(call => (string)call.Arguments[0]! == "med_center");
    }

    [Test]
    public void GetRespawnLocation_FallsBackToTheDefaultPointWhenTheAreaIsGoneOrNotWalkableOrUnset()
    {
        var dbPlayer = new Player { RespawnAreaResref = "med_center" };

        _areaValid = false;
        _death.GetRespawnLocation(dbPlayer);
        _engine.CallsTo("GetWaypointByTag").Single().Arguments[0].Should().Be("DTH_DEFAULT_RESPAWN_POINT");

        _engine.ClearCalls();
        _areaValid = true;
        _walkable = "0";
        _death.GetRespawnLocation(dbPlayer);
        _engine.CountOf("GetWaypointByTag").Should().Be(1);

        _engine.ClearCalls();
        _walkable = "1";
        _engine.SetReturn("GetSurfaceMaterial", 0);
        _death.GetRespawnLocation(dbPlayer);
        _engine.CountOf("GetWaypointByTag").Should().Be(1);

        _engine.ClearCalls();
        _walkable = "not a number";
        _engine.SetReturn("GetSurfaceMaterial", 3);
        _death.GetRespawnLocation(dbPlayer);
        _engine.CountOf("GetWaypointByTag").Should().Be(1);

        _engine.ClearCalls();
        _death.GetRespawnLocation(null!);
        _engine.CountOf("GetWaypointByTag").Should().Be(1);
    }

    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase("  ", false)]
    [TestCase("dan_repgarrison", true)]
    public void HasRespawnArea_NeedsANonBlankResref(string? resref, bool expected)
    {
        _death.HasRespawnArea(new Player { RespawnAreaResref = resref! }).Should().Be(expected);
    }

    [Test]
    public void HasRespawnArea_IsFalseWithoutARecord()
    {
        _death.HasRespawnArea(null!).Should().BeFalse();
    }

    private static Vector3 Vector3Of(float x, float y, float z) => new(x, y, z);

    private sealed class TestWaiver
    {
        public TestWaiver(bool consumes)
        {
            Effect = ScriptedProxy.Create<IDeathDebtWaiverStatusEffect>(out var proxy);
            proxy.On("TryConsumeDeathDebtWaiver", _ =>
            {
                Asked = true;
                return consumes;
            });
        }

        public IDeathDebtWaiverStatusEffect Effect { get; }

        public bool Asked { get; private set; }
    }

    private sealed class TestDeath : DeathService
    {
        public TestDeath(
            IDBService db,
            INamedAnimationService animation,
            IMessagingService messaging,
            IPlayerNameService names,
            IPropertyService property,
            IAreaService areas)
            : base(db, animation, messaging, names, property, areas, FacadeForwarder.For<IStatusEffectService>(), FacadeForwarder.For<ICombatRecorderService>(),
                FacadeForwarder.For<IAbilityService>(), FacadeForwarder.For<ICombatDamageTakenService>())
        {
        }

        public List<string> Calls { get; } = new();
        public List<(LogGroup Group, string Details)> Logs { get; } = new();
        public List<TestWaiver> Waivers { get; } = new();
        public bool PreventFatal { get; set; }
        public bool Recording { get; set; }

        protected override bool TryPreventFatalDamageAndGrantTemporaryHP(uint defender, int damage, bool restoreToOneHP)
        {
            Calls.Add($"TryPreventFatalDamage:{defender}:{damage}:{restoreToOneHP}");
            return PreventFatal;
        }

        protected override void ClearQueuedAbility(uint player) => Calls.Add($"ClearQueuedAbility:{player}");

        protected override void RecordPlayerDeath(uint deceased, uint killer) => Calls.Add($"RecordPlayerDeath:{deceased}:{killer}");

        protected override void ClearStatusEffectsOnDeath(uint player) => Calls.Add($"ClearStatusEffectsOnDeath:{player}");

        protected override List<IDeathDebtWaiverStatusEffect> GetDeathDebtWaiverEffects(uint player)
        {
            Calls.Add("RecordDeathDebtWaiver");
            return Waivers.Select(waiver => waiver.Effect).ToList();
        }

        protected override bool IsRecording() => Recording;

        protected override void RecordSubdual(uint subdued, uint subduedBy) => Calls.Add($"RecordSubdual:{subdued}:{subduedBy}");

        protected override void RecordRespawn(uint player) => Calls.Add($"RecordRespawn:{player}");

        protected override IDisposable BeginRespawnAttribution()
        {
            Calls.Add("BeginRespawnAttribution");
            return new Scope(() => Calls.Add("EndRespawnAttribution"));
        }

        protected override void RestoreGhostAfterRevive(uint creature) => Calls.Add($"RestoreGhostAfterRevive:{creature}");

        protected override void ReapplyAuraEffectsForCreature(uint creature) => Calls.Add($"ReapplyAura:{creature}");

        protected override void ApplyStatusEffect(uint source, uint creature, Type statusEffectClass, float durationSeconds) =>
            Calls.Add($"ApplyStatusEffect:{source}:{creature}:{statusEffectClass.Name}:{durationSeconds}");

        protected override void WriteLog(LogGroup group, string details) => Logs.Add((group, details));
    }

    private sealed class Scope : IDisposable
    {
        private readonly Action _onDispose;

        public Scope(Action onDispose) => _onDispose = onDispose;

        public void Dispose() => _onDispose();
    }
}
