using SWLOR.NWN.API.NWNX;
using NWN.Native.API;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.Bioware;
using DurationType = SWLOR.NWN.API.NWScript.Enum.DurationType;
using ItemProperty = SWLOR.NWN.API.Engine.ItemProperty;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    internal static class MigrationObject
    {
        public static void AddProperty(uint item, ItemProperty property, AddItemPropertyPolicy policy)
        {
            if (!GetIsItemPropertyValid(property))
                throw new InvalidOperationException("Could not construct a replacement migration item property.");

            if (policy == AddItemPropertyPolicy.ReplaceExisting)
            {
                var matching = new List<ItemProperty>();
                for (var existing = GetFirstItemProperty(item); GetIsItemPropertyValid(existing); existing = GetNextItemProperty(item))
                    if (GetItemPropertyType(existing) == GetItemPropertyType(property) &&
                        GetItemPropertySubType(existing) == GetItemPropertySubType(property) &&
                        GetItemPropertyDurationType(existing) == DurationType.Permanent)
                        matching.Add(existing);

                foreach (var existing in matching)
                    RemoveProperty(item, existing);
            }

            BiowareXP2.IPSafeAddItemProperty(item, property, 0f,
                policy == AddItemPropertyPolicy.ReplaceExisting ? AddItemPropertyPolicy.IgnoreExisting : policy,
                false, false);
        }

        public static void RemoveProperty(uint item, ItemProperty property)
        {
            // NWScript marks the backing effect for removal. Flush the item's effect
            // list now so subsequent migration steps and serialization see it gone.
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var nativeItem = server.GetGameObject(item)?.AsNWSItem()
                ?? throw new InvalidOperationException("A migration item was lost before property removal.");
            var id = ItemPropertyPlugin.UnpackIP(property).Id;
            RemoveItemProperty(item, property);
            var timer = server.GetActiveTimer(item);
            nativeItem.UpdateEffectList(timer.GetWorldTimeCalendarDay(), timer.GetWorldTimeTimeOfDay());

            // Deserialized permanent properties also live in the item's saved
            // property lists. Remove any entry left behind by effect cleanup.
            var propertyId = ulong.Parse(id);
            var wearer = server.GetGameObject(nativeItem.m_oidPossessor)?.AsNWSCreature();
            void RemoveEquippedEffect(CNWItemProperty savedProperty)
            {
                if (wearer?.m_pInventory.GetItemInInventory(nativeItem) != 1)
                    return;
                var slot = wearer.m_pInventory.GetSlotFromItem(nativeItem);
                server.GetServerAIMaster().OnItemPropertyRemoved(nativeItem, savedProperty, wearer, slot);
            }

            for (var index = nativeItem.m_lstActiveProperties.Count - 1; index >= 0; index--)
                if (nativeItem.GetActiveProperty(index).m_nID == propertyId)
                {
                    RemoveEquippedEffect(nativeItem.GetActiveProperty(index));
                    nativeItem.RemoveActiveProperty(index);
                }
            for (var index = nativeItem.m_lstPassiveProperties.Count - 1; index >= 0; index--)
                if (nativeItem.GetPassiveProperty(index).m_nID == propertyId)
                {
                    RemoveEquippedEffect(nativeItem.GetPassiveProperty(index));
                    nativeItem.RemovePassiveProperty(index);
                }

            for (var remaining = GetFirstItemProperty(item); GetIsItemPropertyValid(remaining); remaining = GetNextItemProperty(item))
                if (ItemPropertyPlugin.UnpackIP(remaining).Id == id)
                    throw new InvalidOperationException($"Could not remove migration item property {id}.");
        }

        public static uint Deserialize(string data)
        {
            var obj = ObjectPlugin.Deserialize(data);
            if (!GetIsObjectValid(obj))
                throw new InvalidOperationException("A stored migration object could not be deserialized.");

            return obj;
        }

        public static string Serialize(uint obj)
        {
            if (!GetIsObjectValid(obj))
                throw new InvalidOperationException("A migration object was lost before it could be saved.");

            var data = ObjectPlugin.Serialize(obj);
            if (string.IsNullOrWhiteSpace(data))
                throw new InvalidOperationException("A migration object could not be serialized.");

            return data;
        }
    }
}
