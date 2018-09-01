using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using NWN;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Structure;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Service
{
    public class StructureService : IStructureService
    {

        // Variable names
        private const string TerritoryFlagIDVariableName = "TERRITORY_FLAG_ID";
        private const string StructureIDVariableName = "TERRITORY_STRUCTURE_SITE_ID";
        private const string ConstructionSiteIDVariableName = "CONSTRUCTION_SITE_ID";

        private const string IsMovingStructureLocationVariableName = "IS_MOVING_STRUCTURE_LOCATION";
        private const string MovingStructureVariableName = "MOVING_STRUCTURE_OBJECT";

        // Resrefs
        private const string ConstructionSiteResref = "const_site";
        private const string TemporaryLocationCheckerObjectResref = "temp_loc_check";


        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly INWNXChat _nwnxChat;
        private readonly IColorTokenService _color;
        private readonly IItemService _item;
        private readonly IPlayerService _player;
        private readonly INWNXEvents _nwnxEvents;
        private readonly IDialogService _dialog;

        public StructureService(
            INWScript script,
            IDataContext db,
            INWNXChat nwnxChat,
            IColorTokenService color,
            IItemService item,
            IPlayerService player,
            INWNXEvents nwnxEvents,
            IDialogService dialog)
        {
            _ = script;
            _db = db;
            _nwnxChat = nwnxChat;
            _color = color;
            _item = item;
            _player = player;
            _nwnxEvents = nwnxEvents;
            _dialog = dialog;
        }

        public void OnModuleLoad()
        {
            List<ConstructionSite> constructionSites = (from c
                                                        in _db.ConstructionSites
                                                        where ((c.PCTerritoryFlagID == null
                                                            || (c.PCTerritoryFlag.BuildingPCStructureID == null && c.PCTerritoryFlag.IsActive))
                                                            && c.IsActive)
                                                        select c).ToList();
            
            foreach (ConstructionSite entity in constructionSites)
            {
                if (entity.IsActive)
                {
                    CreateConstructionSiteFromEntity(entity);
                }
            }
            
            var territoryFlags = _db.PCTerritoryFlags.Where(x => x.BuildingPCStructureID == null && x.IsActive);
            foreach (PCTerritoryFlag flag in territoryFlags)
            {
                NWArea oArea = NWArea.Wrap(_.GetObjectByTag(flag.LocationAreaTag, 0));
                Vector position = _.Vector((float)flag.LocationX, (float)flag.LocationY, (float)flag.LocationZ);
                Location location = _.Location(oArea.Object, position, (float)flag.LocationOrientation);
                PlayerCharacter playerEntity = _db.PlayerCharacters.Single(x => x.PlayerID == flag.PlayerID);

                NWPlaceable territoryFlag = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, flag.StructureBlueprint.Resref, location));
                territoryFlag.SetLocalInt(TerritoryFlagIDVariableName, flag.PCTerritoryFlagID);

                if (flag.ShowOwnerName)
                {
                    territoryFlag.Name = playerEntity.CharacterName + "'s Territory";
                }
                else
                {
                    territoryFlag.Name = "Claimed Territory";
                }

                foreach (PCTerritoryFlagsStructure structure in flag.PCTerritoryFlagsStructures)
                {
                    if (structure.IsActive)
                    {
                        CreateStructureFromEntity(structure);
                    }
                }

            }
        }

        // Assumption: There will never be an overlap of two or more territory flags' areas of influence.
        // If no territory flags own the location, NWObject.INVALID is returned.
        // If area is a building interior, the area itself will be treated as a flag.
        public NWObject GetTerritoryFlagOwnerOfLocation(Location location)
        {
            NWArea locationArea = NWArea.Wrap(_.GetAreaFromLocation(location));
            if (GetTerritoryFlagID(locationArea) > 0) return locationArea;

            List<PCTerritoryFlag> areaFlags = _db.PCTerritoryFlags
                .Where(x =>
                    x.LocationAreaTag == locationArea.Tag &&
                    x.IsActive)
                .OrderByDescending(o => o.StructureBlueprint.MaxBuildDistance)
                .ToList();
            NWPlaceable placeable = NWPlaceable.Wrap(new Object());

            foreach (PCTerritoryFlag flag in areaFlags)
            {
                Location flagLocation = _.Location(
                    locationArea.Object,
                    _.Vector((float)flag.LocationX, (float)flag.LocationY, (float)flag.LocationZ),
                    (float)flag.LocationOrientation
                );
                float distance = _.GetDistanceBetweenLocations(flagLocation, location);

                if (distance <= flag.StructureBlueprint.MaxBuildDistance)
                {
                    // Found the territory which "owns" this location. Look for the flag placeable with a matching ID.
                    int currentPlaceable = 1;
                    NWPlaceable checker = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, TemporaryLocationCheckerObjectResref, location));

                    do
                    {
                        placeable = NWPlaceable.Wrap(_.GetNearestObject(NWScript.OBJECT_TYPE_PLACEABLE, checker.Object, currentPlaceable));

                        if (GetTerritoryFlagID(placeable) == flag.PCTerritoryFlagID)
                        {
                            break;
                        }

                        currentPlaceable++;
                    } while (placeable.IsValid);

                    checker.Destroy();
                }

            }
            return placeable;
        }

        public int GetTerritoryFlagID(NWObject flag)
        {
            return flag.GetLocalInt(TerritoryFlagIDVariableName);
        }

        private float GetAdjustedFacing(float facing)
        {
            while (facing > 360.0f)
            {
                facing = facing - 360.0f;
            }

            return facing;
        }


        private float GetAdjustedFacing(Location location)
        {
            return GetAdjustedFacing(_.GetFacingFromLocation(location));
        }


        public int GetPlaceableStructureID(NWPlaceable structure)
        {
            return structure.GetLocalInt(StructureIDVariableName) <= 0 ?
                -1 :
                structure.GetLocalInt(StructureIDVariableName);
        }

        public PCTerritoryFlag GetPCTerritoryFlagByBuildingStructureID(int buildingStructureID)
        {
            return _db.PCTerritoryFlags.Single(x => x.BuildingPCStructureID == buildingStructureID);
        }

        public bool PlayerHasPermission(NWPlayer player, StructurePermission permissionType, int flagID)
        {
            if (flagID <= 0) return true;
            if (player.IsDM) return true;

            PCTerritoryFlag territoryFlag = GetPCTerritoryFlagByID(flagID);
            PCTerritoryFlag parentFlag = null;

            if (territoryFlag.BuildingPCStructureID != null)
            {
                PCTerritoryFlagsStructure buildingStructure = GetPCStructureByID((int)territoryFlag.BuildingPCStructureID);
                parentFlag = buildingStructure.PCTerritoryFlag;
            }

            int buildPrivacyID = territoryFlag.BuildPrivacyDomain.BuildPrivacyTypeID;
            bool uuidMatches = player.GlobalID == territoryFlag.PlayerID ||
                                  (parentFlag != null && player.GlobalID == parentFlag.PlayerID);

            if (buildPrivacyID == 1) // Owner Only
            {
                return uuidMatches;
            }
            else if (buildPrivacyID == 2) // Friends only
            {
                PCTerritoryFlagsPermission permission = _db.PCTerritoryFlagsPermissions
                    .SingleOrDefault(x => x.PlayerID == player.GlobalID && 
                                     x.TerritoryFlagPermissionID == (int) permissionType &&
                                     x.PCTerritoryFlagID == flagID); 
                return permission != null || uuidMatches;
            }
            else //noinspection RedundantIfStatement
            if (buildPrivacyID == 3) // Public
            {
                return true;
            }
            else // Shouldn't reach here.
            {
                return false;
            }
        }

        public BuildingOwners GetBuildingOwners(int territoryFlagID, int structureID)
        {
            return _db.StoredProcedureSingle<BuildingOwners>("GetBuildingOwners",
                new SqlParameter("TerritoryFlagID", territoryFlagID),
                new SqlParameter("BuildingStructureID", structureID));
        }

        public PCTerritoryFlagsStructure GetPCStructureByID(int structureID)
        {
            return _db.PCTerritoryFlagsStructures.Single(x => x.PCTerritoryFlagStructureID == structureID);
        }

        public void CreateConstructionSiteFromEntity(ConstructionSite entity)
        {
            NWArea oArea = NWArea.Wrap(_.GetObjectByTag(entity.LocationAreaTag));
            Vector position = _.Vector((float)entity.LocationX, (float)entity.LocationY, (float)entity.LocationZ);
            Location location = _.Location(oArea.Object, position, (float)entity.LocationOrientation);
            NWPlaceable constructionSite = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, ConstructionSiteResref, location));
            constructionSite.SetLocalInt(ConstructionSiteIDVariableName, entity.ConstructionSiteID);
            constructionSite.Name = "Construction Site: " + entity.StructureBlueprint.Name;
            
            Effect eGhostWalk = _.EffectCutsceneGhost();
            _.ApplyEffectToObject(NWScript.DURATION_TYPE_PERMANENT, eGhostWalk, constructionSite.Object);

        }

        public void CreateStructureFromEntity(PCTerritoryFlagsStructure structure)
        {
            NWArea oArea = NWArea.Wrap(_.GetObjectByTag(structure.LocationAreaTag));
            Vector position = _.Vector((float)structure.LocationX, (float)structure.LocationY, (float)structure.LocationZ);
            Location location = _.Location(oArea.Object, position, (float)structure.LocationOrientation);

            NWPlaceable structurePlaceable = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, structure.StructureBlueprint.Resref, location));
            
            structurePlaceable.SetLocalInt(StructureIDVariableName, (int)structure.PCTerritoryFlagStructureID);
            structurePlaceable.IsPlot = true;
            structurePlaceable.IsUseable = structure.IsUseable;

            if (!string.IsNullOrWhiteSpace(structure.CustomName))
            {
                structurePlaceable.Name = structure.CustomName;
            }
            else if (!string.IsNullOrWhiteSpace(structure.StructureBlueprint.ResourceResref))
            {
                structurePlaceable.Name = structure.StructureBlueprint.Name;
            }

            if (structure.StructureBlueprint.ItemStorageCount > 0)
            {

                structurePlaceable.Name += " (" + structure.StructureBlueprint.ItemStorageCount + " items)";
            }

            if (structure.StructureBlueprint.IsBuilding)
            {
                NWPlaceable door = CreateBuildingDoor(location, (int)structure.PCTerritoryFlagStructureID);
                structurePlaceable.SetLocalObject("BUILDING_ENTRANCE_DOOR", door.Object);
            }
        }

        public void JumpPCToBuildingInterior(NWPlayer player, NWArea area)
        {
            NWObject waypoint = null;
            NWObject exit = null;

            NWObject @object = NWObject.Wrap(_.GetFirstObjectInArea(area.Object));
            while(@object.IsValid)
            {
                if (@object.Tag == "PLAYER_HOME_ENTRANCE")
                {
                    waypoint = @object;
                }
                else if (@object.Tag == "building_exit")
                {
                    exit = @object;
                }

                @object = NWObject.Wrap(_.GetNextObjectInArea(area.Object));
            }

            if (waypoint == null)
            {
                player.FloatingText("ERROR: Couldn't find the building interior's entrance. Inform an admin of this issue.");
                return;
            }

            if (exit == null)
            {
                player.FloatingText("ERROR: Couldn't find the building interior's exit. Inform an admin of this issue.");
                return;
            }

            _player.SaveLocation(player);

            exit.SetLocalLocation("PLAYER_HOME_EXIT_LOCATION", player.Location);
            exit.SetLocalInt("IS_BUILDING_DOOR", 1);

            Location location = waypoint.Location;
            player.AssignCommand(() =>
            {
                _.ActionJumpToLocation(location);
            });
        }

        public int CanPCBuildInLocation(NWPlayer player, Location targetLocation, StructurePermission permission)
        {
            NWArea locationArea = NWArea.Wrap(_.GetAreaFromLocation(targetLocation));
            if (locationArea.GetLocalInt("BUILDING_DISABLED") == 1) return 0;
            
            NWObject flag = GetTerritoryFlagOwnerOfLocation(targetLocation);
            Location flagLocation = flag.Location;
            int pcTerritoryFlagID = GetTerritoryFlagID(flag);

            PCTerritoryFlag entity = _db.PCTerritoryFlags.SingleOrDefault(x => x.PCTerritoryFlagID == pcTerritoryFlagID);
            float distance = _.GetDistanceBetweenLocations(flagLocation, targetLocation);

            // No territory flag found, or the distance is too far from the nearest territory flag.
            // Only for non-building areas.
            if ((!flag.IsValid ||
                    entity == null ||
                    distance > entity.StructureBlueprint.MaxBuildDistance))
            {
                return 1;
            }

            // Max number of structures reached for this territory.
            TerritoryStructureCount counts = GetNumberOfStructuresInTerritory(pcTerritoryFlagID);
            
            if (counts.VanityCount >= entity.StructureBlueprint.VanityCount &&
                    counts.SpecialCount >= entity.StructureBlueprint.SpecialCount &&
                    counts.ResourceCount >= entity.StructureBlueprint.ResourceCount &&
                    counts.BuildingCount >= entity.StructureBlueprint.BuildingCount)
            {
                return 2;
            }

            // This territory is for a building. Get the parent territory marker for later use.
            PCTerritoryFlag parentFlag = null;
            if (entity.BuildingPCStructureID != null)
            {
                PCTerritoryFlagsStructure structure = _db.PCTerritoryFlagsStructures.Single(x => x.PCTerritoryFlagStructureID == entity.BuildingPCStructureID);
                parentFlag = structure.PCTerritoryFlag;
            }
            
            // Player is territory or building owner
            if (entity.PlayerID == player.GlobalID ||
                    (parentFlag != null && parentFlag.PlayerID == player.GlobalID ))
            {
                return 1;
            }

            if (PlayerHasPermission(player, permission, pcTerritoryFlagID))
            {
                return 1;
            }

            return 0;
        }


        private bool IsWithinRangeOfTerritoryFlag(NWPlaceable oCheck)
        {
            Location location = oCheck.Location;
            NWObject flag = GetTerritoryFlagOwnerOfLocation(location);

            if (flag.Object == _.GetAreaFromLocation(location)) return true;
            if (!flag.IsValid) return false;

            float distance = _.GetDistanceBetween(oCheck.Object, flag.Object);
            int flagID = GetTerritoryFlagID(flag);
            PCTerritoryFlag entity = _db.PCTerritoryFlags.Single(x => x.PCTerritoryFlagID == flagID); 

            return distance <= entity.StructureBlueprint.MaxBuildDistance;
        }

        public void CreateConstructionSite(NWPlayer player, Location location)
        {
            int buildStatus = CanPCBuildInLocation(player, location, StructurePermission.CanBuildStructures);

            if (buildStatus == 0) // 0 = Can't do it in that location
            {
                player.FloatingText(_color.Red("You can't build a construction site there."));
            }
            else if (buildStatus == 1) // 1 = Success
            {
                NWPlaceable constructionSite = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, ConstructionSiteResref, location));
                Effect eGhostWalk = _.EffectCutsceneGhost();
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_PERMANENT, eGhostWalk, constructionSite.Object);

                player.FloatingText("Construction site created! Use the construction site to select a blueprint.");
            }
            else if (buildStatus == 2) // 2 = Territory can't hold any more structures.
            {
                player.FloatingText(_color.Red("The maximum number of structures this territory can manage has been reached. Raze a structure or upgrade your territory to create a new structure."));
            }
        }

        public bool IsPCMovingStructure(NWPlayer oPC)
        {
            return oPC.GetLocalInt(IsMovingStructureLocationVariableName) == 1 &&
                   NWObject.Wrap(oPC.GetLocalObject(MovingStructureVariableName)).IsValid;
        }

        private void SetPlaceableStructureID(NWPlaceable structure, int structureID)
        {
            structure.SetLocalInt(StructureIDVariableName, structureID);
        }

        public void SetIsPCMovingStructure(NWPlayer player, NWPlaceable structure, bool isMoving)
        {
            if (isMoving)
            {
                player.SetLocalInt(IsMovingStructureLocationVariableName, 1);
                player.SetLocalObject(MovingStructureVariableName, structure.Object);
            }
            else
            {
                player.DeleteLocalInt(IsMovingStructureLocationVariableName);
                player.DeleteLocalObject(MovingStructureVariableName);
            }
        }

        public NWPlaceable CreateBuildingDoor(Location houseLocation, int structureID)
        {
            float facing = _.GetFacingFromLocation(houseLocation);
            float x = _.GetPositionFromLocation(houseLocation).m_X;
            float y = _.GetPositionFromLocation(houseLocation).m_Y;
            float z = _.GetPositionFromLocation(houseLocation).m_Z;

            if (facing == 0.0f) // east
            {
                x += 2.0f;
                y += 2.9f;
            }
            else if (facing == 90.0f) // north
            {
                x -= 2.9f;
                y += 2.0f;
            }
            else if (facing == 180.0f) // west
            {
                x -= 2.0f;
                y -= 2.9f;
            }
            else if (facing == 270.0f) // south
            {
                x += 2.9f;
                y -= 2.0f;
            }

            Vector position = _.Vector(x, y, z);

            Location doorLocation = _.Location(_.GetAreaFromLocation(houseLocation), position, _.GetFacingFromLocation(houseLocation));
            NWPlaceable door = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "building_door", doorLocation));
            door.SetLocalInt(StructureIDVariableName, structureID);
            door.SetLocalInt("IS_BUILDING_DOOR", 1);

            return door;
        }

        public bool IsConstructionSiteValid(NWPlaceable site)
        {
            Location siteLocation = site.Location;
            NWObject flag = GetTerritoryFlagOwnerOfLocation(siteLocation);
            Location flaglocation = flag.Location;
            int flagID = GetTerritoryFlagID(flag);
            int constructionSiteID = GetConstructionSiteID(site);
            if (flagID <= 0) return true;

            ConstructionSite constructionSiteEntity = _db.ConstructionSites.SingleOrDefault(x => x.ConstructionSiteID == constructionSiteID);
            PCTerritoryFlag flagEntity = _db.PCTerritoryFlags.Single(x => x.PCTerritoryFlagID == flagID);
            float distance = _.GetDistanceBetweenLocations(flaglocation, siteLocation);

            // Scenario #1: Territory's structure cap has been reached. Blueprint not set on this construction site.
            //              Site must be razed otherwise player would go over the cap.
            if (constructionSiteID <= 0)
            {
                TerritoryStructureCount counts = GetNumberOfStructuresInTerritory(flagID);
                if (counts.VanityCount >= flagEntity.StructureBlueprint.VanityCount &&
                        counts.SpecialCount >= flagEntity.StructureBlueprint.SpecialCount &&
                        counts.ResourceCount >= flagEntity.StructureBlueprint.ResourceCount &&
                        counts.BuildingCount >= flagEntity.StructureBlueprint.BuildingCount)
                {
                    return false;
                }
            }

            // Scenario #2: Construction site is a territory flag blueprint.
            // Construction site is within the flag's area of influence OR
            // the blueprint selected would bring the flag inside of its area of influence.
            return !(constructionSiteEntity != null &&
                    constructionSiteEntity.StructureBlueprint.IsTerritoryFlag &&
                    (distance <= flagEntity.StructureBlueprint.MaxBuildDistance + constructionSiteEntity.StructureBlueprint.MaxBuildDistance));

        }

        public PCTerritoryFlag GetPCTerritoryFlagByID(int territoryFlagID)
        {
            return _db.PCTerritoryFlags.SingleOrDefault(x => x.PCTerritoryFlagID == territoryFlagID);
        }

        public int GetConstructionSiteID(NWPlaceable site)
        {
            return site.GetLocalInt(ConstructionSiteIDVariableName);
        }

        private void SetConstructionSiteID(NWPlaceable site, int constructionSiteID)
        {
            site.SetLocalInt(ConstructionSiteIDVariableName, constructionSiteID);
        }

        public ConstructionSite GetConstructionSiteByID(int constructionSiteID)
        {
            return _db.ConstructionSites.Single(x => x.ConstructionSiteID == constructionSiteID);
        }

        public string BuildMenuHeader(int blueprintID)
        {
            StructureBlueprint entity = GetStructureBlueprintByID(blueprintID);
            string header = _color.Green("Blueprint Name: ") + entity.Name + "\n";
            header += _color.Green("Level: ") + entity.Level + "\n\n";

            if (entity.MaxBuildDistance > 0.0f)
            {
                header += _color.Green("Build Distance: ") + entity.MaxBuildDistance + " meters \n";
            }
            if (entity.VanityCount > 0)
            {
                header += _color.Green("Max # of Vanity Structures: ") + entity.VanityCount + "\n";
            }
            if (entity.SpecialCount > 0)
            {
                header += _color.Green("Max # of Special Structures: ") + entity.SpecialCount + "\n";
            }
            if (entity.BuildingCount > 0)
            {
                header += _color.Green("Max # of Building Structures: ") + entity.BuildingCount + "\n";
            }
            if (entity.ResourceCount > 0)
            {
                header += _color.Green("Max # of Resource Structures: ") + entity.ResourceCount + "\n";
            }

            if (entity.ItemStorageCount > 0)
            {
                header += _color.Green("Item Storage: ") + entity.ItemStorageCount + " items \n";
            }

            if (!string.IsNullOrWhiteSpace(entity.Description))
            {
                header += _color.Green("Description: ") + entity.Description + "\n\n";
            }
            header += _color.Green("Resources Required: ") + "\n\n";


            foreach (StructureComponent comp in entity.StructureComponents)
            {
                header += comp.Quantity > 0 ? comp.Quantity + "x " + _item.GetNameByResref(comp.Resref) + "\n" : "";
            }

            return header;
        }

        public List<StructureCategory> GetStructureCategoriesByType(string playerID, bool isTerritoryFlagCategory, bool isVanity, bool isSpecial, bool isResource, bool isBuilding)
        {
            return _db.StoredProcedure<StructureCategory>("GetStructureCategoriesByType",
                new SqlParameter("PlayerID", playerID),
                new SqlParameter("IsTerritoryFlagCategory", isTerritoryFlagCategory),
                new SqlParameter("IsVanity", isVanity),
                new SqlParameter("IsSpecial", isSpecial),
                new SqlParameter("IsResource", isResource),
                new SqlParameter("IsBuilding", isBuilding));
        }

        public TerritoryStructureCount GetNumberOfStructuresInTerritory(int flagID)
        {
            return _db.StoredProcedureSingle<TerritoryStructureCount>("GetNumberOfStructuresInTerritory",
                new SqlParameter("FlagID", flagID));
        }

        public List<StructureBlueprint> GetStructuresByCategoryAndType(string playerID, int structureCategoryID, bool isVanity, bool isSpecial, bool isResource, bool isBuilding)
        {
            return _db.StoredProcedure<StructureBlueprint>("GetStructuresByCategoryAndType",
                new SqlParameter("PlayerID", playerID),
                new SqlParameter("StructureCategoryID", structureCategoryID),
                new SqlParameter("IsVanity", isVanity),
                new SqlParameter("IsSpecial", isSpecial),
                new SqlParameter("IsResource", isResource),
                new SqlParameter("IsBuilding", isBuilding));
        }

        public bool WillBlueprintOverlapWithExistingFlags(Location location, int blueprintID)
        {
            NWArea area = NWArea.Wrap(_.GetAreaFromLocation(location));
            List<PCTerritoryFlag> flags = _db.PCTerritoryFlags.Where(x => x.LocationAreaTag == area.Tag && x.IsActive)
                .OrderByDescending(o => o.StructureBlueprint.MaxBuildDistance).ToList();
            StructureBlueprint blueprint = _db.StructureBlueprints.Single(x => x.StructureBlueprintID == blueprintID);

            foreach (PCTerritoryFlag flag in flags)
            {
                Location flagLocation = _.Location(
                    area.Object,
                    _.Vector((float)flag.LocationX, (float)flag.LocationY, (float)flag.LocationZ),
                    (float)flag.LocationOrientation
                );

                float distance = _.GetDistanceBetweenLocations(location, flagLocation);
                float overlapDistance = (float)flag.StructureBlueprint.MaxBuildDistance + (float)blueprint.MaxBuildDistance;

                if (distance <= overlapDistance)
                    return true;

            }

            return false;
        }

        public StructureBlueprint GetStructureBlueprintByID(int blueprintID)
        {
            return _db.StructureBlueprints.Single(x => x.StructureBlueprintID == blueprintID);
        }

        public void RazeConstructionSite(NWPlayer player, NWPlaceable site, bool recoverMaterials)
        {
            int constructionSiteID = GetConstructionSiteID(site);
            if (constructionSiteID > 0)
            {
                ConstructionSite entity = _db.ConstructionSites.Single(x => x.ConstructionSiteID == constructionSiteID);

                if (recoverMaterials)
                {
                    foreach (ConstructionSiteComponent comp in entity.ConstructionSiteComponents)
                    {
                        int quantity = comp.StructureComponent.Quantity - comp.Quantity;
                        for (int q = 1; q <= quantity; q++) _.CreateItemOnObject(comp.StructureComponent.Resref, player.Object);
                    }
                }
                
                entity.IsActive = false;
                _db.SaveChanges();
            }
            site.Destroy();
        }

        public NWPlaceable CompleteStructure(NWPlaceable constructionSite)
        {
            int constructionSiteID = GetConstructionSiteID(constructionSite);
            ConstructionSite entity = _db.ConstructionSites.Single(x => x.ConstructionSiteID == constructionSiteID);
            StructureBlueprint blueprint = entity.StructureBlueprint;
            Location location = constructionSite.Location;

            NWPlaceable structurePlaceable = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, blueprint.Resref, location));
            constructionSite.Destroy();

            if (blueprint.IsTerritoryFlag)
            {
                PlayerCharacter playerEntity = _db.PlayerCharacters.Single(x => x.PlayerID == entity.PlayerID);
                structurePlaceable.Name = playerEntity.CharacterName + "'s Territory";

                PCTerritoryFlag pcFlag = new PCTerritoryFlag
                {
                    StructureBlueprintID = blueprint.StructureBlueprintID,
                    LocationAreaTag = entity.LocationAreaTag,
                    LocationOrientation = entity.LocationOrientation,
                    LocationX = entity.LocationX,
                    LocationY = entity.LocationY,
                    LocationZ = entity.LocationZ,
                    PlayerID = entity.PlayerID,
                    ShowOwnerName = true,
                    BuildingPCStructureID = null,
                    IsActive = true,
                    BuildPrivacySettingID = 1 // 1 = Owner Only
                };
                
                _db.PCTerritoryFlags.Add(pcFlag);
                _db.SaveChanges();
                
                structurePlaceable.SetLocalInt(TerritoryFlagIDVariableName, pcFlag.PCTerritoryFlagID);
            }
            else
            {
                PCTerritoryFlagsStructure pcStructure = new PCTerritoryFlagsStructure
                {
                    StructureBlueprintID = blueprint.StructureBlueprintID,
                    LocationAreaTag = entity.LocationAreaTag,
                    LocationOrientation = entity.LocationOrientation,
                    LocationX = entity.LocationX,
                    LocationY = entity.LocationY,
                    LocationZ = entity.LocationZ,
                    PCTerritoryFlagID = Convert.ToInt32(entity.PCTerritoryFlagID),
                    IsUseable = entity.StructureBlueprint.IsUseable,
                    CustomName = string.Empty,
                    BuildingInteriorID = entity.BuildingInteriorID,
                    IsActive = true
                };
                
                _db.PCTerritoryFlagsStructures.Add(pcStructure);
                _db.SaveChanges();

                structurePlaceable.SetLocalInt(StructureIDVariableName, Convert.ToInt32(pcStructure.PCTerritoryFlagStructureID));

                if (entity.StructureBlueprint.ItemStorageCount > 0)
                {
                    if (string.IsNullOrWhiteSpace(entity.StructureBlueprint.ResourceResref))
                    {
                        structurePlaceable.Name += " (" + entity.StructureBlueprint.ItemStorageCount + " items)";
                    }
                    else
                    {
                        structurePlaceable.Name = entity.StructureBlueprint.Name + " (" + entity.StructureBlueprint.ItemStorageCount + " items)";
                    }
                }

                if (entity.StructureBlueprint.IsBuilding)
                {
                    // Buildings get an entry in the territory flags table. There's no physical territory marker in-game, it's all done in the DB.
                    PCTerritoryFlag pcFlag = new PCTerritoryFlag
                    {
                        StructureBlueprintID = blueprint.StructureBlueprintID,
                        LocationAreaTag = string.Empty,
                        LocationOrientation = 0.0f,
                        LocationX = 0.0f,
                        LocationY = 0.0f,
                        LocationZ = 0.0f,
                        PlayerID = entity.PlayerID,
                        ShowOwnerName = false,
                        BuildingPCStructureID = pcStructure.PCTerritoryFlagStructureID,
                        IsActive = true
                    };

                    _db.PCTerritoryFlags.Add(pcFlag);
                    _db.SaveChanges();
                    
                    CreateBuildingDoor(location, Convert.ToInt32(pcStructure.PCTerritoryFlagStructureID));
                }
            }

            entity.IsActive = false;
            _db.SaveChanges();

            return structurePlaceable;
        }

        public void LogQuickBuildAction(NWPlayer dm, NWPlaceable completedStructure)
        {
            if (!dm.IsPlayer && !dm.IsDM) return;

            string name = dm.Name;
            int flagID = completedStructure.GetLocalInt(TerritoryFlagIDVariableName);
            long structureID = completedStructure.GetLocalInt(StructureIDVariableName);

            StructureQuickBuildAudit audit = new StructureQuickBuildAudit
            {
                DateBuilt = DateTime.UtcNow,
                DMName = name
            };

            if (flagID > 0)
                audit.PCTerritoryFlagID = flagID;
            else
                audit.PCTerritoryFlagID = null;

            if (structureID > 0)
                audit.PCTerritoryFlagStructureID = structureID;
            else
                audit.PCTerritoryFlagStructureID = null;

            _db.StructureQuickBuildAudits.Add(audit);
            _db.SaveChanges();
        }

        public void SelectBlueprint(NWPlayer player, NWPlaceable constructionSite, int blueprintID)
        {
            ConstructionSite entity = new ConstructionSite();
            NWObject area = constructionSite.Area;
            string areaTag = area.Tag;
            Location location = constructionSite.Location;
            StructureBlueprint blueprint = _db.StructureBlueprints.Single(x => x.StructureBlueprintID == blueprintID);

            entity.LocationAreaTag = areaTag;
            entity.LocationOrientation = GetAdjustedFacing(location);
            entity.LocationX = _.GetPositionFromLocation(location).m_X;
            entity.LocationY = _.GetPositionFromLocation(location).m_Y;
            entity.LocationZ = _.GetPositionFromLocation(location).m_Z;
            entity.PlayerID = player.GlobalID;
            entity.StructureBlueprintID = blueprint.StructureBlueprintID;

            entity.IsActive = true;

            foreach (StructureComponent comp in blueprint.StructureComponents)
            {
                ConstructionSiteComponent csComp = new ConstructionSiteComponent
                {
                    ConstructionSite = entity,
                    Quantity = comp.Quantity,
                    StructureComponentID = comp.StructureComponentID
                };
                _db.ConstructionSiteComponents.Add(csComp);
            }

            if (IsWithinRangeOfTerritoryFlag(constructionSite))
            {
                NWObject flag = GetTerritoryFlagOwnerOfLocation(location);
                int flagID = GetTerritoryFlagID(flag);
                PCTerritoryFlag flagEntity = _db.PCTerritoryFlags.Single(x => x.PCTerritoryFlagID == flagID);
                entity.PCTerritoryFlagID = flagEntity.PCTerritoryFlagID;
            }

            // Buildings - get the default interior and assign it to the construction site.
            if (blueprint.IsBuilding)
            {
                if (blueprint.BuildingCategory == null)
                {
                    player.FloatingText("ERROR: Unable to locate building category for this blueprint. Please inform an admin.");
                    return;
                }

                BuildingInterior defaultInterior = _db.BuildingInteriors.Single(x => x.IsDefaultForCategory && x.BuildingCategoryID == blueprint.BuildingCategoryID);
                entity.BuildingInteriorID = defaultInterior.BuildingInteriorID; 

                // Change the construction site facing to 0.0f
                // Houses can only be set to cardinal directions.
                constructionSite.Facing = 0.0f;
            }

            _db.SaveChanges();
            SetConstructionSiteID(constructionSite, entity.ConstructionSiteID);
            constructionSite.Name = "Construction Site: " + entity.StructureBlueprint.Name;

            // If blueprint doesn't have any components, instantly create the structure
            if (blueprint.StructureComponents.Count <= 0)
            {
                CompleteStructure(constructionSite);
            }
            else
            {
                player.FloatingText("Blueprint set. Equip a hammer and 'bash' the construction site to build.");
            }
        }


        public void MoveStructure(NWPlayer oPC, Location location)
        {
            NWPlaceable target = NWPlaceable.Wrap(oPC.GetLocalObject(MovingStructureVariableName));
            NWObject nearestFlag = GetTerritoryFlagOwnerOfLocation(location);
            NWArea locationArea = NWArea.Wrap(_.GetAreaFromLocation(location));
            Location nearestFlagLocation = nearestFlag.Location;
            int nearestFlagID = GetTerritoryFlagID(nearestFlag);
            bool outsideOwnFlagRadius = false;
            int constructionSiteID = GetConstructionSiteID(target);
            int structureID = GetPlaceableStructureID(target);
            oPC.DeleteLocalInt(IsMovingStructureLocationVariableName);
            oPC.DeleteLocalObject(MovingStructureVariableName);

            if (!target.IsValid ||
                locationArea.Object != target.Area.Object)
            {
                return;
            }

            if (!PlayerHasPermission(oPC, StructurePermission.CanMoveStructures, nearestFlagID))
            {
                oPC.FloatingText("You do not have permission to move this structure.");
                return;
            }

            // Moving construction site, no blueprint set
            if (constructionSiteID <= 0 && target.Resref == ConstructionSiteResref)
            {
                if (CanPCBuildInLocation(oPC, location, StructurePermission.CanMoveStructures) == 0)
                {
                    outsideOwnFlagRadius = true;
                }
            }

            // Moving construction site, blueprint is set.
            if (constructionSiteID > 0)
            {
                ConstructionSite entity = _db.ConstructionSites.Single(x => x.ConstructionSiteID == constructionSiteID);
                bool isTerritoryMarkerConstructionSite = entity.PCTerritoryFlag == null;

                // Territory marker - Ensure not in radius of another territory
                if (isTerritoryMarkerConstructionSite)
                {
                    PCTerritoryFlag nearestFlagEntity = _db.PCTerritoryFlags.SingleOrDefault(x => x.PCTerritoryFlagID == nearestFlagID);
                    if (nearestFlagEntity != null && _.GetDistanceBetweenLocations(location, nearestFlagLocation) <= nearestFlagEntity.StructureBlueprint.MaxBuildDistance)
                    {
                        oPC.FloatingText("Cannot move territory markers within the building range of another territory marker.");
                        return;
                    }
                }
                else if (entity.PCTerritoryFlagID != nearestFlagID ||
                        _.GetDistanceBetweenLocations(nearestFlagLocation, location) > entity.PCTerritoryFlag.StructureBlueprint.MaxBuildDistance)
                {
                    outsideOwnFlagRadius = true;
                }
                else
                {
                    entity.LocationOrientation = GetAdjustedFacing(location);
                    entity.LocationX = _.GetPositionFromLocation(location).m_X;
                    entity.LocationY = _.GetPositionFromLocation(location).m_Y;
                    entity.LocationZ = _.GetPositionFromLocation(location).m_Z;

                    _db.SaveChanges();
                }
            }
            else if (structureID > 0)
            {
                PCTerritoryFlagsStructure entity = _db.PCTerritoryFlagsStructures.Single(x => x.PCTerritoryFlagStructureID == structureID);

                if (entity.PCTerritoryFlagID != nearestFlagID ||
                        _.GetDistanceBetweenLocations(nearestFlagLocation, location) > entity.PCTerritoryFlag.StructureBlueprint.MaxBuildDistance)
                {
                    outsideOwnFlagRadius = true;
                }
                else
                {
                    entity.LocationOrientation = GetAdjustedFacing(location);
                    entity.LocationX = _.GetPositionFromLocation(location).m_X;
                    entity.LocationY = _.GetPositionFromLocation(location).m_Y;
                    entity.LocationZ = _.GetPositionFromLocation(location).m_Z;

                    _db.SaveChanges();
                }
            }

            if (outsideOwnFlagRadius)
            {
                oPC.FloatingText("Unable to move structure to that location. New location must be within range of the territory marker it is attached to.");
                return;
            }


            NWPlaceable door = NWPlaceable.Wrap(_.GetLocalObject(target.Object, "BUILDING_ENTRANCE_DOOR"));
            bool hasDoor = door.IsValid;

            NWPlaceable copy = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, target.Resref, location));
            copy.Name = target.Name;

            if (hasDoor)
            {
                door.Destroy();
                door = CreateBuildingDoor(copy.Location, structureID);
                copy.SetLocalObject("BUILDING_ENTRANCE_DOOR", door.Object);
            }

            if (constructionSiteID > 0) SetConstructionSiteID(copy, constructionSiteID);
            else if (structureID > 0) SetPlaceableStructureID(copy, structureID);
            
            _.DestroyObject(target.GetLocalObject("GateBlock"));
            target.Destroy();
        }


        public List<BuildingInterior> GetBuildingInteriorsByCategoryID(int buildingCategoryID)
        {
            return _db.BuildingInteriors.Where(x => x.BuildingCategoryID == buildingCategoryID).ToList();
        }

        public void PreviewBuildingInterior(NWPlayer player, int buildingInteriorID)
        {
            BuildingInterior interior = _db.BuildingInteriors.Single(x => x.BuildingInteriorID == buildingInteriorID);

            NWArea area = NWArea.Wrap(_.CreateArea(interior.AreaResref, "", "PREVIEW - " + interior.Name));
            area.SetLocalInt("BUILDING_DISABLED", 1);
            area.SetLocalInt("IS_BUILDING_PREVIEW", 1);

            JumpPCToBuildingInterior(player, area);
        }

        public void SetStructureCustomName(NWPlayer player, NWPlaceable structure, string customName)
        {
            int structureID = GetPlaceableStructureID(structure);
            customName = customName.Trim();

            if (structureID <= 0) return;
            if (string.IsNullOrWhiteSpace(customName)) return;

            PCTerritoryFlagsStructure entity = GetPCStructureByID(structureID);

            if (!PlayerHasPermission(player, StructurePermission.CanRenameStructures, entity.PCTerritoryFlagID))
            {
                player.FloatingText("You don't have permission to rename structures. Contact the territory owner for permission.");
                return;
            }

            entity.CustomName = customName;
            _db.SaveChanges();

            structure.Name = customName;

            player.FloatingText("New name set: " + customName);
        }

        public List<PCTerritoryFlagsPermission> GetPermissionsByFlagID(int flagID)
        {
            return _db.PCTerritoryFlagsPermissions.Where(x => x.PCTerritoryFlagID == flagID).ToList();
        }

        public List<PCTerritoryFlagsPermission> GetPermissionsByPlayerID(string playerID, int flagID)
        {
            return _db.PCTerritoryFlagsPermissions.Where(x => x.PCTerritoryFlagID == flagID && x.PlayerID == playerID).ToList();
        }

        public List<TerritoryFlagPermission> GetAllTerritorySelectablePermissions()
        {
            return _db.TerritoryFlagPermissions.Where(x => x.IsActive && x.IsSelectable).ToList();
        }

        public void RazeTerritory(NWPlaceable flag)
        {
            int flagID = GetTerritoryFlagID(flag);
            PCTerritoryFlag entity = _db.PCTerritoryFlags.Single(x => x.PCTerritoryFlagID == flagID);
            List<long> constructionSiteIDs = new List<long>();
            List<long> structureSiteIDs = new List<long>();

            foreach (PCTerritoryFlagsStructure structure in entity.PCTerritoryFlagsStructures)
            {
                if (!structureSiteIDs.Contains(structure.PCTerritoryFlagStructureID))
                {
                    structureSiteIDs.Add(structure.PCTerritoryFlagStructureID);
                }
            }

            foreach (ConstructionSite constructionSite in entity.ConstructionSites)
            {
                if (!constructionSiteIDs.Contains(constructionSite.ConstructionSiteID))
                {
                    constructionSiteIDs.Add(constructionSite.ConstructionSiteID);
                }
            }


            int currentPlaceable = 1;
            NWPlaceable placeable = NWPlaceable.Wrap(_.GetNearestObject(NWScript.OBJECT_TYPE_PLACEABLE, flag.Object, currentPlaceable));
            while (placeable.IsValid)
            {
                if (_.GetDistanceBetween(placeable.Object, flag.Object) > entity.StructureBlueprint.MaxBuildDistance) break;

                if (constructionSiteIDs.Contains(GetConstructionSiteID(placeable)) ||
                    structureSiteIDs.Contains(GetPlaceableStructureID(placeable)))
                {

                    _.DestroyObject(placeable.GetLocalObject("GateBlock"));
                    placeable.Destroy();
                }

                currentPlaceable++;
                placeable = NWPlaceable.Wrap(_.GetNearestObject(NWScript.OBJECT_TYPE_PLACEABLE, flag.Object, currentPlaceable));
            }

            flag.Destroy();
            _db.StoredProcedure("SetTerritoryInactive",
                new SqlParameter("FlagID", flagID));
        }

        public void TransferBuildingOwnership(NWArea area, string newOwnerPlayerID)
        {
            int pcFlagID = GetTerritoryFlagID(area);

            PCTerritoryFlag entity = _db.PCTerritoryFlags.Single(x => x.PCTerritoryFlagID == pcFlagID);
            entity.PCTerritoryFlagsPermissions.Clear();
            entity.PlayerID = newOwnerPlayerID;
            entity.ShowOwnerName = true;
            _db.SaveChanges();
        }

        public void TransferTerritoryOwnership(NWPlaceable flag, string newOwnerPlayerID)
        {
            int pcFlagID = GetTerritoryFlagID(flag);
            PlayerCharacter playerEntity = _db.PlayerCharacters.Single(x => x.PlayerID == newOwnerPlayerID);

            // Update building territory marker owner
            _db.StoredProcedure("UpdateBuildingTerritoryFlagsOwner",
                new SqlParameter("NewOwnerPlayerID", newOwnerPlayerID),
                new SqlParameter("FlagID", pcFlagID));

            PCTerritoryFlag entity = _db.PCTerritoryFlags.Single(x => x.PCTerritoryFlagID == pcFlagID);
            entity.PCTerritoryFlagsPermissions.Clear();
            entity.PlayerID = newOwnerPlayerID;
            entity.ShowOwnerName = true;
            _db.SaveChanges();

            flag.Name = playerEntity.CharacterName + "'s Territory";

            NWPlayer player = NWPlayer.Wrap(_.GetFirstPC());
            while(player.IsValid)
            {
                if (player.GlobalID == newOwnerPlayerID)
                {
                    player.FloatingText("Ownership of a territory in " + flag.Area.Name + " has been transferred to you.");
                    break;
                }

                player = NWPlayer.Wrap(_.GetNextPC());
            }
        }

        public List<StructureCategory> GetStructureCategories(string playerID)
        {
            return _db.StoredProcedure<StructureCategory>("GetStructureCategories",
                new SqlParameter("PlayerID", playerID));
        }

        public List<StructureBlueprint> GetStructuresForPCByCategory(string playerID, int structureCategoryID)
        {
            return _db.StoredProcedure<StructureBlueprint>("GetStructuresForPCByCategory",
                new SqlParameter("PlayerID", playerID),
                new SqlParameter("StructureCategoryID", structureCategoryID));
        }

        public void ChangeBuildPrivacy(int flagID, int buildPrivacyID)
        {
            PCTerritoryFlag entity = GetPCTerritoryFlagByID(flagID);
            entity.BuildPrivacySettingID = buildPrivacyID;
            _db.SaveChanges();
        }

        public void TogglePermissionForPlayer(PCTerritoryFlagsPermission foundPerm, string playerID, int permissionID, int flagID)
        {
            if (foundPerm == null)
            {
                PCTerritoryFlagsPermission perm = new PCTerritoryFlagsPermission
                {
                    PlayerID = playerID,
                    TerritoryFlagPermissionID = permissionID,
                    PCTerritoryFlagID = flagID
                };

                _db.PCTerritoryFlagsPermissions.Add(perm);
            }
            else
            {
                _db.PCTerritoryFlagsPermissions.Remove(foundPerm);
            }
        }

        public void DeleteContainerItemByGlobalID(string globalID)
        {
            var record = _db.PCTerritoryFlagsStructuresItems.Single(x => x.GlobalID == globalID);
            _db.PCTerritoryFlagsStructuresItems.Remove(record);
            _db.SaveChanges();
        }

        public void SaveChanges()
        {
            _db.SaveChanges();
        }

        public void OnModuleNWNXChat(NWPlayer sender)
        {
            if (sender.GetLocalInt("LISTENING_FOR_NEW_CONTAINER_NAME") != 1) return;
            if (!sender.IsPlayer && !sender.IsDM) return;

            _nwnxChat.SkipMessage();
            string text = _nwnxChat.GetMessage().Trim();
            if (text.Length > 32)
            {
                sender.FloatingText("Container names must be 32 characters or less.");
                return;
            }

            sender.SetLocalString("NEW_CONTAINER_NAME", text);
            sender.SendMessage("New container name received. Please press the 'Next' button in the conversation window.");
        }

        public void OnModuleUseFeat()
        {
            NWPlayer pc = NWPlayer.Wrap(Object.OBJECT_SELF);
            Location targetLocation = _nwnxEvents.OnFeatUsed_GetTargetLocation();
            int featID = _nwnxEvents.OnFeatUsed_GetFeatID();

            if (featID != (int) CustomFeatType.StructureTool) return;


            bool isMovingStructure = IsPCMovingStructure(pc);

            if (isMovingStructure)
            {
                MoveStructure(pc, targetLocation);
            }
            else
            {
                pc.SetLocalLocation("BUILD_TOOL_LOCATION_TARGET", targetLocation);
                _dialog.StartConversation(pc, pc, "BuildToolMenu");
            }

        }
    }
}
