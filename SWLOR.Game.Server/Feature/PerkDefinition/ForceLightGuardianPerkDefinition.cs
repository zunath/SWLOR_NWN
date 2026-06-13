using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class ForceLightGuardianPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            GuardianWard();
            ForcePush();
            DeflectivePresence();
            ForceLeap();
            CourageousResolve();
            ForceIntercept();
            ReflectiveBarrier();
            PurifyingWave();
            LastStandOfTheLight();

            return _builder.Build();
        }

        private void GuardianWard()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.GuardianWard)
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

        private void ForcePush()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForcePush)
                .Name("Force Push")

                .AddPerkLevel()
                .Description("Deals 8 force DMG to one target, knocks down for 2 seconds, and slows movement for 3 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush1)

                .AddPerkLevel()
                .Description("Deals 12 force DMG to the selected target and up to 1 additional target in a line, knocks down for 2 seconds, and slows movement for 3 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 28)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush2)

                .AddPerkLevel()
                .Description("Deals 18 force DMG to the selected target and up to 2 additional targets in a cone, knocks down for 2 seconds, and slows movement for 4 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 48)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush3);
        }

        private void DeflectivePresence()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.LightGuardianDeflectivePresence)
                .Name("Deflective Presence")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Control powers that grant temporary HP, absorb damage, or prevent defeat grant affected allies +4 Attack Deflection for 10 seconds.")
                .IncreasesStat(StatType.LightGuardianPowerAttackDeflection, creature =>
                    EquipmentPredicates.HasMainHandLightsaber(creature) || EquipmentPredicates.HasMainHandVibroblade(creature) ? 4 : 0)
                .IncreasesStat(StatType.LightGuardianPowerAttackDeflectionDurationSeconds, creature =>
                    EquipmentPredicates.HasMainHandLightsaber(creature) || EquipmentPredicates.HasMainHandVibroblade(creature) ? 10 : 0)
                .Price(4)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void ForceLeap()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForceLeap)
                .Name("Force Leap")

                .AddPerkLevel()
                .Description("Leap to a hostile target up to 15m away, dealing 10 force DMG plus WIL scaling and interrupting activation.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 10)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLeap1)

                .AddPerkLevel()
                .Description("Leap to a hostile target up to 18m away, dealing 18 force DMG plus WIL scaling and interrupting activation.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLeap2);
        }

        private void CourageousResolve()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.CourageousResolve)
                .Name("Courageous Resolve")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("When you use a Sense power, you and nearby allies gain +10 Fear Resistance rating, +10 Daze Resistance rating, and +10 Confusion Resistance rating for 12 seconds. Allies with temporary HP from one of your Force powers gain +15 instead.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.LightGuardianSenseResolve, 1);
        }

        private void ForceIntercept()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForceIntercept)
                .Name("Force Intercept")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Leap to an ally up to 15m away and absorb 50% of the next hit they take within 8 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 32)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceIntercept1);
        }

        private void ReflectiveBarrier()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ReflectiveBarrier)
                .Name("Reflective Barrier")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Control powers that grant temporary HP reflect 8% of force and energy damage taken, plus WIL scaling, back to the attacker while the temporary HP remains.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .IncreasesStat(StatType.LightGuardianTemporaryHPReflectiveBarrier, 1);
        }

        private void PurifyingWave()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.PurifyingWave)
                .Name("Purifying Wave")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Releases a 5m wave of focused light, dealing 22 force DMG plus WIL scaling to enemies and removing one minor negative effect (Bleed, Poison, or Hobble) from nearby allies.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.PurifyingWave1);
        }

        private void LastStandOfTheLight()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.LastStandOfTheLight)
                .Name("Last Stand of the Light")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("For 45 seconds, damage that would drop the target below 1 HP is prevented once and the target gains temporary HP equal to 15% of maximum HP plus WIL scaling.")
                .Price(5)
                .RequirementSkill(SkillType.Force, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LastStandOfTheLight1);
        }

    }
}
