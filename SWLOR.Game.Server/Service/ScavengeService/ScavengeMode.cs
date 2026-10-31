namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>How a session was played.</summary>
    public enum ScavengeMode
    {
        /// <summary>On the board, one action at a time.</summary>
        Board = 0,

        /// <summary>By Quick Search, in one go.</summary>
        Quick = 1
    }
}
