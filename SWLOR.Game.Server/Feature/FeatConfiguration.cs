
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Event;

namespace SWLOR.Game.Server.Feature
{
    public static class FeatConfiguration
    {
        /// <summary>
        /// When the module loads, configure all custom feats.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleLoad)]
        public static void ConfigureFeats()
        {
            FeatPlugin.SetFeatModifier(FeatType.ShieldConcealment1, FeatModifierType.Concealment, 5);
            FeatPlugin.SetFeatModifier(FeatType.ShieldConcealment2, FeatModifierType.Concealment, 10);
            FeatPlugin.SetFeatModifier(FeatType.ShieldConcealment3, FeatModifierType.Concealment, 15);
            FeatPlugin.SetFeatModifier(FeatType.ShieldConcealment4, FeatModifierType.Concealment, 20);
            FeatPlugin.SetFeatModifier(FeatType.ShieldConcealment5, FeatModifierType.Concealment, 25);
        }
    }
}
