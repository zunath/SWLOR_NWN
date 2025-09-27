namespace SWLOR.NWN.API.NWScript.Enum
{
    [Flags]
    public enum ObjectType
    {
        Creature = 1,
        Item = 2,
        Trigger = 4,
        Door = 8,
        AreaOfEffect = 16,
        Waypoint = 32,
        Placeable = 64,
        Player = 69,
        Store = 128,
        Encounter = 256,
        Tile = 512,
        All = 32767,
        Invalid = 32767
    }
}