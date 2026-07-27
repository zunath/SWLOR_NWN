using System.Collections.Concurrent;
using FluentAssertions;
using NUnit.Framework;
using Radoub.Formats.Gff;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Round-trip coverage for GffJsonBridge: JSON -&gt; GffFile -&gt; JSON must reproduce the
    /// original bytes for a sample of the module corpus, and a hand-built GffFile must bridge
    /// to a JSON document with the expected shape.
    /// </summary>
    public class GffBridgeTests
    {
        private const int FilesPerFolder = 30;

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

        [Test]
        public void SampledModuleFiles_BridgeRoundTripsByteIdentical()
        {
            var files = CorpusLocator.GffFolders
                .SelectMany(folder =>
                {
                    var path = Path.Combine(CorpusLocator.ModuleDirectory, folder);
                    return Directory.Exists(path)
                        ? Directory.EnumerateFiles(path, "*.json").Take(FilesPerFolder)
                        : Enumerable.Empty<string>();
                })
                .ToList();

            files.Should().NotBeEmpty();

            var failures = new ConcurrentBag<string>();
            Parallel.ForEach(files, file =>
            {
                try
                {
                    var original = File.ReadAllBytes(file);
                    var document = JsonGffDocument.Parse(original);
                    var gffFile = GffJsonBridge.ToGffFile(document);
                    // The corpus mixes UTF-8 and legacy Windows-1252 files; re-emitting text in
                    // the source file's detected encoding is what keeps the round trip
                    // byte-identical for both.
                    var roundTripped = GffJsonBridge.ToJsonDocument(
                        gffFile, encodeTextAsUtf8: JsonStringCodec.IsUtf8Content(original));

                    // Formatting metadata (EOL style / trailing newline) lives on the JSON
                    // document, not on GffFile, so it is not something the bridge carries.
                    // Copy it from the source so the byte comparison targets content only.
                    roundTripped.UsesCrLf = document.UsesCrLf;
                    roundTripped.HasTrailingNewline = document.HasTrailingNewline;
                    roundTripped.TrailingNewlineUsesCrLf = document.TrailingNewlineUsesCrLf;

                    var written = roundTripped.ToBytes();

                    if (!written.AsSpan().SequenceEqual(original))
                        failures.Add(RoundTripCorpusTests.DescribeMismatch(file, original, written));
                }
                catch (Exception ex)
                {
                    failures.Add($"{file}: {ex.GetType().Name}: {ex.Message}");
                }
            });

            failures.Should().BeEmpty(
                $"all sampled files must bridge round-trip byte-identically. " +
                $"{failures.Count} failed. First failures:\n{string.Join("\n\n", failures.Take(10))}");
        }

        [Test]
        public void HandBuiltGffFile_BridgesToExpectedJsonShape()
        {
            var childStruct = new GffStruct { Type = 2 };
            GffFieldBuilder.AddIntField(childStruct, "ChildValue", 7);

            var locString = new CExoLocString();
            locString.SetString(0, "Hello");
            locString.SetString(2, "Bonjour");

            var listElement = new GffStruct { Type = 0 };
            GffFieldBuilder.AddCExoStringField(listElement, "Name", "Item One");

            var root = new GffStruct { Type = 0 };
            GffFieldBuilder.AddByteField(root, "AByte", 199);
            GffFieldBuilder.AddCharField(root, "AChar", -5);
            GffFieldBuilder.AddWordField(root, "AWord", 6000);
            GffFieldBuilder.AddShortField(root, "AShort", -1234);
            GffFieldBuilder.AddDwordField(root, "ADword", 4000000000u);
            GffFieldBuilder.AddIntField(root, "AnInt", -70000);
            GffFieldBuilder.AddDword64Field(root, "ADword64", 18000000000000000000UL);
            GffFieldBuilder.AddInt64Field(root, "AnInt64", -9000000000000000000L);
            GffFieldBuilder.AddFloatField(root, "AFloat", 70.0f);
            GffFieldBuilder.AddDoubleField(root, "ADouble", 12345.6789);
            GffFieldBuilder.AddCExoStringField(root, "AString", "Hello \"World\"\n");
            GffFieldBuilder.AddCResRefField(root, "AResRef", "some_resref");
            GffFieldBuilder.AddLocStringField(root, "ALocString", locString);
            GffFieldBuilder.AddVoidField(root, "AVoid", new byte[] { 0x00, 0x01, 0xFF, 0x20, (byte)'"' });
            GffFieldBuilder.AddStructField(root, "AStruct", childStruct);
            GffFieldBuilder.AddListField(root, "AList", new[] { listElement });

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

            // Bridging back to GffFile and re-bridging to JSON must reproduce the same shape
            // (a lightweight structural round-trip complementing the corpus byte-identity gate).
            var roundTrippedGff = GffJsonBridge.ToGffFile(document);
            var roundTrippedDocument = GffJsonBridge.ToJsonDocument(roundTrippedGff);
            roundTrippedDocument.Root.Get("AByte").GetUnsignedInteger().Should().Be(199);
            roundTrippedDocument.Root.Get("AFloat").GetSingle().Should().Be(70.0f);
        }

        [Test]
        public void LocStringWithStrRef_RoundTripsIdWhenPresent()
        {
            var locString = new CExoLocString { StrRef = 12835 };
            locString.SetString(0, "Tat Militia Armor");

            var root = new GffStruct { Type = 0 };
            GffFieldBuilder.AddLocStringField(root, "LocalizedName", locString);

            var gffFile = new GffFile { FileType = "UTI ", FileVersion = "V3.2", RootStruct = root };
            var document = GffJsonBridge.ToJsonDocument(gffFile);

            var field = document.Root.Get("LocalizedName");
            field.GetLocStringId().Should().Be(12835u);

            var roundTripped = GffJsonBridge.ToGffFile(document);
            var loc = (CExoLocString)roundTripped.RootStruct.Fields.Single(f => f.Label == "LocalizedName").Value!;
            loc.StrRef.Should().Be(12835u);
        }
    }
}
