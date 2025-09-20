using SWLOR.NWN.API.NWNX.Enum;

namespace SWLOR.NWN.API.NWNX
{
    public static class VisibilityPlugin
    {
        /// <summary>
        /// Queries the existing visibility override for given (player, target) pair.
        /// If player is OBJECT_INVALID, the global visibility override will be returned.
        /// </summary>
        /// <remarks>
        /// Player == VALID -&gt; returns:
        ///   NWNX_VISIBILITY_DEFAULT = Player override not set
        ///   NWNX_VISIBILITY_VISIBLE = Target is always visible for player
        ///   NWNX_VISIBILITY_HIDDEN  = Target is always hidden for player
        ///
        /// Player == OBJECT_INVALID -&gt; returns:
        ///   NWNX_VISIBILITY_DEFAULT = Global override not set
        ///   NWNX_VISIBILITY_VISIBLE = Target is globally visible
        ///   NWNX_VISIBILITY_HIDDEN  = Target is globally hidden
        ///   NWNX_VISIBILITY_DM_ONLY = Target is only visible to DMs
        /// </remarks>
        /// <param name="player">The PC Object or OBJECT_INVALID.</param>
        /// <param name="target">The object for which we're querying the visibility override.</param>
        /// <returns>The VisibilityType.</returns>
        public static VisibilityType GetVisibilityOverride(uint player, uint target)
        {
            int result = global::NWN.Core.NWNX.VisibilityPlugin.GetVisibilityOverride(player, target);
            return (VisibilityType)result;
        }

        /// <summary>
        /// Overrides the default visibility rules about how player perceives the target object.
        /// If player is OBJECT_INVALID, the global visibility override will be set.
        /// </summary>
        /// <remarks>
        /// Player == VALID -&gt; override:
        ///   NWNX_VISIBILITY_DEFAULT = Remove the player override
        ///   NWNX_VISIBILITY_VISIBLE = Target is always visible for player
        ///   NWNX_VISIBILITY_HIDDEN  = Target is always hidden for player
        ///
        /// Player == OBJECT_INVALID -&gt; override:
        ///   NWNX_VISIBILITY_DEFAULT = Remove the global override
        ///   NWNX_VISIBILITY_VISIBLE = Target is globally visible
        ///   NWNX_VISIBILITY_HIDDEN  = Target is globally hidden
        ///   NWNX_VISIBILITY_DM_ONLY = Target is only visible to DMs
        ///
        /// Player state overrides the global state which means if a global state is set
        /// to NWNX_VISIBILITY_HIDDEN or NWNX_VISIBILITY_DM_ONLY but the player's state is
        /// set to NWNX_VISIBILITY_VISIBLE for the target, the object will be visible to the player
        /// </remarks>
        /// <param name="player">The PC Object or OBJECT_INVALID.</param>
        /// <param name="target">The object for which we're altering the visibility.</param>
        /// <param name="override">The visibility type.</param>
        public static void SetVisibilityOverride(uint player, uint target, VisibilityType @override)
        {
            global::NWN.Core.NWNX.VisibilityPlugin.SetVisibilityOverride(player, target, (int)@override);
        }
    }
}