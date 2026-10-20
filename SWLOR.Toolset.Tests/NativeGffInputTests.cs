using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Common;

namespace SWLOR.Toolset.Tests;

public class NativeGffInputTests
{
    [Test]
    public void UnsignedStructureIdsPreserveTheirBitsWithoutChangingSourceOrFieldValues()
    {
        var source = Path.Combine(Path.GetTempPath(), "swlor_gff_source_" + Guid.NewGuid().ToString("N") + ".json");
        const string json = """
            {"__data_type":"IFO ","__struct_id":4294967295,
             "Nested":{"type":"list","value":[{"__struct_id":2147483648}]},
             "Count":{"type":"dword","value":4294967295},
             "Height":{"type":"double","value":1.2345678901234567},
             "Name":{"type":"cexostring","value":"Unicode: é"}}
            """;
        File.WriteAllText(source, json);
        string staged;
        try
        {
            using (var input = NativeGffInput.Prepare(source))
            {
                staged = input.InputPath;
                staged.Should().NotBe(source);
                using var document = JsonDocument.Parse(File.ReadAllBytes(staged));
                document.RootElement.GetProperty("__struct_id").GetInt32().Should().Be(-1);
                document.RootElement.GetProperty("Nested").GetProperty("value")[0]
                    .GetProperty("__struct_id").GetInt32().Should().Be(int.MinValue);
                document.RootElement.GetProperty("Count").GetProperty("value").GetUInt32().Should().Be(uint.MaxValue);
                document.RootElement.GetProperty("Height").GetProperty("value").GetRawText().Should().Be("1.2345678901234567");
                document.RootElement.GetProperty("Name").GetProperty("value").GetString().Should().Be("Unicode: é");
                File.ReadAllText(source).Should().Be(json);
            }
            File.Exists(staged).Should().BeFalse("temporary converter inputs are always removed");
        }
        finally { File.Delete(source); }
    }

    [Test]
    public void LegacyBinaryStringsRemainByteIdentical()
    {
        var source = Path.Combine(Path.GetTempPath(), "swlor_gff_legacy_" + Guid.NewGuid().ToString("N") + ".json");
        const string json = "{\"__struct_id\":4294967295,\"Mod_ID\":{\"type\":\"void\",\"value\":\"ÿÿÿÿ\"}}";
        var bytes = System.Text.Encoding.Latin1.GetBytes(json);
        File.WriteAllBytes(source, bytes);
        try
        {
            using var input = NativeGffInput.Prepare(source);
            File.ReadAllBytes(input.InputPath).Should().Equal(
                System.Text.Encoding.Latin1.GetBytes(json.Replace("4294967295", "-1")));
            File.ReadAllBytes(source).Should().Equal(bytes);
        }
        finally { File.Delete(source); }
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(2147483647)]
    public void CompatibleStructureIdsUseTheOriginalFile(long id)
    {
        var source = Path.Combine(Path.GetTempPath(), "swlor_gff_source_" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(source, "{\"__data_type\":\"IFO \",\"__struct_id\":" + id + "}");
        try
        {
            using (var input = NativeGffInput.Prepare(source)) input.InputPath.Should().Be(source);
            File.Exists(source).Should().BeTrue();
        }
        finally { File.Delete(source); }
    }
}
