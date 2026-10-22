namespace SWLOR.Game.Server.Service.CraftService.Research;

/// <summary>
/// What an experiment gathers evidence toward. A fixed level has one candidate, its own grant, and so does a random level that rolled
/// licensed runs; any other random level has credit reduction and time reduction. Saved on research jobs by name, so a member is never renamed or removed.
/// </summary>
public enum ResearchCandidate
{
    None = 0,
    FixedGrant = 1,
    LicensedRuns = 2,
    CreditReduction = 3,
    TimeReduction = 4
}
