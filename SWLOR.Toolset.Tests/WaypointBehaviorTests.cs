using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Domain.GameData.GameCode;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors.Waypoints;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public class WaypointBehaviorTests
    {
        private static readonly Dictionary<string, (string Field, string Value)[]> ExpectedWrites = new()
        {
            [WaypointBehaviorCatalog.CreatureSpawnPointId] = new[] { ("Appearance", "2") },
            [WaypointBehaviorCatalog.FishingPointId] = new[] { ("Appearance", "3") },
            [WaypointBehaviorCatalog.MapNoteId] =
                new[] { ("HasMapNote", "1"), ("MapNoteEnabled", "1"), ("Appearance", "1") },
            [WaypointBehaviorCatalog.StuckRescuePointId] =
                new[] { ("Tag", "STUCK_WAYPOINT"), ("Appearance", "1") },
            [WaypointBehaviorCatalog.TransitionDestinationId] = new[] { ("Appearance", "1") },
            [WaypointBehaviorCatalog.PlanetLandingId] = new[] { ("Appearance", "1") },
            [WaypointBehaviorCatalog.OrbitPointId] = new[] { ("Appearance", "1") },
            [WaypointBehaviorCatalog.TaxiStopId] = new[] { ("Appearance", "1") },
            [WaypointBehaviorCatalog.StarshipDockId] =
                new[] { ("Tag", "STARSHIP_DOCKPOINT"), ("Appearance", "1") },
            [WaypointBehaviorCatalog.PropertyEntranceId] =
                new[] { ("Tag", "PROPERTY_ENTRANCE"), ("Appearance", "1") },
            [WaypointBehaviorCatalog.DeathRespawnId] = new[] { ("Appearance", "1") },
            [WaypointBehaviorCatalog.RebuildId] = new[] { ("Appearance", "1") },
            [WaypointBehaviorCatalog.CustomId] = Array.Empty<(string, string)>()
        };

        private static readonly Dictionary<string, int> ExpectedPlacementCounts = new()
        {
            [WaypointBehaviorCatalog.CreatureSpawnPointId] = 1952,
            [WaypointBehaviorCatalog.FishingPointId] = 431,
            [WaypointBehaviorCatalog.MapNoteId] = 376,
            [WaypointBehaviorCatalog.StuckRescuePointId] = 300,
            [WaypointBehaviorCatalog.TransitionDestinationId] = 227,
            [WaypointBehaviorCatalog.PropertyEntranceId] = 43,
            [WaypointBehaviorCatalog.StarshipDockId] = 11,
            [WaypointBehaviorCatalog.PlanetLandingId] = 10,
            [WaypointBehaviorCatalog.OrbitPointId] = 10,
            [WaypointBehaviorCatalog.TaxiStopId] = 4,
            [WaypointBehaviorCatalog.DeathRespawnId] = 1,
            [WaypointBehaviorCatalog.RebuildId] = 2,
            [WaypointBehaviorCatalog.CustomId] = 548
        };

        private static string GameServerSourceRoot =>
            Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR.Game.Server");

        private static readonly Lazy<GameCodeIndex> GameCodeIndex =
            new(() => new GameCodeIndex(GameServerSourceRoot));

        private static readonly Lazy<ModuleWorkspace> ModuleWorkspace =
            new(() => new ModuleWorkspace(CorpusLocator.ModuleDirectory));

        private static readonly Lazy<WaypointBehaviorCatalog> BehaviorCatalog = new(() =>
            new WaypointBehaviorCatalog(
                GameCodeIndex.Value,
                ModuleWorkspace.Value.TagIndex.TransitionDestinationTags));

        private static GameCodeIndex GameCode => GameCodeIndex.Value;

        private static WaypointBehaviorCatalog Catalog() => BehaviorCatalog.Value;

        [Test]
        public void EveryWaypointBlueprintAndPlacementClassifiesWithoutThrowing()
        {
            var catalog = Catalog();
            var blueprints = CorpusBlueprints().ToList();
            var placements = CorpusPlacements().ToList();

            blueprints.Should().NotBeEmpty();
            placements.Should().NotBeEmpty();

            foreach (var waypoint in blueprints.Concat(placements))
            {
                catalog.Classify(waypoint).Should().NotBeNull();
            }
        }

        [Test]
        public void PlacementBehaviorCountsMatchTheModuleCorpus()
        {
            var counts = CorpusPlacements()
                .GroupBy(waypoint => Catalog().Classify(waypoint).Id)
                .ToDictionary(group => group.Key, group => group.Count());

            counts.Should().BeEquivalentTo(ExpectedPlacementCounts);
            counts.Values.Sum().Should().Be(3915);
        }

        [Test]
        public void RepresentativeWaypointTagsClassifyToTheirRuntimeBehavior()
        {
            var catalog = Catalog();

            catalog.Classify(Waypoint("CZ220_DROIDS")).Id
                .Should().Be(WaypointBehaviorCatalog.CreatureSpawnPointId);
            catalog.Classify(Waypoint("FP_VISC_CAVERN")).Id
                .Should().Be(WaypointBehaviorCatalog.FishingPointId);
            catalog.Classify(Waypoint("anything", hasMapNote: true)).Id
                .Should().Be(WaypointBehaviorCatalog.MapNoteId);
            catalog.Classify(Waypoint("STUCK_WAYPOINT")).Id
                .Should().Be(WaypointBehaviorCatalog.StuckRescuePointId);
            catalog.Classify(Waypoint("PROPERTY_ENTRANCE")).Id
                .Should().Be(WaypointBehaviorCatalog.PropertyEntranceId);
            catalog.Classify(Waypoint("VISCARA_LANDING")).Id
                .Should().Be(WaypointBehaviorCatalog.PlanetLandingId);
            catalog.Classify(Waypoint("DTH_DEFAULT_RESPAWN_POINT")).Id
                .Should().Be(WaypointBehaviorCatalog.DeathRespawnId);
            catalog.Classify(Waypoint("REBUILD_LANDING")).Id
                .Should().Be(WaypointBehaviorCatalog.RebuildId);
            catalog.Classify(Waypoint("WP_anchor_desert_est_2")).Id
                .Should().Be(WaypointBehaviorCatalog.TransitionDestinationId);
            catalog.Classify(Waypoint("coxxian_hq_exit")).Id
                .Should().Be(WaypointBehaviorCatalog.TransitionDestinationId,
                    "door LinkedTo values are transition destinations too");
            catalog.Classify(Waypoint("NOT_A_DECLARED_WAYPOINT_BEHAVIOR")).Id
                .Should().Be(WaypointBehaviorCatalog.CustomId);
        }

        [Test]
        public void EveryBehaviorWritesTheFieldsItsRuntimeReads()
        {
            var catalog = Catalog();
            catalog.All.Select(behavior => behavior.Id)
                .Should().BeEquivalentTo(ExpectedWrites.Keys);

            foreach (var behavior in catalog.All)
            {
                var store = new BehaviorValueStore(Waypoint("unassigned"));
                foreach (var value in behavior.Manages)
                    store.Apply(value);

                foreach (var (field, expected) in ExpectedWrites[behavior.Id])
                {
                    var actual = long.TryParse(expected, out _)
                        ? store.GetInteger(BehaviorFieldStorage.Field, field)?.ToString() ?? string.Empty
                        : store.GetString(BehaviorFieldStorage.Field, field);

                    actual.Should().Be(expected,
                        $"{behavior.DisplayName} must set {field} for the server to act on it");
                }
            }

            catalog.Get(WaypointBehaviorCatalog.CreatureSpawnPointId).Fields
                .Should().ContainSingle(field => field.Name == "Tag" && field.IsRequired);
            catalog.Get(WaypointBehaviorCatalog.FishingPointId).Fields
                .Should().ContainSingle(field => field.Name == "Tag" && field.IsRequired);
            catalog.Get(WaypointBehaviorCatalog.MapNoteId).Fields
                .Should().ContainSingle(field => field.Name == "MapNote" && field.IsRequired);
            catalog.Get(WaypointBehaviorCatalog.MapNoteId).Fields
                .Should().NotContain(field => field.Name == "MapNoteEnabled");
        }

        [Test]
        public void SwappingBehaviorClearsThePreviousBehaviorInOneUndoStep()
        {
            var catalog = Catalog();
            var waypoint = Waypoint("old_tag", hasMapNote: true);
            var store = new BehaviorValueStore(waypoint);
            store.SetString(
                BehaviorFieldStorage.Field,
                "TemplateResRef",
                GffFieldType.ResRef,
                "kept_blueprint");
            store.SetLocalizedText("MapNote", "Old note");
            store.SetInteger(
                BehaviorFieldStorage.Field,
                "MapNoteEnabled",
                GffFieldType.Byte,
                1);

            var editCount = 0;
            var editor = new WaypointEditorViewModel(
                waypoint,
                "kept_blueprint",
                isInstance: false,
                (_, edit) =>
                {
                    editCount++;
                    edit();
                    return true;
                },
                catalog,
                GameCode);

            editor.Behavior.Id.Should().Be(WaypointBehaviorCatalog.MapNoteId);
            editor.ChooseBehavior(catalog.Get(WaypointBehaviorCatalog.StuckRescuePointId));

            editCount.Should().Be(1);
            store.GetInteger(BehaviorFieldStorage.Field, "HasMapNote").Should().Be(0);
            store.GetLocalizedText("MapNote").Should().BeEmpty();
            store.GetInteger(BehaviorFieldStorage.Field, "MapNoteEnabled").Should().Be(0);
            store.GetString(BehaviorFieldStorage.Field, "Tag").Should().Be("STUCK_WAYPOINT");
            store.GetInteger(BehaviorFieldStorage.Field, "Appearance").Should().Be(1);
            store.GetString(BehaviorFieldStorage.Field, "TemplateResRef").Should().Be("kept_blueprint");
        }

        [Test]
        public void LeavingCustomClearsItsRawBehaviorFields()
        {
            var catalog = Catalog();
            var waypoint = Waypoint("legacy_custom");
            var store = new BehaviorValueStore(waypoint);
            store.SetInteger(BehaviorFieldStorage.Field, "HasMapNote", GffFieldType.Byte, 0);
            store.SetLocalizedText("MapNote", "Legacy text");
            store.SetInteger(BehaviorFieldStorage.Field, "MapNoteEnabled", GffFieldType.Byte, 1);

            var editor = new WaypointEditorViewModel(
                waypoint,
                "legacy_custom",
                isInstance: false,
                (_, edit) =>
                {
                    edit();
                    return true;
                },
                catalog,
                GameCode);

            editor.Behavior.Id.Should().Be(WaypointBehaviorCatalog.CustomId);
            editor.ChooseBehavior(catalog.Get(WaypointBehaviorCatalog.StarshipDockId));

            store.GetLocalizedText("MapNote").Should().BeEmpty();
            store.GetInteger(BehaviorFieldStorage.Field, "MapNoteEnabled").Should().Be(0);
            store.GetString(BehaviorFieldStorage.Field, "Tag").Should().Be("STARSHIP_DOCKPOINT");
        }

        [Test]
        public void VariablesExistOnlyForCustom()
        {
            var catalog = Catalog();

            catalog.All.Where(behavior => behavior.AllowsVariables)
                .Should().ContainSingle()
                .Which.Id.Should().Be(WaypointBehaviorCatalog.CustomId);

            var custom = new WaypointEditorViewModel(
                Waypoint("unrecognized"),
                "test",
                isInstance: false,
                (_, edit) =>
                {
                    edit();
                    return true;
                },
                catalog,
                GameCode);
            custom.ShowsVariablesTab.Should().BeTrue();
            custom.Variables.Should().NotBeNull();

            custom.ChooseBehavior(catalog.Get(WaypointBehaviorCatalog.StuckRescuePointId));
            custom.ShowsVariablesTab.Should().BeFalse();
            custom.Variables.Should().BeNull();
        }

        [Test]
        public void ChoosingASpawnTableWritesTheTagAndCompletesTheBehavior()
        {
            var catalog = Catalog();
            var waypoint = Waypoint("unrecognized");
            var store = new BehaviorValueStore(waypoint);
            var editor = new WaypointEditorViewModel(
                waypoint,
                "test",
                isInstance: false,
                (_, edit) =>
                {
                    edit();
                    return true;
                },
                catalog,
                GameCode);

            editor.ChooseBehavior(catalog.Get(WaypointBehaviorCatalog.CreatureSpawnPointId));
            editor.IsIncomplete.Should().BeTrue();

            var row = editor.BehaviorRows.Single(field => field.Definition.Name == "Tag");
            row.Choice = row.Choices.Single(choice => choice.StringValue == "CZ220_DROIDS");

            store.GetString(BehaviorFieldStorage.Field, "Tag").Should().Be("CZ220_DROIDS");
            editor.IsIncomplete.Should().BeFalse();
        }

        [Test]
        public void RawWaypointFieldsAreRowsInTheCustomBehavior()
        {
            WaypointEditorLayout.Custom.Select(field => field.Name).Should().BeEquivalentTo(
                "Appearance", "Tag", "HasMapNote", "MapNote", "MapNoteEnabled");
            Catalog().Custom.Fields.Select(field => field.Name).Should().Contain(
                "Appearance", "Tag", "HasMapNote", "MapNote", "MapNoteEnabled");

            WaypointEditorLayout.Basic.Select(field => field.Name).Should().BeEquivalentTo(
                "LocalizedName", "TemplateResRef", "PaletteID");
            WaypointEditorLayout.Basic.Single(field => field.Name == "TemplateResRef").Label
                .Should().Be("ResRef");
        }

        [Test]
        public void TransitionDestinationUsesARequiredFreeTextTag()
        {
            var field = Catalog().Get(WaypointBehaviorCatalog.TransitionDestinationId).Fields
                .Single(row => row.Name == "Tag");

            field.Kind.Should().Be(BehaviorFieldKind.Text);
            field.IsRequired.Should().BeTrue();
            field.Choices.Should().BeEmpty();
        }

        [Test]
        public void DeathAndRebuildExposeOnlyTheirOwnDestinations()
        {
            var deathChoices = Catalog().Get(WaypointBehaviorCatalog.DeathRespawnId).Fields
                .Single(row => row.Name == "Tag").Choices;
            var rebuildChoices = Catalog().Get(WaypointBehaviorCatalog.RebuildId).Fields
                .Single(row => row.Name == "Tag").Choices;

            deathChoices.Select(choice => choice.StringValue).Should().BeEquivalentTo(
                "DEATH_DEFAULT_RESPAWN_POINT",
                "DTH_DEFAULT_RESPAWN_POINT");
            rebuildChoices.Select(choice => choice.StringValue).Should().BeEquivalentTo(
                "REBUILD_LANDING",
                "REBUILD_TO_SPENDING_LANDING");
        }

        [Test]
        public void PlainChoiceTemplateWrapsLongWaypointLabels()
        {
            var appPath = Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset",
                "App.axaml");
            var app = File.ReadAllText(appPath);
            var template = app[
                app.IndexOf(
                    "<DataTemplate DataType=\"waypoints:WaypointRowViewModel\">",
                    StringComparison.Ordinal)..app.IndexOf(
                    "<DataTemplate DataType=\"editors:CheckFieldViewModel\">",
                    StringComparison.Ordinal)];

            template.Should().Contain("MinWidth=\"320\"");
            template.Should().Contain(
                "<TextBlock Text=\"{Binding Display}\" TextWrapping=\"Wrap\" MaxWidth=\"292\" />");
        }

        [Test]
        public void SpawnPickersShowFriendlyNamesAlongsideStoredIds()
        {
            var creatureField = Catalog().Get(WaypointBehaviorCatalog.CreatureSpawnPointId).Fields
                .Single(row => row.Name == "Tag");
            var fishingField = Catalog().Get(WaypointBehaviorCatalog.FishingPointId).Fields
                .Single(row => row.Name == "Tag");
            var creatureChoice = creatureField.Choices
                .Single(choice => choice.StringValue == "CZ220_DROIDS");
            var fishingChoice = fishingField.Choices
                .Single(choice => choice.StringValue == "FP_VISC_CAVERN");

            creatureField.IsSearchable.Should().BeTrue();
            fishingField.IsSearchable.Should().BeTrue();
            creatureChoice.Display.Should().Be("CZ-220 Droids (CZ220_DROIDS)");
            fishingChoice.Display.Should().Be("Viscara Cavern (FP_VISC_CAVERN)");

            creatureField.Choices
                .Concat(fishingField.Choices)
                .Select(choice => choice.StringValue)
                .Should().BeEquivalentTo(GameCode.SpawnTableIds,
                    "every declared spawn table belongs in one searchable waypoint list");
        }

        [Test]
        public void TaxiPickerShowsOnlyTheDestinationName()
        {
            var choice = Catalog().Get(WaypointBehaviorCatalog.TaxiStopId).Fields
                .Single(row => row.Name == "Tag").Choices
                .Single(option => option.StringValue == "TAXI_DANTOOINE_GARRISON");

            choice.Display.Should().Be("Dantooine Republic Garrison");
            choice.Display.Should().NotContain("credits");
        }

        [Test]
        public void MapNoteIsEnabledWhenPreparedForSave()
        {
            var waypoint = Waypoint("map_note", hasMapNote: true);
            var store = new BehaviorValueStore(waypoint);
            var editor = new WaypointEditorViewModel(
                waypoint,
                "test",
                isInstance: false,
                (_, edit) =>
                {
                    edit();
                    return true;
                },
                Catalog(),
                GameCode);

            editor.NeedsSaveNormalization.Should().BeTrue();
            editor.PrepareForSave().Should().BeTrue();

            store.GetInteger(BehaviorFieldStorage.Field, "MapNoteEnabled").Should().Be(1);
            editor.NeedsSaveNormalization.Should().BeFalse();
        }

        [Test]
        public void StarshipDockPlanetComesFromItsContainingArea()
        {
            Catalog().Get(WaypointBehaviorCatalog.StarshipDockId).Fields
                .Should().ContainSingle(field =>
                    field.Kind == BehaviorFieldKind.Statement &&
                    field.Label == "Planet" &&
                    field.Note == "Determined by the containing area");
        }

        private static JsonGffStruct Waypoint(string tag, bool hasMapNote = false)
        {
            var document = JsonGffDocument.Parse(Encoding.UTF8.GetBytes(
                $$"""
                {
                  "__data_type": "UTW ",
                  "Tag": { "type": "cexostring", "value": "{{tag}}" },
                  "HasMapNote": { "type": "byte", "value": {{(hasMapNote ? 1 : 0)}} }
                }
                """));
            return document.Root;
        }

        private static IEnumerable<JsonGffStruct> CorpusBlueprints()
        {
            var directory = Path.Combine(CorpusLocator.ModuleDirectory, "utw");
            foreach (var path in Directory.EnumerateFiles(directory, "*.utw.json"))
                yield return JsonGffDocument.Load(path).Root;
        }

        private static IEnumerable<JsonGffStruct> CorpusPlacements()
        {
            var directory = Path.Combine(CorpusLocator.ModuleDirectory, "git");
            foreach (var path in Directory.EnumerateFiles(directory, "*.git.json"))
            {
                var git = GitDocument.Load(path);
                foreach (var waypoint in git.Waypoints)
                    yield return waypoint;
            }
        }
    }
}
