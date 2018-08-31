using System.Collections.Generic;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface IBackgroundService
    {
        IEnumerable<Background> GetActiveBackgrounds();
        void SetPlayerBackground(NWPlayer player, Background background);
        void OnModuleClientEnter();
    }
}
