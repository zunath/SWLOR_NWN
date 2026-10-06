using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
namespace SWLOR.Game.Server.Service
{
    public static partial class Space
    {
        private static bool HasStationPermission(string id,PlayerShip ship)
        {
            var property=DB.Get<WorldProperty>(ship.PropertyId);
            return property!=null&&(property.OwnerPlayerId==id||DB.Search(new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PlayerId),id,false).AddFieldSearch(nameof(WorldPropertyPermission.PropertyId),ship.PropertyId,false))
                .Any(x=>x.Permissions.GetValueOrDefault(PropertyPermissionType.OperateStations)));
        }
        private static bool IsPresentCrew(string id,PlayerShip ship)
        {
            var actor=GetObjectByUUID(id);var record=DB.Get<Player>(id);
            return GetIsObjectValid(actor)&&GetIsPC(actor)&&!GetIsDead(actor)&&!IsPlayerInSpaceMode(actor)&&record?.CrewShipId==ship.Id&&
                Property.GetPropertyId(GetArea(actor))==ship.PropertyId&&HasStationPermission(id,ship);
        }
        public static string GetOperatingShipId(uint actor)
        {
            if(!GetIsObjectValid(actor))return null;
            if(!GetIsPC(actor))return GetLocalString(actor,"SPACE_PROXY_SHIP");
            var record=DB.Get<Player>(GetObjectUUID(actor));
            if(!IsPlayerInSpaceMode(actor)&&!string.IsNullOrEmpty(record?.CrewShipId))
            {
                var ship=DB.Get<PlayerShip>(record.CrewShipId);
                if(ship!=null&&ship.Status.Crew.ContainsValue(record.Id)&&IsPresentCrew(record.Id,ship))return ship.Id;
            }
            return record?.ActiveShipId;
        }
        public static uint GetExteriorShip(uint actor)
        {
            if(!GetIsObjectValid(actor))return OBJECT_INVALID;
            if(IsOperatingShip(actor))return actor;
            if(!GetIsPC(actor))return OBJECT_INVALID;
            var record=DB.Get<Player>(GetObjectUUID(actor));
            if(string.IsNullOrEmpty(record?.CrewShipId))return OBJECT_INVALID;
            var ship=DB.Get<PlayerShip>(record.CrewShipId);
            if(ship==null||!ship.Status.Crew.ContainsValue(record.Id)||!IsPresentCrew(record.Id,ship))return OBJECT_INVALID;
            return _playersInSpace.Concat(_shipNPCs.Keys).FirstOrDefault(x=>GetIsObjectValid(x)&&GetOperatingShipId(x)==ship.Id,OBJECT_INVALID);
        }
        private static Dictionary<ShipCrewStation,uint> ActiveCrew(uint exterior)
        {
            var shipId=GetOperatingShipId(exterior);var ship=string.IsNullOrEmpty(shipId)?null:DB.Get<PlayerShip>(shipId);
            return ship==null?new():ship.Status.Crew.Where(x=>IsPresentCrew(x.Value,ship)).ToDictionary(x=>x.Key,x=>GetObjectByUUID(x.Value));
        }
        public static bool CanOperateSkill(uint actor,SkillType skill)
        {
            var source=GetExteriorShip(actor);
            if(!GetIsObjectValid(source))return false;
            if(!GetIsPC(source)&&string.IsNullOrEmpty(GetLocalString(source,"SPACE_PROXY_PILOT")))return actor==source;
            var pilot=GetIsPC(source)?GetObjectUUID(source):GetLocalString(source,"SPACE_PROXY_PILOT");
            return ShipCrewPolicy.CanOperate(GetObjectUUID(actor),pilot,skill,ActiveCrew(source).ToDictionary(x=>x.Key,x=>GetObjectUUID(x.Value)))&&
                (!GetIsPC(actor)||DB.Get<Player>(GetObjectUUID(actor)).ShipOperations.ReadyAt<=DateTime.UtcNow);
        }
        private static uint SkillOperator(uint source,SkillType skill)=>ShipCrewPolicy.Station(skill) is ShipCrewStation station&&ActiveCrew(source).TryGetValue(station,out var actor)?actor:source;
        public static int GetOwnOperatingRank(uint actor,SkillType skill)=>GetIsPC(actor)?Math.Clamp(Skill.GetCreatureSkillRank(actor,skill),0,50):GetOperatingSkills(actor).GetValueOrDefault(skill);
        public static bool StationAllowsSkill(uint actor,SkillType skill)
        {
            var record=DB.Get<Player>(GetObjectUUID(actor));var ship=string.IsNullOrEmpty(record?.CrewShipId)?null:DB.Get<PlayerShip>(record.CrewShipId);
            if(ship==null||!IsPresentCrew(record.Id,ship))return true;
            return ship.Status.Crew.Any(x=>x.Value==record.Id&&ShipCrewPolicy.Station(skill)==x.Key);
        }
        public static PlayerShip RequireOperatingPreparation(uint actor,string shipId)
        {
            var record=DB.Get<Player>(GetObjectUUID(actor));var ship=DB.Get<PlayerShip>(shipId);
            if(ship!=null&&record?.CrewShipId==shipId&&ship.Status.Crew.ContainsValue(record.Id)&&IsPresentCrew(record.Id,ship))
            {
                var property=DB.Get<WorldProperty>(ship.PropertyId);
                if(property.Positions.ContainsKey(PropertyLocationType.CurrentPosition))throw new InvalidOperationException("Dock before preparing station techniques.");
                if(HasCommittedShipWork(shipId)||ship.Status.PendingModuleActivations.Count>0)throw new InvalidOperationException("Finish committed ship work first.");
                return ship;
            }
            return ShipEquipmentTransfers.RequireDock(actor,shipId);
        }
        public static void JoinShipStation(uint actor,ShipCrewStation station)
        {
            if(!GetIsPC(actor)||GetIsDead(actor)||IsPlayerInSpaceMode(actor))throw new InvalidOperationException("Enter a docked ship's interior to take a station.");
            var propertyId=Property.GetPropertyId(GetArea(actor));
            var ship=string.IsNullOrEmpty(propertyId)?null:DB.Search(new DBQuery<PlayerShip>().AddFieldSearch(nameof(PlayerShip.PropertyId),propertyId,false)).FirstOrDefault();
            if(ship==null||!HasStationPermission(GetObjectUUID(actor),ship))throw new InvalidOperationException("The ship owner must grant Operate Stations permission.");
            var property=DB.Get<WorldProperty>(ship.PropertyId);
            if(property.Positions.ContainsKey(PropertyLocationType.CurrentPosition)||!string.IsNullOrEmpty(ship.Status.ActiveContractId))throw new InvalidOperationException("Dock and finish the ship's contract before changing stations.");
            var record=DB.Get<Player>(GetObjectUUID(actor));
            if(!string.IsNullOrEmpty(record.CrewShipId)&&record.CrewShipId!=ship.Id)throw new InvalidOperationException("Leave your previous station first.");
            if(ship.Status.Crew.TryGetValue(station,out var old)&&old!=record.Id&&!IsPresentCrew(old,ship))
            {
                ship.Status.Crew.Remove(station);var absent=DB.Get<Player>(old);if(absent?.CrewShipId==ship.Id){absent.CrewShipId=null;DB.Set(absent);}
            }
            ShipCrewPolicy.Assign(ship.Status,station,record.Id);ship.Status.OperatorBuildSignature=null;ship.Status.RefitReadyAt=DateTime.UtcNow.AddSeconds(5);DB.Set(ship);
            record.CrewShipId=ship.Id;record.ShipOperations.ReadyAt=ship.Status.RefitReadyAt;DB.Set(record);
            SendMessageToPC(actor,$"Station: {station}. Ready in 5 seconds. Use /cockpit; the pilot selects exterior targets.");
        }
        public static void LeaveShipStation(uint actor)
        {
            var record=DB.Get<Player>(GetObjectUUID(actor));if(string.IsNullOrEmpty(record.CrewShipId))return;
            var ship=DB.Get<PlayerShip>(record.CrewShipId);
            if(ship!=null)
            {
                var property=DB.Get<WorldProperty>(ship.PropertyId);
                if(property?.Positions.ContainsKey(PropertyLocationType.CurrentPosition)==true||!string.IsNullOrEmpty(ship.Status.ActiveContractId))throw new InvalidOperationException("Dock and finish the ship's contract before changing stations.");
                foreach(var station in ship.Status.Crew.Where(x=>x.Value==record.Id).Select(x=>x.Key).ToArray())ship.Status.Crew.Remove(station);
                ship.Status.OperatorBuildSignature=null;ship.Status.RefitReadyAt=DateTime.UtcNow.AddSeconds(5);DB.Set(ship);
            }
            record.CrewShipId=null;record.ShipOperations.ReadyAt=DateTime.UtcNow.AddSeconds(5);DB.Set(record);SendMessageToPC(actor,"Station released.");
        }
        public static string ShipStationSummary(uint observer,string shipId)
        {
            var ship=DB.Get<PlayerShip>(shipId);if(ship==null)return "No assigned ship.";
            return string.Join("; ",Enum.GetValues<ShipCrewStation>().Select(station=>station+": "+
                (ship.Status.Crew.TryGetValue(station,out var id)?PlayerName.GetDisplayNameByPlayerId(observer,id,"Unknown operator")+(IsPresentCrew(id,ship)?"":" (inactive)"):"pilot")));
        }
        private static void SetContractCrewEntitlements(SpaceContract contract)
        {foreach(var id in contract.CrewByPlayer.Keys){var record=DB.Get<Player>(id);record.ActiveSpaceContractId=contract.Id;DB.Set(record);}}
        private static void RegisterContractCrew(SpaceContract contract,string shipId)
        {
            var ship=DB.Get<PlayerShip>(shipId);if(ship==null)return;
            foreach(var id in ship.Status.Crew.Values.Where(id=>IsPresentCrew(id,ship)))
            {
                var record=DB.Get<Player>(id);
                if(!string.IsNullOrEmpty(record.ActiveSpaceContractId)&&record.ActiveSpaceContractId!=contract.Id)throw new InvalidOperationException("A station operator already has another contract.");
                if(contract.ShipsByPlayer.ContainsKey(id)||contract.CrewByPlayer.ContainsKey(id))throw new InvalidOperationException("A contract operator can hold only one ship role.");
            }
            foreach(var id in ship.Status.Crew.Values.Where(id=>IsPresentCrew(id,ship)))
            {contract.CrewByPlayer[id]=shipId;}
        }
    }
}
