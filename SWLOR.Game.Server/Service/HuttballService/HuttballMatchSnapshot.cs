using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.HuttballService
{
    /// <summary>
    /// Everything a re-roll puts back before rolling an action again: each player's place and turn state, the
    /// ball, the clock, the score, and the bribes a dirty move may have spent. The play-by-play is not restored,
    /// so the rolls a re-roll replaces stay on record.
    /// </summary>
    public sealed record HuttballMatchSnapshot(
        IReadOnlyList<HuttballMatchSnapshot.PlayerState> Players,
        HuttballPhase Phase,
        int Half,
        int Turn,
        HuttballTeam ActiveTeam,
        uint? BallCarrier,
        HuttballSquare? LooseBall,
        IReadOnlyDictionary<HuttballTeam, int> Score,
        IReadOnlyDictionary<HuttballTeam, int> Bribes)
    {
        public sealed record PlayerState(
            uint Creature,
            HuttballPlayerStatus Status,
            HuttballSquare? Square,
            int MovementLeft,
            bool HasActed,
            bool IsDone);
    }
}
