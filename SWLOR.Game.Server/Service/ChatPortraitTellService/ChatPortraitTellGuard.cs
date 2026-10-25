using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.ChatPortraitTellService
{
    /// <summary>
    /// The NWN client stores the speaker's connection slot (player ID) in each chat line's portrait, and clicking the
    /// portrait starts a tell to whoever holds that slot when it is clicked. The server reuses a slot as soon as its
    /// player logs out, so an old portrait can address a different character. This records, for each observer, the
    /// characters whose lines arrived from each slot, and refuses a tell started from a portrait that may no longer
    /// belong to the character it was shown for.
    /// </summary>
    public sealed class ChatPortraitTellGuard
    {
        /// <summary>
        /// Chat lines an observer receives after a speaker's last line before that speaker's portraits count as
        /// scrolled out of the chat log. The client keeps 264 messages by default, and combat and server feedback,
        /// which are not counted here, fill the same log, so this is well past what a default log still shows.
        /// </summary>
        public const int ChatLogHorizon = 1000;

        private sealed class ObserverLog
        {
            public long LinesReceived;
            public readonly Dictionary<uint, Dictionary<string, long>> LastLineBySpeakerBySlot = new();
        }

        private sealed record PortraitClick(uint Slot, string Holder, bool Refused, DateTime Expires);

        private readonly TimeSpan _clickLifetime;
        private readonly Dictionary<uint, ObserverLog> _logsByObserver = new();
        private readonly Dictionary<uint, PortraitClick> _clicksByObserver = new();

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
        }

        /// <summary>
        /// Classifies a portrait click that resolved to the slot's current holder, and remembers it so the tell it
        /// starts can be checked.
        /// </summary>
        public ChatPortraitClick Click(uint observer, uint slot, string holder, DateTime now)
        {
            var result = Classify(observer, slot, holder);
            var refused = result is ChatPortraitClick.FormerHolder or ChatPortraitClick.Ambiguous;
            _clicksByObserver[observer] = new PortraitClick(slot, holder, refused, now + _clickLifetime);
            return result;
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

        private ChatPortraitClick Classify(uint observer, uint slot, string holder)
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
