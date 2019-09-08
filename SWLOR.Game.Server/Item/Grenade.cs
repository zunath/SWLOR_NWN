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
            string soundName = "explosion2";
            int perkLevel = PerkService.GetCreaturePerkLevel(user, PerkType.GrenadeProficiency);          

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
                    soundName = "explosion1";
                    break;
                case "ADHESIVE":
                    impactEffect = EffectVisualEffect(VFX_FNF_DISPEL_GREATER);
                    spellId = 974;
                    soundName = "explosion1";
                    break;
                case "SMOKE":
                    impactEffect = EffectVisualEffect(VFX_FNF_SMOKE_PUFF);
                    spellId = 974;
                    soundName = "explosion1";
                    break;
                case "BACTABOMB":
                    impactEffect = EffectVisualEffect(VFX_FNF_SOUND_BURST_SILENT);
                    spellId = 974;
                    soundName = "explosion1";
                    break;
                case "INCENDIARY":
                    impactEffect = EffectVisualEffect(VFX_FNF_SOUND_BURST_SILENT);
                    spellId = 974;
                    soundName = "explosion1";
                    break;
                case "GAS":
                    impactEffect = EffectVisualEffect(VFX_FNF_SOUND_BURST_SILENT);
                    spellId = 974;
                    soundName = "explosion1";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(grenadeType));
            }           

            float delay = GetDistanceBetweenLocations(user.Location, targetLocation) / 18.0f + 0.75f;
            user.ClearAllActions();
            user.AssignCommand(() =>
            {
                ActionCastSpellAtLocation(spellId, targetLocation, METAMAGIC_ANY, TRUE, PROJECTILE_PATH_TYPE_BALLISTIC, TRUE);
                //ActionCastFakeSpellAtLocation(spellId, targetLocation, PROJECTILE_PATH_TYPE_BALLISTIC);
            });
            
            user.DelayAssignCommand(() =>
            {
                PlaySound(soundName);
            }, delay);

            user.DelayAssignCommand(() =>
            {
                ApplyEffectAtLocation(DURATION_TYPE_INSTANT, impactEffect, targetLocation);
            }, delay);

            user.DelayAssignCommand(
                         () =>
                         {
                             DoImpact(targetLocation, grenadeType, perkLevel, RADIUS_SIZE_LARGE, OBJECT_TYPE_CREATURE);
                         }, delay + 0.75f);

        }

        public void DoImpact(Location targetLocation, string grenadeType, int perkLevel, float fExplosionRadius, int nObjectFilter)
        {
            Effect damageEffect = EffectDamage(0, DAMAGE_TYPE_NEGATIVE); 
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
                            // apply burning and or bleeding here
                        }
                        break;
                    case "CONCUSSION":
                        damageEffect = EffectDamage(RandomService.D6(perkLevel), DAMAGE_TYPE_SONIC);
                        break;
                    case "FLASHBANG":
                        break;
                    case "ION":
                        damageEffect = EffectDamage(RandomService.D6(perkLevel), DAMAGE_TYPE_ELECTRICAL);
                        break;
                    case "BACTA":
                        damageEffect = EffectRegenerate(perkLevel, perkLevel * 6.0f);
                        break;
                    case "ADHESIVE":
                        break;
                    case "SMOKE":
                        break;
                    case "BACTABOMB":
                        break;
                    case "INCENDIARY":
                        break;
                    case "GAS":
                        break;
                    default:                        
                        throw new ArgumentOutOfRangeException(nameof(grenadeType));
                }
                ApplyEffectToObject(_.DURATION_TYPE_INSTANT, damageEffect, targetCreature);
                Console.WriteLine("Grenade hit on " + targetCreature.Name);

                targetCreature = GetNextObjectInShape(SHAPE_SPHERE, fExplosionRadius, targetLocation, TRUE, nObjectFilter);
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
