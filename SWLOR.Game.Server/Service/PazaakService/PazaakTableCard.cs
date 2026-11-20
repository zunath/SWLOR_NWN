namespace SWLOR.Game.Server.Service.PazaakService
{
    public class PazaakTableCard
    {
        public PazaakTableCard(int value, PazaakCardType sideCard = PazaakCardType.Invalid)
        {
            Value = value;
            SideCard = sideCard;
        }

        public int Value { get; set; }

        /// <summary>
        /// The side card this was played from, or Invalid for a main deck card.
        /// </summary>
        public PazaakCardType SideCard { get; }

        public bool IsSideCard => SideCard != PazaakCardType.Invalid;

        /// <summary>
        /// The card art shown on the table. A main card flipped negative shows the minus card of its value,
        /// and a played plus/minus card shows the plus or minus card of the sign it was played with.
        /// </summary>
        public string Image
        {
            get
            {
                if (!IsSideCard)
                    return Value > 0 ? PazaakCards.GetMainCardImage(Value) : PazaakCards.GetSignedCardImage(Value);

                return PazaakCards.Get(SideCard).Kind switch
                {
                    PazaakCardKind.Plus or PazaakCardKind.Minus or PazaakCardKind.PlusMinus => PazaakCards.GetSignedCardImage(Value),
                    _ => PazaakCards.GetImage(SideCard)
                };
            }
        }

        public string Label
        {
            get
            {
                if (!IsSideCard)
                    return Value.ToString();

                return PazaakCards.Get(SideCard).Kind switch
                {
                    PazaakCardKind.FlipTwoFour or PazaakCardKind.FlipThreeSix => PazaakCards.Get(SideCard).Label,
                    PazaakCardKind.Double => Value >= 0 ? $"D+{Value}" : $"D{Value}",
                    PazaakCardKind.Tiebreaker => Value >= 0 ? $"+{Value}T" : $"{Value}T",
                    _ => Value >= 0 ? $"+{Value}" : Value.ToString()
                };
            }
        }
    }
}
