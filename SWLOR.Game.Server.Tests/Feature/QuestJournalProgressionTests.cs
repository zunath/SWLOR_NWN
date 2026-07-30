using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Feature;

public class QuestJournalProgressionTests
{
    [Test]
    public void Advance_UsesNextStateJournalTextWhenUpdatingJournal()
    {
        var root = RepoPaths.FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Service",
            "QuestService",
            "QuestDetail.cs"));

        var nextStateIndex = source.IndexOf(
            "var nextState = GetState(playerQuest.CurrentState);",
            StringComparison.Ordinal);
        nextStateIndex.Should().BeGreaterThanOrEqualTo(0);

        var journalIndex = source.IndexOf(
            "PlayerPlugin.AddCustomJournalEntry",
            nextStateIndex,
            StringComparison.Ordinal);
        journalIndex.Should().BeGreaterThanOrEqualTo(0);

        var journalEndIndex = source.IndexOf("});", journalIndex, StringComparison.Ordinal);
        journalEndIndex.Should().BeGreaterThan(journalIndex);

        var journalBlock = source[journalIndex..journalEndIndex];
        journalBlock.Should().Contain("Text = nextState.JournalText");
        journalBlock.Should().NotContain("Text = currentState.JournalText");
    }

}
