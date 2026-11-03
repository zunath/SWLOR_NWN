using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Search;

/// <summary>
/// Gathering's scavenging minigame. A scavenge site opens a 3x3 board of spots, and the scavenger has a number of searches that
/// Gathering rank sets (they replaced Hard Look). Some spots are promising, a number that Gathering rank sets (it replaced
/// Perception); the scavenger does not see which. Every action costs one search and is aimed at one spot:
/// <list type="bullet">
/// <item><b>Search</b> a spot that has not been searched: it finds one roll of the site's loot table with the chance the spot's
/// ground gives. Promising ground finds several times as often.</item>
/// <item><b>Dig Deeper</b> at a spot that has been searched: it searches the same spot again with much better odds of a rare item, at a shown risk of
/// ruining the spot. The risk grows with each dig at the spot.</item>
/// <item><b>Sift</b> any spot: it shows whether the ground next to it is promising.</item>
/// </list>
/// The board ends when the searches are spent. Nothing in the engine rolls loot: it counts the finds, ordinary and deeper, and the
/// lifecycle that owns the site rolls the loot table for each. The engine reads no attribute.
/// <para>
/// The hidden layout is part of the state, so a preview shows the true chance of a Search. A window must therefore show a spot's
/// chance only when the scavenger knows its ground (<see cref="Knowledge"/>), and both chances otherwise (<see cref="SearchActionText"/>).
/// </para>
/// </summary>
public sealed class SearchEngine : MinigameEngineBase<SearchState>
{
    private static readonly IReadOnlyList<MinigameActionInfo> ActionList = BuildActions();

    private readonly SearchSetup _setup;

    /// <param name="random">The only source of chance; it lays out the promising spots, and picks each action's result.</param>
    /// <param name="setup">The searches, the promising spots, the chances and the scavenger's perks.</param>
    /// <param name="promisingMask">The promising spots, as a mask with bit n for spot n; null lays them out at random. A simulation or a test may fix it.</param>
    public SearchEngine(IMinigameRandom random, SearchSetup setup, int? promisingMask = null)
        : base(random, CreateInitialState(random, setup, promisingMask))
    {
        _setup = setup;
    }

    public override CraftMinigameType Type => CraftMinigameType.Search;

    public override IReadOnlyList<MinigameActionInfo> Actions => ActionList;

    public SearchSetup Setup => _setup;

    /// <summary>
    /// The board, hidden layout included. A window never shows it directly: it shows <see cref="Knowledge"/>. A simulation that plays
    /// a perfect-information scavenger reads <see cref="SearchBoard.IsPromising"/> from it.
    /// </summary>
    public SearchBoard Board => State.Board;

    /// <summary>What the scavenger knows about a spot's ground.</summary>
    public SearchSpotKnowledge Knowledge(int spot) => State.Board.KnowledgeOf(spot);

    public bool IsSearched(int spot) => State.Board.IsSearched(spot);

    public bool IsSpoiled(int spot) => State.Board.IsSpoiled(spot);

    public bool HasFound(int spot) => State.Board.HasFound(spot);

    /// <summary>The Dig Deeper attempts made at a spot.</summary>
    public int DigsAt(int spot) => State.Board.DigsAt(spot);

    /// <summary>The searches the scavenger has left.</summary>
    public int SearchesLeft => State.SearchesLeft;

    public int SearchesSpent => State.SearchesSpent;

    /// <summary>The Searches that found something: one roll of the site's loot table each.</summary>
    public int OrdinaryFinds => State.OrdinaryFinds;

    /// <summary>The Dig Deeper attempts that found something: one roll of the site's loot table each, with the rare entries favoured.</summary>
    public int DeeperFinds => State.DeeperFinds;

    public int Finds => State.Finds;

    /// <summary>The Searches and Dig Deeper attempts made, which pay Gathering XP.</summary>
    public int Attempts => State.Attempts;

    public int SuccessfulAttempts => State.SuccessfulAttempts;

    public int Sifts => State.Sifts;

    /// <summary>The last action resolved, or <see cref="SearchEvent.None"/> before the first.</summary>
    public SearchEvent Last => State.Last;

    /// <summary>The id of an action aimed at a spot.</summary>
    public static string ActionId(string action, int spot) => SearchActionIds.For(action, spot);

    private static IReadOnlyList<MinigameActionInfo> BuildActions()
    {
        var actions = new List<MinigameActionInfo>(3 * SearchBoard.Spots);
        foreach (var (action, name, description) in new[]
                 {
                     (SearchActionIds.Search, "Search",
                         "Searches the spot once. Promising ground finds much more often. It costs one search, and a spot can be searched only once."),
                     (SearchActionIds.DigDeeper, "Dig Deeper",
                         "Searches a spot you have already searched, again, with much better odds of a rare item. Each dig risks ruining the spot, and the risk grows with every dig there. It costs one search."),
                     (SearchActionIds.Sift, "Sift",
                         "Sifts the ground around the spot and shows whether the spots next to it are promising. It costs one search and finds nothing.")
                 })
        {
            for (var spot = 0; spot < SearchBoard.Spots; spot++)
                actions.Add(new MinigameActionInfo(SearchActionIds.For(action, spot), $"{name} ({SearchBoard.SpotName(spot)})", description));
        }

        return actions;
    }

    private static SearchState CreateInitialState(IMinigameRandom random, SearchSetup setup, int? promisingMask)
    {
        if (random == null)
            throw new ArgumentNullException(nameof(random));
        if (setup == null)
            throw new ArgumentNullException(nameof(setup));

        var mask = promisingMask ?? LayOut(random, setup.PromisingSpots);
        if (SearchBoard.PopCount(mask) != setup.PromisingSpots)
            throw new ArgumentException($"The layout has {SearchBoard.PopCount(mask)} promising spots, not {setup.PromisingSpots}.", nameof(promisingMask));
        if (mask < 0 || mask >= 1 << SearchBoard.Spots)
            throw new ArgumentOutOfRangeException(nameof(promisingMask), "The layout names spots outside the board.");

        return new SearchState(
            MinigameMeters.Start(setup.Searches, 0, 0, 0),
            null,
            SearchBoard.WithPromising(mask),
            0,
            0,
            0,
            0,
            0,
            SearchEvent.None);
    }

    /// <summary>Picks the promising spots: a shuffle of the nine, taking the first few, so every layout is equally likely.</summary>
    private static int LayOut(IMinigameRandom random, int promising)
    {
        var spots = Enumerable.Range(0, SearchBoard.Spots).ToArray();
        var mask = 0;
        for (var index = 0; index < promising; index++)
        {
            var pick = random.Next(index, SearchBoard.Spots);
            (spots[index], spots[pick]) = (spots[pick], spots[index]);
            mask |= SearchBoard.Bit(spots[index]);
        }

        return mask;
    }

    protected override MinigameActionRule<SearchState> ResolveAction(SearchState state, string actionId)
    {
        if (!SearchActionIds.TryParse(actionId, out var action, out var spot))
            throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));

        var info = ActionList.First(candidate => candidate.Id == actionId);
        return action switch
        {
            SearchActionIds.Search => ResolveSearch(state, info, spot),
            SearchActionIds.DigDeeper => ResolveDig(state, info, spot),
            _ => ResolveSift(state, info, spot)
        };
    }

    private MinigameActionRule<SearchState> ResolveSearch(SearchState state, MinigameActionInfo info, int spot)
    {
        var board = state.Board;
        if (board.IsSpoiled(spot))
            return MinigameActionRule<SearchState>.Disabled(info, 0, 0, "The ground here is spoiled.");
        if (board.IsSearched(spot))
            return MinigameActionRule<SearchState>.Disabled(info, 0, 0, "Already searched. Dig Deeper searches it again.");

        var chance = _setup.SearchChance(board.IsPromising(spot));
        var outcomes = new List<MinigameOutcomeRule<SearchState>>();
        if (chance > 0)
            outcomes.Add(new MinigameOutcomeRule<SearchState>("Find", chance, s => AfterSearch(s, spot, true)));
        if (chance < 100)
            outcomes.Add(new MinigameOutcomeRule<SearchState>("Nothing", 100 - chance, s => AfterSearch(s, spot, false)));

        return new MinigameActionRule<SearchState>(info, 0, 0, outcomes);
    }

    private MinigameActionRule<SearchState> ResolveDig(SearchState state, MinigameActionInfo info, int spot)
    {
        var board = state.Board;
        if (board.IsSpoiled(spot))
            return MinigameActionRule<SearchState>.Disabled(info, 0, 0, "The ground here is spoiled.");
        if (!board.IsSearched(spot))
            return MinigameActionRule<SearchState>.Disabled(info, 0, 0, "Search the spot first.");
        if (board.DigsAt(spot) >= SearchBoard.MaxDigsPerSpot)
            return MinigameActionRule<SearchState>.Disabled(info, 0, 0, "Dug as deep as this spot goes.");

        var risk = _setup.DigRisk(board.DigsAt(spot));
        var safe = 100 - risk;
        var find = (safe * _setup.DigFindChance + 50) / 100;
        var nothing = safe - find;

        var outcomes = new List<MinigameOutcomeRule<SearchState>>();
        if (find > 0)
            outcomes.Add(new MinigameOutcomeRule<SearchState>("Find", find, s => AfterDig(s, spot, SearchEventResult.Found)));
        if (nothing > 0)
            outcomes.Add(new MinigameOutcomeRule<SearchState>("Nothing", nothing, s => AfterDig(s, spot, SearchEventResult.Nothing)));
        if (risk > 0)
            outcomes.Add(new MinigameOutcomeRule<SearchState>("Spoils the spot", risk, s => AfterDig(s, spot, SearchEventResult.Spoiled)));

        return new MinigameActionRule<SearchState>(info, 0, 0, outcomes);
    }

    private MinigameActionRule<SearchState> ResolveSift(SearchState state, MinigameActionInfo info, int spot)
    {
        var unknown = UnknownAround(state.Board, spot);
        if (unknown == 0)
            return MinigameActionRule<SearchState>.Disabled(info, 0, 0, "You already know the ground around here.");

        var outcome = new MinigameOutcomeRule<SearchState>("Sifted", 100, s => AfterSift(s, spot));
        return new MinigameActionRule<SearchState>(info, 0, 0, new[] { outcome });
    }

    /// <summary>The spots a Sift at the spot would show: those next to it whose ground is not known yet.</summary>
    public int UnknownAround(int spot) => UnknownAround(State.Board, spot);

    private int UnknownAround(SearchBoard board, int spot)
    {
        var mask = 0;
        foreach (var neighbor in SearchBoard.Neighbors(spot))
        {
            if (!board.IsKnown(neighbor))
                mask |= SearchBoard.Bit(neighbor);
        }

        return mask;
    }

    private static SearchState AfterSearch(SearchState state, int spot, bool found)
    {
        var next = state.Adjust(new MinigameMeterDelta(Progress: 1));
        return next with
        {
            Board = state.Board.WithSearched(spot, found),
            OrdinaryFinds = state.OrdinaryFinds + (found ? 1 : 0),
            Attempts = state.Attempts + 1,
            SuccessfulAttempts = state.SuccessfulAttempts + (found ? 1 : 0),
            Last = new SearchEvent(SearchEventKind.Search, spot, found ? SearchEventResult.Found : SearchEventResult.Nothing)
        };
    }

    private static SearchState AfterDig(SearchState state, int spot, SearchEventResult result)
    {
        var found = result == SearchEventResult.Found;
        var next = state.Adjust(new MinigameMeterDelta(Progress: 1));
        return next with
        {
            Board = state.Board.WithDug(spot, found, result == SearchEventResult.Spoiled),
            DeeperFinds = state.DeeperFinds + (found ? 1 : 0),
            Attempts = state.Attempts + 1,
            SuccessfulAttempts = state.SuccessfulAttempts + (found ? 1 : 0),
            Last = new SearchEvent(SearchEventKind.DigDeeper, spot, result)
        };
    }

    private SearchState AfterSift(SearchState state, int spot)
    {
        var revealed = UnknownAround(state.Board, spot);
        var next = state.Adjust(new MinigameMeterDelta(Progress: 1));
        return next with
        {
            Board = state.Board.WithKnown(revealed),
            Sifts = state.Sifts + 1,
            Last = new SearchEvent(SearchEventKind.Sift, spot, SearchEventResult.Sifted, revealed)
        };
    }

    protected override MinigameStatus EvaluateStatus(SearchState state) =>
        state.Meters.Progress.IsFull ? MinigameStatus.Succeeded : MinigameStatus.InProgress;

    protected override IReadOnlyList<MinigameMeterView> DescribeMeters(SearchState state) => new[]
    {
        new MinigameMeterView(MinigameMeterIds.Progress, "Searches used", state.Meters.Progress.Value, 0, state.Meters.Progress.Max),
        new MinigameMeterView(FindsMeterId, "Finds", state.Finds, 0, state.Meters.Progress.Max)
    };

    /// <summary>The id of the meter that counts the finds.</summary>
    public const string FindsMeterId = "finds";
}
