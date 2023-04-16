using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class BeastStatusEffectDefinition: IStatusEffectListDefinition
    {
        private readonly StatusEffectBuilder _builder = new();

        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            Assault();
            BolsterArmor();
            BolsterAttack();

            return _builder.Build();
        }

        private void Assault()
        {
            _builder.Create(StatusEffectType.Assault)
                .Name("Assault")
                .EffectIcon(EffectIconType.ACIncrease);
        }

        private void BolsterArmor()
        {
            _builder.Create(StatusEffectType.BolsterArmor1)
                .Name("Bolster Armor I")
                .EffectIcon(EffectIconType.DamageImmunityIncrease);

            _builder.Create(StatusEffectType.BolsterArmor2)
                .Name("Bolster Armor II")
                .EffectIcon(EffectIconType.DamageImmunityIncrease);

            _builder.Create(StatusEffectType.BolsterArmor3)
                .Name("Bolster Armor III")
                .EffectIcon(EffectIconType.DamageImmunityIncrease);

            _builder.Create(StatusEffectType.BolsterArmor4)
                .Name("Bolster Armor IV")
                .EffectIcon(EffectIconType.DamageImmunityIncrease);

            _builder.Create(StatusEffectType.BolsterArmor5)
                .Name("Bolster Armor V")
                .EffectIcon(EffectIconType.DamageImmunityIncrease);
        }

        private void BolsterAttack()
        {
            _builder.Create(StatusEffectType.BolsterAttack1)
                .Name("Bolster Attack I")
                .EffectIcon(EffectIconType.DamageIncrease);

            _builder.Create(StatusEffectType.BolsterAttack2)
                .Name("Bolster Attack II")
                .EffectIcon(EffectIconType.DamageIncrease);

            _builder.Create(StatusEffectType.BolsterAttack3)
                .Name("Bolster Attack III")
                .EffectIcon(EffectIconType.DamageIncrease);

            _builder.Create(StatusEffectType.BolsterAttack4)
                .Name("Bolster Attack IV")
                .EffectIcon(EffectIconType.DamageIncrease);

            _builder.Create(StatusEffectType.BolsterAttack5)
                .Name("Bolster Attack V")
                .EffectIcon(EffectIconType.DamageIncrease);
        }
    }
}
