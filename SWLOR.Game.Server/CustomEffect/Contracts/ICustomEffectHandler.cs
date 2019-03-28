using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.CustomEffect.Contracts
{
    public interface ICustomEffectHandler
    {
        CustomEffectCategoryType CustomEffectCategoryType { get; }
        CustomEffectType CustomEffectType { get; }
        string Apply(NWCreature oCaster, NWObject oTarget, int effectiveLevel);
        void Tick(NWCreature oCaster, NWObject oTarget, int currentTick, int effectiveLevel, string data);
        void WearOff(NWCreature oCaster, NWObject oTarget, int effectiveLevel, string data);
        string StartMessage { get; }
        string ContinueMessage { get; }
        string WornOffMessage { get; }
    }
}
