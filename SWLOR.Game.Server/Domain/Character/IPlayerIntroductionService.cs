using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Character
{
    public interface IPlayerIntroductionService
    {
        void ClearOnExit();

        string ValidateIntroduction(uint presenter, string name);

        void Introduce(uint presenter, string rawName);

        IReadOnlyList<PlayerIntroductionOffer> GetPending(uint observer);

        string Accept(uint observer, Guid offerId, string approvedKnownName);

        void Dismiss(uint observer, Guid offerId);
    }
}
