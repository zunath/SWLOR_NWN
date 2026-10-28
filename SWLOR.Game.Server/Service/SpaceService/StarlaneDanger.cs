using System;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>Why a starlane is marked dangerous for the pilot about to use it (plan section 12).</summary>
    [Flags]
    public enum StarlaneDanger
    {
        None = 0,

        /// <summary>The system it leads to is a higher tier than the one the pilot is in.</summary>
        HigherTier = 1,

        /// <summary>It leads into one of the Small and Medium hull zones.</summary>
        Zone = 2,

        /// <summary>It leads into Wild Space, where a ship another player destroys can drop its modules.</summary>
        WildSpace = 4,
    }
}
