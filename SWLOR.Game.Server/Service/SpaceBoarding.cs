using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
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
        private static readonly Dictionary<string,uint> _boardingAreas=new();
        public static bool TryRescueBoardingPlayer(uint player)
        {
            if(!GetIsPC(player)||GetIsDM(player)||string.IsNullOrEmpty(DB.Get<Player>(GetObjectUUID(player)).SpaceBoardingReturnShipId))return false;
            var contract=GetSpaceContract(player);if(SpaceContractPolicy.IsActive(contract))CloseSpaceContract(contract,false);else ReturnBoardingOperator(player,contract,false);
            return true;
        }
        public static void BeginSpaceBoarding(uint player)
        {
            var contract=GetSpaceContract(player)??throw new InvalidOperationException("No active contract.");var profile=SpaceActivityCatalog.Default.Profiles[contract.Profile];
            if(!profile.Boarding||contract.State!=SpaceContractState.Active||contract.Boarded||!IsPlayerInSpaceMode(player)||contract.BoardingPlayers.Contains(GetObjectUUID(player)))throw new InvalidOperationException("Board your contract's disabled exterior target while piloting.");
            if(!_contractObjects.TryGetValue(contract.Id+"/boarding",out var target)||!GetIsObjectValid(target)||GetIsDead(target))throw new InvalidOperationException("The boarding target is unavailable.");
            var status=GetShipStatus(target);var own=GetShipStatus(player);
            if(status==null||status.Hull<=0||status.Hull>status.MaxHull*profile.BoardingHullFraction||GetArea(player)!=GetArea(target)||GetDistanceBetween(player,target)>profile.BoardingRange)throw new InvalidOperationException($"Disable the target to {profile.BoardingHullFraction:P0} hull and approach within {profile.BoardingRange}m.");
            if(HasCommittedShipWork(DB.Get<Player>(GetObjectUUID(player)).ActiveShipId)||own.PendingModuleActivations.Count>0)throw new InvalidOperationException("Finish your ship's committed actions before boarding.");
            var flight=own.FlightId;SendMessageToPC(player,$"Preparing boarding ({profile.BoardingPreparation}s). Your exterior ship remains vulnerable.");
            DelayCommand(profile.BoardingPreparation,()=>
            {
                if(!GetIsObjectValid(player)||!IsPlayerInSpaceMode(player)||GetShipStatus(player)?.FlightId!=flight||GetShipStatus(player)?.Hull<=0||!GetIsObjectValid(target)||GetIsDead(target)||GetArea(player)!=GetArea(target)||GetDistanceBetween(player,target)>profile.BoardingRange||GetShipStatus(target)?.Hull>GetShipStatus(target)?.MaxHull*profile.BoardingHullFraction)return;
                var current=DB.Get<SpaceContract>(contract.Id);if(current?.State!=SpaceContractState.Active)return;
                if(current.BoardingStartedAt==default){current.BoardingStartedAt=DateTime.UtcNow;current.BoardingEndsAt=DateTime.UtcNow.AddSeconds(profile.BoardingSeconds);}
                if(DateTime.UtcNow>=current.BoardingEndsAt)return;
                var area=GetBoardingArea(current,profile);if(!GetIsObjectValid(area))return;
                var id=GetObjectUUID(player);var position=GetPosition(player);var shipId=current.ShipsByPlayer[id];var ship=DB.Get<PlayerShip>(shipId);var property=DB.Get<WorldProperty>(ship.PropertyId);
                property.Positions[PropertyLocationType.CurrentPosition]=new() {AreaResref=GetResRef(GetArea(player)),X=position.X,Y=position.Y,Z=position.Z,Orientation=GetFacing(player)};DB.Set(property);
                current.BoardingPlayers.Add(id);current.BoardingReturnPositions[id]=new(position.X,position.Y,position.Z);DB.Set(current);
                var record=DB.Get<Player>(id);record.SpaceBoardingReturnShipId=shipId;record.SpaceBoardingReturnPlanet=current.OriginPlanet;DB.Set(record);ExitSpaceMode(player);
                var location=Walkmesh.GetRandomLocation(area);AssignCommand(player,()=>{ClearAllActions();ActionJumpToLocation(location);});
            });
        }
        private static uint GetBoardingArea(SpaceContract contract,SpaceActivityProfile profile)
        {
            if(_boardingAreas.TryGetValue(contract.Id,out var existing)&&GetIsObjectValid(existing))return existing;
            var area=Area.CreateInstance("space_boarding","board_"+contract.Id,"Boarding - Security Deck");if(!GetIsObjectValid(area))return OBJECT_INVALID;
            _boardingAreas[contract.Id]=area;SetLocalString(area,"SPACE_BOARDING_CONTRACT",contract.Id);
            for(var i=0;i<profile.BoardingConsoles;i++)
            {
                var obj=CreateObject(ObjectType.Placeable,"space_security",Walkmesh.GetRandomLocation(area));
                if(GetIsObjectValid(obj)){SetLocalString(obj,"SPACE_BOARDING_CONTRACT",contract.Id);SetLocalInt(obj,"SPACE_BOARDING_CONSOLE",i);SetName(obj,$"Security Console {i+1}");}
            }
            return area;
        }
        [NWNEventHandler("spc_board")]
        public static void UseBoardingConsole()
        {
            var obj=OBJECT_SELF;var player=GetLastUsedBy();if(!GetIsPC(player)||GetIsDM(player)||GetIsDead(player))return;
            var id=GetLocalString(obj,"SPACE_BOARDING_CONTRACT");if(string.IsNullOrEmpty(id))return;var contract=DB.Get<SpaceContract>(id);var playerId=GetObjectUUID(player);
            if(contract?.State!=SpaceContractState.Active||!contract.BoardingPlayers.Contains(playerId)||DateTime.UtcNow>=contract.BoardingEndsAt||GetDistanceBetween(player,obj)>3)return;
            var index=GetLocalInt(obj,"SPACE_BOARDING_CONSOLE");if(contract.BoardingConsoles.Contains(index)||GetLocalBool(obj,"SPACE_CONSOLE_BUSY"))return;
            var profile=SpaceActivityCatalog.Default.Profiles[contract.Profile];var area=GetArea(player);SetLocalBool(obj,"SPACE_CONSOLE_BUSY",true);
            SendMessageToPC(player,$"Recovering console manifests ({profile.BoardingConsoleSeconds}s). Stay within 3m.");
            WaitForBoardingConsole(player,obj,area,profile.BoardingConsoleSeconds,()=>
            {
                if(GetIsObjectValid(obj))DeleteLocalBool(obj,"SPACE_CONSOLE_BUSY");
                if(!GetIsObjectValid(player)||GetIsDead(player)||!GetIsObjectValid(obj)||GetArea(player)!=area||GetDistanceBetween(player,obj)>3)return;
                var current=DB.Get<SpaceContract>(id);if(current?.State!=SpaceContractState.Active||!current.BoardingPlayers.Contains(playerId)||DateTime.UtcNow>=current.BoardingEndsAt||current.BoardingConsoles.Contains(index))return;
                var ship=DB.Get<PlayerShip>(current.ShipsByPlayer[playerId]);
                try
                {
                    if(current.BoardingConsoleOperators.TryGetValue(index,out var owner)&&owner!=playerId){SendMessageToPC(player,"That console recovery belongs to its original operator.");return;}
                    current.BoardingConsoleOperators[index]=playerId;DB.Set(current);
                    if(!SpaceBoardingRecovery.GrantConsole(current,profile,ship.Status,index,playerId,DateTime.UtcNow))return;
                    DB.Set(ship);
                    if(current.Boarded&&!string.IsNullOrEmpty(current.BoardingEncounterId)&&current.Receipts.Add("boarding-combat/"+current.BoardingEncounterId))
                    {
                        var encounter=DB.Get<SpaceEncounter>(current.BoardingEncounterId);
                        if(encounter!=null)foreach(var (operatorId,points) in encounter.Contributions.Participants.Where(x=>current.ShipsByPlayer.ContainsKey(x.Key)))
                            foreach(var (skill,amount) in points.Points)current.Contributions.Credit(operatorId,skill,amount);
                    }
                    DB.Set(current);TouchOperatingClock(player,DateTime.UtcNow.AddSeconds(-profile.BoardingConsoleSeconds),profile.BoardingConsoleSeconds);
                }
                catch(InvalidOperationException ex){SendMessageToPC(player,ex.Message);return;}
                SendMessageToPC(player,$"Recovered security manifests: {current.BoardingConsoles.Count}/{profile.BoardingConsoles}. Return to your exterior ship before the boarding timer ends.");
            });
        }
        private static void WaitForBoardingConsole(uint player,uint console,uint area,int seconds,Action complete)
        {
            if(!GetIsObjectValid(player)||GetIsDead(player)||!GetIsObjectValid(console)||GetArea(player)!=area||GetDistanceBetween(player,console)>3)
            {if(GetIsObjectValid(console))DeleteLocalBool(console,"SPACE_CONSOLE_BUSY");return;}
            if(seconds<=0){complete();return;}
            DelayCommand(1f,()=>WaitForBoardingConsole(player,console,area,seconds-1,complete));
        }
        public static void ReturnFromSpaceBoarding(uint player)
        {
            var record=DB.Get<Player>(GetObjectUUID(player));if(string.IsNullOrEmpty(record.SpaceBoardingReturnShipId))throw new InvalidOperationException("You are not aboard a contract target.");
            var contract=GetSpaceContract(player);ReturnBoardingOperator(player,contract,true);
        }
        private static void ReturnBoardingOperator(uint player,SpaceContract contract,bool resume)
        {
            if(!GetIsObjectValid(player))return;var id=GetObjectUUID(player);var record=DB.Get<Player>(id);var shipId=record.SpaceBoardingReturnShipId;if(string.IsNullOrEmpty(shipId))return;
            var ship=DB.Get<PlayerShip>(shipId);if(ship==null)return;
            var property=DB.Get<WorldProperty>(ship.PropertyId);var location=GetLocalLocation(player,"SPACE_INSTANCE_LOCATION");
            if(!GetIsObjectValid(GetAreaFromLocation(location)))
            {
                if(!property.Positions.TryGetValue(PropertyLocationType.DockPosition,out var dock))return;
                var area=string.IsNullOrEmpty(dock.AreaResref)?Property.TryGetLoadedInstance(dock.InstancePropertyId,out var interior)?interior.Area:OBJECT_INVALID:Area.GetAreaByResref(dock.AreaResref);
                if(!GetIsObjectValid(area))
                {
                    if(!Enum.TryParse<Enumeration.PlanetType>(record.SpaceBoardingReturnPlanet,out var planet))return;
                    var fallback=GetDockPointsByPlanet(planet).Values.FirstOrDefault(x=>x.IsNPC&&GetIsObjectValid(GetAreaFromLocation(x.Location)));
                    if(fallback==null)return;location=fallback.Location;
                }
                else location=Location(area,Vector3(dock.X,dock.Y,dock.Z),dock.Orientation);
                resume=false;
            }
            record.SpaceBoardingReturnShipId=null;record.SpaceBoardingReturnPlanet=null;DB.Set(record);
            if(contract!=null){contract.BoardingPlayers.Remove(id);DB.Set(contract);}
            if(GetIsDead(player))ApplyEffectToObject(DurationType.Instant,EffectResurrection(),player);
            AssignCommand(player,()=>{ClearAllActions();ActionJumpToLocation(location);});
            if(resume&&ship.Status.Hull>0&&contract?.State==SpaceContractState.Active&&contract.BoardingReturnPositions.TryGetValue(id,out var point))
                DelayCommand(.3f,()=>{if(!GetIsObjectValid(player)||GetIsDead(player))return;SetLocalLocation(player,"SPACE_INSTANCE_LOCATION",location);SetLocalBool(player,"SPACE_INSTANCE_LOCATION_SET",true);EnterSpaceMode(player,shipId);var area=Area.GetAreaByResref(contract.AreaResref);if(GetIsObjectValid(area))AssignCommand(player,()=>{ClearAllActions();ActionJumpToLocation(Location(area,Vector3((float)point.X,(float)point.Y,(float)point.Z),0));});});
        }
        private static void ProcessSpaceBoarding(SpaceContract contract,SpaceActivityProfile profile)
        {
            if(!profile.Boarding)return;
            if(_contractObjects.TryGetValue(contract.Id+"/boarding",out var target)&&(!GetIsObjectValid(target)||GetIsDead(target)||GetShipStatus(target)?.Hull<=0))
            {if(!contract.Boarded){CloseSpaceContract(contract,false);return;}}
            if(contract.BoardingStartedAt==default||DateTime.UtcNow<contract.BoardingEndsAt)return;
            foreach(var id in contract.BoardingPlayers.ToArray()){var player=GetObjectByUUID(id);if(GetIsObjectValid(player))ReturnBoardingOperator(player,contract,true);}
            if(!contract.Boarded)CloseSpaceContract(contract,false);
        }
        private static void CloseSpaceBoarding(SpaceContract contract)
        {
            foreach(var id in contract.BoardingPlayers.ToArray()){var player=GetObjectByUUID(id);if(GetIsObjectValid(player))ReturnBoardingOperator(player,contract,false);}
            if(_boardingAreas.Remove(contract.Id,out var area)&&GetIsObjectValid(area))DelayCommand(1f,()=>{if(GetIsObjectValid(area)&&!Area.GetPlayersInArea(area).Any())DestroyArea(area);});
        }
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RecoverBoardingOnLogin()
        {var player=GetEnteringObject();if(GetIsPC(player)&&!GetIsDM(player)&&!string.IsNullOrEmpty(DB.Get<Player>(GetObjectUUID(player)).SpaceBoardingReturnShipId))DelayCommand(1f,()=>ReturnBoardingOperator(player,GetSpaceContract(player),false));}
        [NWNEventHandler(ScriptName.OnCreatureDeathBefore)]
        public static void RecoverExteriorShip()
        {
            var creature=OBJECT_SELF;var id=GetLocalString(creature,"SPACE_PROXY_SHIP");if(string.IsNullOrEmpty(id))return;
            var ship=DB.Get<PlayerShip>(id);if(ship==null||ship.Status.Hull>0)return;
            if(!string.IsNullOrEmpty(ship.Status.ActiveContractId)){var contract=DB.Get<SpaceContract>(ship.Status.ActiveContractId);if(SpaceContractPolicy.IsActive(contract))CloseSpaceContract(contract,false);}
            ShipRecovery.Defeat(ship.Status);DB.Set(ship);var property=DB.Get<WorldProperty>(ship.PropertyId);property.Positions.Remove(PropertyLocationType.CurrentPosition);DB.Set(property);_shipClones.Remove(id);
        }
    }
}
