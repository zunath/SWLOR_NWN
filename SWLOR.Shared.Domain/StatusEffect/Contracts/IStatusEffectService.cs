using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Shared.Domain.StatusEffect.Contracts;

public interface IStatusEffectService
{
    StatGroup GetCreatureStatGroup(uint creature);

    void ApplyPermanentStatusEffect(uint source, uint creature, StatusEffectType type);

    void ApplyStatusEffect(uint source, uint creature, StatusEffectType type, int durationTicks);

    void RemoveStatusEffect(uint creature, StatusEffectType type);
    void RemoveStatusEffectBySourceType(uint creature, StatusEffectSourceType sourceType);
    bool HasEffect(uint creature, StatusEffectType type);
}