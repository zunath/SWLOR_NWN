using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

/// <summary>
/// The Engineering minigame. Wire and Tune build Assembly and Calibration and add Load; every Load point amplifies the work, and
/// Discharge spends all of it as one burst. An action that leaves Load above the recipe's overload line can overload, which costs
/// a large share of Integrity and resets Load to 0. The action that overloads still resolves.
/// </summary>
public sealed class CircuitEngine : MinigameEngineBase<CircuitState>
{
    public const string LoadId = "load";
    public const string OverloadLabel = "Overload";

    private static readonly IReadOnlyList<MinigameActionInfo> ActionList = new[]
    {
        new MinigameActionInfo(CircuitActionIds.Wire, "Wire", $"Builds Assembly and adds {CircuitRules.WireLoad} Load."),
        new MinigameActionInfo(CircuitActionIds.Tune, "Tune",
            $"Builds Calibration, adds {CircuitRules.TuneLoad} Load and costs CP."),
        new MinigameActionInfo(CircuitActionIds.Boost, "Boost", $"Adds {CircuitRules.BoostLoad} Load and costs CP."),
        new MinigameActionInfo(CircuitActionIds.DischargeAssembly, "Discharge (Assembly)",
            "Spends all Load as one burst of Assembly. It never overloads."),
        new MinigameActionInfo(CircuitActionIds.DischargeCalibration, "Discharge (Calibration)",
            "Spends all Load as one burst of Calibration. It never overloads."),
        new MinigameActionInfo(CircuitActionIds.Ground, "Ground",
            $"Safely lowers Load by {CircuitRules.GroundLoad} and costs CP."),
        new MinigameActionInfo(CircuitActionIds.Patch, "Patch",
            $"Restores {CircuitRules.PatchRestore} Integrity and costs CP.")
    };

    private readonly CircuitSetup _setup;

    /// <param name="random">The only source of chance.</param>
    /// <param name="setup">The recipe, the crafter and the enhancements.</param>
    /// <param name="startingLoad">The Load the attempt starts with; a craft starts at 0, a simulation may start elsewhere.</param>
    public CircuitEngine(IMinigameRandom random, CircuitSetup setup, int startingLoad = 0)
        : base(random, CreateInitialState(setup, startingLoad))
    {
        _setup = setup;
    }

    public override CraftMinigameType Type => CraftMinigameType.Circuit;

    public override IReadOnlyList<MinigameActionInfo> Actions => ActionList;

    public CircuitSetup Setup => _setup;

    public int Load => State.Load;

    /// <summary>The chance, in percent, that an action leaving Load at the given level overloads.</summary>
    public int OverloadChancePercent(int loadAfterAction) => _setup.OverloadChancePercent(loadAfterAction);

    private static CircuitState CreateInitialState(CircuitSetup setup, int startingLoad)
    {
        if (setup == null)
            throw new ArgumentNullException(nameof(setup));

        return new CircuitState(setup.StartingMeters(), null, Math.Clamp(startingLoad, 0, CircuitRules.MaxLoad));
    }

    protected override MinigameActionRule<CircuitState> ResolveAction(CircuitState state, string actionId)
    {
        var info = ActionList.FirstOrDefault(action => action.Id == actionId)
                   ?? throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));

        switch (actionId)
        {
            case CircuitActionIds.Wire:
                return ResolveWire(info, state);
            case CircuitActionIds.Tune:
                return ResolveTune(info, state);
            case CircuitActionIds.Boost:
                return ResolveBoost(info, state);
            case CircuitActionIds.DischargeAssembly:
                return ResolveDischarge(info, state, toAssembly: true);
            case CircuitActionIds.DischargeCalibration:
                return ResolveDischarge(info, state, toAssembly: false);
            case CircuitActionIds.Ground:
                return ResolveGround(info, state);
            default:
                return ResolvePatch(info, state);
        }
    }

    private MinigameActionRule<CircuitState> ResolveWire(MinigameActionInfo info, CircuitState state)
    {
        if (Locked(info, CircuitRules.WireIntegrity, 0, out var locked))
            return locked;

        var gain = _setup.WireGain(state.Load);
        return RiskyAction(info, state, 0, CircuitRules.WireIntegrity, CircuitRules.WireLoad, "Wired",
            s => s.Adjust(new MinigameMeterDelta(Progress: gain)));
    }

    private MinigameActionRule<CircuitState> ResolveTune(MinigameActionInfo info, CircuitState state)
    {
        if (Locked(info, CircuitRules.TuneIntegrity, CircuitRules.TuneCp, out var locked))
            return locked;

        var gain = _setup.TuneGain(state.Load);
        var cleanSignal = state.HasCleanSignal;
        if (cleanSignal)
            gain *= CircuitRules.CleanSignalMultiplier;

        return RiskyAction(info, state, CircuitRules.TuneCp, CircuitRules.TuneIntegrity, CircuitRules.TuneLoad,
            cleanSignal ? "Tuned (Clean Signal)" : "Tuned",
            s => s.Adjust(new MinigameMeterDelta(Quality: gain)));
    }

    private MinigameActionRule<CircuitState> ResolveBoost(MinigameActionInfo info, CircuitState state)
    {
        if (Locked(info, CircuitRules.BoostIntegrity, CircuitRules.BoostCp, out var locked))
            return locked;

        if (state.Load >= CircuitRules.MaxLoad)
        {
            return MinigameActionRule<CircuitState>.Disabled(
                info, CircuitRules.BoostCp, CircuitRules.BoostIntegrity, "Load is already at its maximum.");
        }

        return RiskyAction(info, state, CircuitRules.BoostCp, CircuitRules.BoostIntegrity, CircuitRules.BoostLoad, "Boosted", s => s);
    }

    private MinigameActionRule<CircuitState> ResolveDischarge(MinigameActionInfo info, CircuitState state, bool toAssembly)
    {
        if (Locked(info, CircuitRules.DischargeIntegrity, 0, out var locked))
            return locked;

        if (state.Load <= 0)
        {
            return MinigameActionRule<CircuitState>.Disabled(
                info, 0, CircuitRules.DischargeIntegrity, "There is no Load to discharge.");
        }

        var load = state.Load;
        var delta = toAssembly
            ? new MinigameMeterDelta(Progress: _setup.DischargeAssemblyGain(load))
            : new MinigameMeterDelta(Quality: _setup.DischargeCalibrationGain(load));

        return Safe(info, 0, CircuitRules.DischargeIntegrity, "Discharged", s => (s with { Load = 0 }).Adjust(delta));
    }

    private MinigameActionRule<CircuitState> ResolveGround(MinigameActionInfo info, CircuitState state)
    {
        if (Locked(info, CircuitRules.GroundIntegrity, CircuitRules.GroundCp, out var locked))
            return locked;

        if (state.Load <= 0)
        {
            return MinigameActionRule<CircuitState>.Disabled(
                info, CircuitRules.GroundCp, CircuitRules.GroundIntegrity, "There is no Load to ground.");
        }

        return Safe(info, CircuitRules.GroundCp, CircuitRules.GroundIntegrity, "Grounded",
            s => s with { Load = Math.Max(0, s.Load - CircuitRules.GroundLoad) });
    }

    private MinigameActionRule<CircuitState> ResolvePatch(MinigameActionInfo info, CircuitState state)
    {
        if (Locked(info, 0, CircuitRules.PatchCp, out var locked))
            return locked;

        if (state.Meters.Integrity.Value >= state.Meters.Integrity.Max)
        {
            return MinigameActionRule<CircuitState>.Disabled(info, CircuitRules.PatchCp, 0, "Integrity is already full.");
        }

        return RiskyAction(info, state, CircuitRules.PatchCp, 0, loadGain: 0, "Patched",
            s => s.Adjust(new MinigameMeterDelta(Integrity: CircuitRules.PatchRestore)));
    }

    /// <summary>An action that cannot overload: it is a single sure result.</summary>
    private static MinigameActionRule<CircuitState> Safe(
        MinigameActionInfo info,
        int cpCost,
        int integrityCost,
        string label,
        Func<CircuitState, CircuitState> effect) =>
        new(info, cpCost, integrityCost, new[] { new MinigameOutcomeRule<CircuitState>(label, 100, effect) });

    /// <summary>
    /// An action that adds Load. When it leaves Load above the overload line it has two results: it holds, or it overloads after
    /// resolving in full. The chance rises with every point above the line.
    /// </summary>
    private MinigameActionRule<CircuitState> RiskyAction(
        MinigameActionInfo info,
        CircuitState state,
        int cpCost,
        int integrityCost,
        int loadGain,
        string label,
        Func<CircuitState, CircuitState> effect)
    {
        Func<CircuitState, CircuitState> holds = s =>
        {
            var resolved = effect(s);
            return resolved with { Load = Math.Min(CircuitRules.MaxLoad, resolved.Load + loadGain) };
        };

        var chance = _setup.OverloadChancePercent(Math.Min(CircuitRules.MaxLoad, state.Load + loadGain));
        if (chance <= 0)
            return Safe(info, cpCost, integrityCost, label, holds);

        Func<CircuitState, CircuitState> overloads = s =>
            (holds(s) with { Load = 0 }).Adjust(new MinigameMeterDelta(Integrity: -_setup.OverloadIntegrityCost));

        return new MinigameActionRule<CircuitState>(info, cpCost, integrityCost, new[]
        {
            new MinigameOutcomeRule<CircuitState>(label, 100 - chance, holds),
            new MinigameOutcomeRule<CircuitState>(OverloadLabel, chance, overloads)
        });
    }

    private bool Locked(MinigameActionInfo info, int integrityCost, int cpCost, out MinigameActionRule<CircuitState> rule)
    {
        var unlockRank = CircuitSetup.UnlockRank(info.Id);
        if (_setup.Rank >= unlockRank)
        {
            rule = null;
            return false;
        }

        rule = MinigameActionRule<CircuitState>.Disabled(info, cpCost, integrityCost, $"Unlocks at Engineering rank {unlockRank}.");
        return true;
    }

    /// <summary>An announced Clean Signal has been used or has passed once the action is done.</summary>
    protected override CircuitState EndTurn(CircuitState state) => state with { NextOpportunity = null };

    /// <summary>Rolls whether the next turn announces a Clean Signal. Nothing is announced before Tune unlocks.</summary>
    protected override CircuitState RollForecast(CircuitState state, IMinigameRandom random)
    {
        if (_setup.Rank < CircuitRules.TuneRank)
            return state;

        return random.D100() <= CircuitRules.CleanSignalChancePercent
            ? state with
            {
                NextOpportunity = new MinigameOpportunity(
                    CircuitActionIds.CleanSignal,
                    "Clean Signal",
                    "Your next Tune is doubled.",
                    1)
            }
            : state;
    }

    protected override IReadOnlyList<MinigameMeterView> DescribeMeters(CircuitState state)
    {
        var shared = DescribeSharedMeters(state.Meters, progressLabel: "Assembly", qualityLabel: "Calibration");
        return shared.Append(new MinigameMeterView(
            LoadId,
            "Load",
            state.Load,
            0,
            CircuitRules.MaxLoad,
            MinigameMeterMark.Line(_setup.OverloadLine, "Overload line"))).ToList();
    }
}
