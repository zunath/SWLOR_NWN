using SWLOR.Game.Server.Core.NWScript;

namespace SWLOR.Game.Server.Legacy.GameObject
{
    public class NWPlaceable : NWObject
    {
        public NWPlaceable(uint nwnObject) 
            : base(nwnObject)
        {
        }

        public virtual bool IsUseable
        {
            get => NWScript.GetUseableFlag(Object);
            set => NWScript.SetUseableFlag(Object, value);
        }

        public virtual bool IsLocked
        {
            get => NWScript.GetLocked(Object);
            set => NWScript.SetLocked(Object, value);
        }

        //
        // -- BELOW THIS POINT IS JUNK TO MAKE THE API FRIENDLIER!
        //

        public static bool operator ==(NWPlaceable lhs, NWPlaceable rhs)
        {
            var lhsNull = lhs is null;
            var rhsNull = rhs is null;
            return (lhsNull && rhsNull) || (!lhsNull && !rhsNull && lhs.Object == rhs.Object);
        }

        public static bool operator !=(NWPlaceable lhs, NWPlaceable rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object o)
        {
            var other = o as NWPlaceable;
            return other != null && other == this;
        }

        public override int GetHashCode()
        {
            return Object.GetHashCode();
        }

        public static implicit operator uint(NWPlaceable o)
        {
            return o.Object;
        }

        public static implicit operator NWPlaceable(uint o)
        {
            return new NWPlaceable(o);
        }
    }
}
