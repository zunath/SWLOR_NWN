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
        /// gets full rights. The caller has already checked <see cref="ShipTransferRules.GetRefusal"/>.
        /// </summary>
        public static void Complete(PlayerShip ship, string newOwnerPlayerId, string reason)
        {
            var oldOwnerPlayerId = ship.OwnerPlayerId;
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

            DB.Set(ship);

            var dbOldOwner = DB.Get<Player>(oldOwnerPlayerId);
            if (dbOldOwner != null && dbOldOwner.ActiveShipId == ship.Id)
            {
                dbOldOwner.ActiveShipId = Guid.Empty.ToString();
                DB.Set(dbOldOwner);
            }

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

        /// <summary>The recipient accepted: everything is checked again, the giver pays the fee and the ship changes owner.</summary>
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
            var ship = DB.Get<PlayerShip>(offer.ShipId);
            if (!GetIsObjectValid(giver) || ship == null || ship.OwnerPlayerId != offer.GiverPlayerId)
            {
                FloatingTextStringOnCreature("The offer is no longer valid.", recipient, false);
                return;
            }

            var dockArea = GetDockArea(ship);
            if (!GetIsObjectValid(dockArea) || GetArea(giver) != dockArea || GetArea(recipient) != dockArea)
            {
                FloatingTextStringOnCreature("You and the giver must both be at the starport where the ship is docked.", recipient, false);
                return;
            }

            var refusal = ShipTransferRules.GetRefusal(BuildCheck(ship, offer.RecipientPlayerId), false);
            if (refusal != ShipTransferRefusal.None)
            {
                var message = ShipTransferRules.GetRefusalMessage(refusal);
                FloatingTextStringOnCreature(message, recipient, false);
                FloatingTextStringOnCreature(message, giver, false);
                return;
            }

            if (GetGold(giver) < offer.Fee)
            {
                FloatingTextStringOnCreature($"The giver cannot pay the {offer.Fee} credit transfer fee.", recipient, false);
                FloatingTextStringOnCreature($"A ship transfer costs {offer.Fee} credits.", giver, false);
                return;
            }

            var shipName = GetShipName(ship);
            AssignCommand(giver, () => TakeGoldFromCreature(offer.Fee, giver, true));
            Complete(ship, offer.RecipientPlayerId, "changed owner");

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
            return GetListing(shipId)?.IsListed == true;
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
        /// checked again; if it can't change owner now the listing stays and nothing is paid. Otherwise the buyer
        /// pays, the seller's proceeds minus the market tax go to their till, and the ship changes owner where it is
        /// docked. Returns whether it sold.
        /// </summary>
        public static bool Buy(uint buyer, MarketItem listing, float taxRate)
        {
            var ship = DB.Get<PlayerShip>(listing.ShipId);
            if (ship == null || ship.OwnerPlayerId != listing.PlayerId)
            {
                DB.Delete<MarketItem>(listing.Id);
                FloatingTextStringOnCreature("This ship is no longer available.", buyer, false);
                return false;
            }

            // The seller can take the listing off the market while the buyer's confirmation is open.
            if (!listing.IsListed)
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

            var shipName = GetShipName(ship);
            var dockName = GetDockName(ship);
            AssignCommand(buyer, () => TakeGoldFromCreature(listing.Price, buyer, true));

            // The sale is saved before anything changes hands, and the proceeds owed are saved with the new owner, then
            // paid: a restart part-way through finishes the transfer and the payout at the next module load.
            ship.SaleId = Guid.NewGuid().ToString();
            ship.SaleSellerPlayerId = listing.PlayerId;
            ship.SaleBuyerPlayerId = buyerId;
            ship.SaleProceeds = (int)(listing.Price - listing.Price * taxRate);
            DB.Set(ship);
            Complete(ship, buyerId, "was sold");
            PaySaleProceeds(ship);

            var seller = DMPlayerDirectory.GetOnlinePlayer(listing.PlayerId);
            if (GetIsObjectValid(seller))
                SendMessageToPC(seller, $"{shipName} sold on the market for {listing.Price} credits. The proceeds are in your market till.");

            FloatingTextStringOnCreature(ColorToken.Green($"{shipName} is now yours! It is docked at {dockName}."), buyer, false);
            Log.Write(LogGroup.PlayerMarket, $"{GetName(buyer)} [{buyerId}] bought ship {ship.Id} from {listing.SellerName} for {listing.Price} credits.");
            return true;
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
            ship.SaleBuyerPlayerId = string.Empty;
            ship.SaleProceeds = 0;
            DB.Set(ship);

            if (dbSeller != null && dbSeller.PaidShipSaleIds.Remove(saleId))
                DB.Set(dbSeller);
        }

        /// <summary>Finishes any ship sale a restart interrupted: the transfer, whose steps are safe to repeat, then the payout.</summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void PayOwedSaleProceeds()
        {
            var count = (int)DB.SearchCount(new DBQuery<PlayerShip>());
            if (count <= 0)
                return;

            foreach (var ship in DB.Search(new DBQuery<PlayerShip>().AddPaging(count, 0)))
            {
                if (string.IsNullOrWhiteSpace(ship.SaleId))
                    continue;

                if (!string.IsNullOrWhiteSpace(ship.SaleBuyerPlayerId))
                    Complete(ship, ship.SaleBuyerPlayerId, "was sold");
                PaySaleProceeds(ship);
                Log.Write(LogGroup.PlayerMarket, $"Paid owed proceeds for ship {ship.Id} after a restart.");
            }
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
