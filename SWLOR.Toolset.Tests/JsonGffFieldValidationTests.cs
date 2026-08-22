using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests
{
    public class JsonGffFieldValidationTests
    {
        [TestCase(GffFieldType.Byte, 0L, 255L)]
        [TestCase(GffFieldType.Char, -128L, 127L)]
        [TestCase(GffFieldType.Word, 0L, 65535L)]
        [TestCase(GffFieldType.Short, -32768L, 32767L)]
        [TestCase(GffFieldType.Dword, 0L, 4294967295L)]
        [TestCase(GffFieldType.Int, -2147483648L, 2147483647L)]
        [TestCase(GffFieldType.Dword64, 0L, long.MaxValue)]
        [TestCase(GffFieldType.Int64, long.MinValue, long.MaxValue)]
        public void SetInteger_AcceptsDeclaredTypeBoundaries(GffFieldType type, long minimum, long maximum)
        {
            var field = IntegerField(type);

            field.SetInteger(minimum);
            field.GetInteger().Should().Be(minimum);
            field.SetInteger(maximum);
            field.GetInteger().Should().Be(maximum);
        }

        [TestCase(GffFieldType.Byte, -1L)]
        [TestCase(GffFieldType.Byte, 256L)]
        [TestCase(GffFieldType.Char, -129L)]
        [TestCase(GffFieldType.Char, 128L)]
        [TestCase(GffFieldType.Word, -1L)]
        [TestCase(GffFieldType.Word, 65536L)]
        [TestCase(GffFieldType.Short, -32769L)]
        [TestCase(GffFieldType.Short, 32768L)]
        [TestCase(GffFieldType.Dword, -1L)]
        [TestCase(GffFieldType.Dword, 4294967296L)]
        [TestCase(GffFieldType.Int, -2147483649L)]
        [TestCase(GffFieldType.Int, 2147483648L)]
        [TestCase(GffFieldType.Dword64, -1L)]
        public void SetInteger_RejectsValuesOutsideDeclaredStorage(GffFieldType type, long value)
        {
            var field = IntegerField(type);

            var act = () => field.SetInteger(value);

            act.Should().Throw<ArgumentOutOfRangeException>();
            field.GetInteger().Should().Be(0, "a rejected value must not mutate the field");
        }

        [Test]
        public void SetUnsignedInteger_EnforcesSignedAndUnsigned64BitRanges()
        {
            var unsigned = IntegerField(GffFieldType.Dword64);
            unsigned.SetUnsignedInteger(ulong.MaxValue);
            unsigned.GetUnsignedInteger().Should().Be(ulong.MaxValue);

            var signed = IntegerField(GffFieldType.Int64);
            var act = () => signed.SetUnsignedInteger((ulong)long.MaxValue + 1UL);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestCase("")]
        [TestCase("valid_resref_123")]
        [TestCase("MixedCase")]
        public void SetString_AcceptsValidResRefs(string value)
        {
            var field = JsonGffField.CreateScalar(GffFieldType.ResRef, JsonStringCodec.Encode(string.Empty));

            field.SetString(value);

            field.GetString().Should().Be(value);
        }

        [TestCase("seventeen_chars_x")]
        [TestCase("has space")]
        [TestCase("bad-dash")]
        [TestCase("bad.dot")]
        [TestCase("nonascii_\u00E9")]
        public void SetString_RejectsInvalidResRefsWithoutMutation(string value)
        {
            var field = JsonGffField.CreateScalar(GffFieldType.ResRef, JsonStringCodec.Encode("original"));

            var act = () => field.SetString(value);

            act.Should().Throw<ArgumentException>();
            field.GetString().Should().Be("original");
        }

        [Test]
        public void SchemaFieldAccessor_ValidatesNewResRefBeforeAddingField()
        {
            var document = new JsonGffDocument("UTC ", new JsonGffStruct());
            var descriptor = new FieldDescriptor
            {
                Label = "On Used",
                FieldName = "OnUsed",
                Kind = EditorKind.ScriptSlot,
                FieldType = GffFieldType.ResRef
            };

            var act = () => SchemaFieldAccessor.SetText(document, descriptor, "not valid");

            act.Should().Throw<ArgumentException>();
            document.Root.Contains("OnUsed").Should().BeFalse();
        }

        private static JsonGffField IntegerField(GffFieldType type) =>
            JsonGffField.CreateScalar(type, "0"u8.ToArray());
    }
}
