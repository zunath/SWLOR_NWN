using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Staff
{
    public class StaffActiveAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeapon(builder.Create(FeatType.LegSweep1, PerkType.LegSweep).Name("Leg Sweep I").Level(1), SkillType.Staff, 6, 3, 12, SavingThrow.Reflex, typeof(KnockdownStatusEffect), 4);
            ConfigureToggle(builder.Create(FeatType.SentinelStance1, PerkType.SentinelStance).Name("Sentinel Stance").Level(1), typeof(SentinelStanceStatusEffect));
            ConfigureSelfStatus(builder.Create(FeatType.GuardingStep1, PerkType.GuardingStep).Name("Guarding Step").Level(1), typeof(GuardingStepStatusEffect), 8f, 5);
            ConfigureWeapon(builder.Create(FeatType.LegSweep2, PerkType.LegSweep).Name("Leg Sweep II").Level(2), SkillType.Staff, 16, 3, 15, SavingThrow.Reflex, typeof(KnockdownStatusEffect), 5);
            ConfigurePartyStatus(builder.Create(FeatType.SentinelGuard1, PerkType.SentinelGuard).Name("Sentinel Guard").Level(1), typeof(SentinelGuardStatusEffect), 12f, 10, true);
            ConfigureWeapon(builder.Create(FeatType.LegSweep3, PerkType.LegSweep).Name("Leg Sweep III").Level(3), SkillType.Staff, 26, 4, 18, SavingThrow.Reflex, typeof(KnockdownStatusEffect), 7);
            ConfigurePartyStatus(builder.Create(FeatType.ShelterCircle1, PerkType.ShelterCircle).Name("Shelter Circle").Level(1), typeof(ShelterCircleStatusEffect), 15f, 20, true);
            ConfigureSelfStatus(builder.Create(FeatType.UnmovingCenter1, PerkType.UnmovingCenter).Name("Unmoving Center").Level(1), typeof(UnmovingCenterStatusEffect), 20f, 50, activator =>
            {
                Ability.ApplyTemporaryImmunity(activator, 20f, ImmunityType.Knockdown);
                Ability.ApplyTemporaryImmunity(activator, 20f, ImmunityType.Dazed);
            });
            ConfigureWeapon(builder.Create(FeatType.Slam1, PerkType.Slam).Name("Slam I").Level(1), SkillType.Staff, 8, 8, 12, SavingThrow.Fortitude, typeof(BlindStatusEffect), 4);
            ConfigureToggle(builder.Create(FeatType.CrusherStance1, PerkType.CrusherStance).Name("Crusher Stance").Level(1), typeof(CrusherStanceStatusEffect));
            ConfigureWeapon(builder.Create(FeatType.Slam2, PerkType.Slam).Name("Slam II").Level(2), SkillType.Staff, 20, 10, 15, SavingThrow.Fortitude, typeof(BlindStatusEffect), 5);
            ConfigureWeapon(builder.Create(FeatType.Slam3, PerkType.Slam).Name("Slam III").Level(3), SkillType.Staff, 32, 12, 18, SavingThrow.Fortitude, typeof(BlindStatusEffect), 7);


            return builder.Build();
        }
    }
}
