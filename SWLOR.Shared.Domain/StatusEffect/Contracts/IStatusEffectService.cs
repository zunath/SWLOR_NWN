using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.StatusEffect.Enums;

namespace SWLOR.Shared.Domain.StatusEffect.Contracts;

public interface IStatusEffectService
{
    StatGroup GetCreatureStatGroup(uint creature);

    void ApplyPermanentStatusEffect<T>(uint source, uint creature)
        where T: IStatusEffect;

    void ApplyPermanentStatusEffect(Type type, uint source, uint creature);

    void ApplyStatusEffect<T>(uint source, uint creature, int durationTicks)
        where T: IStatusEffect;

    void RemoveStatusEffect<T>(uint creature)
        where T: IStatusEffect;

    void RemoveStatusEffect(Type type, uint creature);
    void RemoveStatusEffectBySourceType(uint creature, StatusEffectSourceType sourceType);
    bool HasEffect(Type type, uint creature);

    bool HasEffect<T>(uint creature)
        where T : IStatusEffect;
}