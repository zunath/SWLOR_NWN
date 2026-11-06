namespace SWLOR.Game.Server.Service.HuttballService
{
    /// <summary>
    /// Where a Huttball match stands.
    /// </summary>
    public enum HuttballPhase
    {
        /// <summary>Players take their places in their own half before the DM starts a drive.</summary>
        Setup = 1,
        /// <summary>Teams take turns moving and acting.</summary>
        Playing = 2,
        /// <summary>The second half has ended.</summary>
        Finished = 3,
    }
}
