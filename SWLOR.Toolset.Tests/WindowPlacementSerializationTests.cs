using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The settings file's floating-point fields.
    /// </summary>
    /// <remarks>
    /// This exists because of a specific silent failure. Window position used NaN in memory for "not
    /// recorded", the DTO carried it as a plain double, and System.Text.Json refuses NaN unless told
    /// otherwise - so every settings write threw, was swallowed by the best-effort catch in
    /// <c>Save</c>, and the whole feature looked simply unimplemented. The DTO is internal, so this
    /// asserts against the serializer's behaviour directly: the point is the rule, not the plumbing.
    /// </remarks>
    [TestFixture]
    public class WindowPlacementSerializationTests
    {
        private sealed class NaNCarrier
        {
            public double Value { get; set; }
        }

        private sealed class NullableCarrier
        {
            public double? Value { get; set; }
        }

        [Test]
        public void SerializingNaN_Throws_WhichIsWhyThePlacementDtoIsNullable()
        {
            var act = () => JsonSerializer.Serialize(new NaNCarrier { Value = double.NaN });

            act.Should().Throw<Exception>(
                "if this ever stops throwing, the nullable workaround in ToolsetSettingsData can go");
        }

        [Test]
        public void AnAbsentValue_RoundTripsAsNull()
        {
            var json = JsonSerializer.Serialize(new NullableCarrier { Value = null });
            var parsed = JsonSerializer.Deserialize<NullableCarrier>(json);

            parsed!.Value.Should().BeNull();
        }

        [Test]
        public void ARecordedValue_RoundTrips()
        {
            var json = JsonSerializer.Serialize(new NullableCarrier { Value = -1280.5 });
            var parsed = JsonSerializer.Deserialize<NullableCarrier>(json);

            parsed!.Value.Should().Be(-1280.5, "a window on a left-hand monitor has a negative X");
        }
    }
}
