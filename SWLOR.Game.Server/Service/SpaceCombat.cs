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
        public static IReadOnlyDictionary<SkillType, int> GetOperatingSkills(uint creature) =>
            new[] { SkillType.Piloting, SkillType.Gunnery, SkillType.ShipSystems, SkillType.Astrometrics, SkillType.SpaceIndustry }
                .ToDictionary(skill => skill, skill => Math.Clamp(GetIsPC(creature) ? Skill.GetCreatureSkillRank(creature, skill) : Stat.GetNPCStats(creature).Level, 0, 50));

        public static IReadOnlyDictionary<StatType, int> GetShipStatAdjustments(uint creature) =>
            ShipFittedStats.StatUnits.Keys.ToDictionary(stat => stat, stat => Stat.GetStatAdjustment(creature, stat));

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
            // Persist before the death event relocates the pilot and settles defeat.
            PersistShipStatus(target, status);
            ExecuteScript("pc_shld_adjusted", target);
            ExecuteScript("pc_hull_adjusted", target);
            if (status.Hull <= 0)
            {
                DelayCommand(0f, () => AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectDeath(), target)));
                ClearCurrentTarget(source);
            }
            return damage;
        }

        private static double RestoreFittedResource(uint creature, ShipStatus status, ShipResource resource, double amount,
            bool external, DateTime now)
        {
            if (external)
            {
                ShipFittingCatalog.Default.Hulls.TryGetValue(status.ItemTag, out var baseHull);
                var maximum = resource == ShipResource.Hull ? baseHull?.Hull ?? status.MaxHull : baseHull?.Shield ?? status.MaxShield;
                amount = ShipTemporaryStats.ExternalRecoveryAllowance(status, resource, maximum, amount, now);
            }
            var applied = ShipResources.RestorePrecise(status, resource, amount);
            if (external && applied > 0) status.ExternalRecoveryReceipts.Add(new(resource, applied, now));
            PersistShipStatus(creature, status);
            ExecuteScript("pc_target_upd", creature);
            return applied;
        }
    }
}
