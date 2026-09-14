using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Validation;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Validation notices a resource that will not parse.
    /// </summary>
    /// <remarks>
    /// Every other rule is a convention check that parses only the files it needs, so a malformed ARE,
    /// UTI, UTD, UTM, UTT or UTS was reported by nobody and a module with a file broken by an external
    /// edit or a bad merge could validate clean. This is the floor beneath the conventions.
    /// </remarks>
    [TestFixture]
    public class GffParseRuleTests
    {
        private string _root = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "swlor-gffparse-" + Guid.NewGuid().ToString("N"));
            foreach (var folder in new[]
                     {
                         "are", "utc", "uti", "utp", "utd", "utm", "utt", "uts", "utw",
                         "dlg", "git", "gic", "fac", "ifo", "itp", "jrl"
                     })
                Directory.CreateDirectory(Path.Combine(_root, folder));
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_root))
                    Directory.Delete(_root, recursive: true);
            }
            catch (IOException)
            {
            }
        }

        private ValidationContext Context() =>
            new(new ModuleWorkspace(_root));

        private void Write(string folder, string fileName, string content) =>
            File.WriteAllText(Path.Combine(_root, folder, fileName), content);

        private const string ValidUti =
            "{\"__data_type\":\"UTI \",\"TemplateResRef\":{\"type\":\"resref\",\"value\":\"probe\"}}";

        [Test]
        public void AWellFormedResourceIsNotReported()
        {
            Write("uti", "probe.uti.json", ValidUti);

            new GffParseRule().Validate(Context()).Should().BeEmpty();
        }

        [Test]
        public void AMalformedResourceIsReportedAsAnError()
        {
            Write("uti", "broken.uti.json", "{ this is not json");

            var issues = new GffParseRule().Validate(Context()).ToList();

            issues.Should().ContainSingle();
            issues[0].Severity.Should().Be(ValidationSeverity.Error, "the file cannot be opened, packed or edited");
            issues[0].ResRef.Should().Be("broken");
            issues[0].RuleId.Should().Be("GffParse");
        }

        [Test]
        public void AResourceWithMergeConflictMarkersIsReported()
        {
            Write("utp", "conflicted.utp.json", "<<<<<<< HEAD\n{}\n=======\n{}\n>>>>>>> other\n");

            new GffParseRule().Validate(Context()).Should().ContainSingle();
        }

        [Test]
        public void OneUnreadableFileDoesNotStopTheSweep()
        {
            // A rule that gave up on the first bad file would hide every later one, which is the
            // opposite of what a validation pass is for.
            Write("uti", "broken.uti.json", "{ nope");
            Write("utp", "alsobroken.utp.json", "{ nope");
            Write("utc", "fine.utc.json", "{\"__data_type\":\"UTC \"}");

            var issues = new GffParseRule().Validate(Context()).ToList();

            issues.Should().HaveCount(2);
            issues.Select(i => i.ResRef).Should().BeEquivalentTo(new[] { "broken", "alsobroken" });
        }

        [Test]
        public void EveryGffBackedTypeIsSwept()
        {
            // Areas, blueprints, dialogs, area companions, and module-level GFF resources.
            foreach (var (folder, extension) in new[]
                     {
                         ("are", "are"), ("uti", "uti"), ("utd", "utd"),
                         ("utm", "utm"), ("utt", "utt"), ("uts", "uts"),
                         ("dlg", "dlg"), ("git", "git"), ("gic", "gic"),
                         ("fac", "fac"), ("ifo", "ifo"), ("itp", "itp"), ("jrl", "jrl")
                     })
            {
                Write(folder, $"bad_{extension}.{extension}.json", "{ broken");
            }

            new GffParseRule().Validate(Context()).Should().HaveCount(13);
        }
    }
}
