using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

/// <summary>
/// The Plasma Forge: keep the piece inside its metal's thermal working range while shaping and finishing it. Shape is the
/// progress meter and Finish the quality meter. Thermal falls by the recipe's cooling rate after every action, and the only
/// chance in the minigame is the telegraphed Stable Containment opportunity.
/// </summary>
public sealed class ForgeEngine : MinigameEngineBase<ForgeState>
{
    public const string ThermalMeterId = "thermal";

    private static readonly IReadOnlyList<MinigameActionInfo> ActionList = new[]
    {
        new MinigameActionInfo(
            ForgeActionIds.RampPlasma,
            "Ramp Plasma",
            "Raises Thermal. Costs CP."),
        new MinigameActionInfo(
            ForgeActionIds.HydroPress,
            "Hydro-Press",
            "A large Shape gain that rises with Thermal across the working range. Below the range it does little and cracks Integrity; above it, it shapes more but cracks Integrity harder."),
        new MinigameActionInfo(
            ForgeActionIds.VibroHone,
            "Vibro-Hone",
            "Raises Finish. Best in the cool half of the working range and weak outside it."),
        new MinigameActionInfo(
            ForgeActionIds.Rebond,
            "Rebond",
            "A molecular rebond that restores Integrity. Costs Thermal and CP."),
        new MinigameActionInfo(
            ForgeActionIds.CryoQuench,
            "Cryo-Quench",
            $"Once per craft, when the piece is {ForgeRules.QuenchMinimumShapePercent}% shaped. A large Finish bonus when Thermal is in the working range, more near its center. Afterward the piece is set: Ramp Plasma and Hydro-Press are disabled and Vibro-Hone works at a flat rate, trimming the set piece to size.")
    };

    private readonly ForgeSetup _setup;

    public ForgeEngine(ForgeSetup setup, IMinigameRandom random)
        : base(random, CreateInitialState(setup))
    {
        _setup = setup;
    }

    public override CraftMinigameType Type => CraftMinigameType.Forge;

    public override IReadOnlyList<MinigameActionInfo> Actions => ActionList;

    public ForgeSetup Setup => _setup;

    public int Thermal => State.Thermal;

    public bool IsQuenched => State.Quenched;

    public int RangeLow => _setup.RangeLow;

    public int RangeHigh => _setup.RangeHigh;

    public ForgeThermalZone Zone => ForgeRules.ZoneOf(State.Thermal, RangeLow, RangeHigh);

    private static ForgeState CreateInitialState(ForgeSetup setup)
    {
        if (setup == null)
            throw new ArgumentNullException(nameof(setup));

        var parameters = setup.Parameters ?? throw new ArgumentException("A craft needs the recipe's Plasma Forge parameters.", nameof(setup));
        var meters = MinigameMeters.Start(setup.ShapeMax, setup.FinishMax, ForgeRules.MaxIntegrity, setup.MaxCp);
        var thermal = Math.Clamp(parameters.StartingThermal, 0, ForgeRules.MaxThermal);
        return new ForgeState(meters, null, thermal, false);
    }

    protected override MinigameActionRule<ForgeState> ResolveAction(ForgeState state, string actionId)
    {
        var info = ActionList.FirstOrDefault(action => action.Id == actionId)
                   ?? throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));

        return actionId switch
        {
            ForgeActionIds.RampPlasma => ResolveRamp(info, state),
            ForgeActionIds.HydroPress => ResolvePress(info, state),
            ForgeActionIds.VibroHone => ResolveHone(info, state),
            ForgeActionIds.Rebond => ResolveRebond(info, state),
            _ => ResolveQuench(info, state)
        };
    }

    private MinigameActionRule<ForgeState> ResolveRamp(MinigameActionInfo info, ForgeState state)
    {
        const int Cp = ForgeRules.RampCp;
        const int Integrity = ForgeRules.RampIntegrity;

        var locked = LockedReason(info.Id);
        if (locked != null)
            return MinigameActionRule<ForgeState>.Disabled(info, Cp, Integrity, locked);
        if (state.Quenched)
            return MinigameActionRule<ForgeState>.Disabled(info, Cp, Integrity, "The piece is set.");
        if (state.Thermal >= ForgeRules.MaxThermal)
            return MinigameActionRule<ForgeState>.Disabled(info, Cp, Integrity, "Thermal is already at its maximum.");

        return OneOutcome(info, Cp, Integrity, "Plasma ramped",
            s => s with { Thermal = Math.Min(ForgeRules.MaxThermal, s.Thermal + ForgeRules.RampThermal) });
    }

    private MinigameActionRule<ForgeState> ResolvePress(MinigameActionInfo info, ForgeState state)
    {
        var integrity = ForgeRules.PressIntegrityCost(state.Thermal, RangeLow, RangeHigh);

        var locked = LockedReason(info.Id);
        if (locked != null)
            return MinigameActionRule<ForgeState>.Disabled(info, 0, integrity, locked);
        if (state.Quenched)
            return MinigameActionRule<ForgeState>.Disabled(info, 0, integrity, "The piece is set.");

        var multiplier = ForgeRules.PressMultiplier(state.Thermal, RangeLow, RangeHigh);
        var shape = Scaled(ForgeRules.PressBaseShape * multiplier * _setup.ShapeScale, state);
        var label = ZoneLabel("Press", state);

        return OneOutcome(info, 0, integrity, label, s => s.Adjust(new MinigameMeterDelta(Progress: shape)));
    }

    private MinigameActionRule<ForgeState> ResolveHone(MinigameActionInfo info, ForgeState state)
    {
        const int Integrity = ForgeRules.HoneIntegrity;

        var locked = LockedReason(info.Id);
        if (locked != null)
            return MinigameActionRule<ForgeState>.Disabled(info, 0, Integrity, locked);

        if (state.Quenched)
        {
            var setFinish = Scaled(ForgeRules.PostQuenchHoneFinish * _setup.FinishScale, state);
            var setShape = Scaled(ForgeRules.PostQuenchHoneShape * _setup.ShapeScale, state);
            return OneOutcome(info, 0, Integrity, Boosted("Set piece honed", state),
                s => s.Adjust(new MinigameMeterDelta(Progress: setShape, Quality: setFinish)));
        }

        var multiplier = ForgeRules.HoneMultiplier(state.Thermal, RangeLow, RangeHigh);
        var finish = Scaled(ForgeRules.HoneBaseFinish * multiplier * _setup.FinishScale, state);

        return OneOutcome(info, 0, Integrity, ZoneLabel("Hone", state),
            s => s.Adjust(new MinigameMeterDelta(Quality: finish)));
    }

    private MinigameActionRule<ForgeState> ResolveRebond(MinigameActionInfo info, ForgeState state)
    {
        const int Cp = ForgeRules.RebondCp;

        var locked = LockedReason(info.Id);
        if (locked != null)
            return MinigameActionRule<ForgeState>.Disabled(info, Cp, 0, locked);
        if (state.Meters.Integrity.Value >= state.Meters.Integrity.Max)
            return MinigameActionRule<ForgeState>.Disabled(info, Cp, 0, "Integrity is already full.");

        return OneOutcome(info, Cp, 0, "Piece rebonded",
            s => (s with { Thermal = Math.Max(0, s.Thermal - ForgeRules.RebondThermal) })
                .Adjust(new MinigameMeterDelta(Integrity: ForgeRules.RebondIntegrity)));
    }

    private MinigameActionRule<ForgeState> ResolveQuench(MinigameActionInfo info, ForgeState state)
    {
        const int Integrity = ForgeRules.QuenchIntegrity;

        var locked = LockedReason(info.Id);
        if (locked != null)
            return MinigameActionRule<ForgeState>.Disabled(info, 0, Integrity, locked);
        if (state.Quenched)
            return MinigameActionRule<ForgeState>.Disabled(info, 0, Integrity, "Already quenched this craft.");
        if (state.Meters.Progress.Percent < ForgeRules.QuenchMinimumShapePercent)
        {
            return MinigameActionRule<ForgeState>.Disabled(
                info, 0, Integrity, $"The piece must be {ForgeRules.QuenchMinimumShapePercent}% shaped first.");
        }

        var multiplier = ForgeRules.QuenchMultiplier(state.Thermal, RangeLow, RangeHigh);
        var finish = Scaled(ForgeRules.QuenchBaseFinish * multiplier * _setup.FinishScale, state, boostable: false);
        var label = ForgeRules.ZoneOf(state.Thermal, RangeLow, RangeHigh) == ForgeThermalZone.InRange
            ? "Quenched in the working range"
            : "Quenched outside the working range";

        return OneOutcome(info, 0, Integrity, label,
            s => (s with { Thermal = 0, Quenched = true }).Adjust(new MinigameMeterDelta(Quality: finish)));
    }

    protected override ForgeState EndTurn(ForgeState state) =>
        state with
        {
            Thermal = Math.Max(0, state.Thermal - _setup.Parameters.CoolingRate),
            NextOpportunity = null
        };

    protected override ForgeState RollForecast(ForgeState state, IMinigameRandom random) =>
        random.D100() <= ForgeRules.StableContainmentChancePercent
            ? state with
            {
                NextOpportunity = new MinigameOpportunity(
                    ForgeRules.StableContainmentId,
                    "Stable Containment",
                    $"Your next Hydro-Press or Vibro-Hone gets +{ForgeRules.StableContainmentBonusPercent}%.",
                    1)
            }
            : state;

    protected override IReadOnlyList<MinigameMeterView> DescribeMeters(ForgeState state)
    {
        var shared = DescribeSharedMeters(state.Meters, progressLabel: "Shape", qualityLabel: "Finish");
        var thermal = new MinigameMeterView(
            ThermalMeterId,
            "Thermal",
            state.Thermal,
            0,
            ForgeRules.MaxThermal,
            MinigameMeterMark.Range(RangeLow, RangeHigh, "Working range"));

        return shared.Prepend(thermal).ToList();
    }

    /// <summary>The reason an action is locked at the crafter's rank, or null when it is unlocked.</summary>
    private string LockedReason(string actionId)
    {
        var required = ForgeRules.UnlockRank(actionId);
        return _setup.Rank >= required ? null : $"Unlocks at Smithery rank {required}.";
    }

    private static MinigameActionRule<ForgeState> OneOutcome(
        MinigameActionInfo info,
        int cpCost,
        int integrityCost,
        string label,
        Func<ForgeState, ForgeState> transition) =>
        new(info, cpCost, integrityCost, new[] { new MinigameOutcomeRule<ForgeState>(label, 100, transition) });

    /// <summary>A gain rounded to a whole number, with Stable Containment's bonus when it is announced.</summary>
    private static int Scaled(double value, ForgeState state, bool boostable = true)
    {
        if (boostable && state.HasStableContainment)
            value *= 1.0 + ForgeRules.StableContainmentBonusPercent / 100.0;

        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private string ZoneLabel(string verb, ForgeState state)
    {
        var zone = ForgeRules.ZoneOf(state.Thermal, RangeLow, RangeHigh);
        var where = zone switch
        {
            ForgeThermalZone.Below => "below the range",
            ForgeThermalZone.Above => "above the range",
            _ => "in range"
        };

        return Boosted($"{verb} {where}", state);
    }

    private static string Boosted(string label, ForgeState state) =>
        state.HasStableContainment ? $"{label} (Stable Containment)" : label;
}
