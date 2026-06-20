using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class HoloNetViewModelTests
{
    [Test]
    public void BroadcastDeductsCreditsBeforeQueueingWebhook()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "HoloNetViewModel.cs"));

        var balanceCheckIndex = source.IndexOf("if (GetGold(Player) < BroadcastPrice)", StringComparison.Ordinal);
        var deductionIndex = source.IndexOf("TakeGoldFromCreature(BroadcastPrice, Player, true)", StringComparison.Ordinal);
        var enqueueIndex = source.IndexOf("await BackgroundJob.EnqueueDiscordWebhook", StringComparison.Ordinal);

        balanceCheckIndex.Should().BeGreaterThanOrEqualTo(0);
        deductionIndex.Should().BeGreaterThan(balanceCheckIndex);
        deductionIndex.Should().BeLessThan(enqueueIndex);
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
