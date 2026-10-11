using System.Text;
using FluentAssertions;
using NUnit.Framework;
using Nwn.Authoring.Documents.Native;
using Nwn.Authoring.Documents.NimGff;
using Nwn.Authoring.Editing;
using SWLOR.Toolset.Domain.Editors.Triggers;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;
using SWLOR.Toolset.Editors.Triggers;
using SWLOR.Toolset.Services;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// A trigger placed in an area gets the shared trigger editor over SWLOR's trigger behaviors, with
    /// its destination checked against the module's tags.
    /// </summary>
    [TestFixture]
    public class PlacedTriggerEditorTests
    {
        [Test]
        public void SelectingAPlacedTriggerUsesTheSharedEditorOverSwlorsBehaviors()
        {
            using var fixture = Fixture(
                Trigger("""
                    "Type": { "type": "int", "value": 1 },
                    "LinkedTo": { "type": "cexostring", "value": "dest_door" },
                    "LinkedToFlags": { "type": "byte", "value": 1 }
                    """),
                new TriggerEditorServices(
                    "my_area",
                    (scope, tag) => scope == BehaviorTagScope.Door && tag == "dest_door"
                        ? TransitionDestinationResult.Resolved("door in far_area")
                        : TransitionDestinationResult.NotFound));

            fixture.Section.SelectedRow = fixture.Section.Rows.Single();

            var editor = fixture.Section.TriggerEditor;
            fixture.Section.HasTriggerBehaviorEditor.Should().BeTrue();
            fixture.Section.UsesGenericDetailEditor.Should().BeFalse();
            fixture.Section.VarTableSection.Should().BeNull();
            editor.Should().NotBeNull().And.BeOfType<PlacedTriggerEditorViewModel>();
            editor!.HeaderKind.Should().Be("instance");
            editor.HeaderOwner.Should().Be("my_area");
            editor.Behavior.Id.Should().Be(TriggerBehaviorCatalog.AreaTransitionId);
            editor.BasicRows.Select(row => row.Label).Should().Contain(new[] { "Tag", "ResRef" });

            var destination = editor.BehaviorRows.Single(row => row.Definition.Name == "LinkedTo");
            destination.Status.Should().Be("✓ door in far_area");
            destination.IsStatusGood.Should().BeTrue();

            var type = editor.BehaviorRows.Single(row => row.Definition.Name == "LinkedToFlags");
            type.Choice = type.Choices.Single(choice => choice.Value == 2);
            destination.Status.Should().Be("⚠ no waypoint carries this tag");
            destination.IsStatusGood.Should().BeFalse();
            fixture.Instance.GetIntOrNull("LinkedToFlags").Should().Be(2);

            editor.BasicRows.Single(row => row.Definition.Name == "Tag").Text = "renamed_trigger";
            fixture.Section.Rows.Single().Tag.Should().Be("renamed_trigger");
        }

        [Test]
        public void ThePlacedEditorClassifiesWithSwlorsCatalog()
        {
            using var fixture = Fixture(
                Trigger("""
                    "ScriptOnEnter": { "type": "resref", "value": "explore_trigger" }
                    """),
                new TriggerEditorServices("my_area", null));
            fixture.Section.SelectedRow = fixture.Section.Rows.Single();
            fixture.Section.TriggerEditor!.Behavior.Id.Should().Be(TriggerBehaviorCatalog.ExplorationNoteId);

            using var trap = Fixture(
                Trigger("""
                    "TrapFlag": { "type": "byte", "value": 1 }
                    """),
                new TriggerEditorServices("my_area", null));
            trap.Section.SelectedRow = trap.Section.Rows.Single();
            trap.Section.TriggerEditor!.Behavior.Id.Should().Be(TriggerBehaviorCatalog.TrapId);

            using var plain = Fixture(Trigger(string.Empty), new TriggerEditorServices("my_area", null));
            plain.Section.SelectedRow = plain.Section.Rows.Single();
            plain.Section.TriggerEditor!.Behavior.Id.Should().Be(TriggerBehaviorCatalog.CustomId);
            plain.Section.TriggerEditor.Variables.Should().NotBeNull();
        }

        [Test]
        public void WithoutTriggerServicesATriggerKeepsTheGenericForm()
        {
            using var fixture = Fixture(Trigger(string.Empty), triggerServices: null);

            fixture.Section.SelectedRow = fixture.Section.Rows.Single();

            fixture.Section.HasTriggerBehaviorEditor.Should().BeFalse();
            fixture.Section.UsesGenericDetailEditor.Should().BeTrue();
            fixture.Section.VarTableSection.Should().NotBeNull();
        }

        private static string Trigger(string fields)
        {
            var separator = fields.Trim().Length == 0 ? string.Empty : ",";
            return """
                { "__struct_id": 1,
                  "Tag": { "type": "cexostring", "value": "placed_trigger" },
                  "TemplateResRef": { "type": "resref", "value": "trigger_template" },
                  "XPosition": { "type": "float", "value": 1.0 },
                  "YPosition": { "type": "float", "value": 2.0 },
                  "ZPosition": { "type": "float", "value": 0.0 }
                """ + separator + fields + "}";
        }

        private static TriggerFixture Fixture(string trigger, TriggerEditorServices? triggerServices)
        {
            var git = new DocumentSession(
                "area.git.json",
                JsonGffDocument.Parse(Encoding.UTF8.GetBytes(
                    """{ "__data_type": "GIT ", "TriggerList": { "type": "list", "value": [""" + trigger + "] } }")));
            var gic = new DocumentSession(
                "area.gic.json",
                JsonGffDocument.Parse(Encoding.UTF8.GetBytes("""{ "__data_type": "GIC " }""")));
            var section = new InstanceListSectionViewModel(
                "Triggers",
                "TriggerList",
                ResourceType.Utt,
                git,
                gic,
                new ModuleWorkspace(CorpusLocator.ModuleDirectory),
                (description, mutation) =>
                {
                    using (git.Begin(description))
                        mutation();
                    return true;
                },
                null,
                new OutputLogService(),
                new StubPrompts(),
                triggerEditorServices: triggerServices);
            return new TriggerFixture(git, gic, section);
        }

        private sealed class TriggerFixture(DocumentSession git, DocumentSession gic, InstanceListSectionViewModel section)
            : IDisposable
        {
            public InstanceListSectionViewModel Section { get; } = section;

            public JsonGffStruct Instance =>
                git.Document.Root.Get("TriggerList").Elements![0];

            public void Dispose()
            {
                Section.Dispose();
                git.Dispose();
                gic.Dispose();
            }
        }

        private sealed class StubPrompts : IEditorPromptService
        {
            public Task<ExternalChangeChoice> ConfirmExternalChangeAsync(string filePath) =>
                Task.FromResult(ExternalChangeChoice.Cancel);

            public Task<UnsavedChangesChoice> ConfirmCloseAsync(string documentTitle) =>
                Task.FromResult(UnsavedChangesChoice.Cancel);

            public Task<bool> ConfirmDestructiveAsync(
                string headline, string message, string confirmLabel) =>
                Task.FromResult(false);

            public Task<string?> PromptForTextAsync(
                string headline, string message, string initialValue, string confirmLabel) =>
                Task.FromResult<string?>(null);
        }
    }
}
