namespace SWLOR.Game.Server.Service.DuelService
{
    /// <summary>
    /// The result of a finished duel. A draw has no winner.
    /// </summary>
    public readonly record struct DuelOutcome(DuelOutcomeType Type, DuelSide Winner)
    {
        /// <summary>The side that lost, or <see cref="DuelSide.None"/> for a draw.</summary>
        public DuelSide Loser => Winner switch
        {
            DuelSide.Challenger => DuelSide.Target,
            DuelSide.Target => DuelSide.Challenger,
            _ => DuelSide.None
        };
    }
}
