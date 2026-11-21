using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.ShipRewardDefinition;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Ship transfers and market sales (plan section 7): the same ship changes owner, with its interior, furniture,
    /// stored items and fitted modules. These rules hold no game state, so tests can drive them; <c>ShipTransfer</c>
    /// reads the facts and applies the result.
    /// </summary>
    public static class ShipTransferRules
    {
        /// <summary>How long a direct transfer offer waits for the recipient's answer.</summary>
        public const int OfferSeconds = 120;

        /// <summary>
        /// The first reason the ship cannot change owner, or <see cref="ShipTransferRefusal.None"/>. Checked when a
        /// transfer is offered or a sale listed, and again when it completes. A sale skips the listing check: the
        /// listing is what is being bought.
        /// </summary>
        public static ShipTransferRefusal GetRefusal(ShipTransferCheck check, bool isSale)
        {
            if (check == null)
                return ShipTransferRefusal.MissingShip;
            if (check.RecipientIsOwner)
                return ShipTransferRefusal.SameOwner;
            if (check.InSpace)
                return ShipTransferRefusal.InSpace;
            if (check.DockedAway)
                return ShipTransferRefusal.DockedAway;
            if (check.PlayersAboard > 0)
                return ShipTransferRefusal.PlayersAboard;
            if (!isSale && check.ListedForSale)
                return ShipTransferRefusal.ListedForSale;
            if (check.CargoLots > 0)
                return ShipTransferRefusal.CargoAboard;
            if (check.SealedFreightLots > 0)
                return ShipTransferRefusal.SealedFreight;
            if (check.HasUntradeableItem)
                return ShipTransferRefusal.UntradeableItem;
            if (check.RecipientShipCount >= Space.MaxRegisteredShips)
                return ShipTransferRefusal.RecipientAtLimit;

            return ShipTransferRefusal.None;
        }

        /// <summary>The player-facing reason for a refusal, or an empty string for none.</summary>
        public static string GetRefusalMessage(ShipTransferRefusal refusal)
        {
            return refusal switch
            {
                ShipTransferRefusal.None => string.Empty,
                ShipTransferRefusal.MissingShip => "That ship no longer exists.",
                ShipTransferRefusal.SameOwner => "That player already owns this ship.",
                ShipTransferRefusal.InSpace => "The ship must be docked at a starport.",
                ShipTransferRefusal.DockedAway => "The ship must be docked at a starport, not aboard another ship or at a derelict.",
                ShipTransferRefusal.PlayersAboard => "Everyone must leave the ship first.",
                ShipTransferRefusal.ListedForSale => "The ship is listed on the market. Cancel the listing first.",
                ShipTransferRefusal.CargoAboard => "Sell the cargo in the cargo bay first.",
                ShipTransferRefusal.SealedFreight => "Deliver or abandon the sealed freight aboard first.",
                ShipTransferRefusal.UntradeableItem => "An item aboard cannot be traded. Remove it first.",
                ShipTransferRefusal.RecipientAtLimit => $"The new owner already has {Space.MaxRegisteredShips} ships registered.",
                _ => "The ship cannot change owner right now.",
            };
        }

        /// <summary>The credits a direct transfer costs its giver, by hull size. A market sale pays the market tax instead.</summary>
        public static int GetTransferFee(ShipSize size)
        {
            return RevampRewardCatalog.TransferFee.TryGetValue(size == ShipSize.Invalid ? ShipSize.Small : size, out var fee)
                ? fee
                : 0;
        }

        /// <summary>
        /// The ship's property and every property it holds: its structures (furniture) and their interiors, all the
        /// way down. A starport the ship is registered at is not the ship's, so it is never included.
        /// </summary>
        public static List<WorldProperty> CollectOwnedProperties(string rootPropertyId, Func<string, WorldProperty> lookup)
        {
            var result = new List<WorldProperty>();
            var seen = new HashSet<string>();
            var pending = new Stack<string>();
            pending.Push(rootPropertyId);

            while (pending.Count > 0)
            {
                var id = pending.Pop();
                if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                    continue;

                var property = lookup(id);
                if (property == null)
                    continue;

                result.Add(property);
                foreach (var childType in new[] { PropertyChildType.Structure, PropertyChildType.Interior })
                {
                    if (!property.ChildPropertyIds.TryGetValue(childType, out var children) || children == null)
                        continue;

                    foreach (var child in children)
                        pending.Push(child);
                }
            }

            return result;
        }

        /// <summary>
        /// Re-owns the ship record and every property it holds. Structures copy their owner's id when placed, so each
        /// one is updated. The ship keeps its look (its Hull Design appearance), name, layout, furniture positions,
        /// stored items and fitted modules; per-player hotbars go with the old crew's access.
        /// </summary>
        public static void Reown(PlayerShip ship, IEnumerable<WorldProperty> properties, string newOwnerPlayerId)
        {
            ship.OwnerPlayerId = newOwnerPlayerId;
            ship.PlayerHotBars.Clear();
            foreach (var property in properties)
                property.OwnerPlayerId = newOwnerPlayerId;
        }

        /// <summary>A permission record with every listed permission and the right to grant it.</summary>
        public static WorldPropertyPermission BuildFullPermission(string propertyId, string playerId, IEnumerable<PropertyPermissionType> permissions)
        {
            var record = new WorldPropertyPermission
            {
                PropertyId = propertyId,
                PlayerId = playerId
            };

            foreach (var permission in permissions)
            {
                record.Permissions[permission] = true;
                record.GrantPermissions[permission] = true;
            }

            return record;
        }

        /// <summary>The summary the recipient or buyer reads before agreeing.</summary>
        public static string BuildSummary(string shipName, string hullName, string designName, int moduleCount,
            int furnitureCount, int storedItemCount, string dockName)
        {
            return $"Ship: {shipName}\n" +
                   $"Hull: {hullName} ({designName} design)\n" +
                   $"Modules fitted: {moduleCount}\n" +
                   $"Furniture: {furnitureCount}\n" +
                   $"Stored items: {storedItemCount}\n" +
                   $"Docked at: {dockName}\n\n" +
                   "The design stays on the ship, but only designs you have unlocked can be refitted. " +
                   "You can own a ship above your Piloting rank, but cannot fly it until you reach that rank.";
        }

        /// <summary>The market listing's name: the ship's name and its hull.</summary>
        public static string BuildListingName(string shipName, string hullName)
        {
            return string.IsNullOrWhiteSpace(hullName) || hullName == shipName ? shipName : $"{shipName} ({hullName})";
        }

        /// <summary>Whether an asking price can be listed: a whole number of credits above zero.</summary>
        public static bool TryParsePrice(string text, out int price)
        {
            price = 0;
            var digits = new string((text ?? string.Empty).Where(c => c != ',' && !char.IsWhiteSpace(c)).ToArray());
            return int.TryParse(digits, out price) && price > 0;
        }
    }
}
