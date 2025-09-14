namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Use oPlaceable.
        /// </summary>
        public static void ActionInteractObject(uint oPlaceable)
        {
            global::NWN.Core.NWScript.ActionInteractObject(oPlaceable);
        }

        /// <summary>
        ///   Get the last object that used the placeable object that is calling this function.
        ///   * Returns OBJECT_INVALID if it is called by something other than a placeable or
        ///   a door.
        /// </summary>
        public static uint GetLastUsedBy()
        {
            return global::NWN.Core.NWScript.GetLastUsedBy();
        }

        /// <summary>
        ///   Set the status of the illumination for oPlaceable.
        ///   - oPlaceable
        ///   - bIlluminate: if this is TRUE, oPlaceable's illumination will be turned on.
        ///   If this is FALSE, oPlaceable's illumination will be turned off.
        ///   Note: You must call RecomputeStaticLighting() after calling this function in
        ///   order for the changes to occur visually for the players.
        ///   SetPlaceableIllumination() buffers the illumination changes, which are then
        ///   sent out to the players once RecomputeStaticLighting() is called.  As such,
        ///   it is best to call SetPlaceableIllumination() for all the placeables you wish
        ///   to set the illumination on, and then call RecomputeStaticLighting() once after
        ///   all the placeable illumination has been set.
        ///   * If oPlaceable is not a placeable object, or oPlaceable is a placeable that
        ///   doesn't have a light, nothing will happen.
        /// </summary>
        public static void SetPlaceableIllumination(uint oPlaceable = OBJECT_INVALID, bool bIlluminate = true)
        {
            global::NWN.Core.NWScript.SetPlaceableIllumination(oPlaceable, bIlluminate ? 1 : 0);
        }

        /// <summary>
        ///   * Returns TRUE if the illumination for oPlaceable is on
        /// </summary>
        public static bool GetPlaceableIllumination(uint oPlaceable = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetPlaceableIllumination(oPlaceable) != 0;
        }

        /// <summary>
        ///   - oPlaceable
        ///   - nPlaceableAction: PLACEABLE_ACTION_*
        ///   * Returns TRUE if nPlacebleAction is valid for oPlaceable.
        /// </summary>
        public static int GetIsPlaceableObjectActionPossible(uint oPlaceable, int nPlaceableAction)
        {
            return global::NWN.Core.NWScript.GetIsPlaceableObjectActionPossible(oPlaceable, nPlaceableAction);
        }

        /// <summary>
        ///   The caller performs nPlaceableAction on oPlaceable.
        ///   - oPlaceable
        ///   - nPlaceableAction: PLACEABLE_ACTION_*
        /// </summary>
        public static void DoPlaceableObjectAction(uint oPlaceable, int nPlaceableAction)
        {
            global::NWN.Core.NWScript.DoPlaceableObjectAction(oPlaceable, nPlaceableAction);
        }
    }
}
