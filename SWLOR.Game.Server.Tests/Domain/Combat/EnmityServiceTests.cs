using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The enmity instance: the damage, healing and attack handlers, the tables and their queries, taunts, reductions,
/// proximity enmity and the attack commands that follow the highest target. The engine is a recording fake, the
/// converted neighbours (AI, activity, party, holo-com, beast mastery) are scripted stand-ins, and the unconverted hot
/// core is answered by a subclass.
/// </summary>
[TestFixture]
public class EnmityServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Player = 1;
    private const uint Player2 = 2;
    private const uint Enemy = 100;
    private const uint Enemy2 = 101;
    private const uint AreaA = 500;
    private const uint AreaB = 501;

    private static readonly DateTime Start = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);

    private FakeNWScript _engine = null!;
    private FakeEventsPlugin _events = null!;
    private IDisposable _engineScope = null!;
    private IDisposable _eventsScope = null!;
    private IDisposable _clock = null!;
    private TestEnmity _enmity = null!;
    private DateTime _now;

    private uint _self;
    private uint _lastDamager;
    private int _damage;
    private uint _lastAttacker;
    private uint _lastDied;
    private uint _exiting;
    private readonly Dictionary<string, string> _eventData = new();
    private readonly Dictionary<uint, uint> _areas = new();
    private readonly HashSet<uint> _players = new();
    private readonly HashSet<uint> _dead = new();
    private readonly Dictionary<uint, int> _hitPoints = new();
    private readonly Dictionary<uint, uint> _masters = new();
    private readonly Dictionary<uint, uint> _attackTargets = new();
    private readonly Dictionary<uint, ActionType> _actions = new();
    private readonly Dictionary<(uint, uint), float> _distances = new();
    private readonly HashSet<uint> _unseen = new();
    private readonly HashSet<uint> _evading = new();
    private readonly HashSet<(uint, uint)> _evadeStarts = new();
    private readonly HashSet<(uint, uint)> _aggroRange = new();
    private readonly HashSet<uint> _busy = new();
    private readonly HashSet<(uint, uint)> _partyPairs = new();
    private readonly HashSet<uint> _holograms = new();
    private readonly HashSet<uint> _playerBeasts = new();
    private readonly List<uint> _combatStopped = new();

    [SetUp]
    public void SetUp()
    {
        _now = Start;
        _self = Enemy;
        _lastDamager = Player;
        _damage = 0;
        _lastAttacker = Player;
        _lastDied = Player;
        _exiting = Player;
        _eventData.Clear();
        _areas.Clear();
        _players.Clear();
        _players.Add(Player);
        _players.Add(Player2);
        _dead.Clear();
        _hitPoints.Clear();
        _masters.Clear();
        _attackTargets.Clear();
        _actions.Clear();
        _distances.Clear();
        _unseen.Clear();
        _evading.Clear();
        _evadeStarts.Clear();
        _aggroRange.Clear();
        _busy.Clear();
        _partyPairs.Clear();
        _holograms.Clear();
        _playerBeasts.Clear();
        _combatStopped.Clear();

        _clock = Clock.OverrideForTests(() => _now);
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid);
        _engine.SetReturnHandler("OBJECT_SELF", _ => _self);
        _engine.SetReturnHandler("GetLastDamager", _ => _lastDamager);
        _engine.SetReturnHandler("GetTotalDamageDealt", _ => _damage);
        _engine.SetReturnHandler("GetLastAttacker", _ => _lastAttacker);
        _engine.SetReturnHandler("GetLastPlayerDied", _ => _lastDied);
        _engine.SetReturnHandler("GetExitingObject", _ => _exiting);
        _engine.SetReturnHandler("GetArea", args => _areas.GetValueOrDefault((uint)args[0]!, AreaA));
        _engine.SetReturnHandler("GetIsPC", args => _players.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsDead", args => _dead.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetCurrentHitPoints", args => _hitPoints.GetValueOrDefault((uint)args[0]!, 100));
        _engine.SetReturnHandler("GetMaster", args => _masters.GetValueOrDefault((uint)args[0]!, ObjectInvalid));
        _engine.SetReturnHandler("GetAttackTarget", args => _attackTargets.GetValueOrDefault((uint)args[0]!, ObjectInvalid));
        _engine.SetReturnHandler("GetCurrentAction", args => _actions.GetValueOrDefault((uint)args[0]!, ActionType.Invalid));
        _engine.SetReturnHandler("GetDistanceBetween", args => _distances.GetValueOrDefault(((uint)args[0]!, (uint)args[1]!), 5f));
        _engine.SetReturnHandler("GetName", args => $"name-{(uint)args[0]!}");
        _engine.SetReturnHandler("GetIsReactionTypeHostile", _ => true);
        _engine.SetReturnHandler("LineOfSightObject", _ => true);
        _engine.SetReturnHandler("StringToObject", args => uint.TryParse((string)args[0]!, out var id) ? id : ObjectInvalid);
        _engineScope = NWScriptRuntime.Override(_engine);

        _events = new FakeEventsPlugin();
        _events.SetReturnHandler("GetEventData", args => _eventData.GetValueOrDefault((string)args[0]!, string.Empty));
        _eventsScope = EventsPluginRuntime.Override(_events);

        var ai = ScriptedProxy.Create<IAIService>(out var aiScript);
        aiScript.On("IsLeashEvading", args => _evading.Contains((uint)args[0]!));
        aiScript.On("TryStartCombatLeashEvade", args => _evadeStarts.Contains(((uint)args[0]!, (uint)args[1]!)));
        aiScript.On("IsInAggroRange", args => _aggroRange.Contains(((uint)args[0]!, (uint)args[1]!)));
        aiScript.On("StopCombatAfterProximityLoss", args =>
        {
            _combatStopped.Add((uint)args[0]!);
            return null;
        });
        var activity = ScriptedProxy.Create<IActivityService>(out var activityScript);
        activityScript.On("IsBusy", args => _busy.Contains((uint)args[0]!));
        var party = ScriptedProxy.Create<IPartyService>(out var partyScript);
        partyScript.On("IsInParty", args => _partyPairs.Contains(((uint)args[0]!, (uint)args[1]!)));
        var holoCom = ScriptedProxy.Create<IHoloComService>(out var holoScript);
        holoScript.On("IsHologram", args => _holograms.Contains((uint)args[0]!));
        var beast = ScriptedProxy.Create<IBeastMasteryService>(out var beastScript);
        beastScript.On("IsPlayerBeast", args => _playerBeasts.Contains((uint)args[0]!));

        _enmity = new TestEnmity(ai, activity, party, holoCom, beast);
    }

    [TearDown]
    public void TearDown()
    {
        _eventsScope.Dispose();
        _engineScope.Dispose();
        _clock.Dispose();
    }

    private List<string> ScriptsRun() =>
        _engine.CallsTo("ExecuteScript").Select(call => (string)call.Arguments[0]!).ToList();

    private void RunAssigned()
    {
        // Runs the commands the service handed to the engine, including those they queue in turn.
        var run = 0;
        while (run < _engine.CallsTo("AssignCommand").Count + _engine.CallsTo("ActionDoCommand").Count)
        {
            var assigned = _engine.CallsTo("AssignCommand");
            var queued = _engine.CallsTo("ActionDoCommand");
            var actions = assigned.Concat(queued).Select(call => (Action)call.Arguments.Last()!).ToList();
            var next = actions[run++];
            next();
        }
    }

    // ---- The pure rules ---------------------------------------------------------------------------------------------

    [TestCase(0, 0, 0)]
    [TestCase(-5, 10, 0)]
    [TestCase(100, 0, 100)]
    [TestCase(100, 25, 125)]
    [TestCase(100, -50, 50)]
    [TestCase(100, -500, 1)]
    [TestCase(1, -99, 1)]
    public void CalculateDamageEnmity_ScalesByTheAdjustmentAndNeverDropsBelowOne(int damage, int adjustment, int expected) =>
        _enmity.CalculateDamageEnmity(damage, adjustment).Should().Be(expected);

    [TestCase(100, 50, 150, 0, 50)]
    [TestCase(100, 50, 90, 0, 20)]
    [TestCase(100, 50, 150, 30, 35)]
    [TestCase(100, 50, 150, -30, 50)]
    [TestCase(100, 50, 50, 0, 0)]
    [TestCase(100, 50, 20, 0, 0)]
    [TestCase(-5, 50, 150, 0, 0)]
    [TestCase(100, 0, 150, 0, 0)]
    [TestCase(100, -10, 150, 0, 0)]
    public void CalculateHealingEnmity_HalvesTheHealingThatReallyLanded(int requested, int previous, int current, long nested, int expected) =>
        _enmity.CalculateHealingEnmity(requested, previous, current, nested).Should().Be(expected);

    [TestCase(0, 0, false)]
    [TestCase(100, 100, false)]
    [TestCase(124, 100, false)]
    [TestCase(125, 100, true)]
    [TestCase(50, 40, true)]
    [TestCase(49, 40, false)]
    [TestCase(10, 20, false)]
    [TestCase(1, 0, true)]
    public void IsThreatLeadSecure_RequiresTheSecureMargin(int holder, int next, bool expected) =>
        _enmity.IsThreatLeadSecure(holder, next).Should().Be(expected);

    [Test]
    public void ClampEnmityPercentAdjustment_KeepsTheAdjustmentWithinTheLimits()
    {
        _enmity.ClampEnmityPercentAdjustment(-900).Should().Be(-50);
        _enmity.ClampEnmityPercentAdjustment(900).Should().Be(50);
        _enmity.ClampEnmityPercentAdjustment(12).Should().Be(12);
    }

    [TestCase(10, 20, 12)]
    [TestCase(10, 900, 15)]
    [TestCase(10, -900, 5)]
    [TestCase(1, -50, 1)]
    [TestCase(0, 20, 0)]
    [TestCase(-10, 20, -10)]
    public void CalculateAdjustedEnmity_AdjustsGainsOnly(int amount, int adjustment, int expected) =>
        _enmity.CalculateAdjustedEnmity(amount, adjustment).Should().Be(expected);

    [TestCase(100, 80, 10, 100)]
    [TestCase(10, 80, 10, 90)]
    [TestCase(10, 80, 0, 81)]
    [TestCase(0, 0, 0, 1)]
    public void CalculateTauntEnmity_CatchesUpToTheRivalPlusALead(int current, int rival, int bonus, int expected) =>
        _enmity.CalculateTauntEnmity(current, rival, bonus).Should().Be(expected);

    [TestCase(100, 50, 50)]
    [TestCase(100, 100, 1)]
    [TestCase(100, 250, 1)]
    [TestCase(100, 0, 100)]
    [TestCase(3, 50, 1)]
    [TestCase(0, 50, 1)]
    public void CalculateReducedEnmity_RoundsTheReductionUpAndKeepsOne(int current, int percent, int expected) =>
        _enmity.CalculateReducedEnmity(current, percent).Should().Be(expected);

    // ---- Handlers ---------------------------------------------------------------------------------------------------

    [Test]
    public void CreatureDamaged_AddsTheDamageAsEnmityScaledByTheDamagersAdjustment()
    {
        _damage = 40;
        _enmity.StatAdjustments[(Player, StatType.DamageEnmityPercentAdjustment)] = 50;
        _enmity.StatAdjustments[(Player, StatType.EnmityPercentAdjustment)] = 0;

        _enmity.CreatureDamaged();

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(60);
    }

    [TestCase(0, Player)]
    [TestCase(-3, Player)]
    [TestCase(10, ObjectInvalid)]
    public void CreatureDamaged_IgnoresNoDamageAndNoDamager(int damage, uint damager)
    {
        _damage = damage;
        _lastDamager = damager;

        _enmity.CreatureDamaged();

        _enmity.GetEnmityTable(Enemy).Should().BeEmpty();
    }

    [Test]
    public void CreatureAttacked_AddsOneEnmityForTheAttacker()
    {
        _enmity.CreatureAttacked();

        _enmity.GetEnmityTable(Enemy).Should().Equal(new Dictionary<uint, int> { [Player] = 1 });
    }

    [Test]
    public void CreatureDeath_AndDestroyed_ClearTheEnemiesTablesAndItsOwnEnmity()
    {
        _enmity.ModifyEnmity(Player, Enemy, 10);
        _enmity.ModifyEnmity(Enemy, Enemy2, 5);
        _enmity.CreatureDeath();

        _enmity.GetEnmityTable(Enemy).Should().BeEmpty();
        _enmity.HasEnmity(Enemy).Should().BeFalse();

        _enmity.ModifyEnmity(Player, Enemy, 10);
        _enmity.CreatureDestroyed();

        _enmity.GetEnmityTable(Enemy).Should().BeEmpty();
        _enmity.HasEnmity(Player).Should().BeFalse();
    }

    [Test]
    public void PlayerDeath_AndExit_RemoveThePlayerFromEveryTable()
    {
        _enmity.ModifyEnmity(Player, Enemy, 10);
        _enmity.ModifyEnmity(Player, Enemy2, 10);
        _lastDied = Player;
        _enmity.PlayerDeath();
        _enmity.HasEnmity(Player).Should().BeFalse();

        _enmity.ModifyEnmity(Player, Enemy, 10);
        _exiting = Player;
        _enmity.PlayerExit();
        _enmity.GetEnmityTowardsAllEnemies(Player).Should().BeEmpty();
    }

    [Test]
    public void CreatureLimbo_ClearsEveryLimboedTarget()
    {
        _enmity.ModifyEnmity(Player, Enemy, 10);
        _enmity.ModifyEnmity(Player, Enemy2, 10);
        _eventData["NUM_TARGETS"] = "3";
        _eventData["TARGET_1"] = Enemy.ToString();
        _eventData["TARGET_2"] = ObjectInvalid.ToString();
        _eventData["TARGET_3"] = Player2.ToString();

        _enmity.CreatureLimbo();

        _enmity.GetEnmityTable(Enemy).Should().BeEmpty();
        _enmity.GetEnmityTable(Enemy2).Should().ContainKey(Player);
        _enmity.HasEnmity(Player).Should().BeTrue();
    }

    // ---- Healing -----------------------------------------------------------------------------------------------------

    private void Heal(uint healer, uint target, int requested, int before, int after)
    {
        _eventData["TARGET_OBJECT_ID"] = target.ToString();
        _eventData["HEAL_AMOUNT"] = requested.ToString();
        _self = healer;
        _hitPoints[target] = before;
        _enmity.HealingStarted();
        _hitPoints[target] = after;
        _enmity.HealingCompleted();
    }

    [Test]
    public void Healing_GivesTheHealerHalfTheRestoredHitPointsAsEnmityOnTheEnemiesFightingTheTarget()
    {
        _enmity.ModifyEnmity(Player2, Enemy, 50);
        _enmity.ModifyEnmity(Player2, Enemy2, 20);
        _enmity.ModifyProximityEnmity(Player2, 102, 4);

        Heal(Player, Player2, 40, 30, 70);

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(20);
        _enmity.GetEnmityTable(Enemy2)[Player].Should().Be(20);
        _enmity.GetEnmityTable(102).Should().NotContainKey(Player);
    }

    [Test]
    public void Healing_IgnoresAnInvalidTargetAnAmountThatCannotBeReadAndAnotherHealersObservation()
    {
        _enmity.ModifyEnmity(Player2, Enemy, 50);
        _eventData["TARGET_OBJECT_ID"] = ObjectInvalid.ToString();
        _enmity.HealingStarted();
        _enmity.HealingCompleted();

        _eventData["TARGET_OBJECT_ID"] = Player2.ToString();
        _eventData["HEAL_AMOUNT"] = "many";
        _self = Player;
        _hitPoints[Player2] = 30;
        _enmity.HealingStarted();
        _hitPoints[Player2] = 70;
        _enmity.HealingCompleted();

        _eventData["HEAL_AMOUNT"] = "40";
        _eventData["TARGET_OBJECT_ID"] = Player2.ToString();
        _self = Player;
        _enmity.HealingStarted();
        _self = Enemy2;
        _enmity.HealingCompleted();

        _enmity.GetEnmityTable(Enemy).Should().NotContainKey(Player);
    }

    [Test]
    public void Healing_CompletedWithoutAStart_DoesNothing()
    {
        _eventData["TARGET_OBJECT_ID"] = Player2.ToString();

        _enmity.HealingCompleted();

        _enmity.HasEnmity(Player).Should().BeFalse();
    }

    [Test]
    public void Healing_NestedHealingIsNotCountedTwice()
    {
        _enmity.ModifyEnmity(Player2, Enemy, 50);
        _eventData["TARGET_OBJECT_ID"] = Player2.ToString();
        _eventData["HEAL_AMOUNT"] = "40";

        _self = Player;
        _hitPoints[Player2] = 30;
        _enmity.HealingStarted();
        _self = Enemy2;
        _enmity.HealingStarted();
        _hitPoints[Player2] = 50;
        _self = Enemy2;
        _enmity.HealingCompleted();
        _hitPoints[Player2] = 70;
        _self = Player;
        _enmity.HealingCompleted();

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(10);
    }

    // ---- ModifyEnmity ------------------------------------------------------------------------------------------------

    [Test]
    public void ModifyEnmity_RecordsTheEnmityTellsTheEnemyToAttackAndRaisesTheScript()
    {
        _enmity.ModifyEnmity(Player, Enemy, 10);

        _enmity.GetEnmityTable(Enemy).Should().Equal(new Dictionary<uint, int> { [Player] = 10 });
        _enmity.GetEnmityTowardsAllEnemies(Player).Should().Equal(new Dictionary<uint, int> { [Enemy] = 10 });
        _enmity.HasEnmity(Player).Should().BeTrue();
        ScriptsRun().Should().Contain(ScriptName.OnEnmityChanged);
        _engine.CallsTo("AssignCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(Enemy);
    }

    [Test]
    public void ModifyEnmity_AppliesTheCreaturesAdjustmentAndTheStatusSourceAdjustmentOnce()
    {
        _enmity.StatAdjustments[(Player, StatType.EnmityPercentAdjustment)] = 20;
        var effect = ScriptedProxy.Create<IStatusEffect>(out var script);
        var group = new StatGroup();
        group.Stats[StatType.EnmityToStatusSourcePercentAdjustment] = 10;
        script.On("get_Source", _ => Player);
        script.On("get_StatGroup", _ => group);
        var other = ScriptedProxy.Create<IStatusEffect>(out var otherScript);
        otherScript.On("get_Source", _ => Player2);
        otherScript.On("get_StatGroup", _ => group);
        _enmity.Effects[Enemy] = new HashSet<IStatusEffect> { effect, other };

        _enmity.ModifyEnmity(Player, Enemy, 100);

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(130);
    }

    [Test]
    public void ModifyEnmity_ReductionsAreNotAdjustedAndEnmityNeverDropsBelowOne()
    {
        _enmity.StatAdjustments[(Player, StatType.EnmityPercentAdjustment)] = 50;
        _enmity.ModifyEnmity(Player, Enemy, 10);

        _enmity.ModifyEnmity(Player, Enemy, -4);
        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(11);

        _enmity.ModifyEnmity(Player, Enemy, -400);
        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(1);
    }

    [Test]
    public void ModifyEnmity_RefusesTheCasesWhereEnmityDoesNotApply()
    {
        void Refused(string why, Action arrange)
        {
            TearDown();
            SetUp();
            arrange();
            _enmity.ModifyEnmity(Player, Enemy, 10);
            _enmity.GetEnmityTable(Enemy).Should().BeEmpty(why);
        }

        _enmity.ModifyEnmity(ObjectInvalid, Enemy, 10);
        _enmity.ModifyEnmity(Player, ObjectInvalid, 10);
        _enmity.ModifyEnmity(Enemy, Enemy, 10);
        _enmity.GetEnmityTable(Enemy).Should().BeEmpty();
        Refused("different areas", () => _areas[Enemy] = AreaB);
        Refused("player enemy", () => _players.Add(Enemy));
        Refused("hologram creature", () => _holograms.Add(Player));
        Refused("hologram enemy", () => _holograms.Add(Enemy));
        Refused("leash evading", () => _evading.Add(Enemy));
        Refused("dead creature", () => _dead.Add(Player));
        Refused("dead enemy", () => _dead.Add(Enemy));
        Refused("party members", () => _partyPairs.Add((Player, Enemy)));
        Refused("starting a leash evade", () => _evadeStarts.Add((Enemy, Player)));
        Refused("associates of two players", () =>
        {
            _masters[Player] = Player2;
            _masters[Enemy] = Player;
        });
    }

    [Test]
    public void ModifyEnmity_RefusesPlayersAgainstPlayersAndAZeroAmount()
    {
        _players.Add(Enemy2);
        _enmity.ModifyEnmity(Player, Enemy2, 10);
        _enmity.ModifyEnmity(Player, Enemy, 0);

        _enmity.GetEnmityTable(Enemy2).Should().BeEmpty();
        _enmity.GetEnmityTable(Enemy).Should().BeEmpty();
    }

    [Test]
    public void ModifyEnmityOnAll_AppliesToEveryEnemyTheCreatureIsOn()
    {
        _enmity.ModifyEnmityOnAll(Player, 5);
        _enmity.ModifyEnmity(Player, Enemy, 10);
        _enmity.ModifyEnmity(Player, Enemy2, 20);

        _enmity.ModifyEnmityOnAll(Player, 0);
        _enmity.ModifyEnmityOnAll(Player, 5);

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(15);
        _enmity.GetEnmityTable(Enemy2)[Player].Should().Be(25);
    }

    // ---- Reductions and proximity -----------------------------------------------------------------------------------

    [Test]
    public void ReduceEnmity_CutsThePercentageAndScalesTheProximityShareDown()
    {
        _enmity.ModifyProximityEnmity(Player, Enemy, 10);
        _enmity.ModifyEnmity(Player, Enemy, 90);

        _enmity.ReduceEnmity(Player, Enemy, 50);

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(50);
        _enmity.HasProximityEnmity(Player, Enemy).Should().BeTrue();
        ScriptsRun().Count(script => script == ScriptName.OnEnmityChanged).Should().Be(3);
    }

    [Test]
    public void ReduceEnmity_IgnoresNonPositivePercentAndUnknownEntries()
    {
        _enmity.ModifyEnmity(Player, Enemy, 10);

        _enmity.ReduceEnmity(Player, Enemy, 0);
        _enmity.ReduceEnmity(Player2, Enemy, 50);
        _enmity.ReduceEnmity(Player, Enemy2, 50);

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(10);
    }

    [Test]
    public void ReduceEnmityOnAll_ReducesEveryTable()
    {
        _enmity.ModifyEnmity(Player, Enemy, 100);
        _enmity.ModifyEnmity(Player, Enemy2, 40);

        _enmity.ReduceEnmityOnAll(Player, 0);
        _enmity.ReduceEnmityOnAll(Player2, 50);
        _enmity.ReduceEnmityOnAll(Player, 50);

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(50);
        _enmity.GetEnmityTable(Enemy2)[Player].Should().Be(20);
    }

    [Test]
    public void ModifyProximityEnmity_TracksOnlyTheAmountThatWasApplied()
    {
        _enmity.ModifyProximityEnmity(Player, Enemy, 0);
        _enmity.HasProximityEnmity(Player, Enemy).Should().BeFalse();

        _enmity.ModifyProximityEnmity(Player, Enemy, 6);
        _enmity.ModifyProximityEnmity(Player, Enemy, 4);
        _evading.Add(Enemy2);
        _enmity.ModifyProximityEnmity(Player, Enemy2, 4);

        _enmity.HasProximityEnmity(Player, Enemy).Should().BeTrue();
        _enmity.HasProximityEnmity(Player, Enemy2).Should().BeFalse();
        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(10);
    }

    [Test]
    public void RemoveProximityEnmity_RemovesTheShareOrTheWholeEntry()
    {
        _enmity.ModifyProximityEnmity(Player, Enemy, 6);
        _enmity.ModifyEnmity(Player, Enemy, 4);
        _enmity.ModifyProximityEnmity(Player2, Enemy, 5);

        _enmity.RemoveProximityEnmity(Player, Enemy).Should().BeTrue();
        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(4);

        _enmity.RemoveProximityEnmity(Player2, Enemy).Should().BeTrue();
        _enmity.GetEnmityTable(Enemy).Should().NotContainKey(Player2);

        _enmity.RemoveProximityEnmity(Player, Enemy).Should().BeFalse();
        _enmity.RemoveProximityEnmity(Player2, Enemy2).Should().BeFalse();
    }

    [Test]
    public void RemoveEnmity_DropsTheEntryAndItsProximityTrackingWithoutRetargeting()
    {
        _enmity.ModifyProximityEnmity(Player, Enemy, 6);
        _engine.ClearCalls();

        _enmity.RemoveEnmity(Player, Enemy);

        _enmity.GetEnmityTable(Enemy).Should().BeEmpty();
        _enmity.HasProximityEnmity(Player, Enemy).Should().BeFalse();
        _enmity.HasEnmity(Player).Should().BeFalse();
        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    [Test]
    public void RemoveProximityEnmity_ForgetsTrackingWhenTheTableEntryIsGone()
    {
        _enmity.ModifyProximityEnmity(Player, Enemy, 6);
        _enmity.ModifyEnmity(Player2, Enemy, 6);
        _enmity.RemoveEnmityAndKeepProximity(Player, Enemy);

        _enmity.RemoveProximityEnmity(Player, Enemy).Should().BeFalse();
        _enmity.HasProximityEnmity(Player, Enemy).Should().BeFalse();
    }

    [Test]
    public void TheNonProximityQueries_SeparateRealCombatFromAggroAuraTraffic()
    {
        _enmity.ModifyProximityEnmity(Player, Enemy, 5);
        _enmity.ModifyEnmity(Player2, Enemy2, 5);

        _enmity.HasNonProximityEnmity(Player, Enemy).Should().BeFalse();
        _enmity.HasNonProximityEnmity(Player2, Enemy2).Should().BeTrue();
        _enmity.HasNonProximityEnmity(Enemy).Should().BeFalse();
        _enmity.HasNonProximityEnmity(Enemy2).Should().BeTrue();
        _enmity.HasNonProximityEnmity(999).Should().BeFalse();
        _enmity.HasNonProximityEnmityForCreature(Player).Should().BeFalse();
        _enmity.HasNonProximityEnmityForCreature(Player2).Should().BeTrue();
        _enmity.HasNonProximityEnmityForCreature(999).Should().BeFalse();
        _enmity.HasNonProximityEnmityOutsidePair(Player, Enemy).Should().BeFalse();
        _enmity.HasNonProximityEnmityOutsidePair(Player2, Enemy).Should().BeTrue();
    }

    [Test]
    public void ClearEnmityTable_RemovesTheEnemyEverywhere()
    {
        _enmity.ModifyEnmity(Player, Enemy, 5);
        _enmity.ModifyEnmity(Player, Enemy2, 5);
        _enmity.ModifyEnmity(Player2, Enemy, 5);
        _enmity.ModifyProximityEnmity(Player2, Enemy2, 3);

        _enmity.ClearEnmityTable(Enemy);
        _enmity.ClearEnmityTable(Enemy2);
        _enmity.ClearEnmityTable(999);

        _enmity.HasEnmity(Player).Should().BeFalse();
        _enmity.HasEnmity(Player2).Should().BeFalse();
        _enmity.GetEnmityTable(Enemy).Should().BeEmpty();
        _enmity.HasProximityEnmity(Player2, Enemy2).Should().BeFalse();
    }

    // ---- Targets -----------------------------------------------------------------------------------------------------

    [Test]
    public void GetHighestEnmityTarget_AnswersTheHighestAndInvalidForAnEmptyTable()
    {
        _enmity.GetHighestEnmityTarget(Enemy).Should().Be(ObjectInvalid);

        _enmity.ModifyEnmity(Player, Enemy, 5);
        _enmity.ModifyEnmity(Player2, Enemy, 9);

        _enmity.GetHighestEnmityTarget(Enemy).Should().Be(Player2);
    }

    [Test]
    public void GetHighestEnmityAttackTarget_SkipsThoseTheEnemyCannotAttack()
    {
        _enmity.ModifyEnmity(Player, Enemy, 5);
        _enmity.ModifyEnmity(Player2, Enemy, 9);

        _dead.Add(Player2);
        _enmity.GetHighestEnmityAttackTarget(Enemy).Should().Be(Player);
        _dead.Clear();
        _hitPoints[Player2] = 0;
        _enmity.GetHighestEnmityAttackTarget(Enemy).Should().Be(Player);
        _hitPoints.Clear();
        _areas[Player2] = AreaB;
        _enmity.GetHighestEnmityAttackTarget(Enemy).Should().Be(Player);
        _areas.Clear();
        _enmity.Hidden.Add((Enemy, Player2));
        _enmity.GetHighestEnmityAttackTarget(Enemy).Should().Be(Player);
        _enmity.Hidden.Add((Enemy, Player));
        _enmity.GetHighestEnmityAttackTarget(Enemy).Should().Be(ObjectInvalid);
    }

    [Test]
    public void IsThreatSecured_ComparesTheCreatureWithTheNextHighest()
    {
        _enmity.IsThreatSecured(Enemy, Player).Should().BeFalse();
        _enmity.ModifyEnmity(Player, Enemy, 100);
        _enmity.IsThreatSecured(Enemy, Player).Should().BeTrue();
        _enmity.IsThreatSecured(Enemy, Player2).Should().BeFalse();

        _enmity.ModifyEnmity(Player2, Enemy, 90);
        _enmity.IsThreatSecured(Enemy, Player).Should().BeFalse();
    }

    // ---- Taunts ------------------------------------------------------------------------------------------------------

    [Test]
    public void TryTaunt_CatchesUpToTheEligibleRivalPlusTheBonus()
    {
        _enmity.ModifyEnmity(Player2, Enemy, 100);
        _enmity.ModifyEnmity(Player, Enemy, 10);

        _enmity.TryTaunt(Player, Enemy, 20).Should().BeTrue();

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(120);
    }

    [Test]
    public void TryTaunt_DoesNotAccumulateWhileAlreadyAhead()
    {
        _enmity.ModifyEnmity(Player, Enemy, 200);
        _enmity.ModifyEnmity(Player2, Enemy, 50);

        _enmity.TryTaunt(Player, Enemy, 20).Should().BeTrue();

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(200);
    }

    [Test]
    public void TryTaunt_IgnoresRivalsTheEnemyCannotAttack()
    {
        _enmity.ModifyEnmity(Player2, Enemy, 500);
        _enmity.Hidden.Add((Enemy, Player2));
        _enmity.ModifyEnmity(Player, Enemy, 10);

        _enmity.TryTaunt(Player, Enemy, 20).Should().BeTrue();

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(20);
    }

    [Test]
    public void TryTaunt_RefusesInvalidTargetsAndEnemiesThatCannotBeTaunted()
    {
        void Refused(string why, Action arrange, int bonus = 20)
        {
            TearDown();
            SetUp();
            arrange();
            _enmity.TryTaunt(Player, Enemy, bonus).Should().BeFalse(why);
            _enmity.GetEnmityTable(Enemy).Should().BeEmpty(why);
        }

        Refused("no bonus", () => { }, 0);
                Refused("dead creature", () => _dead.Add(Player));
        Refused("dead enemy", () => _dead.Add(Enemy));
        Refused("player enemy", () => _players.Add(Enemy));
        Refused("enemy with a master", () => _masters[Enemy] = Player2);
        Refused("player beast", () => _playerBeasts.Add(Enemy));
        Refused("droid", () => _enmity.Droids.Add(Enemy));
        Refused("other area", () => _areas[Enemy] = AreaB);
        Refused("not hostile", () => _engine.SetReturnHandler("GetIsReactionTypeHostile", _ => false));
        Refused("no line of sight", () => _engine.SetReturnHandler("LineOfSightObject", _ => false));
        Refused("leash evading", () => _evading.Add(Enemy));
        Refused("starting a leash evade", () => _evadeStarts.Add((Enemy, Player)));
    }

    [Test]
    public void TryTaunt_FailsWhenTheEnmityCouldNotBeRegistered()
    {
        _partyPairs.Add((Player, Enemy));

        _enmity.TryTaunt(Player, Enemy, 20).Should().BeFalse();
    }

    // ---- Attack commands ---------------------------------------------------------------------------------------------

    [Test]
    public void AttackHighestEnmityTarget_SendsTheEnemyAfterItsTargetOnceAndRunsTheCommand()
    {
        _enmity.ModifyEnmity(Player, Enemy, 5);
        _enmity.ModifyEnmity(Player, Enemy, 5);
        _engine.CallsTo("AssignCommand").Should().ContainSingle("the second call is throttled by the recent command");

        _distances[(Enemy, Player)] = 30f;
        _enmity.WeaponSkill = SkillType.Rifle;
        RunAssigned();

        _engine.CountOf("ClearAllActions").Should().Be(1);
        _engine.CountOf("ActionMoveToObject").Should().Be(1);
        _engine.CallsTo("ActionAttack").Should().ContainSingle().Which.Arguments[0].Should().Be(Player);
    }

    [Test]
    public void IssueAttackCommand_StaysWhereItIsWhenTheTargetIsInRange()
    {
        _enmity.IssueAttackCommand(Enemy, Player, clearActions: false);
        _distances[(Enemy, Player)] = 1f;
        RunAssigned();

        _engine.CountOf("ClearAllActions").Should().Be(0);
        _engine.CountOf("ActionMoveToObject").Should().Be(0);
        _engine.CountOf("ActionAttack").Should().Be(1);
    }

    [Test]
    public void IssueAttackCommand_DoesNotMovePlayersAndDoesNotAttackWhatItCannotSee()
    {
        _enmity.IssueAttackCommand(Player, Enemy);
        _distances[(Player, Enemy)] = 30f;
        RunAssigned();
        _engine.CountOf("ActionMoveToObject").Should().Be(0);

        _engine.ClearCalls();
        _enmity.IssueAttackCommand(Enemy, Player);
        _enmity.Hidden.Add((Enemy, Player));
        RunAssigned();
        _engine.CountOf("ActionAttack").Should().Be(0);
    }

    [Test]
    public void IssueAttackCommand_RefusesInvalidTargetsOtherAreasDeniedCommandsAndLeashEvades()
    {
        _enmity.IssueAttackCommand(ObjectInvalid, Player);
        _enmity.IssueAttackCommand(Enemy, ObjectInvalid);
        _areas[Player] = AreaB;
        _enmity.IssueAttackCommand(Enemy, Player);
        _areas.Clear();
        _enmity.DeniedCommands.Add((Enemy, Player));
        _enmity.IssueAttackCommand(Enemy, Player);
        _enmity.DeniedCommands.Clear();
        _evadeStarts.Add((Enemy, Player));
        _enmity.IssueAttackCommand(Enemy, Player);

        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    [Test]
    public void TheQueuedCommand_IsDroppedWhenTheTargetChangedDiedLeftOrEvadesBeforeItRuns()
    {
        _enmity.IssueAttackCommand(Enemy, Player);
        _enmity.IssueAttackCommand(Enemy, Player2);
        var first = (Action)_engine.CallsTo("AssignCommand")[0].Arguments[1]!;
        first();
        _engine.CountOf("ClearAllActions").Should().Be(0, "a newer command replaced it");

        var second = (Action)_engine.CallsTo("AssignCommand")[1].Arguments[1]!;
        _evadeStarts.Add((Enemy, Player2));
        second();
        _engine.CountOf("ClearAllActions").Should().Be(0, "it starts a leash evade");
        _evadeStarts.Clear();

        second();
        var attack = (Action)_engine.CallsTo("ActionDoCommand").Single().Arguments[0]!;
        _dead.Add(Player2);
        attack();
        _dead.Clear();
        _areas[Player2] = AreaB;
        attack();
        _areas.Clear();
        _evadeStarts.Add((Enemy, Player2));
        attack();
        _engine.CountOf("ActionAttack").Should().Be(0);
    }

    [Test]
    public void AttackHighestEnmityTarget_LeavesABusyCreatureAndOneAlreadyAttackingItsTarget()
    {
        _enmity.ModifyEnmity(Player, Enemy, 5);
        _engine.ClearCalls();
        _enmity.ResumeAttackAfterActionsCleared(Enemy);
        _engine.CallsTo("AssignCommand").Should().ContainSingle("clearing the throttle lets a new command through");

        _engine.ClearCalls();
        _busy.Add(Enemy);
        _enmity.ResumeAttackAfterActionsCleared(Enemy);
        _engine.CountOf("AssignCommand").Should().Be(0);
        _busy.Clear();

        _attackTargets[Enemy] = Player;
        _actions[Enemy] = ActionType.AttackObject;
        _enmity.RecentAttack = true;
        _enmity.AttackHighestEnmityTarget(Enemy);
        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    [Test]
    public void AttackHighestEnmityTarget_RecoversAStaleAttackAndLogsIt()
    {
        _enmity.ModifyEnmity(Player, Enemy, 5);
        _engine.ClearCalls();
        _attackTargets[Enemy] = Player;
        _actions[Enemy] = ActionType.AttackObject;
        _enmity.RecentAttack = false;
        _now = Start.AddSeconds(30);

        _enmity.AttackHighestEnmityTarget(Enemy);

        _engine.CallsTo("AssignCommand").Should().ContainSingle();
        _enmity.Logs.Should().ContainSingle().Which.Should().Contain("recovered stale attack");
    }

    [Test]
    public void AttackHighestEnmityTarget_SwitchesWhenTheNativeTargetDiffersFromTheLeader()
    {
        _enmity.ModifyEnmity(Player, Enemy, 5);
        _engine.ClearCalls();
        _now = Start.AddSeconds(30);
        _attackTargets[Enemy] = Player2;
        _actions[Enemy] = ActionType.AttackObject;
        _enmity.RecentAttack = true;

        _enmity.AttackHighestEnmityTarget(Enemy);

        _engine.CallsTo("AssignCommand").Should().ContainSingle();
    }

    [Test]
    public void AttackHighestEnmityTarget_ReissuesAnApproachWhenTheLeaderChanges()
    {
        _enmity.ModifyEnmity(Player, Enemy, 5);
        _engine.ClearCalls();
        _enmity.ModifyEnmity(Player2, Enemy, 50);

        _engine.CallsTo("AssignCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(Enemy);
    }

    [Test]
    public void AttackHighestEnmityTarget_DropsAStaleProximityTargetAndStopsCombatWhenNothingIsLeft()
    {
        _enmity.ModifyProximityEnmity(Player, Enemy, 5);
        _engine.ClearCalls();

        _enmity.AttackHighestEnmityTarget(Enemy);

        _enmity.HasProximityEnmity(Player, Enemy).Should().BeFalse();
        _combatStopped.Should().Equal(Enemy);
        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    [Test]
    public void AttackHighestEnmityTarget_KeepsAProximityTargetStillInAggroRange()
    {
        _enmity.ModifyProximityEnmity(Player, Enemy, 5);
        _aggroRange.Add((Enemy, Player));
        _engine.ClearCalls();
        _enmity.ResumeAttackAfterActionsCleared(Enemy);

        _enmity.HasProximityEnmity(Player, Enemy).Should().BeTrue();
        _engine.CallsTo("AssignCommand").Should().ContainSingle();
    }

    [Test]
    public void AttackHighestEnmityTarget_HoldsWhileEveryTargetIsHiddenAndKeepsTheEnmity()
    {
        _enmity.ModifyEnmity(Player, Enemy, 5);
        _engine.ClearCalls();
        _enmity.Hidden.Add((Enemy, Player));
        _actions[Enemy] = ActionType.AttackObject;

        _enmity.AttackHighestEnmityTarget(Enemy);

        _engine.CallsTo("AssignCommand").Should().ContainSingle();
        RunAssigned();
        _engine.CountOf("ClearAllActions").Should().Be(1);
        _enmity.GetEnmityTable(Enemy).Should().ContainKey(Player);
    }

    [Test]
    public void HoldingForHiddenTargets_DoesNothingWhenBusyEvadingOrAlreadyIdle()
    {
        _enmity.ModifyEnmity(Player, Enemy, 5);
        _enmity.Hidden.Add((Enemy, Player));
        _engine.ClearCalls();

        _busy.Add(Enemy);
        _enmity.AttackHighestEnmityTarget(Enemy);
        _busy.Clear();
        _evading.Add(Enemy);
        _enmity.AttackHighestEnmityTarget(Enemy);
        _evading.Clear();
        _actions[Enemy] = ActionType.Invalid;
        _enmity.AttackHighestEnmityTarget(Enemy);

        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    [Test]
    public void AttackHighestEnmityTarget_ForACompanionFollowsTheAuthorizedTarget()
    {
        _enmity.Companions.Add(Enemy);
        _enmity.CompanionTargets[Enemy] = Player2;

        _enmity.AttackHighestEnmityTarget(Enemy);

        _engine.CallsTo("AssignCommand").Should().ContainSingle();
    }

    [Test]
    public void AttackHighestEnmityTarget_ForACompanionWithNoTargetStopsCombatOnlyAfterProximityLoss()
    {
        _enmity.Companions.Add(Enemy);
        _enmity.CompanionTargets[Enemy] = ObjectInvalid;

        _enmity.AttackHighestEnmityTarget(Enemy);

        _engine.CountOf("AssignCommand").Should().Be(0);
        _combatStopped.Should().BeEmpty();
    }

    [Test]
    public void AttackTarget_IsSkippedInAnotherAreaAndWhenALeashEvadeStarts()
    {
        _enmity.Companions.Add(Enemy);
        _enmity.CompanionTargets[Enemy] = Player;
        _areas[Player] = AreaB;
        _enmity.AttackHighestEnmityTarget(Enemy);
        _areas.Clear();
        _evadeStarts.Add((Enemy, Player));
        _enmity.AttackHighestEnmityTarget(Enemy);

        _engine.CountOf("AssignCommand").Should().Be(0);
    }

    [Test]
    public void ReevaluateEnemyAttackTargets_MakesEveryEnemyOnTheCreatureChooseAgain()
    {
        _enmity.ModifyEnmity(Player, Enemy, 5);
        _enmity.ModifyEnmity(Player, Enemy2, 5);
        _engine.ClearCalls();
        _enmity.ReevaluateEnemyAttackTargets(Player2);
        _engine.CountOf("AssignCommand").Should().Be(0);

        _enmity.ResumeAttackAfterActionsCleared(Enemy);
        _enmity.ResumeAttackAfterActionsCleared(Enemy2);
        _engine.ClearCalls();
        _enmity.ReevaluateEnemyAttackTargets(Player);

        _engine.CountOf("AssignCommand").Should().Be(0, "both enemies were commanded a moment ago");
    }

    [Test]
    public void RemoveCreatureEnmity_ForgetsTheCommandsThrottleAndRetargetsTheEnemies()
    {
        _enmity.ModifyEnmity(Player, Enemy, 5);
        _enmity.ModifyEnmity(Player2, Enemy, 5);
        _engine.ClearCalls();

        _enmity.RemoveCreatureEnmity(Player);

        _enmity.GetEnmityTable(Enemy).Should().NotContainKey(Player);
        _enmity.GetEnmityTable(Enemy).Should().ContainKey(Player2);
    }

    // ---- The table queries return copies ------------------------------------------------------------------------------

    [Test]
    public void GetEnmityTable_ReturnsACopy()
    {
        _enmity.ModifyEnmity(Player, Enemy, 5);

        _enmity.GetEnmityTable(Enemy)[Player] = 999;

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(5);
    }

    // ---- The test subclass -------------------------------------------------------------------------------------------

    private sealed class TestEnmity : EnmityService
    {
        public TestEnmity(
            IAIService ai,
            IActivityService activity,
            IPartyService party,
            IHoloComService holoCom,
            IBeastMasteryService beastMastery)
            : base(ai, activity, party, holoCom, beastMastery)
        {
        }

        public Dictionary<(uint Creature, StatType Stat), int> StatAdjustments { get; } = new();
        public HashSet<(uint Observer, uint Target)> Hidden { get; } = new();
        public HashSet<uint> Droids { get; } = new();
        public HashSet<uint> Companions { get; } = new();
        public Dictionary<uint, uint> CompanionTargets { get; } = new();
        public HashSet<(uint, uint)> DeniedCommands { get; } = new();
        public Dictionary<uint, HashSet<IStatusEffect>> Effects { get; } = new();
        public SkillType WeaponSkill { get; set; } = SkillType.Lightsaber;
        public bool RecentAttack { get; set; }
        public List<string> Logs { get; } = new();

        public void RemoveEnmityAndKeepProximity(uint creature, uint enemy)
        {
            var table = (Dictionary<uint, Dictionary<uint, int>>)typeof(EnmityService)
                .GetField("_enemyEnmityTables", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(this)!;
            table[enemy].Remove(creature);
        }

        protected override int GetStatAdjustment(uint creature, StatType stat) =>
            StatAdjustments.GetValueOrDefault((creature, stat));

        protected override bool IsHiddenFrom(uint observer, uint target) => Hidden.Contains((observer, target));

        protected override bool IsDroid(uint creature) => Droids.Contains(creature);

        protected override bool IsRegisteredCompanion(uint creature) => Companions.Contains(creature);

        protected override uint PeekAuthorizedTarget(uint companion) =>
            CompanionTargets.TryGetValue(companion, out var target) ? target : 0x7F000000;

        protected override bool CanIssueAttackCommand(uint companion, uint target) => !DeniedCommands.Contains((companion, target));

        protected override SkillType GetEquippedWeaponSkillType(uint creature) => WeaponSkill;

        protected override float GetWeaponEngagementRange(SkillType skillType) => IsRangedWeaponSkill(skillType) ? 10f : 1.5f;

        protected override bool IsRangedWeaponSkill(SkillType skillType) => skillType == SkillType.Rifle;

        protected override bool HasRecentAttackActivity(uint creature, float windowSeconds) => RecentAttack;

        protected override int CalculateAttackDelay(uint attacker) => 1500;

        protected override int CalculateEffectiveAttackDelay(int attackerDelayMilliseconds) => attackerDelayMilliseconds;

        protected override int CalculateAttackSwingDelay(int effectiveDelayMilliseconds) => effectiveDelayMilliseconds;

        protected override HashSet<IStatusEffect> GetAllStatusEffects(uint creature) =>
            Effects.GetValueOrDefault(creature) ?? new HashSet<IStatusEffect>();

        protected override void WriteAILog(string details) => Logs.Add(details);
    }
}
