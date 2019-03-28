using SWLOR.Game.Server.Event;
using NWN;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Placeable.SearchContainer
{
    public class OnUsed : IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            SearchService.OnChestUsed((Object.OBJECT_SELF));
            return true;
        }
    }
}
