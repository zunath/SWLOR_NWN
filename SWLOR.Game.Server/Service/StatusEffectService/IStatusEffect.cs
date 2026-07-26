using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
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
        StatusEffectCategory Categories { get; }
        StatusEffectStackType StackingType { get; }
        bool IsFlaggedForRemoval { get; }
        bool WasNaturallyExpired { get; }
        float SecondsSinceNaturalExpiration { get; }
        bool SendsApplicationMessage { get; }
        bool SendsWornOffMessage { get; }
        StatusEffectCleanseType CleanseTypes { get; }
        ResistanceType ResistanceType { get; }
        ResistanceType AppliedResistanceType { get; }
        float Frequency { get; }
        int DurationTicks { get; }
        bool PersistsOnLogout { get; }
        StatGroup StatGroup { get; }
        List<Type> MorePowerfulEffectTypes { get; }
        List<Type> LessPowerfulEffectTypes { get; }
        IStatusEffect Clone();
        string CanApply(uint creature);
        void AssignResistanceType(ResistanceType type);
        void ApplyEffect(uint source, uint creature, int durationTicks);
        void ReassignSource(uint source);
        void ExtendDurationTicks(int ticks);
        void SetDurationTicks(int ticks);
        void ReapplyEffect(uint creature);
        void RemoveNativeEffects(uint creature);
        void RemoveEffect(uint creature, bool isReplacement = false);
        void TickEffect(uint creature);
        void ReconcileElapsedTime(DateTime currentTime);
        void OnHitEffect(uint creature, uint target, int damage);
        void OnDamageDealtEffect(
            uint attacker,
            uint defender,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct);
        void OnDamageTakenEffect(
            uint defender,
            uint attacker,
            int damage,
            CombatDamageType damageType,
            CombatDamageDeliveryType deliveryType = CombatDamageDeliveryType.Direct);
    }
}
