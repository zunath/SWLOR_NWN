using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature
{
    public static class ArmorDisplay
    {
        /// <summary>
        /// When a player equips a type of armor which can be hidden, set whether it is hidden based on the player's setting.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void EquipHelmet()
        {
            var player = GetPCItemLastEquippedBy();
            var item = GetPCItemLastEquipped();

            // A creature a DM possesses keeps whatever its own items are set to.
            if (!GetIsPC(player) || GetIsDMPossessed(player)) return;
            var itemType = GetBaseItemType(item);
            if (itemType != BaseItem.Helmet && itemType != BaseItem.Cloak) return;

            bool showHelmet, showCloak;
            if (GetIsDM(player))
            {
                var preferences = DMPreference.Get(player);
                showHelmet = preferences.ShowHelmet;
                showCloak = preferences.ShowCloak;
            }
            else
            {
                var playerId = GetObjectUUID(player);
                var dbPlayer = DB.Get<Player>(playerId) ?? new Player(playerId);
                showHelmet = dbPlayer.Settings.ShowHelmet;
                showCloak = dbPlayer.Settings.ShowCloak;
            }

            if (itemType == BaseItem.Helmet)
            {
                HelmetModelRenderer.SetShownByOwner(item, showHelmet);
            }
            else
            {
                SetHiddenWhenEquipped(item, !showCloak);
            }
        }

        /// <summary>
        /// Shows or hides the helmet and cloak a creature is wearing.
        /// </summary>
        /// <param name="creature">The creature wearing them.</param>
        /// <param name="showHelmet">true to show the helmet.</param>
        /// <param name="showCloak">true to show the cloak.</param>
        public static void Apply(uint creature, bool showHelmet, bool showCloak)
        {
            var helmet = GetItemInSlot(InventorySlot.Head, creature);
            if (GetIsObjectValid(helmet))
            {
                HelmetModelRenderer.SetShownByOwner(helmet, showHelmet);
            }

            var cloak = GetItemInSlot(InventorySlot.Cloak, creature);
            if (GetIsObjectValid(cloak))
            {
                SetHiddenWhenEquipped(cloak, !showCloak);
            }
            TintMapService.RefreshAfterColorChange(creature);
        }

        /// <summary>
        /// Returns whether a DM's avatar or possessed creature shows its helmet and cloak.
        /// </summary>
        /// <param name="creature">A DM avatar or the creature a DM possesses.</param>
        public static (bool ShowHelmet, bool ShowCloak) GetDMArmorDisplay(uint creature)
        {
            // Worn items hold the current state. A DM avatar's saved preference covers an empty slot.
            var preferences = GetIsDM(creature) ? DMPreference.Get(creature) : null;
            var helmet = GetItemInSlot(InventorySlot.Head, creature);
            var cloak = GetItemInSlot(InventorySlot.Cloak, creature);
            var showHelmet = GetIsObjectValid(helmet)
                ? HelmetModelRenderer.IsShownByOwner(helmet)
                : preferences?.ShowHelmet ?? true;
            var showCloak = GetIsObjectValid(cloak)
                ? GetHiddenWhenEquipped(cloak) == 0
                : preferences?.ShowCloak ?? true;
            return (showHelmet, showCloak);
        }

        /// <summary>
        /// Shows or hides a DM's avatar's or possessed creature's helmet and cloak.
        /// </summary>
        /// <param name="creature">A DM avatar or the creature a DM possesses.</param>
        /// <param name="showHelmet">true to show the helmet.</param>
        /// <param name="showCloak">true to show the cloak.</param>
        /// <returns>false when a DM avatar's choice could not be saved for items equipped later.</returns>
        public static bool SetDMArmorDisplay(uint creature, bool showHelmet, bool showCloak)
        {
            // A DM avatar's choice also covers items equipped later; a possessed creature's stays on its items.
            var saved = true;
            if (GetIsDM(creature))
            {
                saved = DMPreference.Update(creature, preferences =>
                {
                    preferences.ShowHelmet = showHelmet;
                    preferences.ShowCloak = showCloak;
                });
            }

            Apply(creature, showHelmet, showCloak);
            return saved;
        }
    }
}
