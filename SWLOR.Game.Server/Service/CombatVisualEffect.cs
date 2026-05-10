using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    public static class CombatVisualEffect
    {
        private const float DurationVisualPulseSeconds = 1.25f;

        public static void ApplyToObject(uint target, VisualEffect visualEffect, float scale = 1f)
        {
            if (!GetIsObjectValid(target) || visualEffect == VisualEffect.None)
                return;

            var effect = EffectVisualEffect(visualEffect, false, scale);
            if (IsDurationVisualEffect(visualEffect))
            {
                ApplyEffectToObject(DurationType.Temporary, effect, target, DurationVisualPulseSeconds);
                return;
            }

            ApplyEffectToObject(DurationType.Instant, effect, target);
        }

        public static void ApplyAtLocation(Location location, VisualEffect visualEffect, float scale = 1f)
        {
            if (!GetIsObjectValid(GetAreaFromLocation(location)) || visualEffect == VisualEffect.None)
                return;

            var effect = EffectVisualEffect(visualEffect, false, scale);
            if (IsDurationVisualEffect(visualEffect))
            {
                ApplyEffectAtLocation(DurationType.Temporary, effect, location, DurationVisualPulseSeconds);
                return;
            }

            ApplyEffectAtLocation(DurationType.Instant, effect, location);
        }

        public static void ApplyStatusEffectVisual(uint target, Type statusEffect, bool isHostile)
        {
            var visualEffect = isHostile
                ? GetHostileStatusVisualEffect(statusEffect)
                : GetBeneficialStatusVisualEffect(statusEffect);

            ApplyToObject(target, visualEffect);
        }

        public static VisualEffect GetHostileImpactVisualEffect(
            SkillType skillType,
            int baseDamage,
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects)
        {
            var statusVisualEffect = GetHostileStatusVisualEffect(GetPrimaryStatusEffect(statusEffect, additionalStatusEffects));
            if (statusVisualEffect != VisualEffect.None)
                return statusVisualEffect;

            if (baseDamage <= 0)
                return VisualEffect.None;

            return skillType switch
            {
                SkillType.Force => VisualEffect.Vfx_Imp_Starburst_Red,
                SkillType.Devices => VisualEffect.Vfx_Imp_Mirv,
                SkillType.Pistol => VisualEffect.Vfx_Com_Blood_Spark_Medium,
                SkillType.Rifle => VisualEffect.Vfx_Com_Blood_Spark_Medium,
                SkillType.Throwing => VisualEffect.Vfx_Imp_Mirv,
                SkillType.Lightsaber => VisualEffect.Vfx_Com_Sparks_Parry,
                SkillType.Saberstaff => VisualEffect.Vfx_Com_Sparks_Parry,
                SkillType.TwinBlade => VisualEffect.Vfx_Com_Special_White_Blue,
                SkillType.Katar => VisualEffect.Vfx_Com_Special_Red_White,
                SkillType.Vibroknife => VisualEffect.Vfx_Com_Blood_Spark_Medium,
                SkillType.Vibroblade => VisualEffect.Vfx_Com_Special_Red_Orange,
                SkillType.HeavyVibroblade => VisualEffect.Vfx_Com_Chunk_Red_Medium,
                SkillType.Staff => VisualEffect.Vfx_Com_Chunk_Red_Medium,
                SkillType.Spear => VisualEffect.Vfx_Com_Special_White_Orange,
                _ => VisualEffect.Vfx_Com_Blood_Spark_Medium
            };
        }

        public static VisualEffect GetAreaImpactVisualEffect(
            SkillType skillType,
            int baseDamage,
            Type statusEffect,
            IEnumerable<Type> additionalStatusEffects,
            CombatImpactAreaShape shape)
        {
            var statusVisualEffect = GetAreaStatusVisualEffect(GetPrimaryStatusEffect(statusEffect, additionalStatusEffects));
            if (statusVisualEffect != VisualEffect.None)
                return statusVisualEffect;

            if (baseDamage <= 0)
                return VisualEffect.None;

            return skillType switch
            {
                SkillType.Devices => VisualEffect.Fnf_Fireball,
                SkillType.Throwing => VisualEffect.Fnf_Fireball,
                SkillType.Force => VisualEffect.Vfx_Fnf_Mass_Mind_Affecting,
                SkillType.Pistol => VisualEffect.Vfx_Fnf_Sound_Burst_Silent,
                SkillType.Rifle => VisualEffect.Vfx_Fnf_Sound_Burst_Silent,
                SkillType.Staff => VisualEffect.Vfx_Imp_Dust_Explosion,
                SkillType.HeavyVibroblade => VisualEffect.Vfx_Imp_Dust_Explosion,
                SkillType.Lightsaber => VisualEffect.Vfx_Fnf_Swinging_Blade,
                SkillType.Saberstaff => VisualEffect.Vfx_Fnf_Swinging_Blade,
                SkillType.TwinBlade => VisualEffect.Vfx_Fnf_Swinging_Blade,
                SkillType.Vibroblade => VisualEffect.Vfx_Fnf_Swinging_Blade,
                SkillType.Vibroknife => VisualEffect.Vfx_Fnf_Swinging_Blade,
                SkillType.Katar => VisualEffect.Vfx_Fnf_Swinging_Blade,
                SkillType.Spear => VisualEffect.Vfx_Fnf_Swinging_Blade,
                _ => VisualEffect.Vfx_Fnf_Screen_Bump
            };
        }

        private static VisualEffect GetHostileStatusVisualEffect(Type statusEffect)
        {
            var name = GetStatusEffectName(statusEffect);
            if (string.IsNullOrWhiteSpace(name))
                return VisualEffect.None;

            if (NameIs(name, "BlindStatusEffect", "FlashStatusEffect"))
                return VisualEffect.Vfx_Imp_Blind_Deaf_M;

            if (NameIs(name, "DazedStatusEffect", "ForceStunStatusEffect"))
                return VisualEffect.Vfx_Imp_Dazed_S;

            if (NameIs(name, "DisorientedStatusEffect", "FoggyMindStatusEffect", "MindTrickStatusEffect"))
                return VisualEffect.Vfx_Imp_Confusion_S;

            if (NameIs(name, "StunnedStatusEffect"))
                return VisualEffect.Vfx_Imp_Stun;

            if (NameIs(name, "KnockdownStatusEffect"))
                return VisualEffect.Vfx_Imp_Bigbys_Forceful_Hand;

            if (NameIs(name, "ToxinStatusEffect", "PoisonStatusEffect"))
                return VisualEffect.Vfx_Imp_Poison_S;

            if (NameIs(name, "BurnStatusEffect"))
                return VisualEffect.Vfx_Imp_Flame_S;

            if (NameIs(name, "ShockStatusEffect"))
                return VisualEffect.Vfx_Imp_Head_Electricity;

            if (NameIs(name,
                    "SunderStatusEffect",
                    "WeakenedStatusEffect",
                    "ExposedStatusEffect",
                    "HemorrhageStatusEffect",
                    "BreachStatusEffect",
                    "CrushingBlowStatusEffect",
                    "FlankingBarrageStatusEffect"))
                return VisualEffect.Vfx_Imp_Reduce_Ability_Score;

            if (NameIs(name,
                    "VitalStrikeStatusEffect",
                    "MarkedForDeathStatusEffect",
                    "DuelistsChallengeStatusEffect",
                    "ExposeWeakPointStatusEffect",
                    "MarkingTossStatusEffect"))
                return VisualEffect.Vfx_Imp_Harm;

            if (NameIs(name, "HamstringStatusEffect", "HobbleStatusEffect", "ExhaustedStatusEffect", "ImmobilizedStatusEffect", "ShadowStrikeStatusEffect"))
                return VisualEffect.Vfx_Imp_Slow;

            if (NameIs(name, "ForceDisruptionStatusEffect", "ForceErosionStatusEffect", "FracturedFocusStatusEffect", "ForceSuppressionStatusEffect"))
                return VisualEffect.Vfx_Imp_Head_Mind;

            if (NameIs(name, "EssenceDrainStatusEffect", "LifeSiphonStatusEffect"))
                return VisualEffect.Vfx_Imp_Negative_Energy;

            if (NameIs(name, "TauntingDeflectionStatusEffect", "RoarStatusEffect", "ScreechStatusEffect", "FrenziedShoutStatusEffect"))
                return VisualEffect.Vfx_Fnf_Howl_War_Cry;

            if (NameIs(name, "IncapacitateStatusEffect"))
                return VisualEffect.Vfx_Imp_Sleep;

            if (NameIs(name, "ForcebaneStatusEffect", "DisruptionFieldStatusEffect", "CripplingDefenseStatusEffect"))
                return VisualEffect.Vfx_Imp_Pulse_Negative;

            if (NameIs(name, "DecoyStatusEffect"))
                return VisualEffect.Vfx_Imp_Charm;

            if (NameIs(name, "CreepingTerrorStatusEffect"))
                return VisualEffect.Vfx_Imp_Doom;

            return VisualEffect.None;
        }

        private static VisualEffect GetAreaStatusVisualEffect(Type statusEffect)
        {
            var name = GetStatusEffectName(statusEffect);
            if (string.IsNullOrWhiteSpace(name))
                return VisualEffect.None;

            if (NameIs(name, "BlindStatusEffect", "FlashStatusEffect"))
                return VisualEffect.Fnf_Blinddeaf;

            if (NameIs(name, "DazedStatusEffect", "StunnedStatusEffect", "ForceStunStatusEffect", "DisorientedStatusEffect", "FoggyMindStatusEffect", "MindTrickStatusEffect"))
                return VisualEffect.Vfx_Fnf_Sound_Burst_Silent;

            if (NameIs(name, "ToxinStatusEffect", "PoisonStatusEffect"))
                return VisualEffect.Vfx_Fnf_Gas_Explosion_Nature;

            if (NameIs(name, "BurnStatusEffect"))
                return VisualEffect.Vfx_Fnf_Gas_Explosion_Fire;

            if (NameIs(name, "ForceDisruptionStatusEffect", "ForceErosionStatusEffect", "FracturedFocusStatusEffect", "ForcebaneStatusEffect", "DisruptionFieldStatusEffect", "ForceSuppressionStatusEffect"))
                return VisualEffect.Vfx_Fnf_Mass_Mind_Affecting;

            if (NameIs(name, "KnockdownStatusEffect"))
                return VisualEffect.Vfx_Imp_Dust_Explosion;

            if (NameIs(name,
                    "ExposedStatusEffect",
                    "SunderStatusEffect",
                    "WeakenedStatusEffect",
                    "HemorrhageStatusEffect",
                    "HamstringStatusEffect",
                    "HobbleStatusEffect",
                    "BreachStatusEffect",
                    "CrushingBlowStatusEffect",
                    "FlankingBarrageStatusEffect",
                    "ShadowStrikeStatusEffect",
                    "ExposeWeakPointStatusEffect",
                    "MarkingTossStatusEffect"))
                return VisualEffect.Vfx_Fnf_Swinging_Blade;

            return VisualEffect.None;
        }

        private static VisualEffect GetBeneficialStatusVisualEffect(Type statusEffect)
        {
            var name = GetStatusEffectName(statusEffect);
            if (string.IsNullOrWhiteSpace(name))
                return VisualEffect.None;

            if (NameStartsWith(name, "BurstOfSpeed", "Hasten", "SoldiersSpeed") ||
                NameIs(name, "SnapRollStatusEffect", "ToxicRushStatusEffect"))
                return VisualEffect.Vfx_Imp_Haste;

            if (NameStartsWith(name, "AdrenalStim", "ForceHeal", "Rejuvenation"))
                return VisualEffect.Vfx_Imp_Healing_M;

            if (NameStartsWith(name, "Shielding", "StasisField", "Premonition", "ForceValor") ||
                NameIs(name,
                    "AbsoluteDefenseStatusEffect",
                    "AdamantineGuardStatusEffect",
                    "BastionStanceStatusEffect",
                    "DefensiveStanceStatusEffect",
                    "DeflectingAuraStatusEffect",
                    "DeflectivePresenceStatusEffect",
                    "GuardingStepStatusEffect",
                    "GuardiansResolveStatusEffect",
                    "ImpenetrableGuardStatusEffect",
                    "IronWallStanceStatusEffect",
                    "RampartStatusEffect",
                    "SentinelGuardStatusEffect",
                    "SentinelStanceStatusEffect",
                    "ShelterCircleStatusEffect",
                    "ShieldWallStatusEffect",
                    "TwinGuardStanceStatusEffect",
                    "TwinInterceptStatusEffect",
                    "UnmovingCenterStatusEffect"))
                return VisualEffect.Vfx_Imp_Ac_Bonus;

            if (NameIs(name, "ForceWardingStatusEffect"))
                return VisualEffect.Vfx_Imp_Globe_Use;

            if (NameStartsWith(name, "ForceBody", "ForceMind", "ForceInspiration", "CombatEnhancement", "BolsterArmor", "BolsterAttack"))
                return VisualEffect.Vfx_Imp_Improve_Ability_Score;

            if (NameIs(name,
                    "BrutalAssaultStatusEffect",
                    "ChargeStatusEffect",
                    "DeadlyPrecisionStatusEffect",
                    "DuelistStanceStatusEffect",
                    "FerocityStanceStatusEffect",
                    "FinalFormStatusEffect",
                    "FlankingStanceStatusEffect",
                    "FocusedStanceStatusEffect",
                    "GuardiansWrathStatusEffect",
                    "GunfighterStanceStatusEffect",
                    "PerceptiveStanceStatusEffect",
                    "SniperStanceStatusEffect",
                    "SoldiersPrecisionStatusEffect",
                    "SoldiersStrikeStatusEffect",
                    "SpotterStanceStatusEffect",
                    "TempestStanceStatusEffect"))
                return VisualEffect.Vfx_Imp_Holy_Aid;

            if (NameIs(name,
                    "BerserkerStanceStatusEffect",
                    "BombardierStanceStatusEffect",
                    "CobraStanceStatusEffect",
                    "ConduitStanceStatusEffect",
                    "CrusherStanceStatusEffect",
                    "CycloneStanceStatusEffect",
                    "DeadeyeStanceStatusEffect",
                    "DebilitatingStanceStatusEffect",
                    "SkirmisherStanceStatusEffect",
                    "SoulAscensionStatusEffect",
                    "SoulDevourerStatusEffect",
                    "SoulSacrificeStatusEffect",
                    "SoulStormStatusEffect"))
                return VisualEffect.Vfx_Dur_Aura_Pulse_Red_White;

            if (NameIs(name,
                    "AssaultStatusEffect",
                    "CenteringStatusEffect",
                    "DedicationStatusEffect",
                    "EvasiveCombatStatusEffect",
                    "EvasiveManeuver1StatusEffect",
                    "EvasiveManeuver2StatusEffect",
                    "EvasiveManeuver3StatusEffect",
                    "EvasiveManeuver4StatusEffect",
                    "EvasiveManeuver5StatusEffect",
                    "ImprovedAttentivenessStatusEffect"))
                return VisualEffect.Vfx_Imp_Super_Heroism;

            return VisualEffect.Vfx_Imp_Improve_Ability_Score;
        }

        private static Type GetPrimaryStatusEffect(Type statusEffect, IEnumerable<Type> additionalStatusEffects)
        {
            return statusEffect ?? additionalStatusEffects?.FirstOrDefault(type => type != null);
        }

        private static string GetStatusEffectName(Type statusEffect)
        {
            return statusEffect?.Name ?? string.Empty;
        }

        private static bool NameIs(string name, params string[] names)
        {
            return names.Any(candidate => string.Equals(name, candidate, StringComparison.Ordinal));
        }

        private static bool NameStartsWith(string name, params string[] prefixes)
        {
            return prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal));
        }

        private static bool IsDurationVisualEffect(VisualEffect visualEffect)
        {
            var name = visualEffect.ToString();
            return name.StartsWith("Vfx_Dur_", StringComparison.Ordinal) ||
                   name.StartsWith("Dur_", StringComparison.Ordinal);
        }
    }
}
