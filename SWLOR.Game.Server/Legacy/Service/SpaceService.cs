using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Legacy.Data.Entity;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Event.Creature;
using SWLOR.Game.Server.Legacy.Event.Module;
using SWLOR.Game.Server.Legacy.Event.Player;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.ValueObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using BaseStructureType = SWLOR.Game.Server.Legacy.Enumeration.BaseStructureType;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;
using SkillType = SWLOR.Game.Server.Legacy.Enumeration.SkillType;

namespace SWLOR.Game.Server.Legacy.Service
{
    public static class SpaceService
    {
        public static void SubscribeEvents()
        {
            // Creature Events
            MessageHub.Instance.Subscribe<OnCreatureHeartbeat>(message => OnCreatureHeartbeat());
            MessageHub.Instance.Subscribe<OnCreatureSpawn>(message => OnCreatureSpawn());

            // Player Events
            MessageHub.Instance.Subscribe<OnPlayerHeartbeat>(message => OnCreatureHeartbeat());

            // Module Events
            MessageHub.Instance.Subscribe<OnModuleEquipItem>(message => OnModuleEquipItem());
            MessageHub.Instance.Subscribe<OnModuleLeave>(message => OnModuleLeave());
            MessageHub.Instance.Subscribe<OnModuleNWNXChat>(message => OnModuleNWNXChat());
        }

        private struct ShipStats
        {
            public int weapons;
            public int shields;
            public int stealth;
            public int scanning;
            public int speed;
            public int stronidium;
            public float scale;
            public float range;
        }

        private static ShipStats GetShipStatsByAppearance(AppearanceType appearance)
        {
            var stats = new ShipStats();

            switch (appearance)
            {
                case AppearanceType.TiefighterSM: //Tiefightersm
                    stats.weapons = 3;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 1;
                    stats.speed = 100;
                    stats.stronidium = 250;
                    stats.scale = 1.0f;
                    stats.range = 10.0f;
                    break;
                case AppearanceType.SmallCargoShip: //b_SmallCargoShip
                    stats.weapons = 0;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 0;
                    stats.speed = 40;
                    stats.stronidium = 150;
                    stats.scale = 1.0f;
                    stats.range = 10.0f;
                    break;
                case AppearanceType.SmallShuttle3: //b_SmallShuttle3
                    stats.weapons = 0;
                    stats.shields = 2;
                    stats.stealth = 2;
                    stats.scanning = 2;
                    stats.speed = 80;
                    stats.stronidium = 75;
                    stats.scale = 1.0f;
                    stats.range = 10.0f;
                    break;
                case AppearanceType.SmallShuttle4: //b_SmallShuttle4
                    stats.weapons = 0;
                    stats.shields = 2;
                    stats.stealth = 2;
                    stats.scanning = 2;
                    stats.speed = 80;
                    stats.stronidium = 250;
                    stats.scale = 1.0f;
                    stats.range = 10.0f;
                    break;
                case AppearanceType.SmallFighter1: //b_SmallFighter1
                    stats.weapons = 3;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 1;
                    stats.speed = 100;
                    stats.stronidium = 300;
                    stats.scale = 1.0f;
                    stats.range = 10.0f;
                    break;
                case AppearanceType.SmallFighter2: //b_SmallFighter2
                    stats.weapons = 3;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 1;
                    stats.speed = 100;
                    stats.stronidium = 300;
                    stats.scale = 1.0f;
                    stats.range = 10.0f;
                    break;
                case AppearanceType.SmallFighter3: //b_SmallFighter3
                    stats.weapons = 3;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 1;
                    stats.stronidium = 300;
                    stats.speed = 100;
                    stats.scale = 1.0f;
                    stats.range = 10.0f;
                    break;
                case AppearanceType.XWingSmall: //XWingSmall
                    stats.weapons = 3;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 1;
                    stats.stronidium = 250;
                    stats.speed = 100;
                    stats.range = 10.0f;
                    break;
                case AppearanceType.AWingSmall: //AWingSmall
                    stats.weapons = 3;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 2;
                    stats.speed = 120;
                    stats.stronidium = 150;
                    stats.scale = 1.0f;
                    stats.range = 10.0f;
                    break;

                // Appearances that are one scale up, and need to be scaled down. 
                case AppearanceType.ImperialShuttleSmall: //Impshuttlesm
                    stats.weapons = 0;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 1;
                    stats.speed = 80;
                    stats.stronidium = 75;
                    stats.scale = 0.25f;
                    stats.range = 10.0f;
                    break;
                case AppearanceType.SmallCargoShip2: //b_SmallCargoShip2
                    stats.weapons = 0;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 0;
                    stats.speed = 40;
                    stats.stronidium = 300;
                    stats.scale = 0.25f;
                    stats.range = 10.0f;
                    break;
                case AppearanceType.SmallScoutShip: //d_SmallScoutship
                    stats.weapons = 0;
                    stats.shields = 2;
                    stats.stealth = 2;
                    stats.scanning = 3;
                    stats.speed = 120;
                    stats.stronidium = 150;
                    stats.scale = 0.25f;
                    stats.range = 10.0f;
                    break;
                case AppearanceType.SmallFirefly: //b_SmallFirefly
                    stats.weapons = 1;
                    stats.shields = 3;
                    stats.stealth = 0;
                    stats.scanning = 0;
                    stats.speed = 60;
                    stats.stronidium = 200;
                    stats.scale = 0.25f;
                    stats.range = 10.0f;
                    break;

                //Appearances that are one scale down, and need to be scaled up.
                case AppearanceType.BWingSmall: //BWingSmall
                    stats.weapons = 4;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 1;
                    stats.speed = 60;
                    stats.stronidium = 300;
                    stats.scale = 4.0f;
                    stats.range = 20.0f;
                    break;
                case AppearanceType.FreighterSmall: //FreighterSmall
                    stats.weapons = 1;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 0;
                    stats.speed = 60;
                    stats.stronidium = 100;
                    stats.scale = 4.0f;
                    stats.range = 10.0f;
                    break;
                case AppearanceType.CorvetteSmall: //CorvetteSmall
                    stats.weapons = 6;
                    stats.shields = 5;
                    stats.stealth = 0;
                    stats.scanning = 0;
                    stats.speed = 50;
                    stats.stronidium = 1000;
                    stats.scale = 4.0f;
                    stats.range = 20.0f;
                    break;
                case AppearanceType.VLambda: //v_lambda
                    stats.weapons = 0;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 0;
                    stats.speed = 100;
                    stats.stronidium = 75;
                    stats.scale = 4.0f;
                    stats.range = 10.0f;
                    break;
                case AppearanceType.VEWing: //v_ewing
                    stats.weapons = 3;
                    stats.shields = 3;
                    stats.stealth = 0;
                    stats.scanning = 1;
                    stats.speed = 100;
                    stats.scale = 4.0f;
                    stats.range = 10.0f;
                    break;
                case AppearanceType.VYWing: //v_ywing
                    stats.weapons = 4;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 0;
                    stats.speed = 60;
                    stats.stronidium = 400;
                    stats.scale = 4.0f;
                    stats.range = 15.0f;
                    break;
                case AppearanceType.StarDestroyer: //star destroyer
                    stats.weapons = 10;
                    stats.shields = 10;
                    stats.stealth = 0;
                    stats.scanning = 0;
                    stats.speed = 20;
                    stats.stronidium = 10000;
                    stats.scale = 4.0f;
                    stats.range = 25.0f;
                    break;
                case AppearanceType.MonCalaCruiser: //mon cala cruiser
                    stats.weapons = 10;
                    stats.shields = 10;
                    stats.stealth = 0;
                    stats.scanning = 0;
                    stats.speed = 20;
                    stats.stronidium = 10000;
                    stats.scale = 4.0f;
                    stats.range = 25.0f;
                    break;
            }

            return stats;
        }

        private static AppearanceType GetPCShipAppearanceByStyleID(int style)
        {
            switch (style)
            {
                case 20:
                case 22:
                    return AppearanceType.FreighterSmall;
                case 21:
                case 23:
                    return AppearanceType.SmallFighter2;
            }

            return AppearanceType.SmallShuttle3;
        }

        // Utility methods.
        // Locations are saved as one of the following
        // * A PCBaseStructureID if they are docked
        // * Planetname - Orbit if they are in orbit.
        // * Planetname - Starport if they are in a public spaceport. 
        public static bool IsLocationSpace(string location)
        {
            return location.IndexOf("Orbit") > -1;            
        }

        public static bool IsLocationPublicStarport(string location)
        {
            if (!Guid.TryParse(location, out var locationGuid)) return false;

            var starport = DataService.Starport.GetByStarportIDOrDefault(locationGuid);
            return starport != null;

        }

        public static string GetPlanetFromLocation(string location)
        {
            var hyphen = location.IndexOf(" - ");
            if (hyphen == -1)
            {
                // Ship is docked.  Get the PCBaseStructure of the dock and find its parent base.  
                // Then find the area from the resref.
                var dock = DataService.PCBaseStructure.GetByIDOrDefault(new Guid(location));

                if (dock != null)
                {
                    var parentBase = DataService.PCBase.GetByID(dock.PCBaseID);
                    var areas = NWModule.Get().Areas;

                    foreach (var area in areas)
                    {
                        if (GetResRef(area) == parentBase.AreaResref)
                        {
                            location = GetName(area);
                            hyphen = location.IndexOf(" - ");
                        }
                    }
                }
                else 
                {
                    // Not on a PC dock.  Are we on a starport dock?
                    var starport = DataService.Starport.GetByStarportIDOrDefault(new Guid(location));

                    if (starport != null)
                    {
                        return starport.PlanetName;
                    }
                }

                // Check.
                if (hyphen == -1)
                {
                    // Something went wrong.
                    LoggingService.Trace(TraceComponent.Space, "Unable to find planet name from dock area.");
                    return "";
                }
            }

            // location is now either the actual starship location, or the area name of the base.
            // And hyphen is the index of the space after the area name. 
            return location.Substring(0, hyphen);            
        }

        public static NWPlaceable GetCargoBay(uint starship, NWPlayer player)
        {
            if (!GetIsObjectValid(starship))
            {
                return null;
            }

            NWPlaceable bay = GetLocalObject(starship, "STARSHIP_RESOURCE_BAY");

            if (bay.IsValid)
            {
                NWObject accessor = bay.GetLocalObject("BAY_ACCESSOR");
                if (!accessor.IsValid)
                {
                    bay.Destroy();
                }
                else
                {
                    if (player != null && player.IsValid) player.FloatingText("Someone else is already accessing that structure's inventory. Please wait.");
                    return null;
                }
            }

            var structureID = new Guid(GetLocalString(starship, "PC_BASE_STRUCTURE_ID"));
            var structureItems = DataService.PCBaseStructureItem.GetAllByPCBaseStructureID(structureID);
            
            var location = (player != null ? player.Location : Location(starship, Vector3(1, 1, 0), 0));
            bay = CreateObject(ObjectType.Placeable, "resource_bay", location);

            SetLocalObject(starship, "STARSHIP_RESOURCE_BAY", bay.Object);
            bay.SetLocalString("PC_BASE_STRUCTURE_ID", structureID.ToString());
            
            foreach (var item in structureItems)
            {
                SerializationService.DeserializeItem(item.ItemObject, bay);
            }

            return bay;
        }

        private static void UpdateCargoBonus(uint area, NWCreature ship)
        {
            var stats = GetShipStatsByAppearance(GetAppearanceType(ship));

            var baseStructureID = GetLocalString(area, "PC_BASE_STRUCTURE_ID");

            if (string.IsNullOrWhiteSpace(baseStructureID))
            {
                LoggingService.Trace(TraceComponent.Space, "UpdateCargoBonus called for non-ship area.");
                return;
            }

            var baseStructureGuid = new Guid(baseStructureID);
            var structure = DataService.PCBaseStructure.GetByID(baseStructureGuid);
            var pcBase = DataService.PCBase.GetByID(structure.PCBaseID);
            var bay = GetCargoBay(area, null);

            ship.SetLocalInt("WEAPONS", stats.weapons + GetCargoBonus(bay, ItemPropertyType.Invalid));
            ship.SetLocalInt("SHIELDS", stats.shields + GetCargoBonus(bay, ItemPropertyType.Invalid));
            ship.SetLocalInt("STEALTH", stats.stealth + GetCargoBonus(bay, ItemPropertyType.Invalid));
            ship.SetLocalInt("SCANNING", stats.scanning + GetCargoBonus(bay, ItemPropertyType.Invalid));
            ship.SetLocalInt("SPEED", stats.speed + 25 * GetCargoBonus(bay, ItemPropertyType.Invalid));
            ship.SetLocalInt("STRONIDIUM", pcBase.ReinforcedFuel);
            ship.SetLocalInt("HP", (int) structure.Durability);
            ship.SetLocalFloat("RANGE", stats.range + GetCargoBonus(bay, ItemPropertyType.Invalid));
        }

        public static int GetCargoBonus(NWPlaceable bay, ItemPropertyType stat)
        {
            var bonus = 0;
            if (bay == null) return bonus;
            if (!bay.IsValid) return bonus;

            // Get the starship's cargo inventory and look for enhancement items. 
            foreach (var item in bay.InventoryItems)
            {
                // Find any items with the right properties to improve starship abilities.
                var prop = GetFirstItemProperty(item);

                while (GetIsItemPropertyValid(prop))
                {
                    if (GetItemPropertyType(prop) == stat) bonus += GetItemPropertyCostTableValue(prop);
                    prop = GetNextItemProperty(item);
                }
            }

            return bonus;
        }

        public static void SetShipLocation(uint area, string location)
        {
            var structureID = GetLocalString(area, "PC_BASE_STRUCTURE_ID");
            if (string.IsNullOrWhiteSpace(structureID))
            {
                LoggingService.Trace(TraceComponent.Space, "Asked to set location of invalid ship area.");
                return;
            }

            var structureGuid = new Guid(structureID);
            var structure = DataService.PCBaseStructure.GetByID(structureGuid);
            var starkillerBase = DataService.PCBase.GetByID(structure.PCBaseID);
            starkillerBase.ShipLocation = location;
            SetLocalString(area, "SHIP_LOCATION", location);
            DataService.SubmitDataChange(starkillerBase, DatabaseActionType.Update);
        }

        public static string GetShipLocation(uint area)
        {
            // We cache the location on the area to save database lookups, especially on module heartbeat.
            var location = GetLocalString(area, "SHIP_LOCATION");

            if (!string.IsNullOrWhiteSpace(location))
            {
                return location;
            }

            var structureID = GetLocalString(area, "PC_BASE_STRUCTURE_ID");
            if (string.IsNullOrWhiteSpace(structureID))
            {
                LoggingService.Trace(TraceComponent.Space, "Asked to get location of invalid ship area.");
                return "";
            }

            var structureGuid = new Guid(structureID);
            var structure = DataService.PCBaseStructure.GetByID(structureGuid);
            var starkillerBase = DataService.PCBase.GetByID(structure.PCBaseID);

            return starkillerBase.ShipLocation;
        }

        public static int PlanetToDestination (string planet)
        {
            if (planet == "Viscara")
                return (int) Planet.Viscara;
            if (planet == "Tatooine")
                return (int) Planet.Tatooine;
            if (planet == "Mon Cala")
                return (int)Planet.MonCala;
            if (planet == "Hutlar")
                return (int)Planet.Hutlar;

            return 0;
        }

        public static string DestinationToPlanet(int destination)
        {
            switch (destination)
            {
                case (int)Planet.Viscara: return "Viscara";
                case (int)Planet.Tatooine: return "Tatooine";
                case (int)Planet.MonCala: return "Mon Cala";
                case (int)Planet.Hutlar: return "Hutlar";
                default: return "";
            }
        }

        public static string[] GetHyperspaceDestinationList(PCBase pcBase)
        {
            var destinations = pcBase.Starcharts;
            var planet = GetPlanetFromLocation(pcBase.ShipLocation);

            if (destinations == null)
            {
                destinations = PlanetToDestination(planet);
                pcBase.Starcharts = destinations;
                DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);
                return new string[] { DestinationToPlanet((int)destinations) };
            }

            var list = new List<string>();

            if (((int)destinations & (int)Planet.Viscara) == (int)Planet.Viscara && PlanetToDestination(planet) != (int)Planet.Viscara) list.Add(DestinationToPlanet((int)Planet.Viscara));
            if (((int)destinations & (int)Planet.Tatooine) == (int)Planet.Tatooine && PlanetToDestination(planet) != (int)Planet.Tatooine) list.Add(DestinationToPlanet((int)Planet.Tatooine));
            if (((int)destinations & (int)Planet.MonCala) == (int)Planet.MonCala && PlanetToDestination(planet) != (int)Planet.MonCala) list.Add(DestinationToPlanet((int)Planet.MonCala));
            if (((int)destinations & (int)Planet.Hutlar) == (int)Planet.Hutlar && PlanetToDestination(planet) != (int)Planet.Hutlar) list.Add(DestinationToPlanet((int)Planet.Hutlar));

            return list.ToArray();
        }

        // Returns a Hashtable where the keys are the text to use, and the values are the PC base structure IDs of the landing pads.
        public static Hashtable GetLandingDestinationList(NWPlayer player, PCBase pcBase)
        {
            var planet = GetPlanetFromLocation(pcBase.ShipLocation);
            var landingSpots = new Hashtable();

            // First get any public starport.
            var starport = DataService.Starport.GetByPlanetNameOrDefault(planet);
            if (starport != null)
            {
                landingSpots.Add(starport.Name, starport.StarportID);
            }

            // Go through each area in the planet, find all bases for that area, and find any we have permissions to land a ship in.
            // Then check whether they have any open docks, and add those docks. 
            foreach (var area in NWModule.Get().Areas)
            {
                // Number bases in each area to ensure unique keys.  (We can't use GUIDs as keys as we need to map back from
                // the response text to the GUID).
                var Count = 1;

                if (GetName(area).StartsWith(planet))
                {
                    LoggingService.Trace(TraceComponent.Space, "Checking area " + GetName(area) + " for landing spots.");

                    // This area is on our planet.  
                    BaseService.RegisterAreaStructures(area);

                    var areaResref = GetResRef(area);
                    var pcBases = DataService.PCBase.GetAllNonApartmentPCBasesByAreaResref(areaResref);
                    foreach (var @base in pcBases)
                    {
                        LoggingService.Trace(TraceComponent.Space, "Checking base " + @base.ID.ToString() + " for landing slots.");

                        // Do we have permission to dock here?
                        if (BasePermissionService.HasBasePermission(player, @base.ID, BasePermission.CanDockStarship))
                        {
                            LoggingService.Trace(TraceComponent.Space, "Player has permission to land here.");
                            // Are there any docks in the base?
                            var structures = DataService.PCBaseStructure.GetAllByPCBaseID(@base.ID);
                            foreach (var structure in structures)
                            {
                                var baseStructure = DataService.BaseStructure.GetByID(structure.BaseStructureID);
                                if (baseStructure.BaseStructureTypeID == (int)BaseStructureType.StarshipProduction)
                                {
                                    LoggingService.Trace(TraceComponent.Space, "Found a dock with ID " + baseStructure.ID.ToString());

                                    // Found a dock.  Is it open?  Find the actual placeable object for the dock so we can check its vars.
                                    List<AreaStructure> areaStructures = BaseService.GetAreaStructures(area);
                                    foreach (var plc in areaStructures)
                                    {
                                        if (plc.PCBaseStructureID == structure.ID)
                                        {
                                            LoggingService.Trace(TraceComponent.Space, "Found placeable object.");

                                            if (plc.Structure.GetLocalInt("DOCKED_STARSHIP") == 0)
                                            {
                                                LoggingService.Trace(TraceComponent.Space, "Dock is currently open.");

                                                // We have permission to dock in this base.  Moreover, we have found a dock 
                                                // which is not occupied.  (Possibly several!).  Add each dock to our list.
                                                landingSpots.Add("Dock " + Count.ToString() + " in " + GetName(area) + " (" + PlayerService.GetPlayerEntity(@base.PlayerID).CharacterName + ")", structure.ID);
                                                Count++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return landingSpots;
        }

        public static bool DoPilotingSkillCheck(NWPlayer player, int DC, bool reRollIfFailed = false)
        {
            // Get the player's piloting skill (including gear bonuses).
            if (!player.IsPlayer) return false;
            
            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(player);
            var rank = SkillService.GetPCSkillRank(player, SkillType.Piloting);

            // Award XP. 
            var xp = SkillService.CalculateRegisteredSkillLevelAdjustedXP(250, DC, rank);

            rank += effectiveStats.Piloting;

            if (d100() <= (100 * rank) / (rank + DC) ||
                (reRollIfFailed && d100() <= (100 * rank) / (rank + DC)))
            {
                // Success!
                SkillService.GiveSkillXP(player, SkillType.Piloting, (int)xp);
                return true;
            }
            else
            {
                // Failure.
                SkillService.GiveSkillXP(player, SkillType.Piloting, (int)xp / 5);
                return false;
            }
        }

        public static void CreateShipInSpace(uint ship, NWLocation location = null)
        {
            // check that we are not already flying.
            if (((NWObject)GetLocalObject(ship, "CREATURE")).IsValid)
            {
                LoggingService.Trace(TraceComponent.Space, "Ship already exists.");
                return;
            }

            // Creates the ship instance in space.
            var shipID = GetLocalString(ship, "PC_BASE_STRUCTURE_ID");

            if (string.IsNullOrWhiteSpace(shipID))
            {
                LoggingService.Trace(TraceComponent.Space, "Error - tried to create a ship but can't find its structure ID.");
                return;
            }

            var shipGuid = new Guid(shipID);
            var shipStructure = DataService.PCBaseStructure.GetByID(shipGuid);
            var shipBase = DataService.PCBase.GetByID(shipStructure.PCBaseID);

            if (location == null)
            {
                var planet = GetPlanetFromLocation(shipBase.ShipLocation);
                // Planet names may have spaces in them - so remove any spaces before looking for the tag.
                var waypoint = GetObjectByTag(Regex.Replace(planet, @"\s+", "") + "_Orbit");
                var area = GetArea(waypoint);
                var areaName = GetName(area);
                var waypointName = GetName(waypoint);
                LoggingService.Trace(TraceComponent.Space, "Found space waypoint " + waypointName + " in " + areaName);

                if (!GetIsObjectValid(waypoint))
                {
                    // Uh oh.
                    LoggingService.Trace(TraceComponent.Space, "Could not find orbit waypoint for planet " + planet);
                    return;
                }

                location = GetLocation(waypoint);
            }

            NWCreature shipCreature = CreateObject(ObjectType.Creature, "starship" + shipBase.BuildingStyleID, location, false, shipID);

            shipCreature.SetLocalObject("AREA", ship);
            SetLocalObject(ship, "CREATURE", shipCreature);

            shipCreature.Name = GetName(ship);
            LoggingService.Trace(TraceComponent.Space, "Created ship " + shipCreature.Name + " in area " + GetName(shipCreature.Area));

            // Once the ship has spawned (and had its base stats set), adjust them for any mods we have on board.
            AssignCommand(shipCreature, () => { UpdateCargoBonus(ship, shipCreature); });
        }

        public static void RemoveShipInSpace(uint ship)
        {
            NWCreature shipCreature = GetLocalObject(ship, "CREATURE");
            if (shipCreature.IsValid)
            {
                // Remove the ship object.
                var area = GetArea(shipCreature);
                var areaName = GetName(area);
                LoggingService.Trace(TraceComponent.Space, "Removing ship " + shipCreature.Name + " from " + areaName);
                shipCreature.Destroy();
            }
            else
            {
                LoggingService.Trace(TraceComponent.Space, "Could not find ship to remove from space.");
            }
        }

        public static bool CanLandOnPlanet(uint ship)
        {
            var shipID = GetLocalString(ship, "PC_BASE_STRUCTURE_ID");
            var shipGuid = new Guid(shipID);
            var shipStructure = DataService.PCBaseStructure.GetByID(shipGuid);
            var shipBase = DataService.PCBase.GetByID(shipStructure.PCBaseID);
            var planet = GetPlanetFromLocation(shipBase.ShipLocation);

            NWCreature shipCreature = GetLocalObject(ship, "CREATURE");
            NWObject waypoint = GetObjectByTag(Regex.Replace(planet, @"\s+", "") + "_Orbit");

            if (!shipCreature.IsValid || !waypoint.IsValid || GetDistanceBetween(shipCreature, waypoint) < 10.0f)
            {
                return true;
            }

            return false;
        }

        public static void DoFlyShip(NWPlayer player, uint ship)
        {
            NWCreature shipCreature = GetLocalObject(ship, "CREATURE");
            if (shipCreature.IsPC)
            {
                player.SendMessage("Sorry, this ship already has a pilot.");
                return;
            }

            // Retrieve information about the ship.
            var shipID = GetLocalString(ship, "PC_BASE_STRUCTURE_ID");

            if (string.IsNullOrWhiteSpace(shipID))
            {
                LoggingService.Trace(TraceComponent.Space, "Error - tried to fly a ship but can't find its structure ID.");
                player.SendMessage("Sorry, we hit a problem.  Please report this message.");
                return;
            }

            // Save our location so we come back to the computer if we disconnect in space.
            PlayerService.SaveLocation(player);

            var shipGuid = new Guid(shipID);
            var shipStructure = DataService.PCBaseStructure.GetByID(shipGuid);
            var shipBase = DataService.PCBase.GetByID(shipStructure.PCBaseID);

            var shipAppearance = GetPCShipAppearanceByStyleID((int) shipBase.BuildingStyleID);

            var shipSpeed = GetShipStatsByAppearance(shipAppearance).speed + 
                            25 * GetCargoBonus(GetCargoBay(ship, null), ItemPropertyType.Invalid);

            NWPlaceable chair = GetNearestObjectByTag("pilot_chair", player);
            ClonePCAndSit(player, chair);

            // Find the dummy ship - swap the PC and the dummy ship.
            // Note that the PC is currently invisible thanks to the clone method.
            player.Chest.SetLocalInt("APPEARANCE", (int)GetAppearanceType(player));
            player.SetLocalInt("IS_SHIP", 1);
            player.SetLocalObject("AREA", ship);
            SetCreatureAppearanceType(player, shipAppearance);

            SetObjectVisualTransform(player, ObjectVisualTransform.Scale, GetShipStatsByAppearance(shipAppearance).scale);

            // Make immune to physical damage.
            ApplyEffectToObject(DurationType.Permanent, EffectDamageImmunityIncrease(DamageType.Bludgeoning, 100), player);

            player.AssignCommand(() => { ActionUnequipItem(player.LeftHand); });
            player.AssignCommand(() => { ActionUnequipItem(player.RightHand); });

            // Set the player's movement speed, improved by their skill and the Racer perk. 
            shipSpeed += SkillService.GetPCSkillRank(player, SkillType.Piloting) + PlayerStatService.GetPlayerItemEffectiveStats(player).Piloting +
                            10 * PerkService.GetCreaturePerkLevel(player, PerkType.Racer);
            if (shipSpeed > 100)
            {
                ApplyEffectToObject(DurationType.Permanent, EffectMovementSpeedIncrease(shipSpeed - 100), player);
            }
            else
            {
                ApplyEffectToObject(DurationType.Permanent, EffectMovementSpeedDecrease(100 - shipSpeed), player);
            }
            
            // Clean up the ship model.
            player.AssignCommand(() => { ActionJumpToLocation(shipCreature.Location); });
            player.SetLocalInt("MAX_HP", shipCreature.MaxHP);
            RemoveShipInSpace(ship);
            SetLocalObject(ship, "CREATURE", player);

            // Update the ship's stats for any mods we have on board (and current stronidium/HP).
            UpdateCargoBonus(ship, player);

            player.SendMessage("Type /exit to exit pilot mode.");
        }

        public static void DoStopFlyShip(NWPlayer player)
        {
            NWCreature copy = player.GetLocalObject("COPY");

            // This might be called when a player disconnects in space.  If so, save their location as
            // being on board the ship so we can send them back to the right place afterwards.
            var entity = PlayerService.GetPlayerEntity(player.GlobalID);
            var area = GetArea(copy);
            var areaResref = GetResRef(area);
            entity.LocationAreaResref = areaResref;
            entity.LocationX = copy.Position.X;
            entity.LocationY = copy.Position.Y;
            entity.LocationZ = copy.Position.Z;
            entity.LocationOrientation = (copy.Facing);
            entity.LocationInstanceID = new Guid(GetLocalString(area, "PC_BASE_STRUCTURE_ID"));

            DataService.SubmitDataChange(entity, DatabaseActionType.Update);

            // Apply ghost effects so we can stand on each other's heads, and create the ship again.
            var eGhost = EffectCutsceneGhost();
            ApplyEffectToObject(DurationType.Temporary, eGhost, player, 3.5f);
            ApplyEffectToObject(DurationType.Temporary, eGhost, copy, 3.5f);
            DeleteLocalObject(area, "CREATURE");
            CreateShipInSpace(copy.Area, player.Location);
            player.AssignCommand(() => { ClearAllActions(); ActionJumpToLocation(copy.Location); });

            // Make ourselves invisible for 2.5s and destroy the copy at the same time.  
            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Cutscene_Invisibility), player, 2.5f);
            copy.Destroy(2.5f);

            // Set our appearance back to normal (now that we're invisible). 
            SetCreatureAppearanceType(player, (AppearanceType)player.Chest.GetLocalInt("APPEARANCE"));
            SetObjectVisualTransform(player, ObjectVisualTransform.Scale, 1.0f);
            player.DeleteLocalInt("IS_SHIP");
            player.DeleteLocalObject("AREA");
            player.DeleteLocalObject("COPY");
            player.RemoveEffect(EffectTypeScript.MovementSpeedIncrease);
            player.RemoveEffect(EffectTypeScript.MovementSpeedDecrease);
            player.RemoveEffect(EffectTypeScript.DamageImmunityIncrease);

            ExportSingleCharacter(player);
        }

        public static void DoCrewGuns(NWPlayer player, uint ship)
        {
            if (((NWObject)GetLocalObject(ship, "GUNNER")).IsValid)
            {
                player.SendMessage("Someone is already crewing the guns.  Only one gunner per ship!");
                return; 
            }

            NWPlaceable chair = GetNearestObjectByTag("gunner_chair", player);
            ClonePCAndSit(player, chair);

            NWCreature shipCreature = GetLocalObject(ship, "CREATURE");
            player.Chest.SetLocalInt("APPEARANCE", (int)GetAppearanceType(player));
            SetCreatureAppearanceType(player, AppearanceType.InvisibleHumanMale);
            player.SetLocalInt("IS_GUNNER", 1);

            // Apply effects so we can't be seen or hit.
            player.AssignCommand(() => { ActionUnequipItem(player.LeftHand); });
            player.AssignCommand(() => { ActionUnequipItem(player.RightHand); });
            ApplyEffectToObject(DurationType.Permanent, EffectCutsceneGhost(), player);
            ApplyEffectToObject(DurationType.Permanent, EffectInvisibility(InvisibilityType.Normal), player);
            ApplyEffectToObject(DurationType.Permanent, EffectMovementSpeedIncrease(200), player);

            SetLocalObject(ship, "GUNNER", player);

            player.AssignCommand(() => 
            {
                ActionJumpToLocation(shipCreature.Location);
                ActionForceFollowObject(shipCreature);
                SetCommandable(false, player);
            });

            player.SendMessage("Type /exit to exit gunner mode.");
        }

        public static void DoStopCrewGuns(NWPlayer player)
        {
            SetCommandable(true, player);

            NWCreature copy = player.GetLocalObject("COPY");
            var area = GetArea(copy);

            // Apply ghost effect so we can stand on each other's heads.
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneGhost(), copy, 3.5f);
            player.AssignCommand(() => { ClearAllActions();  ActionJumpToLocation(copy.Location); });

            // Return our appearance to normal.
            SetCreatureAppearanceType(player, (AppearanceType)player.Chest.GetLocalInt("APPEARANCE"));
            player.DeleteLocalInt("IS_GUNNER");
            player.DeleteLocalObject("COPY");
            player.RemoveEffect(EffectTypeScript.MovementSpeedIncrease);

            DelayCommand(2.5f, () => 
            {
                player.RemoveEffect(EffectTypeScript.CutsceneGhost);
                player.RemoveEffect(EffectTypeScript.Invisibility);
            });

            DeleteLocalObject(area, "GUNNER");

            copy.Destroy(2.5f);
        }

        public static void LandCrew(uint ship)
        {
            // Called on landing to get the gunner and pilot (if any) back on board.
            NWPlayer gunner = GetLocalObject(ship, "GUNNER");
            if (gunner.IsValid) DoStopCrewGuns(gunner);

            NWCreature pilot = GetLocalObject(ship, "CREATURE");
            if (pilot.IsValid && pilot.IsPC)
            {
                NWPlayer pilotPlayer = GetLocalObject(ship, "CREATURE");
                DoStopFlyShip(pilotPlayer);
            }
        }

        private static void ClonePCAndSit(NWPlayer player, NWPlaceable chair)
        {
            // Create a copy of the PC and link the two. 
            NWObject copy = CopyObject(player, player.Location, OBJECT_INVALID, "spaceship_copy");
            ChangeToStandardFaction(copy, StandardFaction.Defender);

            var eInv = EffectVisualEffect(VisualEffect.Vfx_Dur_Cutscene_Invisibility);
            var eGhost = EffectCutsceneGhost();

            player.SetLocalObject("COPY", copy);
            copy.SetLocalObject("OWNER", player);

            // Make the player invisible for a short period of time, and allow the two to move through each other. 
            ApplyEffectToObject(DurationType.Permanent, eGhost, copy);
            ApplyEffectToObject(DurationType.Temporary, eInv, player, 2.5f);
            ApplyEffectToObject(DurationType.Temporary, eGhost, player, 3.5f);

            // Clear the copy's inventory and gold. (This won't clear equipped items). 
            TakeGoldFromCreature(GetGold(copy), copy, true);
            NWItem item = GetFirstItemInInventory(copy);
            while (item.IsValid)
            {
                SetDroppableFlag(item, false);
                item.Destroy();
                item = GetNextItemInInventory(copy);
            }

            // Make sure the clone isn't doing anything silly (like walking waypoints).
            // Then sit in the chair. 
            AssignCommand(copy, () => { ClearAllActions(); });
            DelayCommand(1.0f, ()=> { AssignCommand(copy, () => { ActionSit(chair); } ); });
        }

        private static void OnModuleLeave()
        {
            NWPlayer player = GetExitingObject();
            // If a player logs out in space, clean things up. 
            if (player.GetLocalInt("IS_SHIP") == 1)
            {
                DoStopFlyShip(player);
            }

            if (player.GetLocalInt("IS_GUNNER") == 1)
            {
                DoStopCrewGuns(player);
            }
        }

        private static void OnModuleNWNXChat()
        {
            // Is the speaker a pilot or gunner?
            NWPlayer speaker = OBJECT_SELF;
            if (!speaker.IsPlayer) return;

            // Ignore Tells, DM messages etc..
            if (Chat.GetChannel() != Core.NWNX.Enum.ChatChannel.PlayerTalk &&
                Chat.GetChannel() != Core.NWNX.Enum.ChatChannel.PlayerWhisper &&
                Chat.GetChannel() != Core.NWNX.Enum.ChatChannel.PlayerParty)
            {
                return;
            }

            var message = Chat.GetMessage().Trim();

            if (speaker.GetLocalInt("IS_SHIP") == 1 || speaker.GetLocalInt("IS_GUNNER") == 1)
            {
                Chat.SkipMessage();

                // Are we doing a special command?
                if (message == "/exit")
                {
                    if (speaker.GetLocalInt("IS_SHIP") == 1)
                    {
                        DoStopFlyShip(speaker);
                    }
                    else
                    {
                        DoStopCrewGuns(speaker);
                    }
                }
                else
                {
                    AssignCommand(speaker.GetLocalObject("COPY"), () => { SpeakString(message); });
                }

                return;
            }

            // Can a clone of a pilot or gunner hear the speaker?
            var nNth = 1;
            NWCreature copy = GetNearestObjectByTag("spaceship_copy", speaker, nNth);

            while (copy.IsValid)
            {
                ((NWPlayer)copy.GetLocalObject("OWNER")).SendMessage(speaker.Name + ": " + message);
                nNth++;
                copy = GetNearestObjectByTag("spaceship_copy", speaker, nNth);
            }
        }

        private static void DoImpactFeedback(uint ship, string message)
        {
            for(var creature = GetFirstObjectInArea(ship); GetIsObjectValid(creature); creature = GetNextObjectInArea(ship))
            {
                if (GetIsPC(creature) || GetIsDM(creature))
                {
                    FloatingTextStringOnCreature(message, creature);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Bump), creature);
                    // TODO - play sound.
                }
                else if (GetTag(creature) == "spaceship_copy")
                {
                    SendMessageToPC(GetLocalObject(creature, "OWNER"), message);
                }
            }
        }

        public static void CreateSpaceEncounter (NWObject trigger, NWPlayer player)
        {
            // Called in the OnEnter of encounter triggers.
            // Get the location of the player's ship.
            if (!player.IsPC || player.GetLocalInt("IS_SHIP") == 0) return;            
            var ship = player.GetLocalObject("AREA");

            var shipID = GetLocalString(ship, "PC_BASE_STRUCTURE_ID");
            var shipGuid = new Guid(shipID);
            var shipStructure = DataService.PCBaseStructure.GetByID(shipGuid);
            var shipBase = DataService.PCBase.GetByID(shipStructure.PCBaseID);
            var planet = GetPlanetFromLocation(shipBase.ShipLocation);

            LoggingService.Trace(TraceComponent.Space, "Creating space encounter for " + player.Name + " near planet " + planet);

            var encounters = DataService.SpaceEncounter.GetAllByPlanet(planet).ToList();
            var totalChance = 0;

            foreach (var encounter in encounters)
            {
                LoggingService.Trace(TraceComponent.Space, "Found encounter: " + encounter.TypeID + " with base chance " + encounter.Chance);

                if (encounter.TypeID == 1 || encounter.TypeID == 4)
                {
                    encounter.Chance += PerkService.GetCreaturePerkLevel(player, PerkType.Hunter);
                    encounter.Chance -= PerkService.GetCreaturePerkLevel(player, PerkType.Sneak);
                }
                else if (encounter.TypeID == 3)
                {
                    encounter.Chance += PerkService.GetCreaturePerkLevel(player, PerkType.Scavenger);
                }

                if (encounter.Chance < 1) encounter.Chance = 1;
                LoggingService.Trace(TraceComponent.Space, "Modified chance: " + encounter.Chance);

                totalChance += encounter.Chance;
            }

            var random = Random(totalChance - 1);

            foreach (var encounter in encounters)
            {
                if (random < encounter.Chance)
                {
                    // Process the encounter.
                    if (encounter.TypeID == 1 || encounter.TypeID == 4)
                    {
                        // For now, do pirates (4) instead of customs (1).
                        var resref = d2() == 1 ? "pirate_fighter_1" : "pirate_fighter_2";
                        NWCreature pirate = CreateObject(ObjectType.Creature, resref, trigger.Location);
                        pirate.SetLocalInt("DC", encounter.Difficulty);
                        pirate.SetLocalInt("LOOT_TABLE_ID", encounter.LootTable);
                        // TODO - play proximity alert sound.
                    }
                    else if (encounter.TypeID == 2)
                    {
                        // Asteroid!
                        if (DoPilotingSkillCheck(player, encounter.Difficulty))
                        {
                            player.SendMessage("You dodge an asteroid.  Good piloting!");
                        }
                        else
                        {
                            player.SendMessage("The asteroid hits your ship!");
                            var damage = d6(3);

                            // Apply the damage.
                            var targetHP = player.GetLocalInt("HP") - damage;

                            // Feedback.
                            if (targetHP <= 0)
                            {
                                // Boom!
                                ApplyEffectAtLocation(DurationType.Temporary, EffectVisualEffect(VisualEffect.Fnf_Implosion), player.Location, 2.0f);
                                ApplyEffectToObject(DurationType.Instant, EffectDeath(), player);
                            }
                            else
                            {
                                player.SetLocalInt("HP", targetHP);
                                player.FloatingText("Hull points: " + targetHP + "/" + player.GetLocalInt("MAX_HP"));

                                shipStructure.Durability = targetHP;
                                DataService.SubmitDataChange(shipStructure, DatabaseActionType.Update);

                            }

                            DoImpactFeedback(ship, "Something hit the hull! Hull points: " + (targetHP) + "/" + player.GetLocalInt("MAX_HP"));
                        }
                    }
                    else if (encounter.TypeID == 3)
                    { 
                        // Salvage.
                        if (DoPilotingSkillCheck(player, encounter.Difficulty))
                        {
                            player.SendMessage("You found some salvage!");

                            var structure = DataService.BaseStructure.GetByID(shipStructure.BaseStructureID);
                            var count = DataService.PCBaseStructureItem.GetAllByPCBaseStructureID(shipStructure.ID).Count() + 1;
                            if (count > (structure.ResourceStorage + shipStructure.StructureBonus))
                            {
                                player.SendMessage("Your cargo bay is full!  You weren't able to collect the salvage.");
                                return;
                            }

                            var itemDetails = LootService.PickRandomItemFromLootTable(encounter.LootTable);

                            if(itemDetails != null)
                            {
                                var tempStorage = GetObjectByTag("TEMP_ITEM_STORAGE");
                                NWItem item = CreateItemOnObject(itemDetails.Resref, tempStorage, itemDetails.Quantity);

                                // Guard against invalid resrefs and missing items.
                                if (!item.IsValid)
                                {
                                    Console.WriteLine("ERROR: Could not create salvage item with resref '" + itemDetails.Resref + "'. Is this item valid?");
                                    return;
                                }

                                if (!string.IsNullOrWhiteSpace(itemDetails.SpawnRule))
                                {
                                    var rule = SpawnService.GetSpawnRule(itemDetails.SpawnRule);
                                    rule.Run(item);
                                }

                                var dbItem = new PCBaseStructureItem
                                {
                                    PCBaseStructureID = shipStructure.ID,
                                    ItemGlobalID = item.GlobalID.ToString(),
                                    ItemName = item.Name,
                                    ItemResref = item.Resref,
                                    ItemTag = item.Tag,
                                    ItemObject = SerializationService.Serialize(item)
                                };

                                DataService.SubmitDataChange(dbItem, DatabaseActionType.Insert);
                                player.SendMessage(item.Name + " was successfully brought into your cargo bay.");
                                item.Destroy();
                            }
                        }
                        else
                        {
                            player.SendMessage("There is some debris here, but you find nothing salvageable.");
                        }
                    }

                    break;
                }
                else
                {
                    random -= encounter.Chance;
                }
            }
        }

        private static void DoSpaceAttack(NWCreature attacker, NWCreature target, bool gunner)
        {
            // Figure out whether either party is a PC ship.  If they are, the relevant area will be the starship interior area.  Else null.
            var attackerArea = attacker.IsPC ? ((NWObject)attacker.GetLocalObject("COPY")).Area : ((uint)attacker.GetLocalObject("AREA"));
            var defenderArea = target.IsPC ? ((NWObject)target.GetLocalObject("COPY")).Area : ((uint)target.GetLocalObject("AREA"));

            // Get the current stronidium reserves of the attacker and defender.
            var attackStron = attacker.GetLocalInt("STRONIDIUM");
            var defendStron = target.GetLocalInt("STRONIDIUM");

            if (attackStron == 0)
            {
                attacker.FloatingText("Out of Stronidium!");
                LoggingService.Trace(TraceComponent.Space, attacker.Name + " is out of stronidium");
                return;
            }

            // Get the current weapon and shield strength of attacker and defender.
            var attackWeapons = attacker.GetLocalInt("WEAPONS");
            if (attackWeapons > attackStron) attackWeapons = attackStron;
            var defendShields = target.GetLocalInt("SHIELDS");
            if (defendShields > defendStron) defendShields = defendStron;

            LoggingService.Trace(TraceComponent.Space, "Attacker weapons: " + attackWeapons + ", defender shields " + defendShields);

            // Get the piloting skill of the attacker and defender
            var attackerPiloting = 0;
            var defenderPiloting = 0;
            
            NWPlayer pcGunner = null;
            if (gunner && GetIsObjectValid(attackerArea)) // Should always have an area in this case but doesn't hurt to check.
            {
                pcGunner = GetLocalObject(attackerArea, "GUNNER");
                var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(pcGunner);
                attackerPiloting += SkillService.GetPCSkillRank(pcGunner, SkillType.Piloting) + effectiveStats.Piloting;

                attackerPiloting += 3 * PerkService.GetCreaturePerkLevel(pcGunner, PerkType.CrackShot);
            }

            if (attacker.IsPC)
            {
                var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(new NWPlayer(attacker));
                attackerPiloting += SkillService.GetPCSkillRank(new NWPlayer(attacker), SkillType.Piloting) + effectiveStats.Piloting;                
            }
            else
            {
                attackerPiloting += attacker.GetLocalInt("DC") > 0 ? attacker.GetLocalInt("DC") : 10;
            }
            
            if (target.IsPC)
            {
                var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(new NWPlayer(target));
                defenderPiloting += SkillService.GetPCSkillRank(new NWPlayer(target), SkillType.Piloting) + effectiveStats.Piloting;

                defenderPiloting += 3 * PerkService.GetCreaturePerkLevel(new NWPlayer(target), PerkType.Evasive);
            }
            else
            {
                defenderPiloting += target.GetLocalInt("DC") > 0 ? target.GetLocalInt("DC") : 10;
            }

            LoggingService.Trace(TraceComponent.Space, "Attacker skill " + attackerPiloting + ", defender skill " + defenderPiloting);

            // Make the shot... preparing some VFX.
            var damage = 0;
            /*
             * TODO - improve the VFX here by using a custom spell and a miss vector.  Miss vectors on EffectBeam are... not very good.
            Vector3 vAttacker = NWScript.GetPosition(attacker);
            Vector3 vDiff = NWScript.Vector3(vTarget.X - vAttacker.X, vTarget.Y - vAttacker.Y, vAttacker.Z - vTarget.Z);            
            float fAngle = NWScript.VectorToAngle(vDiff) - NWScript.GetFacing(attacker);
            float fTargetDistance = NWScript.GetDistanceBetween(attacker, target);*/

            var check = d100();
            LoggingService.Trace(TraceComponent.Space, "Check result " + check);

            if (check < (100 * attackerPiloting) / (attackerPiloting + defenderPiloting))
            {
                // Hit!
                if (attacker.IsPC) SkillService.GiveSkillXP(new NWPlayer(attacker), SkillType.Piloting, (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(100, defenderPiloting, attackerPiloting));
                if (target.IsPC) SkillService.GiveSkillXP(new NWPlayer(target), SkillType.Piloting, (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(25, attackerPiloting, defenderPiloting));
                if (gunner) SkillService.GiveSkillXP(pcGunner, SkillType.Piloting, (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(100, defenderPiloting, attackerPiloting));

                var eBeam = EffectBeam(VisualEffect.Vfx_Beam_Disintegrate, attacker,  BodyNode.Chest);
                ApplyEffectToObject(DurationType.Temporary, eBeam, target, 0.5f);

                // Reduce the attacker's Stronidium by half their weapon strength.
                attackStron -= attackWeapons/2;
                attacker.SetLocalInt("STRONIDIUM", attackStron);

                // Reduce the defender's Stronidium by half their shield strength.
                defendStron -= defendShields/2;
                target.SetLocalInt("STRONIDIUM", defendStron);

                // Calculate damage. 
                var overkill = (100 * attackerPiloting) / (attackerPiloting + defenderPiloting) - check; // how much we beat the check by.
                var bonus = overkill / 100.0f;
                LoggingService.Trace(TraceComponent.Space, "Hit! Bonus % damage: " + bonus);

                damage = (int)(attackWeapons * (1.0f + bonus)) - defendShields;
                if (damage < 0) damage = 0;

                // Apply the damage.
                var targetHP = target.GetLocalInt("HP") - damage;

                // Feedback.
                if (targetHP <= 0)
                {
                    // Boom!
                    ApplyEffectAtLocation(DurationType.Temporary, EffectVisualEffect(VisualEffect.Fnf_Implosion), target.Location, 2.0f);
                    ApplyEffectToObject(DurationType.Instant, EffectDeath(), target);
                }
                else
                {
                    target.SetLocalInt("HP", targetHP);
                    attacker.FloatingText(target.Name + ": " + targetHP + "/" + target.GetLocalInt("MAX_HP"), true);

                    if (gunner && pcGunner != null && !attacker.IsPC) pcGunner.FloatingText(target.Name + ": " + targetHP + "/" + target.GetLocalInt("MAX_HP"), true);
                    target.FloatingText("Hull points: " + targetHP + "/" + target.GetLocalInt("MAX_HP"), true);
                    if (GetIsObjectValid(defenderArea) && damage > 0) DoImpactFeedback(defenderArea, "Your ship was hit!  Hull points " + targetHP + "/" + target.GetLocalInt("MAX_HP"));
                }
            }
            else
            {
                // Miss!
                if (attacker.IsPC) SkillService.GiveSkillXP(new NWPlayer(attacker), SkillType.Piloting, (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(25, defenderPiloting, attackerPiloting));
                if (target.IsPC) SkillService.GiveSkillXP(new NWPlayer(target), SkillType.Piloting, (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(100, attackerPiloting, defenderPiloting));
                if (gunner) SkillService.GiveSkillXP(pcGunner, SkillType.Piloting, (int)SkillService.CalculateRegisteredSkillLevelAdjustedXP(25, defenderPiloting, attackerPiloting));

                // Get a miss location near the target.
                var eBeam = EffectBeam(VisualEffect.Vfx_Beam_Disintegrate, attacker, BodyNode.Chest, true);
                ApplyEffectToObject(DurationType.Temporary, eBeam, target, 0.5f);

                /* See amove comment about making a custom spell that uses this.
                Vector3 vTarget = NWScript.GetPosition(target);
                NWLocation missLoc = NWScript.Location(target.Location.Area, 
                                                NWScript.Vector3(vTarget.X + 1.0f - NWScript.IntToFloat(NWScript.Random(200))/100.0f,
                                                         vTarget.Y + 1.0f - NWScript.IntToFloat(NWScript.Random(200)) / 100.0f,
                                                         vTarget.Z),
                                                target.Location.Orientation);

                -- This doesn't work, EffectBeams can't be fired at locations.
                NWScript.ApplyEffectAtLocation(DurationType.Temporary, eBeam, missLoc, 0.5f);*/

                // Reduce the attacker's Stronidium by half their weapon strength.
                attackStron -= attackWeapons/2;
                attacker.SetLocalInt("STRONIDIUM", attackStron);
            }

            // If either ship is part of a base, update the base stronidium reserve and the structure durability.
            if (GetIsObjectValid(attackerArea))
            {
                var baseStructureID = GetLocalString(attackerArea, "PC_BASE_STRUCTURE_ID");
                var baseStructureGuid = new Guid(baseStructureID);
                var structure = DataService.PCBaseStructure.GetByID(baseStructureGuid);
                var pcBase = DataService.PCBase.GetByID(structure.PCBaseID);

                var stronLoss = pcBase.ReinforcedFuel - attackStron;

                if (stronLoss > 1)
                {
                    // Look for an engineer on board the ship who can reduce the loss. 
                    for(var obj = GetFirstObjectInArea(attackerArea); GetIsObjectValid(obj); obj = GetNextObjectInArea(attackerArea))
                    {
                        if (GetIsPC(obj))
                        {
                            stronLoss -= PerkService.GetCreaturePerkLevel(new NWPlayer(obj), PerkType.SystemsOptimization);
                            LoggingService.Trace(TraceComponent.Space, "Attacker's stronidium loss reduced by engineer, now " + stronLoss);

                            if (stronLoss < 1)
                            {
                                stronLoss = 1;
                                break;
                            }
                        }
                    }
                }

                if (stronLoss > 0)
                {
                    pcBase.ReinforcedFuel = attackStron;
                    DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);
                }
            }

            if (GetIsObjectValid(defenderArea))
            {
                var baseStructureID = GetLocalString(defenderArea, "PC_BASE_STRUCTURE_ID");
                var baseStructureGuid = new Guid(baseStructureID);
                var structure = DataService.PCBaseStructure.GetByID(baseStructureGuid);
                var pcBase = DataService.PCBase.GetByID(structure.PCBaseID);

                var stronLoss = pcBase.ReinforcedFuel - defendStron;

                if (stronLoss > 1)
                {
                    // Look for an engineer on board the ship who can reduce the loss. 
                    for(var obj = GetFirstObjectInArea(attackerArea); GetIsObjectValid(obj); obj = GetNextObjectInArea(attackerArea))
                    {
                        if (GetIsPC(obj))
                        {
                            stronLoss -= PerkService.GetCreaturePerkLevel(new NWPlayer(obj), PerkType.SystemsOptimization);
                            LoggingService.Trace(TraceComponent.Space, "Defender's stronidium loss reduced by engineer, now " + stronLoss);

                            if (stronLoss < 1)
                            {
                                stronLoss = 1;
                                break;
                            }
                        }
                    }
                }

                if (stronLoss > 0)
                {
                    pcBase.ReinforcedFuel = defendStron;
                    DataService.SubmitDataChange(pcBase, DatabaseActionType.Update);
                }

                if (damage > 0)
                {
                    structure.Durability -= damage;

                    if (structure.Durability <= 0)
                    {
                        // Boom.
                        // TODO - call base destruction.  Can't include the base service here as it would make a 
                        // circular dependency. 
                        // Besides, let's make sure there aren't any significant combat bugs before blowing things up...
                    }
                    else
                    {
                        DataService.SubmitDataChange(structure, DatabaseActionType.Update);
                    }
                }
            }
        }

        private static void OnCreatureSpawn()
        {
            NWCreature creature = OBJECT_SELF;

            // Only do things for ships. 
            var stats = GetShipStatsByAppearance(GetAppearanceType(creature));
            if (stats.scale == default(float)) return;

            SetObjectVisualTransform(creature, ObjectVisualTransform.Scale, stats.scale);
            if (String.IsNullOrWhiteSpace(creature.GetLocalString("BEHAVIOUR"))) creature.SetLocalString("BEHAVIOUR", "StarshipBehaviour");

            // Save off our stats.
            creature.SetLocalInt("HP", creature.MaxHP);
            creature.SetLocalInt("MAX_HP", creature.MaxHP);
            creature.SetLocalInt("WEAPONS", stats.weapons);
            creature.SetLocalInt("SHIELDS", stats.shields);
            creature.SetLocalInt("STEALTH", stats.stealth);
            creature.SetLocalInt("SCANNING", stats.scanning);
            creature.SetLocalInt("SPEED", stats.speed);
            creature.SetLocalInt("STRONIDIUM", stats.stronidium);
            creature.SetLocalFloat("RANGE", stats.range);

            // Make immune to physical damage.
            ApplyEffectToObject(DurationType.Permanent, EffectDamageImmunityIncrease(DamageType.Bludgeoning, 100), creature);
        }

        private static void ShootValidTarget(NWCreature creature)
        {
            var stats = GetShipStatsByAppearance(GetAppearanceType(creature));

            // Fire weapons.
            var hasGunner = false;
            var shape = Shape.SpellCylinder;
            var range = stats.range;

            var targetLocation = Location(
                creature.Area,
                BiowarePosition.GetChangedPosition(creature.Position, range, creature.Facing),
                creature.Facing + 180.0f);

            // If we have a gunner, we can fire in any direction.  If we don't, we can only fire in front of us. 
            var area = creature.GetLocalObject("AREA");
            if ((GetIsObjectValid(area) && ((NWObject)GetLocalObject(area, "GUNNER")).IsValid) || creature.GetLocalInt("HAS_GUNNER") > 0)
            {
                hasGunner = true;
                shape = Shape.Sphere;
                targetLocation = creature.Location;

                if (GetIsObjectValid(area) && ((NWObject)GetLocalObject(area, "GUNNER")).IsValid)
                {
                    range += PerkService.GetCreaturePerkLevel(GetLocalObject(area, "GUNNER"), PerkType.Sniper);
                }
            }

            NWCreature target = GetFirstObjectInShape(shape, range, targetLocation, true, ObjectType.Creature, creature.Position);
            while (target.IsValid)
            {

                if (GetIsEnemy(target, creature) == true &&
                    !target.IsDead &&
                    target.GetLocalInt("IS_GUNNER") == 0 &&
                    GetDistanceBetween(creature, target) <= range &&
                    !target.HasAnyEffect(EffectTypeScript.Invisibility, EffectTypeScript.Sanctuary))
                {
                    LoggingService.Trace(TraceComponent.Space, "Found valid target: " + target.Name);
                    DoSpaceAttack(creature, target, hasGunner);
                    break;
                }

                target = GetNextObjectInShape(shape, range, targetLocation, true, ObjectType.Creature, creature.Position);
            }
        }

        private static void OnCreatureHeartbeat()
        {
            NWCreature creature = OBJECT_SELF;

            // Only do things for armed ships. 
            if (creature.IsDead) return;
            var stats = GetShipStatsByAppearance(GetAppearanceType(creature));
            if (stats.scale == default(float)) return;
            if (creature.GetLocalInt("WEAPONS") == 0) return;

            // Ships with the ship AI package already have this heartbeat behaviour handled.
            if (creature.GetLocalString("BEHAVIOUR") == "StarshipBehaviour") return;

            // Shoot twice per round.
            ShootValidTarget(creature);            
            DelayCommand(3.0f, ()=>{ ShootValidTarget(creature); });
        }

        private static void OnModuleEquipItem()
        {
            NWPlayer equipper = GetPCItemLastEquippedBy();
            if (GetLocalBool(equipper, "IS_CUSTOMIZING_ITEM") == true) return; // Don't run heavy code when customizing equipment.

            if (equipper.GetLocalInt("IS_SHIP") > 0)
            {
                NWItem item = GetPCItemLastEquipped();

                // Using ActionUnequipItem doesn't seem to work.  So copy and destroy.
                CopyItem(item, equipper, true);
                item.Destroy();
                equipper.SendMessage("You cannot equip items while flying a ship.");
            }
        }

        public static void OnPhysicalAttacked(NWCreature creature, NWCreature attacker)
        {
            var stats = GetShipStatsByAppearance(GetAppearanceType(creature));

            if (stats.scale == default(float)) return;

            if (GetDistanceBetween(creature, attacker) < stats.range)
            {
                AssignCommand(creature, () => { SetFacingPoint(attacker.Position); });
            }
            else
            {
                AssignCommand(creature, () => { ActionMoveToObject(attacker); });
            }
        }

        public static void OnPerception(NWCreature creature, NWCreature perceived)
        {
            var stats = GetShipStatsByAppearance(GetAppearanceType(creature));

            if (stats.scale == default(float)) return;

            if (GetIsEnemy(perceived, creature) == false) return;
            if (perceived.GetLocalInt("IS_GUNNER") == 1) return;

            // Would be ideal to respect turning circles and only turn the ship a bit at a time. Less urgent now since 
            // the heartbeat code handles turning. 
            if (GetDistanceBetween(creature, perceived) < stats.range)
            {
                AssignCommand(creature, () => { ClearAllActions(); SetFacingPoint(perceived.Position); });

                // Fire guns. One definite shot...
                DoSpaceAttack(creature, perceived, creature.GetLocalInt("HAS_GUNNER") == 1);

                // ... and a second shot later in the round if there is still a valid target.  
                DelayCommand(3.0f, () => { ShootValidTarget(creature); });
            }
            else
            {
                AssignCommand(creature, () => { ActionMoveToObject(perceived, true, stats.range - 2); });
            }
        }

        private static bool AdjustFacingAndAttack(NWCreature creature)
        {
            // If we have an enemy in front of us, process them. 
            var targetLocation = Location(
                creature.Area,
                BiowarePosition.GetChangedPosition(creature.Position, 25.0f, creature.Facing),
                creature.Facing + 180.0f);

            var shape = Shape.SpellCone;           

            var hasGunner = creature.GetLocalInt("HAS_GUNNER") == 1;
            if (hasGunner)
            {
                shape = Shape.Sphere;
                targetLocation = creature.Location;
            }

            NWCreature enemy = GetFirstObjectInShape(shape, 25.0f, targetLocation, true, ObjectType.Creature);

            while (enemy.IsValid)
            {
                if (GetIsEnemy(enemy, creature))
                {
                    OnPerception(creature, enemy);
                    return true;
                }

                enemy = GetNextObjectInShape(shape, 25.0f, targetLocation, true, ObjectType.Creature);
            }

            enemy = GetNearestCreature(CreatureType.Reputation, (int)ReputationType.Enemy, creature);

            if (enemy.IsValid && GetDistanceBetween(enemy, creature) < 25.0f)
            {
                // We have an enemy but they are not in our front arc.  
                var facing = BiowarePosition.GetRelativeFacing(creature, enemy);

                // Creature facing is, for some reason, based with 0= due east and proceeding counter clockwise.
                // Convert to clockwise with due north as 0 so that we can compare it. 
                var myFacing = creature.Facing;

                if (facing > myFacing && facing - myFacing < 180.0f)
                {
                    // Increase our facing (i.e. turn left). 
                    myFacing = creature.Facing + 60.0f;
                    if (myFacing > 360.0f) myFacing -= 360.0f;
                }
                else
                {
                    // Decrease our facing (i.e. turn right).
                    myFacing = creature.Facing - 60.0f;
                    if (myFacing < 0.0f) myFacing += 360.0f;
                }

                // Move forward a little way in our new facing.  
                targetLocation = Location(
                  creature.Area,
                  BiowarePosition.GetChangedPosition(creature.Position, 5.0f, myFacing),
                  myFacing + 180.0f);

                AssignCommand(creature, () => { ActionMoveToLocation(targetLocation, true); });
            }

            return false;
        }

        // Note - this method is called by the ship behaviour method.  It's different from the generic
        // attack method above which is called for all creatures.  Thus allowing for future ships to 
        // use different AI. 
        public static void OnHeartbeat(NWCreature creature)
        {
            var stats = GetShipStatsByAppearance(GetAppearanceType(creature));
            if (stats.scale == default(float)) return;

            // Turn to face the nearest enemy if they are within range.
            if (AdjustFacingAndAttack(creature))
            {
                // Target in range and arc. Check again half way through the round. 
                DelayCommand(3.0f, () => { AdjustFacingAndAttack(creature); });
            }
            else
            {
                // Target not in range, turn!
                DelayCommand(2.0f, () => { AdjustFacingAndAttack(creature); });
                DelayCommand(4.0f, () => { AdjustFacingAndAttack(creature); });
            }
        }
    }
}
