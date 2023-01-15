using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.BeastMastery
{
    public class TameAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();


        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Tame();

            return _builder.Build();
        }

        private void Tame()
        {
            _builder.Create(FeatType.Tame, PerkType.Tame)
                .Name("Tame")
                .Level(1)
                .HasRecastDelay(RecastGroup.Tame, 60f * 10f)
                .HasActivationDelay(18f)
                .RequirementStamina(10)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var tameLevel = Perk.GetEffectivePerkLevel(activator, PerkType.Tame);

                });
        }
    }
}
