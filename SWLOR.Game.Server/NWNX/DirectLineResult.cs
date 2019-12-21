namespace SWLOR.Game.Server.NWNX
{
    public enum DirectLineResult
    {
        DirectWalkableLine = 1,
        BlockedByTerrain = -1,
        BlockedByPlaceable = -2,
        BlockedByCreature = -3
    }
}
