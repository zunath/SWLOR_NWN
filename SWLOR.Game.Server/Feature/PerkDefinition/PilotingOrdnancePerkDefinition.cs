using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    /// <summary>
    /// The Ordnance style of Piloting (plan section 6, Appendix A): missiles and torpedoes as area ordnance, ported
    /// from the Throwing Ordnance line. Rows, prices, ranks and text are the Design Bible tab "Piloting".
    /// The traits set space-only stats, because the ground stats of the same line pick their skill through selector
    /// stats that cannot coexist with a pilot's ground perks.
    /// </summary>
    public class PilotingOrdnancePerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Volley();
            ProximityWarheads();
            ShrapnelWarheads();
            FlareMissile();
            ClusterWarheads();
            ConcussionBarrage();
            OrdnanceStance();
            SaturationFire();
            BombardiersRhythm();
            SaturationStrike();

            return _builder.Build();
        }

        private void Volley()
        {
            _builder.Create(PerkCategoryType.PilotingOrdnance, PerkType.OrdnanceVolley)
                .Name("Volley")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OrdnanceVolley1)
                .Description("Fire a missile at your target that deals weapon DMG + 7 to enemies near it and inflicts Burn for 30 s.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.OrdnanceVolley2)
                .Description("Fire a missile at your target that deals weapon DMG + 15 to enemies near it and inflicts Burn for 30 s.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 12)

                .AddPerkLevel()
                .GrantsFeat(FeatType.OrdnanceVolley3)
                .Description("Fire a missile at your target that deals weapon DMG + 24 to enemies near it and inflicts Burn for 30 s.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 28)

                .AddPerkLevel()
                .GrantsFeat(FeatType.OrdnanceVolley4)
                .Description("Fire a missile at your target that deals weapon DMG + 32 to enemies near it and inflicts Burn for 30 s.")
                .Price(5)
                .RequirementSkill(SkillType.Piloting, 40);
        }

        private void ProximityWarheads()
        {
            _builder.Create(PerkCategoryType.PilotingOrdnance, PerkType.OrdnanceProximityWarheads)
                .Name("Proximity Warheads")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OrdnanceProximityWarheadsTrait)
                .Description("Launcher hits have a 15% chance to deal 8 damage to the target and up to four other enemies near it. This splash cannot trigger other on-hit effects.")
                .IncreasesStat(StatType.SpaceLauncherSplashChance, 15)
                .IncreasesStat(StatType.SpaceLauncherSplashDamage, 8)
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 5);
        }

        private void ShrapnelWarheads()
        {
            _builder.Create(PerkCategoryType.PilotingOrdnance, PerkType.OrdnanceShrapnelWarheads)
                .Name("Shrapnel Warheads")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OrdnanceShrapnelWarheadsTrait)
                .Description("Area launcher abilities inflict fragmentation for 3 damage every 3 s for 30 s.")
                .IncreasesStat(StatType.SpaceLauncherAreaFragmentationDamage, 3)
                .IncreasesStat(StatType.SpaceLauncherAreaFragmentationPulseSeconds, 3)
                .IncreasesStat(StatType.SpaceLauncherAreaFragmentationDurationSeconds, 30)
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 8)

                .AddPerkLevel()
                .Description("Area launcher abilities inflict fragmentation for 5 damage every 3 s for 30 s.")
                .IncreasesStat(StatType.SpaceLauncherAreaFragmentationDamage, 5)
                .IncreasesStat(StatType.SpaceLauncherAreaFragmentationPulseSeconds, 3)
                .IncreasesStat(StatType.SpaceLauncherAreaFragmentationDurationSeconds, 30)
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 22)

                .AddPerkLevel()
                .Description("Area launcher abilities inflict fragmentation for 7 damage every 3 s for 30 s.")
                .IncreasesStat(StatType.SpaceLauncherAreaFragmentationDamage, 7)
                .IncreasesStat(StatType.SpaceLauncherAreaFragmentationPulseSeconds, 3)
                .IncreasesStat(StatType.SpaceLauncherAreaFragmentationDurationSeconds, 30)
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 45);
        }

        private void FlareMissile()
        {
            _builder.Create(PerkCategoryType.PilotingOrdnance, PerkType.OrdnanceFlareMissile)
                .Name("Flare Missile")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OrdnanceFlareMissile1)
                .Description("Fire a launcher at your target for weapon DMG + 12 and inflict Blind for 15 s.")
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 10)

                .AddPerkLevel()
                .GrantsFeat(FeatType.OrdnanceFlareMissile2)
                .Description("Fire a launcher at your target for weapon DMG + 24 and inflict Blind for 15 s.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 30)

                .AddPerkLevel()
                .GrantsFeat(FeatType.OrdnanceFlareMissile3)
                .Description("Fire a launcher at your target for weapon DMG + 36 and inflict Blind for 15 s.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 38);
        }

        private void ClusterWarheads()
        {
            _builder.Create(PerkCategoryType.PilotingOrdnance, PerkType.OrdnanceClusterWarheads)
                .Name("Cluster Warheads")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OrdnanceClusterWarheadsTrait)
                .Description("Area launcher abilities hit one additional enemy near the target for +10 DMG.")
                .IncreasesStat(StatType.SpaceLauncherAreaClusterDamageBonus, 10)
                .IncreasesStat(StatType.SpaceLauncherAreaClusterMaximumTargets, 1)
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 15);
        }

        private void ConcussionBarrage()
        {
            _builder.Create(PerkCategoryType.PilotingOrdnance, PerkType.OrdnanceConcussionBarrage)
                .Name("Concussion Barrage")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OrdnanceConcussionBarrage1)
                .Description("Deal weapon DMG + 16 to up to five enemies near your ship and inflict Dazed for 15 s.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.OrdnanceConcussionBarrage2)
                .Description("Deal weapon DMG + 30 to up to five enemies near your ship and inflict Dazed for 15 s.")
                .Price(3)
                .RequirementSkill(SkillType.Piloting, 32);
        }

        private void OrdnanceStance()
        {
            _builder.Create(PerkCategoryType.PilotingOrdnance, PerkType.OrdnanceShipStance)
                .Name("Ordnance Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OrdnanceShipStance)
                .Description("While active, area launcher abilities deal +10% damage, but single-target launcher damage is reduced by 10%.")
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 20);
        }

        private void SaturationFire()
        {
            _builder.Create(PerkCategoryType.PilotingOrdnance, PerkType.OrdnanceSaturationFire)
                .Name("Saturation Fire")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OrdnanceSaturationFireTrait)
                .Description("Targets hit by two or more area launcher abilities within 30 s suffer Exposed for 30 s.")
                .IncreasesStat(StatType.SpaceLauncherAreaHitSequenceCountRequired, 2)
                .IncreasesStat(StatType.SpaceLauncherAreaHitSequenceWindowSeconds, 30)
                .IncreasesStat(StatType.SpaceLauncherAreaHitSequenceExposedDurationSeconds, 30)
                .Price(4)
                .RequirementSkill(SkillType.Piloting, 25);
        }

        private void BombardiersRhythm()
        {
            _builder.Create(PerkCategoryType.PilotingOrdnance, PerkType.OrdnanceBombardiersRhythm)
                .Name("Bombardier's Rhythm")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OrdnanceBombardiersRhythmTrait)
                .Description("After a launcher ability hits two or more enemies, restore 4 STM.")
                .IncreasesStat(StatType.SpaceLauncherAreaMinTargetsStaminaRestoreThreshold, 2)
                .IncreasesStat(StatType.SpaceLauncherAreaMinTargetsStaminaRestore, 4)
                .Price(2)
                .RequirementSkill(SkillType.Piloting, 35);
        }

        private void SaturationStrike()
        {
            _builder.Create(PerkCategoryType.PilotingOrdnance, PerkType.OrdnanceSaturationStrike)
                .Name("Saturation Strike")

                .AddPerkLevel()
                .GrantsFeat(FeatType.OrdnanceSaturationStrike)
                .Description("Deal weapon DMG + 20 to enemies near your ship. For 45 s, area launcher abilities leave fragmentation zones that deal 8 damage every 6 s.")
                .Price(6)
                .RequirementSkill(SkillType.Piloting, 50);
        }
    }
}
