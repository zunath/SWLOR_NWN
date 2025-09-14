using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Change the direction in which the camera is facing
        ///   - fDirection is expressed as anticlockwise degrees from Due East.
        ///   (0.0f=East, 90.0f=North, 180.0f=West, 270.0f=South)
        ///   A value of -1.0f for any parameter will be ignored and instead it will
        ///   use the current camera value.
        ///   This can be used to change the way the camera is facing after the player
        ///   emerges from an area transition.
        ///   - nTransitionType: CAMERA_TRANSITION_TYPE_*  SNAP will immediately move the
        ///   camera to the new position, while the other types will result in the camera moving gradually into position
        ///   Pitch and distance are limited to valid values for the current camera mode:
        ///   Top Down: Distance = 5-20, Pitch = 1-50
        ///   Driving camera: Distance = 6 (can't be changed), Pitch = 1-62
        ///   Chase: Distance = 5-20, Pitch = 1-50
        ///   *** NOTE *** In NWN:Hordes of the Underdark the camera limits have been relaxed to the following:
        ///   Distance 1-25
        ///   Pitch 1-89
        /// </summary>
        public static void SetCameraFacing(float fDirection, float fDistance = -1.0f, float fPitch = -1.0f,
            CameraTransitionType nTransitionType = CameraTransitionType.Snap)
        {
            global::NWN.Core.NWScript.SetCameraFacing(fDirection, fDistance, fPitch, (int)nTransitionType);
        }

        /// <summary>
        /// Sets oPlayer's camera limits that override any client configuration limits
        /// Value of -1.0 means use the client config instead
        /// NB: Like all other camera settings, this is not saved when saving the game
        /// </summary>
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
        /// Sets the object oPlayer's camera will be attached to.
        /// - oTarget: A valid creature or placeable. If oTarget is OBJECT_INVALID, it will revert the camera back to oPlayer's character.
        ///            The target must be known to oPlayer's client, this means it must be in the same area and within visible distance.
        ///              - SetObjectVisibleDistance() can be used to increase this range.
        ///              - If the target is a creature, it also must be within the perception range of oPlayer and perceived.
        /// - bFindClearView: if TRUE, the client will attempt to find a camera position where oTarget is in view.
        /// Notes:
        ///       - If oTarget gets destroyed while oPlayer's camera is attached to it, the camera will revert back to oPlayer's character.
        ///       - If oPlayer goes through a transition with its camera attached to a different object, it will revert back to oPlayer's character.
        ///       - The object the player's camera is attached to is not saved when saving the game.
        /// </summary>
        public static void AttachCamera(uint oPlayer, uint oTarget, bool bFindClearView = false)
        {
            global::NWN.Core.NWScript.AttachCamera(oPlayer, oTarget, bFindClearView ? 1 : 0);
        }

        /// <summary>
        /// Sets oPlayer's camera settings that override any client configuration settings
        /// nFlags is a bitmask of CAMERA_FLAG_* constants;
        /// NB: Like all other camera settings, this is not saved when saving the game
        /// </summary>
        public static void SetCameraFlags(uint oPlayer, int nFlags = 0)
        {
            global::NWN.Core.NWScript.SetCameraFlags(oPlayer, nFlags);
        }
    }
}
