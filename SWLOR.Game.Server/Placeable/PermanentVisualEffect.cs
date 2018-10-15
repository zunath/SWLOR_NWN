using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Placeable
{
    public class PermanentVisualEffect: IRegisteredEvent
    {
        private readonly INWScript _;

        public PermanentVisualEffect(INWScript script)
        {
            _ = script;
        }

        public bool Run(params object[] args)
        {
            NWPlaceable self = Object.OBJECT_SELF;

            int vfxID = self.GetLocalInt("PERMANENT_VFX_ID");
            
            if (vfxID > 0)
            {
                _.ApplyEffectToObject(DURATION_TYPE_PERMANENT, _.EffectVisualEffect(vfxID), self);
            }

            _.SetEventScript(self, EVENT_SCRIPT_PLACEABLE_ON_HEARTBEAT, string.Empty);

            return true;
        }
    }
}
