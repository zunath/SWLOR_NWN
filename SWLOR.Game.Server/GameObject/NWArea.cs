using System.Collections.Generic;
using NWN;
using static NWN._;

namespace SWLOR.Game.Server.GameObject
{
    public class NWArea : NWObject
    {
        public NWArea(NWGameObject o) 
            : base(o)
        {
            
        }

        public int Width => _.GetAreaSize(AREA_WIDTH, Object);

        public int Height => _.GetAreaSize(AREA_HEIGHT, Object);

        public bool IsInstance => _.GetLocalInt(Object, "IS_AREA_INSTANCE") == TRUE;

        public IEnumerable<NWObject> Objects
        {
            get
            {
                for (NWObject obj = _.GetFirstObjectInArea(Object); obj.IsValid; obj = _.GetNextObjectInArea(Object))
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
            bool lhsNull = lhs is null;
            bool rhsNull = rhs is null;
            return (lhsNull && rhsNull) || (!lhsNull && !rhsNull && lhs.Object == rhs.Object);
        }

        public static bool operator !=(NWArea lhs, NWArea rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object o)
        {
            NWArea other = o as NWArea;
            return other != null && other == this;
        }

        public override int GetHashCode()
        {
            return Object.GetHashCode();
        }

        public static implicit operator NWGameObject(NWArea o)
        {
            return o.Object;
        }

        public static implicit operator NWArea(NWGameObject o)
        {
            return new NWArea(o);
        }
    }
}
