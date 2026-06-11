using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.PazaakService
{
    public static class PazaakNpcProfileCatalog
    {
        public const string DefaultProfileId = "cantina_regular";

        private static readonly Dictionary<string, PazaakNpcProfile> _profiles = BuildProfiles();

        public static IReadOnlyDictionary<string, PazaakNpcProfile> All => _profiles;

        public static PazaakNpcProfile Get(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId) || !_profiles.ContainsKey(profileId))
                return _profiles[DefaultProfileId];

            return _profiles[profileId];
        }

        public static IEnumerable<PazaakNpcProfile> GetAll()
        {
            return _profiles.Values.OrderBy(x => x.MinimumWager).ThenBy(x => x.Name);
        }

        private static Dictionary<string, PazaakNpcProfile> BuildProfiles()
        {
            var profiles = new Dictionary<string, PazaakNpcProfile>();

            void Add(
                string id,
                string name,
                PazaakNpcDifficulty difficulty,
                int minWager,
                int maxWager,
                List<PazaakCardType> sideDeck,
                int rewardWinCount,
                PazaakCardType rewardCard)
            {
                var validation = PazaakGameEngine.ValidateSideDeck(sideDeck);
                if (!string.IsNullOrWhiteSpace(validation))
                    throw new InvalidOperationException($"Pazaak NPC profile '{id}' has an invalid side deck: {validation}");

                if (rewardWinCount > 0 && !PazaakCardCatalog.IsValidCard(rewardCard))
                    throw new InvalidOperationException($"Pazaak NPC profile '{id}' has an invalid reward card: {rewardCard}");

                profiles[id] = new PazaakNpcProfile
                {
                    Id = id,
                    Name = name,
                    Difficulty = difficulty,
                    MinimumWager = minWager,
                    MaximumWager = maxWager,
                    SideDeck = sideDeck,
                    RewardWinCount = rewardWinCount,
                    RewardCard = rewardCard,
                    RewardCardCount = 1,
                };
            }

            Add("cantina_regular", "Cantina Regular", PazaakNpcDifficulty.Novice, 0, 250, new List<PazaakCardType>
            {
                PazaakCardType.Plus1, PazaakCardType.Plus2, PazaakCardType.Plus3, PazaakCardType.Plus4, PazaakCardType.Plus5,
                PazaakCardType.Minus1, PazaakCardType.Minus2, PazaakCardType.Minus3, PazaakCardType.Minus4, PazaakCardType.Minus5,
            }, 3, PazaakCardType.PlusMinus1);

            Add("outer_rim_sharp", "Outer Rim Sharp", PazaakNpcDifficulty.Skilled, 100, 1000, new List<PazaakCardType>
            {
                PazaakCardType.Plus2, PazaakCardType.Plus3, PazaakCardType.Plus4, PazaakCardType.Plus5, PazaakCardType.PlusMinus1,
                PazaakCardType.PlusMinus2, PazaakCardType.Minus2, PazaakCardType.Minus3, PazaakCardType.Minus4, PazaakCardType.Minus6,
            }, 4, PazaakCardType.PlusMinus3);

            Add("sector_hustler", "Sector Hustler", PazaakNpcDifficulty.Expert, 500, 3500, new List<PazaakCardType>
            {
                PazaakCardType.PlusMinus1, PazaakCardType.PlusMinus2, PazaakCardType.PlusMinus3, PazaakCardType.PlusMinus4,
                PazaakCardType.Minus3, PazaakCardType.Minus4, PazaakCardType.Minus5, PazaakCardType.Minus6,
                PazaakCardType.Flip2And4, PazaakCardType.OneOrMinusTwo,
            }, 5, PazaakCardType.Flip2And4);

            Add("champion", "Pazaak Champion", PazaakNpcDifficulty.Master, 1000, 10000, new List<PazaakCardType>
            {
                PazaakCardType.PlusMinus2, PazaakCardType.PlusMinus3, PazaakCardType.PlusMinus4, PazaakCardType.PlusMinus5,
                PazaakCardType.PlusMinus6, PazaakCardType.Minus5, PazaakCardType.Minus6,
                PazaakCardType.Double, PazaakCardType.TieBreaker, PazaakCardType.Flip3And6,
            }, 6, PazaakCardType.TieBreaker);

            return profiles;
        }
    }
}
