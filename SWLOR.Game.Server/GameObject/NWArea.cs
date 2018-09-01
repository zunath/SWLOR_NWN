using NWN;
using SWLOR.Game.Server.GameObject.Contracts;

using Object = NWN.Object;

namespace SWLOR.Game.Server.GameObject
{
    public class NWArea: NWObject, INWArea
    {
        public NWArea(INWScript script,
            AppState state) 
            : base(script, state)
        {
        }

        public new static NWArea Wrap(Object @object)
        {
            NWArea obj = (NWArea)App.Resolve<INWArea>();
            obj.Object = @object;

            return obj;
        }
    }
}
