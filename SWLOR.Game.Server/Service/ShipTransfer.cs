using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PlayerMarketService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Ship transfers and market sales (plan section 7). A registered ship changes owner whole: the ship record, its
    /// interior property and every structure inside are re-owned, permissions reset to the new owner alone, and the
    /// interior (furniture, stored items, fitted modules and a migrated layout) stays as it is. Hull Designs never
    /// change hands. The rules live in <see cref="ShipTransferRules"/>.
    /// </summary>
    public static class ShipTransfer
    {
        private static readonly Dictionary<string, ShipTransferOffer> _offersByRecipient = new();

        /// <summary>How long a market listing lasts, as <see cref="PlayerMarket.RemoveOldListings"/> applies at module load.</summary>
        private const int ListingDays = 14;

        // Facts

        /// <summary>Reads everything the transfer rules check, from the ship's records and the live game.</summary>
        public static ShipTransferCheck BuildCheck(PlayerShip ship, string recipientPlayerId, bool checkItems = true)
        {
            if (ship == null)
                return null;

            var property = DB.Get<WorldProperty>(ship.PropertyId);
            var recipientShips = string.IsNullOrWhiteSpace(recipientPlayerId)
                ? 0
                : (int)DB.SearchCount(new DBQuery<PlayerShip>()
                    .AddFieldSearch(nameof(PlayerShip.OwnerPlayerId), recipientPlayerId, false));

            return new ShipTransferCheck
            {
                RecipientIsOwner = !string.IsNullOrWhiteSpace(recipientPlayerId) && recipientPlayerId == ship.OwnerPlayerId,
                InSpace = property == null || property.Positions.ContainsKey(PropertyLocationType.CurrentPosition) ||
                          GetIsObjectValid(Space.GetShipPilot(ship.Id)),
                DockedAway = Space.IsDockedAboardHost(ship, out _),
                PlayersAboard = CountPlayersAboard(ship.PropertyId),
                ListedForSale = IsListedForSale(ship.Id),
                CargoLots = ship.Cargo?.Count ?? 0,
                SealedFreightLots = ship.SealedFreight?.Count ?? 0,
                HasUntradeableItem = checkItems && property != null && HasUntradeableItemAboard(ship, property),
                RecipientShipCount = recipientShips,
            };
        }

        private static int CountPlayersAboard(string propertyId)
        {
            if (!Property.TryGetLoadedInstance(propertyId, out var instance))
                return 0;

            var count = 0;
            for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
            {
                if (!GetIsDM(player) && GetArea(player) == instance.Area)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Whether a stored item or a fitted module aboard, or anything nested inside a bag or container, cannot
        /// normally be traded (cursed: undroppable), so a transfer or market sale never passes on bound items.
        /// </summary>
        private static bool HasUntradeableItemAboard(PlayerShip ship, WorldProperty property)
        {
            static bool IsUntradeable(string data)
            {
                return StoredObjectData.ContainsCursedItem(data);
            }

            var modules = ship.Status.HighPowerModules.Values
                .Concat(ship.Status.MidPowerModules.Values)
                .Concat(ship.Status.LowPowerModules.Values);
            if (modules.Any(module => IsUntradeable(module?.SerializedItem)))
                return true;

            foreach (var owned in ShipTransferRules.CollectOwnedProperties(property.Id, DB.Get<WorldProperty>))
            {
                foreach (var category in GetCategories(owned.Id))
                {
                    if (category.Items.Values.Any(item => IsUntradeable(item.Data)))
                        return true;
                }
            }

            return false;
        }

        private static List<WorldPropertyCategory> GetCategories(string propertyId)
        {
            var query = new DBQuery<WorldPropertyCategory>()
                .AddFieldSearch(nameof(WorldPropertyCategory.ParentPropertyId), propertyId, false);
            var count = (int)DB.SearchCount(query);
            return count == 0 ? new List<WorldPropertyCategory>() : DB.Search(query.AddPaging(count, 0)).ToList();
        }

        private static List<WorldPropertyPermission> GetPermissions(string propertyId)
        {
            var query = new DBQuery<WorldPropertyPermission>()
                .AddFieldSearch(nameof(WorldPropertyPermission.PropertyId), propertyId, false);
            var count = (int)DB.SearchCount(query);
            return count == 0 ? new List<WorldPropertyPermission>() : DB.Search(query.AddPaging(count, 0)).ToList();
        }

        // Names and summary

        public static string GetShipName(PlayerShip ship)
        {
            var name = ship == null ? null : DB.Get<WorldProperty>(ship.PropertyId)?.CustomName;
            return string.IsNullOrWhiteSpace(name) ? "the ship" : name;
        }

        private static string GetHullName(PlayerShip ship)
        {
            return Space.FindShipDetail(ship?.Status?.ItemTag)?.Name ?? "Starship";
        }

        /// <summary>The name of the area the ship is docked in, or "space" while it flies.</summary>
        public static string GetDockName(PlayerShip ship)
        {
            var area = GetDockArea(ship);
            return GetIsObjectValid(area) ? GetName(area) : "an unknown dock";
        }

        /// <summary>The area the ship is docked in, or OBJECT_INVALID while it flies, is docked aboard or its dock is unloaded.</summary>
        public static uint GetDockArea(PlayerShip ship)
        {
            var property = ship == null ? null : DB.Get<WorldProperty>(ship.PropertyId);
            if (property == null ||
                property.Positions.ContainsKey(PropertyLocationType.CurrentPosition) ||
                !property.Positions.TryGetValue(PropertyLocationType.DockPosition, out var dock))
            {
                return OBJECT_INVALID;
            }

            if (!string.IsNullOrWhiteSpace(dock.AreaResref))
                return Area.GetAreaByResref(dock.AreaResref);

            return Property.TryGetLoadedInstance(dock.InstancePropertyId, out var instance) ? instance.Area : OBJECT_INVALID;
        }

        /// <summary>What the recipient or buyer reads: ship, design, modules, furniture, stored items and dock.</summary>
        public static string BuildSummary(PlayerShip ship)
        {
            var detail = Space.FindShipDetail(ship.Status.ItemTag);
            var designName = detail == null
                ? HullDesignRules.StandardDesignName
                : HullDesignRules.DesignName(ship.Status.ItemTag, detail.Appearance, Space.GetShipAppearance(ship, detail));
            var moduleCount = ship.Status.HighPowerModules.Count + ship.Status.MidPowerModules.Count + ship.Status.LowPowerModules.Count;
            var owned = ShipTransferRules.CollectOwnedProperties(ship.PropertyId, DB.Get<WorldProperty>);
            var furniture = owned.Count(x => x.PropertyType == PropertyType.Structure);
            var storedItems = owned.SelectMany(x => GetCategories(x.Id)).Sum(category => category.Items.Count);

            return ShipTransferRules.BuildSummary(GetShipName(ship), GetHullName(ship), designName, moduleCount, furniture,
                storedItems, GetDockName(ship));
        }

        // Completing a transfer

        /// <summary>
        /// Hands the ship to its new owner. Ships docked aboard go to the starport first, droids posted aboard go back
        /// to their owners, any market listing ends, and the ship, its interior and every structure inside are
        /// re-owned. Every permission on them resets: the old owner and their guests lose access and the new owner
        /// gets full rights. The caller has already checked <see cref="ShipTransferRules.GetRefusal"/>. The transfer is
        /// saved on the ship before anything changes hands (with a sale's proceeds, when the caller set them), and every
        /// step is safe to repeat, so a restart part-way through finishes it at the next module load.
        /// </summary>
        public static void Complete(PlayerShip ship, string newOwnerPlayerId, string reason)
        {
            if (string.IsNullOrWhiteSpace(ship.TransferToPlayerId))
            {
                ship.TransferToPlayerId = newOwnerPlayerId;
                ship.TransferFromPlayerId = ship.OwnerPlayerId;
                ship.TransferReason = reason;
                DB.Set(ship);
            }

            var oldOwnerPlayerId = ship.TransferFromPlayerId;
            var shipName = GetShipName(ship);

            Space.UndockAllToStarport(ship, reason);
            Space.ReturnShipDroidCrew(ship, reason);
            RemoveListings(ship.Id);

            var owned = ShipTransferRules.CollectOwnedProperties(ship.PropertyId, DB.Get<WorldProperty>);
            ShipTransferRules.Reown(ship, owned, newOwnerPlayerId);

            foreach (var property in owned)
            {
                ResetPermissions(property.Id, property.PropertyType, newOwnerPlayerId);
                foreach (var category in GetCategories(property.Id))
                    ResetPermissions(category.Id, PropertyType.Category, newOwnerPlayerId);

                DB.Set(property);
            }

            var dbOldOwner = DB.Get<Player>(oldOwnerPlayerId);
            if (dbOldOwner != null && dbOldOwner.ActiveShipId == ship.Id)
            {
                dbOldOwner.ActiveShipId = Guid.Empty.ToString();
                DB.Set(dbOldOwner);
            }

            ship.TransferToPlayerId = string.Empty;
            ship.TransferFromPlayerId = string.Empty;
            ship.TransferReason = string.Empty;
            DB.Set(ship);

            Log.Write(LogGroup.Property, $"Ship {ship.Id} ({shipName}) changed owner from {oldOwnerPlayerId} to {newOwnerPlayerId}: {reason}.");
        }

        /// <summary>
        /// Deletes every permission on the property and gives the new owner full rights, when the property had any
        /// (furniture has none). The ship and its storage categories always get the new owner's rights, so a transfer
        /// a restart interrupted after the old rows were deleted still restores them when it is finished.
        /// </summary>
        private static void ResetPermissions(string propertyId, PropertyType type, string newOwnerPlayerId)
        {
            var existing = GetPermissions(propertyId);
            if (existing.Count == 0 && type != PropertyType.Starship && type != PropertyType.Category)
                return;

            foreach (var permission in existing)
                DB.Delete<WorldPropertyPermission>(permission.Id);

            DB.Set(ShipTransferRules.BuildFullPermission(propertyId, newOwnerPlayerId, Property.GetPermissionsByPropertyType(type)));
        }

        // Direct transfer (gifts)

        /// <summary>
        /// The owner offers the selected ship to another player at the same starport. The recipient gets a summary to
        /// accept or decline; the giver pays the transfer fee when it completes.
        /// </summary>
        public static void Offer(uint giver, PlayerShip ship, uint recipient)
        {
            if (!GetIsPC(recipient) || GetIsDM(recipient) || GetIsDMPossessed(recipient))
            {
                FloatingTextStringOnCreature("Only another player can receive a ship.", giver, false);
                return;
            }

            if (recipient == giver)
            {
                FloatingTextStringOnCreature("You already own this ship.", giver, false);
                return;
            }

            var dockArea = GetDockArea(ship);
            if (!GetIsObjectValid(dockArea) || GetArea(giver) != dockArea || GetArea(recipient) != dockArea)
            {
                FloatingTextStringOnCreature("You and the recipient must both be at the starport where the ship is docked.", giver, false);
                return;
            }

            var recipientId = GetObjectUUID(recipient);
            var refusal = ShipTransferRules.GetRefusal(BuildCheck(ship, recipientId), false);
            if (refusal != ShipTransferRefusal.None)
            {
                FloatingTextStringOnCreature(ShipTransferRules.GetRefusalMessage(refusal), giver, false);
                return;
            }

            var fee = ShipTransferRules.GetTransferFee(ship.Status.Size);
            if (GetGold(giver) < fee)
            {
                FloatingTextStringOnCreature($"A ship transfer costs {fee} credits.", giver, false);
                return;
            }

            _offersByRecipient[recipientId] = new ShipTransferOffer
            {
                ShipId = ship.Id,
                GiverPlayerId = GetObjectUUID(giver),
                RecipientPlayerId = recipientId,
                Fee = fee,
                ExpiresAt = DateTime.UtcNow.AddSeconds(ShipTransferRules.OfferSeconds)
            };

            SendMessageToPC(giver, $"You offer {GetShipName(ship)} to {GetName(recipient)}. The transfer fee of {fee} credits is paid when they accept.");
            ConversationMenu.Start(recipient, recipient, nameof(ShipTransferOfferDialog));
        }

        /// <summary>The offer waiting for this recipient, or null when there is none or it expired.</summary>
        public static ShipTransferOffer GetOffer(uint recipient)
        {
            var recipientId = GetObjectUUID(recipient);
            if (!_offersByRecipient.TryGetValue(recipientId, out var offer))
                return null;

            if (offer.ExpiresAt >= DateTime.UtcNow)
                return offer;

            _offersByRecipient.Remove(recipientId);
            return null;
        }

        /// <summary>The recipient turned the offer down.</summary>
        public static void Decline(uint recipient)
        {
            var offer = GetOffer(recipient);
            if (offer == null)
                return;

            _offersByRecipient.Remove(offer.RecipientPlayerId);
            var giver = DMPlayerDirectory.GetOnlinePlayer(offer.GiverPlayerId);
            if (GetIsObjectValid(giver))
                SendMessageToPC(giver, $"{GetName(recipient)} declined the ship.");
        }

        /// <summary>
        /// The recipient accepted: everything is checked again, then the giver's own queue pays the fee
        /// (TakeGoldFromCreature does nothing from a dialog or NUI handler) and the ship changes owner.
        /// </summary>
        public static void Accept(uint recipient)
        {
            var offer = GetOffer(recipient);
            if (offer == null)
            {
                FloatingTextStringOnCreature("The offer has expired.", recipient, false);
                return;
            }

            _offersByRecipient.Remove(offer.RecipientPlayerId);
            var giver = DMPlayerDirectory.GetOnlinePlayer(offer.GiverPlayerId);
            if (!CanCompleteGift(giver, recipient, offer, out _))
                return;

            AssignCommand(giver, () => CompleteGift(giver, recipient, offer));
        }

        /// <summary>Whether the offered ship can still change owner now, telling the players why not.</summary>
        private static bool CanCompleteGift(uint giver, uint recipient, ShipTransferOffer offer, out PlayerShip ship)
        {
            ship = DB.Get<PlayerShip>(offer.ShipId);
            if (!GetIsObjectValid(giver) || ship == null || ship.OwnerPlayerId != offer.GiverPlayerId)
            {
                FloatingTextStringOnCreature("The offer is no longer valid.", recipient, false);
                return false;
            }

            var dockArea = GetDockArea(ship);
            if (!GetIsObjectValid(dockArea) || GetArea(giver) != dockArea || GetArea(recipient) != dockArea)
            {
                FloatingTextStringOnCreature("You and the giver must both be at the starport where the ship is docked.", recipient, false);
                return false;
            }

            var refusal = ShipTransferRules.GetRefusal(BuildCheck(ship, offer.RecipientPlayerId), false);
            if (refusal != ShipTransferRefusal.None)
            {
                var message = ShipTransferRules.GetRefusalMessage(refusal);
                FloatingTextStringOnCreature(message, recipient, false);
                FloatingTextStringOnCreature(message, giver, false);
                return false;
            }

            if (GetGold(giver) < offer.Fee)
            {
                FloatingTextStringOnCreature($"The giver cannot pay the {offer.Fee} credit transfer fee.", recipient, false);
                FloatingTextStringOnCreature($"A ship transfer costs {offer.Fee} credits.", giver, false);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Runs on the giver's queue: checks the gift again, takes the fee and saves it in the giver's character file
        /// with a mark naming the transfer, then saves the transfer. A restart in between returns the fee at the giver's
        /// next login (<see cref="SettleInterruptedPayment"/>).
        /// </summary>
        private static void CompleteGift(uint giver, uint recipient, ShipTransferOffer offer)
        {
            if (!CanCompleteGift(giver, recipient, offer, out var ship))
                return;

            var shipName = GetShipName(ship);
            TakeGoldFromCreature(offer.Fee, giver, true);
            SavePayment(giver, new ShipTransferPayment
            {
                Id = Guid.NewGuid().ToString(),
                ShipId = ship.Id,
                NewOwnerPlayerId = offer.RecipientPlayerId,
                Credits = offer.Fee
            });
            Complete(ship, offer.RecipientPlayerId, "changed owner");
            ClearPayment(giver);

            FloatingTextStringOnCreature(ColorToken.Green($"{shipName} is now yours!"), recipient, false);
            FloatingTextStringOnCreature(ColorToken.Green($"{shipName} now belongs to {GetName(recipient)}."), giver, false);
        }

        // Market sales

        /// <summary>The ship's market listing, or null.</summary>
        public static MarketItem GetListing(string shipId)
        {
            if (string.IsNullOrWhiteSpace(shipId))
                return null;

            return DB.Search(new DBQuery<MarketItem>()
                    .AddFieldSearch(nameof(MarketItem.ShipId), shipId, false))
                .FirstOrDefault();
        }

        /// <summary>
        /// Whether the ship is listed for sale: it is locked, so it can't fly, be furnished or be transferred. A
        /// listing that expires (14 days) or is unlisted unlocks it.
        /// </summary>
        public static bool IsListedForSale(string shipId)
        {
            return IsStillListed(GetListing(shipId));
        }

        /// <summary>
        /// Takes the market's ship listings past the market's 14 days off it, so a search does not show ships whose
        /// listing ran out while the server stayed up.
        /// </summary>
        public static void ExpireOldListings(string marketId)
        {
            var query = new DBQuery<MarketItem>()
                .AddFieldSearch(nameof(MarketItem.IsListed), true)
                .AddFieldSearch(nameof(MarketItem.MarketId), marketId, false)
                .AddFieldSearch(nameof(MarketItem.Category), (int)MarketCategoryType.Starship);
            var count = (int)DB.SearchCount(query);
            if (count <= 0)
                return;

            foreach (var listing in DB.Search(query.AddPaging(count, 0)))
            {
                if (!string.IsNullOrWhiteSpace(listing.ShipId))
                    IsStillListed(listing);
            }
        }

        /// <summary>
        /// Whether the listing is still on the market. One past the market's 14 days is taken off here, as
        /// <see cref="PlayerMarket.RemoveOldListings"/> does at module load, so a ship is not locked while the server stays up.
        /// </summary>
        private static bool IsStillListed(MarketItem listing)
        {
            if (listing == null || !listing.IsListed)
                return false;

            if (listing.DateListed == null || Math.Abs(DateTime.UtcNow.Subtract((DateTime)listing.DateListed).Days) < ListingDays)
                return true;

            listing.IsListed = false;
            DB.Set(listing);
            return false;
        }

        /// <summary>Whether the property is a ship listed for sale, or the ship whose interior it is.</summary>
        public static bool IsPropertyListedForSale(string propertyId)
        {
            if (string.IsNullOrWhiteSpace(propertyId))
                return false;

            var ship = DB.Search(new DBQuery<PlayerShip>()
                    .AddFieldSearch(nameof(PlayerShip.PropertyId), propertyId, false))
                .FirstOrDefault();
            return ship != null && IsListedForSale(ship.Id);
        }

        /// <summary>Ends every market listing of the ship (sold, transferred, unregistered or cancelled).</summary>
        public static void RemoveListings(string shipId)
        {
            if (string.IsNullOrWhiteSpace(shipId))
                return;

            foreach (var listing in DB.Search(new DBQuery<MarketItem>()
                         .AddFieldSearch(nameof(MarketItem.ShipId), shipId, false)).ToList())
            {
                DB.Delete<MarketItem>(listing.Id);
            }
        }

        /// <summary>
        /// Lists the owner's ship on the player market at an asking price. The listing points at the ship instead of
        /// holding an item; the ship stays where it is docked and is locked while listed.
        /// </summary>
        public static bool List(uint seller, PlayerShip ship, int price)
        {
            // The ship can change owner while the seller's confirmation is open.
            if (ship.OwnerPlayerId != GetObjectUUID(seller))
            {
                FloatingTextStringOnCreature("You no longer own this ship.", seller, false);
                return false;
            }

            if (price <= 0)
            {
                FloatingTextStringOnCreature("Enter an asking price above zero.", seller, false);
                return false;
            }

            var refusal = ShipTransferRules.GetRefusal(BuildCheck(ship, string.Empty), false);
            if (refusal != ShipTransferRefusal.None)
            {
                FloatingTextStringOnCreature(ShipTransferRules.GetRefusalMessage(refusal), seller, false);
                return false;
            }

            RemoveListings(ship.Id);

            var deed = ObjectPlugin.Deserialize(ship.SerializedItem);
            var iconResref = GetIsObjectValid(deed) ? Item.GetIconResref(deed) : string.Empty;
            var resref = GetIsObjectValid(deed) ? GetResRef(deed) : string.Empty;
            if (GetIsObjectValid(deed))
                DestroyObject(deed);

            var market = PlayerMarket.GetMarketRegion(MarketRegionType.Global);
            var listing = new MarketItem
            {
                MarketId = market.MarketId,
                MarketName = market.Name,
                PlayerId = GetObjectUUID(seller),
                SellerName = GetName(seller),
                SellerCDKey = GetPCPublicCDKey(seller),
                Price = price,
                IsListed = true,
                DateListed = DateTime.UtcNow,
                Name = ShipTransferRules.BuildListingName(GetShipName(ship), GetHullName(ship)),
                Tag = ship.Status.ItemTag,
                Resref = resref,
                Data = string.Empty,
                Quantity = 1,
                IconResref = iconResref,
                Category = MarketCategoryType.Starship,
                ShipId = ship.Id
            };
            DB.Set(listing);

            Log.Write(LogGroup.PlayerMarket, $"{GetName(seller)} [{GetObjectUUID(seller)}] listed ship {ship.Id} for {price} credits.");
            return true;
        }

        /// <summary>
        /// The buyer bought a ship listing: the market's checks (same CD key, price) already passed. The ship is
        /// checked again; if it can't change owner now the listing stays and nothing is paid. Otherwise the purchase
        /// completes on the buyer's own queue (TakeGoldFromCreature does nothing from a NUI handler): the buyer pays,
        /// the seller's proceeds minus the market tax go to their till, and the ship changes owner where it is docked.
        /// Returns whether the purchase went ahead.
        /// </summary>
        public static bool Buy(uint buyer, MarketItem listing, float taxRate)
        {
            if (!CanBuy(buyer, listing, out _))
                return false;

            var listingId = listing.Id;
            var price = listing.Price;
            AssignCommand(buyer, () => CompletePurchase(buyer, listingId, price, taxRate));
            return true;
        }

        /// <summary>Whether the listed ship can still be bought now, telling the buyer why not.</summary>
        private static bool CanBuy(uint buyer, MarketItem listing, out PlayerShip ship)
        {
            ship = listing == null ? null : DB.Get<PlayerShip>(listing.ShipId);
            if (ship == null || ship.OwnerPlayerId != listing.PlayerId)
            {
                if (listing != null)
                    DB.Delete<MarketItem>(listing.Id);
                FloatingTextStringOnCreature("This ship is no longer available.", buyer, false);
                return false;
            }

            // The seller can take the listing off the market, or it can expire, while the buyer's confirmation is open.
            if (!IsStillListed(listing))
            {
                FloatingTextStringOnCreature("This ship is no longer for sale.", buyer, false);
                return false;
            }

            var buyerId = GetObjectUUID(buyer);
            var refusal = ShipTransferRules.GetRefusal(BuildCheck(ship, buyerId), true);
            if (refusal != ShipTransferRefusal.None)
            {
                FloatingTextStringOnCreature("This ship can't be sold right now: " + ShipTransferRules.GetRefusalMessage(refusal), buyer, false);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Runs on the buyer's queue: checks the purchase again, takes the price and saves it in the buyer's character
        /// file with a mark naming the transfer, then saves the transfer with the proceeds owed. A restart before the
        /// transfer is saved returns the price at the buyer's next login (<see cref="SettleInterruptedPayment"/>); one
        /// after finishes the transfer and the payout at the next module load.
        /// </summary>
        private static void CompletePurchase(uint buyer, string listingId, int price, float taxRate)
        {
            var listing = DB.Get<MarketItem>(listingId);
            if (!CanBuy(buyer, listing, out var ship))
                return;

            if (listing.Price != price)
            {
                FloatingTextStringOnCreature("The price of this item has been changed by the seller. Please try again.", buyer, false);
                return;
            }

            if (GetGold(buyer) < price)
            {
                FloatingTextStringOnCreature("You do not have enough credits to purchase this item.", buyer, false);
                return;
            }

            var buyerId = GetObjectUUID(buyer);
            var shipName = GetShipName(ship);
            var dockName = GetDockName(ship);
            TakeGoldFromCreature(price, buyer, true);
            var payment = new ShipTransferPayment { Id = Guid.NewGuid().ToString(), ShipId = ship.Id, NewOwnerPlayerId = buyerId, Credits = price };
            SavePayment(buyer, payment);

            // The proceeds owed are saved with the transfer, before anything changes hands, then paid: a restart
            // part-way through finishes the transfer and the payout at the next module load.
            ship.SaleId = payment.Id;
            ship.SaleSellerPlayerId = listing.PlayerId;
            ship.SaleProceeds = (int)(listing.Price - listing.Price * taxRate);
            Complete(ship, buyerId, "was sold");
            PaySaleProceeds(ship);
            ClearPayment(buyer);

            var seller = DMPlayerDirectory.GetOnlinePlayer(listing.PlayerId);
            if (GetIsObjectValid(seller))
                SendMessageToPC(seller, $"{shipName} sold on the market for {listing.Price} credits. The proceeds are in your market till.");

            FloatingTextStringOnCreature(ColorToken.Green($"{shipName} is now yours! It is docked at {dockName}."), buyer, false);
            Log.Write(LogGroup.PlayerMarket, $"{GetName(buyer)} [{buyerId}] bought ship {ship.Id} from {listing.SellerName} for {listing.Price} credits.");
        }

        /// <summary>
        /// Pays the seller the proceeds a sold ship still owes into their market till, once: the till and the sale's
        /// id are saved together, so a sale already paid is only cleared from the ship.
        /// </summary>
        private static void PaySaleProceeds(PlayerShip ship)
        {
            if (string.IsNullOrWhiteSpace(ship.SaleId))
                return;

            var dbSeller = DB.Get<Player>(ship.SaleSellerPlayerId);
            if (dbSeller != null && !dbSeller.PaidShipSaleIds.Contains(ship.SaleId))
            {
                dbSeller.MarketTill += ship.SaleProceeds;
                dbSeller.PaidShipSaleIds.Add(ship.SaleId);
                DB.Set(dbSeller);
            }

            var saleId = ship.SaleId;
            ship.SaleId = string.Empty;
            ship.SaleSellerPlayerId = string.Empty;
            ship.SaleProceeds = 0;
            DB.Set(ship);

            if (dbSeller != null && dbSeller.PaidShipSaleIds.Remove(saleId))
                DB.Set(dbSeller);
        }

        /// <summary>Finishes any ship transfer or sale a restart interrupted: the transfer, whose steps are safe to repeat, then the payout.</summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void FinishInterruptedTransfers()
        {
            var count = (int)DB.SearchCount(new DBQuery<PlayerShip>());
            if (count <= 0)
                return;

            foreach (var ship in DB.Search(new DBQuery<PlayerShip>().AddPaging(count, 0)))
            {
                if (!string.IsNullOrWhiteSpace(ship.TransferToPlayerId))
                {
                    Complete(ship, ship.TransferToPlayerId, ship.TransferReason);
                    Log.Write(LogGroup.Property, $"Finished the transfer of ship {ship.Id} after a restart.");
                }

                if (!string.IsNullOrWhiteSpace(ship.SaleId))
                {
                    PaySaleProceeds(ship);
                    Log.Write(LogGroup.PlayerMarket, $"Paid owed proceeds for ship {ship.Id} after a restart.");
                }
            }
        }

        /// <summary>The character-file local naming a ship transfer's payment until the transfer is saved (<see cref="ShipTransferPayment"/>).</summary>
        private const string PaymentVariable = "SHIP_TRANSFER_PAYMENT";

        /// <summary>Saves a payment just taken in the payer's character file, with the mark naming the transfer it buys.</summary>
        private static void SavePayment(uint payer, ShipTransferPayment payment)
        {
            ObjectPlugin.SetString(payer, PaymentVariable, payment.Serialize(), true);
            ExportSingleCharacter(payer);
        }

        /// <summary>The transfer is saved: the mark goes and is saved too, so it is never settled again later.</summary>
        private static void ClearPayment(uint payer)
        {
            ObjectPlugin.DeleteString(payer, PaymentVariable);
            ExportSingleCharacter(payer);
        }

        /// <summary>
        /// Settles a ship transfer's payment a restart interrupted. The module load already finished any transfer that
        /// was saved, which keeps the payment; a payment whose transfer was never saved goes back to the payer.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void SettleInterruptedPayment()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            if (!ShipTransferPayment.TryParse(ObjectPlugin.GetString(player, PaymentVariable), out var payment))
                return;

            if (payment.IsApplied(DB.Get<PlayerShip>(payment.ShipId)))
            {
                Log.Write(LogGroup.Property, $"Ship {payment.ShipId} payment {payment.Id} by {PlayerName.GetAuditName(player)} was settled with its transfer.");
            }
            else
            {
                GiveGoldToCreature(player, payment.Credits);
                var credits = payment.Credits;
                DelayCommand(6f, () => SendMessageToPC(player, ColorToken.Red(
                    $"A restart stopped a ship from changing hands after you paid {credits} credits for it. The credits were returned.")));
                Log.Write(LogGroup.Property, $"Returned {payment.Credits} credits of ship {payment.ShipId} payment {payment.Id} to {PlayerName.GetAuditName(player)}: its transfer was never saved.");
            }

            ClearPayment(player);
        }

        /// <summary>The ship deed a ship listing shows when examined, or OBJECT_INVALID. The caller destroys it.</summary>
        public static uint DeserializeListingDeed(MarketItem listing)
        {
            var ship = DB.Get<PlayerShip>(listing?.ShipId);
            return ship == null || string.IsNullOrWhiteSpace(ship.SerializedItem)
                ? OBJECT_INVALID
                : ObjectPlugin.Deserialize(ship.SerializedItem);
        }
    }
}
