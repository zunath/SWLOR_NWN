using System;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Event;
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
        private readonly IDataContext _db;
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

        public AbilityService(INWScript script, 
            IDataContext db,
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
            IPlayerStatService playerStat)
        {
            _ = script;
            _db = db;
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
        }
        
        public void OnModuleUseFeat()
        {
            NWPlayer pc = (Object.OBJECT_SELF);
            NWCreature target = (_nwnxEvents.OnFeatUsed_GetTarget().Object);
            int featID = _nwnxEvents.OnFeatUsed_GetFeatID();
            Data.Entities.Perk perk = _db.Perks.SingleOrDefault(x => x.FeatID == featID);
            if (perk == null) return;

            App.ResolveByInterface<IPerk>("Perk." + perk.ScriptName, (perkAction) =>
            {
                if (perkAction == null) return;

                PlayerCharacter playerEntity = _db.PlayerCharacters.Single(x => x.PlayerID == pc.GlobalID);

                if (_perk.GetPCPerkLevel(pc, perk.PerkID) <= 0)
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
                PCCooldown pcCooldown = _db.PCCooldowns.SingleOrDefault(x => x.PlayerID == pc.GlobalID && x.CooldownCategoryID == perk.CooldownCategoryID);
                if (pcCooldown == null)
                {
                    pcCooldown = new PCCooldown
                    {
                        CooldownCategoryID = Convert.ToInt32(perk.CooldownCategoryID),
                        DateUnlocked = DateTime.UtcNow.AddSeconds(-1),
                        PlayerID = pc.GlobalID
                    };

                    _db.PCCooldowns.Add(pcCooldown);
                    _db.SaveChanges();
                }

                DateTime unlockDateTime = pcCooldown.DateUnlocked;
                DateTime now = DateTime.UtcNow;

                if (unlockDateTime > now)
                {
                    string timeToWait = _time.GetTimeToWaitLongIntervals(now, unlockDateTime, false);
                    pc.SendMessage("That ability can be used in " + timeToWait + ".");
                    return;
                }

                // Spells w/ casting time
                if (perk.PerkExecutionType.PerkExecutionTypeID == (int)PerkExecutionType.ForceAbility)
                {
                    CastSpell(pc, target, perk, perkAction);
                }
                // Combat Abilities w/o casting time
                else if (perk.PerkExecutionType.PerkExecutionTypeID == (int)PerkExecutionType.CombatAbility)
                {
                    perkAction.OnImpact(pc, target);
                    ApplyEnmity(pc, target, perk);

                    if (fpCost > 0)
                    {
                        playerEntity.CurrentFP = playerEntity.CurrentFP - fpCost;
                        _db.SaveChanges();
                    }
                    ApplyCooldown(pc, perk.CooldownCategory, perkAction);
                }
                // Queued Weapon Skills
                else if (perk.PerkExecutionType.PerkExecutionTypeID == (int)PerkExecutionType.QueuedWeaponSkill)
                {
                    HandleQueueWeaponSkill(pc, perk, perkAction);
                }
            });
            
        }

        public void ApplyEnmity(NWPlayer pc, NWCreature target, Data.Entities.Perk perk)
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

        private void CastSpell(NWPlayer pc,
                               NWObject target,
                               Data.Entities.Perk entity,
                               IPerk perk)
        {
            string spellUUID = Guid.NewGuid().ToString("N");
            int itemBonus = _playerStat.EffectiveCastingSpeed(pc);
            float baseCastingTime = perk.CastingTime(pc, (float)entity.BaseCastingTime);
            float castingTime = baseCastingTime;

            // Casting Bonus % - Shorten casting time.
            if (itemBonus < 0)
            {
                float castingPercentageBonus = Math.Abs(itemBonus) * 0.01f;
                castingTime = castingTime - (castingTime * castingPercentageBonus);
            }
            // Casting Penalty % - Increase casting time.
            else if (itemBonus > 0)
            {
                float castingPercentageBonus = Math.Abs(itemBonus) * 0.01f;
                castingTime = castingTime + (castingTime * castingPercentageBonus);
            }

            if (castingTime < 0.5f)
                castingTime = 0.5f;

            // Heavy armor increases casting time by 2x the base casting time
            if (pc.Chest.CustomItemType == CustomItemType.HeavyArmor)
            {
                castingTime = baseCastingTime * 2;
            }

            if (_.GetActionMode(pc.Object, ACTION_MODE_STEALTH) == 1)
                _.SetActionMode(pc.Object, ACTION_MODE_STEALTH, 0);

            _.ClearAllActions();
            _biowarePosition.TurnToFaceObject(target, pc);
            _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY,
                    _.EffectVisualEffect(VFX_DUR_ELEMENTAL_SHIELD),
                    pc.Object,
                    castingTime + 0.2f);

            float animationTime = castingTime;
            pc.AssignCommand(() => _.ActionPlayAnimation(ANIMATION_LOOPING_CONJURE1, 1.0f, animationTime - 0.1f));

            pc.IsBusy = true;
            CheckForSpellInterruption(pc, spellUUID, pc.Position);
            pc.SetLocalInt(spellUUID, (int)SpellStatusType.Started);

            _nwnxPlayer.StartGuiTimingBar(pc, (int)castingTime, "");

            pc.DelayEvent<FinishAbilityUse>(
                castingTime + 0.5f,
                pc,
                spellUUID,
                entity.PerkID,
                target);
        }
        
        public void ApplyCooldown(NWPlayer pc, CooldownCategory cooldown, IPerk ability)
        {
            float finalCooldown = ability.CooldownTime(pc, (float)cooldown.BaseCooldownTime);
            int cooldownSeconds = (int)finalCooldown;
            int cooldownMillis = (int)((finalCooldown - cooldownSeconds) * 100);

            PCCooldown pcCooldown = _db.PCCooldowns.Single(x => x.PlayerID == pc.GlobalID && x.CooldownCategoryID == cooldown.CooldownCategoryID);
            pcCooldown.DateUnlocked = DateTime.UtcNow.AddSeconds(cooldownSeconds).AddMilliseconds(cooldownMillis);
            _db.SaveChanges();
        }

        private void CheckForSpellInterruption(NWPlayer pc, string spellUUID, Vector position)
        {
            Vector currentPosition = pc.Position;

            if (currentPosition.m_X != position.m_X ||
                currentPosition.m_Y != position.m_Y ||
                currentPosition.m_Z != position.m_Z)
            {
                _nwnxPlayer.StopGuiTimingBar(pc, "", -1);
                pc.IsBusy = false;
                pc.SetLocalInt(spellUUID, (int)SpellStatusType.Interrupted);
                return;
            }
            
            _.DelayCommand(1.0f, () => { CheckForSpellInterruption(pc, spellUUID, position); });
        }

        public void HandleQueueWeaponSkill(NWPlayer pc, Data.Entities.Perk entity, IPerk ability)
        {
            string queueUUID = Guid.NewGuid().ToString("N");
            pc.SetLocalInt("ACTIVE_WEAPON_SKILL", entity.PerkID);
            pc.SetLocalString("ACTIVE_WEAPON_SKILL_UUID", queueUUID);
            pc.SendMessage("Weapon skill '" + entity.Name + "' queued for next attack.");

            ApplyCooldown(pc, entity.CooldownCategory, ability);

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
            PlayerCharacter entity = _db.PlayerCharacters.Single(x => x.PlayerID == oPC.GlobalID);
            ((IObjectContextAdapter)_db).ObjectContext.Refresh(RefreshMode.StoreWins, entity);
            RestoreFP(oPC, amount, entity);
            _db.SaveChanges();
        }

        public void OnHitCastSpell(NWPlayer oPC)
        {
            NWObject oTarget = (_.GetSpellTargetObject());
            HandleGrenadeProficiency(oPC, oTarget);
            int activeWeaponSkillID = oPC.GetLocalInt("ACTIVE_WEAPON_SKILL");
            if (activeWeaponSkillID <= 0) return;

            Data.Entities.Perk entity = _db.Perks.Single(x => x.PerkID == activeWeaponSkillID);

            App.ResolveByInterface<IPerk>("Perk." + entity.ScriptName, (perk) =>
            {
                if (perk.CanCastSpell(oPC, oTarget))
                {
                    perk.OnImpact(oPC, oTarget);

                    if (oTarget.IsNPC)
                    {
                        ApplyEnmity(oPC, (oTarget.Object), entity);
                    }
                }
                else oPC.SendMessage(perk.CannotCastSpellMessage(oPC, oTarget) ?? "That ability cannot be used at this time.");

                oPC.DeleteLocalString("ACTIVE_WEAPON_SKILL_UUID");
                oPC.DeleteLocalInt("ACTIVE_WEAPON_SKILL");
            });

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

                float damageRate = 1.0f + perkRate + (_playerStat.EffectiveSneakAttackBonus(player) * 0.05f);
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

    }
}
