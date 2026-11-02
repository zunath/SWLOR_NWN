using SWLOR.CraftSim.Play;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Scenario.Fabricator
{
    /// <summary>
    /// The adaptive player's leaf heuristic for the Fabricator. The generic one values a position whose progress meter is full as
    /// if the item were made, because in the other minigames a full progress meter makes it. Here the item is made by Assemble, a
    /// separate action, so a full meter is still one action away and the generic value can rank "everything built, not yet
    /// assembled" above the finished item, which makes a depth-limited player dither (retool, retool, retool) instead of finishing.
    /// This one discounts an unfinished position by a little more than the time preference, so finishing always beats not finishing.
    /// It reads only the shared meters and previews, like the generic heuristic.
    /// </summary>
    public sealed class FabricatorLeafHeuristic : ILeafHeuristic
    {
        /// <summary>The share of value an unfinished position loses: the action that finishes it is still to be taken.</summary>
        public const double UnfinishedDiscount = 0.03;

        private readonly DefaultLeafHeuristic _generic;

        public FabricatorLeafHeuristic(DefaultLeafHeuristic generic = null)
        {
            _generic = generic ?? DefaultLeafHeuristic.Instance;
        }

        public double Evaluate(LeafPosition position)
        {
            var value = _generic.Evaluate(position);
            return position.Status == MinigameStatus.InProgress ? value * (1.0 - UnfinishedDiscount) : value;
        }
    }
}
