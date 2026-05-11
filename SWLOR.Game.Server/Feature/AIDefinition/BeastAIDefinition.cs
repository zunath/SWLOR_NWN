using SWLOR.Game.Server.Feature.AbilityDefinition.Beasts;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AIDefinition
{
    public class BeastAIDefinition: AIBase
    {
        public override (FeatType, uint) DeterminePerkAbility()
        {
            // Healing
            var (success, result) = Innervate();
            if (success) return result;

            // Buffs
            (success, result) = BolsterAttack();
            if (success) return result;

            (success, result) = EvasiveManeuver();
            if (success) return result;

            // Enmity
            (success, result) = Anger();
            if (success) return result;

            // Damage

            (success, result) = Bite();
            if (success) return result;

            (success, result) = Claw();
            if (success) return result;

            (success, result) = Hasten();
            if (success) return result;

            (success, result) = PoisonBreath();
            if (success) return result;

            (success, result) = IceBreath();
            if (success) return result;

            (success, result) = Assault();
            if (success) return result;

            (success, result) = ForceTouch();
            if (success) return result;


            return NoAction.Item2;
        }


        private (bool, (FeatType, uint)) Bite()
        {

            if (CheckIfCanUseFeat(Self, Self, FeatType.Bite3))
            {
                return (true, (FeatType.Bite3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Bite2))
            {
                return (true, (FeatType.Bite2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Bite1))
            {
                return (true, (FeatType.Bite1, Self));
            }

            return NoAction;
        }



        private (bool, (FeatType, uint)) Anger()
        {

            if (CheckIfCanUseFeat(Self, Target, FeatType.Anger2))
            {
                return (true, (FeatType.Anger2, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Anger1))
            {
                return (true, (FeatType.Anger1, Target));
            }

            return NoAction;
        }

        private (bool, (FeatType, uint)) Claw()
        {

            if (CheckIfCanUseFeat(Self, Self, FeatType.Claw3))
            {
                return (true, (FeatType.Claw3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Claw2))
            {
                return (true, (FeatType.Claw2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Claw1))
            {
                return (true, (FeatType.Claw1, Self));
            }

            return NoAction;
        }

        private (bool, (FeatType, uint)) BolsterAttack()
        {

            if (CheckIfCanUseFeat(Self, Self, FeatType.BolsterAttack3,
                    () => !StatusEffect.HasStatusEffect(Self,
                        typeof(BolsterAttack3StatusEffect),
                        typeof(BolsterAttack2StatusEffect),
                        typeof(BolsterAttack1StatusEffect))))
            {
                return (true, (FeatType.BolsterAttack3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BolsterAttack2,
                    () => !StatusEffect.HasStatusEffect(Self,
                        typeof(BolsterAttack3StatusEffect),
                        typeof(BolsterAttack2StatusEffect),
                        typeof(BolsterAttack1StatusEffect))))
            {
                return (true, (FeatType.BolsterAttack2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BolsterAttack1,
                    () => !StatusEffect.HasStatusEffect(Self,
                        typeof(BolsterAttack3StatusEffect),
                        typeof(BolsterAttack2StatusEffect),
                        typeof(BolsterAttack1StatusEffect))))
            {
                return (true, (FeatType.BolsterAttack1, Self));
            }

            return NoAction;
        }

        private (bool, (FeatType, uint)) Hasten()
        {

            if (CheckIfCanUseFeat(Self, Self, FeatType.Hasten2,
                    () => !HasEffectByTag(Self, HastenAbilityDefinition.HastenEffectTag)))
            {
                return (true, (FeatType.Hasten2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Hasten1,
                    () => !HasEffectByTag(Self, HastenAbilityDefinition.HastenEffectTag)))
            {
                return (true, (FeatType.Hasten1, Self));
            }

            return NoAction;
        }

        private (bool, (FeatType, uint)) PoisonBreath()
        {

            if (CheckIfCanUseFeat(Self, Self, FeatType.PoisonBreath3))
            {
                return (true, (FeatType.PoisonBreath3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.PoisonBreath2))
            {
                return (true, (FeatType.PoisonBreath2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.PoisonBreath1))
            {
                return (true, (FeatType.PoisonBreath1, Self));
            }

            return NoAction;
        }

        private (bool, (FeatType, uint)) IceBreath()
        {

            if (CheckIfCanUseFeat(Self, Self, FeatType.IceBreath3))
            {
                return (true, (FeatType.IceBreath3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.IceBreath2))
            {
                return (true, (FeatType.IceBreath2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.IceBreath1))
            {
                return (true, (FeatType.IceBreath1, Self));
            }

            return NoAction;
        }

        private (bool, (FeatType, uint)) EvasiveManeuver()
        {

            if (CheckIfCanUseFeat(Self, Self, FeatType.EvasiveManeuver3,
                    () => !StatusEffect.HasStatusEffect(Self,
                        typeof(EvasiveManeuver3StatusEffect),
                        typeof(EvasiveManeuver2StatusEffect),
                        typeof(EvasiveManeuver1StatusEffect))))
            {
                return (true, (FeatType.EvasiveManeuver3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.EvasiveManeuver2,
                    () => !StatusEffect.HasStatusEffect(Self,
                        typeof(EvasiveManeuver3StatusEffect),
                        typeof(EvasiveManeuver2StatusEffect),
                        typeof(EvasiveManeuver1StatusEffect))))
            {
                return (true, (FeatType.EvasiveManeuver2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.EvasiveManeuver1,
                    () => !StatusEffect.HasStatusEffect(Self,
                        typeof(EvasiveManeuver3StatusEffect),
                        typeof(EvasiveManeuver2StatusEffect),
                        typeof(EvasiveManeuver1StatusEffect))))
            {
                return (true, (FeatType.EvasiveManeuver1, Self));
            }


            return NoAction;
        }

        private (bool, (FeatType, uint)) Assault()
        {

            if (CheckIfCanUseFeat(Self, Self, FeatType.Assault3))
            {
                return (true, (FeatType.Assault3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Assault2))
            {
                return (true, (FeatType.Assault2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Assault1))
            {
                return (true, (FeatType.Assault1, Self));
            }

            return NoAction;
        }

        private (bool, (FeatType, uint)) ForceTouch()
        {

            if (CheckIfCanUseFeat(Self, Self, FeatType.ForceTouch3))
            {
                return (true, (FeatType.ForceTouch3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ForceTouch2))
            {
                return (true, (FeatType.ForceTouch2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ForceTouch1))
            {
                return (true, (FeatType.ForceTouch1, Self));
            }

            return NoAction;
        }

        private (bool, (FeatType, uint)) Innervate()
        {

            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.Innervate3, () => LowestHPAllyPercentage <= 70))
            {
                return (true, (FeatType.Innervate3, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.Innervate2, () => LowestHPAllyPercentage <= 80))
            {
                return (true, (FeatType.Innervate2, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.Innervate1, () => LowestHPAllyPercentage <= 90))
            {
                return (true, (FeatType.Innervate1, LowestHPAlly));
            }

            return NoAction;
        }
    }
}
