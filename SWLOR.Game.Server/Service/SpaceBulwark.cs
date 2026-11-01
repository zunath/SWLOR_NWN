using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// What a pilot's Bulwark perks add to a ship (plan section 6, Appendix A), ported from the ground Vibroblade
    /// and Heavy Vibroblade defense lines: the Shield Ram and Defiance "next weapon hit" riders, Anger Strike, the
    /// Critical Ward on incoming critical hits and Unbreakable when the hull drops low. Critical Ward, Unbreakable
    /// and Anger Strike set space-only stats (<c>Space*</c>), because the ground stats of the same perks add up
    /// with a pilot's ground perks and time their cooldowns on hit points.
    /// </summary>
    public static class SpaceBulwark
    {
        /// <summary>The rider Shield Ram arms: an extra instance of damage on the next weapon hit.</summary>
        public const string ShieldRamKey = "BulwarkShieldRam";

        /// <summary>The rider Defiance arms: bonus DMG and threat on the next weapon hit.</summary>
        public const string DefianceKey = "BulwarkDefiance";

        /// <summary>The rider Anger Strike arms after a Defiance hit: bonus DMG on the next weapon hit.</summary>
        public const string AngerStrikeKey = "BulwarkAngerStrike";

        /// <summary>How long the Defense a Defiance hit grants lasts, as the ground's Fortress Strike.</summary>
        public const float DefianceDefenseSeconds = 30f;

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void Subscribe()
        {
            SpaceCombatEvents.HullDamaged -= OnHullDamaged;
            SpaceCombatEvents.HullDamaged += OnHullDamaged;
            SpaceAbilities.RegisterRiderHandler(DefianceKey, OnDefianceHit);
        }

        // Riders

        /// <summary>
        /// Arms Shield Ram: the ship's next landed weapon hit also deals an instance of damage equal to a percent
        /// of the ship's Defense. Like the ground's one queued ability, it replaces a Defiance still waiting.
        /// </summary>
        public static void ArmShieldRam(uint ship, int defensePercent)
        {
            var status = Space.GetShipStatus(ship);
            if (status == null)
                return;

            ClearQueuedRiders(ship);
            SpaceAbilities.ArmRider(ship, new SpaceShotRider
            {
                Key = ShieldRamKey,
                ExtraDamage = SpaceCombatMath.PercentOfDefense(Space.GetShipDefense(ship, status), defensePercent),
            });
        }

        /// <summary>
        /// Arms Defiance: the ship's next landed weapon hit deals bonus DMG and generates extra threat, and gives
        /// the ship <paramref name="defensePercent"/> Defense for 30 s. It replaces a Shield Ram still waiting.
        /// </summary>
        public static void ArmDefiance(uint ship, int bonusDmg, int enmity, int defensePercent)
        {
            if (Space.GetShipStatus(ship) == null)
                return;

            ClearQueuedRiders(ship);
            SpaceAbilities.ArmRider(ship, new SpaceShotRider
            {
                Key = DefianceKey,
                BonusDmg = bonusDmg,
                ExtraEnmity = enmity,
                Amount = defensePercent,

                // The hit lands later, from a weapon cycle; Anger Strike reads the perks of whoever used Defiance.
                StatSource = SpaceCrew.GetAbilityUser(ship),
            });
        }

        private static void ClearQueuedRiders(uint ship)
        {
            SpaceAbilities.ClearRider(ship, ShieldRamKey);
            SpaceAbilities.ClearRider(ship, DefianceKey);
        }

        private static void OnDefianceHit(SpaceRiderContext e)
        {
            // The damage the hit dealt is threat too, through the pilot's damage enmity stat.
            Enmity.ModifyEnmity(e.Attacker, e.Target, Enmity.CalculateDamageEnmity(
                e.Result.Damage,
                Stat.GetStatAdjustment(e.Attacker, StatType.DamageEnmityPercentAdjustment)));

            var user = GetIsObjectValid(e.Rider.StatSource) ? e.Rider.StatSource : e.Attacker;
            var angerStrikeEnmity = Stat.GetStatAdjustment(user, StatType.SpaceDefianceEnmityBonus);
            if (angerStrikeEnmity > 0)
                Enmity.ModifyEnmity(e.Attacker, e.Target, angerStrikeEnmity);

            if (e.Rider.Amount > 0)
            {
                StatusEffect.ApplyStatusEffect(
                    e.Attacker, e.Attacker, new FortressStrikeStatusEffect(e.Rider.Amount), DefianceDefenseSeconds);
            }

            ArmAngerStrike(e.Attacker, user, e.Rider);
        }

        private static void ArmAngerStrike(uint ship, uint user, SpaceShotRider defiance)
        {
            var damage = Stat.GetStatAdjustment(user, StatType.SpaceDefianceNextAttackDamageBonus);
            var window = Stat.GetStatAdjustment(user, StatType.SpaceDefianceNextAttackDamageDurationSeconds);
            var usedAt = defiance.ExpiresAt.AddSeconds(-SpaceAbilities.RiderSeconds);
            var seconds = AngerStrikeSeconds(usedAt, DateTime.UtcNow, window);
            if (damage <= 0 || seconds <= 0f)
                return;

            SpaceAbilities.ArmRider(ship, new SpaceShotRider { Key = AngerStrikeKey, BonusDmg = damage }, seconds);
        }

        /// <summary>
        /// How long the Anger Strike bonus waits: "within <paramref name="windowSeconds"/> after using Defiance",
        /// counted from when Defiance was used. Zero or less when that time has passed.
        /// </summary>
        public static float AngerStrikeSeconds(DateTime defianceUsedAt, DateTime now, int windowSeconds) =>
            windowSeconds <= 0 ? 0f : (float)(defianceUsedAt.AddSeconds(windowSeconds) - now).TotalSeconds;

        // Critical Ward

        /// <summary>
        /// Whether a critical hit on this ship is downgraded to a normal hit that rolls minimum damage, and uses
        /// the ward: the ship needs the stat and the ward must be off its cooldown.
        /// </summary>
        public static bool TryWardCriticalHit(uint ship)
        {
            if (!GetIsObjectValid(ship) ||
                Stat.GetStatAdjustment(ship, StatType.SpaceIncomingCriticalHitDowngradeToMinimumDamage) <= 0)
            {
                return false;
            }

            var cooldown = Stat.GetStatAdjustment(ship, StatType.SpaceIncomingCriticalHitDowngradeCooldownMilliseconds);
            return Combat.TryUseStatTrigger(
                ship,
                StatType.SpaceIncomingCriticalHitDowngradeToMinimumDamage,
                TimeSpan.FromMilliseconds(cooldown));
        }

        // Unbreakable

        private static void OnHullDamaged(SpaceHullEvent e)
        {
            var ship = e.Ship;
            if (!GetIsObjectValid(ship))
                return;

            var threshold = Stat.GetStatAdjustment(ship, StatType.SpaceLowHullDefenseThresholdPercent);
            var defense = Stat.GetStatAdjustment(ship, StatType.SpaceLowHullDefensePercentAdjustment);
            var duration = Stat.GetStatAdjustment(ship, StatType.SpaceLowHullDefenseDurationSeconds);
            var cooldown = Stat.GetStatAdjustment(ship, StatType.SpaceLowHullDefenseCooldownSeconds);
            if (threshold <= 0 || defense == 0 || duration <= 0 ||
                !Space.IsShipInSpace(ship) ||
                !e.CrossedBelow(threshold) ||
                !Combat.TryUseStatTrigger(ship, StatType.SpaceLowHullDefensePercentAdjustment, cooldown))
            {
                return;
            }

            StatusEffect.ApplyStatusEffect(ship, ship, new UnbreakableStatusEffect(defense), duration);
        }

        /// <summary>Ends the immunities Invincible gave when the pilot leaves space, so none follow them onto the ground.</summary>
        public static void Clear(uint ship)
        {
            if (!StatusEffect.HasStatusEffect<InvincibleStatusEffect>(ship))
                return;

            Ability.RemoveTemporaryImmunity(ship, ImmunityType.Knockdown);
            Ability.RemoveTemporaryImmunity(ship, ImmunityType.Dazed);
        }
    }
}
