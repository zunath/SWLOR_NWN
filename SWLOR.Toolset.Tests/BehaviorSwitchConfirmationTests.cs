using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Doors;
using SWLOR.Toolset.Domain.Editors.Sounds;
using SWLOR.Toolset.Domain.Editors.Triggers;
using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Placeables;
using SWLOR.Toolset.Editors.Doors;
using SWLOR.Toolset.Editors.Sounds;
using SWLOR.Toolset.Editors.Triggers;
using SWLOR.Toolset.Editors.Waypoints;
using SWLOR.Toolset.Services;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Switching a Custom object to a preset throws away everything the preset does not know about.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Custom is not a behavior so much as the absence of one: it owns every raw slot the object has,
    /// including scripts and locals wired by hand for reasons no catalog records. Applying a preset
    /// clears all of them first, and until now nothing said so — the loss only surfaced after the
    /// document was saved, by which point undo was gone too.
    /// </para>
    /// <para>
    /// A named behavior's own values are a different matter and are still swapped silently: the
    /// incoming behavior replaces them, which is what choosing it means.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class BehaviorSwitchConfirmationTests
    {
        // ----- triggers -----

        [Test]
        public async Task SwitchingACustomTriggerAwayAsksBeforeDiscardingItsScripts()
        {
            var trigger = TriggerStruct();
            Write(trigger, "ScriptHeartbeat", "my_hb");
            Write(trigger, "ScriptUserDefine", "my_ud");

            var prompts = new RecordingPrompts(answer: false);
            var editor = new TriggerEditorViewModel(
                trigger, "trg_test", isInstance: false, Accept, prompts: prompts);

            editor.Behavior.Id.Should().Be(TriggerBehaviorCatalog.CustomId);

            var target = TriggerBehaviorCatalog.All.First(b => b.Id != TriggerBehaviorCatalog.CustomId);
            await editor.ChooseBehaviorAsync(target);

            prompts.Asked.Should().HaveCount(1);
            prompts.Asked[0].Message.Should().Contain("ScriptHeartbeat").And.Contain("ScriptUserDefine");
        }

        [Test]
        public async Task DecliningTheTriggerPromptKeepsEverything()
        {
            var trigger = TriggerStruct();
            Write(trigger, "ScriptHeartbeat", "my_hb");

            var prompts = new RecordingPrompts(answer: false);
            var editor = new TriggerEditorViewModel(
                trigger, "trg_test", isInstance: false, Accept, prompts: prompts);
            var target = TriggerBehaviorCatalog.All.First(b => b.Id != TriggerBehaviorCatalog.CustomId);

            await editor.ChooseBehaviorAsync(target);

            editor.Behavior.Id.Should().Be(TriggerBehaviorCatalog.CustomId);
            Read(trigger, "ScriptHeartbeat").Should().Be("my_hb");
        }

        [Test]
        public async Task AcceptingTheTriggerPromptGoesThroughWithTheSwitch()
        {
            var trigger = TriggerStruct();
            Write(trigger, "ScriptHeartbeat", "my_hb");

            var prompts = new RecordingPrompts(answer: true);
            var editor = new TriggerEditorViewModel(
                trigger, "trg_test", isInstance: false, Accept, prompts: prompts);
            var target = TriggerBehaviorCatalog.All.First(b => b.Id != TriggerBehaviorCatalog.CustomId);

            await editor.ChooseBehaviorAsync(target);

            editor.Behavior.Id.Should().Be(target.Id);
            Read(trigger, "ScriptHeartbeat").Should().BeNullOrEmpty();
        }

        /// <summary>
        /// An empty slot is not a loss, and naming it would bury the line that matters among a dozen
        /// that do not.
        /// </summary>
        [Test]
        public async Task ATriggerWithNothingInItSwitchesWithoutAsking()
        {
            var prompts = new RecordingPrompts(answer: true);
            var editor = new TriggerEditorViewModel(
                TriggerStruct(), "trg_test", isInstance: false, Accept, prompts: prompts);
            var target = TriggerBehaviorCatalog.All.First(b => b.Id != TriggerBehaviorCatalog.CustomId);

            await editor.ChooseBehaviorAsync(target);

            prompts.Asked.Should().BeEmpty();
            editor.Behavior.Id.Should().Be(target.Id);
        }

        // ----- doors -----

        [Test]
        public async Task SwitchingACustomDoorAwayAsksBeforeSweepingItsLocals()
        {
            var door = DoorStruct();
            var locals = new VarTable(door);
            locals.SetString("SOME_QUEST_HOOK", "escape_pod");
            locals.SetInt("UNRELATED_FLAG", 3);

            var prompts = new RecordingPrompts(answer: false);
            var editor = new DoorEditorViewModel(
                door, "dor_test", isInstance: false, Accept, prompts: prompts);

            editor.Behavior.Id.Should().Be(DoorBehaviorCatalog.CustomId);

            await editor.ChooseBehaviorAsync(DoorBehaviorCatalog.Get(DoorBehaviorCatalog.LockedDoorId));

            prompts.Asked.Should().HaveCount(1);
            prompts.Asked[0].Message.Should().Contain("SOME_QUEST_HOOK").And.Contain("UNRELATED_FLAG");

            // Declined, so the table is untouched.
            new VarTable(door).GetString("SOME_QUEST_HOOK").Should().Be("escape_pod");
            editor.Behavior.Id.Should().Be(DoorBehaviorCatalog.CustomId);
        }

        [Test]
        public async Task AcceptingTheDoorPromptSweepsTheLocals()
        {
            var door = DoorStruct();
            new VarTable(door).SetString("SOME_QUEST_HOOK", "escape_pod");

            var prompts = new RecordingPrompts(answer: true);
            var editor = new DoorEditorViewModel(
                door, "dor_test", isInstance: false, Accept, prompts: prompts);

            await editor.ChooseBehaviorAsync(DoorBehaviorCatalog.Get(DoorBehaviorCatalog.LockedDoorId));

            editor.Behavior.Id.Should().Be(DoorBehaviorCatalog.LockedDoorId);
            new VarTable(door).GetString("SOME_QUEST_HOOK").Should().BeNull();
        }

        // ----- waypoints -----

        [Test]
        public async Task DecliningTheWaypointPromptKeepsCustomMapNoteText()
        {
            var waypoint = MapNoteStruct();
            var prompts = new RecordingPrompts(answer: false);
            var catalog = new WaypointBehaviorCatalog(null, null);
            var editor = new WaypointEditorViewModel(
                waypoint, "wp_test", isInstance: true, Accept, catalog, prompts: prompts);

            await editor.ChooseBehaviorAsync(catalog.Get(WaypointBehaviorCatalog.StuckRescuePointId));

            prompts.Asked.Should().ContainSingle();
            prompts.Asked[0].Message.Should().Contain("MapNote");
            editor.Behavior.Id.Should().Be(WaypointBehaviorCatalog.MapNoteId);
            new BehaviorValueStore(waypoint).GetLocalizedText("MapNote")
                .Should().Be("Builder-authored destination");
        }

        [Test]
        public async Task AcceptingTheWaypointPromptClearsTheOutgoingCustomFields()
        {
            var waypoint = MapNoteStruct();
            var prompts = new RecordingPrompts(answer: true);
            var catalog = new WaypointBehaviorCatalog(null, null);
            var editor = new WaypointEditorViewModel(
                waypoint, "wp_test", isInstance: true, Accept, catalog, prompts: prompts);

            await editor.ChooseBehaviorAsync(catalog.Get(WaypointBehaviorCatalog.StuckRescuePointId));

            editor.Behavior.Id.Should().Be(WaypointBehaviorCatalog.StuckRescuePointId);
            new BehaviorValueStore(waypoint).GetLocalizedText("MapNote").Should().BeEmpty();
        }

        // ----- sounds -----

        [Test]
        public async Task SwitchingASoundAsksBeforeDiscardingScatterConfiguration()
        {
            var sound = ScatteredAmbienceStruct();
            var prompts = new RecordingPrompts(answer: false);
            var editor = new SoundEditorViewModel(
                sound, "snd_test", isInstance: true, Accept, prompts: prompts);

            editor.Behavior.Id.Should().Be(SoundBehaviorCatalog.ScatteredAmbienceId);

            await editor.ChooseBehaviorAsync(SoundBehaviorCatalog.Get(SoundBehaviorCatalog.AreaLoopId));

            prompts.Asked.Should().HaveCount(1);
            prompts.Asked[0].Message.Should().Contain("RandomRangeX").And.Contain("RandomRangeY");
        }

        [Test]
        public async Task DecliningTheSoundPromptKeepsEverything()
        {
            var sound = ScatteredAmbienceStruct();
            var prompts = new RecordingPrompts(answer: false);
            var editor = new SoundEditorViewModel(
                sound, "snd_test", isInstance: true, Accept, prompts: prompts);

            await editor.ChooseBehaviorAsync(SoundBehaviorCatalog.Get(SoundBehaviorCatalog.AreaLoopId));

            editor.Behavior.Id.Should().Be(SoundBehaviorCatalog.ScatteredAmbienceId);
            var store = new SoundValueStore(sound);
            store.GetFloat(BehaviorFieldStorage.Field, "RandomRangeX").Should().Be(22);
            store.GetFloat(BehaviorFieldStorage.Field, "RandomRangeY").Should().Be(13);
            store.GetSounds().Should().Equal("wind_one", "wind_two");
        }

        [Test]
        public async Task AcceptingTheSoundPromptGoesThroughWithTheSwitch()
        {
            var sound = ScatteredAmbienceStruct();
            var prompts = new RecordingPrompts(answer: true);
            var editor = new SoundEditorViewModel(
                sound, "snd_test", isInstance: true, Accept, prompts: prompts);

            await editor.ChooseBehaviorAsync(SoundBehaviorCatalog.Get(SoundBehaviorCatalog.AreaLoopId));

            editor.Behavior.Id.Should().Be(SoundBehaviorCatalog.AreaLoopId);
            var store = new SoundValueStore(sound);
            store.GetFloat(BehaviorFieldStorage.Field, "RandomRangeX").Should().Be(0);
            store.GetFloat(BehaviorFieldStorage.Field, "RandomRangeY").Should().Be(0);
            store.GetSounds().Should().Equal("wind_one");
        }

        [Test]
        public async Task ASoundWithNothingInItSwitchesWithoutAsking()
        {
            var sound = JsonGffDocument.Parse(
                System.Text.Encoding.UTF8.GetBytes("""{"__data_type":"UTS "}""")).Root;
            var store = new SoundValueStore(sound);
            store.SetInteger(BehaviorFieldStorage.Field, "Positional", GffFieldType.Byte, 1);
            store.SetInteger(BehaviorFieldStorage.Field, "RandomPosition", GffFieldType.Byte, 1);
            store.SetInteger(BehaviorFieldStorage.Field, "Continuous", GffFieldType.Byte, 1);

            var prompts = new RecordingPrompts(answer: true);
            var editor = new SoundEditorViewModel(
                sound, "snd_test", isInstance: true, Accept, prompts: prompts);
            var target = SoundBehaviorCatalog.Get(SoundBehaviorCatalog.AreaLoopId);

            await editor.ChooseBehaviorAsync(target);

            prompts.Asked.Should().BeEmpty();
            editor.Behavior.Id.Should().Be(target.Id);
        }

        // ----- placeables -----

        /// <summary>
        /// Custom means "stop interpreting this, show me the raw wiring". Erasing the wiring on the
        /// way in left the panel that exists to reveal it revealing nothing, with no target behavior
        /// replacing what went.
        /// </summary>
        [Test]
        public void SwitchingAPlaceableToCustomKeepsTheScriptsItHad()
        {
            var chair = PlaceableBehaviorCatalog.Behaviors
                .FirstOrDefault(behavior => behavior.Scripts.Count > 0 && !behavior.IsSentinel);
            if (chair == null)
                Assert.Ignore("No scripted placeable behavior in the catalog to switch away from.");

            var document = JsonGffDocument.Parse(
                System.Text.Encoding.UTF8.GetBytes("""{"__data_type":"UTP "}"""));
            PlaceableBehaviorApplier.Apply(document.Root, PlaceableBehaviorCatalog.None, chair);

            var before = chair.Scripts
                .ToDictionary(slot => slot.Key, slot => Read(document.Root, slot.Key));
            before.Values.Should().NotBeEmpty().And.NotContain(string.Empty);

            PlaceableBehaviorApplier.Apply(document.Root, chair, PlaceableBehaviorCatalog.Custom);

            foreach (var slot in before)
            {
                Read(document.Root, slot.Key).Should().Be(
                    slot.Value,
                    "Custom replaces nothing, so it has nothing to justify erasing");
            }
        }

        // ----- helpers -----

        /// <summary>Reads a root field through the public store, since the GFF accessors are internal.</summary>
        private static string Read(JsonGffStruct root, string name) =>
            new BehaviorValueStore(root).GetString(BehaviorFieldStorage.Field, name);

        private static void Write(JsonGffStruct root, string name, string value) =>
            new BehaviorValueStore(root).SetString(
                BehaviorFieldStorage.Field, name, GffFieldType.ResRef, value);

        private static bool Accept(string description, Action mutation)
        {
            mutation();
            return true;
        }

        private static JsonGffStruct TriggerStruct() =>
            JsonGffDocument.Parse(System.Text.Encoding.UTF8.GetBytes("""{"__data_type":"UTT "}""")).Root;

        private static JsonGffStruct DoorStruct() =>
            JsonGffDocument.Parse(System.Text.Encoding.UTF8.GetBytes("""{"__data_type":"UTD "}""")).Root;

        private static JsonGffStruct ScatteredAmbienceStruct()
        {
            var sound = JsonGffDocument.Parse(
                System.Text.Encoding.UTF8.GetBytes("""{"__data_type":"UTS "}""")).Root;
            var store = new SoundValueStore(sound);
            store.SetInteger(BehaviorFieldStorage.Field, "Positional", GffFieldType.Byte, 1);
            store.SetInteger(BehaviorFieldStorage.Field, "RandomPosition", GffFieldType.Byte, 1);
            store.SetInteger(BehaviorFieldStorage.Field, "Continuous", GffFieldType.Byte, 1);
            store.SetFloat(BehaviorFieldStorage.Field, "RandomRangeX", 22);
            store.SetFloat(BehaviorFieldStorage.Field, "RandomRangeY", 13);
            store.AddSound("wind_one");
            store.AddSound("wind_two");
            return sound;
        }

        private static JsonGffStruct MapNoteStruct()
        {
            var waypoint = JsonGffDocument.Parse(
                System.Text.Encoding.UTF8.GetBytes("""{"__data_type":"UTW "}""")).Root;
            var store = new BehaviorValueStore(waypoint);
            store.SetInteger(BehaviorFieldStorage.Field, "HasMapNote", GffFieldType.Byte, 1);
            store.SetInteger(BehaviorFieldStorage.Field, "MapNoteEnabled", GffFieldType.Byte, 1);
            store.SetLocalizedText("MapNote", "Builder-authored destination");
            return waypoint;
        }

        private sealed record Ask(string Headline, string Message);

        private sealed class RecordingPrompts(bool answer) : IEditorPromptService
        {
            public List<Ask> Asked { get; } = new();

            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<bool> ConfirmDestructiveAsync(string headline, string message, string confirmLabel)
            {
                Asked.Add(new Ask(headline, message));
                return Task.FromResult(answer);
            }

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);
        }
    }
}
