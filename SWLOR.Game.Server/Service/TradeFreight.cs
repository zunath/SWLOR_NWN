using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Freight contracts (plan section 13): an exchange's NPC contracts to haul a sealed load to another exchange by a
    /// deadline for a fixed fee. The contract is on the player's record and its sealed load in a ship's cargo bay.
    /// </summary>
    public static partial class Trade
    {
        /// <summary>The freight contracts the player holds.</summary>
        public static List<FreightContract> GetFreightContracts(uint player)
        {
            return DB.Get<Player>(GetObjectUUID(player))?.FreightContracts ?? new List<FreightContract>();
        }

        /// <summary>Accepts an offer of this exchange into the player's ship docked here: the deposit is held and the load sealed aboard.</summary>
        public static void AcceptFreight(uint player, string shipId, TradeExchangeType exchange, string dockId, string offerId)
        {
            var now = DateTime.UtcNow;
            var ship = GetOwnShipDockedHere(player, shipId, exchange, dockId);
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (ship == null || dbPlayer == null)
            {
                SendMessageToPC(player, ColorToken.Red(FreightContractRules.GetRefusalMessage(FreightRefusal.ShipNotDocked)));
                return;
            }

            if (IsFreightExchangeClosed(player, exchange, dockId))
                return;

            FreightContractRules.PruneTaken(dbPlayer.FreightOffersTaken, now);
            var offer = FreightContractRules.GetOffers(exchange, now).FirstOrDefault(x => x.Id == offerId);
            var refusal = FreightContractRules.CanAccept(
                offer, dbPlayer.FreightContracts, dbPlayer.FreightOffersTaken, GetCargoCapacity(ship), GetFreeVolume(ship), GetGold(player),
                GetFreightStanding(player, exchange));
            if (refusal != FreightRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(FreightContractRules.GetRefusalMessage(refusal)));
                return;
            }

            AssignCommand(player, () => TakeGoldFromCreature(offer.Deposit, player, true));
            var contract = FreightContractRules.Accept(offer, ship.Id, now);
            ship.SealedFreight.Add(FreightContractRules.GetSealedLot(contract));
            dbPlayer.FreightContracts.Add(contract);
            dbPlayer.FreightOffersTaken.Add(offer.Id);
            DB.Set(ship);
            DB.Set(dbPlayer);

            var destination = TradeLookup.GetExchange(offer.Destination)?.Name;
            SendMessageToPC(player, ColorToken.Green(
                $"Contract accepted: {offer.Volume} volume of sealed freight to {destination} within {offer.DeadlineMinutes} minutes " +
                $"for {offer.Fee} credits. Your deposit of {offer.Deposit} credits comes back with an on-time delivery."));
            Log.Write(LogGroup.Space, $"FREIGHT ACCEPT - {PlayerName.GetAuditName(player)} - {offer.Id} {offer.Volume} to {offer.Destination} fee {offer.Fee} deposit {offer.Deposit} - ship {ship.Id}");
        }

        /// <summary>
        /// Whether an incursion's blockade has closed the exchange (plan section 30), telling the player so. A closed
        /// exchange neither offers nor takes freight, as it neither buys nor sells; a load waits for the blockade to fall.
        /// </summary>
        private static bool IsFreightExchangeClosed(uint player, TradeExchangeType exchange, string dockId)
        {
            var closed = Space.GetIncursionExchangeRefusal(GetDock(dockId, exchange));
            if (string.IsNullOrEmpty(closed))
                return false;

            SendMessageToPC(player, ColorToken.Red(closed));
            return true;
        }

        /// <summary>Delivers a contract's sealed load from the player's ship docked at its destination.</summary>
        public static void DeliverFreight(uint player, string shipId, TradeExchangeType exchange, string dockId, string contractId)
        {
            var ship = GetOwnShipDockedHere(player, shipId, exchange, dockId);
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (ship == null || dbPlayer == null)
            {
                SendMessageToPC(player, ColorToken.Red(FreightContractRules.GetRefusalMessage(FreightRefusal.ShipNotDocked)));
                return;
            }

            if (IsFreightExchangeClosed(player, exchange, dockId))
                return;

            var contract = dbPlayer.FreightContracts.FirstOrDefault(x => x.Id == contractId);
            var result = FreightContractRules.Deliver(contract, exchange, ship.SealedFreight, DateTime.UtcNow);
            if (result.Refusal != FreightRefusal.None)
            {
                SendMessageToPC(player, ColorToken.Red(FreightContractRules.GetRefusalMessage(result.Refusal)));
                return;
            }

            ship.SealedFreight.RemoveAll(x => x.ContractId == contract.Id);
            dbPlayer.FreightContracts.Remove(contract);
            DB.Set(ship);
            DB.Set(dbPlayer);
            if (contract.IsAgentCourier)
            {
                Log.Write(LogGroup.Space, $"AGENT COURIER DELIVER - {PlayerName.GetAuditName(player)} - {contract.Id}{(result.IsLate ? " (late)" : string.Empty)}");
                FactionAgents.OnCourierDelivered(player, contract.Id, result.IsLate);
                return;
            }

            GiveGoldToCreature(player, result.Fee + result.DepositReturned);

            SendMessageToPC(player, result.IsLate
                ? ColorToken.Red($"Freight delivered late: {result.Fee} credits paid, and the deposit of {contract.Deposit} credits is forfeit.")
                : ColorToken.Green($"Freight delivered on time: {result.Fee} credits paid and your deposit of {result.DepositReturned} credits returned."));
            RecordStanding(player, TradeLookup.GetExchange(exchange)?.StandingFaction ?? FactionType.Invalid, TradeStandingEvent.DeliveredFreight);
            Space.RecordTradeEarnings(player, result.Fee);
            Log.Write(LogGroup.Space, $"FREIGHT DELIVER - {PlayerName.GetAuditName(player)} - {contract.Id} fee {result.Fee} deposit back {result.DepositReturned}{(result.IsLate ? " (late)" : string.Empty)}");
        }

        /// <summary>Gives up a contract: its sealed load is unloaded and the deposit is forfeit.</summary>
        public static void AbandonFreight(uint player, string contractId)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            var contract = dbPlayer?.FreightContracts.FirstOrDefault(x => x.Id == contractId);
            if (contract == null)
                return;

            dbPlayer.FreightContracts.Remove(contract);
            DB.Set(dbPlayer);

            var ship = string.IsNullOrWhiteSpace(contract.ShipId) ? null : DB.Get<PlayerShip>(contract.ShipId);
            if (ship != null && ship.SealedFreight.RemoveAll(x => x.ContractId == contract.Id) > 0)
                DB.Set(ship);

            if (contract.IsAgentCourier)
            {
                FactionAgents.Abandon(player, contract.Id);
                return;
            }

            SendMessageToPC(player, ColorToken.Red($"Contract abandoned. The deposit of {contract.Deposit} credits is forfeit."));
            Log.Write(LogGroup.Space, $"FREIGHT ABANDON - {PlayerName.GetAuditName(player)} - {contract.Id} deposit {contract.Deposit} forfeit");
        }

        /// <summary>A destroyed ship's sealed loads are lost: their contracts fail and the deposits are forfeit.</summary>
        private static void FailSealedFreight(PlayerShip ship)
        {
            if (ship.SealedFreight == null || ship.SealedFreight.Count == 0)
                return;

            var lost = ship.SealedFreight.Select(x => x.ContractId).ToHashSet();
            ship.SealedFreight.Clear();

            var dbPlayer = DB.Get<Player>(ship.OwnerPlayerId);
            if (dbPlayer == null)
                return;

            var failed = dbPlayer.FreightContracts.Where(x => lost.Contains(x.Id)).ToList();
            dbPlayer.FreightContracts.RemoveAll(x => lost.Contains(x.Id));
            DB.Set(dbPlayer);

            var owner = FindOnlinePlayer(ship.OwnerPlayerId);
            foreach (var contract in failed.Where(x => x.IsAgentCourier))
                FactionAgents.OnCourierLost(ship.OwnerPlayerId, contract.Id);
            foreach (var contract in failed.Where(x => !x.IsAgentCourier))
            {
                if (GetIsObjectValid(owner))
                    SendMessageToPC(owner, ColorToken.Red(
                        $"Your freight for {TradeLookup.GetExchange(contract.Destination)?.Name} was lost with your ship. The contract failed and the deposit of {contract.Deposit} credits is forfeit."));
                Log.Write(LogGroup.Space, $"FREIGHT LOST - player {ship.OwnerPlayerId} - {contract.Id} lost with ship {ship.Id}");
            }
        }

        /// <summary>
        /// Loads a faction agent's sealed courier load (plan section 29) into one of the player's ships docked at the
        /// agent's dock with room for it, and records the run as a contract. Returns the ship id, or empty when none fits.
        /// </summary>
        public static string LoadAgentCourier(uint player, string dockId, TradeExchangeType origin, string missionId,
            TradeExchangeType destination, int volume, int deadlineMinutes)
        {
            var ship = GetShipsDockedAt(player, dockId).FirstOrDefault(x => GetCargoCapacity(x) > 0 && GetFreeVolume(x) >= volume);
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (ship == null || dbPlayer == null)
                return string.Empty;

            var contract = FreightContractRules.AgentCourier(missionId, origin, destination, ship.Id, volume, deadlineMinutes, DateTime.UtcNow);
            ship.SealedFreight.Add(FreightContractRules.GetSealedLot(contract));
            dbPlayer.FreightContracts.Add(contract);
            DB.Set(ship);
            DB.Set(dbPlayer);
            return ship.Id;
        }

        /// <summary>Whether one of the player's ships docked here could take a sealed load of this volume.</summary>
        public static bool HasCourierRoom(uint player, string dockId, int volume, out bool noShip)
        {
            var ships = GetShipsDockedAt(player, dockId).Where(x => GetCargoCapacity(x) > 0).ToList();
            noShip = ships.Count == 0;
            return ships.Any(x => GetFreeVolume(x) >= volume);
        }

        /// <summary>Takes a faction agent's courier load off its ship and drops its contract, when the mission ends without delivery.</summary>
        public static void UnloadAgentCourier(string playerId, string missionId)
        {
            var dbPlayer = DB.Get<Player>(playerId);
            var contract = dbPlayer?.FreightContracts.FirstOrDefault(x => x.Id == missionId && x.IsAgentCourier);
            if (contract == null)
                return;

            dbPlayer.FreightContracts.Remove(contract);
            DB.Set(dbPlayer);
            var ship = string.IsNullOrWhiteSpace(contract.ShipId) ? null : DB.Get<PlayerShip>(contract.ShipId);
            if (ship != null && ship.SealedFreight.RemoveAll(x => x.ContractId == contract.Id) > 0)
                DB.Set(ship);
        }

        private static PlayerShip GetOwnShipDockedHere(uint player, string shipId, TradeExchangeType exchange, string dockId)
        {
            var ship = string.IsNullOrWhiteSpace(shipId) ? null : DB.Get<PlayerShip>(shipId);
            if (ship == null || ship.OwnerPlayerId != GetObjectUUID(player) || !IsDockedAt(ship, dockId))
                return null;

            // A city starport only trades and offers freight once its starport is upgraded enough.
            var dock = GetDock(dockId, exchange);
            return dock.HasExchange && dock.Exchange == exchange ? ship : null;
        }

        private static uint FindOnlinePlayer(string playerId)
        {
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (GetObjectUUID(player) == playerId)
                    return player;
            }

            return OBJECT_INVALID;
        }
    }
}
