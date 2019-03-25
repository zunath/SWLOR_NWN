using SWLOR.Game.Server.GameObject;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXVisibility
    {
        private const string NWNX_Visibility = "NWNX_Visibility";


        /// <summary>
        /// Queries the existing visibility override for given (player, target) pair
        /// If player is OBJECT_INVALID, the global visibility override will be returned
        ///
        /// Player == VALID -> returns:
        ///   NWNX_VISIBILITY_DEFAULT = Player override not set
        ///   NWNX_VISIBILITY_VISIBLE = Target is always visible for player
        ///   NWNX_VISIBILITY_HIDDEN  = Target is always hidden for player
        ///
        /// Player == OBJECT_INVALID -> returns:
        ///   NWNX_VISIBILITY_DEFAULT = Global override not set
        ///   NWNX_VISIBILITY_VISIBLE = Target is globally visible
        ///   NWNX_VISIBILITY_HIDDEN  = Target is globally hidden
        /// NWNX_VISIBILITY_DM_ONLY = Target is only visible to DMs
        /// </summary>
        /// <param name="player"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static VisibilityType GetVisibilityOverride(NWPlayer player, NWObject target)
        {
            string sFunc = "GetVisibilityOverride";

            NWNX_PushArgumentObject(NWNX_Visibility, sFunc, target);
            NWNX_PushArgumentObject(NWNX_Visibility, sFunc, player);
            NWNX_CallFunction(NWNX_Visibility, sFunc);

            return (VisibilityType)NWNX_GetReturnValueInt(NWNX_Visibility, sFunc);
        }

        /// <summary>
        /// 
        /// Overrides the default visibility rules about how player perceives the target object
        /// If player is OBJECT_INVALID, the global visibility override will be set
        ///
        /// Player == VALID -> override:
        ///   NWNX_VISIBILITY_DEFAULT = Remove the player override
        ///   NWNX_VISIBILITY_VISIBLE = Target is always visible for player
        ///   NWNX_VISIBILITY_HIDDEN  = Target is always hidden for player
        ///
        /// Player == OBJECT_INVALID -> override:
        ///   NWNX_VISIBILITY_DEFAULT = Remove the global override
        ///   NWNX_VISIBILITY_VISIBLE = Target is globally visible
        ///   NWNX_VISIBILITY_HIDDEN  = Target is globally hidden
        ///   NWNX_VISIBILITY_DM_ONLY = Target is only visible to DMs
        ///
        /// Note:
        /// Player state overrides the global state which means if a global state is set
        /// to NWNX_VISIBILITY_HIDDEN or NWNX_VISIBILITY_DM_ONLY but the player's state is
        /// set to NWNX_VISIBILITY_VISIBLE for the target, the object will be visible to the player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="target"></param>
        /// <param name="override"></param>
        public static void SetVisibilityOverride(NWPlayer player, NWObject target, VisibilityType @override)
        {
            string sFunc = "SetVisibilityOverride";

            NWNX_PushArgumentInt(NWNX_Visibility, sFunc, (int)@override);
            NWNX_PushArgumentObject(NWNX_Visibility, sFunc, target);
            NWNX_PushArgumentObject(NWNX_Visibility, sFunc, player);
            NWNX_CallFunction(NWNX_Visibility, sFunc);
        }

}
}
