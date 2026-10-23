using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;

/// <summary>
/// The Engineering minigame. Wire and Tune build Assembly and Calibration and add Load; every Load point amplifies the work, and
/// Discharge spends all of it as one burst. An action that leaves Load above the recipe's overload line can overload, which costs
/// a large share of Integrity and resets Load to 0. The action that overloads still resolves. The Overclocker and Technician perks
/// (<see cref="CircuitPerkStats"/>) change the numbers, add two actions and add two once-per-craft saves.
/// </summary>
public sealed class CircuitEngine : MinigameEngineBase<CircuitState>
{
    public const string LoadId = "load";
    public const string OverloadLabel = "Overload";
    public const string SurgeLabel = "Surge";

    private readonly CircuitSetup _setup;
    private readonly IReadOnlyList<MinigameActionInfo> _actions;

    /// <param name="random">The only source of chance.</param>
    /// <param name="setup">The recipe, the crafter and the enhancements.</param>
    /// <param name="startingLoad">The Load the attempt starts with; a craft starts at 0, a simulation may start elsewhere.</param>
    public CircuitEngine(IMinigameRandom random, CircuitSetup setup, int startingLoad = 0)
        : base(random, CreateInitialState(setup, startingLoad, random))
    {
        _setup = setup;
        _actions = BuildActions(setup);
    }

    public override CraftMinigameType Type => CraftMinigameType.Circuit;

    public override IReadOnlyList<MinigameActionInfo> Actions => _actions;

    public CircuitSetup Setup => _setup;

    public int Load => State.Load;

    /// <summary>The Clean Signal announced for the action after the next, which only Diagnostics announces, or null.</summary>
    public MinigameOpportunity LaterOpportunity => State.LaterOpportunity;

    /// <summary>True while the crafter has Surge and this craft has not spent it.</summary>
    public bool SurgeReady => _setup.Perks.Surge && !State.SurgeUsed;

    /// <summary>True while the crafter has Failsafe and this craft has not spent it.</summary>
    public bool FailsafeReady => _setup.Perks.Failsafe && !State.FailsafeUsed;

    /// <summary>The chance, in percent, that an action leaving Load at the given level overloads.</summary>
    public int OverloadChancePercent(int loadAfterAction) => _setup.OverloadChancePercent(loadAfterAction);

    private static CircuitState CreateInitialState(CircuitSetup setup, int startingLoad, IMinigameRandom random)
    {
        if (setup == null)
            throw new ArgumentNullException(nameof(setup));

        if (random == null)
            throw new ArgumentNullException(nameof(random));

        var meters = setup.StartingMeters();
        var load = Math.Clamp(startingLoad, 0, CircuitRules.MaxLoad);

        // Diagnostics announces two actions ahead, so a craft that starts with it already knows about its first two actions.
        return setup.ForecastDepth > 1
            ? new CircuitState(meters, RollCleanSignal(setup, random, 1), load, RollCleanSignal(setup, random, 2))
            : new CircuitState(meters, null, load);
    }

    private static IReadOnlyList<MinigameActionInfo> BuildActions(CircuitSetup setup)
    {
        var actions = new List<MinigameActionInfo>
        {
            new(CircuitActionIds.Wire, "Wire", $"Builds Assembly and adds {setup.WireLoad} Load."),
            new(CircuitActionIds.Tune, "Tune", $"Builds Calibration, adds {setup.TuneLoad} Load and costs CP.")
        };

        if (setup.Perks.MicroTune)
        {
            actions.Add(new(CircuitActionIds.MicroTune, "Micro-Tune",
                $"Builds {CircuitRules.MicroTuneSharePercent}% of a Tune's Calibration, adds no Load and costs CP."));
        }

        actions.Add(new(CircuitActionIds.Boost, "Boost", $"Adds {setup.BoostLoad} Load and costs CP."));
        actions.Add(new(CircuitActionIds.DischargeAssembly, "Discharge (Assembly)",
            "Spends all Load as one burst of Assembly. It never overloads."));
        actions.Add(new(CircuitActionIds.DischargeCalibration, "Discharge (Calibration)",
            "Spends all Load as one burst of Calibration. It never overloads."));

        if (setup.Perks.CascadeDischarge)
        {
            actions.Add(new(CircuitActionIds.DischargeBoth, "Cascade Discharge",
                $"Spends all Load as one burst into both Assembly and Calibration at {CircuitRules.CascadeDischargeSharePercent}% each and costs CP. It never overloads."));
        }

        actions.Add(new(CircuitActionIds.Ground, "Ground", $"Safely lowers Load by {CircuitRules.GroundLoad} and costs CP."));
        actions.Add(new(CircuitActionIds.Patch, "Patch", $"Restores {setup.PatchRestore} Integrity and costs CP."));
        return actions;
    }

    protected override MinigameActionRule<CircuitState> ResolveAction(CircuitState state, string actionId)
    {
        var info = _actions.FirstOrDefault(action => action.Id == actionId)
                   ?? throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));

        switch (actionId)
        {
            case CircuitActionIds.Wire:
                return ResolveWire(info, state);
            case CircuitActionIds.Tune:
                return ResolveTune(info, state);
            case CircuitActionIds.MicroTune:
                return ResolveMicroTune(info, state);
            case CircuitActionIds.Boost:
                return ResolveBoost(info, state);
            case CircuitActionIds.DischargeAssembly:
                return ResolveDischarge(info, state, toAssembly: true);
            case CircuitActionIds.DischargeCalibration:
                return ResolveDischarge(info, state, toAssembly: false);
            case CircuitActionIds.DischargeBoth:
                return ResolveCascadeDischarge(info, state);
            case CircuitActionIds.Ground:
                return ResolveGround(info, state);
            default:
                return ResolvePatch(info, state);
        }
    }

    private MinigameActionRule<CircuitState> ResolveWire(MinigameActionInfo info, CircuitState state)
    {
        if (Locked(info, _setup.WireIntegrity, 0, out var locked))
            return locked;

        var gain = _setup.WireGain(state.Load);
        return RiskyAction(info, state, 0, _setup.WireIntegrity, _setup.WireLoad, "Wired",
            s => s.Adjust(new MinigameMeterDelta(Progress: gain)));
    }

    private MinigameActionRule<CircuitState> ResolveTune(MinigameActionInfo info, CircuitState state)
    {
        if (Locked(info, _setup.TuneIntegrity, CircuitRules.TuneCp, out var locked))
            return locked;

        var gain = _setup.TuneGain(state.Load);
        var cleanSignal = state.HasCleanSignal;
        if (cleanSignal)
            gain *= CircuitRules.CleanSignalMultiplier;

        return RiskyAction(info, state, CircuitRules.TuneCp, _setup.TuneIntegrity, _setup.TuneLoad,
            cleanSignal ? "Tuned (Clean Signal)" : "Tuned",
            s => s.Adjust(new MinigameMeterDelta(Quality: gain)));
    }

    private MinigameActionRule<CircuitState> ResolveMicroTune(MinigameActionInfo info, CircuitState state)
    {
        var gain = _setup.MicroTuneGain(state.Load);
        return RiskyAction(info, state, CircuitRules.MicroTuneCp, _setup.MicroTuneIntegrity, loadGain: 0, "Micro-tuned",
            s => s.Adjust(new MinigameMeterDelta(Quality: gain)));
    }

    private MinigameActionRule<CircuitState> ResolveBoost(MinigameActionInfo info, CircuitState state)
    {
        if (Locked(info, _setup.BoostIntegrity, _setup.BoostCp, out var locked))
            return locked;

        if (state.Load >= CircuitRules.MaxLoad)
        {
            return MinigameActionRule<CircuitState>.Disabled(
                info, _setup.BoostCp, _setup.BoostIntegrity, "Load is already at its maximum.");
        }

        return RiskyAction(info, state, _setup.BoostCp, _setup.BoostIntegrity, _setup.BoostLoad, "Boosted", s => s);
    }

    private MinigameActionRule<CircuitState> ResolveDischarge(MinigameActionInfo info, CircuitState state, bool toAssembly)
    {
        if (Locked(info, _setup.DischargeIntegrity, 0, out var locked))
            return locked;

        if (state.Load <= 0)
        {
            return MinigameActionRule<CircuitState>.Disabled(
                info, 0, _setup.DischargeIntegrity, "There is no Load to discharge.");
        }

        var load = state.Load;
        var delta = toAssembly
            ? new MinigameMeterDelta(Progress: _setup.DischargeAssemblyGain(load))
            : new MinigameMeterDelta(Quality: _setup.DischargeCalibrationGain(load));

        return Safe(info, 0, _setup.DischargeIntegrity, "Discharged", s => (s with { Load = 0 }).Adjust(delta));
    }

    private MinigameActionRule<CircuitState> ResolveCascadeDischarge(MinigameActionInfo info, CircuitState state)
    {
        if (state.Load <= 0)
        {
            return MinigameActionRule<CircuitState>.Disabled(
                info, CircuitRules.CascadeDischargeCp, _setup.DischargeIntegrity, "There is no Load to discharge.");
        }

        var load = state.Load;
        var delta = new MinigameMeterDelta(
            Progress: _setup.CascadeAssemblyGain(load),
            Quality: _setup.CascadeCalibrationGain(load));

        return Safe(info, CircuitRules.CascadeDischargeCp, _setup.DischargeIntegrity, "Cascade discharged",
            s => (s with { Load = 0 }).Adjust(delta));
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
        if (Locked(info, 0, _setup.PatchCp, out var locked))
            return locked;

        if (state.Meters.Integrity.Value >= state.Meters.Integrity.Max)
        {
            return MinigameActionRule<CircuitState>.Disabled(info, _setup.PatchCp, 0, "Integrity is already full.");
        }

        return RiskyAction(info, state, _setup.PatchCp, 0, loadGain: 0, "Patched",
            s => s.Adjust(new MinigameMeterDelta(Integrity: _setup.PatchRestore)));
    }

    /// <summary>An action that cannot overload: it is a single sure result.</summary>
    private MinigameActionRule<CircuitState> Safe(
        MinigameActionInfo info,
        int cpCost,
        int integrityCost,
        string label,
        Func<CircuitState, CircuitState> effect) =>
        new(info, cpCost, integrityCost, new[] { new MinigameOutcomeRule<CircuitState>(label, 100, s => Failsafe(effect(s))) });

    /// <summary>
    /// An action that adds Load. When it leaves Load above the overload line it has two results: it holds, or it overloads after
    /// resolving in full. The chance rises with every point above the line. While Surge is ready the overload becomes a Discharge
    /// of the whole Load instead of costing Integrity.
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

        var surges = SurgeReady;
        Func<CircuitState, CircuitState> overloads = s =>
        {
            var resolved = effect(s);
            resolved = resolved with { Load = Math.Min(CircuitRules.MaxLoad, resolved.Load + loadGain) };
            if (surges)
                return Failsafe(Surge(resolved));

            return Failsafe((resolved with { Load = 0 }).Adjust(new MinigameMeterDelta(Integrity: -_setup.OverloadIntegrityCost)));
        };

        return new MinigameActionRule<CircuitState>(info, cpCost, integrityCost, new[]
        {
            new MinigameOutcomeRule<CircuitState>(label, 100 - chance, s => Failsafe(holds(s))),
            new MinigameOutcomeRule<CircuitState>(surges ? SurgeLabel : OverloadLabel, chance, overloads)
        });
    }

    /// <summary>Spends all Load as a full Discharge into the meter that is further from full, Assembly on a tie, and uses up the craft's Surge.</summary>
    private CircuitState Surge(CircuitState state)
    {
        var toAssembly = state.Meters.Progress.Fraction <= state.Meters.Quality.Fraction;
        var delta = toAssembly
            ? new MinigameMeterDelta(Progress: _setup.DischargeAssemblyGain(state.Load))
            : new MinigameMeterDelta(Quality: _setup.DischargeCalibrationGain(state.Load));

        return (state with { Load = 0, SurgeUsed = true }).Adjust(delta);
    }

    /// <summary>The first time Integrity would reach 0 in a craft that is not finished, it stays at 1 instead.</summary>
    private CircuitState Failsafe(CircuitState state)
    {
        if (!_setup.Perks.Failsafe || state.FailsafeUsed || !state.Meters.Integrity.IsEmpty || state.Meters.Progress.IsFull)
            return state;

        return (state with { FailsafeUsed = true }).Adjust(new MinigameMeterDelta(Integrity: 1));
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

    /// <summary>An announced Clean Signal has been used or has passed once the action is done; one announced two actions ahead is now one ahead.</summary>
    protected override CircuitState EndTurn(CircuitState state) =>
        state with { NextOpportunity = Advance(state.LaterOpportunity), LaterOpportunity = null };

    /// <summary>
    /// Rolls whether a Clean Signal is announced after an action. Without Diagnostics it is announced for the next action; with it, for
    /// the action after the next (the next action's was announced by the forecast before).
    /// </summary>
    protected override CircuitState RollForecast(CircuitState state, IMinigameRandom random) =>
        _setup.ForecastDepth > 1
            ? state with { LaterOpportunity = RollCleanSignal(_setup, random, 2) }
            : state with { NextOpportunity = RollCleanSignal(_setup, random, 1) ?? state.NextOpportunity };

    private static MinigameOpportunity RollCleanSignal(CircuitSetup setup, IMinigameRandom random, int turnsAway) =>
        random.D100() <= setup.CleanSignalChancePercent
            ? new MinigameOpportunity(CircuitActionIds.CleanSignal, "Clean Signal", "Your next Tune is doubled.", turnsAway)
            : null;

    private static MinigameOpportunity Advance(MinigameOpportunity opportunity) =>
        opportunity == null
            ? null
            : new MinigameOpportunity(opportunity.Id, opportunity.Name, opportunity.Description, opportunity.TurnsAway - 1);

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
