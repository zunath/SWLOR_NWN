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
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.NWScript._;
using _ = SWLOR.Game.Server.NWScript._;
using BaseStructureType = SWLOR.Game.Server.Enumeration.BaseStructureType;
using BaseItemType = SWLOR.Game.Server.NWScript.Enumerations.BaseItemType;

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
        public static MarketRegion GetMarketRegionID(NWPlaceable terminal)
        {
            var marketRegionID = (MarketRegion)terminal.GetLocalInt("GTN_REGION_ID");
            if (marketRegionID == MarketRegion.Invalid)
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
            DataService.Set(dbPlayer);
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
                DataService.Set(dbPlayer);
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
            NWPlayer player = NWNXChat.GetSender();
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
        public static MarketCategory DetermineMarketCategory(NWItem item)
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
                case BaseItemType.GreatAxe: return MarketCategory.HeavyVibrobladeGA;
                case BaseItemType.BattleAxe: return MarketCategory.VibrobladeBA;
                case BaseItemType.BastardSword: return MarketCategory.VibrobladeBS;
                case BaseItemType.Dagger: return MarketCategory.FinesseVibrobladeD;
                case BaseItemType.GreatSword: return MarketCategory.HeavyVibrobladeGS;
                case BaseItemType.LongSword: return MarketCategory.VibrobladeLS;
                case BaseItemType.Rapier: return MarketCategory.FinesseVibrobladeR;
                case BaseItemType.Katana: return MarketCategory.VibrobladeK;
                case BaseItemType.ShortSword: return MarketCategory.VibrobladeSS;
                case BaseItemType.Club: return MarketCategory.BatonC;
                case BaseItemType.LightMace: return MarketCategory.BatonM;
                case BaseItemType.Morningstar: return MarketCategory.BatonMS;
                case BaseItemType.QuarterStaff: return MarketCategory.Quarterstaff;
                case BaseItemType.DoubleAxe: return MarketCategory.TwinVibrobladeDA;
                case BaseItemType.TwoBladedSword: return MarketCategory.TwinVibrobladeTS;
                case BaseItemType.Kukri: return MarketCategory.FinesseVibrobladeK;
                case BaseItemType.Halberd: return MarketCategory.PolearmH;
                case BaseItemType.ShortSpear: return MarketCategory.PolearmS;
                case BaseItemType.LightCrossBow: return MarketCategory.BlasterRifle; // Blaster Rifles
                case BaseItemType.ShortBow: return MarketCategory.BlasterPistol; // Blaster Pistols
                case BaseItemType.Helmet: return MarketCategory.Helmet;
                case BaseItemType.SmallShield: return MarketCategory.Shield; // Shields
                case BaseItemType.LargeShield: return MarketCategory.Shield; // Shields
                case BaseItemType.TowerShield: return MarketCategory.Shield; // Shields
                case BaseItemType.Book: return MarketCategory.Book;
                case BaseItemType.Gloves: return MarketCategory.PowerGlove; // Power Gloves
                case BaseItemType.Amulet: return MarketCategory.Necklace; // Necklace
                case BaseItemType.Ring: return MarketCategory.Ring;
            }

            // Check for armor.
            if (item.BaseItemType == BaseItemType.Armor ||
                item.BaseItemType == BaseItemType.Belt ||
                item.BaseItemType == BaseItemType.Cloak ||
                item.BaseItemType == BaseItemType.Boots)
            {
                switch (item.CustomItemType)
                {
                    case CustomItemType.LightArmor: return MarketCategory.LightArmor;
                    case CustomItemType.ForceArmor: return MarketCategory.ForceArmor;
                    case CustomItemType.HeavyArmor: return MarketCategory.HeavyArmor;
                    default: return MarketCategory.Clothing; // Default to clothes if no armor type is specified.
                }
            }

            // Check for Scanners
            if (item.GetLocalString("SCRIPT") == "ResourceScanner" ||
                item.GetLocalString("SCRIPT") == "MineralScanner")
                return MarketCategory.Scanner;
            // Check for Harvesters
            if (item.GetLocalString("SCRIPT") == "ResourceHarvester")
                return MarketCategory.Harvester;
            // Check for Repair Kits
            if (item.GetLocalString("SCRIPT") == "RepairKit")
                return MarketCategory.RepairKit;
            // Check for Stim Packs
            if (item.GetLocalString("ACTION_SCRIPT") == "Medicine.StimPack")
                return MarketCategory.StimPack;
            // Check for Force Packs
            if (item.GetLocalString("ACTION_SCRIPT") == "Medicine.ForcePack")
                return MarketCategory.ForcePack;
            // Check for Healing Kits
            if (item.GetLocalString("ACTION_SCRIPT") == "Medicine.HealingKit")
                return MarketCategory.HealingKit;
            // Check for Resuscitation Devices
            if (item.GetLocalString("ACTION_SCRIPT") == "Medicine.ResuscitationKit")
                return MarketCategory.ResuscitationDevice;
            // Check for Starcharts
            if (item.GetLocalString("SCRIPT") == "StarchartDisk" &&
                item.GetLocalInt("Starcharts") > 0)
                return MarketCategory.Starchart;
            // Check for Starship Equipment
            if (item.GetLocalString("SCRIPT") == "SSEnhancement")
                return MarketCategory.StarshipEquipment;
            // Check for Starship Repair Kits
            if (item.GetLocalString("SCRIPT") == "SSRepairKit")
                return MarketCategory.RepairKit;

            // Check item properties
            foreach (var prop in properties)
            {
                var propertyType = _.GetItemPropertyType(prop);
                // Check for components
                if (propertyType == ItemPropertyType.ComponentType)
                {
                    // IDs are mapped to the iprp_comptype.2da file.
                    switch (_.GetItemPropertyCostTableValue(prop))
                    {
                        case 1: return MarketCategory.ComponentRawOre;
                        case 2: return MarketCategory.ComponentMetal;
                        case 3: return MarketCategory.ComponentOrganic;
                        case 4: return MarketCategory.ComponentSmallBlade;
                        case 5: return MarketCategory.ComponentMediumBlade;
                        case 6: return MarketCategory.ComponentLargeBlade;
                        case 7: return MarketCategory.ComponentShaft;
                        case 8: return MarketCategory.ComponentSmallHandle;
                        case 9: return MarketCategory.ComponentMediumHandle;
                        case 10: return MarketCategory.ComponentLargeHandle;
                        case 11: return MarketCategory.ComponentEnhancement;
                        case 12: return MarketCategory.ComponentFiberplast;
                        case 13: return MarketCategory.ComponentLeather;
                        case 14: return MarketCategory.ComponentPadding;
                        case 15: return MarketCategory.ComponentElectronics;
                        case 16: return MarketCategory.ComponentWoodBatonFrame;
                        case 17: return MarketCategory.ComponentMetalBatonFrame;
                        case 18: return MarketCategory.ComponentRangedWeaponCore;
                        case 19: return MarketCategory.ComponentRifleBarrel;
                        case 20: return MarketCategory.ComponentPistolBarrel;
                        case 21: return MarketCategory.ComponentPowerCrystal;
                        case 22: return MarketCategory.ComponentSaberHilt;
                        case 23: return MarketCategory.ComponentSeeds;
                        case 24: return MarketCategory.ComponentBlueCrystal;
                        case 25: return MarketCategory.ComponentRedCrystal;
                        case 26: return MarketCategory.ComponentGreenCrystal;
                        case 27: return MarketCategory.ComponentYellowCrystal;
                        case 28: return MarketCategory.ComponentBlueCrystalCluster;
                        case 29: return MarketCategory.ComponentRedCrystalCluster;
                        case 30: return MarketCategory.ComponentGreenCrystalCluster;
                        case 31: return MarketCategory.ComponentYellowCrystalCluster;
                        case 32: return MarketCategory.ComponentPowerCrystalCluster;
                        case 33: return MarketCategory.ComponentHeavyArmorCore;
                        case 34: return MarketCategory.ComponentLightArmorCore;
                        case 35: return MarketCategory.ComponentForceArmorCore;
                        case 36: return MarketCategory.ComponentHeavyArmorSegment;
                        case 37: return MarketCategory.ComponentLightArmorSegment;
                        case 38: return MarketCategory.ComponentForceArmorSegment;
                        case 39: return MarketCategory.ComponentSmallStructureFrame;
                        case 40: return MarketCategory.ComponentMediumStructureFrame;
                        case 41: return MarketCategory.ComponentLargeStructureFrame;
                        case 42: return MarketCategory.ComponentComputingModule;
                        case 43: return MarketCategory.ComponentConstructionParts;
                        case 44: return MarketCategory.ComponentMainframe;
                        case 45: return MarketCategory.ComponentPowerRelay;
                        case 46: return MarketCategory.ComponentPowerCore;
                        case 47: return MarketCategory.ComponentIngredient;
                        case 48: return MarketCategory.ComponentHerb;
                        case 49: return MarketCategory.ComponentCarbosyrup;
                        case 50: return MarketCategory.ComponentMeat;
                        case 51: return MarketCategory.ComponentCereal;
                        case 52: return MarketCategory.ComponentGrain;
                        case 53: return MarketCategory.ComponentVegetable;
                        case 54: return MarketCategory.ComponentWater;
                        case 55: return MarketCategory.ComponentCurryPaste;
                        case 56: return MarketCategory.ComponentSoup;
                        case 57: return MarketCategory.ComponentSpicedMilk;
                        case 58: return MarketCategory.ComponentDough;
                        case 59: return MarketCategory.ComponentButter;
                        case 60: return MarketCategory.ComponentNoodles;
                        case 61: return MarketCategory.ComponentEggs;
                        case 62: return MarketCategory.ComponentEmitter;
                        case 63: return MarketCategory.ComponentHyperdrive;
                        case 64: return MarketCategory.ComponentHullPlating;
                        case 65: return MarketCategory.ComponentStarshipWeapon;
                    }
                }

                // Check for mods
                if (propertyType == ItemPropertyType.BlueMod)
                {
                    return MarketCategory.BlueMod;
                }
                if (propertyType == ItemPropertyType.GreenMod)
                {
                    return MarketCategory.GreenMod;
                }
                if (propertyType == ItemPropertyType.RedMod)
                {
                    return MarketCategory.RedMod;
                }
                if (propertyType == ItemPropertyType.YellowMod)
                {
                    return MarketCategory.YellowMod;
                }
            }

            // Check base structures.
            var baseStructureID = (BaseStructure)item.GetLocalInt("BASE_STRUCTURE_ID");
            if (baseStructureID != BaseStructure.Invalid)
            {
                var baseStructure = BaseService.GetBaseStructure(baseStructureID);
                var baseStructureType = baseStructure.BaseStructureType;

                switch (baseStructureType)
                {
                    case BaseStructureType.ControlTower: return MarketCategory.ControlTower;
                    case BaseStructureType.Drill: return MarketCategory.Drill;
                    case BaseStructureType.ResourceSilo: return MarketCategory.ResourceSilo;
                    case BaseStructureType.Turret: return MarketCategory.Turret;
                    case BaseStructureType.Building: return MarketCategory.Building;
                    case BaseStructureType.MassProduction: return MarketCategory.MassProduction;
                    case BaseStructureType.StarshipProduction: return MarketCategory.StarshipProduction;
                    case BaseStructureType.Furniture: return MarketCategory.Furniture;
                    case BaseStructureType.StronidiumSilo: return MarketCategory.StronidiumSilo;
                    case BaseStructureType.FuelSilo: return MarketCategory.FuelSilo;
                    case BaseStructureType.CraftingDevice: return MarketCategory.CraftingDevice;
                    case BaseStructureType.PersistentStorage: return MarketCategory.PersistentStorage;
                    case BaseStructureType.Starship: return MarketCategory.Starship;
                }
            }

            // Check for individual resrefs. This should be used as a last-resort.
            switch (resref)
            {
                case "fuel_cell":
                case "stronidium":
                    return MarketCategory.Fuel;
            }

            // A -1 represents that this item is not supported on the market system.
            // This could be because we forgot to add a determination for it but more than likely it was
            // excluded on purpose. Lightsabers and Saberstaffs are an example of this.
            return MarketCategory.Invalid;
        }

    }
}
