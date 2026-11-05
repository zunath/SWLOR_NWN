using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>
/// The quick-cast instance: the saved flags, the displayed feat, the feat and quick bar rewrite, the handlers and the
/// automatic cast. The engine and the NWNX creature and player plugins are recording fakes, the database an in-memory
/// one, and the unconverted <c>Ability</c> registry and feature calls are answered by a subclass.
/// </summary>
[TestFixture]
public class QuickCastServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Player = 1001;
    private const uint Enemy = 2002;
    private const uint Area = 500;

    private readonly List<IDisposable> _scopes = new();
    private FakeNWScript _engine = null!;
    private FakeCreaturePlugin _creature = null!;
    private FakePlayerPlugin _playerPlugin = null!;
    private InMemoryDBService _db = null!;
    private TestQuickCast _quickCast = null!;
    private FeatType _alias;
    private FeatType _original;
    private readonly HashSet<FeatType> _known = new();
    private readonly Dictionary<int, QuickBarSlot> _bar = new();
    private bool _isDm;
    private bool _enemyDead;
    private uint _enemyArea = Area;
    private bool _enemyHostile = true;
    private int _enemyHitPoints = 50;

    [SetUp]
    public void SetUp()
    {
        (_alias, _original) = QuickCastFeatMap.Originals.First();
        _known.Clear();
        _bar.Clear();
        _isDm = false;
        _enemyDead = false;
        _enemyArea = Area;
        _enemyHostile = true;
        _enemyHitPoints = 50;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid);
        _engine.SetReturn("GetIsPC", true);
        _engine.SetReturnHandler("GetIsDM", _ => _isDm);
        _engine.SetReturn("GetIsDMPossessed", false);
        _engine.SetReturnHandler("GetObjectUUID", args => $"uuid-{(uint)args[0]!}");
        _engine.SetReturn("GetEnteringObject", Player);
        _engine.SetReturn("OBJECT_SELF", Player);
        _engine.SetReturnHandler("GetIsDead", args => (uint)args[0]! == Enemy && _enemyDead);
        _engine.SetReturnHandler("GetCurrentHitPoints", args => (uint)args[0]! == Enemy ? _enemyHitPoints : 100);
        _engine.SetReturnHandler("GetArea", args => (uint)args[0]! == Enemy ? _enemyArea : Area);
        _engine.SetReturnHandler("GetIsReactionTypeHostile", _ => _enemyHostile);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _creature = new FakeCreaturePlugin();
        _creature.SetReturnHandler("GetKnowsFeat", args => _known.Contains((FeatType)args[1]!));
        _scopes.Add(CreaturePluginRuntime.Override(_creature));

        _playerPlugin = new FakePlayerPlugin();
        _playerPlugin.SetReturnHandler("GetQuickBarSlot", args =>
            _bar.TryGetValue((int)args[1]!, out var slot) ? slot : new QuickBarSlot { ObjectType = QuickBarSlotType.Empty });
        _scopes.Add(PlayerPluginRuntime.Override(_playerPlugin));

        _db = new InMemoryDBService().With(new Player { Id = "uuid-1001", Settings = new PlayerSettings() });
        _quickCast = new TestQuickCast(_db);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
    }

    private PlayerSettings Settings => _db.Get<Player>("uuid-1001").Settings;

    private static AbilityDetail Ability(bool hostile) => new()
    {
        ActivationType = AbilityActivationType.Casted,
        IsSingleTargetAbility = true,
        RequiresTarget = true,
        IsHostileAbility = hostile,
        AllowsSelfQuickCast = true
    };

    [Test]
    public void GetOriginalFeat_MapsAnAliasAndLeavesOtherFeatsAlone()
    {
        _quickCast.GetOriginalFeat(_alias).Should().Be(_original);
        _quickCast.GetOriginalFeat(_original).Should().Be(_original);
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void IsEnabled_ReadsTheSavedFlagsOfAPlayer(bool self, bool enemy)
    {
        Settings.SelfQuickCastEnabled = self;
        Settings.EnemyQuickCastEnabled = enemy;

        _quickCast.IsEnabled(Player, QuickCastTargetType.Self).Should().Be(self);
        _quickCast.IsEnabled(Player, QuickCastTargetType.CurrentEnemy).Should().Be(enemy);
        _quickCast.IsEnabled(Player, (QuickCastTargetType)(-1)).Should().BeFalse();
    }

    [Test]
    public void IsEnabled_IsFalseForMissingSettings()
    {
        _quickCast.IsEnabled((PlayerSettings)null!, QuickCastTargetType.Self).Should().BeFalse();
        _quickCast.IsEnabled((PlayerSettings)null!, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
    }

    [Test]
    public void IsEnabled_IsFalseForObjectsThatAreNotPlayerCharacters()
    {
        Settings.SelfQuickCastEnabled = true;

        _engine.SetReturnHandler("GetIsObjectValid", _ => false);
        _quickCast.IsEnabled(Player, QuickCastTargetType.Self).Should().BeFalse("an invalid object has no settings");

        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid);
        _engine.SetReturn("GetIsPC", false);
        _quickCast.IsEnabled(Player, QuickCastTargetType.Self).Should().BeFalse("an NPC has no settings");

        _engine.SetReturn("GetIsPC", true);
        _isDm = true;
        _quickCast.IsEnabled(Player, QuickCastTargetType.Self).Should().BeFalse("a DM has no settings");

        _isDm = false;
        _engine.SetReturn("GetIsDMPossessed", true);
        _quickCast.IsEnabled(Player, QuickCastTargetType.Self).Should().BeFalse("a possessed avatar has no settings");

        _engine.SetReturn("GetIsDMPossessed", false);
        _engine.SetReturnHandler("GetObjectUUID", _ => "  ");
        _quickCast.IsEnabled(Player, QuickCastTargetType.Self).Should().BeFalse("a player without an id has no settings");
    }

    [Test]
    public void IsEnabled_IsFalseWhenThePlayerHasNoRecord()
    {
        _engine.SetReturnHandler("GetObjectUUID", _ => "uuid-unknown");

        _quickCast.IsEnabled(Player, QuickCastTargetType.Self).Should().BeFalse();
    }

    [Test]
    public void UsesAutomaticTarget_NeedsSettingsARegisteredAbilityAndAnEnabledEligibleType()
    {
        var settings = new PlayerSettings { SelfQuickCastEnabled = true };

        _quickCast.UsesAutomaticTarget(_original, null!).Should().BeFalse();
        _quickCast.UsesAutomaticTarget(_original, settings).Should().BeFalse("the ability is not registered");

        _quickCast.Abilities[_original] = Ability(hostile: false);
        _quickCast.UsesAutomaticTarget(_original, settings).Should().BeTrue();
        _quickCast.UsesAutomaticTarget(_original, new PlayerSettings()).Should().BeFalse("nothing is enabled");
        _quickCast.UsesAutomaticTarget(_original, new PlayerSettings { EnemyQuickCastEnabled = true })
            .Should().BeFalse("a support ability is not eligible for the current enemy");

        _quickCast.Abilities[_original] = Ability(hostile: true);
        _quickCast.UsesAutomaticTarget(_original, new PlayerSettings { EnemyQuickCastEnabled = true }).Should().BeTrue();
        _quickCast.UsesAutomaticTarget(_original, settings).Should().BeFalse("a hostile ability is not eligible for self");
    }

    [Test]
    public void GetDisplayedFeat_ShowsTheAliasOnlyWhenAutomaticTargetingApplies()
    {
        _quickCast.Abilities[_original] = Ability(hostile: false);

        _quickCast.GetDisplayedFeat(Player, _alias).Should().Be(_original);

        Settings.SelfQuickCastEnabled = true;
        _quickCast.GetDisplayedFeat(Player, _original).Should().Be(_alias);
        _quickCast.GetDisplayedFeat(Player, _alias).Should().Be(_alias);

        var plain = Enum.GetValues<FeatType>().First(f => !QuickCastFeatMap.IsAlias(f) && !QuickCastFeatMap.TryGetAlias(f, out _));
        _quickCast.GetDisplayedFeat(Player, plain).Should().Be(plain, "a feat with no alias is shown as itself");
    }

    [Test]
    public void KnowsFeat_AcceptsEitherRepresentation()
    {
        _quickCast.KnowsFeat(Player, _original).Should().BeFalse();

        _known.Add(_alias);
        _quickCast.KnowsFeat(Player, _original).Should().BeTrue();
        _quickCast.KnowsFeat(Player, _alias).Should().BeTrue();

        _known.Clear();
        _known.Add(_original);
        _quickCast.KnowsFeat(Player, _alias).Should().BeTrue();
    }

    [Test]
    public void RemoveFeat_RemovesTheOriginalAndItsAlias()
    {
        _quickCast.RemoveFeat(Player, _alias);

        _creature.CallsTo("RemoveFeat").Select(c => c.ToString()).Should()
            .Equal($"RemoveFeat({Player}, {_original})", $"RemoveFeat({Player}, {_alias})");
    }

    [Test]
    public void ApplyPreferences_SwitchesAKnownOriginalToItsAliasWhenAutomaticTargetingApplies()
    {
        _quickCast.Abilities[_original] = Ability(hostile: false);
        Settings.SelfQuickCastEnabled = true;
        _known.Add(_original);

        _quickCast.ApplyPreferences(Player);

        _creature.CallsTo("AddFeat").Select(c => c.ToString()).Should().Equal($"AddFeat({Player}, {_alias})");
        _creature.CallsTo("RemoveFeat").Select(c => c.ToString()).Should().Equal($"RemoveFeat({Player}, {_original})");
    }

    [Test]
    public void ApplyPreferences_SwitchesAKnownAliasBackWhenAutomaticTargetingIsOff()
    {
        _quickCast.Abilities[_original] = Ability(hostile: false);
        _known.Add(_alias);

        _quickCast.ApplyPreferences(Player);

        _creature.CallsTo("AddFeat").Select(c => c.ToString()).Should().Equal($"AddFeat({Player}, {_original})");
        _creature.CallsTo("RemoveFeat").Select(c => c.ToString()).Should().Equal($"RemoveFeat({Player}, {_alias})");
    }

    [Test]
    public void ApplyPreferences_LeavesFeatsAloneWhenTheyAlreadyMatchOrAreUnknown()
    {
        _quickCast.Abilities[_original] = Ability(hostile: false);
        _known.Add(_original);

        _quickCast.ApplyPreferences(Player);

        _creature.CountOf("AddFeat").Should().Be(0);
        _creature.CountOf("RemoveFeat").Should().Be(0);
    }

    [Test]
    public void ApplyPreferences_DoesNothingWithoutSettings()
    {
        _isDm = true;
        _known.Add(_original);

        _quickCast.ApplyPreferences(Player);

        _creature.Calls.Should().BeEmpty();
        _playerPlugin.CountOf("SetQuickBarSlot").Should().Be(0);
    }

    [Test]
    public void ApplyPreferences_RewritesQuickBarSlotsToTheDesiredRepresentation()
    {
        _quickCast.Abilities[_original] = Ability(hostile: false);
        Settings.SelfQuickCastEnabled = true;
        _known.Add(_alias);
        _bar[3] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)_original };
        _bar[4] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)_alias };
        _bar[5] = new QuickBarSlot { ObjectType = QuickBarSlotType.Item };
        _bar[6] = new QuickBarSlot
        {
            ObjectType = QuickBarSlotType.Feat,
            INTParam1 = (int)Enum.GetValues<FeatType>().First(f => !QuickCastFeatMap.IsAlias(f) && !QuickCastFeatMap.TryGetAlias(f, out _))
        };

        _quickCast.ApplyPreferences(Player);

        var writes = _playerPlugin.CallsTo("SetQuickBarSlot");
        writes.Should().ContainSingle("only the slot holding the other representation changes");
        writes[0].Arguments[1].Should().Be(3);
        ((QuickBarSlot)writes[0].Arguments[2]!).INTParam1.Should().Be((int)_alias);
    }

    [Test]
    public void ApplyPreferences_ReleasesThePlayerSoALaterCallWorksAndIgnoresAReentrantCall()
    {
        _quickCast.Abilities[_original] = Ability(hostile: false);
        _known.Add(_alias);
        _quickCast.OnRegistryRead = () => _quickCast.ApplyPreferences(Player);

        _quickCast.ApplyPreferences(Player);
        var firstRound = _creature.CountOf("AddFeat");
        _quickCast.OnRegistryRead = null;
        _known.Clear();
        _known.Add(_alias);
        _quickCast.ApplyPreferences(Player);

        firstRound.Should().Be(1, "the nested call returned at once");
        _creature.CountOf("AddFeat").Should().Be(2, "the player was released after the first call");
    }

    [Test]
    public void TheHandlers_ApplyThePreferencesOfTheEnteringPlayerAndTheEditingPlayer()
    {
        _quickCast.Abilities[_original] = Ability(hostile: false);
        _known.Add(_alias);

        _quickCast.ApplyOnEnter();
        _quickCast.ApplyOnQuickbarEdit();

        _creature.CountOf("AddFeat").Should().Be(2);
    }

    [Test]
    public void TryCast_RefusesAnythingButTheAliasOfAPlayerCharacter()
    {
        _quickCast.TryCast(Player, _original, out var error).Should().BeFalse();
        error.Should().Contain("only available for your player character");

        _isDm = true;
        _quickCast.TryCast(Player, _alias, out error).Should().BeFalse();
        error.Should().Contain("only available for your player character");
    }

    [Test]
    public void TryCast_RefusesAnUnknownOrUnregisteredAbility()
    {
        _quickCast.TryCast(Player, _alias, out var error).Should().BeFalse();
        error.Should().Be("You do not know this ability.");

        _known.Add(_original);
        _quickCast.TryCast(Player, _alias, out error).Should().BeFalse();
        error.Should().Be("You do not know this ability.", "the ability is not registered");

        _known.Clear();
        _quickCast.Abilities[_original] = Ability(hostile: false);
        _quickCast.TryCast(Player, _alias, out error).Should().BeFalse();
        error.Should().Be("You do not know this ability.", "the player does not know it");
    }

    [Test]
    public void TryCast_RefusesWhenTheTargetingKindIsDisabledOrIneligible()
    {
        _known.Add(_original);
        _quickCast.Abilities[_original] = Ability(hostile: false);

        _quickCast.TryCast(Player, _alias, out var error).Should().BeFalse();
        error.Should().Contain("Automatic targeting is disabled");

        Settings.SelfQuickCastEnabled = true;
        _quickCast.Abilities[_original].AllowsSelfQuickCast = false;
        _quickCast.TryCast(Player, _alias, out error).Should().BeFalse();
        error.Should().Contain("Automatic targeting is disabled");
    }

    [Test]
    public void TryCast_RefusesWhileAnIncapacitatingEffectIsApplied()
    {
        _known.Add(_original);
        _quickCast.Abilities[_original] = Ability(hostile: false);
        Settings.SelfQuickCastEnabled = true;
        _quickCast.Blocked = true;

        _quickCast.TryCast(Player, _alias, out var error).Should().BeFalse();

        error.Should().Be("You cannot use an ability while incapacitated.");
        _quickCast.Used.Should().BeEmpty();
    }

    [Test]
    public void TryCast_UsesASupportAbilityOnTheCaster()
    {
        _known.Add(_original);
        _quickCast.Abilities[_original] = Ability(hostile: false);
        Settings.SelfQuickCastEnabled = true;

        _quickCast.TryCast(Player, _alias, out var error).Should().BeTrue();

        error.Should().BeEmpty();
        _quickCast.Used.Should().Equal((Player, Player, _original));
    }

    [Test]
    public void TryCast_ReturnsWhatTheAbilityPipelineAnswers()
    {
        _known.Add(_original);
        _quickCast.Abilities[_original] = Ability(hostile: false);
        Settings.SelfQuickCastEnabled = true;
        _quickCast.UseResult = false;

        _quickCast.TryCast(Player, _alias, out _).Should().BeFalse();
    }

    [Test]
    public void TryCast_UsesAHostileAbilityOnTheQueuedAttackTarget()
    {
        _known.Add(_original);
        _quickCast.Abilities[_original] = Ability(hostile: true);
        Settings.EnemyQuickCastEnabled = true;
        _quickCast.QueuedTarget = Enemy;

        _quickCast.TryCast(Player, _alias, out var error).Should().BeTrue();

        error.Should().BeEmpty();
        _quickCast.Used.Should().Equal((Player, Enemy, _original));
    }

    [TestCase("none")]
    [TestCase("self")]
    [TestCase("dead")]
    [TestCase("nohp")]
    [TestCase("area")]
    [TestCase("friendly")]
    public void TryCast_RefusesAHostileAbilityWithoutALivingEnemyInTheArea(string problem)
    {
        _known.Add(_original);
        _quickCast.Abilities[_original] = Ability(hostile: true);
        Settings.EnemyQuickCastEnabled = true;
        _quickCast.QueuedTarget = problem switch
        {
            "none" => ObjectInvalid,
            "self" => Player,
            _ => Enemy
        };
        _enemyDead = problem == "dead";
        _enemyHitPoints = problem == "nohp" ? 0 : 50;
        _enemyArea = problem == "area" ? Area + 1 : Area;
        _enemyHostile = problem != "friendly";

        _quickCast.TryCast(Player, _alias, out var error).Should().BeFalse();

        error.Should().Be("Attack a living enemy first to use current-enemy targeting.");
        _quickCast.Used.Should().BeEmpty();
    }

    private sealed class TestQuickCast : QuickCastService
    {
        public TestQuickCast(IDBService db)
            : base(db)
        {
        }

        public Dictionary<FeatType, AbilityDetail> Abilities { get; } = new();
        public bool Blocked { get; set; }
        public uint QueuedTarget { get; set; } = ObjectInvalid;
        public bool UseResult { get; set; } = true;
        public List<(uint Player, uint Target, FeatType Feat)> Used { get; } = new();
        public Action? OnRegistryRead { get; set; }

        protected override bool IsFeatRegistered(FeatType feat)
        {
            OnRegistryRead?.Invoke();
            return Abilities.ContainsKey(feat);
        }

        protected override AbilityDetail GetAbilityDetail(FeatType feat) => Abilities[feat];

        protected override bool HasCastingBlockingEffect(uint player) => Blocked;

        protected override uint GetQueuedAttackTarget(uint player) => QueuedTarget;

        protected override bool UseAbility(uint player, uint target, FeatType feat)
        {
            Used.Add((player, target, feat));
            return UseResult;
        }
    }
}
