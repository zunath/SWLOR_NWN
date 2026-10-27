using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.TradeDefinition;

namespace SWLOR.Game.Server.Service.TradeService
{
    /// <summary>
    /// NPC freight contracts (plan section 13), mirrored by tools/SpaceRevampBalance/trade.py: each exchange offers a
    /// few sealed loads to other exchanges every hour. Accepting takes a deposit and loads the sealed volume; delivery
    /// pays the fee and returns the deposit when on time. A late delivery pays the fee and forfeits the deposit; an
    /// abandoned or lost load forfeits it with no fee.
    /// </summary>
    public static class FreightContractRules
    {
        private static readonly DateTime Epoch = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static int GetFee(int volume, int hops)
        {
            return (int)Math.Round(volume * (TradeTuning.FreightFeeBase + TradeTuning.FreightFeePerHop * Math.Max(1, hops)),
                MidpointRounding.ToEven);
        }

        public static int GetDeposit(int fee)
        {
            return (int)Math.Ceiling(fee * TradeTuning.FreightDepositRate);
        }

        public static int GetDeadlineMinutes(int hops)
        {
            return TradeTuning.FreightDeadlineBaseMinutes + TradeTuning.FreightDeadlinePerHopMinutes * Math.Max(1, hops);
        }

        /// <summary>The hour slot offers belong to; offers change when it does.</summary>
        public static long GetOfferWindow(DateTime now)
        {
            return (long)Math.Floor((now - Epoch).TotalMinutes / TradeTuning.FreightOfferMinutes);
        }

        /// <summary>When the current offers are replaced.</summary>
        public static DateTime GetNextOfferChange(DateTime now)
        {
            return Epoch.AddMinutes((GetOfferWindow(now) + 1) * TradeTuning.FreightOfferMinutes);
        }

        /// <summary>The exchange's offers this hour, the same for every player: one load of each offer volume, each to another system.</summary>
        public static List<FreightContractOffer> GetOffers(TradeExchangeType origin, DateTime now)
        {
            var offers = new List<FreightContractOffer>();
            // A dock in the same system is a minute away, so no contract runs there.
            var destinations = TradeCatalog.Exchanges
                .Select(x => x.Type)
                .Where(x => x != origin && TradeLookup.GetHops(origin, x) >= 1)
                .ToList();
            if (destinations.Count == 0 || TradeLookup.GetExchange(origin) == null)
                return offers;

            var window = GetOfferWindow(now);
            var random = new System.Random(unchecked((int)(window * 7919 + (int)origin * 104729)));
            for (var slot = 0; slot < TradeTuning.FreightOffersPerExchange; slot++)
            {
                var destination = destinations[random.Next(destinations.Count)];
                var volume = TradeTuning.FreightOfferVolumes[slot % TradeTuning.FreightOfferVolumes.Length];
                var hops = TradeLookup.GetHops(origin, destination);
                var fee = GetFee(volume, hops);
                offers.Add(new FreightContractOffer
                {
                    Id = $"{origin}-{window}-{slot}",
                    Origin = origin,
                    Destination = destination,
                    Volume = volume,
                    Hops = hops,
                    Fee = fee,
                    Deposit = GetDeposit(fee),
                    DeadlineMinutes = GetDeadlineMinutes(hops),
                });
            }

            return offers;
        }

        /// <summary>Whether the player may accept the offer into a ship with this much free volume.</summary>
        public static FreightRefusal CanAccept(
            FreightContractOffer offer,
            IReadOnlyCollection<FreightContract> active,
            IReadOnlyCollection<string> taken,
            int capacity,
            int freeVolume,
            int credits)
        {
            if (offer == null)
                return FreightRefusal.OfferGone;
            if ((taken?.Contains(offer.Id) ?? false) || (active?.Any(x => x.Id == offer.Id) ?? false))
                return FreightRefusal.AlreadyTaken;
            if ((active?.Count ?? 0) >= TradeTuning.FreightMaximumActive)
                return FreightRefusal.TooManyContracts;
            if (capacity <= 0)
                return FreightRefusal.NoCargoBay;
            if (freeVolume < offer.Volume)
                return FreightRefusal.NotEnoughRoom;
            if (credits < offer.Deposit)
                return FreightRefusal.NotEnoughCredits;

            return FreightRefusal.None;
        }

        /// <summary>The contract the player holds once the offer is accepted.</summary>
        public static FreightContract Accept(FreightContractOffer offer, string shipId, DateTime now)
        {
            return new FreightContract
            {
                Id = offer.Id,
                Origin = offer.Origin,
                Destination = offer.Destination,
                ShipId = shipId,
                Volume = offer.Volume,
                Fee = offer.Fee,
                Deposit = offer.Deposit,
                AcceptedAt = now,
                Deadline = now.AddMinutes(offer.DeadlineMinutes),
            };
        }

        /// <summary>The sealed lot the contract loads into the ship.</summary>
        public static SealedFreightLot GetSealedLot(FreightContract contract)
        {
            return new SealedFreightLot { ContractId = contract.Id, Volume = contract.Volume, Destination = contract.Destination };
        }

        /// <summary>What delivering the contract here pays, or why it cannot be delivered.</summary>
        public static FreightDeliveryResult Deliver(
            FreightContract contract,
            TradeExchangeType here,
            IEnumerable<SealedFreightLot> shipFreight,
            DateTime now)
        {
            if (contract == null)
                return FreightDeliveryResult.Refused(FreightRefusal.OfferGone);
            if (contract.Destination != here)
                return FreightDeliveryResult.Refused(FreightRefusal.WrongExchange);
            if (shipFreight == null || shipFreight.All(x => x.ContractId != contract.Id))
                return FreightDeliveryResult.Refused(FreightRefusal.LoadNotAboard);

            var late = now > contract.Deadline;
            return new FreightDeliveryResult
            {
                Fee = contract.Fee,
                DepositReturned = late ? 0 : contract.Deposit,
                IsLate = late,
            };
        }

        /// <summary>The volume sealed loads fill.</summary>
        public static int GetSealedVolume(IEnumerable<SealedFreightLot> freight)
        {
            return (freight ?? Enumerable.Empty<SealedFreightLot>()).Sum(x => Math.Max(0, x.Volume));
        }

        /// <summary>Keeps the taken offer ids of this hour and the last one; older offers are gone anyway.</summary>
        public static void PruneTaken(List<string> taken, DateTime now)
        {
            if (taken == null)
                return;

            var window = GetOfferWindow(now);
            taken.RemoveAll(id =>
            {
                var parts = id.Split('-');
                return parts.Length < 3 || !long.TryParse(parts[parts.Length - 2], out var offerWindow) || offerWindow < window - 1;
            });
        }

        /// <summary>The red text a refused freight action shows.</summary>
        public static string GetRefusalMessage(FreightRefusal refusal)
        {
            return refusal switch
            {
                FreightRefusal.OfferGone => "That contract is no longer offered.",
                FreightRefusal.AlreadyTaken => "You have already taken that contract.",
                FreightRefusal.TooManyContracts => $"You can hold at most {TradeTuning.FreightMaximumActive} freight contracts.",
                FreightRefusal.NoCargoBay => "This ship has no cargo bay.",
                FreightRefusal.NotEnoughRoom => "The ship's cargo bay has no room for that load.",
                FreightRefusal.NotEnoughCredits => "You do not have enough credits for the deposit.",
                FreightRefusal.WrongExchange => "That load is not bound for this exchange.",
                FreightRefusal.LoadNotAboard => "The sealed load is not aboard this ship.",
                FreightRefusal.ShipNotDocked => "That ship is not docked here.",
                _ => string.Empty,
            };
        }
    }
}
