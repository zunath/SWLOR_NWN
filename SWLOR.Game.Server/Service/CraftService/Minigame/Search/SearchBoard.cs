using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Search;

/// <summary>What the player knows about a spot's ground.</summary>
public enum SearchSpotKnowledge
{
    /// <summary>Nothing yet: the ground may be promising or ordinary.</summary>
    Unknown = 0,

    /// <summary>The ground is promising: a Search there finds more often.</summary>
    Promising = 1,

    /// <summary>The ground is ordinary.</summary>
    Ordinary = 2
}

/// <summary>
/// The 3x3 board as one immutable value: which spots are promising (hidden from the player), which the player has learned about,
/// and what has been done at each spot. Each field is a mask with bit <c>n</c> for spot <c>n</c>; the dig counts take two bits a spot.
/// </summary>
/// <param name="Promising">The spots whose ground is promising. The player does not see this; only a Sift or a Search shows it.</param>
/// <param name="Known">The spots whose ground the player has learned, by a Sift next to them or a Search on them.</param>
/// <param name="Searched">The spots that have been searched.</param>
/// <param name="Spoiled">The spots a Dig Deeper has ruined.</param>
/// <param name="Found">The spots that have given at least one find.</param>
/// <param name="Digs">The Dig Deeper attempts at each spot, two bits a spot.</param>
public readonly record struct SearchBoard(int Promising, int Known, int Searched, int Spoiled, int Found, int Digs)
{
    public const int Size = 3;
    public const int Spots = Size * Size;
    public const int MaxDigsPerSpot = 3;

    private static readonly string[] SpotNames =
    {
        "top left", "top", "top right", "left", "center", "right", "bottom left", "bottom", "bottom right"
    };

    public static SearchBoard WithPromising(int promisingMask) => new(promisingMask, 0, 0, 0, 0, 0);

    public bool IsPromising(int spot) => Has(Promising, spot);

    public bool IsKnown(int spot) => Has(Known, spot);

    public bool IsSearched(int spot) => Has(Searched, spot);

    public bool IsSpoiled(int spot) => Has(Spoiled, spot);

    public bool HasFound(int spot) => Has(Found, spot);

    public int PromisingCount => PopCount(Promising);

    public int DigsAt(int spot) => (Digs >> (2 * spot)) & 3;

    /// <summary>What the player knows about the spot's ground.</summary>
    public SearchSpotKnowledge KnowledgeOf(int spot) =>
        !IsKnown(spot) ? SearchSpotKnowledge.Unknown
        : IsPromising(spot) ? SearchSpotKnowledge.Promising
        : SearchSpotKnowledge.Ordinary;

    public SearchBoard WithSearched(int spot, bool found) =>
        this with
        {
            Searched = Searched | Bit(spot),
            Known = Known | Bit(spot),
            Found = found ? Found | Bit(spot) : Found
        };

    public SearchBoard WithDug(int spot, bool found, bool spoiled) =>
        this with
        {
            Digs = Digs + (1 << (2 * spot)),
            Found = found ? Found | Bit(spot) : Found,
            Spoiled = spoiled ? Spoiled | Bit(spot) : Spoiled
        };

    public SearchBoard WithKnown(int mask) => this with { Known = Known | mask };

    /// <summary>The spots next to a spot: up, down, left and right.</summary>
    public static IReadOnlyList<int> Neighbors(int spot)
    {
        var row = spot / Size;
        var column = spot % Size;
        var neighbors = new List<int>(4);
        if (row > 0)
            neighbors.Add(spot - Size);
        if (column > 0)
            neighbors.Add(spot - 1);
        if (column < Size - 1)
            neighbors.Add(spot + 1);
        if (row < Size - 1)
            neighbors.Add(spot + Size);

        return neighbors;
    }

    /// <summary>The mask of a set of spots.</summary>
    public static int MaskOf(IEnumerable<int> spots)
    {
        var mask = 0;
        foreach (var spot in spots)
            mask |= Bit(spot);

        return mask;
    }

    /// <summary>The name of a spot as the window says it, such as "top left" or "center".</summary>
    public static string SpotName(int spot) => SpotNames[spot];

    public static int Bit(int spot) => 1 << spot;

    public static int PopCount(int mask) => System.Numerics.BitOperations.PopCount((uint)mask);

    private static bool Has(int mask, int spot) => (mask & Bit(spot)) != 0;
}
