namespace SWLOR.Game.Server.Service.PazaakService
{
    public class PazaakCardDetail
    {
        public PazaakCardDetail(
            PazaakCardType type,
            PazaakCardKind kind,
            int value,
            string label,
            string name,
            string description,
            string resref,
            int model)
        {
            Type = type;
            Kind = kind;
            Value = value;
            Label = label;
            Name = name;
            Description = description;
            Resref = resref;
            Model = model;
        }

        public PazaakCardType Type { get; }
        public PazaakCardKind Kind { get; }

        /// <summary>
        /// Face value of plus, minus, and plus/minus cards. Zero for special cards.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Short text shown on the card in the Pazaak window.
        /// </summary>
        public string Label { get; }

        public string Name { get; }
        public string Description { get; }

        /// <summary>
        /// Blueprint of the card item that adds this card to a player's collection.
        /// </summary>
        public string Resref { get; }

        /// <summary>
        /// Appearance of the card item on the card base item (inventory icon iit_card_NNN).
        /// </summary>
        public int Model { get; }

        /// <summary>
        /// True when the player picks a sign as the card is played.
        /// </summary>
        public bool HasSignChoice => Kind is PazaakCardKind.PlusMinus or PazaakCardKind.Tiebreaker;
    }
}
