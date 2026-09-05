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

        public static void Refresh(uint creature, uint item)
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

            nativeCreature.UpdateAppearanceForEquippedItems();

            var message = server.GetNWSMessage();
            var players = server.GetPlayerList();
            var refreshedClient = false;
            foreach (var player in players)
            {
                var lastUpdate = player.GetLastUpdateObject(creature);
                if (lastUpdate == null)
                    continue;

                var appearance = lastUpdate.m_cAppearance;
                var changed = false;
                if (appearance.m_oidHeadItem == item)
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
                message.SendServerPlayerItemUpdate_DestroyItem(player, item);
                refreshedClient = true;
            }

            server.SetForceUpdate();
            TintMapService.ApplyCurrentColors(creature);
            Droid.UpdateEquippedItemSnapshot(creature, item);

            if (refreshedClient && GetIsPC(creature))
                DelayCommand(QuickbarRefreshDelaySeconds, () => RestoreQuickbarReferences(creature, item));
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
