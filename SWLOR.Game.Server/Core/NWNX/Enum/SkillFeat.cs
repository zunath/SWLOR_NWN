namespace SWLOR.Game.Server.Core.NWNX.Enum
{
    public class SkillFeat
    {
        public int skill { get; set; }
        public int feat { get; set; }
        public int modifier { get; set; }
        public int focusFeat { get; set; }
        public string classes { get; set; }

        public float classLevelMod { get; set; }
        public int areaFlagsRequired { get; set; }
        public int areaFlagsForbidden { get; set; }
        public int dayOrNight { get; set; }
        public int bypassArmorCheckPenalty { get; set; }
        public int keyAbilityMask { get; set; }
    }
}