using System;
using System.Collections.Generic;
using NWN;
using static NWN.NWScript;
using Object = NWN.Object;


namespace SWLOR.Game.Server.GameObject
{
    public class NWModule : NWObject
    {
        public NWModule(INWScript script,
            AppState state) 
            : base(script, state)
        {
        }

        public static NWModule Get()
        {
            return App.Resolve<INWScript, NWModule>(script => NWObjectFactory.Build<NWModule>(script.GetModule()));
        }

        public IEnumerable<NWPlayer> Players
        {
            get
            {
                for (NWPlayer pc = _.GetFirstPC(); pc.IsValid; pc = _.GetNextPC())
                {
                    yield return pc;
                }
            }
        }

        public IEnumerable<NWArea> Areas
        {
            get
            {
                for (NWArea area = _.GetFirstArea(); _.GetIsObjectValid(area) == TRUE; area = _.GetNextArea())
                {
                    yield return area;
                }
            }
        }


        public static implicit operator Object(NWModule o)
        {
            return o.Object;
        }
        public static implicit operator NWModule(Object o)
        {
            return NWObjectFactory.Build<NWModule>(o);
        }
    }
}
