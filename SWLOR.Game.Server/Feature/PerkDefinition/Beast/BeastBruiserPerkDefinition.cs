using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Feature.QuestDefinition;

namespace SWLOR.Game.Server.Feature.PerkDefinition.Beast
{
    public sealed class BeastBruiserPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            PoisonBreath();
            IceBreath();
            CrushingSlam();
            EnduranceLink();
            VenomousHide();
            Rampage();
            PrimalOverrun();

            return _builder.Build();
        }

        private void PoisonBreath()
        {
            _builder.Create(PerkCategoryType.BeastBruiser, PerkType.PoisonBreath)
                .Name("Poison Breath")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast breathes poison at hostile targets in a 6m x 5m cone, dealing 10 poison DMG plus MGT scaling and attempting to inflict Poison for 12 seconds.")
                .Price(2)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath1)

                .AddPerkLevel()
                .Description("The beast breathes poison at hostile targets in a 6m x 5m cone, dealing 14 poison DMG plus MGT scaling and attempting to inflict Poison for 12 seconds.")
                .Price(3)
                .RequirementBeastLevel(18)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath2)

                .AddPerkLevel()
                .Description("The beast breathes poison at hostile targets in a 6m x 5m cone, dealing 18 poison DMG plus MGT scaling and attempting to inflict Poison for 12 seconds.")
                .Price(3)
                .RequirementBeastLevel(38)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath3);
        }

        private void IceBreath()
        {
            _builder.Create(PerkCategoryType.BeastBruiser, PerkType.IceBreath)
                .Name("Ice Breath")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast breathes ice at hostile targets in a 6m x 5m cone, dealing 10 ice DMG plus MGT scaling and slowing affected enemies for 4 seconds.")
                .Price(2)
                .RequirementBeastLevel(8)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath1)

                .AddPerkLevel()
                .Description("The beast breathes ice at hostile targets in a 6m x 5m cone, dealing 14 ice DMG plus MGT scaling and slowing affected enemies for 5 seconds.")
                .Price(3)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath2)

                .AddPerkLevel()
                .Description("The beast breathes ice at hostile targets in a 6m x 5m cone, dealing 18 ice DMG plus MGT scaling and immobilizing affected enemies for 10 seconds.")
                .Price(4)
                .RequirementBeastLevel(42)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath3);
        }

        private void CrushingSlam()
        {
            _builder.Create(PerkCategoryType.BeastBruiser, PerkType.CrushingSlam)
                .Name("Crushing Slam")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast slams hostile enemies within 5m for 10 physical DMG plus MGT scaling and Dazes them for 15 seconds.")
                .Price(3)
                .RequirementBeastLevel(12)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.CrushingSlam1)

                .AddPerkLevel()
                .Description("The beast slams hostile enemies within 5m for 14 physical DMG plus MGT scaling and Dazes them for 15 seconds.")
                .Price(4)
                .RequirementBeastLevel(28)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.CrushingSlam2)

                .AddPerkLevel()
                .Description("The beast slams hostile enemies within 5m for 18 physical DMG plus MGT scaling and Dazes them for 15 seconds.")
                .Price(4)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.CrushingSlam3);
        }

        private void EnduranceLink()
        {
            _builder.Create(PerkCategoryType.BeastBruiser, PerkType.EnduranceLink)
                .Name("Endurance Link")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .GrantsFeat(FeatType.EnduranceLinkTrait)
                .Description("The beast has a 10% chance to restore 1 STM to its master when it lands an attack.")
                .Price(3)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .IncreasesStat(StatType.AutoAttackMasterStaminaRestoreChance, 10)
                .IncreasesStat(StatType.AutoAttackMasterStaminaRestore, 1)

                .AddPerkLevel()
                .Description("The beast has a 20% chance to restore 1 STM to its master when it lands an attack.")
                .Price(3)
                .RequirementBeastLevel(30)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .IncreasesStat(StatType.AutoAttackMasterStaminaRestoreChance, 20)
                .IncreasesStat(StatType.AutoAttackMasterStaminaRestore, 1)

                .AddPerkLevel()
                .Description("The beast has a 30% chance to restore 1 STM to its master when it lands an attack.")
                .Price(4)
                .RequirementBeastLevel(48)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .IncreasesStat(StatType.AutoAttackMasterStaminaRestoreChance, 30)
                .IncreasesStat(StatType.AutoAttackMasterStaminaRestore, 1);
        }

        private void VenomousHide()
        {
            _builder.Create(PerkCategoryType.BeastBruiser, PerkType.VenomousHide)
                .Name("Venomous Hide")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .GrantsFeat(FeatType.VenomousHideTrait)
                .Description("Enemies that damage the beast in melee have a 10% chance to suffer 8 poison DMG plus MGT scaling.")
                .Price(3)
                .RequirementBeastLevel(22)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .IncreasesStat(StatType.MeleeDamageTakenPoisonDamageChance, 10)
                .IncreasesStat(StatType.MeleeDamageTakenPoisonDamage, 8)
                .IncreasesStat(StatType.MeleeDamageTakenPoisonDamageScalingAbility, (int)AbilityType.Might + 1)

                .AddPerkLevel()
                .Description("Enemies that damage the beast in melee have a 20% chance to suffer 8 poison DMG plus MGT scaling.")
                .Price(3)
                .RequirementBeastLevel(40)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .IncreasesStat(StatType.MeleeDamageTakenPoisonDamageChance, 20)
                .IncreasesStat(StatType.MeleeDamageTakenPoisonDamage, 8)
                .IncreasesStat(StatType.MeleeDamageTakenPoisonDamageScalingAbility, (int)AbilityType.Might + 1);
        }

        private void Rampage()
        {
            _builder.Create(PerkCategoryType.BeastBruiser, PerkType.Rampage)
                .Name("Rampage")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast attacks up to 3 hostile enemies within 5m for 10 physical DMG plus MGT scaling each.")
                .Price(3)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.Rampage1)

                .AddPerkLevel()
                .Description("The beast attacks up to 4 hostile enemies within 5m for 14 physical DMG plus MGT scaling each.")
                .Price(4)
                .RequirementBeastLevel(50)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.Rampage2);
        }

        private void PrimalOverrun()
        {
            _builder.Create(PerkCategoryType.BeastBruiser, PerkType.PrimalOverrun)
                .Name("Primal Overrun")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast deals 42 physical DMG plus MGT scaling to hostile enemies within 5m and deals 12% more damage for 30 seconds.")
                .Price(5)
                .RequirementBeastLevel(50)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PrimalOverrun1)
                .RequirementQuest(BeastMasteryCapstoneQuestDefinition.PrimalOverrunMasteryQuestId);
        }

    }
}
