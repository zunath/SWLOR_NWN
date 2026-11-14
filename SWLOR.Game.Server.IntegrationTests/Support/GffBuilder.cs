using System.Text;

namespace SWLOR.Game.Server.IntegrationTests.Support;

/// <summary>
/// Writes a small GFF V3.2 file, the binary format the engine serializes items in (<c>InventoryItem.Data</c> holds one as
/// base64). Only the field kinds a stored item needs are supported: byte, word, int, string, resref and list. Used to seed
/// older-shape stored items for the migration tests.
/// </summary>
public static class GffBuilder
{
    public sealed class Struct
    {
        public uint Type { get; init; }

        public List<Field> Fields { get; } = new();

        public Struct Byte(string label, int value) => Add(0, label, BitConverter.GetBytes((uint)value));

        public Struct Word(string label, int value) => Add(2, label, BitConverter.GetBytes((uint)value));

        public Struct Int(string label, int value) => Add(5, label, BitConverter.GetBytes(value));

        public Struct Text(string label, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            return Add(10, label, BitConverter.GetBytes(bytes.Length).Concat(bytes).ToArray(), complex: true);
        }

        public Struct ResRef(string label, string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            return Add(11, label, new[] { (byte)bytes.Length }.Concat(bytes).ToArray(), complex: true);
        }

        public Struct List(string label, params Struct[] elements)
        {
            Fields.Add(new Field(15, label, Array.Empty<byte>(), false, elements));
            return this;
        }

        private Struct Add(uint type, string label, byte[] data, bool complex = false)
        {
            Fields.Add(new Field(type, label, data, complex, null));
            return this;
        }
    }

    public sealed record Field(uint Type, string Label, byte[] Data, bool Complex, Struct[]? Elements);

    /// <summary>A stored item as the engine saves it: the root struct of a UTI file.</summary>
    public static Struct Root() => new() { Type = uint.MaxValue };

    public static Struct Element(uint type = 0) => new() { Type = type };

    /// <summary>The base64 text the database stores.</summary>
    public static string Encode(Struct root, string fileType = "UTI ")
    {
        var structs = new List<Struct>();
        var fields = new List<Field>();
        var labels = new List<string>();
        var fieldData = new List<byte>();
        var fieldIndices = new List<uint>();
        var listIndices = new List<byte>();

        // Per struct: the field index (one field) or the byte offset into the field indices (several).
        var structRows = new List<(uint Type, uint DataOrOffset, uint FieldCount)>();
        var fieldRows = new List<(uint Type, uint Label, uint Data)>();

        uint Place(Struct item)
        {
            var index = (uint)structs.Count;
            structs.Add(item);
            structRows.Add(default);

            var ownFieldIndices = new List<uint>();
            foreach (var field in item.Fields)
            {
                var fieldIndex = (uint)fieldRows.Count;
                fieldRows.Add(default);
                ownFieldIndices.Add(fieldIndex);

                if (!labels.Contains(field.Label))
                    labels.Add(field.Label);
                var label = (uint)labels.IndexOf(field.Label);

                uint data;
                if (field.Type == 15)
                {
                    var children = field.Elements!.Select(Place).ToList();
                    data = (uint)listIndices.Count;
                    listIndices.AddRange(BitConverter.GetBytes((uint)children.Count));
                    foreach (var child in children)
                        listIndices.AddRange(BitConverter.GetBytes(child));
                }
                else if (field.Complex)
                {
                    data = (uint)fieldData.Count;
                    fieldData.AddRange(field.Data);
                }
                else
                {
                    data = BitConverter.ToUInt32(field.Data.Concat(new byte[4]).Take(4).ToArray());
                }

                fieldRows[(int)fieldIndex] = (field.Type, label, data);
            }

            uint dataOrOffset;
            if (ownFieldIndices.Count == 1)
            {
                dataOrOffset = ownFieldIndices[0];
            }
            else if (ownFieldIndices.Count == 0)
            {
                dataOrOffset = uint.MaxValue;
            }
            else
            {
                dataOrOffset = (uint)(fieldIndices.Count * 4);
                fieldIndices.AddRange(ownFieldIndices);
            }

            structRows[(int)index] = (item.Type, dataOrOffset, (uint)ownFieldIndices.Count);
            return index;
        }

        Place(root);

        const int HeaderSize = 56;
        var structOffset = (uint)HeaderSize;
        var fieldOffset = structOffset + (uint)structRows.Count * 12;
        var labelOffset = fieldOffset + (uint)fieldRows.Count * 12;
        var fieldDataOffset = labelOffset + (uint)labels.Count * 16;
        var fieldIndicesOffset = fieldDataOffset + (uint)fieldData.Count;
        var listIndicesOffset = fieldIndicesOffset + (uint)fieldIndices.Count * 4;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(Encoding.ASCII.GetBytes(fileType));
        writer.Write(Encoding.ASCII.GetBytes("V3.2"));
        foreach (var value in new[]
                 {
                     structOffset, (uint)structRows.Count, fieldOffset, (uint)fieldRows.Count, labelOffset, (uint)labels.Count,
                     fieldDataOffset, (uint)fieldData.Count, fieldIndicesOffset, (uint)fieldIndices.Count * 4,
                     listIndicesOffset, (uint)listIndices.Count,
                 })
            writer.Write(value);

        foreach (var (type, dataOrOffset, count) in structRows)
        {
            writer.Write(type == uint.MaxValue ? uint.MaxValue : type);
            writer.Write(dataOrOffset);
            writer.Write(count);
        }

        foreach (var (type, label, data) in fieldRows)
        {
            writer.Write(type);
            writer.Write(label);
            writer.Write(data);
        }

        foreach (var label in labels)
        {
            var bytes = new byte[16];
            Encoding.ASCII.GetBytes(label).CopyTo(bytes, 0);
            writer.Write(bytes);
        }

        writer.Write(fieldData.ToArray());
        foreach (var index in fieldIndices)
            writer.Write(index);
        writer.Write(listIndices.ToArray());
        writer.Flush();
        return Convert.ToBase64String(stream.ToArray());
    }
}
