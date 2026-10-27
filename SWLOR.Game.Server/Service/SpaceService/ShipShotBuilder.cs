namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Builds the <see cref="SpaceWeaponShot"/> a revamp weapon step fires, with its size scaling and riders.
    /// </summary>
    public static class ShipShotBuilder
    {
        /// <summary>Extra damage of a Missile Launcher against a target with no shield.</summary>
        public const double MissileUnshieldedBonus = 1.2;

        /// <summary>Extra damage of a Point-Defense Turret against a Small target.</summary>
        public const double PointDefenseSmallBonus = 1.2;

        /// <summary>
        /// A weapon with a fixed size fires as that size with its Dmg as is. Any other weapon fires as the firing hull's
        /// size with its Dmg times that size's weapon multiplier. A preset NPC ship passes its fitted weapon's
        /// <paramref name="npcShot"/>, whose Dmg, Attack, Accuracy and Rank replace the step's with no size scaling;
        /// the family, damage type, flags and riders stay the step's.
        /// </summary>
        public static SpaceWeaponShot Build(
            ShipModuleStep step, ShipSize firingSize, ShipSize targetSize, int targetShield, ShipNpcWeapon npcShot = null)
        {
            var damage = (double)(npcShot?.Dmg ?? step.Dmg);
            if (npcShot == null && !step.FixedSize.HasValue)
                damage *= ShipStatCalculator.WeaponSizeMultiplier(firingSize);

            if (step.Family == ShipModuleFamily.MissileLauncher && targetShield <= 0)
                damage *= MissileUnshieldedBonus;

            if (step.Family == ShipModuleFamily.PointDefenseTurret &&
                (targetSize == ShipSize.Small || targetSize == ShipSize.Invalid))
            {
                damage *= PointDefenseSmallBonus;
            }

            return new SpaceWeaponShot
            {
                Family = step.WeaponFamily,
                DamageType = step.DamageType,
                Dmg = Math.Max(1, (int)Math.Round(damage, MidpointRounding.AwayFromZero)),
                Rank = npcShot?.Rank ?? step.Rank,
                Attack = npcShot?.Attack ?? step.Attack,
                Accuracy = npcShot?.Accuracy ?? step.Accuracy,
                Size = step.FixedSize,
                ShieldBypass = step.ShieldBypass,
                IgnoreSizePenalty = step.IgnoreSizePenalty,
                ShieldDamageMultiplier = step.ShieldDamageMultiplier > 0 ? step.ShieldDamageMultiplier : 1.0,
            };
        }

        /// <summary>The restored amount of a repairer, injector or aura step on a ship of this size.</summary>
        public static int ScaleSupport(int amount, ShipSize size)
        {
            return Math.Max(1, (int)Math.Round(amount * ShipStatCalculator.ModuleSizeMultiplier(size), MidpointRounding.AwayFromZero));
        }
    }
}
