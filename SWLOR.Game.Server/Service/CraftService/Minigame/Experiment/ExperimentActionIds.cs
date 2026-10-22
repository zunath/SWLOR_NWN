using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Experiment;

/// <summary>The ids of the Experiment's actions and meters. Test is one action per candidate; the others are shared.</summary>
public static class ExperimentActionIds
{
    public const string TestFixed = "test_fixed";
    public const string TestLicensedRuns = "test_runs";
    public const string TestCreditReduction = "test_credit";
    public const string TestTimeReduction = "test_time";
    public const string RefineMethod = "refine";
    public const string Analyze = "analyze";
    public const string Conclude = "conclude";

    public const string TimeCutMeter = "timecut";
    public const string MethodMeter = "method";

    /// <summary>The Test action that gathers evidence toward a candidate.</summary>
    public static string TestFor(ResearchCandidate candidate) => candidate switch
    {
        ResearchCandidate.FixedGrant => TestFixed,
        ResearchCandidate.LicensedRuns => TestLicensedRuns,
        ResearchCandidate.CreditReduction => TestCreditReduction,
        ResearchCandidate.TimeReduction => TestTimeReduction,
        _ => throw new ArgumentOutOfRangeException(nameof(candidate), candidate, "That candidate cannot be tested.")
    };

    /// <summary>The candidate a Test action gathers evidence toward; false for any other action.</summary>
    public static bool TryGetCandidate(string actionId, out ResearchCandidate candidate)
    {
        candidate = actionId switch
        {
            TestFixed => ResearchCandidate.FixedGrant,
            TestLicensedRuns => ResearchCandidate.LicensedRuns,
            TestCreditReduction => ResearchCandidate.CreditReduction,
            TestTimeReduction => ResearchCandidate.TimeReduction,
            _ => ResearchCandidate.None
        };

        return candidate != ResearchCandidate.None;
    }

    /// <summary>The id of the meter that shows the evidence on a candidate.</summary>
    public static string EvidenceMeter(ResearchCandidate candidate) => $"evidence_{candidate.ToString().ToLowerInvariant()}";
}
