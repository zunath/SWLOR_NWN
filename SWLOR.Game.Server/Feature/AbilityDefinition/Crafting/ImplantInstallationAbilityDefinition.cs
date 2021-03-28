using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Crafting
{
    public class ImplantInstallationAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<Feat, AbilityDetail> BuildAbilities()
        {
            ImplantInstallation1();
            ImplantInstallation2();
            ImplantInstallation3();
            ImplantInstallation4();
            ImplantInstallation5();

            return _builder.Build();
        }

        private void ImplantInstallation1()
        {
            _builder.Create(Feat.ImplantInstallation1, PerkType.ImplantInstallation)
                .Name("Implant Installation I")
                .HasRecastDelay(RecastGroup.ImplantInstallation, 60f)
                .HasActivationDelay(6f)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level) =>
                {
                    StatusEffect.Apply(activator, target, StatusEffectType.ImplantInstallation1, 1800f);
                });
        }
        private void ImplantInstallation2()
        {
            _builder.Create(Feat.ImplantInstallation1, PerkType.ImplantInstallation)
                .Name("Implant Installation II")
                .HasRecastDelay(RecastGroup.ImplantInstallation, 60f)
                .HasActivationDelay(6f)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level) =>
                {
                    StatusEffect.Apply(activator, target, StatusEffectType.ImplantInstallation2, 1800f);
                });
        }
        private void ImplantInstallation3()
        {
            _builder.Create(Feat.ImplantInstallation1, PerkType.ImplantInstallation)
                .Name("Implant Installation III")
                .HasRecastDelay(RecastGroup.ImplantInstallation, 60f)
                .HasActivationDelay(6f)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level) =>
                {
                    StatusEffect.Apply(activator, target, StatusEffectType.ImplantInstallation3, 1800f);
                });
        }
        private void ImplantInstallation4()
        {
            _builder.Create(Feat.ImplantInstallation1, PerkType.ImplantInstallation)
                .Name("Implant Installation IV")
                .HasRecastDelay(RecastGroup.ImplantInstallation, 60f)
                .HasActivationDelay(6f)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level) =>
                {
                    StatusEffect.Apply(activator, target, StatusEffectType.ImplantInstallation4, 1800f);
                });
        }
        private void ImplantInstallation5()
        {
            _builder.Create(Feat.ImplantInstallation1, PerkType.ImplantInstallation)
                .Name("Implant Installation V")
                .HasRecastDelay(RecastGroup.ImplantInstallation, 60f)
                .HasActivationDelay(6f)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level) =>
                {
                    StatusEffect.Apply(activator, target, StatusEffectType.ImplantInstallation5, 1800f);
                });
        }

    }
}
