using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service.PlayerIntroductionService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the player introductions. The logic and the offer inbox live in
    /// <see cref="IPlayerIntroductionService"/>; the event handler stays here and every member forwards to the
    /// container's instance.
    /// </summary>
    public static class PlayerIntroduction
    {
        public const float IntroductionRange = Domain.Character.PlayerIntroductionService.IntroductionRange;

        private static IPlayerIntroductionService Instance => FacadeAccessor.Resolve<IPlayerIntroductionService>();

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearOnExit()
        {
            Instance.ClearOnExit();
        }

        public static string ValidateIntroduction(uint presenter, string name)
        {
            return Instance.ValidateIntroduction(presenter, name);
        }

        public static void Introduce(uint presenter, string rawName)
        {
            Instance.Introduce(presenter, rawName);
        }

        public static IReadOnlyList<PlayerIntroductionOffer> GetPending(uint observer)
        {
            return Instance.GetPending(observer);
        }

        public static string Accept(uint observer, Guid offerId, string approvedKnownName)
        {
            return Instance.Accept(observer, offerId, approvedKnownName);
        }

        public static void Dismiss(uint observer, Guid offerId)
        {
            Instance.Dismiss(observer, offerId);
        }
    }
}
