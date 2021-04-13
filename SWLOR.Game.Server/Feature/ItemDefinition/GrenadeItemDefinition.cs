using System;
using System.Collections.Generic;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.ItemService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class GrenadeItemDefinition: IItemListDefinition
    {
        private delegate void LocationAction(Location targetLocation);
        private delegate void GrenadeAction(uint user, uint target);

        public Dictionary<string, ItemDetail> BuildItems()
        {
            var builder = new ItemBuilder();
            FragGrenade(builder);
            ConcussionGrenade(builder);
            FlashbangGrenade(builder);
            IonGrenade(builder);
            BactaGrenade(builder);
            AdhesiveGrenade(builder);
            SmokeGrenade(builder);
            BactaBombGrenade(builder);
            IncendiaryGrenade(builder);
            GasGrenade(builder);

            return builder.Build();
        }

        private Location DetermineLocation(uint user, Location targetLocation)
        {
            var userLocation = GetLocation(user);
            var originalTargetLocation = targetLocation;

            if (GetIsPC(user))
            {
                var playerId = GetObjectUUID(user);
                var dbPlayer = DB.Get<Player>(playerId);
                var offMarkDC = 50 - dbPlayer.Skills[SkillType.Ranged].Rank;

                if (Random.D100(1) < offMarkDC)
                {
                    // Critical failure - adjust the location to a nearby random location.
                    if (Random.D20(1) == 1)
                    {
                        SendMessageToPC(user, "You threw... poorly.");
                        targetLocation = BiowareVector.MoveLocation(userLocation,
                            Random.D100(1) + Random.D100(1) + Random.D100(1) + 60,
                            Random.D4(2) * 1.0f,
                            Random.D100(1) + Random.D100(1) + Random.D100(1));

                        var count = 0;
                        while ((GetSurfaceMaterial(targetLocation) == 0 ||
                                LineOfSightVector(GetPositionFromLocation(targetLocation),
                                    GetPosition(user)) == false) &&
                               count < 10)
                        {
                            count++;
                            targetLocation = BiowareVector.MoveLocation(userLocation,
                                Random.D100(1) + Random.D100(1) + Random.D100(1) + 60,
                                Random.D4(2) * 1.0f,
                                Random.D100(1) + Random.D100(1) + Random.D100(1));
                        }
                    }
                    // Off-the-mark throw.
                    else
                    {
                        SendMessageToPC(user, "Your throw was a bit off the mark.");
                        targetLocation = BiowareVector.MoveLocation(targetLocation,
                            Random.D100(1) + Random.D100(1) + Random.D100(1) + 60,
                            Random.D4(2) * 1.0f,
                            Random.D100(1) + Random.D100(1) + Random.D100(1));

                        var count = 0;
                        while ((GetSurfaceMaterial(targetLocation) == 0 ||
                                LineOfSightVector(GetPositionFromLocation(targetLocation), GetPosition(user)) == false) &&
                               count < 10)
                        {
                            count += 1;
                            targetLocation = BiowareVector.MoveLocation(targetLocation,
                                Random.D100(1) + Random.D100(1) + Random.D100(1) + 60,
                                Random.D4(2) * 1.0f,
                                Random.D100(1) + Random.D100(1) + Random.D100(1));
                        }
                    }

                    // Picked an invalid location. Revert to the original location.
                    if (GetSurfaceMaterial(targetLocation) == 0 ||
                        LineOfSightVector(GetPositionFromLocation(targetLocation), GetPosition(user)) == false)
                    {
                        targetLocation = originalTargetLocation;
                    }
                }
            }

            return targetLocation;
        }

        private void AdjustRecastTimer(uint user)
        {
            if (GetIsPC(user) && !GetIsDM(user))
            {
                var playerId = GetObjectUUID(user);
                var dbPlayer = DB.Get<Player>(playerId);

                // Update player's recast timer for grenades.
                var unlockTime = DateTime.UtcNow.AddSeconds(6);
                dbPlayer.RecastTimes[RecastGroup.Grenades] = unlockTime;

                DB.Set(playerId, dbPlayer);
            }
        }

        private void CreateGrenade(
            ItemBuilder builder, 
            string itemTag, 
            Spell spell, 
            string soundName, 
            Effect vfxEffect,
            LocationAction locationAction,
            GrenadeAction action)
        {
            builder.Create(itemTag)
                .TargetsLocation()
                .UserFacesTarget()
                .ReducesItemCharge()
                .MaxDistance((user, item, target, location) =>
                {
                    return 10f + 2f * GetAbilityModifier(AbilityType.Strength, user);
                })
                .ValidationAction((user, item, target, location) =>
                {
                    var (isOnRecastDelay, timeToWait) = Recast.IsOnRecastDelay(user, RecastGroup.Grenades);
                    if (isOnRecastDelay)
                        return $"You can throw another grenade in {timeToWait}.";

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location) =>
                {
                    var userLocation = GetLocation(user);

                    location = DetermineLocation(user, location);
                    AdjustRecastTimer(user);

                    // If a spell isn't defined, randomly pick a projectile appearance for flavor.
                    if (spell == Spell.Invalid)
                    {
                        spell = (Spell)(Random.D6(1) + 973);
                    }

                    // Determine the delay based on distance between thrower and target.
                    var delay = GetDistanceBetweenLocations(userLocation, location) / 18.0f + 0.75f + 0.4f;

                    AssignCommand(user, () => ClearAllActions());
                    AssignCommand(user, () =>
                    {
                        ActionPlayAnimation(Animation.LoopingCustom12);
                        ActionCastSpellAtLocation(spell, location, MetaMagic.Any, true, ProjectilePathType.Ballistic, true);
                    });

                    // Play a sound if defined.
                    if (!string.IsNullOrWhiteSpace(soundName))
                    {
                        DelayCommand(delay, () =>
                        {
                            AssignCommand(user, () => PlaySound(soundName));
                        });
                    }

                    // Play a VFX at the location if defined
                    if (vfxEffect != null)
                    {
                        ApplyEffectAtLocation(DurationType.Instant, vfxEffect, location);
                    }

                    // Run the one-time location action if specified.
                    locationAction?.Invoke(location);

                    DelayCommand(delay, () =>
                    {
                        AssignCommand(user, () =>
                        {
                            for (var targetCreature =
                                    GetFirstObjectInShape(Shape.Sphere, RadiusSize.Large, location, true);
                                GetIsObjectValid(targetCreature);
                                targetCreature = GetNextObjectInShape(Shape.Sphere, RadiusSize.Large, location, true))
                            {
                                action?.Invoke(user, targetCreature);

                                if (!GetIsPC(targetCreature) && !GetIsDM(targetCreature))
                                {
                                    CombatPoint.AddCombatPoint(user, targetCreature, SkillType.Ranged);
                                }

                            }
                        });
                    });
                });
        }

        private void FragGrenade(ItemBuilder builder)
        {
            CreateGrenade(builder,
                "grenade_frag",
                Spell.Grenade10,
                "explosion2",
                EffectVisualEffect(VisualEffect.Fnf_Fireball),
                null,
                (user, target) =>
                {
                    var impactEffect = EffectDamage(Random.D6(1), DamageType.Fire);
                    impactEffect = EffectLinkEffects(EffectDamage(Random.D6(1), DamageType.Piercing), impactEffect);

                    if (Random.D6(1) > 4)
                    {
                        StatusEffect.Apply(user, target, StatusEffectType.Bleed, 6.0f);
                    }

                    if (Random.D6(1) > 4)
                    {
                        StatusEffect.Apply(user, target, StatusEffectType.Burn, 6.0f);
                    }

                    ApplyEffectToObject(DurationType.Instant, impactEffect, target);
                });
        }

        private void ConcussionGrenade(ItemBuilder builder)
        {
            CreateGrenade(builder,
                "grenade_concussion",
                Spell.Grenade10,
                "explosion1",
                EffectLinkEffects(EffectVisualEffect(VisualEffect.Vfx_Fnf_Sound_Burst_Silent), 
                    EffectVisualEffect(VisualEffect.Vfx_Fnf_Screen_Shake)),
                null,
                (user, target) =>
                {
                    var impactEffect = EffectDamage(Random.D12(1), DamageType.Sonic);
                    var durationEffect = EffectDeaf();

                    if (Random.D6(1) > 4)
                    {
                        durationEffect = EffectLinkEffects(EffectKnockdown(), durationEffect);
                    }

                    ApplyEffectToObject(DurationType.Instant, impactEffect, target);
                    ApplyEffectToObject(DurationType.Temporary, durationEffect, target, 6.0f);
                });
        }

        private void FlashbangGrenade(ItemBuilder builder)
        {
            CreateGrenade(builder,
                "grenade_flashbang",
                Spell.Grenade10,
                "explosion1",
                EffectVisualEffect(VisualEffect.Vfx_Fnf_Mystical_Explosion),
                null,
                (user, target) =>
                {
                    var durationEffect = EffectDeaf();

                    if (Random.D6(1) > 4)
                    {
                        FloatingTextStringOnCreature("Your vision blurs and blacks out.", target);
                        durationEffect = EffectLinkEffects(EffectBlindness(), durationEffect);
                    }

                    ApplyEffectToObject(DurationType.Temporary, durationEffect, target, 6.0f);
                });
        }

        private void IonGrenade(ItemBuilder builder)
        {
            CreateGrenade(builder,
                "grenade_ion",
                Spell.Grenade10,
                "explosion1",
                EffectVisualEffect(VisualEffect.Vfx_Fnf_Electric_Explosion),
                null,
                (user, target) =>
                {
                    var damage = EffectDamage(Random.D6(1), DamageType.Electrical);
                    var race = GetRacialType(target);
                    if (race == RacialType.Robot ||
                        race == RacialType.Cyborg && Random.D6(1) > 4)
                    {
                        FloatingTextStringOnCreature("Your circuits are overloaded.", target);
                        ApplyEffectToObject(DurationType.Temporary, EffectStunned(), target, 6.0f);
                    }

                    ApplyEffectToObject(DurationType.Instant, damage, target);
                });
        }

        private void BactaGrenade(ItemBuilder builder)
        {
            CreateGrenade(builder,
                "grenade_bacta",
                Spell.Grenade10,
                string.Empty,
                EffectVisualEffect(VisualEffect.Vfx_Fnf_Gas_Explosion_Nature),
                null,
                (user, target) =>
                {
                    ApplyEffectToObject(DurationType.Temporary, EffectRegenerate(1, 6.0f), target, 18.0f);
                });
        }

        private void AdhesiveGrenade(ItemBuilder builder)
        {
            CreateGrenade(builder,
                "grenade_adhesive",
                Spell.Grenade10,
                string.Empty,
                EffectVisualEffect(VisualEffect.Fnf_Dispel_Greater),
                null,
                (user, target) =>
                {
                    var effect = EffectSlow();

                    if (Random.D6(1) > 4)
                    {
                        FloatingTextStringOnCreature("You are slowed by the adhesive explosion.", target);
                        effect = EffectLinkEffects(EffectCutsceneImmobilize(), effect);
                    }

                    ApplyEffectToObject(DurationType.Temporary, effect, target, 6.0f);
                });

        }

        private void SmokeGrenade(ItemBuilder builder)
        {
            CreateGrenade(builder,
                "grenade_smoke",
                Spell.Grenade10,
                string.Empty,
                null, 
                location =>
                {
                    var effect = EffectAreaOfEffect(AreaOfEffect.FogOfBewilderment, "grenade_smoke_en", "grenade_smoke_hb");
                    ApplyEffectAtLocation(DurationType.Temporary, effect, location, 6.0f);
                },
                (user, target) =>
                {

                });
        }

        private void BactaBombGrenade(ItemBuilder builder)
        {
            CreateGrenade(builder,
                "grenade_bactabomb",
                Spell.Grenade10,
                string.Empty,
                null,
                location =>
                {
                    var effect = EffectAreaOfEffect(AreaOfEffect.FogOfBewilderment, "grenade_bbomb_en", "grenade_bbomb_hb");
                    ApplyEffectAtLocation(DurationType.Temporary, effect, location, 6.0f);
                },
                (user, target) =>
                {

                });
        }

        private void IncendiaryGrenade(ItemBuilder builder)
        {
            CreateGrenade(builder,
                "grenade_incendiary",
                Spell.Grenade10,
                string.Empty,
                null,
                location =>
                {
                    var effect = EffectAreaOfEffect(AreaOfEffect.FogOfBewilderment, "grenade_incen_en", "grenade_incen_hb");
                    ApplyEffectAtLocation(DurationType.Temporary, effect, location, 6.0f);
                },
                (user, target) =>
                {

                });
        }

        private void GasGrenade(ItemBuilder builder)
        {
            CreateGrenade(builder,
                "grenade_gas",
                Spell.Grenade10,
                string.Empty,
                null,
                location =>
                {
                    var effect = EffectAreaOfEffect(AreaOfEffect.FogOfBewilderment, "grenade_gas_en", "grenade_gas_hb");
                    ApplyEffectAtLocation(DurationType.Temporary, effect, location, 6.0f);
                },
                (user, target) =>
                {

                });
        }

        private static void GrenadeAOE(uint target, string grenadeType)
        {
            if (GetObjectType(target) != ObjectType.Creature) return;
            var user = GetAreaOfEffectCreator(OBJECT_SELF);
            var duration = 1;
            Effect impactEffect = null;
            Effect durationEffect = null;

            switch (grenadeType)
            {
                case "SMOKE":
                    durationEffect = EffectInvisibility(InvisibilityType.Normal);
                    break;
                case "BACTABOMB":
                    durationEffect = EffectRegenerate(2, 6.0f);
                    break;
                case "INCENDIARY":
                    impactEffect = EffectDamage(Random.D6(1), DamageType.Fire);
                    duration = Random.D6(1);
                    if (Random.D6(1) > 4)
                    {
                        StatusEffect.Apply(user, target, StatusEffectType.Burn, duration * 6);
                    }
                    break;
                case "GAS":
                    impactEffect = EffectDamage(Random.D6(1), DamageType.Acid);
                    duration = Random.D6(1);
                    if (Random.D6(1) > 4 && !GetIsImmune(target, ImmunityType.Poison))
                    {
                        durationEffect = EffectPoison(Poison.Arsenic);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(grenadeType));
            }

            if (GetIsObjectValid(target))
            {
                if (impactEffect != null) ApplyEffectToObject(DurationType.Instant, impactEffect, target);
                if (durationEffect != null) ApplyEffectToObject(DurationType.Temporary, durationEffect, target, duration * 6.0f);
                if (!GetIsPC(target) && !GetIsDM(target))
                {
                    CombatPoint.AddCombatPoint(user, target, SkillType.Ranged);
                }
            }
        }

        [NWNEventHandler("grenade_bbomb_en")]
        public static void BactaBomb()
        {
            GrenadeAOE(GetEnteringObject(), "BACTABOMB");
        }

        [NWNEventHandler("grenade_bbomb_hb")]
        public static void BactaBombHeartbeat()
        {
            for (var target = GetFirstInPersistentObject(OBJECT_SELF); GetIsObjectValid(target); target = GetNextInPersistentObject(OBJECT_SELF))
            {
                GrenadeAOE(target, "BACTABOMB");
            }
        }

        [NWNEventHandler("grenade_gas_en")]
        public static void GasBomb()
        {
            GrenadeAOE(GetEnteringObject(), "GAS");
        }

        [NWNEventHandler("grenade_gas_hb")]
        public static void GasBombHeartbeat()
        {
            for (var target = GetFirstInPersistentObject(OBJECT_SELF); GetIsObjectValid(target); target = GetNextInPersistentObject(OBJECT_SELF))
            {
                GrenadeAOE(target, "GAS");
            }
        }

        [NWNEventHandler("grenade_incen_en")]
        public static void IncendiaryGrenade()
        {
            GrenadeAOE(GetEnteringObject(), "INCENDIARY");
        }

        [NWNEventHandler("grenade_incen_hb")]
        public static void IncendiaryGrenadeHeartbeat()
        {
            for (var target = GetFirstInPersistentObject(OBJECT_SELF); GetIsObjectValid(target); target = GetNextInPersistentObject(OBJECT_SELF))
            {
                GrenadeAOE(target, "INCENDIARY");
            }
        }

        [NWNEventHandler("grenade_smoke_en")]
        public static void SmokeBomb()
        {
            GrenadeAOE(GetEnteringObject(), "SMOKE");
        }

        [NWNEventHandler("grenade_smoke_hb")]
        public static void SmokeBombHeartbeat()
        {
            for (var target = GetFirstInPersistentObject(OBJECT_SELF); GetIsObjectValid(target); target = GetNextInPersistentObject(OBJECT_SELF))
            {
                GrenadeAOE(target, "SMOKE");
            }
        }

    }
}
