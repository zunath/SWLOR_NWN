using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Search;

/// <summary>Which colour a row of the Search window takes.</summary>
public enum SearchActionKind
{
    Normal = 0,

    /// <summary>The action may spoil the spot.</summary>
    Risky = 1,

    /// <summary>The action cannot be used now.</summary>
    Unavailable = 2,

    /// <summary>The action is the last search of the site.</summary>
    LastSearch = 3
}

/// <summary>An action as the window shows it before the click.</summary>
/// <param name="Summary">The exact result: the chances, what it costs, and any ending it causes.</param>
/// <param name="Tooltip">What the action does.</param>
/// <param name="Kind">Which colour the row takes.</param>
public sealed record SearchActionView(string Summary, string Tooltip, SearchActionKind Kind);

/// <summary>
/// The words the Search window shows for the engine's previews and for the board. A spot's chance of a find is the one thing the
/// hidden layout decides, so the text for ground the scavenger has not learned about names both chances and never the true one.
/// </summary>
public static class SearchActionText
{
    public const string LastSearch = "This is your last search.";

    /// <summary>What the scavenger knows about a spot's ground, in words.</summary>
    public static string GroundText(SearchSpotKnowledge knowledge) => knowledge switch
    {
        SearchSpotKnowledge.Promising => "promising ground",
        SearchSpotKnowledge.Ordinary => "ordinary ground",
        _ => "unknown ground"
    };

    /// <summary>The spot's name with the first letter capitalised, such as "Top left".</summary>
    public static string SpotTitle(int spot)
    {
        var name = SearchBoard.SpotName(spot);
        return char.ToUpperInvariant(name[0]) + name[1..];
    }

    /// <summary>What has happened at a spot, or what it is waiting for.</summary>
    public static string SpotState(SearchEngine engine, int spot)
    {
        if (engine.IsSpoiled(spot))
            return "spoiled";

        if (!engine.IsSearched(spot))
            return "not searched";

        var digs = engine.DigsAt(spot);
        var dug = digs == 0 ? string.Empty : digs == 1 ? ", dug once" : $", dug {digs} times";
        return (engine.HasFound(spot) ? "searched, found something" : "searched, found nothing") + dug;
    }

    /// <summary>The two lines of a spot's button: its name, and what the scavenger knows about it.</summary>
    public static string SpotButtonText(SearchEngine engine, int spot)
    {
        string line;
        if (engine.IsSpoiled(spot))
            line = "Spoiled";
        else if (engine.IsSearched(spot))
            line = engine.HasFound(spot) ? "Found" : "Empty";
        else
            line = engine.Knowledge(spot) switch
            {
                SearchSpotKnowledge.Promising => "Promising",
                SearchSpotKnowledge.Ordinary => "Ordinary",
                _ => "Unknown"
            };

        return $"{SpotTitle(spot)}\n{line}";
    }

    /// <summary>The tooltip of a spot's button: its ground and what has been done there.</summary>
    public static string SpotTooltip(SearchEngine engine, int spot) =>
        $"{SpotTitle(spot)}: {GroundText(engine.Knowledge(spot))}, {SpotState(engine, spot)}.";

    /// <summary>
    /// The row for one action at one spot: its exact chances, its cost and any ending, a tooltip and its colour. A Search of ground the
    /// scavenger does not know names both chances.
    /// </summary>
    public static SearchActionView Describe(SearchEngine engine, int spot, string action)
    {
        var id = SearchActionIds.For(action, spot);
        var preview = engine.Preview(id);
        var tooltip = preview.Action.Description;
        if (!preview.Enabled)
            return new SearchActionView(preview.DisabledReason, tooltip, SearchActionKind.Unavailable);

        string summary;
        var kind = SearchActionKind.Normal;
        switch (action)
        {
            case SearchActionIds.Search:
                summary = engine.Knowledge(spot) == SearchSpotKnowledge.Unknown
                    ? $"Finds {engine.Setup.UnknownChanceText}."
                    : $"Finds {engine.Setup.SearchChance(engine.Knowledge(spot) == SearchSpotKnowledge.Promising)}% of the time on {GroundText(engine.Knowledge(spot))}.";
                break;
            case SearchActionIds.DigDeeper:
                summary = DigText(preview.Outcomes);
                kind = preview.Outcomes.Any(outcome => outcome.Label == "Spoils the spot") ? SearchActionKind.Risky : SearchActionKind.Normal;
                break;
            default:
                summary = SiftText(engine, spot);
                break;
        }

        summary += " Uses 1 search.";
        if (preview.FinishesItem)
        {
            summary += " " + LastSearch;
            kind = SearchActionKind.LastSearch;
        }

        return new SearchActionView(summary, tooltip, kind);
    }

    /// <summary>A Dig Deeper's results as "find 25%, nothing 25%, spoils the spot 50%", in the order of the preview.</summary>
    public static string DigText(IReadOnlyList<MinigameOutcomePreview> outcomes) =>
        string.Join(", ", outcomes.Select(outcome => $"{outcome.Label.ToLowerInvariant()} {outcome.ChancePercent}%")) + ".";

    /// <summary>What a Sift at a spot shows.</summary>
    public static string SiftText(SearchEngine engine, int spot)
    {
        var count = SearchBoard.PopCount(engine.UnknownAround(spot));
        return count == 1 ? "Shows the ground at 1 spot next to it." : $"Shows the ground at {count} spots next to it.";
    }

    /// <summary>What a performed action did, for the status line.</summary>
    public static string EventText(SearchEngine engine, SearchEvent last)
    {
        if (last == null || last.Kind == SearchEventKind.None)
            return string.Empty;

        var where = SearchBoard.SpotName(last.Spot);
        switch (last.Kind)
        {
            case SearchEventKind.Search:
                return last.IsFind ? $"You search the {where} spot and find something." : $"You search the {where} spot and find nothing.";
            case SearchEventKind.DigDeeper:
                return last.Result switch
                {
                    SearchEventResult.Found => $"You dig deeper at the {where} spot and turn up a find.",
                    SearchEventResult.Spoiled => $"You dig too deep at the {where} spot and spoil it.",
                    _ => $"You dig deeper at the {where} spot and find nothing."
                };
            default:
                var revealed = SiftResultText(engine, last);
                return $"You sift the ground around the {where} spot. {revealed}";
        }
    }

    /// <summary>What a Sift showed, naming the promising and the ordinary spots among those it revealed.</summary>
    public static string SiftResultText(SearchEngine engine, SearchEvent last)
    {
        var promising = new List<string>();
        var ordinary = new List<string>();
        for (var spot = 0; spot < SearchBoard.Spots; spot++)
        {
            if ((last.Revealed & SearchBoard.Bit(spot)) == 0)
                continue;

            (engine.Board.IsPromising(spot) ? promising : ordinary).Add(SearchBoard.SpotName(spot));
        }

        var parts = new List<string>();
        if (promising.Count > 0)
            parts.Add($"Promising: {string.Join(", ", promising)}.");
        if (ordinary.Count > 0)
            parts.Add($"Ordinary: {string.Join(", ", ordinary)}.");

        return string.Join(" ", parts);
    }

    /// <summary>The searches left as the window says it, such as "Searches left: 3 of 5".</summary>
    public static string SearchesText(SearchEngine engine) =>
        $"Searches left: {engine.SearchesLeft} of {engine.Setup.Searches}";
}
