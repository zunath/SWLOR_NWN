namespace SWLOR.Game.Server.Service.FarmingService
{
    /// <summary>Where a garden plot's crop is in its life.</summary>
    public enum GardenPlotStage
    {
        /// <summary>Nothing is planted.</summary>
        Empty = 0,

        /// <summary>Growing, and too young to tend yet or already tended.</summary>
        Growing = 1,

        /// <summary>Growing and old enough to tend, and not tended yet.</summary>
        ReadyToTend = 2,

        /// <summary>Ripe: the planter can harvest it.</summary>
        Ripe = 3,

        /// <summary>Left ripe too long. The crop and its seed are lost.</summary>
        Withered = 4,
    }
}
