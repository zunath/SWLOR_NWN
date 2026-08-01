using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SnippetService;

namespace SWLOR.Game.Server.Tests.Service;

public class SnippetActionExecutionTests
{
    [Test]
    public void ActionDelegateReportsWhetherTheOutcomeSucceeded()
    {
        typeof(SnippetActionDelegate).GetMethod("Invoke")!.ReturnType.Should().Be(typeof(bool));
    }

    [Test]
    public void QuestAdvanceCannotUseAPermanentMarkerBeforeRewardSelectionCompletes()
    {
        SnippetActionPolicy.CanRunOncePerPlayer("action-advance-quest").Should().BeFalse();
        SnippetActionPolicy.CanRunOncePerPlayer("action-give-key-items").Should().BeTrue();
    }

    [Test]
    public void OncePerPlayerMarkerIsSavedOnlyAfterSuccessAgainstPostActionPlayerState()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Service",
            "Snippet.cs"));

        var actionIndex = source.IndexOf(
            "snippet.ActionsTakenAction(player, arguments.ToArray())",
            StringComparison.Ordinal);
        var successGateIndex = source.IndexOf(
            "if (!succeeded)",
            actionIndex,
            StringComparison.Ordinal);
        var postActionReloadIndex = source.IndexOf(
            "var dbPlayer = DB.Get<Player>(GetObjectUUID(player));",
            successGateIndex,
            StringComparison.Ordinal);
        var markerIndex = source.IndexOf(
            "dbPlayer.CompletedDialogueActions.Add(oncePerPlayerId);",
            postActionReloadIndex,
            StringComparison.Ordinal);

        actionIndex.Should().BeGreaterThanOrEqualTo(0);
        successGateIndex.Should().BeGreaterThan(actionIndex,
            "a rejected outcome must not consume its once-per-player marker");
        postActionReloadIndex.Should().BeGreaterThan(successGateIndex,
            "reward actions may save Player themselves, so the marker must use their resulting state");
        markerIndex.Should().BeGreaterThan(postActionReloadIndex);
    }

    [Test]
    public void RuntimeDiscardsUnsupportedLegacyMarkersBeforeCheckingCompletion()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Service",
            "Snippet.cs"));

        var policyIndex = source.IndexOf(
            "if (!SnippetActionPolicy.CanRunOncePerPlayer(key))",
            StringComparison.Ordinal);
        var markerLookupIndex = source.IndexOf(
            "CompletedDialogueActions.Contains(oncePerPlayerId)",
            StringComparison.Ordinal);

        policyIndex.Should().BeGreaterThanOrEqualTo(0);
        markerLookupIndex.Should().BeGreaterThan(policyIndex,
            "old quest-advance markers must not block a player from reopening reward selection");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
