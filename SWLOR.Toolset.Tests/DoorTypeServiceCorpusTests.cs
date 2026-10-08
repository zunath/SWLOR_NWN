using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Tlk;
using SWLOR.Toolset.Domain.GameData.TwoDa;
using SWLOR.Toolset.Tests.Support;

namespace SWLOR.Toolset.Tests;

[TestFixture]
public sealed class DoorTypeServiceCorpusTests
{
    [Test]
    public void DoorTablesUsePhysicalRowIndicesWhenLabelsDiffer()
    {
        var haksRoot = ToolsetCorpusPaths.HaksRoot
                       ?? throw new InvalidOperationException("Select the read-only HAK corpus with SWLOR_TEST_HAKS_ROOT.");
        var twoDa = new TwoDaService(Path.Combine(haksRoot, "sw_2da"));
        var tlk = TlkService.Load(Path.Combine(haksRoot, "sw_tlk", "sw_tlk.tlk.json"));
        var service = new DoorTypeService(twoDa, tlk);
        var specificRows = service.GetAll();
        var genericRows = service.GetGenericAll();
        TestContext.Out.WriteLine($"doortypes selectable={specificRows.Count}; ids={string.Join(",", specificRows.Select(row => row.Id))}");
        TestContext.Out.WriteLine($"genericdoors selectable={genericRows.Count}; ids={string.Join(",", genericRows.Select(row => row.Id))}");

        var genericTable = twoDa.GetTable(TwoDaLookupTables.GenericDoor.TableName);
        Assert.That(genericTable.GetRowLabel(127), Is.EqualTo("1361"));
        Assert.That(genericTable.GetRowLabel(128), Is.EqualTo("1362"));
        Assert.That(service.GetGeneric(127), Is.EqualTo(
            new GenericDoorRow(127, "Jabba_palace_door", service.GetGeneric(127).DisplayName, "ttd_door009")
            {
                VisibleModel = true
            }));
        Assert.That(service.GetGeneric(128).Label, Is.EqualTo("Astroport_door"));
        Assert.That(service.GetGeneric(128).Model, Is.EqualTo("ttd_door_010"));

        var specificTable = twoDa.GetTable(TwoDaLookupTables.DoorType.TableName);
        var legacySpecific = new List<(int Id, string Model)>();
        for (var row = 0; row < specificTable.RowCount; row++)
        {
            var label = specificTable.GetString(row, "Label");
            var model = specificTable.GetString(row, "Model");
            var stringRef = specificTable.GetString(row, "StringRefGame");
            var visibleModel = TryReadInteger(specificTable, row, "VisibleModel");
            if (TwoDaChoicePolicy.IsSelectableLabel(label) &&
                TwoDaChoicePolicy.IsSelectableLabel(model) &&
                TwoDaChoicePolicy.IsSelectableLabel(stringRef) &&
                visibleModel is not null)
            {
                legacySpecific.Add((row, model!));
            }
        }

        var actualSpecific = specificRows.Select(row => (row.Id, row.Model!)).ToArray();
        TestContext.Out.WriteLine($"legacy eligible specific IDs/models: {string.Join(",", legacySpecific.Select(row => $"{row.Id}:{row.Model}"))}");
        TestContext.Out.WriteLine($"shared-reader specific IDs/models: {string.Join(",", actualSpecific.Select(row => $"{row.Id}:{row.Item2}"))}");
        Assert.That(actualSpecific, Is.EqualTo(legacySpecific));

        Assert.That(specificTable.GetRowLabel(1361), Is.EqualTo("1361"));
        Assert.That(specificTable.GetRowLabel(1362), Is.EqualTo("1362"));
        Assert.That(service.Get(1361).Label, Is.EqualTo("Jabba_palace_door"));
        Assert.That(service.Get(1361).Model, Is.EqualTo("ttd_door009"));
        Assert.That(service.Get(1362).Label, Is.EqualTo("Astroport_door"));
        Assert.That(service.Get(1362).Model, Is.EqualTo("ttd_door_010"));
    }
    private static int? TryReadInteger(TwoDaTable table, int row, string column) =>
        int.TryParse(table.GetString(row, column), System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

}
