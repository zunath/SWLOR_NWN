using System.Text.Json;
using FluentAssertions;

namespace SWLOR.Toolset.Tests.Support;

/// <summary>
/// The checked-in animation bakes are produced on Windows. MathF.Acos, Sin, Cos, and Atan2 come from
/// the platform's C runtime, so other hosts differ in the last bits, and Acos near 1 amplifies that
/// to under 1e-3 in a few quaternion components. Windows compares the text exactly. Other hosts
/// compare the JSON with that numeric tolerance and ignore Quaternion.IsIdentity, which follows from
/// the rounded values.
/// </summary>
internal static class GoldenBake
{
    private const double NonWindowsTolerance = 1e-3;

    public static bool Matches(string actual, string expected)
    {
        if (actual == expected)
            return true;
        if (OperatingSystem.IsWindows())
            return false;

        using var actualJson = JsonDocument.Parse(actual);
        using var expectedJson = JsonDocument.Parse(expected);
        return Equivalent(actualJson.RootElement, expectedJson.RootElement);
    }

    public static void ShouldMatch(string actual, string expected, string because)
    {
        if (OperatingSystem.IsWindows())
            actual.Should().Be(expected, because);
        else
            Matches(actual, expected).Should().BeTrue(because);
    }

    private static bool Equivalent(JsonElement actual, JsonElement expected)
    {
        switch (actual.ValueKind)
        {
            case JsonValueKind.Object when expected.ValueKind == JsonValueKind.Object:
                var actualProperties = actual.EnumerateObject().Where(p => p.Name != "IsIdentity").ToList();
                var expectedProperties = expected.EnumerateObject().Where(p => p.Name != "IsIdentity").ToList();
                return actualProperties.Count == expectedProperties.Count &&
                       actualProperties.Zip(expectedProperties).All(pair =>
                           pair.First.Name == pair.Second.Name && Equivalent(pair.First.Value, pair.Second.Value));
            case JsonValueKind.Array when expected.ValueKind == JsonValueKind.Array:
                return actual.GetArrayLength() == expected.GetArrayLength() &&
                       actual.EnumerateArray().Zip(expected.EnumerateArray()).All(pair => Equivalent(pair.First, pair.Second));
            case JsonValueKind.Number when expected.ValueKind == JsonValueKind.Number:
                return Math.Abs(actual.GetDouble() - expected.GetDouble()) <= NonWindowsTolerance;
            default:
                return actual.ValueKind == expected.ValueKind && actual.GetRawText() == expected.GetRawText();
        }
    }
}
