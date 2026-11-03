using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class CasinoPlayTrackerTests
{
    private static readonly DateTime Start = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void LosingSpinsHaveNoDailyLimit()
    {
        var play = new CasinoDailyPlay();
        for (var i = 0; i < 1000; i++)
            CasinoPlayTracker.RecordSpin(play, Start.AddSeconds(i * 3), 0);

        Assert.That(CasinoPlayTracker.IsCoolingDown(play, Start.AddSeconds(3000)), Is.False);
        Assert.That(play.Spins, Is.EqualTo(1000));
        Assert.That(play.ProfitableWins, Is.Zero);
    }

    [Test]
    public void ProfitableWinsDoNotLimitFurtherPlay()
    {
        var play = new CasinoDailyPlay();
        var payouts = new[] { 500, 1000, 10000 };
        for (var i = 0; i < 1000; i++)
            CasinoPlayTracker.RecordSpin(play, Start.AddSeconds(i * 3), payouts[i % payouts.Length]);

        Assert.That(play.Spins, Is.EqualTo(1000));
        Assert.That(play.ProfitableWins, Is.EqualTo(1000));
        Assert.That(CasinoPlayTracker.IsCoolingDown(play, Start.AddSeconds(3000)), Is.False);
    }

    [Test]
    public void BreakEvenResultsUseSpinsWithoutConsumingProfitableWins()
    {
        var play = new CasinoDailyPlay();
        for (var i = 0; i < 20; i++)
            CasinoPlayTracker.RecordSpin(play, Start.AddSeconds(i * 3), CasinoSlot.SpinCost);
        Assert.That(play.Spins, Is.EqualTo(20));
        Assert.That(play.ProfitableWins, Is.Zero);
        Assert.That(CasinoPlayTracker.IsCoolingDown(play, Start.AddSeconds(60)), Is.False);
    }

    [Test]
    public void CooldownRejectsImmediateRetriesWithoutChangingStatistics()
    {
        var play = new CasinoDailyPlay();
        CasinoPlayTracker.RecordSpin(play, Start, 500);
        Assert.That(CasinoPlayTracker.IsCoolingDown(play, Start.AddSeconds(2)), Is.True);
        Assert.Throws<InvalidOperationException>(() => CasinoPlayTracker.RecordSpin(play, Start.AddSeconds(2), 1000));
        Assert.That(play.Spins, Is.EqualTo(1));
        Assert.That(play.ProfitableWins, Is.EqualTo(1));
        CasinoPlayTracker.RecordSpin(play, Start.AddSeconds(3), 0);
        Assert.That(play.Spins, Is.EqualTo(2));
    }

    [Test]
    public void DailyStatisticsResetAtTenUtcWithoutClearingAnActiveCooldown()
    {
        var beforeReset = new DateTime(2026, 10, 23, 9, 59, 59, DateTimeKind.Utc);
        var play = new CasinoDailyPlay();
        CasinoPlayTracker.RecordSpin(play, beforeReset, 10000);
        play.Spins = 20;
        Assert.That(CasinoPlayTracker.IsCoolingDown(play, beforeReset), Is.True);
        Assert.That(CasinoPlayTracker.IsCoolingDown(play, beforeReset.AddSeconds(1)), Is.True);
        Assert.That(play.Spins, Is.Zero);
        Assert.That(play.ProfitableWins, Is.Zero);
        Assert.That(CasinoPlayTracker.IsCoolingDown(play, beforeReset.AddSeconds(3)), Is.False);
    }

    [Test]
    public void MidnightDoesNotResetTheServerDay()
    {
        var play = new CasinoDailyPlay();
        CasinoPlayTracker.RecordSpin(play, Start, 500);
        CasinoPlayTracker.ResetIfNeeded(play, new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc));
        Assert.That(play.Spins, Is.EqualTo(1));
        Assert.That(play.ProfitableWins, Is.EqualTo(1));
        Assert.That(CasinoPlayTracker.GetNextResetUtc(Start),
            Is.EqualTo(new DateTime(2026, 10, 23, 10, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void SaveReloadAndCharacterSwitchingRetainStatisticsAndCooldownWithoutLimitingPlay()
    {
        var accountId = CasinoPlayTracker.GetAccountId("test-account");
        var play = new CasinoDailyPlay { Id = accountId };
        for (var cycle = 0; cycle < 100; cycle++)
        {
            CasinoPlayTracker.RecordSpin(play, Start.AddSeconds(cycle * 3), 500);
            play = JsonConvert.DeserializeObject<CasinoDailyPlay>(JsonConvert.SerializeObject(play))!;
            Assert.That(play.Id, Is.EqualTo(CasinoPlayTracker.GetAccountId("test-account")));
            Assert.That(play.Spins, Is.EqualTo(cycle + 1));
            Assert.That(play.ProfitableWins, Is.EqualTo(cycle + 1));
            Assert.That(CasinoPlayTracker.IsCoolingDown(play, Start.AddSeconds(cycle * 3 + 1)), Is.True);
        }
        Assert.That(CasinoPlayTracker.IsCoolingDown(play, Start.AddMinutes(5)), Is.False);
        Assert.That(CasinoPlayTracker.GetAccountId("different-account"), Is.Not.EqualTo(play.Id));
        Assert.That(accountId, Is.Not.EqualTo("test-account"));
    }

    [TestCase(-1)]
    [TestCase(10001)]
    public void InvalidPayoutDoesNotChangeStatistics(int payout)
    {
        var play = new CasinoDailyPlay();
        Assert.Throws<ArgumentOutOfRangeException>(() => CasinoPlayTracker.RecordSpin(play, Start, payout));
        Assert.That(play.Spins, Is.Zero);
        Assert.That(play.ProfitableWins, Is.Zero);
    }

    [TestCase("")]
    [TestCase(" ")]
    public void MissingAccountCannotPlay(string account) =>
        Assert.Throws<ArgumentException>(() => CasinoPlayTracker.GetAccountId(account));
}
