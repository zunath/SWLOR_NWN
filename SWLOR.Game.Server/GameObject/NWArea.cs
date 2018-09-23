using System;
using NWN;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.GameObject
{
    public class NWArea : NWObject
    {
        public NWArea(Object o) 
            : base(o)
        {
            
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
            return new NWArea(o);
        }
    }
}
