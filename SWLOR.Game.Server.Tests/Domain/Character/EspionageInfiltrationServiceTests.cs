using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Entity;
using DbPlayer = SWLOR.Game.Server.Entity.Player;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// Unit tests of the infiltration tracker: the engine is a fake world of one stealthed player and two hostile
/// creatures, the AI service is scripted, and the unconverted stealth, enmity, stat and skill services are the
/// instance's own seams.
/// </summary>
[TestFixture]
public class EspionageInfiltrationServiceTests
{
    private const uint Player = 1;
    private const uint Npc = 2;
    private const uint OtherNpc = 3;
    private const uint Dm = 4;
    private const uint AreaOne = 100;
    private const uint AreaTwo = 101;
    private const uint ObjectInvalid = 0x7F000000;

    private FakeNWScript _engine = null!;
    private FakeCreaturePlugin _creature = null!;
    private InMemoryDBService _db = null!;
    private ScriptedProxy _aiScript = null!;
    private BenchEspionage _service = null!;
    private readonly List<IDisposable> _scopes = new();
    private readonly Dictionary<nint, (uint Area, float X)> _locations = new();
    private readonly HashSet<uint> _dead = new();
    private readonly HashSet<uint> _invalid = new();
    private readonly Dictionary<uint, int> _hp = new();
    private float _playerX;
    private uint _playerArea;
    private bool _stealthed;
    private int _combatEntryWindow;
    private uint _master;
    private nint _nextLocation = 1000;
    private bool _inAggroRange;
    private bool _aiEnabled;

    [SetUp]
    public void SetUp()
    {
        _locations.Clear();
        _dead.Clear();
        _invalid.Clear();
        _hp.Clear();
        _playerX = 0f;
        _playerArea = AreaOne;
        _stealthed = true;
        _combatEntryWindow = 0;
        _master = ObjectInvalid;
        _inAggroRange = true;
        _aiEnabled = true;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid && !_invalid.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! is Player or Dm);
        _engine.SetReturnHandler("GetIsDM", arguments => (uint)arguments[0]! == Dm);
        _engine.SetReturnHandler("GetIsDead", arguments => _dead.Contains((uint)arguments[0]!));
        _engine.SetReturnHandler("GetCurrentHitPoints", arguments => _hp.GetValueOrDefault((uint)arguments[0]!, 50));
        _engine.SetReturnHandler("GetActionMode", _ => _stealthed);
        _engine.SetReturnHandler("GetLocalInt", _ => _combatEntryWindow);
        _engine.SetReturnHandler("GetIsEnemy", _ => true);
        _engine.SetReturnHandler("GetMaster", _ => _master);
        _engine.SetReturnHandler("GetObjectUUID", arguments => "id-" + arguments[0]);
        _engine.SetReturnHandler("GetArea", arguments => (uint)arguments[0]! == Player ? _playerArea : AreaOne);
        _engine.SetReturnHandler("GetLocation", arguments =>
        {
            var handle = _nextLocation++;
            _locations[handle] = ((uint)arguments[0]! == Player ? _playerArea : AreaOne, (uint)arguments[0]! == Player ? _playerX : 0f);
            return EngineStructures.Create<Location>(handle);
        });
        _engine.SetReturnHandler("GetAreaFromLocation", arguments => _locations[((Location)arguments[0]!).Handle].Area);
        _engine.SetReturnHandler("GetDistanceBetweenLocations", arguments =>
            Math.Abs(_locations[((Location)arguments[0]!).Handle].X - _locations[((Location)arguments[1]!).Handle].X));
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _creature = new FakeCreaturePlugin();
        _creature.SetReturnHandler("GetFaction", _ => EspionageInfiltration.HostileFactionId);
        _scopes.Add(CreaturePluginRuntime.Override(_creature));

        var ai = ScriptedProxy.Create<IAIService>(out _aiScript);
        _aiScript
            .On("IsInAggroRange", _ => _inAggroRange)
            .On("IsCreatureAIEnabled", _ => _aiEnabled);

        _db = new InMemoryDBService();
        var dbPlayer = new DbPlayer("id-" + Player);
        dbPlayer.Skills[SkillType.Espionage] = new PlayerSkill { Rank = 20 };
        _db.With(dbPlayer);
        _service = new BenchEspionage(_db, ai);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in Enumerable.Reverse(_scopes))
            scope.Dispose();
        _scopes.Clear();
    }

    private sealed class BenchEspionage : EspionageInfiltrationService
    {
        public BenchEspionage(IDBService db, IAIService ai)
            : base(db, ai, FacadeForwarder.For<IStealthService>(), FacadeForwarder.For<IEnmityService>(), FacadeForwarder.For<IStatService>(), FacadeForwarder.For<ISkillService>())
        {
        }

        public bool GrantedStealth { get; set; }
        public HashSet<uint> Clouded { get; } = new();
        public bool PairEnmity { get; set; }
        public bool UnrelatedEnmity { get; set; }
        public bool PlayerEnmity { get; set; }
        public bool NpcEnmity { get; set; }
        public int NpcLevel { get; set; } = 20;
        public List<(uint Player, SkillType Skill, int Xp)> XpAwards { get; } = new();

        protected override bool HasGrantedStealth(uint creature) => GrantedStealth;
        protected override bool IsStealthDetectionBlocked(uint observer) => Clouded.Contains(observer);
        protected override bool HasNonProximityEnmity(uint creature, uint enemy) => PairEnmity;
        protected override bool HasNonProximityEnmity(uint enemy) => NpcEnmity;
        protected override bool HasNonProximityEnmityForCreature(uint creature) => PlayerEnmity;
        protected override bool HasNonProximityEnmityOutsidePair(uint first, uint second) => UnrelatedEnmity;
        protected override NPCStats GetNPCStats(uint npc) => new() { Level = NpcLevel };
        protected override int GetDeltaXP(int delta) => delta >= 0 ? 100 + delta : 0;

        protected override int GiveSkillXP(uint player, SkillType skill, int xp, bool ignoreBonuses, bool applyHenchmanPenalty)
        {
            XpAwards.Add((player, skill, xp));
            return xp;
        }
    }

    // ---- beginning an attempt

    [Test]
    public void An_attempt_begins_for_a_stealthed_player_in_an_eligible_hostiles_aura_and_starts_a_sampler()
    {
        _service.TryBegin(Player, Npc).Should().BeTrue();

        _engine.CallsTo("DelayCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(1f);
        _service.TryBegin(Player, Npc).Should().BeTrue("an attempt already running is reported as started");
        _engine.CallsTo("DelayCommand").Should().ContainSingle("the sampler starts once per player");
    }

    [Test]
    public void A_second_attempt_for_the_same_player_shares_the_sampler()
    {
        _service.TryBegin(Player, Npc).Should().BeTrue();
        _service.TryBegin(Player, OtherNpc).Should().BeTrue();

        _engine.CallsTo("DelayCommand").Should().ContainSingle();
    }

    [Test]
    public void Invalid_objects_and_non_players_never_begin()
    {
        _invalid.Add(Npc);
        _service.TryBegin(Player, Npc).Should().BeFalse();
        _invalid.Clear();

        _service.TryBegin(ObjectInvalid, Npc).Should().BeFalse();
        _service.TryBegin(OtherNpc, Npc).Should().BeFalse("the player must be a player character");
        _service.TryBegin(Dm, Npc).Should().BeFalse("a DM never infiltrates");
        _service.TryBegin(Player, Player).Should().BeFalse("the hostile must not be a player");
        _service.TryBegin(Player, Dm).Should().BeFalse();
    }

    [Test]
    public void Dead_or_hurt_creatures_never_begin()
    {
        _dead.Add(Player);
        _service.TryBegin(Player, Npc).Should().BeFalse();
        _dead.Clear();

        _dead.Add(Npc);
        _service.TryBegin(Player, Npc).Should().BeFalse();
        _dead.Clear();

        _hp[Player] = 0;
        _service.TryBegin(Player, Npc).Should().BeFalse();
        _hp.Clear();

        _hp[Npc] = 0;
        _service.TryBegin(Player, Npc).Should().BeFalse();
    }

    [Test]
    public void Stealth_state_and_the_combat_entry_window_gate_the_attempt()
    {
        _stealthed = false;
        _service.TryBegin(Player, Npc).Should().BeFalse();
        _stealthed = true;

        _combatEntryWindow = 1;
        _service.TryBegin(Player, Npc).Should().BeFalse();
    }

    [Test]
    public void Granted_stealth_and_a_clouded_hostile_award_no_bypass()
    {
        _service.GrantedStealth = true;
        _service.TryBegin(Player, Npc).Should().BeFalse();
        _service.GrantedStealth = false;

        _service.Clouded.Add(Npc);
        _service.TryBegin(Player, Npc).Should().BeFalse();
    }

    [Test]
    public void Enmity_ai_faction_aggro_and_level_gate_the_attempt()
    {
        _service.PlayerEnmity = true;
        _service.TryBegin(Player, Npc).Should().BeFalse();
        _service.PlayerEnmity = false;

        _aiEnabled = false;
        _service.TryBegin(Player, Npc).Should().BeFalse();
        _aiEnabled = true;

        _creature.SetReturnHandler("GetFaction", _ => 2);
        _service.TryBegin(Player, Npc).Should().BeFalse();
        _creature.SetReturnHandler("GetFaction", _ => EspionageInfiltration.HostileFactionId);

        _inAggroRange = false;
        _service.TryBegin(Player, Npc).Should().BeFalse();
        _inAggroRange = true;

        _service.NpcEnmity = true;
        _service.TryBegin(Player, Npc).Should().BeFalse();
        _service.NpcEnmity = false;

        _service.NpcLevel = 0;
        _service.TryBegin(Player, Npc).Should().BeFalse();
    }

    [Test]
    public void A_hostile_owned_by_a_player_or_dm_is_not_eligible_but_an_npc_owner_is()
    {
        _master = Player;
        _service.TryBegin(Player, Npc).Should().BeFalse();
        _master = Dm;
        _service.TryBegin(Player, Npc).Should().BeFalse();
        _master = OtherNpc;
        _service.TryBegin(Player, Npc).Should().BeTrue();
    }

    // ---- detection

    [Test]
    public void An_undetected_verdict_on_a_new_pair_begins_and_qualifies_the_attempt()
    {
        _service.RecordDetection(Npc, Player, false);
        _playerX = 5f;
        _service.UpdateMovement(Player);

        _service.Complete(Player, Npc);

        _service.XpAwards.Should().Equal((Player, SkillType.Espionage, 100));
    }

    [Test]
    public void A_verdict_for_a_pair_that_cannot_begin_does_nothing()
    {
        _stealthed = false;

        _service.RecordDetection(Npc, Player, true);

        _service.XpAwards.Should().BeEmpty();
    }

    [Test]
    public void A_detected_verdict_pays_the_small_failure_award_once_per_pair()
    {
        _service.TryBegin(Player, Npc);

        _service.RecordDetection(Npc, Player, true);
        _service.XpAwards.Should().Equal((Player, SkillType.Espionage, 15));

        // The pair is resolved: it cannot start again and cannot pay again.
        _service.TryBegin(Player, Npc).Should().BeFalse();
        _service.RecordDetection(Npc, Player, true);
        _service.XpAwards.Should().HaveCount(1);
    }

    [Test]
    public void A_blinded_observer_ends_the_attempt_with_no_award_and_resolves_the_pair()
    {
        _service.TryBegin(Player, Npc);
        _service.Clouded.Add(Npc);

        _service.RecordDetection(Npc, Player, false);

        _service.XpAwards.Should().BeEmpty();
        _service.Clouded.Clear();
        _service.TryBegin(Player, Npc).Should().BeFalse("the pair is marked resolved");
    }

    [Test]
    public void A_verdict_outside_the_aggro_aura_changes_nothing()
    {
        _service.TryBegin(Player, Npc);
        _inAggroRange = false;

        _service.RecordDetection(Npc, Player, true);

        _service.XpAwards.Should().BeEmpty();
        _inAggroRange = true;
        _service.TryBegin(Player, Npc).Should().BeTrue("the attempt is still running");
    }

    [Test]
    public void Unrelated_enmity_or_a_player_initiated_fight_rejects_the_outcome_without_resolving()
    {
        _service.TryBegin(Player, Npc);
        _service.UnrelatedEnmity = true;
        _service.RecordDetection(Npc, Player, true);
        _service.XpAwards.Should().BeEmpty();
        _service.UnrelatedEnmity = false;
        _service.TryBegin(Player, Npc).Should().BeTrue("the rejected attempt was discarded, so a fresh one can start");

        _service.RecordPlayerCombatInitiation(Player);
        _service.RecordDetection(Npc, Player, true);
        _service.XpAwards.Should().BeEmpty();
    }

    [Test]
    public void Pair_enmity_rejects_an_undetected_verdict_but_not_a_detected_one()
    {
        _service.TryBegin(Player, Npc);
        _service.PairEnmity = true;
        _service.RecordDetection(Npc, Player, false);
        _service.XpAwards.Should().BeEmpty();

        _service.TryBegin(Player, Npc);
        _service.RecordDetection(Npc, Player, true);
        _service.XpAwards.Should().Equal((Player, SkillType.Espionage, 15));
    }

    // ---- completing

    [Test]
    public void Leaving_the_aura_after_evading_and_travelling_far_enough_pays_the_full_award_once()
    {
        _service.TryBegin(Player, Npc);
        _service.RecordDetection(Npc, Player, false);
        _playerX = 4.5f;

        _service.Complete(Player, Npc);
        _service.XpAwards.Should().Equal((Player, SkillType.Espionage, 100));

        // The attempt is gone and the pair resolved.
        _service.Complete(Player, Npc);
        _service.TryBegin(Player, Npc).Should().BeFalse();
        _service.XpAwards.Should().HaveCount(1);
    }

    [Test]
    public void Leaving_without_travelling_far_enough_or_without_evading_pays_nothing()
    {
        _service.TryBegin(Player, Npc);
        _service.RecordDetection(Npc, Player, false);
        _playerX = 3f;
        _service.Complete(Player, Npc);

        _service.TryBegin(Player, OtherNpc);
        _playerX = 9f;
        _service.Complete(Player, OtherNpc);

        _service.XpAwards.Should().BeEmpty();
    }

    [Test]
    public void Leaving_while_not_stealthed_or_in_combat_pays_nothing()
    {
        _service.TryBegin(Player, Npc);
        _service.RecordDetection(Npc, Player, false);
        _playerX = 9f;
        _stealthed = false;
        _service.Complete(Player, Npc);

        _stealthed = true;
        _service.TryBegin(Player, OtherNpc);
        _service.RecordDetection(OtherNpc, Player, false);
        _playerX = 20f;
        _service.PlayerEnmity = true;
        _service.Complete(Player, OtherNpc);

        _service.XpAwards.Should().BeEmpty();
    }

    [Test]
    public void Completing_when_either_side_is_gone_pays_nothing()
    {
        _service.TryBegin(Player, Npc);
        _service.RecordDetection(Npc, Player, false);
        _playerX = 9f;
        _invalid.Add(Npc);
        _service.Complete(Player, Npc);
        _invalid.Clear();

        _service.TryBegin(Player, OtherNpc);
        _dead.Add(OtherNpc);
        _service.Complete(Player, OtherNpc);
        _dead.Clear();

        _service.TryBegin(Player, Npc);
        _hp[Player] = 0;
        _service.Complete(Player, Npc);
        _hp.Clear();

        _service.TryBegin(Player, OtherNpc);
        _dead.Add(Player);
        _service.Complete(Player, OtherNpc);

        _service.XpAwards.Should().BeEmpty();
    }

    [Test]
    public void Completing_an_unknown_pair_does_nothing()
    {
        _service.Complete(Player, Npc);

        _service.XpAwards.Should().BeEmpty();
    }

    [Test]
    public void Granted_stealth_or_a_cloud_that_lands_late_cancels_the_award()
    {
        _service.TryBegin(Player, Npc);
        _service.GrantedStealth = true;

        _service.RecordDetection(Npc, Player, true);

        _service.XpAwards.Should().BeEmpty();
    }

    // ---- movement sampling

    [Test]
    public void Movement_in_another_area_does_not_extend_the_distance()
    {
        _service.TryBegin(Player, Npc);
        _service.RecordDetection(Npc, Player, false);
        _playerArea = AreaTwo;
        _playerX = 50f;
        _service.UpdateMovement(Player);
        _playerArea = AreaOne;
        _playerX = 1f;

        _service.Complete(Player, Npc);

        _service.XpAwards.Should().BeEmpty();
    }

    [Test]
    public void Movement_drops_attempts_whose_hostile_is_gone_dead_or_elsewhere()
    {
        _service.TryBegin(Player, Npc);
        _service.TryBegin(Player, OtherNpc);
        _invalid.Add(Npc);
        _service.UpdateMovement(Player);
        _invalid.Clear();
        _service.TryBegin(Player, Npc).Should().BeTrue("the dropped attempt can start again");

        _dead.Add(OtherNpc);
        _service.UpdateMovement(Player);
        _dead.Clear();

        _service.RecordDetection(Npc, Player, false);
        _playerArea = AreaTwo;
        _service.UpdateMovement(Player);
        _playerArea = AreaOne;
        _playerX = 9f;
        _service.Complete(Player, Npc);
        _service.XpAwards.Should().BeEmpty("the movement pass in another area dropped the attempt too");
    }

    [Test]
    public void The_sampler_repeats_while_attempts_exist_and_stops_when_they_end()
    {
        _service.TryBegin(Player, Npc);
        var first = _engine.CallsTo("DelayCommand").Single();

        ((Action)first.Arguments[1]!)();
        _engine.CallsTo("DelayCommand").Should().HaveCount(2, "an active attempt schedules the next sample");

        _service.CancelPlayer(Player);
        ((Action)_engine.CallsTo("DelayCommand")[1].Arguments[1]!)();
        _engine.CallsTo("DelayCommand").Should().HaveCount(2, "a cancelled player stops the sampler");
    }

    [Test]
    public void The_sampler_stops_when_the_player_is_gone_or_has_no_attempts()
    {
        _service.TryBegin(Player, Npc);
        var first = _engine.CallsTo("DelayCommand").Single();
        _invalid.Add(Player);

        ((Action)first.Arguments[1]!)();

        _engine.CallsTo("DelayCommand").Should().HaveCount(1);
        _invalid.Clear();

        // A new attempt starts a new sampler, and a stale sampler id is ignored.
        _service.CancelPlayer(Player);
        _service.TryBegin(Player, Npc);
        var second = _engine.CallsTo("DelayCommand")[1];
        _service.CancelPlayer(Player);
        _service.TryBegin(Player, Npc);
        ((Action)second.Arguments[1]!)();
        _engine.CallsTo("DelayCommand").Should().HaveCount(3, "only the newest sampler keeps running");

        // With no attempts left the sampler ends on its own.
        var third = _engine.CallsTo("DelayCommand")[2];
        _service.ClearNpc(Npc);
        ((Action)third.Arguments[1]!)();
        _engine.CallsTo("DelayCommand").Should().HaveCount(3);
    }

    // ---- cancelling and the handlers

    [Test]
    public void Cancelling_a_player_drops_their_attempts_and_clearing_a_hostile_forgets_its_resolved_players()
    {
        _service.TryBegin(Player, Npc);
        _service.RecordDetection(Npc, Player, true);
        _service.TryBegin(Player, Npc).Should().BeFalse("resolved");

        _service.ClearNpc(Npc);
        _service.TryBegin(Player, Npc).Should().BeTrue("the hostile's memory was cleared");

        _service.CancelPlayer(Player);
        _service.RecordDetection(Npc, Player, false);
        _service.XpAwards.Should().HaveCount(1);
    }

    [Test]
    public void The_exit_handler_cancels_only_players()
    {
        _service.TryBegin(Player, Npc);
        var exiting = Player;
        _engine.SetReturnHandler("GetExitingObject", _ => exiting);

        exiting = OtherNpc;
        _service.OnPlayerExit();
        _service.RecordDetection(Npc, Player, true);
        _service.XpAwards.Should().HaveCount(1, "a creature leaving cancels nothing");

        _service.ClearNpc(Npc);
        _service.TryBegin(Player, Npc);
        exiting = Player;
        _service.OnPlayerExit();
        _service.Complete(Player, Npc);
        _service.XpAwards.Should().HaveCount(1, "the player's attempt was cancelled");
    }

    [Test]
    public void The_death_handler_cancels_the_player_who_died()
    {
        _service.TryBegin(Player, Npc);
        _engine.SetReturnHandler("GetLastPlayerDied", _ => Player);

        _service.OnPlayerDeath();

        _playerX = 9f;
        _service.Complete(Player, Npc);
        _service.XpAwards.Should().BeEmpty();
    }

    [Test]
    public void The_removal_handler_clears_the_hostile_that_ran_it()
    {
        _service.TryBegin(Player, Npc);
        _service.RecordDetection(Npc, Player, true);
        _engine.SetReturn("OBJECT_SELF", Npc);

        _service.OnNpcRemoved();

        _service.TryBegin(Player, Npc).Should().BeTrue();
    }

    // ---- pure rules

    [TestCase(true, 4f, true, false, true)]
    [TestCase(false, 4f, true, false, false)]
    [TestCase(true, 3.99f, true, false, false)]
    [TestCase(true, 4f, false, false, false)]
    [TestCase(true, 4f, true, true, false)]
    public void Success_needs_evasion_distance_stealth_and_no_combat(bool evaded, float travelled, bool stealthed, bool combat, bool expected)
    {
        _service.MeetsSuccessRequirements(evaded, travelled, stealthed, combat).Should().Be(expected);
    }

    [TestCase(false, false, false, false, false)]
    [TestCase(true, false, false, false, false)]
    [TestCase(false, true, false, false, true)]
    [TestCase(false, false, false, true, true)]
    [TestCase(false, false, true, false, true)]
    [TestCase(true, false, true, false, false)]
    [TestCase(true, true, false, false, true)]
    public void An_outcome_is_rejected_after_player_combat_or_unrelated_enmity_or_pair_enmity_on_a_miss(
        bool detected, bool initiated, bool pair, bool unrelated, bool rejected)
    {
        // Parameter order of the rule is (detected, playerInitiatedCombat, hasPairCombatEnmity, hasUnrelatedCombatEnmity).
        _service.ShouldRejectDetectionOutcome(detected, initiated, pair, unrelated).Should().Be(rejected);
    }

    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, true, false)]
    public void Granted_stealth_and_clouded_hostiles_are_not_eligible(bool granted, bool clouded, bool eligible)
    {
        _service.IsBypassXpEligible(granted, clouded).Should().Be(eligible);
    }

    [Test]
    public void The_award_is_the_delta_xp_scaled_down_when_detected()
    {
        _service.CalculateXp(20, 20, false).Should().Be(100);
        _service.CalculateXp(20, 20, true).Should().Be(15);
        _service.CalculateXp(10, 20, false).Should().Be(0);
    }

    // ---- the facade

    [Test]
    public void The_facade_forwards_to_the_container_instance_and_repeats_the_constants()
    {
        using var container = new FacadeContainer(services =>
            Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.Replace(
                services,
                Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Singleton<IEspionageInfiltrationService>(_service)));
        using var scope = container.Install();

        EspionageInfiltration.RequiredTravelDistanceMeters.Should().Be(EspionageInfiltrationService.RequiredTravelDistanceMeters);
        EspionageInfiltration.DetectionFailureXpPercent.Should().Be(EspionageInfiltrationService.DetectionFailureXpPercent);
        EspionageInfiltration.HostileFactionId.Should().Be(EspionageInfiltrationService.HostileFactionId);

        EspionageInfiltration.TryBegin(Player, Npc).Should().BeTrue();
        EspionageInfiltration.RecordPlayerCombatInitiation(Player);
        EspionageInfiltration.RecordDetection(Npc, Player, true);
        EspionageInfiltration.UpdateMovement(Player);
        EspionageInfiltration.Complete(Player, Npc);
        EspionageInfiltration.CancelPlayer(Player);
        EspionageInfiltration.ClearNpc(Npc);
        EspionageInfiltration.MeetsSuccessRequirements(true, 4f, true, false).Should().BeTrue();
        EspionageInfiltration.ShouldRejectDetectionOutcome(false, true, false, false).Should().BeTrue();
        EspionageInfiltration.IsBypassXpEligible(false, false).Should().BeTrue();
        EspionageInfiltration.CalculateXp(20, 20, false).Should().Be(100);

        _engine.SetReturnHandler("GetExitingObject", _ => OtherNpc);
        _engine.SetReturnHandler("GetLastPlayerDied", _ => Player);
        _engine.SetReturn("OBJECT_SELF", Npc);
        EspionageInfiltration.OnPlayerExit();
        EspionageInfiltration.OnPlayerDeath();
        EspionageInfiltration.OnNpcRemoved();
    }
}
