namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A rift between a Wild Space system and a known orbit or another Wild Space system (plan section 23). Each end is
    /// a hidden Rift signature in its area. Held in memory: a restart seeds a new network.
    /// </summary>
    public class RiftConnection
    {
        public string Id { get; init; }

        /// <summary>The Wild Space system that opened it.</summary>
        public string AreaA { get; init; }

        /// <summary>The far end: a known orbit or another Wild Space system.</summary>
        public string AreaB { get; init; }

        public RiftSizeClass Size { get; init; }
        public DateTime OpenedAt { get; init; }

        /// <summary>When it collapses: the end of its lifetime, or sooner once its mass is spent.</summary>
        public DateTime CollapsesAt { get; set; }

        public int MassRemaining { get; set; }
        public bool WarningSent { get; set; }
        public bool Collapsed { get; set; }

        /// <summary>The signature IDs of its two ends.</summary>
        public string SignatureA { get; set; }
        public string SignatureB { get; set; }

        public bool Touches(string areaResref) => AreaA == areaResref || AreaB == areaResref;

        public string OtherEnd(string areaResref) => areaResref == AreaA ? AreaB : AreaA;

        public string SignatureIn(string areaResref) => areaResref == AreaA ? SignatureA : SignatureB;
    }
}
