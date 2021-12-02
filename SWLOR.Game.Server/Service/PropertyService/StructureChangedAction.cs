using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service.PropertyService
{
    public static class StructureChangedAction
    {
        private static readonly Dictionary<StructureType, Action<WorldProperty, uint>> _actions = new();

        /// <summary>
        /// Builds the actions which are run when certain structures are changed in the game world.
        /// </summary>
        /// <returns>A dictionary of spawn actions.</returns>
        public static Dictionary<StructureType, Action<WorldProperty, uint>> BuildSpawnActions()
        {
            _actions[StructureType.CityHall] = CityHall();
            _actions[StructureType.Bank] = Bank();
            _actions[StructureType.MedicalCenter] = MedicalCenter();
            _actions[StructureType.Starport] = Starport();
            _actions[StructureType.Cantina] = Cantina();
            _actions[StructureType.House] = House1();

            return _actions;
        }

        private static Location GetDoorLocation(uint building, float orientationAdjustment, float sqrtAdjustment)
        {
            var area = GetArea(building);
            var location = GetLocation(building);

            var position = GetPositionFromLocation(location);
            var orientation = GetFacingFromLocation(location);

            orientation = orientation + orientationAdjustment;
            if (orientation > 360.0) 
                orientation -= 360.0f;

            var mod = sqrt(sqrtAdjustment) * sin(orientation);
            position.X += mod;

            mod = sqrt(sqrtAdjustment) * cos(orientation);
            position.Y -= mod;
            var doorLocation = Location(area, position, orientation);
            return doorLocation;
        }

        private static void SpawnDoor(uint building, Location location, string name)
        {
            var door = GetLocalObject(building, "PROPERTY_DOOR");
            if (GetIsObjectValid(door))
                DestroyObject(door);

            door = CreateObject(ObjectType.Placeable, "building_ent1", location);
            SetLocalObject(building, "PROPERTY_DOOR", door);

            Property.AssignPropertyId(door, Property.GetPropertyId(building));
            SetName(door, name);
        }

        private static void AdjustBuildingName(WorldProperty property)
        {
            // If the interior has been linked, also update its name.
            var interiorId = property.ChildPropertyIds.SingleOrDefault();
            if (!string.IsNullOrWhiteSpace(interiorId))
            {
                var interior = DB.Get<WorldProperty>(interiorId);
                interior.CustomName = property.CustomName;
                DB.Set(interior);

                var instance = Property.GetRegisteredInstance(interiorId);
                SetName(instance.Area, property.CustomName);
            }
        }

        private static Action<WorldProperty, uint> CityHall()
        {
            return (property, building) =>
            {
                var location = GetDoorLocation(building, 245f, 95f);
                SpawnDoor(building, location, property.CustomName);
                AdjustBuildingName(property);
            };
        }

        private static Action<WorldProperty, uint> Bank()
        {
            return (property, building) =>
            {
                var location = GetDoorLocation(building, 205f, 55f);
                SpawnDoor(building, location, property.CustomName);
                AdjustBuildingName(property);
            };
        }

        private static Action<WorldProperty, uint> MedicalCenter()
        {
            return (property, building) =>
            {
                var location = GetDoorLocation(building, 312f, 145f);
                SpawnDoor(building, location, property.CustomName);
                AdjustBuildingName(property);
            };
        }

        private static Action<WorldProperty, uint> Starport()
        {
            return (property, building) =>
            {
                var location = GetDoorLocation(building, 90f, 220f);
                SpawnDoor(building, location, property.CustomName);
                AdjustBuildingName(property);
            };
        }

        private static Action<WorldProperty, uint> Cantina()
        {
            return (property, building) =>
            {
                var location = GetDoorLocation(building, 90f, 50f);
                SpawnDoor(building, location, property.CustomName);
                AdjustBuildingName(property);
            };
        }

        private static Action<WorldProperty, uint> House1()
        {
            return (property, building) =>
            {
                var location = GetDoorLocation(building, 198f, 13.0f);
                SpawnDoor(building, location, property.CustomName);
                AdjustBuildingName(property);
            };
        }

    }
}
