using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

internal sealed partial class StoredObjectData
{
    /// <summary>Moves an enzyme still on its 2x2 base item to <see cref="BaseItem.Enzyme"/> with its new icon model.</summary>
    internal static bool ConvertEnzyme(Node item)
    {
        if (!EnzymeBaseItemMigration.TryGetConversion(ReadResRef(item), ReadBaseItem(item), out var model))
            return false;
        WriteInteger(item, "BaseItem", (int)BaseItem.Enzyme);
        WriteSimpleModel(item, model);
        return true;
    }
}
