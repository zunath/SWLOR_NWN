using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Gff;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Read-side coverage for converting parsed binary GFF models to editor JSON documents.
    /// </summary>
    public class GffBridgeTests
    {
        [Test]
        public void Utf8AndWindows1252TokensBothDecodeToTheRealCharacter()
        {
            // The corpus stores the em dash both ways: veles_genstore as real UTF-8 bytes,
            // coolship as the raw Windows-1252 byte. Both must decode to U+2014, and each must
            // re-encode byte-identically in its own detected encoding.
            var utf8Token = new byte[] { (byte)'"', (byte)'a', 0xE2, 0x80, 0x94, (byte)'b', (byte)'"' };
            var cp1252Token = new byte[] { (byte)'"', (byte)'a', 0x97, (byte)'b', (byte)'"' };

            JsonStringCodec.Decode(utf8Token).Should().Be("a—b");
            JsonStringCodec.Decode(cp1252Token).Should().Be("a—b");

            JsonStringCodec.Encode("a—b", useUtf8: true).Should().Equal(utf8Token);
            JsonStringCodec.Encode("a—b", useUtf8: false).Should().Equal(cp1252Token);
        }

        // The JSON->GffFile->JSON byte-identity corpus gate lives on the base branch; this branch
        // defers binary GFF writing (ToGffFile has no production caller), so only the read-side
        // and codec coverage applies here.

        [Test]
        public void HandBuiltGffFile_BridgesToExpectedJsonShape()
        {
            var childStruct = new GffStruct { Type = 2 };
            Add(childStruct, GffField.INT, "ChildValue", 7);

            var locString = new CExoLocString();
            locString.SetString(0, "Hello");
            locString.SetString(2, "Bonjour");

            var listElement = new GffStruct { Type = 0 };
            Add(listElement, GffField.CExoString, "Name", "Item One");

            var root = new GffStruct { Type = 0 };
            Add(root, GffField.BYTE, "AByte", (byte)199);
            Add(root, GffField.CHAR, "AChar", (sbyte)-5);
            Add(root, GffField.WORD, "AWord", (ushort)6000);
            Add(root, GffField.SHORT, "AShort", (short)-1234);
            Add(root, GffField.DWORD, "ADword", 4000000000u);
            Add(root, GffField.INT, "AnInt", -70000);
            Add(root, GffField.DWORD64, "ADword64", 18000000000000000000UL);
            Add(root, GffField.INT64, "AnInt64", -9000000000000000000L);
            Add(root, GffField.FLOAT, "AFloat", 70.0f);
            Add(root, GffField.DOUBLE, "ADouble", 12345.6789);
            Add(root, GffField.CExoString, "AString", "Hello \"World\"\n");
            Add(root, GffField.CResRef, "AResRef", "some_resref");
            Add(root, GffField.CExoLocString, "ALocString", locString);
            Add(root, GffField.VOID, "AVoid", new byte[] { 0x00, 0x01, 0xFF, 0x20, (byte)'"' });
            Add(root, GffField.Struct, "AStruct", childStruct);
            var list = new GffList();
            list.Elements.Add(listElement);
            Add(root, GffField.List, "AList", list);

            var gffFile = new GffFile { FileType = "TST ", FileVersion = "V3.2", RootStruct = root };

            var document = GffJsonBridge.ToJsonDocument(gffFile);

            document.DataType.Should().Be("TST ");
            document.Root.Contains("AByte").Should().BeTrue();

            document.Root.Get("AByte").Type.Should().Be(GffFieldType.Byte);
            document.Root.Get("AByte").GetUnsignedInteger().Should().Be(199);

            document.Root.Get("AChar").Type.Should().Be(GffFieldType.Char);
            document.Root.Get("AChar").GetInteger().Should().Be(-5);

            document.Root.Get("AWord").GetUnsignedInteger().Should().Be(6000);
            document.Root.Get("AShort").GetInteger().Should().Be(-1234);
            document.Root.Get("ADword").GetUnsignedInteger().Should().Be(4000000000u);
            document.Root.Get("AnInt").GetInteger().Should().Be(-70000);
            document.Root.Get("ADword64").GetUnsignedInteger().Should().Be(18000000000000000000UL);
            document.Root.Get("AnInt64").GetInteger().Should().Be(-9000000000000000000L);
            document.Root.Get("AFloat").GetSingle().Should().Be(70.0f);
            document.Root.Get("ADouble").GetDouble().Should().Be(12345.6789);
            document.Root.Get("AString").GetString().Should().Be("Hello \"World\"\n");
            document.Root.Get("AResRef").GetString().Should().Be("some_resref");

            var locField = document.Root.Get("ALocString");
            locField.GetLocStringId().Should().BeNull();
            locField.LocStringEntries.Should().HaveCount(2);
            locField.LocStringEntries![0].LanguageKey.Should().Be("0");
            locField.LocStringEntries[0].GetText().Should().Be("Hello");
            locField.LocStringEntries[1].LanguageKey.Should().Be("2");
            locField.LocStringEntries[1].GetText().Should().Be("Bonjour");

            var voidField = document.Root.Get("AVoid");
            voidField.Type.Should().Be(GffFieldType.Void);
            JsonStringCodec.DecodeToBytes(voidField.RawValue!).Should().Equal(0x00, 0x01, 0xFF, 0x20, (byte)'"');

            var structField = document.Root.Get("AStruct");
            structField.Type.Should().Be(GffFieldType.Struct);
            structField.GetStructId().Should().Be(2u);
            structField.Struct!.RawStructId.Should().NotBeNull();
            structField.Struct.Get("ChildValue").GetInteger().Should().Be(7);

            var listField = document.Root.Get("AList");
            listField.Type.Should().Be(GffFieldType.List);
            listField.Elements.Should().HaveCount(1);
            listField.Elements![0].Get("Name").GetString().Should().Be("Item One");

        }

        [Test]
        public void LocStringWithStrRef_PreservesId()
        {
            var locString = new CExoLocString { StrRef = 12835 };
            locString.SetString(0, "Tat Militia Armor");

            var root = new GffStruct { Type = 0 };
            Add(root, GffField.CExoLocString, "LocalizedName", locString);

            var gffFile = new GffFile { FileType = "UTI ", FileVersion = "V3.2", RootStruct = root };
            var document = GffJsonBridge.ToJsonDocument(gffFile);

            var field = document.Root.Get("LocalizedName");
            field.GetLocStringId().Should().Be(12835u);
        }

        [Test]
        public void NonWesternLocalizedSubstringFallsBackToUtf8()
        {
            const string polishText = "Łódź";
            var locString = new CExoLocString();
            locString.SetString(10, polishText);

            var root = new GffStruct { Type = 0 };
            Add(root, GffField.CExoLocString, "LocalizedName", locString);
            var gffFile = new GffFile
            {
                FileType = "UTI ",
                FileVersion = "V3.2",
                RootStruct = root
            };

            var document = GffJsonBridge.ToJsonDocument(gffFile);
            var entry = document.Root.Get("LocalizedName").LocStringEntries!.Single();

            entry.LanguageKey.Should().Be("10");
            entry.GetText().Should().Be(polishText);
            entry.RawText.Should().Equal(JsonStringCodec.Encode(polishText, useUtf8: true));
        }

        private static void Add(GffStruct target, uint type, string label, object? value) =>
            target.Fields.Add(new GffField(type, label, value));
    }
}
