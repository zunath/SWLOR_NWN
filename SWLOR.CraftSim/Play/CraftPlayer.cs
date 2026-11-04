using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Play
{
    /// <summary>Chooses a player's next action from the engine as the window shows it.</summary>
    public interface ICraftPolicy
    {
        string Name { get; }

        /// <summary>The id of the action to perform, or null when the player has nothing useful to do and gives up. A policy is used for one attempt.</summary>
        string Choose(ICraftMinigameEngine engine);
    }

    /// <summary>How one simulated attempt ended.</summary>
    /// <param name="Status">Succeeded, Failed, or InProgress when the player gave up or reached the action limit.</param>
    /// <param name="QualityFraction">The quality meter's share of its maximum when the attempt ended.</param>
    /// <param name="ReachedMaxQuality">True when the item was made with the quality meter full.</param>
    /// <param name="Actions">Actions performed.</param>
    /// <param name="CpSpent">CP spent.</param>
    /// <param name="QualityPercent">The quality meter as a whole percentage, rounded down: the "quality percent" the enhancement thresholds are compared with (<c>MinigameMeter.Percent</c>).</param>
    public readonly record struct CraftPlayResult(
        MinigameStatus Status, double QualityFraction, bool ReachedMaxQuality, int Actions, int CpSpent, int QualityPercent = 0)
    {
        public bool Succeeded => Status == MinigameStatus.Succeeded;

        /// <summary>The attempt ended with neither an item nor a broken piece: the player gave up or stopped at the action limit.</summary>
        public bool Stalled => Status == MinigameStatus.InProgress;

        /// <summary>The quality of the item made, or 0 when no item was: the number the gates average.</summary>
        public double Score => Succeeded ? QualityFraction : 0;
    }

    /// <summary>Plays one attempt to its end with a policy.</summary>
    public static class CraftPlayer
    {
        /// <summary>No policy needs this many actions; an attempt that reaches it counts as stalled, which is no item.</summary>
        public const int DefaultActionLimit = 100;

        public static CraftPlayResult Play(ICraftMinigameEngine engine, ICraftPolicy policy, int actionLimit = DefaultActionLimit)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            var startingTurn = engine.Turn;
            while (engine.Status == MinigameStatus.InProgress && engine.Turn - startingTurn < actionLimit)
            {
                var action = policy.Choose(engine);
                if (action == null || !engine.Perform(action).Performed)
                    break;
            }

            return Result(engine, startingTurn);
        }

        /// <summary>
        /// Plays a fixed rotation: the actions in order, an unavailable one skipped (a refused action changes nothing and spends no chance,
        /// and an action this engine does not offer at all is unavailable, so a rotation shared by several recipes can play on engines whose
        /// action lists differ), and the last one repeated while it stays available once the list runs out. It gives the same result as
        /// playing a <see cref="FixedSequencePolicy"/>, with one rules evaluation per action instead of two.
        /// </summary>
        public static CraftPlayResult PlayFixed(ICraftMinigameEngine engine, IReadOnlyList<string> sequence, int actionLimit = DefaultActionLimit)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));

            var startingTurn = engine.Turn;
            var next = 0;
            while (engine.Status == MinigameStatus.InProgress && engine.Turn - startingTurn < actionLimit)
            {
                if (next < sequence.Count)
                {
                    var action = sequence[next++];
                    if (Offers(engine, action))
                        engine.Perform(action);

                    continue;
                }

                if (sequence.Count == 0 || !Offers(engine, sequence[^1]) || !engine.Perform(sequence[^1]).Performed)
                    break;
            }

            return Result(engine, startingTurn);
        }

        /// <summary>True when the engine has an action of this id at all (it may still be disabled right now).</summary>
        internal static bool Offers(ICraftMinigameEngine engine, string actionId)
        {
            var actions = engine.Actions;
            for (var index = 0; index < actions.Count; index++)
            {
                if (actions[index].Id == actionId)
                    return true;
            }

            return false;
        }

        private static CraftPlayResult Result(ICraftMinigameEngine engine, int startingTurn)
        {
            var meters = engine.Meters;
            return new CraftPlayResult(
                engine.Status,
                meters.Quality.Fraction,
                engine.Status == MinigameStatus.Succeeded && meters.Quality.IsFull,
                engine.Turn - startingTurn,
                meters.Cp.Max - meters.Cp.Value,
                meters.Quality.Percent);
        }
    }
}
