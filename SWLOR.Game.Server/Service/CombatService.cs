using System;
using System.Globalization;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;


using SWLOR.Game.Server.ValueObject;
using static NWN._;

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
            NWCreature target = NWGameObject.OBJECT_SELF;

            int attackType = target.GetLocalInt(AbilityService.LAST_ATTACK + player.GlobalID);

            LoggingService.Trace(TraceComponent.LastAttack, "Last attack from " + player.GlobalID + " on " + _.GetName(target) + " was type " + attackType);

            if (attackType == AbilityService.ATTACK_PHYSICAL)
            {
                // Only apply bonus damage from physical attacks. 
                HandleWeaponStatBonuses();
                HandleEvadeOrDeflectBlasterFire();
                HandleApplySneakAttackDamage();
            }

            HandleDamageImmunity();
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
            NWCreature target = NWGameObject.OBJECT_SELF;

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
                perkLevel = PerkService.GetCreaturePerkLevel(target.Object, PerkType.EvadeBlasterFire);
                modifier = target.DexterityModifier;
                action = "evade";
            }
            else if (target.Chest.CustomItemType == CustomItemType.ForceArmor &&
                     (targetWeapon.CustomItemType == CustomItemType.Lightsaber ||
                     targetWeapon.CustomItemType == CustomItemType.Saberstaff ||
                      targetWeapon.GetLocalInt("LIGHTSABER") == TRUE))
            {
                // Lightsabers (lightsaber or saberstaff) uses the Deflect Blaster Fire perk which is primarily CHA based.
                perkLevel = PerkService.GetCreaturePerkLevel(target.Object, PerkType.DeflectBlasterFire);
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
        
        private static void HandleDamageImmunity()
        {
            DamageEventData data = NWNXDamage.GetDamageEventData();
            if (data.Total <= 0) return;

            NWCreature target = NWGameObject.OBJECT_SELF;
            NWItem shield = target.LeftHand;
            var concentrationEffect = AbilityService.GetActiveConcentrationEffect(target);
            double reduction = 0.0f;

            // Shield damage reduction and absorb energy are calculated here. They don't stack, so the one
            // with the highest reduction will take precedence.

            // Calculate shield damage reduction.
            if (ItemService.ShieldBaseItemTypes.Contains(shield.BaseItemType))
            {
                // Apply damage scaling based on shield presence
                int perkLevel = PerkService.GetCreaturePerkLevel(target.Object, PerkType.ShieldProficiency);
                float perkBonus = 0.02f * perkLevel;

                // DI = 10% + 1% / 3 AC bonuses on the shield + 2% per perk bonus. 
                reduction = (0.1 + 0.01 * shield.AC / 3) + perkBonus;
            }
            // Calculate Absorb Energy concentration effect reduction.
            if (concentrationEffect.Type == PerkType.AbsorbEnergy)
            {
                double perkReduction = concentrationEffect.Tier * 0.1;
                if (perkReduction > reduction)
                {
                    reduction = perkReduction;
                    // Calculate and award force XP based on total damage reduced.
                    int xp = (int)(data.Total * reduction * 3);
                    if (xp < 5) xp = 5;

                    SkillService.GiveSkillXP(target.Object, SkillType.ForceControl, xp);
                    // Play a visual effect signifying the ability was activated.
                    _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_BLUR), target, 0.5f);
                }
            }

            // No reduction found. Bail out early.
            if (reduction <= 0.0f) return;

            target.SendMessage("Damage reduced by " + (int)(reduction * 100) + "%");
            reduction = 1.0f - reduction;

            data.Bludgeoning = (int)(data.Bludgeoning * reduction);
            data.Pierce = (int)(data.Pierce * reduction);
            data.Slash = (int)(data.Slash * reduction);
            data.Magical = (int)(data.Magical * reduction);
            data.Acid = (int)(data.Acid * reduction);
            data.Cold = (int)(data.Cold * reduction);
            data.Divine = (int)(data.Divine * reduction);
            data.Electrical = (int)(data.Electrical * reduction);
            data.Fire = (int)(data.Fire * reduction);
            data.Negative = (int)(data.Negative * reduction);
            data.Positive = (int)(data.Positive * reduction);
            data.Sonic = (int)(data.Sonic * reduction);
            data.Base = (int)(data.Base * reduction);

            NWNXDamage.SetDamageEventData(data);
        }

        private static void HandleApplySneakAttackDamage()
        {
            DamageEventData data = NWNXDamage.GetDamageEventData();
            if (data.Total <= 0) return;
            NWObject damager = data.Damager;
            int sneakAttackType = damager.GetLocalInt("SNEAK_ATTACK_ACTIVE");

            if (damager.IsPlayer && sneakAttackType > 0)
            {
                NWPlayer player = damager.Object;
                NWCreature target = NWGameObject.OBJECT_SELF;
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
            NWObject target = NWGameObject.OBJECT_SELF;
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

            AbilityService.RestorePlayerFP(player, absorbed);
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
            NWObject self = NWGameObject.OBJECT_SELF;

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

        /// <summary>
        /// Calculates ability resistance for an ability.
        /// The attacker and defender's skills, ability modifiers, and balance affinity will be
        /// used to make this determination.
        /// </summary>
        /// <param name="attacker">The creature using the ability.</param>
        /// <param name="defender">The creature being targeted by the ability.</param>
        /// <param name="skill">The skill used for this ability.</param>
        /// <param name="balanceType">The force balance type to use for this ability.</param>
        /// <param name="sendRollMessage">If true, the roll message will be sent. Otherwise it won't be.</param>
        /// <returns>Data regarding the ability resistance roll</returns>
        public static AbilityResistanceResult CalculateAbilityResistance(NWCreature attacker, NWCreature defender, SkillType skill, ForceBalanceType balanceType, bool sendRollMessage = true)
        {
            int abilityScoreType;
            switch (skill)
            {
                case SkillType.ForceAlter:
                    abilityScoreType = ABILITY_INTELLIGENCE;
                    break;
                case SkillType.ForceControl:
                    abilityScoreType = ABILITY_WISDOM;
                    break;
                case SkillType.ForceSense:
                    abilityScoreType = ABILITY_CHARISMA;
                    break;
                default:
                    throw new ArgumentException("Invalid skill type called for " + nameof(CalculateAbilityResistance) + ", value '" + skill + "' not supported.");
            }


            AbilityResistanceResult result = new AbilityResistanceResult();

            int attackerSkill = SkillService.GetPCSkillRank(attacker.Object, skill);
            int attackerAbility = _.GetAbilityModifier(abilityScoreType, attacker);

            int defenderSkill = SkillService.GetPCSkillRank(defender.Object, skill);
            int defenderAbility = _.GetAbilityModifier(abilityScoreType, defender);

            // If the defender is equipped with a lightsaber, we check their lightsaber skill
            if (defender.RightHand.CustomItemType == CustomItemType.Lightsaber ||
                defender.LeftHand.CustomItemType == CustomItemType.Lightsaber)
            {
                int lightsaberSkill = SkillService.GetPCSkillRank(defender.Object, SkillType.Lightsaber);
                if (lightsaberSkill > defenderSkill)
                    defenderSkill = lightsaberSkill;
            }

            // If the defender's martial arts skill is greater than the current skill they're using, we'll use that instead.
            int defenderMASkill = SkillService.GetPCSkillRank(defender.Object, SkillType.MartialArts);
            if (defenderMASkill > defenderSkill)
                defenderSkill = defenderMASkill;

            int attackerAffinity = 0; 
            int defenderAffinity = 0;

            // Only check affinity if ability has a force balance type.
            if (balanceType == ForceBalanceType.Dark || balanceType == ForceBalanceType.Light)
            {
                attackerAffinity = GetBalanceAffinity(attacker.Object, balanceType);
                defenderAffinity = GetBalanceAffinity(defender.Object, balanceType);
            }

            float attackerCR = attacker.IsPlayer ? 0f : attacker.ChallengeRating * 5f;
            float defenderCR = defender.IsPlayer ? 0f : defender.ChallengeRating * 5f;

            float attackerTotal = attackerSkill + attackerAbility + attackerAffinity + attackerCR;
            float defenderTotal = defenderSkill + defenderAbility + defenderAffinity + defenderCR;
            float divisor = attackerTotal + defenderTotal + 1; // +1 to prevent division by zero.

            //Console.WriteLine("attackerCR = " + attackerCR);
            //Console.WriteLine("defenderCR = " + defenderCR);
            //Console.WriteLine("attackerSkill = " + attackerSkill);
            //Console.WriteLine("attackerAbility = " + attackerAbility);
            //Console.WriteLine("attackerAffinity = " + attackerAffinity);
            //Console.WriteLine("defenderSkill = " + defenderSkill);
            //Console.WriteLine("defenderAbility = " + defenderAbility);
            //Console.WriteLine("defenderAffinity = " + defenderAffinity);
            //Console.WriteLine("attackerTotal = " + attackerTotal);
            //Console.WriteLine("defenderTotal = " + defenderTotal);
            //Console.WriteLine("divisor = " + divisor);

            result.DC = (int) (attackerTotal / divisor * 100);
            result.Roll = RandomService.D100(1);

            if (sendRollMessage)
            {
                string resisted = result.IsResisted ? ColorTokenService.Red(" [RESISTED " + Math.Abs(result.Delta) + "%]") : string.Empty;
                string message = ColorTokenService.SavingThrow("Roll: " + result.Roll + " VS " + result.DC + " DC") + resisted;
                attacker.SendMessage(message);
                defender.SendMessage(message);
            }

            return result;
        }

        private static int GetBalanceAffinity(NWPlayer player, ForceBalanceType balanceType)
        {
            if (!player.IsPlayer) return 0;

            var perkIDs = DataService.PCPerk.GetAllByPlayerID(player.GlobalID)
                .Select(s => new {s.PerkID, s.PerkLevel});

            int balance = 0;
            foreach (var perkID in perkIDs)
            {
                var perk = DataService.Perk.GetByID(perkID.PerkID);
                if (perk.ForceBalance == ForceBalanceType.Universal) continue;
                var perkLevels = DataService.PerkLevel.GetAllByPerkID(perkID.PerkID)
                    .Where(x => x.Level <= perkID.PerkLevel);

                foreach (var perkLevel in perkLevels)
                {
                    int adjustment = perkLevel.Price / 2;
                    if (adjustment < 1) adjustment = 1;

                    if (perk.ForceBalance == balanceType)
                    {
                        balance += adjustment;
                    }
                    else
                    {
                        balance -= adjustment;
                    }
                }
                
            }

            return balance;
        }
    }
}
