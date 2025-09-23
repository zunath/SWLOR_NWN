using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Component.Character.Model
{

    public class RacialAppearance
    {
        public int HeadId { get; set; } = 1;
        public int SkinColorId { get; set; } = 2;
        public int HairColorId { get; set; }
        public AppearanceType AppearanceType { get; set; } = AppearanceType.Human;
        public float Scale { get; set; } = 1.0f;

        public int NeckId { get; set; } = 1;
        public int TorsoId { get; set; } = 1;
        public int PelvisId { get; set; } = 1;

        public int RightBicepId { get; set; } = 1;
        public int RightForearmId { get; set; } = 1;
        public int RightHandId { get; set; } = 1;
        public int RightThighId { get; set; } = 1;
        public int RightShinId { get; set; } = 1;
        public int RightFootId { get; set; } = 1;

        public int LeftBicepId { get; set; } = 1;
        public int LeftForearmId { get; set; } = 1;
        public int LeftHandId { get; set; } = 1;
        public int LeftThighId { get; set; } = 1;
        public int LeftShinId { get; set; } = 1;
        public int LeftFootId { get; set; } = 1;
    }
}
