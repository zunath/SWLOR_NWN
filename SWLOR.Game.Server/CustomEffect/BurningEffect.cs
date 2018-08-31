using System;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;

namespace SWLOR.Game.Server.CustomEffect
{
    public class BurningEffect: ICustomEffect
    {
        private readonly INWScript _;

        public BurningEffect(INWScript script)
        {
            _ = script;
        }

        public void Apply(NWCreature oCaster, NWObject oTarget)
        {
        }

        public void Tick(NWCreature oCaster, NWObject oTarget)
        {
            Random random = new Random();
            int amount = random.Next(1, 2);
            oCaster.AssignCommand(() =>
            {
                Effect damage = _.EffectDamage(amount, NWScript.DAMAGE_TYPE_FIRE);
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, damage, oTarget.Object);
            });

            Effect vfx = _.EffectVisualEffect(NWScript.VFX_COM_HIT_FIRE);
            _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, vfx, oTarget.Object);
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget)
        {
        }
    }
}
