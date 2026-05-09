using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class VibroknifeActiveAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureCastedTarget(builder.Create(FeatType.Backstab1, PerkType.Backstab).Name("Backstab I").Level(1), SkillType.Vibroknife, 20, 4);
            ConfigureToggle(builder.Create(FeatType.DeadlyPrecision1, PerkType.DeadlyPrecision).Name("Deadly Precision").Level(1), typeof(DeadlyPrecisionStatusEffect));
            ConfigureSelfStatus(builder.Create(FeatType.EvasiveCombat1, PerkType.EvasiveCombat).Name("Evasive Combat I").Level(1), typeof(EvasiveCombatStatusEffect), 30f, 10, activator => Enmity.ModifyEnmityOnAll(activator, -150));
            ConfigureCastedTarget(builder.Create(FeatType.Backstab2, PerkType.Backstab).Name("Backstab II").Level(2), SkillType.Vibroknife, 40, 6);
            ConfigureTargetStatus(builder.Create(FeatType.MarkedForDeath1, PerkType.MarkedForDeath).Name("Marked for Death").Level(1), typeof(MarkedForDeathStatusEffect), 20f, 6);
            ConfigureCastedTarget(builder.Create(FeatType.Backstab3, PerkType.Backstab).Name("Backstab III").Level(3), SkillType.Vibroknife, 60, 8, 3, 14, SavingThrow.Fortitude, typeof(KnockdownStatusEffect));
            ConfigureSelfStatus(builder.Create(FeatType.EvasiveCombat2, PerkType.EvasiveCombat).Name("Evasive Combat II").Level(2), typeof(EvasiveCombatStatusEffect), 30f, 20, activator => Enmity.ModifyEnmityOnAll(activator, -250));
            ConfigureAreaStatus(builder.Create(FeatType.Decoy1, PerkType.Decoy).Name("Decoy").Level(1), typeof(DecoyStatusEffect), 12f, 25, true);
            ConfigureWeapon(builder.Create(FeatType.Hamstring1, PerkType.Hamstring).Name("Hamstring I").Level(1), SkillType.Vibroknife, 8, 12, 10, SavingThrow.Reflex, typeof(HamstringStatusEffect), 4);
            ConfigureWeapon(builder.Create(FeatType.ToxicCoating1, PerkType.ToxicCoating).Name("Toxic Coating I").Level(1), SkillType.Vibroknife, 10, 30, 10, SavingThrow.Fortitude, typeof(ToxinStatusEffect), 4);
            ConfigureWeapon(builder.Create(FeatType.Hamstring2, PerkType.Hamstring).Name("Hamstring II").Level(2), SkillType.Vibroknife, 18, 12, 14, SavingThrow.Reflex, typeof(HamstringStatusEffect), 5);
            ConfigureToggle(builder.Create(FeatType.DebilitatingStance1, PerkType.DebilitatingStance).Name("Debilitating Stance").Level(1), typeof(DebilitatingStanceStatusEffect));
            ConfigureWeapon(builder.Create(FeatType.Hamstring3, PerkType.Hamstring).Name("Hamstring III").Level(3), SkillType.Vibroknife, 28, 12, 18, SavingThrow.Reflex, typeof(HamstringStatusEffect), 7);
            ConfigureWeapon(builder.Create(FeatType.ToxicCoating2, PerkType.ToxicCoating).Name("Toxic Coating II").Level(2), SkillType.Vibroknife, 22, 30, 15, SavingThrow.Fortitude, typeof(ToxinStatusEffect), 6);
            ConfigureAreaStatus(builder.Create(FeatType.Incapacitate1, PerkType.Incapacitate).Name("Incapacitate").Level(1), typeof(IncapacitateStatusEffect), 20f, 20, true);


            return builder.Build();
        }
    }
}
