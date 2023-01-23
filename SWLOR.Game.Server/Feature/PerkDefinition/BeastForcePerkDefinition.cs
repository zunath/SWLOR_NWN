using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using System.Collections.Generic;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class BeastForcePerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            ForceTouch();
            Innervate();
            ForceLink();

            return _builder.Build();
        }

        private void ForceTouch()
        {
            _builder.Create(PerkCategoryType.BeastForce, PerkType.ForceTouch)
                .Name("Force Touch")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 12 force DMG.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch1)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 16 force DMG.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch2)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 20 force DMG.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch3)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 24 force DMG.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch4)

                .AddPerkLevel()
                .Description("The beast's next attack deals an additional 28 force DMG.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch5);
        }

        private void Innervate()
        {
            _builder.Create(PerkCategoryType.BeastForce, PerkType.Innervate)
                .Name("Innervate")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast restores 30 HP to a single target.")
                .Price(1)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate1)

                .AddPerkLevel()
                .Description("The beast restores 40 HP to a single target.")
                .Price(1)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate2)

                .AddPerkLevel()
                .Description("The beast restores 60 HP to a single target.")
                .Price(1)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate3)

                .AddPerkLevel()
                .Description("The beast restores 80 HP to a single target.")
                .Price(2)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate4)

                .AddPerkLevel()
                .Description("The beast restores 120 HP to a single target.")
                .Price(2)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate5);
        }

        [NWNEventHandler("item_on_hit")]
        public static void OnForceLinkHit()
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
                var chance = Perk.GetEffectivePerkLevel(beast, PerkType.ForceLink) * 10;

                if (Random.D100(1) <= chance)
                {
                    Stat.RestoreFP(player, 1);
                }
            }
        }
        private void ForceLink()
        {
            _builder.Create(PerkCategoryType.BeastForce, PerkType.ForceLink)
                .Name("Force Link")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Grants a 10% chance to restore 1 FP to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceLink1)

                .AddPerkLevel()
                .Description("Grants a 20% chance to restore 1 FP to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceLink2)

                .AddPerkLevel()
                .Description("Grants a 30% chance to restore 1 FP to the Beastmaster when the beast lands an attack on an enemy.")
                .Price(4)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceLink3);
        }
    }
}
