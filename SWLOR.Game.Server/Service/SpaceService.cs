using NWN;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.SpawnRule.Contracts;
using SWLOR.Game.Server.ValueObject;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NWN.NWScript;
using BaseStructureType = SWLOR.Game.Server.Enumeration.BaseStructureType;
using BuildingType = SWLOR.Game.Server.Enumeration.BuildingType;

namespace SWLOR.Game.Server.Service
{
    public class SpaceService : ISpaceService
    {
        private readonly INWScript _;
        private readonly IBasePermissionService _perm;
        private readonly IBiowarePosition _biowarePos;
        private readonly IDataService _data;
        private readonly IErrorService _error;
        private readonly ILootService _loot;
        private readonly INWNXChat _nwnxChat;
        private readonly INWNXCreature _nwnxCreature;
        private readonly IPerkService _perk;
        private readonly IPlayerService _player;
        private readonly IPlayerStatService _playerStat;
        private readonly ISerializationService _serialization;
        private readonly ISkillService _skill;

        private const string SPACE = "SPACE"; // For tracing. 

        public SpaceService(INWScript script,
                            IBasePermissionService perm,
                            IBiowarePosition biowarePos,
                            IDataService data,
                            IErrorService error,
                            ILootService loot,
                            INWNXChat nwnxChat,
                            INWNXCreature nwnxCreature,
                            IPerkService perk,
                            IPlayerService player,
                            IPlayerStatService playerStat,
                            ISerializationService serial,
                            ISkillService skill)
        {
            _ = script;
            _biowarePos = biowarePos;
            _data = data;
            _error = error;
            _loot = loot;
            _nwnxChat = nwnxChat;
            _nwnxCreature = nwnxCreature;
            _perk = perk;
            _perm = perm;
            _player = player;
            _playerStat = playerStat;
            _serialization = serial;
            _skill = skill;
        }

        struct ShipStats
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

        private ShipStats GetShipStatsByAppearance(int appearance)
        {
            ShipStats stats = new ShipStats();

            switch (appearance)
            {
                case 870: //Tiefightersm
                    stats.weapons = 3;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 1;
                    stats.speed = 100;
                    stats.stronidium = 250;
                    stats.scale = 1.0f;
                    stats.range = 10.0f;
                    break;
                case 893: //b_SmallCargoShip
                    stats.weapons = 0;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 0;
                    stats.speed = 40;
                    stats.stronidium = 150;
                    stats.scale = 1.0f;
                    stats.range = 10.0f;
                    break;
                case 895: //b_SmallShuttle3
                    stats.weapons = 0;
                    stats.shields = 2;
                    stats.stealth = 2;
                    stats.scanning = 2;
                    stats.speed = 80;
                    stats.stronidium = 75;
                    stats.scale = 1.0f;
                    stats.range = 10.0f;
                    break;
                case 896: //b_SmallShuttle4
                    stats.weapons = 0;
                    stats.shields = 2;
                    stats.stealth = 2;
                    stats.scanning = 2;
                    stats.speed = 80;
                    stats.stronidium = 250;
                    stats.scale = 1.0f;
                    stats.range = 10.0f;
                    break;
                case 897: //b_SmallFighter1
                    stats.weapons = 3;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 1;
                    stats.speed = 100;
                    stats.stronidium = 300;
                    stats.scale = 1.0f;
                    stats.range = 10.0f;
                    break;
                case 898: //b_SmallFighter2
                    stats.weapons = 3;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 1;
                    stats.speed = 100;
                    stats.stronidium = 300;
                    stats.scale = 1.0f;
                    stats.range = 10.0f;
                    break;
                case 899: //b_SmallFighter3
                    stats.weapons = 3;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 1;
                    stats.stronidium = 300;
                    stats.speed = 100;
                    stats.scale = 1.0f;
                    stats.range = 10.0f;
                    break;
                case 964: //XWingSmall
                    stats.weapons = 3;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 1;
                    stats.stronidium = 250;
                    stats.speed = 100;
                    stats.range = 10.0f;
                    break;
                case 965: //AWingSmall
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
                case 872: //Impshuttlesm
                    stats.weapons = 0;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 1;
                    stats.speed = 80;
                    stats.stronidium = 75;
                    stats.scale = 0.25f;
                    stats.range = 10.0f;
                    break;
                case 894: //b_SmallCargoShip2
                    stats.weapons = 0;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 0;
                    stats.speed = 40;
                    stats.stronidium = 300;
                    stats.scale = 0.25f;
                    stats.range = 10.0f;
                    break;
                case 969: //d_SmallScoutship
                    stats.weapons = 0;
                    stats.shields = 2;
                    stats.stealth = 2;
                    stats.scanning = 3;
                    stats.speed = 120;
                    stats.stronidium = 150;
                    stats.scale = 0.25f;
                    stats.range = 10.0f;
                    break;
                case 973: //b_SmallFirefly
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
                case 966: //BWingSmall
                    stats.weapons = 4;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 1;
                    stats.speed = 60;
                    stats.stronidium = 300;
                    stats.scale = 4.0f;
                    stats.range = 20.0f;
                    break;
                case 967: //FreighterSmall
                    stats.weapons = 1;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 0;
                    stats.speed = 60;
                    stats.stronidium = 100;
                    stats.scale = 4.0f;
                    stats.range = 10.0f;
                    break;
                case 968: //CorvetteSmall
                    stats.weapons = 6;
                    stats.shields = 5;
                    stats.stealth = 0;
                    stats.scanning = 0;
                    stats.speed = 50;
                    stats.stronidium = 600;
                    stats.scale = 4.0f;
                    stats.range = 20.0f;
                    break;
                case 2002: //v_lambda
                    stats.weapons = 0;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 0;
                    stats.speed = 100;
                    stats.stronidium = 75;
                    stats.scale = 4.0f;
                    stats.range = 10.0f;
                    break;
                case 2003: //v_ewing
                    stats.weapons = 3;
                    stats.shields = 3;
                    stats.stealth = 0;
                    stats.scanning = 1;
                    stats.speed = 100;
                    stats.scale = 4.0f;
                    stats.range = 10.0f;
                    break;
                case 2005: //v_ywing
                    stats.weapons = 4;
                    stats.shields = 2;
                    stats.stealth = 0;
                    stats.scanning = 0;
                    stats.speed = 60;
                    stats.stronidium = 400;
                    stats.scale = 4.0f;
                    stats.range = 15.0f;
                    break;
            }

            return stats;
        }

        int GetPCShipAppearanceByStyleID(int style)
        {
            switch (style)
            {
                case 20:
                case 22:
                    return 967;
                case 21:
                case 23:
                    return 898;
            }

            return 895;
        }

        // Utility methods.
        // Locations are saved as one of the following
        // * A PCBaseStructureID if they are docked
        // * Planetname - Orbit if they are in orbit.
        // * Planetname - Starport if they are in a public spaceport. 
        public bool IsLocationSpace(string location)
        {
            return location.IndexOf("Orbit") > -1;            
        }

        public bool IsLocationPublicStarport(string location)
        {
            SpaceStarport starport = _data.SingleOrDefault<SpaceStarport>(x => x.ID.ToString() == location);
            return (starport != null);
        }

        public string GetPlanetFromLocation(string location)
        {
            int hyphen = location.IndexOf(" - ");
            if (hyphen == -1)
            {
                // Ship is docked.  Get the PCBaseStructure of the dock and find its parent base.  
                // Then find the area from the resref.
                PCBaseStructure dock = _data.SingleOrDefault<PCBaseStructure>(x => x.ID.ToString() == location);

                if (dock != null)
                {
                    PCBase parentBase = _data.SingleOrDefault<PCBase>(x => x.ID == dock.PCBaseID);
                    IEnumerable<NWArea> areas = NWModule.Get().Areas;

                    foreach (var area in areas)
                    {
                        if (_.GetResRef(area) == parentBase.AreaResref)
                        {
                            location = _.GetName(area);
                            hyphen = location.IndexOf(" - ");
                        }
                    }
                }
                else 
                {
                    // Not on a PC dock.  Are we on a starport dock?
                    SpaceStarport starport = _data.SingleOrDefault<SpaceStarport>(x => x.ID.ToString() == location);

                    if (starport != null)
                    {
                        return starport.Planet;
                    }
                }

                // Check.
                if (hyphen == -1)
                {
                    // Something went wrong.
                    _error.Trace(SPACE, "Unable to find planet name from dock area.");
                    return "";
                }
            }

            // location is now either the actual starship location, or the area name of the base.
            // And hyphen is the index of the space after the area name. 
            return location.Substring(0, hyphen);            
        }

        public NWPlaceable GetCargoBay(NWArea starship, NWPlayer player)
        {
            if (starship == null || !starship.IsValid)
            {
                return null;
            }

            NWPlaceable bay = starship.GetLocalObject("STARSHIP_RESOURCE_BAY");

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

            Guid structureID = new Guid(starship.GetLocalString("PC_BASE_STRUCTURE_ID"));
            var structureItems = _data.Where<PCBaseStructureItem>(x => x.PCBaseStructureID == structureID);
            
            NWLocation location = (player != null ? player.Location : (NWLocation) _.Location(starship, _.Vector(1, 1, 0), 0));
            bay = _.CreateObject(OBJECT_TYPE_PLACEABLE, "resource_bay", location);

            starship.SetLocalObject("STARSHIP_RESOURCE_BAY", bay.Object);
            bay.SetLocalString("PC_BASE_STRUCTURE_ID", structureID.ToString());
            
            foreach (var item in structureItems)
            {
                _serialization.DeserializeItem(item.ItemObject, bay);
            }

            return bay;
        }

        private void UpdateCargoBonus(NWArea area, NWCreature ship)
        {
            ShipStats stats = GetShipStatsByAppearance(_.GetAppearanceType(ship));

            string baseStructureID = area.GetLocalString("PC_BASE_STRUCTURE_ID");

            if (string.IsNullOrWhiteSpace(baseStructureID))
            {
                _error.Trace(SPACE, "UpdateCargoBonus called for non-ship area.");
                return;
            }

            PCBaseStructure structure = _data.Single<PCBaseStructure>(x => x.ID.ToString() == baseStructureID);
            PCBase pcBase = _data.Single<PCBase>(x => x.ID == structure.PCBaseID);
            NWPlaceable bay = GetCargoBay(area, null);

            ship.SetLocalInt("WEAPONS", stats.weapons + GetCargoBonus(bay, (int)CustomItemPropertyType.StarshipWeaponsBonus));
            ship.SetLocalInt("SHIELDS", stats.shields + GetCargoBonus(bay, (int)CustomItemPropertyType.StarshipShieldsBonus));
            ship.SetLocalInt("STEALTH", stats.stealth + GetCargoBonus(bay, (int)CustomItemPropertyType.StarshipStealthBonus));
            ship.SetLocalInt("SCANNING", stats.scanning + GetCargoBonus(bay, (int)CustomItemPropertyType.StarshipScanningBonus));
            ship.SetLocalInt("SPEED", stats.speed + 25 * GetCargoBonus(bay, (int)CustomItemPropertyType.StarshipSpeedBonus));
            ship.SetLocalInt("STRONIDIUM", pcBase.ReinforcedFuel);
            ship.SetLocalInt("HP", (int) structure.Durability);
            ship.SetLocalFloat("RANGE", stats.range + GetCargoBonus(bay, (int)CustomItemPropertyType.StarshipRangeBonus));
        }

        public int GetCargoBonus(NWPlaceable bay, int stat)
        {
            int bonus = 0;
            if (bay == null) return bonus;
            if (!bay.IsValid) return bonus;

            // Get the starship's cargo inventory and look for enhancement items. 
            foreach (var item in bay.InventoryItems)
            {
                // Find any items with the right properties to improve starship abilities.
                ItemProperty prop = _.GetFirstItemProperty(item);

                while (_.GetIsItemPropertyValid(prop) == 1)
                {
                    if (_.GetItemPropertyType(prop) == stat) bonus += _.GetItemPropertyCostTableValue(prop);
                    prop = _.GetNextItemProperty(item);
                }
            }

            return bonus;
        }

        public void SetShipLocation(NWArea area, string location)
        {
            string structureID = area.GetLocalString("PC_BASE_STRUCTURE_ID");
            if (string.IsNullOrWhiteSpace(structureID))
            {
                _error.Trace(SPACE, "Asked to set location of invalid ship area.");
                return;
            }

            PCBaseStructure structure = _data.SingleOrDefault<PCBaseStructure>(x => x.ID.ToString() == structureID);
            PCBase starkillerBase = _data.SingleOrDefault<PCBase>(x => x.ID == structure.PCBaseID);
            starkillerBase.ShipLocation = location;
            area.SetLocalString("SHIP_LOCATION", location);
            _data.SubmitDataChange(starkillerBase, DatabaseActionType.Update);
        }

        public string GetShipLocation(NWArea area)
        {
            // We cache the location on the area to save database lookups, especially on module heartbeat.
            string location = area.GetLocalString("SHIP_LOCATION");

            if (!string.IsNullOrWhiteSpace(location))
            {
                return location;
            }

            string structureID = area.GetLocalString("PC_BASE_STRUCTURE_ID");
            if (string.IsNullOrWhiteSpace(structureID))
            {
                _error.Trace(SPACE, "Asked to get location of invalid ship area.");
                return "";
            }

            PCBaseStructure structure = _data.SingleOrDefault<PCBaseStructure>(x => x.ID.ToString() == structureID);
            PCBase starkillerBase = _data.SingleOrDefault<PCBase>(x => x.ID == structure.PCBaseID);

            return starkillerBase.ShipLocation;
        }

        public int PlanetToDestination (string planet)
        {
            if (planet == "Viscara")
                return (int) Planet.Viscara;
            if (planet == "Tattooine")
                return (int) Planet.Tattooine;

            return 0;
        }

        public string DestinationToPlanet(int destination)
        {
            switch (destination)
            {
                case (int)Planet.Viscara: return "Viscara";
                case (int)Planet.Tattooine: return "Tattooine";
                default: return "";
            }
        }

        public string[] GetHyperspaceDestinationList(PCBase pcBase)
        {
            int? destinations = pcBase.Starcharts;
            string planet = GetPlanetFromLocation(pcBase.ShipLocation);

            if (destinations == null)
            {
                destinations = PlanetToDestination(planet);
                pcBase.Starcharts = destinations;
                _data.SubmitDataChange(pcBase, DatabaseActionType.Update);
                return new string[] { DestinationToPlanet((int)destinations) };
            }

            List<String> list = new List<string>();

            if (((int)destinations & (int)Planet.Viscara) == (int)Planet.Viscara && PlanetToDestination(planet) != (int)Planet.Viscara) list.Add(DestinationToPlanet((int)Planet.Viscara));
            if (((int)destinations & (int)Planet.Tattooine) == (int)Planet.Tattooine && PlanetToDestination(planet) != (int)Planet.Tattooine) list.Add(DestinationToPlanet((int)Planet.Tattooine));

            return list.ToArray();
        }

        // Returns a Hashtable where the keys are the text to use, and the values are the PC base structure IDs of the landing pads.
        public Hashtable GetLandingDestinationList(NWPlayer player, PCBase pcBase)
        {
            string planet = GetPlanetFromLocation(pcBase.ShipLocation);
            Hashtable landingSpots = new Hashtable();

            // First get any public starports.
            HashSet<SpaceStarport> starports = _data.Where<SpaceStarport>(x => x.Planet == planet);

            foreach (var starport in starports)
            {
                landingSpots.Add(starport.Name, starport.ID);
            }

            // Go through each area in the planet, find all bases for that area, and find any we have permissions to land a ship in.
            // Then check whether they have any open docks, and add those docks. 
            foreach (var area in NWModule.Get().Areas)
            {
                // Number bases in each area to ensure unique keys.  (We can't use GUIDs as keys as we need to map back from
                // the response text to the GUID).
                int Count = 1;

                if (_.GetName(area).StartsWith(planet))
                {
                    _error.Trace(SPACE, "Checking area " + _.GetName(area) + " for landing spots.");

                    // This area is on our planet.  
                    if (!area.Data.ContainsKey("BASE_SERVICE_STRUCTURES"))
                    {
                        area.Data["BASE_SERVICE_STRUCTURES"] = new List<AreaStructure>();
                    }

                    var pcBases = _data.Where<PCBase>(x => x.AreaResref == area.Resref && x.ApartmentBuildingID == null).ToList();
                    foreach (var @base in pcBases)
                    {
                        _error.Trace(SPACE, "Checking base " + @base.ID.ToString() + " for landing slots.");

                        // Do we have permission to dock here?
                        if (_perm.HasBasePermission(player, @base.ID, BasePermission.CanDockStarship))
                        {
                            _error.Trace(SPACE, "Player has permission to land here.");
                            // Are there any docks in the base?
                            var structures = _data.Where<PCBaseStructure>(x => x.PCBaseID == @base.ID);
                            foreach (var structure in structures)
                            {
                                BaseStructure baseStructure = _data.Get<BaseStructure>(structure.BaseStructureID);
                                if (baseStructure.BaseStructureTypeID == (int)BaseStructureType.StarshipProduction)
                                {
                                    _error.Trace(SPACE, "Found a dock with ID " + baseStructure.ID.ToString());

                                    // Found a dock.  Is it open?  Find the actual placeable object for the dock so we can check its vars.
                                    List<AreaStructure> areaStructures = area.Data["BASE_SERVICE_STRUCTURES"];
                                    foreach (var plc in areaStructures)
                                    {
                                        if (plc.PCBaseStructureID == structure.ID)
                                        {
                                            _error.Trace(SPACE, "Found placeable object.");

                                            if (plc.Structure.GetLocalInt("DOCKED_STARSHIP") == 0)
                                            {
                                                _error.Trace(SPACE, "Dock is currently open.");

                                                // We have permission to dock in this base.  Moreover, we have found a dock 
                                                // which is not occupied.  (Possibly several!).  Add each dock to our list.
                                                landingSpots.Add("Dock " + Count.ToString() + " in " + _.GetName(area) + " (" + _player.GetPlayerEntity(@base.PlayerID).CharacterName + ")", structure.ID);
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

        public bool DoPilotingSkillCheck(NWPlayer player, int DC, bool reRollIfFailed = false)
        {
            // Get the player's piloting skill (including gear bonuses).
            if (!player.IsPlayer) return false;
            
            EffectiveItemStats effectiveStats = _playerStat.GetPlayerItemEffectiveStats(player);
            int rank = _skill.GetPCSkillRank(player, SkillType.Piloting);

            // Award XP. 
            float xp = _skill.CalculateRegisteredSkillLevelAdjustedXP(250, DC, rank);

            rank += effectiveStats.Piloting;

            if (_.d100() <= (100 * rank) / (rank + DC) ||
                (reRollIfFailed && _.d100() <= (100 * rank) / (rank + DC)))
            {
                // Success!
                _skill.GiveSkillXP(player, SkillType.Piloting, (int)xp);
                return true;
            }
            else
            {
                // Failure.
                _skill.GiveSkillXP(player, SkillType.Piloting, (int)xp / 5);
                return false;
            }
        }

        public void CreateShipInSpace(NWArea ship, NWLocation location = null)
        {
            // check that we are not already flying.
            if (((NWObject)ship.GetLocalObject("CREATURE")).IsValid)
            {
                _error.Trace(SPACE, "Ship already exists.");
                return;
            }

            // Creates the ship instance in space.
            string shipID = ship.GetLocalString("PC_BASE_STRUCTURE_ID");

            if (string.IsNullOrWhiteSpace(shipID))
            {
                _error.Trace(SPACE, "Error - tried to create a ship but can't find its structure ID.");
                return;
            }

            PCBaseStructure shipStructure = _data.SingleOrDefault<PCBaseStructure>(x => x.ID.ToString() == shipID);
            PCBase shipBase = _data.Get<PCBase>(shipStructure.PCBaseID);

            if (location == null)
            {
                string planet = GetPlanetFromLocation(shipBase.ShipLocation);
                NWObject waypoint = _.GetObjectByTag(planet + "_Orbit");
                _error.Trace(SPACE, "Found space waypoint " + waypoint.Name + " in " + waypoint.Area.Name);

                if (!waypoint.IsValid)
                {
                    // Uh oh.
                    _error.Trace(SPACE, "Could not find orbit waypoint for planet " + planet);
                    return;
                }

                location = waypoint.Location;
            }

            NWCreature shipCreature = _.CreateObject(NWScript.OBJECT_TYPE_CREATURE, "starship" + shipBase.BuildingStyleID.ToString(), location, 0, shipID);

            shipCreature.SetLocalObject("AREA", ship);
            ship.SetLocalObject("CREATURE", shipCreature);

            shipCreature.Name = ship.Name;
            _error.Trace(SPACE, "Created ship " + shipCreature.Name + " in area " + shipCreature.Area.Name);

            // Once the ship has spawned (and had its base stats set), adjust them for any mods we have on board.
            _.AssignCommand(shipCreature, () => { UpdateCargoBonus(ship, shipCreature); });
        }

        public void RemoveShipInSpace(NWArea ship)
        {
            NWCreature shipCreature = ship.GetLocalObject("CREATURE");
            if (shipCreature.IsValid)
            {
                string shipID = ship.GetLocalString("PC_BASE_STRUCTURE_ID");
                PCBaseStructure shipStructure = _data.SingleOrDefault<PCBaseStructure>(x => x.ID.ToString() == shipID);
                PCBase shipBase = _data.Get<PCBase>(shipStructure.PCBaseID);
                
                // Remove the ship object.
                _error.Trace(SPACE, "Removing ship " + shipCreature.Name + " from " + shipCreature.Area.Name);
                shipCreature.Destroy();
            }
            else
            {
                _error.Trace(SPACE, "Could not find ship to remove from space.");
            }
        }

        public bool CanLandOnPlanet(NWArea ship)
        {
            string shipID = ship.GetLocalString("PC_BASE_STRUCTURE_ID");
            PCBaseStructure shipStructure = _data.SingleOrDefault<PCBaseStructure>(x => x.ID.ToString() == shipID);
            PCBase shipBase = _data.Get<PCBase>(shipStructure.PCBaseID);
            string planet = GetPlanetFromLocation(shipBase.ShipLocation);

            NWCreature shipCreature = ship.GetLocalObject("CREATURE");
            NWObject waypoint = _.GetObjectByTag(planet + "_Orbit");

            if (!shipCreature.IsValid || !waypoint.IsValid || _.GetDistanceBetween(shipCreature, waypoint) < 10.0f)
            {
                return true;
            }

            return false;
        }

        public void DoFlyShip(NWPlayer player, NWArea ship)
        {
            NWCreature shipCreature = ship.GetLocalObject("CREATURE");
            if (shipCreature.IsPC)
            {
                player.SendMessage("Sorry, this ship already has a pilot.");
                return;
            }

            // Retrieve information about the ship.
            string shipID = ship.GetLocalString("PC_BASE_STRUCTURE_ID");

            if (string.IsNullOrWhiteSpace(shipID))
            {
                _error.Trace(SPACE, "Error - tried to fly a ship but can't find its structure ID.");
                player.SendMessage("Sorry, we hit a problem.  Please report this message.");
                return;
            }

            // Save our location so we come back to the computer if we disconnect in space.
            _player.SaveLocation(player);

            PCBaseStructure shipStructure = _data.SingleOrDefault<PCBaseStructure>(x => x.ID.ToString() == shipID);
            PCBase shipBase = _data.Get<PCBase>(shipStructure.PCBaseID);

            int shipAppearance = GetPCShipAppearanceByStyleID((int) shipBase.BuildingStyleID);

            int shipSpeed = GetShipStatsByAppearance(shipAppearance).speed + 
                            25 * GetCargoBonus(GetCargoBay(ship, null), (int)CustomItemPropertyType.StarshipSpeedBonus);

            NWPlaceable chair = _.GetNearestObjectByTag("pilot_chair", player);
            ClonePCAndSit(player, chair);

            // Find the dummy ship - swap the PC and the dummy ship.
            // Note that the PC is currently invisible thanks to the clone method.
            player.Chest.SetLocalInt("APPEARANCE", _.GetAppearanceType(player));
            player.SetLocalInt("IS_SHIP", 1);
            player.SetLocalObject("AREA", ship);
            _.SetCreatureAppearanceType(player, shipAppearance);

            _.SetObjectVisualTransform(player, OBJECT_VISUAL_TRANSFORM_SCALE, GetShipStatsByAppearance(shipAppearance).scale);

            // Make immune to physical damage.
            _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, _.EffectDamageImmunityIncrease(DAMAGE_TYPE_BLUDGEONING, 100), player);

            player.AssignCommand(() => { _.ActionUnequipItem(player.LeftHand); });
            player.AssignCommand(() => { _.ActionUnequipItem(player.RightHand); });

            // Set the player's movement speed, improved by their skill and the Racer perk. 
            shipSpeed += _skill.GetPCSkillRank(player, SkillType.Piloting) + _playerStat.GetPlayerItemEffectiveStats(player).Piloting +
                            10 * _perk.GetPCPerkLevel(player, PerkType.Racer);
            if (shipSpeed > 100)
            {
                _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, _.EffectMovementSpeedIncrease(shipSpeed - 100), player);
            }
            else
            {
                _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, _.EffectMovementSpeedDecrease(100 - shipSpeed), player);
            }
            
            // Clean up the ship model.
            player.AssignCommand(() => { _.ActionJumpToLocation(shipCreature.Location); });
            player.SetLocalInt("MAX_HP", shipCreature.MaxHP);
            RemoveShipInSpace(ship);
            ship.SetLocalObject("CREATURE", player);

            // Update the ship's stats for any mods we have on board (and current stronidium/HP).
            UpdateCargoBonus(ship, player);

            player.SendMessage("Type /exit to exit pilot mode.");
        }

        public void DoStopFlyShip(NWPlayer player)
        {
            NWCreature copy = player.GetLocalObject("COPY");

            // This might be called when a player disconnects in space.  If so, save their location as
            // being on board the ship so we can send them back to the right place afterwards.
            Player entity = _player.GetPlayerEntity(player.GlobalID);
            entity.LocationAreaResref = copy.Area.Resref;
            entity.LocationX = copy.Position.m_X;
            entity.LocationY = copy.Position.m_Y;
            entity.LocationZ = copy.Position.m_Z;
            entity.LocationOrientation = (copy.Facing);
            entity.LocationInstanceID = new Guid(copy.Area.GetLocalString("PC_BASE_STRUCTURE_ID"));

            _data.SubmitDataChange(entity, DatabaseActionType.Update);

            // Apply ghost effects so we can stand on each other's heads, and create the ship again.
            Effect eGhost = _.EffectCutsceneGhost();
            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, eGhost, player, 3.5f);
            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, eGhost, copy, 3.5f);
            copy.Area.DeleteLocalObject("CREATURE");
            CreateShipInSpace(copy.Area, player.Location);
            player.AssignCommand(() => { _.ClearAllActions(); _.ActionJumpToLocation(copy.Location); });

            // Make ourselves invisible for 2.5s and destroy the copy at the same time.  
            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectVisualEffect(VFX_DUR_CUTSCENE_INVISIBILITY), player, 2.5f);
            copy.Destroy(2.5f);

            // Set our appearance back to normal (now that we're invisible). 
            _.SetCreatureAppearanceType(player, player.Chest.GetLocalInt("APPEARANCE"));
            _.SetObjectVisualTransform(player, OBJECT_VISUAL_TRANSFORM_SCALE, 1.0f);
            player.DeleteLocalInt("IS_SHIP");
            player.DeleteLocalObject("AREA");
            player.DeleteLocalObject("COPY");
            player.RemoveEffect(EFFECT_TYPE_MOVEMENT_SPEED_INCREASE);
            player.RemoveEffect(EFFECT_TYPE_MOVEMENT_SPEED_DECREASE);
            player.RemoveEffect(EFFECT_TYPE_DAMAGE_IMMUNITY_INCREASE);

            _.ExportSingleCharacter(player);
        }

        public void DoCrewGuns(NWPlayer player, NWArea ship)
        {
            if (((NWObject)ship.GetLocalObject("GUNNER")).IsValid)
            {
                player.SendMessage("Someone is already crewing the guns.  Only one gunner per ship!");
                return; 
            }

            NWPlaceable chair = _.GetNearestObjectByTag("gunner_chair", player);
            ClonePCAndSit(player, chair);

            NWCreature shipCreature = ship.GetLocalObject("CREATURE");
            player.Chest.SetLocalInt("APPEARANCE", _.GetAppearanceType(player));
            _.SetCreatureAppearanceType(player, APPEARANCE_TYPE_INVISIBLE_HUMAN_MALE);
            player.SetLocalInt("IS_GUNNER", 1);

            // Apply effects so we can't be seen or hit.
            player.AssignCommand(() => { _.ActionUnequipItem(player.LeftHand); });
            player.AssignCommand(() => { _.ActionUnequipItem(player.RightHand); });
            _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, _.EffectCutsceneGhost(), player);
            _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, _.EffectInvisibility(INVISIBILITY_TYPE_NORMAL), player);
            _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, _.EffectMovementSpeedIncrease(200), player);

            ship.SetLocalObject("GUNNER", player);

            player.AssignCommand(() => 
            {
                _.ActionJumpToLocation(shipCreature.Location);
                _.ActionForceFollowObject(shipCreature);
                _.SetCommandable(0, player);
            });

            player.SendMessage("Type /exit to exit gunner mode.");
        }

        public void DoStopCrewGuns(NWPlayer player)
        {
            _.SetCommandable(1, player);

            NWCreature copy = player.GetLocalObject("COPY");

            // Apply ghost effect so we can stand on each other's heads.
            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectCutsceneGhost(), copy, 3.5f);
            player.AssignCommand(() => { _.ClearAllActions();  _.ActionJumpToLocation(copy.Location); });

            // Return our appearance to normal.
            _.SetCreatureAppearanceType(player, player.Chest.GetLocalInt("APPEARANCE"));
            player.DeleteLocalInt("IS_GUNNER");
            player.DeleteLocalObject("COPY");
            player.RemoveEffect(EFFECT_TYPE_MOVEMENT_SPEED_INCREASE);

            _.DelayCommand(2.5f, () => 
            {
                player.RemoveEffect(EFFECT_TYPE_CUTSCENEGHOST);
                player.RemoveEffect(EFFECT_TYPE_INVISIBILITY);
            });

            copy.Area.DeleteLocalObject("GUNNER");

            copy.Destroy(2.5f);
        }

        public void LandCrew(NWArea ship)
        {
            // Called on landing to get the gunner and pilot (if any) back on board.
            NWPlayer gunner = ship.GetLocalObject("GUNNER");
            if (gunner.IsValid) DoStopCrewGuns(gunner);

            NWCreature pilot = ship.GetLocalObject("CREATURE");
            if (pilot.IsValid && pilot.IsPC)
            {
                NWPlayer pilotPlayer = ship.GetLocalObject("CREATURE");
                DoStopFlyShip(pilotPlayer);
            }
        }

        private void ClonePCAndSit(NWPlayer player, NWPlaceable chair)
        {
            // Create a copy of the PC and link the two. 
            NWObject copy = _.CopyObject(player, player.Location, NWN.Object.OBJECT_INVALID, "spaceship_copy");
            _.ChangeToStandardFaction(copy, STANDARD_FACTION_DEFENDER);

            Effect eInv = _.EffectVisualEffect(VFX_DUR_CUTSCENE_INVISIBILITY);
            Effect eGhost = _.EffectCutsceneGhost();

            player.SetLocalObject("COPY", copy);
            copy.SetLocalObject("OWNER", player);

            // Make the player invisible for a short period of time, and allow the two to move through each other. 
            _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, eGhost, copy);
            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, eInv, player, 2.5f);
            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, eGhost, player, 3.5f);

            // Clear the copy's inventory and gold. (This won't clear equipped items). 
            _.TakeGoldFromCreature(_.GetGold(copy), copy, 1);
            NWItem item = _.GetFirstItemInInventory(copy);
            while (item.IsValid)
            {
                _.SetDroppableFlag(item, FALSE);
                item.Destroy();
                item = _.GetNextItemInInventory(copy);
            }

            // Make sure the clone isn't doing anything silly (like walking waypoints).
            // Then sit in the chair. 
            _.AssignCommand(copy, () => { _.ClearAllActions(); });
            _.DelayCommand(1.0f, ()=> { _.AssignCommand(copy, () => { _.ActionSit(chair); } ); });
        }

        public void OnModuleLeave(NWPlayer player)
        {
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

        public void OnNWNXChat()
        {
            // Is the speaker a pilot or gunner?
            NWPlayer speaker = NWN.Object.OBJECT_SELF;
            if (!speaker.IsPlayer) return;

            string message = _nwnxChat.GetMessage().Trim();

            if (speaker.GetLocalInt("IS_SHIP") == 1 || speaker.GetLocalInt("IS_GUNNER") == 1)
            {
                _nwnxChat.SkipMessage();

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
                    _.AssignCommand(speaker.GetLocalObject("COPY"), () => { _.SpeakString(message); });
                }

                return;
            }

            // Can a clone of a pilot or gunner hear the speaker?
            int nNth = 1;
            NWCreature copy = _.GetNearestObjectByTag("spaceship_copy", speaker, nNth);

            while (copy.IsValid)
            {
                ((NWPlayer)copy.GetLocalObject("OWNER")).SendMessage(speaker.Name + ": " + message);
                nNth++;
                copy = _.GetNearestObjectByTag("spaceship_copy", speaker, nNth);
            }
        }

        private void DoImpactFeedback(NWArea ship, string message)
        {
            foreach (var creature in ship.Objects)
            {
                if (creature.IsPC || creature.IsDM)
                {
                    _.FloatingTextStringOnCreature(message, creature);
                    _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectVisualEffect(VFX_FNF_SCREEN_BUMP), creature);
                    // TODO - play sound.
                }
                else if (creature.Tag == "spaceship_copy")
                {
                    ((NWPlayer)creature.GetLocalObject("OWNER")).SendMessage(message);
                }
            }
        }

        public void CreateSpaceEncounter (NWObject trigger, NWPlayer player)
        {
            // Called in the OnEnter of encounter triggers.
            // Get the location of the player's ship.
            if (!player.IsPC || player.GetLocalInt("IS_SHIP") == 0) return;            
            NWArea ship = player.GetLocalObject("AREA");

            string shipID = ship.GetLocalString("PC_BASE_STRUCTURE_ID");
            PCBaseStructure shipStructure = _data.SingleOrDefault<PCBaseStructure>(x => x.ID.ToString() == shipID);
            PCBase shipBase = _data.Get<PCBase>(shipStructure.PCBaseID);
            string planet = GetPlanetFromLocation(shipBase.ShipLocation);

            _error.Trace(SPACE, "Creating space encounter for " + player.Name + " near planet " + planet);

            HashSet<SpaceEncounter> encounters = _data.Where<SpaceEncounter>(x => x.Planet == planet);
            int totalChance = 0;

            foreach (var encounter in encounters)
            {
                _error.Trace(SPACE, "Found encounter: " + encounter.Type + " with base chance " + encounter.Chance);

                if (encounter.Type == 1 || encounter.Type == 4)
                {
                    encounter.Chance += _perk.GetPCPerkLevel(player, PerkType.Hunter);
                    encounter.Chance -= _perk.GetPCPerkLevel(player, PerkType.Sneak);
                }
                else if (encounter.Type == 3)
                {
                    encounter.Chance += _perk.GetPCPerkLevel(player, PerkType.Scavenger);
                }

                if (encounter.Chance < 1) encounter.Chance = 1;
                _error.Trace(SPACE, "Modified chance: " + encounter.Chance);

                totalChance += encounter.Chance;
            }

            int random = _.Random(totalChance - 1);

            foreach (var encounter in encounters)
            {
                if (random < encounter.Chance)
                {
                    // Process the encounter.
                    if (encounter.Type == 1 || encounter.Type == 4)
                    {
                        // For now, do pirates (4) instead of customs (1).
                        string resref = _.d2() == 1 ? "pirate_fighter_1" : "pirate_fighter_2";
                        NWCreature pirate = _.CreateObject(OBJECT_TYPE_CREATURE, resref, trigger.Location);
                        pirate.SetLocalInt("DC", encounter.Difficulty);
                        pirate.SetLocalInt("LOOT_TABLE_ID", encounter.LootTable);
                        // TODO - play proximity alert sound.
                    }
                    else if (encounter.Type == 2)
                    {
                        // Asteroid!
                        if (DoPilotingSkillCheck(player, encounter.Difficulty))
                        {
                            player.SendMessage("You dodge an asteroid.  Good piloting!");
                        }
                        else
                        {
                            player.SendMessage("The asteroid hits your ship!");
                            int damage = _.d6(3);

                            // Apply the damage.
                            int targetHP = player.GetLocalInt("HP") - damage;

                            // Feedback.
                            if (targetHP <= 0)
                            {
                                // Boom!
                                _.ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, _.EffectVisualEffect(VFX_FNF_IMPLOSION), player.Location, 2.0f);
                                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectDeath(), player);
                            }
                            else
                            {
                                player.SetLocalInt("HP", targetHP);
                                player.FloatingText("Hull points: " + targetHP + "/" + player.GetLocalInt("MAX_HP"));

                                shipStructure.Durability = targetHP;
                                _data.SubmitDataChange(shipStructure, DatabaseActionType.Update);

                            }

                            DoImpactFeedback(ship, "Something hit the hull! Hull points: " + (targetHP) + "/" + player.GetLocalInt("MAX_HP"));
                        }
                    }
                    else if (encounter.Type == 3)
                    { 
                        // Salvage.
                        if (DoPilotingSkillCheck(player, encounter.Difficulty))
                        {
                            player.SendMessage("You found some salvage!");

                            BaseStructure structure = _data.Get<BaseStructure>(shipStructure.BaseStructureID);
                            int count = _data.Where<PCBaseStructureItem>(x => x.PCBaseStructureID == shipStructure.ID).Count() + 1;
                            if (count > (structure.ResourceStorage + shipStructure.StructureBonus))
                            {
                                player.SendMessage("Your cargo bay is full!  You weren't able to collect the salvage.");
                                return;
                            }

                            var itemDetails = _loot.PickRandomItemFromLootTable(encounter.LootTable);

                            var tempStorage = _.GetObjectByTag("TEMP_ITEM_STORAGE");
                            NWItem item = _.CreateItemOnObject(itemDetails.Resref, tempStorage, itemDetails.Quantity);

                            // Guard against invalid resrefs and missing items.
                            if (!item.IsValid)
                            {
                                Console.WriteLine("ERROR: Could not create salvage item with resref '" + itemDetails.Resref + "'. Is this item valid?");
                                return;
                            }

                            if (!string.IsNullOrWhiteSpace(itemDetails.SpawnRule))
                            {
                                App.ResolveByInterface<ISpawnRule>("SpawnRule." + itemDetails.SpawnRule, action =>
                                {
                                    action.Run(item);
                                });
                            }

                            var dbItem = new PCBaseStructureItem
                            {
                                PCBaseStructureID = shipStructure.ID,
                                ItemGlobalID = item.GlobalID.ToString(),
                                ItemName = item.Name,
                                ItemResref = item.Resref,
                                ItemTag = item.Tag,
                                ItemObject = _serialization.Serialize(item)
                            };

                            _data.SubmitDataChange(dbItem, DatabaseActionType.Insert);
                            player.SendMessage(item.Name + " was successfully brought into your cargo bay.");
                            item.Destroy();
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

        private void DoSpaceAttack(NWCreature attacker, NWCreature target, bool gunner)
        {
            // Figure out whether either party is a PC ship.  If they are, the relevant area will be the starship interior area.  Else null.
            NWArea attackerArea = attacker.IsPC ? ((NWObject)attacker.GetLocalObject("COPY")).Area : ((NWArea)attacker.GetLocalObject("AREA"));
            NWArea defenderArea = target.IsPC ? ((NWObject)target.GetLocalObject("COPY")).Area : ((NWArea)target.GetLocalObject("AREA"));

            // Get the current stronidium reserves of the attacker and defender.
            int attackStron = attacker.GetLocalInt("STRONIDIUM");
            int defendStron = target.GetLocalInt("STRONIDIUM");

            if (attackStron == 0)
            {
                attacker.FloatingText("Out of Stronidium!");
                _error.Trace(SPACE, attacker.Name + " is out of stronidium");
                return;
            }

            // Get the current weapon and shield strength of attacker and defender.
            int attackWeapons = attacker.GetLocalInt("WEAPONS");
            if (attackWeapons > attackStron) attackWeapons = attackStron;
            int defendShields = target.GetLocalInt("SHIELDS");
            if (defendShields > defendStron) defendShields = defendStron;

            _error.Trace(SPACE, "Attacker weapons: " + attackWeapons + ", defender shields " + defendShields);

            // Get the piloting skill of the attacker and defender
            int attackerPiloting = 0;
            int defenderPiloting = 0;
            
            NWPlayer pcGunner = null;
            if (gunner && attackerArea != null) // Should always have an area in this case but doesn't hurt to check.
            {
                pcGunner = attackerArea.GetLocalObject("GUNNER");
                EffectiveItemStats effectiveStats = _playerStat.GetPlayerItemEffectiveStats(pcGunner);
                attackerPiloting += _skill.GetPCSkillRank(pcGunner, SkillType.Piloting) + effectiveStats.Piloting;

                attackerPiloting += 3 * _perk.GetPCPerkLevel(pcGunner, PerkType.CrackShot);
            }

            if (attacker.IsPC)
            {
                EffectiveItemStats effectiveStats = _playerStat.GetPlayerItemEffectiveStats(new NWPlayer(attacker));
                attackerPiloting += _skill.GetPCSkillRank(new NWPlayer(attacker), SkillType.Piloting) + effectiveStats.Piloting;                
            }
            else
            {
                attackerPiloting += attacker.GetLocalInt("DC") > 0 ? attacker.GetLocalInt("DC") : 10;
            }
            
            if (target.IsPC)
            {
                EffectiveItemStats effectiveStats = _playerStat.GetPlayerItemEffectiveStats(new NWPlayer(target));
                defenderPiloting += _skill.GetPCSkillRank(new NWPlayer(target), SkillType.Piloting) + effectiveStats.Piloting;

                defenderPiloting += 3 * _perk.GetPCPerkLevel(new NWPlayer(target), PerkType.Evasive);
            }
            else
            {
                defenderPiloting += target.GetLocalInt("DC") > 0 ? target.GetLocalInt("DC") : 10;
            }

            _error.Trace(SPACE, "Attacker skill " + attackerPiloting + ", defender skill " + defenderPiloting);

            // Make the shot... preparing some VFX.
            int damage = 0;
            /*
             * TODO - improve the VFX here by using a custom spell and a miss vector.  Miss vectors on EffectBeam are... not very good.
            Vector vAttacker = _.GetPosition(attacker);
            Vector vDiff = _.Vector(vTarget.m_X - vAttacker.m_X, vTarget.m_Y - vAttacker.m_Y, vAttacker.m_Z - vTarget.m_Z);            
            float fAngle = _.VectorToAngle(vDiff) - _.GetFacing(attacker);
            float fTargetDistance = _.GetDistanceBetween(attacker, target);*/

            int check = _.d100();
            _error.Trace(SPACE, "Check result " + check);

            if (check < (100 * attackerPiloting) / (attackerPiloting + defenderPiloting))
            {
                // Hit!
                if (attacker.IsPC) _skill.GiveSkillXP(new NWPlayer(attacker), SkillType.Piloting, (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(100, defenderPiloting, attackerPiloting));
                if (target.IsPC) _skill.GiveSkillXP(new NWPlayer(target), SkillType.Piloting, (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(25, attackerPiloting, defenderPiloting));
                if (gunner) _skill.GiveSkillXP(pcGunner, SkillType.Piloting, (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(100, defenderPiloting, attackerPiloting));

                Effect eBeam = _.EffectBeam(447, attacker, BODY_NODE_CHEST);
                _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, eBeam, target, 0.5f);

                // Reduce the attacker's Stronidium by half their weapon strength.
                attackStron -= attackWeapons/2;
                attacker.SetLocalInt("STRONIDIUM", attackStron);

                // Reduce the defender's Stronidium by half their shield strength.
                defendStron -= defendShields/2;
                target.SetLocalInt("STRONIDIUM", defendStron);

                // Calculate damage. 
                int overkill = (100 * attackerPiloting) / (attackerPiloting + defenderPiloting) - check; // how much we beat the check by.
                float bonus = (float)overkill / 100.0f;
                _error.Trace(SPACE, "Hit! Bonus % damage: " + bonus);

                damage = (int)((float)attackWeapons * (1.0f + bonus)) - defendShields;
                if (damage < 0) damage = 0;

                // Apply the damage.
                int targetHP = target.GetLocalInt("HP") - damage;

                // Feedback.
                if (targetHP <= 0)
                {
                    // Boom!
                    _.ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, _.EffectVisualEffect(VFX_FNF_IMPLOSION), target.Location, 2.0f);
                    _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectDeath(), target);
                }
                else
                {
                    target.SetLocalInt("HP", targetHP);
                    attacker.FloatingText(target.Name + ": " + targetHP + "/" + target.GetLocalInt("MAX_HP"), true);

                    if (gunner && !attacker.IsPC) pcGunner.FloatingText(target.Name + ": " + targetHP + "/" + target.GetLocalInt("MAX_HP"), true);
                    target.FloatingText("Hull points: " + targetHP + "/" + target.GetLocalInt("MAX_HP"), true);
                    if (defenderArea.IsValid && damage > 0) DoImpactFeedback(defenderArea, "Your ship was hit!  Hull points " + targetHP + "/" + target.GetLocalInt("MAX_HP"));
                }
            }
            else
            {
                // Miss!
                if (attacker.IsPC) _skill.GiveSkillXP(new NWPlayer(attacker), SkillType.Piloting, (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(25, defenderPiloting, attackerPiloting));
                if (target.IsPC) _skill.GiveSkillXP(new NWPlayer(target), SkillType.Piloting, (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(100, attackerPiloting, defenderPiloting));
                if (gunner) _skill.GiveSkillXP(pcGunner, SkillType.Piloting, (int)_skill.CalculateRegisteredSkillLevelAdjustedXP(25, defenderPiloting, attackerPiloting));

                // Get a miss location near the target.
                Effect eBeam = _.EffectBeam(447, attacker, BODY_NODE_CHEST, 1);
                _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, eBeam, target, 0.5f);

                /* See amove comment about making a custom spell that uses this.
                Vector vTarget = _.GetPosition(target);
                NWLocation missLoc = _.Location(target.Location.Area, 
                                                _.Vector(vTarget.m_X + 1.0f - _.IntToFloat(_.Random(200))/100.0f,
                                                         vTarget.m_Y + 1.0f - _.IntToFloat(_.Random(200)) / 100.0f,
                                                         vTarget.m_Z),
                                                target.Location.Orientation);

                -- This doesn't work, EffectBeams can't be fired at locations.
                _.ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, eBeam, missLoc, 0.5f);*/

                // Reduce the attacker's Stronidium by half their weapon strength.
                attackStron -= attackWeapons/2;
                attacker.SetLocalInt("STRONIDIUM", attackStron);
            }

            // If either ship is part of a base, update the base stronidium reserve and the structure durability.
            if (attackerArea.IsValid)
            {
                string baseStructureID = attackerArea.GetLocalString("PC_BASE_STRUCTURE_ID");
                PCBaseStructure structure = _data.Single<PCBaseStructure>(x => x.ID.ToString() == baseStructureID);
                PCBase pcBase = _data.Single<PCBase>(x => x.ID == structure.PCBaseID);

                int stronLoss = pcBase.ReinforcedFuel - attackStron;

                if (stronLoss > 1)
                {
                    // Look for an engineer on board the ship who can reduce the loss. 
                    foreach (NWObject obj in attackerArea.Objects)
                    {
                        if (obj.IsPC)
                        {
                            stronLoss -= _perk.GetPCPerkLevel(new NWPlayer(obj), PerkType.SystemsOptimization);
                            _error.Trace(SPACE, "Attacker's stronidium loss reduced by engineer, now " + stronLoss);

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
                    _data.SubmitDataChange(pcBase, DatabaseActionType.Update);
                }
            }

            if (defenderArea.IsValid)
            {
                string baseStructureID = defenderArea.GetLocalString("PC_BASE_STRUCTURE_ID");
                PCBaseStructure structure = _data.Single<PCBaseStructure>(x => x.ID.ToString() == baseStructureID);
                PCBase pcBase = _data.Single<PCBase>(x => x.ID == structure.PCBaseID);

                int stronLoss = pcBase.ReinforcedFuel - defendStron;

                if (stronLoss > 1)
                {
                    // Look for an engineer on board the ship who can reduce the loss. 
                    foreach (NWObject obj in attackerArea.Objects)
                    {
                        if (obj.IsPC)
                        {
                            stronLoss -= _perk.GetPCPerkLevel(new NWPlayer(obj), PerkType.SystemsOptimization);
                            _error.Trace(SPACE, "Defender's stronidium loss reduced by engineer, now " + stronLoss);

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
                    _data.SubmitDataChange(pcBase, DatabaseActionType.Update);
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
                        _data.SubmitDataChange(structure, DatabaseActionType.Update);
                    }
                }
            }
        }

        public void OnCreatureSpawn(NWCreature creature)
        {
            // Only do things for ships. 
            ShipStats stats = GetShipStatsByAppearance(_.GetAppearanceType(creature));
            if (stats.scale == default(float)) return;

            _.SetObjectVisualTransform(creature, OBJECT_VISUAL_TRANSFORM_SCALE, stats.scale);
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
            _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, _.EffectDamageImmunityIncrease(DAMAGE_TYPE_BLUDGEONING, 100), creature);
        }

        private void ShootValidTarget(NWCreature creature)
        {
            ShipStats stats = GetShipStatsByAppearance(_.GetAppearanceType(creature));

            // Fire weapons.
            bool hasGunner = false;
            int shape = SHAPE_SPELLCYLINDER;
            float range = stats.range;

            Location targetLocation = _.Location(
                creature.Area.Object,
                _biowarePos.GetChangedPosition(creature.Position, range, creature.Facing),
                creature.Facing + 180.0f);

            // If we have a gunner, we can fire in any direction.  If we don't, we can only fire in front of us. 
            NWArea area = creature.GetLocalObject("AREA");
            if ((area.IsValid && ((NWObject)area.GetLocalObject("GUNNER")).IsValid) || creature.GetLocalInt("HAS_GUNNER") > 0)
            {
                hasGunner = true;
                shape = SHAPE_SPHERE;
                targetLocation = creature.Location;

                // TODO: increase range by gunner's perk level.
                range += _perk.GetPCPerkLevel(area.GetLocalObject("GUNNER"), PerkType.Sniper);
            }

            NWCreature target = _.GetFirstObjectInShape(shape, range, targetLocation, TRUE, OBJECT_TYPE_CREATURE, creature.Position);
            while (target.IsValid)
            {

                if (_.GetIsEnemy(target, creature) == TRUE &&
                    !target.IsDead &&
                    _.GetDistanceBetween(creature, target) <= range &&
                    !target.HasAnyEffect(EFFECT_TYPE_INVISIBILITY, EFFECT_TYPE_SANCTUARY))
                {
                    _error.Trace(SPACE, "Found valid target: " + target.Name);
                    DoSpaceAttack(creature, target, hasGunner);
                    break;
                }

                target = _.GetNextObjectInShape(shape, range, targetLocation, TRUE, OBJECT_TYPE_CREATURE, creature.Position);
            }
        }

        public void OnCreatureHeartbeat(NWCreature creature)
        {
            // Only do things for armed ships. 
            if (creature.IsDead) return;
            ShipStats stats = GetShipStatsByAppearance(_.GetAppearanceType(creature));
            if (stats.scale == default(float)) return;
            if (creature.GetLocalInt("WEAPONS") == 0) return;

            // Ships with the ship AI package already have this heartbeat behaviour handled.
            if (creature.GetLocalString("BEHAVIOUR") == "StarshipBehaviour") return;

            // Shoot twice per round.
            ShootValidTarget(creature);            
            _.DelayCommand(3.0f, ()=>{ ShootValidTarget(creature); });
        }

        public void OnModuleItemEquipped()
        {
            NWPlayer equipper = _.GetPCItemLastEquippedBy();
            if (equipper.GetLocalInt("IS_SHIP") > 0)
            {
                NWItem item = _.GetPCItemLastEquipped();

                // Using ActionUnequipItem doesn't seem to work.  So copy and destroy.
                _.CopyItem(item, equipper, 1);
                item.Destroy();
                equipper.SendMessage("You cannot equip items while flying a ship.");
            }
        }

        public void OnPhysicalAttacked(NWCreature creature, NWCreature attacker)
        {
            ShipStats stats = GetShipStatsByAppearance(_.GetAppearanceType(creature));

            if (stats.scale == default(float)) return;

            if (_.GetDistanceBetween(creature, attacker) < stats.range)
            {
                _.AssignCommand(creature, () => { _.SetFacingPoint(attacker.Position); });
            }
            else
            {
                _.AssignCommand(creature, () => { _.ActionMoveToObject(attacker); });
            }
        }

        public void OnPerception(NWCreature creature, NWCreature perceived)
        {
            ShipStats stats = GetShipStatsByAppearance(_.GetAppearanceType(creature));

            if (stats.scale == default(float)) return;

            if (_.GetIsEnemy(perceived, creature) == 0) return;

            // Would be ideal to respect turning circles and only turn the ship a bit at a time. Less urgent now since 
            // the heartbeat code handles turning. 
            if (_.GetDistanceBetween(creature, perceived) < stats.range)
            {
                _.AssignCommand(creature, () => { _.ClearAllActions(); _.SetFacingPoint(perceived.Position); });

                // Fire guns. One definite shot...
                DoSpaceAttack(creature, perceived, creature.GetLocalInt("HAS_GUNNER") == 1);

                // ... and a second shot later in the round if there is still a valid target.  
                _.DelayCommand(3.0f, () => { ShootValidTarget(creature); });
            }
            else
            {
                _.AssignCommand(creature, () => { _.ActionMoveToObject(perceived, 1, stats.range - 2); });
            }
        }

        private bool AdjustFacingAndAttack(NWCreature creature)
        {
            // If we have an enemy in front of us, process them. 
            Location targetLocation = _.Location(
                creature.Area.Object,
                _biowarePos.GetChangedPosition(creature.Position, 25.0f, creature.Facing),
                creature.Facing + 180.0f);

            NWCreature enemy = _.GetFirstObjectInShape(SHAPE_SPELLCONE, 25.0f, targetLocation, 1, OBJECT_TYPE_CREATURE);

            while (enemy.IsValid)
            {
                if (_.GetIsEnemy(enemy, creature) == 1)
                {
                    OnPerception(creature, enemy);
                    return true;
                }

                enemy = _.GetNextObjectInShape(SHAPE_SPELLCONE, 25.0f, targetLocation, 1, OBJECT_TYPE_CREATURE);
            }

            enemy = _.GetNearestCreature(CREATURE_TYPE_REPUTATION, REPUTATION_TYPE_ENEMY, creature);

            if (enemy.IsValid && _.GetDistanceBetween(enemy, creature) < 25.0f)
            {
                // We have an enemy but they are not in our front arc.  
                float facing = _biowarePos.GetRelativeFacing(creature, enemy);

                // Creature facing is, for some reason, based with 0= due east and proceeding counter clockwise.
                // Convert to clockwise with due north as 0 so that we can compare it. 
                float myFacing = creature.Facing;

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
                targetLocation = _.Location(
                  creature.Area.Object,
                  _biowarePos.GetChangedPosition(creature.Position, 5.0f, myFacing),
                  myFacing + 180.0f);

                _.AssignCommand(creature, () => { _.ActionMoveToLocation(targetLocation, 1); });
            }

            return false;
        }

        // Note - this method is called by the ship behaviour method.  It's different from the generic
        // attack method above which is called for all creatures.  Thus allowing for future ships to 
        // use different AI. 
        public void OnHeartbeat(NWCreature creature)
        {
            ShipStats stats = GetShipStatsByAppearance(_.GetAppearanceType(creature));
            if (stats.scale == default(float)) return;

            // Turn to face the nearest enemy if they are within range.
            if (AdjustFacingAndAttack(creature))
            {
                // Target in range and arc. Check again half way through the round. 
                _.DelayCommand(3.0f, () => { AdjustFacingAndAttack(creature); });
            }
            else
            {
                // Target not in range, turn!
                _.DelayCommand(2.0f, () => { AdjustFacingAndAttack(creature); });
                _.DelayCommand(4.0f, () => { AdjustFacingAndAttack(creature); });
            }
        }
    }
}
