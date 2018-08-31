using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWN.NWScript;

namespace SWLOR.Game.Server.CustomEffect
{
    public class ChainspellEffect: ICustomEffect
    {
        private readonly INWScript _;

        public ChainspellEffect(INWScript script)
        {
            _ = script;
        }

        public void Apply(NWCreature oCaster, NWObject oTarget)
        {
        }

        public void Tick(NWCreature oCaster, NWObject oTarget)
        {
            _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, _.EffectVisualEffect(NWScript.VFX_IMP_EVIL_HELP), oTarget.Object, 6.1f);
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget)
        {
        }
    }
}
