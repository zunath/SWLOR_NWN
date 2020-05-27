using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.NWNX
{
    public class DamageEventData
    {
        public NWObject Damager { get; set; }
        private int _bludgeoning;
        public int Bludgeoning
        {
            get => _bludgeoning;
            set
            {
                if (value < -1)
                    value = -1;

                _bludgeoning = value;
            }
        }

        private int _pierce;

        public int Pierce
        {
            get => _pierce;
            set
            {
                if (value < -1)
                    value = -1;

                _pierce = value;
            }
        }

        private int _slash;

        public int Slash
        {
            get => _slash;
            set
            {
                if (value < -1)
                    value = -1;

                _slash = value;
            }
        }

        private int _magical;
        public int Magical
        {
            get => _magical;
            set
            {
                if (value < -1)
                    value = -1;

                _magical = value;
            }
        }

        private int _acid;
        public int Acid
        {
            get => _acid;
            set
            {
                if (value < -1)
                    value = -1;

                _acid = value;
            }
        }

        private int _cold;
        public int Cold
        {
            get => _cold;
            set
            {
                if (value < -1)
                    value = -1;

                _cold = value;
            }
        }

        private int _divine;
        public int Divine
        {
            get => _divine;
            set
            {
                if (value < -1)
                    value = -1;

                _divine = value;
            }
        }

        private int _electrical;
        public int Electrical
        {
            get => _electrical;
            set
            {
                if (value < -1)
                    value = -1;

                _electrical = value;
            }
        }

        private int _fire;
        public int Fire
        {
            get => _fire;
            set
            {
                if (value < -1)
                    value = -1;

                _fire = value;
            }
        }

        private int _negative;
        public int Negative
        {
            get => _negative;
            set
            {
                if (value < -1)
                    value = -1;

                _negative = value;
            }
        }

        private int _positive;
        public int Positive
        {
            get => _positive;
            set
            {
                if (value < -1)
                    value = -1;

                _positive = value;
            }
        }

        private int _sonic;
        public int Sonic
        {
            get => _sonic;
            set
            {
                if (value < -1)
                    value = -1;

                _sonic = value;
            }
        }

        private int _base;
        public int Base
        {
            get => _base;
            set
            {
                if (value < -1)
                    value = -1;

                _base = value;
            }
        }



        public int Total => (Bludgeoning < 0 ? 0 : Bludgeoning) +
                            (Pierce < 0 ? 0: Pierce) +
                            (Slash < 0 ? 0: Slash) +
                            (Magical < 0 ? 0 : Magical) +
                            (Acid < 0 ? 0 : Acid) +
                            (Cold < 0 ? 0 : Cold) +
                            (Divine < 0 ? 0 : Divine) +
                            (Electrical < 0 ? 0 : Electrical) +
                            (Fire < 0 ? 0 : Fire) +
                            (Negative < 0 ? 0 : Negative) +
                            (Positive < 0 ? 0 : Positive) +
                            (Sonic < 0 ? 0 : Sonic) +
                            (Base < 0 ? 0 : Base);

        public void AdjustAllByPercent(float percent)
        {
            Bludgeoning = CalculateAdjustment(Bludgeoning, percent);
            Pierce = CalculateAdjustment(Pierce, percent);
            Slash = CalculateAdjustment(Slash, percent);
            Magical = CalculateAdjustment(Magical, percent);
            Acid = CalculateAdjustment(Acid, percent);
            Cold = CalculateAdjustment(Cold, percent);
            Divine = CalculateAdjustment(Divine, percent);
            Electrical = CalculateAdjustment(Electrical, percent);
            Fire = CalculateAdjustment(Fire, percent);
            Negative = CalculateAdjustment(Negative, percent);
            Positive = CalculateAdjustment(Positive, percent);
            Sonic = CalculateAdjustment(Sonic, percent);
            Base = CalculateAdjustment(Base, percent);
        }

        private static int CalculateAdjustment(int original, float percent)
        {
            bool show = original > -1;
            int output = (int)(original + (original * percent));
            if (original <= 0 && show)
                output = 0;
            else if (original <= 0 && !show)
                output = -1;

            return output;
        }
    }
}
