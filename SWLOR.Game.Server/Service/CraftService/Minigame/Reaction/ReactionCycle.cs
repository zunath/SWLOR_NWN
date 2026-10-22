using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;

/// <summary>
/// The fixed sequence of phases one recipe's reaction repeats. Each phase lasts at least one turn; the turns beyond four go to the
/// phases the pattern names, in order, wrapping around when the pattern runs out of names.
/// </summary>
public sealed class ReactionCycle
{
    public const int MinimumLength = 4;
    public const int MaximumLength = 8;

    private static readonly IReadOnlyList<ReactionPhase> PhaseOrder = new[]
    {
        ReactionPhase.Calm, ReactionPhase.Rising, ReactionPhase.Peak, ReactionPhase.Settling
    };

    private readonly ReactionPhase[] _phases;

    public ReactionCycle(ReactionCyclePattern pattern, int length)
    {
        if (!Enum.IsDefined(pattern))
            throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown reaction cycle pattern.");
        if (length < MinimumLength || length > MaximumLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(length), length, $"A reaction cycle runs {MinimumLength} to {MaximumLength} turns.");
        }

        Pattern = pattern;
        _phases = BuildPhases(pattern, length);
    }

    public ReactionCyclePattern Pattern { get; }

    /// <summary>The number of turns in one full cycle.</summary>
    public int Length => _phases.Length;

    /// <summary>The phase of every turn in one cycle, in order.</summary>
    public IReadOnlyList<ReactionPhase> Phases => _phases;

    /// <summary>The phase at a position in the cycle. Positions past the end wrap around.</summary>
    public ReactionPhase PhaseAt(int index) => _phases[((index % Length) + Length) % Length];

    /// <summary>How many turns of every cycle fall in the phase.</summary>
    public int TurnsIn(ReactionPhase phase) => _phases.Count(candidate => candidate == phase);

    /// <summary>The first position of the phase in one cycle.</summary>
    public int FirstIndexOf(ReactionPhase phase) => Array.IndexOf(_phases, phase);

    /// <summary>The last position of the phase in one cycle.</summary>
    public int LastIndexOf(ReactionPhase phase) => Array.LastIndexOf(_phases, phase);

    /// <summary>
    /// The number of positions an attempt may start at: the Calm and Rising turns, so the first action never lands in a Peak or a
    /// Settling the player had no turn to prepare for.
    /// </summary>
    public int StartPositionCount => TurnsIn(ReactionPhase.Calm) + TurnsIn(ReactionPhase.Rising);

    /// <summary>The phases that receive the turns beyond four, in the order the pattern hands them out.</summary>
    public static IReadOnlyList<ReactionPhase> ExtraTurnOrder(ReactionCyclePattern pattern) => pattern switch
    {
        ReactionCyclePattern.Brew => new[]
        {
            ReactionPhase.Calm, ReactionPhase.Settling, ReactionPhase.Calm, ReactionPhase.Rising
        },
        ReactionCyclePattern.Surge => new[]
        {
            ReactionPhase.Peak, ReactionPhase.Rising, ReactionPhase.Peak, ReactionPhase.Calm
        },
        ReactionCyclePattern.Trigger => new[]
        {
            ReactionPhase.Rising, ReactionPhase.Calm, ReactionPhase.Rising, ReactionPhase.Settling
        },
        _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown reaction cycle pattern.")
    };

    private static ReactionPhase[] BuildPhases(ReactionCyclePattern pattern, int length)
    {
        var turns = PhaseOrder.ToDictionary(phase => phase, _ => 1);
        var order = ExtraTurnOrder(pattern);
        for (var extra = 0; extra < length - PhaseOrder.Count; extra++)
            turns[order[extra % order.Count]]++;

        return PhaseOrder.SelectMany(phase => Enumerable.Repeat(phase, turns[phase])).ToArray();
    }
}
