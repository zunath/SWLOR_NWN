using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Scenario.Reaction
{
    /// <summary>
    /// A Reaction engine with Dampen taken away: the action is not offered, previewed or performed. It lets the simulator measure what
    /// Dampen is worth by playing the same recipes without it.
    /// </summary>
    internal sealed class DampenlessEngine : ICraftMinigameEngine
    {
        private const string Dampen = "dampen";

        private readonly ICraftMinigameEngine _inner;
        private readonly IReadOnlyList<MinigameActionInfo> _actions;

        public DampenlessEngine(ICraftMinigameEngine inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _actions = inner.Actions.Where(action => action.Id != Dampen).ToList();
        }

        public CraftMinigameType Type => _inner.Type;

        public MinigameStatus Status => _inner.Status;

        public MinigameMeters Meters => _inner.Meters;

        public int Turn => _inner.Turn;

        public MinigameOpportunity NextOpportunity => _inner.NextOpportunity;

        public IReadOnlyList<MinigameActionInfo> Actions => _actions;

        public IReadOnlyList<MinigameMeterView> DescribeMeters() => _inner.DescribeMeters();

        public MinigameActionPreview Preview(string actionId) =>
            actionId == Dampen ? throw new ArgumentException("Dampen is not offered.", nameof(actionId)) : _inner.Preview(actionId);

        public IReadOnlyList<MinigameActionPreview> PreviewAll() => _actions.Select(action => _inner.Preview(action.Id)).ToList();

        public MinigameActionResult Perform(string actionId) =>
            actionId == Dampen
                ? new MinigameActionResult(actionId, false, "Dampen is not offered.", null, Meters, Meters, DescribeMeters(), Status)
                : _inner.Perform(actionId);

        public ICraftMinigameEngine Fork(IMinigameRandom random) => new DampenlessEngine(_inner.Fork(random));
    }
}
