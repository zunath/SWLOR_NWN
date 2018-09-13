using System;
using NWN;
using SWLOR.Game.Server.GameObject.Contracts;
using static NWN.NWScript;
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

        public int Width => _.GetAreaSize(AREA_WIDTH, Object);

        public int Height => _.GetAreaSize(AREA_HEIGHT, Object);

        public bool IsInstance => _.GetLocalInt(Object, "IS_AREA_INSTANCE") == TRUE;
        
        public static implicit operator Object(NWArea o)
        {
            return o.Object;
        }
        public static implicit operator NWArea(Object o)
        {
            return Wrap(o);
        }
    }
}
