using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.BeastMastery
{
    public class RewardAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();


        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Reward1();
            Reward2();
            Reward3();

            return _builder.Build();
        }

        private void Reward1()
        {
            _builder.Create(FeatType.Reward1, PerkType.Reward)
                .Name("Reward I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Reward, 60f)
                .HasActivationDelay(1f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {

                });
        }

        private void Reward2()
        {
            _builder.Create(FeatType.Reward2, PerkType.Reward)
                .Name("Reward II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Reward, 60f)
                .HasActivationDelay(1f)
                .RequirementStamina(8)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {

                });
        }

        private void Reward3()
        {
            _builder.Create(FeatType.Reward3, PerkType.Reward)
                .Name("Reward III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Reward, 60f)
                .HasActivationDelay(1f)
                .RequirementStamina(10)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {

                });
        }
    }
}