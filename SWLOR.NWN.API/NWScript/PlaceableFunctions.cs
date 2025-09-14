namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Uses the placeable object.
        /// </summary>
        /// <param name="oPlaceable">The placeable object to use</param>
        public static void ActionInteractObject(uint oPlaceable)
        {
            global::NWN.Core.NWScript.ActionInteractObject(oPlaceable);
        }

        /// <summary>
        /// Gets the last object that used the placeable object that is calling this function.
        /// </summary>
        /// <returns>The last object that used the placeable, or OBJECT_INVALID if called by something other than a placeable or door</returns>
        public static uint GetLastUsedBy()
        {
            return global::NWN.Core.NWScript.GetLastUsedBy();
        }

        /// <summary>
        /// Sets the status of the illumination for the placeable.
        /// Note: You must call RecomputeStaticLighting() after calling this function in
        /// order for the changes to occur visually for the players.
        /// SetPlaceableIllumination() buffers the illumination changes, which are then
        /// sent out to the players once RecomputeStaticLighting() is called. As such,
        /// it is best to call SetPlaceableIllumination() for all the placeables you wish
        /// to set the illumination on, and then call RecomputeStaticLighting() once after
        /// all the placeable illumination has been set.
        /// If the placeable is not a placeable object, or is a placeable that doesn't have a light, nothing will happen.
        /// </summary>
        /// <param name="oPlaceable">The placeable object (defaults to OBJECT_INVALID)</param>
        /// <param name="bIlluminate">If TRUE, the placeable's illumination will be turned on. If FALSE, it will be turned off</param>
        public static void SetPlaceableIllumination(uint oPlaceable = OBJECT_INVALID, bool bIlluminate = true)
        {
            global::NWN.Core.NWScript.SetPlaceableIllumination(oPlaceable, bIlluminate ? 1 : 0);
        }

        /// <summary>
        /// Returns TRUE if the illumination for the placeable is on.
        /// </summary>
        /// <param name="oPlaceable">The placeable object to check (defaults to OBJECT_INVALID)</param>
        /// <returns>TRUE if the illumination is on</returns>
        public static bool GetPlaceableIllumination(uint oPlaceable = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetPlaceableIllumination(oPlaceable) != 0;
        }

        /// <summary>
        /// Returns TRUE if the placeable action is valid for the placeable.
        /// </summary>
        /// <param name="oPlaceable">The placeable object</param>
        /// <param name="nPlaceableAction">PLACEABLE_ACTION_* constant</param>
        /// <returns>TRUE if the action is valid for the placeable</returns>
        public static int GetIsPlaceableObjectActionPossible(uint oPlaceable, int nPlaceableAction)
        {
            return global::NWN.Core.NWScript.GetIsPlaceableObjectActionPossible(oPlaceable, nPlaceableAction);
        }

        /// <summary>
        /// The caller performs the placeable action on the placeable.
        /// </summary>
        /// <param name="oPlaceable">The placeable object</param>
        /// <param name="nPlaceableAction">PLACEABLE_ACTION_* constant</param>
        public static void DoPlaceableObjectAction(uint oPlaceable, int nPlaceableAction)
        {
            global::NWN.Core.NWScript.DoPlaceableObjectAction(oPlaceable, nPlaceableAction);
        }
    }
}
