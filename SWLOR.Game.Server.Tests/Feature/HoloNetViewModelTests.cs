using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Feature;

public class HoloNetViewModelTests
{
    [Test]
    public void BroadcastDeductsCreditsBeforeQueueingWebhook()
    {
        var root = RepoPaths.FindRepositoryRoot();
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

    [Test]
    public void BroadcastRefundsCreditsWhenWebhookQueueFails()
    {
        var root = RepoPaths.FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "GuiDefinition",
            "ViewModel",
            "HoloNetViewModel.cs"));

        var failedEnqueueIndex = source.IndexOf("if (!await BackgroundJob.EnqueueDiscordWebhook", StringComparison.Ordinal);
        failedEnqueueIndex.Should().BeGreaterThanOrEqualTo(0, "the failed enqueue branch should exist");

        var refundIndex = source.IndexOf("GiveGoldToCreature(Player, BroadcastPrice)", failedEnqueueIndex, StringComparison.Ordinal);
        var returnIndex = source.IndexOf("return;", failedEnqueueIndex, StringComparison.Ordinal);

        refundIndex.Should().BeGreaterThan(failedEnqueueIndex);
        refundIndex.Should().BeLessThan(returnIndex);
    }

}
