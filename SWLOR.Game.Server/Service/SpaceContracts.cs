using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
namespace SWLOR.Game.Server.Service
{
    public static partial class Space
    {
        private static readonly Dictionary<string,uint> _contractObjects=new();
        private static IEnumerable<SpaceContract> OpenSpaceContracts()
        {
            for(var offset=0;;offset+=100)
            {
                var page=DB.Search(new DBQuery<SpaceContract>().AddFieldSearch(nameof(SpaceContract.Closed),false).OrderBy(nameof(SpaceContract.Id)).AddPaging(100,offset)).ToArray();
                foreach(var row in page)yield return row;
                if(page.Length<100)yield break;
            }
        }
        public static SpaceContract GetSpaceContract(uint player)
        { var id=DB.Get<Player>(GetObjectUUID(player))?.ActiveSpaceContractId;return string.IsNullOrEmpty(id)?null:DB.Get<SpaceContract>(id); }
        public static void NavigateSpaceContract(uint player)
        {
            var contract=GetSpaceContract(player)??throw new InvalidOperationException("No active contract.");
            if(contract.State!=SpaceContractState.Active||!IsPlayerInSpaceMode(player)||GetResRef(GetArea(player))!=contract.AreaResref)throw new InvalidOperationException("Launch your assigned ship and travel to the contract region first.");
            var profile=SpaceActivityCatalog.Default.Profiles[contract.Profile];
            if(contract.CompletedLegs>=profile.Legs)throw new InvalidOperationException("All navigation legs are complete.");
            var point=contract.Route[contract.CompletedLegs];var location=Location(GetArea(player),Vector3((float)point.X,(float)point.Y,(float)point.Z),0);
            AssignCommand(player,()=>{ClearAllActions();ActionMoveToLocation(location,true);});
        }

        public static void AcceptSpaceContract(uint player,string shipId,string profileId)
        {
            var record=DB.Get<Player>(GetObjectUUID(player));
            if(SpaceContractPolicy.IsActive(GetSpaceContract(player)))throw new InvalidOperationException("Finish or cancel your current contract first.");
            var ship=ShipEquipmentTransfers.RequireDock(player,shipId);
            if(!string.IsNullOrEmpty(ship.Status.ActiveContractId))throw new InvalidOperationException("This ship already has an active contract.");
            var profile=SpaceActivityCatalog.Default.Profiles[profileId];
            if(profile.Freight>ShipCargo.Available(ship.Status))throw new InvalidOperationException($"Reserve {profile.Freight} cargo units before accepting freight.");
            var planet=Planet.GetPlanetType(GetArea(player));
            if(planet==PlanetType.Invalid||GetDockPointsByPlanet(planet).Count==0)throw new InvalidOperationException("Visit a registered planetary dock to accept work.");
            var property=DB.Get<WorldProperty>(ship.PropertyId);
            var origin=Area.GetAreaByResref(property.Positions[PropertyLocationType.SpacePosition].AreaResref);
            var candidates=new List<uint>();
            for(var area=GetFirstArea();GetIsObjectValid(area);area=GetNextArea())
            {
                var table=GetLocalString(area,"RESOURCE_SPAWN_TABLE_ID");
                if(!SpaceIndustryCatalog.IsRegionTable(table))continue;
                var starter=table.EndsWith("VISCARA_ORBIT",StringComparison.Ordinal)||table.EndsWith("MONCALA_ORBIT",StringComparison.Ordinal);
                if(starter==(profile.Region=="starter"))candidates.Add(area);
            }
            if(!candidates.Contains(origin))origin=candidates.OrderBy(GetResRef).FirstOrDefault(OBJECT_INVALID);
            if(!GetIsObjectValid(origin))throw new InvalidOperationException("The contract region is unavailable.");
            var route=new List<SpaceRoutePoint>();
            for(var tries=0;tries<200&&route.Count<profile.Legs;tries++)
            {
                var point=GetPositionFromLocation(Walkmesh.GetRandomLocation(origin));
                if(route.Any(x=>Math.Sqrt(Math.Pow(x.X-point.X,2)+Math.Pow(x.Y-point.Y,2))<profile.MinimumRouteDistance))continue;
                route.Add(new(point.X,point.Y,point.Z));
            }
            if(route.Count!=profile.Legs)throw new InvalidOperationException("The region cannot provide a safe contract route.");
            var contract=new SpaceContract { Id=Guid.NewGuid().ToString(),Profile=profileId,LeaderId=record.Id,OriginPlanet=planet.ToString(),
                AreaResref=GetResRef(origin),Route=route,State=SpaceContractState.Recruiting,StartedAt=DateTime.MaxValue,ExpiresAt=DateTime.UtcNow.AddSeconds(profile.ExpirySeconds),ShipsByPlayer=new() {[record.Id]=shipId} };
            if(profile.Freight>0)
            {
                contract.DestinationPlanet=_dockPoints.Keys.Where(x=>x!=planet&&_dockPoints[x].Values.Any(p=>p.IsNPC)).OrderBy(x=>(int)x).Select(x=>x.ToString()).FirstOrDefault();
                if(contract.DestinationPlanet==null)throw new InvalidOperationException("No destination dock is available.");
            }
            RegisterContractCrew(contract,shipId);DB.Set(contract);SetContractCrewEntitlements(contract);record.ActiveSpaceContractId=contract.Id;DB.Set(record);ship.Status.ActiveContractId=contract.Id;DB.Set(ship);
            StartSpaceContractLoading(contract);
        }
        public static void JoinSpaceContract(uint player,string shipId)
        {
            if(SpaceContractPolicy.IsActive(GetSpaceContract(player)))throw new InvalidOperationException("Finish or cancel your current contract first.");
            SpaceContract contract=null;
            for(var member=GetFirstFactionMember(player);GetIsObjectValid(member);member=GetNextFactionMember(player))
            {
                if(member==player||!GetIsPC(member)||GetArea(member)!=GetArea(player))continue;
                var candidate=GetSpaceContract(member);
                if(candidate?.State==SpaceContractState.Recruiting&&candidate.LeaderId==GetObjectUUID(member)){contract=candidate;break;}
            }
            if(contract==null)throw new InvalidOperationException("A party leader at this dock must accept a group contract first.");
            var ship=ShipEquipmentTransfers.RequireDock(player,shipId);var profile=SpaceActivityCatalog.Default.Profiles[contract.Profile];
            if(contract.ShipsByPlayer.Count>=profile.Party||contract.ShipsByPlayer.Values.Contains(shipId)||!string.IsNullOrEmpty(ship.Status.ActiveContractId))throw new InvalidOperationException("Choose an available ship and contract station.");
            if(Planet.GetPlanetType(GetArea(player)).ToString()!=contract.OriginPlanet)throw new InvalidOperationException("Meet the contract party at its origin dock.");
            var record=DB.Get<Player>(GetObjectUUID(player));contract.ShipsByPlayer[record.Id]=shipId;RegisterContractCrew(contract,shipId);DB.Set(contract);SetContractCrewEntitlements(contract);
            record.ActiveSpaceContractId=contract.Id;DB.Set(record);ship.Status.ActiveContractId=contract.Id;DB.Set(ship);StartSpaceContractLoading(contract);
        }
        private static void StartSpaceContractLoading(SpaceContract contract)
        {
            var profile=SpaceActivityCatalog.Default.Profiles[contract.Profile];
            if(contract.ShipsByPlayer.Count!=profile.Party)return;
            contract.State=SpaceContractState.Loading;contract.TransitionAt=DateTime.UtcNow.AddSeconds(profile.Freight>0?profile.LoadingSeconds:0);DB.Set(contract);
        }
        public static void FinishSpaceContract(uint player,string shipId)
        {
            var contract=GetSpaceContract(player)??throw new InvalidOperationException("No active contract.");
            if(!contract.ShipsByPlayer.TryGetValue(GetObjectUUID(player),out var assigned)||assigned!=shipId)throw new InvalidOperationException("Use your assigned contract ship.");
            ShipEquipmentTransfers.RequireDock(player,shipId);var profile=SpaceActivityCatalog.Default.Profiles[contract.Profile];
            if(!SpaceContractPolicy.CanFinish(contract,profile,DateTime.UtcNow))throw new InvalidOperationException("Finish the route, all objectives and the minimum trip time first.");
            var destination=profile.Freight>0?contract.DestinationPlanet:contract.OriginPlanet;
            if(Planet.GetPlanetType(GetArea(player)).ToString()!=destination)throw new InvalidOperationException("Deliver this contract at "+destination+".");
            foreach(var (id,participantShip) in contract.ShipsByPlayer)
            {
                var pilot=GetObjectByUUID(id);
                if(!GetIsObjectValid(pilot)||Planet.GetPlanetType(GetArea(pilot)).ToString()!=destination)throw new InvalidOperationException("All contract operators must return to the destination dock.");
                ShipEquipmentTransfers.RequireDock(pilot,participantShip);
            }
            contract.State=SpaceContractState.Unloading;contract.TransitionAt=DateTime.UtcNow.AddSeconds(profile.Freight>0?profile.UnloadingSeconds:0);DB.Set(contract);
        }
        public static void CancelSpaceContract(uint player)
        {
            var contract=GetSpaceContract(player);if(!SpaceContractPolicy.IsActive(contract))return;
            if(contract.LeaderId!=GetObjectUUID(player))throw new InvalidOperationException("The party leader must cancel the shared contract.");
            CloseSpaceContract(contract,false);
        }
        private static void CloseSpaceContract(SpaceContract contract,bool success)
        {
            contract.State=success?SpaceContractState.Settling:SpaceContractState.Cancelled;DB.Set(contract);
            CloseSpaceBoarding(contract);
            foreach(var (id,shipId) in contract.ShipsByPlayer)
            {
                var ship=DB.Get<PlayerShip>(shipId);
                if(ship!=null)
                {
                    foreach(var key in ship.Status.Cargo.Where(x=>x.Value.ContractId==contract.Id).Select(x=>x.Key).ToArray())ship.Status.Cargo.Remove(key);
                    ship.Status.ActiveContractId=null;ship.Status.CommittedLegId=null;DB.Set(ship);
                }
                var record=DB.Get<Player>(id);if(record?.ActiveSpaceContractId==contract.Id){record.ActiveSpaceContractId=null;DB.Set(record);}
            }
            foreach(var id in contract.CrewByPlayer.Keys){var record=DB.Get<Player>(id);if(record?.ActiveSpaceContractId==contract.Id){record.ActiveSpaceContractId=null;DB.Set(record);}}
            foreach(var key in _contractObjects.Keys.Where(x=>x.StartsWith(contract.Id+"/",StringComparison.Ordinal)).ToArray())
            { if(_contractObjects.Remove(key,out var obj)&&GetIsObjectValid(obj)){_shipNPCs.Remove(obj);DestroyObject(obj);} }
            foreach(var id in contract.Sites)
            {
                var site=DB.Get<SpaceSite>(id);if(site==null)continue;
                foreach(var claim in site.Claims.Values.Where(x=>x.State==SpaceWorkState.Reserved).Select(x=>x.Id).ToArray())SpaceWorkClaims.Cancel(site,claim);
                site.ExpiresAt=DateTime.UtcNow;site.RespawnsAt=DateTime.MaxValue;DB.Set(site);
            }
            if(success)CreateSpaceContractRewards(contract);
            else {contract.Closed=true;DB.Set(contract);}
        }
        private static void CreateSpaceContractRewards(SpaceContract contract)
        {
            if(contract.RewardJournalComplete)return;
            var profile=SpaceActivityCatalog.Default.Profiles[contract.Profile];var credits=contract.Contributions.Allocate(profile.Credits);var experience=SpaceContractPolicy.Experience(contract,profile);
            foreach(var id in contract.Participants)
            {
                var rewardId="space-contract/"+contract.Id+"/"+id;
                if(DB.Get<SpaceReward>(rewardId)!=null)continue;
                DB.Set(new SpaceReward { Id=rewardId,PlayerId=id,Credits=credits.GetValueOrDefault(id)+(id==contract.LeaderId&&contract.DepositPaid?profile.FreightDeposit:0),Experience=experience[id],Reputation=contract.Contributions.Participants.ContainsKey(id)?profile.Reputation:0 });
            }
            contract.RewardJournalComplete=true;contract.State=SpaceContractState.Completed;contract.Closed=true;DB.Set(contract);
        }
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadSpaceContracts()
        {
            // Native target instances cannot survive a server restart. Frozen settlement journals do.
            foreach(var contract in OpenSpaceContracts().ToArray())
            { if(contract.State==SpaceContractState.Settling)CloseSpaceContract(contract,true);else CloseSpaceContract(contract,false); }
            Scheduler.ScheduleRepeating(ProcessSpaceContracts,TimeSpan.FromSeconds(5));
        }
        private static void ProcessSpaceContracts()
        {
            var now=DateTime.UtcNow;
            foreach(var contract in OpenSpaceContracts().ToArray())
            {
                var profile=SpaceActivityCatalog.Default.Profiles[contract.Profile];
                if(now>=contract.ExpiresAt){CloseSpaceContract(contract,false);continue;}
                if(contract.State==SpaceContractState.Loading&&now>=contract.TransitionAt)
                {
                    var leader=GetObjectByUUID(contract.LeaderId);if(!GetIsObjectValid(leader))continue;
                    try
                    {
                        foreach(var (id,shipId) in contract.ShipsByPlayer)ShipEquipmentTransfers.RequireDock(GetObjectByUUID(id),shipId);
                        var ship=DB.Get<PlayerShip>(contract.ShipsByPlayer[contract.LeaderId]);
                        if(profile.Freight>0)
                        {
                            if(!ShipCargo.CanAdd(ship.Status,profile.Freight))continue;
                            var receipt="SPACE_CONTRACT_PAID";
                            if(GetLocalString(leader,receipt)!=contract.Id)
                            {
                                if(GetGold(leader)<profile.FreightDeposit)continue;
                                TakeGoldFromCreature(profile.FreightDeposit,leader,true);SetLocalString(leader,receipt,contract.Id);ExportSingleCharacter(leader);
                            }
                            contract.DepositPaid=true;ShipCargo.Add(ship.Status,"employer_freight",profile.Freight,"freight/"+contract.Id,contractId:contract.Id);DB.Set(ship);
                        }
                        contract.State=SpaceContractState.Active;contract.StartedAt=now;DB.Set(contract);CreateSpaceContractSites(contract,profile);
                    }
                    catch(InvalidOperationException){continue;}
                }
                if(contract.State==SpaceContractState.Unloading&&now>=contract.TransitionAt)
                {
                    var ship=DB.Get<PlayerShip>(contract.ShipsByPlayer[contract.LeaderId]);
                    if(profile.Freight>0&&ship.Status.Cargo.Values.Where(x=>x.ContractId==contract.Id).Sum(x=>x.Quantity)!=profile.Freight){CloseSpaceContract(contract,false);continue;}
                    var allDocked=contract.ShipsByPlayer.All(x=>IsContractShipDocked(x.Key,x.Value,profile.Freight>0?contract.DestinationPlanet:contract.OriginPlanet));
                    if(allDocked)CloseSpaceContract(contract,true);continue;
                }
                if(contract.State!=SpaceContractState.Active)continue;
                var area=Area.GetAreaByResref(contract.AreaResref);if(!GetIsObjectValid(area)){CloseSpaceContract(contract,false);continue;}
                CreateSpaceContractSites(contract,profile);
                var leg=Math.Min(contract.CompletedLegs,profile.Legs-1);var point=contract.Route[leg];
                foreach(var (id,shipId) in contract.ShipsByPlayer)
                {
                    var pilot=GetObjectByUUID(id);if(!GetIsObjectValid(pilot)||!IsPlayerInSpaceMode(pilot)||DB.Get<Player>(id).ActiveShipId!=shipId||GetArea(pilot)!=area)continue;
                    var ship=DB.Get<PlayerShip>(shipId);var committed=contract.CompletedLegs<profile.Legs?contract.Id+"/leg/"+leg:null;
                    if(ship.Status.CommittedLegId!=committed){ship.Status.CommittedLegId=committed;DB.Set(ship);}
                    var position=GetPosition(pilot);var distance=Math.Sqrt(Math.Pow(position.X-point.X,2)+Math.Pow(position.Y-point.Y,2));
                    if(distance<=profile.LegRadius && contract.CompletedLegs<profile.Legs && now>=contract.StartedAt.AddSeconds(profile.LegInterval*(contract.CompletedLegs+1)))
                    {
                        var legReceipt=contract.Id+"/navigation/"+contract.CompletedLegs;
                        var permanent=Math.Min(.25,Math.Max(0,ShipFittedStats.Bonus(ship.Status,StatType.ShipServiceDiscount)-ShipFittedStats.Penalty(ship.Status,StatType.ShipServiceDiscount)));
                        var temporary=Math.Clamp(ShipTemporaryStats.Current(ship.Status,now).GetValueOrDefault(StatType.ShipServiceDiscount),0,.30);
                        var fee=SpaceContractPolicy.FreightCharge(profile.NavigationFee,permanent+temporary);
                        if(GetLocalString(pilot,"SPACE_NAVIGATION_PAID")!=legReceipt)
                        {if(GetGold(pilot)<fee)continue;TakeGoldFromCreature(fee,pilot,true);SetLocalString(pilot,"SPACE_NAVIGATION_PAID",legReceipt);ExportSingleCharacter(pilot);}
                    }
                    if(SpaceContractPolicy.Arrive(contract,profile,id,distance,now)) {ship.Status.CommittedLegId=null;ship.Status.TemporaryAdjustments.RemoveAll(x=>x.ConsumeOnPaidOperation&&x.Stat==StatType.ShipServiceDiscount);DB.Set(ship);TouchOperatingClock(pilot,now,5);DB.Set(contract);SendMessageToPC(pilot,$"Contract route: {contract.CompletedLegs}/{profile.Legs} arrivals.");break;}
                }
                SpawnSpaceContractRescue(contract,profile,area);
                if(contract.Closed)continue;
                SpawnSpaceContractTargets(contract,profile,area);
                ProcessSpaceBoarding(contract,profile);if(contract.Closed)continue;
                foreach(var encounterId in contract.EncounterObjectives.Keys.ToArray())
                { var encounter=DB.Get<SpaceEncounter>(encounterId);if(encounter!=null&&SpaceContractPolicy.CreditEncounter(contract,encounter))DB.Set(contract); }
            }
        }
        private static bool IsContractShipDocked(string id,string shipId,string planet)
        {
            var player=GetObjectByUUID(id);if(!GetIsObjectValid(player)||Planet.GetPlanetType(GetArea(player)).ToString()!=planet)return false;
            try {ShipEquipmentTransfers.RequireDock(player,shipId);return true;}catch(InvalidOperationException){return false;}
        }
        private static void CreateSpaceContractSites(SpaceContract contract,SpaceActivityProfile profile)
        {
            var area=Area.GetAreaByResref(contract.AreaResref);if(!GetIsObjectValid(area))return;
            var count=profile.Wrecks>0?profile.Wrecks:profile.Ore>0?1:profile.Surveys;
            for(var i=0;i<count;i++)
            {
                var id=contract.Id+"/site/"+i;if(contract.Sites.Contains(id))continue;
                var point=contract.Route[i%contract.Route.Count];SpaceSite site;
                if(profile.Wrecks>0)site=SpaceWorkClaims.NewWreck(id,contract.AreaResref,SpaceEncounterCatalog.Default.Profiles["advanced_interceptor"],contract.Participants,DateTime.UtcNow);
                else
                {
                    var reference=SpaceIndustryCatalog.Default.Deposits[profile.Ore>0?3:5];
                    site=SpaceWorkClaims.NewDeposit(id,contract.AreaResref,i,reference,DateTime.UtcNow);
                    if(profile.Ore>0){site.Reserves=profile.Composition.ToDictionary(x=>x.Key,x=>x.Value*profile.Reserve);site.InitialReserves=new(site.Reserves);}
                }
                site.ActivityId=contract.Id;site.Participants=contract.Participants.ToHashSet();site.ExclusiveUntil=contract.ExpiresAt;site.ExpiresAt=contract.ExpiresAt;site.RespawnsAt=DateTime.MaxValue;
                site.X=point.X+i*2;site.Y=point.Y;site.Z=point.Z;DB.Set(site);contract.Sites.Add(id);
                if(!_sitePools.TryGetValue(area,out var sites))_sitePools[area]=sites=new();if(!sites.Contains(id))sites.Add(id);DB.Set(contract);
            }
        }
        public static void SelectSpaceContractObjective(uint player)
        {
            var contract=GetSpaceContract(player)??throw new InvalidOperationException("No active contract.");
            var profile=SpaceActivityCatalog.Default.Profiles[contract.Profile];
            var target=profile.Rescue>contract.RescueRecovery&&_contractObjects.TryGetValue(contract.Id+"/rescue",out var rescue)?rescue:OBJECT_INVALID;
            if(!GetIsObjectValid(target)&&profile.Boarding&&!contract.Boarded&&_contractObjects.TryGetValue(contract.Id+"/boarding",out var boardingTarget))target=boardingTarget;
            if(!GetIsObjectValid(target))target=_contractObjects.Where(x=>x.Key.StartsWith(contract.Id+"/enemy/",StringComparison.Ordinal)&&GetIsObjectValid(x.Value)&&!GetIsDead(x.Value)).Select(x=>x.Value).FirstOrDefault(OBJECT_INVALID);
            if(!GetIsObjectValid(target))target=contract.Sites.Where(_siteObjects.ContainsKey).Select(x=>_siteObjects[x]).FirstOrDefault(x=>GetIsObjectValid(x)&&GetIndustrySite(x)?.Reserves.Values.Sum()>0,OBJECT_INVALID);
            if(!GetIsObjectValid(target)||GetArea(target)!=GetArea(player))throw new InvalidOperationException("Travel to the contract region and wait for the next objective wave.");
            SetCurrentTarget(player,target);SendMessageToPC(player,"Contract objective selected.");
        }
        private static void SpawnSpaceContractRescue(SpaceContract contract,SpaceActivityProfile profile,uint area)
        {
            if(profile.Rescue<=0)return;
            var key=contract.Id+"/rescue";
            if(_contractObjects.TryGetValue(key,out var existing))
            {
                if(!GetIsObjectValid(existing)||GetIsDead(existing)||GetShipStatus(existing)?.Hull<=0){CloseSpaceContract(contract,false);return;}
                var escort=contract.ShipsByPlayer.Keys.Select(GetObjectByUUID).FirstOrDefault(x=>GetIsObjectValid(x)&&IsPlayerInSpaceMode(x)&&GetArea(x)==area,OBJECT_INVALID);
                if(GetIsObjectValid(escort))AssignCommand(existing,()=>{ClearAllActions();ActionMoveToObject(escort,true,8);});
                return;
            }
            if(!contract.ShipsByPlayer.Keys.Select(GetObjectByUUID).Any(x=>GetIsObjectValid(x)&&IsPlayerInSpaceMode(x)&&GetArea(x)==area))return;
            var point=contract.Route[0];var obj=CreateObject(ObjectType.Creature,"t1cargo",Location(area,Vector3((float)point.X,(float)point.Y,(float)point.Z),0));
            if(!GetIsObjectValid(obj))return;
            _contractObjects[key]=obj;SetLocalString(obj,"SPACE_RESCUE_CONTRACT",contract.Id);SetLocalString(obj,"SPACE_ACTIVITY_ID",contract.Id);SetName(obj,"Disabled Relief Transport");
            ChangeToStandardFaction(obj,StandardFaction.Commoner);
            DelayCommand(.1f,()=>
            {
                var status=GetShipStatus(obj);if(status==null)return;status.HighPowerModules.Clear();
                var encounter=DB.Get<SpaceEncounter>(EncounterId(status));if(encounter!=null){encounter.ActivityId=contract.Id;DB.Set(encounter);}
                foreach(var id in contract.ShipsByPlayer.Keys){var pilot=GetObjectByUUID(id);if(GetIsObjectValid(pilot))SetIsTemporaryFriend(pilot,obj);}
            });
        }
        private static void SpawnSpaceContractTargets(SpaceContract contract,SpaceActivityProfile profile,uint area)
        {
            var participants=contract.ShipsByPlayer.Keys.Select(GetObjectByUUID).Where(x=>GetIsObjectValid(x)&&IsPlayerInSpaceMode(x)&&GetArea(x)==area).ToArray();
            if(participants.Length==0)return;
            var targets=profile.Kills.Concat(profile.Boarding?new[]{profile.BoardingTarget}:Array.Empty<string>()).ToArray();
            for(var i=0;i<targets.Length;i++)
            {
                var boarding=profile.Boarding&&i==targets.Length-1;
                var key=boarding?contract.Id+"/boarding":contract.Id+"/enemy/"+i;
                if(contract.EncounterObjectives.Values.Contains(key)||_contractObjects.ContainsKey(key))continue;
                if(DateTime.UtcNow<contract.StartedAt.AddSeconds(profile.LegInterval*Math.Min(i,profile.Legs-1)))continue;
                var binding=SpaceEncounterCatalog.Default.Bindings.Values.First(x=>x.Profile==targets[i]);var point=contract.Route[i%profile.Legs];
                var obj=CreateObject(ObjectType.Creature,binding.Tag,Location(area,Vector3((float)point.X,(float)point.Y,(float)point.Z),0));
                if(!GetIsObjectValid(obj))continue;
                _contractObjects[key]=obj;SetLocalString(obj,"SPACE_ACTIVITY_ID",contract.Id);
                DelayCommand(.1f,()=>
                {
                    var status=GetShipStatus(obj);if(status==null)return;var encounter=DB.Get<SpaceEncounter>(EncounterId(status));if(encounter==null)return;
                    var current=DB.Get<SpaceContract>(contract.Id);if(current?.State!=SpaceContractState.Active){DestroyObject(obj);return;}
                    encounter.ActivityId=current.Id;DB.Set(encounter);if(boarding)current.BoardingEncounterId=encounter.Id;else current.EncounterObjectives[encounter.Id]=key;DB.Set(current);
                    foreach(var pilot in participants){SetIsTemporaryEnemy(pilot,obj);Enmity.ModifyEnmity(pilot,obj,1);}
                    if(_contractObjects.TryGetValue(contract.Id+"/rescue",out var rescue)&&GetIsObjectValid(rescue))
                    {SetIsTemporaryEnemy(rescue,obj);Enmity.ModifyEnmity(rescue,obj,50);}
                    SetCurrentTarget(obj,participants[0]);
                });
            }
        }
        private static double? ContractRecoveryRemaining(SpaceSite site)
        {
            if(site?.ActivityId==null)return null;
            var contract=DB.Get<SpaceContract>(site.ActivityId);if(!SpaceContractPolicy.IsActive(contract))return 0;
            var profile=SpaceActivityCatalog.Default.Profiles[contract.Profile];if(profile.Ore<=0)return null;
            var pending=contract.Sites.Select(DB.Get<SpaceSite>).Where(x=>x!=null).SelectMany(x=>x.Claims.Values)
                .Where(x=>x.Action==ShipModuleAction.Extraction && (x.State==SpaceWorkState.Reserved || !contract.Receipts.Contains("work/"+x.Id)))
                .Sum(x=>x.State==SpaceWorkState.Reserved?x.Allocations.Values.Sum()*x.Recovery:x.Cargo.Where(k=>k.Key.StartsWith("ore_",StringComparison.Ordinal)).Sum(k=>k.Value));
            return Math.Max(0,profile.Ore-contract.RecoveredOre-pending);
        }
        private static bool CreditContractWork(SpaceSite site,SpaceWorkClaim claim)
        {
            if(claim.ActivityId==null)return false;
            var contract=DB.Get<SpaceContract>(claim.ActivityId);
            if(contract!=null&&SpaceContractPolicy.CreditWork(contract,site,claim))DB.Set(contract);
            return true;
        }
        public static string DescribeSpaceContract(uint player)
        {
            var contract=GetSpaceContract(player);if(contract==null)return "Choose a contract at a dock. Group operators join the leader's contract before departure.";
            var profile=SpaceActivityCatalog.Default.Profiles[contract.Profile];
            var text=$"{profile.Name} | {contract.State} | Party {contract.ShipsByPlayer.Count}/{profile.Party} | Route {contract.CompletedLegs}/{profile.Legs} | {profile.Credits}cr shared, {profile.XP}XP maximum shared.\n{profile.Objective}\nReturn to {(profile.Freight>0?contract.DestinationPlanet:contract.OriginPlanet)}. Region: {contract.AreaResref}.";
            if(contract.State==SpaceContractState.Active&&contract.CompletedLegs<profile.Legs)
            {var point=contract.Route[contract.CompletedLegs];text+=$"\nNext arrival: {point.X:0},{point.Y:0}; opens in {Math.Max(0,(contract.StartedAt.AddSeconds(profile.LegInterval*(contract.CompletedLegs+1))-DateTime.UtcNow).TotalSeconds):0}s.";}
            text+=$"\nHostiles {contract.Kills.Count}/{profile.Kills.Count}; surveys {contract.Surveys.Count}/{profile.Surveys}; ore {contract.RecoveredOre:0.#}/{profile.Ore}; salvage {contract.RecoveredBulk:0.#}/{profile.Bulk} reserve, {contract.UsedAttempts:0}/{profile.Attempts} attempts; rescue {contract.RescueRecovery:0.#}/{profile.Rescue}; boarding consoles {contract.BoardingConsoles.Count}/{profile.BoardingConsoles}.";
            return text;
        }
    }
}
