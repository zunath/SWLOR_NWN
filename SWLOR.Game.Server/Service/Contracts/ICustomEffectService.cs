﻿using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service.Contracts
{
    public interface ICustomEffectService
    {
        void ApplyCustomEffect(NWCreature caster, NWCreature target, CustomEffectType effectType, int ticks, int level, string data);
        void ApplyCustomEffect(NWCreature caster, NWCreature target, int customEffectID, int ticks, int effectiveLevel, string data);
        int CalculateEffectAC(NWCreature creature);
        float CalculateEffectHPBonusPercent(NWCreature creature);
        bool DoesPCHaveCustomEffect(NWPlayer oPC, CustomEffectType customEffectType);
        bool DoesPCHaveCustomEffect(NWPlayer oPC, int customEffectID);
        void OnModuleLoad();
        void OnModuleEnter();
        void RemovePCCustomEffect(NWPlayer oPC, CustomEffectType customEffectType);
        void RemovePCCustomEffect(NWPlayer oPC, int customEffectID);
        int GetCustomEffectLevel(NWCreature creature, CustomEffectType customEffectType);
        void ApplyStance(NWPlayer player, CustomEffectType customEffect, PerkType perkType, int effectiveLevel, string data);
        bool RemoveStance(NWPlayer player, PCCustomEffect stanceEffect = null, bool sendMessage = true);
        CustomEffectType GetCurrentStanceType(NWPlayer player);
        bool DoesPCHaveCustomEffectByCategory(NWPlayer player, CustomEffectCategoryType category);
        ForceSpreadDetails GetForceSpreadDetails(NWPlayer player);
        void SetForceSpreadUses(NWPlayer player, int uses);
    }
}
