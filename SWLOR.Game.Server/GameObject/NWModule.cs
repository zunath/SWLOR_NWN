
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript;


namespace SWLOR.Game.Server.GameObject
{
    public class NWModule : NWObject
    {
        public NWModule(uint nwnObject) 
            : base(nwnObject)
        {
        }

        public static NWModule Get()
        {
            return new NWModule(NWScript.GetModule());
        }

        public IEnumerable<NWPlayer> Players
        {
            get
            {
                for (NWPlayer pc = NWScript.GetFirstPC(); pc.IsValid; pc = NWScript.GetNextPC())
                {
                    yield return pc;
                }
            }
        }

        public IEnumerable<uint> Areas
        {
            get
            {
                for (var area = NWScript.GetFirstArea(); NWScript.GetIsObjectValid(area) == true; area = NWScript.GetNextArea())
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
            var lhsNull = lhs is null;
            var rhsNull = rhs is null;
            return (lhsNull && rhsNull) || (!lhsNull && !rhsNull && lhs.Object == rhs.Object);
        }

        public static bool operator !=(NWModule lhs, NWModule rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object o)
        {
            var other = o as NWModule;
            return other != null && other == this;
        }

        public override int GetHashCode()
        {
            return Object.GetHashCode();
        }

        public static implicit operator uint(NWModule o)
        {
            return o.Object;
        }
        public static implicit operator NWModule(uint o)
        {
            return new NWModule(o);
        }

    }
}
