using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Model;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;

namespace SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition
{
    public class AuraStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();
        private readonly IPerkService _perkService;
        private readonly IStatService _statService;

        public AuraStatusEffectDefinition(IPerkService perkService, IStatService statService)
        {
            _perkService = perkService;
            _statService = statService;
        }

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            Charge();
            Dedication();
            FrenziedShout();
            Rejuvenation();
            SoldiersPrecision();
            SoldiersSpeed();
            SoldiersStrike();

            return _builder.Build();
        }

        private void Charge()
        {
            const string EffectTag = "AURA_CHARGE_EFFECT";
            _builder.Create(StatusEffectType.Charge)
                .Name("Charge")
                .EffectIcon(EffectIconType.Charge)
                .GrantAction((source, target, length, data) => 
                {
                    RemoveEffectByTag(target, EffectTag);
                    var effectiveLevel = _perkService.GetPerkLevel(source, PerkType.Charge);
                    Effect effect;

                    switch (effectiveLevel)
                    {
                        case 1:
                            effect = EffectMovementSpeedIncrease(15);
                            effect = TagEffect(effect, EffectTag);
                            ApplyEffectToObject(DurationType.Permanent, effect, target);
                            break;
                        case 2:
                            effect = EffectMovementSpeedIncrease(30);
                            effect = TagEffect(effect, EffectTag);
                            ApplyEffectToObject(DurationType.Permanent, effect, target);
                            break;
                    }
                    
                    _statService.ApplyPlayerMovementRate(target);
                })
                .RemoveAction((target, data) =>
                {
                    RemoveEffectByTag(target, EffectTag);

                    // We have to put this on a delay because the RemoveEffect() call above does not
                    // actually remove the effect until after the script ends. This throws off the calculations happening in Stat.ApplyPlayerMovementRate.
                    DelayCommand(0.1f, () =>
                    {
                        _statService.ApplyPlayerMovementRate(target);
                    });
                });
        }

        private void Dedication()
        {
            _builder.Create(StatusEffectType.Dedication)
                .Name("Dedication")
                .EffectIcon(EffectIconType.Dedication);
        }

        private void FrenziedShout()
        {
            _builder.Create(StatusEffectType.FrenziedShout)
                .Name("Frenzied Shout")
                .EffectIcon(EffectIconType.FrenziedShout);
        }

        private void Rejuvenation()
        {
            _builder.Create(StatusEffectType.Rejuvenation)
                .Name("Rejuvenation")
                .EffectIcon(EffectIconType.Rejuvenation)
                .TickAction((source, target, data) =>
                {
                    var level = _perkService.GetPerkLevel(source, PerkType.Rejuvenation);
                    _statService.RestoreStamina(target, level);
                });
        }

        private void SoldiersPrecision()
        {
            _builder.Create(StatusEffectType.SoldiersPrecision)
                .Name("Soldier's Precision")
                .EffectIcon(EffectIconType.SoldiersPrecision);
        }

        private void SoldiersSpeed()
        {
            _builder.Create(StatusEffectType.SoldiersSpeed)
                .Name("Soldier's Speed")
                .EffectIcon(EffectIconType.SoldiersSpeed);
        }

        private void SoldiersStrike()
        {
            _builder.Create(StatusEffectType.SoldiersStrike)
                .Name("Soldier's Strike")
                .EffectIcon(EffectIconType.SoldiersStrike);
        }
    }
}
