namespace SWLOR.Game.Server.Service.CraftService.Research;

/// <summary>
/// What an experiment gathers evidence toward. A fixed level has one candidate, its own grant; a random level has the three blueprint
/// bonuses. Saved on research jobs by name, so a member is never renamed or removed.
/// </summary>
public enum ResearchCandidate
{
    None = 0,
    FixedGrant = 1,
    LicensedRuns = 2,
    CreditReduction = 3,
    TimeReduction = 4
}
