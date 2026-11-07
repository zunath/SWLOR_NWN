using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Static readonly tables of deeply immutable types: the rule accepts every field.</summary>
public static class ImmutableTableHolder
{
    public static readonly int Number = 4;
    public static readonly string Text = "text";
    public static readonly ImmutableTableKind Kind = ImmutableTableKind.Second;
    public static readonly Regex Pattern = new("a+", RegexOptions.Compiled);
    public static readonly FrozenDictionary<string, int> Lookup = new Dictionary<string, int> { ["a"] = 1 }.ToFrozenDictionary();
    public static readonly FrozenSet<ImmutableTableKind> Kinds = new[] { ImmutableTableKind.First }.ToFrozenSet();
    public static readonly ImmutableArray<string> Names = ["a", "b"];
    public static readonly ImmutableList<int> Numbers = ImmutableList.Create(1, 2);
    public static readonly ImmutableDictionary<string, ImmutableArray<int>> Nested = ImmutableDictionary<string, ImmutableArray<int>>.Empty;
    public static readonly ImmutableRow Row = new("row", 1);
    public static readonly ImmutableRowStruct Range = new(1, 2);
    public static readonly ImmutableArray<ImmutableRow> Rows = [new ImmutableRow("row", 1)];
    public static readonly FrozenDictionary<ImmutableTableKind, ImmutableRowStruct> Ranges = new Dictionary<ImmutableTableKind, ImmutableRowStruct>().ToFrozenDictionary();
    public static readonly ImmutableNode Node = new("node");
    public static readonly int? Maybe = 3;
    public static readonly ImmutableArray<(int Rank, int Value)> Steps = ImmutableArray.Create((0, 6), (10, 8));
    public static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    public static readonly Guid Id = Guid.Empty;
    public static readonly FrozenDictionary<EffectIconType, AbilityType> EngineEnums =
        new Dictionary<EffectIconType, AbilityType> { [EffectIconType.Stunned] = AbilityType.Might }.ToFrozenDictionary();
}
