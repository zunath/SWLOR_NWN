using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.ChatPortraitTellService
{
    /// <summary>
    /// The NWN client stores the speaker's connection slot (player ID) in each chat line's portrait, and clicking the
    /// portrait starts a tell to whoever holds that slot when it is clicked. The server reuses a slot as soon as its
    /// player logs out, so an old portrait can address a different character. This records, for each observer, the
    /// characters whose lines arrived from each slot, and refuses a tell started from a portrait that may no longer
    /// belong to the character it was shown for, unless the player clicks the portrait again to confirm who now
    /// holds the slot.
    /// </summary>
    public sealed class ChatPortraitTellGuard
    {
        /// <summary>
        /// Chat lines an observer receives after a speaker's last line before that speaker's portraits count as
        /// scrolled out of the chat log. The client keeps 264 messages by default, and combat and server feedback,
        /// which are not counted here, fill the same log, so this is well past what a default log still shows.
        /// </summary>
        public const int ChatLogHorizon = 1000;

        /// <summary>
        /// How long after a doubtful click a second click on the same portrait still confirms it. The player is told
        /// who the tell now reaches, so the second click is a choice of that player, like Send Tell in the Player List.
        /// </summary>
        public static readonly TimeSpan ConfirmationWindow = TimeSpan.FromMinutes(2);

        /// <summary>
        /// How long a confirming click must come after the doubtful one, so a double-click, made before the player
        /// could read who the tell reaches, does not confirm it.
        /// </summary>
        public static readonly TimeSpan ConfirmationDelay = TimeSpan.FromSeconds(1);

        private sealed class ObserverLog
        {
            public long LinesReceived;
            public readonly Dictionary<uint, Dictionary<string, long>> LastLineBySpeakerBySlot = new();
        }

        private sealed record PortraitClick(uint Slot, string Holder, bool Refused, DateTime Expires);

        private sealed record DoubtfulClick(uint Slot, string Holder, DateTime ClickedAt);

        private readonly TimeSpan _clickLifetime;
        private readonly Dictionary<uint, ObserverLog> _logsByObserver = new();
        private readonly Dictionary<uint, PortraitClick> _clicksByObserver = new();
        private readonly Dictionary<uint, DoubtfulClick> _doubtfulClicksByObserver = new();

        /// <param name="clickLifetime">How long after a portrait click its tell is still checked.</param>
        public ChatPortraitTellGuard(TimeSpan clickLifetime)
        {
            _clickLifetime = clickLifetime;
        }

        /// <summary>Records that a chat line from the speaker, who holds the slot, reached the observer's client.</summary>
        public void RecordLine(uint observer, uint speakerSlot, string speaker)
        {
            if (!_logsByObserver.TryGetValue(observer, out var log))
            {
                log = new ObserverLog();
                _logsByObserver[observer] = log;
            }

            if (!log.LastLineBySpeakerBySlot.TryGetValue(speakerSlot, out var speakers))
            {
                speakers = new Dictionary<string, long>();
                log.LastLineBySpeakerBySlot[speakerSlot] = speakers;
            }

            speakers[speaker] = ++log.LinesReceived;
            RemoveScrolledOut(log, speakers);
        }

        /// <summary>How many chat lines from other players have reached the observer's client.</summary>
        public long CountLinesReceived(uint observer)
        {
            return _logsByObserver.TryGetValue(observer, out var log) ? log.LinesReceived : 0;
        }

        /// <summary>Drops an observer's history once their client, and with it their chat log, is gone.</summary>
        public void ForgetObserver(uint observer)
        {
            _logsByObserver.Remove(observer);
            _clicksByObserver.Remove(observer);
            _doubtfulClicksByObserver.Remove(observer);
        }

        /// <summary>
        /// Classifies a portrait click that resolved to the slot's current holder, and remembers it so the tell it
        /// starts can be checked. A doubtful click (<see cref="ChatPortraitClick.FormerHolder"/> or
        /// <see cref="ChatPortraitClick.Ambiguous"/>) is <see cref="ChatPortraitClick.Confirmed"/> when it repeats the
        /// player's previous doubtful click on the same slot and holder within <see cref="ConfirmationWindow"/>, but no
        /// sooner than <see cref="ConfirmationDelay"/>.
        /// </summary>
        public ChatPortraitClick Click(uint observer, uint slot, string holder, DateTime now)
        {
            var result = Classify(observer, slot, holder);
            if (IsDoubtful(result))
            {
                if (_doubtfulClicksByObserver.TryGetValue(observer, out var previous) &&
                    previous.Slot == slot &&
                    previous.Holder == holder &&
                    now - previous.ClickedAt >= ConfirmationDelay &&
                    now - previous.ClickedAt <= ConfirmationWindow)
                {
                    _doubtfulClicksByObserver.Remove(observer);
                    result = ChatPortraitClick.Confirmed;
                }
                else
                {
                    _doubtfulClicksByObserver[observer] = new DoubtfulClick(slot, holder, now);
                }
            }
            else
            {
                _doubtfulClicksByObserver.Remove(observer);
            }

            _clicksByObserver[observer] = new PortraitClick(slot, holder, IsDoubtful(result), now + _clickLifetime);
            return result;
        }

        /// <summary>Whether a click may have come from a line of someone other than the slot's current holder.</summary>
        public static bool IsDoubtful(ChatPortraitClick click)
        {
            return click is ChatPortraitClick.FormerHolder or ChatPortraitClick.Ambiguous;
        }

        /// <summary>
        /// Whether to refuse a tell because it is the one a portrait click started and the click may not belong to
        /// the character the tell now reaches: either it already looked stale, or the slot changed hands since.
        /// Any tell ends the click, since the prompt the click filled in has been sent.
        /// </summary>
        public bool ConsumeRefusedTell(uint observer, uint targetSlot, string target, DateTime now)
        {
            if (!_clicksByObserver.Remove(observer, out var click))
                return false;

            if (click.Slot != targetSlot || now > click.Expires)
                return false;

            return click.Refused || click.Holder != target;
        }

        /// <summary>
        /// Classifies a portrait of the slot by the lines the observer received from it, without counting as a click.
        /// </summary>
        public ChatPortraitClick Classify(uint observer, uint slot, string holder)
        {
            if (!_logsByObserver.TryGetValue(observer, out var log) ||
                !log.LastLineBySpeakerBySlot.TryGetValue(slot, out var speakers))
            {
                return ChatPortraitClick.Unrecorded;
            }

            RemoveScrolledOut(log, speakers);
            if (speakers.Count == 0)
            {
                log.LastLineBySpeakerBySlot.Remove(slot);
                return ChatPortraitClick.Unrecorded;
            }

            if (!speakers.ContainsKey(holder))
                return ChatPortraitClick.FormerHolder;

            return speakers.Count == 1
                ? ChatPortraitClick.CurrentHolder
                : ChatPortraitClick.Ambiguous;
        }

        // Portraits of lines older than the chat log can no longer be clicked.
        private static void RemoveScrolledOut(ObserverLog log, Dictionary<string, long> speakers)
        {
            foreach (var scrolledOut in speakers.Where(speaker => log.LinesReceived - speaker.Value >= ChatLogHorizon).ToList())
                speakers.Remove(scrolledOut.Key);
        }
    }
}
