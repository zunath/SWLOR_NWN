using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Factions;

namespace SWLOR.Toolset.Tests
{
    public sealed class FactionReferenceRewriterTests
    {
        private string _moduleRoot = null!;

        [SetUp]
        public void SetUp()
        {
            _moduleRoot = Path.Combine(
                Path.GetTempPath(), "swlor-faction-references-" + Guid.NewGuid().ToString("N"));
            Write("utc", "pilot.utc.json", """
                {
                  "__data_type": "UTC ",
                  "FactionID": { "type": "dword", "value": 7 },
                  "FactionID1": { "type": "dword", "value": 7 }
                }
                """);
            Write("utp", "terminal.utp.json", """
                {
                  "__data_type": "UTP ",
                  "Faction": { "type": "word", "value": 8 }
                }
                """);
            Write("git", "test.git.json", """
                {
                  "__data_type": "GIT ",
                  "Creature List": {
                    "type": "list",
                    "value": [
                      {
                        "__struct_id": 0,
                        "FactionID": { "type": "dword", "value": 7 }
                      }
                    ]
                  },
                  "Placeable List": {
                    "type": "list",
                    "value": [
                      {
                        "__struct_id": 0,
                        "Faction": { "type": "word", "value": 8 }
                      }
                    ]
                  }
                }
                """);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_moduleRoot))
                Directory.Delete(_moduleRoot, recursive: true);
        }

        [Test]
        public void ScanSeparatesBlueprintAndPlacedObjectUsage()
        {
            var usage = FactionReferenceRewriter.ScanUsage(_moduleRoot, factionCount: 10);

            usage[7].Should().Be(new FactionReferenceUsage(BlueprintCount: 1, PlacedObjectCount: 1));
            usage[8].Should().Be(new FactionReferenceUsage(BlueprintCount: 1, PlacedObjectCount: 1));
            usage[0].Total.Should().Be(0);
        }

        [Test]
        public void RewriteUpdatesOnlyRealFactionFieldsAcrossBlueprintsAndAreaInstances()
        {
            var rewrites = FactionReferenceRewriter.BuildRewrites(
                _moduleRoot,
                new Dictionary<int, int> { [7] = 1, [8] = 7 });

            rewrites.Should().HaveCount(3);
            rewrites.Single(rewrite => rewrite.Path.EndsWith("pilot.utc.json"))
                .ChangedReferences.Should().Be(1);
            rewrites.Single(rewrite => rewrite.Path.EndsWith("test.git.json"))
                .Should().Match<FactionReferenceRewrite>(rewrite =>
                    rewrite.ChangedReferences == 2 && rewrite.IsAreaInstanceFile);

            var utc = Parse(rewrites, "pilot.utc.json");
            utc.Get("FactionID").GetInteger().Should().Be(1);
            utc.Get("FactionID1").GetInteger().Should().Be(7,
                "similarly named fields are not object faction membership references");

            Parse(rewrites, "terminal.utp.json").Get("Faction").GetInteger().Should().Be(7);
            var git = Parse(rewrites, "test.git.json");
            git.Get("Creature List").Elements![0].Get("FactionID").GetInteger().Should().Be(1);
            git.Get("Placeable List").Elements![0].Get("Faction").GetInteger().Should().Be(7);
        }

        [Test]
        public void RewriteFingerprintDetectsAResourceChangedAfterTheScan()
        {
            var rewrite = FactionReferenceRewriter.BuildRewrites(
                    _moduleRoot,
                    new Dictionary<int, int> { [7] = 1 })
                .Single(candidate => candidate.Path.EndsWith("pilot.utc.json"));

            rewrite.SourceMatchesCurrentFile().Should().BeTrue();

            File.AppendAllText(rewrite.Path, " ");

            rewrite.SourceMatchesCurrentFile().Should().BeFalse();
        }

        private void Write(string directory, string fileName, string json)
        {
            var path = Path.Combine(_moduleRoot, directory, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        private static JsonGffStruct Parse(
            IEnumerable<FactionReferenceRewrite> rewrites,
            string fileName) =>
            JsonGffDocument.Parse(
                rewrites.Single(rewrite => rewrite.Path.EndsWith(fileName)).Bytes).Root;
    }
}
