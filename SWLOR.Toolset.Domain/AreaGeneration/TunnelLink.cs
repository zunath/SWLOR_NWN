#nullable disable
using System;
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// A tunnel segment connecting two open regions through solid cells. Recorded by layout styles
    /// carving in Tunnel mode so geodesic passes (role assignment) can traverse connections that do
    /// not exist in the open-corner graph.
    /// </summary>
    public class TunnelLink
    {
        /// <summary>Open corner where the tunnel meets open space on one side.</summary>
        public (int X, int Y) CornerA { get; set; }
        /// <summary>Open corner where the tunnel meets open space on the other side.</summary>
        public (int X, int Y) CornerB { get; set; }
        /// <summary>Traversal cost in cells (>= 1).</summary>
        public int Length { get; set; }
    }
}
