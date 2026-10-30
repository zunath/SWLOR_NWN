using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

internal sealed partial class StoredObjectData
{
    private static readonly string[] RenamedTextFields = { "LocalizedName", "DescIdentified", "Description" };

    /// <summary>
    /// Mirrors <see cref="FurnitureRenameMigration.RenameItem"/>: the saved name, both descriptions, and the original
    /// name a DM rename keeps for its Reset action.
    /// </summary>
    internal static bool RenameFurnitureItem(Node item)
    {
        var resref = ReadResRef(item);
        var changed = false;
        foreach (var label in RenamedTextFields)
        {
            var field = item.Fields.SingleOrDefault(x => x.Name == label && x.Type == 12);
            changed |= ReplaceLocalizedText(field, text => FurnitureRenameMigration.GetRenamedItemText(resref, text));
        }
        var originalName = FindLocal(item, RenameTargetViewModel.RenamedItemOriginalName, 3);
        if (originalName != null)
        {
            var text = ReadText(originalName);
            var renamed = FurnitureRenameMigration.GetRenamedItemText(resref, text);
            if (renamed != text)
            {
                WriteText(originalName, renamed);
                changed = true;
            }
        }
        return changed;
    }

    /// <summary>Rewrites each language's text of a saved CExoLocString, keeping its string reference.</summary>
    private static bool ReplaceLocalizedText(Field field, Func<string, string> replace)
    {
        if (field == null) return false;
        var changed = false;
        using var reader = new BinaryReader(new MemoryStream(field.Data), Encoding.UTF8);
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output, Encoding.UTF8, true);
        writer.Write(reader.ReadUInt32()); // Payload length, fixed below.
        writer.Write(reader.ReadUInt32()); // String reference.
        var count = reader.ReadUInt32();
        writer.Write(count);
        for (var i = 0u; i < count; i++)
        {
            writer.Write(reader.ReadUInt32()); // Language/gender ID.
            var bytes = reader.ReadBytes(checked((int)reader.ReadUInt32()));
            var text = Encoding.UTF8.GetString(bytes);
            var replacement = replace(text);
            changed |= text != replacement;
            var result = text == replacement ? bytes : Encoding.UTF8.GetBytes(replacement);
            writer.Write(result.Length);
            writer.Write(result);
        }
        if (!changed) return false;
        field.Data = output.ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(field.Data, field.Data.Length - 4);
        return true;
    }
}
