using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AIDefinition
{
    public sealed class BeastAIDefinition : AIBase
    {
        private static readonly FeatType[] SelfTargetedFeats =
        {
            FeatType.PredatorRush1,
            FeatType.IronHide3,
            FeatType.IronHide2,
            FeatType.IronHide1,
            FeatType.GuardingRoar3,
            FeatType.GuardingRoar2,
            FeatType.GuardingRoar1,
            FeatType.Intercept2,
            FeatType.Intercept1,
            FeatType.RampartHide1,
            FeatType.UnbreakableBeast1,
            FeatType.BolsterAttack3,
            FeatType.BolsterAttack2,
            FeatType.BolsterAttack1,
            FeatType.Hasten2,
            FeatType.Hasten1,
            FeatType.PackRecovery1,
            FeatType.AlphaRhythm1,
            FeatType.PrimalOverrun1,
            FeatType.EvasiveManeuver3,
            FeatType.EvasiveManeuver2,
            FeatType.EvasiveManeuver1,
            FeatType.EvasiveChallenge2,
            FeatType.UntouchableInstinct1,
            FeatType.Innervate3,
            FeatType.Innervate2,
            FeatType.Innervate1,
            FeatType.WardingHowl3,
            FeatType.WardingHowl2,
            FeatType.WardingHowl1,
            FeatType.PsychicCry3,
            FeatType.PsychicCry2,
            FeatType.PsychicCry1,
            FeatType.ForceBondedBeast1,
        };

        private static readonly FeatType[] EnemyTargetedFeats =
        {
            FeatType.Bite3,
            FeatType.Bite2,
            FeatType.Bite1,
            FeatType.RendingClaw3,
            FeatType.RendingClaw2,
            FeatType.RendingClaw1,
            FeatType.Pounce2,
            FeatType.Pounce1,
            FeatType.PredatorsMark1,
            FeatType.ExposePrey1,
            FeatType.ExecutePrey1,
            FeatType.ApexBite1,
            FeatType.Anger2,
            FeatType.Anger1,
            FeatType.Claw3,
            FeatType.Claw2,
            FeatType.Claw1,
            FeatType.GuardedBite3,
            FeatType.GuardedBite2,
            FeatType.GuardedBite1,
            FeatType.CoordinatedStrike2,
            FeatType.CoordinatedStrike1,
            FeatType.PoisonBreath3,
            FeatType.PoisonBreath2,
            FeatType.PoisonBreath1,
            FeatType.IceBreath3,
            FeatType.IceBreath2,
            FeatType.IceBreath1,
            FeatType.CrushingSlam3,
            FeatType.CrushingSlam2,
            FeatType.CrushingSlam1,
            FeatType.Rampage2,
            FeatType.Rampage1,
            FeatType.Assault3,
            FeatType.Assault2,
            FeatType.Assault1,
            FeatType.DistractingFeint3,
            FeatType.DistractingFeint2,
            FeatType.DistractingFeint1,
            FeatType.EvasiveChallenge1,
            FeatType.ForceTouch3,
            FeatType.ForceTouch2,
            FeatType.ForceTouch1,
        };

        public override (FeatType, uint) DeterminePerkAbility()
        {
            foreach (var feat in SelfTargetedFeats)
            {
                if (CheckIfCanUseFeat(Self, Self, feat))
                    return (feat, Self);
            }

            foreach (var feat in EnemyTargetedFeats)
            {
                if (CheckIfCanUseFeat(Self, Target, feat))
                    return (feat, Target);
            }

            return base.DeterminePerkAbility();
        }
    }
}
