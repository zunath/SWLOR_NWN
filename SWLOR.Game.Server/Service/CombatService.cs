using System;
using System.Globalization;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;


using SWLOR.Game.Server.ValueObject;
using static NWN._;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Service
{
    public static class CombatService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleApplyDamage>(message => OnModuleApplyDamage());
        }

        private static void OnModuleApplyDamage()
        {
            DamageEventData data = NWNXDamage.GetDamageEventData();

            NWPlayer player = data.Damager.Object;
            NWCreature target = Object.OBJECT_SELF;

            int attackType = target.GetLocalInt(AbilityService.LAST_ATTACK + player.GlobalID);

            LoggingService.Trace(TraceComponent.LastAttack, "Last attack from " + player.GlobalID + " on " + _.GetName(target) + " was type " + attackType);

            if (attackType == AbilityService.ATTACK_PHYSICAL)
            {
                // Only apply bonus damage from physical attacks. 
                HandleWeaponStatBonuses();
                HandleEvadeOrDeflectBlasterFire();
                HandleApplySneakAttackDamage();
                HandleBattlemagePerk();
            }

            HandleShieldProtection();
            HandleAbsorptionFieldEffect();
            HandleRecoveryBlast();
            HandleTranquilizerEffect();
            HandleStances();
        }

        private static void HandleWeaponStatBonuses()
        {
            DamageEventData data = NWNXDamage.GetDamageEventData();
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

            NWNXDamage.SetDamageEventData(data);
        }

        private static void HandleEvadeOrDeflectBlasterFire()
        {
            DamageEventData data = NWNXDamage.GetDamageEventData();
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
                perkLevel = PerkService.GetPCPerkLevel(target.Object, PerkType.EvadeBlasterFire);
                modifier = target.DexterityModifier;
                action = "evade";
            }
            else if (target.Chest.CustomItemType == CustomItemType.ForceArmor &&
                     (targetWeapon.CustomItemType == CustomItemType.Lightsaber ||
                     targetWeapon.CustomItemType == CustomItemType.Saberstaff ||
                      targetWeapon.GetLocalInt("LIGHTSABER") == TRUE))
            {
                // Lightsabers (lightsaber or saberstaff) uses the Deflect Blaster Fire perk which is primarily CHA based.
                perkLevel = PerkService.GetPCPerkLevel(target.Object, PerkType.DeflectBlasterFire);
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

            int roll = RandomService.D100(1);

            if (roll <= chanceToDeflect)
            {
                target.SendMessage(ColorTokenService.Gray("You " + action + " a blaster shot."));
                data.AdjustAllByPercent(-1);
                NWNXDamage.SetDamageEventData(data);
            }
            else
            {
                target.SendMessage(ColorTokenService.Gray("You fail to " + action + " a blaster shot. (" + roll + " vs " + chanceToDeflect + ")"));
            }
        }

        private static void HandleBattlemagePerk()
        {
            DamageEventData data = NWNXDamage.GetDamageEventData();
            if (data.Base <= 0) return;

            NWObject target = Object.OBJECT_SELF;
            if (!data.Damager.IsPlayer || !target.IsNPC) return;
            if (_.GetHasFeat((int)CustomFeatType.Battlemage, data.Damager.Object) == FALSE) return;

            NWPlayer player = data.Damager.Object;
            NWItem weapon = _.GetLastWeaponUsed(player.Object);
            if (weapon.CustomItemType != CustomItemType.Baton) return;
            if (player.Chest.CustomItemType != CustomItemType.ForceArmor) return;

            int perkRank = PerkService.GetPCPerkLevel(player, PerkType.Battlemage);

            int restoreAmount = 0;
            bool metRoll = RandomService.Random(100) + 1 <= 50;

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
                AbilityService.RestoreFP(player, restoreAmount);
        }

        private void HandleShieldProtection()
        {
            DamageData data = _nwnxDamage.GetDamageEventData();
            if (data.Total <= 0) return;

            NWCreature target = Object.OBJECT_SELF;

            NWItem shield = target.LeftHand;

            if (ItemService.ShieldBaseItemTypes.Contains(shield.BaseItemType))
            {
                // Apply damage scaling based on shield presence
                // TODO add a line of perks here to make this more effective.
                // 5 perk ranks at 4% per rank or 10 at 20% per rank.

                // DI = 10% + 1% / 3 AC bonuses on the shield. 
                double damageMultiplier = 1.0 - (0.1 + 0.01 * shield.AC / 3);

                data.Bludgeoning = (int) (data.Bludgeoning * damageMultiplier);
                data.Pierce = (int)(data.Pierce * damageMultiplier);
                data.Slash = (int)(data.Slash * damageMultiplier);
                data.Magical = (int)(data.Magical * damageMultiplier);
                data.Acid = (int)(data.Acid * damageMultiplier);
                data.Cold = (int)(data.Cold * damageMultiplier);
                data.Divine = (int)(data.Divine * damageMultiplier);
                data.Electrical = (int)(data.Electrical * damageMultiplier);
                data.Fire = (int)(data.Fire * damageMultiplier);
                data.Negative = (int)(data.Negative * damageMultiplier);
                data.Positive = (int)(data.Positive * damageMultiplier);
                data.Sonic = (int)(data.Sonic * damageMultiplier);
                data.Base = (int)(data.Base * damageMultiplier);

                _nwnxDamage.SetDamageEventData(data);
            }
        }

        private void HandleApplySneakAttackDamage()
        {
            DamageEventData data = NWNXDamage.GetDamageEventData();
            if (data.Total <= 0) return;
            NWObject damager = data.Damager;
            int sneakAttackType = damager.GetLocalInt("SNEAK_ATTACK_ACTIVE");

            if (damager.IsPlayer && sneakAttackType > 0)
            {
                NWPlayer player = damager.Object;
                NWCreature target = Object.OBJECT_SELF;
                int perkRank = PerkService.GetPCPerkByID(damager.GlobalID, (int)PerkType.SneakAttack).PerkLevel;
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

                var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(player);
                float damageRate = 1.0f + perkRate + effectiveStats.SneakAttack * 0.05f;
                data.Base = (int)(data.Base * damageRate);

                if (target.IsNPC)
                {
                    EnmityService.AdjustEnmity(target, player, 5 * data.Base);
                }

                NWNXDamage.SetDamageEventData(data);
            }

            damager.DeleteLocalInt("SNEAK_ATTACK_ACTIVE");
        }

        private static void HandleAbsorptionFieldEffect()
        {
            DamageEventData data = NWNXDamage.GetDamageEventData();
            if (data.Total <= 0) return;
            NWObject target = Object.OBJECT_SELF;
            if (!target.IsPlayer) return;

            NWPlayer player = target.Object;
            int effectLevel = CustomEffectService.GetCustomEffectLevel(player, CustomEffectType.AbsorptionField);
            if (effectLevel <= 0) return;

            // Remove effect if player activates ability and removes the armor.
            if (player.Chest.CustomItemType != CustomItemType.ForceArmor)
            {
                CustomEffectService.RemovePCCustomEffect(player, CustomEffectType.AbsorptionField);
            }

            float absorptionRate = effectLevel * 0.1f;
            int absorbed = (int)(data.Total * absorptionRate);

            if (absorbed < 1) absorbed = 1;

            AbilityService.RestoreFP(player, absorbed);
        }

        private static void HandleRecoveryBlast()
        {
            DamageEventData data = NWNXDamage.GetDamageEventData();
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

            NWNXDamage.SetDamageEventData(data);
        }

        private static void HandleTranquilizerEffect()
        {
            DamageEventData data = NWNXDamage.GetDamageEventData();
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

        private static void HandleStances()
        {
            DamageEventData data = NWNXDamage.GetDamageEventData();
            NWPlayer damager = data.Damager.Object;
            NWItem damagerWeapon = _.GetLastWeaponUsed(damager);

            if (damager.IsPlayer)
            {
                CustomEffectType stance = CustomEffectService.GetCurrentStanceType(damager);

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
            
            NWNXDamage.SetDamageEventData(data);
        }

        private static int CalculateForceAccuracy(
            NWCreature caster, 
            NWCreature target,
            ForceAbilityType abilityType)
        {
            EffectiveItemStats casterItemStats = caster.IsPlayer ? 
                PlayerStatService.GetPlayerItemEffectiveStats(caster.Object) : 
                null;
            float casterPrimary;
            float casterSecondary;
            float casterItemAccuracy = casterItemStats?.ForceAccuracy ?? 0;

            EffectiveItemStats targetItemStats = target.IsPlayer ? 
                PlayerStatService.GetPlayerItemEffectiveStats(target.Object) : 
                null;
            float targetPrimary;
            float targetSecondary;
            float targetItemDefense = targetItemStats?.ForceDefense ?? 0;

            switch (abilityType)
            {
                case ForceAbilityType.Electrical:
                    casterPrimary = caster.Intelligence;
                    casterSecondary = caster.Wisdom;
                    targetPrimary = target.Intelligence;
                    targetSecondary = target.Wisdom;
                    targetItemDefense = targetItemDefense + targetItemStats?.ElectricalDefense ?? 0;
                    break;
                case ForceAbilityType.Dark:
                    casterPrimary = caster.Intelligence;
                    casterSecondary = caster.Wisdom;
                    targetPrimary = target.Wisdom;
                    targetSecondary = target.Intelligence;
                    targetItemDefense = targetItemDefense + targetItemStats?.DarkDefense ?? 0;
                    break;
                case ForceAbilityType.Mind:
                    casterPrimary = caster.Wisdom;
                    casterSecondary = caster.Intelligence;
                    targetPrimary = target.Wisdom;
                    targetSecondary = target.Intelligence;
                    targetItemDefense = targetItemDefense + targetItemStats?.MindDefense ?? 0;
                    break;
                case ForceAbilityType.Light:
                    casterPrimary = caster.Wisdom;
                    casterSecondary = caster.Intelligence;
                    targetPrimary = target.Intelligence;
                    targetSecondary = target.Wisdom;
                    targetItemDefense = targetItemDefense + targetItemStats?.ElectricalDefense ?? 0;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(abilityType), abilityType, null);
            }

            // Calculate accuracy based on the caster's primary and secondary stats. Add modifiers for overall item accuracy.
            float baseAccuracy = caster.Charisma * 0.25f + casterPrimary * 0.75f + casterSecondary * 0.5f + casterItemAccuracy * 0.15f;

            // Calculate defense based on target's primary and secondary stats. Add modifiers for specific defense types.
            float baseDefense = target.Charisma * 0.25f + targetPrimary * 0.75f + targetSecondary * 0.5f + targetItemDefense * 0.15f;

            // Temp defense increases whenever a hostile force ability is used. This is primarily a deterrant towards spamming the same ability over and over.
            string expiration = target.GetLocalString("TEMP_FORCE_DEFENSE_" + (int) abilityType);
            if(DateTime.TryParse(expiration, out var unused))
            {
                int tempDefense = target.GetLocalInt("TEMP_FORCE_DEFENSE_" + (int)abilityType);
                baseDefense += tempDefense;
            }


            float delta = baseAccuracy - baseDefense;
            float finalAccuracy = delta < 0 ?
                75 + (float)Math.Floor(delta / 2.0f) :
                75 + delta;

            // Accuracy cannot go above 95% or below 0%
            if (finalAccuracy > 95)
                finalAccuracy = 95;
            else if (finalAccuracy < 0)
                finalAccuracy = 0;

            return (int)finalAccuracy;
        }

        public static void AddTemporaryForceDefense(NWCreature target, ForceAbilityType forceAbility, int amount = 5, int length = 5)
        {
            if (amount <= 0) amount = 1;
            string variable = "TEMP_FORCE_DEFENSE_" + (int) forceAbility;
            int tempDefense = target.GetLocalInt(variable) + amount;
            string tempDateExpires = target.GetLocalString(variable);
            DateTime expirationDate = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(tempDateExpires))
            {
                expirationDate = DateTime.Parse(tempDateExpires);
            }

            expirationDate = expirationDate.AddSeconds(length);
            target.SetLocalString(variable, expirationDate.ToString(CultureInfo.InvariantCulture));
            target.SetLocalInt(variable, tempDefense);
        }

        public static ForceResistanceResult CalculateResistanceRating(
            NWCreature caster,
            NWCreature target,
            ForceAbilityType forceAbility)
        {
            int accuracy = CalculateForceAccuracy(caster, target, forceAbility);
            ForceResistanceResult result = new ForceResistanceResult();

            // Do four checks to see if the attacker overcomes the defender's
            // resistance.  The more checks you succeed, the lower the resistance. 
            int successes = 0;
            
            if (RandomService.D100(1) <= accuracy)
            {
                successes++;

                if (RandomService.D100(1) <= accuracy)
                {
                    successes++;

                    if (RandomService.D100(1) <= accuracy)
                    {
                        successes++;

                        if (RandomService.D100(1) <= accuracy)
                        {
                            successes++;
                        }
                    }
                }
            }

            switch (successes)
            {
                case 0:
                    result.Amount = 0f;
                    result.Type = ResistanceType.Full;
                    break;
                case 1:
                    result.Amount = 0.125f;
                    result.Type = ResistanceType.Eighth;
                    break;
                case 2:
                    result.Amount = 0.25f;
                    result.Type = ResistanceType.Fourth;
                    break;
                case 3:
                    result.Amount = 0.5f;
                    result.Type = ResistanceType.Half;
                    break;
                case 4:
                    result.Amount = 1.0f;
                    result.Type = ResistanceType.Zero;
                    break;
            }

            return result;
        }

        public static int CalculateItemPotencyBonus(NWCreature caster, ForceAbilityType abilityType)
        {
            if (!caster.IsPlayer) return 0;
            EffectiveItemStats itemStats = PlayerStatService.GetPlayerItemEffectiveStats(caster.Object);
            
            int itemBonus = itemStats.ForcePotency;
            switch (abilityType)
            {
                case ForceAbilityType.Electrical:
                    itemBonus += itemStats.ElectricalPotency;
                    break;
                case ForceAbilityType.Mind:
                    itemBonus += itemStats.MindPotency;
                    break;
                case ForceAbilityType.Light:
                    itemBonus += itemStats.LightPotency;
                    break;
                case ForceAbilityType.Dark:
                    itemBonus += itemStats.DarkPotency;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(abilityType), abilityType, null);
            }

            return itemBonus;
        }

        public static ForceDamageResult CalculateForceDamage(
            NWCreature caster,
            NWCreature target,
            ForceAbilityType abilityType,
            int basePotency,
            float tier1Modifier,
            float tier2Modifier,
            float tier3Modifier,
            float tier4Modifier)
        {
            ForceResistanceResult resistance = CalculateResistanceRating(caster, target, abilityType);
            int itemBonus = CalculateItemPotencyBonus(caster, abilityType);

            int casterPrimary = 0;
            int casterSecondary = 0;
            int targetPrimary = 0;
            int targetSecondary = 0;
            switch (abilityType)
            {
                case ForceAbilityType.Electrical:
                    casterPrimary = caster.Intelligence;
                    casterSecondary = caster.Wisdom;
                    targetPrimary = target.Intelligence;
                    targetSecondary = target.Wisdom;
                    break;
                case ForceAbilityType.Mind:
                    casterPrimary = caster.Wisdom;
                    casterSecondary = caster.Intelligence;
                    targetPrimary = target.Wisdom;
                    targetSecondary = target.Intelligence;
                    break;
                case ForceAbilityType.Light:
                    casterPrimary = caster.Wisdom;
                    casterSecondary = caster.Intelligence;
                    targetPrimary = target.Intelligence;
                    targetSecondary = target.Wisdom;
                    break;
                case ForceAbilityType.Dark:
                    casterPrimary = caster.Intelligence;
                    casterSecondary = caster.Wisdom;
                    targetPrimary = target.Wisdom;
                    targetSecondary = target.Intelligence;
                    break;
            }

            // Calculate delta between caster's primary/secondary stats and target's primary and secondary stats
            int delta = (int)((casterPrimary + casterSecondary * 0.5f) - (targetPrimary + targetSecondary * 0.5f));

            float multiplier;
            // Not every ability will have tiers 2-4. Default to the next lowest one if it's missing.
            if (delta <= 49 || tier2Modifier <= 0.0f)
            {
                multiplier = tier1Modifier;
            }
            else if (delta <= 99 || tier3Modifier <= 0.0f)
            {
                multiplier = tier2Modifier;
            }
            else if (delta <= 199 || tier4Modifier <= 0.0f) 
            {
                multiplier = tier3Modifier;
            }
            else
            {
                multiplier = tier4Modifier;
            }

            //caster.SendMessage("casterPrimary = " + casterPrimary + ", casterSecondary = " + casterSecondary + ", targetPrimary = " + targetPrimary + ", targetSecondary = " + targetSecondary);
            //caster.SendMessage("itemBonus = " + itemBonus + ", basePotency = " + basePotency + ", delta = " + delta + ", multiplier = " + multiplier + ", resistanceMultiplier = " + resistanceMultiplier);

            // Combine everything together to get the damage result.
            int damage = (int)((itemBonus + basePotency + (delta * multiplier)) * resistance.Amount);

            if (damage > 0)
                damage += RandomService.D8(1);

            if (damage <= 1)
                damage = 1;

            ForceDamageResult result = new ForceDamageResult
            {
                Damage = damage,
                Resistance = resistance,
                ItemBonus = itemBonus
            };

            // If this ability was resisted in any way, notify the caster.
            if (resistance.Type != ResistanceType.Zero)
            {
                string name = GetForceResistanceName(resistance.Type);
                caster.SendMessage("Your force ability was resisted. " + name);
            }

            return result;
        }

        private static string GetForceResistanceName(ResistanceType type)
        {
            switch (type)
            {
                case ResistanceType.Zero:
                    return string.Empty;
                case ResistanceType.Half:
                    return "(1/2)";
                case ResistanceType.Fourth:
                    return "(1/4)";
                case ResistanceType.Eighth:
                    return "(1/8)";
                case ResistanceType.Full:
                    return "(Fully Resisted)";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

    }
}
