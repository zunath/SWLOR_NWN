﻿using NWN;
using SWLOR.Game.Server.NWScript;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.GameObject
{
    public class NWPlaceable : NWObject
    {
        public NWPlaceable(NWGameObject nwnObject) 
            : base(nwnObject)
        {
        }

        public virtual bool IsUseable
        {
            get => _.GetUseableFlag(Object) == true;
            set => _.SetUseableFlag(Object, value ? true : false);
        }

        public virtual bool IsLocked
        {
            get => _.GetLocked(Object) == true;
            set => _.SetLocked(Object, value ? true : false);
        }

        //
        // -- BELOW THIS POINT IS JUNK TO MAKE THE API FRIENDLIER!
        //

        public static bool operator ==(NWPlaceable lhs, NWPlaceable rhs)
        {
            bool lhsNull = lhs is null;
            bool rhsNull = rhs is null;
            return (lhsNull && rhsNull) || (!lhsNull && !rhsNull && lhs.Object == rhs.Object);
        }

        public static bool operator !=(NWPlaceable lhs, NWPlaceable rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object o)
        {
            NWPlaceable other = o as NWPlaceable;
            return other != null && other == this;
        }

        public override int GetHashCode()
        {
            return Object.GetHashCode();
        }

        public static implicit operator NWGameObject(NWPlaceable o)
        {
            return o.Object;
        }

        public static implicit operator NWPlaceable(NWGameObject o)
        {
            return new NWPlaceable(o);
        }
    }
}
