using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;
using SWLOR.Toolset.Domain.Editing;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Fail-closed guards at the GFF domain boundary: non-finite floats are rejected before any
    /// mutation (a stored nan/inf token is unparseable by the reader, so a saved file could never
    /// be reopened), a root-level __struct_id survives a save, void payloads cannot be transcoded
    /// through the text heuristic, and integer reads do not silently wrap Dword sentinels.
    /// </summary>
    [TestFixture]
    public class GffDomainGuardTests
    {
        private static JsonGffDocument ParseDocument(string json) =>
            JsonGffDocument.Parse(Encoding.UTF8.GetBytes(json));

        private const string FloatAndDoubleDocument =
            "{\"__data_type\":\"UTC \"," +
            "\"Bearing\":{\"type\":\"float\",\"value\":1.5}," +
            "\"Precision\":{\"type\":\"double\",\"value\":2.5}}";

        // ---------- non-finite float edits ----------

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void SetSingle_RejectsNonFiniteValues_AndLeavesTheDocumentUnchanged(float value)
        {
            var document = ParseDocument(FloatAndDoubleDocument);
            var original = document.ToBytes();

            var act = () => document.Root.Get("Bearing").SetSingle(value);

            act.Should().Throw<ArgumentOutOfRangeException>();
            document.ToBytes().Should().Equal(original, "a rejected edit must not mutate anything");
            var reparsed = JsonGffDocument.Parse(document.ToBytes());
            reparsed.Root.Get("Bearing").GetSingle().Should().Be(1.5f,
                "the document must still serialize to a re-parseable file");
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        public void SetDouble_RejectsNonFiniteValues_AndLeavesTheDocumentUnchanged(double value)
        {
            var document = ParseDocument(FloatAndDoubleDocument);
            var original = document.ToBytes();

            var act = () => document.Root.Get("Precision").SetDouble(value);

            act.Should().Throw<ArgumentOutOfRangeException>();
            document.ToBytes().Should().Equal(original);
            JsonGffDocument.Parse(document.ToBytes()).Root.Get("Precision").GetDouble().Should().Be(2.5);
        }

        [Test]
        public void SetSingleExtension_RejectsNonFiniteValues_OnTheCreatePathToo()
        {
            var document = ParseDocument("{\"__data_type\":\"UTC \"}");
            var original = document.ToBytes();

            var act = () => document.Root.SetSingle("Bearing", float.NaN);

            act.Should().Throw<ArgumentOutOfRangeException>();
            document.Root.Contains("Bearing").Should().BeFalse("a rejected create must add nothing");
            document.ToBytes().Should().Equal(original);
        }

        [Test]
        public void VarTableSetFloat_RejectsNonFiniteValues()
        {
            var document = ParseDocument("{\"__data_type\":\"UTP \"}");
            var original = document.ToBytes();
            var varTable = new VarTable(document.Root);

            var act = () => varTable.SetFloat("MY_LOCAL", float.PositiveInfinity);

            act.Should().Throw<ArgumentOutOfRangeException>();
            document.Root.Contains("VarTable").Should().BeFalse();
            document.ToBytes().Should().Equal(original);
        }

        [Test]
        public void SchemaFieldAccessorSetFloat_RejectsNonFiniteValues()
        {
            var document = ParseDocument("{\"__data_type\":\"UTC \"}");
            var original = document.ToBytes();
            var descriptor = new FieldDescriptor
            {
                Label = "Bearing",
                FieldName = "Bearing",
                Kind = EditorKind.Float,
                FieldType = GffFieldType.Float
            };

            var act = () => SchemaFieldAccessor.SetFloat(document, descriptor, double.NaN);

            act.Should().Throw<ArgumentOutOfRangeException>();
            document.ToBytes().Should().Equal(original);
        }

        [Test]
        public void SchemaFieldAccessorSetFloat_RejectsDoublesThatOverflowTheFloatStorage()
        {
            // 1e39 is a finite double but narrows to float infinity, which would serialize as an
            // unparseable "inf" token in a float-typed field.
            var document = ParseDocument("{\"__data_type\":\"UTC \"}");
            var descriptor = new FieldDescriptor
            {
                Label = "Bearing",
                FieldName = "Bearing",
                Kind = EditorKind.Float,
                FieldType = GffFieldType.Float
            };

            var act = () => SchemaFieldAccessor.SetFloat(document, descriptor, 1e39);

            act.Should().Throw<ArgumentOutOfRangeException>();
            document.Root.Contains("Bearing").Should().BeFalse();
        }

        // ---------- root __struct_id ----------

        [Test]
        public void ARootLevelStructId_RoundTripsThroughSaveByteIdentically()
        {
            var json =
                "{\n" +
                "  \"__data_type\": \"UTC \",\n" +
                "  \"__struct_id\": 4294967295,\n" +
                "  \"Tag\": {\n" +
                "    \"type\": \"cexostring\",\n" +
                "    \"value\": \"probe\"\n" +
                "  }\n" +
                "}\n";
            var original = Encoding.UTF8.GetBytes(json);

            var document = JsonGffDocument.Parse(original);
            document.Root.StructId.Should().Be(uint.MaxValue);

            document.ToBytes().Should().Equal(original,
                "the writer must emit the root __struct_id it parsed instead of silently dropping it");

            var reparsed = JsonGffDocument.Parse(document.ToBytes());
            reparsed.Root.StructId.Should().Be(uint.MaxValue);
        }

        // ---------- void fields fail closed on string accessors ----------

        private const string VoidDocument =
            "{\"__data_type\":\"UTC \",\"Payload\":{\"type\":\"void\",\"value\":\"\\u0000\\u0001abc\"}}";

        [Test]
        public void GetString_OnAVoidField_Throws()
        {
            var document = ParseDocument(VoidDocument);

            var act = () => document.Root.Get("Payload").GetString();

            act.Should().Throw<InvalidOperationException>(
                "void payloads are binary and must not be transcoded through the text heuristic");
        }

        [Test]
        public void SetString_OnAVoidField_Throws_AndLeavesThePayloadIntact()
        {
            var document = ParseDocument(VoidDocument);
            var original = document.ToBytes();

            var act = () => document.Root.Get("Payload").SetString("oops");

            act.Should().Throw<InvalidOperationException>();
            document.ToBytes().Should().Equal(original);
        }

        [Test]
        public void SetString_CreatingAnAbsentVoidField_Throws_AndAddsNothing()
        {
            // The existing-field setter rejects Void; the create path must too, or a caller could
            // smuggle a text-encoded payload past the binary codec only when the field is absent.
            var document = ParseDocument("{\"__data_type\":\"UTC \"}");
            var original = document.ToBytes();

            var act = () => document.Root.SetString("Payload", GffFieldType.Void, "oops");

            act.Should().Throw<ArgumentException>();
            document.Root.GetOrNull("Payload").Should().BeNull();
            document.ToBytes().Should().Equal(original);
        }

        [Test]
        public void AVoidPayload_RemainsReachableThroughTheLosslessByteCodec()
        {
            var document = ParseDocument(VoidDocument);

            var bytes = JsonStringCodec.DecodeToBytes(document.Root.Get("Payload").RawValue!);

            bytes.Should().Equal(0x00, 0x01, (byte)'a', (byte)'b', (byte)'c');
        }

        // ---------- GetIntOrNull does not silently wrap Dword sentinels ----------

        [Test]
        public void GetIntOrNull_ThrowsOnADwordValueOutsideIntRange_InsteadOfWrappingToNegative()
        {
            var document = ParseDocument(
                "{\"__data_type\":\"UTC \",\"Sentinel\":{\"type\":\"dword\",\"value\":4294967295}}");

            var act = () => document.Root.GetIntOrNull("Sentinel");

            act.Should().Throw<OverflowException>(
                "0xFFFFFFFF must not silently read as -1 through the signed accessor");
            document.Root.GetUIntOrNull("Sentinel").Should().Be(uint.MaxValue,
                "the unsigned accessor is the correct path for Dword sentinels");
        }

        [Test]
        public void GetIntOrNull_StillReadsOrdinaryValues()
        {
            var document = ParseDocument(
                "{\"__data_type\":\"UTC \",\"Small\":{\"type\":\"dword\",\"value\":7}}");

            document.Root.GetIntOrNull("Small").Should().Be(7);
            document.Root.GetIntOrNull("Absent").Should().BeNull();
        }

        // ---------- LocString.CopyFrom participates in transactions ----------

        [Test]
        public void DuplicatingADialogLine_InsideATransaction_UndoesToTheExactOriginalBytes()
        {
            // DuplicateNode copies the line's localized text via LocString.CopyFrom; that copy
            // must flow through the edit scope like every other mutation so undo restores the
            // document byte-for-byte.
            var path = Path.Combine(CorpusLocator.ModuleDirectory, "dlg", "dantherbs.dlg.json");
            var original = File.ReadAllBytes(path);
            var dialog = DlgDocument.Parse(original);

            using var session = new DocumentSession(path, dialog.Document);

            using (var tx = session.Begin("duplicate line"))
            {
                dialog.DuplicateNode(dialog.Entries[0]);
            }

            dialog.ToBytes().AsSpan().SequenceEqual(original).Should().BeFalse();

            session.UndoStack.Undo();
            dialog.ToBytes().AsSpan().SequenceEqual(original).Should().BeTrue(
                "undoing the duplication must restore the exact original bytes");
        }
    }
}
