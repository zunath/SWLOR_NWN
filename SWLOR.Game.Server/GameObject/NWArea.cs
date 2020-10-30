using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum.Area;

namespace SWLOR.Game.Server.GameObject
{
    public class NWArea : NWObject
    {
        public NWArea(uint o) 
            : base(o)
        {
            
        }

        public int Width => NWScript.GetAreaSize(Dimension.Width, Object);

        public int Height => NWScript.GetAreaSize(Dimension.Height, Object);

        public bool IsInstance => NWScript.GetLocalBool(Object, "IS_AREA_INSTANCE");

        public IEnumerable<NWObject> Objects
        {
            get
            {
                for (NWObject obj = NWScript.GetFirstObjectInArea(Object); obj.IsValid; obj = NWScript.GetNextObjectInArea(Object))
                {
                    yield return obj;
                }
            }
        }

        //
        // -- BELOW THIS POINT IS JUNK TO MAKE THE API FRIENDLIER!
        //

        public static bool operator ==(NWArea lhs, NWArea rhs)
        {
            var lhsNull = lhs is null;
            var rhsNull = rhs is null;
            return (lhsNull && rhsNull) || (!lhsNull && !rhsNull && lhs.Object == rhs.Object);
        }

        public static bool operator !=(NWArea lhs, NWArea rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object o)
        {
            var other = o as NWArea;
            return other != null && other == this;
        }

        public override int GetHashCode()
        {
            return Object.GetHashCode();
        }

        public static implicit operator uint(NWArea o)
        {
            return o.Object;
        }

        public static implicit operator NWArea(uint o)
        {
            return new NWArea(o);
        }
    }
}
