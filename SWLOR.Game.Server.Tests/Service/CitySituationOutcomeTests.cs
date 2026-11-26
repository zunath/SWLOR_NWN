using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// A City Affairs decision or expiry is saved with its city effects in one city write, and the situation is settled from it
/// once when a restart came before the situation's own save.
/// </summary>
public class CitySituationOutcomeTests
{
    private static readonly DateTime Now = new(2026, 11, 23, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void ADecisionTheCityHolds_SettlesTheSituationARestartLeftOpen()
    {
        var decided = new CitySituation
        {
            Id = "s1", CityId = "c1", Type = SituationType.SmugglersOffer, State = CitySituationState.Decided,
            ChosenOption = 1, DecidedByPlayerId = "p1", ResolvedUtc = Now, TreasuryGain = 5000
        };
        var outcome = CitySituationOutcome.Capture(decided);
        var saved = new CitySituation { Id = "s1", CityId = "c1", Type = SituationType.SmugglersOffer, State = CitySituationState.Open };

        outcome.ApplyTo(saved).Should().BeTrue();
        saved.State.Should().Be(CitySituationState.Decided);
        saved.ChosenOption.Should().Be(1);
        saved.DecidedByPlayerId.Should().Be("p1");
        saved.ResolvedUtc.Should().Be(Now);
        saved.TreasuryGain.Should().Be(5000, "the gain counts toward the weekly cap");

        outcome.ApplyTo(saved).Should().BeFalse("a settled situation is never settled again");
    }

    [Test]
    public void AnExpiryTheCityHolds_SettlesTheSituationAsExpired()
    {
        var expired = new CitySituation { Id = "s2", State = CitySituationState.Expired, ResolvedUtc = Now };
        var saved = new CitySituation { Id = "s2", State = CitySituationState.Open };

        CitySituationOutcome.Capture(expired).ApplyTo(saved).Should().BeTrue();
        saved.State.Should().Be(CitySituationState.Expired);
        saved.ChosenOption.Should().Be(-1);
        saved.DecidedByPlayerId.Should().BeEmpty();
    }

    [Test]
    public void AnOutcome_LeavesOtherAndAlreadySavedSituationsAlone()
    {
        var outcome = CitySituationOutcome.Capture(new CitySituation { Id = "s1", State = CitySituationState.Decided, ChosenOption = 0, ResolvedUtc = Now });

        outcome.ApplyTo(null).Should().BeFalse("a pruned situation has nothing to settle");
        outcome.ApplyTo(new CitySituation { Id = "s9", State = CitySituationState.Open }).Should().BeFalse();
        var alreadySaved = new CitySituation { Id = "s1", State = CitySituationState.Decided, ChosenOption = 2, DecidedByPlayerId = "p2" };
        outcome.ApplyTo(alreadySaved).Should().BeFalse("its own save already settled it");
        alreadySaved.ChosenOption.Should().Be(2);
    }

    [Test]
    public void DecideAndExpire_SaveTheEffectsAndOutcomeInTheCityBeforeTheSituation()
    {
        var source = ReadServerSource("Service", "CityAffairs.cs");

        var save = Body(source, "private static void SaveOutcome(");
        var mark = save.IndexOf("city.PendingSituationOutcome = CitySituationOutcome.Capture(situation, holdingEffects);\n            DB.Set(city);", StringComparison.Ordinal);
        var holding = save.IndexOf("HoldingCityAffairs.ApplyOutcome(city, city.PendingSituationOutcome, DateTime.UtcNow);", StringComparison.Ordinal);
        save.IndexOf("CreateFollowUps(city.PendingSituationOutcome);", StringComparison.Ordinal)
            .Should().BeInRange(mark, save.IndexOf("DB.Set(situation);", StringComparison.Ordinal),
                "a follow-up is created only once the city holds the decision that queues it");
        Body(source, "private static void QueueFollowUp(").Should().NotContain("DB.Set(", "the follow-up rides the outcome")
            .And.Contain("outcomeEffects.FollowUps.Add(followUp);");
        var situation = save.IndexOf("DB.Set(situation);", StringComparison.Ordinal);
        var clear = save.IndexOf("city.PendingSituationOutcome = null;\n            DB.Set(city);", StringComparison.Ordinal);
        mark.Should().BeGreaterThanOrEqualTo(0, "the city's effects and the outcome are saved in one write");
        holding.Should().BeGreaterThan(mark, "the holding changes only once the city holds the outcome that pays for it");
        situation.Should().BeGreaterThan(holding, "the situation is saved only after the city holds its outcome");
        clear.Should().BeGreaterThan(situation, "the outcome is cleared once the situation is saved");

        foreach (var method in new[] { "private static void Expire(", "public static CityDecisionResult Decide(" })
        {
            var body = Body(source, method);
            body.Should().Contain("SaveOutcome(city, situation, holdingEffects);", method);
            body.Should().NotContain("DB.Set(situation);", $"{method} saves the situation only through SaveOutcome");
            body.IndexOf("ApplyEffects(city, situation,", StringComparison.Ordinal)
                .Should().BeInRange(0, body.IndexOf("SaveOutcome(city, situation, holdingEffects);", StringComparison.Ordinal), method);
        }

        var decide = Body(source, "public static CityDecisionResult Decide(");
        decide.IndexOf("SettlePendingOutcome(city);", StringComparison.Ordinal)
            .Should().BeInRange(0, decide.IndexOf("if (situation.State != CitySituationState.Open)", StringComparison.Ordinal),
                "a situation whose outcome reached the city is settled before anyone can decide it again");

        var process = Body(source, "private static void ProcessCity(");
        process.IndexOf("SettlePendingOutcome(city);", StringComparison.Ordinal)
            .Should().BeInRange(0, process.IndexOf("var situations = LoadSituations(city.Id);", StringComparison.Ordinal),
                "the restart pass settles it before expiring anything");

        var settle = Body(source, "public static void SettlePendingOutcome(");
        settle.IndexOf("HoldingCityAffairs.ApplyOutcome(city, outcome, DateTime.UtcNow)", StringComparison.Ordinal)
            .Should().BeInRange(0, settle.IndexOf("city.PendingSituationOutcome = null;", StringComparison.Ordinal),
                "an outcome a restart left on the city changes its holding before the outcome is cleared");
        Body(source, "private static int ApplyEffects(").Should().NotContain("DB.Set(", "the effects reach records only through SaveOutcome");
        settle
            .Should().Contain("if (outcome.ApplyTo(situation))\n            {\n                DB.Set(situation);")
            .And.Contain("city.PendingSituationOutcome = null;\n            DB.Set(city);");
    }

    private static string Body(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, signature);
        var rest = source[(start + signature.Length)..];
        var next = rest.IndexOf("\n        private static ", StringComparison.Ordinal);
        var nextPublic = rest.IndexOf("\n        public static ", StringComparison.Ordinal);
        if (next < 0 || (nextPublic >= 0 && nextPublic < next))
            next = nextPublic;
        return next < 0 ? rest : rest[..next];
    }

    private static string ReadServerSource(params string[] path)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        var full = Path.Combine(new[] { directory!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray());
        return File.ReadAllText(full).Replace("\r\n", "\n");
    }
}
