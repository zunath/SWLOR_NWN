using SWLOR.NWN.API.NWNX;
using NWN.Native.API;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.Bioware;
using DurationType = SWLOR.NWN.API.NWScript.Enum.DurationType;
using ItemProperty = SWLOR.NWN.API.Engine.ItemProperty;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    internal static class MigrationObject
    {
        /// <summary>
        /// Newly expanded blueprints have their own identity and inventory slot;
        /// they must not inherit the original item's temporary preservation markers.
        /// </summary>
        public static void ClearCopiedInventoryMarkers(uint item)
        {
            for (var index = ObjectPlugin.GetLocalVariableCount(item) - 1; index >= 0; index--)
            {
                var variable = ObjectPlugin.GetLocalVariable(item, index);
                if (variable.Key.StartsWith(StoredObjectData.IdentityMarkerPrefix, StringComparison.Ordinal) ||
                    variable.Key.StartsWith(StoredObjectData.EquipmentMarkerPrefix, StringComparison.Ordinal))
                    DeleteLocalInt(item, variable.Key);
            }
        }

        /// <summary>
        /// Releases temporary inventory UUID registrations immediately, then schedules native destruction after the current script.
        /// </summary>
        public static void DestroyTemporaryObject(uint obj)
        {
            var visited = new HashSet<uint>();
            void ReleaseIdentities(uint target)
            {
                if (!GetIsObjectValid(target) || !visited.Add(target)) return;
                if (GetHasInventory(target))
                    for (var item = GetFirstItemInInventory(target); GetIsObjectValid(item); item = GetNextItemInInventory(target))
                        ReleaseIdentities(item);
                if (GetObjectType(target) == ObjectType.Creature)
                    for (var slot = 0; slot < NumberOfInventorySlots; slot++)
                        ReleaseIdentities(GetItemInSlot((InventorySlot)slot, target));

                // Destruction is deferred until the startup script returns. The
                // saved data has already captured these UUIDs; release their
                // native registrations now so later archived copies can load
                // the same original identities without losing them on save.
                if (!string.IsNullOrEmpty(ObjectPlugin.PeekUUID(target)))
                    NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(target).AsNWSObject().m_pUUID.AssignRandom();
            }
            ReleaseIdentities(obj);
            DestroyObject(obj);
        }

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
            // The script command queues removal until a later engine update. A
            // subsequent migration can replace the same property before that
            // event runs, causing the replacement to be removed instead.
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var nativeItem = server.GetGameObject(item)?.AsNWSItem()
                ?? throw new InvalidOperationException("A migration item was lost before property removal.");
            var id = ItemPropertyPlugin.UnpackIP(property).Id;
            var propertyId = ulong.Parse(id);
            // Saved properties and backing effects have different IDs. The
            // source ID is the native link between them, including after load.
            var effectIds = new List<ulong>();
            foreach (var effect in nativeItem.m_appliedEffects)
                if (effect.m_nItemPropertySourceId == propertyId)
                    effectIds.Add(effect.m_nID);
            foreach (var effectId in effectIds)
                nativeItem.RemoveEffectById(effectId);
            var timer = server.GetActiveTimer(item);
            nativeItem.UpdateEffectList(timer.GetWorldTimeCalendarDay(), timer.GetWorldTimeTimeOfDay());
            foreach (var effect in nativeItem.m_appliedEffects)
                if (effect.m_nItemPropertySourceId == propertyId)
                    throw new InvalidOperationException($"Could not remove the backing effect for migration item property {id}.");

            // Deserialized permanent properties also live in the item's saved
            // property lists. Remove any entry left behind by effect cleanup.
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

        /// <summary>
        /// Serializes the migrated object while preserving the original root identity when an archive payload is supplied.
        /// </summary>
        public static string Serialize(uint obj, string originalData = null)
        {
            if (!GetIsObjectValid(obj))
                throw new InvalidOperationException("A migration object was lost before it could be saved.");

            var data = ObjectPlugin.Serialize(obj);
            if (string.IsNullOrWhiteSpace(data))
                throw new InvalidOperationException("A migration object could not be serialized.");

            return originalData == null ? data : StoredObjectData.PreserveRootIdentity(originalData, data);
        }
    }
}
