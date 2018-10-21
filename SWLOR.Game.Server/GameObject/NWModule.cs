
using System.Collections.Generic;
using NWN;
using static NWN.NWScript;
using Object = NWN.Object;


namespace SWLOR.Game.Server.GameObject
{
    public class NWModule : NWObject
    {
        public NWModule(Object nwnObject) 
            : base(nwnObject)
        {
        }

        public static NWModule Get()
        {
            INWScript _ = App.GetNWScript(); 
            return new NWModule(_.GetModule());
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

        //
        // -- BELOW THIS POINT IS JUNK TO MAKE THE API FRIENDLIER!
        //

        public static bool operator ==(NWModule lhs, NWModule rhs)
        {
            bool lhsNull = lhs is null;
            bool rhsNull = rhs is null;
            return (lhsNull && rhsNull) || (!lhsNull && !rhsNull && lhs.Object == rhs.Object);
        }

        public static bool operator !=(NWModule lhs, NWModule rhs)
        {
            return !(lhs == rhs);
        }

        public static implicit operator Object(NWModule o)
        {
            return o.Object;
        }
        public static implicit operator NWModule(Object o)
        {
            return new NWModule(o);
        }

    }
}
