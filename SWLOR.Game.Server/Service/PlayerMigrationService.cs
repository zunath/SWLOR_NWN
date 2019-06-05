using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
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

            // VERSION 5: We're doing another Force rework, so remove any force feats the player may have acquired.
            if (dbPlayer.VersionNumber < 5)
            {
                NWNXCreature.RemoveFeat(player, 1135); // Force Breach
                NWNXCreature.RemoveFeat(player, 1136); // Force Lightning
                NWNXCreature.RemoveFeat(player, 1137); // Force Heal I
                NWNXCreature.RemoveFeat(player, 1140); // Absorption Field
                NWNXCreature.RemoveFeat(player, 1143); // Force Spread
                NWNXCreature.RemoveFeat(player, 1145); // Force Push
                NWNXCreature.RemoveFeat(player, 1125); // Force Aura
                NWNXCreature.RemoveFeat(player, 1152); // Drain Life
                NWNXCreature.RemoveFeat(player, 1134); // Chainspell
                NWNXCreature.RemoveFeat(player, 1162); // Force Heal II
                NWNXCreature.RemoveFeat(player, 1163); // Force Heal III
                NWNXCreature.RemoveFeat(player, 1164); // Force Heal IV

                dbPlayer.VersionNumber = 5;
            }

            // VERSION 6: Remove AC from all items the player is carrying. If possible,
            // grant +1 durability and +1 max durability for every 2 AC the item has.
            if (dbPlayer.VersionNumber < 6)
            {
                ProcessVersion6ItemChanges(player);
                dbPlayer.VersionNumber = 6;
            }

            DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Update);
        }
        
        private static void ProcessVersion6ItemChanges(NWPlayer player)
        {
            // Start with equipped items.
            foreach (var item in player.EquippedItems)
            {
                ProcessVersion6RemoveACFromItem(item);
                ProcessVersion6LightsaberRename(item);
            }
            // Next do all inventory items.
            foreach (var item in player.InventoryItems)
            {
                ProcessVersion6RemoveACFromItem(item);
                ProcessVersion6LightsaberRename(item);
            }
        }

        public static void ProcessVersion6RemoveACFromItem(NWItem item)
        {
            // Start by pulling the custom AC off the item and halving it.
            // Durability is +1 for every 2 AC on the item.
            int amount = item.CustomAC / 2;
            if (amount > 0)
            {
                float newMax = DurabilityService.GetMaxDurability(item) + amount;
                float newCurrent = DurabilityService.GetDurability(item) + amount;
                DurabilityService.SetMaxDurability(item, newMax);
                DurabilityService.SetDurability(item, newCurrent);
            }
            
            item.CustomAC = 0;

            // Check all item properties. If the IP is a component Armor Class Bonus, remove it and replace with an increase to durability.
            foreach (var ip in item.ItemProperties)
            {
                if (_.GetItemPropertyType(ip) == (int) CustomItemPropertyType.ComponentBonus)
                {
                    // Check the sub-type. If it's AC, then do the replacement.
                    if (GetItemPropertySubType(ip) == (int) ComponentBonusType.ACUp)
                    {
                        amount = GetItemPropertyCostTableValue(ip) / 2;
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

        public static void ProcessVersion6LightsaberRename(NWItem item)
        {
            string resref = item.Resref;
            string name = item.Name;
            // Lightsabers -> Light Foil
            if (item.CustomItemType == CustomItemType.Lightsaber)
            {
                switch (resref)
                {
                    case "lightsaber_b":
                        name = "Basic Light Foil";
                        break;
                    case "lightsaber_1":
                        name = "Light Foil I";
                        break;
                    case "lightsaber_2":
                        name = "Light Foil II";
                        break;
                    case "lightsaber_3":
                        name = "Light Foil III";
                        break;
                    case "lightsaber_4":
                        name = "Light Foil IV";
                        break;
                }
            }
            else if (item.CustomItemType == CustomItemType.Saberstaff)
            {
                switch (resref)
                {
                    case "saberstaff_b":
                        name = "Basic Lightfoil Staff";
                        break;
                    case "saberstaff_1":
                        name = "Lightfoil Staff I";
                        break;
                    case "saberstaff_2":
                        name = "Lightfoil Staff II";
                        break;
                    case "saberstaff_3":
                        name = "Lightfoil Staff III";
                        break;
                    case "saberstaff_4":
                        name = "Lightfoil Staff IV";
                        break;
                }
            }

            item.Name = name;
        }
    }
}
