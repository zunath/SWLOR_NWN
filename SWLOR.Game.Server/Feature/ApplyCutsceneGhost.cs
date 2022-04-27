using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public class ApplyCutsceneGhost
    {
        [NWNEventHandler("mod_enter")]
        public static void ApplyCutsceneGhostToPlayer()
        {
            var player = GetEnteringObject();
            var effect = SupernaturalEffect(EffectCutsceneGhost());
            ApplyEffectToObject(DurationType.Permanent, effect, player);
        }
    }
}
