using System;
using System.Linq;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Enumeration;
using static NWN._;
using SWLOR.Game.Server.Service;
using Object = NWN.Object;

namespace SWLOR.Game.Server.AI
{
    /// <summary>
    /// Standard behaviour which executes for all derived behaviours
    /// </summary>
    public class DarkForceUser : BehaviourBase
    {
        public override bool IgnoreNWNEvents => true;

        private void DoForceAttack()
        {
            // Trigger ForceAttackHighestEmnity if not doing anything. 
            if (_.GetCurrentAction() == _.ACTION_ATTACKOBJECT)
            {
                _.ClearAllActions();

                NWCreature self = Object.OBJECT_SELF;
                ForceAttackHighestEnmity(self);
            }
        }
        
        public override void OnPhysicalAttacked(NWCreature self)
        {
            base.OnPhysicalAttacked(self);
            DoForceAttack();
        }
        
        public override void OnDamaged(NWCreature self)
        {
            base.OnDamaged(self);
            DoForceAttack();
        }
        
        private bool UseFeat(int featID, string featName, NWCreature caster, NWCreature target)
        {
            // Note - this code is loosely based on code from AbilityService.  However, the perk interface
            // is written assuming players will always be using perks.  To allow NPCs to use them requires some hackery.
            int perkLevel = (int)caster.ChallengeRating / 5;
            if (perkLevel < 1) perkLevel = 1;

            if (caster.Area.Resref != target.Area.Resref ||
                    _.LineOfSightObject(caster.Object, target.Object) == 0)
            {
                return false;
            }

            // Give NPCs a bit longer range than most PCs.
            if (_.GetDistanceBetween(caster, target) > 20.0f)
            {
                return false;
            }

            // Note - NPCs are assumed to have infinite FPs.
            if (_.GetIsDead(caster) == 1)
            {
                return false;
            }

            // Cooldown of 1 round.
            string timeout = caster.GetLocalString("TIMEOUT_" + featName);
            DateTime unlockTime = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(timeout)) unlockTime = DateTime.Parse(timeout);
            DateTime now = DateTime.UtcNow;

            if (unlockTime > now)
            {
                return false;
            }
            else
            {
                unlockTime = now.AddSeconds(6);
                caster.SetLocalString("TIMEOUT_" + featName, unlockTime.ToString());
            }

            // Do the actual force attack.  Code taken from perks. 
            if (featID == (int)CustomFeatType.ForceLightning)
            {
                int length;
                int dotAmount;

                int basePotency;
                const float Tier1Modifier = 1.0f;
                const float Tier2Modifier = 1.6f;
                const float Tier3Modifier = 2.2f;
                const float Tier4Modifier = 0;

                switch (perkLevel)
                {
                    case 1:
                        basePotency = 15;
                        length = 0;
                        dotAmount = 0;
                        break;
                    case 2:
                        basePotency = 20;
                        length = 6;
                        dotAmount = 4;
                        break;
                    case 3:
                        basePotency = 25;
                        length = 6;
                        dotAmount = 6;
                        break;
                    case 4:
                        basePotency = 40;
                        length = 12;
                        dotAmount = 6;
                        break;
                    case 5:
                        basePotency = 50;
                        length = 12;
                        dotAmount = 6;
                        break;
                    case 6:
                        basePotency = 60;
                        length = 12;
                        dotAmount = 6;
                        break;
                    case 7:
                        basePotency = 70;
                        length = 12;
                        dotAmount = 6;
                        break;
                    case 8:
                        basePotency = 80;
                        length = 12;
                        dotAmount = 8;
                        break;
                    case 9:
                        basePotency = 90;
                        length = 12;
                        dotAmount = 8;
                        break;
                    default:
                        basePotency = 100;
                        length = 12;
                        dotAmount = 10;
                        break;
                }

                var calc = CombatService.CalculateForceDamage(
                    caster,
                    target.Object,
                    ForceAbilityType.Electrical,
                    basePotency,
                    Tier1Modifier,
                    Tier2Modifier,
                    Tier3Modifier,
                    Tier4Modifier);

                caster.AssignCommand(() => {
                    _.SetFacingPoint(target.Location.Position);
                    _.ActionPlayAnimation(ANIMATION_LOOPING_CONJURE1, 1.0f, 1.0f);
                });

                caster.SetLocalInt("CASTING", 1);

                _.DelayCommand(1.0f, () =>
                {
                    caster.AssignCommand(() =>
                    {
                        Effect damage = _.EffectDamage(calc.Damage, DAMAGE_TYPE_ELECTRICAL);
                        _.ApplyEffectToObject(DURATION_TYPE_INSTANT, damage, target);
                    });

                    if (length > 0.0f && dotAmount > 0)
                    {
                        CustomEffectService.ApplyCustomEffect(caster, target.Object, CustomEffectType.ForceShock, length, perkLevel, dotAmount.ToString());
                    }

                    caster.AssignCommand(() =>
                    {
                        _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectVisualEffect(VFX_BEAM_LIGHTNING), target, 1.0f);
                        caster.DeleteLocalInt("CASTING");
                    });

                    CombatService.AddTemporaryForceDefense(target.Object, ForceAbilityType.Electrical);
                });
            }
            else if (featID == (int)CustomFeatType.DrainLife)
            {
                float recoveryPercent;
                int basePotency;
                const float Tier1Modifier = 1;
                const float Tier2Modifier = 2;
                const float Tier3Modifier = 0;
                const float Tier4Modifier = 0;

                switch (perkLevel)
                {
                    case 1:
                        basePotency = 10;
                        recoveryPercent = 0.2f;
                        break;
                    case 2:
                        basePotency = 15;
                        recoveryPercent = 0.2f;
                        break;
                    case 3:
                        basePotency = 20;
                        recoveryPercent = 0.4f;
                        break;
                    case 4:
                        basePotency = 25;
                        recoveryPercent = 0.4f;
                        break;
                    default:
                        basePotency = 30;
                        recoveryPercent = 0.5f;
                        break;
                }

                var calc = CombatService.CalculateForceDamage(
                    caster,
                    target.Object,
                    ForceAbilityType.Dark,
                    basePotency,
                    Tier1Modifier,
                    Tier2Modifier,
                    Tier3Modifier,
                    Tier4Modifier);

                caster.AssignCommand(() => {
                    _.SetFacingPoint(target.Location.Position);
                    _.ActionPlayAnimation(ANIMATION_LOOPING_CONJURE1, 1.0f, 1.0f);
                });
                caster.SetLocalInt("CASTING", 1);

                _.DelayCommand(1.0f, () =>
                {
                    _.AssignCommand(caster, () =>
                    {
                        int heal = (int)(calc.Damage * recoveryPercent);
                        if (heal > target.CurrentHP) heal = target.CurrentHP;

                        _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectDamage(calc.Damage), target);
                        _.ApplyEffectToObject(DURATION_TYPE_INSTANT, _.EffectHeal(heal), caster);
                        _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectVisualEffect(VFX_BEAM_MIND), target, 1.0f);
                        caster.DeleteLocalInt("CASTING");
                    });
                });


                CombatService.AddTemporaryForceDefense(target.Object, ForceAbilityType.Dark);
            }

            return true;
        }

        private void ForceAttackHighestEnmity(NWCreature self)
        {
            if (self.GetLocalInt("CASTING") == 1) return;
            var enmityTable = EnmityService.GetEnmityTable(self);
            var target = enmityTable.Values
                .OrderByDescending(o => o.TotalAmount)
                .FirstOrDefault(x => x.TargetObject.IsValid &&
                                     x.TargetObject.Area.Equals(self.Area));

            self.AssignCommand(() =>
            {
                if (target == null)
                {
                    _.ClearAllActions();
                }
                else
                {
                    bool bDone = false;

                    // See which force feats we have, and pick one to use. 
                    if (_.GetHasFeat((int)CustomFeatType.ForceLightning, self) == 1)
                    {
                        _.ClearAllActions();
                        bDone = UseFeat((int)CustomFeatType.ForceLightning, "ForceLightning", self, target.TargetObject);
                    }

                    if (!bDone && _.GetHasFeat((int)CustomFeatType.DrainLife, self) == 1)
                    {
                        _.ClearAllActions();
                        bDone = UseFeat((int)CustomFeatType.DrainLife, "DrainLife", self, target.TargetObject);
                    }

                    if (!bDone)
                    {
                        // No abilities available right now, run away!
                        _.ActionMoveAwayFromObject(target.TargetObject, 1);
                    }
                }
            });
        }
    }
}
