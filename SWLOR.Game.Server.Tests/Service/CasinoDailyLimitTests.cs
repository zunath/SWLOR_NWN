using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class CasinoDailyLimitTests
{
    private static readonly DateTime Start = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void TwentyLosingSpinsStopPlayWithoutAnotherReservation()
    {
        var play = new CasinoDailyPlay();
        for (var i = 0; i < 20; i++)
            CasinoDailyLimit.RecordSpin(play, Start.AddSeconds(i * 3), 0);

        Assert.That(CasinoDailyLimit.GetFailure(play, Start.AddMinutes(5)), Does.Contain("20 account spins"));
        Assert.Throws<InvalidOperationException>(() => CasinoDailyLimit.RecordSpin(play, Start.AddMinutes(5), 10000));
        Assert.That(play.Spins, Is.EqualTo(20));
        Assert.That(play.ProfitableWins, Is.Zero);
    }

    [Test]
    public void ThreeProfitableWinsStopPlayAndPreserveTheFinalPayout()
    {
        var play = new CasinoDailyPlay();
        var payouts = new[] { 500, 1000, 10000 };
        for (var i = 0; i < payouts.Length; i++)
            CasinoDailyLimit.RecordSpin(play, Start.AddSeconds(i * 3), payouts[i]);

        Assert.That(play.Spins, Is.EqualTo(3));
        Assert.That(play.ProfitableWins, Is.EqualTo(3));
        Assert.That(CasinoDailyLimit.GetFailure(play, Start.AddMinutes(5)), Does.Contain("3 profitable wins"));
        Assert.Throws<InvalidOperationException>(() => CasinoDailyLimit.RecordSpin(play, Start.AddMinutes(5), 500));
        Assert.That(play.Spins, Is.EqualTo(3));
        Assert.That(play.ProfitableWins, Is.EqualTo(3));
    }

    [Test]
    public void BreakEvenResultsUseSpinsWithoutConsumingProfitableWins()
    {
        var play = new CasinoDailyPlay();
        for (var i = 0; i < 20; i++)
            CasinoDailyLimit.RecordSpin(play, Start.AddSeconds(i * 3), CasinoSlot.SpinCost);
        Assert.That(play.Spins, Is.EqualTo(20));
        Assert.That(play.ProfitableWins, Is.Zero);
        Assert.That(CasinoDailyLimit.GetDailyFailure(play), Is.Not.Empty);
    }

    [Test]
    public void CooldownRejectsImmediateRetriesWithoutConsumingAllowance()
    {
        var play = new CasinoDailyPlay();
        CasinoDailyLimit.RecordSpin(play, Start, 500);
        Assert.That(CasinoDailyLimit.GetFailure(play, Start.AddSeconds(2)), Does.Contain("three seconds"));
        Assert.Throws<InvalidOperationException>(() => CasinoDailyLimit.RecordSpin(play, Start.AddSeconds(2), 1000));
        Assert.That(play.Spins, Is.EqualTo(1));
        Assert.That(play.ProfitableWins, Is.EqualTo(1));
        CasinoDailyLimit.RecordSpin(play, Start.AddSeconds(3), 0);
        Assert.That(play.Spins, Is.EqualTo(2));
    }

    [Test]
    public void DailyResetHappensAtTenUtcAndDoesNotClearAnActiveCooldown()
    {
        var beforeReset = new DateTime(2026, 10, 23, 9, 59, 59, DateTimeKind.Utc);
        var play = new CasinoDailyPlay();
        CasinoDailyLimit.RecordSpin(play, beforeReset, 10000);
        play.Spins = 20;
        Assert.That(CasinoDailyLimit.GetFailure(play, beforeReset), Does.Contain("20 account spins"));
        Assert.That(CasinoDailyLimit.GetFailure(play, beforeReset.AddSeconds(1)), Does.Contain("three seconds"));
        Assert.That(play.Spins, Is.Zero);
        Assert.That(play.ProfitableWins, Is.Zero);
        Assert.That(CasinoDailyLimit.GetFailure(play, beforeReset.AddSeconds(3)), Is.Empty);
    }

    [Test]
    public void MidnightDoesNotResetTheServerDay()
    {
        var play = new CasinoDailyPlay();
        CasinoDailyLimit.RecordSpin(play, Start, 500);
        CasinoDailyLimit.ResetIfNeeded(play, new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc));
        Assert.That(play.Spins, Is.EqualTo(1));
        Assert.That(play.ProfitableWins, Is.EqualTo(1));
        Assert.That(CasinoDailyLimit.GetNextResetUtc(Start),
            Is.EqualTo(new DateTime(2026, 10, 23, 10, 0, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void SaveReloadAndCharacterSwitchingRetainTheSameAccountQuota()
    {
        var accountId = CasinoDailyLimit.GetAccountId("test-account");
        var play = new CasinoDailyPlay { Id = accountId };
        for (var cycle = 0; cycle < 3; cycle++)
        {
            CasinoDailyLimit.RecordSpin(play, Start.AddSeconds(cycle * 3), 500);
            play = JsonConvert.DeserializeObject<CasinoDailyPlay>(JsonConvert.SerializeObject(play))!;
            Assert.That(play.Id, Is.EqualTo(CasinoDailyLimit.GetAccountId("test-account")));
            Assert.That(play.Spins, Is.EqualTo(cycle + 1));
            Assert.That(play.ProfitableWins, Is.EqualTo(cycle + 1));
            Assert.That(CasinoDailyLimit.GetFailure(play, Start.AddSeconds(cycle * 3 + 1)), Is.Not.Empty);
        }
        Assert.That(CasinoDailyLimit.GetFailure(play, Start.AddMinutes(5)), Does.Contain("3 profitable wins"));
        Assert.That(CasinoDailyLimit.GetAccountId("different-account"), Is.Not.EqualTo(play.Id));
        Assert.That(accountId, Is.Not.EqualTo("test-account"));
    }

    [TestCase(-1)]
    [TestCase(10001)]
    public void InvalidPayoutDoesNotConsumeAllowance(int payout)
    {
        var play = new CasinoDailyPlay();
        Assert.Throws<ArgumentOutOfRangeException>(() => CasinoDailyLimit.RecordSpin(play, Start, payout));
        Assert.That(play.Spins, Is.Zero);
        Assert.That(play.ProfitableWins, Is.Zero);
    }

    [TestCase("")]
    [TestCase(" ")]
    public void MissingAccountCannotGetAnAllowance(string account) =>
        Assert.Throws<ArgumentException>(() => CasinoDailyLimit.GetAccountId(account));
}
