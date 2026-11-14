namespace SWLOR.Architecture.Tests.Fixtures.Rules;

/// <summary>
/// A converted service done right: state on the instance, a constant, and a static lambda (whose compiler-generated
/// cache field is a static field the rule must not report).
/// </summary>
public class CleanInstanceService
{
    public const string Name = "clean";

    private readonly Dictionary<string, int> _cache = new();
    private int _counter;

    public int Count => _cache.Count + _counter;

    public void Increment() => _counter++;

    public Func<int, int> Doubler() => value => value * 2;
}
