namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The facts a ship transfer or market sale is checked against (<see cref="ShipTransferRules.GetRefusal"/>), read
    /// from the ship's records and the live game when the transfer is offered and again when it completes.
    /// </summary>
    public class ShipTransferCheck
    {
        /// <summary>The recipient or buyer already owns the ship.</summary>
        public bool RecipientIsOwner { get; set; }

        /// <summary>The ship is in space: flown, or left there by a pilot who logged out.</summary>
        public bool InSpace { get; set; }

        /// <summary>The ship is docked aboard another ship or at a derelict instead of at a starport.</summary>
        public bool DockedAway { get; set; }

        /// <summary>Players standing in the ship's interior.</summary>
        public int PlayersAboard { get; set; }

        /// <summary>The ship is listed on the player market.</summary>
        public bool ListedForSale { get; set; }

        /// <summary>Commodity lots in the cargo bay: commodities never change hands outside an exchange.</summary>
        public int CargoLots { get; set; }

        /// <summary>Freight contracts' sealed loads aboard: they belong to their contract.</summary>
        public int SealedFreightLots { get; set; }

        /// <summary>An item aboard (stored or fitted) that can't normally be traded.</summary>
        public bool HasUntradeableItem { get; set; }

        /// <summary>The ships the recipient or buyer has registered. 0 while listing, before there is a buyer.</summary>
        public int RecipientShipCount { get; set; }
    }
}
