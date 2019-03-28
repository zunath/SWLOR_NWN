using NWN;
using SWLOR.Game.Server.Bioware;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Delayed;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.Module;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Perk;
using System;
using System.Linq;
using SWLOR.Game.Server.Event.Feat;
using static NWN._;
using Object = NWN.Object;
using PerkExecutionType = SWLOR.Game.Server.Enumeration.PerkExecutionType;

namespace SWLOR.Game.Server.Service
{
    public static class AbilityService
    {

        // These variables are used throughout the engine to flag the type of damage being done to 
        // a creature.  The damage code reads this to determine what bonus effects to apply.
        // The LAST_ATTACK variable name should be appended with the GlobalID of the attacking
        // (N)PC so that attacks from different creatures are treated correctly. 
        public const string LAST_ATTACK = "LAST_ATTACK_";

        public static int ATTACK_PHYSICAL = 1;  // Weapon attacks and weapon skills
        public static int ATTACK_FORCE = 2;  // Force effects
        public static int ATTACK_COMBATABILITY = 3; // Combat tricks like Provoke
        public static int ATTACK_DOT = 4; // Subsequent damage effects

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnHitCastSpell>(message => OnHitCastSpell());
            MessageHub.Instance.Subscribe<OnModuleUseFeat>(message => OnModuleUseFeat());
        }

        private static void OnModuleUseFeat()
        {
            NWPlayer pc = Object.OBJECT_SELF;
            NWCreature target = NWNXEvents.OnFeatUsed_GetTarget().Object;
            int featID = NWNXEvents.OnFeatUsed_GetFeatID();
            var perkFeat = DataService.SingleOrDefault<PerkFeat>(x => x.FeatID == featID);
            if (perkFeat == null) return;
            Data.Entity.Perk perk = DataService.GetAll<Data.Entity.Perk>().SingleOrDefault(x => x.ID == perkFeat.PerkID);
            if (perk == null) return;

            // Check to see if we are a spaceship.  Spaceships can't use abilities...
            if (pc.GetLocalInt("IS_SHIP") > 0 || pc.GetLocalInt("IS_GUNNER") > 0)
            {
                pc.SendMessage("You cannot use that ability while piloting a ship.");
                return;
            }

            var perkAction = PerkService.GetPerkHandler(perkFeat.PerkID);
            
            Player playerEntity = DataService.Get<Player>(pc.GlobalID);
            int pcPerkLevel = PerkService.GetPCPerkLevel(pc, perk.ID);

            // If player is disabling an existing stance, remove that effect.
            if (perk.ExecutionTypeID == (int)PerkExecutionType.Stance)
            {
                PCCustomEffect stanceEffect = DataService.SingleOrDefault<PCCustomEffect>(x => x.StancePerkID == perk.ID &&
                                                                                               x.PlayerID == pc.GlobalID);

                if (stanceEffect != null)
                {
                    if (CustomEffectService.RemoveStance(pc))
                    {
                        return;
                    }
                }
            }

            if (pcPerkLevel <= 0)
            {
                pc.SendMessage("You do not meet the prerequisites to use this ability.");
                return;
            }

            if (perkAction.IsHostile() && target.IsPlayer)
            {
                if (!PVPSanctuaryService.IsPVPAttackAllowed(pc, target.Object)) return;
            }

            if (pc.Area.Resref != target.Area.Resref ||
                    _.LineOfSightObject(pc.Object, target.Object) == 0)
            {
                pc.SendMessage("You cannot see your target.");
                return;
            }

            if (!perkAction.CanCastSpell(pc, target))
            {
                pc.SendMessage(perkAction.CannotCastSpellMessage(pc, target) ?? "That ability cannot be used at this time.");
                return;
            }

            int fpCost = perkAction.FPCost(pc, perkAction.FPCost(pc, perk.BaseFPCost, featID), featID);
            if (playerEntity.CurrentFP < fpCost)
            {
                pc.SendMessage("You do not have enough FP. (Required: " + fpCost + ". You have: " + playerEntity.CurrentFP + ")");
                return;
            }

            if (pc.IsBusy || pc.CurrentHP <= 0)
            {
                pc.SendMessage("You are too busy to activate that ability.");
                return;
            }

            // Check cooldown
            int? cooldownCategoryID = perkAction.CooldownCategoryID(pc, perk.CooldownCategoryID, featID);
            PCCooldown pcCooldown = DataService.GetAll<PCCooldown>().SingleOrDefault(x => x.PlayerID == pc.GlobalID &&
                                                                                    x.CooldownCategoryID == cooldownCategoryID);
            if (pcCooldown == null)
            {
                pcCooldown = new PCCooldown
                {
                    CooldownCategoryID = Convert.ToInt32(cooldownCategoryID),
                    DateUnlocked = DateTime.UtcNow.AddSeconds(-1),
                    PlayerID = pc.GlobalID
                };

                DataService.SubmitDataChange(pcCooldown, DatabaseActionType.Insert);
            }

            DateTime unlockDateTime = pcCooldown.DateUnlocked;
            DateTime now = DateTime.UtcNow;

            if (unlockDateTime > now)
            {
                string timeToWait = TimeService.GetTimeToWaitLongIntervals(now, unlockDateTime, false);
                pc.SendMessage("That ability can be used in " + timeToWait + ".");
                return;
            }

            // Force Abilities (aka Spells)
            if (perk.ExecutionTypeID == (int)PerkExecutionType.ForceAbility)
            {
                target.SetLocalInt(LAST_ATTACK + pc.GlobalID, ATTACK_FORCE);
                ActivateAbility(pc, target, perk, perkAction, pcPerkLevel, PerkExecutionType.ForceAbility, featID);
            }
            // Combat Abilities
            else if (perk.ExecutionTypeID == (int)PerkExecutionType.CombatAbility)
            {
                target.SetLocalInt(LAST_ATTACK + pc.GlobalID, ATTACK_PHYSICAL);
                ActivateAbility(pc, target, perk, perkAction, pcPerkLevel, PerkExecutionType.CombatAbility, featID);
            }
            // Queued Weapon Skills
            else if (perk.ExecutionTypeID == (int)PerkExecutionType.QueuedWeaponSkill)
            {
                target.SetLocalInt(LAST_ATTACK + pc.GlobalID, ATTACK_PHYSICAL);
                HandleQueueWeaponSkill(pc, perk, perkAction, featID);
            }
            // Stances
            else if (perk.ExecutionTypeID == (int)PerkExecutionType.Stance)
            {
                target.SetLocalInt(LAST_ATTACK + pc.GlobalID, ATTACK_COMBATABILITY);
                ActivateAbility(pc, target, perk, perkAction, pcPerkLevel, PerkExecutionType.Stance, featID);
            }


        }

        public static void ApplyEnmity(NWPlayer pc, NWCreature target, Data.Entity.Perk perk)
        {
            switch ((EnmityAdjustmentRuleType)perk.EnmityAdjustmentRuleID)
            {
                case EnmityAdjustmentRuleType.AllTaggedTargets:
                    EnmityService.AdjustEnmityOnAllTaggedCreatures(pc, perk.Enmity);
                    break;
                case EnmityAdjustmentRuleType.TargetOnly:
                    if (target.IsValid)
                    {
                        EnmityService.AdjustEnmity(target, pc, perk.Enmity);
                    }
                    break;
                case EnmityAdjustmentRuleType.Custom:
                    var handler = PerkService.GetPerkHandler(perk.ID);
                    handler.OnCustomEnmityRule(pc, perk.Enmity);
                    break;
            }
        }

        private static void ActivateAbility(NWPlayer pc,
                               NWObject target,
                               Data.Entity.Perk entity,
                               IPerkHandler perkHandler,
                               int pcPerkLevel,
                               PerkExecutionType executionType,
                               int spellFeatID)
        {
            string uuid = Guid.NewGuid().ToString();
            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(pc);
            int itemBonus = effectiveStats.CastingSpeed;
            float baseActivationTime = perkHandler.CastingTime(pc, (float)entity.BaseCastingTime, spellFeatID);
            float activationTime = baseActivationTime;
            int vfxID = -1;
            int animationID = -1;

            // Activation Bonus % - Shorten activation time.
            if (itemBonus > 0)
            {
                float activationBonus = Math.Abs(itemBonus) * 0.01f;
                activationTime = activationTime - activationTime * activationBonus;
            }
            // Activation Penalty % - Increase activation time.
            else if (itemBonus < 0)
            {
                float activationPenalty = Math.Abs(itemBonus) * 0.01f;
                activationTime = activationTime + activationTime * activationPenalty;
            }

            if (baseActivationTime > 0f && activationTime < 1.0f)
                activationTime = 1.0f;

            // Force ability armor penalties
            if (executionType == PerkExecutionType.ForceAbility)
            {
                float armorPenalty = 0.0f;
                string penaltyMessage = string.Empty;
                foreach (var item in pc.EquippedItems)
                {
                    if (item.CustomItemType == CustomItemType.HeavyArmor)
                    {
                        armorPenalty = 2;
                        penaltyMessage = "Heavy armor slows your force activation speed by 100%.";
                        break;
                    }
                    else if (item.CustomItemType == CustomItemType.LightArmor)
                    {
                        armorPenalty = 1.25f;
                        penaltyMessage = "Light armor slows your force activation speed by 25%.";
                    }
                }

                if (armorPenalty > 0.0f)
                {
                    activationTime = baseActivationTime * armorPenalty;
                    pc.SendMessage(penaltyMessage);
                }

            }

            if (_.GetActionMode(pc.Object, ACTION_MODE_STEALTH) == 1)
                _.SetActionMode(pc.Object, ACTION_MODE_STEALTH, 0);

            _.ClearAllActions();
            BiowarePosition.TurnToFaceObject(target, pc);

            if (executionType == PerkExecutionType.ForceAbility)
            {
                vfxID = VFX_DUR_IOUNSTONE_YELLOW;
                animationID = ANIMATION_LOOPING_CONJURE1;
            }

            if (vfxID > -1)
            {
                var vfx = _.EffectVisualEffect(vfxID);
                vfx = _.TagEffect(vfx, "ACTIVATION_VFX");
                _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, vfx, pc.Object, activationTime + 0.2f);
            }

            if (animationID > -1)
            {
                pc.AssignCommand(() => _.ActionPlayAnimation(animationID, 1.0f, activationTime - 0.1f));
            }

            pc.IsBusy = true;
            CheckForSpellInterruption(pc, uuid, pc.Position);
            pc.SetLocalInt(uuid, (int)SpellStatusType.Started);

            NWNXPlayer.StartGuiTimingBar(pc, (int)activationTime, "");

            int perkID = entity.ID;
            pc.DelayEvent<FinishAbilityUse>(activationTime + 0.2f,
                pc,
                uuid,
                perkID,
                target,
                pcPerkLevel,
                spellFeatID);
        }

        public static void ApplyCooldown(NWPlayer pc, CooldownCategory cooldown, IPerkHandler ability, int spellFeatID)
        {
            float finalCooldown = ability.CooldownTime(pc, (float)cooldown.BaseCooldownTime, spellFeatID);
            int cooldownSeconds = (int)finalCooldown;
            int cooldownMillis = (int)((finalCooldown - cooldownSeconds) * 100);

            PCCooldown pcCooldown = DataService.GetAll<PCCooldown>().Single(x => x.PlayerID == pc.GlobalID && x.CooldownCategoryID == cooldown.ID);
            pcCooldown.DateUnlocked = DateTime.UtcNow.AddSeconds(cooldownSeconds).AddMilliseconds(cooldownMillis);
            DataService.SubmitDataChange(pcCooldown, DatabaseActionType.Update);
        }

        private static void CheckForSpellInterruption(NWPlayer pc, string spellUUID, Vector position)
        {
            if (pc.GetLocalInt(spellUUID) == (int)SpellStatusType.Completed) return;

            Vector currentPosition = pc.Position;

            if (currentPosition.m_X != position.m_X ||
                currentPosition.m_Y != position.m_Y ||
                currentPosition.m_Z != position.m_Z)
            {
                var effect = pc.Effects.SingleOrDefault(x => _.GetEffectTag(x) == "ACTIVATION_VFX");
                if (effect != null)
                {
                    _.RemoveEffect(pc, effect);
                }

                NWNXPlayer.StopGuiTimingBar(pc, "", -1);
                pc.IsBusy = false;
                pc.SetLocalInt(spellUUID, (int)SpellStatusType.Interrupted);
                pc.SendMessage("Your ability has been interrupted.");
                return;
            }

            _.DelayCommand(0.5f, () => { CheckForSpellInterruption(pc, spellUUID, position); });
        }

        public static void HandleQueueWeaponSkill(NWPlayer pc, Data.Entity.Perk entity, IPerkHandler ability, int spellFeatID)
        {
            int? cooldownCategoryID = ability.CooldownCategoryID(pc, entity.CooldownCategoryID, spellFeatID);
            var cooldownCategory = DataService.Get<CooldownCategory>(cooldownCategoryID);
            string queueUUID = Guid.NewGuid().ToString();
            pc.SetLocalInt("ACTIVE_WEAPON_SKILL", entity.ID);
            pc.SetLocalString("ACTIVE_WEAPON_SKILL_UUID", queueUUID);
            pc.SetLocalInt("ACTIVE_WEAPON_SKILL_FEAT_ID", spellFeatID);
            pc.SendMessage("Weapon skill '" + entity.Name + "' queued for next attack.");

            ApplyCooldown(pc, cooldownCategory, ability, spellFeatID);

            // Player must attack within 30 seconds after queueing or else it wears off.
            _.DelayCommand(30f, () =>
            {
                if (pc.GetLocalString("ACTIVE_WEAPON_SKILL_UUID") == queueUUID)
                {
                    pc.DeleteLocalInt("ACTIVE_WEAPON_SKILL");
                    pc.DeleteLocalString("ACTIVE_WEAPON_SKILL_UUID");
                    pc.DeleteLocalInt("ACTIVE_WEAPON_SKILL_FEAT_ID");
                    pc.SendMessage("Your weapon skill '" + entity.Name + "' is no longer queued.");
                }
            });
        }

        public static Player RestoreFP(NWPlayer oPC, int amount, Player entity)
        {
            entity.CurrentFP = entity.CurrentFP + amount;
            if (entity.CurrentFP > entity.MaxFP)
                entity.CurrentFP = entity.MaxFP;

            oPC.SendMessage(ColorTokenService.Custom("FP: " + entity.CurrentFP + " / " + entity.MaxFP, 32, 223, 219));

            return entity;
        }

        public static void RestoreFP(NWPlayer oPC, int amount)
        {
            Player entity = DataService.Get<Player>(oPC.GlobalID);
            RestoreFP(oPC, amount, entity);
            DataService.SubmitDataChange(entity, DatabaseActionType.Update);
        }

        private static void OnHitCastSpell()
        {
            NWPlayer oPC = Object.OBJECT_SELF;
            if (!oPC.IsValid) return;

            NWObject oTarget = _.GetSpellTargetObject();
            NWItem oItem = _.GetSpellCastItem();

            // If this method was triggered by our own armor (from getting hit), return. 
            if (oItem.BaseItemType == BASE_ITEM_ARMOR) return;

            // Flag this attack as physical so that the damage scripts treat it properly.
            LoggingService.Trace(TraceComponent.LastAttack, "Setting attack type from " + oPC.GlobalID + " against " + _.GetName(oTarget) + " to physical (" + ATTACK_PHYSICAL.ToString() + ")");
            oTarget.SetLocalInt(LAST_ATTACK + oPC.GlobalID, ATTACK_PHYSICAL);

            HandleGrenadeProficiency(oPC, oTarget);
            HandlePlasmaCellPerk(oPC, oTarget);
            int activeWeaponSkillID = oPC.GetLocalInt("ACTIVE_WEAPON_SKILL");
            if (activeWeaponSkillID <= 0) return;
            int activeWeaponSkillFeatID = oPC.GetLocalInt("ACTIVE_WEAPON_SKILL_FEAT_ID");
            if (activeWeaponSkillFeatID < 0) activeWeaponSkillFeatID = -1;

            PCPerk entity = DataService.GetAll<PCPerk>().Single(x => x.PlayerID == oPC.GlobalID && x.PerkID == activeWeaponSkillID);
            var perk = DataService.Get<Data.Entity.Perk>(entity.PerkID);
            var handler = PerkService.GetPerkHandler(activeWeaponSkillID);

            if (handler.CanCastSpell(oPC, oTarget))
            {
                handler.OnImpact(oPC, oTarget, entity.PerkLevel, activeWeaponSkillFeatID);

                if (oTarget.IsNPC)
                {
                    ApplyEnmity(oPC, oTarget.Object, perk);
                }
            }
            else oPC.SendMessage(handler.CannotCastSpellMessage(oPC, oTarget) ?? "That ability cannot be used at this time.");

            oPC.DeleteLocalString("ACTIVE_WEAPON_SKILL_UUID");
            oPC.DeleteLocalInt("ACTIVE_WEAPON_SKILL");
            oPC.DeleteLocalInt("ACTIVE_WEAPON_SKILL_FEAT_ID");

        }

        public static void HandlePlasmaCellPerk(NWPlayer player, NWObject target)
        {
            if (!player.IsPlayer) return;
            if (_.GetHasFeat((int)CustomFeatType.PlasmaCell, player) == FALSE) return;  // Check if player has the perk
            if (player.RightHand.CustomItemType != CustomItemType.BlasterPistol &&
                player.RightHand.CustomItemType != CustomItemType.BlasterRifle) return; // Check if player has the right weapons
            if (target.GetLocalInt("TRANQUILIZER_EFFECT_FIRST_RUN") == _.TRUE) return;   // Check if Tranquilizer is on to avoid conflict
            if (player.GetLocalInt("PLASMA_CELL_TOGGLE_OFF") == _.TRUE) return;  // Check if Plasma Cell toggle is on or off
            if (target.GetLocalInt("TRANQUILIZER_EFFECT_FIRST_RUN") == _.TRUE) return;

            int perkLevel = PerkService.GetPCPerkLevel(player, PerkType.PlasmaCell);
            int chance;
            CustomEffectType[] damageTypes;
            switch (perkLevel)
            {
                case 1:
                    chance = 10;
                    damageTypes = new[] { CustomEffectType.FireCell };
                    break;
                case 2:
                    chance = 10;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell };
                    break;
                case 3:
                    chance = 20;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell };
                    break;
                case 4:
                    chance = 20;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell, CustomEffectType.SonicCell };
                    break;
                case 5:
                    chance = 30;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell, CustomEffectType.SonicCell };
                    break;
                case 6:
                    chance = 30;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell, CustomEffectType.SonicCell, CustomEffectType.AcidCell };
                    break;
                case 7:
                    chance = 40;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell, CustomEffectType.SonicCell, CustomEffectType.AcidCell };
                    break;
                case 8:
                    chance = 40;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell, CustomEffectType.SonicCell, CustomEffectType.AcidCell, CustomEffectType.IceCell };
                    break;
                case 9:
                    chance = 50;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell, CustomEffectType.SonicCell, CustomEffectType.AcidCell, CustomEffectType.IceCell };
                    break;
                case 10:
                    chance = 50;
                    damageTypes = new[] { CustomEffectType.FireCell, CustomEffectType.ElectricCell, CustomEffectType.SonicCell, CustomEffectType.AcidCell, CustomEffectType.IceCell, CustomEffectType.DivineCell };
                    break;

                default: return;
            }

            foreach (var effect in damageTypes)
            {
                if (RandomService.D100(1) <= chance)
                {
                    CustomEffectService.ApplyCustomEffect(player, target.Object, effect, RandomService.D6(1), perkLevel, null);
                }
            }

        }

        private static void HandleGrenadeProficiency(NWPlayer oPC, NWObject target)
        {
            NWItem weapon = _.GetSpellCastItem();
            if (weapon.BaseItemType != BASE_ITEM_GRENADE) return;

            int perkLevel = PerkService.GetPCPerkLevel(oPC, PerkType.GrenadeProficiency);
            int chance = 10 * perkLevel;
            float duration;

            switch (perkLevel)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                    duration = 6;
                    break;
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                    duration = 9;
                    break;
                default: return;
            }


            if (RandomService.D100(1) <= chance)
            {
                _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectKnockdown(), target, duration);
            }
        }
    }
}
