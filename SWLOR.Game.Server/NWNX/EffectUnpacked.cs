
using NWN;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.NWNX
{
    public class EffectUnpacked
    {
        public int EffectID { get; set; }
        public int Type { get; set; }
        public int SubType { get; set; }

        public float Duration { get; set; }
        public int ExpiryCalendarDay { get; set; }
        public int ExpiryTimeOfDay { get; set; }

        public NWObject Creator { get; set; }
        public int SpellID { get; set; }
        public int Expose { get; set; }
        public int ShowIcon { get; set; }
        public int CasterLevel { get; set; }

        public Effect LinkLeft { get; set; }
        public int LinkLeftValid { get; set; }
        public Effect LinkRight { get; set; }
        public int LinkRightValid { get; set; }

        public int NumIntegers { get; set; }
        public int nParam0 { get; set; }
        public int nParam1 { get; set; }
        public int nParam2 { get; set; }
        public int nParam3 { get; set; }
        public int nParam4 { get; set; }
        public int nParam5 { get; set; }
        public int nParam6 { get; set; }
        public int nParam7 { get; set; }
        public float fParam0 { get; set; }
        public float fParam1 { get; set; }
        public float fParam2 { get; set; }
        public float fParam3 { get; set; }
        public string sParam0 { get; set; }
        public string sParam1 { get; set; }
        public string sParam2 { get; set; }
        public string sParam3 { get; set; }
        public string sParam4 { get; set; }
        public string sParam5 { get; set; }
        public NWObject oParam0 { get; set; }
        public NWObject oParam1 { get; set; }
        public NWObject oParam2 { get; set; }
        public NWObject oParam3 { get; set; }

        public string Tag { get; set; }
    }
}
