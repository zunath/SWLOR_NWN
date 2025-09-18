using System;
using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public interface IStatusEffect
    {
        string Id { get; }
        uint Source { get; }
        StatusEffectActivationType ActivationType { get; }
        StatusEffectSourceType SourceType { get; }
        string Name { get; }
        EffectIconType Icon { get; }
        StatusEffectStackType StackingType { get; }
        bool IsFlaggedForRemoval { get; }
        bool SendsApplicationMessage { get; }
        bool SendsWornOffMessage { get; }
        bool IsRemovedOnJobChange { get; }
        float Frequency { get; }
        StatGroup StatGroup { get; }
        List<Type> MorePowerfulEffectTypes { get; }
        List<Type> LessPowerfulEffectTypes { get; }
        string CanApply(uint creature);
        void ApplyEffect(uint source, uint creature, int durationTicks);
        void RemoveEffect(uint creature);
        void TickEffect(uint creature);
        void OnHitEffect(uint creature, uint target, int damage);
    }
}
