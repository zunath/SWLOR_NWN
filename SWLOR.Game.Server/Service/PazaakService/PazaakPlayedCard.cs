namespace SWLOR.Game.Server.Service.PazaakService
{
    public class PazaakPlayedCard
    {
        public PazaakCardType CardType { get; set; }
        public string Label { get; set; }
        public int Value { get; set; }
        public bool IsMainDeckCard { get; set; }

        public PazaakPlayedCard()
        {
            Label = string.Empty;
        }

        public PazaakPlayedCard(PazaakCardType cardType, string label, int value, bool isMainDeckCard)
        {
            CardType = cardType;
            Label = label;
            Value = value;
            IsMainDeckCard = isMainDeckCard;
        }
    }
}
