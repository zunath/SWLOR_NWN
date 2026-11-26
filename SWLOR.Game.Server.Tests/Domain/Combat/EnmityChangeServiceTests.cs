using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>The enmity change part: the damage, healing and attack handlers, the changes, taunts, reductions and proximity enmity.</summary>
[TestFixture]
public class EnmityChangeServiceTests : EnmityPartsTestBase
{
    [TestCase(0, 0, 0)]
    [TestCase(-5, 10, 0)]
    [TestCase(100, 0, 100)]
    [TestCase(100, 25, 125)]
    [TestCase(100, -50, 50)]
    [TestCase(100, -500, 1)]
    [TestCase(1, -99, 1)]
    public void CalculateDamageEnmity_ScalesByTheAdjustmentAndNeverDropsBelowOne(int damage, int adjustment, int expected) =>
        _change.CalculateDamageEnmity(damage, adjustment).Should().Be(expected);

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
        _change.CalculateHealingEnmity(requested, previous, current, nested).Should().Be(expected);

    [Test]
    public void ClampEnmityPercentAdjustment_KeepsTheAdjustmentWithinTheLimits()
    {
        _change.ClampEnmityPercentAdjustment(-900).Should().Be(-50);
        _change.ClampEnmityPercentAdjustment(900).Should().Be(50);
        _change.ClampEnmityPercentAdjustment(12).Should().Be(12);
    }

    [TestCase(10, 20, 12)]
    [TestCase(10, 900, 15)]
    [TestCase(10, -900, 5)]
    [TestCase(1, -50, 1)]
    [TestCase(0, 20, 0)]
    [TestCase(-10, 20, -10)]
    public void CalculateAdjustedEnmity_AdjustsGainsOnly(int amount, int adjustment, int expected) =>
        _change.CalculateAdjustedEnmity(amount, adjustment).Should().Be(expected);

    [TestCase(100, 80, 10, 100)]
    [TestCase(10, 80, 10, 90)]
    [TestCase(10, 80, 0, 81)]
    [TestCase(0, 0, 0, 1)]
    public void CalculateTauntEnmity_CatchesUpToTheRivalPlusALead(int current, int rival, int bonus, int expected) =>
        _change.CalculateTauntEnmity(current, rival, bonus).Should().Be(expected);

    [Test]
    public void CreatureDamaged_AddsTheDamageAsEnmityScaledByTheDamagersAdjustment()
    {
        _damage = 40;
        _change.StatAdjustments[(Player, StatType.DamageEnmityPercentAdjustment)] = 50;
        _change.StatAdjustments[(Player, StatType.EnmityPercentAdjustment)] = 0;

        _change.CreatureDamaged();

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(60);
    }

    [TestCase(0, Player)]
    [TestCase(-3, Player)]
    [TestCase(10, ObjectInvalid)]
    public void CreatureDamaged_IgnoresNoDamageAndNoDamager(int damage, uint damager)
    {
        _damage = damage;
        _lastDamager = damager;

        _change.CreatureDamaged();

        _enmity.GetEnmityTable(Enemy).Should().BeEmpty();
    }

    [Test]
    public void CreatureAttacked_AddsOneEnmityForTheAttacker()
    {
        _change.CreatureAttacked();

        _enmity.GetEnmityTable(Enemy).Should().Equal(new Dictionary<uint, int> { [Player] = 1 });
    }

    [Test]
    public void CreatureDeath_AndDestroyed_ClearTheEnemiesTablesAndItsOwnEnmity()
    {
        _change.ModifyEnmity(Player, Enemy, 10);
        _change.ModifyEnmity(Enemy, Enemy2, 5);
        _change.CreatureDeath();

        _enmity.GetEnmityTable(Enemy).Should().BeEmpty();
        _enmity.HasEnmity(Enemy).Should().BeFalse();

        _change.ModifyEnmity(Player, Enemy, 10);
        _change.CreatureDestroyed();

        _enmity.GetEnmityTable(Enemy).Should().BeEmpty();
        _enmity.HasEnmity(Player).Should().BeFalse();
    }

    [Test]
    public void PlayerDeath_AndExit_RemoveThePlayerFromEveryTable()
    {
        _change.ModifyEnmity(Player, Enemy, 10);
        _change.ModifyEnmity(Player, Enemy2, 10);
        _lastDied = Player;
        _change.PlayerDeath();
        _enmity.HasEnmity(Player).Should().BeFalse();

        _change.ModifyEnmity(Player, Enemy, 10);
        _exiting = Player;
        _change.PlayerExit();
        _enmity.GetEnmityTowardsAllEnemies(Player).Should().BeEmpty();
    }

    [Test]
    public void CreatureLimbo_ClearsEveryLimboedTarget()
    {
        _change.ModifyEnmity(Player, Enemy, 10);
        _change.ModifyEnmity(Player, Enemy2, 10);
        _eventData["NUM_TARGETS"] = "3";
        _eventData["TARGET_1"] = Enemy.ToString();
        _eventData["TARGET_2"] = ObjectInvalid.ToString();
        _eventData["TARGET_3"] = Player2.ToString();

        _change.CreatureLimbo();

        _enmity.GetEnmityTable(Enemy).Should().BeEmpty();
        _enmity.GetEnmityTable(Enemy2).Should().ContainKey(Player);
        _enmity.HasEnmity(Player).Should().BeTrue();
    }

    [Test]
    public void Healing_GivesTheHealerHalfTheRestoredHitPointsAsEnmityOnTheEnemiesFightingTheTarget()
    {
        _change.ModifyEnmity(Player2, Enemy, 50);
        _change.ModifyEnmity(Player2, Enemy2, 20);
        _change.ModifyProximityEnmity(Player2, 102, 4);

        Heal(Player, Player2, 40, 30, 70);

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(20);
        _enmity.GetEnmityTable(Enemy2)[Player].Should().Be(20);
        _enmity.GetEnmityTable(102).Should().NotContainKey(Player);
    }

    [Test]
    public void Healing_IgnoresAnInvalidTargetAnAmountThatCannotBeReadAndAnotherHealersObservation()
    {
        _change.ModifyEnmity(Player2, Enemy, 50);
        _eventData["TARGET_OBJECT_ID"] = ObjectInvalid.ToString();
        _change.HealingStarted();
        _change.HealingCompleted();

        _eventData["TARGET_OBJECT_ID"] = Player2.ToString();
        _eventData["HEAL_AMOUNT"] = "many";
        _self = Player;
        _hitPoints[Player2] = 30;
        _change.HealingStarted();
        _hitPoints[Player2] = 70;
        _change.HealingCompleted();

        _eventData["HEAL_AMOUNT"] = "40";
        _eventData["TARGET_OBJECT_ID"] = Player2.ToString();
        _self = Player;
        _change.HealingStarted();
        _self = Enemy2;
        _change.HealingCompleted();

        _enmity.GetEnmityTable(Enemy).Should().NotContainKey(Player);
    }

    [Test]
    public void Healing_CompletedWithoutAStart_DoesNothing()
    {
        _eventData["TARGET_OBJECT_ID"] = Player2.ToString();

        _change.HealingCompleted();

        _enmity.HasEnmity(Player).Should().BeFalse();
    }

    [Test]
    public void Healing_NestedHealingIsNotCountedTwice()
    {
        _change.ModifyEnmity(Player2, Enemy, 50);
        _eventData["TARGET_OBJECT_ID"] = Player2.ToString();
        _eventData["HEAL_AMOUNT"] = "40";

        _self = Player;
        _hitPoints[Player2] = 30;
        _change.HealingStarted();
        _self = Enemy2;
        _change.HealingStarted();
        _hitPoints[Player2] = 50;
        _self = Enemy2;
        _change.HealingCompleted();
        _hitPoints[Player2] = 70;
        _self = Player;
        _change.HealingCompleted();

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(10);
    }

    [Test]
    public void ModifyEnmity_RecordsTheEnmityTellsTheEnemyToAttackAndRaisesTheScript()
    {
        _change.ModifyEnmity(Player, Enemy, 10);

        _enmity.GetEnmityTable(Enemy).Should().Equal(new Dictionary<uint, int> { [Player] = 10 });
        _enmity.GetEnmityTowardsAllEnemies(Player).Should().Equal(new Dictionary<uint, int> { [Enemy] = 10 });
        _enmity.HasEnmity(Player).Should().BeTrue();
        ScriptsRun().Should().Contain(ScriptName.OnEnmityChanged);
        _engine.CallsTo("AssignCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(Enemy);
    }

    [Test]
    public void ModifyEnmity_AppliesTheCreaturesAdjustmentAndTheStatusSourceAdjustmentOnce()
    {
        _change.StatAdjustments[(Player, StatType.EnmityPercentAdjustment)] = 20;
        var effect = ScriptedProxy.Create<IStatusEffect>(out var script);
        var group = new StatGroup();
        group.Stats[StatType.EnmityToStatusSourcePercentAdjustment] = 10;
        script.On("get_Source", _ => Player);
        script.On("get_StatGroup", _ => group);
        var other = ScriptedProxy.Create<IStatusEffect>(out var otherScript);
        otherScript.On("get_Source", _ => Player2);
        otherScript.On("get_StatGroup", _ => group);
        _change.Effects[Enemy] = new HashSet<IStatusEffect> { effect, other };

        _change.ModifyEnmity(Player, Enemy, 100);

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(130);
    }

    [Test]
    public void ModifyEnmity_ReductionsAreNotAdjustedAndEnmityNeverDropsBelowOne()
    {
        _change.StatAdjustments[(Player, StatType.EnmityPercentAdjustment)] = 50;
        _change.ModifyEnmity(Player, Enemy, 10);

        _change.ModifyEnmity(Player, Enemy, -4);
        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(11);

        _change.ModifyEnmity(Player, Enemy, -400);
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
            _change.ModifyEnmity(Player, Enemy, 10);
            _enmity.GetEnmityTable(Enemy).Should().BeEmpty(why);
        }

        _change.ModifyEnmity(ObjectInvalid, Enemy, 10);
        _change.ModifyEnmity(Player, ObjectInvalid, 10);
        _change.ModifyEnmity(Enemy, Enemy, 10);
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
        _change.ModifyEnmity(Player, Enemy2, 10);
        _change.ModifyEnmity(Player, Enemy, 0);

        _enmity.GetEnmityTable(Enemy2).Should().BeEmpty();
        _enmity.GetEnmityTable(Enemy).Should().BeEmpty();
    }

    [Test]
    public void ModifyEnmityOnAll_AppliesToEveryEnemyTheCreatureIsOn()
    {
        _change.ModifyEnmityOnAll(Player, 5);
        _change.ModifyEnmity(Player, Enemy, 10);
        _change.ModifyEnmity(Player, Enemy2, 20);

        _change.ModifyEnmityOnAll(Player, 0);
        _change.ModifyEnmityOnAll(Player, 5);

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(15);
        _enmity.GetEnmityTable(Enemy2)[Player].Should().Be(25);
    }

    [Test]
    public void ReduceEnmity_CutsThePercentageAndScalesTheProximityShareDown()
    {
        _change.ModifyProximityEnmity(Player, Enemy, 10);
        _change.ModifyEnmity(Player, Enemy, 90);

        _change.ReduceEnmity(Player, Enemy, 50);

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(50);
        _enmity.HasProximityEnmity(Player, Enemy).Should().BeTrue();
        ScriptsRun().Count(script => script == ScriptName.OnEnmityChanged).Should().Be(3);
    }

    [Test]
    public void ReduceEnmity_IgnoresNonPositivePercentAndUnknownEntries()
    {
        _change.ModifyEnmity(Player, Enemy, 10);

        _change.ReduceEnmity(Player, Enemy, 0);
        _change.ReduceEnmity(Player2, Enemy, 50);
        _change.ReduceEnmity(Player, Enemy2, 50);

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(10);
    }

    [Test]
    public void ReduceEnmityOnAll_ReducesEveryTable()
    {
        _change.ModifyEnmity(Player, Enemy, 100);
        _change.ModifyEnmity(Player, Enemy2, 40);

        _change.ReduceEnmityOnAll(Player, 0);
        _change.ReduceEnmityOnAll(Player2, 50);
        _change.ReduceEnmityOnAll(Player, 50);

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(50);
        _enmity.GetEnmityTable(Enemy2)[Player].Should().Be(20);
    }

    [Test]
    public void ModifyProximityEnmity_TracksOnlyTheAmountThatWasApplied()
    {
        _change.ModifyProximityEnmity(Player, Enemy, 0);
        _enmity.HasProximityEnmity(Player, Enemy).Should().BeFalse();

        _change.ModifyProximityEnmity(Player, Enemy, 6);
        _change.ModifyProximityEnmity(Player, Enemy, 4);
        _evading.Add(Enemy2);
        _change.ModifyProximityEnmity(Player, Enemy2, 4);

        _enmity.HasProximityEnmity(Player, Enemy).Should().BeTrue();
        _enmity.HasProximityEnmity(Player, Enemy2).Should().BeFalse();
        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(10);
    }

    [Test]
    public void TryTaunt_CatchesUpToTheEligibleRivalPlusTheBonus()
    {
        _change.ModifyEnmity(Player2, Enemy, 100);
        _change.ModifyEnmity(Player, Enemy, 10);

        _change.TryTaunt(Player, Enemy, 20).Should().BeTrue();

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(120);
    }

    [Test]
    public void TryTaunt_DoesNotAccumulateWhileAlreadyAhead()
    {
        _change.ModifyEnmity(Player, Enemy, 200);
        _change.ModifyEnmity(Player2, Enemy, 50);

        _change.TryTaunt(Player, Enemy, 20).Should().BeTrue();

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(200);
    }

    [Test]
    public void TryTaunt_IgnoresRivalsTheEnemyCannotAttack()
    {
        _change.ModifyEnmity(Player2, Enemy, 500);
        _attack.Hidden.Add((Enemy, Player2));
        _change.ModifyEnmity(Player, Enemy, 10);

        _change.TryTaunt(Player, Enemy, 20).Should().BeTrue();

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
            _change.TryTaunt(Player, Enemy, bonus).Should().BeFalse(why);
            _enmity.GetEnmityTable(Enemy).Should().BeEmpty(why);
        }

        Refused("no bonus", () => { }, 0);
                Refused("dead creature", () => _dead.Add(Player));
        Refused("dead enemy", () => _dead.Add(Enemy));
        Refused("player enemy", () => _players.Add(Enemy));
        Refused("enemy with a master", () => _masters[Enemy] = Player2);
        Refused("player beast", () => _playerBeasts.Add(Enemy));
        Refused("droid", () => _change.Droids.Add(Enemy));
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

        _change.TryTaunt(Player, Enemy, 20).Should().BeFalse();
    }

    [Test]
    public void RemoveCreatureEnmity_ForgetsTheCommandsThrottleAndRetargetsTheEnemies()
    {
        _change.ModifyEnmity(Player, Enemy, 5);
        _change.ModifyEnmity(Player2, Enemy, 5);
        _engine.ClearCalls();

        _change.RemoveCreatureEnmity(Player);

        _enmity.GetEnmityTable(Enemy).Should().NotContainKey(Player);
        _enmity.GetEnmityTable(Enemy).Should().ContainKey(Player2);
    }
}
