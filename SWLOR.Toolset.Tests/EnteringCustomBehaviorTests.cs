using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Doors;
using SWLOR.Toolset.Domain.Editors.Sounds;
using SWLOR.Toolset.Domain.Editors.Triggers;
using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Doors;
using SWLOR.Toolset.Editors.Sounds;
using SWLOR.Toolset.Editors.Triggers;
using SWLOR.Toolset.Editors.Waypoints;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Switching *to* Custom keeps everything.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Custom is the raw editor for the very fields the named behaviors own, so clearing them on the
    /// way in left the panel that exists to expose the configuration opening with the configuration
    /// erased. A Map Note switched to Custom lost its text, <c>HasMapNote</c>, <c>MapNoteEnabled</c>
    /// and appearance; a Point Ambience lost its Volume, Interval, PitchVariation, MaxDistance,
    /// Elevation and Times. Saving made that permanent.
    /// </para>
    /// <para>
    /// It is also the one direction where the clear buys nothing: no incoming behavior is writing
    /// replacements over what went. Custom-to-named still clears, and still asks first — see
    /// <see cref="BehaviorSwitchConfirmationTests"/>.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class EnteringCustomBehaviorTests
    {
        [Test]
        public void AWaypointKeepsItsFieldsOnTheWayIntoCustom()
        {
            var catalog = new WaypointBehaviorCatalog(gameCodeIndex: null, transitionDestinationTags: null);
            var document = Document("UTW ");
            var waypoint = document.Root;
            var editor = new WaypointEditorViewModel(
                waypoint, "wp_test", isInstance: false, Accept, catalog);

            var mapNote = catalog.All.FirstOrDefault(b => b.Id.Contains("map_note", StringComparison.Ordinal));
            if (mapNote == null)
                Assert.Ignore("No map-note behavior in the catalog.");

            editor.ChooseBehavior(mapNote);

            // A plain scalar the map-note behavior owns; the localized MapNote text needs a
            // different setter and proves the same thing.
            var store = new BehaviorValueStore(waypoint);
            store.SetInteger(BehaviorFieldStorage.Field, "MapNoteEnabled", GffFieldType.Byte, 1);
            store.SetInteger(BehaviorFieldStorage.Field, "Appearance", GffFieldType.Byte, 3);
            var before = Snapshot(document);

            editor.ChooseBehavior(catalog.Custom);

            editor.Behavior.Id.Should().Be(WaypointBehaviorCatalog.CustomId);
            new BehaviorValueStore(waypoint).GetInteger(BehaviorFieldStorage.Field, "Appearance")
                .Should().Be(3);
            Snapshot(document).Should().Be(
                before, "Custom is the raw editor for these fields, and nothing is replacing them");
        }

        [Test]
        public void ASoundKeepsItsSettingsOnTheWayIntoCustom()
        {
            var document = Document("UTS ");
            var sound = document.Root;
            var editor = new SoundEditorViewModel(
                sound, "snd_test", isInstance: false, Accept);

            var named = SoundBehaviorCatalog.All.FirstOrDefault(b => !b.AllowsVariables);
            if (named == null)
                Assert.Ignore("No named sound behavior in the catalog.");

            editor.ChooseBehavior(named);

            var store = new BehaviorValueStore(sound);
            store.SetInteger(BehaviorFieldStorage.Field, "Volume", GffFieldType.Byte, 87);
            store.SetInteger(BehaviorFieldStorage.Field, "Interval", GffFieldType.Dword, 4200);
            var before = Snapshot(document);

            editor.ChooseBehavior(SoundBehaviorCatalog.All.First(b => b.AllowsVariables));

            new BehaviorValueStore(sound).GetInteger(BehaviorFieldStorage.Field, "Volume")
                .Should().Be(87);
            Snapshot(document).Should().Be(before);
        }

        [Test]
        public void ATriggerKeepsItsFieldsOnTheWayIntoCustom()
        {
            var document = Document("UTT ");
            var trigger = document.Root;
            var editor = new TriggerEditorViewModel(
                trigger, "trg_test", isInstance: false, Accept);

            var named = TriggerBehaviorCatalog.All.First(b => b.Id != TriggerBehaviorCatalog.CustomId);
            editor.ChooseBehavior(named);

            var before = Snapshot(document);

            editor.ChooseBehavior(TriggerBehaviorCatalog.Custom);

            editor.Behavior.Id.Should().Be(TriggerBehaviorCatalog.CustomId);
            Snapshot(document).Should().Be(before);
        }

        /// <summary>
        /// The door is deliberately not on this rule, and it is worth saying so here rather than
        /// leaving the omission to look like an oversight. A door is classified by its locals, so
        /// switching a Key Item Door to Custom is precisely how a builder unwires it - the clear is
        /// the operation, not a side effect. What a door needed instead was the confirmation in
        /// <see cref="BehaviorSwitchConfirmationTests"/>, for the locals it does not own.
        /// </summary>
        [Test]
        public void ADoorStillClearsItsPresetOnTheWayIntoCustom()
        {
            var document = Document("UTD ");
            var door = document.Root;
            var editor = new DoorEditorViewModel(
                door, "dor_test", isInstance: false, Accept);

            editor.ChooseBehavior(DoorBehaviorCatalog.Get(DoorBehaviorCatalog.LockedDoorId));

            var store = new BehaviorValueStore(door);
            store.SetString(BehaviorFieldStorage.Field, "KeyName", GffFieldType.CExoString, "vault_key");

            editor.ChooseBehavior(DoorBehaviorCatalog.Get(DoorBehaviorCatalog.CustomId));

            editor.Behavior.Id.Should().Be(DoorBehaviorCatalog.CustomId);
            new BehaviorValueStore(door).GetString(BehaviorFieldStorage.Field, "KeyName")
                .Should().BeEmpty("unwiring the preset is what switching a door to Custom means");
        }

        /// <summary>The whole document as bytes - the cheapest "nothing at all changed" assertion.</summary>
        private static string Snapshot(JsonGffDocument document) =>
            Encoding.UTF8.GetString(document.ToBytes());

        private static bool Accept(string description, Action mutation)
        {
            mutation();
            return true;
        }

        private static JsonGffDocument Document(string dataType) =>
            JsonGffDocument.Parse(Encoding.UTF8.GetBytes("{\"__data_type\":\"" + dataType + "\"}"));
    }
}
