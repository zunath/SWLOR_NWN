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
    public sealed class BeastForcePerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            ForceTouch();
            Innervate();
            WardingHowl();
            ForceLink();
            PsychicCry();
            MindfulHide();
            ForceBondedBeast();

            return _builder.Build();
        }

        private void ForceTouch()
        {
            _builder.Create(PerkCategoryType.BeastForce, PerkType.ForceTouch)
                .Name("Force Touch")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast's next attack deals +12 force DMG.")
                .Price(2)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch1)

                .AddPerkLevel()
                .Description("The beast's next attack deals +22 force DMG.")
                .Price(3)
                .RequirementBeastLevel(18)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch2)

                .AddPerkLevel()
                .Description("The beast's next attack deals +34 force DMG.")
                .Price(4)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceTouch3);
        }

        private void Innervate()
        {
            _builder.Create(PerkCategoryType.BeastForce, PerkType.Innervate)
                .Name("Innervate")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast restores 6% of maximum HP to a single ally.")
                .Price(2)
                .RequirementBeastLevel(8)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate1)

                .AddPerkLevel()
                .Description("The beast restores 10% of maximum HP to a single ally.")
                .Price(2)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate2)

                .AddPerkLevel()
                .Description("The beast restores 14% of maximum HP to a single ally.")
                .Price(4)
                .RequirementBeastLevel(42)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.Innervate3);
        }

        private void WardingHowl()
        {
            _builder.Create(PerkCategoryType.BeastForce, PerkType.WardingHowl)
                .Name("Warding Howl")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast causes allies within 5m to take 5% less force damage for 30 seconds.")
                .Price(3)
                .RequirementBeastLevel(12)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.WardingHowl1)

                .AddPerkLevel()
                .Description("The beast causes allies within 5m to take 8% less force damage for 30 seconds.")
                .Price(4)
                .RequirementBeastLevel(28)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.WardingHowl2)

                .AddPerkLevel()
                .Description("The beast causes allies within 5m to take 12% less force damage for 30 seconds.")
                .Price(4)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.WardingHowl3);
        }

        private void ForceLink()
        {
            _builder.Create(PerkCategoryType.BeastForce, PerkType.ForceLink)
                .Name("Force Link")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ForceLinkTrait)
                .Description("The beast has a 10% chance to restore 1 FP to its master when it lands an attack.")
                .Price(3)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Force)
                .IncreasesStat(StatType.AutoAttackMasterFPRestoreChance, 10)
                .IncreasesStat(StatType.AutoAttackMasterFPRestore, 1)

                .AddPerkLevel()
                .Description("The beast has a 20% chance to restore 1 FP to its master when it lands an attack.")
                .Price(3)
                .RequirementBeastLevel(30)
                .RequirementBeastRole(BeastRoleType.Force)
                .IncreasesStat(StatType.AutoAttackMasterFPRestoreChance, 20)
                .IncreasesStat(StatType.AutoAttackMasterFPRestore, 1)

                .AddPerkLevel()
                .Description("The beast has a 30% chance to restore 1 FP to its master when it lands an attack.")
                .Price(4)
                .RequirementBeastLevel(48)
                .RequirementBeastRole(BeastRoleType.Force)
                .IncreasesStat(StatType.AutoAttackMasterFPRestoreChance, 30)
                .IncreasesStat(StatType.AutoAttackMasterFPRestore, 1);
        }

        private void PsychicCry()
        {
            _builder.Create(PerkCategoryType.BeastForce, PerkType.PsychicCry)
                .Name("Psychic Cry")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast reduces the hit chance of enemies within 5m by 5% for 30 seconds.")
                .Price(3)
                .RequirementBeastLevel(22)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.PsychicCry1)

                .AddPerkLevel()
                .Description("The beast reduces the hit chance of enemies within 5m by 8% and increases their force damage taken by 5% for 30 seconds.")
                .Price(3)
                .RequirementBeastLevel(40)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.PsychicCry2)

                .AddPerkLevel()
                .Description("The beast reduces the hit chance of enemies within 5m by 12% and increases their force damage taken by 8% for 30 seconds.")
                .Price(3)
                .RequirementBeastLevel(50)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.PsychicCry3);
        }

        private void MindfulHide()
        {
            _builder.Create(PerkCategoryType.BeastForce, PerkType.MindfulHide)
                .Name("Mindful Hide")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .GrantsFeat(FeatType.MindfulHideTrait)
                .Description("The beast takes 8% less force damage and gains +10 Mind Resistance.")
                .Price(3)
                .RequirementBeastLevel(38)
                .RequirementBeastRole(BeastRoleType.Force)
                .IncreasesStat(StatType.ForceDamageTakenPercentAdjustment, -8)
                .IncreasesStat(StatType.MindResistance, 10);
        }

        private void ForceBondedBeast()
        {
            _builder.Create(PerkCategoryType.BeastForce, PerkType.ForceBondedBeast)
                .Name("Force-Bonded Beast")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast and its master take 10% less force damage and restore 1 FP every 3 seconds for 30 seconds.")
                .Price(5)
                .RequirementBeastLevel(50)
                .RequirementBeastRole(BeastRoleType.Force)
                .GrantsFeat(FeatType.ForceBondedBeast1)
                .RequirementQuest(BeastMasteryCapstoneQuestDefinition.ForceBondedBeastMasteryQuestId);
        }

    }
}
