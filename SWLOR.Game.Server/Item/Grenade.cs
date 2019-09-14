using System;
using NWN;

using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject;
using static NWN._;

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
            Effect impactEffect = null;
            int spellId = SPELL_GRENADE_FIRE;
            string soundName = null;
            int perkLevel = 1 + PerkService.GetCreaturePerkLevel(user, PerkType.GrenadeProficiency);
            int skillLevel = 5 + SkillService.GetPCSkillRank((NWPlayer)user, SkillType.Throwing);
            if (perkLevel == 0) perkLevel += 1;

            int roll = RandomService.D100(1);

            SendMessageToPC(user, roll + " vs. " + (100 - skillLevel));
            if (roll > 100 - skillLevel)
            {
                SendMessageToPC(user, "Your throw was a bit off the mark.");
                targetLocation = VectorService.MoveLocation(targetLocation, GetFacing(user), RandomService.D12(1) - 6 * 1.0f, 
                                                            RandomService.D100(1) + RandomService.D100(1) + RandomService.D100(1));
            }

            string grenadeType = item.GetLocalString("TYPE");

            Console.WriteLine("Throwing " + grenadeType + " grenade at perk level " + perkLevel);

            switch (grenadeType)
            {
                case "FRAG":                    
                    impactEffect = EffectVisualEffect(VFX_FNF_FIREBALL);
                    spellId = 974;
                    soundName = "explosion2";                    
                    break;
                case "CONCUSSION":
                    impactEffect = EffectVisualEffect(VFX_FNF_SOUND_BURST_SILENT);
                    impactEffect = EffectLinkEffects(EffectVisualEffect(VFX_FNF_SCREEN_SHAKE), impactEffect);
                    spellId = 974;
                    soundName = "explosion1";
                    break;
                case "FLASHBANG":
                    impactEffect = EffectVisualEffect(VFX_FNF_MYSTICAL_EXPLOSION);
                    spellId = 974;
                    soundName = "explosion1";
                    break;
                case "ION":
                    impactEffect = EffectVisualEffect(VFX_FNF_ELECTRIC_EXPLOSION);
                    spellId = 974;
                    soundName = "explosion1";
                    break;
                case "BACTA":
                    impactEffect = EffectVisualEffect(VFX_FNF_GAS_EXPLOSION_NATURE);
                    spellId = 974;
                    //soundName = "explosion1";
                    break;
                case "ADHESIVE":
                    impactEffect = EffectVisualEffect(VFX_FNF_DISPEL_GREATER);
                    spellId = 974;
                    //soundName = "explosion1";
                    break;
                case "SMOKE":
                    impactEffect = null;
                    spellId = 974;
                    //soundName = "explosion1";
                    break;
                case "BACTABOMB":
                    impactEffect = null;
                    spellId = 974;
                    //soundName = "explosion1";
                    break;
                case "INCENDIARY":
                    impactEffect = null;
                    spellId = 974;
                    //soundName = "explosion1";
                    break;
                case "GAS":
                    impactEffect = null;                    
                    spellId = 974;
                    //soundName = "explosion1";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(grenadeType));
            }           

            float delay = GetDistanceBetweenLocations(user.Location, targetLocation) / 18.0f + 0.75f;
            user.ClearAllActions();
            user.AssignCommand(() =>
            {
                ActionCastSpellAtLocation(spellId, targetLocation, METAMAGIC_ANY, TRUE, PROJECTILE_PATH_TYPE_BALLISTIC, TRUE);
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
                    ApplyEffectAtLocation(DURATION_TYPE_INSTANT, impactEffect, targetLocation);
                }, delay);
            }

            user.DelayAssignCommand(
                         () =>
                         {
                             DoImpact(user, targetLocation, grenadeType, perkLevel, RADIUS_SIZE_LARGE, OBJECT_TYPE_CREATURE);
                         }, delay + 0.75f);

        }

        public void DoImpact(NWCreature user, Location targetLocation, string grenadeType, int perkLevel, float fExplosionRadius, int nObjectFilter)
        {
            Effect damageEffect = EffectDamage(0, DAMAGE_TYPE_NEGATIVE);
            Effect durationEffect = null;
            int duration = perkLevel;

            switch (grenadeType)
            {
                case "SMOKE":
                    durationEffect = EffectAreaOfEffect(AOE_PER_FOG_OF_BEWILDERMENT, "grenade_smoke_en", "grenade_smoke_hb", "");
                    break;
                case "BACTABOMB":
                    durationEffect = EffectAreaOfEffect(AOE_PER_FOG_OF_BEWILDERMENT, "grenade_bbomb_en", "grenade_bbomb_hb", "");
                    break;
                case "INCENDIARY":
                    durationEffect = EffectAreaOfEffect(AOE_PER_FOGFIRE, "grenade_incen_en", "grenade_incen_hb", "");
                    break;
                case "GAS":
                    durationEffect = EffectAreaOfEffect(AOE_PER_FOGGHOUL, "grenade_gas_en", "grenade_gas_hb", "");
                    break;
                default:
                    break;
            }

            if (durationEffect != null)
            {
                //Apply AOE
                ApplyEffectAtLocation(_.DURATION_TYPE_TEMPORARY, durationEffect, targetLocation, duration * 6.0f);
            }
            else
            {
                //Apply impact

                // Target the next nearest creature and do the same thing.
                NWObject targetCreature = GetFirstObjectInShape(SHAPE_SPHERE, fExplosionRadius, targetLocation, TRUE, nObjectFilter);
                while (targetCreature.IsValid)
                {
                    switch (grenadeType)
                    {
                        case "FRAG":
                            damageEffect = EffectDamage(RandomService.D6(perkLevel), DAMAGE_TYPE_FIRE);
                            damageEffect = EffectLinkEffects(EffectDamage(RandomService.D6(perkLevel), _.DAMAGE_TYPE_PIERCING), damageEffect);
                            if (RandomService.D6(1) > 4)
                            {
                                CustomEffectService.ApplyCustomEffect(user, (NWCreature)targetCreature, CustomEffectType.Bleeding, duration * 6, perkLevel, Convert.ToString(perkLevel));
                            }
                            if (RandomService.D6(1) > 4)
                            {
                                CustomEffectService.ApplyCustomEffect(user, (NWCreature)targetCreature, CustomEffectType.Burning, duration * 6, perkLevel, Convert.ToString(perkLevel));
                            }
                            break;
                        case "CONCUSSION":
                            damageEffect = EffectDamage(RandomService.D12(perkLevel), DAMAGE_TYPE_SONIC);
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
                            damageEffect = EffectDamage(RandomService.D6(perkLevel), DAMAGE_TYPE_ELECTRICAL);
                            if (GetRacialType(targetCreature) == (int)CustomRaceType.Robot ||
                                (RandomService.D6(1) > 4 && GetRacialType(targetCreature) == (int)CustomRaceType.Cyborg))
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

                    if (damageEffect != null) ApplyEffectToObject(_.DURATION_TYPE_INSTANT, damageEffect, targetCreature);
                    if (durationEffect != null) ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, durationEffect, targetCreature, duration * 6.0f);

                    if (!targetCreature.IsPlayer)
                    {
                        SkillService.RegisterPCToNPCForSkill(user.Object, targetCreature, SkillType.Throwing);
                    }

                    Console.WriteLine("Grenade hit on " + targetCreature.Name);

                    targetCreature = GetNextObjectInShape(SHAPE_SPHERE, fExplosionRadius, targetLocation, TRUE, nObjectFilter);
                }
            }
        }

        public static void grenadeAoe(NWObject oTarget, string grenadeType)
        {
            NWCreature user = GetAreaOfEffectCreator();
            int perkLevel = PerkService.GetCreaturePerkLevel(user, PerkType.GrenadeProficiency);
            int duration = 1;
            Effect impactEffect = null;
            Effect durationEffect = null;

            switch (grenadeType)
            {
                case "SMOKE":
                    durationEffect = EffectInvisibility(INVISIBILITY_TYPE_NORMAL);
                    break;
                case "BACTABOMB":
                    durationEffect = EffectRegenerate(perkLevel*2, 6.0f);
                    break;
                case "INCENDIARY":
                    impactEffect = EffectDamage(RandomService.D6(perkLevel), DAMAGE_TYPE_FIRE);
                    duration = RandomService.D6(1);
                    if (RandomService.D6(1) > 4)
                    {
                        CustomEffectService.ApplyCustomEffect(user, (NWCreature)oTarget, CustomEffectType.Burning, duration * 6, perkLevel, Convert.ToString(perkLevel));
                    }
                    break;
                case "GAS":
                    impactEffect = EffectDamage(RandomService.D6(perkLevel), DAMAGE_TYPE_ACID);
                    duration = RandomService.D6(1);
                    if (RandomService.D6(1) > 4 && GetIsImmune(oTarget, IMMUNITY_TYPE_POISON) == FALSE)
                    {
                        durationEffect = EffectPoison(POISON_ARSENIC);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(grenadeType));
            }

            if (GetIsObjectValid(oTarget) == TRUE)
            {
                if (impactEffect != null) ApplyEffectToObject(_.DURATION_TYPE_INSTANT, impactEffect, oTarget);
                if (durationEffect != null) ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, durationEffect, oTarget, duration * 6.0f);
                if (!oTarget.IsPlayer)
                {
                    SkillService.RegisterPCToNPCForSkill(user.Object, oTarget, SkillType.Throwing);
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

        public int AnimationID()
        {
            return -1;
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 10.0f + 2.0f * user.StrengthModifier;
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
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
