using NWN;
using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.CustomEffect
{
    public class AcidCellEffect: ICustomEffect
    {
        private readonly INWScript _;
        private readonly IRandomService _random;

        public AcidCellEffect(
            INWScript script,
            IRandomService random)
        {
            _ = script;
            _random = random;
        }

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            oCaster.SendMessage("An acid cell lands on your target.");
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
            if (currentTick % 2 != 0) return;
            int damage = _random.D4(1);
            oCaster.AssignCommand(() =>
            {
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectDamage(damage, DAMAGE_TYPE_ACID), oTarget);
            });
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
        }
    }
}
