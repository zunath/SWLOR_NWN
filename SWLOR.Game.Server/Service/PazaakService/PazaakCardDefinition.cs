using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.PazaakService
{
    public class PazaakCardDefinition
    {
        public PazaakCardType Type { get; }
        public string Name { get; }
        public string ShortName { get; }
        public PazaakCardRule Rule { get; }
        public IReadOnlyList<int> PlayableValues { get; }
        public IReadOnlyList<int> FlipValues { get; }
        public bool IsGoldCard { get; }
        public string Rarity { get; }
        public int VendorPrice { get; }
        public string IconResref { get; }

        public PazaakCardDefinition(
            PazaakCardType type,
            string name,
            string shortName,
            PazaakCardRule rule,
            IEnumerable<int> playableValues,
            IEnumerable<int> flipValues,
            bool isGoldCard,
            string rarity,
            int vendorPrice,
            string iconResref)
        {
            Type = type;
            Name = name;
            ShortName = shortName;
            Rule = rule;
            PlayableValues = playableValues.ToList();
            FlipValues = flipValues.ToList();
            IsGoldCard = isGoldCard;
            Rarity = rarity;
            VendorPrice = vendorPrice;
            IconResref = iconResref;
        }
    }
}
