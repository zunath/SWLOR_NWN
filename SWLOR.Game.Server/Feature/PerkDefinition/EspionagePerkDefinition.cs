using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class EspionagePerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Stealth();
            BackAttack();
            Slicing();
            TacticalEscape();
            ShadowStep();
            SilentStride();
            GhostProtocol();

            Poisoncraft();
            Trapcraft();
            VenomExpertise();
            RazorTrap();
            ShockTrap();
            TrapManagement();
            LastingCoatings();
            MasterSaboteur();

            return _builder.Build();
        }

        private void Stealth()
        {
            _builder.Create(PerkCategoryType.EspionageInfiltrator, PerkType.Stealth)
                .Name("Stealth")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Stealth1)
                .Description("Enter stealth, increasing stealth effectiveness by 15% while active. Drains STM over time and breaks on hostile action. Usable only while out of combat.")
                .Price(2)
                .IncreasesStat(StatType.StealthEffectivenessPercent, 15)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Stealth2)
                .Description("Enter stealth, increasing stealth effectiveness by 25% while active. Drains STM over time and breaks on hostile action. Usable only while out of combat.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 15)
                .IncreasesStat(StatType.StealthEffectivenessPercent, 25)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Stealth3)
                .Description("Enter stealth, increasing stealth effectiveness by 35% while active. Drains STM over time and breaks on hostile action. Usable only while out of combat.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 28)
                .IncreasesStat(StatType.StealthEffectivenessPercent, 35)

                .AddPerkLevel()
                .GrantsFeat(FeatType.Stealth4)
                .Description("Enter stealth, increasing stealth effectiveness by 45% while active. Drains STM over time and breaks on hostile action. Usable only while out of combat.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 40)
                .IncreasesStat(StatType.StealthEffectivenessPercent, 45);
        }

        private void BackAttack()
        {
            _builder.Create(PerkCategoryType.EspionageInfiltrator, PerkType.BackAttack)
                .Name("Back Attack")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BackAttackTrait)
                .Description("Weapon attacks from behind a target deal +3% damage.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 5)
                .IncreasesStat(StatType.BackAttackDamagePercentAdjustment, 3)

                .AddPerkLevel()
                .Description("Weapon attacks from behind a target deal +5% damage and gain +3% critical chance.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 18)
                .IncreasesStat(StatType.BackAttackDamagePercentAdjustment, 5)
                .IncreasesStat(StatType.BackAttackCriticalRatePercentAdjustment, 3)

                .AddPerkLevel()
                .Description("Weapon attacks from behind a target deal +8% damage and gain +5% critical chance.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 38)
                .IncreasesStat(StatType.BackAttackDamagePercentAdjustment, 8)
                .IncreasesStat(StatType.BackAttackCriticalRatePercentAdjustment, 5);
        }

        private void Slicing()
        {
            _builder.Create(PerkCategoryType.EspionageInfiltrator, PerkType.Slicing)
                .Name("Slicing")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SlicingTrait)
                .Description("Can pick tier 1 locks and hack tier 1 terminals.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 8)

                .AddPerkLevel()
                .Description("Can pick tier 2 locks and hack tier 2 terminals.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 22)

                .AddPerkLevel()
                .Description("Can pick tier 3 locks and hack tier 3 terminals. Hacking actions are 20% faster.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 30)

                .AddPerkLevel()
                .Description("Can pick tier 4 locks and hack tier 4 terminals. Hacking actions are 30% faster.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 42)

                .AddPerkLevel()
                .Description("Can pick tier 5 locks and hack tier 5 terminals. Hacking actions are 40% faster.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 48);
        }

        private void TacticalEscape()
        {
            _builder.Create(PerkCategoryType.EspionageInfiltrator, PerkType.TacticalEscape)
                .Name("Tactical Escape")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TacticalEscape1)
                .Description("Reduces your enmity by 35% and increases Evasion by 8% for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.TacticalEscape2)
                .Description("Reduces your enmity by 60%, removes movement slow, and increases Evasion by 12% for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 35);
        }

        private void ShadowStep()
        {
            _builder.Create(PerkCategoryType.EspionageInfiltrator, PerkType.ShadowStep)
                .Name("Shadow Step")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShadowStep1)
                .Description("Dash behind one hostile target within 5m and increase Evasion by 10% for 30 seconds. Does not grant invisibility.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 25)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShadowStep2)
                .Description("Dash behind one hostile target within 5m, cleanse movement impairing effects, and increase Evasion by 15% for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 45);
        }

        private void SilentStride()
        {
            _builder.Create(PerkCategoryType.EspionageInfiltrator, PerkType.SilentStride)
                .Name("Silent Stride")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SilentStrideTrait)
                .Description("Movement speed while stealthed is no longer reduced, and stealth drains STM 20% slower.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 32)
                .IncreasesStat(StatType.StealthStaminaDrainReductionPercent, 20);
        }

        private void GhostProtocol()
        {
            _builder.Create(PerkCategoryType.EspionageInfiltrator, PerkType.GhostProtocol)
                .Name("Ghost Protocol")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GhostProtocol)
                .Description("Drop most enmity, enter stealth for 30 seconds, and cause your next back attack within 30 seconds to critically hit and inflict Exposed.")
                .Price(6)
                .RequirementSkill(SkillType.Espionage, 50);
        }

        private void Poisoncraft()
        {
            _builder.Create(PerkCategoryType.EspionageSaboteur, PerkType.Poisoncraft)
                .Name("Poisoncraft")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PoisoncraftTrait)
                .Description("Can craft tier 1 weapon poisons.")
                .Price(2)

                .AddPerkLevel()
                .Description("Can craft tier 2 weapon poisons.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 15)

                .AddPerkLevel()
                .Description("Can craft tier 3 weapon poisons.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 28)

                .AddPerkLevel()
                .Description("Can craft tier 4 weapon poisons.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 40)

                .AddPerkLevel()
                .Description("Can craft tier 5 weapon poisons.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 48);
        }

        private void Trapcraft()
        {
            _builder.Create(PerkCategoryType.EspionageSaboteur, PerkType.Trapcraft)
                .Name("Trapcraft")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TrapcraftTrait)
                .Description("Can craft, place, detect, and disarm tier 1 traps.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 5)

                .AddPerkLevel()
                .Description("Can craft, place, detect, and disarm tier 2 traps.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 18)

                .AddPerkLevel()
                .Description("Can craft, place, detect, and disarm tier 3 traps. Trap placement is 20% faster.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 30)

                .AddPerkLevel()
                .Description("Can craft, place, detect, and disarm tier 4 traps. Trap placement is 30% faster.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 45);
        }

        private void VenomExpertise()
        {
            _builder.Create(PerkCategoryType.EspionageSaboteur, PerkType.VenomExpertise)
                .Name("Venom Expertise")

                .AddPerkLevel()
                .GrantsFeat(FeatType.VenomExpertiseTrait)
                .Description("Weapon poisons applied by you deal 10% more damage or last 10% longer, based on poison type.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 8)
                .IncreasesStat(StatType.PoisonBonus, 10)

                .AddPerkLevel()
                .Description("Weapon poisons applied by you deal 20% more damage or last 20% longer, based on poison type.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 35)
                .IncreasesStat(StatType.PoisonBonus, 20);
        }

        private void RazorTrap()
        {
            _builder.Create(PerkCategoryType.EspionageSaboteur, PerkType.RazorTrap)
                .Name("Razor Trap")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RazorTrap1)
                .Description("Places a visible trap that arms after 3 seconds. Triggered enemies take 14 physical DMG plus PER scaling and Bleed for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.RazorTrap2)
                .Description("Places a visible trap that arms after 3 seconds. Triggered enemies take 30 physical DMG plus PER scaling and Bleed for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 38);
        }

        private void ShockTrap()
        {
            _builder.Create(PerkCategoryType.EspionageSaboteur, PerkType.ShockTrap)
                .Name("Shock Trap")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShockTrap)
                .Description("Places a visible trap that arms after 3 seconds. Triggered enemies take 22 electrical DMG plus PER scaling and suffer Shock for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 25);
        }

        private void TrapManagement()
        {
            _builder.Create(PerkCategoryType.EspionageSaboteur, PerkType.TrapManagement)
                .Name("Trap Management")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TrapManagementTrait)
                .Description("Increases maximum concurrent traps to 2.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 22)
                .IncreasesStat(StatType.AdditionalTrapCapacity, 1)

                .AddPerkLevel()
                .Description("Increases maximum concurrent traps to 3 and improves trap detection range.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 42)
                .IncreasesStat(StatType.AdditionalTrapCapacity, 2);
        }

        private void LastingCoatings()
        {
            _builder.Create(PerkCategoryType.EspionageSaboteur, PerkType.LastingCoatings)
                .Name("Lasting Coatings")

                .AddPerkLevel()
                .GrantsFeat(FeatType.LastingCoatingsTrait)
                .Description("Weapon poisons you apply last 50% longer before wearing off.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 32)
                .IncreasesStat(StatType.PoisonCoatingDurationPercent, 50);
        }

        private void MasterSaboteur()
        {
            _builder.Create(PerkCategoryType.EspionageSaboteur, PerkType.MasterSaboteur)
                .Name("Master Saboteur")

                .AddPerkLevel()
                .GrantsFeat(FeatType.MasterSaboteurTrait)
                .Description("Can craft, place, detect, and disarm tier 5 traps. Your trap effect strength increases by 10% and your poisons gain +10% potency.")
                .Price(6)
                .RequirementSkill(SkillType.Espionage, 50)
                .IncreasesStat(StatType.PoisonBonus, 10)
                .IncreasesStat(StatType.TrapBonus, 10);
        }
    }
}
