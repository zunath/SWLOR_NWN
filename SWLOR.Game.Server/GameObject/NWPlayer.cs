using System.Collections.Generic;
using NWN;

namespace SWLOR.Game.Server.GameObject
{
    public class NWPlayer : NWCreature
    {
        public NWPlayer(NWGameObject nwnObject)
            : base(nwnObject)
        {
        }

        public override IEnumerable<NWCreature> PartyMembers
        {
            get
            {
                for (NWPlayer member = _.GetFirstFactionMember(Object); member.IsValid; member = _.GetNextFactionMember(Object))
                {
                    yield return member;
                }
            }
        }

        //
        // -- BELOW THIS POINT IS JUNK TO MAKE THE API FRIENDLIER!
        //

        public static bool operator ==(NWPlayer lhs, NWPlayer rhs)
        {
            bool lhsNull = lhs is null;
            bool rhsNull = rhs is null;
            return (lhsNull && rhsNull) || (!lhsNull && !rhsNull && lhs.Object == rhs.Object);
        }

        public static bool operator !=(NWPlayer lhs, NWPlayer rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object o)
        {
            NWPlayer other = o as NWPlayer;
            return other != null && other == this;
        }

        public override int GetHashCode()
        {
            return Object.GetHashCode();
        }

        public static implicit operator NWGameObject(NWPlayer o)
        {
            return o.Object;
        }

        public static implicit operator NWPlayer(NWGameObject o)
        {
            return new NWPlayer(o);
        }
    }
}
