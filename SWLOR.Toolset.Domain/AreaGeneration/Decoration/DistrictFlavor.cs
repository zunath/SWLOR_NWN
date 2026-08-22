#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Toolset.Domain.AreaGeneration.Decoration
{
    /// <summary>
    /// District flavor of one open room in an urban-grammar area (see
    /// DungeonDecorationPlanner.AssignDistrictFlavors): hand-built city repetition is
    /// DISTRICT-SCOPED, not globally uniform -- big cargo concentrates in industrial yards
    /// (swd_conta004 mined 61x in the pw_ar_nsshipyard shipyard and ZERO in the commercial
    /// promenades), while promenades use kiosks/benches/signage and plazas use pillars/monuments.
    /// Rooms are assigned a flavor deterministically (no RNG) from road frontage, stamped-structure
    /// adjacency, entrance distance, and interior depth; palette entries then opt into flavors via
    /// <see cref="DungeonDecorationEntry.DistrictWeights"/>. None = no district system (every
    /// non-urban tileset).
    /// </summary>
    public enum DistrictFlavor
    {
        /// <summary>No district assignment (non-urban tilesets; entries use their base Weight).</summary>
        None = 0,
        /// <summary>Cargo yards, docks, machinery: big containers, tanks, pipes, dumpsters,
        /// work lighting. The ONLY flavor whose zones may host <see cref="DecorationSize.Huge"/>
        /// building-scale placements.</summary>
        Industrial = 1,
        /// <summary>Market/promenade frontage: kiosk rows, benches, holo signage, market goods,
        /// street lamps. Prefers road-frontage rooms.</summary>
        Commercial = 2,
        /// <summary>Civic plazas: pillars/colonnades, holo monuments, clean floors, courtyards.
        /// Prefers rooms with a real interior (courtyard anchor).</summary>
        Civic = 3
    }
}
