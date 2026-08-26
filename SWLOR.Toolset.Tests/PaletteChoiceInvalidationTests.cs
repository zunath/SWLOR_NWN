using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.Editors.Waypoints;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors.Behaviors;
using SWLOR.Toolset.Editors.Creatures;
using SWLOR.Toolset.Editors.Doors;
using SWLOR.Toolset.Editors.Items;
using SWLOR.Toolset.Editors.Merchants;
using SWLOR.Toolset.Editors.Sounds;
using SWLOR.Toolset.Editors.Triggers;
using SWLOR.Toolset.Editors.Waypoints;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public sealed class PaletteChoiceInvalidationTests
    {
        [Test]
        public void OpenSpecializedEditorsRebuildTheirMaterializedTlkChoices()
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
            using var creature = new CreatureEditorViewModel(
                NewStruct("UTC "),
                Path.Combine(Path.GetTempPath(), "utc", "creature.utc.json"),
                "creature",
                Run,
                gameCodeIndex: null,
                resolveChoices: ResolveChoices,
                resourceIndex: null,
                resolveModel: null,
                appearance: _ => null,
                armorParts: null);
            using var item = new ItemEditorViewModel(
                NewStruct("UTI "),
                "item",
                Run,
                resolveChoices: ResolveChoices);
            using var merchant = new MerchantEditorViewModel(
                NewStruct("UTM "),
                "merchant",
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

            ChoiceDisplay(door.BasicRows, "PaletteID").Should().Be("Category 0");
            ChoiceDisplay(creature.BasicRows, "PaletteID").Should().Be("Category 0");
            ChoiceDisplay(item.BasicRows, "PaletteID").Should().Be("Category 0");
            ChoiceDisplay(merchant.DetailRows, "ID").Should().Be("Category 0");
            ChoiceDisplay(trigger.BasicRows, "PaletteID").Should().Be("Category 0");
            ChoiceDisplay(waypoint.BasicRows, "PaletteID").Should().Be("Category 0");
            ChoiceDisplay(sound.BasicRows, "PaletteID").Should().Be("Category 0");
            ChoiceDisplay(door.BasicRows, "Faction").Should().Be("Category 0");
            ChoiceDisplay(creature.BasicRows, "WalkRate").Should().Be("Category 0");
            ChoiceDisplay(item.BasicRows, "BaseItem").Should().Be("Category 0");

            revision = 1;
            door.RefreshTlkLabels();
            creature.RefreshTlkLabels();
            item.RefreshTlkLabels();
            merchant.RefreshTlkLabels();
            trigger.RefreshTlkLabels();
            waypoint.RefreshTlkLabels();
            sound.RefreshTlkLabels();

            ChoiceDisplay(door.BasicRows, "PaletteID").Should().Be("Category 1");
            ChoiceDisplay(creature.BasicRows, "PaletteID").Should().Be("Category 1");
            ChoiceDisplay(item.BasicRows, "PaletteID").Should().Be("Category 1");
            ChoiceDisplay(merchant.DetailRows, "ID").Should().Be("Category 1");
            ChoiceDisplay(trigger.BasicRows, "PaletteID").Should().Be("Category 1");
            ChoiceDisplay(waypoint.BasicRows, "PaletteID").Should().Be("Category 1");
            ChoiceDisplay(sound.BasicRows, "PaletteID").Should().Be("Category 1");
            ChoiceDisplay(door.BasicRows, "Faction").Should().Be("Category 1");
            ChoiceDisplay(creature.BasicRows, "WalkRate").Should().Be("Category 1");
            ChoiceDisplay(item.BasicRows, "BaseItem").Should().Be("Category 1");
        }

        private static string ChoiceDisplay<T>(IEnumerable<T> rows, string fieldName)
            where T : BehaviorRowViewModel =>
            rows.Single(row => row.Definition.Name == fieldName).Choices.Single().Display;

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
