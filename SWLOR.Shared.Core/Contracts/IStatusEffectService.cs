using System;
using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IStatusEffectService
    {
        void CacheStatusEffects();
        void Apply(uint source, uint target, StatusEffectType statusEffectType, int duration, uint effectSource = OBJECT_INVALID, bool sendApplicationMessage = true);
        void PlayerEnter();
        void PlayerExit();
        void TickStatusEffects();
        void OnPlayerDeath();
        void Remove(uint creature, StatusEffectType statusEffectType, bool showMessage = true);
        void RemoveAll(uint creature);
        bool HasStatusEffect(uint creature, params StatusEffectType[] statusEffectTypes);
        StatusEffectDetail GetDetail(StatusEffectType type);
        T GetEffectData<T>(uint creature, StatusEffectType effectType);
        int GetEffectDuration(uint creature, params StatusEffectType[] effectTypes);
        EffectTypeScript GetEffectTypeFromIcon(EffectIconType effectIcon);
        List<StatusEffectType> GetStatusEffectTypesFromIcon(EffectIconType effectIcon);
        AbilityType GetAbilityTypeBuffed(EffectIconType effectIcon);
    }
}
