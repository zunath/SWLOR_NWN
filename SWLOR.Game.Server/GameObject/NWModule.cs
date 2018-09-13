using System;
using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.GameObject.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;


namespace SWLOR.Game.Server.GameObject
{
    public class NWModule: NWObject, INWModule
    {
        public NWModule(INWScript script,
            AppState state) 
            : base(script, state)
        {
        }

        public static NWModule Get()
        {
            var module = (NWModule) App.Resolve<INWModule>();
            INWScript _ = App.Resolve<INWScript>();
            module.Object = _.GetModule();

            return module;
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
    }
}
