using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.Game.Server.Tests.Service;

public class PartialMatchSearchTextTests
{
    [TestCase("Catman", "(Catman*)")]
    [TestCase("Perm Tester", "(Perm* Tester*)")]
    [TestCase("Per Tes", "(Per* Tes*)")]
    [TestCase("  Kel   Tal ", "(Kel* Tal*)")]
    public void EveryWordMatchesTheStartOfANameWord(string search, string expected)
    {
        PartialMatchSearchText.Build(search).Should().Be(expected);
    }

    [TestCase("Vibroblade I", "(Vibroblade* I)")]
    [TestCase("Droid X 7", "(Droid* X 7)")]
    [TestCase("Perm Tester 1", "(Perm* Tester* 1)")]
    public void OneCharacterWordsStayExactBecauseRediSearchCannotPrefixThem(string search, string expected)
    {
        PartialMatchSearchText.Build(search).Should().Be(expected);
    }

    [TestCase("The Hunter", "(The* Hunter*)")]
    [TestCase("Lord of War", "(Lord* of* War*)")]
    [TestCase("Bob A", "(Bob* A)")]
    [TestCase("the", "(the*)")]
    [TestCase("a", "(a)")]
    public void StopWordsAreSearchedLikeAnyOtherWord(string search, string expected)
    {
        PartialMatchSearchText.Build(search).Should().Be(expected);
    }

    [Test]
    public void IndexesKeepStopWordsSoEveryWordCanBeRequired()
    {
        // RediSearch strips default stop words from the index and the query unless the index sets STOPWORDS 0.
        var options = typeof(DB).GetMethod("BuildIndexOptions", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, null)!;
        var serialize = options.GetType().GetMethod(
            "SerializeRedisArgs",
            BindingFlags.NonPublic | BindingFlags.Instance,
            new[] { typeof(List<object>) })!;
        var args = new List<object>();
        serialize.Invoke(options, new object[] { args });

        var text = args.Select(arg => arg.ToString()).ToList();
        var index = text.IndexOf("STOPWORDS");
        index.Should().BeGreaterThanOrEqualTo(0, "the index must be created with an explicit stop-word list");
        text[index + 1].Should().Be("0");
    }

    [Test]
    public void EachWordIsEscaped()
    {
        PartialMatchSearchText.Build("Dark-Side O'Neil").Should().Be("(Dark\\-Side* O\\'Neil*)");
    }

    [Test]
    public void PartialMatchesScopeEveryWordToTheField()
    {
        var queryString = new DBQuery<Player>()
            .AddFieldSearch(nameof(Player.Name), "Per Tes", true)
            .BuildQuery()
            .QueryString;

        // Without the group only the first word is limited to Name and the rest match any indexed field.
        queryString.Should().Contain("@Name:(Per* Tes*)");
    }

    [Test]
    public void ExactMatchesAreUnchanged()
    {
        var queryString = new DBQuery<Player>()
            .AddFieldSearch(nameof(Player.CitizenPropertyId), "city-id", false)
            .BuildQuery()
            .QueryString;

        queryString.Should().Contain("@CitizenPropertyId:city\\-id");
    }
}
