using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Feature.QuestDefinition;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class ForceLightGuardianPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            GuardianWard();
            DeflectivePresence();
            CourageousResolve();
            ForceIntercept();
            ReflectiveBarrier();
            PurifyingWave();
            LastStandOfTheLight();

            return _builder.Build();
        }

        private void GuardianWard()
        {
            _builder.Create(PerkCategoryType.ForceControl, PerkType.GuardianWard)
                .Name("Guardian Ward")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Grants a single ally temporary HP equal to 6% of the target's maximum HP plus WIL scaling for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 2)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.GuardianWard1)

                .AddPerkLevel()
                .Description("Grants a single ally temporary HP equal to 9% of the target's maximum HP plus WIL scaling for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.GuardianWard2)

                .AddPerkLevel()
                .Description("Grants a single ally temporary HP equal to 12% of the target's maximum HP plus WIL scaling for 30 seconds.")
                .Price(5)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.GuardianWard3)

                .AddPerkLevel()
                .Description("Grants a single ally temporary HP equal to 15% of the target's maximum HP plus WIL scaling for 30 seconds.")
                .Price(5)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.GuardianWard4);
        }

        private void DeflectivePresence()
        {
            _builder.Create(PerkCategoryType.ForceControl, PerkType.LightGuardianDeflectivePresence)
                .Name("Protective Presence")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .GrantsFeat(FeatType.LightGuardianDeflectivePresenceTrait)
                .Description("Control powers that grant temporary HP, absorb damage, or prevent defeat grant affected allies +4 Ranged Deflection for 30 seconds.")
                .IncreasesStat(StatType.LightGuardianPowerAttackDeflection, 4)
                .IncreasesStat(StatType.LightGuardianPowerAttackDeflectionDurationSeconds, 30)
                .Price(4)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void CourageousResolve()
        {
            _builder.Create(PerkCategoryType.ForceSense, PerkType.CourageousResolve)
                .Name("Courageous Resolve")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .GrantsFeat(FeatType.CourageousResolveTrait)
                .Description("When you use a Sense power, you and allies within 5m gain +10 Mind Resistance rating for 30 seconds. Allies with temporary HP from one of your Force powers gain +15 instead.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.LightGuardianSenseResolve, 1);
        }

        private void ForceIntercept()
        {
            _builder.Create(PerkCategoryType.ForceSense, PerkType.ForceIntercept)
                .Name("Force Intercept")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Leap to an ally up to 15m away and absorb 50% of the next hit they take within 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 32)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceIntercept1);
        }

        private void ReflectiveBarrier()
        {
            _builder.Create(PerkCategoryType.ForceControl, PerkType.ReflectiveBarrier)
                .Name("Reflective Barrier")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ReflectiveBarrierTrait)
                .Description("Control powers that grant temporary HP reflect 8% of force and energy damage taken, plus WIL scaling, back to the attacker while the temporary HP remains.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.LightGuardianTemporaryHPReflectiveBarrier, 1);
        }

        private void PurifyingWave()
        {
            _builder.Create(PerkCategoryType.ForceAlter, PerkType.PurifyingWave)
                .Name("Purifying Wave")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Releases a 5m wave of focused light, dealing 22 force DMG plus WIL scaling to enemies and removing one minor negative effect (Bleed, Poison, or Hobble) from allies within 5m.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.PurifyingWave1);
        }

        private void LastStandOfTheLight()
        {
            _builder.Create(PerkCategoryType.ForceAlter, PerkType.LastStandOfTheLight)
                .Name("Last Stand of the Light")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("For 45 seconds, damage that would drop the target below 1 HP is prevented once and the target gains temporary HP equal to 15% of maximum HP plus WIL scaling.")
                .Price(5)
                .RequirementSkill(SkillType.Force, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LastStandOfTheLight1)
                .RequirementQuest(ForceCapstoneQuestDefinition.LastStandOfTheLightMasteryQuestId);
        }

    }
}
