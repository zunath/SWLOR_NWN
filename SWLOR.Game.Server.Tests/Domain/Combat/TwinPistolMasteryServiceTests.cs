using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The Twin Pistol mastery instance: the trial gates (player, area, active quest, pistol pair), the twelve-hit and the
/// three-ability proofs, and the clear paths. The database is in memory, the key item and quest services are scripted,
/// and the engine is a recording fake.
/// </summary>
[TestFixture]
public class TwinPistolMasteryServiceTests
{
    private const uint Actor = 1001;
    private const uint Target = 2002;
    private const uint Invalid = 0x7F000000;
    private const string TrialArea = "pw_sc_canyonrng";
    private const string TwinSuns = "twin_suns_mastery";
    private const string FinalSalvo = "final_salvo_mastery";

    private readonly List<IDisposable> _scopes = new();
    private FakeNWScript _engine = null!;
    private InMemoryDBService _db = null!;
    private ScriptedProxy _keyItems = null!;
    private ScriptedProxy _quests = null!;
    private TestMastery _mastery = null!;
    private bool _isPc;
    private bool _isDm;
    private bool _isDead;
    private bool _hasPair;
    private string _areaResref = null!;
    private bool _enemy;
    private uint _targetArea;
    private bool _targetIsPc;

    [SetUp]
    public void SetUp()
    {
        _isPc = true;
        _isDm = false;
        _isDead = false;
        _hasPair = true;
        _enemy = true;
        _areaResref = TrialArea;
        _targetArea = 500;
        _targetIsPc = false;
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != Invalid);
        _engine.SetReturnHandler("GetIsPC", args => (uint)args[0]! == Target ? _targetIsPc : _isPc);
        _engine.SetReturnHandler("GetIsDM", _ => _isDm);
        _engine.SetReturnHandler("GetIsDead", _ => _isDead);
        _engine.SetReturnHandler("GetIsEnemy", _ => _enemy);
        _engine.SetReturnHandler("GetArea", args => (uint)args[0]! == Target ? _targetArea : 500u);
        _engine.SetReturnHandler("GetResRef", _ => _areaResref);
        _engine.SetReturnHandler("GetObjectUUID", args => $"uuid-{(uint)args[0]!}");
        _scopes.Add(NWScriptRuntime.Override(_engine));
        _db = new InMemoryDBService().With(PlayerWithQuests(TwinSuns, FinalSalvo));
        var keyItem = ScriptedProxy.Create<IKeyItemService>(out _keyItems);
        _keyItems.On("GiveKeyItem", _ => null);
        var quest = ScriptedProxy.Create<IQuestService>(out _quests);
        _quests.On("AdvanceQuest", _ => true);
        _mastery = new TestMastery(_db, keyItem, quest, () => _hasPair);
    }

    [TearDown]
    public void TearDown()
    {
        for (var index = _scopes.Count - 1; index >= 0; index--) _scopes[index].Dispose();
        _scopes.Clear();
    }

    private static Player PlayerWithQuests(params string[] questIds)
    {
        var player = new Player { Id = $"uuid-{Actor}" };
        foreach (var id in questIds) player.Quests[id] = new PlayerQuest { CurrentState = 1 };
        return player;
    }

    [Test]
    public void SixMainAndSixCompanionHitsOnOneTargetAwardTheTwinSunsProof()
    {
        for (var hit = 0; hit < 6; hit++) _mastery.RecordOrdinaryHit(Actor, Target, false);
        _keyItems.Calls.Should().BeEmpty("six main hits alone are not enough");
        for (var hit = 0; hit < 6; hit++) _mastery.RecordOrdinaryHit(Actor, Target, true);

        _keyItems.Calls.Should().ContainSingle().Which.Arguments.Should().Equal(Actor, KeyItemType.TwinSunsCanyonFiringTrace);
        _quests.Calls.Should().ContainSingle().Which.Arguments.Should().Equal(Actor, Actor, TwinSuns);
    }

    [Test]
    public void AHitOutsideTheTrialIsNotCounted()
    {
        _areaResref = "somewhere_else";
        Twelve();
        _areaResref = TrialArea;
        _isDm = true;
        Twelve();
        _isDm = false;
        _isDead = true;
        Twelve();
        _isDead = false;
        _isPc = false;
        Twelve();
        _isPc = true;
        _hasPair = false;
        Twelve();
        _hasPair = true;
        _enemy = false;
        Twelve();
        _enemy = true;
        _targetIsPc = true;
        Twelve();
        _targetIsPc = false;
        _targetArea = 501;
        Twelve();
        _targetArea = 500;

        _keyItems.Calls.Should().BeEmpty();
    }

    [Test]
    public void ThePlayerNeedsTheTrialQuestInItsFirstState()
    {
        var player = PlayerWithQuests(TwinSuns);
        player.Quests[TwinSuns].CurrentState = 2;
        _db.With(player);
        Twelve();
        player.Quests[TwinSuns].CurrentState = 1;
        player.Quests[TwinSuns].DateLastCompleted = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        Twelve();
        player.Quests.Remove(TwinSuns);
        Twelve();
        _db = new InMemoryDBService();
        var missing = new TestMastery(_db, ScriptedProxy.Create<IKeyItemService>(out _), ScriptedProxy.Create<IQuestService>(out _), () => true);
        for (var hit = 0; hit < 12; hit++) missing.RecordOrdinaryHit(Actor, Target, hit % 2 == 0);

        _keyItems.Calls.Should().BeEmpty();
    }

    [Test]
    public void ChangingTargetRestartsTheCount()
    {
        for (var hit = 0; hit < 5; hit++) _mastery.RecordOrdinaryHit(Actor, Target, false);
        _mastery.RecordTargetChange(Actor, Target + 1);
        _mastery.RecordTargetChange(Actor + 9, Target);
        for (var hit = 0; hit < 6; hit++) _mastery.RecordOrdinaryHit(Actor, Target, false);
        for (var hit = 0; hit < 5; hit++) _mastery.RecordOrdinaryHit(Actor, Target, true);

        _keyItems.Calls.Should().BeEmpty("the earlier hits were on another target");
    }

    [Test]
    public void ClearingTheTrialStateDropsTheProgress()
    {
        for (var hit = 0; hit < 6; hit++) _mastery.RecordOrdinaryHit(Actor, Target, false);
        for (var hit = 0; hit < 5; hit++) _mastery.RecordOrdinaryHit(Actor, Target, true);

        _mastery.ClearTrialState(Actor);
        _mastery.RecordOrdinaryHit(Actor, Target, true);

        _keyItems.Calls.Should().BeEmpty();
    }

    [Test]
    public void TempoLeadAndClosingShotsInOrderAwardTheFinalSalvoProof()
    {
        _mastery.RecordAbility(Actor, true, PerkType.TempoShot, 0);
        _mastery.RecordAbility(Actor, true, PerkType.LeadShot, 0);
        _keyItems.Calls.Should().BeEmpty();
        _mastery.RecordAbility(Actor, true, PerkType.ClosingShot, 2);

        _keyItems.Calls.Should().ContainSingle().Which.Arguments.Should().Equal(Actor, KeyItemType.FinalSalvoCanyonRouteTrace);
        _quests.Calls.Should().ContainSingle().Which.Arguments.Should().Equal(Actor, Actor, FinalSalvo);
    }

    [Test]
    public void AClosingShotWithoutTheSequenceOrTheCadenceDoesNotCount()
    {
        _mastery.RecordAbility(Actor, true, PerkType.ClosingShot, 5);
        _mastery.RecordAbility(Actor, true, PerkType.TempoShot, 0);
        _mastery.RecordAbility(Actor, true, PerkType.LeadShot, 0);
        _mastery.RecordAbility(Actor, true, PerkType.ClosingShot, 1);
        _mastery.RecordAbility(Actor, false, PerkType.ClosingShot, 5);

        _isDm = true;
        _mastery.RecordAbility(Actor, true, PerkType.ClosingShot, 5);

        _keyItems.Calls.Should().BeEmpty();
    }

    [Test]
    public void TheAbilityTrialDoesNotNeedThePistolPair()
    {
        _hasPair = false;
        _mastery.RecordAbility(Actor, true, PerkType.TempoShot, 0);
        _mastery.RecordAbility(Actor, true, PerkType.LeadShot, 0);
        _mastery.RecordAbility(Actor, true, PerkType.ClosingShot, 3);

        _keyItems.Calls.Should().ContainSingle();
    }

    private void Twelve()
    {
        for (var hit = 0; hit < 12; hit++) _mastery.RecordOrdinaryHit(Actor, Target, hit % 2 == 0);
    }

    private sealed class TestMastery : TwinPistolMasteryService
    {
        private readonly Func<bool> _hasPair;

        public TestMastery(InMemoryDBService db, IKeyItemService keyItem, IQuestService quest, Func<bool> hasPair)
            : base(db, keyItem, quest)
        {
            _hasPair = hasPair;
        }

        protected override bool HasTwinPistol(uint actor) => _hasPair();
    }
}
