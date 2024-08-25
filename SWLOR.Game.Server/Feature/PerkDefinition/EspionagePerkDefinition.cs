using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class EspionagePerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            BackAttack();
            TacticalEscape();
            Poisoncraft();
            PoisonExpertise();
            Lockpicking();
            Trapcraft();
            TrapManagement();
            Stealth();

            return _builder.Build();
        }

        private void BackAttack()
        {
            _builder.Create(PerkCategoryType.Espionage, PerkType.BackAttack)
                .Name("Back Attack")

                .AddPerkLevel()
                .Description("Melee damage dealt from behind a target is increased by 10%.")
                .Price(2)
                .GrantsFeat(FeatType.BackAttack1)

                .AddPerkLevel()
                .Description("Melee damage dealt from behind a target is increased by 20%.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 10)
                .GrantsFeat(FeatType.BackAttack2)

                .AddPerkLevel()
                .Description("Melee damage dealt from behind a target is increased by 30%.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 20)
                .GrantsFeat(FeatType.BackAttack3)

                .AddPerkLevel()
                .Description("Melee damage dealt from behind a target is increased by 40%.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 30)
                .GrantsFeat(FeatType.BackAttack4)

                .AddPerkLevel()
                .Description("Melee damage dealt from behind a target is increased by 50%.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 40)
                .GrantsFeat(FeatType.BackAttack5);
        }

        private void TacticalEscape()
        {
            _builder.Create(PerkCategoryType.Espionage, PerkType.TacticalEscape)
                .Name("Tactical Escape")

                .AddPerkLevel()
                .Description("Reduces your enmity by 50%.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 25)
                .GrantsFeat(FeatType.TacticalEscape1)

                .AddPerkLevel()
                .Description("Reduces your enmity by 100%.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 50)
                .GrantsFeat(FeatType.TacticalEscape2);
        }

        private void Poisoncraft()
        {
            _builder.Create(PerkCategoryType.Espionage, PerkType.Poisoncraft)
                .Name("Poisoncraft")

                .AddPerkLevel()
                .Description("Grants ability to craft tier 1 poisons to be applied to weapons.")
                .Price(1)
                .RequirementSkill(SkillType.Espionage, 5)
                .GrantsFeat(FeatType.Poisoncraft1)

                .AddPerkLevel()
                .Description("Grants ability to craft tier 2 poisons to be applied to weapons.")
                .Price(1)
                .RequirementSkill(SkillType.Espionage, 15)
                .GrantsFeat(FeatType.Poisoncraft2)

                .AddPerkLevel()
                .Description("Grants ability to craft tier 3 poisons to be applied to weapons.")
                .Price(1)
                .RequirementSkill(SkillType.Espionage, 25)
                .GrantsFeat(FeatType.Poisoncraft3)

                .AddPerkLevel()
                .Description("Grants ability to craft tier 4 poisons to be applied to weapons.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 35)
                .GrantsFeat(FeatType.Poisoncraft4)

                .AddPerkLevel()
                .Description("Grants ability to craft tier 5 poisons to be applied to weapons.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 45)
                .GrantsFeat(FeatType.Poisoncraft5);
        }

        private void PoisonExpertise()
        {
            _builder.Create(PerkCategoryType.Espionage, PerkType.PoisonExpertise)
                .Name("Poison Expertise")

                .AddPerkLevel()
                .Description("Improves poisons applied to your weapons by 20%.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 30)
                .GrantsFeat(FeatType.PoisonExpertise1)

                .AddPerkLevel()
                .Description("Improves poisons applied to your weapons by 40%.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 50)
                .GrantsFeat(FeatType.PoisonExpertise2);
        }

        private void Lockpicking()
        {
            _builder.Create(PerkCategoryType.Espionage, PerkType.Lockpicking)
                .Name("Lockpicking")

                .AddPerkLevel()
                .Description("Tier 1 locks can be picked.")
                .Price(1)
                .GrantsFeat(FeatType.Lockpicking1)

                .AddPerkLevel()
                .Description("Tier 2 locks can be picked.")
                .Price(1)
                .RequirementSkill(SkillType.Espionage, 10)
                .GrantsFeat(FeatType.Lockpicking2)

                .AddPerkLevel()
                .Description("Tier 3 locks can be picked.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 20)
                .GrantsFeat(FeatType.Lockpicking3)

                .AddPerkLevel()
                .Description("Tier 4 locks can be picked.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 30)
                .GrantsFeat(FeatType.Lockpicking4)

                .AddPerkLevel()
                .Description("Tier 5 locks can be picked.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 40)
                .GrantsFeat(FeatType.Lockpicking5);
        }

        private void Trapcraft()
        {
            _builder.Create(PerkCategoryType.Espionage, PerkType.Trapcraft)
                .Name("Trapcraft")

                .AddPerkLevel()
                .Description("Grants ability to craft, use and disarm tier 1 traps.")
                .Price(1)
                .RequirementSkill(SkillType.Espionage, 5)
                .GrantsFeat(FeatType.Trapcraft1)

                .AddPerkLevel()
                .Description("Grants ability to craft, use and disarm tier 2 traps.")
                .Price(1)
                .RequirementSkill(SkillType.Espionage, 15)
                .GrantsFeat(FeatType.Trapcraft2)

                .AddPerkLevel()
                .Description("Grants ability to craft, use and disarm tier 3 traps.")
                .Price(1)
                .RequirementSkill(SkillType.Espionage, 25)
                .GrantsFeat(FeatType.Trapcraft3)

                .AddPerkLevel()
                .Description("Grants ability to craft, use and disarm tier 4 traps.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 35)
                .GrantsFeat(FeatType.Trapcraft4)

                .AddPerkLevel()
                .Description("Grants ability to craft, use and disarm tier 5 traps.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 45)
                .GrantsFeat(FeatType.Trapcraft5);
        }

        private void TrapManagement()
        {
            _builder.Create(PerkCategoryType.Espionage, PerkType.TrapManagement)
                .Name("Trap Management")

                .AddPerkLevel()
                .Description("Increases the maximum number of concurrent traps to 2.")
                .Price(1)
                .RequirementSkill(SkillType.Espionage, 20)
                .GrantsFeat(FeatType.TrapManagement1)

                .AddPerkLevel()
                .Description("Increases the maximum number of concurrent traps to 3.")
                .Price(1)
                .RequirementSkill(SkillType.Espionage, 40)
                .GrantsFeat(FeatType.TrapManagement2);
        }

        private void Stealth()
        {
            _builder.Create(PerkCategoryType.Espionage, PerkType.Stealth)
                .Name("Stealth")

                .AddPerkLevel()
                .Description("Increases Stealth by 2. Stamina drains while ability is active.")
                .Price(1)
                .GrantsFeat(FeatType.Stealth1)

                .AddPerkLevel()
                .Description("Increases Stealth by 4. Stamina drains while ability is active.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 10)
                .GrantsFeat(FeatType.Stealth2)

                .AddPerkLevel()
                .Description("Increases Stealth by 6. Stamina drains while ability is active.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 20)
                .GrantsFeat(FeatType.Stealth3)

                .AddPerkLevel()
                .Description("Increases Stealth by 8. Stamina drains while ability is active.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 30)
                .GrantsFeat(FeatType.Stealth4)

                .AddPerkLevel()
                .Description("Increases Stealth by 10. Stamina drains while ability is active.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 40)
                .GrantsFeat(FeatType.Stealth5);
        }
    }
}
