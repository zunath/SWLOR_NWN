namespace SWLOR.Game.Server.Service.PazaakService
{
    public class PazaakCardDetail
    {
        public PazaakCardDetail(PazaakCardType type, PazaakCardKind kind, int value, string label, string description)
        {
            Type = type;
            Kind = kind;
            Value = value;
            Label = label;
            Description = description;
        }

        public PazaakCardType Type { get; }
        public PazaakCardKind Kind { get; }

        /// <summary>
        /// Face value of plus, minus, and plus/minus cards. Zero for special cards.
        /// </summary>
        public int Value { get; }

        public string Label { get; }
        public string Description { get; }

        /// <summary>
        /// True when the player picks a sign as the card is played.
        /// </summary>
        public bool HasSignChoice => Kind is PazaakCardKind.PlusMinus or PazaakCardKind.Tiebreaker;
    }
}
