namespace SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance
{
    public abstract class RacialAppearanceBaseDefinition: IRacialAppearanceDefinition
    {
        public virtual float MaximumScale => 1.15f;
        public virtual float MinimumScale => 0.85f;
        public abstract int[] MaleHeads { get; }
        public abstract int[] FemaleHeads { get; }
        public virtual int[] Torsos { get; } = { 1, 2, 166 };
        public virtual int[] Pelvis { get; } = { 1, 2, 11, 158 };
        public virtual int[] RightBicep { get; } = { 1, 2 };
        public virtual int[] RightForearm { get; } = { 1, 2, 152 };
        public virtual int[] RightHand { get; } = { 1, 2, 5, 6, 63, 100, 110, 113, 121, 151 };
        public virtual int[] RightThigh { get; } = { 1, 2, 154 };
        public virtual int[] RightShin { get; } = { 1, 2 };
        public virtual int[] RightFoot { get; } = { 1 };
        public virtual int[] LeftBicep { get; } = { 1, 2 };
        public virtual int[] LeftForearm { get; } = { 1, 2, 152 };
        public virtual int[] LeftHand { get; } = { 1, 2, 5, 6, 63, 100, 110, 113, 121, 151 };
        public virtual int[] LeftThigh { get; } = { 1, 2, 154 };
        public virtual int[] LeftShin { get; } = { 1, 2 };
        public virtual int[] LeftFoot { get; } = { 1 };
    }
}
