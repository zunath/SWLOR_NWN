using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.MasteryService;

namespace SWLOR.Game.Server.Tests.Service;

public class MasteryCatalogSeedTests
{
    [Test]
    public void Entries_ContainsExactly99Rows()
    {
        MasteryCatalogSeed.Entries.Should().HaveCount(99);
    }

    [Test]
    public void Entries_EveryRowHasAUniqueNonEmptyName()
    {
        MasteryCatalogSeed.Entries.Should().OnlyHaveUniqueItems(e => e.Name);
        MasteryCatalogSeed.Entries.Should().OnlyContain(e => !string.IsNullOrWhiteSpace(e.Name));
    }

    [Test]
    public void BuildMissingCatalogEntries_EmptyExistingCatalog_ReturnsEveryCatalogSeedEntry()
    {
        var missing = MasteryRules.BuildMissingCatalogEntries(new List<Mastery>());

        missing.Should().HaveCount(MasteryCatalogSeed.Entries.Count);
    }

    [Test]
    public void BuildMissingCatalogEntries_NullExistingCatalog_ReturnsEveryCatalogSeedEntry()
    {
        var missing = MasteryRules.BuildMissingCatalogEntries(null);

        missing.Should().HaveCount(MasteryCatalogSeed.Entries.Count);
    }

    [Test]
    public void BuildMissingCatalogEntries_SomeNamesAlreadyExist_OnlyReturnsTheMissingOnes()
    {
        var existing = new List<Mastery>
        {
            new Mastery { Name = "Chef" },
            new Mastery { Name = "Slicing" }
        };

        var missing = MasteryRules.BuildMissingCatalogEntries(existing);

        missing.Should().HaveCount(MasteryCatalogSeed.Entries.Count - 2);
        missing.Should().NotContain(m => m.Name == "Chef");
        missing.Should().NotContain(m => m.Name == "Slicing");
    }

    [Test]
    public void BuildMissingCatalogEntries_EveryNameAlreadyExists_ReturnsNothing()
    {
        var existing = MasteryCatalogSeed.Entries
            .Select(e => new Mastery { Name = e.Name })
            .ToList();

        var missing = MasteryRules.BuildMissingCatalogEntries(existing);

        missing.Should().BeEmpty();
    }

    [Test]
    public void BuildMissingCatalogEntries_NeverProducesADuplicateOfAnExistingName()
    {
        var existing = new List<Mastery> { new Mastery { Name = "Chef" } };

        var missing = MasteryRules.BuildMissingCatalogEntries(existing);

        missing.Count(m => m.Name == "Chef").Should().Be(0);
    }

    [Test]
    public void BuildMissingCatalogEntries_ExistingRowWithStaffEditedFields_IsNeverOverwrittenOrDuplicated()
    {
        // Simulates a staff member having edited a seeded row's description/rarity after
        // launch. Re-running the seed must never touch it - only insertion of genuinely
        // missing rows is allowed.
        var staffEdited = new Mastery
        {
            Name = "Chef",
            Description = "A totally different, staff-rewritten description.",
            Rarity = MasteryRarityType.Rare,
            IsActive = false
        };

        var missing = MasteryRules.BuildMissingCatalogEntries(new List<Mastery> { staffEdited });

        missing.Should().NotContain(m => m.Name == "Chef");
        // The staff-edited instance itself is untouched (BuildMissingCatalogEntries never
        // mutates its input) - Mastery.SeedCatalog only ever calls DB.Set on the missing list.
        staffEdited.Description.Should().Be("A totally different, staff-rewritten description.");
        staffEdited.Rarity.Should().Be(MasteryRarityType.Rare);
        staffEdited.IsActive.Should().BeFalse();
    }

    [Test]
    public void BuildMissingCatalogEntries_NameMatchIsCaseInsensitive()
    {
        var existing = new List<Mastery> { new Mastery { Name = "cHEF" } };

        var missing = MasteryRules.BuildMissingCatalogEntries(existing);

        missing.Should().NotContain(m => m.Name == "Chef");
    }

    [Test]
    public void BuildMissingCatalogEntries_SeededRowRenamedByStaff_MatchesBySeedKeyAndIsNeverRecreated()
    {
        // Renaming a seeded row's Name (staff catalog management) must not make its
        // original seed entry look "missing" - SeedKey is set once at creation and never
        // changes, so it stays the source of truth for seed matching even after a rename.
        var renamedSeededRow = new Mastery
        {
            Name = "Master Chef",
            SeedKey = "Chef",
            IsSeeded = true
        };

        var missing = MasteryRules.BuildMissingCatalogEntries(new List<Mastery> { renamedSeededRow });

        missing.Should().NotContain(m => m.SeedKey == "Chef");
        missing.Should().NotContain(m => m.Name == "Chef");
    }

    [Test]
    public void BuildMissingCatalogEntries_ExistingRowWithNoSeedKey_FallsBackToMatchingByName()
    {
        // Rows created before SeedKey existed have no SeedKey at all - these must still be
        // recognized as already-seeded via the Name fallback, or every pre-existing seeded
        // row would be duplicated the first time this runs after the SeedKey field ships.
        var legacyRow = new Mastery { Name = "Chef" };
        legacyRow.SeedKey.Should().BeEmpty();

        var missing = MasteryRules.BuildMissingCatalogEntries(new List<Mastery> { legacyRow });

        missing.Should().NotContain(m => m.Name == "Chef");
    }

    [Test]
    public void BuildMissingCatalogEntries_NewlyInsertedSeedRows_HaveSeedKeySetToTheirName()
    {
        var missing = MasteryRules.BuildMissingCatalogEntries(new List<Mastery>());

        missing.Should().OnlyContain(m => m.SeedKey == m.Name);
    }

    [Test]
    public void BuildMissingCatalogEntries_NewRowsAreMarkedSeededAndActive()
    {
        var missing = MasteryRules.BuildMissingCatalogEntries(new List<Mastery>());

        missing.Should().OnlyContain(m => m.IsSeeded && m.IsActive);
    }
}
