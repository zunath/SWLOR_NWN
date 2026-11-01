namespace SWLOR.Game.Server.Entity
{
    /// <summary>Where a research job's experiment stands. Saved by name, so a member is never renamed.</summary>
    public enum ResearchExperimentStatus
    {
        /// <summary>The player is still working the experiment, or left without it being settled yet.</summary>
        InProgress = 0,

        /// <summary>The experiment ended and its outcome is stored; completion grants exactly that.</summary>
        Settled = 1
    }
}
