using Object = NWN.Object;

namespace SWLOR.Game.Server.GameObject
{
    public class NWPlaceable : NWObject
    {
        public NWPlaceable(Object nwnObject) 
            : base(nwnObject)
        {
        }

        public virtual bool IsUseable
        {
            get => _.GetUseableFlag(Object) == 1;
            set => _.SetUseableFlag(Object, value ? 1 : 0);
        }

        public virtual bool IsLocked
        {
            get => _.GetLocked(Object) == 1;
            set => _.SetLocked(Object, value ? 1 : 0);
        }

        public static implicit operator Object(NWPlaceable o)
        {
            return o.Object;
        }
        public static implicit operator NWPlaceable(Object o)
        {
            return new NWPlaceable(o);
        }

    }
}
