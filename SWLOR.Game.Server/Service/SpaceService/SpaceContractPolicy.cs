using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SkillService;
namespace SWLOR.Game.Server.Service.SpaceService
{
    public static class SpaceContractPolicy
    {
        public static bool IsActive(SpaceContract contract)=>contract!=null&&contract.State is SpaceContractState.Recruiting or SpaceContractState.Loading or SpaceContractState.Active or SpaceContractState.Unloading;
        public static bool CanFinish(SpaceContract contract,SpaceActivityProfile profile,DateTime now)=>
            contract.State==SpaceContractState.Active&&now>=contract.StartedAt.AddSeconds(profile.MinimumSeconds)&&now<contract.ExpiresAt&&contract.CompletedLegs==profile.Legs&&contract.Kills.Count>=profile.Kills.Count&&contract.Surveys.Count>=profile.Surveys&&contract.RecoveredOre+1e-7>=profile.Ore&&contract.RecoveredBulk+1e-7>=profile.Bulk&&contract.UsedAttempts+1e-7>=profile.Attempts&&contract.RescueRecovery+1e-7>=profile.Rescue&&(!profile.Boarding||contract.Boarded);
        public static bool Arrive(SpaceContract contract,SpaceActivityProfile profile,string playerId,double distance,DateTime now)
        {
            if(!contract.ShipsByPlayer.ContainsKey(playerId)||contract.State!=SpaceContractState.Active||contract.CompletedLegs>=profile.Legs||now<contract.StartedAt.AddSeconds(profile.LegInterval*(contract.CompletedLegs+1))||now>=contract.ExpiresAt||!double.IsFinite(distance)||distance<0||distance>profile.LegRadius)return false;
            var receipt="navigation/"+contract.CompletedLegs;
            if(!contract.Receipts.Add(receipt))return false;
            contract.Contributions.Credit(playerId,SkillType.Piloting,1);contract.CompletedLegs++;return true;
        }
        public static bool CreditWork(SpaceContract contract,SpaceSite site,SpaceWorkClaim claim)
        {
            if(!IsActive(contract)||contract.ParticipantShip(claim.PlayerId)!=claim.ShipId||!contract.Sites.Contains(site.Id)||claim.State!=SpaceWorkState.Completed||!contract.Receipts.Add("work/"+claim.Id))return false;
            if(claim.Action==ShipModuleAction.Survey)
            { if(contract.Surveys.Add(site.Id))contract.Contributions.Credit(claim.PlayerId,SkillType.Astrometrics,1); }
            else
            {
                var reserve=claim.Allocations.Values.Sum();
                contract.Contributions.Credit(claim.PlayerId,SkillType.SpaceIndustry,reserve);
                contract.RecoveredOre+=claim.Cargo.Where(x=>x.Key.StartsWith("ore_",StringComparison.Ordinal)).Sum(x=>x.Value);
                contract.RecoveredBulk+=claim.Allocations.GetValueOrDefault("bulk");
                contract.UsedAttempts+=claim.Allocations.GetValueOrDefault("components");
            }
            return true;
        }
        public static bool CreditEncounter(SpaceContract contract,SpaceEncounter encounter)
        {
            if(!IsActive(contract)||!encounter.Completed||encounter.ActivityId!=contract.Id||!contract.EncounterObjectives.ContainsKey(encounter.Id)||!contract.Receipts.Add("encounter/"+encounter.Id))return false;
            contract.Kills.Add(encounter.Id);
            foreach(var (id,points) in encounter.Contributions.Participants.Where(x=>contract.ParticipantShip(x.Key)!=null))
            {
                foreach(var (skill,amount) in points.Points)contract.Contributions.Credit(id,skill,amount);
                if(points.EnergyPoints>0) { if(!contract.Contributions.Participants.TryGetValue(id,out var participant))contract.Contributions.Participants[id]=participant=new(); participant.EnergyPoints+=points.EnergyPoints; }
            }
            return true;
        }
        public static IReadOnlyDictionary<string,Dictionary<SkillType,int>> Experience(SpaceContract contract,SpaceActivityProfile profile)
        {
            var result=contract.Participants.ToDictionary(x=>x,x=>new Dictionary<SkillType,int>());
            foreach(var (skill,total) in profile.SkillPools)
            {
                var hasEnergy=skill==SkillType.ShipSystems&&contract.Contributions.Participants.Values.Any(x=>x.EnergyPoints>0);
                var energy=hasEnergy?(int)Math.Floor(total*.1):0;
                foreach(var (id,amount) in contract.Contributions.Allocate(total-energy,skill))if(result.ContainsKey(id))result[id][skill]=amount;
                foreach(var (id,amount) in contract.Contributions.Allocate(energy,skill,true))if(result.ContainsKey(id))result[id][skill]=result[id].GetValueOrDefault(skill)+amount;
            }
            return result;
        }
        public static int FreightCharge(int reference,double discount)=>Math.Max(2,(int)Math.Ceiling(reference*(1-Math.Clamp(discount,0,.55))));
    }
}
