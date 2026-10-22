using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Line;

/// <summary>
/// The words the Line window shows for the engine's previews and for the fish's announced move. Every number comes from the
/// preview or the setup, so what a button promises is what a click does.
/// </summary>
public static class LineActionText
{
    public const string LandsTheFish = "LANDS THE FISH";
    public const string SnapsTheLine = "SNAPS THE LINE";
    public const string FishEscapes = "THE FISH ESCAPES";

    /// <summary>The name of a move as the window announces it.</summary>
    public static string MoveName(FishMove move) => move switch
    {
        FishMove.Run => "RUN",
        FishMove.Dive => "DIVE",
        FishMove.Thrash => "THRASH",
        _ => "TIRE"
    };

    /// <summary>What a move does and how to meet it, with the numbers of this fish.</summary>
    public static string MoveHint(LineSetup setup, FishMove move) => move switch
    {
        FishMove.Run =>
            $"The fish bolts. It takes line unless you reel against it, and reeling now adds {setup.Pull + LineRules.RunReelStrain} extra Tension.",
        FishMove.Dive =>
            $"The fish plunges. The line takes {setup.Pull} Tension whatever you do, except that Give Line absorbs half.",
        FishMove.Thrash =>
            $"The fish thrashes. The line takes {setup.Pull + LineRules.ThrashSpike} Tension, and only Hold shrugs it off.",
        _ =>
            $"The fish rests and loses {LineRules.TireStaminaLoss} Stamina. Reeling now pulls it in {LineRules.TireReelDistance} further."
    };

    /// <summary>The fish's announced moves as one line, such as "RUN, then THRASH".</summary>
    public static string ForecastText(IReadOnlyList<FishMove> forecast) =>
        string.Join(", then ", forecast.Select(MoveName));

    /// <summary>
    /// How the fight moves if the action is taken, as "Distance 8 -> 5, Tension 3 -> 8, Stamina 9 -> 7", with the ending it causes
    /// last. A disabled action shows why instead.
    /// </summary>
    public static string ResultText(LineEngine engine, MinigameActionPreview preview)
    {
        return preview.Enabled ? OutcomeText(engine, preview.Outcomes[0]) : preview.DisabledReason;
    }

    /// <summary>The same text for a result, such as the one a performed action returned.</summary>
    public static string OutcomeText(LineEngine engine, MinigameOutcomePreview outcome)
    {
        var parts = new List<string>();
        AddChange(parts, "Distance", outcome.Change(MinigameMeterIds.Progress), engine.Setup.DistanceMax);
        AddChange(parts, "Tension", outcome.Change(MinigameMeterIds.Integrity), engine.TensionCap);
        AddChange(parts, "Stamina", outcome.Change(MinigameMeterIds.Quality), engine.StaminaMax);

        var text = parts.Count == 0 ? "No change" : string.Join(", ", parts);
        var ending = Ending(outcome);
        return ending == null ? text : $"{text}. {ending}";
    }

    /// <summary>The ending an outcome causes, or null when the fight goes on.</summary>
    public static string Ending(MinigameOutcomePreview outcome)
    {
        if (outcome.FinishesItem)
            return LandsTheFish;

        if (!outcome.BreaksItem)
            return null;

        return outcome.Change(MinigameMeterIds.Integrity).After <= 0 ? SnapsTheLine : FishEscapes;
    }

    /// <summary>The row for one action: its result, a tooltip and its colour.</summary>
    public static LineActionView Describe(LineEngine engine, MinigameActionPreview preview)
    {
        if (!preview.Enabled)
            return new LineActionView(preview.DisabledReason, preview.Action.Description, LineActionKind.Unavailable);

        var outcome = preview.Outcomes[0];
        var kind = outcome.FinishesItem ? LineActionKind.LandsFish
            : outcome.BreaksItem ? LineActionKind.LosesFish
            : LineActionKind.Normal;
        var tooltip = $"{preview.Action.Description} {MoveHint(engine.Setup, engine.Move)}";
        return new LineActionView(ResultText(engine, preview), tooltip, kind);
    }

    /// <summary>The text of a meter: its current value out of its maximum, with what the end of the scale means.</summary>
    public static string MeterText(string name, int value, int max, string note) => $"{name}: {value} of {max} ({note})";

    /// <summary>
    /// Distance, Tension and Stamina are held as what has been gained (Reeled in, Line strength, Worn down), so each reads as the
    /// maximum less the stored value.
    /// </summary>
    private static void AddChange(List<string> parts, string name, MinigameMeterChange change, int max)
    {
        if (change == null || change.Before == change.After)
            return;

        parts.Add($"{name} {max - change.Before} -> {max - change.After}");
    }
}
