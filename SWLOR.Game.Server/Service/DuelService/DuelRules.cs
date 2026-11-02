namespace SWLOR.Game.Server.Service.DuelService
{
    /// <summary>
    /// The Dueling Ring's rules, with no engine calls: which blows interfere, how long protection lasts, when a duelist
    /// counts as subdued, who wins, and what the Gazette says. <see cref="Duels"/> feeds them the game's data.
    /// </summary>
    public static class DuelRules
    {
        /// <summary>How long both duelists stay protected after a third party voids the duel.</summary>
        public const double ProtectionSeconds = 6.0;

        /// <summary>How long a challenge waits for an answer.</summary>
        public const double ChallengeSeconds = 30.0;

        /// <summary>Duelists who fall within this many seconds of each other fell in the same pass: a draw.</summary>
        public const double SamePassSeconds = 1.0;

        /// <summary>The ring's radius in meters. A duelist who leaves it forfeits.</summary>
        public const float RingRadiusMeters = 10.0f;

        /// <summary>Which side a creature is on, or <see cref="DuelSide.None"/> when it is not a duelist.</summary>
        public static DuelSide GetSide(DuelRecord record, uint creature)
        {
            if (creature == record.Challenger)
                return DuelSide.Challenger;

            return creature == record.Target ? DuelSide.Target : DuelSide.None;
        }

        /// <summary>
        /// Traces a blow back to who is behind it. A pet, droid or beast stands for the player it fights for, and
        /// otherwise for its own master; a damaging effect arrives as its creator. Anything else stands for itself.
        /// </summary>
        /// <param name="attacker">The creature, or damaging effect's creator, behind the blow.</param>
        /// <param name="isValid">Whether an id names a creature that exists.</param>
        /// <param name="getControllingPlayer">The player a creature fights for, or an invalid id.</param>
        /// <param name="getMaster">A creature's own master, or an invalid id.</param>
        /// <returns>The source, or <paramref name="attacker"/> unchanged when it is invalid.</returns>
        public static uint TraceSource(
            uint attacker,
            Func<uint, bool> isValid,
            Func<uint, uint> getControllingPlayer,
            Func<uint, uint> getMaster)
        {
            if (!isValid(attacker))
                return attacker;

            var player = getControllingPlayer(attacker);
            if (isValid(player))
                return player;

            var master = getMaster(attacker);
            return isValid(master) ? master : attacker;
        }

        /// <summary>
        /// Classifies a blow on a duelist by its source, already traced back from a pet, droid or damaging effect to
        /// its owner or creator. A blow from either duelist is part of the duel; from anyone else it interferes; with
        /// no source it is neither.
        /// </summary>
        /// <param name="record">The duel.</param>
        /// <param name="hasSource">False when no creature could be named as the source.</param>
        /// <param name="resolvedSource">The source after tracing it back to its owner or creator.</param>
        public static DuelBlowType ClassifyBlow(DuelRecord record, bool hasSource, uint resolvedSource)
        {
            if (!hasSource)
                return DuelBlowType.Unattributed;

            return GetSide(record, resolvedSource) == DuelSide.None
                ? DuelBlowType.Interference
                : DuelBlowType.Duel;
        }

        /// <summary>
        /// A third party voided the duel: it becomes Interrupted and both duelists stay protected for
        /// <see cref="ProtectionSeconds"/> from now. A duel that is already Interrupted keeps its original deadline, so
        /// further blows cannot stretch the protection.
        /// </summary>
        /// <returns>True when this call interrupted the duel.</returns>
        public static bool Interrupt(DuelRecord record, DateTime nowUtc)
        {
            if (record.State != DuelState.Active)
                return false;

            record.State = DuelState.Interrupted;
            record.ProtectedUntilUtc = nowUtc.AddSeconds(ProtectionSeconds);
            record.InterruptionNoticePending = true;
            return true;
        }

        /// <summary>
        /// Whether a blow that would kill a duelist subdues them instead: while the duel is Active, and for the
        /// protection window after it was Interrupted.
        /// </summary>
        public static bool ShouldSubdue(DuelRecord record, DateTime nowUtc)
        {
            return record.State == DuelState.Active || record.ProtectedUntilUtc > nowUtc;
        }

        /// <summary>Whether the duel has nothing left to do: it was interrupted and the protection has lapsed.</summary>
        public static bool IsOver(DuelRecord record, DateTime nowUtc)
        {
            return record.State == DuelState.Interrupted && record.ProtectedUntilUtc <= nowUtc;
        }

        /// <summary>
        /// Notes that a duelist fell. The first fall starts the clock that lets a second fall in the same pass make the
        /// duel a draw. A fall after that pass counts for nothing: the duel already has its result.
        /// </summary>
        /// <returns>True when this was the first fall.</returns>
        public static bool RecordFall(DuelRecord record, DuelSide side, DateTime nowUtc)
        {
            if (side == DuelSide.None)
                return false;

            if (record.FirstFallUtc.HasValue)
            {
                if (!IsSamePass(record.FirstFallUtc.Value, nowUtc))
                    return false;

                MarkFallen(record, side);
                return false;
            }

            MarkFallen(record, side);
            record.FirstFallUtc = nowUtc;
            return true;
        }

        /// <summary>Whether two falls happened within <see cref="SamePassSeconds"/> of each other.</summary>
        public static bool IsSamePass(DateTime firstFallUtc, DateTime laterFallUtc)
        {
            return (laterFallUtc - firstFallUtc).TotalSeconds <= SamePassSeconds;
        }

        private static void MarkFallen(DuelRecord record, DuelSide side)
        {
            if (side == DuelSide.Challenger)
                record.ChallengerFell = true;
            else
                record.TargetFell = true;
        }

        /// <summary>Notes that a duelist left the ring or logged out.</summary>
        public static void RecordForfeit(DuelRecord record, DuelSide side)
        {
            if (side == DuelSide.Challenger)
                record.ChallengerForfeited = true;
            else if (side == DuelSide.Target)
                record.TargetForfeited = true;
        }

        /// <summary>
        /// The result so far, or null while nobody has lost. A side loses by falling or forfeiting. Both sides losing
        /// is a draw, wherever they lost it, so a duelist who falls and a duelist who logs out in the same pass tie.
        /// One side losing gives the other the win, by forfeit when they left and did not fall.
        /// </summary>
        public static DuelOutcome? GetOutcome(DuelRecord record)
        {
            var challengerLost = record.ChallengerFell || record.ChallengerForfeited;
            var targetLost = record.TargetFell || record.TargetForfeited;

            if (challengerLost && targetLost)
                return new DuelOutcome(DuelOutcomeType.Draw, DuelSide.None);

            if (challengerLost)
            {
                return new DuelOutcome(
                    record.ChallengerForfeited && !record.ChallengerFell ? DuelOutcomeType.Forfeit : DuelOutcomeType.Victory,
                    DuelSide.Target);
            }

            if (targetLost)
            {
                return new DuelOutcome(
                    record.TargetForfeited && !record.TargetFell ? DuelOutcomeType.Forfeit : DuelOutcomeType.Victory,
                    DuelSide.Challenger);
            }

            return null;
        }

        /// <summary>Whether a point lies inside the ring, edge included. Height does not matter.</summary>
        public static bool IsInsideRing(float ringX, float ringY, float x, float y, float radius = RingRadiusMeters)
        {
            var dx = x - ringX;
            var dy = y - ringY;
            return dx * dx + dy * dy <= radius * radius;
        }

        /// <summary>
        /// The Gazette line for a finished duel. It names both duelists by how strangers see them, since the Gazette is
        /// public and a disguise must never be undone by it.
        /// </summary>
        public static string BuildResultText(DuelOutcome outcome, string challengerLabel, string targetLabel)
        {
            var challenger = string.IsNullOrWhiteSpace(challengerLabel) ? "Someone" : challengerLabel.Trim();
            var target = string.IsNullOrWhiteSpace(targetLabel) ? "Someone" : targetLabel.Trim();
            var winner = outcome.Winner == DuelSide.Challenger ? challenger : target;
            var loser = outcome.Winner == DuelSide.Challenger ? target : challenger;

            var text = outcome.Type switch
            {
                DuelOutcomeType.Victory => $"{winner} defeated {loser} in a duel at the Dueling Ring.",
                DuelOutcomeType.Forfeit => $"{loser} forfeited a duel against {winner} at the Dueling Ring.",
                _ => $"{challenger} and {target} fought a duel at the Dueling Ring and both fell: a draw."
            };

            return char.ToUpperInvariant(text[0]) + text[1..];
        }
    }
}
