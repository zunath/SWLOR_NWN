using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.MigrationService;

namespace SWLOR.Game.Server.Tests.Service;

public class ServerMigrationProgressTests
{
    [Test]
    public void UnchangedRecordsAdvanceOverallProgressAcrossSections()
    {
        var messages = new List<string>();
        var elapsed = TimeSpan.Zero;
        var progress = new ServerMigrationProgress(24,
            new[] { (nameof(InventoryItem), 2), (nameof(PlayerShip), 1) },
            messages.Add, () => elapsed);

        progress.BeginSection<InventoryItem>().Should().Be(2);
        elapsed = TimeSpan.FromSeconds(5);
        progress.RecordProcessed(false);
        messages[^1].Should().Contain("1/3 records (33.3%), 2 remaining, 0 changed")
            .And.Contain("ETA: about 10s");
        progress.RecordProcessed(true);
        progress.FinishSection();
        messages[^1].Should().Contain("2/3 records (66.7%), 1 remaining, 1 changed");

        progress.BeginSection<PlayerShip>().Should().Be(1);
        progress.RecordProcessed(false);
        progress.FinishSection();
        messages[^1].Should().Contain("section 2/2 PlayerShip (1/1 records, 0 changed)")
            .And.Contain("3/3 records (100.0%), 0 remaining, 1 changed")
            .And.Contain("ETA: 0s");
    }

    [Test]
    public void EmptySectionsReportCompletionWithoutInvalidPercentOrEta()
    {
        var messages = new List<string>();
        var progress = new ServerMigrationProgress(26, new[] { (nameof(Player), 0) },
            messages.Add, () => TimeSpan.Zero);

        progress.BeginSection<Player>();
        progress.FinishSection();

        messages[^1].Should().Contain("0/0 records (100.0%), 0 remaining")
            .And.Contain("Finished section 1/1 Player").And.Contain("ETA: 0s");
    }

    [Test]
    public void ReportsAreThrottledByTimeAndSectionCompletionAlwaysPrints()
    {
        var messages = new List<string>();
        var elapsed = TimeSpan.Zero;
        var progress = new ServerMigrationProgress(25, new[] { (nameof(InventoryItem), 1000) },
            messages.Add, () => elapsed);
        progress.BeginSection<InventoryItem>();
        messages[^1].Should().Contain("Loading").And.Contain("ETA: calculating");
        var initialCount = messages.Count;

        for (var i = 0; i < 998; i++) progress.RecordProcessed(false);
        messages.Count.Should().Be(initialCount);
        elapsed = TimeSpan.FromSeconds(5);
        progress.RecordProcessed(false);
        messages.Count.Should().Be(initialCount + 1);
        progress.RecordProcessed(false);
        progress.FinishSection();
        messages.Count.Should().Be(initialCount + 2);
        messages[^1].Should().Contain("1000/1000 records (100.0%)");
    }
}
