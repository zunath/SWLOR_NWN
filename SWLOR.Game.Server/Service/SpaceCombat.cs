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
        public static int GetOperatingAttribute(uint creature, AbilityType attribute) => GetIsPC(creature) ? GetAbilityScore(creature, attribute) :
            attribute == AbilityType.Agility && GetShipStatus(creature)?.EncounterProfile is string profile ? SpaceEncounterCatalog.Default.Profiles[profile].Agility : 10;

        public static IReadOnlyDictionary<SkillType, int> GetOperatingSkills(uint creature) =>
            new[] { SkillType.Piloting, SkillType.Gunnery, SkillType.ShipSystems, SkillType.Astrometrics, SkillType.SpaceIndustry }
                .ToDictionary(skill => skill, skill => Math.Clamp(GetIsPC(creature) ? Skill.GetCreatureSkillRank(creature, skill) : GetEncounterSkill(creature, skill), 0, 50));

        private static int GetEncounterSkill(uint creature, SkillType skill)
        {
            var id = GetShipStatus(creature)?.EncounterProfile;
            if (id == null) return Stat.GetNPCStats(creature).Level;
            var profile = SpaceEncounterCatalog.Default.Profiles[id];
            return skill == SkillType.Gunnery ? profile.Gunnery : skill == SkillType.Piloting ? profile.Piloting : 0;
        }

        public static IReadOnlyDictionary<StatType, int> GetShipStatAdjustments(uint creature)
        {
            var result = ShipFittedStats.StatUnits.Keys.ToDictionary(stat => stat, stat => Stat.GetStatAdjustment(creature, stat));
            if (!GetIsPC(creature)) return result;
            var state = DB.Get<Player>(GetObjectUUID(creature)).ShipOperations;
            var mode = ShipTechniqueCatalog.Default.Profiles.FirstOrDefault(x=>x.Kind==ShipPerkKind.Mode && x.Key==state.SelectedMode && Perk.GetPerkLevel(creature,x.Perk)>0);
            if (mode!=null) foreach (var (stat,amount) in mode.Stats)
                result[stat]=checked(result[stat]+(int)Math.Round(amount*ShipFittedStats.StatUnits[stat]));
            return result;
        }

        public static void RefreshOperatingBuild(uint pilot,Player record,ShipStatus status)
        {
            if(status.FittingVersion!=ShipFittingConversion.CurrentVersion)return;
            var skills=new[]{SkillType.Piloting,SkillType.Gunnery,SkillType.ShipSystems,SkillType.Astrometrics,SkillType.SpaceIndustry}.ToDictionary(x=>x,x=>record.Skills.GetValueOrDefault(x)?.Rank??0);
            var signature=record.Id+"/"+record.ShipOperations.SelectedMode+"/"+string.Join("/",skills.Values)+"/"+string.Join("/",record.Perks.OrderBy(x=>(int)x.Key).Select(x=>$"{(int)x.Key}:{x.Value}"));
            if(status.OperatorBuildSignature==signature)return;
            var unowned=ShipTechniqueCatalog.Default.Profiles.Where(x=>x.Kind!=ShipPerkKind.Trait&&record.Perks.GetValueOrDefault(x.Perk)==0).Select(x=>x.Key).ToHashSet();
            status.TemporaryAdjustments.RemoveAll(x=>unowned.Contains(x.Family));
            var changed=record.ShipOperations.Prepared.RemoveAll(unowned.Contains)>0;
            if(record.ShipOperations.SelectedMode!=null && unowned.Contains(record.ShipOperations.SelectedMode)){record.ShipOperations.SelectedMode=null;changed=true;}
            if(changed)DB.Set(record);
            ShipFittedStats.Recompute(status,skills,GetShipStatAdjustments(pilot));status.OperatorBuildSignature=signature;
            Stat.ApplyCreatureMovementRate(pilot);
        }

        public static void PersistShipStatus(uint creature, ShipStatus status)
        {
            if (GetIsPC(creature))
            {
                var player = DB.Get<Player>(GetObjectUUID(creature));
                if (string.IsNullOrEmpty(player.ActiveShipId) || player.ActiveShipId == Guid.Empty.ToString()) return;
                var ship = DB.Get<PlayerShip>(player.ActiveShipId);
                if (ship == null) return;
                ship.Status = status;
                DB.Set(ship);
            }
            else if (_shipNPCs.ContainsKey(creature)) _shipNPCs[creature] = status;
        }

        public static (double Shield, double Hull) ApplyFittedShipDamage(uint source, uint target, double amount,
            double shieldMultiplier = 1, double hullMultiplier = 1)
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
            RecordSpaceDamage(source, target, GetShipStatus(source), status, damage.Shield, damage.Hull);
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
                else DelayCommand(0f, () => AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDeath(), target)));
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
