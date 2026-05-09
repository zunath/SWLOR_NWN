using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class KatarActiveAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(builder.Create(FeatType.TwinGuardStance1, PerkType.TwinGuardStance).Name("Twin Guard Stance").Level(1), typeof(TwinGuardStanceStatusEffect));

            builder.Create(FeatType.TwinIntercept1, PerkType.TwinIntercept)
                .Name("Twin Intercept")
                .Level(1)
                .HasActivationDelay(0f)
                .RequiresTarget()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    var shield = Math.Max(1, (int)Math.Ceiling(GetMaxHitPoints(activator) * 0.2f));
                    ApplyEffectToObject(DurationType.Temporary, EffectTemporaryHitpoints(shield), target, 8f);
                    StatusEffect.ApplyStatusEffect(activator, target, typeof(TwinInterceptStatusEffect), 8f);
                    Enmity.ModifyEnmityOnAll(activator, 450);
                })
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(10);

            ConfigureToggle(builder.Create(FeatType.IronWallStance1, PerkType.IronWallStance).Name("Iron Wall Stance").Level(1), typeof(IronWallStanceStatusEffect));
            ConfigureSelfStatus(builder.Create(FeatType.AdamantineGuard1, PerkType.AdamantineGuard).Name("Adamantine Guard").Level(1), typeof(AdamantineGuardStatusEffect), 20f, 12);
            ConfigureWeapon(builder.Create(FeatType.StrikingCobra1, PerkType.StrikingCobra).Name("Striking Cobra I").Level(1), SkillType.Katar, 8, 30, 12, SavingThrow.Fortitude, typeof(PoisonStatusEffect), 3);
            ConfigureToggle(builder.Create(FeatType.CobraStance1, PerkType.CobraStance).Name("Cobra Stance").Level(1), typeof(CobraStanceStatusEffect));
            ConfigureWeapon(builder.Create(FeatType.StrikingCobra2, PerkType.StrikingCobra).Name("Striking Cobra II").Level(2), SkillType.Katar, 18, 60, 15, SavingThrow.Fortitude, typeof(PoisonStatusEffect), 5);
            ConfigureWeapon(builder.Create(FeatType.StrikingCobra3, PerkType.StrikingCobra).Name("Striking Cobra III").Level(3), SkillType.Katar, 28, 60, 20, SavingThrow.Fortitude, typeof(PoisonStatusEffect), 7);
            ConfigureSelfStatus(builder.Create(FeatType.ToxicRush1, PerkType.ToxicRush).Name("Toxic Rush").Level(1), typeof(ToxicRushStatusEffect), 20f, 8);


            return builder.Build();
        }
    }
}
