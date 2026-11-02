namespace SWLOR.Game.Server.Service.LivestockService
{
    /// <summary>Where an animal is in its life.</summary>
    public enum LivestockStage
    {
        /// <summary>Not yet 72 hours old with 3 feedings.</summary>
        Young = 1,

        /// <summary>At least 72 hours old with 3 feedings. It can be collected from and butchered.</summary>
        Adult = 2,
    }
}
