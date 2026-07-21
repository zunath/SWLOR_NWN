using System;

namespace SWLOR.Game.Server.Service.AreaGenerationService
{
    /// <summary>
    /// How a generated area behaves across server restarts.
    /// Only Ephemeral is implemented; the other modes are reserved so consumer code
    /// can declare intent now and pick up the behavior when those strategies land.
    /// </summary>
    public enum AreaPersistenceMode
    {
        /// <summary>Instance vanishes on restart; players who were inside relocate to the recorded entrance location.</summary>
        Ephemeral = 0,
        /// <summary>Seed and config persist; the same layout regenerates deterministically after a restart.</summary>
        SeedPersisted = 1,
        /// <summary>The realized area is exported as a real module resource and survives restarts.</summary>
        FullExport = 2
    }
}
