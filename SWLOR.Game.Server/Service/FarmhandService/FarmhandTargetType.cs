namespace SWLOR.Game.Server.Service.FarmhandService
{
    /// <summary>What a farmhand droid is assigned to.</summary>
    public enum FarmhandTargetType
    {
        Invalid = 0,

        /// <summary>One of its owner's houses. The droid tends its owner's crops in the house's garden plots.</summary>
        House = 1,

        /// <summary>A corral. The droid feeds and collects from its owner's animals in it.</summary>
        Corral = 2,
    }
}
