using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    public class ModulePlacementIndexTests
    {
        private string _moduleRoot = string.Empty;
        private string _gitPath = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _moduleRoot = Path.Combine(
                Path.GetTempPath(), "swlor-placement-index-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_moduleRoot, "are"));
            Directory.CreateDirectory(Path.Combine(_moduleRoot, "utc"));
            Directory.CreateDirectory(Path.Combine(_moduleRoot, "git"));
            File.WriteAllText(Path.Combine(_moduleRoot, "are", "test_area.are.json"), "{}");
            _gitPath = Path.Combine(_moduleRoot, "git", "test_area.git.json");
            WriteGit("guard_a");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_moduleRoot))
                Directory.Delete(_moduleRoot, recursive: true);
        }

        [Test]
        public async Task FindAsync_IndexesKindAreaListIndexTagAndPosition()
        {
            var index = new ModuleWorkspace(_moduleRoot).PlacementIndex;

            var creature = (await index.FindAsync(ResourceType.Utc, "guard_a")).Should().ContainSingle().Subject;
            creature.AreaResRef.Should().Be("test_area");
            creature.InstanceIndex.Should().Be(0);
            creature.Tag.Should().Be("GUARD_TAG");
            creature.X.Should().BeApproximately(12.5f, 0.001f);
            creature.Y.Should().BeApproximately(8.25f, 0.001f);
            creature.Z.Should().BeApproximately(1.5f, 0.001f);

            var placeable = (await index.FindAsync(ResourceType.Utp, "crate_a")).Should().ContainSingle().Subject;
            placeable.InstanceIndex.Should().Be(0, "indices are within each kind's GIT list");
            placeable.X.Should().BeApproximately(3f, 0.001f);
            placeable.Y.Should().BeApproximately(4f, 0.001f);
            placeable.Z.Should().BeApproximately(5f, 0.001f);
        }

        [Test]
        public async Task Invalidate_RebuildsAgainstChangedGit()
        {
            var index = new ModuleWorkspace(_moduleRoot).PlacementIndex;
            (await index.FindAsync(ResourceType.Utc, "guard_a")).Should().ContainSingle();

            WriteGit("guard_b");
            index.Invalidate();

            (await index.FindAsync(ResourceType.Utc, "guard_a")).Should().BeEmpty();
            (await index.FindAsync(ResourceType.Utc, "guard_b")).Should().ContainSingle();
        }

        [Test]
        public async Task FindAsync_DecodesWindows1252Git()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var json = File.ReadAllText(_gitPath).Replace("GUARD_TAG", "GUARD_CAFÉ");
            File.WriteAllBytes(_gitPath, Encoding.GetEncoding(1252).GetBytes(json));
            var index = new ModuleWorkspace(_moduleRoot).PlacementIndex;

            var placement = (await index.FindAsync(ResourceType.Utc, "guard_a"))
                .Should().ContainSingle().Subject;

            placement.Tag.Should().Be("GUARD_CAFÉ");
        }

        [Test]
        public async Task FindAsync_DecodesUnmarkedUtf8Git()
        {
            var expectedTag = "GUARD_\u6771\u4EAC";
            var json = File.ReadAllText(_gitPath).Replace("GUARD_TAG", expectedTag);
            File.WriteAllText(
                _gitPath,
                json,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            var index = new ModuleWorkspace(_moduleRoot).PlacementIndex;

            var placement = (await index.FindAsync(ResourceType.Utc, "guard_a"))
                .Should().ContainSingle().Subject;

            placement.Tag.Should().Be(expectedTag);
        }

        [Test]
        public async Task FindAsync_DecodesUtf8AndWindows1252TokensInTheSameGit()
        {
            var expectedTag = "GUARD_\u6771\u4EAC";
            var json = File.ReadAllText(_gitPath)
                .Replace("GUARD_TAG", expectedTag)
                .Replace("CRATE_TAG", "CRATE_X");
            var bytes = Encoding.UTF8.GetBytes(json);
            var legacyMarker = Encoding.ASCII.GetBytes("CRATE_X");
            var markerOffset = bytes.AsSpan().IndexOf(legacyMarker);
            markerOffset.Should().BeGreaterThanOrEqualTo(0);
            bytes[markerOffset + legacyMarker.Length - 1] = 0x97;
            File.WriteAllBytes(_gitPath, bytes);
            var index = new ModuleWorkspace(_moduleRoot).PlacementIndex;

            var placement = (await index.FindAsync(ResourceType.Utc, "guard_a"))
                .Should().ContainSingle().Subject;

            placement.Tag.Should().Be(
                expectedTag,
                "a legacy token elsewhere in the GIT must not force genuine UTF-8 tags through Windows-1252");
        }

        [Test]
        public async Task FindAsync_DecodesBomMarkedUtf8Git()
        {
            var expectedTag = "GUARD_\u6771\u4EAC";
            var json = File.ReadAllText(_gitPath).Replace("GUARD_TAG", expectedTag);
            File.WriteAllText(
                _gitPath,
                json,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            var index = new ModuleWorkspace(_moduleRoot).PlacementIndex;

            var placement = (await index.FindAsync(ResourceType.Utc, "guard_a"))
                .Should().ContainSingle().Subject;

            placement.Tag.Should().Be(expectedTag);
        }

        [Test]
        public async Task IncompleteScan_NamesLogsAndRetriesFailedArea()
        {
            File.WriteAllText(Path.Combine(_moduleRoot, "are", "broken.are.json"), "{}");
            var brokenGit = Path.Combine(_moduleRoot, "git", "broken.git.json");
            File.WriteAllText(brokenGit, "{");
            var index = new ModuleWorkspace(_moduleRoot).PlacementIndex;
            string? loggedArea = null;
            Exception? loggedError = null;
            index.AreaReadFailed += (area, error) =>
            {
                loggedArea = area;
                loggedError = error;
            };

            var act = async () => await index.FindAsync(ResourceType.Utc, "guard_a");

            var failure = await act.Should().ThrowAsync<PlacementIndexIncompleteException>();
            failure.Which.AreaResRefs.Should().Equal("broken");
            loggedArea.Should().Be("broken");
            loggedError.Should().NotBeNull();

            File.WriteAllText(brokenGit, "{}");
            (await index.FindAsync(ResourceType.Utc, "guard_a")).Should().ContainSingle(
                "a failed build must be discarded so Refresh can retry it");
        }

        [Test]
        public async Task Invalidate_CancelsAnObsoleteBuildAndAllowsANewQuery()
        {
            File.WriteAllText(Path.Combine(_moduleRoot, "are", "broken.are.json"), "{}");
            var brokenGit = Path.Combine(_moduleRoot, "git", "broken.git.json");
            File.WriteAllText(brokenGit, "{");
            var index = new ModuleWorkspace(_moduleRoot).PlacementIndex;
            using var readFailed = new ManualResetEventSlim();
            using var releaseFailure = new ManualResetEventSlim();
            index.AreaReadFailed += (area, _) =>
            {
                if (area != "broken")
                    return;

                readFailed.Set();
                releaseFailure.Wait(TimeSpan.FromSeconds(5));
            };

            var obsoleteWarmup = index.WarmAsync();
            readFailed.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            index.Invalidate();
            releaseFailure.Set();

            var awaitObsoleteWarmup = async () => await obsoleteWarmup;
            await awaitObsoleteWarmup.Should().ThrowAsync<OperationCanceledException>();

            File.WriteAllText(brokenGit, "{}");
            (await index.FindAsync(ResourceType.Utc, "guard_a")).Should().ContainSingle(
                "the canceled generation must not poison the next placement query");
        }

        private void WriteGit(string creatureResRef)
        {
            File.WriteAllText(_gitPath, $$"""
            {
              "Creature List": {
                "type": "list",
                "value": [
                  {
                    "TemplateResRef": { "type": "resref", "value": "{{creatureResRef}}" },
                    "Tag": { "type": "cexostring", "value": "GUARD_TAG" },
                    "XPosition": { "type": "float", "value": 12.5 },
                    "YPosition": { "type": "float", "value": 8.25 },
                    "ZPosition": { "type": "float", "value": 1.5 }
                  }
                ]
              },
              "Placeable List": {
                "type": "list",
                "value": [
                  {
                    "TemplateResRef": { "type": "resref", "value": "crate_a" },
                    "Tag": { "type": "cexostring", "value": "CRATE_TAG" },
                    "X": { "type": "float", "value": 3.0 },
                    "Y": { "type": "float", "value": 4.0 },
                    "Z": { "type": "float", "value": 5.0 }
                  }
                ]
              }
            }
            """);
        }
    }
}
