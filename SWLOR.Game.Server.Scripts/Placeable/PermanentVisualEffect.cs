using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Scripting.Contracts;

namespace SWLOR.Game.Server.Scripts.Placeable
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
            NWPlaceable self = NWGameObject.OBJECT_SELF;

            int vfxID = self.GetLocalInt("PERMANENT_VFX_ID");
            
            if (vfxID > 0)
            {
                _.ApplyEffectToObject(_.DURATION_TYPE_PERMANENT, _.EffectVisualEffect(vfxID), self);
            }

            _.SetEventScript(self, _.EVENT_SCRIPT_PLACEABLE_ON_HEARTBEAT, string.Empty);
        }
    }
}
