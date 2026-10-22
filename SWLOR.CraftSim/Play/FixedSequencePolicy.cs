using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Play
{
    /// <summary>
    /// A fixed rotation: the same actions in the same order whatever happens. An action that is not available when its turn comes
    /// (not enough CP, disabled by the state) is skipped. When the list runs out before the attempt ends, the last action repeats for
    /// as long as it is available, as a rotation such as "Wire, Tune, Tune, Tune..." does, and then the player stops.
    /// </summary>
    public sealed class FixedSequencePolicy : ICraftPolicy
    {
        private readonly IReadOnlyList<string> _sequence;
        private int _next;

        public FixedSequencePolicy(IReadOnlyList<string> sequence)
        {
            _sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
        }

        public string Name => "Fixed rotation";

        public IReadOnlyList<string> Sequence => _sequence;

        public string Choose(ICraftMinigameEngine engine)
        {
            while (_next < _sequence.Count)
            {
                var action = _sequence[_next++];
                if (engine.Preview(action).Enabled)
                    return action;
            }

            if (_sequence.Count == 0)
                return null;

            var last = _sequence[^1];
            return engine.Preview(last).Enabled ? last : null;
        }

        /// <summary>The rotation in words, with a run of one action written once: "stoke x2 > press x3 > polish".</summary>
        public static string Describe(IReadOnlyList<string> sequence)
        {
            if (sequence.Count == 0)
                return "(none)";

            var parts = new List<string>();
            for (var index = 0; index < sequence.Count;)
            {
                var run = 1;
                while (index + run < sequence.Count && sequence[index + run] == sequence[index])
                    run++;

                parts.Add(run == 1 ? sequence[index] : $"{sequence[index]} x{run}");
                index += run;
            }

            return string.Join(" > ", parts);
        }
    }
}
