using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
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
        private const int _SCANNING = 1;
        private const int _SHIELDS = 2;
        private const int _STEALTH = 3;
        private const int _WEAPONS = 4;

        private readonly INWScript _;
        private readonly IBasePermissionService _perm;
        private readonly IDataService _data;
        private readonly IErrorService _error;
        private readonly IPlayerService _player;
        private readonly IPlayerStatService _playerStat;
        private readonly ISerializationService _serialization;
        private readonly ISkillService _skill;

        private const string SPACE = "SPACE"; // For tracing. 

        public SpaceService(INWScript script,
                            IBasePermissionService perm,
                            IDataService data,
                            IErrorService error,
                            IPlayerService player,
                            IPlayerStatService playerStat,
                            ISerializationService serial,
                            ISkillService skill)
        {
            _ = script;            
            _data = data;
            _error = error;
            _perm = perm;
            _player = player;
            _playerStat = playerStat;
            _serialization = serial;
            _skill = skill;
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

        public int GetShipBaseStat(int BaseStructureID, int Stat)
        {
            switch (BaseStructureID)
            {
                case 156: // Light Transport 1
                    switch (Stat)
                    {
                        case _SCANNING:
                            return 0;
                        case _SHIELDS:
                            return 2;
                        case _STEALTH:
                            return 0;
                        case _WEAPONS:
                            return 1;
                    }
                    break;

                case 157: // Light Escort 1
                    switch (Stat)
                    {
                        case _SCANNING:
                            return 1;
                        case _SHIELDS:
                            return 3;
                        case _STEALTH:
                            return 0;
                        case _WEAPONS:
                            return 3;

                    }
                    break;
            }
            return 0;
        }

        public NWPlaceable GetCargoBay(NWArea starship, NWPlayer player)
        {
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
                    player.FloatingText("Someone else is already accessing that structure's inventory. Please wait.");
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

        public int GetCargoBonus(PCBase pcBase, int Stat)
        {
            // Get the structure ID of the starship.
            PCBaseStructure structure = _data.SingleOrDefault<PCBaseStructure>(x => x.PCBaseID == pcBase.ID && x.ExteriorStyleID != null);

            // Find the area.
            foreach (var area in NWModule.Get().Areas)
            {
                if (area.GetLocalString("PC_BASE_STRUCTURE_ID") == structure.ID.ToString())
                {
                    return GetCargoBonus(area, Stat);
                }
            }

            _error.Trace(SPACE, "Unable to find starship to check cargo bonus: " + pcBase.ID.ToString());
            return 0;
        }

        public int GetCargoBonus (NWArea starship, int Stat)
        { 
            // Get the starship's cargo inventory and look for enhancement items. 
            NWPlaceable bay = GetCargoBay(starship, null);

            foreach (var item in bay.InventoryItems)
            {
                // Find any items with the right properties to improve starship abilities.
                // TODO
            }

            return 0;
        }

        public void OnModuleHeartbeat()
        {
            // Fire every minute (count to 10)
            // Find all occupied spaceship areas (areas whose base ID is part of a base in a space location)

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

        public bool CanDetect(PCBase scanningShip, PCBase otherShip)
        {
            // Returns true if the first ship is capable of detecting the second
            // (i.e. scanning on scanningShip >= stealth on otherShip).
            BuildingStyle scanner = _data.SingleOrDefault<BuildingStyle>(x => x.ID == scanningShip.BuildingStyleID);
            BuildingStyle stealther = _data.SingleOrDefault<BuildingStyle>(x => x.ID == otherShip.BuildingStyleID);

            if (GetShipBaseStat((int)scanner.BaseStructureID, _SCANNING) + GetCargoBonus(scanningShip, _SCANNING) <
                GetShipBaseStat((int)stealther.BaseStructureID, _STEALTH + GetCargoBonus(otherShip, _STEALTH)))
            {
                return false;
            }

            return true;
        }

        public bool DoPilotingSkillCheck(NWPlayer player, int DC)
        {
            // Get the player's piloting skill (including gear bonuses).
            if (!player.IsPlayer) return false;
            
            EffectiveItemStats effectiveStats = _playerStat.GetPlayerItemEffectiveStats(player);
            int rank = _skill.GetPCSkillRank(player, SkillType.Piloting);

            // Award XP. 
            float xp = _skill.CalculateRegisteredSkillLevelAdjustedXP(250, DC, rank);

            rank += effectiveStats.Piloting;

            if (_.d100() <= (100 * rank) / (rank + DC))
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

        
    }
}
