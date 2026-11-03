using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;

namespace SWLOR.Game.Server.Tests.Service.Search;

/// <summary>
/// A middling board: five searches, two promising spots (the top and the bottom right), a 10% chance on ordinary ground and 45% on
/// promising, a first dig that risks 25% and 20% more for each dig before it, and a 50% find chance on a dig that does not spoil the spot.
/// The pinned numbers in the Search tests come from this setup.
/// </summary>
internal static class SearchTestData
{
    public const int Top = 1;
    public const int Left = 3;
    public const int Center = 4;
    public const int Right = 5;
    public const int TopLeft = 0;
    public const int BottomRight = 8;

    /// <summary>The promising spots of the test layout: the top and the bottom right.</summary>
    public static readonly int Layout = SearchBoard.Bit(Top) | SearchBoard.Bit(BottomRight);

    /// <summary>A roll of 1 picks the first outcome of an action; 100 picks the last.</summary>
    public const int FirstOutcome = 1;
    public const int LastOutcome = 100;

    public static SearchSetup Setup(
        int searches = 5,
        int promising = 2,
        int ordinary = 10,
        int promisingChance = 45,
        int riskBase = 25,
        int riskStep = 20,
        int findChance = 50,
        SearchEffects effects = null) =>
        new(searches, promising, ordinary, promisingChance, riskBase, riskStep, findChance, effects, 20, 3);

    /// <summary>An engine on the test setup and layout whose outcomes are chosen by the scripted rolls, one for each action with a chance in it.</summary>
    public static SearchEngine Engine(params int[] rolls) => new(new ScriptedMinigameRandom(rolls), Setup(), Layout);

    /// <summary>The same on a setup of the test's own.</summary>
    public static SearchEngine Engine(SearchSetup setup, params int[] rolls) => new(new ScriptedMinigameRandom(rolls), setup, Layout);

    /// <summary>The same on a layout of the test's own.</summary>
    public static SearchEngine EngineWithLayout(SearchSetup setup, int layout, params int[] rolls) =>
        new(new ScriptedMinigameRandom(rolls), setup, layout);

    public static string SearchAt(int spot) => SearchActionIds.For(SearchActionIds.Search, spot);

    public static string Dig(int spot) => SearchActionIds.For(SearchActionIds.DigDeeper, spot);

    public static string Sift(int spot) => SearchActionIds.For(SearchActionIds.Sift, spot);
}
