using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class EspionagePerkDefinition : IPerkListDefinition
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

            FalseIdentities();
            CoverStory();

            return _builder.Build();
        }

        private void Stealth()
        {
            _builder.Create(PerkCategoryType.EspionageInfiltrator, PerkType.Stealth)
                .Name("Stealth")
                .Icon("ife_stealth1")
                .AutoAddActionModeToHotBar(ActionMode.Stealth)

                .AddPerkLevel()
                .Description("Enter stealth, increasing Stealth by 5 while active. Drains 2 STM every 6 seconds, breaks on hostile action, and can only be entered while out of combat.")
                .Price(2)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.ActiveStealthBonus, 5)

                .AddPerkLevel()
                .Description("Enter stealth, increasing Stealth by 10 while active. Drains 2 STM every 6 seconds, breaks on hostile action, and can only be entered while out of combat.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 15)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.ActiveStealthBonus, 10)

                .AddPerkLevel()
                .Description("Enter stealth, increasing Stealth by 15 while active. Drains 2 STM every 6 seconds, breaks on hostile action, and can only be entered while out of combat.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 28)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.ActiveStealthBonus, 15)

                .AddPerkLevel()
                .Description("Enter stealth, increasing Stealth by 20 while active. Drains 2 STM every 6 seconds, breaks on hostile action, and can only be entered while out of combat.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.ActiveStealthBonus, 20)
                .TriggerPurchase(Service.Stealth.RefreshActiveStatusAfterPerkLevelChange)
                .TriggerRefund(Service.Stealth.RefreshActiveStatusAfterPerkLevelChange);
        }

        private void BackAttack()
        {
            _builder.Create(PerkCategoryType.EspionageInfiltrator, PerkType.BackAttack)
                .Name("Back Attack")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BackAttackTrait)
                .Description("Melee weapon attacks from behind a target deal +3% damage.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 5)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.BackAttackDamagePercentAdjustment, 3)

                .AddPerkLevel()
                .Description("Melee weapon attacks from behind a target deal +5% damage and gain +3% Critical Rate.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 18)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.BackAttackDamagePercentAdjustment, 5)
                .IncreasesStat(StatType.BackAttackCriticalRatePercentAdjustment, 3)

                .AddPerkLevel()
                .Description("Melee weapon attacks from behind a target deal +8% damage and gain +5% Critical Rate.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 38)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.BackAttackDamagePercentAdjustment, 8)
                .IncreasesStat(StatType.BackAttackCriticalRatePercentAdjustment, 5);
        }

        private void Slicing()
        {
            _builder.Create(PerkCategoryType.EspionageInfiltrator, PerkType.Slicing)
                .Name("Slicing")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SlicingTrait)
                .Description("Can slice tier 1 lockboxes and terminals.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 8)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Can slice tier 2 lockboxes and terminals.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 22)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Can slice tier 3 lockboxes and terminals. Grants +1 trace during slicing.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 30)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Can slice tier 4 lockboxes and terminals. Grants +2 trace during slicing.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 42)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Can slice tier 5 lockboxes and terminals. Grants +3 trace during slicing.")
                .Price(4)
                .RequirementCharacterType(CharacterType.Standard)
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
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .GrantsFeat(FeatType.TacticalEscape2)
                .Description("Reduces your enmity by 60%, removes negative movement-speed effects, and increases Evasion by 12% for 30 seconds.")
                .Price(3)
                .RequirementCharacterType(CharacterType.Standard)
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
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShadowStep2)
                .Description("Dash behind one hostile target within 5m, remove negative movement-speed effects, and increase Evasion by 15% for 30 seconds.")
                .Price(4)
                .RequirementCharacterType(CharacterType.Standard)
                .RequirementSkill(SkillType.Espionage, 45);
        }

        private void SilentStride()
        {
            _builder.Create(PerkCategoryType.EspionageInfiltrator, PerkType.SilentStride)
                .Name("Silent Stride")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SilentStrideTrait)
                .Description("While stealthed, increases Movement Speed by 30% and reduces STM drain by 20%, from 2 STM every 6 seconds to 2 STM every 7.5 seconds. Stealth still prevents running at full speed.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 32)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.StealthMovementSpeedPercentAdjustment, 30)
                .IncreasesStat(StatType.StealthStaminaDrainReductionPercent, 20);
        }

        private void GhostProtocol()
        {
            _builder.Create(PerkCategoryType.EspionageInfiltrator, PerkType.GhostProtocol)
                .Name("Ghost Protocol")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GhostProtocol)
                .Description("Reduces your enmity by 80%, enters stealth for up to 30 seconds, and causes your next back attack within 30 seconds to critically hit and inflict Exposed, reducing Defense by 20% for 30 seconds.")
                .Price(6)
                .RequirementCharacterType(CharacterType.Standard)
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
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Can craft tier 2 weapon poisons.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 15)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Can craft tier 3 weapon poisons.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 28)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Can craft tier 4 weapon poisons.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 40)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Can craft tier 5 weapon poisons.")
                .Price(4)
                .RequirementCharacterType(CharacterType.Standard)
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
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Can craft, place, detect, and disarm tier 2 traps.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 18)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .Description("Can craft, place, detect, and disarm tier 3 traps. Traps arm 20% faster, reducing their arming time from 3 seconds to 2.4 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 30)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.TrapPlacementSpeedPercent, 20)

                .AddPerkLevel()
                .Description("Can craft, place, detect, and disarm tier 4 traps. Traps arm 30% faster, reducing their arming time from 3 seconds to 2.1 seconds.")
                .Price(4)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.TrapPlacementSpeedPercent, 30)
                .RequirementSkill(SkillType.Espionage, 45);
        }

        private void VenomExpertise()
        {
            _builder.Create(PerkCategoryType.EspionageSaboteur, PerkType.VenomExpertise)
                .Name("Venom Expertise")

                .AddPerkLevel()
                .GrantsFeat(FeatType.VenomExpertiseTrait)
                .Description("Venom from weapon poisons you apply deals 10% more damage.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 8)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.PoisonBonus, 10)

                .AddPerkLevel()
                .Description("Venom from weapon poisons you apply deals 20% more damage.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 35)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.PoisonBonus, 20);
        }

        private void RazorTrap()
        {
            _builder.Create(PerkCategoryType.EspionageSaboteur, PerkType.RazorTrap)
                .Name("Razor Trap")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RazorTrap1)
                .Description("Places a visible trap that arms after 3 seconds. When triggered, enemies within its 3m blast take 14 physical DMG plus PER scaling and Bleed for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 12)
                .RequirementCharacterType(CharacterType.Standard)

                .AddPerkLevel()
                .GrantsFeat(FeatType.RazorTrap2)
                .Description("Places a visible trap that arms after 3 seconds. When triggered, enemies within its 3m blast take 30 physical DMG plus PER scaling and Bleed for 30 seconds.")
                .Price(3)
                .RequirementCharacterType(CharacterType.Standard)
                .RequirementSkill(SkillType.Espionage, 38);
        }

        private void ShockTrap()
        {
            _builder.Create(PerkCategoryType.EspionageSaboteur, PerkType.ShockTrap)
                .Name("Shock Trap")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ShockTrap)
                .Description("Places a visible trap that arms after 3 seconds. When triggered, enemies within its 3m blast take 22 electrical DMG plus PER scaling and suffer Shock for 30 seconds.")
                .Price(4)
                .RequirementCharacterType(CharacterType.Standard)
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
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.AdditionalTrapCapacity, 1)

                .AddPerkLevel()
                .Description("Increases maximum concurrent traps to 3 and trap detection range by 5m, from 6m to 11m.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 42)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.TrapDetectionRangeBonus, 5)
                .IncreasesStat(StatType.AdditionalTrapCapacity, 2);
        }

        private void LastingCoatings()
        {
            _builder.Create(PerkCategoryType.EspionageSaboteur, PerkType.LastingCoatings)
                .Name("Lasting Coatings")

                .AddPerkLevel()
                .GrantsFeat(FeatType.LastingCoatingsTrait)
                .Description("Weapon poison coatings you apply gain 50% more charges, increasing from 20 to 30 charges.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 32)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.PoisonCoatingDurationPercent, 50);
        }

        private void MasterSaboteur()
        {
            _builder.Create(PerkCategoryType.EspionageSaboteur, PerkType.MasterSaboteur)
                .Name("Master Saboteur")

                .AddPerkLevel()
                .GrantsFeat(FeatType.MasterSaboteurTrait)
                .Description("Can craft, place, detect, and disarm tier 5 traps. Your trap damage and weapon-poison Venom damage increase by 10%.")
                .Price(6)
                .RequirementSkill(SkillType.Espionage, 50)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.PoisonBonus, 10)
                .IncreasesStat(StatType.TrapBonus, 10);
        }

        private void FalseIdentities()
        {
            _builder.Create(PerkCategoryType.EspionageTradecraft, PerkType.FalseIdentities)
                .Name("False Identities")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FalseIdentitiesTrait)
                .Description("Increases the number of disguises you may keep on file to 2.")
                .Price(2)
                .RequirementSkill(SkillType.Espionage, 10)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.AdditionalDisguiseSlots, 1)

                .AddPerkLevel()
                .Description("Increases the number of disguises you may keep on file to 3.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 28)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.AdditionalDisguiseSlots, 2)

                .AddPerkLevel()
                .Description("Increases the number of disguises you may keep on file to 4.")
                .Price(4)
                .RequirementSkill(SkillType.Espionage, 44)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.AdditionalDisguiseSlots, 3);
        }

        private void CoverStory()
        {
            _builder.Create(PerkCategoryType.EspionageTradecraft, PerkType.CoverStory)
                .Name("Cover Story")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CoverStoryTrait)
                .Description("Reduces the delay between disguise activations by 40%.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 20)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.DisguiseSwapCooldownReductionPercent, 40)

                .AddPerkLevel()
                .Description("Reduces the delay between disguise activations by 70%.")
                .Price(3)
                .RequirementSkill(SkillType.Espionage, 40)
                .RequirementCharacterType(CharacterType.Standard)
                .IncreasesStat(StatType.DisguiseSwapCooldownReductionPercent, 70);
        }
    }
}
