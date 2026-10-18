using System;
using System.Buffers.Binary;
using System.Linq;
using System.Text;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>Saved-character helpers for items moved out of a character file.</summary>
internal sealed partial class StoredObjectData
{
    private static readonly byte[] ItemHeader = Encoding.ASCII.GetBytes("UTI V3.2");

    private StoredObjectData(byte[] header, Node root)
    {
        _header = header;
        _root = root;
    }

    /// <summary>
    /// An item from a character's inventory as a standalone saved item (UTI, base64), the form item storage
    /// keeps: the same fields without its inventory position, as the root structure.
    /// </summary>
    internal static string ItemToUti(Node item)
    {
        var root = new Node
        {
            Type = uint.MaxValue,
            Fields = item.Fields.Where(x => x.Name is not ("Repos_PosX" or "Repos_Posy")).ToList(),
        };
        return new StoredObjectData(ItemHeader.ToArray(), root).Serialize();
    }

    /// <summary>The root structure of a saved item (UTI, base64), to insert into an inventory.</summary>
    internal static Node ParseItem(string data) => new StoredObjectData(Convert.FromBase64String(data))._root;

    /// <summary>
    /// Replaces the text of every saved string in an item's localized name, keeping its string reference and
    /// language IDs; a name without saved strings gains one for language 0.
    /// </summary>
    internal static void WriteLocalizedName(Node item, string name)
    {
        var field = item.Fields.SingleOrDefault(x => x.Name == "LocalizedName" && x.Type == 12);
        var reference = field == null || field.Data.Length < 8 ? uint.MaxValue : BinaryPrimitives.ReadUInt32LittleEndian(field.Data.AsSpan(4));
        var languages = new System.Collections.Generic.List<uint>();
        if (field != null && field.Data.Length >= 12)
        {
            var count = BinaryPrimitives.ReadUInt32LittleEndian(field.Data.AsSpan(8));
            var offset = 12;
            for (var index = 0u; index < count && offset + 8 <= field.Data.Length; index++)
            {
                languages.Add(BinaryPrimitives.ReadUInt32LittleEndian(field.Data.AsSpan(offset)));
                offset += 8 + BinaryPrimitives.ReadInt32LittleEndian(field.Data.AsSpan(offset + 4));
            }
        }
        if (languages.Count == 0)
            languages.Add(0);
        var text = Encoding.UTF8.GetBytes(name);
        using var output = new System.IO.MemoryStream();
        using var writer = new System.IO.BinaryWriter(output);
        writer.Write(0u);
        writer.Write(reference);
        writer.Write((uint)languages.Count);
        foreach (var language in languages)
        {
            writer.Write(language);
            writer.Write(text.Length);
            writer.Write(text);
        }
        writer.Flush();
        var data = output.ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(data, data.Length - 4);
        if (field == null)
            item.Fields.Add(MakeField(12, "LocalizedName", data));
        else
            field.Data = data;
    }

    /// <summary>The string reference of an item's localized name; 0xFFFFFFFF when it has none.</summary>
    internal static uint ReadLocalizedNameReference(Node item)
    {
        var field = item.Fields.SingleOrDefault(x => x.Name == "LocalizedName" && x.Type == 12);
        return field == null || field.Data.Length < 8 ? uint.MaxValue : BinaryPrimitives.ReadUInt32LittleEndian(field.Data.AsSpan(4));
    }

    /// <summary>The first saved string of an item's localized name, or empty when it uses only a string reference.</summary>
    internal static string ReadLocalizedName(Node item)
    {
        var field = item.Fields.SingleOrDefault(x => x.Name == "LocalizedName" && x.Type == 12);
        if (field == null || field.Data.Length < 12)
            return string.Empty;
        var count = BinaryPrimitives.ReadUInt32LittleEndian(field.Data.AsSpan(8));
        if (count == 0 || field.Data.Length < 20)
            return string.Empty;
        var length = BinaryPrimitives.ReadInt32LittleEndian(field.Data.AsSpan(16));
        return Encoding.UTF8.GetString(field.Data, 20, Math.Min(length, field.Data.Length - 20));
    }
}
