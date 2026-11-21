namespace SWLOR.Game.Server.Service.SlicingService
{
    /// <summary>What a space slicing success's bonus pick gives (plan section 24, "Rewards").</summary>
    public enum SpaceSlicingRewardKind
    {
        Invalid = 0,
        Credits = 1,
        Component = 2,
        RareComponent = 3,
        Ammo = 4,
        DesignSchematic = 5,
        RecipeUnlock = 6,
        ShipSchematic = 7,
        AgentIntel = 8,
        PrototypeTech = 9,
    }
}
