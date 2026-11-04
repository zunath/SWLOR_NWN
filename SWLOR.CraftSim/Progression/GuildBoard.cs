namespace SWLOR.CraftSim.Progression
{
    /// <summary>
    /// The guild task board. At each server start <c>Guild.RefreshGuildTasks</c> keeps all of a tier's tasks when it has ten or fewer, and
    /// otherwise a random ten, so a crafter can hand in only the tasks that are on this boot's board. The chances below follow from drawing
    /// those ten uniformly; over a climb that spans many server starts they are the expected share of boards that hold a task.
    /// </summary>
    public static class GuildBoard
    {
        /// <summary>Tasks <c>Guild.RefreshGuildTasks</c> keeps per tier when the tier has more.</summary>
        public const int TasksPerTier = 10;

        /// <summary>
        /// The chance a board holds at least one of <paramref name="matching"/> tasks, out of a tier of <paramref name="tierTasks"/> of which
        /// <paramref name="board"/> are drawn.
        /// </summary>
        public static double ChanceAtLeastOne(int tierTasks, int matching, int board = TasksPerTier)
        {
            if (matching <= 0 || tierTasks <= 0)
                return 0;

            if (tierTasks <= board)
                return 1;

            // None of the matching tasks is drawn: (N - B)/N x (N - B - 1)/(N - 1) x ... for each matching task.
            var none = 1.0;
            for (var task = 0; task < matching; task++)
            {
                var left = tierTasks - board - task;
                if (left <= 0)
                    return 1;

                none *= left / (double)(tierTasks - task);
            }

            return 1 - none;
        }

        /// <summary>
        /// For the tasks a crafter would pick from, in order of preference, the chance the board holds at least one, and the expected cost of
        /// the preferred task that is on it given that one is.
        /// </summary>
        /// <param name="costs">The cost of each eligible task, most preferred first.</param>
        /// <param name="tierTasks">All the tasks in the tier, eligible or not.</param>
        public static (double ChanceAny, double ExpectedCost) PreferredOnBoard(IReadOnlyList<double> costs, int tierTasks, int board = TasksPerTier)
        {
            if (costs.Count == 0 || tierTasks <= 0)
                return (0, 0);

            if (tierTasks <= board)
                return (1, costs[0]);

            // Task i (1-based) is the preferred one on the board when it is drawn (B/N) and none of the i - 1 before it are, among the
            // other B - 1 draws: C(N - i, B - 1) / C(N - 1, B - 1), which falls by (N - i - B + 1)/(N - i) from one task to the next.
            double chance = 0, weighted = 0;
            var ratio = 1.0;
            for (var i = 1; i <= costs.Count; i++)
            {
                var probability = board / (double)tierTasks * ratio;
                chance += probability;
                weighted += probability * costs[i - 1];

                var denominator = tierTasks - i;
                if (denominator <= 0)
                    break;

                ratio *= Math.Max(0, tierTasks - i - board + 1) / (double)denominator;
                if (ratio <= 0)
                    break;
            }

            return (chance, weighted / chance);
        }
    }
}
