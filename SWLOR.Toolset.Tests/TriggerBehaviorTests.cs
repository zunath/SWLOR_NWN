using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
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
            // previous heading. Custom is not a hazard, and neither is None.
            TriggerBehaviorCatalog.Custom.Group.Should().BeNull();
            TriggerBehaviorCatalog.None.Group.Should().BeNull();
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

        [Test]
        public void ATriggerWithNothingSetIsNoneRatherThanCustom()
        {
            TriggerBehaviorCatalog.Classify(NewTrigger()).Id
                .Should().Be(TriggerBehaviorCatalog.NoneId);
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
