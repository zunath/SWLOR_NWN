using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.CustomEffect.Contracts
{
    public interface ICustomEffect
    {
        string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel);
        void Tick(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data);
        void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data);
    }
}
