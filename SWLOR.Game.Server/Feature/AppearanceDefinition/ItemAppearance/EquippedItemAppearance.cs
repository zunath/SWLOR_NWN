using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using ItemAppearanceType = SWLOR.NWN.API.NWScript.Enum.Item.ItemAppearanceType;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public static class EquippedItemAppearance
    {
        private const float QuickbarRefreshDelaySeconds = 0.2f;
        private const int DetachedHeadItemAttempts = 10;

        public static void Set(uint item, ItemAppearanceType type, int index, int value)
        {
            if (!GetIsObjectValid(item))
                return;

            var nativeItem = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(item)?.AsNWSItem();
            if (nativeItem == null)
                return;

            if (type == ItemAppearanceType.ArmorModel)
            {
                if (index < 0 || index >= 19 || value < 0 || value > ushort.MaxValue)
                    return;

                // NWNX's armor-model setter also recomputes armor class and weight.
                // A cosmetic edit changes only this model field and retains both stats.
                nativeItem.m_nArmorModelPart[index] = (ushort)value;
            }
            else
            {
                ItemPlugin.SetItemAppearance(item, type, index, value, updateCreatureAppearance: false);
            }
        }

        public static void Refresh(uint creature, uint item, bool resetShaderOverrides = true)
        {
            if (!GetIsObjectValid(creature) || !GetIsObjectValid(item))
                return;

            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var nativeCreature = server.GetGameObject(creature)?.AsNWSCreature();
            var nativeItem = server.GetGameObject(item)?.AsNWSItem();
            if (nativeCreature == null || nativeItem == null ||
                nativeItem.m_oidPossessor != creature ||
                nativeCreature.m_pInventory.GetItemInInventory(nativeItem) == 0)
                return;

            RobeAppearance.RemoveUnavailableRobe(creature, item);
            NeckAppearance.RemoveUnavailableNeck(creature, item);
            ArmorPartAppearance.RemoveUnavailableParts(creature, item);
            nativeCreature.UpdateAppearanceForEquippedItems();

            // A helmet that will render through the head is not resent as an attachment. Its
            // client must see the attachment removed: discarding the cached item while it stays
            // attached leaves every later render head invisible until relog.
            var resendsHeadItem = GetItemInSlot(InventorySlot.Head, creature) != item ||
                                  !HelmetModelRenderer.RendersThroughHead(
                                      creature, TintMapModelResolver.GetCurrentSelections(creature));

            var message = server.GetNWSMessage();
            var players = server.GetPlayerList();
            var refreshedClient = false;
            var detachingPlayers = new List<uint>();
            foreach (var player in players)
            {
                var lastUpdate = player.GetLastUpdateObject(creature);
                if (lastUpdate == null)
                    continue;

                var appearance = lastUpdate.m_cAppearance;
                if (appearance.m_oidHeadItem == item && !resendsHeadItem)
                    detachingPlayers.Add(player.m_nPlayerID);
                var handItem = appearance.m_oidLeftHandItem == item || appearance.m_oidRightHandItem == item;
                var cloakItem = appearance.m_oidCloakItem == item;
                var changed = false;
                if (appearance.m_oidHeadItem == item && resendsHeadItem)
                {
                    appearance.m_oidHeadItem = OBJECT_INVALID;
                    changed = true;
                }
                if (appearance.m_oidChestItem == item)
                {
                    appearance.m_oidChestItem = OBJECT_INVALID;
                    changed = true;
                }
                if (appearance.m_oidCloakItem == item)
                {
                    appearance.m_oidCloakItem = OBJECT_INVALID;
                    changed = true;
                }
                if (appearance.m_oidLeftHandItem == item)
                {
                    appearance.m_oidLeftHandItem = OBJECT_INVALID;
                    changed = true;
                }
                if (appearance.m_oidRightHandItem == item)
                {
                    appearance.m_oidRightHandItem = OBJECT_INVALID;
                    changed = true;
                }
                if (!changed)
                    continue;

                // Match NWNX_Item_SetItemAppearance's observer refresh, including hands.
                // Only the client's cached item is discarded; ownership and equipment stay put.
                if (handItem || cloakItem)
                {
                    RefreshInventorySlot(player, player.m_pInventoryGUI, creature, item, cloakItem);
                    RefreshInventorySlot(player, player.m_pOtherInventoryGUI, creature, item, cloakItem);
                }
                message.SendServerPlayerItemUpdate_DestroyItem(player, item);
                refreshedClient = true;
            }

            server.SetForceUpdate();
            TintMapService.ApplyCurrentColors(creature, resetShaderOverrides);
            Droid.UpdateEquippedItemSnapshot(creature, item);

            if (detachingPlayers.Count > 0)
                DelayCommand(QuickbarRefreshDelaySeconds, () =>
                    RefreshDetachedHeadItem(creature, item, detachingPlayers, resetShaderOverrides, DetachedHeadItemAttempts));

            if (refreshedClient && GetIsPC(creature))
                DelayCommand(QuickbarRefreshDelaySeconds, () => RestoreQuickbarReferences(creature, item));

            if (refreshedClient && (GetItemInSlot(InventorySlot.RightHand, creature) == item ||
                                    GetItemInSlot(InventorySlot.LeftHand, creature) == item))
                DelayCommand(QuickbarRefreshDelaySeconds, () => RefreshHandAppearance(creature, item));
        }

        /// <summary>
        /// Replaces the client's cached helmet once its native attachment has been detached.
        /// Deleting it while still attached masks the head, so this waits for each player's
        /// creature update to drop the head item first.
        /// </summary>
        private static void RefreshDetachedHeadItem(uint creature, uint item, List<uint> playerIds,
            bool resetShaderOverrides, int attemptsLeft)
        {
            if (!GetIsObjectValid(creature) || !GetIsObjectValid(item) ||
                GetItemInSlot(InventorySlot.Head, creature) != item)
                return;

            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var nativeCreature = server.GetGameObject(creature)?.AsNWSCreature();
            // A later click that selected the native helmet again runs its own refresh.
            if (nativeCreature == null || nativeCreature.m_cAppearance.m_oidHeadItem == item)
                return;

            var message = server.GetNWSMessage();
            var pending = new List<uint>();
            var refreshedClient = false;
            foreach (var player in server.GetPlayerList())
            {
                if (!playerIds.Contains(player.m_nPlayerID))
                    continue;

                var lastUpdate = player.GetLastUpdateObject(creature);
                if (lastUpdate == null)
                    continue;
                if (lastUpdate.m_cAppearance.m_oidHeadItem == item)
                {
                    pending.Add(player.m_nPlayerID);
                    continue;
                }

                RefreshInventorySlot(player, player.m_pInventoryGUI, creature, item, true);
                RefreshInventorySlot(player, player.m_pOtherInventoryGUI, creature, item, true);
                message.SendServerPlayerItemUpdate_DestroyItem(player, item);
                refreshedClient = true;
            }

            if (pending.Count > 0 && attemptsLeft > 1)
                DelayCommand(QuickbarRefreshDelaySeconds, () =>
                    RefreshDetachedHeadItem(creature, item, pending, resetShaderOverrides, attemptsLeft - 1));

            if (!refreshedClient)
                return;

            server.SetForceUpdate();
            TintMapService.ApplyCurrentColors(creature, resetShaderOverrides);
            if (GetIsPC(creature))
                DelayCommand(QuickbarRefreshDelaySeconds, () => RestoreQuickbarReferences(creature, item));
        }

        private static unsafe void RefreshInventorySlot(CNWSPlayer player, CNWSPlayerInventoryGUI gui, uint creature, uint item, bool clearIcon)
        {
            if (gui == null || gui.m_oidInventoryOwner != creature || gui.m_pcLastUpdateInventory == null)
                return;

            var message = NWNXLib.g_pAppManager.m_pServerExoApp.GetNWSMessage();
            var writing = false;
            var slots = gui.m_pcLastUpdateInventory.m_oidInventorySlots;
            for (var index = 0; index < 18; index++)
            {
                if (slots[index] == item)
                {
                    if (clearIcon && gui.m_bGuiInventoryOpen != 0)
                    {
                        if (!writing)
                        {
                            message.CreateWriteMessage(128, player.m_nPlayerID, 1);
                            writing = true;
                        }

                        // Native MajorGUIPanels_Inventory's slot-delete record. A slot add
                        // for the same item ID leaves the client's existing icon cached.
                        // Delete only its GUI entry before the normal update sends fresh
                        // appearance data; the server's equipment never moves.
                        message.WriteCHAR((byte)'G');
                        message.WriteCHAR((byte)(player.m_oidNWSObject == creature ? 'I' : 'i'));
                        message.WriteCHAR((byte)'D');
                        message.WriteDWORD(1u << index);
                    }
                    slots[index] = OBJECT_INVALID;
                }
            }

            if (!writing)
                return;

            byte* data = null;
            uint size = 0;
            if (message.GetWriteMessage(&data, &size) != 0 && size > 0)
                message.SendServerToPlayerMessage(player.m_nPlayerID,
                    (byte)MessageMajor.GameObjectUpdate, (byte)MessageGameObjectUpdateMinor.ObjectList, data, size);
        }

        private static void RefreshHandAppearance(uint creature, uint item)
        {
            if (!GetIsObjectValid(creature) || !GetIsObjectValid(item) ||
                (GetItemInSlot(InventorySlot.RightHand, creature) != item &&
                 GetItemInSlot(InventorySlot.LeftHand, creature) != item))
                return;

            // Rebuild the hand attachment after the client's replacement item has replicated.
            // No item movement or equipment events: rapid clicks use the latest model, and
            // an item genuinely unequipped during the delay is left alone.
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            foreach (var player in server.GetPlayerList())
            {
                var appearance = player.GetLastUpdateObject(creature)?.m_cAppearance;
                if (appearance == null)
                    continue;
                if (appearance.m_oidLeftHandItem == item)
                    appearance.m_oidLeftHandItem = OBJECT_INVALID;
                if (appearance.m_oidRightHandItem == item)
                    appearance.m_oidRightHandItem = OBJECT_INVALID;
            }
            server.SetForceUpdate();
            TintMapService.ApplyCurrentColors(creature);
        }

        public static void ApplyOutfit(uint creature, uint item, uint template)
        {
            if (!GetIsObjectValid(creature) || !GetIsObjectValid(item) || !GetIsObjectValid(template) ||
                GetItemInSlot(InventorySlot.Chest, creature) != item ||
                GetBaseItemType(item) != BaseItem.Armor || GetBaseItemType(template) != BaseItem.Armor)
                return;

            for (var part = 0; part < (int)AppearanceArmor.Num; part++)
                Set(item, ItemAppearanceType.ArmorModel, part,
                    GetItemAppearance(template, ItemAppearanceType.ArmorModel, part));

            // Six global channels followed by six channels for each of the nineteen parts.
            for (var color = 0; color < 6 * (1 + (int)AppearanceArmor.Num); color++)
                Set(item, ItemAppearanceType.ArmorColor, color,
                    GetItemAppearance(template, ItemAppearanceType.ArmorColor, color));

            TintMapService.ReplaceItemTintOverrides(template, item);
            Refresh(creature, item);
        }

        private static void RestoreQuickbarReferences(uint creature, uint item)
        {
            if (!GetIsObjectValid(creature) || !GetIsPC(creature) || !GetIsObjectValid(item))
                return;

            // The refresh packet clears client shortcuts. Read the current server slots
            // after replication so intervening quickbar edits are never overwritten.
            for (var slot = 0; slot < 36; slot++)
            {
                var entry = PlayerPlugin.GetQuickBarSlot(creature, slot);
                if (entry.Item == item || entry.SecondaryItem == item)
                    PlayerPlugin.SetQuickBarSlot(creature, slot, entry);
            }
        }
    }
}
