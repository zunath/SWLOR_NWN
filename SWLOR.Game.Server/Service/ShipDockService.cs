using System.Collections.Generic;
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
        private static int ServicePrice(uint player, ShipStatus status)
        {
            var price=ShipRecovery.DockPrice(status);
            var permanent=Math.Min(.25,Space.GetShipStatAdjustments(player).GetValueOrDefault(StatService.StatType.ShipServiceDiscount)/10000d);
            var temporary=Math.Min(.30,ShipTemporaryStats.Current(status,DateTime.UtcNow).GetValueOrDefault(StatService.StatType.ShipServiceDiscount));
            return price==0?0:Math.Max(2,(int)Math.Ceiling(price*(1-permanent-temporary)));
        }
        public static int Quote(uint player, ShipStatus status)=>Math.Max(0,ServicePrice(player,status)-DB.Get<Player>(GetObjectUUID(player)).SpaceEconomy.ServiceVoucher);
        public static void Repair(uint player, string shipId, int quotedPrice)
        {
            var ship = ShipEquipmentTransfers.RequireDock(player, shipId);
            if (ship.Status.PendingDockPayment != null) { RecoverPayment(player, shipId); return; }
            var servicePrice=ServicePrice(player,ship.Status);
            var record=DB.Get<Player>(GetObjectUUID(player));
            var voucher=Math.Min(servicePrice,record.SpaceEconomy.ServiceVoucher);
            var price=servicePrice-voucher;
            if (price != quotedPrice) throw new InvalidOperationException("The ship's service bill changed. Review the new amount.");
            if (GetGold(player) < price) throw new InvalidOperationException("Not enough credits for the service bill.");
            ship.Status.PendingDockPayment = new(Guid.NewGuid().ToString(), GetObjectUUID(player), price, voucher);
            DB.Set(ship); RecoverPayment(player, shipId);
        }
        public static void RecoverPayment(uint player, string shipId)
        {
            var ship = DB.Get<PlayerShip>(shipId); var payment = ship.Status.PendingDockPayment;
            if (payment == null || payment.PlayerId != GetObjectUUID(player)) return;
            var record=DB.Get<Player>(payment.PlayerId);
            if(record.SpaceEconomy.Receipts.Add("dock/"+payment.Id))
            {
                if(record.SpaceEconomy.ServiceVoucher<payment.Voucher)throw new InvalidOperationException("The recorded service voucher is unavailable.");
                record.SpaceEconomy.ServiceVoucher-=payment.Voucher;DB.Set(record);
            }
            if (GetLocalString(player, PaidReceiptVariable) != payment.Id)
            {
                if (GetGold(player) < payment.Credits) throw new InvalidOperationException("The pending dock service needs its recorded credit payment.");
                TakeGoldFromCreature(payment.Credits, player, true);
                SetLocalString(player, PaidReceiptVariable, payment.Id);
                ExportSingleCharacter(player);
            }
            foreach (var module in ShipFittedStats.Modules(ship.Status).Concat(ship.Status.ConfigurationModules.Values)) module.Condition = 100;
            ShipFittedStats.Recompute(ship.Status, Space.GetOperatingSkills(player), Space.GetShipStatAdjustments(player));
            ShipRecovery.CompleteDockService(ship.Status);
            ship.Status.TemporaryAdjustments.RemoveAll(x=>x.ConsumeOnPaidOperation&&x.Stat==StatService.StatType.ShipServiceDiscount);
            DB.Set(ship);
        }
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RecoverServiceOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;
            foreach (var ship in ShipEquipmentTransfers.AllShips().Where(x => x.Status.PendingDockPayment?.PlayerId == GetObjectUUID(player)).ToArray())
            {
                try { RecoverPayment(player, ship.Id); }
                catch (InvalidOperationException ex) { SendMessageToPC(player, ex.Message); }
            }
        }
    }
}
