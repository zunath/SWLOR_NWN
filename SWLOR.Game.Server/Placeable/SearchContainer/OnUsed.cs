using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.SearchContainer
{
    public class OnUsed : IRegisteredEvent
    {
        private readonly ISearchService _search;

        public OnUsed(ISearchService search)
        {
            _search = search;
        }

        public bool Run(params object[] args)
        {
            _search.OnChestUsed(NWPlaceable.Wrap(Object.OBJECT_SELF));
            return true;
        }
    }
}
