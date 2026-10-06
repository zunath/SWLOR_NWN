using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public enum ShipFittingBank { High, Low, Configuration }
    public enum ShipInventoryTransferDirection { Install, Withdraw }
    public sealed record ShipInventoryTransfer(string PlayerId, ShipInventoryTransferDirection Direction);

    public static class ShipRefitting
    {
        public static ShipStatus Equip(ShipStatus original, ShipFittingBank bank, int slot,
            ShipStatus.ShipStatusModule equipment, IReadOnlyDictionary<SkillType, int> skills,
            DateTime now, IReadOnlyDictionary<StatType, int> adjustments = null, ShipFittingCatalog catalog = null)
        {
            ArgumentNullException.ThrowIfNull(equipment);
            if (string.IsNullOrEmpty(equipment.ItemInstanceId)) throw new ArgumentException("Equipment requires an item identity.");
            catalog ??= ShipFittingCatalog.Default;
            var status = Clone(original);
            var hull = catalog.Hulls[status.ItemTag];
            if (ShipFittedStats.Modules(status).Concat(status.ConfigurationModules.Values)
                    .Any(x => x.ItemInstanceId == equipment.ItemInstanceId) || status.RefitRecovery.ContainsKey(equipment.ItemInstanceId) ||
                status.PendingInventoryTransfers.ContainsKey(equipment.ItemInstanceId))
                throw new InvalidOperationException("This item already belongs to the ship.");
            var slots = Bank(status, bank);
            var maximum = bank == ShipFittingBank.High ? hull.HighSlots : bank == ShipFittingBank.Low ? hull.LowSlots : 1;
            if (slot < 1 || slot > maximum || slots.ContainsKey(slot)) throw new InvalidOperationException("Fitting slot unavailable.");
            if (bank == ShipFittingBank.Configuration)
            {
                if (!catalog.Configurations.ContainsKey(equipment.Design)) throw new InvalidOperationException("Choose a ship configuration.");
                if (equipment.Quality < 0 || equipment.Quality > 100 ||
                    (equipment.QualityDimension != ShipQualityDimension.None && equipment.QualityDimension != ShipQualityDimension.Output))
                    throw new InvalidOperationException("Configurations permit one output refinement.");
                status.ConfigurationDesign = equipment.Design;
            }
            else
            {
                var profile = catalog.Modules[equipment.Design];
                if ((profile.Slot == ShipFittingSlot.High) != (bank == ShipFittingBank.High))
                    throw new InvalidOperationException("This module belongs in a different fitting bank.");
            }
            slots.Add(slot, JsonConvert.DeserializeObject<ShipStatus.ShipStatusModule>(JsonConvert.SerializeObject(equipment)));
            var fitting = ShipFittingCalculator.Calculate(status.ItemTag, ShipFittedStats.Modules(status)
                .Select(x => new ShipFittingModule(x.Design, x.Calibration, x.QualityDimension, x.Quality)),
                skills, status.ConfigurationDesign, catalog);
            if (!fitting.IsLegal) throw new InvalidOperationException(string.Join(" ", fitting.Errors));
            ShipFittedStats.Recompute(status, skills, adjustments, catalog);
            status.RefitReadyAt = now.AddSeconds(5);
            return status;
        }

        public static ShipStatus Remove(ShipStatus original, ShipFittingBank bank, int slot,
            DateTime now, IReadOnlyDictionary<SkillType, int> skills = null,
            IReadOnlyDictionary<StatType, int> adjustments = null, ShipFittingCatalog catalog = null)
        {
            var status = Clone(original);
            var slots = Bank(status, bank);
            if (!slots.Remove(slot, out var module)) throw new InvalidOperationException("No module is fitted in that slot.");
            if (status.PendingInventoryTransfers.ContainsKey(module.ItemInstanceId))
                throw new InvalidOperationException("Complete the pending equipment transfer first.");
            status.RefitRecovery.Add(module.ItemInstanceId, module);
            status.RefitRecoveryReasons.Add(module.ItemInstanceId, "Uninstalled equipment available for dock withdrawal.");
            if (bank == ShipFittingBank.Configuration) status.ConfigurationDesign = null;
            ShipFittedStats.Recompute(status, skills, adjustments, catalog);
            status.RefitReadyAt = now.AddSeconds(5);
            return status;
        }

        public static Dictionary<int, ShipStatus.ShipStatusModule> Bank(ShipStatus status, ShipFittingBank bank) => bank switch {
            ShipFittingBank.High => status.HighPowerModules,
            ShipFittingBank.Low => status.LowPowerModules,
            ShipFittingBank.Configuration => status.ConfigurationModules,
            _ => throw new ArgumentOutOfRangeException(nameof(bank)) };

        private static ShipStatus Clone(ShipStatus status)
        {
            ArgumentNullException.ThrowIfNull(status);
            if (status.FittingVersion != ShipFittingConversion.CurrentVersion)
                throw new InvalidOperationException("Convert the ship's fittings before refitting.");
            return JsonConvert.DeserializeObject<ShipStatus>(JsonConvert.SerializeObject(status));
        }
    }
}
