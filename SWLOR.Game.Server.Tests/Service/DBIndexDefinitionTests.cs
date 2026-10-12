using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Tests.Service;

public class DBIndexDefinitionTests
{
    private static readonly (string Name, bool IsNumeric)[] PlayerFields =
    {
        ("Version", true), ("Name", false), ("IsDeleted", false), ("Id", false), ("EntityType", false)
    };

    [Test]
    public void EachIndexOnlyCoversItsOwnEntityHashesAndKeepsStopWords()
    {
        var args = SearchIndexDefinition.BuildCreateArgs("Player", "Player", PlayerFields);

        // Without PREFIX, RediSearch indexes every hash in the keyspace, so all entity indexes held every record.
        // Without STOPWORDS 0, words such as "the" and "a" are dropped from the index and from every query.
        args.Select(arg => arg.ToString()).Should().Equal(
            "Player", "ON", "HASH", "PREFIX", "1", "Index:Player:", "STOPWORDS", "0", "SCHEMA",
            "EntityType", "TEXT", "Id", "TEXT", "IsDeleted", "TEXT", "Name", "TEXT", "Version", "NUMERIC");
    }

    [Test]
    public void PrefixEndsAtTheTypeNameBoundary()
    {
        // "Index:Player" alone would also match "Index:PlayerNote:..." hashes.
        SearchIndexDefinition.KeyPrefix("Player").Should().Be("Index:Player:");
        SearchIndexDefinition.KeyPrefix("PlayerNote").Should().Be("Index:PlayerNote:");
    }

    [Test]
    public void FingerprintIgnoresReflectionOrder()
    {
        var reversed = PlayerFields.Reverse();

        Fingerprint(PlayerFields).Should().Be(Fingerprint(reversed));
    }

    [Test]
    public void FingerprintChangesWhenTheSchemaChanges()
    {
        var original = Fingerprint(PlayerFields);

        Fingerprint(PlayerFields.Append(("CitizenPropertyId", false))).Should().NotBe(original, "a field was added");
        Fingerprint(PlayerFields.Where(field => field.Name != "Name")).Should().NotBe(original, "a field was removed");
        Fingerprint(PlayerFields.Select(field => field.Name == "Version" ? (field.Name, false) : field))
            .Should().NotBe(original, "a field changed type");
    }

    [TestCase(typeof(int), true)]
    [TestCase(typeof(long?), true)]
    [TestCase(typeof(ulong), true)]
    [TestCase(typeof(string), false)]
    [TestCase(typeof(bool), false)]
    public void OnlyIntegerPropertiesAreNumericFields(Type type, bool expected)
    {
        SearchIndexDefinition.IsNumeric(type).Should().Be(expected);
    }

    [Test]
    public void FingerprintKeysDoNotCollideWithEntityKeys()
    {
        SearchIndexDefinition.FingerprintKey("Player").Should().Be("IndexDefinition:Player");
    }

    [Test]
    public void LiveIndexBuiltByThisCodeMatchesTheFingerprint()
    {
        // FT.INFO reports STOPWORDS 0 as an empty stopwords_list, and field types as TEXT/NUMERIC.
        var live = SearchIndexDefinition.DescribeLiveIndex("Player", "HASH", new[] { "Index:Player:" }, Array.Empty<string>(), LiveFields());

        SearchIndexDefinition.Fingerprint(live).Should().Be(Fingerprint(PlayerFields));
    }

    [Test]
    public void LiveIndexWithDefaultStopWordsDoesNotMatch()
    {
        // An earlier prefixed build kept RediSearch's default stop words; FT.INFO then omits stopwords_list.
        var live = SearchIndexDefinition.DescribeLiveIndex("Player", "HASH", new[] { "Index:Player:" }, null, LiveFields());

        SearchIndexDefinition.Fingerprint(live).Should().NotBe(Fingerprint(PlayerFields));
    }

    [Test]
    public void LegacyUnprefixedLiveIndexDoesNotMatch()
    {
        // Legacy FT.CREATE without PREFIX reports a single empty prefix.
        var live = SearchIndexDefinition.DescribeLiveIndex("Player", "HASH", new[] { string.Empty }, null, LiveFields());

        SearchIndexDefinition.Fingerprint(live).Should().NotBe(Fingerprint(PlayerFields));
    }

    [Test]
    public void LiveIndexWithAnotherSchemaDoesNotMatch()
    {
        var missingField = LiveFields().Where(field => field.Name != "Name");
        var retyped = LiveFields().Select(field => field.Name == "Version" ? (field.Name, "TEXT") : field);
        var customStopWords = new[] { "foo" };

        Describe(missingField, Array.Empty<string>()).Should().NotBe(Fingerprint(PlayerFields));
        Describe(retyped, Array.Empty<string>()).Should().NotBe(Fingerprint(PlayerFields));
        Describe(LiveFields(), customStopWords).Should().NotBe(Fingerprint(PlayerFields));
    }

    private static IEnumerable<(string Name, string Type)> LiveFields()
    {
        return PlayerFields.Select(field => (field.Name, field.IsNumeric ? "NUMERIC" : "TEXT"));
    }

    private static string Describe(IEnumerable<(string Name, string Type)> fields, IReadOnlyList<string> stopWords)
    {
        return SearchIndexDefinition.Fingerprint(
            SearchIndexDefinition.DescribeLiveIndex("Player", "HASH", new[] { "Index:Player:" }, stopWords, fields));
    }

    private static string Fingerprint(IEnumerable<(string Name, bool IsNumeric)> fields)
    {
        return SearchIndexDefinition.Fingerprint(SearchIndexDefinition.BuildCreateArgs("Player", "Player", fields));
    }
}
