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
    [TestCase("pfh0_handr001")]
    [TestCase("")]
    [TestCase(null)]
    [TestCase("hand")]
    public void HandsFittedLikeTheReferenceAreNotMoved(string hand)
    {
        WeaponHandFitCatalog.Get(hand).Should().Be(FitVector.Zero);
    }

    [TestCase("pmz0_handr001")]
    [TestCase("pmz0_handr009")]
    [TestCase("PMZ0_HANDR204")]
    public void EveryHandStyleOfABodyInheritsItsMove(string hand)
    {
        WeaponHandFitCatalog.Get(hand).Should().Be(WeaponHandFitCatalog.Bodies["pmz0_handr"]);
    }

    [Test]
    public void AHandModelWithItsOwnGripReplacesItsBodysMove()
    {
        WeaponHandFitCatalog.Get("pfd0_handr209").Should().Be(WeaponHandFitCatalog.Hands["pfd0_handr209"])
            .And.NotBe(WeaponHandFitCatalog.Bodies["pfd0_handr"]);
        WeaponHandFitCatalog.Get("PFH0_HANDR204").Should().Be(WeaponHandFitCatalog.Hands["pfh0_handr204"]);
    }

    [Test]
    public void EveryEntryNamesAPlayableHandAndStaysWithinSixCentimetres()
    {
        foreach (var (key, move) in WeaponHandFitCatalog.Bodies)
        {
            Regex.IsMatch(key, "^p[mf][hedoz]0_hand[rl]$").Should().BeTrue(key);
            move.Length().Should().BeInRange(.0005f, .06f, key);
        }
        foreach (var (key, move) in WeaponHandFitCatalog.Hands)
        {
            Regex.IsMatch(key, @"^p[mf][hedoz]0_hand[rl]\d{3}$").Should().BeTrue(key);
            move.Length().Should().BeLessThanOrEqualTo(.06f, key);
            WeaponHandFitCatalog.Bodies.TryGetValue(key[..10], out var body);
            move.Should().NotBe(body, $"{key} would only repeat its body's move");
        }
    }

    [Test]
    public void LeftHandsMirrorRightHands()
    {
        var entries = WeaponHandFitCatalog.Bodies.Concat(WeaponHandFitCatalog.Hands).ToDictionary(e => e.Key, e => e.Value);
        foreach (var (key, move) in entries.Where(e => e.Key[9] == 'r'))
        {
            var left = key[..9] + "l" + key[10..];
            entries.Should().ContainKey(left);
            entries[left].Should().Be(new FitVector(-move.X, move.Y, move.Z), left);
        }
        entries.Keys.Count(key => key[9] == 'l').Should().Be(entries.Keys.Count(key => key[9] == 'r'));
    }

    [TestCase(BaseItem.Pistol, true)]
    [TestCase(BaseItem.Sling, true)]
    [TestCase(BaseItem.LegacyPistol, true)]
    [TestCase(BaseItem.CompanionPistol, true)]
    [TestCase(BaseItem.Rifle, false)]
    [TestCase(BaseItem.PowerGlove, false)]
    [TestCase(BaseItem.Longsword, false)]
    public void OnlyPistolsAreMoved(BaseItem type, bool expected)
    {
        WeaponHandFit.IsPistol(type).Should().Be(expected);
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
