using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Item.Enemy
{
    public class BleedBite: IRegisteredEvent
    {
        
        private readonly IRandomService _random;
        private readonly ICustomEffectService _customEffect;

        public BleedBite(
            
            IRandomService random,
            ICustomEffectService customEffect)
        {
            
            _random = random;
            _customEffect = customEffect;
        }

        public bool Run(params object[] args)
        {
            NWCreature oTarget = _.GetSpellTargetObject();

            if (_random.D100(1) > 5) return false;
            
            _customEffect.ApplyCustomEffect(Object.OBJECT_SELF, oTarget, CustomEffectType.Bleeding, 12, 1, null);
            return true;
        }
    }
}
