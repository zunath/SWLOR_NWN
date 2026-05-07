using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public class StaffPerkDefinition: IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            Bonecrusher();
            BreakPosture();
            CrusherStance();
            CrushingMastery();
            FlowingDefense();
            GroundQuake();
            GuardingStep();
            HeavyHands();
            LineBreaker();
            PatientSentinel();
            PerfectFootwork();
            RibBreaker();
            SentinelGuard();
            SentinelStance();
            ShelterCircle();
            SkullRattle();
            StaffParry();
            SweepingGuard();
            UnmovingCenter();
            Worldbreaker();

            return _builder.Build();
        }

        private void Bonecrusher()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.Bonecrusher)
                .Name("Bonecrusher")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Bonecrusher1)
                .Description("Deals weapon DMG + 50. If the target is Knocked down, they make a Fortitude DC18 check or become Stunned for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 45);
        }

        private void BreakPosture()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.BreakPosture)
                .Name("Break Posture")

                .AddPerkLevel()
                .Description("Critical staff hits inflict Exposed, reducing Defense by 10% for 10 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Staff, 40);
        }

        private void CrusherStance()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.CrusherStance)
                .Name("Crusher Stance")

                .AddPerkLevel()
                .Description("While active, grants +20% Attack and +15% critical chance, but reduces Defense by 20%.")
                .Price(2)
                .RequirementSkill(SkillType.Staff, 15);
        }

        private void CrushingMastery()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.CrushingMastery)
                .Name("Crushing Mastery")

                .AddPerkLevel()
                .Description("Critical staff hits deal +10% damage and restore 2 STM. This can only trigger once every 6 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 12)

                .AddPerkLevel()
                .Description("Bonus damage with staves increases to 2x your MGT modifier and critical chance increases by an additional 10%.")
                .Price(2)
                .RequirementSkill(SkillType.Staff, 32)

                .AddPerkLevel()
                .Description("Staff critical hits deal +20% damage and restore 4 STM. This can only trigger once every 6 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Staff, 48);
        }

        private void FlowingDefense()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.FlowingDefense)
                .Name("Flowing Defense")

                .AddPerkLevel()
                .Description("After dodging or deflecting an attack, your next Staff ability costs 2 less STM.")
                .Price(2)
                .RequirementSkill(SkillType.Staff, 32);
        }

        private void GroundQuake()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.GroundQuake)
                .Name("Ground Quake")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GroundQuake1)
                .Description("Deals weapon DMG + 18 to nearby enemies. Reflex DC14 check to inflict Knockdown for 2 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 25)

                .AddPerkLevel()
                .GrantsFeat(FeatType.GroundQuake2)
                .Description("Deals weapon DMG + 28 to nearby enemies. Reflex DC16 check to inflict Knockdown for 3 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Staff, 35);
        }

        private void GuardingStep()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.GuardingStep)
                .Name("Guarding Step")

                .AddPerkLevel()
                .Description("Gain +25% Evasion and +20% Defense for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 20);
        }

        private void HeavyHands()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.HeavyHands)
                .Name("Heavy Hands")

                .AddPerkLevel()
                .Description("Staff combat abilities deal +10% damage to targets affected by Knockdown or Blind.")
                .Price(2)
                .RequirementSkill(SkillType.Staff, 22);
        }

        private void LineBreaker()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.LineBreaker)
                .Name("Line Breaker")

                .AddPerkLevel()
                .GrantsFeat(FeatType.LineBreaker1)
                .Description("Deals weapon DMG + 18 to enemies in a line. Reflex DC14 check to inflict Disoriented for 12 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 25);
        }

        private void PatientSentinel()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.PatientSentinel)
                .Name("Patient Sentinel")

                .AddPerkLevel()
                .Description("If you have not used a combat ability for 6 seconds, your next Staff ability gains +15% accuracy and deals +15 DMG.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 40);
        }

        private void PerfectFootwork()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.PerfectFootwork)
                .Name("Perfect Footwork")

                .AddPerkLevel()
                .Description("When reduced below 40% HP, gain +30% Evasion for 10 seconds. This can only trigger once every 3 minutes.")
                .Price(4)
                .RequirementSkill(SkillType.Staff, 48);
        }

        private void RibBreaker()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.RibBreaker)
                .Name("Rib Breaker")

                .AddPerkLevel()
                .GrantsFeat(FeatType.RibBreaker1)
                .Description("Deals weapon DMG + 18 and has a Fortitude DC14 check to inflict Weakened, reducing Attack by 10% for 15 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 20)

                .AddPerkLevel()
                .GrantsFeat(FeatType.RibBreaker2)
                .Description("Deals weapon DMG + 30 and has a Fortitude DC16 check to inflict Weakened, reducing Attack by 15% for 15 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 30)

                .AddPerkLevel()
                .GrantsFeat(FeatType.RibBreaker3)
                .Description("Deals weapon DMG + 42 and has a Fortitude DC18 check to inflict Weakened, reducing Attack by 20% for 15 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Staff, 42);
        }

        private void SentinelGuard()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.SentinelGuard)
                .Name("Sentinel Guard")

                .AddPerkLevel()
                .Description("For 12 seconds, allies within 5 meters gain +10 Attack Deflection and you generate extra enmity.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 30);
        }

        private void SentinelStance()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.SentinelStance)
                .Name("Sentinel Stance")

                .AddPerkLevel()
                .Description("While active, grants +15% Evasion and +15 Attack Deflection, but reduces Attack by 15%.")
                .Price(2)
                .RequirementSkill(SkillType.Staff, 15);
        }

        private void ShelterCircle()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.ShelterCircle)
                .Name("Shelter Circle")

                .AddPerkLevel()
                .Description("Allies in an area of effect (sphere) gain +20% Defense and +20% Evasion for 15 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 45);
        }

        private void SkullRattle()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.SkullRattle)
                .Name("Skull Rattle")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SkullRattle1)
                .Description("Deals weapon DMG + 34 and has a Fortitude DC16 check to inflict Dazed for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 38);
        }

        private void StaffParry()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.StaffParry)
                .Name("Staff Parry")

                .AddPerkLevel()
                .Description("Gain +10 Attack Deflection while wielding a staff.")
                .Price(2)
                .RequirementSkill(SkillType.Staff, 8)

                .AddPerkLevel()
                .Description("Gain +20 Attack Deflection total while wielding a staff.")
                .Price(4)
                .RequirementSkill(SkillType.Staff, 18)

                .AddPerkLevel()
                .Description("Gain +30 Attack Deflection total while wielding a staff. Deflecting attacks restores 2 STM.")
                .Price(4)
                .RequirementSkill(SkillType.Staff, 28)

                .AddPerkLevel()
                .Description("Gain +40 Attack Deflection total while wielding a staff. Deflecting attacks restores 4 STM.")
                .Price(4)
                .RequirementSkill(SkillType.Staff, 42);
        }

        private void SweepingGuard()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.SweepingGuard)
                .Name("Sweeping Guard")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SweepingGuard1)
                .Description("Deals weapon DMG + 18 to all nearby enemies. Reflex DC16 check to inflict Knockdown for 2 seconds. You gain +20% Defense for 10 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Staff, 38);
        }

        private void UnmovingCenter()
        {
            _builder.Create(PerkCategoryType.StaffSentinel, PerkType.UnmovingCenter)
                .Name("Unmoving Center")

                .AddPerkLevel()
                .Description("For 20 seconds, you cannot be Knocked down or Dazed, gain +50 Attack Deflection, and staff attacks generate extra enmity.")
                .Price(4)
                .RequirementSkill(SkillType.Staff, 50);
        }

        private void Worldbreaker()
        {
            _builder.Create(PerkCategoryType.StaffCrusher, PerkType.Worldbreaker)
                .Name("Worldbreaker")

                .AddPerkLevel()
                .GrantsFeat(FeatType.Worldbreaker1)
                .Description("Strike the ground. Enemies in an area of effect (sphere) take weapon DMG + 45 and make a Reflex DC18 check or suffer Knockdown for 4 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Staff, 50);
        }
    }
}
