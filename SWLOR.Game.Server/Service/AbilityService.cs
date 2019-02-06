using System;
using System.Linq;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.Delayed;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Perk;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;
using PerkExecutionType = SWLOR.Game.Server.Enumeration.PerkExecutionType;

namespace SWLOR.Game.Server.Service
{
    public class AbilityService : IAbilityService
    {
        private readonly INWScript _;
        private readonly IDataService _data;
        private readonly IPerkService _perk;
        private readonly IPVPSanctuaryService _pvpSanctuary;
        private readonly ITimeService _time;
        private readonly IBiowarePosition _biowarePosition;
        private readonly INWNXPlayer _nwnxPlayer;
        private readonly IColorTokenService _color;
        private readonly IRandomService _random;
        private readonly IEnmityService _enmity;
        private readonly IErrorService _error;
        private readonly INWNXEvents _nwnxEvents;
        private readonly ICustomEffectService _customEffect;
        private readonly IPlayerStatService _playerStat;

        // These variables are used throughout the engine to flag the type of damage being done to 
        // a creature.  The damage code reads this to determine what bonus effects to apply.
        // The LAST_ATTACK variable name should be appended with the GlobalID of the attacking
        // (N)PC so that attacks from different creatures are treated correctly. 
        public static string LAST_ATTACK = "LAST_ATTACK_";

        public static int ATTACK_PHYSICAL = 1;  // Weapon attacks and weapon skills
        public static int ATTACK_FORCE = 2;  // Force effects
        public static int ATTACK_COMBATABILITY = 3; // Combat tricks like Provoke
        public static int ATTACK_DOT = 4; // Subsequent damage effects

        public AbilityService(INWScript script, 
            IDataService data,
            IPerkService perk,
            IPVPSanctuaryService pvpSanctuary,
            ITimeService time,
            IBiowarePosition biowarePosition,
            INWNXPlayer nwnxPlayer,
            IColorTokenService color,
            IRandomService random,
            IEnmityService enmity,
            IErrorService error,
            INWNXEvents nwnxEvents,
            ICustomEffectService customEffect,
            IPlayerStatService playerStat)
        {
            _ = script;
            _data = data;
            _perk = perk;
            _pvpSanctuary = pvpSanctuary;
            _time = time;
            _biowarePosition = biowarePosition;
            _nwnxPlayer = nwnxPlayer;
            _color = color;
            _random = random;
            _enmity = enmity;
            _error = error;
            _nwnxEvents = nwnxEvents;
            _customEffect = customEffect;
            _playerStat = playerStat;
        }
        
        public void OnModuleUseFeat()
        {
            NWPlayer pc = Object.OBJECT_SELF;
            NWCreature target = _nwnxEvents.OnFeatUsed_GetTarget().Object;
            int featID = _nwnxEvents.OnFeatUsed_GetFeatID();
            var perkFeat = _data.SingleOrDefault<PerkFeat>(x => x.FeatID == featID);
            if (perkFeat == null) return;
            Data.Entity.Perk perk = _data.GetAll<Data.Entity.Perk>().SingleOrDefault(x => x.ID == perkFeat.PerkID);
            if (perk == null) return;

            // Check to see if we are a spaceship.  Spaceships can't use abilities...
            if (pc.GetLocalInt("IS_SHIP") > 0 || pc.GetLocalInt("IS_GUNNER")>0)
            {
                pc.SendMessage("You cannot use that ability while piloting a ship.");
                return;
            }

            App.ResolveByInterface<IPerk>("Perk." + perk.ScriptName, (perkAction) =>
            {
                if (perkAction == null) return;

                Player playerEntity =  _data.Get<Player>(pc.GlobalID);
                int pcPerkLevel = _perk.GetPCPerkLevel(pc, perk.ID);

                // If player is disabling an existing stance, remove that effect.
                if (perk.ExecutionTypeID == (int) PerkExecutionType.Stance)
                {
                    PCCustomEffect stanceEffect = _data.GetAll<PCCustomEffect>().SingleOrDefault(x =>
                    {
                        var customEffect = _data.Get<Data.Entity.CustomEffect>(x.CustomEffectID);

                        return x.PlayerID == pc.GlobalID &&
                               customEffect.CustomEffectCategoryID == (int) CustomEffectCategoryType.Stance;
                    });

                    if (stanceEffect != null && perk.ID == stanceEffect.StancePerkID)
                    {
                        if (_customEffect.RemoveStance(pc))
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
                    if (!_pvpSanctuary.IsPVPAttackAllowed(pc, target.Object)) return;
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
                PCCooldown pcCooldown = _data.GetAll<PCCooldown>().SingleOrDefault(x => x.PlayerID == pc.GlobalID && 
                                                                                        x.CooldownCategoryID == cooldownCategoryID);
                if (pcCooldown == null)
                {
                    pcCooldown = new PCCooldown
                    {
                        CooldownCategoryID = Convert.ToInt32(cooldownCategoryID),
                        DateUnlocked = DateTime.UtcNow.AddSeconds(-1),
                        PlayerID = pc.GlobalID
                    };

                    _data.SubmitDataChange(pcCooldown, DatabaseActionType.Insert);
                }

                DateTime unlockDateTime = pcCooldown.DateUnlocked;
                DateTime now = DateTime.UtcNow;

                if (unlockDateTime > now)
                {
                    string timeToWait = _time.GetTimeToWaitLongIntervals(now, unlockDateTime, false);
                    pc.SendMessage("That ability can be used in " + timeToWait + ".");
                    return;
                }
                
                // Force Abilities (aka Spells)
                if (perk.ExecutionTypeID== (int)PerkExecutionType.ForceAbility)
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
                else if (perk.ExecutionTypeID == (int) PerkExecutionType.Stance)
                {
                    target.SetLocalInt(LAST_ATTACK + pc.GlobalID, ATTACK_COMBATABILITY);
                    ActivateAbility(pc, target, perk, perkAction, pcPerkLevel, PerkExecutionType.Stance, featID);
                }
            });
            
        }

        public void ApplyEnmity(NWPlayer pc, NWCreature target, Data.Entity.Perk perk)
        {
            switch ((EnmityAdjustmentRuleType)perk.EnmityAdjustmentRuleID)
            {
                case EnmityAdjustmentRuleType.AllTaggedTargets:
                    _enmity.AdjustEnmityOnAllTaggedCreatures(pc, perk.Enmity);
                    break;
                case EnmityAdjustmentRuleType.TargetOnly:
                    if (target.IsValid)
                    {
                        _enmity.AdjustEnmity(target, pc, perk.Enmity);
                    }
                    break;
                case EnmityAdjustmentRuleType.Custom:
                    App.ResolveByInterface<IPerk>("Perk." + perk.ScriptName, (perkAction) =>
                    {
                        perkAction?.OnCustomEnmityRule(pc, perk.Enmity);
                    });
                    break;
            }
        }
        
        private void ActivateAbility(NWPlayer pc,
                               NWObject target,
                               Data.Entity.Perk entity,
                               IPerk perk,
                               int pcPerkLevel,
                               PerkExecutionType executionType,
                               int spellFeatID)
        {
            string uuid = Guid.NewGuid().ToString();
            var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(pc);
            int itemBonus = effectiveStats.CastingSpeed;
            float baseActivationTime = perk.CastingTime(pc, (float)entity.BaseCastingTime, spellFeatID);
            float activationTime = baseActivationTime;
            int vfxID = -1;
            int animationID = -1;

            // Activation Bonus % - Shorten activation time.
            if (itemBonus < 0)
            {
                float activationBonus = Math.Abs(itemBonus) * 0.01f;
                activationTime = activationTime - activationTime * activationBonus;
            }
            // Activation Penalty % - Increase activation time.
            else if (itemBonus > 0)
            {
                float activationPenalty = Math.Abs(itemBonus) * 0.01f;
                activationTime = activationTime + activationTime * activationPenalty;
            }

            if (baseActivationTime > 0f && activationTime < 0.5f)
                activationTime = 0.5f;

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
            _biowarePosition.TurnToFaceObject(target, pc);

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

            _nwnxPlayer.StartGuiTimingBar(pc, (int)activationTime, "");
            
            int perkID = entity.ID;
            pc.DelayEvent<FinishAbilityUse>(activationTime + 0.2f,
                pc,
                uuid,
                perkID,
                target,
                pcPerkLevel,
                spellFeatID);
        }
        
        public void ApplyCooldown(NWPlayer pc, CooldownCategory cooldown, IPerk ability, int spellFeatID)
        {
            float finalCooldown = ability.CooldownTime(pc, (float)cooldown.BaseCooldownTime, spellFeatID);
            int cooldownSeconds = (int)finalCooldown;
            int cooldownMillis = (int)((finalCooldown - cooldownSeconds) * 100);

            PCCooldown pcCooldown = _data.GetAll<PCCooldown>().Single(x => x.PlayerID == pc.GlobalID && x.CooldownCategoryID == cooldown.ID);
            pcCooldown.DateUnlocked = DateTime.UtcNow.AddSeconds(cooldownSeconds).AddMilliseconds(cooldownMillis);
            _data.SubmitDataChange(pcCooldown, DatabaseActionType.Update);
        }

        private void CheckForSpellInterruption(NWPlayer pc, string spellUUID, Vector position)
        {
            if (pc.GetLocalInt(spellUUID) == (int) SpellStatusType.Completed) return;

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

                _nwnxPlayer.StopGuiTimingBar(pc, "", -1);
                pc.IsBusy = false;
                pc.SetLocalInt(spellUUID, (int)SpellStatusType.Interrupted);
                pc.SendMessage("Your ability has been interrupted.");
                return;
            }
            
            _.DelayCommand(0.5f, () => { CheckForSpellInterruption(pc, spellUUID, position); });
        }

        public void HandleQueueWeaponSkill(NWPlayer pc, Data.Entity.Perk entity, IPerk ability, int spellFeatID)
        {
            int? cooldownCategoryID = ability.CooldownCategoryID(pc, entity.CooldownCategoryID, spellFeatID);
            var cooldownCategory = _data.Get<CooldownCategory>(cooldownCategoryID);
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

        public Player RestoreFP(NWPlayer oPC, int amount, Player entity)
        {
            entity.CurrentFP = entity.CurrentFP + amount;
            if (entity.CurrentFP > entity.MaxFP)
                entity.CurrentFP = entity.MaxFP;

            oPC.SendMessage(_color.Custom("FP: " + entity.CurrentFP + " / " + entity.MaxFP, 32, 223, 219));

            return entity;
        }

        public void RestoreFP(NWPlayer oPC, int amount)
        {
            Player entity = _data.Get<Player>(oPC.GlobalID);
            RestoreFP(oPC, amount, entity);
            _data.SubmitDataChange(entity, DatabaseActionType.Update);
        }

        public void OnHitCastSpell(NWPlayer oPC)
        {
            NWObject oTarget = _.GetSpellTargetObject();

            // Flag this attack as physical so that the damage scripts treat it properly.
            _error.Trace(AbilityService.LAST_ATTACK, "Setting attack type from " + oPC.GlobalID + " against " + _.GetName(oTarget) + " to physical (" + ATTACK_PHYSICAL.ToString() + ")");
            oTarget.SetLocalInt(LAST_ATTACK + oPC.GlobalID, ATTACK_PHYSICAL);

            HandleGrenadeProficiency(oPC, oTarget);
            HandlePlasmaCellPerk(oPC, oTarget);
            int activeWeaponSkillID = oPC.GetLocalInt("ACTIVE_WEAPON_SKILL");
            if (activeWeaponSkillID <= 0) return;
            int activeWeaponSkillFeatID = oPC.GetLocalInt("ACTIVE_WEAPON_SKILL_FEAT_ID");
            if (activeWeaponSkillFeatID < 0) activeWeaponSkillFeatID = -1;

            PCPerk entity = _data.GetAll<PCPerk>().Single(x => x.PlayerID == oPC.GlobalID && x.PerkID == activeWeaponSkillID);
            var perk = _data.Get<Data.Entity.Perk>(entity.PerkID);
            
            App.ResolveByInterface<IPerk>("Perk." + perk.ScriptName, (script) =>
            {
                if (script.CanCastSpell(oPC, oTarget))
                {
                    script.OnImpact(oPC, oTarget, entity.PerkLevel, activeWeaponSkillFeatID);
                    
                    if (oTarget.IsNPC)
                    {
                        ApplyEnmity(oPC, oTarget.Object, perk);
                    }
                }
                else oPC.SendMessage(script.CannotCastSpellMessage(oPC, oTarget) ?? "That ability cannot be used at this time.");

                oPC.DeleteLocalString("ACTIVE_WEAPON_SKILL_UUID");
                oPC.DeleteLocalInt("ACTIVE_WEAPON_SKILL");
                oPC.DeleteLocalInt("ACTIVE_WEAPON_SKILL_FEAT_ID");
            });
        }

        public void HandlePlasmaCellPerk(NWPlayer player, NWObject target)
        {
            if (!player.IsPlayer) return;
            if (_.GetHasFeat((int)CustomFeatType.PlasmaCell, player) == FALSE) return;  // Check if player has the perk
            if (player.RightHand.CustomItemType != CustomItemType.BlasterPistol &&
                player.RightHand.CustomItemType != CustomItemType.BlasterRifle) return; // Check if player has the right weapons
            if (target.GetLocalInt("TRANQUILIZER_EFFECT_FIRST_RUN") == NWScript.TRUE) return;   // Check if Tranquilizer is on to avoid conflict
            if (player.GetLocalInt("PLASMA_CELL_TOGGLE_OFF") == NWScript.TRUE) return;  // Check if Plasma Cell toggle is on or off
            if (target.GetLocalInt("TRANQUILIZER_EFFECT_FIRST_RUN") == NWScript.TRUE) return;

            int perkLevel = _perk.GetPCPerkLevel(player, PerkType.PlasmaCell);
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
                if (_random.D100(1) <= chance)
                {
                    _customEffect.ApplyCustomEffect(player, target.Object, effect, _random.D6(1), perkLevel, null);
                }
            }

        }

        private void HandleGrenadeProficiency(NWPlayer oPC, NWObject target)
        {
            NWItem weapon = _.GetSpellCastItem();
            if (weapon.BaseItemType != BASE_ITEM_GRENADE) return;

            int perkLevel = _perk.GetPCPerkLevel(oPC, PerkType.GrenadeProficiency);
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


            if (_random.D100(1) <= chance)
            {
                _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, _.EffectKnockdown(), target, duration);
            }
        }
    }
}
