using System.Collections.Generic;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IBackgroundService
    {
        void ApplyBackgroundBonuses(NWPlayer oPC);
    }
}
