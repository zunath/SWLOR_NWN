using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using static SWLOR.Game.Server.NWScript._;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.GameObject
{
    public class NWArea : NWObject
    {
        public NWArea(NWGameObject o) 
            : base(o)
        {
            
        }

        public int Width => _.GetAreaSize(AreaProperty.Width, Object);

        public int Height => _.GetAreaSize(AreaProperty.Height, Object);

        public bool IsInstance => _.GetLocalBoolean(Object, "IS_AREA_INSTANCE") == true;

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
