using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Editors.Triggers;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Triggers;
using SWLOR.Toolset.Domain.Gff;

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
                    .Where(field => field.Storage == TriggerFieldStorage.Local)
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
            var store = new TriggerValueStore(trigger);
            var exploration = TriggerBehaviorCatalog.Get(TriggerBehaviorCatalog.ExplorationNoteId);

            foreach (var value in exploration.Manages)
                store.Apply(value);

            store.GetString(TriggerFieldStorage.Field, "ScriptOnEnter").Should().Be("explore_trigger");
            store.GetFloat(TriggerFieldStorage.Field, "HighlightHeight").Should().BeApproximately(3.0, 1e-4);
            exploration.Manages.Should().OnlyContain(value => store.Matches(value));
        }

        [Test]
        public void SwappingBehaviorLeavesNothingOfThePreviousOneBehind()
        {
            var trigger = NewTrigger();
            var store = new TriggerValueStore(trigger);
            var exploration = TriggerBehaviorCatalog.Get(TriggerBehaviorCatalog.ExplorationNoteId);
            var transition = TriggerBehaviorCatalog.Get(TriggerBehaviorCatalog.AreaTransitionId);

            foreach (var value in exploration.Manages)
                store.Apply(value);
            store.SetString(TriggerFieldStorage.Local, "DISPLAY_TEXT", GffFieldType.CExoString, "a note");

            store.Clear(exploration);
            foreach (var value in transition.Manages)
                store.Apply(value);

            // A stale OnEnter would still fire in game, and a stale local would still be read.
            store.GetString(TriggerFieldStorage.Field, "ScriptOnEnter").Should().BeEmpty();
            store.Locals.GetString("DISPLAY_TEXT").Should().BeNull();
            store.GetInteger(TriggerFieldStorage.Field, "Type").Should().Be(1);
            store.GetInteger(TriggerFieldStorage.Field, "Cursor").Should().Be(1);
        }

        [Test]
        public void AMangledManagedValueLosesItsTick()
        {
            var trigger = NewTrigger();
            var store = new TriggerValueStore(trigger);
            var restZone = TriggerBehaviorCatalog.Get(TriggerBehaviorCatalog.RestZoneId);

            foreach (var value in restZone.Manages)
                store.Apply(value);
            restZone.Manages.Should().OnlyContain(value => store.Matches(value));

            store.SetString(TriggerFieldStorage.Field, "ScriptOnExit", GffFieldType.ResRef, "something_else");

            store.Matches(restZone.Manages.Single(value => value.Name == "ScriptOnExit"))
                .Should().BeFalse();
        }

        [Test]
        public void ClassifyingReadsTheHandlerBeforeTheEngineType()
        {
            // An exploration note is Type 0 like any generic trigger; only its handler identifies it.
            var trigger = NewTrigger();
            new TriggerValueStore(trigger)
                .SetString(TriggerFieldStorage.Field, "ScriptOnEnter", GffFieldType.ResRef, "explore_trigger");

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

            // 16 is the engine's own limit - the GFF ResRef field is a fixed 16 bytes.
            resRef.MaxLength.Should().Be(16);
            resRef.MaxLength.Should().Be(TriggerEditorLayout.MaxResRefLength);

            // A tag is a CExoString with no engine maximum; this is the base toolset's editor limit,
            // and it has to stay clear of the longest tag the module actually ships.
            tag.MaxLength.Should().Be(32);
            LongestTriggerTagLength().Should().BeLessThan(tag.MaxLength);
        }

        [Test]
        public void NeitherCursorNorGeometryIsAskedFor()
        {
            var rows = TriggerEditorLayout.Basic.Concat(TriggerEditorLayout.Advanced).ToList();

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
            var type = TriggerEditorLayout.Advanced.Single(row => row.Name == "Type");
            type.CustomOnly.Should().BeTrue();

            foreach (var behavior in TriggerBehaviorCatalog.All)
            {
                var writesType = behavior.Manages.Any(value => value.Name == "Type");
                if (behavior.Id == TriggerBehaviorCatalog.CustomId)
                    writesType.Should().BeFalse("Custom manages nothing, so the raw row is the only way to set it");
                else
                    writesType.Should().BeTrue($"{behavior.DisplayName} hides the raw row, so it must set Type itself");
            }
        }

        [Test]
        public void CategoryAndFactionArePickedRatherThanTyped()
        {
            var category = TriggerEditorLayout.Basic.Single(row => row.Name == "PaletteID");
            var faction = TriggerEditorLayout.Advanced.Single(row => row.Name == "Faction");

            category.Kind.Should().Be(TriggerFieldKind.Choice);
            category.ChoicesKey.Should().Be(TriggerChoiceKeys.PaletteCategories);
            faction.Kind.Should().Be(TriggerFieldKind.Choice);
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
                longest = Math.Max(longest, new TriggerValueStore(trigger)
                    .GetString(TriggerFieldStorage.Field, "Tag").Length);

            return longest;
        }

        [Test]
        public void CustomExposesEveryScriptSlotItsClassifierRecognises()
        {
            var names = TriggerBehaviorCatalog.Custom.Fields
                .Where(field => field.Kind == TriggerFieldKind.Script)
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
            var store = new TriggerValueStore(trigger);

            var local = () => store.SetInteger(
                TriggerFieldStorage.Local, "QUEST_STATE", GffFieldType.Int, 2_147_483_648L);
            var dword = () => store.SetInteger(
                TriggerFieldStorage.Field, "LargeValue", GffFieldType.Dword, 4_294_967_296L);

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

            new TriggerValueStore(trigger).SetString(
                TriggerFieldStorage.Field, "ScriptOnEnter", GffFieldType.ResRef, "explore_trigger");
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
            new TriggerChoice(2, "Waypoint").ToString().Should().Be("Waypoint");
        }

        [Test]
        public void TheLoadScreenRowTakesItsValuesFromGameData()
        {
            var loadScreen = TriggerBehaviorCatalog.Get(TriggerBehaviorCatalog.AreaTransitionId)
                .Fields.Single(field => field.Name == "LoadScreenID");

            loadScreen.Kind.Should().Be(TriggerFieldKind.Choice);
            loadScreen.ChoicesKey.Should().Be(TriggerChoiceKeys.LoadScreens);
            loadScreen.Choices.Should().BeEmpty("the screens come from loadscreens.2da, not from this file");
        }

        /// <summary>A bare trigger struct, standing in for a freshly created blueprint.</summary>
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
