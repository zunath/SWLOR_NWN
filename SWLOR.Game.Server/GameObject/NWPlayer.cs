using System.Collections.Generic;
using Object = NWN.Object;

namespace SWLOR.Game.Server.GameObject
{
    public class NWPlayer : NWCreature
    {
        public NWPlayer(Object nwnObject)
            : base(nwnObject)
        {
        }

        public virtual bool IsBusy
        {
            get => GetLocalInt("IS_BUSY") == 1;
            set => SetLocalInt("IS_BUSY", value ? 1 : 0);
        }


        public virtual IEnumerable<NWPlayer> PartyMembers
        {
            get
            {
                for (NWPlayer member = _.GetFirstFactionMember(Object); member.IsValid; member = _.GetNextFactionMember(Object))
                {
                    yield return member;
                }
            }
        }
        

        public static implicit operator Object(NWPlayer o)
        {
            return o.Object;
        }

        public static implicit operator NWPlayer(Object o)
        {
            return new NWPlayer(o);
        }
    }
}
