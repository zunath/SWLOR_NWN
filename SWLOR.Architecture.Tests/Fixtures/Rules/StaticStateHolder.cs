namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>Holds static state the way an unconverted service does: a collection, a counter and a constant.</summary>
public static class StaticStateHolder
{
    public const int Limit = 3;

    public static readonly Dictionary<string, int> Cache = new();

    public static int Counter;

    public static List<int> Items { get; } = new();

    public static int Total { get; set; }
}
