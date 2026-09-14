using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Script;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Lexicon deep links. The Lexicon is MediaWiki and titles its pages with the exact engine
    /// function name, so a link needs no lookup table — but it does need to refuse anything that is
    /// not a plausible page title, or the action lands the builder on a "page does not exist" screen.
    /// </summary>
    public class ScriptLexiconTests
    {
        [TestCase("GetNearestCreature")]
        [TestCase("CREATURE_TYPE_PLAYER_CHAR")]
        [TestCase("d20")]
        [TestCase("_leading_underscore")]
        public void BuildsAUrlForAnIdentifier(string name)
        {
            var url = ScriptLexicon.UrlFor(name);

            url.Should().NotBeNull();
            url.Should().StartWith("https://www.nwnlexicon.com/");
            url.Should().EndWith(name);
        }

        /// <summary>
        /// The host is pinned deliberately. The apex (no <c>www</c>) answers 403 to some clients and
        /// produced SSL_ERROR_INTERNAL_ERROR_ALERT on a builder's machine while another opened it
        /// fine — a TLS failure against the apex, not a malformed link. Dropping back to the apex or
        /// re-adding index.php should fail here rather than in someone's browser.
        /// </summary>
        [Test]
        public void UsesTheWwwHostAndNoIndexPhp()
        {
            var url = ScriptLexicon.UrlFor("GetNearestCreature")!;

            url.Should().Be("https://www.nwnlexicon.com/GetNearestCreature");
            url.Should().NotContain("index.php");

            new Uri(url).Host.Should().Be("www.nwnlexicon.com");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("has space")]
        [TestCase("1leading_digit")]
        [TestCase("punct!")]
        [TestCase("a/b")]
        public void RefusesWhatCannotBeAPageTitle(string? name)
        {
            ScriptLexicon.UrlFor(name).Should().BeNull();
            ScriptLexicon.IsLinkableName(name).Should().BeFalse();
        }

        [Test]
        public void TrimsSurroundingWhitespace()
        {
            ScriptLexicon.UrlFor("  Random  ").Should().EndWith("/Random");
        }

        [Test]
        public void ProducesAnAbsoluteHttpsUri()
        {
            var url = ScriptLexicon.UrlFor("ApplyEffectToObject")!;

            Uri.TryCreate(url, UriKind.Absolute, out var uri).Should().BeTrue();
            uri!.Scheme.Should().Be(Uri.UriSchemeHttps, "the link service only opens http/https");
        }
    }
}
