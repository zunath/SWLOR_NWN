using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class SecondWindAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder.Create(FeatType.SecondWind1, PerkType.SecondWind)
                .Name("Second Wind")
                .Level(1)
                .HasActivationDelay(0f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var percent = Math.Min(75, 50 + Math.Max(0, GetAbilityModifier(AbilityType.Might, activator)));
                    var amount = Math.Max(1, (int)Math.Ceiling(Stat.GetMaxStamina(activator) * (percent / 100f)));
                    Stat.RestoreStamina(activator, amount);
                })
                .IsCastedAbility()
                .BreaksStealth();

            return builder.Build();
        }
    }
}
