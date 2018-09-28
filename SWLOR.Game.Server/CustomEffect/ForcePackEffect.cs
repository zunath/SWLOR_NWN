using System;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.CustomEffect
{
    public class ForcePackEffect: ICustomEffect
    {
        private readonly IAbilityService _ability;

        public ForcePackEffect(IAbilityService ability)
        {
            _ability = ability;
        }

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
            string[] split = data.Split(',');
            int interval = Convert.ToInt32(split[0]);
            int amount = Convert.ToInt32(split[1]);

            if (currentTick % interval != 0) return;

            _ability.RestoreFP(oTarget.Object, amount);
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
        }
    }
}
