namespace SWLOR.Game.Server.Service.CivicExchangeService
{
    /// <summary>One thing the Civic Exchange sells. The list lives in <see cref="CivicExchangeCatalog"/>.</summary>
    public sealed class CivicExchangeItem
    {
        public CivicExchangeItem(
            string key,
            CivicExchangeItemKind kind,
            string name,
            string description,
            string icon,
            int priceXp,
            int minCantinaLevel,
            int tier)
        {
            Key = key;
            Kind = kind;
            Name = name;
            Description = description;
            Icon = icon;
            PriceXp = priceXp;
            MinCantinaLevel = minCantinaLevel;
            Tier = tier;
        }

        /// <summary>The stable key the window and the log use for the item.</summary>
        public string Key { get; }

        public CivicExchangeItemKind Kind { get; }

        public string Name { get; }

        /// <summary>What the item does, shown as the row's tooltip.</summary>
        public string Description { get; }

        /// <summary>The icon resref the window shows.</summary>
        public string Icon { get; }

        /// <summary>The price in Available XP.</summary>
        public int PriceXp { get; }

        /// <summary>The Cantina level from which the item is stocked.</summary>
        public int MinCantinaLevel { get; }

        /// <summary>The material tier of a crate (1 to 5), or 0 for anything else.</summary>
        public int Tier { get; }
    }
}
