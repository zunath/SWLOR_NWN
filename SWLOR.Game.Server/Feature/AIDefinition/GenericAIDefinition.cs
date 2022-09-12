using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature.AIDefinition
{
    public static class GenericAIDefinition
    {
        /// <summary>
        /// Determines which perk ability to use.
        /// </summary>
        /// <param name="self">The creature</param>
        /// <param name="target">The creature's target</param>
        /// <param name="allies">Allies associated with this creature. Should also include this creature.</param>
        /// <returns>A feat and target</returns>
        public static (FeatType, uint) DeterminePerkAbility(uint self, uint target, HashSet<uint> allies)
        {
            static float CalculateAverageHP(uint creature)
            {
                var currentHP = GetCurrentHitPoints(creature);
                var maxHP = GetMaxHitPoints(creature);
                return ((float)currentHP / (float)maxHP) * 100;
            }

            var hpPercentage = CalculateAverageHP(self);

            var lowestHPAlly = allies.OrderBy(CalculateAverageHP).First();
            var allyHPPercentage = CalculateAverageHP(lowestHPAlly);
            var selfRace = GetRacialType(self);
            var lowestHPAllyRace = GetRacialType(lowestHPAlly);
            var allyCount = allies.Count;
            var activeConcentration = Ability.GetActiveConcentration(self).Feat;
            

            // Battle Insight
            if (CheckIfCanUseFeat(self, self, FeatType.BattleInsight2, () => allyCount >= 1 && activeConcentration == FeatType.Invalid))
            {
                return (FeatType.BattleInsight2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.BattleInsight1, () => allyCount >= 1 && activeConcentration == FeatType.Invalid))
            {
                return (FeatType.BattleInsight1, self);
            }

            // Force Heal
            if (CheckIfCanUseFeat(self, self, FeatType.ForceHeal5, () => hpPercentage <= 80 && activeConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceHeal5, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.ForceHeal4, () => hpPercentage <= 80 && activeConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceHeal4, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.ForceHeal3, () => hpPercentage <= 80 && activeConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceHeal3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.ForceHeal2, () => hpPercentage <= 80 && activeConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceHeal2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.ForceHeal1, () => hpPercentage <= 80 && activeConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceHeal1, self);
            }

            // Benevolence
            if(CheckIfCanUseFeat(self, self, FeatType.Benevolence3, () => hpPercentage <= 50))
            {
                return (FeatType.Benevolence3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.Benevolence2, () => hpPercentage <= 50))
            {
                return (FeatType.Benevolence2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.Benevolence2, () => hpPercentage <= 50))
            {
                return (FeatType.Benevolence2, self);
            }

            // Force Drain
            if (CheckIfCanUseFeat(self, target, FeatType.ForceDrain5, () => activeConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceDrain5, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForceDrain4, () => activeConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceDrain4, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForceDrain3, () => activeConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceDrain3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForceDrain2, () => activeConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceDrain2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForceDrain1, () => activeConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceDrain1, target);
            }

            // Force Lightning
            if (CheckIfCanUseFeat(self, target, FeatType.ForceLightning4))
            {
                return (FeatType.ForceLightning4, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForceLightning3))
            {
                return (FeatType.ForceLightning3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForceLightning2))
            {
                return (FeatType.ForceLightning2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForceLightning1))
            {
                return (FeatType.ForceLightning1, target);
            }

            // Creeping Terror
            if (CheckIfCanUseFeat(self, target, FeatType.CreepingTerror3))
            {
                return (FeatType.CreepingTerror3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.CreepingTerror2))
            {
                return (FeatType.CreepingTerror2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.CreepingTerror1))
            {
                return (FeatType.CreepingTerror1, target);
            }

            //Force Burst
            if (CheckIfCanUseFeat(self, target, FeatType.ForceBurst4))
            {
                return (FeatType.ForceBurst4, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForceBurst4))
            {
                return (FeatType.ForceBurst3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForceBurst2))
            {
                return (FeatType.ForceBurst2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForceBurst1))
            {
                return (FeatType.ForceBurst1, target);
            }

            // Force Spark
            if (CheckIfCanUseFeat(self, target, FeatType.ForceSpark3))
            {
                return (FeatType.ForceSpark3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForceSpark2))
            {
                return (FeatType.ForceSpark2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForceSpark1))
            {
                return (FeatType.ForceSpark1, target);
            }

            // Disturbance
            if (CheckIfCanUseFeat(self, target, FeatType.Disturbance3))
            {
                return (FeatType.Disturbance3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.Disturbance2))
            {
                return (FeatType.Disturbance2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.Disturbance1))
            {
                return (FeatType.Disturbance1, target);
            }

            // Concussion Grenade
            if (CheckIfCanUseFeat(self, target, FeatType.ConcussionGrenade3))
            {
                return (FeatType.ConcussionGrenade3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ConcussionGrenade2))
            {
                return (FeatType.ConcussionGrenade2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ConcussionGrenade1))
            {
                return (FeatType.ConcussionGrenade1, target);
            }

            // Adhesive Grenade
            if (CheckIfCanUseFeat(self, target, FeatType.AdhesiveGrenade3))
            {
                return (FeatType.AdhesiveGrenade3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.AdhesiveGrenade2))
            {
                return (FeatType.AdhesiveGrenade2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.AdhesiveGrenade1))
            {
                return (FeatType.AdhesiveGrenade1, target);
            }

            // Incendiary Bomb
            if (CheckIfCanUseFeat(self, target, FeatType.IncendiaryBomb3))
            {
                return (FeatType.IncendiaryBomb3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.IncendiaryBomb2))
            {
                return (FeatType.IncendiaryBomb2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.IncendiaryBomb1))
            {
                return (FeatType.IncendiaryBomb1, target);
            }

            // Gas Bomb
            if (CheckIfCanUseFeat(self, target, FeatType.GasBomb3))
            {
                return (FeatType.GasBomb3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.GasBomb2))
            {
                return (FeatType.GasBomb2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.GasBomb1))
            {
                return (FeatType.GasBomb1, target);
            }

            // Gas Bomb
            if (CheckIfCanUseFeat(self, target, FeatType.WristRocket3))
            {
                return (FeatType.WristRocket3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.WristRocket2))
            {
                return (FeatType.WristRocket2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.WristRocket1))
            {
                return (FeatType.WristRocket1, target);
            }

            // Frag Grenade
            if (CheckIfCanUseFeat(self, target, FeatType.FragGrenade3))
            {
                return (FeatType.FragGrenade3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.FragGrenade2))
            {
                return (FeatType.FragGrenade2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.FragGrenade1))
            {
                return (FeatType.FragGrenade1, target);
            }

            // Deflector Shield
            if (CheckIfCanUseFeat(self, self, FeatType.DeflectorShield3))
            {
                return (FeatType.DeflectorShield3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.DeflectorShield2))
            {
                return (FeatType.DeflectorShield2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.DeflectorShield1))
            {
                return (FeatType.DeflectorShield1, self);
            }

            // Knockdown
            if (CheckIfCanUseFeat(self, self, FeatType.Knockdown))
            {
                return (FeatType.Knockdown, self);
            }

            // Knockdown
            if (CheckIfCanUseFeat(self, self, FeatType.Furor))
            {
                return (FeatType.Furor, self);
            }

            // Throw Saber
            if (CheckIfCanUseFeat(self, target, FeatType.ThrowLightsaber3))
            {
                return (FeatType.ThrowLightsaber3, target);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.ThrowLightsaber2))
            {
                return (FeatType.ThrowLightsaber2, target);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.ThrowLightsaber1))
            {
                return (FeatType.ThrowLightsaber1, target);
            }

            // Force Stun
            if (CheckIfCanUseFeat(self, target, FeatType.ForceStun3, () => activeConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceStun3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForceStun2, () => activeConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceStun2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForceStun1, () => activeConcentration == FeatType.Invalid))
            {
                return (FeatType.ForceStun1, target);
            }

            // Mind Trick
            if (CheckIfCanUseFeat(self, target, FeatType.MindTrick2, () => activeConcentration == FeatType.Invalid))
            {
                return (FeatType.MindTrick2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.MindTrick1, () => activeConcentration == FeatType.Invalid))
            {
                return (FeatType.MindTrick1, target);
            }

            // Burst of Speed
            if (CheckIfCanUseFeat(self, self, FeatType.BurstOfSpeed5, () => activeConcentration == FeatType.Invalid))
            {
                return (FeatType.BurstOfSpeed5, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.BurstOfSpeed4, () => activeConcentration == FeatType.Invalid))
            {
                return (FeatType.BurstOfSpeed4, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.BurstOfSpeed3, () => activeConcentration == FeatType.Invalid))
            {
                return (FeatType.BurstOfSpeed3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.BurstOfSpeed2, () => activeConcentration == FeatType.Invalid))
            {
                return (FeatType.BurstOfSpeed2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.BurstOfSpeed1, () => activeConcentration == FeatType.Invalid))
            {
                return (FeatType.BurstOfSpeed1, self);
            }

            // Hacking Blade
            if (CheckIfCanUseFeat(self, self, FeatType.HackingBlade3))
            {
                return (FeatType.HackingBlade3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.HackingBlade2))
            {
                return (FeatType.HackingBlade2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.HackingBlade1))
            {
                return (FeatType.HackingBlade1, self);
            }

            // Riot Blade
            if (CheckIfCanUseFeat(self, target, FeatType.RiotBlade3))
            {
                return (FeatType.RiotBlade3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.RiotBlade2))
            {
                return (FeatType.RiotBlade2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.RiotBlade1))
            {
                return (FeatType.RiotBlade1, target);
            }

            // Poison Stab
            if (CheckIfCanUseFeat(self, self, FeatType.PoisonStab3))
            {
                return (FeatType.PoisonStab3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.PoisonStab2))
            {
                return (FeatType.PoisonStab2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.PoisonStab1))
            {
                return (FeatType.PoisonStab1, self);
            }

            // Backstab
            if (CheckIfCanUseFeat(self, target, FeatType.Backstab3))
            {
                return (FeatType.Backstab3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.Backstab2))
            {
                return (FeatType.Backstab2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.Backstab1))
            {
                return (FeatType.Backstab1, target);
            }

            // Force Leap
            if (CheckIfCanUseFeat(self, target, FeatType.ForceLeap3))
            {
                return (FeatType.ForceLeap3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForceLeap2))
            {
                return (FeatType.ForceLeap2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForceLeap1))
            {
                return (FeatType.ForceLeap1, target);
            }

            // Saber Strike
            if (CheckIfCanUseFeat(self, self, FeatType.SaberStrike3))
            {
                return (FeatType.SaberStrike3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.SaberStrike2))
            {
                return (FeatType.SaberStrike2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.SaberStrike1))
            {
                return (FeatType.SaberStrike1, self);
            }

            // Crescent Moon
            if (CheckIfCanUseFeat(self, self, FeatType.CrescentMoon3))
            {
                return (FeatType.CrescentMoon3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.CrescentMoon2))
            {
                return (FeatType.CrescentMoon2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.CrescentMoon1))
            {
                return (FeatType.CrescentMoon1, self);
            }

            // Hard Slash
            if (CheckIfCanUseFeat(self, target, FeatType.HardSlash3))
            {
                return (FeatType.HardSlash3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.HardSlash2))
            {
                return (FeatType.HardSlash2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.HardSlash1))
            {
                return (FeatType.HardSlash1, target);
            }

            // Skewer
            if (CheckIfCanUseFeat(self, self, FeatType.Skewer3))
            {
                return (FeatType.Skewer3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.Skewer2))
            {
                return (FeatType.Skewer2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.Skewer1))
            {
                return (FeatType.Skewer1, self);
            }

            // Double Thrust
            if (CheckIfCanUseFeat(self, target, FeatType.DoubleThrust3))
            {
                return (FeatType.DoubleThrust3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.DoubleThrust2))
            {
                return (FeatType.DoubleThrust2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.DoubleThrust1))
            {
                return (FeatType.DoubleThrust1, target);
            }


            // Leg Sweep
            if (CheckIfCanUseFeat(self, self, FeatType.LegSweep3))
            {
                return (FeatType.LegSweep3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.LegSweep2))
            {
                return (FeatType.LegSweep2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.LegSweep1))
            {
                return (FeatType.LegSweep1, self);
            }

            // Cross Cut
            if (CheckIfCanUseFeat(self, target, FeatType.CrossCut3))
            {
                return (FeatType.CrossCut3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.CrossCut2))
            {
                return (FeatType.CrossCut2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.CrossCut3))
            {
                return (FeatType.CrossCut1, target);
            }

            // Circle Slash
            if (CheckIfCanUseFeat(self, self, FeatType.CircleSlash3))
            {
                return (FeatType.CircleSlash3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.CircleSlash2))
            {
                return (FeatType.CircleSlash2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.CircleSlash1))
            {
                return (FeatType.CircleSlash1, self);
            }

            // Double Strike
            if (CheckIfCanUseFeat(self, target, FeatType.DoubleStrike3))
            {
                return (FeatType.DoubleStrike3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.DoubleStrike2))
            {
                return (FeatType.DoubleStrike2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.DoubleStrike1))
            {
                return (FeatType.DoubleStrike1, target);
            }

            // Electric Fist
            if (CheckIfCanUseFeat(self, self, FeatType.ElectricFist3))
            {
                return (FeatType.ElectricFist3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.ElectricFist2))
            {
                return (FeatType.ElectricFist2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.ElectricFist1))
            {
                return (FeatType.ElectricFist1, self);
            }

            // Striking Cobra
            if (CheckIfCanUseFeat(self, self, FeatType.StrikingCobra3))
            {
                return (FeatType.StrikingCobra3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.StrikingCobra2))
            {
                return (FeatType.StrikingCobra2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.StrikingCobra1))
            {
                return (FeatType.StrikingCobra1, self);
            }

            // Slam
            if (CheckIfCanUseFeat(self, target, FeatType.Slam3))
            {
                return (FeatType.Slam3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.Slam2))
            {
                return (FeatType.Slam2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.Slam1))
            {
                return (FeatType.Slam1, target);
            }

            // Spinning Whirl
            if (CheckIfCanUseFeat(self, target, FeatType.SpinningWhirl3))
            {
                return (FeatType.SpinningWhirl3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.SpinningWhirl2))
            {
                return (FeatType.SpinningWhirl2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.SpinningWhirl1))
            {
                return (FeatType.SpinningWhirl1, target);
            }

            // Quick Draw
            if (CheckIfCanUseFeat(self, target, FeatType.QuickDraw3))
            {
                return (FeatType.QuickDraw3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.QuickDraw2))
            {
                return (FeatType.QuickDraw2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.QuickDraw1))
            {
                return (FeatType.QuickDraw1, target);
            }

            // Double Shot
            if (CheckIfCanUseFeat(self, target, FeatType.DoubleShot3))
            {
                return (FeatType.DoubleShot3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.DoubleShot2))
            {
                return (FeatType.DoubleShot2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.DoubleShot1))
            {
                return (FeatType.DoubleShot1, target);
            }

            // Explosive Toss
            if (CheckIfCanUseFeat(self, self, FeatType.ExplosiveToss3))
            {
                return (FeatType.ExplosiveToss3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.ExplosiveToss2))
            {
                return (FeatType.ExplosiveToss2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.ExplosiveToss1))
            {
                return (FeatType.ExplosiveToss1, self);
            }

            // Piercing Toss
            if (CheckIfCanUseFeat(self, target, FeatType.PiercingToss3))
            {
                return (FeatType.PiercingToss3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.PiercingToss2))
            {
                return (FeatType.PiercingToss2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.PiercingToss1))
            {
                return (FeatType.PiercingToss1, target);
            }

            // Tranquilizer Shot
            if (CheckIfCanUseFeat(self, self, FeatType.TranquilizerShot3))
            {
                return (FeatType.TranquilizerShot3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.TranquilizerShot2))
            {
                return (FeatType.TranquilizerShot2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.TranquilizerShot1))
            {
                return (FeatType.TranquilizerShot1, self);
            }

            // Crippling Shot
            if (CheckIfCanUseFeat(self, self, FeatType.CripplingShot3))
            {
                return (FeatType.CripplingShot3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.CripplingShot2))
            {
                return (FeatType.CripplingShot2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.CripplingShot1))
            {
                return (FeatType.CripplingShot1, self);
            }

            // Roar
            if (CheckIfCanUseFeat(self, self, FeatType.Roar))
            {
                return (FeatType.Roar, self);
            }

            // Bite
            if (CheckIfCanUseFeat(self, target, FeatType.Bite))
            {
                return (FeatType.Bite, target);
            }

            // Iron Shell
            if (CheckIfCanUseFeat(self, target, FeatType.IronShell))
            {
                return (FeatType.IronShell, self);
            }

            // Earthquake
            if (CheckIfCanUseFeat(self, self, FeatType.Earthquake))
            {
                return (FeatType.Earthquake, target);
            }

            // Fire Breath
            if (CheckIfCanUseFeat(self, target, FeatType.FireBreath))
            {
                return (FeatType.FireBreath, target);
            }

            // Spikes
            if (CheckIfCanUseFeat(self, target, FeatType.Spikes))
            {
                return (FeatType.Spikes, target);
            }

            // Venom
            if (CheckIfCanUseFeat(self, target, FeatType.Venom))
            {
                return (FeatType.Venom, target);
            }

            // Talon
            if (CheckIfCanUseFeat(self, target, FeatType.Talon))
            {
                return (FeatType.Talon, target);
            }

            return (FeatType.Invalid, OBJECT_INVALID);
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
        private static bool CheckIfCanUseFeat(uint creature, uint target, FeatType feat, Func<bool> condition = null)
        {
            if (!GetHasFeat(feat, creature)) return false;
            if (condition != null && !condition()) return false;
            if (!GetIsObjectValid(target)) return false;

            var targetLocation = GetLocation(target);
            var abilityDetail = Ability.GetAbilityDetail(feat);
            var effectiveLevel = Perk.GetEffectivePerkLevel(creature, abilityDetail.EffectiveLevelPerkType);
            return Ability.CanUseAbility(creature, target, feat, effectiveLevel, targetLocation);
        }

    }
}
