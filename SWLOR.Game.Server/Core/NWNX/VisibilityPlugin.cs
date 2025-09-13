using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Core.NWNX
{
    public static class VisibilityPlugin
    {
        private const string PLUGIN_NAME = "NWNX_Visibility";

        // Queries the existing visibility override for given (player, target) pair
        // If player is OBJECT_INVALID, the global visibility override will be returned
        //
        // Player == VALID -> returns:
        //   NWNX_VISIBILITY_DEFAULT = Player override not set
        //   NWNX_VISIBILITY_VISIBLE = Target is always visible for player
        //   NWNX_VISIBILITY_HIDDEN  = Target is always hidden for player
        //
        // Player == OBJECT_INVALID -> returns:
        //   NWNX_VISIBILITY_DEFAULT = Global override not set
        //   NWNX_VISIBILITY_VISIBLE = Target is globally visible
        //   NWNX_VISIBILITY_HIDDEN  = Target is globally hidden
        // NWNX_VISIBILITY_DM_ONLY = Target is only visible to DMs

        public static VisibilityType GetVisibilityOverride(uint player, uint target)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "GetVisibilityOverride");
            NWNXPInvoke.NWNXPushObject(target);
            NWNXPInvoke.NWNXPushObject(player);
            NWNXPInvoke.NWNXCallFunction();
            return (VisibilityType)NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Overrides the default visibility rules about how player perceives the target object
        // If player is OBJECT_INVALID, the global visibility override will be set
        //
        // Player == VALID -> override:
        //   NWNX_VISIBILITY_DEFAULT = Remove the player override
        //   NWNX_VISIBILITY_VISIBLE = Target is always visible for player
        //   NWNX_VISIBILITY_HIDDEN  = Target is always hidden for player
        //
        // Player == OBJECT_INVALID -> override:
        //   NWNX_VISIBILITY_DEFAULT = Remove the global override
        //   NWNX_VISIBILITY_VISIBLE = Target is globally visible
        //   NWNX_VISIBILITY_HIDDEN  = Target is globally hidden
        //   NWNX_VISIBILITY_DM_ONLY = Target is only visible to DMs
        //
        // Note:
        // Player state overrides the global state which means if a global state is set
        // to NWNX_VISIBILITY_HIDDEN or NWNX_VISIBILITY_DM_ONLY but the player's state is
        // set to NWNX_VISIBILITY_VISIBLE for the target, the object will be visible to the player
        public static void SetVisibilityOverride(uint player, uint target, VisibilityType @override)
        {
            NWNXPInvoke.NWNXSetFunction(PLUGIN_NAME, "SetVisibilityOverride");
            NWNXPInvoke.NWNXPushInt((int)@override);
            NWNXPInvoke.NWNXPushObject(target);
            NWNXPInvoke.NWNXPushObject(player);
            NWNXPInvoke.NWNXCallFunction();
        }
    }
}