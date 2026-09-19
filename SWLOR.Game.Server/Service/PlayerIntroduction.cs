using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PlayerIntroductionService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    public static class PlayerIntroduction
    {
        public const float IntroductionRange = 20f;
        private static readonly TimeSpan OfferLifetime = TimeSpan.FromMinutes(10);
        private const float IntroductionDelaySeconds = 300f;
        private static readonly PlayerIntroductionInbox Inbox = new();

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void ClearOnExit()
        {
            var playerId = GetObjectUUID(GetExitingObject());
            if (string.IsNullOrWhiteSpace(playerId))
                return;

            Inbox.RemovePlayer(playerId);
        }

        public static string ValidateIntroduction(uint presenter, string name)
        {
            if (!GetIsObjectValid(presenter) || !GetIsPC(presenter) || GetIsDM(presenter))
                return "Only player characters can introduce themselves.";

            var error = PlayerName.ValidateKnownNameInput(name);
            if (!string.IsNullOrEmpty(error))
                return error;

            var (isOnCooldown, timeToWait) = Recast.IsOnRecastDelay(presenter, RecastGroup.Introduction);
            if (isOnCooldown)
                return $"You can introduce yourself again in {timeToWait}.";

            return string.Empty;
        }

        public static void Introduce(uint presenter, string rawName)
        {
            var error = ValidateIntroduction(presenter, rawName);
            if (!string.IsNullOrEmpty(error))
            {
                SendMessageToPC(presenter, ColorToken.Red(error));
                return;
            }

            var now = DateTime.UtcNow;
            var presenterId = GetObjectUUID(presenter);
            var identityKey = Disguise.GetIdentityKey(presenter);
            var name = PlayerName.SanitizeKnownName(rawName);
            Recast.ApplyRecastDelay(presenter, RecastGroup.Introduction, IntroductionDelaySeconds);

            for (var observer = GetFirstPC(); GetIsObjectValid(observer); observer = GetNextPC())
            {
                if (observer == presenter || !GetIsPC(observer) || GetIsDM(observer) ||
                    GetArea(observer) != GetArea(presenter) ||
                    GetDistanceBetween(presenter, observer) > IntroductionRange ||
                    !GetObjectSeen(presenter, observer))
                    continue;

                var displayName = PlayerName.GetDisplayName(observer, presenter);
                var offer = new PlayerIntroductionOffer(Guid.NewGuid(), presenter, presenterId, identityKey,
                    UtilPlugin.StripColors(displayName), name, now + OfferLifetime);
                if (Inbox.Add(GetObjectUUID(observer), offer, now))
                    SendMessageToPC(observer,
                        $"{PlayerName.GetColoredDisplayName(observer, presenter)} introduces themselves as '{name}'. " +
                        "Use /introductions to remember or dismiss this name. Your private label stays unchanged until you accept.");
            }

            SendMessageToPC(presenter, ColorToken.Green(
                $"You introduced yourself as '{name}' to nearby players. They choose whether to remember this name."));
        }

        public static IReadOnlyList<PlayerIntroductionOffer> GetPending(uint observer)
        {
            var observerId = GetObjectUUID(observer);
            var offers = Inbox.GetPending(observerId, DateTime.UtcNow);
            foreach (var offer in offers.Where(offer => !IsCurrentIdentity(offer)))
                Inbox.Dismiss(observerId, offer.Id);

            return Inbox.GetPending(observerId, DateTime.UtcNow);
        }

        public static string Accept(uint observer, Guid offerId, string approvedKnownName)
        {
            return Inbox.Accept(GetObjectUUID(observer), offerId, DateTime.UtcNow, offer =>
            {
                if (!IsCurrentIdentity(offer))
                    return "This introduction is no longer available.";

                PlayerName.TryGetKnownName(observer, offer.Presenter, out var currentKnownName);
                var error = offer.ValidateIdentityAndConsent(GetObjectUUID(offer.Presenter),
                    Disguise.GetIdentityKey(offer.Presenter), currentKnownName, approvedKnownName);
                return string.IsNullOrEmpty(error)
                    ? PlayerName.ValidateKnownNameAssignment(observer, offer.Presenter, offer.Name)
                    : error;
            }, offer =>
            {
                PlayerName.SetKnownName(observer, offer.Presenter, offer.Name);
                Log.WriteStructured(LogGroup.PlayerName,
                    "Player identity name change: Action={Action} ObserverPlayerId={ObserverPlayerId} TargetPlayerId={TargetPlayerId} IdentityKey={IdentityKey} Name={Name}",
                    "introduction-accepted", GetObjectUUID(observer), offer.PresenterPlayerId, offer.IdentityKey, offer.Name);
            });
        }

        public static void Dismiss(uint observer, Guid offerId)
        {
            Inbox.Dismiss(GetObjectUUID(observer), offerId);
        }

        private static bool IsCurrentIdentity(PlayerIntroductionOffer offer)
        {
            return GetIsObjectValid(offer.Presenter) && GetIsPC(offer.Presenter) && !GetIsDM(offer.Presenter) &&
                   GetObjectUUID(offer.Presenter) == offer.PresenterPlayerId &&
                   Disguise.GetIdentityKey(offer.Presenter) == offer.IdentityKey;
        }
    }
}
