using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>
/// The Kitchen: the rules engine for cooking. You steer a dish's flavor (Savory, Sweet, Spice) toward the recipe's target zone
/// while it cooks. Quality is read from where the flavor sits: 100% inside the inner radius, falling to 0% at the outer radius.
/// Doneness is the progress meter, Freshness is the integrity meter and falls every turn, and the dish is ruined at 0.
/// </summary>
public sealed class KitchenEngine : MinigameEngineBase<KitchenState>
{
    public const string SavoryMeterId = "savory";
    public const string SweetMeterId = "sweet";
    public const string SpiceMeterId = "spice";
    public const string DistanceMeterId = "distance";
    public const string HeatMeterId = "heat";
    public const string CoverMeterId = "cover";

    private const string StockMeterPrefix = "stock.";

    private readonly KitchenRecipe _recipe;
    private readonly KitchenStats _stats;
    private readonly IReadOnlyList<MinigameActionInfo> _actions;

    public KitchenEngine(IMinigameRandom random, KitchenRecipe recipe, KitchenStats stats)
        : base(random, CreateStart(recipe, stats))
    {
        _recipe = recipe;
        _stats = stats;
        _actions = BuildActions(recipe);
    }

    public override CraftMinigameType Type => CraftMinigameType.Kitchen;

    public override IReadOnlyList<MinigameActionInfo> Actions => _actions;

    public KitchenRecipe Recipe => _recipe;

    public KitchenStats Stats => _stats;

    /// <summary>The dish's flavor right now.</summary>
    public FlavorVector Profile => State.Profile;

    public KitchenHeat Heat => State.Heat;

    /// <summary>The turns left on the lid; Freshness loss is halved while it is above 0.</summary>
    public int CoverTurns => State.CoverTurns;

    /// <summary>The drift announced for the end of the next turn, before the heat scales it.</summary>
    public FlavorVector NextDrift => State.NextDrift;

    /// <summary>The drift that will move the flavor at the end of the next turn if the heat stays as it is.</summary>
    public FlavorVector EffectiveNextDrift => State.EffectiveNextDrift;

    public double DistanceFromCenter => _recipe.Target.DistanceTo(State.Profile);

    /// <summary>True when Peak Aroma is announced, so the next Add is twice as strong.</summary>
    public bool IsPeakAromaNext => State.NextOpportunity?.Id == KitchenTuning.PeakAromaId;

    public static string StockMeterId(int line) => StockMeterPrefix + line;

    /// <summary>The portions left of a committed ingredient line.</summary>
    public int PortionsLeft(int line) => State.Stock.Remaining(line);

    /// <summary>The three flavor axes with the dish's position and both target zones, for a window.</summary>
    public IReadOnlyList<KitchenAxisView> DescribeAxes() => DescribeAxes(State);

    private static KitchenState CreateStart(KitchenRecipe recipe, KitchenStats stats)
    {
        if (recipe == null)
            throw new ArgumentNullException(nameof(recipe));
        if (stats == null)
            throw new ArgumentNullException(nameof(stats));

        var meters = MinigameMeters.Start(
            KitchenTuning.DonenessMax,
            KitchenTuning.QualityMax,
            recipe.Freshness,
            stats.MaxCp);

        return new KitchenState(
            meters,
            null,
            recipe.Target,
            FlavorVector.Zero,
            KitchenHeat.Medium,
            0,
            FlavorVector.Zero,
            new KitchenStock(recipe.Lines.Select(line => line.Portions))).WithMeters(meters);
    }

    private static IReadOnlyList<MinigameActionInfo> BuildActions(KitchenRecipe recipe)
    {
        var actions = new List<MinigameActionInfo>
        {
            new(KitchenActionIds.HeatLow, "Adjust Heat: Low",
                "Takes a turn. Low heat cooks slowly and holds the flavor steady."),
            new(KitchenActionIds.HeatMedium, "Adjust Heat: Medium",
                "Takes a turn. Medium heat cooks at a steady pace and lets the flavor drift a little."),
            new(KitchenActionIds.HeatHigh, "Adjust Heat: High",
                "Takes a turn. High heat cooks fast and drifts the flavor every turn."),
            new(KitchenActionIds.Simmer, "Simmer", "Adds Doneness at Low or Medium heat."),
            new(KitchenActionIds.Sear, "Sear", "Adds a lot of Doneness at High heat and costs Freshness."),
            new(KitchenActionIds.Stir, "Stir", "Cancels the announced drift."),
            new(KitchenActionIds.Cover, "Cover", "Halves Freshness loss for 3 turns.")
        };

        for (var line = 0; line < recipe.Lines.Count; line++)
        {
            var ingredient = recipe.Lines[line];
            var description = ingredient.Kind == KitchenIngredientKind.Dilute
                ? $"Adds a portion of {ingredient.Name} and thins every flavor."
                : $"Adds a portion of {ingredient.Name} to the flavor.";
            actions.Add(new MinigameActionInfo(KitchenActionIds.AddIngredient(line), $"Add {ingredient.Name}", description));
        }

        actions.Add(new MinigameActionInfo(KitchenActionIds.AddSalt, "Add Salt", "A free pinch of salt. A small push toward Savory."));
        actions.Add(new MinigameActionInfo(KitchenActionIds.AddSugar, "Add Sugar", "A free pinch of sugar. A small push toward Sweet."));
        actions.Add(new MinigameActionInfo(KitchenActionIds.AddSpice, "Add Spice", "A free pinch of spice. A small push toward Spice."));

        return actions;
    }

    protected override MinigameActionRule<KitchenState> ResolveAction(KitchenState state, string actionId)
    {
        var info = _actions.FirstOrDefault(action => action.Id == actionId)
                   ?? throw new ArgumentException($"Unknown action '{actionId}'.", nameof(actionId));

        switch (actionId)
        {
            case KitchenActionIds.HeatLow:
                return AdjustHeat(state, info, KitchenHeat.Low);
            case KitchenActionIds.HeatMedium:
                return AdjustHeat(state, info, KitchenHeat.Medium);
            case KitchenActionIds.HeatHigh:
                return AdjustHeat(state, info, KitchenHeat.High);
            case KitchenActionIds.Simmer:
                return Simmer(state, info);
            case KitchenActionIds.Sear:
                return Sear(state, info);
            case KitchenActionIds.Stir:
                return Stir(state, info);
            case KitchenActionIds.Cover:
                return Cover(state, info);
        }

        if (KitchenActionIds.TryGetPantryAxis(actionId, out var axis))
        {
            var push = FlavorVector.Zero.With(axis, KitchenTuning.PantryPush);
            return AddFlavor(state, info, push, null);
        }

        // Every other id was found in the action list, so it is an ingredient line.
        KitchenActionIds.TryParseIngredient(actionId, out var line);
        return AddIngredient(state, info, line);
    }

    private static MinigameActionRule<KitchenState> AdjustHeat(KitchenState state, MinigameActionInfo info, KitchenHeat heat)
    {
        if (state.Heat == heat)
            return MinigameActionRule<KitchenState>.Disabled(info, 0, 0, $"The heat is already {heat}.");

        return Single(info, 0, 0, $"Heat {heat}", s => s with { Heat = heat });
    }

    private MinigameActionRule<KitchenState> Simmer(KitchenState state, MinigameActionInfo info)
    {
        if (state.Heat == KitchenHeat.High)
            return MinigameActionRule<KitchenState>.Disabled(info, 0, 0, "Turn the heat down to Medium or Low to simmer.");

        var amount = state.Heat == KitchenHeat.Low ? KitchenTuning.SimmerLowDoneness : KitchenTuning.SimmerMediumDoneness;
        var doneness = _stats.Doneness(amount, _recipe.Level);
        return Single(info, 0, 0, "Simmered", s => s.Adjust(new MinigameMeterDelta(Progress: doneness)));
    }

    private MinigameActionRule<KitchenState> Sear(KitchenState state, MinigameActionInfo info)
    {
        var cost = state.CoverTurns > 0 ? Halve(KitchenTuning.SearFreshnessCost) : KitchenTuning.SearFreshnessCost;
        if (state.Heat != KitchenHeat.High)
            return MinigameActionRule<KitchenState>.Disabled(info, 0, cost, "Searing needs High heat.");

        var doneness = _stats.Doneness(KitchenTuning.SearDoneness, _recipe.Level);
        return Single(info, 0, cost, "Seared", s => s.Adjust(new MinigameMeterDelta(Progress: doneness)));
    }

    private static MinigameActionRule<KitchenState> Stir(KitchenState state, MinigameActionInfo info)
    {
        if (state.EffectiveNextDrift.IsZero)
        {
            var reason = state.Heat == KitchenHeat.Low ? "Low heat holds the flavor steady." : "Nothing is drifting.";
            return MinigameActionRule<KitchenState>.Disabled(info, KitchenTuning.StirCp, 0, reason);
        }

        return Single(info, KitchenTuning.StirCp, 0, "Stirred", s => s with { NextDrift = FlavorVector.Zero });
    }

    private static MinigameActionRule<KitchenState> Cover(KitchenState state, MinigameActionInfo info)
    {
        if (state.CoverTurns > 0)
            return MinigameActionRule<KitchenState>.Disabled(info, KitchenTuning.CoverCp, 0, "The lid is already on.");

        return Single(info, KitchenTuning.CoverCp, 0, "Covered", s => s with { CoverTurns = KitchenTuning.CoverTurns });
    }

    private MinigameActionRule<KitchenState> AddIngredient(KitchenState state, MinigameActionInfo info, int line)
    {
        var ingredient = _recipe.Lines[line];
        if (state.Stock.Remaining(line) <= 0)
            return MinigameActionRule<KitchenState>.Disabled(info, 0, 0, $"No {ingredient.Name} left.");

        if (ingredient.Kind == KitchenIngredientKind.Dilute)
        {
            var percent = IsPeakAroma(state)
                ? Math.Min(100, ingredient.DilutePercent * KitchenTuning.PeakAromaMultiplier)
                : ingredient.DilutePercent;
            return Single(info, 0, 0, "Diluted", s => s.WithProfile(s.Profile.Scaled(100 - percent)).UsePortion(line));
        }

        return AddFlavor(state, info, ingredient.PortionVector, line);
    }

    /// <summary>
    /// A push of the given strength. The push lands light, steady or heavy, with a spread that Control narrows, and Peak Aroma
    /// doubles it. A <paramref name="line"/> of null is a pantry seasoning, which has no stock to spend.
    /// </summary>
    private MinigameActionRule<KitchenState> AddFlavor(KitchenState state, MinigameActionInfo info, FlavorVector push, int? line)
    {
        var aroma = IsPeakAroma(state) ? KitchenTuning.PeakAromaMultiplier : 1;
        var spread = _stats.PushSpreadPercent;

        KitchenState Apply(KitchenState s, int percent)
        {
            var next = s.WithProfile(s.Profile + push.Scaled(percent * aroma));
            return line.HasValue ? next.UsePortion(line.Value) : next;
        }

        if (spread <= 0)
        {
            return Single(info, 0, 0, "Steady hand", s => Apply(s, 100));
        }

        var steadyChance = 100 - 2 * KitchenTuning.ExtremePushChancePercent;
        return new MinigameActionRule<KitchenState>(info, 0, 0, new[]
        {
            new MinigameOutcomeRule<KitchenState>("Light hand", KitchenTuning.ExtremePushChancePercent,
                s => Apply(s, 100 - spread)),
            new MinigameOutcomeRule<KitchenState>("Steady hand", steadyChance, s => Apply(s, 100)),
            new MinigameOutcomeRule<KitchenState>("Heavy hand", KitchenTuning.ExtremePushChancePercent,
                s => Apply(s, 100 + spread))
        });
    }

    private static MinigameActionRule<KitchenState> Single(
        MinigameActionInfo info,
        int cpCost,
        int freshnessCost,
        string label,
        Func<KitchenState, KitchenState> transition) =>
        new(info, cpCost, freshnessCost, new[] { new MinigameOutcomeRule<KitchenState>(label, 100, transition) });

    private static bool IsPeakAroma(KitchenState state) => state.NextOpportunity?.Id == KitchenTuning.PeakAromaId;

    private static int Halve(int amount) => (amount + 1) / 2;

    protected override KitchenState EndTurn(KitchenState state)
    {
        var decay = state.CoverTurns > 0
            ? Halve(KitchenTuning.FreshnessDecayPerTurn)
            : KitchenTuning.FreshnessDecayPerTurn;

        var next = state.Adjust(new MinigameMeterDelta(Integrity: -decay));
        next = next.WithProfile(next.Profile + next.EffectiveNextDrift);

        // The lid ages, the announced drift has now happened (or was stirred away), and Peak Aroma has been used or has passed.
        return next with
        {
            CoverTurns = Math.Max(0, next.CoverTurns - 1),
            NextDrift = FlavorVector.Zero,
            NextOpportunity = null
        };
    }

    protected override KitchenState RollForecast(KitchenState state, IMinigameRandom random)
    {
        var axis = (KitchenAxis)random.Next(0, 3);
        var direction = random.Next(0, 2) == 0 ? -1 : 1;
        var drift = FlavorVector.Zero.With(axis, direction * KitchenTuning.DriftStrength);

        MinigameOpportunity opportunity = null;
        if (random.D100() <= KitchenTuning.PeakAromaChancePercent)
        {
            opportunity = new MinigameOpportunity(
                KitchenTuning.PeakAromaId,
                KitchenTuning.PeakAromaName,
                "The next Add is twice as strong.",
                1);
        }

        return state with { NextDrift = drift, NextOpportunity = opportunity };
    }

    protected override IReadOnlyList<MinigameMeterView> DescribeMeters(KitchenState state)
    {
        var meters = DescribeSharedMeters(
            state.Meters,
            progressLabel: "Doneness",
            qualityLabel: "Quality",
            integrityLabel: "Freshness").ToList();

        foreach (var axis in DescribeAxes(state))
        {
            meters.Add(new MinigameMeterView(
                AxisMeterId(axis.Axis),
                axis.Name,
                axis.Value,
                0,
                KitchenTuning.AxisMax,
                MinigameMeterMark.Range(axis.PerfectFrom, axis.PerfectTo, "Perfect")));
        }

        var target = state.Target;
        meters.Add(new MinigameMeterView(
            DistanceMeterId,
            "Off Target",
            (int)Math.Round(target.DistanceTo(state.Profile), MidpointRounding.AwayFromZero),
            0,
            KitchenTuning.MaxDistance,
            MinigameMeterMark.Range(target.InnerRadius, target.OuterRadius, "Quality falls")));
        meters.Add(new MinigameMeterView(HeatMeterId, "Heat", (int)state.Heat, 0, (int)KitchenHeat.High));
        meters.Add(new MinigameMeterView(CoverMeterId, "Cover", state.CoverTurns, 0, KitchenTuning.CoverTurns));

        for (var line = 0; line < _recipe.Lines.Count; line++)
        {
            var ingredient = _recipe.Lines[line];
            meters.Add(new MinigameMeterView(StockMeterId(line), ingredient.Name, state.Stock.Remaining(line), 0, ingredient.Portions));
        }

        return meters;
    }

    private static IReadOnlyList<KitchenAxisView> DescribeAxes(KitchenState state)
    {
        var views = new List<KitchenAxisView>();
        foreach (var axis in new[] { KitchenAxis.Savory, KitchenAxis.Sweet, KitchenAxis.Spice })
        {
            var perfect = state.Target.PerfectRange(axis);
            var tolerance = state.Target.ToleranceRange(axis);
            views.Add(new KitchenAxisView(
                axis,
                axis.ToString(),
                state.Profile.Get(axis),
                state.Target.Center.Get(axis),
                perfect.From,
                perfect.To,
                tolerance.From,
                tolerance.To));
        }

        return views;
    }

    private static string AxisMeterId(KitchenAxis axis) => axis switch
    {
        KitchenAxis.Savory => SavoryMeterId,
        KitchenAxis.Sweet => SweetMeterId,
        _ => SpiceMeterId
    };
}
