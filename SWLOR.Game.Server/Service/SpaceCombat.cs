using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    public static partial class Space
    {
        public static int GetOperatingAttribute(uint creature, AbilityType attribute)
        {
            var exterior=GetExteriorShip(creature);if(GetIsObjectValid(exterior))creature=exterior;
            if(attribute==AbilityType.Perception)creature=SkillOperator(creature,SkillType.Gunnery);
            if(GetIsPC(creature))return GetAbilityScore(creature,attribute);
            if(!string.IsNullOrEmpty(GetLocalString(creature,"SPACE_PROXY_SHIP")))return GetLocalInt(creature,attribute==AbilityType.Agility?"SPACE_PROXY_AGI":"SPACE_PROXY_PER");
            return attribute==AbilityType.Agility&&GetShipStatus(creature)?.EncounterProfile is string profile?SpaceEncounterCatalog.Default.Profiles[profile].Agility:10;
        }
        public static IReadOnlyDictionary<SkillType,int> GetOperatingSkills(uint creature)
        {
            var exterior=GetExteriorShip(creature);if(GetIsObjectValid(exterior))creature=exterior;
            var pilot=GetLocalString(creature,"SPACE_PROXY_PILOT");
            var record=string.IsNullOrEmpty(pilot)?null:DB.Get<Player>(pilot);
            var skills=new[]{SkillType.Piloting,SkillType.Gunnery,SkillType.ShipSystems,SkillType.Astrometrics,SkillType.SpaceIndustry}
                .ToDictionary(skill=>skill,skill=>Math.Clamp(GetIsPC(creature)?Skill.GetCreatureSkillRank(creature,skill):record!=null?record.Skills.GetValueOrDefault(skill)?.Rank??0:GetEncounterSkill(creature,skill),0,50));
            return ShipCrewPolicy.Skills(skills,ActiveCrew(creature).ToDictionary(x=>x.Key,x=>(IReadOnlyDictionary<SkillType,int>)skills.Keys.ToDictionary(skill=>skill,skill=>GetOwnOperatingRank(x.Value,skill))));
        }
        private static int GetEncounterSkill(uint creature, SkillType skill)
        {
            var id = GetShipStatus(creature)?.EncounterProfile;
            if (id == null) return Stat.GetNPCStats(creature).Level;
            var profile = SpaceEncounterCatalog.Default.Profiles[id];
            return skill == SkillType.Gunnery ? profile.Gunnery : skill == SkillType.Piloting ? profile.Piloting : 0;
        }
        private static IEnumerable<ShipTechniqueProfile> OperatorModifiers(Player record)=>ShipTechniqueCatalog.Default.Profiles
            .Where(x=>record.Perks.GetValueOrDefault(x.Perk)==x.Rank&&(x.Kind==ShipPerkKind.Trait||x.Kind==ShipPerkKind.Mode&&x.Key==record.ShipOperations.SelectedMode));
        public static IReadOnlyDictionary<StatType, int> GetShipStatAdjustments(uint creature)
        {
            var exterior=GetExteriorShip(creature);if(GetIsObjectValid(exterior))creature=exterior;
            var proxyPilot=GetLocalString(creature,"SPACE_PROXY_PILOT");
            var pilot=GetIsPC(creature)?creature:string.IsNullOrEmpty(proxyPilot)?OBJECT_INVALID:GetObjectByUUID(proxyPilot);
            var result = ShipFittedStats.StatUnits.Keys.ToDictionary(stat => stat, stat => Stat.GetStatAdjustment(GetIsObjectValid(pilot)?pilot:creature, stat));
            if(!GetIsObjectValid(pilot)||!GetIsPC(pilot))return result;
            var record=DB.Get<Player>(GetObjectUUID(pilot));var original=OperatorModifiers(record).ToArray();
            void Apply(ShipTechniqueProfile p,int sign)
            {foreach(var (stat,amount) in p.Stats)result[stat]=checked(result[stat]+sign*(int)Math.Round(amount*ShipFittedStats.StatUnits[stat]));}
            foreach(var mode in original.Where(x=>x.Kind==ShipPerkKind.Mode))Apply(mode,1);
            foreach(var (station,actor) in ActiveCrew(creature))
            {
                foreach(var profile in original.Where(x=>ShipCrewPolicy.Station(x.Skill)==station))Apply(profile,-1);
                foreach(var profile in OperatorModifiers(DB.Get<Player>(GetObjectUUID(actor))).Where(x=>ShipCrewPolicy.Station(x.Skill)==station))Apply(profile,1);
            }
            return result;
        }
        public static void RefreshOperatingBuild(uint pilot,Player record,ShipStatus status)
        {
            var source=GetExteriorShip(pilot);if(GetIsObjectValid(source))pilot=source;
            if(status.FittingVersion!=ShipFittingConversion.CurrentVersion)return;
            var operatorRecords=ActiveCrew(pilot).Values.Select(x=>DB.Get<Player>(GetObjectUUID(x))).ToList();
            var pilotRecord=GetIsPC(pilot)?DB.Get<Player>(GetObjectUUID(pilot)):DB.Get<Player>(GetLocalString(pilot,"SPACE_PROXY_PILOT"));
            if(pilotRecord==null)return;operatorRecords.Add(pilotRecord);
            foreach(var op in operatorRecords)
            {
                var unowned=ShipTechniqueCatalog.Default.Profiles.Where(x=>x.Kind!=ShipPerkKind.Trait&&op.Perks.GetValueOrDefault(x.Perk)==0).Select(x=>x.Key).ToHashSet();
                var changed=op.ShipOperations.Prepared.RemoveAll(unowned.Contains)>0;
                if(op.ShipOperations.SelectedMode!=null&&unowned.Contains(op.ShipOperations.SelectedMode)){op.ShipOperations.SelectedMode=null;changed=true;}
                if(changed)DB.Set(op);
            }
            var skills=GetOperatingSkills(pilot);
            var signature=string.Join("/",operatorRecords.OrderBy(x=>x.Id).Select(op=>op.Id+":"+op.ShipOperations.SelectedMode+":"+string.Join(",",op.Skills.OrderBy(x=>(int)x.Key).Select(x=>$"{x.Key}:{x.Value.Rank}"))+":"+string.Join(",",op.Perks.OrderBy(x=>(int)x.Key).Select(x=>$"{x.Key}:{x.Value}"))));
            if(status.OperatorBuildSignature==signature)return;
            var owned=operatorRecords.SelectMany(op=>ShipTechniqueCatalog.Default.Profiles.Where(x=>op.Perks.GetValueOrDefault(x.Perk)>0).Select(x=>x.Key)).ToHashSet();
            var families=ShipTechniqueCatalog.Default.Profiles.Select(x=>x.Key).ToHashSet();
            status.TemporaryAdjustments.RemoveAll(x=>families.Contains(x.Family)&&!owned.Contains(x.Family));
            ShipFittedStats.Recompute(status,skills,GetShipStatAdjustments(pilot));status.OperatorBuildSignature=signature;
            Stat.ApplyCreatureMovementRate(pilot);
        }

        public static void PersistShipStatus(uint creature, ShipStatus status)
        {
            if (GetIsPC(creature))
            {
                var player = DB.Get<Player>(GetObjectUUID(creature));
                var shipId=GetOperatingShipId(creature);
                if (string.IsNullOrEmpty(shipId) || shipId == Guid.Empty.ToString()) return;
                var ship = DB.Get<PlayerShip>(shipId);
                if (ship == null) return;
                ship.Status = status;
                DB.Set(ship);
            }
            else if (_shipNPCs.ContainsKey(creature))
            {
                _shipNPCs[creature]=status;var proxy=GetLocalString(creature,"SPACE_PROXY_SHIP");
                if(!string.IsNullOrEmpty(proxy)){var ship=DB.Get<PlayerShip>(proxy);if(ship!=null){ship.Status=status;DB.Set(ship);}}
            }
        }

        public static (double Shield, double Hull) ApplyFittedShipDamage(uint source, uint target, double amount,
            double shieldMultiplier = 1, double hullMultiplier = 1, string creditOperatorId = null)
        {
            var status = GetShipStatus(target);
            if (status == null) return (0, 0);
            var now = DateTime.UtcNow;
            var effects = ShipTemporaryStats.Current(status, now);
            var damage = ShipOperations.ApplyDamage(status, amount, shieldMultiplier, hullMultiplier,
                Math.Max(0, effects.GetValueOrDefault(StatType.ShipShieldResistance)), Math.Max(0, effects.GetValueOrDefault(StatType.ShipHullResistance)));
            if (amount > 0)
            {
                status.LastHostileActivity = now;
                var sourceStatus = GetShipStatus(source);
                if (sourceStatus != null)
                {
                    sourceStatus.LastHostileActivity = now;
                    if (source != target) PersistShipStatus(source, sourceStatus);
                }
            }
            RecordSpaceDamage(source, target, GetShipStatus(source), status, damage.Shield, damage.Hull,creditOperatorId);
            if (status.EncounterProfile is string profileId && status.Shield <= 0 && damage.Shield + damage.Hull > 0 && source != target && (GetIsEnemy(source, target) || GetIsEnemy(target, source)))
            {
                var exposed = SpaceEncounterCatalog.Default.Profiles[profileId].ExposedSeconds;
                ShipTemporaryStats.Add(status, StatType.ShipExposedSystems, 1, exposed, "exposed systems", now, label: "Exposed systems");
            }
            // Persist before the death event relocates the pilot and settles defeat.
            PersistShipStatus(target, status);
            ExecuteScript("pc_shld_adjusted", target);
            ExecuteScript("pc_hull_adjusted", target);
            if (status.Hull <= 0)
            {
                if (GetIsPC(target)) RescueFittedShipPilot(target);
                else { if(!string.IsNullOrEmpty(GetLocalString(target,"SPACE_PROXY_SHIP"))){SetPlotFlag(target,false);SetImmortal(target,false);} DelayCommand(0f, () => AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDeath(), target))); }
                ClearCurrentTarget(source);
            }
            return damage;
        }

        private static double RestoreFittedResource(uint creature, ShipStatus status, ShipResource resource, double amount,
            bool external, DateTime now, uint benefactor = OBJECT_INVALID)
        {
            if (external)
            {
                ShipFittingCatalog.Default.Hulls.TryGetValue(status.ItemTag, out var baseHull);
                var maximum = resource == ShipResource.Hull ? baseHull?.Hull ?? status.MaxHull : baseHull?.Shield ?? status.MaxShield;
                amount = ShipTemporaryStats.ExternalRecoveryAllowance(status, resource, maximum, amount, now);
            }
            var applied = ShipResources.RestorePrecise(status, resource, amount);
            RecordSpaceRecovery(benefactor, creature, status, resource, applied);
            if (external && applied > 0) status.ExternalRecoveryReceipts.Add(new(resource, applied, now));
            PersistShipStatus(creature, status);
            ExecuteScript("pc_target_upd", creature);
            return applied;
        }
    }
}
