using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.CustomEffect.Contracts
{
    public interface ICustomEffect
    {
        void Apply(NWCreature oCaster, NWObject oTarget);
        void Tick(NWCreature oCaster, NWObject oTarget);
        void WearOff(NWCreature oCaster, NWObject oTarget);
    }
}
