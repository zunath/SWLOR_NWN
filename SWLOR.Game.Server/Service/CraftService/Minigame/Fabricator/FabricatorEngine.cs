using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;

/// <summary>
/// The Fabrication minigame. An industrial fabricator builds a piece's three sub-assemblies with three tool heads: the Cutter works
/// the Frame, the Extruder the Shell and the Bonder the Fittings, and one head is loaded at a time. Run builds a sub-assembly (at
/// full effect with its own head, half effect with another), Precision Pass raises the quality of the loaded head's sub-assembly,
/// Retool changes heads and Service restores the Machine Condition. The quality meter shows the weighted average of the three
/// sub-assemblies' quality; Assemble finishes the item once all three are complete and caps that average at the weakest plus 20.
/// The only chance is the telegraphed Prime Feedstock opportunity.
/// </summary>
public sealed class FabricatorEngine : MinigameEngineBase<FabricatorState>
{
    public const string FrameProgressId = "frame_progress";
    public const string ShellProgressId = "shell_progress";
    public const string FittingsProgressId = "fittings_progress";
    public const string FrameQualityId = "frame_quality";
    public const string ShellQualityId = "shell_quality";
    public const string FittingsQualityId = "fittings_quality";

    private static readonly IReadOnlyList<MinigameActionInfo> ActionList = new[]
    {
        new MinigameActionInfo(FabricatorActionIds.RunFrame, "Run (Frame)",
            "Builds the Frame. Full effect with the Cutter loaded, half effect with another head and no retooling."),
        new MinigameActionInfo(FabricatorActionIds.RunShell, "Run (Shell)",
            "Builds the Shell. Full effect with the Extruder loaded, half effect with another head and no retooling."),
        new MinigameActionInfo(FabricatorActionIds.RunFittings, "Run (Fittings)",
            "Builds the Fittings. Full effect with the Bonder loaded, half effect with another head and no retooling."),
        new MinigameActionInfo(FabricatorActionIds.PrecisionPass, "Precision Pass",
            "Raises the quality of the sub-assembly the loaded head serves. It cannot be done off-head."),
        new MinigameActionInfo(FabricatorActionIds.RetoolCutter, "Retool (Cutter)",
            "Loads the Cutter, which serves the Frame. Costs CP."),
        new MinigameActionInfo(FabricatorActionIds.RetoolExtruder, "Retool (Extruder)",
            "Loads the Extruder, which serves the Shell. Costs CP."),
        new MinigameActionInfo(FabricatorActionIds.RetoolBonder, "Retool (Bonder)",
            "Loads the Bonder, which serves the Fittings. Costs CP."),
        new MinigameActionInfo(FabricatorActionIds.Service, "Service",
            $"Restores {FabricatorRules.ServiceRestore} Machine Condition. Costs CP."),
        new MinigameActionInfo(FabricatorActionIds.Assemble, "Assemble",
            $"Finishes the item once the Frame, Shell and Fittings are complete. The quality is the average of the three, no higher than the weakest plus {FabricatorRules.AssembleCapPoints}.")
    };

    private readonly FabricatorSetup _setup;

    /// <param name="random">The only source of chance.</param>
    /// <param name="setup">The recipe, the crafter and the enhancements.</param>
    public FabricatorEngine(IMinigameRandom random, FabricatorSetup setup)
        : base(random, CreateInitialState(setup))
    {
        _setup = setup;
    }

    public override CraftMinigameType Type => CraftMinigameType.Fabricator;

    public override IReadOnlyList<MinigameActionInfo> Actions => ActionList;

    public FabricatorSetup Setup => _setup;

    public FabricatorHead ActiveHead => State.ActiveHead;

    public SubAssemblyState Part(SubAssembly part) => State.Part(part);

    /// <summary>The head Prime Feedstock names, or null when none is announced or waiting.</summary>
    public FabricatorHead? PrimedHead => State.PrimedHead;

    /// <summary>How many more actions the named head can use its prime in, or 0 when none is in play.</summary>
    public int PrimeActionsLeft => State.PrimeActionsLeft;

    /// <summary>The highest the piece's quality can be with its sub-assemblies as they are: the weakest plus the cap.</summary>
    public int QualityCap => _setup.CapFor(State.Frame.Quality, State.Shell.Quality, State.Fittings.Quality);

    /// <summary>The quality the item would be made at if Assemble finished it now: the weighted average of the sub-assemblies, capped.</summary>
    public int AssembledQuality => _setup.PieceQuality(State.Frame.Quality, State.Shell.Quality, State.Fittings.Quality);

    /// <summary>True when all three sub-assemblies are complete, which is when Assemble becomes available.</summary>
    public bool IsComplete => AllComplete(State);

    private static FabricatorState CreateInitialState(FabricatorSetup setup)
    {
        if (setup == null)
            throw new ArgumentNullException(nameof(setup));

        return new FabricatorState(
            setup.StartingMeters(),
            null,
            setup.StartingHead,
            default,
            default,
            default,
            null,
            0,
            false);
    }

    protected override MinigameActionRule<FabricatorState> ResolveAction(FabricatorState state, string actionId)
    {
        var info = ActionList.FirstOrDefault(action => action.Id == actionId)
                   ?? throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));

        return actionId switch
        {
            FabricatorActionIds.RunFrame => ResolveRun(info, state, SubAssembly.Frame),
            FabricatorActionIds.RunShell => ResolveRun(info, state, SubAssembly.Shell),
            FabricatorActionIds.RunFittings => ResolveRun(info, state, SubAssembly.Fittings),
            FabricatorActionIds.PrecisionPass => ResolvePrecision(info, state),
            FabricatorActionIds.RetoolCutter => ResolveRetool(info, state, FabricatorHead.Cutter),
            FabricatorActionIds.RetoolExtruder => ResolveRetool(info, state, FabricatorHead.Extruder),
            FabricatorActionIds.RetoolBonder => ResolveRetool(info, state, FabricatorHead.Bonder),
            FabricatorActionIds.Service => ResolveService(info, state),
            _ => ResolveAssemble(info, state)
        };
    }

    private MinigameActionRule<FabricatorState> ResolveRun(MinigameActionInfo info, FabricatorState state, SubAssembly part)
    {
        var wear = _setup.WearRate;
        var size = _setup.SizeOf(part);
        if (state.Part(part).Progress >= size)
            return MinigameActionRule<FabricatorState>.Disabled(info, 0, wear, $"The {part} is already complete.");

        var head = state.ActiveHead;
        var onHead = head.Serves() == part;
        var primed = state.IsPrimed(head);
        var gain = _setup.RunGain(onHead, primed);
        var label = onHead ? $"Ran the {head} on the {part}" : $"Ran the {head} off-head on the {part}";
        if (primed)
            label += " (Prime Feedstock)";

        return One(info, 0, wear, label, s =>
        {
            var current = s.Part(part);
            var next = s.WithPart(part, current with { Progress = Math.Min(size, current.Progress + gain) });
            return Sync(primed ? next.WithoutPrime() : next);
        });
    }

    private MinigameActionRule<FabricatorState> ResolvePrecision(MinigameActionInfo info, FabricatorState state)
    {
        var wear = _setup.WearRate;
        var head = state.ActiveHead;
        var part = head.Serves();
        var current = state.Part(part);
        if (current.Progress <= 0)
            return MinigameActionRule<FabricatorState>.Disabled(info, 0, wear, $"Run the {part} first: there is nothing to refine yet.");
        if (current.Quality >= FabricatorRules.MaxQuality)
            return MinigameActionRule<FabricatorState>.Disabled(info, 0, wear, $"The {part} is already at full quality.");

        var primed = state.IsPrimed(head);
        var gain = _setup.PrecisionGain(primed);
        var label = primed ? $"Precision pass on the {part} (Prime Feedstock)" : $"Precision pass on the {part}";

        return One(info, 0, wear, label, s =>
        {
            var part2 = s.Part(part);
            var next = s.WithPart(part, part2 with { Quality = Math.Min(FabricatorRules.MaxQuality, part2.Quality + gain) });
            return Sync(primed ? next.WithoutPrime() : next);
        });
    }

    private MinigameActionRule<FabricatorState> ResolveRetool(MinigameActionInfo info, FabricatorState state, FabricatorHead head)
    {
        var cp = _setup.RetoolCp;
        if (state.ActiveHead == head)
            return MinigameActionRule<FabricatorState>.Disabled(info, cp, FabricatorRules.RetoolWear, $"The {head} is already loaded.");

        return One(info, cp, FabricatorRules.RetoolWear, $"Retooled to the {head}", s => s with { ActiveHead = head });
    }

    private MinigameActionRule<FabricatorState> ResolveService(MinigameActionInfo info, FabricatorState state)
    {
        var cp = _setup.ServiceCp;
        if (!_setup.IsUnlocked(info.Id))
        {
            return MinigameActionRule<FabricatorState>.Disabled(
                info, cp, 0, $"Unlocks at Fabrication rank {FabricatorRules.ServiceRank}.");
        }

        if (state.Meters.Integrity.Value >= state.Meters.Integrity.Max)
            return MinigameActionRule<FabricatorState>.Disabled(info, cp, 0, "Machine Condition is already full.");

        return One(info, cp, 0, "Serviced",
            s => s.Adjust(new MinigameMeterDelta(Integrity: FabricatorRules.ServiceRestore)));
    }

    private MinigameActionRule<FabricatorState> ResolveAssemble(MinigameActionInfo info, FabricatorState state)
    {
        if (!AllComplete(state))
        {
            return MinigameActionRule<FabricatorState>.Disabled(
                info, 0, 0, "Complete the Frame, the Shell and the Fittings first.");
        }

        return One(info, 0, 0, "Assembled", s =>
        {
            var quality = _setup.PieceQuality(s.Frame.Quality, s.Shell.Quality, s.Fittings.Quality);
            return (s with { Assembled = true })
                .WithMeters(s.Meters with { Quality = new MinigameMeter(quality, s.Meters.Quality.Max) });
        });
    }

    private static MinigameActionRule<FabricatorState> One(
        MinigameActionInfo info,
        int cpCost,
        int integrityCost,
        string label,
        Func<FabricatorState, FabricatorState> transition) =>
        new(info, cpCost, integrityCost, new[] { new MinigameOutcomeRule<FabricatorState>(label, 100, transition) });

    private bool AllComplete(FabricatorState state) =>
        FabricatorHeads.AllParts.All(part => state.Part(part).Progress >= _setup.SizeOf(part));

    /// <summary>
    /// Brings the shared meters in step with the sub-assemblies: the progress is their sum and the quality their weighted average.
    /// The cap applies when Assemble makes the item, so a gain to any sub-assembly shows in the quality meter while the piece is
    /// built, and the meter's mark shows where the cap would hold it.
    /// </summary>
    private FabricatorState Sync(FabricatorState state)
    {
        var progress = state.Frame.Progress + state.Shell.Progress + state.Fittings.Progress;
        var quality = _setup.AverageQuality(state.Frame.Quality, state.Shell.Quality, state.Fittings.Quality);
        return state.WithMeters(state.Meters with
        {
            Progress = new MinigameMeter(progress, state.Meters.Progress.Max),
            Quality = new MinigameMeter(quality, state.Meters.Quality.Max)
        });
    }

    /// <summary>The item is made by Assemble, not by a full progress meter; an empty Machine Condition breaks the piece.</summary>
    protected override MinigameStatus EvaluateStatus(FabricatorState state)
    {
        if (state.Assembled)
            return MinigameStatus.Succeeded;

        return state.Meters.Integrity.IsEmpty ? MinigameStatus.Failed : MinigameStatus.InProgress;
    }

    /// <summary>A prime that its head has not used passes after <see cref="FabricatorRules.PrimeWindowActions"/> actions.</summary>
    protected override FabricatorState EndTurn(FabricatorState state)
    {
        if (state.PrimedHead is not { } head)
            return state;

        var left = state.PrimeActionsLeft - 1;
        return left <= 0
            ? state.WithoutPrime()
            : state with { PrimeActionsLeft = left, NextOpportunity = PrimeOpportunity(head, left) };
    }

    /// <summary>
    /// Rolls whether the next turn announces Prime Feedstock. Each head that still has work has the same chance to be the one it
    /// names; nothing is announced while a prime is waiting.
    /// </summary>
    protected override FabricatorState RollForecast(FabricatorState state, IMinigameRandom random)
    {
        if (state.PrimedHead != null)
            return state;

        var candidates = FabricatorHeads.AllHeads.Where(head => HasWork(state, head)).ToList();
        if (candidates.Count == 0)
            return state;

        var index = (random.D100() - 1) / FabricatorRules.PrimeChancePerHeadPercent;
        if (index >= candidates.Count)
            return state;

        var named = candidates[index];
        return state with
        {
            PrimedHead = named,
            PrimeActionsLeft = FabricatorRules.PrimeWindowActions,
            NextOpportunity = PrimeOpportunity(named, FabricatorRules.PrimeWindowActions)
        };
    }

    /// <summary>True when the head's sub-assembly can still be built or refined.</summary>
    private bool HasWork(FabricatorState state, FabricatorHead head)
    {
        var part = head.Serves();
        var current = state.Part(part);
        return current.Progress < _setup.SizeOf(part) || current.Quality < FabricatorRules.MaxQuality;
    }

    private static MinigameOpportunity PrimeOpportunity(FabricatorHead head, int actionsLeft) =>
        new(
            FabricatorActionIds.PrimeFeedstock,
            $"Prime Feedstock: {head}",
            $"The {head}'s next action gets +{FabricatorRules.PrimeBonusPercent}%. It waits {ActionsText(actionsLeft)}.",
            1);

    private static string ActionsText(int actions) => actions == 1 ? "1 more action" : $"{actions} more actions";

    protected override IReadOnlyList<MinigameMeterView> DescribeMeters(FabricatorState state)
    {
        var shared = DescribeSharedMeters(
            state.Meters,
            progressLabel: "Build progress",
            qualityLabel: "Quality",
            integrityLabel: "Machine Condition");

        var quality = shared[1];
        var cap = _setup.CapFor(state.Frame.Quality, state.Shell.Quality, state.Fittings.Quality);
        var meters = new List<MinigameMeterView>
        {
            shared[0],
            quality with { Mark = MinigameMeterMark.Line(cap, $"Weakest + {FabricatorRules.AssembleCapPoints}") },
            shared[2],
            shared[3]
        };

        foreach (var part in FabricatorHeads.AllParts)
        {
            meters.Add(new MinigameMeterView(ProgressId(part), $"{part} progress", state.Part(part).Progress, 0, _setup.SizeOf(part)));
        }

        foreach (var part in FabricatorHeads.AllParts)
        {
            meters.Add(new MinigameMeterView(QualityId(part), $"{part} quality", state.Part(part).Quality, 0, FabricatorRules.MaxQuality));
        }

        return meters;
    }

    public static string ProgressId(SubAssembly part) => part switch
    {
        SubAssembly.Frame => FrameProgressId,
        SubAssembly.Shell => ShellProgressId,
        _ => FittingsProgressId
    };

    public static string QualityId(SubAssembly part) => part switch
    {
        SubAssembly.Frame => FrameQualityId,
        SubAssembly.Shell => ShellQualityId,
        _ => FittingsQualityId
    };
}
