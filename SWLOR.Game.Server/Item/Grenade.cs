﻿using System;
using NWN;
using System.Globalization;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.NWScript._;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Item
{
    public class Grenade : IActionItem
    {
        public string CustomKey => null;

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            DateTime now = DateTime.UtcNow;
            DateTime unlockDateTime = now;
            if (string.IsNullOrWhiteSpace(GetLocalString(user, "GRENADE_UNLOCKTIME")))
            {
                unlockDateTime = unlockDateTime.AddSeconds(-1);
            }
            else
            {
                unlockDateTime = DateTime.ParseExact(GetLocalString(user, "GRENADE_UNLOCKTIME"), "yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture);
            }
            Console.WriteLine("IsValidTarget - Current Time = " + now.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture));
            Console.WriteLine("IsValidTarget - Unlocktime = " + unlockDateTime.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture));
            Console.WriteLine("IsValidTarget - DateTime.Compare = " + DateTime.Compare(unlockDateTime, now));

            // Check if we've passed the unlock date. Exit early if we have not.
            if (DateTime.Compare(unlockDateTime, now) > 0 || unlockDateTime > now)
            {
                string timeToWait = TimeService.GetTimeToWaitLongIntervals(now, unlockDateTime, false);
                Console.WriteLine("IsValidTarget - That ability can be used in " + timeToWait + ".");
                SendMessageToPC(user, "That ability can be used in " + timeToWait + ".");
                return;
            }

            Effect impactEffect = null;
            Spell spellId = Spell.Invalid;
            string soundName = null;
            int perkLevel = 1 + PerkService.GetCreaturePerkLevel(user, PerkType.GrenadeProficiency);
            int skillLevel = 5 + SkillService.GetPCSkillRank((NWPlayer)user, Skill.Throwing);
            if (perkLevel == 0) perkLevel += 1;

            if (GetIsObjectValid(target) == true) targetLocation = GetLocation(target);
            string grenadeType = item.GetLocalString("TYPE");
            Console.WriteLine("Throwing " + grenadeType + " grenade at perk level " + perkLevel);
            Location originalLocation = targetLocation;

            int roll = RandomService.D100(1);
            
            SendMessageToPC(user, roll + " vs. DC " + (100 - skillLevel));
            if (roll < (100 - skillLevel))
            {
                if (RandomService.D20(1) == 1)
                {
                    SendMessageToPC(user, "You threw... poorly.");
                    //targetLocation = VectorService.MoveLocation(targetLocation, GetFacing(user), (RandomService.D6(4) - 10) * 1.0f, 
                    targetLocation = VectorService.MoveLocation(user.Location, RandomService.D100(1) + RandomService.D100(1) + RandomService.D100(1) + 60, RandomService.D4(2) * 1.0f,
                                                                RandomService.D100(1) + RandomService.D100(1) + RandomService.D100(1));
                    int count = 0;
                    while ((GetSurfaceMaterial(targetLocation) == 0 ||
                           LineOfSightVector(GetPositionFromLocation(targetLocation), GetPosition(user)) == false) &&
                           count < 10)
                    {
                        count += 1;
                        targetLocation = VectorService.MoveLocation(user.Location, RandomService.D100(1) + RandomService.D100(1) + RandomService.D100(1) + 60, RandomService.D4(2) * 1.0f,
                                                                    RandomService.D100(1) + RandomService.D100(1) + RandomService.D100(1));
                    }
                }
                else
                {
                    SendMessageToPC(user, "Your throw was a bit off the mark.");
                    //targetLocation = VectorService.MoveLocation(targetLocation, GetFacing(user), (RandomService.D6(4) - 10) * 1.0f, 
                    targetLocation = VectorService.MoveLocation(targetLocation, RandomService.D100(1) + RandomService.D100(1) + RandomService.D100(1) + 60, RandomService.D4(2) /*(RandomService.D6(4) - 10) */ * 1.0f,
                                                                RandomService.D100(1) + RandomService.D100(1) + RandomService.D100(1));
                    int count = 0;
                    while ((GetSurfaceMaterial(targetLocation) == 0 ||
                           LineOfSightVector(GetPositionFromLocation(targetLocation), GetPosition(user)) == false) &&
                           count < 10)
                    {
                        count += 1;
                        targetLocation = VectorService.MoveLocation(targetLocation, RandomService.D100(1) + RandomService.D100(1) + RandomService.D100(1) + 60, RandomService.D4(2) /*(RandomService.D6(4) - 10) */ * 1.0f,
                                                                    RandomService.D100(1) + RandomService.D100(1) + RandomService.D100(1));
                    }
                }

                if (GetSurfaceMaterial(targetLocation) == 0 ||
                           LineOfSightVector(GetPositionFromLocation(targetLocation), GetPosition(user)) == false)
                {
                    targetLocation = originalLocation;
                }
            }

            switch (grenadeType)
            {
                case "FRAG":                    
                    impactEffect = EffectVisualEffect(Vfx.Fnf_Fireball);
                    // force a specific spell id (for projectile model) for this grenade.
                    spellId = Spell.Grenade10;
                    soundName = "explosion2";                    
                    break;
                case "CONCUSSION":
                    impactEffect = EffectVisualEffect(Vfx.Vfx_Fnf_Sound_Burst_Silent);
                    impactEffect = EffectLinkEffects(EffectVisualEffect(Vfx.Vfx_Fnf_Screen_Shake), impactEffect);
                    //spellId = 974;
                    soundName = "explosion1";
                    break;
                case "FLASHBANG":
                    impactEffect = EffectVisualEffect(Vfx.Vfx_Fnf_Mystical_Explosion);
                    //spellId = 974;
                    soundName = "explosion1";
                    break;
                case "ION":
                    impactEffect = EffectVisualEffect(Vfx.Vfx_Fnf_Electric_Explosion);
                    //spellId = 974;
                    soundName = "explosion1";
                    break;
                case "BACTA":
                    impactEffect = EffectVisualEffect(Vfx.Vfx_Fnf_Gas_Explosion_Nature);
                    //spellId = 974;
                    //soundName = "explosion1";
                    break;
                case "ADHESIVE":
                    impactEffect = EffectVisualEffect(Vfx.Fnf_Dispel_Greater);
                    //spellId = 974;
                    //soundName = "explosion1";
                    break;
                case "SMOKE":
                    impactEffect = null;
                    //spellId = 974;
                    //soundName = "explosion1";
                    break;
                case "BACTABOMB":
                    impactEffect = null;
                    //spellId = 974;
                    //soundName = "explosion1";
                    break;
                case "INCENDIARY":
                    impactEffect = null;
                    //spellId = 974;
                    //soundName = "explosion1";
                    break;
                case "GAS":
                    impactEffect = null;                    
                    //spellId = 974;
                    //soundName = "explosion1";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(grenadeType));
            }           

            if (spellId == 0)
            {
                // start 974 through 979 in spells.2da for grenades
                // lets randomly assign a projectile appearance for flavor?
                spellId = (Spell)(RandomService.D6(1) + 973);
            }

            float delay = GetDistanceBetweenLocations(user.Location, targetLocation) / 18.0f + 0.75f;
            delay += 0.4f; // added for animation
            user.ClearAllActions();
            //user.AssignCommand(() => _.ActionPlayAnimation(32));
            //user.DelayAssignCommand(() => _.ActionPlayAnimation(32), 0.0f);
            user.AssignCommand(() =>
            {
                ActionPlayAnimation(Animation.Custom12);
                ActionCastSpellAtLocation(spellId, targetLocation, MetaMagic.Any, true, ProjectilePathType.Ballistic, true);
                //ActionCastFakeSpellAtLocation(spellId, targetLocation, PROJECTILE_PATH_TYPE_BALLISTIC);
            });            

            if (soundName != null)
            {
                user.DelayAssignCommand(() =>
                {
                    PlaySound(soundName);
                }, delay);
            }

            if (impactEffect != null)
            {
                user.DelayAssignCommand(() =>
                {
                    ApplyEffectAtLocation(DurationType.Instant, impactEffect, targetLocation);
                }, delay);
            }

            user.DelayAssignCommand(
                         () =>
                         {
                             DoImpact(user, targetLocation, grenadeType, perkLevel, RadiusSize.Large, ObjectType.Creature);
                         }, delay + 0.75f);


            perkLevel = PerkService.GetCreaturePerkLevel(user, PerkType.GrenadeProficiency);

            now = DateTime.UtcNow;
            DateTime unlockTime = now;

            if (perkLevel < 5)
            {
                unlockTime = unlockTime.AddSeconds(6);
            }
            else if (perkLevel < 10)
            {
                unlockTime = unlockTime.AddSeconds(3);                
            }
            else
            {
                unlockTime = unlockTime.AddSeconds(2);
            }

            SetLocalString(user, "GRENADE_UNLOCKTIME", unlockTime.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture));
            Console.WriteLine("StartUseItem - Current Time = " + now.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture));
            Console.WriteLine("StartUseItem - Unlocktime Set To = " + unlockTime.ToString("yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture));

        }

        public void DoImpact(NWCreature user, Location targetLocation, string grenadeType, int perkLevel, float fExplosionRadius, ObjectType nObjectFilter)
        {
            Effect damageEffect = EffectDamage(0, DamageType.Negative);
            Effect durationEffect = null;
            int duration = perkLevel;

            switch (grenadeType)
            {
                case "SMOKE":
                    durationEffect = EffectAreaOfEffect(Aoe.Per_Fog_Of_Bewilderment, "grenade_smoke_en", "grenade_smoke_hb", "");
                    break;
                case "BACTABOMB":
                    durationEffect = EffectAreaOfEffect(Aoe.Per_Fogmind, "grenade_bbomb_en", "grenade_bbomb_hb", "");
                    break;
                case "INCENDIARY":
                    durationEffect = EffectAreaOfEffect(Aoe.Per_Fogfire, "grenade_incen_en", "grenade_incen_hb", "");
                    break;
                case "GAS":
                    durationEffect = EffectAreaOfEffect(Aoe.Per_Fogghoul, "grenade_gas_en", "grenade_gas_hb", "");
                    break;
                default:
                    break;
            }

            if (durationEffect != null)
            {
                //Apply AOE
                ApplyEffectAtLocation(DurationType.Temporary, durationEffect, targetLocation, duration * 6.0f);
            }
            else
            {
                //Apply impact

                // Target the next nearest creature and do the same thing.
                NWObject targetCreature = GetFirstObjectInShape(Shape.Sphere, fExplosionRadius, targetLocation, true, nObjectFilter);
                while (targetCreature.IsValid)
                {
                    Console.WriteLine("Grenade hit on " + targetCreature.Name);

                    switch (grenadeType)
                    {
                        case "FRAG":
                            damageEffect = EffectDamage(RandomService.D6(perkLevel), DamageType.Fire);
                            damageEffect = EffectLinkEffects(EffectDamage(RandomService.D6(perkLevel), DamageType.Piercing), damageEffect);
                            if (RandomService.D6(1) > 4)
                            {
                                Console.WriteLine("grenade effect bleeding - frag");
                                CustomEffectService.ApplyCustomEffect(user, targetCreature.Object, CustomEffectType.Bleeding, duration * 6, perkLevel, Convert.ToString(perkLevel));
                            }
                            if (RandomService.D6(1) > 4)
                            {
                                Console.WriteLine("grenade effects burning - frag");
                                CustomEffectService.ApplyCustomEffect(user, targetCreature.Object, CustomEffectType.Burning, duration * 6, perkLevel, Convert.ToString(perkLevel));
                            }
                            Console.WriteLine("grenade effects set - frag");
                            break;
                        case "CONCUSSION":
                            damageEffect = EffectDamage(RandomService.D12(perkLevel), DamageType.Sonic);
                            durationEffect = EffectDeaf();
                            if (RandomService.D6(1) > 4)
                            {
                                durationEffect = EffectLinkEffects(EffectKnockdown(), durationEffect);
                            }
                            break;
                        case "FLASHBANG":
                            duration = RandomService.D4(1);
                            durationEffect = EffectDeaf();
                            if (RandomService.D6(1) > 4)
                            {
                                durationEffect = EffectLinkEffects(EffectBlindness(), durationEffect);
                            }
                            break;
                        case "ION":
                            duration = RandomService.D4(1);
                            damageEffect = EffectDamage(RandomService.D6(perkLevel), DamageType.Electrical);
                            if (GetRacialType(targetCreature) == RacialType.Robot ||
                                (RandomService.D6(1) > 4 && GetRacialType(targetCreature) == RacialType.Cyborg))
                            {
                                durationEffect = EffectStunned();
                            }
                            break;
                        case "BACTA":
                            damageEffect = null;
                            durationEffect = EffectRegenerate(perkLevel, 6.0f);
                            break;
                        case "ADHESIVE":
                            durationEffect = EffectSlow();
                            if (RandomService.D6(1) > 4)
                            {
                                durationEffect = EffectLinkEffects(EffectCutsceneImmobilize(), durationEffect);
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(grenadeType));
                    }

                    if (damageEffect != null) ApplyEffectToObject(DurationType.Instant, damageEffect, targetCreature);
                    if (durationEffect != null) ApplyEffectToObject(DurationType.Temporary, durationEffect, targetCreature, duration * 6.0f);

                    if (!targetCreature.IsPlayer)
                    {
                        SkillService.RegisterPCToNPCForSkill(user.Object, targetCreature, Skill.Throwing);
                    }                    

                    targetCreature = GetNextObjectInShape(Shape.Sphere, fExplosionRadius, targetLocation, true, nObjectFilter);
                }
            }
        }

        public static void GrenadeAoe(NWObject oTarget, string grenadeType)
        {
            NWCreature user = GetAreaOfEffectCreator(NWGameObject.OBJECT_SELF);
            int perkLevel = PerkService.GetCreaturePerkLevel(user, PerkType.GrenadeProficiency);
            int duration = 1;
            Effect impactEffect = null;
            Effect durationEffect = null;

            Console.WriteLine("In grenadeAoe for grenade type " + grenadeType + " on " + GetName(oTarget));

            switch (grenadeType)
            {
                case "SMOKE":
                    durationEffect = EffectInvisibility(InvisibilityType.Normal);
                    break;
                case "BACTABOMB":
                    durationEffect = EffectRegenerate(perkLevel*2, 6.0f);
                    break;
                case "INCENDIARY":
                    impactEffect = EffectDamage(RandomService.D6(perkLevel), DamageType.Fire);
                    duration = RandomService.D6(1);
                    if (RandomService.D6(1) > 4)
                    {
                        CustomEffectService.ApplyCustomEffect(user, (NWCreature)oTarget, CustomEffectType.Burning, duration * 6, perkLevel, Convert.ToString(perkLevel));
                    }
                    break;
                case "GAS":
                    impactEffect = EffectDamage(RandomService.D6(perkLevel), DamageType.Acid);
                    duration = RandomService.D6(1);
                    if (RandomService.D6(1) > 4 && GetIsImmune(oTarget, ImmunityType.Poison) == false)
                    {
                        durationEffect = EffectPoison(Poison.Arsenic);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(grenadeType));
            }

            if (GetIsObjectValid(oTarget) == true)
            {
                if (impactEffect != null) ApplyEffectToObject(DurationType.Instant, impactEffect, oTarget);
                if (durationEffect != null) ApplyEffectToObject(DurationType.Temporary, durationEffect, oTarget, duration * 6.0f);
                if (!oTarget.IsPlayer)
                {
                    SkillService.RegisterPCToNPCForSkill(user.Object, oTarget, Skill.Throwing);
                }
            }
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return 0.0f;
        }

        public bool FaceTarget()
        {
            return true;
        }

        public Animation AnimationType()
        {
            return Animation.Invalid;
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 10.0f + 2.0f * user.StrengthModifier;
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            // infinite for testing only
            return false;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public bool AllowLocationTarget()
        {
            return true;
        }
    }
}
