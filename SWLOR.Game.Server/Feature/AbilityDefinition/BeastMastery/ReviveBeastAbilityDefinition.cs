using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.BeastMastery
{
    public class ReviveBeastAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();


        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            ReviveBeast1();
            ReviveBeast2();
            ReviveBeast3();

            return _builder.Build();
        }

        private void ReviveBeast1()
        {
            _builder.Create(FeatType.ReviveBeast1, PerkType.ReviveBeast)
                .Name("Revive Beast I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ReviveBeast, 60f * 5)
                .HasActivationDelay(4f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {

                });
        }

        private void ReviveBeast2()
        {
            _builder.Create(FeatType.ReviveBeast2, PerkType.ReviveBeast)
                .Name("Revive Beast II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ReviveBeast, 60f * 5)
                .HasActivationDelay(4f)
                .RequirementStamina(7)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {

                });
        }

        private void ReviveBeast3()
        {
            _builder.Create(FeatType.ReviveBeast3, PerkType.ReviveBeast)
                .Name("Revive Beast III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ReviveBeast, 60f * 5)
                .HasActivationDelay(4f)
                .RequirementStamina(9)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {

                });
        }
    }
}