using System;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Legacy.Enumeration;
using SWLOR.Game.Server.Legacy.Event.Module;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Messaging;
using SWLOR.Game.Server.Legacy.ValueObject;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;
using SkillType = SWLOR.Game.Server.Legacy.Enumeration.SkillType;

namespace SWLOR.Game.Server.Legacy.Service
{
    public static class CombatService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleApplyDamage>(message => OnModuleApplyDamage());
        }

        private static void OnModuleApplyDamage()
        {
            var data = Damage.GetDamageEventData();

            NWPlayer player = data.Damager;
            NWCreature target = OBJECT_SELF;

            var attackType = target.GetLocalInt(AbilityService.LAST_ATTACK + player.GlobalID);

            LoggingService.Trace(TraceComponent.LastAttack, "Last attack from " + player.GlobalID + " on " + GetName(target) + " was type " + attackType);

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
            var data = Damage.GetDamageEventData();
            if (data.Total <= 0) return;

            NWPlayer player = data.Damager;
            NWItem weapon = GetLastWeaponUsed(player);
            
            if (weapon.CustomItemType == CustomItemType.BlasterPistol)
            {
                var statBonus = (int)(player.DexterityModifier * 0.5f);
                data.Base += statBonus;
            }
            else if (weapon.CustomItemType == CustomItemType.BlasterRifle)
            {
                var statbonus = (int)(player.DexterityModifier * 0.6f);
                data.Base += statbonus;
            }
            else if (weapon.CustomItemType == CustomItemType.Lightsaber ||
                     weapon.CustomItemType == CustomItemType.Saberstaff ||
                     GetLocalBool(weapon, "LIGHTSABER"))
            {
                var statBonus = (int) (player.CharismaModifier * 0.25f);
                data.Base += statBonus;
            }

            Damage.SetDamageEventData(data);
        }

        private static void HandleEvadeOrDeflectBlasterFire()
        {
            var data = Damage.GetDamageEventData();
            if (data.Total <= 0) return;
            NWCreature damager = data.Damager;
            NWCreature target = OBJECT_SELF;

            NWItem damagerWeapon = GetLastWeaponUsed(damager);
            var targetWeapon = target.RightHand;

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
            if (targetWeapon.CustomItemType == CustomItemType.MartialArtWeapon ||
                (!target.RightHand.IsValid && !target.LeftHand.IsValid))
            {
                // Martial Arts (weapon or unarmed) uses the Evade Blaster Fire perk which is primarily DEX based.
                perkLevel = PerkService.GetCreaturePerkLevel(target.Object, PerkType.EvadeBlasterFire);
                modifier = target.DexterityModifier;
                action = "evade";
            }
            else if (targetWeapon.CustomItemType == CustomItemType.Lightsaber ||
                     targetWeapon.CustomItemType == CustomItemType.Saberstaff ||
                     GetLocalBool(targetWeapon, "LIGHTSABER"))
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
            var delta = modifier - damager.DexterityModifier;
            if (delta <= 0) return;
            
            // Has the delay between block/evade attempts past?
            var cooldown = DateTime.UtcNow;
            var lastAttemptVar = target.GetLocalString("EVADE_OR_DEFLECT_BLASTER_FIRE_COOLDOWN");
            if (!string.IsNullOrWhiteSpace(lastAttemptVar))
                cooldown = DateTime.Parse(lastAttemptVar);

            // Cooldown hasn't expired yet. Not ready to attempt a deflect.
            if (cooldown >= DateTime.UtcNow) return;

            // Ready to attempt a deflect. Adjust chance based on the delta of attacker DEX versus primary stat of defender.
            var chanceToDeflect = 5 * delta;
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

            var roll = SWLOR.Game.Server.Service.Random.D100(1);

            if (roll <= chanceToDeflect)
            {
                target.SendMessage(ColorToken.Gray("You " + action + " a blaster shot."));
                data.AdjustAllByPercent(-1);
                Damage.SetDamageEventData(data);
            }
            else
            {
                target.SendMessage(ColorToken.Gray("You fail to " + action + " a blaster shot. (" + roll + " vs " + chanceToDeflect + ")"));
            }
        }
        
        private static void HandleDamageImmunity()
        {
            var data = Damage.GetDamageEventData();
            if (data.Total <= 0) return;

            NWCreature target = OBJECT_SELF;
            var shield = target.LeftHand;
            var concentrationEffect = AbilityService.GetActiveConcentrationEffect(target);
            double reduction = 0.0f;

            // Shield damage reduction and absorb energy are calculated here. They don't stack, so the one
            // with the highest reduction will take precedence.

            // Calculate shield damage reduction.
            if (ItemService.ShieldBaseItemTypes.Contains(shield.BaseItemType))
            {
                // Apply damage scaling based on shield presence
                var perkLevel = PerkService.GetCreaturePerkLevel(target.Object, PerkType.ShieldProficiency);
                var perkBonus = 0.02f * perkLevel;

                // DI = 10% + 1% / 3 AC bonuses on the shield + 2% per perk bonus. 
                reduction = (0.1 + 0.01 * shield.AC / 3) + perkBonus;
            }
            // Calculate Absorb Energy concentration effect reduction.
            if (concentrationEffect.Type == PerkType.AbsorbEnergy)
            {
                var perkReduction = concentrationEffect.Tier * 0.1;
                if (perkReduction > reduction)
                {
                    reduction = perkReduction;
                    // Calculate and award force XP based on total damage reduced.
                    var xp = (int)(data.Total * 3);
                    if (xp < 5) xp = 5;

                    SkillService.GiveSkillXP(target.Object, SkillType.ForceControl, xp);
                    // Play a visual effect signifying the ability was activated.
                    ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Dur_Blur), target, 0.5f);
                }
            }
            //Shield Oath Damage Immunity
            NWPlayer player = OBJECT_SELF;
            if (target.IsPC)
            {
                if (CustomEffectService.GetCurrentStanceType(player) == CustomEffectType.ShieldOath)
                {
                    reduction += 0.2f;
                }
            }

            // No reduction found. Bail out early.
            if (reduction <= 0.0f) return;
            target.SendMessage("Total Damage: " + data.Total);
            target.SendMessage("Damage reduced by " + (int)(reduction * 100) + "%");
            reduction = 1.0f - reduction;

            data.Bludgeoning = (int)(data.Bludgeoning * reduction);
            data.Pierce = (int)(data.Pierce * reduction);
            data.Slash = (int)(data.Slash * reduction);
            data.Magical = (int)(data.Magical * reduction);
            data.Acid = (int)(data.Acid * reduction);
            data.Cold = (int)(data.Cold * reduction);
            //data.Divine = (int)(data.Divine * reduction); -- special damage types, such as force rage
            data.Electrical = (int)(data.Electrical * reduction);
            data.Fire = (int)(data.Fire * reduction);
            data.Negative = (int)(data.Negative * reduction);
            data.Positive = (int)(data.Positive * reduction);
            data.Sonic = (int)(data.Sonic * reduction);
            data.Base = (int)(data.Base * reduction);
            
            target.SendMessage("Total Damage: " + data.Total);

            Damage.SetDamageEventData(data);
        }

        private static void HandleApplySneakAttackDamage()
        {
            var data = Damage.GetDamageEventData();
            if (data.Total <= 0) return;
            NWObject damager = data.Damager;
            var sneakAttackType = damager.GetLocalInt("SNEAK_ATTACK_ACTIVE");

            if (damager.IsPlayer && sneakAttackType > 0)
            {
                NWPlayer player = damager.Object;
                NWCreature target = OBJECT_SELF;
                var pcPerk = PerkService.GetPCPerkByID(damager.GlobalID, (int) PerkType.SneakAttack);
                var perkRank = pcPerk?.PerkLevel ?? 0;
                var perkBonus = 1;

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
                var damageRate = 1.0f + perkRate + effectiveStats.SneakAttack * 0.05f;
                data.Base = (int)(data.Base * damageRate);

                if (target.IsNPC)
                {
                    EnmityService.AdjustEnmity(target, player, 5 * data.Base);
                }

                Damage.SetDamageEventData(data);
            }

            damager.DeleteLocalInt("SNEAK_ATTACK_ACTIVE");
        }

        private static void HandleAbsorptionFieldEffect()
        {
            var data = Damage.GetDamageEventData();
            if (data.Total <= 0) return;
            NWObject target = OBJECT_SELF;
            if (!target.IsPlayer) return;

            NWPlayer player = target.Object;
            var effectLevel = CustomEffectService.GetCustomEffectLevel(player, CustomEffectType.AbsorptionField);
            if (effectLevel <= 0) return;

            // Remove effect if player activates ability and removes the armor.
            if (player.Chest.CustomItemType != CustomItemType.ForceArmor)
            {
                CustomEffectService.RemovePCCustomEffect(player, CustomEffectType.AbsorptionField);
            }

            var absorptionRate = effectLevel * 0.1f;
            var absorbed = (int)(data.Total * absorptionRate);

            if (absorbed < 1) absorbed = 1;

            AbilityService.RestorePlayerFP(player, absorbed);
        }

        private static void HandleRecoveryBlast()
        {
            var data = Damage.GetDamageEventData();
            NWObject damager = data.Damager;
            var isActive = GetLocalBool(damager,"RECOVERY_BLAST_ACTIVE");
            damager.DeleteLocalInt("RECOVERY_BLAST_ACTIVE");
            NWItem weapon = GetLastWeaponUsed(damager.Object);

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

            Damage.SetDamageEventData(data);
        }

        private static void HandleTranquilizerEffect()
        {
            var data = Damage.GetDamageEventData();
            if (data.Total <= 0) return;
            NWObject self = OBJECT_SELF;

            // Ignore the first damage because it occurred during the application of the effect.
            if (self.GetLocalInt("TRANQUILIZER_EFFECT_FIRST_RUN") > 0)
            {
                self.DeleteLocalInt("TRANQUILIZER_EFFECT_FIRST_RUN");
                return;
            }

            for (var effect = GetFirstEffect(self.Object); GetIsEffectValid(effect) == true; effect = GetNextEffect(self.Object))
            {
                if (GetEffectTag(effect) == "TRANQUILIZER_EFFECT")
                {
                    RemoveEffect(self, effect);
                }
            }
        }

        private static void HandleStances()
        {
            var data = Damage.GetDamageEventData();
            NWPlayer damager = data.Damager;
            NWItem damagerWeapon = GetLastWeaponUsed(damager);

            if (damager.IsPlayer)
            {
                var stance = CustomEffectService.GetCurrentStanceType(damager);

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
            
            Damage.SetDamageEventData(data);
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
            AbilityType abilityScoreType;
            switch (skill)
            {
                case SkillType.ForceAlter:
                    abilityScoreType = AbilityType.Intelligence;
                    break;
                case SkillType.ForceControl:
                    abilityScoreType = AbilityType.Wisdom;
                    break;
                case SkillType.ForceSense:
                    abilityScoreType = AbilityType.Charisma;
                    break;
                default:
                    throw new ArgumentException("Invalid skill type called for " + nameof(CalculateAbilityResistance) + ", value '" + skill + "' not supported.");
            }


            var result = new AbilityResistanceResult();

            var attackerSkill = SkillService.GetPCSkillRank(attacker.Object, skill);
            var attackerAbility = GetAbilityModifier(abilityScoreType, attacker);

            var defenderSkill = SkillService.GetPCSkillRank(defender.Object, skill);
            var defenderAbility = GetAbilityModifier(abilityScoreType, defender);

            // If the defender is equipped with a lightsaber, we check their lightsaber skill
            if (defender.RightHand.CustomItemType == CustomItemType.Lightsaber ||
                defender.LeftHand.CustomItemType == CustomItemType.Lightsaber)
            {
                var lightsaberSkill = SkillService.GetPCSkillRank(defender.Object, SkillType.Lightsaber);
                if (lightsaberSkill > defenderSkill)
                    defenderSkill = lightsaberSkill;
            }

            // If the defender's martial arts skill is greater than the current skill they're using, we'll use that instead.
            var defenderMASkill = SkillService.GetPCSkillRank(defender.Object, SkillType.MartialArts);
            if (defenderMASkill > defenderSkill)
                defenderSkill = defenderMASkill;

            var attackerAffinity = 0; 
            var defenderAffinity = 0;

            // Only check affinity if ability has a force balance type.
            if (balanceType == ForceBalanceType.Dark || balanceType == ForceBalanceType.Light)
            {
                attackerAffinity = GetBalanceAffinity(attacker.Object, balanceType);
                defenderAffinity = GetBalanceAffinity(defender.Object, balanceType);
            }

            var attackerCR = attacker.IsPlayer ? 0f : attacker.ChallengeRating * 5f;
            var defenderCR = defender.IsPlayer ? 0f : defender.ChallengeRating * 5f;

            var attackerTotal = attackerSkill + attackerAbility + attackerAffinity + attackerCR;
            var defenderTotal = defenderSkill + defenderAbility + defenderAffinity + defenderCR;
            var divisor = attackerTotal + defenderTotal + 1; // +1 to prevent division by zero.

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

            result.DC = (int) (defenderTotal / divisor * 100);
            result.Roll = SWLOR.Game.Server.Service.Random.D100(1);

            if (sendRollMessage)
            {
                var resisted = result.IsResisted ? ColorToken.Red(" [RESISTED " + Math.Abs(result.Delta) + "%]") : string.Empty;
                var message = ColorToken.SavingThrow("Roll: " + result.Roll + " VS " + result.DC + " DC") + resisted;
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

            var balance = 0;
            foreach (var perkID in perkIDs)
            {
                var perk = DataService.Perk.GetByID(perkID.PerkID);
                if (perk.ForceBalance == ForceBalanceType.Universal) continue;
                var perkLevels = DataService.PerkLevel.GetAllByPerkID(perkID.PerkID)
                    .Where(x => x.Level <= perkID.PerkLevel);

                foreach (var perkLevel in perkLevels)
                {
                    var adjustment = perkLevel.Price / 2;
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
