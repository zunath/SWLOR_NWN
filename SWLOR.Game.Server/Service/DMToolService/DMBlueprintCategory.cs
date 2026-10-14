namespace SWLOR.Game.Server.Service.DMToolService
{
    /// <summary>
    /// The kinds of blueprints the DM creator can spawn. Values are stored as combo box option values.
    /// </summary>
    public enum DMBlueprintCategory
    {
        Creatures = 0,
        Placeables = 1,
        Items = 2,
        Stores = 3,
        Waypoints = 4,
        Triggers = 5,
        Encounters = 6,
        Doors = 7
    }
}
