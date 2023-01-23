using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class BeastBruiserPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            PoisonBreath();
            IceBreath();
            EnduranceLink();

            return _builder.Build();
        }

        private void PoisonBreath()
        {
            _builder.Create(PerkCategoryType.BeastBruiser, PerkType.PoisonBreath)
                .Name("Poison Breath")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Deals 8 poison DMG to all targets within a cone in front of the beast.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath1)

                .AddPerkLevel()
                .Description("Deals 12 poison DMG to all targets within a cone in front of the beast.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath2)

                .AddPerkLevel()
                .Description("Deals 16 poison DMG to all targets within a cone in front of the beast. Also has an 8DC reflex check to inflict Poison.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath3)

                .AddPerkLevel()
                .Description("Deals 20 poison DMG to all targets within a cone in front of the beast. Also has a 12DC reflex check to inflict Poison.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath4)

                .AddPerkLevel()
                .Description("Deals 24 poison DMG to all targets within a cone in front of the beast. Also has a 14DC reflex check to inflict Poison.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.PoisonBreath5);
        }

        private void IceBreath()
        {
            _builder.Create(PerkCategoryType.BeastBruiser, PerkType.IceBreath)
                .Name("Ice Breath")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Deals 8 ice DMG to all targets within a cone in front of the beast.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath1)

                .AddPerkLevel()
                .Description("Deals 12 ice DMG to all targets within a cone in front of the beast.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath2)

                .AddPerkLevel()
                .Description("Deals 16 ice DMG to all targets within a cone in front of the beast. Also has an 8DC reflex check to inflict Freezing.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath3)

                .AddPerkLevel()
                .Description("Deals 20 ice DMG to all targets within a cone in front of the beast. Also has a 12DC reflex check to inflict Freezing.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath4)

                .AddPerkLevel()
                .Description("Deals 24 ice DMG to all targets within a cone in front of the beast. Also has a 14DC reflex check to inflict Freezing.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.IceBreath5);
        }


        [NWNEventHandler("item_on_hit")]
        public static void OnEnduranceLinkHit()
        {
            var beast = OBJECT_SELF;
            var item = GetSpellCastItem();

            if (!BeastMastery.IsPlayerBeast(beast) || GetResRef(item) != BeastMastery.BeastClawResref)
            {
                return;
            }

            var player = GetMaster(beast);
            if (GetIsPC(player) && !GetIsDead(player))
            {
                var chance = Perk.GetEffectivePerkLevel(beast, PerkType.EnduranceLink) * 10;

                if (Random.D100(1) <= chance)
                {
                    Stat.RestoreStamina(player, 1);
                }
            }
        }

        private void EnduranceLink()
        {
            _builder.Create(PerkCategoryType.BeastBruiser, PerkType.EnduranceLink)
                .Name("Endurance Link")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Grants a 10% chance to restore 1 STM to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.EnduranceLink1)

                .AddPerkLevel()
                .Description("Grants a 20% chance to restore 1 STM to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.EnduranceLink2)

                .AddPerkLevel()
                .Description("Grants a 30% chance to restore 1 STM to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Bruiser)
                .GrantsFeat(FeatType.EnduranceLink3);
        }
    }
}
