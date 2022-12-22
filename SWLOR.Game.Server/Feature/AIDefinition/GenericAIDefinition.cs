using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AIService;

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
            

            // Kolto Recovery
            if (CheckIfCanUseFeat(self, target, FeatType.KoltoRecovery3, () => allyHPPercentage < 100))
            {
                return (FeatType.KoltoRecovery3, lowestHPAlly);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.KoltoRecovery2, () => allyHPPercentage < 100))
            {
                return (FeatType.KoltoRecovery2, lowestHPAlly);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.KoltoRecovery1, () => allyHPPercentage < 100))
            {
                return (FeatType.KoltoRecovery1, lowestHPAlly);
            }

            // Battle Insight
            if (CheckIfCanUseFeat(self, self, FeatType.BattleInsight2, () => allyCount >= 1 && activeConcentration == FeatType.Invalid))
            {
                return (FeatType.BattleInsight2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.BattleInsight1, () => allyCount >= 1 && activeConcentration == FeatType.Invalid))
            {
                return (FeatType.BattleInsight1, self);
            }

            foreach (var feat in featTypes)
            {
                try
                {
                    var ability = Ability.GetAbilityDetail(feat);
                }
                catch (Exception ex)
                {
                    continue;
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

            // Flamethrower
            if (CheckIfCanUseFeat(self, target, FeatType.Flamethrower3))
            {
                return (FeatType.Flamethrower3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.Flamethrower2))
            {
                return (FeatType.Flamethrower2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.Flamethrower1))
            {
                return (FeatType.Flamethrower1, target);
            }

            // Flashbang Grenade
            if (CheckIfCanUseFeat(self, target, FeatType.FlashbangGrenade3))
            {
                return (FeatType.FlashbangGrenade3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.FlashbangGrenade2))
            {
                return (FeatType.FlashbangGrenade2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.FlashbangGrenade1))
            {
                return (FeatType.FlashbangGrenade1, target);
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

            // Ion Grenade
            if (CheckIfCanUseFeat(self, target, FeatType.IonGrenade3))
            {
                return (FeatType.IonGrenade3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.IonGrenade2))
            {
                return (FeatType.IonGrenade2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.IonGrenade1))
            {
                return (FeatType.IonGrenade1, target);
            }

            // Smoke Bomb
            if (CheckIfCanUseFeat(self, target, FeatType.SmokeBomb3, () => allyHPPercentage < 50))
            {
                return (FeatType.SmokeBomb3, lowestHPAlly);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.SmokeBomb2, () => allyHPPercentage < 50))
            {
                return (FeatType.SmokeBomb2, lowestHPAlly);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.SmokeBomb1, () => allyHPPercentage < 50))
            {
                return (FeatType.SmokeBomb1, lowestHPAlly);
            }

            // Stealth Generator
            if (CheckIfCanUseFeat(self, self, FeatType.StealthGenerator3, () => hpPercentage < 100))
            {
                return (FeatType.StealthGenerator3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.StealthGenerator2, () => hpPercentage < 100))
            {
                return (FeatType.StealthGenerator2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.StealthGenerator1, () => hpPercentage < 100))
            { 
                return (FeatType.StealthGenerator1, self);
            }

            // Deflector Shield
            if (CheckIfCanUseFeat(self, self, FeatType.DeflectorShield3, () => hpPercentage < 100))
            {
                return (FeatType.DeflectorShield3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.DeflectorShield2, () => hpPercentage < 100))
            {
                return (FeatType.DeflectorShield2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.DeflectorShield1, () => hpPercentage < 100))
            {
                return (FeatType.DeflectorShield1, self);
            }

            // Combat Enhancement
            if (CheckIfCanUseFeat(self, self, FeatType.CombatEnhancement3, () => hpPercentage >= 95))
            {
                return (FeatType.CombatEnhancement3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.CombatEnhancement2, () => hpPercentage >= 95))
            {
                return (FeatType.CombatEnhancement2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.CombatEnhancement1, () => hpPercentage >= 95))
            {
                return (FeatType.CombatEnhancement1, self);
            }

            // Shielding
            if (CheckIfCanUseFeat(self, self, FeatType.Shielding4))
            {
                return (FeatType.Shielding4, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.Shielding3))
            {
                return (FeatType.Shielding3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.Shielding2))
            {
                return (FeatType.Shielding2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.Shielding1))
            {
                return (FeatType.Shielding1, self);
            }

            // Stasis Field
            if (CheckIfCanUseFeat(self, self, FeatType.StasisField3))
            {
                return (FeatType.StasisField3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.StasisField2))
            {
                return (FeatType.StasisField2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.StasisField1))
            {
                return (FeatType.StasisField1, self);
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

            // Force Burst
            if (CheckIfCanUseFeat(self, target, FeatType.ForceBurst4))
            {
                return (FeatType.ForceBurst4, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForceBurst3))
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

            // Throw Rock
            if (CheckIfCanUseFeat(self, target, FeatType.ThrowRock5))
            {
                return (FeatType.ThrowRock5, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ThrowRock4))
            {
                return (FeatType.ThrowRock4, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ThrowRock3))
            {
                return (FeatType.ThrowRock3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ThrowRock2))
            {
                return (FeatType.ThrowRock2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ThrowRock1))
            {
                return (FeatType.ThrowRock1, target);
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

            // Force Push
            if (CheckIfCanUseFeat(self, target, FeatType.ForcePush4))
            {
                return (FeatType.ForcePush4, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForcePush3))
            {
                return (FeatType.ForcePush3, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForcePush2))
            {
                return (FeatType.ForcePush2, target);
            }
            if (CheckIfCanUseFeat(self, target, FeatType.ForcePush1))
            {
                return (FeatType.ForcePush1, target);
            }

            // Force Inspiration
            if (CheckIfCanUseFeat(self, self, FeatType.ForceInspiration3))
            {
                return (FeatType.ForceInspiration3, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.ForceInspiration2))
            {
                return (FeatType.ForceInspiration2, self);
            }
            if (CheckIfCanUseFeat(self, self, FeatType.ForceInspiration1))
            {
                return (FeatType.ForceInspiration1, self);
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

            if (usableFeats.Count == 0)
            {
                return (FeatType.Invalid, OBJECT_INVALID);
            }

            // randomize pick from remaining featTypes list
            var randomFeat = Service.Random.Next(usableFeats.Count);
            if (Ability.GetAbilityDetail(usableFeats[randomFeat]).IsHostileAbility)
            {
                return (usableFeats[randomFeat], target);
            }
            else
            {
                return (usableFeats[randomFeat], self);
            }
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

        private const string CURRENT_WP_VARNAME = "WP_CUR";
        private const string TOTAL_WP_VARNAME = "WP_NUM";
        private const string WALK_WAYPOINT_FLAG_VARNAME = "WALKWP_FLAGS";
        private const string WAYPOINT_PREFIX = "WP_";

        public static void WalkWayPoints(bool run = false)
        {
            var self = OBJECT_SELF;
            var walkWpFlags = GetWalkWpFlag(self);
            
            var nearestEnemy = GetNearestCreature(CreatureType.Reputation, (int)ReputationType.Enemy);
            if (GetIsObjectValid(nearestEnemy) && GetObjectSeen(nearestEnemy)) return;

            if (GetCurrentAction(self) == ActionType.Wait)
                return;

            // Initialize if necessary
            if (!walkWpFlags.HasFlag(WalkWpFlag.Initialized))
            {
                InitializeWalkWayPoints();                
            }

            // Move to the next waypoint
            var waypoint = GetNextWalkWayPoint(self);
            if (GetIsObjectValid(waypoint))
            {
                ClearAllActions();
                DelayCommand(Random(10), () => { ActionMoveToObject(waypoint, run, 1.0f); });
                
            }
        }

        private static void InitializeWalkWayPoints()
        {
            var self = OBJECT_SELF;                        
            var sTag = WAYPOINT_PREFIX + GetTag(self) + "_";
            var waypoint = GetWaypointByTag(sTag + 1);

            SetLocalInt(self, CURRENT_WP_VARNAME, -1);

            if (!GetIsObjectValid(waypoint)) SetLocalInt(self, TOTAL_WP_VARNAME, -1);
            else
            {
                var nNth = 1;
                while (GetIsObjectValid(waypoint))
                {
                    SetLocalObject(self, WAYPOINT_PREFIX + IntToString(nNth), waypoint);
                    nNth++;

                    waypoint = GetWaypointByTag(sTag + nNth);
                }
                nNth--;
                SetLocalInt(self, TOTAL_WP_VARNAME, nNth);
            }

            SetWalkWpFlag(self, WalkWpFlag.Initialized);
        }

        private static uint GetNextWalkWayPoint(uint self)
        {
            var totalWaypoints = GetLocalInt(self, WAYPOINT_PREFIX + "NUM");
            if (totalWaypoints == 1)
            {
                return GetLocalObject(self, WAYPOINT_PREFIX + "1");
            }

            var currentWaypoint = GetLocalInt(self, CURRENT_WP_VARNAME);
            var walkWpFlags = GetWalkWpFlag(self);

            if (currentWaypoint < 1)
            {
                currentWaypoint = 1;
            }
            else
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

            // Screech
            if (CheckIfCanUseFeat(self, target, FeatType.Screech))
            {
                return (FeatType.Screech, self);
            }

            // Greater Earthquake
            if (CheckIfCanUseFeat(self, self, FeatType.GreaterEarthquake))
            {
                return (FeatType.GreaterEarthquake, target);
            }

            // Earthquake
            if (CheckIfCanUseFeat(self, self, FeatType.Earthquake))
            {
                return (FeatType.Earthquake, target);
            }

            // Flame Blast
            if (CheckIfCanUseFeat(self, target, FeatType.FlameBlast))
            {
                return (FeatType.FlameBlast, target);
            }

            // Fire Breath
            if (CheckIfCanUseFeat(self, target, FeatType.FireBreath))
            {
                return (FeatType.FireBreath, target);
            }

            SetLocalInt(self, CURRENT_WP_VARNAME, currentWaypoint);
            if (currentWaypoint == -1)
                return OBJECT_INVALID;

            return GetLocalObject(self, WAYPOINT_PREFIX + currentWaypoint);
        }

        public static void SetWalkWpFlag(uint creature, WalkWpFlag flags)
        {
            var flagValue = (int)flags;
            SetLocalInt(creature, WALK_WAYPOINT_FLAG_VARNAME, flagValue);
        }

        public static WalkWpFlag GetWalkWpFlag(uint creature)
        {
            var flagValue = GetLocalInt(creature, WALK_WAYPOINT_FLAG_VARNAME);
            return (WalkWpFlag)flagValue;
        }

    }
}
