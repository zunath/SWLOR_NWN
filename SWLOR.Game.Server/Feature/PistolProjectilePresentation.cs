using System;
using System.Globalization;
using System.Numerics;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Canonical pistols use the native sling base item so NWN equips them in the right hand
    /// alongside shields. Replace only the sling projectile presentation with the existing
    /// single-emitter arrow blaster projectile; combat resolution and ammunition remain native.
    /// </summary>
    public static class PistolProjectilePresentation
    {
        private const int HighestWeaponProjectileType = 5;
        private const Spell ProjectileSpell = Spell.Trap_Arrow;
        private const string NoImpactScript = "****";
        private const string PistolShotSound = "cb_sh_blstrfire1";

        [NWNEventHandler(ScriptName.OnBroadcastSafeProjectileBefore)]
        public static void OnBroadcastSafeProjectile()
        {
            var attacker = OBJECT_SELF;
            var rightHand = GetItemInSlot(InventorySlot.RightHand, attacker);

            if (!int.TryParse(EventsPlugin.GetEventData("PROJECTILE_TYPE"), out var projectileType) ||
                !ShouldReplaceProjectile(projectileType, GetBaseItemType(rightHand)))
            {
                return;
            }

            if (!int.TryParse(EventsPlugin.GetEventData("DELTA"), out var deltaMilliseconds) ||
                deltaMilliseconds <= 0 ||
                !TryReadTargetPosition(out var targetPosition))
            {
                return;
            }

            var area = GetArea(attacker);
            if (!GetIsObjectValid(area))
            {
                return;
            }

            var targetLocation = Location(area, targetPosition, 0.0f);

            // Skip only the original sling visual. The attack, damage, and ammo consumption have
            // already been resolved independently by the engine.
            EventsPlugin.SkipEvent();
            CreaturePlugin.DoItemCastSpell(
                attacker,
                OBJECT_INVALID,
                targetLocation,
                ProjectileSpell,
                1,
                deltaMilliseconds / 1000.0f,
                ProjectilePathType.Default,
                ProjectileSpell,
                OBJECT_INVALID,
                NoImpactScript);
            AssignCommand(attacker, () => PlaySound(PistolShotSound));
        }

        private static bool ShouldReplaceProjectile(int projectileType, BaseItem rightHandBaseItem)
        {
            return projectileType is >= 0 and <= HighestWeaponProjectileType &&
                   rightHandBaseItem == BaseItem.Sling;
        }

        private static bool TryReadTargetPosition(out Vector3 targetPosition)
        {
            var style = NumberStyles.Float;
            var culture = CultureInfo.InvariantCulture;
            var hasX = float.TryParse(EventsPlugin.GetEventData("TARGET_POSITION_X"), style, culture, out var x);
            var hasY = float.TryParse(EventsPlugin.GetEventData("TARGET_POSITION_Y"), style, culture, out var y);
            var hasZ = float.TryParse(EventsPlugin.GetEventData("TARGET_POSITION_Z"), style, culture, out var z);

            targetPosition = new Vector3(x, y, z);
            return hasX && hasY && hasZ;
        }
    }
}
