using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>
/// Gathering's harvesting minigame, the node session a harvester opens on a vein or a tree. The node holds a pool of units (today's d4
/// harvests), a Stability of 100 and one to three hidden seams, each sealing one unit of the pool. A seam is a rich seam (its unit and
/// sometimes an extra ore roll) or a gem pocket (a gem in place of its unit). The harvester's charges are the budget.
/// <list type="bullet">
/// <item><b>Survey</b> reveals hidden seams. It costs Stability and no charge. A Gathering rank milestone reveals more at once.</item>
/// <item><b>Strike</b> takes 1 or 2 units from the pool; <b>Heavy Strike</b> takes 2 to 4, costs more, and can shatter a gem pocket that has not been surveyed. Gathering rank milestones add yield to both. Both take the loose units first and break into a seam only when no loose unit is left.</item>
/// <item><b>Extract</b> claims a revealed seam for extra charges.</item>
/// <item><b>Leave</b> ends the session and leaves the rest of the node for others.</item>
/// </list>
/// The session ends when the last unit is taken (the node is spent), when Stability reaches 0 (the node collapses and the yield left is
/// lost), when the charges run out, or when the player leaves. Nothing is left to chance but the units a strike takes, whether a Heavy
/// Strike shatters a pocket and whether a rich seam gives its bonus roll, and the window shows all three before the click. The seams are
/// the one thing that starts hidden: finding them is the point.
/// </summary>
public sealed class ProspectingEngine : MinigameEngineBase<ProspectingState>
{
    public const string NotEnoughCharges = "Not enough harvester charges.";
    public const string NoRevealedSeam = "No seam has been found to extract.";
    public const string NodeSpent = "The node is spent.";

    /// <summary>The id of the meter that counts what the session has gathered so far.</summary>
    public const string GatheredMeterId = "gathered";

    private static readonly IReadOnlyList<MinigameActionInfo> ActionList = new[]
    {
        new MinigameActionInfo(
            ProspectingActionIds.Survey,
            "Survey",
            "Reveals hidden seams in the node for Stability only, no charge. Gathering rank milestones reveal more at once."),
        new MinigameActionInfo(
            ProspectingActionIds.Strike,
            "Strike",
            "Takes 1 or 2 units from the node and costs Stability."),
        new MinigameActionInfo(
            ProspectingActionIds.HeavyStrike,
            "Heavy Strike",
            "Takes 2 to 4 units and costs more Stability. It can shatter a gem pocket that has not been surveyed."),
        new MinigameActionInfo(
            ProspectingActionIds.Extract,
            "Extract",
            "Claims a revealed seam for extra charges: a rich seam gives its unit and sometimes an extra ore roll, and a gem pocket gives a gem."),
        new MinigameActionInfo(
            ProspectingActionIds.Leave,
            "Leave",
            "Ends the session. The rest of the node stays for others.")
    };

    private readonly ProspectingSetup _setup;

    /// <param name="random">The only source of chance; it rolls the units a strike takes, whether a Heavy Strike shatters a pocket and a rich seam's bonus.</param>
    /// <param name="setup">The node, the harvester's charges and what the Gathering rank earned.</param>
    public ProspectingEngine(IMinigameRandom random, ProspectingSetup setup)
        : base(random, CreateInitialState(setup))
    {
        _setup = setup;
    }

    public override CraftMinigameType Type => CraftMinigameType.Prospecting;

    public override IReadOnlyList<MinigameActionInfo> Actions => ActionList;

    public ProspectingSetup Setup => _setup;

    /// <summary>The units of yield left in the pool, the ones sealed in seams included.</summary>
    public int Pool => State.Pool;

    /// <summary>The pool the session began with.</summary>
    public int PoolAtStart => State.Meters.Progress.Max;

    /// <summary>The node's Stability: 0 collapses it.</summary>
    public int Stability => State.Stability;

    /// <summary>The harvester's charges left.</summary>
    public int Charges => State.Charges;

    /// <summary>The charges the session began with.</summary>
    public int ChargesAtStart => State.Meters.Cp.Max;

    /// <summary>Plain units taken from the pool: each is one roll of the node's loot table.</summary>
    public int Units => State.Units;

    /// <summary>Extra ore rolls from the rich seams claimed.</summary>
    public int RichRolls => State.RichRolls;

    /// <summary>Gems from the gem pockets claimed.</summary>
    public int Gems => State.Gems;

    /// <summary>Gem pockets a Heavy Strike shattered.</summary>
    public int Shattered => State.Shattered;

    /// <summary>Everything gathered so far, each item one unit of XP.</summary>
    public int Items => State.Items;

    /// <summary>How the session stands: going, or how it ended.</summary>
    public ProspectingEnding Ending => State.Ending;

    /// <summary>True once the first Heavy Strike of the node, the one Mother Lode doubles, has been made.</summary>
    public bool MotherLodeSpent => State.MotherLodeSpent;

    /// <summary>
    /// The kinds of the seams still hidden, in the order a Survey reveals them, for a harvester who can see them (Core Sample); empty for
    /// anyone else, so the window cannot give away how many seams are hidden.
    /// </summary>
    public IReadOnlyList<ProspectingSeamKind> SeenHiddenSeams => _setup.Effects.SeesSeamTypes
        ? SurveyOrder(State.Seams).Where(index => !State.Seams[index].Revealed).Select(index => State.Seams[index].Kind).ToList()
        : Array.Empty<ProspectingSeamKind>();

    /// <summary>The seconds the action takes to resolve now, perks and Mother Lode included. Leave is immediate.</summary>
    public double SecondsFor(string actionId) =>
        _setup.SecondsFor(
            actionId,
            actionId is ProspectingActionIds.Strike or ProspectingActionIds.HeavyStrike && MotherLodeReady(State),
            State.NextExtractIndex is { } next ? State.Seams[next].Kind : null);

    /// <summary>True while the harvester has Mother Lode and has not yet made the Strike or Heavy Strike it makes quick.</summary>
    private bool MotherLodeReady(ProspectingState state) => _setup.Effects.MotherLode && !state.MotherLodeSpent;

    /// <summary>The seams the player has found and not yet claimed, in the order Extract claims them.</summary>
    public IReadOnlyList<ProspectingSeam> RevealedSeams =>
        State.Seams.Where(seam => seam.Revealed).OrderBy(seam => seam.Kind == ProspectingSeamKind.GemPocket ? 0 : 1).ToList();

    /// <summary>The seam Extract would claim now, or null when none has been found.</summary>
    public ProspectingSeam? NextExtract => State.NextExtractIndex is { } index ? State.Seams[index] : null;

    /// <summary>
    /// The node as it stands, with its hidden seams, for the game to store: the pool left, the Stability, and every seam not yet claimed
    /// or shattered. It is not for the window, which must not show how many seams are hidden.
    /// </summary>
    public ProspectingNode ToNode() => new(State.Pool, Math.Max(0, State.Stability), State.Seams);

    private static ProspectingState CreateInitialState(ProspectingSetup setup)
    {
        if (setup == null)
            throw new ArgumentNullException(nameof(setup));

        var node = setup.Node;
        var meters = new MinigameMeters(
            new MinigameMeter(0, node.Pool),
            new MinigameMeter(0, 0),
            new MinigameMeter(node.Stability, ProspectingRules.MaxStability),
            new MinigameMeter(setup.Charges, setup.Charges));

        return new ProspectingState(meters, null, node.Seams, 0, 0, 0, 0, ProspectingEnding.InProgress);
    }

    protected override MinigameActionRule<ProspectingState> ResolveAction(ProspectingState state, string actionId)
    {
        var info = ActionList.FirstOrDefault(action => action.Id == actionId)
                   ?? throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));

        switch (actionId)
        {
            case ProspectingActionIds.Survey:
                return Costly(
                    state, info, ProspectingRules.SurveyCharges, ProspectingRules.SurveyStability,
                    new[] { new MinigameOutcomeRule<ProspectingState>("Surveys the node", 100, SurveyTransition) });

            case ProspectingActionIds.Strike:
                if (state.Pool <= 0)
                    return MinigameActionRule<ProspectingState>.Disabled(info, ProspectingRules.StrikeCharges, _setup.StrikeStability, NodeSpent);

                return Costly(
                    state, info, ProspectingRules.StrikeCharges, _setup.StrikeStability,
                    StrikeOutcomes(state, _setup.StrikeYield));

            case ProspectingActionIds.HeavyStrike:
                if (state.Pool <= 0)
                {
                    return MinigameActionRule<ProspectingState>.Disabled(
                        info, ProspectingRules.HeavyStrikeCharges, ProspectingRules.HeavyStrikeStability, NodeSpent);
                }

                return Costly(
                    state, info, ProspectingRules.HeavyStrikeCharges, ProspectingRules.HeavyStrikeStability,
                    HeavyStrikeOutcomes(state, _setup.HeavyStrikeYield));

            case ProspectingActionIds.Extract:
                if (state.NextExtractIndex == null)
                {
                    return MinigameActionRule<ProspectingState>.Disabled(
                        info, ProspectingRules.ExtractCharges, ProspectingRules.ExtractStability, NoRevealedSeam);
                }

                var seam = state.Seams[state.NextExtractIndex.Value];
                var rich = seam.Kind == ProspectingSeamKind.Rich;
                return Costly(
                    state, info,
                    ProspectingRules.ExtractCharges,
                    ProspectingRules.ExtractStability,
                    ExtractOutcomes(seam));

            default:
                return new MinigameActionRule<ProspectingState>(
                    info, 0, 0,
                    new[] { new MinigameOutcomeRule<ProspectingState>("Leaves the node", 100, s => s with { Ending = ProspectingEnding.Left }) });
        }
    }

    protected override MinigameStatus EvaluateStatus(ProspectingState state) => state.Ending switch
    {
        ProspectingEnding.InProgress => MinigameStatus.InProgress,
        ProspectingEnding.Collapsed => MinigameStatus.Failed,
        _ => MinigameStatus.Succeeded
    };

    protected override IReadOnlyList<MinigameMeterView> DescribeMeters(ProspectingState state) => new[]
    {
        // The pool is held as what has been taken, so it reads as the maximum less the stored value.
        new MinigameMeterView(MinigameMeterIds.Progress, "Yield", state.Pool, 0, state.Meters.Progress.Max),
        new MinigameMeterView(MinigameMeterIds.Integrity, "Stability", state.Meters.Integrity.Value, 0, state.Meters.Integrity.Max),
        new MinigameMeterView(MinigameMeterIds.Cp, "Charges", state.Meters.Cp.Value, 0, state.Meters.Cp.Max),

        // What the session has gathered. Its maximum is the most a node can give, so it does not show how many seams are hidden.
        new MinigameMeterView(
            GatheredMeterId, "Gathered", state.Items, 0, state.Meters.Progress.Max + ProspectingRules.MaxSeams * ProspectingRules.RichSeamBonusRolls)
    };

    private static MinigameActionRule<ProspectingState> Costly(
        ProspectingState state,
        MinigameActionInfo info,
        int charges,
        int stability,
        IReadOnlyList<MinigameOutcomeRule<ProspectingState>> outcomes)
    {
        if (state.Charges < charges)
            return MinigameActionRule<ProspectingState>.Disabled(info, charges, stability, NotEnoughCharges);

        return new MinigameActionRule<ProspectingState>(info, charges, stability, outcomes);
    }

    /// <summary>
    /// How the session stands after an action: the last unit taken spends the node, otherwise a Stability of 0 collapses it, otherwise a
    /// harvester with no charges is used up. The first of those that holds decides the ending.
    /// </summary>
    private static ProspectingState Settle(ProspectingState state)
    {
        if (state.Ending != ProspectingEnding.InProgress)
            return state;

        if (state.Pool <= 0)
            return state with { Ending = ProspectingEnding.MinedOut };

        if (state.Stability <= 0)
            return state with { Ending = ProspectingEnding.Collapsed };

        return state.Charges <= 0 ? state with { Ending = ProspectingEnding.OutOfCharges } : state;
    }

    private IReadOnlyList<MinigameOutcomeRule<ProspectingState>> StrikeOutcomes(
        ProspectingState state,
        IReadOnlyList<(int Units, int Chance)> yield)
    {
        // The pool caps what a strike takes, so two rolls that take the same units are one result.
        var chances = new SortedDictionary<int, int>();
        foreach (var (units, chance) in yield)
        {
            if (chance <= 0)
                continue;

            var taken = Math.Min(units, state.Pool);
            chances[taken] = chances.GetValueOrDefault(taken) + chance;
        }

        var lode = MotherLodeReady(state);
        return chances
            .Select(pair => new MinigameOutcomeRule<ProspectingState>(
                lode ? TakesLabel(pair.Key) + " in half the time (Mother Lode)" : TakesLabel(pair.Key),
                pair.Value,
                s =>
                {
                    var next = TakeUnits(s, pair.Key);
                    return lode ? next with { MotherLodeSpent = true } : next;
                }))
            .ToList();
    }

    private IReadOnlyList<MinigameOutcomeRule<ProspectingState>> HeavyStrikeOutcomes(
        ProspectingState state,
        IReadOnlyList<(int Units, int Chance)> yield)
    {
        // Whether a pocket shatters is always an outcome, so the preview does not show whether a hidden pocket exists (a harvester who
        // can see the seams knows). With none to shatter, the two results of one roll do the same thing.
        var lode = MotherLodeReady(state);
        var shatterPercent = _setup.Effects.SeesSeamTypes && !state.HasHiddenGemPocket ? 0 : _setup.ShatterPercent;
        var chances = new SortedDictionary<(int Units, bool Shatters), int>();
        foreach (var (units, chance) in yield)
        {
            if (chance <= 0)
                continue;

            var taken = Math.Min(units, state.Pool);
            AddChance(chances, (taken, false), chance * (100 - shatterPercent));
            AddChance(chances, (taken, true), chance * shatterPercent);
        }

        // The weights are out of 10000; whole percentages keep the preview readable whatever the shatter chance is.
        var keys = chances.Keys.ToList();
        var percentages = ProspectingRules.WholePercentages(keys.Select(key => (long)chances[key]).ToList());
        return keys
            .Select((key, index) =>
            {
                var taken = key.Units;
                var shatters = key.Shatters;
                var label = lode ? TakesLabel(taken) + " in half the time (Mother Lode)" : TakesLabel(taken);
                return new MinigameOutcomeRule<ProspectingState>(
                    shatters ? label + " and shatters a gem pocket if one is hidden" : label,
                    percentages[index],
                    s =>
                    {
                        var next = TakeUnits(s, taken);
                        next = shatters ? Shatter(next) : next;
                        return lode ? next with { MotherLodeSpent = true } : next;
                    });
            })
            .ToList();
    }

    private static IReadOnlyList<MinigameOutcomeRule<ProspectingState>> ExtractOutcomes(ProspectingSeam seam)
    {
        if (seam.Kind == ProspectingSeamKind.GemPocket)
        {
            return new[]
            {
                new MinigameOutcomeRule<ProspectingState>(
                    $"Claims a gem pocket for {GemsText(ProspectingRules.GemPocketGems)}", 100, s => Extract(s, false))
            };
        }

        return new[]
        {
            new MinigameOutcomeRule<ProspectingState>(
                "Claims a rich seam and finds an extra ore roll", ProspectingRules.RichSeamBonusPercent, s => Extract(s, true)),
            new MinigameOutcomeRule<ProspectingState>(
                "Claims a rich seam", 100 - ProspectingRules.RichSeamBonusPercent, s => Extract(s, false))
        };
    }

    private static void AddChance(SortedDictionary<(int Units, bool Shatters), int> chances, (int, bool) key, int weight)
    {
        if (weight > 0)
            chances[key] = chances.GetValueOrDefault(key) + weight;
    }

    private static string TakesLabel(int units) => units == 1 ? "Takes 1 unit" : $"Takes {units} units";

    private static string GemsText(int gems) => gems == 1 ? "1 gem" : $"{gems} gems";

    /// <summary>
    /// Takes units from the pool. The loose units go first; once none is left a strike breaks into a seam, hidden ones first, and the
    /// seam is lost with its unit, which comes out as plain ore.
    /// </summary>
    private static ProspectingState TakeUnits(ProspectingState state, int units)
    {
        var taken = Math.Min(units, state.Pool);
        var seams = state.Seams.ToBuilder();
        for (var broken = Math.Max(0, taken - state.LooseUnits); broken > 0; broken--)
        {
            var index = 0;
            while (index < seams.Count && seams[index].Revealed)
                index++;

            seams.RemoveAt(index < seams.Count ? index : 0);
        }

        var next = state with { Seams = seams.ToImmutable(), Units = state.Units + taken };
        return Settle(next.Adjust(new MinigameMeterDelta(Progress: taken)));
    }

    /// <summary>Shatters the first gem pocket that has not been surveyed, if the node has one. Its unit stays, as plain ore, but the gem is lost.</summary>
    private static ProspectingState Shatter(ProspectingState state)
    {
        for (var index = 0; index < state.Seams.Length; index++)
        {
            var seam = state.Seams[index];
            if (seam.Revealed || seam.Kind != ProspectingSeamKind.GemPocket)
                continue;

            return state with { Seams = state.Seams.RemoveAt(index), Shattered = state.Shattered + 1 };
        }

        return state;
    }

    private Func<ProspectingState, ProspectingState> SurveyTransition => state =>
    {
        var seams = state.Seams.ToBuilder();
        var left = _setup.SurveyReveals;
        foreach (var index in SurveyOrder(state.Seams))
        {
            if (left <= 0)
                break;

            if (seams[index].Revealed)
                continue;

            seams[index] = seams[index].Reveal();
            left--;
        }

        return Settle(state with { Seams = seams.ToImmutable() });
    };

    /// <summary>The order a Survey reveals the seams in: the order they are found in, gem pockets first for a harvester who finds them first.</summary>
    private IEnumerable<int> SurveyOrder(ImmutableArray<ProspectingSeam> seams)
    {
        var order = Enumerable.Range(0, seams.Length);
        return _setup.Effects.PocketsFirst
            ? order.OrderBy(index => seams[index].Kind == ProspectingSeamKind.GemPocket ? 0 : 1).ThenBy(index => index).ToList()
            : order.ToList();
    }

    /// <summary>Opens the next revealed seam. Its unit leaves the pool: a gem pocket gives a gem for it, a rich seam gives the ore and sometimes more.</summary>
    private static ProspectingState Extract(ProspectingState state, bool bonus)
    {
        var index = state.NextExtractIndex;
        if (index == null)
            return Settle(state);

        var seam = state.Seams[index.Value];
        var next = state with { Seams = state.Seams.RemoveAt(index.Value) };
        next = seam.Kind == ProspectingSeamKind.GemPocket
            ? next with { Gems = next.Gems + ProspectingRules.GemPocketGems }
            : next with
            {
                Units = next.Units + 1,
                RichRolls = next.RichRolls + (bonus ? ProspectingRules.RichSeamBonusRolls : 0)
            };

        return Settle(next.Adjust(new MinigameMeterDelta(Progress: 1)));
    }
}
