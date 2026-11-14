using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum.Item;
using FitVector = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Tests.Feature;

[TestFixture]
public class WeaponHandFitTests
{
    [TestCase("pmh0_handr001")]
    [TestCase("pmh0_handl001")]
    [TestCase("pmh0_handr204")]
    [TestCase("")]
    [TestCase(null)]
    [TestCase("hand")]
    public void TheReferenceBodyIsNotMoved(string hand)
    {
        WeaponHandFitCatalog.Get(hand).Should().Be(FitVector.Zero);
    }

    [TestCase("pmz0_handr001", "pmz0_handr")]
    [TestCase("PMZ0_HANDR204", "pmz0_handr")]
    [TestCase("pfh0_handl001", "pfh0_handl")]
    public void AHandStyleWithoutItsOwnEntryTakesItsBodysMove(string hand, string body)
    {
        WeaponHandFitCatalog.Hands.Should().NotContainKey(hand);
        WeaponHandFitCatalog.Get(hand).Should().Be(WeaponHandFitCatalog.Bodies[body]);
    }

    [TestCase("pme0_handr208", "pme0_handr")]
    [TestCase("PMO0_HANDR201", "pmo0_handr")]
    [TestCase("pfo0_handl201", "pfo0_handl")]
    public void AHandModelWithItsOwnEntryReplacesItsBodysMove(string hand, string body)
    {
        WeaponHandFitCatalog.Get(hand).Should().Be(WeaponHandFitCatalog.Hands[hand])
            .And.NotBe(WeaponHandFitCatalog.Bodies[body]);
    }

    [Test]
    public void EveryEntryNamesAPlayableHandAndStaysWithinSevenCentimetres()
    {
        foreach (var (key, move) in WeaponHandFitCatalog.Bodies)
        {
            Regex.IsMatch(key, "^p[mf][hedoz]0_hand[rl]$").Should().BeTrue(key);
            move.Length().Should().BeInRange(.0005f, .07f, key);
        }
        foreach (var (key, move) in WeaponHandFitCatalog.Hands)
        {
            Regex.IsMatch(key, @"^p[mf][hedoz]0_hand[rl]\d{3}$").Should().BeTrue(key);
            move.Length().Should().BeLessThanOrEqualTo(.07f, key);
            WeaponHandFitCatalog.Bodies.TryGetValue(key[..10], out var body);
            move.Should().NotBe(body, $"{key} would only repeat its body's move");
        }
    }

    [Test]
    public void EachBodyHasBothSidesAndTheLeftMoveRoughlyMirrorsTheRight()
    {
        // The sides are derived separately because some left hooks are not mirror images, but a left
        // move far from the mirrored right move means a sign or a key was mistyped.
        foreach (var (key, move) in WeaponHandFitCatalog.Bodies.Where(e => e.Key[9] == 'r'))
        {
            var left = key[..9] + "l";
            WeaponHandFitCatalog.Bodies.Should().ContainKey(left);
            (WeaponHandFitCatalog.Bodies[left] - new FitVector(-move.X, move.Y, move.Z)).Length()
                .Should().BeLessThan(.01f, left);
        }
        WeaponHandFitCatalog.Bodies.Keys.Count(key => key[9] == 'l').Should().Be(WeaponHandFitCatalog.Bodies.Count / 2);
    }

    [TestCase(BaseItem.Pistol, true, false)]
    [TestCase(BaseItem.Sling, true, false)]
    [TestCase(BaseItem.LegacyPistol, true, false)]
    [TestCase(BaseItem.CompanionPistol, true, false)]
    [TestCase(BaseItem.Rifle, false, true)]
    [TestCase(BaseItem.Cannon, false, true)]
    [TestCase(BaseItem.PowerGlove, false, false)]
    [TestCase(BaseItem.Longsword, false, false)]
    public void OnlyPistolsAndRiflesAreMoved(BaseItem type, bool pistol, bool rifle)
    {
        WeaponHandFit.IsPistol(type).Should().Be(pistol);
        WeaponHandFit.IsRifle(type).Should().Be(rifle);
    }

    [TestCase("pmo0_handr201")]
    [TestCase("pfo0_handr201")]
    [TestCase("pfd0_handr001")]
    [TestCase("pmz0_handl001")]
    public void ARifleMoveIsHalfwayBetweenWhatTheIdleAndReadyClipsNeed(string hand)
    {
        var idle = WeaponHandFitCatalog.Get(hand);
        var rifle = WeaponHandFit.RifleMove(idle, hand);
        // The ready clip needs the idle move turned with the hook, which keeps its length; the midpoint is
        // equally far from both and never further from either than they are from each other.
        var ready = rifle * 2f - idle;
        ready.Length().Should().BeApproximately(idle.Length(), .0001f);
        (rifle - idle).Length().Should().BeLessThan(idle.Length() * .5f);
        rifle.Length().Should().BeGreaterThan(idle.Length() * .85f);
    }

    [Test]
    public void ARifleMoveAlongTheTurnAxisIsUnchangedAndZeroStaysZero()
    {
        var axis = FitVector.Normalize(new FitVector(.4155f, -.1652f, .0770f)) * .02f;
        (WeaponHandFit.RifleMove(axis, "pmo0_handr201") - axis).Length().Should().BeLessThan(.0001f);
        WeaponHandFit.RifleMove(FitVector.Zero, "pfo0_handr201").Should().Be(FitVector.Zero);
        WeaponHandFit.RifleMove(axis, null).Should().Be(WeaponHandFit.RifleMove(axis, "pmh0_handr001"));
    }

    [Test]
    public void TheFemaleDwarfAndHalfOrcFramesTurnTheRifleLess()
    {
        var move = new FitVector(0f, .02f, 0f);
        var tall = WeaponHandFit.RifleMove(move, "pfh0_handr001");
        (WeaponHandFit.RifleMove(move, "pfd0_handr001") - move).Length().Should().BeLessThan((tall - move).Length());
        WeaponHandFit.RifleMove(move, "PFO0_handr201").Should().Be(WeaponHandFit.RifleMove(move, "pfd0_handr001"));
    }

    [TestCase(0f, 0f, .025f, .025f)]
    [TestCase(.025f, .025f, 0f, 0f)]
    [TestCase(.025f, .025f, -.009f, -.009f)]
    [TestCase(.1f, 0f, .025f, .125f)]
    [TestCase(.125f, .025f, 0f, .1f)]
    public void AMoveReplacesOnlyItsOwnEarlierShareOfTheTransform(float current, float previous, float next, float expected)
    {
        WeaponHandFit.Rebase(current, previous, next).Should().BeApproximately(expected, .000001f);
    }
}
