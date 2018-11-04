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
        private readonly INWNXEvents _nwnxEvents;
        private readonly INWNXDamage _nwnxDamage;
        private readonly ICustomEffectService _customEffect;
        private readonly IPlayerStatService _playerStat;
        private readonly IItemService _item;

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
            INWNXEvents nwnxEvents,
            INWNXDamage nwnxDamage,
            ICustomEffectService customEffect,
            IPlayerStatService playerStat,
            IItemService item)
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
            _nwnxEvents = nwnxEvents;
            _nwnxDamage = nwnxDamage;
            _customEffect = customEffect;
            _playerStat = playerStat;
            _item = item;
        }
        
        public void OnModuleUseFeat()
        {
            NWPlayer pc = (Object.OBJECT_SELF);
            NWCreature target = (_nwnxEvents.OnFeatUsed_GetTarget().Object);
            int featID = _nwnxEvents.OnFeatUsed_GetFeatID();
            Data.Entity.Perk perk = _data.GetAll<Data.Entity.Perk>().SingleOrDefault(x => x.FeatID == featID);
            if (perk == null) return;

            App.ResolveByInterface<IPerk>("Perk." + perk.ScriptName, (perkAction) =>
            {
                if (perkAction == null) return;

                PlayerCharacter playerEntity =  _data.Get<PlayerCharacter>(pc.GlobalID);
                int pcPerkLevel = _perk.GetPCPerkLevel(pc, perk.PerkID);

                // If player is disabling an existing stance, remove that effect.
                if (perk.ExecutionTypeID == (int) PerkExecutionType.Stance)
                {
                    PCCustomEffect stanceEffect = _data.GetAll<PCCustomEffect>().SingleOrDefault(x =>
                    {
                        var customEffect = _data.Get<Data.Entity.CustomEffect>(x.CustomEffectID);

                        return x.PlayerID == pc.GlobalID &&
                               customEffect.CustomEffectCategoryID == (int) CustomEffectCategoryType.Stance;
                    });

                    if (stanceEffect != null && perk.PerkID == stanceEffect.StancePerkID)
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
                    if (!_pvpSanctuary.IsPVPAttackAllowed(pc, (target.Object))) return;
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

                int fpCost = perkAction.FPCost(pc, perkAction.FPCost(pc, perk.BaseFPCost));
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
                PCCooldown pcCooldown = _data.GetAll<PCCooldown>().SingleOrDefault(x => x.PlayerID == pc.GlobalID && x.CooldownCategoryID == perk.CooldownCategoryID);
                if (pcCooldown == null)
                {
                    pcCooldown = new PCCooldown
                    {
                        CooldownCategoryID = Convert.ToInt32(perk.CooldownCategoryID),
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
                    ActivateAbility(pc, target, perk, perkAction, pcPerkLevel, PerkExecutionType.ForceAbility);
                }
                // Combat Abilities
                else if (perk.ExecutionTypeID == (int)PerkExecutionType.CombatAbility)
                {
                    ActivateAbility(pc, target, perk, perkAction, pcPerkLevel, PerkExecutionType.CombatAbility);
                }
                // Queued Weapon Skills
                else if (perk.ExecutionTypeID == (int)PerkExecutionType.QueuedWeaponSkill)
                {
                    HandleQueueWeaponSkill(pc, perk, perkAction);
                }
                // Stances
                else if (perk.ExecutionTypeID == (int) PerkExecutionType.Stance)
                {
                    ActivateAbility(pc, target, perk, perkAction, pcPerkLevel, PerkExecutionType.Stance);
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
                               PerkExecutionType executionType)
        {
            string uuid = Guid.NewGuid().ToString("N");
            var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(pc);
            int itemBonus = effectiveStats.CastingSpeed;
            float baseActivationTime = perk.CastingTime(pc, (float)entity.BaseCastingTime);
            float activationTime = baseActivationTime;
            int vfxID = -1;
            int animationID = -1;

            // Activation Bonus % - Shorten activation time.
            if (itemBonus < 0)
            {
                float activationBonus = Math.Abs(itemBonus) * 0.01f;
                activationTime = activationTime - (activationTime * activationBonus);
            }
            // Activation Penalty % - Increase activation time.
            else if (itemBonus > 0)
            {
                float activationPenalty = Math.Abs(itemBonus) * 0.01f;
                activationTime = activationTime + (activationTime * activationPenalty);
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
                vfxID = VFX_DUR_ELEMENTAL_SHIELD;
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
            
            int perkID = entity.PerkID;
            pc.DelayEvent<FinishAbilityUse>(
                activationTime + 0.2f,
                pc,
                uuid,
                perkID,
                target,
                pcPerkLevel);
        }
        
        public void ApplyCooldown(NWPlayer pc, CooldownCategory cooldown, IPerk ability)
        {
            float finalCooldown = ability.CooldownTime(pc, (float)cooldown.BaseCooldownTime);
            int cooldownSeconds = (int)finalCooldown;
            int cooldownMillis = (int)((finalCooldown - cooldownSeconds) * 100);

            PCCooldown pcCooldown = _data.GetAll<PCCooldown>().Single(x => x.PlayerID == pc.GlobalID && x.CooldownCategoryID == cooldown.CooldownCategoryID);
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

        public void HandleQueueWeaponSkill(NWPlayer pc, Data.Entity.Perk entity, IPerk ability)
        {
            var cooldownCategory = _data.Get<CooldownCategory>(entity.CooldownCategoryID);
            string queueUUID = Guid.NewGuid().ToString("N");
            pc.SetLocalInt("ACTIVE_WEAPON_SKILL", entity.PerkID);
            pc.SetLocalString("ACTIVE_WEAPON_SKILL_UUID", queueUUID);
            pc.SendMessage("Weapon skill '" + entity.Name + "' queued for next attack.");

            ApplyCooldown(pc, cooldownCategory, ability);

            // Player must attack within 30 seconds after queueing or else it wears off.
            _.DelayCommand(30f, () =>
            {
                if (pc.GetLocalString("ACTIVE_WEAPON_SKILL_UUID") == queueUUID)
                {
                    pc.DeleteLocalInt("ACTIVE_WEAPON_SKILL");
                    pc.DeleteLocalString("ACTIVE_WEAPON_SKILL_UUID");
                    pc.SendMessage("Your weapon skill '" + entity.Name + "' is no longer queued.");
                }
            });
        }

        public PlayerCharacter RestoreFP(NWPlayer oPC, int amount, PlayerCharacter entity)
        {
            entity.CurrentFP = entity.CurrentFP + amount;
            if (entity.CurrentFP > entity.MaxFP)
                entity.CurrentFP = entity.MaxFP;

            oPC.SendMessage(_color.Custom("FP: " + entity.CurrentFP + " / " + entity.MaxFP, 32, 223, 219));

            return entity;
        }

        public void RestoreFP(NWPlayer oPC, int amount)
        {
            PlayerCharacter entity = _data.Get<PlayerCharacter>(oPC.GlobalID);
            RestoreFP(oPC, amount, entity);
            _data.SubmitDataChange(entity, DatabaseActionType.Update);
        }

        public void OnHitCastSpell(NWPlayer oPC)
        {
            NWObject oTarget = (_.GetSpellTargetObject());
            HandleGrenadeProficiency(oPC, oTarget);
            HandlePlasmaCellPerk(oPC, oTarget);
            int activeWeaponSkillID = oPC.GetLocalInt("ACTIVE_WEAPON_SKILL");
            if (activeWeaponSkillID <= 0) return;

            PCPerk entity = _data.GetAll<PCPerk>().Single(x => x.PlayerID == oPC.GlobalID && x.PerkID == activeWeaponSkillID);
            var perk = _data.Get<Data.Entity.Perk>(entity.PerkID);

            App.ResolveByInterface<IPerk>("Perk." + perk.ScriptName, (script) =>
            {
                if (script.CanCastSpell(oPC, oTarget))
                {
                    script.OnImpact(oPC, oTarget, entity.PerkLevel);
                    
                    if (oTarget.IsNPC)
                    {
                        ApplyEnmity(oPC, (oTarget.Object), perk);
                    }
                }
                else oPC.SendMessage(script.CannotCastSpellMessage(oPC, oTarget) ?? "That ability cannot be used at this time.");

                oPC.DeleteLocalString("ACTIVE_WEAPON_SKILL_UUID");
                oPC.DeleteLocalInt("ACTIVE_WEAPON_SKILL");
            });
        }

        public void HandlePlasmaCellPerk(NWPlayer player, NWObject target)
        {
            if (!player.IsPlayer) return;
            if (player.RightHand.CustomItemType != CustomItemType.BlasterPistol) return;

            PCCustomEffect pcEffect = _data.GetAll<PCCustomEffect>().SingleOrDefault(x => x.PlayerID == player.GlobalID && x.CustomEffectID == (int) CustomEffectType.PlasmaCell);
            if (pcEffect == null) return;

            int chance;
            CustomEffectType[] damageTypes;
            switch (pcEffect.EffectiveLevel)
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
                    _customEffect.ApplyCustomEffect(player, target.Object, effect, _random.D6(1), pcEffect.EffectiveLevel, null);
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

        public void OnModuleApplyDamage()
        {
            HandleStances();
            HandleApplySneakAttackDamage();
            HandleBattlemagePerk();
            HandleAbsorptionFieldEffect();
            HandleRecoveryBlast();
            HandleTranquilizerEffect();
        }

        private void HandleBattlemagePerk()
        {
            DamageData data = _nwnxDamage.GetDamageEventData();
            NWObject target = (Object.OBJECT_SELF);
            if (!data.Damager.IsPlayer || !target.IsNPC) return;
            if (_.GetHasFeat((int)CustomFeatType.Battlemage, data.Damager.Object) == FALSE) return;

            NWPlayer player = (data.Damager.Object);
            NWItem weapon = (_.GetLastWeaponUsed(player.Object));
            if (weapon.CustomItemType != CustomItemType.Baton) return;
            if (player.Chest.CustomItemType != CustomItemType.ForceArmor) return;

            int perkRank = _perk.GetPCPerkLevel(player, PerkType.Battlemage);

            int restoreAmount = 0;
            bool metRoll = _random.Random(100) + 1 <= 50;

            switch (perkRank)
            {
                case 1 when metRoll:
                    restoreAmount = 1;
                    break;
                case 2:
                    restoreAmount = 1;
                    break;
                case 3:
                    restoreAmount = 1;
                    if (metRoll) restoreAmount++;
                    break;
                case 4:
                    restoreAmount = 2;
                    break;
                case 5:
                    restoreAmount = 2;
                    if (metRoll) restoreAmount++;
                    break;
                case 6:
                    restoreAmount = 3;
                    break;
            }

            if(restoreAmount > 0)
                RestoreFP(player, restoreAmount);
        }

        private void HandleApplySneakAttackDamage()
        {
            DamageData data = _nwnxDamage.GetDamageEventData();
            NWObject damager = data.Damager;
            int sneakAttackType = damager.GetLocalInt("SNEAK_ATTACK_ACTIVE");

            if (damager.IsPlayer && sneakAttackType > 0)
            {
                NWPlayer player = (damager.Object);
                NWCreature target = (Object.OBJECT_SELF);
                int perkRank = _perk.GetPCPerkByID(damager.GlobalID, (int)PerkType.SneakAttack).PerkLevel;
                int perkBonus = 1;

                // Rank 4 increases damage bonus by 2x (total: 3x)
                if (perkRank == 4) perkBonus = 2;

                float perkRate;
                if (sneakAttackType == 1) // Player is behind target.
                {
                    perkRate = 1.0f * perkBonus;
                }
                else // Player is anywhere else.
                {
                    perkRate = 0.5f * perkBonus;
                }

                var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(player);
                float damageRate = 1.0f + perkRate + (effectiveStats.SneakAttack * 0.05f);
                data.Base = (int)(data.Base * damageRate);

                if (target.IsNPC)
                {
                    _enmity.AdjustEnmity(target, player, 5 * data.Base);
                }

                _nwnxDamage.SetDamageEventData(data);
            }

            damager.DeleteLocalInt("SNEAK_ATTACK_ACTIVE");
        }

        private void HandleAbsorptionFieldEffect()
        {
            DamageData data = _nwnxDamage.GetDamageEventData();
            NWObject target = Object.OBJECT_SELF;
            if (!target.IsPlayer) return;

            NWPlayer player = target.Object;
            int effectLevel = _customEffect.GetCustomEffectLevel(player, CustomEffectType.AbsorptionField);
            if (effectLevel <= 0) return;

            // Remove effect if player activates ability and removes the armor.
            if (player.Chest.CustomItemType != CustomItemType.ForceArmor)
            {
                _customEffect.RemovePCCustomEffect(player, CustomEffectType.AbsorptionField);
            }

            float absorptionRate = effectLevel * 0.1f;
            int absorbed = (int)(data.Total * absorptionRate);

            if (absorbed < 1) absorbed = 1;

            RestoreFP(player, absorbed);
        }

        private void HandleRecoveryBlast()
        {
            DamageData data = _nwnxDamage.GetDamageEventData();
            NWObject damager = data.Damager;
            bool isActive = damager.GetLocalInt("RECOVERY_BLAST_ACTIVE") == TRUE;
            if (!isActive) return;

            data.Bludgeoning = 0;
            data.Pierce = 0;
            data.Slash = 0;
            data.Magical = 0;
            data.Acid = 0;
            data.Cold = 0;
            data.Divine = 0;
            data.Electrical = 0;
            data.Fire = 0;
            data.Negative = 0;
            data.Positive = 0;
            data.Sonic = 0;
            data.Base = 0;

            _nwnxDamage.SetDamageEventData(data);
        }

        private void HandleTranquilizerEffect()
        {
            DamageData data = _nwnxDamage.GetDamageEventData();
            if (data.Total <= 0) return;
            NWObject self = Object.OBJECT_SELF;

            // Ignore the first damage because it occurred during the application of the effect.
            if (self.GetLocalInt("TRANQUILIZER_EFFECT_FIRST_RUN") > 0)
            {
                self.DeleteLocalInt("TRANQUILIZER_EFFECT_FIRST_RUN");
                return;
            }
            
            for (Effect effect = _.GetFirstEffect(self.Object); _.GetIsEffectValid(effect) == TRUE; effect = _.GetNextEffect(self.Object))
            {
                if (_.GetEffectTag(effect) == "TRANQUILIZER_EFFECT")
                {
                    _.RemoveEffect(self, effect);
                }
            }
        }

        private void HandleStances()
        {
            DamageData data = _nwnxDamage.GetDamageEventData();
            NWPlayer damager = data.Damager.Object;
            NWPlayer receiver = Object.OBJECT_SELF;
            NWItem damagerWeapon = _.GetLastWeaponUsed(damager);

            if (damager.IsPlayer)
            {
                CustomEffectType stance = _customEffect.GetCurrentStanceType(damager);

                switch (stance)
                {
                    case CustomEffectType.ShieldOath:
                        data.AdjustAllByPercent(-0.30f);
                        break;
                    case CustomEffectType.SwordOath:
                        
                        if (_item.MeleeWeaponTypes.Contains(damagerWeapon.BaseItemType))
                        {
                            data.AdjustAllByPercent(0.20f);
                        }
                        break;
                }
            }
            
            if (receiver.IsPlayer)
            {
                CustomEffectType stance = _customEffect.GetCurrentStanceType(receiver);
            }

            _nwnxDamage.SetDamageEventData(data);
        }

    }
}
