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
    public sealed class BeastTankPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            IronHide();
            Anger();
            FocusAttention();
            GuardingRoar();
            Intercept();
            BodyguardsResolve();
            RampartHide();
            LastGuardian();
            UnbreakableBeast();

            return _builder.Build();
        }

        private void IronHide()
        {
            _builder.Create(PerkCategoryType.BeastTank, PerkType.IronHide)
                .Name("Iron Hide")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast takes 5% less physical and force damage for 3 minutes.")
                .Price(2)
                .RequirementBeastLevel(5)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.IronHide1)

                .AddPerkLevel()
                .Description("The beast takes 8% less physical and force damage for 3 minutes.")
                .Price(3)
                .RequirementBeastLevel(18)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.IronHide2)

                .AddPerkLevel()
                .Description("The beast takes 12% less physical and force damage for 3 minutes.")
                .Price(3)
                .RequirementBeastLevel(38)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.IronHide3);
        }

        private void Anger()
        {
            _builder.Create(PerkCategoryType.BeastTank, PerkType.Anger)
                .Name("Anger")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Goads a single target into attacking the beast.")
                .Price(2)
                .RequirementBeastLevel(8)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.Anger1)

                .AddPerkLevel()
                .Description("Goads a single target into attacking the beast and grants the beast temporary HP equal to 15% of its maximum HP for 30 seconds.")
                .Price(4)
                .RequirementBeastLevel(28)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.Anger2);
        }

        private void FocusAttention()
        {
            _builder.Create(PerkCategoryType.BeastTank, PerkType.FocusAttention)
                .Name("Focus Attention")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FocusAttentionTrait)
                .Description("The beast's enmity generation is increased by 15%.")
                .Price(2)
                .RequirementBeastLevel(12)
                .RequirementBeastRole(BeastRoleType.Tank)
                .IncreasesStat(StatType.EnmityPercentAdjustment, 15)

                .AddPerkLevel()
                .Description("The beast's enmity generation is increased by 30%.")
                .Price(3)
                .RequirementBeastLevel(25)
                .RequirementBeastRole(BeastRoleType.Tank)
                .IncreasesStat(StatType.EnmityPercentAdjustment, 30)

                .AddPerkLevel()
                .Description("The beast's enmity generation is increased by 45% and Anger cooldown is reduced by 3 seconds.")
                .Price(4)
                .RequirementBeastLevel(42)
                .RequirementBeastRole(BeastRoleType.Tank)
                .IncreasesStat(StatType.EnmityPercentAdjustment, 45)
                .IncreasesStat(StatType.AbilityRecastDelayFlatAdjustmentPerkType, (int)PerkType.Anger)
                .IncreasesStat(StatType.AbilityRecastDelayFlatAdjustment, -3);
        }

        private void GuardingRoar()
        {
            _builder.Create(PerkCategoryType.BeastTank, PerkType.GuardingRoar)
                .Name("Guarding Roar")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("Enemies within 5m are goaded into attacking the beast and the beast takes 6% less physical damage for 30 seconds.")
                .Price(3)
                .RequirementBeastLevel(15)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.GuardingRoar1)

                .AddPerkLevel()
                .Description("Enemies within 5m are goaded into attacking the beast and the beast takes 10% less physical damage for 30 seconds.")
                .Price(4)
                .RequirementBeastLevel(35)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.GuardingRoar2)

                .AddPerkLevel()
                .Description("Enemies within 5m are goaded into attacking the beast and the beast takes 15% less physical damage for 30 seconds.")
                .Price(4)
                .RequirementBeastLevel(48)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.GuardingRoar3);
        }

        private void Intercept()
        {
            _builder.Create(PerkCategoryType.BeastTank, PerkType.Intercept)
                .Name("Intercept")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast redirects 35% of the next hit taken by its master within 30 seconds to itself.")
                .Price(3)
                .RequirementBeastLevel(22)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.Intercept1)

                .AddPerkLevel()
                .Description("The beast redirects 50% of the next hit taken by its master within 30 seconds to itself.")
                .Price(3)
                .RequirementBeastLevel(40)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.Intercept2);
        }

        private void BodyguardsResolve()
        {
            _builder.Create(PerkCategoryType.BeastTank, PerkType.BodyguardsResolve)
                .Name("Bodyguard's Resolve")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BodyguardsResolveTrait)
                .Description("The beast takes 10% less damage while its master is below 50% HP.")
                .Price(3)
                .RequirementBeastLevel(30)
                .RequirementBeastRole(BeastRoleType.Tank)
                .IncreasesStat(StatType.DamageTakenPercentAdjustment, creature => IsMasterBelowHalfHP(creature) ? -10 : 0);
        }

        private void RampartHide()
        {
            _builder.Create(PerkCategoryType.BeastTank, PerkType.RampartHide)
                .Name("Rampart Hide")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast gains 20% damage reduction for 30 seconds.")
                .Price(4)
                .RequirementBeastLevel(45)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.RampartHide1);
        }

        private void LastGuardian()
        {
            _builder.Create(PerkCategoryType.BeastTank, PerkType.LastGuardian)
                .Name("Last Guardian")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .GrantsFeat(FeatType.LastGuardianTrait)
                .Description("Once every 3 minutes, when the beast would take fatal damage, prevent that damage and grant temporary HP equal to 20% of its maximum HP for 30 seconds.")
                .Price(4)
                .RequirementBeastLevel(50)
                .RequirementBeastRole(BeastRoleType.Tank)
                .IncreasesStat(StatType.FatalDamageTemporaryHPPercent, 20)
                .IncreasesStat(StatType.FatalDamageTemporaryHPDurationSeconds, 30)
                .IncreasesStat(StatType.FatalDamageTemporaryHPCooldownSeconds, 180);
        }

        private void UnbreakableBeast()
        {
            _builder.Create(PerkCategoryType.BeastTank, PerkType.UnbreakableBeast)
                .Name("Unbreakable Beast")
                .GroupType(PerkGroupType.Beast)

                .AddPerkLevel()
                .Description("The beast becomes immune to knockdown, daze, and forced movement for 30 seconds and gains 25% damage reduction.")
                .Price(5)
                .RequirementBeastLevel(50)
                .RequirementBeastRole(BeastRoleType.Tank)
                .GrantsFeat(FeatType.UnbreakableBeast1)
                .RequirementQuest(BeastMasteryCapstoneQuestDefinition.UnbreakableBeastMasteryQuestId);
        }

        private static bool IsMasterBelowHalfHP(uint creature)
        {
            var master = GetMaster(creature);
            if (!GetIsObjectValid(master))
                return false;

            var maxHP = GetMaxHitPoints(master);
            return maxHP > 0 && GetCurrentHitPoints(master) < maxHP * 0.5f;
        }

    }
}
