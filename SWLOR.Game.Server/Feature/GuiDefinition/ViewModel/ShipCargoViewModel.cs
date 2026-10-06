using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public sealed class ShipCargoViewModel : GuiViewModelBase<ShipCargoViewModel, ShipCargoPayload>
    {
        public const string ContentElement = "ship-cargo-content";
        public const string MainContentPartial = "ship-cargo-main";
        private string _shipId;
        private readonly List<string> _lots = new();
        public string Summary { get => Get<string>(); set => Set(value); }
        public string StatusText { get => Get<string>(); set => Set(value); }
        public GuiBindingList<string> CargoRows { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<string> WithdrawLabels { get => Get<GuiBindingList<string>>(); set => Set(value); }
        public GuiBindingList<bool> WithdrawEnabled { get => Get<GuiBindingList<bool>>(); set => Set(value); }
        protected override void Initialize(ShipCargoPayload initialPayload)
        {
            _shipId = initialPayload.ShipId;
            Refresh(); ChangePartialView(ContentElement, MainContentPartial);
        }
        protected override void OnModalClosedRestore() => ChangePartialView(ContentElement, MainContentPartial);
        public Action OnRefresh() => () => Refresh();
        public Action OnLoad() => () => Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select a resource or ship supply stack to load.", item =>
        {
            try { ShipCargoTransfers.Load(Player, _shipId, item); Refresh("Loading the selected stack. Refresh after the transfer settles."); }
            catch (InvalidOperationException ex) { Refresh(ex.Message); }
        });
        public Action OnRecover() => () =>
        {
            try { ShipEquipmentTransfers.RequireDock(Player, _shipId); ShipCargoTransfers.Recover(Player, _shipId); Refresh("Retried pending cargo delivery."); }
            catch (InvalidOperationException ex) { Refresh(ex.Message); }
        };
        public Action OnWithdraw() => () =>
        {
            var index = NuiGetEventArrayIndex();
            if (index < 0 || index >= _lots.Count) return;
            var lotId = _lots[index];
            try { ShipCargoTransfers.Withdraw(Player, _shipId, lotId); Refresh("Cargo withdrawn to inventory."); }
            catch (InvalidOperationException ex) { Refresh(ex.Message); }
        };
        private void Refresh(string message = null)
        {
            var rows = new GuiBindingList<string>(); var labels = new GuiBindingList<string>(); var enabled = new GuiBindingList<bool>();
            var ship = DB.Get<PlayerShip>(_shipId); _lots.Clear();
            if (ship == null) { Summary = "Ship unavailable"; StatusText = "Return to ship management."; }
            else
            {
                Summary = $"Cargo {ShipCargo.Occupied(ship.Status):0.##} / {ship.Status.CargoCapacity:0} units";
                foreach (var (id, lot) in ship.Status.Cargo.OrderBy(x => x.Value.Resref).ThenBy(x => x.Key))
                {
                    rows.Add($"{lot.Quantity:0.##} {lot.Resref.Replace("ore_", "").Replace("ref_", "refined ").Replace('_', ' ')}" + (lot.Compressed ? " (packed)" : "") + (lot.ContractId != null ? " (employer cargo)" : ""));
                    labels.Add($"Withdraw {Math.Min(20, (int)Math.Floor(lot.Quantity + 1e-9))}");
                    enabled.Add(lot.ContractId == null && lot.Quantity >= 1 && ship.Status.PendingCargoTransfers.Count == 0);
                    _lots.Add(id);
                }
                StatusText = message ?? $"Dock access required. {ship.Status.PendingCargoTransfers.Count} pending transfer(s). Fractions stay in the hold until they form whole items.";
            }
            CargoRows = rows; WithdrawLabels = labels; WithdrawEnabled = enabled;
        }
    }
}
