using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Module;
using SWLOR.Game.Server.NWNX;

using static NWN._;

namespace SWLOR.Game.Server.Service
{
    public static class PlayerMigrationService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleEnter>(message => OnModuleEnter());
        }

        private static void OnModuleEnter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            var dbPlayer = DataService.Get<Player>(player.GlobalID);

            // VERSION 2: Background items are no longer plot because item level no longer dictates your skill XP gain.
            if (dbPlayer.VersionNumber < 2) 
            {
                string[] resrefs =
                {
                    "blaster_s",
                    "rifle_s",
                    "powerglove_t",
                    "baton_s",
                    "doubleaxe_z",
                    "kukri_d",
                    "greatsword_s",
                    "scanner_r_h",
                    "harvest_r_h",
                    "man_armor"
                };

                foreach (var resref in resrefs)
                {
                    NWItem item = _.GetItemPossessedBy(player, resref);
                    if (item.IsValid)
                    {
                        item.IsPlot = false;
                    }
                }

                dbPlayer.VersionNumber = 2;
            }

            // VERSION 3: Force feats need to be removed since force powers were reworked.
            if (dbPlayer.VersionNumber < 3)
            {
                // These IDs come from the Feat.2da file.
                NWNXCreature.RemoveFeat(player, 1135); // Force Breach
                NWNXCreature.RemoveFeat(player, 1136); // Force Lightning
                NWNXCreature.RemoveFeat(player, 1137); // Force Heal
                NWNXCreature.RemoveFeat(player, 1138); // Dark Heal
                NWNXCreature.RemoveFeat(player, 1143); // Force Spread
                NWNXCreature.RemoveFeat(player, 1144); // Dark Spread
                NWNXCreature.RemoveFeat(player, 1145); // Force Push
                NWNXCreature.RemoveFeat(player, 1125); // Force Aura
                NWNXCreature.RemoveFeat(player, 1152); // Drain Life
                NWNXCreature.RemoveFeat(player, 1134); // Chainspell

                dbPlayer.VersionNumber = 3;
            }

            // VERSION 4: Give the Uncanny Dodge 1 feat to all characters.
            if (dbPlayer.VersionNumber < 4)
            {
                NWNXCreature.AddFeatByLevel(player, FEAT_UNCANNY_DODGE_1, 1);
                dbPlayer.VersionNumber = 4;
            }

            // VERSION 5: Remove AC from all items the player is carrying. If possible,
            // grant +1 durability and +1 max durability for every 2 AC the item has.
            if (dbPlayer.VersionNumber < 5)
            {
                ProcessVersion5ACRemoval(player);
                dbPlayer.VersionNumber = 5;
            }

            DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Update);
        }
        
        private static void ProcessVersion5ACRemoval(NWPlayer player)
        {
            // Start with equipped items.
            foreach (var item in player.EquippedItems)
            {
                ProcessVersion5RemoveACFromItem(item);
            }
            // Next do all inventory items.
            foreach (var item in player.InventoryItems)
            {
                ProcessVersion5RemoveACFromItem(item);
            }
        }

        private static void ProcessVersion5RemoveACFromItem(NWItem item)
        {
            // Check all item properties. If the IP is an Armor Class property, remove it and replace with an increase to durability.
            // Durability is +1 for every 2 AC on the item.
            foreach (var ip in item.ItemProperties)
            {
                int type = _.GetItemPropertyType(ip);
                // Regular Armor Class item property
                if (type == (int) CustomItemPropertyType.ArmorClass)
                {
                    int amount = GetItemPropertyCostTableValue(ip) / 2;
                    float newMax = DurabilityService.GetMaxDurability(item) + amount;
                    float newCurrent = DurabilityService.GetDurability(item) + amount;
                    DurabilityService.SetMaxDurability(item, newMax);
                    DurabilityService.SetDurability(item, newCurrent);

                    _.RemoveItemProperty(item, ip);
                }

                // Component-specific Armor Class Bonus item property
                else if (type == (int) CustomItemPropertyType.ComponentBonus)
                {
                    // Check the sub-type. If it's AC, then do the replacement.
                    if (GetItemPropertySubType(ip) == (int) ComponentBonusType.ACUp)
                    {
                        int amount = GetItemPropertyCostTableValue(ip) / 2;
                        // Grant the durability up property if amount > 0
                        if (amount > 0)
                        {
                            // Unpack the IP we're working with. Adjust its type and value, then reapply it.
                            var unpacked = NWNXItemProperty.UnpackIP(ip);
                            unpacked.SubType = (int) ComponentBonusType.DurabilityUp;
                            unpacked.CostTableValue = amount;
                            var packed = NWNXItemProperty.PackIP(unpacked);
                            BiowareXP2.IPSafeAddItemProperty(item, packed, 0.0f, AddItemPropertyPolicy.IgnoreExisting, true, true);
                        }

                        _.RemoveItemProperty(item, ip);
                    }
                }
            }
        }

    }
}
