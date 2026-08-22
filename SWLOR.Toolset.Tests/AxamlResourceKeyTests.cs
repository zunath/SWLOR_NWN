using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Every resource key the XAML asks for is defined somewhere in the app.
    /// </summary>
    /// <remarks>
    /// A misspelled or invented key is not an error at build time and not an error at runtime - the
    /// binding simply resolves to null. That is quiet for a colour and loud in odd ways for anything
    /// structural: a Background of null is not hit-testable in Avalonia, so a control themed with a
    /// nonexistent hover brush stopped receiving the pointer while hovered, which cleared the hover
    /// state, which restored the background. The cursor flickered between hand and arrow over
    /// everything except the text, whose glyphs hit-test on their own.
    /// </remarks>
    [TestFixture]
    public class AxamlResourceKeyTests
    {
        /// <summary>
        /// Keys supplied by Avalonia's own themes rather than this app. Empty today - every key the
        /// XAML references is declared in-app - and here so a genuine built-in can be recorded
        /// rather than silently widening the check.
        /// </summary>
        private static readonly HashSet<string> ProvidedByAvalonia = new();

        private static readonly Regex Reference =
            new(@"\{(?:Dynamic|Static)Resource\s+([A-Za-z0-9_.]+)\s*\}", RegexOptions.Compiled);

        private static readonly Regex Declaration =
            new(@"x:Key=""([A-Za-z0-9_.]+)""", RegexOptions.Compiled);

        [Test]
        public void EveryReferencedResourceKeyIsDeclared()
        {
            var toolset = Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR.Toolset");
            Directory.Exists(toolset).Should().BeTrue("the app's XAML is the subject of this test");

            var files = Directory.EnumerateFiles(toolset, "*.axaml", SearchOption.AllDirectories).ToList();
            files.Should().NotBeEmpty();

            var declared = new HashSet<string>(StringComparer.Ordinal);
            var referenced = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                foreach (Match match in Declaration.Matches(text))
                    declared.Add(match.Groups[1].Value);

                foreach (Match match in Reference.Matches(text))
                    referenced.TryAdd(match.Groups[1].Value, Path.GetFileName(file));
            }

            var missing = referenced
                .Where(pair => !declared.Contains(pair.Key) && !ProvidedByAvalonia.Contains(pair.Key))
                .Select(pair => $"{pair.Key} (first seen in {pair.Value})")
                .OrderBy(entry => entry)
                .ToList();

            missing.Should().BeEmpty(
                "a resource key that resolves to nothing fails silently - add the key, fix the " +
                "spelling, or record it in ProvidedByAvalonia");
        }
    }
}
