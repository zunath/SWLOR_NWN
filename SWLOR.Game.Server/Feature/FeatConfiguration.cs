using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    public static class FeatConfiguration
    {
        /// <summary>
        /// When the module loads, configure all custom feats.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void ConfigureFeats()
        {
            Feat.SetFeatModifier(FeatType.ShieldConcealment1, FeatModifierType.Concealment, 5);
            Feat.SetFeatModifier(FeatType.ShieldConcealment2, FeatModifierType.Concealment, 10);
            Feat.SetFeatModifier(FeatType.ShieldConcealment3, FeatModifierType.Concealment, 15);
            Feat.SetFeatModifier(FeatType.ShieldConcealment4, FeatModifierType.Concealment, 20);
            Feat.SetFeatModifier(FeatType.ShieldConcealment5, FeatModifierType.Concealment, 25);
        }
    }
}
