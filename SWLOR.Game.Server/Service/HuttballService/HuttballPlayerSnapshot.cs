namespace SWLOR.Game.Server.Service.HuttballService
{
    /// <summary>
    /// One player's place and turn state, as a re-roll puts it back.
    /// </summary>
    public sealed record HuttballPlayerSnapshot(
        uint Creature,
        HuttballPlayerStatus Status,
        HuttballSquare? Square,
        int MovementLeft,
        bool HasActed,
        bool IsDone);
}
