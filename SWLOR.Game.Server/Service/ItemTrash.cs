using System.Collections.Generic;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static class ItemTrash
    {
        private const string AuditedVariable = "ITEM_TRASH_AUDITED";

        public static void Audit(uint player, uint item, string source)
        {
            if (!GetIsObjectValid(item) || GetObjectType(item) != ObjectType.Item)
                return;

            if (GetIsObjectValid(player))
                player = DMPresence.GetDM(player);

            try
            {
                AuditItem(player, item, OBJECT_INVALID, source, new HashSet<uint>());
            }
            catch (Exception ex)
            {
                Log.WriteError(ex,
                    "Item trash audit failed: PlayerId={PlayerId} ItemId={ItemId} Source={Source}",
                    GetIsObjectValid(player) ? GetObjectUUID(player) : string.Empty, GetObjectUUID(item), source);
            }
        }

        private static void AuditItem(uint player, uint item, uint parentItem, string source, HashSet<uint> visited)
        {
            if (!visited.Add(item) || GetLocalBool(item, AuditedVariable))
                return;

            var properties = new List<object>();
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(property);
                properties.Add(new
                {
                    Type = (int)type,
                    TypeName = type.ToString(),
                    SubType = GetItemPropertySubType(property),
                    CostTable = GetItemPropertyCostTable(property),
                    CostTableValue = GetItemPropertyCostTableValue(property),
                    Param1 = GetItemPropertyParam1(property),
                    Param1Value = GetItemPropertyParam1Value(property),
                    DurationType = (int)GetItemPropertyDurationType(property),
                    DurationSeconds = GetItemPropertyDuration(property),
                    RemainingSeconds = GetItemPropertyDurationRemaining(property),
                    UsesPerDayRemaining = GetItemPropertyUsesPerDayRemaining(item, property.Handle),
                    Tag = GetItemPropertyTag(property)
                });
            }

            var validPlayer = GetIsObjectValid(player);
            var serializedItem = string.Empty;
            try
            {
                serializedItem = ObjectPlugin.Serialize(item);
            }
            catch (Exception ex)
            {
                Log.WriteError(ex, "Trashed item snapshot failed: ItemId={ItemId} Source={Source}",
                    GetObjectUUID(item), source);
            }

            Log.WriteStructured(LogGroup.ItemTrash,
                "Item trashed: PlayerId={PlayerId} PlayerName={PlayerName} IdentityKey={IdentityKey} AccountName={AccountName} PublicCDKey={PublicCDKey} Source={Source} ItemId={ItemId} ParentItemId={ParentItemId} ItemName={ItemName} Resref={Resref} Tag={Tag} BaseItemType={BaseItemType} Quantity={Quantity} Charges={Charges} PropertyDescriptions={PropertyDescriptions} Properties={Properties} SnapshotComplete={SnapshotComplete} SerializedItem={SerializedItem}",
                validPlayer ? GetObjectUUID(player) : string.Empty,
                validPlayer ? PlayerName.GetAuditName(player) : "Unknown player",
                validPlayer ? Disguise.GetIdentityKey(player) : string.Empty,
                validPlayer ? GetPCPlayerName(player) : string.Empty,
                validPlayer ? GetPCPublicCDKey(player) : string.Empty,
                source,
                GetObjectUUID(item),
                GetIsObjectValid(parentItem) ? GetObjectUUID(parentItem) : string.Empty,
                JsonConvert.SerializeObject(GetName(item)),
                GetResRef(item),
                JsonConvert.SerializeObject(GetTag(item)),
                (int)GetBaseItemType(item),
                GetItemStackSize(item),
                GetItemCharges(item),
                JsonConvert.SerializeObject(GetPropertyDescriptions(item)),
                JsonConvert.SerializeObject(properties),
                !string.IsNullOrWhiteSpace(serializedItem),
                serializedItem);

            SetLocalBool(item, AuditedVariable, true);

            for (var child = GetFirstItemInInventory(item); GetIsObjectValid(child); child = GetNextItemInInventory(item))
            {
                AuditItem(player, child, item, source, visited);
            }
        }

        private static string GetPropertyDescriptions(uint item)
        {
            try
            {
                return Item.BuildItemPropertyString(item);
            }
            catch (Exception ex)
            {
                return $"Unavailable ({ex.Message}); see raw Properties.";
            }
        }
    }
}
