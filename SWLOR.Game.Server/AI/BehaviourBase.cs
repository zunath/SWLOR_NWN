using System.Linq;
using SWLOR.Game.Server.GameObject;
using NWN;
using SWLOR.Game.Server.AI.Contracts;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Service;
using static NWN._;

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
            if (flags.HasFlag(AIFlags.RandomWalk))
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
            var flags = GetAIFlags(self);

            CleanUpEnmity(self);
            AttackHighestEnmity(self);

            // Target nearby enemy flag
            if(flags.HasFlag(AIFlags.AggroNearby))
                AggroTargetInRange(self);

            EquipBestWeapon(self);

            // Target nearby player who is attacking 
            if (flags.HasFlag(AIFlags.Link))
                Link(self);

            OnAIProcessing(self);
        }

        protected virtual void OnAIProcessing(NWCreature self)
        {

        }

        protected void CleanUpEnmity(NWCreature self)
        {
            var table = EnmityService.GetEnmityTable(self);
            if (table.Count <= 0) return;

            foreach (var enmity in table.ToArray())
            {
                var val = enmity.Value;
                var target = val.TargetObject;

                // Remove invalid objects from the enmity table
                if (target == null ||
                    !target.IsValid ||
                    !target.Area.Equals(self.Area) ||
                    target.CurrentHP <= -11)
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

        protected void AggroTargetInRange(NWCreature self)
        {
            if (self.IsInCombat) return;

            float aggroRange = self.GetLocalFloat("AGGRO_RANGE");
            if (aggroRange <= 0.0f) aggroRange = 10.0f;

            int nth = 1;
            NWCreature creature = GetNearestObject(OBJECT_TYPE_CREATURE, self.Object, nth);
            while (creature.IsValid)
            {
                if (GetIsEnemy(creature.Object, self.Object) == TRUE &&
                    !EnmityService.IsOnEnmityTable(self, creature) &&
                    !creature.HasAnyEffect(EFFECT_TYPE_SANCTUARY) &&
                    GetDistanceBetween(self.Object, creature.Object) <= aggroRange &&
                    LineOfSightObject(self.Object, creature.Object) == TRUE)
                {
                    EnmityService.AdjustEnmity(self, creature, 0, 1);
                }

                nth++;
                creature = GetNearestObject(OBJECT_TYPE_CREATURE, self.Object, nth);
            }
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

        private static void Link(NWCreature self)
        {
            if (EnmityService.IsEnmityTableEmpty(self)) return;
            float aggroRange = self.GetLocalFloat("LINK_RANGE");
            if (aggroRange <= 0.0f) aggroRange = 12.0f;

            int nth = 1;
            NWCreature creature = _.GetNearestObject(OBJECT_TYPE_CREATURE, self, nth);
            var target = EnmityService.GetEnmityTable(self).OrderByDescending(x => x.Value).First().Value.TargetObject;

            while (creature.IsValid)
            {
                if (!creature.IsPlayer &&
                    _.GetIsEnemy(creature, self) == FALSE &&
                    !EnmityService.IsOnEnmityTable(creature, target) &&
                    _.GetDistanceBetween(self, creature) <= aggroRange &&
                    self.RacialType == creature.RacialType)
                {
                    EnmityService.AdjustEnmity(creature, target, 0, 1);
                }
                nth++;
                creature = _.GetNearestObject(OBJECT_TYPE_CREATURE, self, nth);
            }

        }
    }
}
