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
    public class Grenade: IActionItem
    {
        public string CustomKey => null;

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            Effect damageEffect = damageEffect = EffectDamage(RandomService.D6(6), _.DAMAGE_TYPE_PIERCING);
            Effect impactEffect = null;

            NWPlaceable tempPlaceable = CreateObject(OBJECT_TYPE_PLACEABLE, "grenade_obj001", user.Location);



            if (targetLocation == null) targetLocation = GetLocation(target);

            float delay = GetDistanceBetweenLocations(user.Location, targetLocation) / 20.0f;

            if (item.GetLocalString("TYPE") == "FRAG")
            {
                damageEffect = EffectDamage(RandomService.D6(6), _.DAMAGE_TYPE_PIERCING);

                impactEffect = EffectVisualEffect(VFX_FNF_FIREBALL);
            }

            tempPlaceable.DelayAssignCommand(() =>
            {
                ActionCastSpellAtLocation(974, targetLocation, METAMAGIC_ANY, TRUE, PROJECTILE_PATH_TYPE_BALLISTIC, TRUE);
            }, delay + 0.1f);

            delay += 0.75f;

            DelayCommand(delay, () =>
            {
                DestroyObject(tempPlaceable);
            });

            user.DelayAssignCommand(() =>
            {
                PlaySound("explosion2");
            }, delay);

            user.DelayAssignCommand(() =>
            {
                ApplyEffectAtLocation(DURATION_TYPE_INSTANT, impactEffect, targetLocation);
            }, delay + 0.5f);

            
            DelayCommand(delay+0.75f,
                         () =>
                         {
                             DoImpact(user, targetLocation, RandomService.D6(4), 0, 0, DAMAGE_TYPE_FIRE, RADIUS_SIZE_LARGE, OBJECT_TYPE_CREATURE);
                         });

        }

        public void DoImpact(NWCreature creature, Location targetLocation, int nDamage, int vSmallHit, int vRingHit, int nDamageType, float fExplosionRadius, int nObjectFilter, int nRacialType = RACIAL_TYPE_ALL)
        {
            Effect damageEffect = EffectDamage(nDamage, nDamageType);           
            // Target the next nearest creature and do the same thing.
            NWObject targetCreature = GetFirstObjectInShape(SHAPE_SPHERE, fExplosionRadius, targetLocation, TRUE, nObjectFilter);
            while (targetCreature.IsValid)
            {
                Console.WriteLine("Grenade hit on " + targetCreature.Name);
                creature.AssignCommand(() =>
                {
                    _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, damageEffect, targetCreature);
                });

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
