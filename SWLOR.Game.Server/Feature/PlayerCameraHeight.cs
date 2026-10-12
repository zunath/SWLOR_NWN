using System;
using System.Globalization;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// The client places the camera from appearance.2da HEIGHT and ignores the creature's visual scale,
    /// so a player who changes their height would otherwise see the camera aimed lower or higher on their body.
    /// </summary>
    public static class PlayerCameraHeight
    {
        // The client's racial camera pivot sits this far below appearance.2da HEIGHT.
        private const float RacialPivotOffset = 0.5f;

        // Area loads and appearance type changes reset the camera after their server events fire.
        private const float ReapplyDelaySeconds = 1f;

        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void ReapplyOnAreaEnter()
        {
            ScheduleReapply(GetEnteringObject());
        }

        [NWNEventHandler(ScriptName.OnSpaceExit)]
        public static void ReapplyOnSpaceExit()
        {
            ScheduleReapply(OBJECT_SELF);
        }

        // A full model rebuild can reset the camera later than the delayed reapply, and screen shakes reset it too.
        [NWNEventHandler(ScriptName.OnPlayerHeartbeat)]
        public static void ReapplyOnHeartbeat()
        {
            Reapply(OBJECT_SELF);
        }

        /// <summary>
        /// Aims the player's camera at the same point on their body as at their species' normal height.
        /// Call after changing the player's height.
        /// </summary>
        public static void Apply(uint player)
        {
            if (!IsPlayer(player))
                return;

            SetCameraHeight(player, GetCameraHeight(player));
        }

        /// <summary>
        /// Returns the camera height for a body scaled from its species' appearance.2da HEIGHT,
        /// or 0 to keep the client's racial default.
        /// </summary>
        public static float CalculateCameraHeight(float racialHeight, float scale)
        {
            if (racialHeight <= RacialPivotOffset || scale <= 0f || Math.Abs(scale - 1f) < 0.001f)
                return 0f;

            return (racialHeight - RacialPivotOffset) * scale;
        }

        private static void ScheduleReapply(uint player)
        {
            if (!IsPlayer(player))
                return;

            AssignCommand(player, () => DelayCommand(ReapplyDelaySeconds, () => Reapply(player)));
        }

        // Resets only ever return the client to the racial default, so unresized players need nothing sent.
        private static void Reapply(uint player)
        {
            if (!IsPlayer(player))
                return;

            var height = GetCameraHeight(player);
            if (height > 0f)
                SetCameraHeight(player, height);
        }

        private static float GetCameraHeight(uint player)
        {
            var appearance = GetAppearanceType(player);
            if (!RacialAppearanceRegistry.TryGet(appearance, out _) ||
                !float.TryParse(Get2DAString("appearance", "HEIGHT", (int)appearance),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out var racialHeight))
                return 0f;

            return CalculateCameraHeight(racialHeight, GetObjectVisualTransform(player, ObjectVisualTransform.Scale));
        }

        private static bool IsPlayer(uint creature) =>
            GetIsObjectValid(creature) && GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);
    }
}
