using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.SpaceObjectDefinition;
using SWLOR.Game.Server.Service.SpaceService;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The NPC ship role decisions (ShipNpcRoleRules): target choice, thresholds and cooldowns for each role.
/// </summary>
public class ShipNpcRoleRulesTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void ParseRole_ReadsEveryPresetRole()
    {
        foreach (var entry in RevampNpcShipCatalog.Ships)
            ShipNpcRoleRules.ParseRole(entry.Role).Should().NotBe(ShipNpcRole.Invalid, entry.CreatureTag);

        ShipNpcRoleRules.ParseRole("Electronic Warfare").Should().Be(ShipNpcRole.ElectronicWarfare);
        ShipNpcRoleRules.ParseRole("Gunship").Should().Be(ShipNpcRole.Gunship);
        ShipNpcRoleRules.ParseRole("Pilot").Should().Be(ShipNpcRole.Invalid);
        ShipNpcRoleRules.ParseRole("99").Should().Be(ShipNpcRole.Invalid);
        ShipNpcRoleRules.ParseRole(null).Should().Be(ShipNpcRole.Invalid);
    }

    [Test]
    public void IsReady_WaitsOutTheCooldown()
    {
        ShipNpcRoleRules.IsReady(Now, DateTime.MinValue, 12).Should().BeTrue();
        ShipNpcRoleRules.IsReady(Now, Now.AddSeconds(-11.9), 12).Should().BeFalse();
        ShipNpcRoleRules.IsReady(Now, Now.AddSeconds(-12), 12).Should().BeTrue();
    }

    [Test]
    public void Tank_PicksTheEnemyWithTheMostThreatAcrossTheSquad()
    {
        var tank = new Dictionary<uint, int> { [1] = 300, [2] = 100 };
        var wingman = new Dictionary<uint, int> { [2] = 450, [3] = 900 };
        var tables = new IReadOnlyDictionary<uint, int>[] { tank, wingman };

        // 2 holds 550 in all, 3 holds 900 but is not an enemy (a friendly ship on the table).
        ShipNpcRoleRules.PickTopDamageEnemy(tables, x => x != 3).Should().Be(2u);
        ShipNpcRoleRules.PickTopDamageEnemy(tables, _ => true).Should().Be(3u);
        ShipNpcRoleRules.PickTopDamageEnemy(Array.Empty<IReadOnlyDictionary<uint, int>>(), _ => true).Should().Be(OBJECT_INVALID);
    }

    [Test]
    public void Tank_TiesGoToTheLowestId()
    {
        var tables = new IReadOnlyDictionary<uint, int>[] { new Dictionary<uint, int> { [7] = 50, [4] = 50 } };

        ShipNpcRoleRules.PickTopDamageEnemy(tables, _ => true).Should().Be(4u);
    }

    [TestCase(100, 200, false, false)]
    [TestCase(99, 200, false, true)]
    [TestCase(150, 200, true, true)]
    [TestCase(200, 200, true, false)]
    [TestCase(0, 0, false, false)]
    public void Tank_HardensBelowHalfHullUntilWhole(int hull, int maxHull, bool isOn, bool expected)
    {
        ShipNpcRoleRules.ShouldHarden(hull, maxHull, isOn).Should().Be(expected);
    }

    [Test]
    public void Logistics_LocksOntoTheAllyMissingTheLargestShare()
    {
        var allies = new (uint, int, int)[]
        {
            (1, 30, 100),   // 30%
            (2, 120, 300),  // 40%
            (3, 20, 100),   // 20%: under the quarter
        };

        ShipNpcRoleRules.PickRepairTarget(allies).Should().Be(2u);
    }

    [Test]
    public void Logistics_RepairsNoOneUntilAQuarterIsMissing()
    {
        ShipNpcRoleRules.PickRepairTarget(new (uint, int, int)[] { (1, 24, 100), (2, 0, 0) }).Should().Be(OBJECT_INVALID);
        ShipNpcRoleRules.PickRepairTarget(new (uint, int, int)[] { (1, 25, 100) }).Should().Be(1u);
        ShipNpcRoleRules.IsRepairDone(0).Should().BeTrue();
        ShipNpcRoleRules.IsRepairDone(1).Should().BeFalse();
    }

    [Test]
    public void Logistics_EqualSharesGoToTheLargerMissingAmount()
    {
        ShipNpcRoleRules.PickRepairTarget(new (uint, int, int)[] { (1, 50, 100), (2, 150, 300) }).Should().Be(2u);
    }

    [TestCase(false, false, ShipNpcEwarAction.Jam)]
    [TestCase(false, true, ShipNpcEwarAction.Jam)]
    [TestCase(true, false, ShipNpcEwarAction.Paint)]
    [TestCase(true, true, ShipNpcEwarAction.None)]
    public void ElectronicWarfare_JamsThenPaints(bool jammed, bool painted, ShipNpcEwarAction expected)
    {
        ShipNpcRoleRules.ChooseEwarAction(jammed, painted).Should().Be(expected);
    }

    [Test]
    public void ElectronicWarfare_CooldownIsOneAndAHalfTimesTheDebuff()
    {
        ShipNpcRoleTuning.EwarCooldownSeconds.Should().Be(ShipNpcRoleTuning.EwarSeconds * 1.5);
        ShipNpcRoleTuning.WebCooldownSeconds.Should().Be(ShipNpcRoleTuning.WebSeconds * 1.5);
    }

    [TestCase(null, 50f, 10, 100, 30f, false)]   // first sighting
    [TestCase(20f, 20.5f, 10, 100, 30f, false)]  // barely moved
    [TestCase(20f, 25f, 40, 100, 30f, true)]     // moving away below half hull
    [TestCase(20f, 25f, 60, 100, 30f, false)]    // moving away, healthy and in range
    [TestCase(29f, 35f, 90, 100, 30f, true)]     // moving away beyond weapon range
    [TestCase(35f, 30f, 10, 100, 30f, false)]    // closing in
    public void Interceptor_SpotsShipsThatFlee(float? previous, float distance, int hull, int maxHull, float range, bool expected)
    {
        ShipNpcRoleRules.IsFleeing(previous, distance, hull, maxHull, range).Should().Be(expected);
    }

    [Test]
    public void Interceptor_ChasesTheNearestFleeingShip()
    {
        var enemies = new (uint, float?, float, int, int)[]
        {
            (1, 40f, 45f, 90, 100),  // fleeing, beyond range, far
            (2, 30f, 34f, 90, 100),  // fleeing, beyond range, nearer
            (3, 10f, 5f, 10, 100),   // closing in
        };

        ShipNpcRoleRules.PickQuarry(enemies, 30f).Should().Be(2u);
        ShipNpcRoleRules.PickQuarry(new (uint, float?, float, int, int)[] { (3, 10f, 5f, 10, 100) }, 30f).Should().Be(OBJECT_INVALID);
    }

    [Test]
    public void Bomber_HuntsTheBiggestHullInRange()
    {
        var enemies = new (uint, ShipSize, int, float)[]
        {
            (1, ShipSize.Small, 900, 10f),
            (2, ShipSize.Large, 500, 20f),
            (3, ShipSize.Capital, 5000, 80f),  // out of range
            (4, ShipSize.Large, 600, 25f),
        };

        ShipNpcRoleRules.PickBiggestHull(enemies, 40f).Should().Be(4u);
        ShipNpcRoleRules.PickBiggestHull(enemies, 100f).Should().Be(3u);
        ShipNpcRoleRules.PickBiggestHull(enemies, 5f).Should().Be(OBJECT_INVALID);
    }

    [Test]
    public void Sniper_KitesJustInsideItsLongestRange()
    {
        ShipNpcRoleRules.KiteDistance(40f).Should().BeApproximately(36f, 0.001f);
        ShipNpcRoleRules.KiteDistance(0f).Should().Be(0f);
    }

    [Test]
    public void Carrier_LaunchesWavesOnItsCooldownUpToItsCaps()
    {
        ShipNpcRoleRules.FightersToLaunch(Now, DateTime.MinValue, 0, 0).Should().Be(2);
        ShipNpcRoleRules.FightersToLaunch(Now, Now.AddSeconds(-29), 1, 0).Should().Be(0);
        ShipNpcRoleRules.FightersToLaunch(Now, Now.AddSeconds(-30), 1, 2).Should().Be(2);
        ShipNpcRoleRules.FightersToLaunch(Now, Now.AddSeconds(-30), 2, 3).Should().Be(1);
        ShipNpcRoleRules.FightersToLaunch(Now, Now.AddSeconds(-30), 2, 4).Should().Be(0);
        ShipNpcRoleRules.FightersToLaunch(Now, Now.AddSeconds(-60), ShipNpcRoleTuning.MaxWaves, 0).Should().Be(0);
    }

    [Test]
    public void Carrier_LaunchesItsTiersFighter()
    {
        ShipNpcRoleRules.FighterTag("t3carrier").Should().Be("t3fighter");
        ShipNpcRoleRules.FighterTag("sithcap1").Should().BeEmpty();
        ShipNpcRoleRules.FighterTag("").Should().BeEmpty();

        foreach (var carrier in RevampNpcShipCatalog.Ships.Where(x => x.Role == "Carrier"))
        {
            var fighter = ShipNpcRoleRules.FighterTag(carrier.CreatureTag);
            RevampNpcShipCatalog.Ships.Should().Contain(x => x.CreatureTag == fighter && x.Role == "Fighter", carrier.CreatureTag);
        }
    }

    [Test]
    public void Minelayer_DropsAMineEveryTwentySecondsUpToThree()
    {
        ShipNpcRoleRules.CanDropMine(Now, DateTime.MinValue, 0).Should().BeTrue();
        ShipNpcRoleRules.CanDropMine(Now, Now.AddSeconds(-19), 1).Should().BeFalse();
        ShipNpcRoleRules.CanDropMine(Now, Now.AddSeconds(-20), 2).Should().BeTrue();
        ShipNpcRoleRules.CanDropMine(Now, Now.AddSeconds(-20), 3).Should().BeFalse();
        ShipNpcRoleRules.IsMineExpired(Now, Now.AddSeconds(-59)).Should().BeFalse();
        ShipNpcRoleRules.IsMineExpired(Now, Now.AddSeconds(-60)).Should().BeTrue();
    }
}