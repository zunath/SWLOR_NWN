using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

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
                .Description("The beast breathes poison at hostile targets in a cone, dealing poison DMG and attempting inflicts Poison.")
                .Price(2)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath1)

                .AddPerkLevel()
                .Description("The beast breathes poison at hostile targets in a cone, dealing increased poison DMG and attempting inflicts Poison.")
                .Price(3)
                .RequirementBeastLevel(18)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath2)

                .AddPerkLevel()
                .Description("The beast breathes poison at hostile targets in a cone, dealing high poison DMG and attempting inflicts Poison.")
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
                .Description("The beast breathes ice at hostile targets in a cone, dealing ice DMG and slowing affected enemies for 4 seconds.")
                .Price(2)
                .RequirementBeastLevel(8)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath1)

                .AddPerkLevel()
                .Description("The beast breathes ice at hostile targets in a cone, dealing increased ice DMG and slowing affected enemies for 5 seconds.")
                .Price(2)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath2)

                .AddPerkLevel()
                .Description("The beast breathes ice at hostile targets in a cone, dealing high ice DMG and immobilizing for 2 seconds.")
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
                .Description("The beast slams nearby hostile enemies for physical DMG and daze for 2 seconds.")
                .Price(3)
                .RequirementBeastLevel(12)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.CrushingSlam1)

                .AddPerkLevel()
                .Description("The beast slams nearby hostile enemies for physical DMG and daze for 2 seconds.")
                .Price(4)
                .RequirementBeastLevel(28)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.CrushingSlam2)

                .AddPerkLevel()
                .Description("The beast slams nearby hostile enemies for physical DMG and daze for 3 seconds.")
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
                .Description("The beast has a 10% chance to restore 1 STM to its master when it lands an attack.")
                .Price(3)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Bruiser)

                .AddPerkLevel()
                .Description("The beast has a 20% chance to restore 1 STM to its master when it lands an attack.")
                .Price(3)
                .RequirementBeastLevel(30)
                .RequirementBeastRole(BeastRoleType.Bruiser)

                .AddPerkLevel()
                .Description("The beast has a 30% chance to restore 1 STM to its master when it lands an attack.")
                .Price(4)
                .RequirementBeastLevel(48)
                .RequirementBeastRole(BeastRoleType.Bruiser);
        }

        private void VenomousHide()
        {
            _builder.Create(PerkCategoryType.BeastBruiser, PerkType.VenomousHide)
                .Name("Venomous Hide")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Enemies that damage the beast in melee have a 10% chance to suffer 8 poison DMG plus MGT scaling.")
                .Price(3)
                .RequirementBeastLevel(22)
                .RequirementBeastRole(BeastRoleType.Bruiser)

                .AddPerkLevel()
                .Description("Enemies that damage the beast in melee have a 20% chance to suffer poison DMG.")
                .Price(3)
                .RequirementBeastLevel(40)
                .RequirementBeastRole(BeastRoleType.Bruiser);
        }

        private void Rampage()
        {
            _builder.Create(PerkCategoryType.BeastBruiser, PerkType.Rampage)
                .Name("Rampage")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast attacks up to 3 nearby hostile enemies for physical DMG.")
                .Price(4)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.Rampage1)

                .AddPerkLevel()
                .Description("The beast attacks up to 4 nearby hostile enemies for physical DMG.")
                .Price(3)
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
                .Description("The beast deals heavy physical DMG to nearby hostile enemies and deals 12% more damage for 15 seconds.")
                .Price(5)
                .RequirementBeastLevel(50)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PrimalOverrun1);
        }

    }
}
