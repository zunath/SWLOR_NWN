using System.Runtime.CompilerServices;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.Space;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The combat point instance: the trackers, the recent-enemy-level locals, the weapon-attack ledger that keeps a landed
/// hit from counting twice, the support points and the XP split when a creature dies. The engine and the events plugin
/// are recording fakes, the database an in-memory one, the converted neighbours scripted stand-ins, and the unconverted
/// skill, stat and space calls are answered by a subclass.
/// </summary>
[TestFixture]
public class CombatPointServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Player = 1001;
    private const uint Player2 = 1002;
    private const uint Dm = 1003;
    private const uint Npc = 2001;
    private const uint Npc2 = 2002;
    private const uint Beast = 3001;
    private const uint Weapon = 4001;
    private const uint Area = 500;
    private const uint OtherArea = 501;

    private static readonly DateTime Start = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);

    private FakeNWScript _engine = null!;
    private LocalVariables _locals = null!;
    private FakeEventsPlugin _events = null!;
    private InMemoryDBService _db = null!;
    private TestCombatPoint _points = null!;
    private IDisposable _clock = null!;
    private IDisposable _engineScope = null!;
    private IDisposable _eventsScope = null!;
    private DateTime _now;

    private uint _self;
    private uint _exiting;
    private uint _spellItem;
    private uint _spellTarget;
    private readonly HashSet<uint> _dead = new();
    private readonly Dictionary<uint, uint> _areas = new();
    private readonly Dictionary<(uint, uint), float> _distances = new();
    private readonly HashSet<uint> _playerBeasts = new();
    private readonly Dictionary<uint, Dictionary<uint, int>> _enmityOf = new();
    private readonly HashSet<(uint, uint)> _nonProximity = new();
    private List<uint> _partyMembers = new();

    [SetUp]
    public void SetUp()
    {
        _now = Start;
        _self = Player;
        _exiting = Player;
        _spellItem = Weapon;
        _spellTarget = Npc;
        _dead.Clear();
        _areas.Clear();
        _distances.Clear();
        _playerBeasts.Clear();
        _enmityOf.Clear();
        _nonProximity.Clear();
        _partyMembers = new List<uint>();

        _clock = Clock.OverrideForTests(() => _now);
        _engine = new FakeNWScript();
        _locals = new LocalVariables(_engine);
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid);
        _engine.SetReturnHandler("GetIsPC", args => (uint)args[0]! is Player or Player2 or Dm);
        _engine.SetReturnHandler("GetIsDM", args => (uint)args[0]! == Dm);
        _engine.SetReturnHandler("OBJECT_SELF", _ => _self);
        _engine.SetReturnHandler("GetExitingObject", _ => _exiting);
        _engine.SetReturnHandler("GetSpellCastItem", _ => _spellItem);
        _engine.SetReturnHandler("GetSpellTargetObject", _ => _spellTarget);
        _engine.SetReturnHandler("GetAssociate", args => (AssociateType)args[0]! == AssociateType.Henchman ? Beast : ObjectInvalid);
        _engine.SetReturnHandler("GetIsDead", args => _dead.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetCurrentHitPoints", _ => 100);
        _engine.SetReturnHandler("GetArea", args => _areas.GetValueOrDefault((uint)args[0]!, Area));
        _engine.SetReturnHandler("GetDistanceBetween", args => _distances.GetValueOrDefault(((uint)args[0]!, (uint)args[1]!), 5f));
        _engine.SetReturnHandler("GetObjectUUID", args => $"uuid-{(uint)args[0]!}");
        _engine.SetReturnHandler("ObjectToString", args => $"obj-{(uint)args[0]!}");
        _engineScope = NWScriptRuntime.Override(_engine);

        _events = new FakeEventsPlugin();
        _eventsScope = EventsPluginRuntime.Override(_events);

        _db = new InMemoryDBService()
            .With(NewPlayer("uuid-1001"))
            .With(NewPlayer("uuid-1002"));

        var enmity = ScriptedProxy.Create<IEnmityService>(out var enmityScript);
        enmityScript.On("GetEnmityTowardsAllEnemies", args =>
            _enmityOf.TryGetValue((uint)args[0]!, out var table) ? new Dictionary<uint, int>(table) : new Dictionary<uint, int>());
        enmityScript.On("HasNonProximityEnmity", args => _nonProximity.Contains(((uint)args[0]!, (uint)args[1]!)));
        var party = ScriptedProxy.Create<IPartyService>(out var partyScript);
        partyScript.On("GetAllPartyMembersWithinRange", _ => _partyMembers);
        var beast = ScriptedProxy.Create<IBeastMasteryService>(out var beastScript);
        beastScript.On("IsPlayerBeast", args => _playerBeasts.Contains((uint)args[0]!));

        _points = new TestCombatPoint(_db, enmity, party, beast);
    }

    [TearDown]
    public void TearDown()
    {
        _eventsScope.Dispose();
        _engineScope.Dispose();
        _clock.Dispose();
    }

    private static Player NewPlayer(string id)
    {
        var player = new Player { Id = id };
        player.Skills[SkillType.Pistol] = new PlayerSkill { Rank = 15 };
        player.Skills[SkillType.BeastMastery] = new PlayerSkill { Rank = 5 };
        player.Skills[SkillType.Mimicry] = new PlayerSkill { Rank = 10 };
        player.Skills[SkillType.Armor] = new PlayerSkill { Rank = 18 };
        return player;
    }

    private void Die(uint npc = Npc)
    {
        _self = npc;
        _points.OnCreatureDeath();
    }

    // ---- Adding and counting points ------------------------------------------------------------------------------------

    [Test]
    public void AddCombatPoint_TracksThePlayerAgainstTheCreatureAndStampsTheEnemyLevel()
    {
        _points.AddCombatPoint(Player, Npc, SkillType.Pistol);
        _points.AddCombatPoint(Player, Npc, SkillType.Pistol, 4);

        _points.HasCombatPoints(Player, Npc).Should().BeTrue();
        _points.HasCombatPoints(Player2, Npc).Should().BeFalse();
        _points.HasCombatPoints(Player, Npc2).Should().BeFalse();
        _points.GetTaggedCreatureCount(Player).Should().Be(1);
        _points.GetTaggedCreatureCount(Player2).Should().Be(-1);
        _locals.Int(Player, "COMBAT_POINT_LAST_NPC_LEVEL").Should().Be(20);
        _locals.String(Player, "COMBAT_POINT_LAST_NPC_EXPIRATION").Should().Be("2026-11-05 12:02:00");
    }

    [Test]
    public void AddCombatPoint_IgnoresNonPlayersDMsAndPlayerTargets()
    {
        _points.AddCombatPoint(Npc2, Npc, SkillType.Pistol);
        _points.AddCombatPoint(Dm, Npc, SkillType.Pistol);
        _points.AddCombatPoint(Player, Player2, SkillType.Pistol);
        _points.AddCombatPoint(Player, Dm, SkillType.Pistol);

        _points.GetTaggedCreatureCount(Player).Should().Be(-1);
        _points.HasCombatPoints(Player, Player2).Should().BeFalse();
    }

    [Test]
    public void AddPlayerToNPCReferenceToCache_IsIdempotent()
    {
        _points.AddPlayerToNPCReferenceToCache(Player, Npc);
        _points.AddPlayerToNPCReferenceToCache(Player, Npc);
        _points.AddPlayerToNPCReferenceToCache(Player, Npc2);

        _points.GetTaggedCreatureCount(Player).Should().Be(2);
    }

    [Test]
    public void AddCombatPointToAllTagged_AddsToEveryTaggedCreature()
    {
        _points.AddCombatPointToAllTagged(Player, SkillType.Pistol);
        _points.AddCombatPoint(Player, Npc, SkillType.Pistol);
        _points.AddCombatPoint(Player, Npc2, SkillType.Pistol);
        _points.AddCombatPointToAllTagged(Player, SkillType.Mimicry, 2);

        _points.HasCombatPoints(Player, Npc).Should().BeTrue();
        Die(Npc);
        _points.Awards.Should().Contain(award => award.Skill == SkillType.Mimicry);
    }

    // ---- The recent enemy level ----------------------------------------------------------------------------------------

    [Test]
    public void GetRecentEnemyLevel_ReadsTheLevelUntilTheTwoMinutesRunOut()
    {
        _points.GetRecentEnemyLevel(Player).Should().Be(-1);

        _points.AddCombatPoint(Player, Npc, SkillType.Pistol);
        _now = Start.AddSeconds(119);
        _points.GetRecentEnemyLevel(Player).Should().Be(20);

        _now = Start.AddSeconds(120);
        _points.GetRecentEnemyLevel(Player).Should().Be(-1);
    }

    [Test]
    public void ClearRecentEnemyLevel_RemovesBothLocals()
    {
        _points.AddCombatPoint(Player, Npc, SkillType.Pistol);

        _points.ClearRecentEnemyLevel(Player);

        _points.GetRecentEnemyLevel(Player).Should().Be(-1);
        _locals.Has(Player, "COMBAT_POINT_LAST_NPC_LEVEL").Should().BeFalse();
    }

    // ---- Weapon attacks --------------------------------------------------------------------------------------------------

    [Test]
    public void RecordWeaponAttackPoint_CreditsTheSkillAndTheBeastMasteryOfAPlayerBeast()
    {
        _playerBeasts.Add(Beast);

        _points.RecordWeaponAttackPoint(Player, Npc, Weapon, SkillType.Rifle);
        Die();

        _points.Awards.Select(award => award.Skill).Should().BeEquivalentTo(new[] { SkillType.Armor, SkillType.Rifle, SkillType.BeastMastery });
    }

    [Test]
    public void RecordWeaponAttackPoint_IgnoresInvalidActorsTargetsAndSkills()
    {
        _points.RecordWeaponAttackPoint(ObjectInvalid, Npc, Weapon, SkillType.Pistol);
        _points.RecordWeaponAttackPoint(Npc2, Npc, Weapon, SkillType.Pistol);
        _points.RecordWeaponAttackPoint(Dm, Npc, Weapon, SkillType.Pistol);
        _points.RecordWeaponAttackPoint(Player, ObjectInvalid, Weapon, SkillType.Pistol);
        _points.RecordWeaponAttackPoint(Player, Player2, Weapon, SkillType.Pistol);
        _points.RecordWeaponAttackPoint(Player, Dm, Weapon, SkillType.Pistol);
        _points.RecordWeaponAttackPoint(Player, Npc, Weapon, SkillType.Invalid);

        _points.GetTaggedCreatureCount(Player).Should().Be(-1);
    }

    [Test]
    public void RecordItemHitPoint_UsesTheItemsWeaponSkill()
    {
        _points.WeaponSkill = SkillType.Pistol;

        _points.RecordItemHitPoint(Player, Npc, Weapon);

        _points.HasCombatPoints(Player, Npc).Should().BeTrue();
    }

    [Test]
    public void OnHitCastSpell_CreditsAnItemHitOnceAndLetsACapturedNativeHitSuppressIt()
    {
        _self = Player;
        _points.OnHitCastSpell();
        _points.HasCombatPoints(Player, Npc).Should().BeTrue();

        _points.CaptureNativeWeaponAttackPoint(Player, Npc2, Weapon, SkillType.Pistol, rollId: 7);
        _spellTarget = Npc2;
        _points.OnHitCastSpell();

        _points.HasCombatPoints(Player, Npc2).Should().BeTrue("the capture credited it");
        Die(Npc2);
        _points.Awards.Count(award => award.Skill == SkillType.Pistol).Should().Be(1);
    }

    [TestCase(Dm)]
    [TestCase(Npc2)]
    public void OnHitCastSpell_IgnoresDMsAndNonPlayers(uint self)
    {
        _self = self;

        _points.OnHitCastSpell();

        _points.GetTaggedCreatureCount(Player).Should().Be(-1);
    }

    [TestCase(Player2)]
    [TestCase(Dm)]
    public void OnHitCastSpell_IgnoresPlayerTargets(uint target)
    {
        _spellTarget = target;

        _points.OnHitCastSpell();

        _points.GetTaggedCreatureCount(Player).Should().Be(-1);
    }

    [Test]
    public void CaptureNativeWeaponAttackPoint_CreditsARollOnlyOnce()
    {
        _points.CaptureNativeWeaponAttackPoint(Player, Npc, Weapon, SkillType.Pistol, rollId: 9);
        _points.CaptureNativeWeaponAttackPoint(Player, Npc, Weapon, SkillType.Pistol, rollId: 9);
        Die();

        _points.Awards.Where(award => award.Skill == SkillType.Pistol).Should().ContainSingle();
    }

    [Test]
    public void CaptureNativeWeaponAttackPoint_CanSuppressTheCallbackWithoutAPoint()
    {
        _points.CaptureNativeWeaponAttackPoint(Player, Npc, Weapon, SkillType.Pistol, awardPoint: false);

        _points.HasCombatPoints(Player, Npc).Should().BeFalse();
        _points.OnHitCastSpell();
        _points.HasCombatPoints(Player, Npc).Should().BeFalse("the pending capture swallowed the item hit");
    }

    [Test]
    public void CaptureNativeWeaponAttackPoint_IgnoresInvalidActorsAndTargets()
    {
        _points.CaptureNativeWeaponAttackPoint(Dm, Npc, Weapon, SkillType.Pistol);
        _points.CaptureNativeWeaponAttackPoint(Player, Player2, Weapon, SkillType.Pistol);
        _points.CaptureNativeWeaponAttackPoint(Player, Npc, Weapon, SkillType.Invalid);

        _points.GetTaggedCreatureCount(Player).Should().Be(-1);
    }

    [Test]
    public void ClearPendingWeaponAttackPoints_ForgetsTheLeavingPlayersPendingCaptures()
    {
        _points.CaptureNativeWeaponAttackPoint(Player, Npc, Weapon, SkillType.Pistol, awardPoint: false);
        _exiting = Player;

        _points.ClearPendingWeaponAttackPoints();
        _points.OnHitCastSpell();

        _points.HasCombatPoints(Player, Npc).Should().BeTrue();
    }

    // ---- Leaving ---------------------------------------------------------------------------------------------------------

    [Test]
    public void OnPlayerExit_RemovesThePlayersPointsEverywhere()
    {
        _points.AddCombatPoint(Player, Npc, SkillType.Pistol);
        _points.AddCombatPoint(Player2, Npc, SkillType.Pistol);
        _points.AddCombatPoint(Player, Npc2, SkillType.Pistol);
        _exiting = Player;

        _points.OnPlayerExit();

        _points.HasCombatPoints(Player, Npc).Should().BeFalse();
        _points.HasCombatPoints(Player, Npc2).Should().BeFalse();
        _points.HasCombatPoints(Player2, Npc).Should().BeTrue();
        _points.GetTaggedCreatureCount(Player).Should().Be(-1);
    }

    [TestCase(Dm)]
    [TestCase(Npc2)]
    public void OnPlayerExit_IgnoresDMsAndNonPlayers(uint exiting)
    {
        _points.AddCombatPoint(Player, Npc, SkillType.Pistol);
        _exiting = exiting;

        _points.OnPlayerExit();

        _points.HasCombatPoints(Player, Npc).Should().BeTrue();
    }

    [Test]
    public void OnPlayerExit_ForAPlayerWithoutPointsDoesNothing()
    {
        _exiting = Player2;

        _points.OnPlayerExit();

        _points.GetTaggedCreatureCount(Player2).Should().Be(-1);
    }

    // ---- Support points --------------------------------------------------------------------------------------------------

    [Test]
    public void AddSupportCombatPoint_CreditsTaggedCreaturesAndTheEnemiesOfTheSupportedOnes()
    {
        _points.AddCombatPoint(Player, Npc, SkillType.Pistol);
        _enmityOf[Player2] = new Dictionary<uint, int> { [Npc2] = 10, [Npc] = 5 };
        _nonProximity.Add((Player2, Npc2));

        _points.AddSupportCombatPoint(Player, new[] { Player2 }, SkillType.Mimicry);

        _points.HasCombatPoints(Player, Npc2).Should().BeTrue();
        _points.GetTaggedCreatureCount(Player).Should().Be(2);
    }

    [Test]
    public void AddSupportCombatPoint_SkipsEnemiesAlreadyCreditedForTheCast()
    {
        _enmityOf[Player2] = new Dictionary<uint, int> { [Npc2] = 10 };
        _nonProximity.Add((Player2, Npc2));
        var credited = new HashSet<uint> { Npc2 };

        _points.AddSupportCombatPoint(Player, new[] { Player2 }, SkillType.Mimicry, 1, credited);
        _points.HasCombatPoints(Player, Npc2).Should().BeFalse();

        credited.Clear();
        _points.AddSupportCombatPoint(Player, new[] { Player2 }, SkillType.Mimicry, 1, credited);
        _points.HasCombatPoints(Player, Npc2).Should().BeTrue();
        credited.Should().Contain(Npc2);
    }

    [Test]
    public void AddSupportCombatPoint_IgnoresNonPlayersAndDMs()
    {
        _enmityOf[Player2] = new Dictionary<uint, int> { [Npc2] = 10 };
        _nonProximity.Add((Player2, Npc2));

        _points.AddSupportCombatPoint(Npc, new[] { Player2 }, SkillType.Mimicry);
        _points.AddSupportCombatPoint(Dm, Player2, SkillType.Mimicry);

        _points.HasCombatPoints(Dm, Npc2).Should().BeFalse();
        _points.GetTaggedCreatureCount(Dm).Should().Be(-1);
    }

    [Test]
    public void AddSupportCombatPoint_ForOneCreatureWorksLikeAListOfOne()
    {
        _enmityOf[Player2] = new Dictionary<uint, int> { [Npc2] = 10 };
        _nonProximity.Add((Player2, Npc2));

        _points.AddSupportCombatPoint(Player, Player2, SkillType.Mimicry);

        _points.HasCombatPoints(Player, Npc2).Should().BeTrue();
    }

    [Test]
    public void AddReviveSupportCombatPoint_CreditsTheEnemiesFightingTheNearbyPartyMembers()
    {
        _partyMembers = new List<uint> { Player2 };
        _enmityOf[Player2] = new Dictionary<uint, int> { [Npc2] = 10 };
        _nonProximity.Add((Player2, Npc2));

        _points.AddReviveSupportCombatPoint(Player, Player2, SkillType.Mimicry);

        _points.HasCombatPoints(Player, Npc2).Should().BeTrue();
    }

    // ---- Death ---------------------------------------------------------------------------------------------------------

    [Test]
    public void OnCreatureDeath_SplitsTheCategoryXPByCombatPointsAndAlwaysGivesArmorXP()
    {
        _points.AddCombatPoint(Player, Npc, SkillType.Pistol, 3);
        _points.AddCombatPoint(Player, Npc, SkillType.BeastMastery, 1);

        Die();

        _points.Awards.Should().Equal(
            (Player, SkillType.Armor, 120, ObjectInvalid),
            (Player, SkillType.Pistol, 112, ObjectInvalid),
            (Player, SkillType.BeastMastery, 37, ObjectInvalid));
        _events.CallsTo("SignalEvent").Should().ContainSingle().Which.Arguments[1].Should().Be(Player);
        _events.CallsTo("PushEventData").Should().ContainSingle().Which.Arguments[1].Should().Be("obj-2001");
        _points.HasCombatPoints(Player, Npc).Should().BeFalse("the creature's points are cleared");
        _points.GetTaggedCreatureCount(Player).Should().Be(0);
    }

    [Test]
    public void OnCreatureDeath_ReportsTheMimicryNpcAsTheXPMessageSource()
    {
        _points.AddCombatPoint(Player, Npc, SkillType.Mimicry, 2);

        Die();

        _points.Awards.Should().Contain((Player, SkillType.Mimicry, 200, Npc));
    }

    [Test]
    public void OnCreatureDeath_AddsTheAreaBonus()
    {
        NWScript.SetLocalInt(Area, "AREA_XP_BONUS_PERCENTAGE", 50);
        _points.AddCombatPoint(Player, Npc, SkillType.Pistol, 1);

        Die();

        _points.Awards.Should().Contain((Player, SkillType.Pistol, 225, ObjectInvalid));
        _points.Awards.Should().Contain((Player, SkillType.Armor, 180, ObjectInvalid));
    }

    [Test]
    public void OnCreatureDeath_SkipsArmorXPForAPlayerInSpace()
    {
        _points.InSpace.Add(Player);
        _points.AddCombatPoint(Player, Npc, SkillType.Pistol, 1);

        Die();

        _points.Awards.Select(award => award.Skill).Should().Equal(SkillType.Pistol);
    }

    [Test]
    public void OnCreatureDeath_SharesNothingWithPlayersWhoAreDeadFarAwayOrElsewhere()
    {
        _points.AddCombatPoint(Player, Npc, SkillType.Pistol);
        _points.AddCombatPoint(Player2, Npc, SkillType.Pistol);
        _dead.Add(Player);
        Die();
        _points.Awards.Select(award => award.Player).Should().OnlyContain(player => player == Player2);

        _points.Awards.Clear();
        _dead.Clear();
        _points.AddCombatPoint(Player, Npc, SkillType.Pistol);
        _distances[(Player, Npc)] = 41f;
        _areas[Player2] = OtherArea;
        Die();
        _points.Awards.Should().BeEmpty();
    }

    [Test]
    public void OnCreatureDeath_WithNoPointsDoesNothing()
    {
        Die(Npc2);

        _points.Awards.Should().BeEmpty();
        _events.CountOf("SignalEvent").Should().Be(0);
    }

    [Test]
    public void OnCreatureDeath_SkipsCategoriesWhereThePlayerHasNoSkillWithPoints()
    {
        _db.Get<Player>("uuid-1001").Skills.Remove(SkillType.Pistol);
        _points.AddCombatPoint(Player, Npc, SkillType.Pistol);

        Die();

        _points.Awards.Select(award => award.Skill).Should().Equal(SkillType.Armor);
    }

    // ---- The test subclass -----------------------------------------------------------------------------------------------

    private sealed class TestCombatPoint : CombatPointService
    {
        public TestCombatPoint(IDBService db, IEnmityService enmity, IPartyService party, IBeastMasteryService beastMastery)
            : base(db, enmity, party, beastMastery, FacadeForwarder.For<ISkillService>(), FacadeForwarder.For<IStatService>(), FacadeForwarder.LazyFor<ISpaceService>())
        {
        }

        public SkillType WeaponSkill { get; set; } = SkillType.Pistol;
        public HashSet<uint> InSpace { get; } = new();
        public List<(uint Player, SkillType Skill, int Xp, uint Source)> Awards { get; } = new();

        protected override SkillType GetWeaponSkill(uint creature, uint weapon) => WeaponSkill;

        protected override SkillAttribute GetSkillDetails(SkillType skillType)
        {
            var details = (SkillAttribute)RuntimeHelpers.GetUninitializedObject(typeof(SkillAttribute));
            details.CombatPointCategory = skillType switch
            {
                SkillType.Armor => CombatPointCategoryType.Exempt,
                SkillType.Mimicry => CombatPointCategoryType.Utility,
                _ => CombatPointCategoryType.Weapon
            };
            return details;
        }

        protected override int GetDeltaXP(int delta) => 100 + delta * 10;

        protected override int GiveSkillXP(uint player, SkillType skill, int xp, uint xpMessageSource = 0x7F000000)
        {
            Awards.Add((player, skill, xp, xpMessageSource));
            return xp;
        }

        protected override NPCStats GetNPCStats(uint npc) => new() { Level = 20 };

        protected override bool IsPlayerInSpaceMode(uint player) => InSpace.Contains(player);
    }
}
