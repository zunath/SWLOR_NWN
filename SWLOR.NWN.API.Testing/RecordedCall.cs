namespace SWLOR.NWN.API.Testing;

/// <summary>One engine call a fake received: the member it was made on and the arguments in order.</summary>
/// <param name="Member">The member name, for example `GetTag`. Overloads share a name; tell them apart by the arguments.</param>
/// <param name="Arguments">The arguments as passed. A `params` array is one argument.</param>
public sealed record RecordedCall(string Member, IReadOnlyList<object?> Arguments)
{
    public override string ToString() => $"{Member}({string.Join(", ", Arguments.Select(Describe))})";

    private static string Describe(object? argument)
    {
        return argument switch
        {
            null => "null",
            string text => "\"" + text + "\"",
            Array array => "[" + string.Join(", ", array.Cast<object?>().Select(Describe)) + "]",
            _ => Convert.ToString(argument, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
        };
    }
}
