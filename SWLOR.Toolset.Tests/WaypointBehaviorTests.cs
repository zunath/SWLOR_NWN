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
            [WaypointBehaviorCatalog.StuckRescuePointId] = 306,
            [WaypointBehaviorCatalog.TransitionDestinationId] = 229,
            [WaypointBehaviorCatalog.PropertyEntranceId] = 43,
            [WaypointBehaviorCatalog.StarshipDockId] = 11,
            [WaypointBehaviorCatalog.PlanetLandingId] = 10,
            [WaypointBehaviorCatalog.OrbitPointId] = 10,
            [WaypointBehaviorCatalog.TaxiStopId] = 4,
            [WaypointBehaviorCatalog.DeathRespawnId] = 1,
            [WaypointBehaviorCatalog.RebuildId] = 2,
            [WaypointBehaviorCatalog.CustomId] = 550
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
            counts.Values.Sum().Should().Be(3925);
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
            var resRef = WaypointEditorLayout.Basic.Single(field => field.Name == "TemplateResRef");
            resRef.Label.Should().Be("ResRef");
            resRef.IsReadOnly.Should().BeFalse(
                "rename-on-save keeps the internal identity, file name, and placements together");
            resRef.IsRequired.Should().BeTrue();
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
        public void SingletonDestinationRejectsAnotherPlacementButExemptsTheCurrentInstance()
        {
            var catalog = Catalog();
            var taxiTag = catalog.Get(WaypointBehaviorCatalog.TaxiStopId).Fields
                .Single(field => field.Name == "Tag")
                .Choices.First().StringValue!;

            var conflicting = new WaypointEditorViewModel(
                Waypoint(taxiTag),
                "area",
                isInstance: true,
                (_, edit) =>
                {
                    edit();
                    return true;
                },
                catalog,
                singletonTagInUse: _ => true);
            conflicting.IsIncomplete.Should().BeTrue();
            conflicting.PrepareForSave().Should().BeFalse();

            var currentInstanceOnly = new WaypointEditorViewModel(
                Waypoint(taxiTag),
                "area",
                isInstance: true,
                (_, edit) =>
                {
                    edit();
                    return true;
                },
                catalog,
                singletonTagInUse: _ => false);
            currentInstanceOnly.IsIncomplete.Should().BeFalse();
            currentInstanceOnly.PrepareForSave().Should().BeTrue();
        }

        [Test]
        public void TransitionDestinationIdentityPersistsBeforeAnInboundLinkExists()
        {
            var catalog = new WaypointBehaviorCatalog(null, Array.Empty<string>());
            var waypoint = Waypoint("new_destination");
            var editor = new WaypointEditorViewModel(
                waypoint,
                "new_destination",
                isInstance: false,
                (_, edit) =>
                {
                    edit();
                    return true;
                },
                catalog);

            editor.ChooseBehavior(catalog.Get(WaypointBehaviorCatalog.TransitionDestinationId));
            editor.BehaviorRows.Single(row => row.Definition.Name == "Tag").Text =
                "destination_authored_before_link";

            new VarTable(waypoint)
                .GetString(WaypointBehaviorCatalog.PersistedBehaviorLocal)
                .Should().Be(WaypointBehaviorCatalog.TransitionDestinationId);
            catalog.Classify(waypoint).Id.Should().Be(
                WaypointBehaviorCatalog.TransitionDestinationId,
                "the free-text destination must remain a transition until its inbound link is authored");

            var reopened = new WaypointEditorViewModel(
                waypoint,
                "new_destination",
                isInstance: false,
                (_, edit) =>
                {
                    edit();
                    return true;
                },
                catalog);
            reopened.Behavior.Id.Should().Be(WaypointBehaviorCatalog.TransitionDestinationId);
            reopened.RefreshCatalog(new WaypointBehaviorCatalog(null, Array.Empty<string>()));
            reopened.Behavior.Id.Should().Be(
                WaypointBehaviorCatalog.TransitionDestinationId,
                "an explicitly authored destination remains valid while it waits for an inbound link");
        }

        [Test]
        public void RefreshingCatalogReclassifiesAnInboundOnlyTransitionWithoutPersistingIt()
        {
            const string tag = "destination_with_removed_link";
            var waypoint = Waypoint(tag);
            var originalCatalog = new WaypointBehaviorCatalog(null, new[] { tag });
            var editor = new WaypointEditorViewModel(
                waypoint,
                tag,
                isInstance: false,
                (_, edit) =>
                {
                    edit();
                    return true;
                },
                originalCatalog);
            editor.Behavior.Id.Should().Be(WaypointBehaviorCatalog.TransitionDestinationId);

            editor.RefreshCatalog(new WaypointBehaviorCatalog(null, Array.Empty<string>()));

            editor.Behavior.Id.Should().Be(WaypointBehaviorCatalog.CustomId);
            editor.NeedsSaveNormalization.Should().BeFalse(
                "catalog refresh is classification, not a user-authored behavior change");
            new VarTable(waypoint).GetString(WaypointBehaviorCatalog.PersistedBehaviorLocal)
                .Should().BeNull("an obsolete inbound-only classification must not become durable metadata");
        }

        [Test]
        public void WaypointNamesAndTagsHaveNoArtificialCapsBelowTheGffFormat()
        {
            var catalog = new WaypointBehaviorCatalog(null, null);
            var waypoint = Waypoint("unrecognized");
            var store = new BehaviorValueStore(waypoint);
            var editor = new WaypointEditorViewModel(
                waypoint,
                "long_fields",
                isInstance: false,
                (_, edit) =>
                {
                    edit();
                    return true;
                },
                catalog);
            var longName = new string('N', 96);
            var longTag = new string('T', 96);

            editor.BasicRows.Single(row => row.Definition.Name == "LocalizedName").Text = longName;
            editor.BehaviorRows.Single(row => row.Definition.Name == "Tag").Text = longTag;

            store.GetLocalizedText("LocalizedName").Should().Be(longName);
            store.GetString(BehaviorFieldStorage.Field, "Tag").Should().Be(longTag);
            editor.BasicRows.Single(row => row.Definition.Name == "LocalizedName")
                .MaxLength.Should().Be(0);
            editor.BehaviorRows.Single(row => row.Definition.Name == "Tag")
                .MaxLength.Should().Be(0);
            catalog.Get(WaypointBehaviorCatalog.TransitionDestinationId).Fields
                .Single(row => row.Name == "Tag").MaxLength.Should().Be(0);
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
            // The row markup is shared by every behavior editor now, so the wrapping rule lives in
            // one place rather than in a waypoint-only template.
            var view = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset",
                "Editors",
                "Behaviors",
                "BehaviorRowView.axaml"));

            view.Should().Contain(
                "<TextBlock Text=\"{Binding Display}\" TextWrapping=\"Wrap\" MaxWidth=\"420\" />");
        }

        [Test]
        public void TheRowGivesItsWidthToTheValueRatherThanTheLabel()
        {
            // Every pixel the label column takes comes out of the value, and the value is the part
            // that has to hold a search list, a picture grid, or a tag.
            foreach (var (path, file) in SharedRowMarkup())
            {
                file.Should().NotContain("ColumnDefinitions=\"220,*\"",
                    $"{path} still reserves the old label column");
                file.Should().NotContain("ColumnDefinitions=\"180,*\"",
                    $"{path} still reserves the old label column");
            }

            // Anything drawn underneath a row follows the row: indented under the label column when
            // there is room for one, and full width when there is not. A fixed grid cannot do the
            // second, which is how a key-item list ends up hanging off the side of a narrow pane.
            foreach (var view in new[] { "DoorEditorView.axaml", "SoundEditorView.axaml" })
            {
                File.ReadAllText(Path.Combine(
                        CorpusLocator.RepositoryRoot, "SWLOR.Toolset", "Editors", "Views", view))
                    .Should().Contain("behaviors:LabeledFieldPanel", $"{view} follows the shared row");
            }

            foreach (var view in new[]
                     {
                         "WaypointEditorView.axaml", "TriggerDocumentView.axaml",
                         "DoorEditorView.axaml", "SoundEditorView.axaml"
                     })
            {
                var markup = File.ReadAllText(Path.Combine(
                    CorpusLocator.RepositoryRoot, "SWLOR.Toolset", "Editors", "Views", view));
                markup.Should().Contain("ColumnDefinitions=\"210,*\"",
                    $"{view}'s behavior rail lists short names and does not need more");
            }
        }

        [Test]
        public void APictureSetThatFitsThePageIsNotHiddenBehindAButton()
        {
            var row = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot,
                "SWLOR.Toolset", "Editors", "Behaviors", "BehaviorRowView.axaml"));

            // The inline grid is the whole point of a picture picker: names are what it replaces.
            row.Should().Contain("IsVisible=\"{Binding IsInlineGallery}\"");
            row.Should().NotContain("Content=\"Choose&#x2026;\"",
                "a picture set on the page needs no button, and one behind the preview is opened by "
                + "clicking the preview");

            // The large sets keep their popup, opened by the picture itself.
            row.Should().Contain("IsVisible=\"{Binding IsPopupGallery}\"");
            row.Should().Contain("Command=\"{Binding OpenGalleryCommand}\"");
        }

        [Test]
        public void EveryBehaviorEditorDrawsItsRowsFromTheSharedControl()
        {
            // One row control, not four. The trigger, waypoint, door, and sound editors each used to
            // carry their own copy, which is how three different label-column widths shipped.
            var app = File.ReadAllText(Path.Combine(
                CorpusLocator.RepositoryRoot, "SWLOR.Toolset", "App.axaml"));
            app.Should().Contain("<DataTemplate DataType=\"behaviors:BehaviorRowViewModel\">");
            app.Should().NotContain("DataType=\"waypoints:WaypointRowViewModel\"");

            foreach (var view in new[] { "DoorEditorView.axaml", "SoundEditorView.axaml" })
            {
                var markup = File.ReadAllText(Path.Combine(
                    CorpusLocator.RepositoryRoot, "SWLOR.Toolset", "Editors", "Views", view));
                markup.Should().Contain("<behaviors:BehaviorRowView />", $"{view} reuses the shared row");
            }
        }

        [Test]
        public void NoBehaviorEditorShowsAnAdvancedTab()
        {
            foreach (var view in new[]
                     {
                         "WaypointDocumentView.axaml", "TriggerDocumentView.axaml",
                         "DoorEditorView.axaml", "SoundEditorView.axaml"
                     })
            {
                var markup = File.ReadAllText(Path.Combine(
                    CorpusLocator.RepositoryRoot, "SWLOR.Toolset", "Editors", "Views", view));
                markup.Should().NotContain(
                    "Header=\"Advanced\"",
                    $"{view} folds its raw fields into Basic and Custom");
            }
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
        public void AppearancesOfferMarkerModelsRatherThanNames()
        {
            var sw2Da = Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR_Haks", "sw_2da");
            if (!Directory.Exists(sw2Da))
                Assert.Ignore("The haks submodule is not initialised in this checkout.");

            var appearances = WaypointAppearanceCatalog.Read(
                new Domain.GameData.Lookups.WaypointAppearanceService(
                    new Domain.GameData.TwoDa.TwoDaService(sw2Da),
                    new Domain.GameData.Tlk.TlkService(
                        Domain.GameData.Tlk.TlkJsonFile.Parse("{\"language\":0,\"entries\":[]}"))));

            appearances.Should().NotBeEmpty();
            appearances.Should().OnlyContain(row => !string.IsNullOrWhiteSpace(row.ModelResRef),
                "the picker draws each marker, so every row must carry waypoint.2da's RESREF");
        }

        [Test]
        public async Task AnAppearanceRowIsPickedFromPicturesOnThePage()
        {
            var appearances = Enumerable.Range(1, 76)
                .Select(id => new BehaviorChoice(id, $"marker {id}", modelResRef: $"gi_waypoint{id:00}"))
                .ToList();
            using var row = new WaypointRowViewModel(
                WaypointEditorLayout.Custom.Single(field => field.Name == "Appearance"),
                new BehaviorValueStore(Waypoint("wp_test")),
                (_, edit) =>
                {
                    edit();
                    return true;
                },
                appearances);

            // A model is artwork as much as a texture is, and 76 markers fit the page. Before this,
            // the row had no pictures to offer and degraded to the searchable list of colour names.
            row.IsGallery.Should().BeTrue();
            row.IsInlineGallery.Should().BeTrue();
            row.IsSearchableChoice.Should().BeFalse();
            await row.OpenGalleryCommand.ExecuteAsync(null);
            row.GalleryChoices.Should().NotBeEmpty();
            row.GalleryChoices[0].Detail.Should().Be("gi_waypoint01");
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
        public void PreparingForSaveEnforcesEveryManagedWaypointValue()
        {
            var waypoint = Waypoint(WaypointBehaviorCatalog.StarshipDockTag);
            var store = new BehaviorValueStore(waypoint);
            store.SetInteger(BehaviorFieldStorage.Field, "Appearance", GffFieldType.Byte, 4);
            var editor = new WaypointEditorViewModel(
                waypoint,
                "starship_dockpoi",
                isInstance: false,
                (_, edit) =>
                {
                    edit();
                    return true;
                },
                Catalog());

            editor.Behavior.Id.Should().Be(WaypointBehaviorCatalog.StarshipDockId);
            editor.NeedsSaveNormalization.Should().BeTrue();

            editor.PrepareForSave().Should().BeTrue();

            store.GetInteger(BehaviorFieldStorage.Field, "Appearance").Should().Be(1);
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

        /// <summary>Every markup file that declares a field row's label column.</summary>
        private static IEnumerable<(string Path, string Markup)> SharedRowMarkup()
        {
            var files = new[]
            {
                Path.Combine("SWLOR.Toolset", "Editors", "Behaviors", "BehaviorRowView.axaml"),
                Path.Combine("SWLOR.Toolset", "App.axaml")
            };

            foreach (var file in files)
                yield return (file, File.ReadAllText(Path.Combine(CorpusLocator.RepositoryRoot, file)));
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
