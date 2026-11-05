using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

/// <summary>
/// The Plasma Forge: keep the piece inside its metal's thermal working range while shaping and finishing it. Shape is the
/// progress meter and Finish the quality meter. Thermal falls by the recipe's cooling rate after every action, and the only
/// chance in the minigame is the telegraphed Stable Containment opportunity. The Forgemaster and Artisan perks and the two
/// styles (<see cref="ForgePerkStats"/>) change the numbers and add five actions, each offered only to a crafter who owns its perk.
/// </summary>
public sealed class ForgeEngine : MinigameEngineBase<ForgeState>
{
    public const string ThermalMeterId = "thermal";

    private enum Boost
    {
        /// <summary>Stable Containment does not apply.</summary>
        None,

        /// <summary>Stable Containment gives its Hydro-Press bonus.</summary>
        Press,

        /// <summary>Stable Containment gives its Vibro-Hone bonus.</summary>
        Hone
    }

    private readonly ForgeSetup _setup;
    private readonly IReadOnlyList<MinigameActionInfo> _actions;

    public ForgeEngine(ForgeSetup setup, IMinigameRandom random)
        : base(random, CreateInitialState(setup))
    {
        _setup = setup;
        _actions = BuildActions(setup);
    }

    public override CraftMinigameType Type => CraftMinigameType.Forge;

    public override IReadOnlyList<MinigameActionInfo> Actions => _actions;

    public ForgeSetup Setup => _setup;

    public int Thermal => State.Thermal;

    public bool IsQuenched => State.Quenched;

    public int RangeLow => _setup.RangeLow;

    public int RangeHigh => _setup.RangeHigh;

    public ForgeThermalZone Zone => ForgeRules.ZoneOf(State.Thermal, RangeLow, RangeHigh);

    /// <summary>Hydro-Presses made in a row just now, which Hammer Rhythm counts. Always 0 for a crafter without it.</summary>
    public int PressStreak => State.PressStreak;

    /// <summary>The actions left during which Temper Hold keeps the piece from cooling, or 0.</summary>
    public int TemperHoldLeft => Math.Clamp(State.TemperHold, 0, _setup.Perks.TemperHoldActions);

    /// <summary>True while the crafter has a Red Line Press left this craft.</summary>
    public bool RedLineReady => _setup.Perks.RedLineCharges > State.RedLineUsed;

    private static ForgeState CreateInitialState(ForgeSetup setup)
    {
        if (setup == null)
            throw new ArgumentNullException(nameof(setup));

        var parameters = setup.Parameters ?? throw new ArgumentException("A craft needs the recipe's Plasma Forge parameters.", nameof(setup));
        var meters = MinigameMeters.Start(setup.ShapeMax, setup.FinishMax, ForgeRules.MaxIntegrity, setup.MaxCp);
        var thermal = Math.Clamp(parameters.StartingThermal, 0, ForgeRules.MaxThermal);
        return new ForgeState(meters, null, thermal, false);
    }

    /// <summary>The five usual actions with the ones the crafter's perks add, in the order the window lists them.</summary>
    private static IReadOnlyList<MinigameActionInfo> BuildActions(ForgeSetup setup)
    {
        var perks = setup.Perks;
        var actions = new List<MinigameActionInfo>
        {
            new(ForgeActionIds.RampPlasma, "Ramp Plasma", "Raises Thermal. Costs CP.")
        };

        if (perks.SurgePlasmaThermal > 0)
        {
            actions.Add(new(ForgeActionIds.SurgePlasma, "Surge Plasma",
                $"Raises Thermal by {setup.SurgeThermal} in one action. Costs {ForgePerkRules.SurgePlasmaCpMultiplier:0.#} times a Ramp Plasma's CP."));
        }

        actions.Add(new(ForgeActionIds.HydroPress, "Hydro-Press",
            "A large Shape gain that rises with Thermal across the working range. Below the range it does little and cracks Integrity; above it, it shapes more but cracks Integrity harder."));

        if (perks.OverdrivePressPercent > 0)
        {
            actions.Add(new(ForgeActionIds.OverdrivePress, "Overdrive Press",
                $"A Hydro-Press with {100 + perks.OverdrivePressPercent}% of the Shape. Costs {ForgePerkRules.OverdrivePressCp} CP and {ForgePerkRules.OverdrivePressIntegrityMultiplier} times a Hydro-Press's Integrity. Cannot be used twice in a row."));
        }

        if (perks.RedLineCharges > 0)
        {
            actions.Add(new(ForgeActionIds.RedLinePress, "Red Line Press",
                $"Once per craft, only while Thermal is above the working range: a Hydro-Press with {ForgePerkRules.RedLineShapeMultiplier} times the Shape that costs no Integrity."));
        }

        actions.Add(new(ForgeActionIds.VibroHone, "Vibro-Hone",
            "Raises Finish. Best in the cool half of the working range and weak outside it."));

        if (perks.MicroHonePercent > 0)
        {
            actions.Add(new(ForgeActionIds.MicroHone, "Micro-Hone",
                $"Raises Finish by {perks.MicroHonePercent}% of a Vibro-Hone's best result at any Thermal. Costs {ForgePerkRules.MicroHoneCp} CP."));
        }

        actions.Add(new(ForgeActionIds.Rebond, "Rebond", "A molecular rebond that restores Integrity. Costs Thermal and CP."));

        if (perks.TemperHoldActions > 0)
        {
            actions.Add(new(ForgeActionIds.TemperHold, "Temper Hold",
                $"For the next {perks.TemperHoldActions} actions the piece does not cool. Costs {ForgePerkRules.TemperHoldCp} CP."));
        }

        actions.Add(new(ForgeActionIds.CryoQuench, "Cryo-Quench",
            $"Once per craft, when the piece is {ForgeRules.QuenchMinimumShapePercent}% shaped. A large Finish bonus when Thermal is in the working range, more near its center. Afterward the piece is set: Ramp Plasma and Hydro-Press are disabled and Vibro-Hone works at a flat rate, trimming the set piece to size."));

        return actions;
    }

    protected override MinigameActionRule<ForgeState> ResolveAction(ForgeState state, string actionId)
    {
        var info = _actions.FirstOrDefault(action => action.Id == actionId)
                   ?? throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));

        return actionId switch
        {
            ForgeActionIds.RampPlasma => ResolveRamp(info, state),
            ForgeActionIds.SurgePlasma => ResolveSurge(info, state),
            ForgeActionIds.HydroPress => ResolvePress(info, state),
            ForgeActionIds.OverdrivePress => ResolveOverdrive(info, state),
            ForgeActionIds.RedLinePress => ResolveRedLine(info, state),
            ForgeActionIds.VibroHone => ResolveHone(info, state),
            ForgeActionIds.MicroHone => ResolveMicroHone(info, state),
            ForgeActionIds.Rebond => ResolveRebond(info, state),
            ForgeActionIds.TemperHold => ResolveTemperHold(info, state),
            _ => ResolveQuench(info, state)
        };
    }

    private MinigameActionRule<ForgeState> ResolveRamp(MinigameActionInfo info, ForgeState state)
    {
        var Cp = _setup.RampCp;
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

    private MinigameActionRule<ForgeState> ResolveSurge(MinigameActionInfo info, ForgeState state)
    {
        var Cp = _setup.SurgeCp;
        const int Integrity = ForgeRules.RampIntegrity;

        if (state.Quenched)
            return MinigameActionRule<ForgeState>.Disabled(info, Cp, Integrity, "The piece is set.");
        if (state.Thermal >= ForgeRules.MaxThermal)
            return MinigameActionRule<ForgeState>.Disabled(info, Cp, Integrity, "Thermal is already at its maximum.");

        return OneOutcome(info, Cp, Integrity, "Plasma surged",
            s => s with { Thermal = Math.Min(ForgeRules.MaxThermal, s.Thermal + _setup.SurgeThermal) });
    }

    private MinigameActionRule<ForgeState> ResolvePress(MinigameActionInfo info, ForgeState state)
    {
        var integrity = _setup.PressIntegrity(state.Thermal);

        var locked = LockedReason(info.Id);
        if (locked != null)
            return MinigameActionRule<ForgeState>.Disabled(info, 0, integrity, locked);
        if (state.Quenched)
            return MinigameActionRule<ForgeState>.Disabled(info, 0, integrity, "The piece is set.");

        var shape = PressShape(state, 1.0, state.PressStreak);
        var label = ZoneLabel("Press", state);

        return OneOutcome(info, 0, integrity, label, s => s.Adjust(new MinigameMeterDelta(Progress: shape)), press: true);
    }

    private MinigameActionRule<ForgeState> ResolveOverdrive(MinigameActionInfo info, ForgeState state)
    {
        var integrity = _setup.OverdriveIntegrity(state.Thermal);
        const int Cp = ForgePerkRules.OverdrivePressCp;

        if (state.Quenched)
            return MinigameActionRule<ForgeState>.Disabled(info, Cp, integrity, "The piece is set.");
        if (state.LastWasOverdrive)
            return MinigameActionRule<ForgeState>.Disabled(info, Cp, integrity, "Overdrive Press cannot be used twice in a row.");

        var shape = PressShape(state, 1.0 + _setup.Perks.OverdrivePressPercent / 100.0, 0);
        var label = ZoneLabel("Overdrive", state);

        return OneOutcome(info, Cp, integrity, label, s => s.Adjust(new MinigameMeterDelta(Progress: shape)), overdrive: true);
    }

    private MinigameActionRule<ForgeState> ResolveRedLine(MinigameActionInfo info, ForgeState state)
    {
        if (state.Quenched)
            return MinigameActionRule<ForgeState>.Disabled(info, 0, 0, "The piece is set.");
        if (!RedLineReady)
            return MinigameActionRule<ForgeState>.Disabled(info, 0, 0, "The Red Line Press is spent this craft.");
        if (ForgeRules.ZoneOf(state.Thermal, RangeLow, RangeHigh) != ForgeThermalZone.Above)
            return MinigameActionRule<ForgeState>.Disabled(info, 0, 0, "Needs Thermal above the working range.");

        var shape = PressShape(state, ForgePerkRules.RedLineShapeMultiplier, state.PressStreak);
        var label = Boosted("Red Line Press", state);

        return OneOutcome(info, 0, 0, label,
            s => (s.Adjust(new MinigameMeterDelta(Progress: shape))) with { RedLineUsed = s.RedLineUsed + 1 }, press: true);
    }

    private MinigameActionRule<ForgeState> ResolveHone(MinigameActionInfo info, ForgeState state)
    {
        var integrity = _setup.HoneIntegrity;

        var locked = LockedReason(info.Id);
        if (locked != null)
            return MinigameActionRule<ForgeState>.Disabled(info, 0, integrity, locked);

        if (state.Quenched)
        {
            var setFinish = Scaled(ForgeRules.PostQuenchHoneFinish * _setup.FinishScale * _setup.HoneFactor, state, Boost.Hone);
            var setShape = Scaled(ForgeRules.PostQuenchHoneShape * _setup.ShapeScale * _setup.PressFactor(0), state, Boost.Hone);
            return OneOutcome(info, 0, integrity, Boosted("Set piece honed", state),
                s => s.Adjust(new MinigameMeterDelta(Progress: setShape, Quality: setFinish)));
        }

        var multiplier = ForgeRules.HoneMultiplier(state.Thermal, RangeLow, RangeHigh, _setup.HoneHotEdgeMultiplier);
        var finish = Scaled(ForgeRules.HoneBaseFinish * multiplier * _setup.FinishScale * _setup.HoneFactor, state, Boost.Hone);

        return OneOutcome(info, 0, integrity, ZoneLabel("Hone", state),
            s => s.Adjust(new MinigameMeterDelta(Quality: finish)));
    }

    private MinigameActionRule<ForgeState> ResolveMicroHone(MinigameActionInfo info, ForgeState state)
    {
        const int Cp = ForgePerkRules.MicroHoneCp;
        const int Integrity = ForgePerkRules.MicroHoneIntegrity;

        // A Vibro-Hone's best result: its base Finish in the cool half of the range, or the flat rate of a set piece.
        var best = state.Quenched ? ForgeRules.PostQuenchHoneFinish : ForgeRules.HoneBaseFinish;
        var finish = Scaled(best * _setup.Perks.MicroHonePercent / 100.0 * _setup.FinishScale * _setup.HoneFactor, state, Boost.Hone);

        return OneOutcome(info, Cp, Integrity, Boosted("Micro-honed", state),
            s => s.Adjust(new MinigameMeterDelta(Quality: finish)));
    }

    private MinigameActionRule<ForgeState> ResolveRebond(MinigameActionInfo info, ForgeState state)
    {
        var Cp = _setup.RebondCp;

        var locked = LockedReason(info.Id);
        if (locked != null)
            return MinigameActionRule<ForgeState>.Disabled(info, Cp, 0, locked);
        if (state.Meters.Integrity.Value >= state.Meters.Integrity.Max)
            return MinigameActionRule<ForgeState>.Disabled(info, Cp, 0, "Integrity is already full.");

        return OneOutcome(info, Cp, 0, "Piece rebonded",
            s => (s with { Thermal = Math.Max(0, s.Thermal - _setup.RebondThermal) })
                .Adjust(new MinigameMeterDelta(Integrity: ForgeRules.RebondIntegrity)));
    }

    private MinigameActionRule<ForgeState> ResolveTemperHold(MinigameActionInfo info, ForgeState state)
    {
        const int Cp = ForgePerkRules.TemperHoldCp;

        if (state.Quenched)
            return MinigameActionRule<ForgeState>.Disabled(info, Cp, 0, "The piece is set.");
        if (state.TemperHold > 0)
            return MinigameActionRule<ForgeState>.Disabled(info, Cp, 0, "The piece is already holding its heat.");

        // The countdown starts one above the held actions: this action cools as usual, and the next ones do not.
        return OneOutcome(info, Cp, 0, "Heat held",
            s => s with { TemperHold = _setup.Perks.TemperHoldActions + 1 });
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

        var inRange = ForgeRules.ZoneOf(state.Thermal, RangeLow, RangeHigh) == ForgeThermalZone.InRange;
        var perfect = _setup.Perks.PerfectTemperPercent > 0 && inRange &&
                      ForgeRules.NearCenter(state.Thermal, RangeLow, RangeHigh, ForgePerkRules.PerfectTemperWindow);
        var multiplier = ForgeRules.QuenchMultiplier(state.Thermal, RangeLow, RangeHigh, _setup.QuenchEdgeMultiplier);
        var perfectFactor = perfect ? 1.0 + _setup.Perks.PerfectTemperPercent / 100.0 : 1.0;
        var finish = Scaled(ForgeRules.QuenchBaseFinish * multiplier * _setup.FinishScale * perfectFactor, state, Boost.None);
        var label = perfect
            ? "Perfect Temper"
            : inRange ? "Quenched in the working range" : "Quenched outside the working range";

        return OneOutcome(info, 0, Integrity, label,
            s => (s with { Thermal = 0, Quenched = true }).Adjust(new MinigameMeterDelta(Quality: finish)));
    }

    protected override ForgeState EndTurn(ForgeState state)
    {
        // Temper Hold counts down from one above its actions: the action itself cools, and each of the next ones does not.
        var holding = state.TemperHold >= 1 && state.TemperHold <= _setup.Perks.TemperHoldActions;
        return state with
        {
            Thermal = holding ? state.Thermal : Math.Max(0, state.Thermal - _setup.CoolingRate),
            TemperHold = Math.Max(0, state.TemperHold - 1),
            NextOpportunity = null
        };
    }

    protected override ForgeState RollForecast(ForgeState state, IMinigameRandom random) =>
        random.D100() <= _setup.ContainmentChancePercent
            ? state with
            {
                NextOpportunity = new MinigameOpportunity(
                    ForgeRules.StableContainmentId,
                    "Stable Containment",
                    $"Your next Hydro-Press or Vibro-Hone gets +{ForgeRules.StableContainmentBonusPercent}%{(_setup.ContainmentHonePercent != ForgeRules.StableContainmentBonusPercent ? $" (+{_setup.ContainmentHonePercent}% for Vibro-Hone)" : string.Empty)}.",
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

    /// <summary>One certain outcome. A Hydro-Press-like action counts toward Hammer Rhythm and an Overdrive Press is remembered; any other action resets both.</summary>
    private MinigameActionRule<ForgeState> OneOutcome(
        MinigameActionInfo info,
        int cpCost,
        int integrityCost,
        string label,
        Func<ForgeState, ForgeState> transition,
        bool press = false,
        bool overdrive = false) =>
        new(info, cpCost, integrityCost, new[]
        {
            new MinigameOutcomeRule<ForgeState>(label, 100, before =>
            {
                var after = transition(before);
                var streak = press && _setup.Perks.PressStreakPercent > 0
                    ? Math.Min(before.PressStreak + 1, ForgePerkRules.HammerRhythmMaxStacks)
                    : 0;
                return after with { PressStreak = streak, LastWasOverdrive = overdrive };
            })
        });

    /// <summary>
    /// The Shape of a Hydro-Press-like action at the current Thermal, after the style's output percent and, with
    /// <paramref name="streak"/> presses just before it, Hammer Rhythm's.
    /// </summary>
    private int PressShape(ForgeState state, double times, int streak)
    {
        var multiplier = ForgeRules.PressMultiplier(state.Thermal, RangeLow, RangeHigh);
        return Scaled(ForgeRules.PressBaseShape * multiplier * _setup.ShapeScale * _setup.PressFactor(streak) * times, state, Boost.Press);
    }

    /// <summary>A gain rounded to a whole number, with Stable Containment's bonus for the kind of action when it is announced.</summary>
    private int Scaled(double value, ForgeState state, Boost boost)
    {
        if (boost != Boost.None && state.HasStableContainment)
        {
            var percent = boost == Boost.Hone ? _setup.ContainmentHonePercent : _setup.ContainmentPressPercent;
            value *= 1.0 + percent / 100.0;
        }

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
