using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ICustomEffectService
    {
        void ApplyCustomEffect(NWCreature caster, NWCreature target, CustomEffectType effectType, int ticks, int level, string data);
        void ApplyCustomEffect(NWCreature caster, NWCreature target, int customEffectID, int ticks, int effectiveLevel, string data);
        int CalculateEffectAC(NWCreature creature);
        bool DoesPCHaveCustomEffect(NWPlayer oPC, CustomEffectType customEffectType);
        bool DoesPCHaveCustomEffect(NWPlayer oPC, int customEffectID);
        void OnModuleLoad();
        void RemovePCCustomEffect(NWPlayer oPC, CustomEffectType customEffectType);
        void RemovePCCustomEffect(NWPlayer oPC, long customEffectID);
        int GetEffectiveLevelOfPCCustomEffect(NWPlayer player, CustomEffectType customEffectType);
    }
}
