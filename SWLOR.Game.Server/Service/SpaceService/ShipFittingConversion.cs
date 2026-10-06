using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public static class ShipFittingConversion
    {
        public const int CurrentVersion = 1;

        // Operates on detached data. The caller durably replaces the ship once the plan succeeds.
        public static ShipStatus Convert(ShipStatus original, ShipFittingCatalog catalog = null, string sourceIdentity = null)
        {
            ArgumentNullException.ThrowIfNull(original);
            catalog ??= ShipFittingCatalog.Default;
            if (original.FittingVersion >= CurrentVersion) return original;
            var status = Newtonsoft.Json.JsonConvert.DeserializeObject<ShipStatus>(
                Newtonsoft.Json.JsonConvert.SerializeObject(original));
            var hull = catalog.Hulls[status.ItemTag];
            status.ResourceDeficits ??= ShipResourceDeficits.Capture(status);
            status.RefitRecovery ??= new();
            status.RefitRecoveryReasons ??= new();
            var installed = new List<ShipStatus.ShipStatusModule>();
            var all = status.HighPowerModules.OrderBy(x => x.Key).Select(x => ("High", x.Key, x.Value))
                .Concat(status.LowPowerModules.OrderBy(x => x.Key).Select(x => ("Low", x.Key, x.Value)))
                .Concat(status.ConfigurationModules.OrderBy(x => x.Key).Select(x => ("Config", x.Key, x.Value)))
                .ToArray();
            status.HighPowerModules.Clear(); status.LowPowerModules.Clear(); status.ConfigurationModules.Clear();
            status.ConfigurationDesign = null;
            status.FittingPowerUsed = 0;
            foreach (var (bank, slot, module) in all)
            {
                var identity = module.ItemInstanceId;
                if (string.IsNullOrEmpty(identity))
                {
                    if (string.IsNullOrWhiteSpace(sourceIdentity))
                        throw new ArgumentException("A persisted ship identity is required when equipment has no item identity.", nameof(sourceIdentity));
                    var key = $"{sourceIdentity}:{bank}:{slot}:{module.SerializedItem}";
                    identity = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(key)).AsSpan(0, 16)).ToString();
                }
                if (installed.Any(x => x.ItemInstanceId == identity) || status.RefitRecovery.ContainsKey(identity))
                    throw new InvalidOperationException("Duplicate fitted item identity: " + identity);
                module.ItemInstanceId = identity;
                var isLegacy = string.IsNullOrEmpty(module.Design);
                var design = module.Design;
                if (string.IsNullOrEmpty(design))
                    design = catalog.DesignByItemTag(module.ItemTag) ?? string.Empty;
                module.OriginalSerializedItem ??= module.SerializedItem;
                if (!string.IsNullOrEmpty(module.OriginalSerializedItem)) status.LegacyEquipmentAudit[identity] = module.OriginalSerializedItem;
                module.Design = design;
                if (isLegacy)
                {
                    module.Calibration = "Standard";
                    module.Quality = (int)Math.Min(100L, (long)Math.Max(0, module.ModuleBonus) * 2);
                    module.Condition = 100;
                }
                if (catalog.Configurations.TryGetValue(design, out var configuration))
                {
                    if (isLegacy) module.QualityDimension = module.Quality > 0 ? ShipQualityDimension.Output : ShipQualityDimension.None;
                    if (status.ConfigurationDesign != null || status.FittingPowerUsed + configuration.Power > hull.Power)
                    { Recover("Configuration slot or fitting power unavailable."); continue; }
                    module.ItemTag = configuration.ItemTag;
                    status.ConfigurationDesign = design;
                    status.ConfigurationModules[1] = module;
                    status.FittingPowerUsed += configuration.Power;
                    installed.Add(module);
                    continue;
                }
                if (!catalog.Modules.TryGetValue(design, out var profile))
                { Recover("Unmapped original equipment retained for review."); continue; }
                module.ItemTag = profile.ItemTag;
                var eligible = profile.QualityDimensions;
                if (isLegacy) module.QualityDimension = eligible.HasFlag(ShipQualityDimension.Output) ? ShipQualityDimension.Output :
                    eligible.HasFlag(ShipQualityDimension.RecoveryFraction) ? ShipQualityDimension.RecoveryFraction :
                    eligible.HasFlag(ShipQualityDimension.Tracking) ? ShipQualityDimension.Tracking :
                    eligible.HasFlag(ShipQualityDimension.Range) ? ShipQualityDimension.Range : ShipQualityDimension.None;
                var variant = catalog.GetVariant(design, module.Calibration);
                var slots = profile.Slot == ShipFittingSlot.High ? status.HighPowerModules : status.LowPowerModules;
                var limit = profile.Slot == ShipFittingSlot.High ? hull.HighSlots : hull.LowSlots;
                if (!hull.Allows(profile.Mount) || slots.Count >= limit || status.FittingPowerUsed + variant.Power > hull.Power ||
                    (profile.MaxFitted > 0 && installed.Count(x => x.Design == design) >= profile.MaxFitted))
                { Recover("Incompatible mount, fitting power, slot, or duplicate module limit."); continue; }
                var newSlot = Enumerable.Range(1, limit).First(x => !slots.ContainsKey(x));
                slots[newSlot] = module;
                installed.Add(module);
                status.FittingPowerUsed += variant.Power;

                void Recover(string reason)
                {
                    status.RefitRecovery.Add(identity, module);
                    status.RefitRecoveryReasons.Add(identity, reason);
                }
            }
            ShipFittedStats.Recompute(status, catalog: catalog);
            status.FittingVersion = CurrentVersion;
            return status;
        }
    }
}
