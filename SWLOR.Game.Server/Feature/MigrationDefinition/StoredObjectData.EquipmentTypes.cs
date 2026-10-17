using System;
using System.Linq;
using System.Text;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

internal sealed partial class StoredObjectData
{
    internal static (InventorySlot Previous, InventorySlot Target)? GetEquipmentSlotCorrection(string data)
    {
        if (string.IsNullOrWhiteSpace(data)) return null;
        var root = new StoredObjectData(Convert.FromBase64String(data))._root;
        var resref = root.Fields.SingleOrDefault(x => x.Name == "TemplateResRef" && x.Type == 11);
        var baseItem = root.Fields.SingleOrDefault(x => x.Name == "BaseItem" && x.Type == 5);
        if (resref == null || baseItem == null) return null;
        var name = Encoding.ASCII.GetString(resref.Data.AsSpan(1));
        if (!EquipmentTypeMigration.IsLegacyEquipment(name, (BaseItem)ReadScalar(baseItem))) return null;
        var correction = EquipmentTypeMigration.Corrections[name];
        return (EquipmentTypeMigration.GetLegacySlot(correction.LegacyType), correction.Slot);
    }
}
