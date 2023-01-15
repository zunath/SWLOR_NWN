using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.BeastMastery
{
    public class SoothePetAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();


        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            SoothePet();

            return _builder.Build();
        }

        private void SoothePet()
        {
            _builder.Create(FeatType.SoothePet, PerkType.SoothePet)
                .Name("Soothe Pet")
                .Level(1)
                .HasRecastDelay(RecastGroup.Tame, 60f * 3)
                .HasActivationDelay(1f)
                .RequirementStamina(2)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {

                });
        }
    }
}