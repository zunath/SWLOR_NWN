namespace SWLOR.Game.Server.Service.CraftService.Research;

/// <summary>What completing a research job gave the blueprint, for the message the player reads.</summary>
/// <param name="Level">The blueprint's new research level.</param>
/// <param name="Grant">What the level gives.</param>
/// <param name="Candidate">For a random level, the bonus that was granted; otherwise <see cref="ResearchCandidate.FixedGrant"/>.</param>
/// <param name="Amount">For a random level, the size of the bonus: licensed runs, or percent.</param>
/// <param name="ExtraBonusTier">The tier of the extra guaranteed bonus that was granted, or 0 when there was none.</param>
/// <param name="FromExperiment">True when the stored experiment outcome decided the grant; false when today's random roll did.</param>
public sealed record ResearchCompletion(
    int Level,
    ResearchLevelGrant Grant,
    ResearchCandidate Candidate,
    int Amount,
    int ExtraBonusTier,
    bool FromExperiment);
