using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Feature.AbilityDefinition.Beasts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;

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
            
            (success, result) = BolsterArmor();
            if (success) return result;

            (success, result) = EvasiveManeuver();
            if (success) return result;

            // Enmity
            (success, result) = Anger();
            if (success) return result;

            // Damage
            (success, result) = DiseasedTouch();
            if (success) return result;

            (success, result) = Clip();
            if (success) return result;

            (success, result) = SpinningClaw();
            if (success) return result;

            (success, result) = Bite();
            if (success) return result;

            (success, result) = FlameBreath();
            if (success) return result;

            (success, result) = ShockingSlash();
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

        private (bool, (FeatType, uint)) DiseasedTouch()
        {
            if (CheckIfCanUseFeat(Self, Self, FeatType.DiseasedTouch5))
            {
                return (true, (FeatType.DiseasedTouch5, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.DiseasedTouch4))
            {
                return (true, (FeatType.DiseasedTouch4, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.DiseasedTouch3))
            {
                return (true, (FeatType.DiseasedTouch3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.DiseasedTouch2))
            {
                return (true, (FeatType.DiseasedTouch2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.DiseasedTouch1))
            {
                return (true, (FeatType.DiseasedTouch1, Self));
            }

            return NoAction;
        }

        private (bool, (FeatType, uint)) Clip()
        {
            if (CheckIfCanUseFeat(Self, Self, FeatType.Clip5))
            {
                return (true, (FeatType.Clip5, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Clip4))
            {
                return (true, (FeatType.Clip4, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Clip3))
            {
                return (true, (FeatType.Clip3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Clip2))
            {
                return (true, (FeatType.Clip2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Clip1))
            {
                return (true, (FeatType.Clip1, Self));
            }

            return NoAction;
        }

        private (bool, (FeatType, uint)) SpinningClaw()
        {
            if (CheckIfCanUseFeat(Self, Self, FeatType.SpinningClaw5))
            {
                return (true, (FeatType.SpinningClaw5, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.SpinningClaw4))
            {
                return (true, (FeatType.SpinningClaw4, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.SpinningClaw3))
            {
                return (true, (FeatType.SpinningClaw3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.SpinningClaw2))
            {
                return (true, (FeatType.SpinningClaw2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.SpinningClaw1))
            {
                return (true, (FeatType.SpinningClaw1, Self));
            }

            return NoAction;
        }

        private (bool, (FeatType, uint)) Bite()
        {
            if (CheckIfCanUseFeat(Self, Self, FeatType.Bite5))
            {
                return (true, (FeatType.Bite5, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Bite4))
            {
                return (true, (FeatType.Bite4, Self));
            }
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

        private (bool, (FeatType, uint)) FlameBreath()
        {
            if (CheckIfCanUseFeat(Self, Self, FeatType.FlameBreath5))
            {
                return (true, (FeatType.FlameBreath5, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.FlameBreath4))
            {
                return (true, (FeatType.FlameBreath4, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.FlameBreath3))
            {
                return (true, (FeatType.FlameBreath3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.FlameBreath2))
            {
                return (true, (FeatType.FlameBreath2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.FlameBreath1))
            {
                return (true, (FeatType.FlameBreath1, Self));
            }

            return NoAction;
        }

        private (bool, (FeatType, uint)) ShockingSlash()
        {
            if (CheckIfCanUseFeat(Self, Self, FeatType.ShockingSlash5))
            {
                return (true, (FeatType.ShockingSlash5, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ShockingSlash4))
            {
                return (true, (FeatType.ShockingSlash4, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ShockingSlash3))
            {
                return (true, (FeatType.ShockingSlash3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ShockingSlash2))
            {
                return (true, (FeatType.ShockingSlash2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ShockingSlash1))
            {
                return (true, (FeatType.ShockingSlash1, Self));
            }

            return NoAction;
        }

        private (bool, (FeatType, uint)) BolsterArmor()
        {
            if (CheckIfCanUseFeat(Self, Self, FeatType.BolsterArmor5, 
                    () => !StatusEffect.HasStatusEffect(Self, 
                        StatusEffectType.BolsterArmor5, 
                        StatusEffectType.BolsterArmor4, 
                        StatusEffectType.BolsterArmor3, 
                        StatusEffectType.BolsterArmor2, 
                        StatusEffectType.BolsterArmor1)))
            {
                return (true, (FeatType.BolsterArmor5, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BolsterArmor4,
                    () => !StatusEffect.HasStatusEffect(Self,
                        StatusEffectType.BolsterArmor5,
                        StatusEffectType.BolsterArmor4,
                        StatusEffectType.BolsterArmor3,
                        StatusEffectType.BolsterArmor2,
                        StatusEffectType.BolsterArmor1)))
            {
                return (true, (FeatType.BolsterArmor4, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BolsterArmor3,
                    () => !StatusEffect.HasStatusEffect(Self,
                        StatusEffectType.BolsterArmor5,
                        StatusEffectType.BolsterArmor4,
                        StatusEffectType.BolsterArmor3,
                        StatusEffectType.BolsterArmor2,
                        StatusEffectType.BolsterArmor1)))
            {
                return (true, (FeatType.BolsterArmor3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BolsterArmor2,
                    () => !StatusEffect.HasStatusEffect(Self,
                        StatusEffectType.BolsterArmor5,
                        StatusEffectType.BolsterArmor4,
                        StatusEffectType.BolsterArmor3,
                        StatusEffectType.BolsterArmor2,
                        StatusEffectType.BolsterArmor1)))
            {
                return (true, (FeatType.BolsterArmor2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BolsterArmor1,
                    () => !StatusEffect.HasStatusEffect(Self,
                        StatusEffectType.BolsterArmor5,
                        StatusEffectType.BolsterArmor4,
                        StatusEffectType.BolsterArmor3,
                        StatusEffectType.BolsterArmor2,
                        StatusEffectType.BolsterArmor1)))
            {
                return (true, (FeatType.BolsterArmor1, Self));
            }

            return NoAction;
        }

        private (bool, (FeatType, uint)) Anger()
        {
            if (CheckIfCanUseFeat(Self, Target, FeatType.Anger5))
            {
                return (true, (FeatType.Anger5, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Anger4))
            {
                return (true, (FeatType.Anger4, Target));
            }
            if (CheckIfCanUseFeat(Self, Target, FeatType.Anger3))
            {
                return (true, (FeatType.Anger3, Target));
            }
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
            if (CheckIfCanUseFeat(Self, Self, FeatType.Claw5))
            {
                return (true, (FeatType.Claw5, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Claw4))
            {
                return (true, (FeatType.Claw4, Self));
            }
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
            if (CheckIfCanUseFeat(Self, Self, FeatType.BolsterAttack5,
                    () => !StatusEffect.HasStatusEffect(Self,
                        StatusEffectType.BolsterAttack5,
                        StatusEffectType.BolsterAttack4,
                        StatusEffectType.BolsterAttack3,
                        StatusEffectType.BolsterAttack2,
                        StatusEffectType.BolsterAttack1)))
            {
                return (true, (FeatType.BolsterAttack5, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BolsterAttack4,
                    () => !StatusEffect.HasStatusEffect(Self,
                        StatusEffectType.BolsterAttack5,
                        StatusEffectType.BolsterAttack4,
                        StatusEffectType.BolsterAttack3,
                        StatusEffectType.BolsterAttack2,
                        StatusEffectType.BolsterAttack1)))
            {
                return (true, (FeatType.BolsterAttack4, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BolsterAttack3,
                    () => !StatusEffect.HasStatusEffect(Self,
                        StatusEffectType.BolsterAttack5,
                        StatusEffectType.BolsterAttack4,
                        StatusEffectType.BolsterAttack3,
                        StatusEffectType.BolsterAttack2,
                        StatusEffectType.BolsterAttack1)))
            {
                return (true, (FeatType.BolsterAttack3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BolsterAttack2,
                    () => !StatusEffect.HasStatusEffect(Self,
                        StatusEffectType.BolsterAttack5,
                        StatusEffectType.BolsterAttack4,
                        StatusEffectType.BolsterAttack3,
                        StatusEffectType.BolsterAttack2,
                        StatusEffectType.BolsterAttack1)))
            {
                return (true, (FeatType.BolsterAttack2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.BolsterAttack1,
                    () => !StatusEffect.HasStatusEffect(Self,
                        StatusEffectType.BolsterAttack5,
                        StatusEffectType.BolsterAttack4,
                        StatusEffectType.BolsterAttack3,
                        StatusEffectType.BolsterAttack2,
                        StatusEffectType.BolsterAttack1)))
            {
                return (true, (FeatType.BolsterAttack1, Self));
            }

            return NoAction;
        }

        private (bool, (FeatType, uint)) Hasten()
        {
            if (CheckIfCanUseFeat(Self, Self, FeatType.Hasten3,
                    () => !HasEffectByTag(Self, HastenAbilityDefinition.HastenEffectTag)))
            {
                return (true, (FeatType.Hasten3, Self));
            }
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
            if (CheckIfCanUseFeat(Self, Self, FeatType.PoisonBreath5))
            {
                return (true, (FeatType.PoisonBreath5, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.PoisonBreath4))
            {
                return (true, (FeatType.PoisonBreath4, Self));
            }
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
            if (CheckIfCanUseFeat(Self, Self, FeatType.IceBreath5))
            {
                return (true, (FeatType.IceBreath5, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.IceBreath4))
            {
                return (true, (FeatType.IceBreath4, Self));
            }
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
            if (CheckIfCanUseFeat(Self, Self, FeatType.EvasiveManeuver5,
                    () => !StatusEffect.HasStatusEffect(Self,
                        StatusEffectType.EvasiveManeuver5,
                        StatusEffectType.EvasiveManeuver4,
                        StatusEffectType.EvasiveManeuver3,
                        StatusEffectType.EvasiveManeuver2,
                        StatusEffectType.EvasiveManeuver1)))
            {
                return (true, (FeatType.EvasiveManeuver5, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.EvasiveManeuver4,
                    () => !StatusEffect.HasStatusEffect(Self,
                        StatusEffectType.EvasiveManeuver5,
                        StatusEffectType.EvasiveManeuver4,
                        StatusEffectType.EvasiveManeuver3,
                        StatusEffectType.EvasiveManeuver2,
                        StatusEffectType.EvasiveManeuver1)))
            {
                return (true, (FeatType.EvasiveManeuver4, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.EvasiveManeuver3,
                    () => !StatusEffect.HasStatusEffect(Self,
                        StatusEffectType.EvasiveManeuver5,
                        StatusEffectType.EvasiveManeuver4,
                        StatusEffectType.EvasiveManeuver3,
                        StatusEffectType.EvasiveManeuver2,
                        StatusEffectType.EvasiveManeuver1)))
            {
                return (true, (FeatType.EvasiveManeuver3, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.EvasiveManeuver2,
                    () => !StatusEffect.HasStatusEffect(Self,
                        StatusEffectType.EvasiveManeuver5,
                        StatusEffectType.EvasiveManeuver4,
                        StatusEffectType.EvasiveManeuver3,
                        StatusEffectType.EvasiveManeuver2,
                        StatusEffectType.EvasiveManeuver1)))
            {
                return (true, (FeatType.EvasiveManeuver2, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.EvasiveManeuver1,
                    () => !StatusEffect.HasStatusEffect(Self,
                        StatusEffectType.EvasiveManeuver5,
                        StatusEffectType.EvasiveManeuver4,
                        StatusEffectType.EvasiveManeuver3,
                        StatusEffectType.EvasiveManeuver2,
                        StatusEffectType.EvasiveManeuver1)))
            {
                return (true, (FeatType.EvasiveManeuver1, Self));
            }


            return NoAction;
        }

        private (bool, (FeatType, uint)) Assault()
        {
            if (CheckIfCanUseFeat(Self, Self, FeatType.Assault5))
            {
                return (true, (FeatType.Assault5, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.Assault4))
            {
                return (true, (FeatType.Assault4, Self));
            }
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
            if (CheckIfCanUseFeat(Self, Self, FeatType.ForceTouch5))
            {
                return (true, (FeatType.ForceTouch5, Self));
            }
            if (CheckIfCanUseFeat(Self, Self, FeatType.ForceTouch4))
            {
                return (true, (FeatType.ForceTouch4, Self));
            }
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
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.Innervate5, () => LowestHPAllyPercentage <= 50))
            {
                return (true, (FeatType.Innervate5, LowestHPAlly));
            }
            if (CheckIfCanUseFeat(Self, LowestHPAlly, FeatType.Innervate4, () => LowestHPAllyPercentage <= 60))
            {
                return (true, (FeatType.Innervate4, LowestHPAlly));
            }
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
