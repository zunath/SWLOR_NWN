using System.Text;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Triggers;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Domain.Placeables;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Editors.Placeables;
using SWLOR.Toolset.Editors.Triggers;

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

        [Test]
        public void AnAreaWithMixedUtf8AndWindows1252TokensPreservesItsUtf8Tag()
        {
            const string expectedTag = "WP_D\u00C9TRITUS";
            WriteArea("anchor", waypointTag: expectedTag, displayName: "LEGACY_X", asUtf8: true);
            var path = Path.Combine(_root, "git", "anchor.git.json");
            var bytes = File.ReadAllBytes(path);
            var legacyMarker = Encoding.ASCII.GetBytes("LEGACY_X");
            var markerOffset = bytes.AsSpan().IndexOf(legacyMarker);
            markerOffset.Should().BeGreaterThanOrEqualTo(0);
            bytes[markerOffset + legacyMarker.Length - 1] = 0x97;
            File.WriteAllBytes(path, bytes);

            var index = new ModuleTagIndex(new ModuleWorkspace(_root));

            index.Tags.Should().Contain(
                expectedTag,
                "a legacy display-name token must not change the encoding of a genuine UTF-8 tag token");
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

        [Test]
        public void WaypointBehaviorFieldsDoNotOfferDoorOrStoreTags()
        {
            WriteArea(
                "typed",
                waypointTag: "WP_ONLY",
                displayName: "Typed",
                doorTag: "DOOR_ONLY",
                storeTag: "STORE_ONLY");
            var index = new ModuleTagIndex(new ModuleWorkspace(_root));
            index.Tags.Should().Contain(new[] { "WP_ONLY", "DOOR_ONLY", "STORE_ONLY" });

            var sources = new BehaviorValueSourceProvider(gameCode: null, tags: () => index);
            var options = sources.GetOptions(PlaceableValueSource.ObjectTags);

            options.Select(option => option.Value)
                .Should().BeEquivalentTo(index.TagsFor(ResourceType.Utw))
                .And.Contain("WP_ONLY")
                .And.NotContain(new[] { "DOOR_ONLY", "STORE_ONLY" });
        }

        [Test]
        public void TriggerTransitionValidatesAgainstTheSelectedDestinationType()
        {
            WriteArea(
                "typed",
                waypointTag: "WP_ONLY",
                displayName: "Typed",
                doorTag: "DOOR_ONLY",
                storeTag: "STORE_ONLY");
            var index = new ModuleTagIndex(new ModuleWorkspace(_root));
            var trigger = JsonGffDocument.Parse(
                Encoding.UTF8.GetBytes("""{ "__data_type": "UTT " }"""));
            var editor = new TriggerEditorViewModel(
                trigger.Root,
                "typed_transition",
                isInstance: false,
                (_, edit) =>
                {
                    edit();
                    return true;
                },
                resolveTag: (scope, tag) =>
                {
                    var type = scope switch
                    {
                        BehaviorTagScope.Waypoint => ResourceType.Utw,
                        BehaviorTagScope.Door => ResourceType.Utd,
                        _ => (ResourceType?)null
                    };
                    return type == null ? null : index.FindAreaDefiningTag(tag, type.Value);
                });
            editor.ChooseBehavior(TriggerBehaviorCatalog.Get(TriggerBehaviorCatalog.AreaTransitionId));

            var destination = editor.BehaviorRows.Single(row => row.Definition.Name == "LinkedTo");
            var destinationType = editor.BehaviorRows.Single(
                row => row.Definition.Name == "LinkedToFlags");
            destinationType.Choice = destinationType.Choices.Single(choice => choice.Value == 2);

            destination.Text = "STORE_ONLY";
            destination.IsStatusGood.Should().BeFalse();
            destination.Status.Should().Contain("no waypoint");

            destination.Text = "DOOR_ONLY";
            destination.IsStatusGood.Should().BeFalse(
                "a door-only tag cannot satisfy the selected Waypoint destination type");

            destinationType.Choice = destinationType.Choices.Single(choice => choice.Value == 1);
            destination.IsStatusGood.Should().BeTrue();
            destination.Status.Should().Contain("in typed");
        }

        [Test]
        public void AppearanceGridRebuildsWhenTheUsageIndexCompletes()
        {
            var twoDaRoot = Path.Combine(_root, "sw_2da");
            Directory.CreateDirectory(twoDaRoot);
            File.WriteAllText(
                Path.Combine(twoDaRoot, "placeables.2da"),
                "2DA V2.0\r\n\r\nLabel StrRef ModelName\r\n" +
                "0 Used **** plc_used\r\n" +
                "1 Available **** plc_unused\r\n");
            var catalog = new PlaceableModelCatalog(
                new TwoDaService(twoDaRoot),
                new TlkService(TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}")));
            _ = catalog.GetAll();

            Directory.CreateDirectory(Path.Combine(_root, "utp"));
            var document = JsonGffDocument.Parse(
                System.Text.Encoding.UTF8.GetBytes(
                    "{\n" +
                    "  \"__data_type\": \"UTP \",\n" +
                    "  \"Appearance\": { \"type\": \"dword\", \"value\": 0 }\n" +
                    "}\n"));
            File.WriteAllBytes(Path.Combine(_root, "utp", "used.utp.json"), document.ToBytes());

            var usage = PlaceableAppearanceUsageIndex.Empty;
            using var appearance = new AppearanceSectionViewModel(
                new EditorFieldContext(
                    document,
                    (_, mutation) =>
                    {
                        mutation();
                        return true;
                    }),
                catalog,
                thumbnails: null,
                () => usage,
                (_, mutation) =>
                {
                    mutation();
                    return true;
                });
            appearance.Gallery.MatchSummary.Should().Be("2 models");

            usage = PlaceableAppearanceUsageIndex.Build(new ModuleWorkspace(_root));
            appearance.RefreshUsage();

            appearance.Gallery.MatchSummary.Should().Be("1 model");
            appearance.Gallery.Tiles.Should().ContainSingle()
                .Which.Option.ModelResRef.Should().Be("plc_used");
        }

        [AvaloniaTest]
        public void InvalidatingAPlaceableIndexBuildQueuesARealReplacementScan()
        {
            var workspace = new SWLOR.Toolset.Workspace.WorkspaceContext(
                root => new ModuleWorkspace(root),
                new SWLOR.Toolset.Workspace.OutputLogService());
            workspace.Open(_root);

            using var firstStarted = new ManualResetEventSlim();
            using var releaseFirst = new ManualResetEventSlim();
            var scans = 0;
            var index = new SWLOR.Toolset.Workspace.PlaceableIndexService(
                workspace,
                _ =>
                {
                    if (Interlocked.Increment(ref scans) == 1)
                    {
                        firstStarted.Set();
                        releaseFirst.Wait(TimeSpan.FromSeconds(5));
                    }

                    return PlaceableAppearanceUsageIndex.Empty;
                });
            var updates = 0;
            index.Updated += () => updates++;

            index.EnsureBuilt();
            firstStarted.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
            index.Invalidate();
            releaseFirst.Set();

            // The production debounce is two seconds; keep pumping the UI dispatcher so both the
            // stale completion and the delayed EnsureBuilt callback can land.
            for (var attempt = 0; attempt < 400 && updates < 2; attempt++)
            {
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(10);
            }

            Volatile.Read(ref scans).Should().Be(2,
                "the stale completion must leave the built marker clear for its queued replacement");
            updates.Should().Be(2, "both background scans must finish publishing before the test exits");
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

        private void WriteArea(
            string resRef,
            string waypointTag,
            string displayName,
            bool asUtf8 = false,
            string? doorTag = null,
            string? storeTag = null)
        {
            File.WriteAllText(Path.Combine(_root, "are", $"{resRef}.are.json"), "{}");

            var doors = doorTag == null
                ? string.Empty
                : $$"""
                    {
                      "__struct_id": 8,
                      "Tag": { "type": "cexostring", "value": "{{doorTag}}" }
                    }
                    """;
            var stores = storeTag == null
                ? string.Empty
                : $$"""
                    {
                      "__struct_id": 11,
                      "Tag": { "type": "cexostring", "value": "{{storeTag}}" }
                    }
                    """;
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
                  "Door List": { "type": "list", "value": [{{doors}}] },
                  "StoreList": { "type": "list", "value": [{{stores}}] },
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
