using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.CustomEffect
{
    public class ForceAuraEffect: ICustomEffect
    {
        private readonly IPlayerStatService _stat;

        public ForceAuraEffect(IPlayerStatService stat)
        {
            _stat = stat;
        }

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            _stat.ApplyStatChanges(oTarget.Object, null);

            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
            _stat.ApplyStatChanges(oTarget.Object, null);
        }
    }
}
