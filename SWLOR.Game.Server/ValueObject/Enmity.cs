using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ValueObject
{
    public class Enmity
    {
        public NWCreature TargetObject { get; }

        private int _volatileAmount;

        public int VolatileAmount
        {
            get => _volatileAmount;
            set
            {
                if (value <= 1) value = 1;
                _volatileAmount = value;
            }
        }

        private int _cumulativeAmount;

        public int CumulativeAmount
        {
            get => _cumulativeAmount;
            set
            {
                if(value <= 1) value = 1;
                _cumulativeAmount = value;
            }
        }

        public int TotalAmount
        {
            get => VolatileAmount + CumulativeAmount;
        }

        public Enmity(NWCreature targetObject)
        {
            TargetObject = targetObject;
            VolatileAmount = 1;
            CumulativeAmount = 1;
        }
    }
}
