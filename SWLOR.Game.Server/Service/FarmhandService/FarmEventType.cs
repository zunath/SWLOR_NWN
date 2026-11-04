namespace SWLOR.Game.Server.Service.FarmhandService
{
    /// <summary>One thing that falls due in a farmhand's replay, in the order ties between events at the same moment are applied.</summary>
    public enum FarmEventType
    {
        Invalid = 0,

        /// <summary>An animal went unfed for 72 hours and wandered off.</summary>
        WanderedOff = 1,

        /// <summary>A crop was left ripe for 72 hours and withered.</summary>
        Withered = 2,

        /// <summary>The droid tended a crop that became ready.</summary>
        Tended = 3,

        /// <summary>The droid fed an animal.</summary>
        Fed = 4,

        /// <summary>A young animal grew up.</summary>
        Matured = 5,

        /// <summary>The droid collected from an animal into the output hopper.</summary>
        Collected = 6,

        /// <summary>A crop ripened.</summary>
        Ripened = 7,
    }
}
