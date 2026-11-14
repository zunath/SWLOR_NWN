using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityGazetteService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The City Gazette: which events become entries, how an entry reads, who sees it, and how old entries are dropped.
/// </summary>
public class CityGazetteTests
{
    private static readonly DateTime Start = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    private static CityGazetteEntry Entry(int minutesAfterStart, bool isSecret = false, string text = "Entry")
    {
        var time = Start.AddMinutes(minutesAfterStart);
        return new CityGazetteEntry
        {
            CityId = "city",
            TimeUtc = time,
            DateCreated = time,
            Text = text,
            IsSecret = isSecret
        };
    }

    private static List<CityGazetteEntry> Entries(int count) =>
        Enumerable.Range(0, count).Select(i => Entry(i)).ToList();

    // Pruning

    [Test]
    public void ACityKeepsItsNewest200Entries()
    {
        var entries = Entries(250);

        var pruned = CityGazetteRules.SelectEntriesToPrune(entries);

        pruned.Should().HaveCount(50);
        pruned.Should().OnlyContain(entry => entry.TimeUtc < Start.AddMinutes(50));
        entries.Except(pruned).Should().HaveCount(CityGazetteRules.MaxEntriesPerCity);
    }

    [Test]
    public void NothingIsPrunedAtOrBelow200Entries()
    {
        CityGazetteRules.SelectEntriesToPrune(Entries(200)).Should().BeEmpty();
        CityGazetteRules.SelectEntriesToPrune(Entries(1)).Should().BeEmpty();
        CityGazetteRules.SelectEntriesToPrune(new List<CityGazetteEntry>()).Should().BeEmpty();
    }

    [Test]
    public void PruningCountsSecretEntriesLikeAnyOther()
    {
        var entries = Entries(201);
        entries[0].IsSecret = true;

        var pruned = CityGazetteRules.SelectEntriesToPrune(entries);

        pruned.Should().ContainSingle().Which.Should().BeSameAs(entries[0]);
    }

    [Test]
    public void PruningKeepsTheNewestWhenEntriesShareATime()
    {
        var entries = Enumerable.Range(0, 3).Select(_ => Entry(0)).ToList();
        entries[1].DateCreated = Start.AddSeconds(5);

        var pruned = CityGazetteRules.SelectEntriesToPrune(entries, 1);

        pruned.Should().HaveCount(2);
        pruned.Should().NotContain(entries[1]);
    }

    // Who sees what

    [Test]
    public void SecretEntriesAreHiddenFromReaders()
    {
        var entries = new List<CityGazetteEntry>
        {
            Entry(1, text: "Public"),
            Entry(2, isSecret: true, text: "Secret"),
            Entry(3, text: "Also public")
        };

        var visible = CityGazetteRules.SelectVisibleEntries(entries, false);

        visible.Select(entry => entry.Text).Should().Equal("Also public", "Public");
    }

    [Test]
    public void SecretEntriesAppearWhenAskedFor()
    {
        var entries = new List<CityGazetteEntry>
        {
            Entry(1, text: "Public"),
            Entry(2, isSecret: true, text: "Secret")
        };

        var visible = CityGazetteRules.SelectVisibleEntries(entries, true);

        visible.Select(entry => entry.Text).Should().Equal("Secret", "Public");
    }

    [Test]
    public void EntriesAreListedNewestFirstAndCappedAt200()
    {
        var entries = Entries(230);

        var visible = CityGazetteRules.SelectVisibleEntries(entries, false);

        visible.Should().HaveCount(200);
        visible[0].TimeUtc.Should().Be(Start.AddMinutes(229));
        visible.Select(entry => entry.TimeUtc).Should().BeInDescendingOrder();
    }

    [Test]
    public void ARevealedEntryJoinsThePublicLogWhereItsTimeSaysItBelongs()
    {
        var entries = new List<CityGazetteEntry>
        {
            Entry(1, text: "Old"),
            Entry(2, isSecret: true, text: "Bribe"),
            Entry(3, text: "New")
        };

        entries[1].IsSecret = false;

        CityGazetteRules.SelectVisibleEntries(entries, false).Select(entry => entry.Text)
            .Should().Equal("New", "Bribe", "Old");
    }

    // Discord

    [Test]
    public void OnlyNonSecretEntriesPostToTheCityFeed()
    {
        CityGazetteRules.ShouldPostToFeed(new CityGazetteEntry { IsSecret = false }).Should().BeTrue();
        CityGazetteRules.ShouldPostToFeed(new CityGazetteEntry { IsSecret = true }).Should().BeFalse();
        CityGazetteRules.ShouldPostToFeed(null).Should().BeFalse();
    }

    [Test]
    public void HookEntriesPostToTheDMFeedWhetherOrNotTheyAreSecret()
    {
        CityGazetteRules.ShouldPostToDMFeed(new CityGazetteEntry { IsHook = true }).Should().BeTrue();
        CityGazetteRules.ShouldPostToDMFeed(new CityGazetteEntry { IsHook = true, IsSecret = true }).Should().BeTrue();
        CityGazetteRules.ShouldPostToDMFeed(new CityGazetteEntry { IsHook = false }).Should().BeFalse();
        CityGazetteRules.ShouldPostToDMFeed(null).Should().BeFalse();
    }

    [Test]
    public void TheCityFeedNamesTheOfficialsTitleAndNeverTheirName()
    {
        var description = CityGazetteRules.BuildFeedDescription("Dawn Reach", "Village", 2, "Mayor", "Taxes changed: Citizenship tax 500 -> 600.");

        description.Should().Contain("**City**: Dawn Reach");
        description.Should().Contain("**City Level**: Village (Lvl. 2)");
        description.Should().Contain("**Official**: Mayor");
        description.Should().EndWith("Taxes changed: Citizenship tax 500 -> 600.");
    }

    [Test]
    public void TheCityFeedLeavesOutTheOfficialLineWhenThereIsNoTitle()
    {
        CityGazetteRules.BuildFeedDescription("Dawn Reach", "Village", 2, "", "Text").Should().NotContain("Official");
    }

    [Test]
    public void TheDMFeedNamesTheOfficialAndFlagsSecrets()
    {
        var description = CityGazetteRules.BuildHookDescription("Dawn Reach", "Village", 2, "Jaro Tek", "Marshal", true, "Took a bribe.");

        description.Should().Contain("**Official**: Jaro Tek (Marshal)");
        description.Should().Contain("**Secret**: Hidden from the public Gazette");
        description.Should().EndWith("Took a bribe.");
    }

    [Test]
    public void ThePublicDMFeedHookHasNoSecretLine()
    {
        CityGazetteRules.BuildHookDescription("Dawn Reach", "Village", 2, "", "", false, "A beast was sighted.")
            .Should().NotContain("Secret").And.NotContain("Official");
    }

    // Text

    [TestCase("**City**: Dawn", "City: Dawn")]
    [TestCase("a *b* c", "a b c")]
    [TestCase("__under__ ~~gone~~ ||spoiler|| `code`", "under gone spoiler code")]
    [TestCase("plain text", "plain text")]
    [TestCase("", "")]
    [TestCase(null, "")]
    public void DiscordMarkdownIsStripped(string text, string expected)
    {
        CityGazetteRules.StripMarkdown(text).Should().Be(expected);
    }

    [Test]
    public void ABroadcastReadsAsOneLineWithItsFactsInBrackets()
    {
        var text = CityGazetteRules.BuildBroadcastText(
            "City Election Open",
            "A new mayoral election has opened for candidate registration.\n" +
            "**Registration Closes**: 2026-11-16 12:00 UTC\n" +
            "**Voting Closes**: 2026-11-23 12:00 UTC");

        text.Should().Be(
            "City Election Open: A new mayoral election has opened for candidate registration. " +
            "(Registration Closes: 2026-11-16 12:00 UTC; Voting Closes: 2026-11-23 12:00 UTC)");
        text.Should().NotContain("*").And.NotContain("\n");
    }

    [Test]
    public void ABroadcastWithoutFactsHasNoBrackets()
    {
        CityGazetteRules.BuildBroadcastText("City Upkeep Paid", "The outstanding city upkeep has been paid.")
            .Should().Be("City Upkeep Paid: The outstanding city upkeep has been paid.");
    }

    [Test]
    public void ABroadcastWithNoDetailsIsJustItsTitle()
    {
        CityGazetteRules.BuildBroadcastText("City Upkeep Paid", "").Should().Be("City Upkeep Paid");
    }

    [Test]
    public void OnlyChangedTaxesAreListed()
    {
        var text = CityGazetteRules.BuildTaxChangeText(new[]
        {
            ("Citizenship tax", "500 cr per week", "700 cr per week"),
            ("Transportation tax", "5%", "5%"),
            ("Trade Exchange tariff", "3%", "4%")
        });

        text.Should().Be("Taxes changed: Citizenship tax 500 cr per week -> 700 cr per week; Trade Exchange tariff 3% -> 4%.");
    }

    [Test]
    public void UnchangedTaxesMakeNoEntry()
    {
        CityGazetteRules.BuildTaxChangeText(new[] { ("Citizenship tax", "500", "500") }).Should().BeEmpty();
        CityGazetteRules.BuildTaxChangeText(null).Should().BeEmpty();
    }

    [Test]
    public void TheMayorActsAsMayorAndAnyoneElseAsADelegate()
    {
        CityGazetteRules.GetTaxOfficialTitle(true).Should().Be("Mayor");
        CityGazetteRules.GetTaxOfficialTitle(false).Should().Be("Delegate");
    }

    [Test]
    public void ANewEntryCarriesTheOfficialAndFlags()
    {
        var entry = CityGazetteRules.CreateEntry("city-1", CityGazetteEntryType.Decision, "  Text  ", "player-1", " Marshal ", true, true, Start);

        entry.CityId.Should().Be("city-1");
        entry.EntryType.Should().Be(CityGazetteEntryType.Decision);
        entry.Text.Should().Be("Text");
        entry.OfficialPlayerId.Should().Be("player-1");
        entry.OfficialTitle.Should().Be("Marshal");
        entry.IsSecret.Should().BeTrue();
        entry.IsHook.Should().BeTrue();
        entry.TimeUtc.Should().Be(Start);
    }

    [Test]
    public void ANewEntryWithNoOfficialHasEmptyOfficialFields()
    {
        var entry = CityGazetteRules.CreateEntry("city-1", CityGazetteEntryType.Notice, "Text", null, null, false, false, Start);

        entry.OfficialPlayerId.Should().BeEmpty();
        entry.OfficialTitle.Should().BeEmpty();
    }

    [TestCase(null, "text", false)]
    [TestCase("", "text", false)]
    [TestCase("city", "", false)]
    [TestCase("city", "   ", false)]
    [TestCase("city", null, false)]
    [TestCase("city", "text", true)]
    public void AnEntryNeedsACityAndText(string cityId, string text, bool expected)
    {
        CityGazetteRules.IsPostable(cityId, text).Should().Be(expected);
    }

    // Which events create entries

    [Test]
    public void EveryCityAndStationBroadcastBecomesAnEntry()
    {
        CityGazetteRules.ShouldMirrorBroadcast(new WorldProperty { PropertyType = PropertyType.City }).Should().BeTrue();
        CityGazetteRules.ShouldMirrorBroadcast(new WorldProperty
        {
            PropertyType = PropertyType.City,
            Layout = PropertyLayoutType.Station
        }).Should().BeTrue();
    }

    [Test]
    public void OtherPropertiesMakeNoEntries()
    {
        CityGazetteRules.ShouldMirrorBroadcast(null).Should().BeFalse();
        CityGazetteRules.ShouldMirrorBroadcast(new WorldProperty { PropertyType = PropertyType.Apartment }).Should().BeFalse();
        CityGazetteRules.ShouldMirrorBroadcast(new WorldProperty { PropertyType = PropertyType.CityHall }).Should().BeFalse();
        CityGazetteRules.ShouldMirrorBroadcast(new WorldProperty { PropertyType = PropertyType.Structure }).Should().BeFalse();
        CityGazetteRules.ShouldMirrorBroadcast(new WorldProperty { PropertyType = PropertyType.City, Id = "" }).Should().BeFalse();
    }

    [TestCase("City Election Open", CityGazetteEntryType.Election)]
    [TestCase("City Election Voting Open", CityGazetteEntryType.Election)]
    [TestCase("City Election Concluded", CityGazetteEntryType.Election)]
    [TestCase("New Mayor Elected", CityGazetteEntryType.Election)]
    [TestCase("City Upkeep Due", CityGazetteEntryType.Upkeep)]
    [TestCase("City Upkeep Paid", CityGazetteEntryType.Upkeep)]
    [TestCase("City Upkeep Warning", CityGazetteEntryType.Upkeep)]
    [TestCase("City Upkeep Expiration", CityGazetteEntryType.Upkeep)]
    [TestCase("City Citizen Warning", CityGazetteEntryType.Citizens)]
    [TestCase("City Expiration", CityGazetteEntryType.Citizens)]
    [TestCase("New Player City Founded", CityGazetteEntryType.Founding)]
    [TestCase("New Player Station Founded", CityGazetteEntryType.Founding)]
    [TestCase("Something New", CityGazetteEntryType.Notice)]
    [TestCase("", CityGazetteEntryType.Notice)]
    [TestCase(null, CityGazetteEntryType.Notice)]
    public void BroadcastsMapToEntryTypes(string title, CityGazetteEntryType expected)
    {
        CityGazetteRules.GetEntryTypeForEvent(title).Should().Be(expected);
    }

    [Test]
    public void ThePropertyBroadcastWritesTheGazetteBeforeItChecksForAWebhook()
    {
        var method = FindMethod(ReadServerSource("Service/Property.cs"), "BroadcastPropertyEvent");

        var first = method.Body!.Statements.First();
        first.ToString().Should().Be("CityGazette.RecordPropertyBroadcast(city, title, details);");
    }

    [Test]
    public void ChangingTaxesInManageCityPostsATaxEntry()
    {
        var method = FindMethod(ReadServerSource("Feature/GuiDefinition/ViewModel/ManageCityViewModel.cs"), "PostTaxChangesToGazette");

        method.ToString().Should().Contain("CityGazette.Post(").And.Contain("CityGazetteEntryType.Tax");

        var save = FindMethod(ReadServerSource("Feature/GuiDefinition/ViewModel/ManageCityViewModel.cs"), "SaveChanges");
        save.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Select(call => call.Expression.ToString())
            .Should().Contain("PostTaxChangesToGazette");
    }

    [Test]
    public void ADeletedCityTakesItsGazetteWithIt()
    {
        var method = FindMethod(ReadServerSource("Service/Property.cs"), "DeleteProperty");

        method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Select(call => call.Expression.ToString())
            .Should().Contain("CityGazette.DeleteAll");
    }

    private static MethodDeclarationSyntax FindMethod(SyntaxNode root, string name) =>
        root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(method => method.Identifier.ValueText == name);

    private static SyntaxNode ReadServerSource(string path) =>
        CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(ServerRoot(), path))).GetRoot();

    private static string ServerRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull();
        return Path.Combine(directory!.FullName, "SWLOR.Game.Server");
    }
}
