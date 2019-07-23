namespace SWLOR.Game.Server.NWNX
{
    // I don't understand why NWNX had to make separate IDs for these, but they did.
    // Corresponds to the NWNX_Events plugin methods.
    public enum NWNXEventObjectType
    {
        Creature = 5,
        Item = 6,
        Trigger = 7,
        Placeable = 9,
        Waypoint = 12,
        Encounter = 13,
        Portal = 15
    }
}
