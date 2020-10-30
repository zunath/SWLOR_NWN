using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.NWN;

namespace SWLOR.Game.Server.GameObject
{
    public class NWPlayer : NWCreature
    {
        public NWPlayer(uint nwnObject)
            : base(nwnObject)
        {
        }

        public override IEnumerable<NWCreature> PartyMembers
        {
            get
            {
                for (NWPlayer member = NWScript.GetFirstFactionMember(Object); member.IsValid; member = NWScript.GetNextFactionMember(Object))
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

        public static implicit operator uint(NWPlayer o)
        {
            return o.Object;
        }

        public static implicit operator NWPlayer(uint o)
        {
            return new NWPlayer(o);
        }
    }
}
