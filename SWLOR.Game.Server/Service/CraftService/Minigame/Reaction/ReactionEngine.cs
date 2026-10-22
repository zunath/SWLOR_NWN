using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;

/// <summary>
/// The Reaction: Espionage's poison and trap crafting. The player works a volatile mixture through a visible cycle of Calm,
/// Rising, Peak and Settling that advances every turn. Potency (progress) completes the item, Subtlety (quality) decides how well
/// it turned out, Stability (integrity) breaks the piece at 0, and CP pays for Dampen and Stabilize. The cycle is the only thing
/// the engine announces ahead of time, so nothing here is left to chance after the attempt starts; the one random choice is the
/// position the cycle starts at, which the window shows from the first turn.
/// </summary>
public sealed class ReactionEngine : MinigameEngineBase<ReactionState>
{
    public const string Measure = "measure";
    public const string Charge = "charge";
    public const string Refine = "refine";
    public const string Dampen = "dampen";
    public const string Stabilize = "stabilize";

    /// <summary>The id of the meter that tracks the cycle position. Its marked range is the Peak.</summary>
    public const string CycleMeterId = "cycle";

    private static readonly IReadOnlyList<MinigameActionInfo> ActionList = new[]
    {
        new MinigameActionInfo(Measure, "Measure", "A small, safe reading of the mixture: a little Potency in any phase."),
        new MinigameActionInfo(
            Charge,
            "Charge",
            "Drive the reaction: a lot of Potency, doubled at Peak. It costs heavy Stability unless the phase is Calm."),
        new MinigameActionInfo(Refine, "Refine", "Purify the mixture: Subtlety, doubled in Settling and halved at Peak."),
        new MinigameActionInfo(Dampen, "Dampen", "Hold the current phase for one more turn. Costs CP."),
        new MinigameActionInfo(Stabilize, "Stabilize", "Steady the mixture and restore Stability. Costs CP.")
    };

    private readonly ReactionSettings _settings;

    /// <param name="random">The source of chance; the only use is the starting position.</param>
    /// <param name="settings">The recipe's cycle and the amounts the crafter's rank gives each action.</param>
    /// <param name="startPosition">The position in the cycle to start at; null rolls one among the Calm and Rising turns.</param>
    public ReactionEngine(IMinigameRandom random, ReactionSettings settings, int? startPosition = null)
        : base(random, CreateInitialState(random, settings, startPosition))
    {
        _settings = settings;
    }

    public override CraftMinigameType Type => CraftMinigameType.Reaction;

    public override IReadOnlyList<MinigameActionInfo> Actions => ActionList;

    public ReactionSettings Settings => _settings;

    public ReactionCycle Cycle => _settings.Cycle;

    /// <summary>The position in the cycle.</summary>
    public int CycleIndex => State.CycleIndex;

    public ReactionPhase CurrentPhase => _settings.Cycle.PhaseAt(State.CycleIndex);

    /// <summary>The current phase and the next two, which the window shows.</summary>
    public IReadOnlyList<ReactionPhase> UpcomingPhases => Enumerable.Range(0, 3)
        .Select(offset => _settings.Cycle.PhaseAt(State.CycleIndex + offset))
        .ToList();

    /// <summary>The phase at a position in the cycle, such as the one a preview's cycle meter moves to.</summary>
    public ReactionPhase PhaseAt(int cycleIndex) => _settings.Cycle.PhaseAt(cycleIndex);

    protected override MinigameActionRule<ReactionState> ResolveAction(ReactionState state, string actionId)
    {
        var info = ActionList.FirstOrDefault(action => action.Id == actionId)
                   ?? throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));
        var phase = _settings.Cycle.PhaseAt(state.CycleIndex);

        switch (actionId)
        {
            case Measure:
                return OneOutcome(info, 0, ReactionRules.ActionStability, "Measured", s =>
                    s.Adjust(new MinigameMeterDelta(Progress: _settings.MeasurePotency)));

            case Charge:
            {
                var potency = phase == ReactionPhase.Peak
                    ? _settings.ChargePotency * ReactionRules.PeakChargeMultiplier
                    : _settings.ChargePotency;
                var strain = phase switch
                {
                    ReactionPhase.Calm => 0,
                    ReactionPhase.Peak => _settings.PeakChargeStability,
                    _ => ReactionRules.HeavyChargeStability
                };
                var label = phase switch
                {
                    ReactionPhase.Calm => "Safe charge",
                    ReactionPhase.Peak => "Doubled charge",
                    _ => "Strained charge"
                };

                return OneOutcome(info, 0, ReactionRules.ActionStability + strain, label, s =>
                    s.Adjust(new MinigameMeterDelta(Progress: potency)));
            }

            case Refine:
            {
                var subtlety = phase switch
                {
                    ReactionPhase.Settling => _settings.RefineSubtlety * ReactionRules.SettlingRefineMultiplier,
                    ReactionPhase.Peak => Math.Max(1, (int)Math.Floor(_settings.RefineSubtlety * ReactionRules.PeakRefineFactor)),
                    _ => _settings.RefineSubtlety
                };
                var label = phase switch
                {
                    ReactionPhase.Settling => "Doubled refine",
                    ReactionPhase.Peak => "Halved refine",
                    _ => "Refined"
                };

                return OneOutcome(info, 0, ReactionRules.ActionStability, label, s =>
                    s.Adjust(new MinigameMeterDelta(Quality: subtlety)));
            }

            case Dampen:
                return OneOutcome(info, ReactionRules.DampenCp, ReactionRules.ActionStability, "Phase held", s =>
                    s with { HoldCycle = true });

            case Stabilize:
                if (state.Meters.Integrity.Value >= state.Meters.Integrity.Max)
                    return MinigameActionRule<ReactionState>.Disabled(info, ReactionRules.StabilizeCp, 0, "Stability is already full.");

                return OneOutcome(info, ReactionRules.StabilizeCp, 0, "Stabilized", s =>
                    s.Adjust(new MinigameMeterDelta(Integrity: ReactionRules.StabilizeRestore)));

            default:
                throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));
        }
    }

    /// <summary>The cycle advances by one turn, unless Dampen held it this turn.</summary>
    protected override ReactionState EndTurn(ReactionState state) =>
        state.HoldCycle
            ? state with { HoldCycle = false }
            : state with { CycleIndex = (state.CycleIndex + 1) % _settings.Cycle.Length };

    protected override IReadOnlyList<MinigameMeterView> DescribeMeters(ReactionState state)
    {
        var cycle = _settings.Cycle;
        var peakFrom = cycle.FirstIndexOf(ReactionPhase.Peak);
        var peakTo = cycle.LastIndexOf(ReactionPhase.Peak);
        var shared = DescribeSharedMeters(
            state.Meters,
            progressLabel: "Potency",
            qualityLabel: "Subtlety",
            integrityLabel: "Stability");

        return shared
            .Append(new MinigameMeterView(
                CycleMeterId,
                "Reaction",
                state.CycleIndex,
                0,
                cycle.Length - 1,
                MinigameMeterMark.Range(peakFrom, peakTo, "Peak")))
            .ToList();
    }

    private static MinigameActionRule<ReactionState> OneOutcome(
        MinigameActionInfo info,
        int cpCost,
        int integrityCost,
        string label,
        Func<ReactionState, ReactionState> transition) =>
        new(info, cpCost, integrityCost, new[] { new MinigameOutcomeRule<ReactionState>(label, 100, transition) });

    private static ReactionState CreateInitialState(IMinigameRandom random, ReactionSettings settings, int? startPosition)
    {
        if (random == null)
            throw new ArgumentNullException(nameof(random));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        var position = startPosition ?? random.Next(0, settings.Cycle.StartPositionCount);
        if (position < 0 || position >= settings.Cycle.Length)
            throw new ArgumentOutOfRangeException(nameof(startPosition), position, "The start position is outside the cycle.");

        var meters = MinigameMeters.Start(
            ReactionRules.MeterMaximum,
            ReactionRules.MeterMaximum,
            ReactionRules.MeterMaximum,
            settings.MaxCp);
        return new ReactionState(meters, null, position, false);
    }
}
