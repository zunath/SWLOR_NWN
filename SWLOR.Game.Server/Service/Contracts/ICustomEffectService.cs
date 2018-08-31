using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ICustomEffectService
    {
        void ApplyCustomEffect(NWCreature caster, NWCreature target, CustomEffectType effectType, int ticks, int level);
        void ApplyCustomEffect(NWCreature oCaster, NWCreature oTarget, int customEffectID, int ticks, int effectLevel);
        int CalculateEffectAC(NWCreature creature);
        bool DoesPCHaveCustomEffect(NWPlayer oPC, int customEffectID);
        bool DoesPCHaveCustomEffect(NWPlayer oPC, CustomEffectType customEffectType);
        int GetActiveEffectLevel(NWObject target, CustomEffectType effectType);
        int GetActiveEffectLevel(NWObject oTarget, int customEffectID);
        void OnModuleHeartbeat();
        void OnPlayerHeartbeat(NWPlayer oPC);
        void RemovePCCustomEffect(NWPlayer oPC, long customEffectID);
        void RemovePCCustomEffect(NWPlayer oPC, CustomEffectType customEffectType);
    }
}
