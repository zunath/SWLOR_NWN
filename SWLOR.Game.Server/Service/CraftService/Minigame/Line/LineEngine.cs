using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Line;

/// <summary>
/// Agriculture's fishing minigame. A bite hooks a fish that starts part-way along the line. The angler reels it in against its
/// Stamina while the line holds: Distance 0 lands the fish, the maximum Distance lets it escape, and the cap on Tension snaps the
/// line. The fish announces its move before every turn (Run, Dive, Thrash or Tire), and Reel, Give Line and Hold answer each one
/// differently (<see cref="LineEffects"/>). Nothing here is left to chance except which move the fish picks next, and that is
/// announced before the angler acts. An exhausted fish only rests.
/// </summary>
public sealed class LineEngine : MinigameEngineBase<LineState>
{
    private static readonly IReadOnlyList<MinigameActionInfo> ActionList = new[]
    {
        new MinigameActionInfo(
            LineActionIds.SetHook,
            "Set Hook",
            "Only on the first turn: sets the hook firmly, draining the fish's Stamina. The fish's move is spent on the strike."),
        new MinigameActionInfo(
            LineActionIds.Reel,
            "Reel",
            "Closes Distance and adds Tension: a lot against a Run, and extra Distance against a Tire."),
        new MinigameActionInfo(
            LineActionIds.GiveLine,
            "Give Line",
            "Lowers Tension and lets out Distance."),
        new MinigameActionInfo(
            LineActionIds.Hold,
            "Hold",
            "Drains the fish's Stamina. Strong against a Thrash.")
    };

    private readonly LineSetup _setup;

    /// <param name="random">The only source of chance; it picks the fish's moves.</param>
    /// <param name="setup">The line, the fish and the angler's perks.</param>
    /// <param name="firstMove">The fish's first move; null rolls it. A simulation or a test may fix it.</param>
    /// <param name="thenMove">The move after it when the angler sees two moves ahead; null rolls it.</param>
    public LineEngine(IMinigameRandom random, LineSetup setup, FishMove? firstMove = null, FishMove? thenMove = null)
        : base(random, CreateInitialState(random, setup, firstMove, thenMove))
    {
        _setup = setup;
    }

    public override CraftMinigameType Type => CraftMinigameType.Line;

    public override IReadOnlyList<MinigameActionInfo> Actions => ActionList;

    public LineSetup Setup => _setup;

    /// <summary>How far the fish is: 0 is landed, the maximum is escaped.</summary>
    public int Distance => State.Distance;

    /// <summary>The strain on the line: at its cap the line snaps.</summary>
    public int Tension => State.Tension;

    public int TensionCap => State.TensionCap;

    /// <summary>The fish's remaining Stamina.</summary>
    public int Stamina => State.Stamina;

    public int StaminaMax => State.StaminaMax;

    /// <summary>The move the fish makes this turn, announced before the angler acts.</summary>
    public FishMove Move => State.Move;

    /// <summary>
    /// The fish's announced moves: the one it makes this turn, and the one after when the angler sees two ahead
    /// (<see cref="LineSetup.ForecastDepth"/>).
    /// </summary>
    public IReadOnlyList<FishMove> Forecast
    {
        get
        {
            var moves = new List<FishMove> { State.Move };
            if (_setup.ForecastDepth >= 2 && State.ThenMove.HasValue)
                moves.Add(State.ThenMove.Value);

            return moves;
        }
    }

    /// <summary>The number of turns taken.</summary>
    public int Round => State.Round;

    /// <summary>How the fight stands: going, landed, escaped or snapped.</summary>
    public LineEnding Ending => State.Ending;

    private static LineState CreateInitialState(IMinigameRandom random, LineSetup setup, FishMove? firstMove, FishMove? thenMove)
    {
        if (random == null)
            throw new ArgumentNullException(nameof(random));
        if (setup == null)
            throw new ArgumentNullException(nameof(setup));

        var meters = new MinigameMeters(
            new MinigameMeter(setup.DistanceMax - setup.StartDistance, setup.DistanceMax),
            new MinigameMeter(0, setup.Stamina),
            new MinigameMeter(setup.TensionCap - LineRules.StartTension, setup.TensionCap),
            new MinigameMeter(0, 0));

        var first = firstMove ?? setup.Pattern.Roll(random, null);
        FishMove? then = null;
        if (setup.SeesTwoMoves(0))
            then = thenMove ?? setup.Pattern.Roll(random, first);

        return new LineState(meters, null, first, then, 0);
    }

    protected override MinigameActionRule<LineState> ResolveAction(LineState state, string actionId)
    {
        var info = ActionList.FirstOrDefault(action => action.Id == actionId)
                   ?? throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));

        if (actionId == LineActionIds.SetHook && state.Round > 0)
            return MinigameActionRule<LineState>.Disabled(info, 0, 0, "Only on the first turn.");

        var effect = LineEffects.Of(_setup, actionId, state.Move);
        var label = actionId == LineActionIds.SetHook ? "Hook set" : $"{info.Name} against a {state.Move}";
        var outcome = new MinigameOutcomeRule<LineState>(label, 100, s => Apply(s, effect));
        return new MinigameActionRule<LineState>(info, 0, 0, new[] { outcome });
    }

    private static LineState Apply(LineState state, LineEffect effect) =>
        state.Adjust(new MinigameMeterDelta(
            Progress: -effect.Distance,
            Quality: -effect.Stamina,
            Integrity: -effect.Tension));

    protected override MinigameStatus EvaluateStatus(LineState state) => state.Ending switch
    {
        LineEnding.InProgress => MinigameStatus.InProgress,
        LineEnding.Landed => MinigameStatus.Succeeded,
        _ => MinigameStatus.Failed
    };

    protected override LineState EndTurn(LineState state) => state with { Round = state.Round + 1 };

    /// <summary>
    /// Announces the fish's next move. An exhausted fish only rests. Otherwise the move that was announced as "then" comes up now, and
    /// a new one is picked after it when the angler sees two moves ahead on this turn (Fish Sense, for its first turns only).
    /// </summary>
    protected override LineState RollForecast(LineState state, IMinigameRandom random)
    {
        var sees = _setup.SeesTwoMoves(state.Round);
        if (state.Exhausted)
            return state with { Move = FishMove.Tire, ThenMove = sees ? FishMove.Tire : null };

        var now = state.ThenMove ?? _setup.Pattern.Roll(random, state.Move);
        return state with { Move = now, ThenMove = sees ? _setup.Pattern.Roll(random, now) : null };
    }

    protected override IReadOnlyList<MinigameMeterView> DescribeMeters(LineState state) => new[]
    {
        new MinigameMeterView(MinigameMeterIds.Progress, "Reeled in", state.Meters.Progress.Value, 0, state.Meters.Progress.Max),
        new MinigameMeterView(MinigameMeterIds.Quality, "Worn down", state.Meters.Quality.Value, 0, state.Meters.Quality.Max),
        new MinigameMeterView(MinigameMeterIds.Integrity, "Line strength", state.Meters.Integrity.Value, 0, state.Meters.Integrity.Max)
    };
}
