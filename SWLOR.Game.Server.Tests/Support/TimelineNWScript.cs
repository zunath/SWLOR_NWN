#nullable enable
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The recording NWScript fake with a way to write a note into its call list, so a test can see in what order the
/// instance under test called the engine and the seams it asks the test to answer.
/// </summary>
public sealed class TimelineNWScript : FakeNWScript
{
    public const string NoteMember = "Note";

    /// <summary>Records <paramref name="text"/> as a call in sequence with the engine calls.</summary>
    public void Note(string text) => RecordCall(NoteMember, new object?[] { text });
}
