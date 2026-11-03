using SWLOR.CraftSim.Play;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

namespace SWLOR.CraftSim.Harvesting
{
    /// <summary>How much of the node's Stability and charges a prospector keeps back to mine the pool before spending any on seams.</summary>
    public enum ProspectorReserve
    {
        /// <summary>None: seams first. The player can collapse the node and lose the units left in it.</summary>
        None = 0,

        /// <summary>
        /// Enough for the number of Strikes the pool takes on average (rounded to the nearest). A Strike that happens to take fewer units
        /// than average can leave the node one Strike short, and a collapse after the last unit costs nothing.
        /// </summary>
        Expected = 1,

        /// <summary>Enough for the most Strikes the pool is likely to take (rounded up).</summary>
        Cautious = 2
    }

    /// <summary>
    /// How a simulated prospector plays a node. Every choice reads only what the window shows (the yield, Stability and charges, the
    /// seams found, the costs on the buttons) plus what the player has learned: a Survey that finds fewer seams than it can reveal, or
    /// the third seam a node can hold, has found them all. The hidden seams are never read.
    /// </summary>
    /// <param name="Name">The plan's name in the reports.</param>
    /// <param name="MaxSurveys">The most Surveys the player takes: 0 never looks for seams.</param>
    /// <param name="ExtractSeams">Whether the player claims the seams they find.</param>
    /// <param name="HeavyStrikes">Whether the player takes the pool with Heavy Strikes once no gem pocket can be shattered.</param>
    /// <param name="Reserve">How much Stability and how many charges the player keeps back to mine the pool before they spend any on seams.</param>
    public sealed record ProspectorPlan(string Name, int MaxSurveys, bool ExtractSeams, bool HeavyStrikes, ProspectorReserve Reserve)
    {
        /// <summary>The casual player: strikes the vein until it is spent and never looks for seams.</summary>
        public static readonly ProspectorPlan Casual = new("Strike only", 0, false, false, ProspectorReserve.Expected);

        /// <summary>The player who spares no thought for Stability: surveys, extracts everything, then strikes.</summary>
        public static readonly ProspectorPlan Reckless = new("Seams first, no reserve", 3, true, false, ProspectorReserve.None);

        /// <summary>
        /// The plans a careful player chooses between: how many Surveys to risk, always extracting what is found and mining with Strikes.
        /// The skilled player of the length and XP-rate gates is the best of these.
        /// </summary>
        public static IReadOnlyList<ProspectorPlan> Skilled { get; } = new[]
        {
            new ProspectorPlan("Survey once, extract", 1, true, false, ProspectorReserve.Expected),
            new ProspectorPlan("Survey twice, extract", 2, true, false, ProspectorReserve.Expected),
            new ProspectorPlan("Survey until done, extract", 4, true, false, ProspectorReserve.Expected),
            Casual
        };

        /// <summary>
        /// The plans of a player in a hurry: Heavy Strikes take the pool in fewer actions, and gain no item over the careful plans (a Heavy
        /// Strike is a faster Strike, not a bigger one). They count toward the ceiling the +15% cap is checked against, but not toward the
        /// length of a careful session.
        /// </summary>
        public static IReadOnlyList<ProspectorPlan> Fast { get; } = new[]
        {
            new ProspectorPlan("Survey until done, extract, heavy strikes", 4, true, true, ProspectorReserve.Expected),
            new ProspectorPlan("Survey once, extract, heavy strikes", 1, true, true, ProspectorReserve.Expected),
            new ProspectorPlan("Heavy strikes only", 0, false, true, ProspectorReserve.Expected)
        };

        /// <summary>The careful plans with the larger reserve: they claim fewer seams on a large pool. They count toward the ceiling too.</summary>
        public static IReadOnlyList<ProspectorPlan> Cautious { get; } = new[]
        {
            new ProspectorPlan("Survey once, extract (cautious)", 1, true, false, ProspectorReserve.Cautious),
            new ProspectorPlan("Survey until done, extract (cautious)", 4, true, false, ProspectorReserve.Cautious)
        };

        /// <summary>Every plan the study plays besides the reckless one.</summary>
        public static IReadOnlyList<ProspectorPlan> All { get; } = Skilled.Concat(Fast).Concat(Cautious).ToList();
    }

    /// <summary>A prospector playing one node by a <see cref="ProspectorPlan"/>. One policy plays one node.</summary>
    public sealed class ProspectorPolicy : ICraftPolicy
    {
        private readonly ProspectorPlan _plan;
        private int _surveys;
        private int _seamsFound;
        private bool _foundAll;
        private int _revealedBefore;
        private bool _surveyed;

        public ProspectorPolicy(ProspectorPlan plan)
        {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        }

        public string Name => _plan.Name;

        public string Choose(ICraftMinigameEngine engine)
        {
            if (engine is not ProspectingEngine node)
                throw new ArgumentException("A prospector works a node with the Prospecting engine.", nameof(engine));

            LearnFromTheLastSurvey(node);

            var setup = node.Setup;

            var strikes = StrikesToFinish(node);
            var stabilityToFinish = strikes * setup.StrikeStability;
            var chargesToFinish = strikes * ProspectingRules.StrikeCharges;

            if (_plan.ExtractSeams && node.NextExtract != null)
            {
                var (extractStability, extractCharges) = ExtractCost(setup, node.NextExtract.Value);
                if (CanSpend(node, extractStability, extractCharges, stabilityToFinish, chargesToFinish))
                    return ProspectingActionIds.Extract;
            }

            if (_surveys < _plan.MaxSurveys && !LearnedAllSeams(node) &&
                CanSpend(
                    node,
                    ProspectingRules.SurveyStability + (_plan.ExtractSeams ? ProspectingRules.ExtractStability : 0),
                    ProspectingRules.SurveyCharges + (_plan.ExtractSeams ? ProspectingRules.ExtractCharges : 0),
                    stabilityToFinish,
                    chargesToFinish))
            {
                _surveys++;
                _surveyed = true;
                _revealedBefore = node.RevealedSeams.Count;
                return ProspectingActionIds.Survey;
            }

            if (_plan.HeavyStrikes && node.Pool >= 3 && node.Charges >= ProspectingRules.HeavyStrikeCharges && !MayShatterAGem(node))
                return ProspectingActionIds.HeavyStrike;

            return node.Charges >= ProspectingRules.StrikeCharges ? ProspectingActionIds.Strike : null;
        }

        /// <summary>What an Extract of the seam costs in Stability and charges, perks included.</summary>
        private static (int Stability, int Charges) ExtractCost(ProspectingSetup setup, ProspectingSeam seam) =>
            (ProspectingRules.ExtractStability, ProspectingRules.ExtractCharges);

        /// <summary>
        /// Whether the player knows there is nothing left to find: a Survey told them (it revealed fewer seams than it can, or the most a
        /// node holds), or the window shows no hidden seam (Core Sample).
        /// </summary>
        private bool LearnedAllSeams(ProspectingEngine node) =>
            _foundAll || node.Setup.Effects.SeesSeamTypes && node.SeenHiddenSeams.Count == 0;

        /// <summary>The Strikes it takes to mine what is left of the pool, never fewer than one, by the plan's reserve.</summary>
        private int StrikesToFinish(ProspectingEngine node)
        {
            var perStrike = ProspectingRules.ExpectedUnits(node.Setup.StrikeYield);
            var strikes = _plan.Reserve == ProspectorReserve.Cautious
                ? Math.Ceiling(node.Pool / perStrike)
                : Math.Round(node.Pool / perStrike, MidpointRounding.AwayFromZero);
            return Math.Max(1, (int)strikes);
        }

        private bool CanSpend(ProspectingEngine node, int stability, int charges, int stabilityToFinish, int chargesToFinish)
        {
            if (node.Charges < charges)
                return false;

            if (_plan.Reserve == ProspectorReserve.None)
                return true;

            // Spend on seams only what leaves enough to mine the rest of the pool, so the node does not collapse on the way.
            return node.Stability - stability > stabilityToFinish && node.Charges - charges >= chargesToFinish;
        }

        /// <summary>
        /// A Heavy Strike can shatter a gem pocket while any seam may still be hidden, because the player cannot tell a hidden pocket from
        /// a hidden rich seam. A player who claims seams Heavy Strikes only once every seam has been found; one who never extracts has
        /// nothing to lose.
        /// </summary>
        private bool MayShatterAGem(ProspectingEngine node) =>
            node.Setup.Effects.SeesSeamTypes
                ? node.SeenHiddenSeams.Contains(ProspectingSeamKind.GemPocket)
                : _plan.ExtractSeams && !_foundAll;

        private void LearnFromTheLastSurvey(ProspectingEngine node)
        {
            if (!_surveyed)
                return;

            _surveyed = false;
            var found = node.RevealedSeams.Count - _revealedBefore;
            _seamsFound += found;

            // A Survey that reveals fewer seams than it can has found them all, and so has one that brings the count to the most a node
            // holds (the help page says so).
            if (found < node.Setup.SurveyReveals || _seamsFound >= ProspectingRules.MaxSeams)
                _foundAll = true;
        }
    }
}
