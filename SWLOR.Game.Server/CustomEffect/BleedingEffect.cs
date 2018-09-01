using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;

using NWN;

namespace SWLOR.Game.Server.CustomEffect
{
    public class BleedingEffect: ICustomEffect
    {
        private readonly INWScript _;

        public BleedingEffect(INWScript script)
        {
            _ = script;
        }

        public void Apply(NWCreature oCaster, NWObject oTarget)
        {
        }

        public void Tick(NWCreature oCaster, NWObject oTarget)
        {
            Location location = oTarget.Location;
            NWPlaceable oBlood = NWPlaceable.Wrap(_.CreateObject(NWScript.OBJECT_TYPE_PLACEABLE, "plc_bloodstain", location));
            oBlood.Destroy(48.0f);

            oCaster.AssignCommand(() =>
            {
                Effect damage = _.EffectDamage(1);
                _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, damage, oTarget.Object);
            });
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget)
        {
        }
    }
}
