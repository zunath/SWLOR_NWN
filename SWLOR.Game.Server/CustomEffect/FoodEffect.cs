using SWLOR.Game.Server.CustomEffect.Contracts;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.CustomEffect
{
    public class FoodEffect: ICustomEffect
    {
        public string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel)
        {
            return null;
        }

        public void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data)
        {
            string[] split = data.Split(',');


        }

        public void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data)
        {
        }
    }
}
