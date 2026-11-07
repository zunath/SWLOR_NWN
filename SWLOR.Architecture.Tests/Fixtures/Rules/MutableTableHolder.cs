using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Static readonly tables that are, or may be, mutable: the rule reports every field.</summary>
public static class MutableTableHolder
{
    public static readonly int[] Array = [1, 2];
    public static readonly List<int> List = [1];
    public static readonly Dictionary<string, int> Dictionary = new();
    public static readonly HashSet<string> Set = new();
    public static readonly IReadOnlyList<int> ReadOnlyView = new[] { 1 };
    public static readonly FrozenDictionary<string, List<int>> FrozenOfMutable = new Dictionary<string, List<int>>().ToFrozenDictionary();
    public static readonly ImmutableArray<int[]> ImmutableOfArrays = [];
    public static readonly SettableRow Settable = new("row");
    public static readonly PlainClassRow Plain = new();
    public static readonly ImmutableRecordWithArray WithArray = new([1]);
    public static readonly object Anything = new();
    public static readonly ImmutableArray<(int Rank, List<int> Values)> TupleOfList = [];
    public static readonly Stopwatch Watch = new();
    public static ImmutableArray<int> NotReadonly = [1];
    public static readonly FrozenDictionary<int, Effect> FrozenOfEngineClass = new Dictionary<int, Effect>().ToFrozenDictionary();
}
