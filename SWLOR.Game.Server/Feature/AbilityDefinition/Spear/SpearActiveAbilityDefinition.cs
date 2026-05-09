using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class SpearActiveAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeapon(builder.Create(FeatType.DisablingStrike1, PerkType.DisablingStrike).Name("Disabling Strike I").Level(1), SkillType.Spear, 12, 8, 12, SavingThrow.Will, typeof(ForceDisruptionStatusEffect), 4);
            ConfigureInterrupt(builder.Create(FeatType.InterruptionStrike1, PerkType.InterruptionStrike).Name("Interruption Strike I").Level(1), SkillType.Spear, 0, 30, 12, SavingThrow.Will, typeof(FoggyMindStatusEffect), 5, FoggyMind(2));
            ConfigureToggle(builder.Create(FeatType.PerceptiveStance1, PerkType.PerceptiveStance).Name("Perceptive Stance").Level(1), typeof(PerceptiveStanceStatusEffect));
            ConfigureWeapon(builder.Create(FeatType.DisablingStrike2, PerkType.DisablingStrike).Name("Disabling Strike II").Level(2), SkillType.Spear, 18, 8, 16, SavingThrow.Will, typeof(ForceDisruptionStatusEffect), 5);
            ConfigureAreaStatus(builder.Create(FeatType.DisruptionField1, PerkType.DisruptionField).Name("Disruption Field").Level(1), typeof(DisruptionFieldStatusEffect), 20f, 5, false, fpDrainPercent: 20);
            ConfigureInterrupt(builder.Create(FeatType.InterruptionStrike2, PerkType.InterruptionStrike).Name("Interruption Strike II").Level(2), SkillType.Spear, 0, 30, 18, SavingThrow.Will, typeof(FoggyMindStatusEffect), 7, FoggyMind(2));
            ConfigureWeapon(builder.Create(FeatType.DisablingStrike3, PerkType.DisablingStrike).Name("Disabling Strike III").Level(3), SkillType.Spear, 26, 8, 20, SavingThrow.Will, typeof(ForceDisruptionStatusEffect), 6);
            ConfigureAreaStatus(builder.Create(FeatType.Forcebane1, PerkType.Forcebane).Name("Forcebane").Level(1), typeof(ForcebaneStatusEffect), 8f, 50, false, fpDrainPercent: 50);
            ConfigureToggle(builder.Create(FeatType.FlankingStance1, PerkType.FlankingStance).Name("Flanking Stance").Level(1), typeof(FlankingStanceStatusEffect));
            ConfigureWeapon(builder.Create(FeatType.SideAssault1, PerkType.SideAssault).Name("Side Assault I").Level(1), SkillType.Spear, 12, 0, 0, SavingThrow.Reflex, null, 4);
            ConfigurePartyStatus(builder.Create(FeatType.ImprovedAttentiveness1, PerkType.ImprovedAttentiveness).Name("Improved Attentiveness").Level(1), typeof(ImprovedAttentivenessStatusEffect), 60f, 25, false);
            ConfigureWeapon(builder.Create(FeatType.SideAssault2, PerkType.SideAssault).Name("Side Assault II").Level(2), SkillType.Spear, 25, 0, 0, SavingThrow.Reflex, null, 6);
            ConfigureWeapon(builder.Create(FeatType.SideAssault3, PerkType.SideAssault).Name("Side Assault III").Level(3), SkillType.Spear, 35, 0, 0, SavingThrow.Reflex, null, 8);
            ConfigureToggle(builder.Create(FeatType.CalmingStance1, PerkType.CalmingStance).Name("Calming Stance").Level(1), typeof(CalmingStanceStatusEffect));
            ConfigureAreaStatus(builder.Create(FeatType.CripplingDefense1, PerkType.CripplingDefense).Name("Crippling Defense").Level(1), typeof(CripplingDefenseStatusEffect), 15f, 35, true, restoreStamina: 25);


            return builder.Build();
        }
    }
}
