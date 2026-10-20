using System.Text.Json;
using FluentAssertions;

namespace SWLOR.Toolset.Tests.Support;

/// <summary>
/// The checked-in animation bakes are produced on Windows. MathF.Acos, Sin, Cos, and Atan2 come from
/// the platform's C runtime, so other hosts differ in the last bits: on Linux, Orientation components
/// by up to 8.6e-4 (Acos near 1 amplifies the difference) and Position components derived from them
/// by under 1e-6. Windows compares the text exactly. Other hosts compare the JSON exactly except for
/// those components, which get their own tolerances, and Orientation.IsIdentity, which follows from
/// the rounded components.
/// </summary>
internal static class GoldenBake
{
    private const double OrientationTolerance = 1e-3;
    private const double PositionTolerance = 1e-6;

    public static bool Matches(string actual, string expected)
    {
        if (actual == expected)
            return true;
        if (OperatingSystem.IsWindows())
            return false;

        using var actualJson = JsonDocument.Parse(actual);
        using var expectedJson = JsonDocument.Parse(expected);
        return Equivalent(actualJson.RootElement, expectedJson.RootElement, name: null, parent: null);
    }

    public static void ShouldMatch(string actual, string expected, string because)
    {
        if (OperatingSystem.IsWindows())
            actual.Should().Be(expected, because);
        else
            Matches(actual, expected).Should().BeTrue(because);
    }

    /// <param name="name">The property that holds <paramref name="actual"/>.</param>
    /// <param name="parent">The property that holds the object containing <paramref name="actual"/>.</param>
    private static bool Equivalent(JsonElement actual, JsonElement expected, string? name, string? parent)
    {
        switch (actual.ValueKind)
        {
            case JsonValueKind.Object when expected.ValueKind == JsonValueKind.Object:
                Func<JsonProperty, bool> compared = property => !(name == "Orientation" && property.Name == "IsIdentity");
                var actualProperties = actual.EnumerateObject().Where(compared).ToList();
                var expectedProperties = expected.EnumerateObject().Where(compared).ToList();
                return actualProperties.Count == expectedProperties.Count &&
                       actualProperties.Zip(expectedProperties).All(pair =>
                           pair.First.Name == pair.Second.Name &&
                           Equivalent(pair.First.Value, pair.Second.Value, pair.First.Name, name));
            case JsonValueKind.Array when expected.ValueKind == JsonValueKind.Array:
                return actual.GetArrayLength() == expected.GetArrayLength() &&
                       actual.EnumerateArray().Zip(expected.EnumerateArray())
                           .All(pair => Equivalent(pair.First, pair.Second, name, parent));
            case JsonValueKind.Number when expected.ValueKind == JsonValueKind.Number:
                var tolerance = parent switch
                {
                    "Orientation" => OrientationTolerance,
                    "Position" => PositionTolerance,
                    _ => 0d
                };
                return tolerance == 0d
                    ? actual.GetRawText() == expected.GetRawText()
                    : Math.Abs(actual.GetDouble() - expected.GetDouble()) <= tolerance;
            default:
                return actual.ValueKind == expected.ValueKind && actual.GetRawText() == expected.GetRawText();
        }
    }
}
