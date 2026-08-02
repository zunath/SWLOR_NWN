using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.Doors;
using SWLOR.Toolset.Editors.Sounds;
using SWLOR.Toolset.Editors.Triggers;
using SWLOR.Toolset.Editors.Waypoints;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public sealed class PaletteChoiceInvalidationTests
    {
        [Test]
        public void OpenBehaviorEditorsRebuildTheirMaterializedPaletteChoices()
        {
            var revision = 0;
            IReadOnlyList<BehaviorChoice> ResolveChoices(string _) =>
                new[] { new BehaviorChoice(revision, $"Category {revision}") };

            using var door = new DoorEditorViewModel(
                NewStruct("UTD "),
                "door",
                isInstance: false,
                Run,
                resolveChoices: ResolveChoices);
            var trigger = new TriggerEditorViewModel(
                NewStruct("UTT "),
                "trigger",
                isInstance: false,
                Run,
                resolveChoices: ResolveChoices);
            var waypoint = new WaypointEditorViewModel(
                NewStruct("UTW "),
                "waypoint",
                isInstance: false,
                Run,
                new WaypointBehaviorCatalog(null, null),
                resolveChoices: ResolveChoices);
            var sound = new SoundEditorViewModel(
                NewStruct("UTS "),
                "sound",
                isInstance: false,
                Run,
                resolveChoices: ResolveChoices);

            CategoryDisplay(door.BasicRows).Should().Be("Category 0");
            CategoryDisplay(trigger.BasicRows).Should().Be("Category 0");
            CategoryDisplay(waypoint.BasicRows).Should().Be("Category 0");
            CategoryDisplay(sound.BasicRows).Should().Be("Category 0");

            revision = 1;
            door.RefreshPaletteChoices();
            trigger.RefreshPaletteChoices();
            waypoint.RefreshPaletteChoices();
            sound.RefreshPaletteChoices();

            CategoryDisplay(door.BasicRows).Should().Be("Category 1");
            CategoryDisplay(trigger.BasicRows).Should().Be("Category 1");
            CategoryDisplay(waypoint.BasicRows).Should().Be("Category 1");
            CategoryDisplay(sound.BasicRows).Should().Be("Category 1");
        }

        private static string CategoryDisplay<T>(IEnumerable<T> rows)
            where T : BehaviorRowViewModel =>
            rows.Single(row => row.Definition.Name == "PaletteID").Choices.Single().Display;

        private static bool Run(string _, Action mutation)
        {
            mutation();
            return true;
        }

        private static JsonGffStruct NewStruct(string type) =>
            JsonGffDocument.Parse(Encoding.UTF8.GetBytes(
                $"{{ \"__data_type\": \"{type}\" }}")).Root;
    }
}
