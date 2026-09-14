using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Categories;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// A category sidecar that cannot be parsed is preserved, not replaced.
    /// </summary>
    /// <remarks>
    /// A malformed <c>categories.json</c> - unresolved merge-conflict markers, a truncated write - used
    /// to load as an empty but <i>writable</i> catalog. Startup then asked for the Area section, seeded
    /// it, and saved, overwriting the builder's arrangement with a fresh one. The only warning was a log
    /// line, and the work was gone. Opening the toolset must never destroy category data.
    /// </remarks>
    [TestFixture]
    public class CategoryCatalogRecoveryTests
    {
        private string _directory = string.Empty;
        private string _file = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "swlor-categories-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _file = Path.Combine(_directory, "categories.json");
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (Directory.Exists(_directory))
                    Directory.Delete(_directory, recursive: true);
            }
            catch (IOException)
            {
                // A leftover temp folder is not worth failing a test over.
            }
        }

        [Test]
        public void AMalformedSidecarLoadsReadOnly()
        {
            File.WriteAllText(_file, "{ \"version\": 1, \"sections\": { truncated");

            var catalog = CategoryCatalog.Load(_file, out var warning);

            catalog.IsReadOnly.Should().BeTrue("an unreadable file must not be silently replaced");
            warning.Should().NotBeNullOrEmpty();
            catalog.ReadOnlyReason.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void ASidecarWithMergeConflictMarkersLoadsReadOnly()
        {
            File.WriteAllText(_file, "<<<<<<< HEAD\n{ \"version\": 1 }\n=======\n{ \"version\": 1 }\n>>>>>>> other\n");

            CategoryCatalog.Load(_file, out _).IsReadOnly.Should().BeTrue();
        }

        [Test]
        public void SavingAMalformedSidecarIsRefused()
        {
            var original = "{ \"version\": 1, \"sections\": { truncated";
            File.WriteAllText(_file, original);
            var catalog = CategoryCatalog.Load(_file, out _);

            var act = () => catalog.Save();

            act.Should().Throw<InvalidOperationException>();
            File.ReadAllText(_file).Should().Be(original, "the builder's file must survive untouched");
        }

        [Test]
        public void TheRefusalSaysWhichProblemItWas()
        {
            // Both read-only paths used to report "written by a newer Toolset", which sends someone
            // whose file is merely truncated looking for a version problem that does not exist.
            File.WriteAllText(_file, "{ not json");
            var malformed = CategoryCatalog.Load(_file, out _);

            malformed.ReadOnlyReason.Should().Contain("Could not read categories");
            malformed.ReadOnlyReason.Should().NotContain("newer Toolset");
        }

        [Test]
        public void ASidecarFromANewerToolsetStillReportsThat()
        {
            File.WriteAllText(_file, "{ \"version\": 999, \"sections\": {} }");

            var catalog = CategoryCatalog.Load(_file, out _);

            catalog.IsReadOnly.Should().BeTrue();
            catalog.ReadOnlyReason.Should().Contain("newer Toolset");
        }

        [Test]
        public void ASidecarFromANewerToolsetStillShowsTheFoldersThisBuildUnderstands()
        {
            // The warning says the categories are shown but will not be saved. Returning before reading
            // any section broke that promise: an older build opening a newer file hid the whole saved
            // arrangement behind freshly seeded empties.
            File.WriteAllText(_file, """
                {
                  "version": 999,
                  "sections": {
                    "utc": { "folders": [ { "name": "Troopers" } ], "pinned": [] }
                  }
                }
                """);

            var catalog = CategoryCatalog.Load(_file, out _);

            catalog.IsReadOnly.Should().BeTrue("it still must not be written back");
            var section = catalog.Section(SWLOR.Toolset.Domain.Workspace.ResourceType.Utc);
            section.Should().NotBeNull();
            section!.Folders.Should().ContainSingle().Which.Name.Should().Be("Troopers");
        }

        [Test]
        public void AnAbsentSidecarIsStillTheNormalFirstRun()
        {
            var catalog = CategoryCatalog.Load(Path.Combine(_directory, "nothing-here.json"), out var warning);

            catalog.IsReadOnly.Should().BeFalse("a first run has to be able to write its first sidecar");
            warning.Should().BeNull();
        }

        /// <summary>
        /// The JSON token <c>null</c> deserializes without throwing, so it never reaches the
        /// JsonException handler above. Loading it as a writable empty catalog let the normal
        /// section-seeding path save fresh sections straight over whatever the truncated or
        /// corrupted write actually left behind, destroying the only evidence of it.
        /// </summary>
        [Test]
        public void ASidecarThatDeserializesToNullLoadsReadOnly()
        {
            File.WriteAllText(_file, "null");

            var catalog = CategoryCatalog.Load(_file, out var warning);

            catalog.IsReadOnly.Should().BeTrue("a null document must not be treated as an empty, writable catalog");
            warning.Should().NotBeNullOrEmpty();
            catalog.ReadOnlyReason.Should().NotBeNullOrEmpty();

            var act = () => catalog.Save();
            act.Should().Throw<InvalidOperationException>();
            File.ReadAllText(_file).Should().Be("null", "the builder's file must survive untouched");
        }
    }
}
