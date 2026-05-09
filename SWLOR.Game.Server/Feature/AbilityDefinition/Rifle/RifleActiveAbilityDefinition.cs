using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class RifleActiveAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(builder.Create(FeatType.SniperStance1, PerkType.SniperStance).Name("Sniper Stance").Level(1), typeof(SniperStanceStatusEffect));
            ConfigureSelfStatus(builder.Create(FeatType.KillZone1, PerkType.KillZone).Name("Kill Zone").Level(1), typeof(KillZoneStatusEffect), 20f, 6);
            ConfigureWeapon(builder.Create(FeatType.TranquilizerShot1, PerkType.TranquilizerShot).Name("Tranquilizer Shot I").Level(1), SkillType.Rifle, 0, 8, 12, SavingThrow.Will, typeof(DazedStatusEffect), 4);
            ConfigureWeapon(builder.Create(FeatType.CripplingShot1, PerkType.CripplingShot).Name("Crippling Shot I").Level(1), SkillType.Rifle, 12, 12, 12, SavingThrow.Reflex, typeof(DisorientedStatusEffect), 4);
            ConfigureToggle(builder.Create(FeatType.SpotterStance1, PerkType.SpotterStance).Name("Spotter Stance").Level(1), typeof(SpotterStanceStatusEffect));
            ConfigureWeapon(builder.Create(FeatType.TranquilizerShot2, PerkType.TranquilizerShot).Name("Tranquilizer Shot II").Level(2), SkillType.Rifle, 0, 14, 15, SavingThrow.Will, typeof(DazedStatusEffect), 5);
            ConfigureWeapon(builder.Create(FeatType.CripplingShot2, PerkType.CripplingShot).Name("Crippling Shot II").Level(2), SkillType.Rifle, 22, 15, 15, SavingThrow.Reflex, typeof(DisorientedStatusEffect), 6);
            ConfigureTelegraphedArea(builder.Create(FeatType.TranqCone1, PerkType.TranqCone).Name("Tranq Cone I").Level(1), SkillType.Rifle, CombatImpactAreaShape.Cone, 0, 8, 12, SavingThrow.Will, typeof(DazedStatusEffect), 8f, 6f, 6);
            ConfigureWeapon(builder.Create(FeatType.CripplingShot3, PerkType.CripplingShot).Name("Crippling Shot III").Level(3), SkillType.Rifle, 34, 20, 18, SavingThrow.Reflex, typeof(DisorientedStatusEffect), 8);
            ConfigureTelegraphedArea(builder.Create(FeatType.TranqCone2, PerkType.TranqCone).Name("Tranq Cone II").Level(2), SkillType.Rifle, CombatImpactAreaShape.Cone, 0, 10, 15, SavingThrow.Will, typeof(DazedStatusEffect), 10f, 7f, 8);


            return builder.Build();
        }
    }
}
