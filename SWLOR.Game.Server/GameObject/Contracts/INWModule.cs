using System.Collections.Generic;

namespace SWLOR.Game.Server.GameObject.Contracts
{
    public interface INWModule
    {
        List<NWPlayer> Players { get; }
    }
}