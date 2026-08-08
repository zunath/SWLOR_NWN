using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Guards the words the toolset shows a builder.
    /// </summary>
    /// <remarks>
    /// "ResRef" is the term, spelled that way everywhere. The alternatives that showed up in the
    /// same window - "Blueprint ResRef", "resref", "Resref" - all name the same thing, and a builder
    /// reading three spellings of one concept has to work out for themselves that it is one concept.
    /// </remarks>
    [TestFixture]
    public class TerminologyTests
    {
        private static readonly string[] SourceRoots =
        {
            "SWLOR.Toolset",
            "SWLOR.Toolset.Domain"
        };

        /// <summary>
        /// Matches a quoted literal - the strings that reach a builder - rather than identifiers.
        /// Field names, file extensions, and GFF labels legitimately use other casings.
        /// </summary>
        private static readonly Regex QuotedLiteral = new("\"([^\"\\r\\n]*)\"", RegexOptions.Compiled);

        private static readonly Regex InterpolationHole = new("\\{[^}]*\\}", RegexOptions.Compiled);

        private static readonly Regex WrongCasing =
            new("(?<![A-Za-z])(resref|Resref|RESREF|ResRefs?ID)(?![A-Za-z])", RegexOptions.Compiled);

        [Test]
        public void NoUserFacingStringSpellsResRefAnyOtherWay()
        {
            var offenders = new List<string>();

            foreach (var file in EnumerateSourceFiles())
            {
                var lineNumber = 0;
                foreach (var line in File.ReadLines(file))
                {
                    lineNumber++;

                    var trimmed = line.TrimStart();
                    // Comments are prose about the format, where "resref" as a lowercase noun is
                    // normal English. Only what ships in a string is a label.
                    if (trimmed.StartsWith("//", StringComparison.Ordinal) ||
                        trimmed.StartsWith("///", StringComparison.Ordinal) ||
                        trimmed.StartsWith("<!--", StringComparison.Ordinal) ||
                        trimmed.StartsWith("*", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    foreach (Match literal in QuotedLiteral.Matches(line))
                    {
                        var value = literal.Groups[1].Value;

                        // A path fragment or a lowercase placeholder is not a label.
                        if (value.Contains('.', StringComparison.Ordinal) ||
                            value.Contains('<', StringComparison.Ordinal) ||
                            value.Contains('/', StringComparison.Ordinal))
                        {
                            continue;
                        }

                        // A single token is an identifier - a GFF field name, a wire-format type
                        // token, a style class. Only a phrase is something a builder reads.
                        if (!value.Contains(' ', StringComparison.Ordinal))
                            continue;

                        // A style class or selector is markup, not prose: Classes="resref" names the
                        // monospace treatment, and renaming it would say nothing to a builder.
                        var lead = line[..literal.Index];
                        if (lead.EndsWith("Classes=", StringComparison.Ordinal) ||
                            lead.EndsWith("Selector=", StringComparison.Ordinal) ||
                            lead.EndsWith("x:Name=", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        // An interpolation hole holds a C# expression, not prose.
                        var prose = InterpolationHole.Replace(value, " ");
                        if (WrongCasing.IsMatch(prose))
                            offenders.Add($"{Path.GetFileName(file)}:{lineNumber}  \"{value}\"");
                    }
                }
            }

            offenders.Should().BeEmpty(
                "\"ResRef\" is the term the toolset uses; these spell it another way");
        }

        [Test]
        public void EveryBlueprintSchemaLabelsItsIdentityFieldResRef()
        {
            var schemas = new[]
            {
                Domain.Editors.Schemas.UtcSchema.Build(),
                Domain.Editors.Schemas.UtdSchema.Build(),
                Domain.Editors.Schemas.UtiSchema.Build(),
                Domain.Editors.Schemas.UtmSchema.Build(),
                Domain.Editors.Schemas.UtpSchema.Build(),
                Domain.Editors.Schemas.UtsSchema.Build(),
                Domain.Editors.Schemas.UttSchema.Build(),
                Domain.Editors.Schemas.UtwSchema.Build(),
                Domain.Editors.Schemas.AreSchema.Build()
            };

            foreach (var schema in schemas)
            {
                var identity = schema.Groups
                    .SelectMany(group => group.Fields)
                    .Single(field => field.FieldName is "TemplateResRef" or "ResRef");

                identity.Label.Should().Be(
                    "ResRef",
                    $"{schema.ResourceType} names the same concept as every other blueprint");
            }
        }

        [Test]
        public void EveryBehaviorEditorLabelsItsIdentityFieldResRef()
        {
            Domain.Editors.Triggers.TriggerEditorLayout.Basic
                .Single(field => field.Name == "TemplateResRef").Label.Should().Be("ResRef");
            Domain.Editors.Waypoints.WaypointEditorLayout.Basic
                .Single(field => field.Name == "TemplateResRef").Label.Should().Be("ResRef");
            Domain.Editors.Doors.DoorEditorLayout.Basic
                .Single(field => field.Name == "TemplateResRef").Label.Should().Be("ResRef");
            Domain.Editors.Sounds.SoundEditorLayout.Basic
                .Single(field => field.Name == "TemplateResRef").Label.Should().Be("ResRef");
        }

        private static IEnumerable<string> EnumerateSourceFiles()
        {
            foreach (var root in SourceRoots)
            {
                var directory = Path.Combine(CorpusLocator.RepositoryRoot, root);
                if (!Directory.Exists(directory))
                    continue;

                foreach (var file in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
                {
                    if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                            StringComparison.Ordinal) ||
                        file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                        file.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return file;
                    }
                }
            }
        }
    }
}
