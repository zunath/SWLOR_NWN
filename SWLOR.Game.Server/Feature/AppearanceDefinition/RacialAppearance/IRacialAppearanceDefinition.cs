namespace SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance
{
    public interface IRacialAppearanceDefinition
    {
        float MaximumScale { get; }
        float MinimumScale { get; }
        int[] MaleHeads { get; }
        int[] FemaleHeads { get; }

        int[] Torsos { get; } 
        int[] Pelvis { get; }
        int[] RightBicep { get; }
        int[] RightForearm { get; }
        int[] RightHand { get; }
        int[] RightThigh { get; }
        int[] RightShin { get; }
        int[] RightFoot { get; }
        int[] LeftBicep { get; }
        int[] LeftForearm { get; }
        int[] LeftHand { get; } 
        int[] LeftThigh { get; }
        int[] LeftShin { get; }
        int[] LeftFoot { get; }
    }
}
