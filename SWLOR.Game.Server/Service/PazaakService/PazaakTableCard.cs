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
