﻿using System;
using System.Globalization;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Service
{
    public class CombatService : ICombatService
    {
        private readonly INWScript _;
        private readonly INWNXDamage _nwnxDamage;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;
        private readonly IAbilityService _ability;
        private readonly IEnmityService _enmity;
        private readonly IPlayerStatService _playerStat;
        private readonly ICustomEffectService _customEffect;
        private readonly IItemService _item;
        private readonly IColorTokenService _color;
        
        public CombatService(
            INWScript script,
            INWNXDamage nwnxDamage,
            IPerkService perk,
            IRandomService random,
            IAbilityService ability,
            IEnmityService enmity,
            IPlayerStatService playerStat,
            ICustomEffectService customEffect,
            IItemService item,
            IColorTokenService color)
        {
            _ = script;
            _nwnxDamage = nwnxDamage;
            _perk = perk;
            _random = random;
            _ability = ability;
            _enmity = enmity;
            _playerStat = playerStat;
            _customEffect = customEffect;
            _item = item;
            _color = color;
        }

        public void OnModuleApplyDamage()
        {
            HandleWeaponStatBonuses();
            HandleStances();
            HandleEvadeOrDeflectBlasterFire();
            HandleApplySneakAttackDamage();
            HandleBattlemagePerk();
            HandleAbsorptionFieldEffect();
            HandleRecoveryBlast();
            HandleTranquilizerEffect();
        }

        private void HandleWeaponStatBonuses()
        {
            DamageData data = _nwnxDamage.GetDamageEventData();
            if (data.Total <= 0) return;

            NWPlayer player = data.Damager.Object;
            NWItem weapon = _.GetLastWeaponUsed(player);
            
            if (weapon.CustomItemType == CustomItemType.BlasterPistol ||
                weapon.CustomItemType == CustomItemType.BlasterRifle)
            {
                int statBonus = (int)(player.DexterityModifier * 0.5f);
                data.Base += statBonus;
            }
            else if (weapon.CustomItemType == CustomItemType.Lightsaber ||
                     weapon.CustomItemType == CustomItemType.Saberstaff ||
                     weapon.GetLocalInt("LIGHTSABER") == TRUE)
            {
                int statBonus = (int) (player.CharismaModifier * 0.25f);
                data.Base += statBonus;
            }

            _nwnxDamage.SetDamageEventData(data);
        }

        private void HandleEvadeOrDeflectBlasterFire()
        {
            DamageData data = _nwnxDamage.GetDamageEventData();
            if (data.Total <= 0) return;
            NWCreature damager = data.Damager.Object;
            NWCreature target = Object.OBJECT_SELF;

            NWItem damagerWeapon = _.GetLastWeaponUsed(damager);
            NWItem targetWeapon = target.RightHand;

            int perkLevel;

            // Attacker isn't using a pistol or rifle. Return.
            if (damagerWeapon.CustomItemType != CustomItemType.BlasterPistol &&
                damagerWeapon.CustomItemType != CustomItemType.BlasterRifle)
            {
                return;
            }

            int modifier;
            string action;
            // Check target's equipped weapon, armor and perk.
            if (target.Chest.CustomItemType == CustomItemType.LightArmor &&
                (targetWeapon.CustomItemType == CustomItemType.MartialArtWeapon ||
                !target.RightHand.IsValid && !target.LeftHand.IsValid))
            {
                // Martial Arts (weapon or unarmed) uses the Evade Blaster Fire perk which is primarily DEX based.
                perkLevel = _perk.GetPCPerkLevel(target.Object, PerkType.EvadeBlasterFire);
                modifier = target.DexterityModifier;
                action = "evade";
            }
            else if (target.Chest.CustomItemType == CustomItemType.ForceArmor &&
                     (targetWeapon.CustomItemType == CustomItemType.Lightsaber ||
                     targetWeapon.CustomItemType == CustomItemType.Saberstaff ||
                      targetWeapon.GetLocalInt("LIGHTSABER") == TRUE))
            {
                // Lightsabers (lightsaber or saberstaff) uses the Deflect Blaster Fire perk which is primarily CHA based.
                perkLevel = _perk.GetPCPerkLevel(target.Object, PerkType.DeflectBlasterFire);
                modifier = target.CharismaModifier;
                action = "deflect";
            }
            else return;

            // Don't have the perk. Return.
            if (perkLevel <= 0) return;

            // Check attacker's DEX against the primary stat of the perk.
            int delta = modifier - damager.DexterityModifier;
            if (delta <= 0) return;
            
            // Has the delay between block/evade attempts past?
            DateTime cooldown = DateTime.UtcNow;
            string lastAttemptVar = target.GetLocalString("EVADE_OR_DEFLECT_BLASTER_FIRE_COOLDOWN");
            if (!string.IsNullOrWhiteSpace(lastAttemptVar))
                cooldown = DateTime.Parse(lastAttemptVar);

            // Cooldown hasn't expired yet. Not ready to attempt a deflect.
            if (cooldown >= DateTime.UtcNow) return;

            // Ready to attempt a deflect. Adjust chance based on the delta of attacker DEX versus primary stat of defender.
            int chanceToDeflect = 5 * delta;
            if (chanceToDeflect > 80) chanceToDeflect = 80;

            int delay; // Seconds delay between deflect attempts.
            switch (perkLevel)
            {
                case 1:
                    delay = 18;
                    break;
                case 2:
                    delay = 12;
                    break;
                case 3:
                    delay = 6;
                    break;
                default: throw new Exception("HandleEvadeOrDeflectBlasterFire -> Perk Level " + perkLevel + " unsupported.");
            }

            cooldown = DateTime.UtcNow.AddSeconds(delay);
            target.SetLocalString("EVADE_OR_DEFLECT_BLASTER_FIRE_COOLDOWN", cooldown.ToString(CultureInfo.InvariantCulture));

            int roll = _random.D100(1);

            if (roll <= chanceToDeflect)
            {
                target.SendMessage(_color.Gray("You " + action + " a blaster shot."));
                data.AdjustAllByPercent(-1);
                _nwnxDamage.SetDamageEventData(data);
            }
            else
            {
                target.SendMessage(_color.Gray("You fail to " + action + " a blaster shot. (" + roll + " vs " + chanceToDeflect + ")"));
            }
        }

        private void HandleBattlemagePerk()
        {
            DamageData data = _nwnxDamage.GetDamageEventData();
            if (data.Total <= 0) return;

            NWObject target = Object.OBJECT_SELF;
            if (!data.Damager.IsPlayer || !target.IsNPC) return;
            if (_.GetHasFeat((int)CustomFeatType.Battlemage, data.Damager.Object) == FALSE) return;

            NWPlayer player = data.Damager.Object;
            NWItem weapon = _.GetLastWeaponUsed(player.Object);
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

            if (restoreAmount > 0)
                _ability.RestoreFP(player, restoreAmount);
        }

        private void HandleApplySneakAttackDamage()
        {
            DamageData data = _nwnxDamage.GetDamageEventData();
            if (data.Total <= 0) return;
            NWObject damager = data.Damager;
            int sneakAttackType = damager.GetLocalInt("SNEAK_ATTACK_ACTIVE");

            if (damager.IsPlayer && sneakAttackType > 0)
            {
                NWPlayer player = damager.Object;
                NWCreature target = Object.OBJECT_SELF;
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
                float damageRate = 1.0f + perkRate + effectiveStats.SneakAttack * 0.05f;
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
            if (data.Total <= 0) return;
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

            _ability.RestoreFP(player, absorbed);
        }

        private void HandleRecoveryBlast()
        {
            DamageData data = _nwnxDamage.GetDamageEventData();
            NWObject damager = data.Damager;
            bool isActive = damager.GetLocalInt("RECOVERY_BLAST_ACTIVE") == TRUE;
            damager.DeleteLocalInt("RECOVERY_BLAST_ACTIVE");
            NWItem weapon = _.GetLastWeaponUsed(damager.Object);

            if (!isActive || weapon.CustomItemType != CustomItemType.BlasterRifle) return;

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
            NWItem damagerWeapon = _.GetLastWeaponUsed(damager);

            if (damager.IsPlayer)
            {
                CustomEffectType stance = _customEffect.GetCurrentStanceType(damager);

                switch (stance)
                {
                    case CustomEffectType.ShieldOath:
                        data.AdjustAllByPercent(-0.30f);
                        break;
                    case CustomEffectType.PrecisionTargeting:

                        if (damagerWeapon.CustomItemType == CustomItemType.BlasterPistol ||
                            damagerWeapon.CustomItemType == CustomItemType.BlasterRifle)
                        {
                            data.AdjustAllByPercent(0.20f);
                        }
                        break;
                }
            }
            
            _nwnxDamage.SetDamageEventData(data);
        }
    }
}
