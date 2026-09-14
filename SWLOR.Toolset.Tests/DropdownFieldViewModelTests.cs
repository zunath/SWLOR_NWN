using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Gff;
using SWLOR.Toolset.Editors;

namespace SWLOR.Toolset.Tests;

public class DropdownFieldViewModelTests
{
    [Test]
    public void RefreshOptions_ReplacesCachedLabelsWithoutWritingTheDocument()
    {
        var document = JsonGffDocument.Parse(Encoding.UTF8.GetBytes(
            "{\"__data_type\":\"UTP \",\"Appearance\":{\"type\":\"dword\",\"value\":7}}"));
        var editCount = 0;
        var context = new EditorFieldContext(
            document,
            (_, mutation) =>
            {
                editCount++;
                mutation();
                return true;
            });
        var descriptor = new FieldDescriptor
        {
            Label = "Appearance",
            FieldName = "Appearance",
            Kind = EditorKind.TwoDaDropdown,
            FieldType = GffFieldType.Dword,
            LookupKey = "appearance"
        };
        var field = new DropdownFieldViewModel(
            descriptor,
            context,
            new[] { new LookupOption(7, "Old TLK label") });

        field.RefreshOptions(new[] { new LookupOption(7, "New TLK label") });

        field.SelectedOption!.Display.Should().Be("New TLK label");
        field.Options.Should().ContainSingle(option => option.Id == 7 && option.Display == "New TLK label");
        editCount.Should().Be(0);
        document.Root.GetOrNull("Appearance")!.GetInteger().Should().Be(7);
    }

    [Test]
    public void MissingLookup_ShowsRawValueWithoutAllowingItToBeWritten()
    {
        var document = JsonGffDocument.Parse(Encoding.UTF8.GetBytes(
            "{\"__data_type\":\"UTP \",\"Faction\":{\"type\":\"dword\",\"value\":42}}"));
        var editCount = 0;
        var context = new EditorFieldContext(
            document,
            (_, mutation) =>
            {
                editCount++;
                mutation();
                return true;
            });
        var descriptor = new FieldDescriptor
        {
            Label = "Faction",
            FieldName = "Faction",
            Kind = EditorKind.TwoDaDropdown,
            FieldType = GffFieldType.Dword,
            LookupKey = "factions"
        };

        var field = new DropdownFieldViewModel(descriptor, context, Array.Empty<LookupOption>());

        field.HasOptions.Should().BeFalse();
        field.RawValue.Should().Be(42);
        field.LookupUnavailableMessage.Should().Contain("read-only");

        field.RawValue = 99;

        editCount.Should().Be(0);
        document.Root.GetOrNull("Faction")!.GetInteger().Should().Be(42);
    }
}
