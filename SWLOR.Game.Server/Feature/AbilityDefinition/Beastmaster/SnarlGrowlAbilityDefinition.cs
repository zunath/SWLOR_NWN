using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public class SnarlGrowlAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();


        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Snarl();
            Growl();

            return _builder.Build();
        }

        private void Snarl()
        {
            _builder.Create(FeatType.Snarl, PerkType.Snarl)
                .Name("Snarl")
                .Level(1)
                .HasRecastDelay(RecastGroup.SnarlGrowl, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    

                });
        }

        private void Growl()
        {
            _builder.Create(FeatType.Growl, PerkType.Growl)
                .Name("Growl")
                .Level(1)
                .HasRecastDelay(RecastGroup.SnarlGrowl, 30f)
                .HasActivationDelay(1f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {


                });
        }
    }
}
