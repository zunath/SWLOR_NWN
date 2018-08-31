using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.NWScript;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Placeable.SearchContainer
{
    public class OnDisturbed: IRegisteredEvent
    {
        private readonly ISearchService _search;

        public OnDisturbed(ISearchService search)
        {
            _search = search;
        }

        public bool Run(params object[] args)
        {
            _search.OnChestDisturbed(NWPlaceable.Wrap(Object.OBJECT_SELF));
            return true;
        }
    }
}
