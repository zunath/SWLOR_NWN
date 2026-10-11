using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
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
    [TestCase("Bob A", "(Bob* A)")]
    [TestCase("Perm Tester 1", "(Perm* Tester* 1)")]
    public void OneCharacterWordsStayExactBecauseRediSearchCannotPrefixThem(string search, string expected)
    {
        PartialMatchSearchText.Build(search).Should().Be(expected);
    }

    [TestCase("The Hunter", "(The Hunter*)")]
    [TestCase("Lord of War", "(Lord* of War*)")]
    public void StopWordsStayExactBecauseTheyAreNeverIndexed(string search, string expected)
    {
        PartialMatchSearchText.Build(search).Should().Be(expected);
    }

    [TestCase("the", "the*")]
    [TestCase("a", "a*")]
    [TestCase("The A", "The A*")]
    public void SearchesWithoutAPrefixableWordKeepTheSinglePrefixForm(string search, string expected)
    {
        // An all-exact group of stop words would match every record.
        PartialMatchSearchText.Build(search).Should().Be(expected);
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
