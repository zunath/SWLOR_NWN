using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Service
{
    public static partial class Space
    {
        /// <summary>Pairs fitted high and low modules with their independent feat and recast slots.</summary>
        private static IEnumerable<(ShipModuleFeat Feat, ShipStatus.ShipStatusModule Module)> ModuleRecastSlots(ShipStatus status)
        {
            foreach (var (slot, module) in status.HighPowerModules)
                yield return (ShipModuleFeats[HighSlotToFeat(slot)], module);
            foreach (var (slot, module) in status.LowPowerModules)
                yield return (ShipModuleFeats[LowSlotToFeat(slot)], module);
        }

        /// <summary>Publishes a timed active module; older records retain their deadline and infer their original start.</summary>
        private static void ApplyShipModuleRecast(uint player, ShipModuleFeat feat, ShipStatus.ShipStatusModule module, ShipStatus status)
        {
            if (!GetIsObjectValid(player) || !GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player) ||
                module.RecastTime <= DateTime.UtcNow || !_shipModules.TryGetValue(module.ItemTag, out var detail) ||
                detail.CalculateRecastAction == null) return;
            var startedAt = module.RecastStartedAt;
            if (startedAt == default || startedAt >= module.RecastTime)
            {
                // Older fitted records have only the deadline. Estimate elapsed progress without restarting their timer.
                var seconds = detail.CalculateRecastAction(player, status, module.ModuleBonus);
                startedAt = module.RecastTime.AddSeconds(-Math.Max(0.1, seconds));
            }
            Recast.ApplyRecastDelay(player, feat.RecastGroup, startedAt, module.RecastTime, feat.TextureName, detail.Texture);
        }

        /// <summary>Replaces stale slot displays with fitted artwork and resumes unexpired hardware cooldowns.</summary>
        public static void RestoreShipModuleRecasts(uint player)
        {
            if (!GetIsObjectValid(player) || !GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player)) return;
            var status = GetShipStatus(player);
            ClearShipModuleRecasts(player);
            if (status == null) return;
            foreach (var (feat, module) in ModuleRecastSlots(status))
            {
                if (_shipModules.TryGetValue(module.ItemTag, out var detail)) SetTextureOverride(feat.TextureName, detail.Texture, player);
                ApplyShipModuleRecast(player, feat, module, status);
            }
        }

        /// <summary>Clears ship-slot timers and texture overrides while preserving regular ability recasts.</summary>
        private static void ClearShipModuleRecasts(uint player, Player dbPlayer = null)
        {
            dbPlayer ??= DB.Get<Player>(GetObjectUUID(player));
            foreach (var feat in ShipModuleFeats.Values.Where(x => x.RecastGroup != RecastGroup.Invalid))
            {
                AbilityCooldownVisual.ClearRecastDelay(player, feat.RecastGroup);
                SetTextureOverride(feat.TextureName, string.Empty, player);
                dbPlayer.RecastTimes?.Remove(feat.RecastGroup);
            }
            DB.Set(dbPlayer);
        }
    }
}
