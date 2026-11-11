using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class CasinoSlotPresentationTests
{
    [TestCase(0, "cas_hutt", "cas_star", "cas_credit", "TRY AGAIN", false)]
    [TestCase(100, "cas_star", "cas_star", "cas_hutt", "BREAK EVEN", false)]
    [TestCase(500, "cas_star", "cas_star", "cas_star", "WIN!", false)]
    [TestCase(1000, "cas_hutt", "cas_hutt", "cas_hutt", "BIG WIN!", true)]
    [TestCase(10000, "cas_credit", "cas_credit", "cas_credit", "JACKPOT!", true)]
    public void SettledPayoutChoosesTheMatchingReelsAndCelebration(
        int payout, string left, string middle, string right, string headline, bool celebration)
    {
        Assert.That(CasinoSlotPresentation.GetReels(payout), Is.EqualTo(new[] { left, middle, right }));
        Assert.That(CasinoSlotPresentation.GetHeadline(payout), Is.EqualTo(headline));
        Assert.That(CasinoSlotPresentation.IsBigWin(payout), Is.EqualTo(celebration));
    }

    [Test]
    public void EveryPossiblePayoutHasACompleteVisualResult()
    {
        foreach (var payout in Enumerable.Range(0, 10000).Select(CasinoSlot.GetPayout).Distinct())
        {
            Assert.That(CasinoSlotPresentation.GetReels(payout), Has.Length.EqualTo(3));
            Assert.That(CasinoSlotPresentation.GetHeadline(payout), Is.Not.Empty);
        }
    }

    [TestCase("cas_credit", 256)]
    [TestCase("cas_star", 256)]
    [TestCase("cas_hutt", 256)]
    [TestCase("cas_bigwin", 512)]
    public void CustomArtworkShipsAsAnOpaqueBottomLeftTga(string resref, int size)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md")))
            root = root.Parent;
        Assert.That(root, Is.Not.Null);
        var bytes = File.ReadAllBytes(Path.Combine(root!.FullName, "SWLOR_Haks", "sw_ui", resref + ".tga"));
        Assert.That(resref.Length, Is.LessThanOrEqualTo(16));
        Assert.That(bytes[2], Is.EqualTo(2));
        Assert.That(BitConverter.ToUInt16(bytes, 12), Is.EqualTo(size));
        Assert.That(BitConverter.ToUInt16(bytes, 14), Is.EqualTo(size));
        Assert.That(bytes[16], Is.EqualTo(32));
        Assert.That(bytes[17], Is.EqualTo(8));
        Assert.That(bytes.Length, Is.EqualTo(18 + size * size * 4));
        for (var i = 21; i < bytes.Length; i += 4)
            Assert.That(bytes[i], Is.EqualTo(255));
    }
}
