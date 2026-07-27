using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The module-wide indexes the editors validate against, and what happens when they go stale or
    /// cannot be built at all.
    /// </summary>
    [TestFixture]
    public class ModuleIndexFreshnessTests
    {
        private string _root = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), $"swlor_index_{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path.Combine(_root, "are"));
            Directory.CreateDirectory(Path.Combine(_root, "utc"));
            Directory.CreateDirectory(Path.Combine(_root, "git"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        /// <summary>
        /// nwn_gff writes NWN text as raw Windows-1252, so an em-dash is the single byte 0x97 - not
        /// valid UTF-8, and the UTF-8 reader threw on it. The enclosing catch turned that into
        /// silence: every waypoint and door tag in that area vanished from tag resolution, and a
        /// transition pointing at one of them read as pointing at nothing.
        /// </summary>
        [Test]
        public void AnAreaWithWindows1252TextStillContributesItsTags()
        {
            WriteArea("coolship", waypointTag: "WP_STUCK", displayName: "Stuck — coolship");

            var workspace = new ModuleWorkspace(_root);
            var index = new ModuleTagIndex(workspace);

            index.Tags.Should().Contain("WP_STUCK");
            index.FindAreaDefiningTag("WP_STUCK").Should().Be("coolship");
        }

        [Test]
        public void AnAreaWithUtf8TextStillContributesItsTags()
        {
            WriteArea("anchor", waypointTag: "WP_DETRITUS", displayName: "Détritus", asUtf8: true);

            var index = new ModuleTagIndex(new ModuleWorkspace(_root));

            index.Tags.Should().Contain("WP_DETRITUS");
        }

        /// <summary>
        /// Every dictionary here used to be built once and held for the life of the workspace, which
        /// is right for a module nobody is editing and wrong for the one open in the toolset.
        /// </summary>
        [Test]
        public void InvalidatingTheTagIndexPicksUpANewWaypoint()
        {
            WriteArea("first", waypointTag: "WP_ONE", displayName: "First");

            var index = new ModuleTagIndex(new ModuleWorkspace(_root));
            index.Tags.Should().Contain("WP_ONE").And.NotContain("WP_TWO");

            WriteArea("second", waypointTag: "WP_TWO", displayName: "Second");

            index.Tags.Should().NotContain("WP_TWO", "nothing has told it the module changed");

            index.Invalidate();

            index.Tags.Should().Contain("WP_TWO");
        }

        /// <summary>
        /// A Lazy whose factory throws caches the exception and rethrows it to every later caller.
        /// The placeable editor caught the failure on its background thread and then rebuilt its
        /// grid on the UI thread, where the same cached exception came back unhandled - so the
        /// intended empty grid was a crash on opening a placeable instead.
        /// </summary>
        [Test]
        public void AnUnreadableModelTableDegradesToAnEmptyGridRatherThanThrowing()
        {
            // A 2DA directory with no placeables.2da in it, which is what an unresolved repository
            // layout looks like from here.
            var empty = Path.Combine(_root, "no2da");
            Directory.CreateDirectory(empty);
            var catalog = new PlaceableModelCatalog(new TwoDaService(empty), new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));

            catalog.GetAll().Should().BeEmpty();
            catalog.BuildFailure.Should().NotBeNull();

            // The second call is the one that used to throw: same cached Lazy, different thread.
            var search = () => catalog.Search("crate").ToList();
            search.Should().NotThrow();
            catalog.TryGet(1, out _).Should().BeFalse();
        }

        private void WriteArea(string resRef, string waypointTag, string displayName, bool asUtf8 = false)
        {
            File.WriteAllText(Path.Combine(_root, "are", $"{resRef}.are.json"), "{}");

            var json =
                $$"""
                {
                  "__data_type": "GIT ",
                  "WaypointList": {
                    "type": "list",
                    "value": [
                      {
                        "__struct_id": 5,
                        "Tag": { "type": "cexostring", "value": "{{waypointTag}}" },
                        "LocalizedName": {
                          "type": "cexolocstring",
                          "value": { "0": "{{displayName}}" }
                        }
                      }
                    ]
                  },
                  "Door List": { "type": "list", "value": [] },
                  "TriggerList": { "type": "list", "value": [] }
                }
                """;

            var bytes = asUtf8
                ? Encoding.UTF8.GetBytes(json)
                : Windows1252().GetBytes(json);

            File.WriteAllBytes(Path.Combine(_root, "git", $"{resRef}.git.json"), bytes);
        }

        private static Encoding Windows1252()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252);
        }
    }
}
