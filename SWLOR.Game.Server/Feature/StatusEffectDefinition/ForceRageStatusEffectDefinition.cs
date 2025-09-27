using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class ForceRageStatusEffectDefinition : IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            ForceRage1();
            ForceRage2();

            return _builder.Build();
        }

        private void ForceRage1()
        {
            _builder.Create(StatusEffectType.ForceRage1)
                .Name("Force Rage I")
                .EffectIcon(EffectIconType.DamageIncrease)
                .CannotReplace(StatusEffectType.ForceRage2);
        }

        private void ForceRage2()
        {
            _builder.Create(StatusEffectType.ForceRage2)
                .Name("Force Rage II")
                .EffectIcon(EffectIconType.DamageIncrease)
                .Replaces(StatusEffectType.ForceRage1);
        }
    }
}