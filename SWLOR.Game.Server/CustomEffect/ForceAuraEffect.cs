using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.CustomEffect
{
    public class ForceAuraEffect: ICustomEffect
    {
        

        public ForceAuraEffect()
        {
            
        }

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            PlayerStatService.ApplyStatChanges(oTarget.Object, null);

            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
            PlayerStatService.ApplyStatChanges(oTarget.Object, null);
        }
    }
}
