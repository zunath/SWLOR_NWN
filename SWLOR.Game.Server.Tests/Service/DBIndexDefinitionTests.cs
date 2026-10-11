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

    private static string Fingerprint(IEnumerable<(string Name, bool IsNumeric)> fields)
    {
        return SearchIndexDefinition.Fingerprint(SearchIndexDefinition.BuildCreateArgs("Player", "Player", fields));
    }
}
