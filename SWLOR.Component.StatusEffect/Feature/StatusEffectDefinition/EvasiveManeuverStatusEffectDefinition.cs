using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Combat.ValueObjects;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.ValueObjects;

namespace SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition
{
    internal class EvasiveManeuverStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            EvasiveManeuver1();
            EvasiveManeuver2();
            EvasiveManeuver3();
            EvasiveManeuver4();
            EvasiveManeuver5();

            return _builder.Build();
        }

        private void EvasiveManeuver1()
        {
            _builder.Create(StatusEffectType.EvasiveManeuver1)
                .Name("Evasive Maneuver I")
                .EffectIcon(EffectIconType.ACIncrease)
                .CannotReplace(
                    StatusEffectType.EvasiveManeuver2,
                    StatusEffectType.EvasiveManeuver3,
                    StatusEffectType.EvasiveManeuver4,
                    StatusEffectType.EvasiveManeuver5);
        }

        private void EvasiveManeuver2()
        {
            _builder.Create(StatusEffectType.EvasiveManeuver2)
                .Name("Evasive Maneuver II")
                .EffectIcon(EffectIconType.ACIncrease)
                .CannotReplace(
                    StatusEffectType.EvasiveManeuver3,
                    StatusEffectType.EvasiveManeuver4,
                    StatusEffectType.EvasiveManeuver5)
                .Replaces(
                    StatusEffectType.EvasiveManeuver1);
        }

        private void EvasiveManeuver3()
        {
            _builder.Create(StatusEffectType.EvasiveManeuver3)
                .Name("Evasive Maneuver III")
                .EffectIcon(EffectIconType.ACIncrease)
                .CannotReplace(
                    StatusEffectType.EvasiveManeuver4,
                    StatusEffectType.EvasiveManeuver5)
                .Replaces(
                    StatusEffectType.EvasiveManeuver1,
                    StatusEffectType.EvasiveManeuver2);
        }

        private void EvasiveManeuver4()
        {
            _builder.Create(StatusEffectType.EvasiveManeuver4)
                .Name("Evasive Maneuver IV")
                .EffectIcon(EffectIconType.ACIncrease)
                .CannotReplace(
                    StatusEffectType.EvasiveManeuver5)
                .Replaces(
                    StatusEffectType.EvasiveManeuver1,
                    StatusEffectType.EvasiveManeuver2,
                    StatusEffectType.EvasiveManeuver3);
        }
        private void EvasiveManeuver5()
        {
            _builder.Create(StatusEffectType.EvasiveManeuver5)
                .Name("Evasive Maneuver V")
                .EffectIcon(EffectIconType.ACIncrease)
                .Replaces(
                    StatusEffectType.EvasiveManeuver1,
                    StatusEffectType.EvasiveManeuver2,
                    StatusEffectType.EvasiveManeuver3,
                    StatusEffectType.EvasiveManeuver4);
        }
    }
}
