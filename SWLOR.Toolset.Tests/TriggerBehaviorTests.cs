using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Common;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Triggers;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Triggers;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The trigger behavior catalog and the value store behind it.
    /// </summary>
    /// <remarks>
    /// Two properties carry the design. Every trigger already in the module must classify as
    /// something — a behavior list that cannot name the content it was derived from is worthless —
    /// and swapping behavior must leave none of the previous one's scripts or locals behind, because
    /// a stale OnEnter still fires in game.
    /// </remarks>
    [TestFixture]
    public class TriggerBehaviorTests
    {
        /// <summary>
        /// What each behavior writes, spelled out here rather than read back out of the catalog, so
        /// the catalog cannot drift away from what the server listens for without this failing. Every
        /// handler named below is a real <c>ScriptName</c> constant in SWLOR.Game.Server.
        /// </summary>
        private static readonly Dictionary<string, (string Field, string Value)[]> ExpectedWrites = new()
        {
            [TriggerBehaviorCatalog.AreaTransitionId] = new[] { ("Type", "1"), ("Cursor", "1") },
            [TriggerBehaviorCatalog.NoSpawnZoneId] = new[] { ("TemplateResRef", "anti_spawn_trigg"), ("Type", "0") },
            [TriggerBehaviorCatalog.ExplorationNoteId] = new[] { ("Type", "0"), ("ScriptOnEnter", "explore_trigger") },
            [TriggerBehaviorCatalog.RestZoneId] = new[] { ("Type", "0"), ("ScriptOnEnter", "rest_trg_enter"), ("ScriptOnExit", "rest_trg_exit") },
            [TriggerBehaviorCatalog.QuestId] = new[] { ("Type", "0"), ("ScriptOnEnter", "quest_trigger") },
            [TriggerBehaviorCatalog.TrapId] = new[] { ("Type", "2"), ("TrapFlag", "1") },
            [TriggerBehaviorCatalog.CustomId] = Array.Empty<(string, string)>()
        };

        [Test]
        public void EveryBehaviorWritesTheEngineFieldsItsRuntimeReads()
        {
            TriggerBehaviorCatalog.All.Select(behavior => behavior.Id)
                .Should().BeEquivalentTo(ExpectedWrites.Keys,
                    "a behavior with no stated writes has never been checked against the server");

            foreach (var behavior in TriggerBehaviorCatalog.All)
            {
                var store = new BehaviorValueStore(NewTrigger());
                foreach (var value in behavior.Manages)
                    store.Apply(value, isInstance: true);

                foreach (var (field, expected) in ExpectedWrites[behavior.Id])
                {
                    // The expected value's own shape says how to read the field back: a GFF field
                    // refuses to hand an int out as a string, and vice versa.
                    var actual = long.TryParse(expected, out _)
                        ? store.GetInteger(BehaviorFieldStorage.Field, field)?.ToString() ?? string.Empty
                        : store.GetString(BehaviorFieldStorage.Field, field);

                    actual.Should().Be(expected,
                        $"{behavior.DisplayName} must set {field} for the server to act on it");
                }
            }
        }

        [Test]
        public void ABehaviorSurvivesBeingSavedAndReopened()
        {
            // The point of the whole thing: choosing a behavior has to reach the file, not just the
            // screen, and reopening the file has to recognise what was chosen.
            var path = CopyToTemp("badge_trigger");
            var document = new TriggerDocumentViewModel(
                path, "badge_trigger", null, new OutputLogService(), new StubPrompts());
            try
            {
                document.Editor.ChooseBehavior(
                    TriggerBehaviorCatalog.Get(TriggerBehaviorCatalog.ExplorationNoteId));
                document.Editor.BehaviorRows.Single(row => row.Definition.Name == "DISPLAY_TEXT")
                    .Text = "A crashed shuttle lies half-buried in the dune.";

                document.IsDirty.Should().BeTrue();
                document.TrySaveAsync().GetAwaiter().GetResult().Should().BeTrue();
                document.IsDirty.Should().BeFalse();
            }
            finally
            {
                // Closing releases the session's edit-scope guard; leaving it open would leak into
                // whatever test ran next.
                document.OnClose();
            }

            var saved = new BehaviorValueStore(JsonGffDocument.Load(path).Root);
            saved.GetString(BehaviorFieldStorage.Field, "ScriptOnEnter").Should().Be("explore_trigger");
            saved.GetFloat(BehaviorFieldStorage.Field, "HighlightHeight").Should().BeApproximately(3.0, 1e-4);
            saved.GetString(BehaviorFieldStorage.Local, "DISPLAY_TEXT")
                .Should().Be("A crashed shuttle lies half-buried in the dune.");
            TriggerBehaviorCatalog.Classify(JsonGffDocument.Load(path).Root).Id
                .Should().Be(TriggerBehaviorCatalog.ExplorationNoteId);
        }

        [Test]
        public void RevertPutsTheStoredBehaviorBackOnScreen()
        {
            var path = CopyToTemp("badge_trigger");
            var document = new TriggerDocumentViewModel(
                path, "badge_trigger", null, new OutputLogService(), new StubPrompts());
            try
            {
                var stored = document.Editor.Behavior.Id;

                document.Editor.ChooseBehavior(TriggerBehaviorCatalog.Get(TriggerBehaviorCatalog.TrapId));
                document.Editor.Behavior.Id.Should().Be(TriggerBehaviorCatalog.TrapId);

                document.RevertCommand.Execute(null);

                // Reverting restores the fields, so the editor has to follow them rather than keep
                // showing a behavior the trigger no longer has.
                document.Editor.Behavior.Id.Should().Be(stored);
                document.IsDirty.Should().BeFalse();
            }
            finally
            {
                document.OnClose();
            }
        }

        [Test]
        public void ANoSpawnZoneIsNotStampedOntoABlueprintsOwnResRef()
        {
            // The runtime matches no-spawn volumes by resref, so a placement gets it written; a
            // blueprint's resref is its file name and must not be rewritten underneath it.
            var behavior = TriggerBehaviorCatalog.Get(TriggerBehaviorCatalog.NoSpawnZoneId);
            var resRef = behavior.Manages.Single(value => value.Name == "TemplateResRef");

            var blueprint = new BehaviorValueStore(NewTrigger());
            blueprint.Apply(resRef, isInstance: false);
            blueprint.GetString(BehaviorFieldStorage.Field, "TemplateResRef").Should().BeEmpty();

            var placement = new BehaviorValueStore(NewTrigger());
            placement.Apply(resRef, isInstance: true);
            placement.GetString(BehaviorFieldStorage.Field, "TemplateResRef").Should().Be("anti_spawn_trigg");

            placement.Clear(behavior.Manages, behavior.Fields);
            placement.GetString(BehaviorFieldStorage.Field, "TemplateResRef")
                .Should().Be("anti_spawn_trigg", "swapping behavior must not orphan a placement");
        }

        [Test]
        public void LoadScreensOfferPicturesRatherThanNames()
        {
            var sw2Da = Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR_Haks", "sw_2da");
            if (!Directory.Exists(sw2Da))
                Assert.Ignore("The haks submodule is not initialised in this checkout.");

            var screens = LoadScreenCatalog.Read(new Domain.GameData.TwoDa.TwoDaService(sw2Da));

            screens.Should().NotBeEmpty();
            screens.Should().Contain(screen => !string.IsNullOrWhiteSpace(screen.ImageResRef),
                "the picker shows each screen's artwork, so the rows must carry a BMPResRef");
            screens.Should().OnlyContain(screen => !screen.Display.Contains('_'),
                "2DA labels are identifiers; the picker shows names");
        }

        [Test]
        public void LoadScreensRejectSentinelAndScriptedRows()
        {
            File.WriteAllText(
                Path.Combine(_tempDirectory, "loadscreens.2da"),
                "2DA V2.0\r\n\r\n" +
                "Label BMPResRef\r\n" +
                "0 Random ****\r\n" +
                "1 SWLOR_17_Tatooine load_tat\r\n" +
                "2 DELETED load_deleted\r\n" +
                "3 UNUSED_3 load_unused\r\n" +
                "4 Padding load_padding\r\n" +
                "5 UserDefined ****\r\n" +
                "6 SWLOR_MissingArt ****\r\n" +
                "7 SWLOR_BlankArt\r\n" +
                "8 SWLOR_DeletedArt DELETED\r\n");

            var screens = LoadScreenCatalog.Read(new Domain.GameData.TwoDa.TwoDaService(_tempDirectory));

            screens.Select(screen => screen.Value).Should().Equal(0, 1);
            screens[0].IsAny.Should().BeTrue();
            screens[1].Display.Should().Be("17 Tatooine");
        }

        [Test]
        public void TrapTypesRequireCompleteRuntimeMetadata()
        {
            var path = Path.Combine(_tempDirectory, "traps.2da");
            File.WriteAllText(
                path,
                "2DA V2.0\r\n\r\n" +
                "Label TrapScript SetDC DetectDCMod DisarmDCMod TrapName ResRef IconResRef\r\n" +
                "0 MinorSpike trap_script 5 10 22 6846 trap_item trap_icon\r\n" +
                "1 MissingScript **** 5 10 22 6847 trap_item trap_icon\r\n" +
                "2 MissingItem trap_script 5 10 22 6848 **** trap_icon\r\n");

            var traps = TrapTypeCatalog.Read(new Domain.GameData.TwoDa.TwoDaService(_tempDirectory));

            traps.Select(trap => trap.Value).Should().Equal(0);

            File.WriteAllText(
                path,
                "2DA V2.0\r\n\r\n" +
                "Label TrapScript SetDC DetectDCMod DisarmDCMod TrapName ResRef\r\n" +
                "0 MinorSpike trap_script 5 10 22 6846 trap_item\r\n");

            TrapTypeCatalog.Read(new Domain.GameData.TwoDa.TwoDaService(_tempDirectory))
                .Should().BeEmpty("a missing required column makes the table unsafe to offer");
        }

        private string _tempDirectory = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "swlor-trigger-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, recursive: true);
        }

        private string CopyToTemp(string resRef)
        {
            var source = Path.Combine(CorpusLocator.ModuleDirectory, "utt", resRef + ".utt.json");
            if (!File.Exists(source))
                Assert.Ignore($"{resRef}.utt.json is not present in this checkout.");

            var destination = Path.Combine(_tempDirectory, resRef + ".utt.json");
            File.Copy(source, destination);
            return destination;
        }

        /// <summary>No test here reaches a prompt; a call means the test wandered off its path.</summary>
        private sealed class StubPrompts : IEditorPromptService
        {
            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                throw new InvalidOperationException("Nothing changed the file underneath the editor.");

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                throw new InvalidOperationException("No close prompt is expected.");

            public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel) =>
                throw new InvalidOperationException("The editor destroys nothing.");

            public Task<string?> PromptForTextAsync(string headline, string message, string initialValue, string confirmLabel) =>
                throw new InvalidOperationException("The editor asks for no text.");
        }

        [Test]
        public void OnlyCustomOffersRawVariables()
        {
            var withVariables = TriggerBehaviorCatalog.All.Where(b => b.AllowsVariables).ToList();

            withVariables.Should().ContainSingle()
                .Which.Id.Should().Be(TriggerBehaviorCatalog.CustomId);
        }

        [Test]
        public void EveryBehaviorThatUsesALocalExposesItAsAField()
        {
            // The point of the rule above: if a behavior needed a local it did not surface as a named
            // field, that local would be unreachable, since only Custom shows the raw grid.
            foreach (var behavior in TriggerBehaviorCatalog.All.Where(b => !b.AllowsVariables))
            {
                var surfaced = behavior.Fields
                    .Where(field => field.Storage == BehaviorFieldStorage.Local)
                    .Select(field => field.Name)
                    .ToHashSet(StringComparer.Ordinal);

                foreach (var local in behavior.OwnedLocals)
                    surfaced.Should().Contain(local,
                        $"{behavior.DisplayName} uses the local {local}, which nothing else can reach");
            }
        }

        [Test]
        public void EveryTriggerInTheModuleClassifiesAsSomething()
        {
            var triggers = CorpusTriggers().ToList();
            triggers.Should().NotBeEmpty("the corpus is what the catalog was derived from");

            foreach (var trigger in triggers)
                TriggerBehaviorCatalog.Classify(trigger).Should().NotBeNull();
        }

        [Test]
        public void TheModulesOwnTriggersLandOnTheExpectedBehaviors()
        {
            var byBehavior = CorpusTriggers()
                .GroupBy(trigger => TriggerBehaviorCatalog.Classify(trigger).Id)
                .ToDictionary(group => group.Key, group => group.Count());

            // Counts measured over Module\git: the catalog exists because these patterns dominate.
            byBehavior.GetValueOrDefault(TriggerBehaviorCatalog.AreaTransitionId)
                .Should().BeGreaterThan(250);
            byBehavior.GetValueOrDefault(TriggerBehaviorCatalog.NoSpawnZoneId)
                .Should().BeGreaterThan(100);
            byBehavior.GetValueOrDefault(TriggerBehaviorCatalog.ExplorationNoteId)
                .Should().BeGreaterThan(30);
            byBehavior.GetValueOrDefault(TriggerBehaviorCatalog.RestZoneId)
                .Should().BeGreaterThan(10);
        }

        [Test]
        public void ChoosingABehaviorWritesEverythingItManages()
        {
            var trigger = NewTrigger();
            var store = new BehaviorValueStore(trigger);
            var exploration = TriggerBehaviorCatalog.Get(TriggerBehaviorCatalog.ExplorationNoteId);

            foreach (var value in exploration.Manages)
                store.Apply(value);

            store.GetString(BehaviorFieldStorage.Field, "ScriptOnEnter").Should().Be("explore_trigger");
            store.GetFloat(BehaviorFieldStorage.Field, "HighlightHeight").Should().BeApproximately(3.0, 1e-4);
            exploration.Manages.Should().OnlyContain(value => store.Matches(value));
        }

        [Test]
        public void SwappingBehaviorLeavesNothingOfThePreviousOneBehind()
        {
            var trigger = NewTrigger();
            var store = new BehaviorValueStore(trigger);
            var exploration = TriggerBehaviorCatalog.Get(TriggerBehaviorCatalog.ExplorationNoteId);
            var transition = TriggerBehaviorCatalog.Get(TriggerBehaviorCatalog.AreaTransitionId);

            foreach (var value in exploration.Manages)
                store.Apply(value);
            store.SetString(BehaviorFieldStorage.Local, "DISPLAY_TEXT", GffFieldType.CExoString, "a note");

            store.Clear(exploration.Manages, exploration.Fields);
            foreach (var value in transition.Manages)
                store.Apply(value);

            // A stale OnEnter would still fire in game, and a stale local would still be read.
            store.GetString(BehaviorFieldStorage.Field, "ScriptOnEnter").Should().BeEmpty();
            store.Locals.GetString("DISPLAY_TEXT").Should().BeNull();
            store.GetInteger(BehaviorFieldStorage.Field, "Type").Should().Be(1);
            store.GetInteger(BehaviorFieldStorage.Field, "Cursor").Should().Be(1);
        }

        [Test]
        public void AMangledManagedValueLosesItsTick()
        {
            var trigger = NewTrigger();
            var store = new BehaviorValueStore(trigger);
            var restZone = TriggerBehaviorCatalog.Get(TriggerBehaviorCatalog.RestZoneId);

            foreach (var value in restZone.Manages)
                store.Apply(value);
            restZone.Manages.Should().OnlyContain(value => store.Matches(value));

            store.SetString(BehaviorFieldStorage.Field, "ScriptOnExit", GffFieldType.ResRef, "something_else");

            store.Matches(restZone.Manages.Single(value => value.Name == "ScriptOnExit"))
                .Should().BeFalse();
        }

        [Test]
        public void ClassifyingReadsTheHandlerBeforeTheEngineType()
        {
            // An exploration note is Type 0 like any generic trigger; only its handler identifies it.
            var trigger = NewTrigger();
            new BehaviorValueStore(trigger)
                .SetString(BehaviorFieldStorage.Field, "ScriptOnEnter", GffFieldType.ResRef, "explore_trigger");

            TriggerBehaviorCatalog.Classify(trigger).Id
                .Should().Be(TriggerBehaviorCatalog.ExplorationNoteId);
        }

        [Test]
        public void CustomBelongsToNoGroup()
        {
            // It rendered under HAZARD because the list builder let an ungrouped entry inherit the
            // previous heading. Custom is not a hazard.
            TriggerBehaviorCatalog.Custom.Group.Should().BeNull();
        }

        [Test]
        public void ATriggerThatMatchesNothingIsCustom()
        {
            // There is no longer a separate "None": a volume with nothing set is a Custom trigger
            // that has not been given any scripts yet, which is the same thing with one less concept.
            TriggerBehaviorCatalog.All.Should().NotContain(behavior => behavior.DisplayName == "None");
            TriggerBehaviorCatalog.Classify(NewTrigger()).Id.Should().Be(TriggerBehaviorCatalog.CustomId);
        }

        [Test]
        public void TheIdentityRowsCapWhatTheEngineCaps()
        {
            var resRef = TriggerEditorLayout.Basic.Single(row => row.Name == "TemplateResRef");
            var tag = TriggerEditorLayout.Basic.Single(row => row.Name == "Tag");

            resRef.MaxLength.Should().Be(NwnResRef.MaxLength);
            resRef.Label.Should().Be("ResRef");
            resRef.IsReadOnly.Should().BeFalse(
                "rename-on-save keeps the internal identity, file name, and placements together");
            resRef.IsRequired.Should().BeTrue();
            TriggerBehaviorCatalog.Get(TriggerBehaviorCatalog.NoSpawnZoneId).Manages
                .Single(value => value.Name == "TemplateResRef").Label.Should().Be("ResRef");

            // A tag is a CExoString with no engine maximum; this is the base toolset's editor limit,
            // and it has to stay clear of the longest tag the module actually ships.
            tag.MaxLength.Should().Be(32);
            LongestTriggerTagLength().Should().BeLessThan(tag.MaxLength);
        }

        [Test]
        public void NeitherCursorNorGeometryIsAskedFor()
        {
            var rows = TriggerEditorLayout.Basic
                .Concat(TriggerBehaviorCatalog.All.SelectMany(behavior => behavior.Fields))
                .ToList();

            rows.Should().NotContain(row => row.Name == "Cursor",
                "the cursor follows from what the trigger is; the transition behavior sets it");
            rows.Should().NotContain(row => row.Label.Contains("Geometry", StringComparison.OrdinalIgnoreCase),
                "dimensions are drawn per placement in the area editor");
        }

        [Test]
        public void TriggerTypeIsOnlyOfferedUnderCustom()
        {
            // Every other behavior writes Type itself, so offering it beside them would let a builder
            // set something the behavior contradicts - and the behavior wins on the next swap.
            TriggerBehaviorCatalog.Custom.Fields.Should().ContainSingle(row => row.Name == "Type");
            TriggerEditorLayout.Basic.Should().NotContain(row => row.Name == "Type");

            foreach (var behavior in TriggerBehaviorCatalog.All)
            {
                var writesType = behavior.Manages.Any(value => value.Name == "Type");
                if (behavior.Id == TriggerBehaviorCatalog.CustomId)
                {
                    writesType.Should().BeFalse("Custom manages nothing, so the raw row is the only way to set it");
                }
                else
                {
                    writesType.Should().BeTrue($"{behavior.DisplayName} hides the raw row, so it must set Type itself");
                    behavior.Fields.Should().NotContain(row => row.Name == "Type");
                }
            }
        }

        [Test]
        public void CategoryAndFactionArePickedRatherThanTyped()
        {
            var category = TriggerEditorLayout.Basic.Single(row => row.Name == "PaletteID");
            var faction = TriggerEditorLayout.Basic.Single(row => row.Name == "Faction");

            category.Kind.Should().Be(BehaviorFieldKind.Choice);
            category.ChoicesKey.Should().Be(TriggerChoiceKeys.PaletteCategories);
            faction.Kind.Should().Be(BehaviorFieldKind.Choice);
            faction.ChoicesKey.Should().Be(TriggerChoiceKeys.Factions);
        }

        [Test]
        public void ThePaletteYieldsNamedCategoriesRatherThanNumbers()
        {
            var path = Path.Combine(CorpusLocator.ModuleDirectory, "itp", "triggerpalcus.itp.json");
            if (!File.Exists(path))
                Assert.Ignore("triggerpalcus.itp.json is not present in this checkout.");

            var categories = PaletteCategoryReader.Read(
                SWLOR.Toolset.Domain.Documents.ItpDocument.Load(path));

            categories.Should().NotBeEmpty();
            categories.Select(category => category.Value).Should().OnlyHaveUniqueItems();
            categories.Should().OnlyContain(category => category.Display.Length > 0);
        }

        private static int LongestTriggerTagLength()
        {
            var longest = 0;
            foreach (var trigger in CorpusTriggers())
                longest = Math.Max(longest, new BehaviorValueStore(trigger)
                    .GetString(BehaviorFieldStorage.Field, "Tag").Length);

            return longest;
        }

        [Test]
        public void CustomExposesEveryScriptSlotItsClassifierRecognises()
        {
            var names = TriggerBehaviorCatalog.Custom.Fields
                .Where(field => field.Kind == BehaviorFieldKind.Script)
                .Select(field => field.Name)
                .ToHashSet(StringComparer.Ordinal);

            names.Should().Contain(new[]
            {
                "ScriptOnEnter", "ScriptOnExit", "ScriptHeartbeat", "ScriptUserDefine",
                "OnClick", "OnDisarm", "OnTrapTriggered"
            });
        }

        [Test]
        public void TriggerIntegersOutsideTheirStorageRangeAreRejectedWithoutMutation()
        {
            var trigger = NewTrigger();
            var store = new BehaviorValueStore(trigger);

            var local = () => store.SetInteger(
                BehaviorFieldStorage.Local, "QUEST_STATE", GffFieldType.Int, 2_147_483_648L);
            var dword = () => store.SetInteger(
                BehaviorFieldStorage.Field, "LargeValue", GffFieldType.Dword, 4_294_967_296L);

            local.Should().Throw<ArgumentOutOfRangeException>();
            dword.Should().Throw<ArgumentOutOfRangeException>();
            store.Locals.GetInt("QUEST_STATE").Should().BeNull();
            trigger.TryGet("LargeValue", out _).Should().BeFalse();
        }

        [Test]
        public void ReloadReclassifiesBehaviorAfterDocumentStateChanges()
        {
            var trigger = NewTrigger();
            var editor = new TriggerEditorViewModel(
                trigger, "test_trigger", isInstance: false,
                (_, edit) =>
                {
                    edit();
                    return true;
                });
            editor.Behavior.Id.Should().Be(TriggerBehaviorCatalog.CustomId);

            new BehaviorValueStore(trigger).SetString(
                BehaviorFieldStorage.Field, "ScriptOnEnter", GffFieldType.ResRef, "explore_trigger");
            editor.ReloadFromDocument();

            editor.Behavior.Id.Should().Be(TriggerBehaviorCatalog.ExplorationNoteId);
            editor.HeaderName.Should().Be("Exploration Note");
            editor.BehaviorRows.Should().Contain(row => row.Label == "Message");
        }

        [Test]
        public void ChoosingBlankCustomBehaviorRemainsCustom()
        {
            var editor = new TriggerEditorViewModel(
                NewTrigger(), "test_trigger", isInstance: false,
                (_, edit) =>
                {
                    edit();
                    return true;
                });

            editor.ChooseBehavior(TriggerBehaviorCatalog.Custom);

            editor.Behavior.Id.Should().Be(TriggerBehaviorCatalog.CustomId);
            editor.ShowsVariablesTab.Should().BeTrue();
        }

        [Test]
        public void AChoiceRendersAsItsNameAlone()
        {
            // A combo box falls back to ToString with no item template, and the record default
            // printed the whole shape - which is what "Destination is a" was showing.
            new BehaviorChoice(2, "Waypoint").ToString().Should().Be("Waypoint");
        }

        [Test]
        public void TheLoadScreenRowTakesItsValuesFromGameData()
        {
            var loadScreen = TriggerBehaviorCatalog.Get(TriggerBehaviorCatalog.AreaTransitionId)
                .Fields.Single(field => field.Name == "LoadScreenID");

            loadScreen.Kind.Should().Be(BehaviorFieldKind.Choice);
            loadScreen.ChoicesKey.Should().Be(TriggerChoiceKeys.LoadScreens);
            loadScreen.Choices.Should().BeEmpty("the screens come from loadscreens.2da, not from this file");
        }

        /// <summary>A bare trigger struct, standing in for a freshly created blueprint.</summary>
        [Test]
        public void SwappingAwayFromABehaviorDoesNotAskAboutWhatItWroteItself()
        {
            var trigger = NewTrigger();
            var store = new BehaviorValueStore(trigger);
            var transition = TriggerBehaviorCatalog.Get(TriggerBehaviorCatalog.AreaTransitionId);
            var noSpawn = TriggerBehaviorCatalog.Get(TriggerBehaviorCatalog.NoSpawnZoneId);

            foreach (var value in transition.Manages)
                store.Apply(value, isInstance: true);

            // Choosing Area Transition writes Cursor; choosing something else takes it away again.
            // Asking the builder to approve that is asking them to approve undoing a change they
            // never made.
            BehaviorSwitchLosses.Describe(
                    store, transition.Manages, transition.Fields, noSpawn.Manages)
                .Should().BeEmpty();

            // A value the builder has since moved off what the behavior pinned is a real loss.
            store.SetInteger(BehaviorFieldStorage.Field, "Cursor", GffFieldType.Byte, 9);
            BehaviorSwitchLosses.Describe(
                    store, transition.Manages, transition.Fields, noSpawn.Manages)
                .Should().Contain("Cursor");
        }

        private static JsonGffStruct NewTrigger()
        {
            var document = JsonGffDocument.Parse(
                System.Text.Encoding.UTF8.GetBytes(
                    "{\n  \"__data_type\": \"UTT \",\n  \"Tag\": { \"type\": \"cexostring\", \"value\": \"t\" }\n}\n"));
            return document.Root;
        }

        private static IEnumerable<JsonGffStruct> CorpusTriggers()
        {
            var gitDirectory = Path.Combine(CorpusLocator.ModuleDirectory, "git");
            if (!Directory.Exists(gitDirectory))
                Assert.Ignore("The module corpus is not present in this checkout.");

            foreach (var path in Directory.EnumerateFiles(gitDirectory, "*.git.json"))
            {
                var git = new GitDocument(JsonGffDocument.Load(path));
                foreach (var trigger in git.Triggers)
                    yield return trigger;
            }
        }

        /// <summary>Kept honest against the raw JSON: the corpus reader must see real triggers.</summary>
        [Test]
        public void TheCorpusReaderFindsTheTriggersTheFilesActuallyContain()
        {
            var path = Path.Combine(CorpusLocator.ModuleDirectory, "git", "tat_tomoseisley1.git.json");
            if (!File.Exists(path))
                Assert.Ignore("tat_tomoseisley1.git.json is not present in this checkout.");

            using var raw = JsonDocument.Parse(File.ReadAllText(path));
            var expected = raw.RootElement.GetProperty("TriggerList").GetProperty("value").GetArrayLength();

            new GitDocument(JsonGffDocument.Load(path)).Triggers.Count.Should().Be(expected);
        }
    }
}
