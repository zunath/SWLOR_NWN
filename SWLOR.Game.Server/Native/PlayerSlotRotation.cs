namespace SWLOR.Game.Server.Native
{
    /// <summary>
    /// Picks the connection slot a new login takes. The engine always takes the lowest free slot, so a slot
    /// freed by a logout went to the next login within minutes, and the chat portraits clients still held for
    /// the player who left then started tells to the newcomer. This picks the slot that has been free the
    /// longest instead, so slots that were never used come first and a freed slot waits behind every other.
    /// </summary>
    public sealed class PlayerSlotRotation
    {
        private readonly bool[] _inUse;
        private readonly ulong[] _freedAt;
        private ulong _observation;

        public PlayerSlotRotation(int slotCount)
        {
            if (slotCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(slotCount), slotCount, "There must be at least one slot.");

            _inUse = new bool[slotCount];
            _freedAt = new ulong[slotCount];
        }

        public int SlotCount => _inUse.Length;

        /// <summary>
        /// Records which slots are in use now. A slot that was in use at the last observation and is free now
        /// counts as freed at this observation, so slots freed between two logins tie and fall back to slot order.
        /// </summary>
        public void Observe(ReadOnlySpan<bool> inUse)
        {
            if (inUse.Length != _inUse.Length)
                throw new ArgumentException($"Expected {_inUse.Length} slots, got {inUse.Length}.", nameof(inUse));

            _observation++;
            for (var slot = 0; slot < _inUse.Length; slot++)
            {
                if (_inUse[slot] && !inUse[slot])
                    _freedAt[slot] = _observation;

                _inUse[slot] = inUse[slot];
            }
        }

        /// <summary>
        /// The free slot that has been free the longest at the last observation, or -1 when every slot is in use.
        /// </summary>
        public int ChooseSlot()
        {
            var chosen = -1;
            for (var slot = 0; slot < _inUse.Length; slot++)
            {
                if (_inUse[slot])
                    continue;

                if (chosen < 0 || _freedAt[slot] < _freedAt[chosen])
                    chosen = slot;
            }

            return chosen;
        }

        /// <summary>
        /// Records that the engine gave a slot to a connection, so it counts as in use until it is seen free.
        /// </summary>
        public void MarkInUse(int slot)
        {
            if ((uint)slot >= (uint)_inUse.Length)
                throw new ArgumentOutOfRangeException(nameof(slot), slot, "No such slot.");

            _inUse[slot] = true;
        }
    }
}
