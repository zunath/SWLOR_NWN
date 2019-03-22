using System;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.CustomEffect
{
    public class BleedingEffect: ICustomEffect
    {
        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
            if (currentTick % 2 == 0) return;

            Location location = oTarget.Location;
            NWPlaceable oBlood = (_.CreateObject(_.OBJECT_TYPE_PLACEABLE, "plc_bloodstain", location));
            oBlood.Destroy(48.0f);

            int amount = 1;

            if (!string.IsNullOrWhiteSpace(data))
            {
                amount = Convert.ToInt32(data);
            }

            oTarget.SetLocalInt(AbilityService.LAST_ATTACK + oCaster.GlobalID, AbilityService.ATTACK_DOT);

            oCaster.AssignCommand(() =>
            {
                Effect damage = _.EffectDamage(amount);
                _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, damage, oTarget.Object);
            });
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
        }
    }
}
