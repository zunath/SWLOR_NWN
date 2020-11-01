using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Legacy.GameObject;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable
{
    public class PermanentVisualEffect: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlaceable self = NWScript.OBJECT_SELF;

            var vfxID = (VisualEffect)self.GetLocalInt("PERMANENT_VFX_ID");
            
            if (vfxID > 0)
            {
                NWScript.ApplyEffectToObject(DurationType.Permanent, NWScript.EffectVisualEffect(vfxID), self);
            }

            NWScript.SetEventScript(self, EventScript.Placeable_OnHeartbeat, string.Empty);
        }
    }
}
