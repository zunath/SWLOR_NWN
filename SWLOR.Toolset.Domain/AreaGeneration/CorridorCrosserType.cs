#nullable disable
using System;
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// Which crosser vocabulary Tunnel-mode corridors carve. Corridor (default) is the classic
    /// wall-embedded facility tunnel: a Corridor-edge body chain entering rooms through Doorway-edge
    /// ports (see LayoutTunnelCarver). Alley reuses the identical port/BFS/chain mechanics but carves
    /// vmr01's exterior "alley" crosser instead -- verified offline against vmr01 .set data, a single
    /// crosser name serves both the tunnel body (TILE221, all-solid straight pair) AND the room-facing
    /// port (TILE210, Plaza-cornered with the crosser on the solid side) -- there is no separate
    /// Doorway-equivalent the way Corridor mode has. Custom carves an arbitrary tileset-declared
    /// body/port crosser PAIR (see MacroLayoutParameters.TunnelBodyCrosser/TunnelPortCrosser): several
    /// onboarded tilesets ship a district-scoped crosser family that is mechanically identical to the
    /// Corridor/Doorway pairing, just under different names (e.g. tdc01's "[Grey]" district uses
    /// "GreyCorridor" for the body but the CANONICAL "Doorway" for the port; tdm01's "[Desert]"/
    /// "[Organic]" districts follow the same body-only-renamed pattern) -- production carvers only
    /// ever WRITE the literal strings a profile declares, they never infer a family from a naming
    /// convention. Ignored unless CorridorMode is Tunnel.
    /// </summary>
    public enum CorridorCrosserType
    {
        Corridor = 0,
        Alley = 1,
        Custom = 2
    }
}
