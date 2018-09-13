using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;

using NWN;

namespace SWLOR.Game.Server.CustomEffect
{
    public class ChainspellEffect: ICustomEffect
    {
        private readonly INWScript _;

        public ChainspellEffect(INWScript script)
        {
            _ = script;
        }

        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
            _.ApplyEffectToObject(NWScript.DURATION_TYPE_TEMPORARY, _.EffectVisualEffect(NWScript.VFX_IMP_EVIL_HELP), oTarget.Object, 1.0f);
        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
        }
    }
}
