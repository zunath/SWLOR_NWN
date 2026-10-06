using System;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Service
{
    public static class ShipDockService
    {
        private const string PaidReceiptVariable = "SHIP_DOCK_PAID";
        public static void Repair(uint player, string shipId, int quotedPrice)
        {
            var ship = ShipEquipmentTransfers.RequireDock(player, shipId);
            if (ship.Status.PendingDockPayment != null) { RecoverPayment(player, shipId); return; }
            var price = ShipRecovery.DockPrice(ship.Status);
            if (price != quotedPrice) throw new InvalidOperationException("The ship's service bill changed. Review the new amount.");
            if (GetGold(player) < price) throw new InvalidOperationException("Not enough credits for the service bill.");
            ship.Status.PendingDockPayment = new(Guid.NewGuid().ToString(), GetObjectUUID(player), price);
            DB.Set(ship); RecoverPayment(player, shipId);
        }
        public static void RecoverPayment(uint player, string shipId)
        {
            var ship = DB.Get<PlayerShip>(shipId); var payment = ship.Status.PendingDockPayment;
            if (payment == null || payment.PlayerId != GetObjectUUID(player)) return;
            if (GetLocalString(player, PaidReceiptVariable) != payment.Id)
            {
                if (GetGold(player) < payment.Credits) throw new InvalidOperationException("The pending dock service needs its recorded credit payment.");
                TakeGoldFromCreature(payment.Credits, player, true);
                SetLocalString(player, PaidReceiptVariable, payment.Id);
                ExportSingleCharacter(player);
            }
            foreach (var module in ShipFittedStats.Modules(ship.Status).Concat(ship.Status.ConfigurationModules.Values)) module.Condition = 100;
            ShipFittedStats.Recompute(ship.Status, Space.GetOperatingSkills(player), Space.GetShipStatAdjustments(player));
            ShipRecovery.CompleteDockService(ship.Status); DB.Set(ship);
        }
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RecoverServiceOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;
            foreach (var ship in DB.Search(new DBQuery<PlayerShip>()).Where(x => x.Status.PendingDockPayment?.PlayerId == GetObjectUUID(player)).ToArray())
            {
                try { RecoverPayment(player, ship.Id); }
                catch (InvalidOperationException ex) { SendMessageToPC(player, ex.Message); }
            }
        }
    }
}
