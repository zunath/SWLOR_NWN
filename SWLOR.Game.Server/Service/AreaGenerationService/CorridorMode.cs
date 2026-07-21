using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>How a layout style realizes the connections between rooms.</summary>
    public enum CorridorMode
    {
        /// <summary>Corridors are open-terrain corner bands (walkable floor lanes).</summary>
        OpenLane = 0,
        /// <summary>Corridors are Corridor edge-crosser chains through solid cells (wall-embedded tunnels).</summary>
        Tunnel = 1
    }
}
