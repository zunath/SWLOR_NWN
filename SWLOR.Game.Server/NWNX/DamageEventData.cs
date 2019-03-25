using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.NWNX
{
    public class DamageEventData
    {
        public NWObject Damager { get; set; }
        public int Bludgeoning { get; set; }
        public int Pierce { get; set; }
        public int Slash { get; set; }
        public int Magical { get; set; }
        public int Acid { get; set; }
        public int Cold { get; set; }
        public int Divine { get; set; }
        public int Electrical { get; set; }
        public int Fire { get; set; }
        public int Negative { get; set; }
        public int Positive { get; set; }
        public int Sonic { get; set; }
        public int Base { get; set; }


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
