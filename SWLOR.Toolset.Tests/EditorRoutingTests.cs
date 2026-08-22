using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Workspace;
using SWLOR.Toolset.Editors;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// What opens when a builder double-clicks something.
    /// </summary>
    /// <remarks>
    /// A type with no schema does not fail loudly — it writes "No editor available yet" to the
    /// Output panel and nothing appears, which reads as the double-click not registering. So the
    /// invariant worth holding is that every type the explorer lists opens as <em>something</em>.
    /// </remarks>
    [TestFixture]
    public class EditorRoutingTests
    {
        [Test]
        public void EveryBlueprintTypeTheExplorerListsHasAnEditor()
        {
            foreach (var type in ModuleWorkspace.BlueprintTypes)
            {
                EditorService.SchemaFor(type).Should().NotBeNull(
                    $"{type.DisplayName()} is offered in the explorer, so double-clicking one has to open");
            }
        }

        [Test]
        public void EverySchemaDeclaresTheTypeItEdits()
        {
            foreach (var type in ModuleWorkspace.BlueprintTypes)
            {
                EditorService.SchemaFor(type)!.ResourceType.Should().Be(
                    type, "a schema wired to the wrong type would edit the wrong fields");
            }
        }

        [Test]
        public void EverySchemaOffersItsIdentityFieldsAndNothingUnlabelled()
        {
            foreach (var type in ModuleWorkspace.BlueprintTypes)
            {
                var schema = EditorService.SchemaFor(type)!;
                var fields = schema.Groups.SelectMany(group => group.Fields).ToList();

                fields.Should().NotBeEmpty($"{type.DisplayName()} must offer something to edit");
                fields.Should().OnlyContain(
                    field => !string.IsNullOrWhiteSpace(field.Label),
                    $"{type.DisplayName()} has a field a builder cannot identify");
                fields.Should().OnlyContain(
                    field => !string.IsNullOrWhiteSpace(field.FieldName),
                    $"{type.DisplayName()} has a field bound to nothing");

                fields.Select(field => field.FieldName)
                    .Should().Contain(
                        name => name == "TemplateResRef" || name == "ResRef",
                        $"{type.DisplayName()} must show which blueprint is open");
            }
        }

        [Test]
        public void NoSchemaPutsTwoFieldsOnTheSameStoredValue()
        {
            // Two controls over one field disagree the moment one of them is edited, and which one
            // wins depends on tab order.
            foreach (var type in ModuleWorkspace.BlueprintTypes)
            {
                var schema = EditorService.SchemaFor(type)!;
                schema.Groups
                    .SelectMany(group => group.Fields)
                    .GroupBy(field => field.FieldName, StringComparer.Ordinal)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .Should().BeEmpty($"{type.DisplayName()} binds a field twice");
            }
        }

        [Test]
        public void ATypeWithNoEditorSaysSoRatherThanGuessing()
        {
            EditorService.SchemaFor(ResourceType.Nss).Should().BeNull();
            EditorService.SchemaFor(ResourceType.Dlg).Should().BeNull(
                "conversations open in the dialogue editor, not the schema form");
            EditorService.SchemaFor(ResourceType.Area).Should().BeNull(
                "areas open in the area editor, not the schema form");
        }

        [Test]
        public void EveryDropdownFieldNamesALookupToFillItself()
        {
            foreach (var type in ModuleWorkspace.BlueprintTypes)
            {
                var schema = EditorService.SchemaFor(type)!;
                var unsourced = schema.Groups
                    .SelectMany(group => group.Fields)
                    .Where(field => field.Kind == EditorKind.TwoDaDropdown)
                    .Where(field => string.IsNullOrWhiteSpace(field.LookupKey))
                    .Select(field => field.FieldName)
                    .ToList();

                // Not OnlyContain: a type with no dropdowns at all is fine, and the placeable has
                // none — its appearance moved to the picture grid.
                unsourced.Should().BeEmpty(
                    $"{type.DisplayName()} has a dropdown with no source, which renders empty");
            }
        }
    }
}
