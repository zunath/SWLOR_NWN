using System.Text;
using System.Runtime.ExceptionServices;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.NWN.Formats.Tests;

public class TwoDAReaderTests
{
    [Test]
    public void TextReader_PreservesLabelsDefaultsQuotedCellsAndNulls()
    {
        var bytes = Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(
                "2DA V2.0\r\n\r\nDEFAULT: \"fallback value\"\r\nLABEL VALUE NOTE\r\n" +
                "10 chicken 7 \"two words\"\r\n20 **** **** \"\"\r\n"))
            .ToArray();

        var table = TwoDAReader.Read(bytes);

        table.Columns.Should().Equal("LABEL", "VALUE", "NOTE");
        table.RowLabels.Should().Equal("10", "20");
        table.RowCount.Should().Be(2);
        table.GetValue(0, "note").Should().Be("two words");
        table.GetValue(1, "LABEL").Should().BeNull();
        table.GetValue(1, "NOTE").Should().BeEmpty();
        table.GetValue(100, "NOTE").Should().Be("fallback value");
    }

    [Test, NonParallelizable]
    public void LegacyTextDetection_DoesNotThrowAFirstChanceDecoderException()
    {
        var bytes = Encoding.ASCII.GetBytes("2DA V2.0\n\nVALUE\n0 caf")
            .Concat(new byte[] { 0xE9, (byte)'\n' })
            .ToArray();
        var decoderExceptions = 0;
        EventHandler<FirstChanceExceptionEventArgs> observe = (_, args) =>
        {
            if (args.Exception is DecoderFallbackException)
                decoderExceptions++;
        };

        AppDomain.CurrentDomain.FirstChanceException += observe;
        try
        {
            TwoDAReader.Read(bytes).GetValue(0, "VALUE").Should().Be("café");
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= observe;
        }

        decoderExceptions.Should().Be(0,
            "legacy game data is normal input and must not fill the debugger console with caught exceptions");
    }

    [Test]
    public void BinaryReader_UsesRowMajorOffsets()
    {
        var table = TwoDAReader.Read(BuildBinaryTwoDa());

        table.Columns.Should().Equal("LABEL", "VALUE");
        table.RowLabels.Should().Equal("rowA", "rowB");
        table.GetValue(0, "LABEL").Should().Be("alpha");
        table.GetValue(0, "VALUE").Should().Be("7");
        table.GetValue(1, "LABEL").Should().BeNull();
        table.GetValue(1, "VALUE").Should().Be("beta");
    }

    [Test]
    public void WrongVersionAndTruncatedBinary_AreRejected()
    {
        var wrong = Encoding.ASCII.GetBytes("2DA V9.9\n");
        Action wrongAction = () => TwoDAReader.Read(wrong);
        wrongAction.Should().Throw<NwnFormatException>();

        var truncated = BuildBinaryTwoDa()[..^1];
        Action truncatedAction = () => TwoDAReader.Read(truncated);
        truncatedAction.Should().Throw<NwnFormatException>();
    }

    [Test]
    public void TextReader_PadsMissingCellsAndKeepsUnquotedFinalColumnPhrases()
    {
        var bytes = Encoding.ASCII.GetBytes(
            "2DA V2.0\n\nFirst Second\n0 alpha a phrase with spaces\n1 beta\n");

        var file = TwoDAReader.Read(bytes);

        file.GetValue(0, "First").Should().Be("alpha");
        file.GetValue(0, "Second").Should().Be("a phrase with spaces");
        file.GetValue(1, "First").Should().Be("beta");
        file.GetValue(1, "Second").Should().BeNull();
    }

    [Test]
    public void TextReader_RejectsTablesWhoseRowColumnProductExceedsTheCellBudget()
    {
        const int columns = 16_384;
        const int rows = 1_954;
        var text = new StringBuilder("2DA V2.0\n\n");
        for (var column = 0; column < columns; column++)
            text.Append('C').Append(column).Append(column == columns - 1 ? '\n' : ' ');
        for (var row = 0; row < rows; row++)
            text.Append(row).Append('\n');

        Action action = () => TwoDAReader.Read(Encoding.ASCII.GetBytes(text.ToString()));

        action.Should().Throw<NwnFormatException>()
            .WithMessage("*cell count*");
    }

    [TestCase("LABEL\t\t", "empty")]
    [TestCase("LABEL\tlabel\t", "unique")]
    public void BinaryReader_RejectsInvalidColumnNamesAsFormatErrors(string columnBytes, string message)
    {
        Action action = () => TwoDAReader.Read(BuildBinaryTwoDa(columnBytes));

        action.Should().Throw<NwnFormatException>()
            .WithMessage($"*{message}*");
    }

    private static byte[] BuildBinaryTwoDa()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write("2DA V2.b\n"u8);
        writer.Write(Encoding.ASCII.GetBytes("LABEL\tVALUE\t"));
        writer.Write((byte)0);
        writer.Write(2u);
        writer.Write(Encoding.ASCII.GetBytes("rowA\trowB\t"));

        var strings = Encoding.ASCII.GetBytes("alpha\0" + "7\0" + "****\0" + "beta\0");
        writer.Write((ushort)0);
        writer.Write((ushort)6);
        writer.Write((ushort)8);
        writer.Write((ushort)13);
        // The declared string-data section size - not padding; every cell offset must land in it.
        writer.Write((ushort)strings.Length);
        writer.Write(strings);
        return stream.ToArray();
    }

    [Test]
    public void BinaryReader_RejectsCellOffsetsOutsideTheDeclaredDataSection()
    {
        var bytes = BuildBinaryTwoDa();
        // Shrink the declared data-section size below the last cell offset (13): the reader must
        // refuse rather than read trailing bytes beyond the declared window as cell text.
        var declaredSizeOffset = bytes.Length - Encoding.ASCII.GetBytes(
            "alpha\0" + "7\0" + "****\0" + "beta\0").Length - 2;
        bytes[declaredSizeOffset] = 10;
        bytes[declaredSizeOffset + 1] = 0;

        Action action = () => TwoDAReader.Read(bytes);

        action.Should().Throw<NwnFormatException>()
            .WithMessage("*declared*data*section*");
    }

    private static byte[] BuildBinaryTwoDa(string columnBytes)
    {
        var columnCount = columnBytes.Count(character => character == '\t');
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write("2DA V2.b\n"u8);
        writer.Write(Encoding.ASCII.GetBytes(columnBytes));
        writer.Write((byte)0);
        writer.Write(1u);
        writer.Write(Encoding.ASCII.GetBytes("row\t"));
        for (var column = 0; column < columnCount; column++)
            writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write((byte)0);
        return stream.ToArray();
    }
}
