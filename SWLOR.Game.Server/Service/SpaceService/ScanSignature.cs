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

        /// <summary>The tier of its area (1-6): its signal, site content and rewards. Wild Space is tier 5 with <see cref="WildSpace"/> set.</summary>
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

        /// <summary>Whether a cache site's cache is locked and must be sliced open (space slicing).</summary>
        public bool Locked { get; init; }

        /// <summary>Whether the site has been completed: its cache was taken.</summary>
        public bool Completed { get; set; }

        /// <summary>Whether it lies in Wild Space: tier 5.5 content, shown as Tier 5.5 (plan section 23).</summary>
        public bool WildSpace { get; init; }

        /// <summary>The rift a Rift signature is an end of; empty for every other type.</summary>
        public string RiftId { get; init; } = string.Empty;

        /// <summary>A rift's size and destination, shown from the threat level on.</summary>
        public string RiftDetail { get; set; } = string.Empty;

        /// <summary>The faction agent mission this signature is a lead for (plan section 29); empty for every other signature.</summary>
        public string AgentMissionId { get; init; } = string.Empty;

        /// <summary>The player an agent lead is for: only their party's probes find it.</summary>
        public string AgentPlayerId { get; init; } = string.Empty;

        public bool IsRevealed => RevealedAt != null;

        /// <summary>Whether a faction agent placed it for one player's mission; it is not part of its area's signature count.</summary>
        public bool IsAgentLead => !string.IsNullOrEmpty(AgentMissionId);
    }
}
