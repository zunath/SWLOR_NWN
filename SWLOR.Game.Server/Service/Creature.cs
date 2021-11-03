using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using System;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Service
{
    public static class Creature
    {
        [NWNEventHandler("crea_death")]
        public static void OnDeath()
        {
            var creature = Internal.OBJECT_SELF;

            // apply death VFX
            var vfx = (VisualEffect)GetLocalInt(creature, "DEATH_VFX_ID");
            Console.WriteLine(vfx);

            // technically 0 could be valid, but we can't differentiate between err and blur here; no blurring on death allowed
            if (vfx != 0)
            {
                var effect = EffectVisualEffect(vfx);
                ApplyEffectToObject(Core.NWScript.Enum.DurationType.Instant, effect, creature);
            }
        }
    }
}
