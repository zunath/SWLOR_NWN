using SWLOR.Game.Server.Service.AIService;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using System;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AIDefinition
{
    public abstract class AIBase : IAIDefinition
    {
        protected uint Self { get; private set; }
        protected uint Target { get; private set; }
        protected RacialType SelfRace { get; private set; }
        protected float SelfHPPercentage { get; private set; }
        protected uint LowestHPAlly { get; private set; }
        protected float LowestHPAllyPercentage { get; private set; }
        protected RacialType LowestHPAllyRace { get; private set; }
        protected int AllyCount { get; private set; }
        protected FeatType SelfActiveConcentration { get; private set; }
        protected uint AllyWithTreatmentKit1StatusEffect { get; private set; }
        protected uint AllyWithTreatmentKit2StatusEffect { get; private set; }

        private void ResetCachedData()
        {
            Self = OBJECT_INVALID;
            Target = OBJECT_INVALID;
            SelfRace = RacialType.Invalid;
            SelfHPPercentage = 100f;
            LowestHPAlly = OBJECT_INVALID;
            LowestHPAllyPercentage = 100f;
            LowestHPAllyRace = RacialType.Invalid;
            AllyCount = 0;
            SelfActiveConcentration = FeatType.Invalid;
            AllyWithTreatmentKit1StatusEffect = OBJECT_INVALID;
            AllyWithTreatmentKit2StatusEffect = OBJECT_INVALID;
        }

        /// <inheritdoc />
        public virtual void PreProcessAI(uint self, uint target, HashSet<uint> allies)
        {
            static float CalculateAverageHP(uint creature)
            {
                var currentHP = GetCurrentHitPoints(creature);
                var maxHP = GetMaxHitPoints(creature);
                return ((float)currentHP / (float)maxHP) * 100;
            }

            ResetCachedData();

            Self = self;
            Target = target;

            SelfHPPercentage = CalculateAverageHP(self);

            foreach (var ally in allies)
            {
                if (!GetIsObjectValid(AllyWithTreatmentKit1StatusEffect) &&  
                    StatusEffect.HasStatusEffect(ally, StatusEffectType.Bleed, StatusEffectType.Poison))
                    AllyWithTreatmentKit1StatusEffect = ally;
                if (!GetIsObjectValid(AllyWithTreatmentKit2StatusEffect) && 
                    StatusEffect.HasStatusEffect(ally, StatusEffectType.Shock, StatusEffectType.Burn))
                    AllyWithTreatmentKit2StatusEffect = ally;

                // Exit if we've found a target for both abilities.
                if (GetIsObjectValid(AllyWithTreatmentKit1StatusEffect) &&
                    GetIsObjectValid(AllyWithTreatmentKit2StatusEffect))
                    break;
            }

            LowestHPAlly = allies.MinBy(CalculateAverageHP);
            LowestHPAllyPercentage = CalculateAverageHP(LowestHPAlly);
            SelfRace = GetRacialType(self);
            LowestHPAllyRace = GetRacialType(LowestHPAlly);
            AllyCount = allies.Count;
            SelfActiveConcentration = Ability.GetActiveConcentration(self).Feat;
        }

        /// <summary>
        /// Checks whether a creature can use a specific feat.
        /// Verifies whether a creature has the feat, meets the condition, and can use the ability.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <param name="target">The target of the feat</param>
        /// <param name="feat">The feat to check</param>
        /// <param name="condition">The custom condition to check</param>
        /// <returns>true if feat can be used, false otherwise</returns>
        protected static bool CheckIfCanUseFeat(uint creature, uint target, FeatType feat, Func<bool> condition = null)
        {
            if (!GetHasFeat(feat, creature)) return false;
            if (condition != null && !condition()) return false;
            if (!GetIsObjectValid(target)) return false;

            var targetLocation = GetLocation(target);
            var abilityDetail = Ability.GetAbilityDetail(feat);
            var effectiveLevel = Perk.GetEffectivePerkLevel(creature, abilityDetail.EffectiveLevelPerkType);
            return Ability.CanUseAbility(creature, target, feat, effectiveLevel, targetLocation);
        }

        /// <inheritdoc />
        public virtual (FeatType, uint) DeterminePerkAbility()
        {
            // Note: The order is important here. The top-most abilities take precedence over lower ones.
            
            Benevolence();
            ForceHeal();
            MedKit();
            KoltoBomb();
            KoltoGrenade();
            KoltoRecovery();
            Infusion();
            Chi();
            Provoke();
            Resuscitation();
            TreatmentKit();
            BattleInsight();
            ThrowSaber();
            ForceStun();
            AdhesiveGrenade();
            ConcussionGrenade();
            Flamethrower();
            FlashbangGrenade();
            FragGrenade();
            GasBomb();
            IncendiaryBomb();
            IonGrenade();
            SmokeBomb();
            WristRocket();
            StealthGenerator();
            DeflectorShield();
            CombatEnhancement();
            Shielding();
            StasisField();
            CreepingTerror();
            Disturbance();
            ForceSpark();
            ForceLightning();
            ForceBurst();
            ThrowRock();
            ForceDrain();
            ForcePush();
            ForceInspiration();
            MindTrick();
            BurstOfSpeed();
            Knockdown();
            HackingBlade();
            RiotBlade();
            PoisonStab();
            Backstab();
            ForceLeap();
            SaberStrike();
            CrescentMoon();
            HardSlash();
            Skewer();
            DoubleThrust();
            LegSweep();
            CrossCut();
            CircleSlash();
            DoubleStrike();
            ElectricFist();
            StrikingCobra();
            Slam();
            SpinningWhirl();
            QuickDraw();
            DoubleShot();
            ExplosiveToss();
            PiercingToss();
            TranquilizerShot();
            CripplingShot();
            NPCAbilities();

            return NoAction;
        }

        protected static (FeatType, uint) NoAction => (FeatType.Invalid, OBJECT_INVALID);

        protected (FeatType, uint) Benevolence()
        {
            // Benevolence
            if (CheckIfCanUseFeat(Self, Target, FeatType.Benevolence3, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.Benevolence3, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Benevolence2, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.Benevolence2, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Benevolence1, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.Benevolence1, LowestHPAlly);
            }

            return NoAction;
        }

        protected (FeatType, uint) ForceHeal()
        {
            // Force Heal
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceHeal5, () => LowestHPAllyPercentage <= 100 && SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceHeal5, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceHeal4, () => LowestHPAllyPercentage <= 100 && SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceHeal4, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceHeal3, () => LowestHPAllyPercentage <= 100 && SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceHeal3, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceHeal2, () => LowestHPAllyPercentage <= 100 && SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceHeal2, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceHeal1, () => LowestHPAllyPercentage <= 100 && SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceHeal1, LowestHPAlly);
            }

            return NoAction;
        }

        protected (FeatType, uint) MedKit()
        {
            // Medkit
            if (CheckIfCanUseFeat(Self, Target, FeatType.MedKit5, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.MedKit5, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.MedKit4, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.MedKit4, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.MedKit3, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.MedKit3, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.MedKit2, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.MedKit2, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.MedKit1, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.MedKit1, LowestHPAlly);
            }

            return NoAction;
        }

        protected (FeatType, uint) KoltoBomb()
        {
            // Kolto Bomb
            if (CheckIfCanUseFeat(Self, Target, FeatType.KoltoBomb3, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.KoltoBomb3, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.KoltoBomb2, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.KoltoBomb2, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.KoltoBomb1, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.KoltoBomb1, LowestHPAlly);
            }

            return NoAction;
        }

        protected (FeatType, uint) KoltoGrenade()
        {
            // Kolto Grenade
            if (CheckIfCanUseFeat(Self, Target, FeatType.KoltoGrenade3, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.KoltoGrenade3, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.KoltoGrenade2, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.KoltoGrenade2, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.KoltoGrenade1, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.KoltoGrenade1, LowestHPAlly);
            }

            return NoAction;
        }

        protected (FeatType, uint) KoltoRecovery()
        {
            // Kolto Recovery
            if (CheckIfCanUseFeat(Self, Target, FeatType.KoltoRecovery3, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.KoltoRecovery3, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.KoltoRecovery2, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.KoltoRecovery2, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.KoltoRecovery1, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.KoltoRecovery1, LowestHPAlly);
            }

            return NoAction;
        }

        protected (FeatType, uint) BattleInsight()
        {
            // Battle Insight
            if (CheckIfCanUseFeat(Self, Self, FeatType.BattleInsight2, () => AllyCount >= 1 && SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.BattleInsight2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BattleInsight1, () => AllyCount >= 1 && SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.BattleInsight1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) ThrowSaber()
        {
            // Throw Saber
            if (CheckIfCanUseFeat(Self, Self, FeatType.ThrowLightsaber3))
            {
                return (FeatType.ThrowLightsaber3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ThrowLightsaber2))
            {
                return (FeatType.ThrowLightsaber2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ThrowLightsaber1))
            {
                return (FeatType.ThrowLightsaber1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) ForceStun()
        {
            // Force Stun
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceStun3, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceStun3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceStun2, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceStun2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceStun1, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceStun1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) AdhesiveGrenade()
        {
            // Adhesive Grenade
            if (CheckIfCanUseFeat(Self, Target, FeatType.AdhesiveGrenade3))
            {
                return (FeatType.AdhesiveGrenade3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.AdhesiveGrenade2))
            {
                return (FeatType.AdhesiveGrenade2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.AdhesiveGrenade1))
            {
                return (FeatType.AdhesiveGrenade1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) ConcussionGrenade()
        {
            // Concussion Grenade
            if (CheckIfCanUseFeat(Self, Target, FeatType.ConcussionGrenade3))
            {
                return (FeatType.ConcussionGrenade3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ConcussionGrenade2))
            {
                return (FeatType.ConcussionGrenade2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ConcussionGrenade1))
            {
                return (FeatType.ConcussionGrenade1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) Flamethrower()
        {
            // Flamethrower
            if (CheckIfCanUseFeat(Self, Target, FeatType.Flamethrower3))
            {
                return (FeatType.Flamethrower3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Flamethrower2))
            {
                return (FeatType.Flamethrower2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Flamethrower1))
            {
                return (FeatType.Flamethrower1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) FlashbangGrenade()
        {
            // Flashbang Grenade
            if (CheckIfCanUseFeat(Self, Target, FeatType.FlashbangGrenade3))
            {
                return (FeatType.FlashbangGrenade3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.FlashbangGrenade2))
            {
                return (FeatType.FlashbangGrenade2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.FlashbangGrenade1))
            {
                return (FeatType.FlashbangGrenade1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) FragGrenade()
        {
            // Frag Grenade
            if (CheckIfCanUseFeat(Self, Target, FeatType.FragGrenade3))
            {
                return (FeatType.FragGrenade3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.FragGrenade2))
            {
                return (FeatType.FragGrenade2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.FragGrenade1))
            {
                return (FeatType.FragGrenade1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) GasBomb()
        {
            // Gas Bomb
            if (CheckIfCanUseFeat(Self, Target, FeatType.GasBomb3))
            {
                return (FeatType.GasBomb3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.GasBomb2))
            {
                return (FeatType.GasBomb2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.GasBomb1))
            {
                return (FeatType.GasBomb1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) IncendiaryBomb()
        {
            // Incendiary Bomb
            if (CheckIfCanUseFeat(Self, Target, FeatType.IncendiaryBomb3))
            {
                return (FeatType.IncendiaryBomb3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.IncendiaryBomb2))
            {
                return (FeatType.IncendiaryBomb2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.IncendiaryBomb1))
            {
                return (FeatType.IncendiaryBomb1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) IonGrenade()
        {
            // Ion Grenade
            if (CheckIfCanUseFeat(Self, Target, FeatType.IonGrenade3))
            {
                return (FeatType.IonGrenade3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.IonGrenade2))
            {
                return (FeatType.IonGrenade2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.IonGrenade1))
            {
                return (FeatType.IonGrenade1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) SmokeBomb()
        {
            // Smoke Bomb
            if (CheckIfCanUseFeat(Self, Target, FeatType.SmokeBomb3, () => LowestHPAllyPercentage < 50))
            {
                return (FeatType.SmokeBomb3, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.SmokeBomb2, () => LowestHPAllyPercentage < 50))
            {
                return (FeatType.SmokeBomb2, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.SmokeBomb1, () => LowestHPAllyPercentage < 50))
            {
                return (FeatType.SmokeBomb1, LowestHPAlly);
            }

            return NoAction;
        }

        protected (FeatType, uint) StealthGenerator()
        {
            // Stealth Generator
            if (CheckIfCanUseFeat(Self, Self, FeatType.StealthGenerator3, () => SelfHPPercentage < 100))
            {
                return (FeatType.StealthGenerator3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.StealthGenerator2, () => SelfHPPercentage < 100))
            {
                return (FeatType.StealthGenerator2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.StealthGenerator1, () => SelfHPPercentage < 100))
            {
                return (FeatType.StealthGenerator1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) DeflectorShield()
        {
            // Deflector Shield
            if (CheckIfCanUseFeat(Self, Self, FeatType.DeflectorShield3, () => SelfHPPercentage < 100))
            {
                return (FeatType.DeflectorShield3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.DeflectorShield2, () => SelfHPPercentage < 100))
            {
                return (FeatType.DeflectorShield2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.DeflectorShield1, () => SelfHPPercentage < 100))
            {
                return (FeatType.DeflectorShield1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) CombatEnhancement()
        {
            // Combat Enhancement
            if (CheckIfCanUseFeat(Self, Self, FeatType.CombatEnhancement3, () => SelfHPPercentage >= 95))
            {
                return (FeatType.CombatEnhancement3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.CombatEnhancement2, () => SelfHPPercentage >= 95))
            {
                return (FeatType.CombatEnhancement2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.CombatEnhancement1, () => SelfHPPercentage >= 95))
            {
                return (FeatType.CombatEnhancement1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) Shielding()
        {
            // Shielding
            if (CheckIfCanUseFeat(Self, Self, FeatType.Shielding4))
            {
                return (FeatType.Shielding4, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Shielding3))
            {
                return (FeatType.Shielding3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Shielding2))
            {
                return (FeatType.Shielding2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Shielding1))
            {
                return (FeatType.Shielding1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) StasisField()
        {
            // Stasis Field
            if (CheckIfCanUseFeat(Self, Self, FeatType.StasisField3))
            {
                return (FeatType.StasisField3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.StasisField2))
            {
                return (FeatType.StasisField2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.StasisField1))
            {
                return (FeatType.StasisField1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) CreepingTerror()
        {
            // Creeping Terror
            if (CheckIfCanUseFeat(Self, Target, FeatType.CreepingTerror3))
            {
                return (FeatType.CreepingTerror3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.CreepingTerror2))
            {
                return (FeatType.CreepingTerror2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.CreepingTerror1))
            {
                return (FeatType.CreepingTerror1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) Disturbance()
        {
            // Disturbance
            if (CheckIfCanUseFeat(Self, Target, FeatType.Disturbance3))
            {
                return (FeatType.Disturbance3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Disturbance2))
            {
                return (FeatType.Disturbance2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Disturbance1))
            {
                return (FeatType.Disturbance1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) ForceSpark()
        {
            // Force Spark
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceSpark3))
            {
                return (FeatType.ForceSpark3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceSpark2))
            {
                return (FeatType.ForceSpark2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceSpark1))
            {
                return (FeatType.ForceSpark1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) ForceLightning()
        {
            // Force Lightning
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceLightning4))
            {
                return (FeatType.ForceLightning4, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceLightning3))
            {
                return (FeatType.ForceLightning3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceLightning2))
            {
                return (FeatType.ForceLightning2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceLightning1))
            {
                return (FeatType.ForceLightning1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) ForceBurst()
        {
            // Force Burst
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceBurst4))
            {
                return (FeatType.ForceBurst4, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceBurst3))
            {
                return (FeatType.ForceBurst3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceBurst2))
            {
                return (FeatType.ForceBurst2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceBurst1))
            {
                return (FeatType.ForceBurst1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) ThrowRock()
        {
            // Throw Rock
            if (CheckIfCanUseFeat(Self, Target, FeatType.ThrowRock5))
            {
                return (FeatType.ThrowRock5, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ThrowRock4))
            {
                return (FeatType.ThrowRock4, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ThrowRock3))
            {
                return (FeatType.ThrowRock3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ThrowRock2))
            {
                return (FeatType.ThrowRock2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ThrowRock1))
            {
                return (FeatType.ThrowRock1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) ForceDrain()
        {
            // Force Drain
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceDrain5, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceDrain5, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceDrain4, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceDrain4, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceDrain3, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceDrain3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceDrain2, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceDrain2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceDrain1, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceDrain1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) ForcePush()
        {
            // Force Push
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForcePush4))
            {
                return (FeatType.ForcePush4, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForcePush3))
            {
                return (FeatType.ForcePush3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForcePush2))
            {
                return (FeatType.ForcePush2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForcePush1))
            {
                return (FeatType.ForcePush1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) ForceInspiration()
        {
            // Force Inspiration
            if (CheckIfCanUseFeat(Self, Self, FeatType.ForceInspiration3))
            {
                return (FeatType.ForceInspiration3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ForceInspiration2))
            {
                return (FeatType.ForceInspiration2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ForceInspiration1))
            {
                return (FeatType.ForceInspiration1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) MindTrick()
        {
            // Mind Trick
            if (CheckIfCanUseFeat(Self, Target, FeatType.MindTrick2, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.MindTrick2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.MindTrick1, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.MindTrick1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) BurstOfSpeed()
        {
            // Burst of Speed
            if (CheckIfCanUseFeat(Self, Self, FeatType.BurstOfSpeed5, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.BurstOfSpeed5, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BurstOfSpeed4, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.BurstOfSpeed4, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BurstOfSpeed3, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.BurstOfSpeed3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BurstOfSpeed2, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.BurstOfSpeed2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BurstOfSpeed1, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (FeatType.BurstOfSpeed1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) HackingBlade()
        {
            // Hacking Blade
            if (CheckIfCanUseFeat(Self, Self, FeatType.HackingBlade3))
            {
                return (FeatType.HackingBlade3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.HackingBlade2))
            {
                return (FeatType.HackingBlade2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.HackingBlade1))
            {
                return (FeatType.HackingBlade1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) RiotBlade()
        {
            // Riot Blade
            if (CheckIfCanUseFeat(Self, Target, FeatType.RiotBlade3))
            {
                return (FeatType.RiotBlade3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.RiotBlade2))
            {
                return (FeatType.RiotBlade2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.RiotBlade1))
            {
                return (FeatType.RiotBlade1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) PoisonStab()
        {
            // Poison Stab
            if (CheckIfCanUseFeat(Self, Self, FeatType.PoisonStab3))
            {
                return (FeatType.PoisonStab3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.PoisonStab2))
            {
                return (FeatType.PoisonStab2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.PoisonStab1))
            {
                return (FeatType.PoisonStab1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) Backstab()
        {
            // Backstab
            if (CheckIfCanUseFeat(Self, Target, FeatType.Backstab3))
            {
                return (FeatType.Backstab3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Backstab2))
            {
                return (FeatType.Backstab2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Backstab1))
            {
                return (FeatType.Backstab1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) ForceLeap()
        {
            // Force Leap
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceLeap3))
            {
                return (FeatType.ForceLeap3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceLeap2))
            {
                return (FeatType.ForceLeap2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceLeap1))
            {
                return (FeatType.ForceLeap1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) SaberStrike()
        {
            // Saber Strike
            if (CheckIfCanUseFeat(Self, Self, FeatType.SaberStrike3))
            {
                return (FeatType.SaberStrike3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.SaberStrike2))
            {
                return (FeatType.SaberStrike2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.SaberStrike1))
            {
                return (FeatType.SaberStrike1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) CrescentMoon()
        {
            // Crescent Moon
            if (CheckIfCanUseFeat(Self, Self, FeatType.CrescentMoon3))
            {
                return (FeatType.CrescentMoon3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.CrescentMoon2))
            {
                return (FeatType.CrescentMoon2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.CrescentMoon1))
            {
                return (FeatType.CrescentMoon1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) HardSlash()
        {
            // Hard Slash
            if (CheckIfCanUseFeat(Self, Target, FeatType.HardSlash3))
            {
                return (FeatType.HardSlash3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.HardSlash2))
            {
                return (FeatType.HardSlash2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.HardSlash1))
            {
                return (FeatType.HardSlash1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) Skewer()
        {
            // Skewer
            if (CheckIfCanUseFeat(Self, Self, FeatType.Skewer3))
            {
                return (FeatType.Skewer3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Skewer2))
            {
                return (FeatType.Skewer2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Skewer1))
            {
                return (FeatType.Skewer1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) DoubleThrust()
        {
            // Double Thrust
            if (CheckIfCanUseFeat(Self, Target, FeatType.DoubleThrust3))
            {
                return (FeatType.DoubleThrust3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.DoubleThrust2))
            {
                return (FeatType.DoubleThrust2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.DoubleThrust1))
            {
                return (FeatType.DoubleThrust1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) LegSweep()
        {
            // Leg Sweep
            if (CheckIfCanUseFeat(Self, Self, FeatType.LegSweep3))
            {
                return (FeatType.LegSweep3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.LegSweep2))
            {
                return (FeatType.LegSweep2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.LegSweep1))
            {
                return (FeatType.LegSweep1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) CrossCut()
        {
            // Cross Cut
            if (CheckIfCanUseFeat(Self, Target, FeatType.CrossCut3))
            {
                return (FeatType.CrossCut3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.CrossCut2))
            {
                return (FeatType.CrossCut2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.CrossCut3))
            {
                return (FeatType.CrossCut1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) CircleSlash()
        {
            // Circle Slash
            if (CheckIfCanUseFeat(Self, Self, FeatType.CircleSlash3))
            {
                return (FeatType.CircleSlash3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.CircleSlash2))
            {
                return (FeatType.CircleSlash2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.CircleSlash1))
            {
                return (FeatType.CircleSlash1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) DoubleStrike()
        {
            // Double Strike
            if (CheckIfCanUseFeat(Self, Target, FeatType.DoubleStrike3))
            {
                return (FeatType.DoubleStrike3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.DoubleStrike2))
            {
                return (FeatType.DoubleStrike2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.DoubleStrike1))
            {
                return (FeatType.DoubleStrike1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) ElectricFist()
        {
            // Electric Fist
            if (CheckIfCanUseFeat(Self, Self, FeatType.ElectricFist3))
            {
                return (FeatType.ElectricFist3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ElectricFist2))
            {
                return (FeatType.ElectricFist2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ElectricFist1))
            {
                return (FeatType.ElectricFist1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) StrikingCobra()
        {
            // Striking Cobra
            if (CheckIfCanUseFeat(Self, Self, FeatType.StrikingCobra3))
            {
                return (FeatType.StrikingCobra3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.StrikingCobra2))
            {
                return (FeatType.StrikingCobra2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.StrikingCobra1))
            {
                return (FeatType.StrikingCobra1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) Slam()
        {
            // Slam
            if (CheckIfCanUseFeat(Self, Target, FeatType.Slam3))
            {
                return (FeatType.Slam3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Slam2))
            {
                return (FeatType.Slam2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Slam1))
            {
                return (FeatType.Slam1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) SpinningWhirl()
        {
            // Spinning Whirl
            if (CheckIfCanUseFeat(Self, Target, FeatType.SpinningWhirl3))
            {
                return (FeatType.SpinningWhirl3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.SpinningWhirl2))
            {
                return (FeatType.SpinningWhirl2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.SpinningWhirl1))
            {
                return (FeatType.SpinningWhirl1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) QuickDraw()
        {
            // Quick Draw
            if (CheckIfCanUseFeat(Self, Target, FeatType.QuickDraw3))
            {
                return (FeatType.QuickDraw3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.QuickDraw2))
            {
                return (FeatType.QuickDraw2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.QuickDraw1))
            {
                return (FeatType.QuickDraw1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) DoubleShot()
        {
            // Double Shot
            if (CheckIfCanUseFeat(Self, Target, FeatType.DoubleShot3))
            {
                return (FeatType.DoubleShot3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.DoubleShot2))
            {
                return (FeatType.DoubleShot2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.DoubleShot1))
            {
                return (FeatType.DoubleShot1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) ExplosiveToss()
        {
            // Explosive Toss
            if (CheckIfCanUseFeat(Self, Self, FeatType.ExplosiveToss3))
            {
                return (FeatType.ExplosiveToss3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ExplosiveToss2))
            {
                return (FeatType.ExplosiveToss2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ExplosiveToss1))
            {
                return (FeatType.ExplosiveToss1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) PiercingToss()
        {
            // Piercing Toss
            if (CheckIfCanUseFeat(Self, Target, FeatType.PiercingToss3))
            {
                return (FeatType.PiercingToss3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.PiercingToss2))
            {
                return (FeatType.PiercingToss2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.PiercingToss1))
            {
                return (FeatType.PiercingToss1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) TranquilizerShot()
        {
            // Tranquilizer Shot
            if (CheckIfCanUseFeat(Self, Self, FeatType.TranquilizerShot3))
            {
                return (FeatType.TranquilizerShot3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.TranquilizerShot2))
            {
                return (FeatType.TranquilizerShot2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.TranquilizerShot1))
            {
                return (FeatType.TranquilizerShot1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) CripplingShot()
        {

            // Crippling Shot
            if (CheckIfCanUseFeat(Self, Self, FeatType.CripplingShot3))
            {
                return (FeatType.CripplingShot3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.CripplingShot2))
            {
                return (FeatType.CripplingShot2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.CripplingShot1))
            {
                return (FeatType.CripplingShot1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) NPCAbilities()
        {

            // Roar
            if (CheckIfCanUseFeat(Self, Self, FeatType.Roar))
            {
                return (FeatType.Roar, Self);
            }

            // Bite
            if (CheckIfCanUseFeat(Self, Target, FeatType.Bite))
            {
                return (FeatType.Bite, Target);
            }

            // Iron Shell
            if (CheckIfCanUseFeat(Self, Target, FeatType.IronShell))
            {
                return (FeatType.IronShell, Self);
            }

            // Screech
            if (CheckIfCanUseFeat(Self, Target, FeatType.Screech))
            {
                return (FeatType.Screech, Self);
            }

            // Greater Earthquake
            if (CheckIfCanUseFeat(Self, Self, FeatType.GreaterEarthquake))
            {
                return (FeatType.GreaterEarthquake, Target);
            }

            // Earthquake
            if (CheckIfCanUseFeat(Self, Self, FeatType.Earthquake))
            {
                return (FeatType.Earthquake, Target);
            }

            // Flame Blast
            if (CheckIfCanUseFeat(Self, Target, FeatType.FlameBlast))
            {
                return (FeatType.FlameBlast, Target);
            }

            // Fire Breath
            if (CheckIfCanUseFeat(Self, Target, FeatType.FireBreath))
            {
                return (FeatType.FireBreath, Target);
            }

            // Spikes
            if (CheckIfCanUseFeat(Self, Target, FeatType.Spikes))
            {
                return (FeatType.Spikes, Target);
            }

            // Venom
            if (CheckIfCanUseFeat(Self, Target, FeatType.Venom))
            {
                return (FeatType.Venom, Target);
            }

            // Talon
            if (CheckIfCanUseFeat(Self, Target, FeatType.Talon))
            {
                return (FeatType.Talon, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) Knockdown()
        {
            // Knockdown
            if (CheckIfCanUseFeat(Self, Target, FeatType.Knockdown))
            {
                return (FeatType.Knockdown, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) Chi()
        {
            // Chi
            if (CheckIfCanUseFeat(Self, Self, FeatType.Chi3, () => SelfHPPercentage < 100f))
            {
                return (FeatType.Chi3, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Benevolence2, () => SelfHPPercentage < 100f))
            {
                return (FeatType.Chi2, Self);
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Benevolence1, () => SelfHPPercentage < 100f))
            {
                return (FeatType.Chi1, Self);
            }

            return NoAction;
        }

        protected (FeatType, uint) WristRocket()
        {
            // Wrist Rocket
            if (CheckIfCanUseFeat(Self, Target, FeatType.WristRocket3))
            {
                return (FeatType.WristRocket3, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.WristRocket2))
            {
                return (FeatType.WristRocket2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.WristRocket1))
            {
                return (FeatType.WristRocket1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) Provoke()
        {
            // Provoke
            if (CheckIfCanUseFeat(Self, Target, FeatType.Provoke2))
            {
                return (FeatType.Provoke2, Target);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Provoke1))
            {
                return (FeatType.Provoke1, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) Resuscitation()
        {
            if (CheckIfCanUseFeat(Self, Target, FeatType.Provoke2))
            {
                return (FeatType.Provoke2, Target);
            }

            return NoAction;
        }

        protected (FeatType, uint) TreatmentKit()
        {
            if (CheckIfCanUseFeat(Self, AllyWithTreatmentKit2StatusEffect, FeatType.TreatmentKit2))
            {
                return (FeatType.TreatmentKit2, AllyWithTreatmentKit2StatusEffect);
            }
            if (CheckIfCanUseFeat(Self, AllyWithTreatmentKit1StatusEffect, FeatType.TreatmentKit1))
            {
                return (FeatType.TreatmentKit1, AllyWithTreatmentKit1StatusEffect);
            }

            return NoAction;
        }

        protected (FeatType, uint) Infusion()
        {
            // Infusion
            if (CheckIfCanUseFeat(Self, Target, FeatType.Infusion2, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.Infusion2, LowestHPAlly);
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Infusion1, () => LowestHPAllyPercentage < 100))
            {
                return (FeatType.Infusion1, LowestHPAlly);
            }

            return NoAction;
        }
    }
}
