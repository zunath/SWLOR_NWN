using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Introductions between players: the logic and state that used to be static in <c>Service.PlayerIntroduction</c>,
    /// moved here unchanged. The event handler runs on the container's instance.
    /// </summary>
    public class PlayerIntroductionService : IPlayerIntroductionService
    {
        public const float IntroductionRange = 20f;
        private readonly TimeSpan _offerLifetime = TimeSpan.FromMinutes(10);
        private const float IntroductionDelaySeconds = 120f;
        private readonly PlayerIntroductionInbox _inbox = new();
        private readonly IPlayerNameService _playerName;
        private readonly IDisguiseService _disguise;
        private readonly IRecastService _recast;

        public PlayerIntroductionService(IPlayerNameService playerName, IDisguiseService disguise, IRecastService recast)
        {
            _playerName = playerName;
            _disguise = disguise;
            _recast = recast;
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void ClearOnExit()
        {
            var playerId = GetObjectUUID(GetExitingObject());
            if (string.IsNullOrWhiteSpace(playerId))
                return;

            _inbox.RemovePlayer(playerId);
        }

        public string ValidateIntroduction(uint presenter, string name)
        {
            if (!GetIsObjectValid(presenter) || !GetIsPC(presenter) || GetIsDM(presenter))
                return "Only player characters can introduce themselves.";

            var error = _playerName.ValidateKnownNameInput(name);
            if (!string.IsNullOrEmpty(error))
                return error;

            var (isOnCooldown, timeToWait) = IsOnIntroductionRecastDelay(presenter);
            if (isOnCooldown)
                return $"You can introduce yourself again in {timeToWait}.";

            return string.Empty;
        }

        public void Introduce(uint presenter, string rawName)
        {
            var error = ValidateIntroduction(presenter, rawName);
            if (!string.IsNullOrEmpty(error))
            {
                SendMessageToPC(presenter, ColorToken.Red(error));
                return;
            }

            var now = UtcNow();
            var presenterId = GetObjectUUID(presenter);
            var identityKey = GetDisguiseIdentityKey(presenter);
            var name = _playerName.SanitizeKnownName(rawName);
            ApplyIntroductionRecastDelay(presenter, IntroductionDelaySeconds);

            for (var observer = GetFirstPC(); GetIsObjectValid(observer); observer = GetNextPC())
            {
                if (observer == presenter || !GetIsPC(observer) || GetIsDM(observer) ||
                    GetArea(observer) != GetArea(presenter) ||
                    GetDistanceBetween(presenter, observer) > IntroductionRange ||
                    !GetObjectSeen(presenter, observer))
                    continue;

                var displayName = _playerName.GetDisplayName(observer, presenter);
                var offer = new PlayerIntroductionOffer(Guid.NewGuid(), presenter, presenterId, identityKey,
                    UtilPlugin.StripColors(displayName), name, now + _offerLifetime);
                if (_inbox.Add(GetObjectUUID(observer), offer, now))
                    SendMessageToPC(observer,
                        $"{_playerName.GetColoredDisplayName(observer, presenter)} introduces themselves as '{name}'. " +
                        "Use /introductions to remember or dismiss this name. Your private label stays unchanged until you accept.");
            }

            SendMessageToPC(presenter, ColorToken.Green(
                $"You introduced yourself as '{name}' to nearby players. They choose whether to remember this name."));
        }

        public IReadOnlyList<PlayerIntroductionOffer> GetPending(uint observer)
        {
            var observerId = GetObjectUUID(observer);
            var offers = _inbox.GetPending(observerId, UtcNow());
            foreach (var offer in offers.Where(offer => !IsCurrentIdentity(offer)))
                _inbox.Dismiss(observerId, offer.Id);

            return _inbox.GetPending(observerId, UtcNow());
        }

        public string Accept(uint observer, Guid offerId, string approvedKnownName)
        {
            return _inbox.Accept(GetObjectUUID(observer), offerId, UtcNow(), offer =>
            {
                if (!IsCurrentIdentity(offer))
                    return "This introduction is no longer available.";

                _playerName.TryGetKnownName(observer, offer.Presenter, out var currentKnownName);
                var error = offer.ValidateIdentityAndConsent(GetObjectUUID(offer.Presenter),
                    GetDisguiseIdentityKey(offer.Presenter), currentKnownName, approvedKnownName);
                return string.IsNullOrEmpty(error)
                    ? _playerName.ValidateKnownNameAssignment(observer, offer.Presenter, offer.Name)
                    : error;
            }, offer =>
            {
                var previousName = _playerName.TryGetKnownName(observer, offer.Presenter, out var knownName)
                    ? knownName
                    : string.Empty;
                _playerName.SetKnownName(observer, offer.Presenter, offer.Name);
                WriteStructuredLog(LogGroup.PrivateNames,
                    "Player identity name change: Action={Action} ObserverPlayerId={ObserverPlayerId} ObserverName={ObserverName} TargetPlayerId={TargetPlayerId} TargetName={TargetName} IdentityKey={IdentityKey} PreviousName={PreviousName} Name={Name}",
                    "introduction-accepted", GetObjectUUID(observer), _playerName.GetAuditName(observer),
                    offer.PresenterPlayerId, _playerName.GetAuditName(offer.Presenter), offer.IdentityKey,
                    previousName, offer.Name);
            });
        }

        public void Dismiss(uint observer, Guid offerId)
        {
            _inbox.Dismiss(GetObjectUUID(observer), offerId);
        }

        private bool IsCurrentIdentity(PlayerIntroductionOffer offer)
        {
            return GetIsObjectValid(offer.Presenter) && GetIsPC(offer.Presenter) && !GetIsDM(offer.Presenter) &&
                   GetObjectUUID(offer.Presenter) == offer.PresenterPlayerId &&
                   GetDisguiseIdentityKey(offer.Presenter) == offer.IdentityKey;
        }

        /// <summary>The current time: <c>DateTime.UtcNow</c>.</summary>
        protected virtual DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

        /// <summary>The identity key of the player's active identity: <c>Disguise.GetIdentityKey</c>.</summary>
        protected virtual string GetDisguiseIdentityKey(uint player)
        {
            return _disguise.GetIdentityKey(player);
        }

        /// <summary>The introduction recast check: <c>Recast.IsOnRecastDelay</c> for <c>RecastGroup.Introduction</c>.</summary>
        protected virtual (bool, string) IsOnIntroductionRecastDelay(uint presenter)
        {
            return _recast.IsOnRecastDelay(presenter, RecastGroup.Introduction);
        }

        /// <summary>Starts the introduction recast delay: <c>Recast.ApplyRecastDelay</c> for <c>RecastGroup.Introduction</c>.</summary>
        protected virtual void ApplyIntroductionRecastDelay(uint presenter, float delaySeconds)
        {
            _recast.ApplyRecastDelay(presenter, RecastGroup.Introduction, delaySeconds);
        }

        /// <summary>The structured log line: <c>Log.WriteStructured</c> (<c>Log</c> stays static in Phase 2).</summary>
        protected virtual void WriteStructuredLog(LogGroup group, string messageTemplate, params object[] propertyValues)
        {
            Log.WriteStructured(group, messageTemplate, propertyValues);
        }
    }
}
