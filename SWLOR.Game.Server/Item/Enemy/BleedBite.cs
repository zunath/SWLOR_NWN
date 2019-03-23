using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Item.Enemy
{
    public class BleedBite: IRegisteredEvent
    {
        
        
        

        public BleedBite(
            
            
            )
        {
            
            
            
        }

        public bool Run(params object[] args)
        {
            NWCreature oTarget = _.GetSpellTargetObject();

            if (RandomService.D100(1) > 5) return false;
            
            CustomEffectService.ApplyCustomEffect(Object.OBJECT_SELF, oTarget, CustomEffectType.Bleeding, 12, 1, null);
            return true;
        }
    }
}
