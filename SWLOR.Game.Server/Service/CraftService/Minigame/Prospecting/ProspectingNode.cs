using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;

/// <summary>
/// What a vein or tree holds between two sessions: the units left in its pool, its Stability, and the seams still in it, hidden or
/// revealed. A node gets it when it is first worked and keeps it until the node is spent or collapses, so a player who leaves a node
/// leaves the rest of its yield, its damage and the seams already found to whoever works it next. It is an immutable value that
/// round-trips through <see cref="Serialize"/> and <see cref="TryParse"/>, which is how the game stores it on the node.
/// </summary>
public sealed record ProspectingNode
{
    private const char PoolMarker = 'P';
    private const char StabilityMarker = 'S';
    private const char SeamsMarker = ':';

    public ProspectingNode(int pool, int stability, ImmutableArray<ProspectingSeam> seams)
    {
        if (pool < 0)
            throw new ArgumentOutOfRangeException(nameof(pool), "A node cannot hold a negative pool.");
        if (stability < 0 || stability > ProspectingRules.MaxStability)
            throw new ArgumentOutOfRangeException(nameof(stability), "Stability is 0 to the maximum.");

        seams = seams.IsDefault ? ImmutableArray<ProspectingSeam>.Empty : seams;
        if (seams.Length > pool || seams.Length > ProspectingRules.MaxSeams)
            throw new ArgumentException("Each seam seals one unit of the pool, so a node holds no more seams than units.", nameof(seams));

        Pool = pool;
        Stability = stability;
        Seams = seams;
    }

    /// <summary>The units of yield left to take.</summary>
    public int Pool { get; }

    /// <summary>How sound the node is: 0 collapses it.</summary>
    public int Stability { get; }

    /// <summary>The seams still in the node, in the order they are found. Each one seals a unit of the pool.</summary>
    public ImmutableArray<ProspectingSeam> Seams { get; }

    /// <summary>True when nothing is left to take.</summary>
    public bool IsSpent => Pool <= 0;

    /// <summary>The units of the pool that are not sealed in a seam: what a Strike takes before it breaks into one.</summary>
    public int LooseUnits => Pool - Seams.Length;

    /// <summary>
    /// A node as it is the first time it is worked: a pool of 1 to <see cref="ProspectingRules.PoolDie"/> units (today's d4 harvests), full
    /// Stability and one to three hidden seams, no more than the pool has units. A seam is a gem pocket with a small chance that
    /// follows how often the node's table gives a gem (<see cref="ProspectingRules.GemPocketBasisPoints"/>), and a rich seam otherwise.
    /// </summary>
    public static ProspectingNode Create(IMinigameRandom random, double gemChance)
    {
        if (random == null)
            throw new ArgumentNullException(nameof(random));

        var pool = random.Next(1, ProspectingRules.PoolDie + 1);
        var seamRoll = random.Next(1, ProspectingRules.OneSeamWeight + ProspectingRules.TwoSeamsWeight + ProspectingRules.ThreeSeamsWeight + 1);
        var seamCount = Math.Min(pool, seamRoll <= ProspectingRules.OneSeamWeight ? 1
            : seamRoll <= ProspectingRules.OneSeamWeight + ProspectingRules.TwoSeamsWeight ? 2
            : 3);

        var pocketBasisPoints = ProspectingRules.GemPocketBasisPoints(gemChance);
        var seams = ImmutableArray.CreateBuilder<ProspectingSeam>(seamCount);
        for (var index = 0; index < seamCount; index++)
        {
            var gem = pocketBasisPoints > 0 && random.Next(1, 10001) <= pocketBasisPoints;
            seams.Add(ProspectingSeam.Hidden(gem ? ProspectingSeamKind.GemPocket : ProspectingSeamKind.Rich));
        }

        return new ProspectingNode(pool, ProspectingRules.MaxStability, seams.ToImmutable());
    }

    /// <summary>
    /// The node after units were taken from it without a session, as Quick Harvest does: the pool less the units, and no more seams than
    /// the pool has units left, the hidden ones lost first. Stability does not change.
    /// </summary>
    public ProspectingNode Take(int units)
    {
        var pool = Math.Max(0, Pool - Math.Max(0, units));
        var seams = Seams.ToBuilder();
        while (seams.Count > pool)
        {
            var index = 0;
            while (index < seams.Count && seams[index].Revealed)
                index++;

            seams.RemoveAt(index < seams.Count ? index : 0);
        }

        return new ProspectingNode(pool, Stability, seams.ToImmutable());
    }

    /// <summary>
    /// The text a node is stored as: <c>P3S85:rG</c> is a pool of 3, Stability 85, a hidden rich seam and a revealed gem pocket. A hidden
    /// seam is lower case (<c>r</c> rich, <c>g</c> gem pocket) and a revealed one upper case.
    /// </summary>
    public string Serialize()
    {
        var text = new StringBuilder();
        text.Append(PoolMarker).Append(Pool.ToString(CultureInfo.InvariantCulture));
        text.Append(StabilityMarker).Append(Stability.ToString(CultureInfo.InvariantCulture));
        text.Append(SeamsMarker);
        foreach (var seam in Seams)
            text.Append(Letter(seam));

        return text.ToString();
    }

    /// <summary>Reads text written by <see cref="Serialize"/>. Returns false for anything else, which a caller treats as a node never worked.</summary>
    public static bool TryParse(string text, out ProspectingNode node)
    {
        node = null;
        if (string.IsNullOrEmpty(text) || text[0] != PoolMarker)
            return false;

        var stabilityAt = text.IndexOf(StabilityMarker);
        var seamsAt = text.IndexOf(SeamsMarker);
        if (stabilityAt < 2 || seamsAt < stabilityAt + 2)
            return false;

        if (!int.TryParse(text.AsSpan(1, stabilityAt - 1), NumberStyles.None, CultureInfo.InvariantCulture, out var pool) ||
            !int.TryParse(text.AsSpan(stabilityAt + 1, seamsAt - stabilityAt - 1), NumberStyles.None, CultureInfo.InvariantCulture, out var stability) ||
            stability > ProspectingRules.MaxStability)
        {
            return false;
        }

        var seams = ImmutableArray.CreateBuilder<ProspectingSeam>();
        foreach (var letter in text.Substring(seamsAt + 1))
        {
            if (!TryReadSeam(letter, out var seam) || seams.Count >= ProspectingRules.MaxSeams)
                return false;

            seams.Add(seam);
        }

        if (seams.Count > pool)
            return false;

        node = new ProspectingNode(pool, stability, seams.ToImmutable());
        return true;
    }

    public override string ToString() => Serialize();

    public bool Equals(ProspectingNode other) =>
        other != null && Pool == other.Pool && Stability == other.Stability && Seams.SequenceEqual(other.Seams);

    public override int GetHashCode() => HashCode.Combine(Pool, Stability, Seams.Length);

    private static char Letter(ProspectingSeam seam)
    {
        var letter = seam.Kind == ProspectingSeamKind.GemPocket ? 'g' : 'r';
        return seam.Revealed ? char.ToUpperInvariant(letter) : letter;
    }

    private static bool TryReadSeam(char letter, out ProspectingSeam seam)
    {
        switch (letter)
        {
            case 'r':
                seam = new ProspectingSeam(ProspectingSeamKind.Rich, false);
                return true;
            case 'R':
                seam = new ProspectingSeam(ProspectingSeamKind.Rich, true);
                return true;
            case 'g':
                seam = new ProspectingSeam(ProspectingSeamKind.GemPocket, false);
                return true;
            case 'G':
                seam = new ProspectingSeam(ProspectingSeamKind.GemPocket, true);
                return true;
            default:
                seam = default;
                return false;
        }
    }
}
