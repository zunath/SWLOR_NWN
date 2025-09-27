using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Changes the direction in which the camera is facing.
        /// </summary>
        /// <param name="fDirection">The direction expressed as anticlockwise degrees from Due East (0.0f=East, 90.0f=North, 180.0f=West, 270.0f=South)</param>
        /// <param name="fDistance">The camera distance. A value of -1.0f will use the current camera value (default: -1.0f)</param>
        /// <param name="fPitch">The camera pitch. A value of -1.0f will use the current camera value (default: -1.0f)</param>
        /// <param name="nTransitionType">The transition type (CAMERA_TRANSITION_TYPE_*). SNAP will immediately move the camera, while other types will move gradually (default: CameraTransitionType.Snap)</param>
        /// <remarks>This can be used to change the way the camera is facing after the player emerges from an area transition. Pitch and distance are limited to valid values for the current camera mode: Top Down: Distance = 5-20, Pitch = 1-50; Driving camera: Distance = 6 (can't be changed), Pitch = 1-62; Chase: Distance = 5-20, Pitch = 1-50. In NWN:Hordes of the Underdark the camera limits have been relaxed to: Distance 1-25, Pitch 1-89.</remarks>
        public static void SetCameraFacing(float fDirection, float fDistance = -1.0f, float fPitch = -1.0f,
            CameraTransitionType nTransitionType = CameraTransitionType.Snap)
        {
            global::NWN.Core.NWScript.SetCameraFacing(fDirection, fDistance, fPitch, (int)nTransitionType);
        }

        /// <summary>
        /// Sets the player's camera limits that override any client configuration limits.
        /// </summary>
        /// <param name="oPlayer">The player to set camera limits for</param>
        /// <param name="fMinPitch">The minimum pitch limit. Value of -1.0 means use the client config instead (default: -1.0f)</param>
        /// <param name="fMaxPitch">The maximum pitch limit. Value of -1.0 means use the client config instead (default: -1.0f)</param>
        /// <param name="fMinDist">The minimum distance limit. Value of -1.0 means use the client config instead (default: -1.0f)</param>
        /// <param name="fMaxDist">The maximum distance limit. Value of -1.0 means use the client config instead (default: -1.0f)</param>
        /// <remarks>Like all other camera settings, this is not saved when saving the game.</remarks>
        public static void SetCameraLimits(
            uint oPlayer,
            float fMinPitch = -1.0f,
            float fMaxPitch = -1.0f,
            float fMinDist = -1.0f,
            float fMaxDist = -1.0f)
        {
            global::NWN.Core.NWScript.SetCameraLimits(oPlayer, fMinPitch, fMaxPitch, fMinDist, fMaxDist);
        }

        /// <summary>
        /// Sets the object that the player's camera will be attached to.
        /// </summary>
        /// <param name="oPlayer">The player whose camera to attach</param>
        /// <param name="oTarget">A valid creature or placeable. If OBJECT_INVALID, it will revert the camera back to the player's character. The target must be known to the player's client (same area and within visible distance). If the target is a creature, it must also be within the player's perception range and perceived</param>
        /// <param name="bFindClearView">If true, the client will attempt to find a camera position where the target is in view (default: false)</param>
        /// <remarks>SetObjectVisibleDistance() can be used to increase the visible distance range. If the target gets destroyed while the player's camera is attached to it, the camera will revert back to the player's character. If the player goes through a transition with its camera attached to a different object, it will revert back to the player's character. The object the player's camera is attached to is not saved when saving the game.</remarks>
        public static void AttachCamera(uint oPlayer, uint oTarget, bool bFindClearView = false)
        {
            global::NWN.Core.NWScript.AttachCamera(oPlayer, oTarget, bFindClearView ? 1 : 0);
        }

        /// <summary>
        /// Sets the player's camera settings that override any client configuration settings.
        /// </summary>
        /// <param name="oPlayer">The player to set camera flags for</param>
        /// <param name="nFlags">A bitmask of CAMERA_FLAG_* constants (default: 0)</param>
        /// <remarks>Like all other camera settings, this is not saved when saving the game.</remarks>
        public static void SetCameraFlags(uint oPlayer, int nFlags = 0)
        {
            global::NWN.Core.NWScript.SetCameraFlags(oPlayer, nFlags);
        }
    }
}
