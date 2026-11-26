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

/// <summary>The enmity table part: its reads, threat checks, removal and clearing, over the shared parts of <see cref="EnmityPartsTestBase"/>.</summary>
[TestFixture]
public class EnmityServiceTests : EnmityPartsTestBase
{
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

    [TestCase(100, 50, 50)]
    [TestCase(100, 100, 1)]
    [TestCase(100, 250, 1)]
    [TestCase(100, 0, 100)]
    [TestCase(3, 50, 1)]
    [TestCase(0, 50, 1)]
    public void CalculateReducedEnmity_RoundsTheReductionUpAndKeepsOne(int current, int percent, int expected) =>
        _enmity.CalculateReducedEnmity(current, percent).Should().Be(expected);

    [Test]
    public void RemoveProximityEnmity_RemovesTheShareOrTheWholeEntry()
    {
        _change.ModifyProximityEnmity(Player, Enemy, 6);
        _change.ModifyEnmity(Player, Enemy, 4);
        _change.ModifyProximityEnmity(Player2, Enemy, 5);

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
        _change.ModifyProximityEnmity(Player, Enemy, 6);
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
        _change.ModifyProximityEnmity(Player, Enemy, 6);
        _change.ModifyEnmity(Player2, Enemy, 6);
        _enmity.RemoveEnmityAndKeepProximity(Player, Enemy);

        _enmity.RemoveProximityEnmity(Player, Enemy).Should().BeFalse();
        _enmity.HasProximityEnmity(Player, Enemy).Should().BeFalse();
    }

    [Test]
    public void TheNonProximityQueries_SeparateRealCombatFromAggroAuraTraffic()
    {
        _change.ModifyProximityEnmity(Player, Enemy, 5);
        _change.ModifyEnmity(Player2, Enemy2, 5);

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
        _change.ModifyEnmity(Player, Enemy, 5);
        _change.ModifyEnmity(Player, Enemy2, 5);
        _change.ModifyEnmity(Player2, Enemy, 5);
        _change.ModifyProximityEnmity(Player2, Enemy2, 3);

        _enmity.ClearEnmityTable(Enemy);
        _enmity.ClearEnmityTable(Enemy2);
        _enmity.ClearEnmityTable(999);

        _enmity.HasEnmity(Player).Should().BeFalse();
        _enmity.HasEnmity(Player2).Should().BeFalse();
        _enmity.GetEnmityTable(Enemy).Should().BeEmpty();
        _enmity.HasProximityEnmity(Player2, Enemy2).Should().BeFalse();
    }

    [Test]
    public void GetHighestEnmityTarget_AnswersTheHighestAndInvalidForAnEmptyTable()
    {
        _enmity.GetHighestEnmityTarget(Enemy).Should().Be(ObjectInvalid);

        _change.ModifyEnmity(Player, Enemy, 5);
        _change.ModifyEnmity(Player2, Enemy, 9);

        _enmity.GetHighestEnmityTarget(Enemy).Should().Be(Player2);
    }

    [Test]
    public void IsThreatSecured_ComparesTheCreatureWithTheNextHighest()
    {
        _enmity.IsThreatSecured(Enemy, Player).Should().BeFalse();
        _change.ModifyEnmity(Player, Enemy, 100);
        _enmity.IsThreatSecured(Enemy, Player).Should().BeTrue();
        _enmity.IsThreatSecured(Enemy, Player2).Should().BeFalse();

        _change.ModifyEnmity(Player2, Enemy, 90);
        _enmity.IsThreatSecured(Enemy, Player).Should().BeFalse();
    }

    [Test]
    public void GetEnmityTable_ReturnsACopy()
    {
        _change.ModifyEnmity(Player, Enemy, 5);

        _enmity.GetEnmityTable(Enemy)[Player] = 999;

        _enmity.GetEnmityTable(Enemy)[Player].Should().Be(5);
    }
}
