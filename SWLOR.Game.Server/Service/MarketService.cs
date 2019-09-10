using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;

using SWLOR.Game.Server.ValueObject;
using static NWN._;
using BaseStructureType = SWLOR.Game.Server.Enumeration.BaseStructureType;

namespace SWLOR.Game.Server.Service
{
    public static class MarketService
    {
        // Couldn't get any more specific than this. :)
        public static int NumberOfItemsAllowedToBeSoldAtATime => 50;

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleEnter>(message => OnModuleEnter());
            MessageHub.Instance.Subscribe<OnModuleNWNXChat>(message => OnModuleNWNXChat());
        }

        /// <summary>
        /// Retrieves the temporary market data for a given player.
        /// This data is only stored through the lifespan of a market transaction.
        /// </summary>
        /// <param name="player">The player to retrieve from.</param>
        /// <returns>The market data for the player specified.</returns>
        public static PCMarketData GetPlayerMarketData(NWPlayer player)
        {
            // Need to store the data outside of the conversation because of the constant
            // context switching between conversation and accessing placeable containers.
            // Conversation data is wiped when it closes.
            if (player.Data.ContainsKey("MARKET_MODEL"))
            {
                return player.Data["MARKET_MODEL"];
            }

            var model = new PCMarketData();
            player.Data["MARKET_MODEL"] = model;
            return model;
        }

        /// <summary>
        /// Removes the temporary market data stored for a player.
        /// </summary>
        /// <param name="player"></param>
        public static void ClearPlayerMarketData(NWPlayer player)
        {
            player.Data.Remove("MARKET_MODEL");
        }

        /// <summary>
        /// Determines which region a market terminal belongs to, based on the GTN_REGION_ID local variable.
        /// </summary>
        /// <param name="terminal">The market terminal placeable</param>
        /// <returns>The ID which links up to the MarketRegion database table.</returns>
        public static int GetMarketRegionID(NWPlaceable terminal)
        {
            int marketRegionID = terminal.GetLocalInt("GTN_REGION_ID");
            if (marketRegionID <= 0)
                throw new Exception("GTN Region ID not specified on target terminal object: " + terminal.Name);

            return marketRegionID;
        }

        /// <summary>
        /// This will either give the seller of an item money immediately or place it in their "GoldTill"
        /// value in the database. This money will be delivered to the player the next time he or she logs in.
        /// </summary>
        /// <param name="playerID">The player ID to pay.</param>
        /// <param name="amount">The amount of gold to give them.</param>
        public static void GiveMarketGoldToPlayer(Guid playerID, int amount)
        {
            NWPlayer player = NWModule.Get().Players.SingleOrDefault(x => x.GlobalID == playerID);

            // Player is online. Give them the gold directly and notify them they sold an item.
            if (player != null && player.IsValid)
            {
                _.GiveGoldToCreature(player, amount);
                player.FloatingText("You sold an item on the Galactic Trade Network for " + amount + " credits.");
                return;
            }

            // Player is offline. Put the gold into their "Till" and give it to them the next time they log on.
            Player dbPlayer = DataService.Player.GetByID(playerID);
            dbPlayer.GoldTill += amount;
            DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Update);
        }

        /// <summary>
        /// Call this on the module's OnEnter event.
        /// If a player sold items on the market while they were offline, they'll receive that money on entry.
        /// </summary>
        private static void OnModuleEnter()
        {
            NWPlayer player = _.GetEnteringObject();
            if (!player.IsPlayer) return;

            Player dbPlayer = DataService.Player.GetByID(player.GlobalID);

            if (dbPlayer.GoldTill > 0)
            {
                player.FloatingText("You sold goods on the GTN Market while you were offline. " + dbPlayer.GoldTill + " credits have been transferred to your account.");
                _.GiveGoldToCreature(player, dbPlayer.GoldTill);
                dbPlayer.GoldTill = 0;
                DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Update);
            }
        }

        public static bool CanHandleChat(NWPlayer player)
        {
            if (!player.IsPlayer) return false;

            // Is the player currently in the market process?
            return player.Data.ContainsKey("MARKET_MODEL");
        }

        /// <summary>
        /// Call this on the NWNX OnChat event (not the OnPlayerChat event provided by base NWN).
        /// If a player is currently setting a "Seller Note", look for the text and apply it to their
        /// temporary market data object.
        /// </summary>
        private static void OnModuleNWNXChat()
        {
            NWPlayer player = NWNXChat.GetSender().Object;
            if (!CanHandleChat(player)) return;

            var model = GetPlayerMarketData(player);

            // Is the player specifying a seller note?
            if (!model.IsSettingSellerNote) return;
            model.IsSettingSellerNote = false;

            var message = NWNXChat.GetMessage();
            message = message.Truncate(1024);
            model.SellerNote = message;

            player.FloatingText("Seller note set! Please click 'Refresh' to see the changes.");
            NWNXChat.SkipMessage();
        }

        /// <summary>
        /// Returns the fee percentage charged to players who sell an item.
        /// This percentage should be tied to the price the item is being sold for.
        /// Example: 1000 credit item should be charged 7 credits for a 7-day listing.
        /// </summary>
        /// <param name="days">The number of days the listing will be posted.</param>
        /// <returns>The percentage, in decimal form, to apply when determining fees.</returns>
        public static float CalculateFeePercentage(int days)
        {
            const float Rate = 0.001f; // 0.1%
            return days * Rate;
        }

        /// <summary>
        /// Returns an ID tied to the MarketCategory table. This is where players may find
        /// the item on the marketplace.
        /// </summary>
        /// <param name="item">The item to use for the determination.</param>
        /// <returns>The market category ID or a value of -1 if item is not supported.</returns>
        public static int DetermineMarketCategory(NWItem item)
        {
            // ===============================================================================
            // The following items are intentionally excluded from market transactions:
            // Lightsaber, Saberstaff
            // ===============================================================================

            // Some of the determinations require looking at the item's properties. Pull that list back now for later use.
            var properties = item.ItemProperties.ToList();
            var resref = item.Resref;

            // Weapons - These IDs are based solely on the NWN BaseItemType
            switch (item.BaseItemType)
            {
                case BASE_ITEM_GREATAXE: return 1;
                case BASE_ITEM_BATTLEAXE: return 2;
                case BASE_ITEM_BASTARDSWORD: return 3;
                case BASE_ITEM_DAGGER: return 4;
                case BASE_ITEM_GREATSWORD: return 5;
                case BASE_ITEM_LONGSWORD: return 7;
                case BASE_ITEM_RAPIER: return 8;
                case BASE_ITEM_KATANA: return 9;
                case BASE_ITEM_SHORTSWORD: return 10;
                case BASE_ITEM_CLUB: return 11;
                case BASE_ITEM_LIGHTMACE: return 12;
                case BASE_ITEM_MORNINGSTAR: return 13;
                case BASE_ITEM_QUARTERSTAFF: return 15;
                case BASE_ITEM_DOUBLEAXE: return 16;
                case BASE_ITEM_TWOBLADEDSWORD: return 17;
                case BASE_ITEM_KUKRI: return 18;
                case BASE_ITEM_HALBERD: return 19;
                case BASE_ITEM_SHORTSPEAR: return 20;
                case BASE_ITEM_LIGHTCROSSBOW: return 21; // Blaster Rifles
                case BASE_ITEM_SHORTBOW: return 22; // Blaster Pistols
                case BASE_ITEM_HELMET: return 23;
                case BASE_ITEM_SMALLSHIELD: return 28; // Shields
                case BASE_ITEM_LARGESHIELD: return 28; // Shields
                case BASE_ITEM_TOWERSHIELD: return 28; // Shields
                case BASE_ITEM_BOOK: return 29;
                case BASE_ITEM_GLOVES: return 30; // Power Gloves
                case BASE_ITEM_AMULET: return 102; // Necklace
                case BASE_ITEM_RING: return 103;
            }

            // Check for armor.
            if (item.BaseItemType == BASE_ITEM_ARMOR ||
                item.BaseItemType == BASE_ITEM_BELT ||
                item.BaseItemType == BASE_ITEM_CLOAK ||
                item.BaseItemType == BASE_ITEM_BOOTS)
            {
                switch (item.CustomItemType)
                {
                    case CustomItemType.LightArmor: return 24;
                    case CustomItemType.ForceArmor: return 25;
                    case CustomItemType.HeavyArmor: return 26;
                    default: return 23; // Default to clothes if no armor type is specified.
                }
            }

            // Check for Scanners
            if (item.GetLocalString("SCRIPT") == "ResourceScanner" ||
                item.GetLocalString("SCRIPT") == "MineralScanner")
                return 31;
            // Check for Harvesters
            if (item.GetLocalString("SCRIPT") == "ResourceHarvester")
                return 32;
            // Check for Repair Kits
            if (item.GetLocalString("SCRIPT") == "RepairKit")
                return 104;
            // Check for Stim Packs
            if (item.GetLocalString("ACTION_SCRIPT") == "Medicine.StimPack")
                return 105;
            // Check for Force Packs
            if (item.GetLocalString("ACTION_SCRIPT") == "Medicine.ForcePack")
                return 106;
            // Check for Healing Kits
            if (item.GetLocalString("ACTION_SCRIPT") == "Medicine.HealingKit")
                return 107;
            // Check for Resuscitation Devices
            if (item.GetLocalString("ACTION_SCRIPT") == "Medicine.ResuscitationKit")
                return 108;
            // Check for Starcharts
            if (item.GetLocalString("SCRIPT") == "StarchartDisk" &&
                item.GetLocalInt("Starcharts") > 0)
                return 109;
            // Check for Starship Equipment
            if (item.GetLocalString("SCRIPT") == "SSEnhancement")
                return 124;
            // Check for Starship Repair Kits
            if (item.GetLocalString("SCRIPT") == "SSRepairKit")
                return 104;

            // Check item properties
            foreach (var prop in properties)
            {
                var propertyType = _.GetItemPropertyType(prop);
                // Check for components
                if (propertyType == (int) CustomItemPropertyType.ComponentType)
                {
                    // IDs are mapped to the iprp_comptype.2da file.
                    switch (_.GetItemPropertyCostTableValue(prop))
                    {
                        case 1: return 33;
                        case 2: return 34;
                        case 3: return 35;
                        case 4: return 36;
                        case 5: return 37;
                        case 6: return 38;
                        case 7: return 39;
                        case 8: return 40;
                        case 9: return 41;
                        case 10: return 42;
                        case 11: return 43;
                        case 12: return 44;
                        case 13: return 45;
                        case 14: return 46;
                        case 15: return 47;
                        case 16: return 48;
                        case 17: return 49;
                        case 18: return 50;
                        case 19: return 51;
                        case 20: return 52;
                        case 21: return 53;
                        case 22: return 54;
                        case 23: return 55;
                        case 24: return 56;
                        case 25: return 57;
                        case 26: return 58;
                        case 27: return 59;
                        case 28: return 60;
                        case 29: return 61;
                        case 30: return 62;
                        case 31: return 63;
                        case 32: return 64;
                        case 33: return 65;
                        case 34: return 66;
                        case 35: return 67;
                        case 36: return 68;
                        case 37: return 69;
                        case 38: return 70;
                        case 39: return 71;
                        case 40: return 72;
                        case 41: return 73;
                        case 42: return 74;
                        case 43: return 75;
                        case 44: return 76;
                        case 45: return 77;
                        case 46: return 78;
                        case 47: return 79;
                        case 48: return 80;
                        case 49: return 81;
                        case 50: return 82;
                        case 51: return 83;
                        case 52: return 84;
                        case 53: return 85;
                        case 54: return 86;
                        case 55: return 87;
                        case 56: return 88;
                        case 57: return 89;
                        case 58: return 90;
                        case 59: return 91;
                        case 60: return 92;
                        case 61: return 93;
                        case 62: return 94;
                        case 63: return 95;
                        case 64: return 96;
                        case 65: return 97;
                    }
                }

                // Check for mods
                if (propertyType == (int)CustomItemPropertyType.BlueMod)
                {
                    return 98;
                }
                if (propertyType == (int)CustomItemPropertyType.GreenMod)
                {
                    return 99;
                }
                if (propertyType == (int)CustomItemPropertyType.RedMod)
                {
                    return 100;
                }
                if (propertyType == (int)CustomItemPropertyType.YellowMod)
                {
                    return 101;
                }
            }

            // Check base structures.
            int baseStructureID = item.GetLocalInt("BASE_STRUCTURE_ID");
            if (baseStructureID > 0)
            {
                var baseStructure = DataService.BaseStructure.GetByID(baseStructureID);
                var baseStructureType = (BaseStructureType) baseStructure.BaseStructureTypeID;

                switch (baseStructureType)
                {
                    case BaseStructureType.ControlTower: return 111;
                    case BaseStructureType.Drill: return 112;
                    case BaseStructureType.ResourceSilo: return 113;
                    case BaseStructureType.Turret: return 114;
                    case BaseStructureType.Building: return 115;
                    case BaseStructureType.MassProduction: return 116;
                    case BaseStructureType.StarshipProduction: return 117;
                    case BaseStructureType.Furniture: return 118;
                    case BaseStructureType.StronidiumSilo: return 119;
                    case BaseStructureType.FuelSilo: return 120;
                    case BaseStructureType.CraftingDevice: return 121;
                    case BaseStructureType.PersistentStorage: return 122;
                    case BaseStructureType.Starship: return 123;
                }
            }

            // Check for individual resrefs. This should be used as a last-resort.
            switch (resref)
            {
                case "fuel_cell":
                case "stronidium":
                    return 110;
            }

            // A -1 represents that this item is not supported on the market system.
            // This could be because we forgot to add a determination for it but more than likely it was
            // excluded on purpose. Lightsabers and Saberstaffs are an example of this.
            return -1;
        }

    }
}
