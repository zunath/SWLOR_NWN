using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.PlayerIntroductionService
{
    public class PlayerIntroductionInbox
    {
        public const int MaximumPendingOffers = 50;
        private readonly Dictionary<string, List<PlayerIntroductionOffer>> _offers = new();

        public IReadOnlyList<PlayerIntroductionOffer> GetPending(string observerId, DateTime now)
        {
            if (!_offers.TryGetValue(observerId, out var offers))
                return Array.Empty<PlayerIntroductionOffer>();

            offers.RemoveAll(offer => offer.ExpiresAt <= now);
            if (offers.Count == 0)
                _offers.Remove(observerId);

            return offers.ToArray();
        }

        public bool Add(string observerId, PlayerIntroductionOffer offer, DateTime now)
        {
            var offers = GetPending(observerId, now).ToList();
            if (offers.Any(existing => existing.PresenterPlayerId == offer.PresenterPlayerId &&
                                       existing.IdentityKey == offer.IdentityKey && existing.Name == offer.Name))
                return false;

            // A changed introduction gets a new ID, invalidating buttons in an older view.
            offers.RemoveAll(existing => existing.PresenterPlayerId == offer.PresenterPlayerId);
            offers.Add(offer);
            if (offers.Count > MaximumPendingOffers)
                offers.RemoveAt(0);

            _offers[observerId] = offers;
            return true;
        }

        public string Accept(string observerId, Guid offerId, DateTime now,
            Func<PlayerIntroductionOffer, string> validate, Action<PlayerIntroductionOffer> remember)
        {
            var offer = GetPending(observerId, now).FirstOrDefault(entry => entry.Id == offerId);
            if (offer == null)
                return "This introduction is no longer available.";

            var error = validate(offer);
            if (!string.IsNullOrEmpty(error))
                return error;

            remember(offer);
            Dismiss(observerId, offerId);
            return string.Empty;
        }

        public void Dismiss(string observerId, Guid offerId)
        {
            if (!_offers.TryGetValue(observerId, out var offers))
                return;

            offers.RemoveAll(offer => offer.Id == offerId);
            if (offers.Count == 0)
                _offers.Remove(observerId);
        }

        public void RemovePlayer(string playerId)
        {
            _offers.Remove(playerId);
            foreach (var observerId in _offers.Keys.ToArray())
            {
                _offers[observerId].RemoveAll(offer => offer.PresenterPlayerId == playerId);
                if (_offers[observerId].Count == 0)
                    _offers.Remove(observerId);
            }
        }
    }
}
