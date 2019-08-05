
using System.Collections.Generic;
using NWN;


namespace SWLOR.Game.Server.GameObject
{
    public class NWModule : NWObject
    {
        public NWModule(NWGameObject nwnObject) 
            : base(nwnObject)
        {
        }

        public static NWModule Get()
        {
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
                for (NWArea area = _.GetFirstArea(); _.GetIsObjectValid(area) == _.TRUE; area = _.GetNextArea())
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

        public override bool Equals(object o)
        {
            NWModule other = o as NWModule;
            return other != null && other == this;
        }

        public override int GetHashCode()
        {
            return Object.GetHashCode();
        }

        public static implicit operator NWGameObject(NWModule o)
        {
            return o.Object;
        }
        public static implicit operator NWModule(NWGameObject o)
        {
            return new NWModule(o);
        }

    }
}
