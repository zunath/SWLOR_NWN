using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// A hidden signature in a space area (plan section 24). Nothing exists in the area for it until a party's signal
    /// reaches 100%; then its site is revealed and its objects spawn. Held in memory and never saved.
    /// </summary>
    public class ScanSignature
    {
        public string Id { get; init; }
        public string AreaResref { get; init; }
        public SignatureType Type { get; init; }

        /// <summary>The tier of its area (1-6): its signal, site content and rewards.</summary>
        public int Tier { get; init; }

        public float X { get; init; }
        public float Y { get; init; }
        public float Z { get; init; }

        public DateTime CreatedAt { get; init; }

        /// <summary>When an unresolved signature fades.</summary>
        public DateTime ExpiresAt { get; init; }

        /// <summary>The signal needed to resolve it.</summary>
        public double SignalRequired { get; init; }

        /// <summary>Signal gathered so far, by the player ID of the ship that scanned it.</summary>
        public Dictionary<string, double> Signal { get; } = new();

        /// <summary>When its site opened, or null while it is hidden.</summary>
        public DateTime? RevealedAt { get; set; }

        /// <summary>Its site's objectives (ships, clouds, the wreck, a cache) while they last.</summary>
        public List<uint> Objectives { get; } = new();

        /// <summary>The site cache, once it is spawned.</summary>
        public uint Cache { get; set; }

        /// <summary>Whether the site's cache has been spawned (or, for a cache site, is the site).</summary>
        public bool CacheSpawned { get; set; }

        /// <summary>Whether the site has been completed: its cache was taken.</summary>
        public bool Completed { get; set; }

        public bool IsRevealed => RevealedAt != null;
    }
}
