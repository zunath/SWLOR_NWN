using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    /// <summary>
    /// The activated behavior shared by the legacy and revamp ship modules: ammo, weapon effects, mining and support.
    /// </summary>
    public static class ShipModuleActions
    {
        public const string MissileResref = "ship_missile";
        public const string ProtonBombResref = "proton_bomb";
        public const string ConcussionMissileResref = "acm_ammo";
        public const string FuelCapsuleResref = "ship_fuelcapsule";

        private const string Mined = "BEING_MINED";

        // Ammo

        /// <summary>An error message when a player has none of the ammo item. NPC ships need no ammo.</summary>
        public static string AmmoError(uint activator, string resref, string message)
        {
            if (!GetIsPC(activator)) return string.Empty;

            var item = GetItemPossessedBy(activator, resref);
            var stackSize = GetItemStackSize(item);
            return stackSize <= 0 ? message : string.Empty;
        }

        /// <summary>Uses up one ammo item. NPC ships have none to use.</summary>
        public static void ConsumeAmmo(uint activator, string resref)
        {
            if (!GetIsPC(activator)) return;

            var item = GetItemPossessedBy(activator, resref);
            var stackSize = GetItemStackSize(item);
            if (stackSize <= 1)
            {
                DestroyObject(item);
            }
            else
            {
                SetItemStackSize(item, stackSize - 1);
            }
        }

        // Weapons

        /// <summary>The Ion Cannon's slow on hull damage, in percent, and its Agility penalty.</summary>
        private const int IonSlowPercent = 50;
        private const int IonAgilityPenalty = 2;

        /// <summary>The Storm Cannon's slow on hull damage, in percent, and its Agility penalty.</summary>
        private const int StormSlowPercent = 75;
        private const int StormAgilityPenalty = 4;

        /// <summary>A Laser Battery's volley reaches this far from the target and hits this many other ships.</summary>
        private const float BatteryVolleyRadius = 15f;
        private const int BatteryVolleyExtraTargets = 4;

        private const float BeamSeconds = 1.0f;

        /// <summary>The module families that fire a weapon shot at a ship.</summary>
        public static bool IsShipWeapon(ShipModuleFamily family)
        {
            return family is ShipModuleFamily.LaserCannon or ShipModuleFamily.PointDefenseTurret or
                ShipModuleFamily.Turbolaser or ShipModuleFamily.QuadLaser or ShipModuleFamily.IonCannon or
                ShipModuleFamily.StormCannon or ShipModuleFamily.BeamCannon or ShipModuleFamily.LaserBattery or
                ShipModuleFamily.MissileLauncher or ShipModuleFamily.TorpedoLauncher or
                ShipModuleFamily.AssaultConcussionMissile;
        }

        /// <summary>The ammo item a launcher family spends per shot, or null for weapons that need none.</summary>
        public static string GetAmmoResref(ShipModuleFamily family)
        {
            return family switch
            {
                ShipModuleFamily.MissileLauncher => MissileResref,
                ShipModuleFamily.TorpedoLauncher => ProtonBombResref,
                ShipModuleFamily.AssaultConcussionMissile => ConcussionMissileResref,
                _ => null,
            };
        }

        /// <summary>
        /// Fires one shot of a weapon module family with that family's effects and returns its roll (null if the
        /// ship cannot fire). Launcher ammo is the caller's to spend. With <paramref name="singleImpact"/> a Laser
        /// Battery fires its one bolt instead of its volley, for abilities that must not scale with weapon count.
        /// </summary>
        public static ShipShotResult FireStep(
            ShipModuleStep step,
            uint activator,
            ShipStatus status,
            uint target,
            ShipStatus targetStatus,
            SpaceWeaponShot shot,
            bool singleImpact)
        {
            switch (step.Family)
            {
                case ShipModuleFamily.LaserCannon:
                case ShipModuleFamily.PointDefenseTurret:
                case ShipModuleFamily.Turbolaser:
                case ShipModuleFamily.QuadLaser:
                    return FireBolt(activator, status, target, targetStatus, shot, step.Family == ShipModuleFamily.Turbolaser);
                case ShipModuleFamily.IonCannon:
                    return FireIon(activator, status, target, targetStatus, shot, IonSlowPercent, IonAgilityPenalty);
                case ShipModuleFamily.StormCannon:
                    return FireStormPulse(activator, status, target, targetStatus, shot, StormSlowPercent, StormAgilityPenalty);
                case ShipModuleFamily.BeamCannon:
                    return FireBeamPulse(activator, status, target, targetStatus, shot, BeamSeconds);
                case ShipModuleFamily.LaserBattery:
                    return singleImpact
                        ? FireBolt(activator, status, target, targetStatus, shot)
                        : FireVolley(activator, status, target, targetStatus, shot, BatteryVolleyRadius, BatteryVolleyExtraTargets);
                case ShipModuleFamily.MissileLauncher:
                    return FireMissile(activator, status, target, targetStatus, shot, false);
                case ShipModuleFamily.TorpedoLauncher:
                case ShipModuleFamily.AssaultConcussionMissile:
                    return FireMissile(activator, status, target, targetStatus, shot, true);
                default:
                    return null;
            }
        }

        /// <summary>Fires one bolt shot: the sound and bolt effects and a damage hit 0.3 seconds later.</summary>
        public static ShipShotResult FireBolt(
            uint activator,
            ShipStatus activatorStatus,
            uint target,
            ShipStatus targetStatus,
            SpaceWeaponShot shot,
            bool explosionOnHit = false)
        {
            var result = Space.RollWeaponShot(activator, activatorStatus, target, targetStatus, shot);

            var sound = EffectVisualEffect(VisualEffect.Vfx_Ship_Blast);
            var missile = EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2);
            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, sound, target);
                ApplyEffectToObject(DurationType.Instant, missile, target);
                if (explosionOnHit && result.IsHit)
                {
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Gas_Explosion_Fire), target);
                }
            });

            Space.CompleteWeaponShot(activator, target, shot, result, 0.3f);
            return result;
        }

        /// <summary>Fires one ion shot. Damage that gets past the shield slows the target and lowers its Agility.</summary>
        public static ShipShotResult FireIon(
            uint activator,
            ShipStatus activatorStatus,
            uint target,
            ShipStatus targetStatus,
            SpaceWeaponShot shot,
            int slowPercent,
            int agilityPenalty)
        {
            var result = Space.RollWeaponShot(activator, activatorStatus, target, targetStatus, shot);

            if (result.IsHit)
            {
                AssignCommand(activator, () =>
                {
                    var effect = EffectBeam(VisualEffect.Vfx_Beam_Mind, activator, BodyNode.Chest);
                    ApplyEffectToObject(DurationType.Temporary, effect, target, 1.0f);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ion_Shot), activator);
                });

                DelayCommand(0.1f, () =>
                {
                    AssignCommand(activator, () =>
                    {
                        var effect = EffectVisualEffect(VisualEffect.Vfx_Imp_Dispel, false, 0.5f);
                        ApplyEffectToObject(DurationType.Instant, effect, target);
                    });
                });
            }
            else
            {
                AssignCommand(activator, () =>
                {
                    var effect = EffectBeam(VisualEffect.Vfx_Beam_Cold, activator, BodyNode.Chest, true);
                    ApplyEffectToObject(DurationType.Temporary, effect, target, 1.0f);
                });

                DelayCommand(0.1f, () =>
                {
                    AssignCommand(activator, () =>
                    {
                        var effect = EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion, true);
                        ApplyEffectToObject(DurationType.Instant, effect, target);
                    });
                });
            }

            Space.CompleteWeaponShot(activator, target, shot, result, 0.1f, landed =>
            {
                if (landed.HullDamage > 0)
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectMovementSpeedDecrease(slowPercent), target, 6f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Agility, agilityPenalty), target, 12f);
                }
            });
            return result;
        }

        /// <summary>Fires one Storm Cannon pulse, with the same slow on hull damage as an ion hit.</summary>
        public static ShipShotResult FireStormPulse(
            uint activator,
            ShipStatus activatorStatus,
            uint target,
            ShipStatus targetStatus,
            SpaceWeaponShot shot,
            int slowPercent,
            int agilityPenalty)
        {
            var effectBeam = EffectBeam(VisualEffect.Vfx_Beam_Mind, activator, BodyNode.Chest);
            var effectLightning = EffectBeam(VisualEffect.Vfx_Beam_Silent_Lightning, activator, BodyNode.Chest);

            var result = Space.RollWeaponShot(activator, activatorStatus, target, targetStatus, shot);

            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Temporary, effectBeam, target, 1.0f);
                ApplyEffectToObject(DurationType.Temporary, effectLightning, target, 1.0f);
                if (result.IsHit)
                {
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ion_Shot), activator);
                }
            });

            if (result.IsHit)
            {
                DelayCommand(0.1f, () =>
                {
                    AssignCommand(activator, () =>
                    {
                        var effect = EffectVisualEffect(VisualEffect.Vfx_Imp_Dispel, false);
                        ApplyEffectToObject(DurationType.Instant, effect, target);
                    });
                });
            }

            // Damage that gets past the shield slows the target.
            Space.CompleteWeaponShot(activator, target, shot, result, 0.1f, landed =>
            {
                if (landed.HullDamage > 0)
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectMovementSpeedDecrease(slowPercent), target, 6f);
                    ApplyEffectToObject(DurationType.Temporary, EffectAbilityDecrease(AbilityType.Agility, agilityPenalty), target, 12f);
                }
            });
            return result;
        }

        /// <summary>Fires one beam tick: a short beam on the target and an immediate shot.</summary>
        public static ShipShotResult FireBeamPulse(
            uint activator,
            ShipStatus activatorStatus,
            uint target,
            ShipStatus targetStatus,
            SpaceWeaponShot shot,
            float beamSeconds)
        {
            if (!Space.CanShipFire(activator))
                return null;

            var beam = EffectBeam(VisualEffect.Vfx_Beam_Holy, activator, BodyNode.Chest);
            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Temporary, beam, target, beamSeconds);
            });

            return Space.ResolveWeaponShot(activator, activatorStatus, target, targetStatus, shot);
        }

        /// <summary>
        /// Launches a missile that explodes on the target after its travel time. A heavy missile (a torpedo or
        /// concussion missile) shakes the screen and shows a bigger blast.
        /// </summary>
        public static ShipShotResult FireMissile(
            uint activator,
            ShipStatus activatorStatus,
            uint target,
            ShipStatus targetStatus,
            SpaceWeaponShot shot,
            bool heavy)
        {
            var targetDistance = GetDistanceBetween(activator, target);
            var delay = (float)(targetDistance / (3.0 * log(targetDistance) + 2.0));

            var result = Space.RollWeaponShot(activator, activatorStatus, target, targetStatus, shot);
            var isHit = result.IsHit;

            // Shoot some missiles out to the target.
            AssignCommand(activator, () =>
            {
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Ship_Trp), activator);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Mirv_Torpedo, !isHit), target);
            });

            // Display an explosion at the target location in a few seconds (based on travel distance of the initial missile graphic)
            // Then apply damage on target and those nearby.
            DelayCommand(delay, () =>
            {
                if (heavy)
                {
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Shake, !isHit), target);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Summondragon, !isHit), target);
                }
                else
                {
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball, !isHit, 0.5f), target);
                }

                Space.CompleteWeaponShot(activator, target, shot, result);
            });

            return result;
        }

        /// <summary>
        /// Fires a volley: a bolt at the target and at up to <paramref name="extraTargets"/> other enemy ships
        /// within <paramref name="radius"/> meters of it.
        /// </summary>
        public static ShipShotResult FireVolley(
            uint activator,
            ShipStatus activatorStatus,
            uint target,
            ShipStatus targetStatus,
            SpaceWeaponShot shot,
            float radius,
            int extraTargets)
        {
            var primary = FireBolt(activator, activatorStatus, target, targetStatus, shot);

            var count = 0;
            foreach (var nearby in ObjectSnapshot.InShape(Shape.Sphere, radius, GetLocation(target), true, ObjectType.Creature))
            {
                if (count >= extraTargets)
                    break;

                if (!GetIsObjectValid(nearby) || nearby == target || nearby == activator || !GetIsEnemy(nearby, activator))
                    continue;

                var nearbyStatus = Space.GetShipStatus(nearby);
                if (nearbyStatus == null)
                    continue;

                FireBolt(activator, activatorStatus, nearby, nearbyStatus, shot);
                count++;
            }

            return primary;
        }

        // Mining

        /// <summary>An error message when the target is not an asteroid this mining laser can mine.</summary>
        public static string MiningLaserError(uint target, int tier)
        {
            // Ensure an asteroid ore type has been specified by the builder.
            var lootTableId = GetLocalString(target, "ASTEROID_LOOT_TABLE_ID");
            if (string.IsNullOrWhiteSpace(lootTableId))
            {
                return "Only asteroids may be targeted with this module.";
            }

            // Ensure required level of the module matches or exceeds tier of asteroid.
            var tierRequired = GetLocalInt(target, "ASTEROID_TIER");
            if (tier < tierRequired)
            {
                return "This mining laser is not powerful enough to harvest that asteroid.";
            }

            if (GetLocalBool(target, Mined))
            {
                return "This asteroid is already being mined.";
            }

            return string.Empty;
        }

        /// <summary>
        /// An error message when the target is not an asteroid this strip miner can strip.
        /// A null tier skips the tier check.
        /// </summary>
        public static string StripMinerError(uint target, int? tier)
        {
            var lootTableId = GetLocalString(target, "STRIPMINE_LOOT_TABLE_ID");
            if (string.IsNullOrWhiteSpace(lootTableId))
            {
                return "Only asteroids may be targeted with this module.";
            }

            if (tier.HasValue && tier.Value < GetLocalInt(target, "ASTEROID_TIER"))
            {
                return "This strip miner is not powerful enough to harvest that asteroid.";
            }

            if (GetLocalBool(target, Mined))
            {
                return "This asteroid is already being mined.";
            }

            return string.Empty;
        }

        /// <summary>Mines one cycle of an asteroid and delivers the ore when the cycle ends.</summary>
        public static void MineAsteroid(uint activator, uint target, float cycleSeconds, int moduleBonus)
        {
            SetLocalBool(target, Mined, true);
            // Remaining units aren't set - pick a random number to assign.
            var remainingUnits = GetLocalInt(target, "ASTEROID_REMAINING_UNITS");
            if (remainingUnits <= 0)
            {
                remainingUnits = Random.D4(1) + 2;
                SetLocalInt(target, "ASTEROID_REMAINING_UNITS", remainingUnits);
            }

            AssignCommand(activator, () =>
            {
                var beam = EffectBeam(VisualEffect.Vfx_Beam_Holy, activator, BodyNode.Chest);
                ApplyEffectToObject(DurationType.Temporary, beam, target, cycleSeconds + 0.1f);
            });

            // At the end of the process, spawn the ore on the activator and reduce remaining units.
            DelayCommand(cycleSeconds + 0.1f, () =>
            {
                // The pilot may have left their ship before mining completes.
                var shipStatus = Space.GetShipStatus(activator);
                if (shipStatus == null)
                    return;

                // Perk & module bonuses. These increase the overall yield of each asteroid.
                var industrialBonus = shipStatus.Industrial;

                var amountToMine = 1 + Perk.GetPerkLevel(activator, PerkType.StarshipMining) + (int)(industrialBonus / 4) + (int)(moduleBonus) / 6f;

                // Refresh remaining units (could have changed since the start)
                remainingUnits = GetLocalInt(target, "ASTEROID_REMAINING_UNITS");

                // Safety check - if another player pulls all of the ore from the asteroid, give an error message.
                if (!GetIsObjectValid(target) || remainingUnits <= 0)
                {
                    SendMessageToPC(activator, "Your target has been fully mined.");
                    return;
                }

                remainingUnits -= 1 + (int)amountToMine / 2;

                // Fully deplete the rock - destroy it.
                if (remainingUnits <= 0)
                {
                    // DestroyObject bypasses the OnDeath event, and removes the object so we can't send events.
                    // Use EffectDeath to ensure that we trigger death processing.
                    SetPlotFlag(target, false);
                    ApplyEffectToObject(DurationType.Instant, EffectDeath(), target);

                    SendMessageToPC(activator, $"{GetName(target)} has been fully mined.");
                }
                // Update remaining units.
                else
                {
                    SetLocalInt(target, "ASTEROID_REMAINING_UNITS", remainingUnits);
                }

                Loot.SpawnLoot(target, activator, "LOOT_TABLE_");

                var lootTableId = GetLocalString(target, "ASTEROID_LOOT_TABLE_ID");
                var lootTable = Loot.GetLootTableByName(lootTableId);

                // Spawn the units.
                for (var count = 1; count <= amountToMine; count++)
                {
                    var item = lootTable.GetRandomItem();
                    CreateItemOnObject(item.Resref, activator);
                }

                GiveMiningExperience(activator, target);
            });
            SetLocalBool(target, Mined, false);
        }

        /// <summary>Strips a whole asteroid over <paramref name="seconds"/> while the ship is rooted.</summary>
        public static void StripAsteroid(uint activator, uint target, float seconds, int moduleBonus)
        {
            SetLocalBool(target, Mined, true);

            ApplyEffectToObject(DurationType.Temporary, EffectEntangle(), activator, seconds);

            AssignCommand(activator, () =>
            {
                var beam = EffectBeam(VisualEffect.Vfx_Beam_Disintegrate, activator, BodyNode.Chest);
                ApplyEffectToObject(DurationType.Temporary, beam, target, seconds);
            });

            DelayCommand(seconds, () =>
            {
                var industrialBonus = Space.GetShipStatus(activator).Industrial;

                var amountToMine = 1 + Perk.GetPerkLevel(activator, PerkType.StarshipMining) + (int)(industrialBonus / 6) + (moduleBonus / 6);

                SetPlotFlag(target, false);
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), target);

                SendMessageToPC(activator, $"{GetName(target)} has been depleted by your strip miner.");

                Loot.SpawnLoot(target, activator, "LOOT_TABLE_");

                var lootTableId = GetLocalString(target, "STRIPMINE_LOOT_TABLE_ID");
                var lootTable = Loot.GetLootTableByName(lootTableId);

                for (var count = 1; count <= amountToMine; count++)
                {
                    var item = lootTable.GetRandomItem();
                    CreateItemOnObject(item.Resref, activator);
                }

                GiveMiningExperience(activator, target);
            });
            SetLocalBool(target, Mined, false);
        }

        private static void GiveMiningExperience(uint activator, uint target)
        {
            if (GetIsPC(activator) && !GetIsDM(activator) && !GetIsDMPossessed(activator))
            {
                var playerId = GetObjectUUID(activator);
                var dbPlayer = DB.Get<Player>(playerId);
                var rank = dbPlayer.Skills[SkillType.Piloting].Rank;
                var asteroidLevel = GetLocalInt(target, "ASTEROID_TIER") * 10;
                var delta = asteroidLevel - rank;
                var xp = Skill.GetDeltaXP(delta);

                Skill.GiveSkillXP(activator, SkillType.Piloting, xp);
            }
        }

        // Support

        /// <summary>Restores shield to the target ship, or to the activator's own when the target is not a friendly ship.</summary>
        public static void RepairShield(uint activator, ShipStatus activatorStatus, uint target, ShipStatus targetStatus, int amount)
        {
            ApplySupport(activator, activatorStatus, target, targetStatus, amount, VisualEffect.Vfx_Imp_Ac_Bonus,
                "shield HP", Space.RestoreShield, null, status => status.Shield);
        }

        /// <summary>Restores hull to the target ship, or to the activator's own when the target is not a friendly ship.</summary>
        public static void RepairHull(uint activator, ShipStatus activatorStatus, uint target, ShipStatus targetStatus, int amount)
        {
            ApplySupport(activator, activatorStatus, target, targetStatus, amount, VisualEffect.Vfx_Imp_Breach,
                "hull HP", Space.RestoreHull, null, status => status.Hull);
        }

        /// <summary>Spends a fuel capsule to restore capacitor to the target ship, or to the activator's own.</summary>
        public static void InjectCapacitor(uint activator, ShipStatus activatorStatus, uint target, ShipStatus targetStatus, int amount)
        {
            ApplySupport(activator, activatorStatus, target, targetStatus, amount, VisualEffect.Vfx_Imp_Breach,
                "capacitor charge", Space.RestoreCapacitor, () => ConsumeAmmo(activator, FuelCapsuleResref));
        }

        private static void ApplySupport(
            uint activator,
            ShipStatus activatorStatus,
            uint target,
            ShipStatus targetStatus,
            int amount,
            VisualEffect effect,
            string noun,
            System.Action<uint, ShipStatus, int> restore,
            System.Action beforeEffect,
            System.Func<ShipStatus, int> pool = null)
        {
            if (!GetIsObjectValid(target) || GetIsEnemy(target, activator))
            {
                target = activator;
                targetStatus = activatorStatus;
            }

            if (activator != target)
            {
                AssignCommand(activator, () =>
                {
                    var beam = EffectBeam(VisualEffect.Vfx_Beam_Mind, activator, BodyNode.Chest);
                    ApplyEffectToObject(DurationType.Temporary, beam, target, 1.0f);
                });
            }

            beforeEffect?.Invoke();

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(effect), target);

            var before = pool?.Invoke(targetStatus) ?? 0;
            restore(target, targetStatus, amount);
            if (pool != null)
                Space.ApplyRepairEnmity(activator, target, pool(targetStatus) - before);

            Messaging.SendMessageNearbyToPlayers(
                activator,
                receiver => $"{PlayerName.GetDisplayName(receiver, activator)} restores {amount} {noun} to {PlayerName.GetDisplayName(receiver, target)}'s ship.");
            CombatPoint.AddSupportCombatPoint(activator, target, SkillType.Piloting);
        }

        /// <summary>
        /// The ships within <paramref name="radius"/> meters of the activator, other than itself: friendly ships
        /// or, with <paramref name="allies"/> false, enemy ships. At most <paramref name="maximum"/> are returned.
        /// </summary>
        public static List<uint> GetNearbyShips(uint activator, float radius, int maximum, bool allies)
        {
            var ships = new List<uint>();
            if (GetIsDead(activator))
                return ships;

            foreach (var nearby in ObjectSnapshot.InShape(Shape.Sphere, radius, GetLocation(activator), true, ObjectType.Creature))
            {
                if (ships.Count >= maximum)
                    break;

                if (!GetIsObjectValid(nearby) || nearby == activator)
                    continue;

                if (GetIsEnemy(nearby, activator) == allies)
                    continue;

                if (Space.GetShipStatus(nearby) == null)
                    continue;

                ships.Add(nearby);
            }

            return ships;
        }
    }
}
