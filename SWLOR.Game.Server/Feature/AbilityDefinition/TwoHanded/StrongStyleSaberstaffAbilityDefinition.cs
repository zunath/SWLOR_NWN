using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwoHanded
{
    public class StrongStyleSaberstaffAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            StrongStyleSaberstaff();

            return _builder.Build();
        }

        private void DoToggle(uint activator, AbilityToggleType type)
        {
            var isToggled = Ability.IsAbilityToggled(activator, type);
            Ability.ToggleAbility(activator, type, !isToggled);
        }

        private void StrongStyleSaberstaff()
        {
            _builder.Create(FeatType.StrongStyleSaberstaff, PerkType.StrongStyleSaberstaff)
                .Name("Strong Style (Saberstaff)")
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    switch (level)
                    {
                        case 1:
                            DoToggle(activator, AbilityToggleType.StrongStyleSaberstaff1);
                            break;
                        case 2:
                            DoToggle(activator, AbilityToggleType.StrongStyleSaberstaff2);
                            break;
                        case 3:
                            DoToggle(activator, AbilityToggleType.StrongStyleSaberstaff3);
                            break;
                        case 4:
                            DoToggle(activator, AbilityToggleType.StrongStyleSaberstaff4);
                            break;
                        case 5:
                            DoToggle(activator, AbilityToggleType.StrongStyleSaberstaff5);
                            break;
                    }

                });
        }
    }
}
