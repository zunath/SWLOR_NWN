using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.ChatPortraitTellService
{
    public enum ChatPortraitClick
    {
        /// <summary>No chat line from the slot was recorded for the observer, so there is nothing to compare.</summary>
        Unrecorded,
        /// <summary>Every recorded line from the slot came from the character who holds it now.</summary>
        CurrentHolder,
        /// <summary>The slot's lines all came from characters who left; the current holder sent none of them.</summary>
        FormerHolder,
        /// <summary>The current holder and a former holder both sent lines from the slot, so the clicked line is unknown.</summary>
        Ambiguous,
    }

    /// <summary>
    /// The NWN client stores the speaker's connection slot (player ID) in each chat line's portrait, and clicking the
    /// portrait starts a tell to whoever holds that slot when it is clicked. The server reuses a slot as soon as its
    /// player logs out, so an old portrait can address a different character. This records, for each observer, the
    /// characters whose lines arrived from each slot, and refuses a tell started from a portrait that may no longer
    /// belong to the character it was shown for.
    /// </summary>
    public sealed class ChatPortraitTellGuard
    {
        private readonly TimeSpan _refusalLifetime;
        private readonly Dictionary<uint, Dictionary<uint, HashSet<string>>> _speakersBySlotByObserver = new();
        private readonly Dictionary<uint, (uint Slot, DateTime Expires)> _refusedClicks = new();

        /// <param name="refusalLifetime">How long after a refused portrait click its tell is still refused.</param>
        public ChatPortraitTellGuard(TimeSpan refusalLifetime)
        {
            _refusalLifetime = refusalLifetime;
        }

        /// <summary>Records that a chat line from the speaker, who holds the slot, reached the observer's client.</summary>
        public void RecordLine(uint observer, uint speakerSlot, string speaker)
        {
            if (!_speakersBySlotByObserver.TryGetValue(observer, out var speakersBySlot))
            {
                speakersBySlot = new Dictionary<uint, HashSet<string>>();
                _speakersBySlotByObserver[observer] = speakersBySlot;
            }

            if (!speakersBySlot.TryGetValue(speakerSlot, out var speakers))
            {
                speakers = new HashSet<string>();
                speakersBySlot[speakerSlot] = speakers;
            }

            speakers.Add(speaker);
        }

        /// <summary>Drops an observer's history once their client, and with it their chat log, is gone.</summary>
        public void ForgetObserver(uint observer)
        {
            _speakersBySlotByObserver.Remove(observer);
            _refusedClicks.Remove(observer);
        }

        /// <summary>
        /// Classifies a portrait click that resolved to the slot's current holder. A click that may not belong to
        /// the holder is remembered so the tell it starts can be refused.
        /// </summary>
        public ChatPortraitClick Click(uint observer, uint slot, string holder, DateTime now)
        {
            var result = Classify(observer, slot, holder);
            if (result is ChatPortraitClick.FormerHolder or ChatPortraitClick.Ambiguous)
                _refusedClicks[observer] = (slot, now + _refusalLifetime);
            else
                _refusedClicks.Remove(observer);

            return result;
        }

        /// <summary>
        /// Whether a tell is the one a refused portrait click started. Any tell ends the click, since the
        /// prompt the click filled in has been sent.
        /// </summary>
        public bool ConsumeRefusedTell(uint observer, uint targetSlot, DateTime now)
        {
            if (!_refusedClicks.Remove(observer, out var click))
                return false;

            return click.Slot == targetSlot && now <= click.Expires;
        }

        private ChatPortraitClick Classify(uint observer, uint slot, string holder)
        {
            if (!_speakersBySlotByObserver.TryGetValue(observer, out var speakersBySlot) ||
                !speakersBySlot.TryGetValue(slot, out var speakers))
            {
                return ChatPortraitClick.Unrecorded;
            }

            if (!speakers.Contains(holder))
                return ChatPortraitClick.FormerHolder;

            return speakers.Count == 1
                ? ChatPortraitClick.CurrentHolder
                : ChatPortraitClick.Ambiguous;
        }
    }
}
