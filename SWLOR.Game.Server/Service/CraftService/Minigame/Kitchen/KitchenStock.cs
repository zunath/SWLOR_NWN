using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Kitchen;

/// <summary>
/// How many portions of each committed ingredient line are left in the pot's reach. It is immutable; using a portion returns a
/// new stock, and two stocks with the same counts are equal.
/// </summary>
public sealed class KitchenStock : IEquatable<KitchenStock>
{
    private readonly int[] _remaining;

    public KitchenStock(IEnumerable<int> remaining)
    {
        _remaining = remaining.ToArray();
    }

    public int Count => _remaining.Length;

    public int Remaining(int line) => _remaining[line];

    /// <summary>The stock with one portion of the line used.</summary>
    public KitchenStock Use(int line)
    {
        if (_remaining[line] <= 0)
            throw new InvalidOperationException($"No portions of ingredient line {line} are left.");

        var copy = (int[])_remaining.Clone();
        copy[line]--;
        return new KitchenStock(copy);
    }

    public bool Equals(KitchenStock other) =>
        other != null && _remaining.AsSpan().SequenceEqual(other._remaining);

    public override bool Equals(object obj) => obj is KitchenStock other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var value in _remaining)
            hash.Add(value);

        return hash.ToHashCode();
    }

    public override string ToString() => $"[{string.Join(", ", _remaining)}]";
}
