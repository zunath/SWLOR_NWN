using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.SearchContainer
{
    public class OnClosed: IRegisteredEvent
    {
        private readonly ISearchService _search;

        public OnClosed(ISearchService search)
        {
            _search = search;
        }

        public bool Run(params object[] args)
        {
            _search.OnChestClose((Object.OBJECT_SELF));
            return true;
        }
    }
}
