using SWLOR.Game.Server.Service.AIService;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

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
        public virtual void PreProcessAI(uint self, uint target, List<uint> allies)
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


            SelfHPPercentage = CalculateAverageHP(Self);

            foreach (var ally in allies)
            {
                if (!GetIsObjectValid(AllyWithTreatmentKit1StatusEffect) &&
                    StatusEffect.HasCleanseableStatusEffect(ally, StatusEffectCleanseType.TreatmentKit1))
                    AllyWithTreatmentKit1StatusEffect = ally;
                if (!GetIsObjectValid(AllyWithTreatmentKit2StatusEffect) &&
                    StatusEffect.HasCleanseableStatusEffect(ally, StatusEffectCleanseType.TreatmentKit2))
                    AllyWithTreatmentKit2StatusEffect = ally;

                // Exit if we've found a target for both abilities.
                if (GetIsObjectValid(AllyWithTreatmentKit1StatusEffect) &&
                    GetIsObjectValid(AllyWithTreatmentKit2StatusEffect))
                    break;
            }

            LowestHPAlly = allies.MinBy(CalculateAverageHP);
            LowestHPAllyPercentage = CalculateAverageHP(LowestHPAlly);
            SelfRace = GetRacialType(Self);
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
            var effectiveLevel = Perk.GetPerkLevel(creature, abilityDetail.EffectiveLevelPerkType);
            return Ability.CanUseAbility(creature, target, feat, effectiveLevel, targetLocation);
        }

        /// <inheritdoc />
        public virtual (FeatType, uint) DeterminePerkAbility()
        {
            // Note: The order is important here. The top-most abilities take precedence over lower ones.

            var (success, result) = Benevolence();
            if (success) return result;

            (success, result) = ForceHeal();
            if (success) return result;

            (success, result) = MedKit();
            if (success) return result;

            (success, result) = KoltoBomb();
            if (success) return result;

            (success, result) = KoltoGrenade();
            if (success) return result;

            (success, result) = KoltoRecovery();
            if (success) return result;

            (success, result) = Infusion();
            if (success) return result;

            (success, result) = Provoke();
            if (success) return result;

            (success, result) = Resuscitation();
            if (success) return result;

            (success, result) = TreatmentKit();
            if (success) return result;

            (success, result) = BattleInsight();
            if (success) return result;

            (success, result) = ThrowSaber();
            if (success) return result;

            (success, result) = ForceStun();
            if (success) return result;

            (success, result) = AdhesiveGrenade();
            if (success) return result;

            (success, result) = ConcussionGrenade();
            if (success) return result;

            (success, result) = Flamethrower();
            if (success) return result;

            (success, result) = FlashbangGrenade();
            if (success) return result;

            (success, result) = FragGrenade();
            if (success) return result;

            (success, result) = GasBomb();
            if (success) return result;

            (success, result) = IncendiaryBomb();
            if (success) return result;

            (success, result) = IonGrenade();
            if (success) return result;

            (success, result) = WristRocket();
            if (success) return result;

            (success, result) = StealthGenerator();
            if (success) return result;

            (success, result) = DeflectorShield();
            if (success) return result;

            (success, result) = CombatEnhancement();
            if (success) return result;

            (success, result) = Shielding();
            if (success) return result;

            (success, result) = StasisField();
            if (success) return result;

            (success, result) = CreepingTerror();
            if (success) return result;

            (success, result) = Disturbance();
            if (success) return result;

            (success, result) = ForceSpark();
            if (success) return result;

            (success, result) = ForceLightning();
            if (success) return result;

            (success, result) = ForceBurst();
            if (success) return result;

            (success, result) = ThrowRock();
            if (success) return result;

            (success, result) = ForceDrain();
            if (success) return result;

            (success, result) = ForcePush();
            if (success) return result;

            (success, result) = ForceInspiration();
            if (success) return result;

            (success, result) = MindTrick();
            if (success) return result;

            (success, result) = BurstOfSpeed();
            if (success) return result;

            (success, result) = ForceLeap();
            if (success) return result;

            (success, result) = NPCAbilities();
            if (success) return result;


            return NoAction.Item2;
        }

        protected static (bool, (FeatType, uint)) NoAction => (false, (FeatType.Invalid, OBJECT_INVALID));

        protected (bool, (FeatType, uint)) Benevolence()
        {
            // Benevolence
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.Benevolence3, () => LowestHPAllyPercentage <= 50))
            {
                return (true, (FeatType.Benevolence3, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.Benevolence2, () => LowestHPAllyPercentage <= 75))
            {
                return (true, (FeatType.Benevolence2, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.Benevolence1, () => LowestHPAllyPercentage <= 90))
            {
                return (true, (FeatType.Benevolence1, LowestHPAlly));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) ForceHeal()
        {
            // Force Heal
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.ForceHeal5, () => LowestHPAllyPercentage <= 40 && SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.ForceHeal5, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.ForceHeal4, () => LowestHPAllyPercentage <= 60 && SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.ForceHeal4, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.ForceHeal3, () => LowestHPAllyPercentage <= 75 && SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.ForceHeal3, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.ForceHeal2, () => LowestHPAllyPercentage <= 85 && SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.ForceHeal2, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.ForceHeal1, () => LowestHPAllyPercentage <= 95 && SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.ForceHeal1, LowestHPAlly));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) MedKit()
        {
            // Medkit
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.MedKit5, () => LowestHPAllyPercentage <= 50))
            {
                return (true, (FeatType.MedKit5, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.MedKit4, () => LowestHPAllyPercentage <= 65))
            {
                return (true, (FeatType.MedKit4, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.MedKit3, () => LowestHPAllyPercentage <= 75))
            {
                return (true, (FeatType.MedKit3, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.MedKit2, () => LowestHPAllyPercentage <= 85))
            {
                return (true, (FeatType.MedKit2, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.MedKit1, () => LowestHPAllyPercentage <= 95))
            {
                return (true, (FeatType.MedKit1, LowestHPAlly));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) KoltoBomb()
        {
            // Kolto Bomb
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.KoltoBomb3, () => LowestHPAllyPercentage <= 65))
            {
                return (true, (FeatType.KoltoBomb3, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.KoltoBomb2, () => LowestHPAllyPercentage <= 85))
            {
                return (true, (FeatType.KoltoBomb2, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.KoltoBomb1, () => LowestHPAllyPercentage <= 95))
            {
                return (true, (FeatType.KoltoBomb1, LowestHPAlly));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) KoltoGrenade()
        {
            // Kolto Grenade
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.KoltoGrenade3, () => LowestHPAllyPercentage <= 60))
            {
                return (true, (FeatType.KoltoGrenade3, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.KoltoGrenade2, () => LowestHPAllyPercentage <= 75))
            {
                return (true, (FeatType.KoltoGrenade2, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.KoltoGrenade1, () => LowestHPAllyPercentage <= 90))
            {
                return (true, (FeatType.KoltoGrenade1, LowestHPAlly));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) KoltoRecovery()
        {
            // Kolto Recovery
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.KoltoRecovery3, () => LowestHPAllyPercentage <= 65))
            {
                return (true, (FeatType.KoltoRecovery3, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.KoltoRecovery2, () => LowestHPAllyPercentage <= 75))
            {
                return (true, (FeatType.KoltoRecovery2, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.KoltoRecovery1, () => LowestHPAllyPercentage <= 90))
            {
                return (true, (FeatType.KoltoRecovery1, LowestHPAlly));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) BattleInsight()
        {
            // Battle Insight
            if (CheckIfCanUseFeat(Self, Self, FeatType.BattleInsight2, () => AllyCount >= 1 && SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.BattleInsight2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BattleInsight1, () => AllyCount >= 1 && SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.BattleInsight1, Self));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) ThrowSaber()
        {
            // Throw Saber
            if (CheckIfCanUseFeat(Self, Self, FeatType.ThrowLightsaber3))
            {
                return (true, (FeatType.ThrowLightsaber3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ThrowLightsaber2))
            {
                return (true, (FeatType.ThrowLightsaber2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ThrowLightsaber1))
            {
                return (true, (FeatType.ThrowLightsaber1, Self));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) ForceStun()
        {
            // Force Stun
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceStun3, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.ForceStun3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceStun2, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.ForceStun2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceStun1, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.ForceStun1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) AdhesiveGrenade()
        {
            // Adhesive Grenade
            if (CheckIfCanUseFeat(Self, Target, FeatType.AdhesiveGrenade3))
            {
                return (true, (FeatType.AdhesiveGrenade3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.AdhesiveGrenade2))
            {
                return (true, (FeatType.AdhesiveGrenade2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.AdhesiveGrenade1))
            {
                return (true, (FeatType.AdhesiveGrenade1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) ConcussionGrenade()
        {
            // Concussion Grenade
            if (CheckIfCanUseFeat(Self, Target, FeatType.ConcussionGrenade3))
            {
                return (true, (FeatType.ConcussionGrenade3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ConcussionGrenade2))
            {
                return (true, (FeatType.ConcussionGrenade2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ConcussionGrenade1))
            {
                return (true, (FeatType.ConcussionGrenade1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) Flamethrower()
        {
            // Flamethrower
            if (CheckIfCanUseFeat(Self, Target, FeatType.Flamethrower3))
            {
                return (true, (FeatType.Flamethrower3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Flamethrower2))
            {
                return (true, (FeatType.Flamethrower2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Flamethrower1))
            {
                return (true, (FeatType.Flamethrower1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) FlashbangGrenade()
        {
            // Flashbang Grenade
            if (CheckIfCanUseFeat(Self, Target, FeatType.FlashbangGrenade3))
            {
                return (true, (FeatType.FlashbangGrenade3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.FlashbangGrenade2))
            {
                return (true, (FeatType.FlashbangGrenade2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.FlashbangGrenade1))
            {
                return (true, (FeatType.FlashbangGrenade1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) FragGrenade()
        {
            // Frag Grenade
            if (CheckIfCanUseFeat(Self, Target, FeatType.FragGrenade3))
            {
                return (true, (FeatType.FragGrenade3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.FragGrenade2))
            {
                return (true, (FeatType.FragGrenade2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.FragGrenade1))
            {
                return (true, (FeatType.FragGrenade1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) GasBomb()
        {
            // Gas Bomb
            if (CheckIfCanUseFeat(Self, Target, FeatType.GasBomb3))
            {
                return (true, (FeatType.GasBomb3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.GasBomb2))
            {
                return (true, (FeatType.GasBomb2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.GasBomb1))
            {
                return (true, (FeatType.GasBomb1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) IncendiaryBomb()
        {
            // Incendiary Bomb
            if (CheckIfCanUseFeat(Self, Target, FeatType.IncendiaryBomb3))
            {
                return (true, (FeatType.IncendiaryBomb3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.IncendiaryBomb2))
            {
                return (true, (FeatType.IncendiaryBomb2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.IncendiaryBomb1))
            {
                return (true, (FeatType.IncendiaryBomb1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) IonGrenade()
        {
            // Ion Grenade
            if (CheckIfCanUseFeat(Self, Target, FeatType.IonGrenade3))
            {
                return (true, (FeatType.IonGrenade3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.IonGrenade2))
            {
                return (true, (FeatType.IonGrenade2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.IonGrenade1))
            {
                return (true, (FeatType.IonGrenade1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) StealthGenerator()
        {
            // Stealth Generator
            if (CheckIfCanUseFeat(Self, Self, FeatType.StealthGenerator3, () => SelfHPPercentage < 100))
            {
                return (true, (FeatType.StealthGenerator3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.StealthGenerator2, () => SelfHPPercentage < 100))
            {
                return (true, (FeatType.StealthGenerator2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.StealthGenerator1, () => SelfHPPercentage < 100))
            {
                return (true, (FeatType.StealthGenerator1, Self));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) DeflectorShield()
        {
            // Deflector Shield
            if (CheckIfCanUseFeat(Self, Self, FeatType.DeflectorShield3, () => SelfHPPercentage < 100))
            {
                return (true, (FeatType.DeflectorShield3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.DeflectorShield2, () => SelfHPPercentage < 100))
            {
                return (true, (FeatType.DeflectorShield2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.DeflectorShield1, () => SelfHPPercentage < 100))
            {
                return (true, (FeatType.DeflectorShield1, Self));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) CombatEnhancement()
        {
            // Combat Enhancement
            if (CheckIfCanUseFeat(Self, Self, FeatType.CombatEnhancement3, () => !HasEffectByTag(Self, "COMBAT_ENHANCEMENT", "FORCE_INSPIRATION")))
            {
                return (true, (FeatType.CombatEnhancement3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.CombatEnhancement2, () => !HasEffectByTag(Self, "COMBAT_ENHANCEMENT", "FORCE_INSPIRATION")))
            {
                return (true, (FeatType.CombatEnhancement2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.CombatEnhancement1, () => !HasEffectByTag(Self, "COMBAT_ENHANCEMENT", "FORCE_INSPIRATION")))
            {
                return (true, (FeatType.CombatEnhancement1, Self));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) Shielding()
        {
            // Shielding
            if (CheckIfCanUseFeat(Self, Self, FeatType.Shielding4, () => !StatusEffect.HasStatusEffect(Self, typeof(Shielding1StatusEffect), typeof(Shielding2StatusEffect), typeof(Shielding3StatusEffect), typeof(Shielding4StatusEffect))))
            {
                return (true, (FeatType.Shielding4, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Shielding3, () => !StatusEffect.HasStatusEffect(Self, typeof(Shielding1StatusEffect), typeof(Shielding2StatusEffect), typeof(Shielding3StatusEffect), typeof(Shielding4StatusEffect))))
            {
                return (true, (FeatType.Shielding3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Shielding2, () => !StatusEffect.HasStatusEffect(Self, typeof(Shielding1StatusEffect), typeof(Shielding2StatusEffect), typeof(Shielding3StatusEffect), typeof(Shielding4StatusEffect))))
            {
                return (true, (FeatType.Shielding2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Shielding1, () => !StatusEffect.HasStatusEffect(Self, typeof(Shielding1StatusEffect), typeof(Shielding2StatusEffect), typeof(Shielding3StatusEffect), typeof(Shielding4StatusEffect))))
            {
                return (true, (FeatType.Shielding1, Self));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) StasisField()
        {
            // Stasis Field
            if (CheckIfCanUseFeat(Self, Self, FeatType.StasisField3, () => !HasEffectByTag(Self, "STASIS_FIELD")))
            {
                return (true, (FeatType.StasisField3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.StasisField2, () => !HasEffectByTag(Self, "STASIS_FIELD")))
            {
                return (true, (FeatType.StasisField2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.StasisField1, () => !HasEffectByTag(Self, "STASIS_FIELD")))
            {
                return (true, (FeatType.StasisField1, Self));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) CreepingTerror()
        {
            // Creeping Terror
            if (CheckIfCanUseFeat(Self, Target, FeatType.CreepingTerror3))
            {
                return (true, (FeatType.CreepingTerror3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.CreepingTerror2))
            {
                return (true, (FeatType.CreepingTerror2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.CreepingTerror1))
            {
                return (true, (FeatType.CreepingTerror1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) Disturbance()
        {
            // Disturbance
            if (CheckIfCanUseFeat(Self, Target, FeatType.Disturbance3))
            {
                return (true, (FeatType.Disturbance3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Disturbance2))
            {
                return (true, (FeatType.Disturbance2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Disturbance1))
            {
                return (true, (FeatType.Disturbance1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) ForceSpark()
        {
            // Force Spark
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceSpark3))
            {
                return (true, (FeatType.ForceSpark3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceSpark2))
            {
                return (true, (FeatType.ForceSpark2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceSpark1))
            {
                return (true, (FeatType.ForceSpark1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) ForceLightning()
        {
            // Force Lightning
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceLightning4))
            {
                return (true, (FeatType.ForceLightning4, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceLightning3))
            {
                return (true, (FeatType.ForceLightning3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceLightning2))
            {
                return (true, (FeatType.ForceLightning2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceLightning1))
            {
                return (true, (FeatType.ForceLightning1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) ForceBurst()
        {
            // Force Burst
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceBurst4))
            {
                return (true, (FeatType.ForceBurst4, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceBurst3))
            {
                return (true, (FeatType.ForceBurst3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceBurst2))
            {
                return (true, (FeatType.ForceBurst2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceBurst1))
            {
                return (true, (FeatType.ForceBurst1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) ThrowRock()
        {
            // Throw Rock
            if (CheckIfCanUseFeat(Self, Target, FeatType.ThrowRock5))
            {
                return (true, (FeatType.ThrowRock5, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ThrowRock4))
            {
                return (true, (FeatType.ThrowRock4, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ThrowRock3))
            {
                return (true, (FeatType.ThrowRock3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ThrowRock2))
            {
                return (true, (FeatType.ThrowRock2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ThrowRock1))
            {
                return (true, (FeatType.ThrowRock1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) ForceDrain()
        {
            // Force Drain
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceDrain5, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.ForceDrain5, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceDrain4, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.ForceDrain4, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceDrain3, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.ForceDrain3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceDrain2, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.ForceDrain2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceDrain1, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.ForceDrain1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) ForcePush()
        {
            // Force Push
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForcePush4))
            {
                return (true, (FeatType.ForcePush4, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForcePush3))
            {
                return (true, (FeatType.ForcePush3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForcePush2))
            {
                return (true, (FeatType.ForcePush2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForcePush1))
            {
                return (true, (FeatType.ForcePush1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) ForceInspiration()
        {
            // Force Inspiration
            if (CheckIfCanUseFeat(Self, Self, FeatType.ForceInspiration3, () => !HasEffectByTag(Self, "COMBAT_ENHANCEMENT", "FORCE_INSPIRATION")))
            {
                return (true, (FeatType.ForceInspiration3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ForceInspiration2, () => !HasEffectByTag(Self, "COMBAT_ENHANCEMENT", "FORCE_INSPIRATION")))
            {
                return (true, (FeatType.ForceInspiration2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ForceInspiration1, () => !HasEffectByTag(Self, "COMBAT_ENHANCEMENT", "FORCE_INSPIRATION")))
            {
                return (true, (FeatType.ForceInspiration1, Self));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) MindTrick()
        {
            // Mind Trick
            if (CheckIfCanUseFeat(Self, Target, FeatType.MindTrick2, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.MindTrick2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.MindTrick1, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.MindTrick1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) BurstOfSpeed()
        {
            // Burst of Speed
            if (CheckIfCanUseFeat(Self, Self, FeatType.BurstOfSpeed5, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.BurstOfSpeed5, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BurstOfSpeed4, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.BurstOfSpeed4, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BurstOfSpeed3, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.BurstOfSpeed3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BurstOfSpeed2, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.BurstOfSpeed2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BurstOfSpeed1, () => SelfActiveConcentration == FeatType.Invalid))
            {
                return (true, (FeatType.BurstOfSpeed1, Self));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) ForceLeap()
        {
            // Force Leap
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceLeap3))
            {
                return (true, (FeatType.ForceLeap3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceLeap2))
            {
                return (true, (FeatType.ForceLeap2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.ForceLeap1))
            {
                return (true, (FeatType.ForceLeap1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) NPCAbilities()
        {

            // Roar
            if (CheckIfCanUseFeat(Self, Self, FeatType.Roar))
            {
                return (true, (FeatType.Roar, Self));
            }

            // Bite
            if (CheckIfCanUseFeat(Self, Target, FeatType.Bite))
            {
                return (true, (FeatType.Bite, Target));
            }

            // Iron Shell
            if (CheckIfCanUseFeat(Self, Target, FeatType.IronShell))
            {
                return (true, (FeatType.IronShell, Self));
            }

            // Screech
            if (CheckIfCanUseFeat(Self, Target, FeatType.Screech))
            {
                return (true, (FeatType.Screech, Self));
            }

            // Greater Earthquake
            if (CheckIfCanUseFeat(Self, Self, FeatType.GreaterEarthquake))
            {
                return (true, (FeatType.GreaterEarthquake, Target));
            }

            // Earthquake
            if (CheckIfCanUseFeat(Self, Self, FeatType.Earthquake))
            {
                return (true, (FeatType.Earthquake, Target));
            }

            // Flame Blast
            if (CheckIfCanUseFeat(Self, Target, FeatType.FlameBlast))
            {
                return (true, (FeatType.FlameBlast, Target));
            }

            // Fire Breath
            if (CheckIfCanUseFeat(Self, Target, FeatType.FireBreath))
            {
                return (true, (FeatType.FireBreath, Target));
            }

            // Spikes
            if (CheckIfCanUseFeat(Self, Target, FeatType.Spikes))
            {
                return (true, (FeatType.Spikes, Target));
            }

            // Venom
            if (CheckIfCanUseFeat(Self, Target, FeatType.Venom))
            {
                return (true, (FeatType.Venom, Target));
            }

            // Talon
            if (CheckIfCanUseFeat(Self, Target, FeatType.Talon))
            {
                return (true, (FeatType.Talon, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) WristRocket()
        {
            // Wrist Rocket
            if (CheckIfCanUseFeat(Self, Target, FeatType.WristRocket3))
            {
                return (true, (FeatType.WristRocket3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.WristRocket2))
            {
                return (true, (FeatType.WristRocket2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.WristRocket1))
            {
                return (true, (FeatType.WristRocket1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) Provoke()
        {
            // Provoke
            if (CheckIfCanUseFeat(Self, Target, FeatType.Provoke2))
            {
                return (true, (FeatType.Provoke2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Provoke1))
            {
                return (true, (FeatType.Provoke1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) Resuscitation()
        {
            // Resuscitation
            if (CheckIfCanUseFeat(Self, Target, FeatType.Resuscitation3))
            {
                return (true, (FeatType.Resuscitation3, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Resuscitation2))
            {
                return (true, (FeatType.Resuscitation2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Resuscitation1))
            {
                return (true, (FeatType.Resuscitation1, Target));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) TreatmentKit()
        {
            if (CheckIfCanUseFeat(Self, AllyWithTreatmentKit2StatusEffect, FeatType.TreatmentKit2))
            {
                return (true, (FeatType.TreatmentKit2, AllyWithTreatmentKit2StatusEffect));
            }
            if (CheckIfCanUseFeat(Self, AllyWithTreatmentKit1StatusEffect, FeatType.TreatmentKit1))
            {
                return (true, (FeatType.TreatmentKit1, AllyWithTreatmentKit1StatusEffect));
            }

            return NoAction;
        }

        protected (bool, (FeatType, uint)) Infusion()
        {
            // Infusion
            if (CheckIfCanUseFeat(Self, Target, FeatType.Infusion2, () => LowestHPAllyPercentage < 100))
            {
                return (true, (FeatType.Infusion2, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Infusion1, () => LowestHPAllyPercentage < 100))
            {
                return (true, (FeatType.Infusion1, LowestHPAlly));
            }

            return NoAction;
        }
    }
}
