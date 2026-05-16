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
            SoothingGuard();
            AuraOfCourage();
            ForceIntercept();
            ReflectiveBarrier();
            PurifyingWave();
            BastionOfLight();
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
                .Price(2)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.GuardianWard1)

                .AddPerkLevel()
                .Description("Grants a single ally temporary HP equal to 9% of the target's maximum HP plus WIL scaling for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 8)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.GuardianWard2)

                .AddPerkLevel()
                .Description("Grants a single ally temporary HP equal to 12% of the target's maximum HP plus WIL scaling for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 25)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.GuardianWard3)

                .AddPerkLevel()
                .Description("Grants a single ally temporary HP equal to 15% of the target's maximum HP plus WIL scaling for 30 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 42)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.GuardianWard4);
        }

        private void ForcePush()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForcePush)
                .Name("Force Push")

                .AddPerkLevel()
                .Description("Knock down one target for 2 seconds. slows movement for 3 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 5)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush1)

                .AddPerkLevel()
                .Description("Knock down up to 2 targets in a line for 2 seconds. slows movement for 3 seconds.")
                .Price(2)
                .RequirementSkill(SkillType.Force, 22)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForcePush2)

                .AddPerkLevel()
                .Description("Knock down up to 3 targets in a cone for 2 seconds. slows movement for 4 seconds.")
                .Price(3)
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
                .Description("While a one-handed lightsaber or vibroblade is equipped, Light Guardian combat powers increase attack deflection effectiveness by 8% for 10 seconds.")
                .IncreasesStat(StatType.LightGuardianPowerAttackDeflection, creature =>
                    EquipmentPredicates.HasMainHandLightsaber(creature) || EquipmentPredicates.HasMainHandVibroblade(creature) ? 8 : 0)
                .IncreasesStat(StatType.LightGuardianPowerAttackDeflectionDurationSeconds, creature =>
                    EquipmentPredicates.HasMainHandLightsaber(creature) || EquipmentPredicates.HasMainHandVibroblade(creature) ? 10 : 0)
                .Price(3)
                .RequirementSkill(SkillType.Force, 12)
                .RequirementCharacterType(CharacterType.ForceSensitive);
        }

        private void ForceLeap()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForceLeap)
                .Name("Force Leap")

                .AddPerkLevel()
                .Description("Leap to a hostile target up to 15m away, dealing 10 force DMG plus WIL scaling and interrupting activation.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 15)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLeap1)

                .AddPerkLevel()
                .Description("Leap to a hostile target up to 18m away, dealing 18 force DMG plus WIL scaling and interrupting activation.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 35)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceLeap2);
        }

        private void SoothingGuard()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.SoothingGuard)
                .Name("Soothing Guard")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Removes one poison, bleed, burn, shock, or disease effect from an ally and grants 10% damage reduction for 8 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 18)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.SoothingGuard1);
        }

        private void AuraOfCourage()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.AuraOfCourage)
                .Name("Courageous Resolve")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Nearby party members take 5% less Force damage and gain +10% resistance to fear, daze, and confusion for 30 seconds.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 28)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.AuraOfCourage1);
        }

        private void ForceIntercept()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ForceIntercept)
                .Name("Force Intercept")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Leap to an ally up to 15m away and absorb 50% of the next hit they take within 8 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 30)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ForceIntercept1);
        }

        private void ReflectiveBarrier()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.ReflectiveBarrier)
                .Name("Reflective Barrier")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Grants a single ally a barrier for 20 seconds. While active, 15% of force and energy damage taken, plus WIL scaling, is reflected to the attacker.")
                .Price(3)
                .RequirementSkill(SkillType.Force, 38)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.ReflectiveBarrier1);
        }

        private void PurifyingWave()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.PurifyingWave)
                .Name("Purifying Wave")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Removes one major negative effect from nearby allies and restores HP equal to 8% of each target's maximum HP plus WIL scaling.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 40)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.PurifyingWave1);
        }

        private void BastionOfLight()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.BastionOfLight)
                .Name("Bastion of Light")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("Nearby allies gain temporary HP equal to 10% of maximum HP plus WIL scaling and take 10% less force damage for 20 seconds.")
                .Price(4)
                .RequirementSkill(SkillType.Force, 45)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.BastionOfLight1);
        }

        private void LastStandOfTheLight()
        {
            _builder.Create(PerkCategoryType.ForceLight, PerkType.LastStandOfTheLight)
                .Name("Last Stand of the Light")
                .ForceAffinity(ForceAffinityType.Light)

                .AddPerkLevel()
                .Description("For 12 seconds, damage that would drop the target below 1 HP is prevented once and the target gains temporary HP equal to 20% of maximum HP plus WIL scaling.")
                .Price(5)
                .RequirementSkill(SkillType.Force, 50)
                .RequirementCharacterType(CharacterType.ForceSensitive)
                .GrantsFeat(FeatType.LastStandOfTheLight1);
        }

    }
}
