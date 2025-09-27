using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Combat.ValueObjects;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Domain.StatusEffect.ValueObjects;

namespace SWLOR.Component.StatusEffect.Feature.StatusEffectDefinition
{
    public class ItemStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly IStatService _statService;

        public ItemStatusEffectDefinition(IStatService statService)
        {
            _statService = statService;
        }
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            ForcePackEffect(builder);
            
            return builder.Build();
        }

        private void ForcePackEffect(StatusEffectBuilder builder)
        {
            void CreateEffect(string name, int amount)
            {
                builder.Create(StatusEffectType.ForcePack1)
                    .Name(name)
                    .EffectIcon(EffectIconType.Regenerate)
                    .TickAction((source, target, effectData) =>
                    {
                        _statService.RestoreFP(target, amount);
                    });
            }

            CreateEffect("Force Pack I", 2);
            CreateEffect("Force Pack II", 3);
            CreateEffect("Force Pack III", 4);
            CreateEffect("Force Pack IV", 5);
            CreateEffect("Force Pack V", 6);
        }
    }
}
