using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Tests.Support
{
    internal sealed record ToyState(MinigameMeters Meters, MinigameOpportunity NextOpportunity) : IMinigameState<ToyState>
    {
        public ToyState WithMeters(MinigameMeters meters) => this with { Meters = meters };
    }

    /// <summary>
    /// A deterministic engine whose best fixed rotation is known. "a" adds progress, "b" adds quality, "c" finishes the item at once with
    /// no quality (the greedy trap), and "d" adds a lot of quality but costs CP. Three of each of "b" and "a" make a full-quality item, so
    /// the best score is 1.0 and "c" alone scores 0.
    /// </summary>
    internal sealed class ToyEngine : MinigameEngineBase<ToyState>
    {
        public const string A = "a";
        public const string B = "b";
        public const string C = "c";
        public const string D = "d";

        private static readonly MinigameActionInfo[] ActionList =
        {
            new(A, "A", "Progress."), new(B, "B", "Quality."), new(C, "C", "Finishes with no quality."), new(D, "D", "Much quality for CP.")
        };

        public ToyEngine(IMinigameRandom random, int maxCp = 50)
            : base(random, new ToyState(MinigameMeters.Start(100, 100, 100, maxCp), null))
        {
        }

        public override CraftMinigameType Type => CraftMinigameType.Forge;

        public override IReadOnlyList<MinigameActionInfo> Actions => ActionList;

        protected override MinigameActionRule<ToyState> ResolveAction(ToyState state, string actionId)
        {
            var info = ActionList.FirstOrDefault(action => action.Id == actionId)
                       ?? throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));

            return actionId switch
            {
                A => Rule(info, 0, new MinigameMeterDelta(Progress: 34)),
                B => Rule(info, 0, new MinigameMeterDelta(Quality: 34)),
                C => Rule(info, 0, new MinigameMeterDelta(Progress: 100)),
                _ => Rule(info, 5, new MinigameMeterDelta(Quality: 60))
            };
        }

        private static MinigameActionRule<ToyState> Rule(MinigameActionInfo info, int cp, MinigameMeterDelta delta) =>
            new(info, cp, 0, new[] { new MinigameOutcomeRule<ToyState>("Done", 100, state => state.Adjust(delta)) });
    }
}
