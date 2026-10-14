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
    public void EachWordIsEscaped()
    {
        PartialMatchSearchText.Build("Dark-Side O'Neil").Should().Be("(Dark\\-Side* O\\'Neil*)");
    }

    [TestCase("Bookshelf, Pedestal", "(Bookshelf* Pedestal*)")]
    [TestCase("Mk.II", "(Mk* II*)")]
    [TestCase("*DRUMS* Lamp", "(DRUMS* Lamp*)")]
    [TestCase("50% off", "(50* off*)")]
    [TestCase("Rifle [Ion]; Mk:2", "(Rifle* Ion* Mk* 2)")]
    public void UnescapedPunctuationSeparatesWords(string search, string expected)
    {
        // "Bookshelf,*" is a RediSearch syntax error; the index splits words on these characters too.
        PartialMatchSearchText.Build(search).Should().Be(expected);
    }

    [TestCase("Brawler’s Wraps", "(Brawler’s* Wraps*)")]
    [TestCase("Café Noir", "(Café* Noir*)")]
    public void NonAsciiCharactersStayInsideAWord(string search, string expected)
    {
        // RediSearch only splits on ASCII punctuation, so "Brawler’s" is one indexed word.
        PartialMatchSearchText.Build(search).Should().Be(expected);
    }

    [TestCase("AC\\DC", "(ACDC*)")]
    [TestCase("a\\,b tail", "(a\\,b* tail*)")]
    [TestCase("Bob\\", "(Bob*)")]
    public void BackslashMakesTheNextCharacterLiteralAsInTheIndex(string search, string expected)
    {
        // The index stores "AC\DC" as the word "acdc" and "a\,b" as the word "a,b".
        PartialMatchSearchText.Build(search).Should().Be(expected);
    }

    [TestCase("Alpha Beta", "(Alpha Beta*)")]
    [TestCase("Alpha_Beta Gamma", "(Alpha_Beta* Gamma*)")]
    public void OnlyAsciiSpaceAndTabSeparateWhitespace(string search, string expected)
    {
        // The tokenizer splits on space and tab only; a no-break space stays inside the word.
        PartialMatchSearchText.Build(search).Should().Be(expected);
    }

    [TestCase("AC\\@DC", "(AC\\\\*)")]
    [TestCase("Q\\-R Tail", "(Q\\\\* Tail*)")]
    [TestCase("\\@ Tail", "(Tail*)")]
    public void WordsWithABackslashBeforePunctuationMatchUpToTheBackslash(string search, string expected)
    {
        // "AC\@DC" is stored as "AC\\@DC" and indexed as the word "ac\@dc", which no query can spell in full.
        PartialMatchSearchText.Build(search).Should().Be(expected);
    }

    [TestCase("X\\\\Y", "(X\\\\Y*)")]
    [TestCase("K\\\\ L", "(K\\\\L*)")]
    public void BackslashesBeforeLettersAreSearchedInFull(string search, string expected)
    {
        // "K\\ L" is indexed as the single word "k\l": a separator right after a backslash does not split.
        PartialMatchSearchText.Build(search).Should().Be(expected);
    }

    [Test]
    public void ControlCharactersAreDroppedFromWords()
    {
        PartialMatchSearchText.Build("Al\nBe Cd").Should().Be("(AlBe* Cd*)");
    }

    [TestCase("\U0001F600", "(\U0001F600)")]
    [TestCase("\U0001F600 Grin", "(\U0001F600 Grin*)")]
    [TestCase("Grin\U0001F600", "(Grin\U0001F600)")]
    public void WordsWithCharactersBeyondTheBasicPlaneStayExact(string search, string expected)
    {
        // A single emoji is two UTF-16 code units, and RediSearch prefixes never match characters beyond U+FFFF.
        PartialMatchSearchText.Build(search).Should().Be(expected);
    }

    [TestCase(",", "(\\,)")]
    [TestCase(", .", "(\\,\\.)")]
    public void PunctuationOnlySearchesStayValid(string search, string expected)
    {
        PartialMatchSearchText.Build(search).Should().Be(expected);
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
