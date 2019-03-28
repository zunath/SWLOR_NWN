﻿using System;
using System.Linq;
using SWLOR.Game.Server.GameObject;
using NWN;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;
using static NWN._;
using Object = NWN.Object;

namespace SWLOR.Game.Server.AI
{
    /// <summary>
    /// The base class for creating new behaviours.
    /// </summary>
    public abstract class BehaviourBase: IAIBehaviour
    {
        public virtual bool IgnoreNWNEvents => true;
        public virtual bool IgnoreOnBlocked => false;
        public virtual bool IgnoreOnCombatRoundEnd => false;
        public virtual bool IgnoreOnConversation => false;
        public virtual bool IgnoreOnDamaged => false;
        public virtual bool IgnoreOnDeath => false;
        public virtual bool IgnoreOnDisturbed => false;
        public virtual bool IgnoreOnHeartbeat => false;
        public virtual bool IgnoreOnPerception => false;
        public virtual bool IgnoreOnPhysicalAttacked => false;
        public virtual bool IgnoreOnRested => false;
        public virtual bool IgnoreOnSpawn => false;
        public virtual bool IgnoreOnSpellCastAt => false;
        public virtual bool IgnoreOnUserDefined => false;

        private const float DefaultAggroRange = 10.0f;
        private const float DefaultLinkRange = 12.0f;

        private AIFlags GetAIFlags(NWCreature self)
        {
            return (AIFlags)self.GetLocalInt("AI_FLAGS");
        }

        public virtual void OnBlocked(NWCreature self)
        {
            NWObject door = (GetBlockingDoor());
            if (!door.IsValid) return;

            if (GetIsDoorActionPossible(door.Object, DOOR_ACTION_OPEN) == TRUE)
            {
                DoDoorAction(door.Object, DOOR_ACTION_OPEN);
            }
        }

        public virtual void OnCombatRoundEnd(NWCreature self)
        {
        }

        public virtual void OnConversation(NWCreature self)
        {
            string convo = self.GetLocalString("CONVERSATION");

            if (!string.IsNullOrWhiteSpace(convo))
            {
                NWPlayer player = (GetLastSpeaker());
                DialogService.StartConversation(player, self, convo);
            }
            else if (!string.IsNullOrWhiteSpace(NWNXObject.GetDialogResref(self)))
            {
                BeginConversation(NWNXObject.GetDialogResref(self));
            }
        }

        public virtual void OnDamaged(NWCreature self)
        {
            EnmityService.OnNPCDamaged();
        }

        public virtual void OnDeath(NWCreature self)
        {
            int vfx = self.GetLocalInt("DEATH_VFX");
            if (vfx > 0)
            {
                ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(vfx), self);
            }
        }

        public virtual void OnDisturbed(NWCreature self)
        {
        }

        public virtual void OnHeartbeat(NWCreature self)
        {
            var flags = GetAIFlags(self);
            if ((flags & AIFlags.RandomWalk) != 0)
                RandomWalk(self);
        }

        public virtual void OnPerception(NWCreature self)
        {
        }

        public virtual void OnPhysicalAttacked(NWCreature self)
        {
            EnmityService.OnNPCPhysicallyAttacked();
        }

        public virtual void OnRested(NWCreature self)
        {
        }

        public virtual void OnSpawn(NWCreature self)
        {
        }

        public virtual void OnSpellCastAt(NWCreature self)
        {
        }

        public virtual void OnUserDefined(NWCreature self)
        {
        }

        public void OnProcessObject(NWCreature self)
        {
            CleanUpEnmity(self);
            AttackHighestEnmity(self);
            EquipBestWeapon(self);
            ProcessNearbyCreatures(self);

            OnAIProcessing(self);
        }

        protected virtual void OnAIProcessing(NWCreature self)
        {

        }

        protected void CleanUpEnmity(NWCreature self)
        {
            var table = EnmityService.GetEnmityTable(self);
            if (table.Count <= 0) return;

            for(int x = table.Count-1; x >= 0; x--)
            {
                var enmity = table.ElementAt(x);
                var val = enmity.Value;
                var target = val.TargetObject;

                // Remove invalid objects from the enmity table
                if (target == null ||
                    !target.IsValid ||
                    !target.Area.Equals(self.Area) ||
                    target.CurrentHP <= -11 ||
                    target.IsDead)
                {
                    EnmityService.GetEnmityTable(self).Remove(enmity.Key);
                    continue;
                }

                AdjustReputation(target.Object, self.Object, -100);

                // Reduce volatile enmity every tick
                EnmityService.GetEnmityTable(self)[target.GlobalID].VolatileAmount--;
            }
        }

        protected void AttackHighestEnmity(NWCreature self)
        {
            var enmityTable = EnmityService.GetEnmityTable(self);
            var target = enmityTable.Values
                .OrderByDescending(o => o.TotalAmount)
                .FirstOrDefault(x => x.TargetObject.IsValid &&
                                     x.TargetObject.Area.Equals(self.Area));

            if(target != null)
            {
                self.AssignCommand(() =>
                {
                    if (GetAttackTarget(self.Object) != target.TargetObject.Object)
                    {
                        ClearAllActions();
                        ActionAttack(target.TargetObject.Object);
                    }
                });
            }
        }

        protected void EquipBestWeapon(NWCreature self)
        {
            if (!self.IsInCombat) return;

            if (!self.IsInCombat ||
                self.RightHand.IsRanged)
                return;

            if (self.RightHand.IsRanged)
            {
                self.AssignCommand(() =>
                {
                    ActionEquipMostDamagingRanged(new Object());
                });
            }
            else
            {
                self.AssignCommand(() =>
                {
                    ActionEquipMostDamagingMelee(new Object());
                });
            }
        }

        private void ProcessNearbyCreatures(NWCreature self)
        {
            var flags = GetAIFlags(self);

            // Does this creature have one of the supported flags? If not, exit early.
            if ((flags & AIFlags.AggroNearby) == 0 && (flags & AIFlags.Link) == 0) return;

            // Aggro & Link Flags - Only process if not currently aggro'd to someone else.
            if ((flags & AIFlags.AggroNearby) != 0 || (flags & AIFlags.Link) != 0)
            {
                if (!EnmityService.IsEnmityTableEmpty(self)) return;
            }

            // Cycle through each nearby creature. Process their flags individually if necessary.
            int nth = 1;
            NWCreature creature = _.GetNearestObject(OBJECT_TYPE_CREATURE, self, nth);
            while (creature.IsValid)
            {
                float aggroRange = GetAggroRange(creature);
                float linkRange = GetLinkRange(creature);

                float distance = _.GetDistanceBetween(creature, self);
                if (distance > aggroRange && distance > linkRange) break;

                if ((flags & AIFlags.AggroNearby) != 0)
                {
                    AggroTargetInRange(self, creature);
                }

                if ((flags & AIFlags.Link) != 0)
                {
                    Link(self, creature);
                }
                nth++;
                creature = _.GetNearestObject(OBJECT_TYPE_CREATURE, self, nth);
            }
        }

        private float GetAggroRange(NWCreature creature)
        {
            float aggroRange = creature.GetLocalFloat("AGGRO_RANGE");
            if (aggroRange <= 0.0f) aggroRange = DefaultAggroRange;

            return aggroRange;
        }

        private float GetLinkRange(NWCreature creature)
        {
            float linkRange = creature.GetLocalFloat("LINK_RANGE");
            if (linkRange <= 0.0f) linkRange = DefaultLinkRange;

            return linkRange;
        }

        protected void AggroTargetInRange(NWCreature self, NWCreature nearby)
        {
            float aggroRange = GetAggroRange(self);
            // Check distance
            if (GetDistanceBetween(self, nearby) > aggroRange) return;

            // Is creature dead?
            if (nearby.IsDead) return;

            // Does the nearby creature have line of sight to the creature being attacked?
            if (LineOfSightObject(self, nearby) == FALSE) return;

            // Is the nearby creature not an enemy?
            if (GetIsEnemy(nearby, self.Object) == FALSE) return;

            // Does the nearby creature have sanctuary?
            if (nearby.HasAnyEffect(EFFECT_TYPE_SANCTUARY)) return;

            // Success. Increase enmity on the nearby target.
            EnmityService.AdjustEnmity(self, nearby, 0, 1);
        }

        private static void Link(NWCreature self, NWCreature nearby)
        {
            float linkRange = self.GetLocalFloat("LINK_RANGE");
            if (linkRange <= 0.0f) linkRange = 12.0f;

            // Check distance. If too far away stop processing.
            if (_.GetDistanceBetween(self, nearby) > linkRange) return;
            
            // Is the nearby object an NPC?
            if (!nearby.IsNPC) return;

            // Is the nearby creature dead?
            if (nearby.IsDead) return;

            // Is the nearby creature an enemy?
            if (_.GetIsEnemy(nearby, self) == TRUE) return;

            // Does the calling creature have the same racial type as the nearby creature?
            if (self.RacialType != nearby.RacialType) return;

            // Does the nearby creature have anything on its enmity table?
            var nearbyEnmityTable = EnmityService.GetEnmityTable(nearby).OrderByDescending(x => x.Value).FirstOrDefault();
            if (nearbyEnmityTable.Value == null) return;

            var target = nearbyEnmityTable.Value.TargetObject;
            // Is the target dead?
            if (target.IsDead) return;
            
            // Add the target of the nearby creature to this creature's enmity table.
            EnmityService.AdjustEnmity(self, target, 0, 1);
        }

        private static void RandomWalk(NWCreature self)
        {
            if (self.IsInCombat || !EnmityService.IsEnmityTableEmpty(self))
            {
                return;
            }

            if (_.GetCurrentAction(self.Object) == _.ACTION_INVALID &&
                _.IsInConversation(self.Object) == _.FALSE &&
                _.GetCurrentAction(self.Object) != _.ACTION_RANDOMWALK &&
                RandomService.Random(100) <= 25)
            {
                self.AssignCommand(_.ActionRandomWalk);
            }
        }

    }
}
