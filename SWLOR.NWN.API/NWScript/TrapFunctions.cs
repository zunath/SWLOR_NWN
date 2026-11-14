using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Sets the calendar to the specified date.
        /// Time can only be advanced forwards; attempting to set the time backwards
        /// will result in no change to the calendar.
        /// If values larger than the month or day are specified, they will be wrapped
        /// around and the overflow will be used to advance the next field.
        /// e.g. Specifying a year of 1350, month of 33 and day of 10 will result in
        /// the calendar being set to a year of 1352, a month of 9 and a day of 10.
        /// </summary>
        /// <param name="nYear">Year should be from 0 to 32000 inclusive</param>
        /// <param name="nMonth">Month should be from 1 to 12 inclusive</param>
        /// <param name="nDay">Day should be from 1 to 28 inclusive</param>
        public static void SetCalendar(int nYear, int nMonth, int nDay)
        {
            NWScriptRuntime.Current.SetCalendar(nYear, nMonth, nDay);
        }

        /// <summary>
        /// Sets the time to the time specified.
        /// Time can only be advanced forwards; attempting to set the time backwards
        /// will result in the day advancing and then the time being set to that
        /// specified, e.g. if the current hour is 15 and then the hour is set to 3,
        /// the day will be advanced by 1 and the hour will be set to 3.
        /// If values larger than the max hour, minute, second or millisecond are
        /// specified, they will be wrapped around and the overflow will be used to
        /// advance the next field, e.g. specifying 62 hours, 250 minutes, 10 seconds
        /// and 10 milliseconds will result in the calendar day being advanced by 2
        /// and the time being set to 18 hours, 10 minutes, 10 milliseconds.
        /// </summary>
        /// <param name="nHour">Hour should be from 0 to 23 inclusive</param>
        /// <param name="nMinute">Minute should be from 0 to 59 inclusive</param>
        /// <param name="nSecond">Second should be from 0 to 59 inclusive</param>
        /// <param name="nMillisecond">Millisecond should be from 0 to 999 inclusive</param>
        public static void SetTime(int nHour, int nMinute, int nSecond, int nMillisecond)
        {
            NWScriptRuntime.Current.SetTime(nHour, nMinute, nSecond, nMillisecond);
        }

        /// <summary>
        /// Gets the current calendar year.
        /// </summary>
        /// <returns>The current calendar year</returns>
        public static int GetCalendarYear()
        {
            return NWScriptRuntime.Current.GetCalendarYear();
        }

        /// <summary>
        /// Gets the current calendar month.
        /// </summary>
        /// <returns>The current calendar month</returns>
        public static int GetCalendarMonth()
        {
            return NWScriptRuntime.Current.GetCalendarMonth();
        }

        /// <summary>
        /// Gets the current calendar day.
        /// </summary>
        /// <returns>The current calendar day</returns>
        public static int GetCalendarDay()
        {
            return NWScriptRuntime.Current.GetCalendarDay();
        }

        /// <summary>
        /// Sets whether or not the creature has detected the trapped object.
        /// </summary>
        /// <param name="oTrap">A trapped trigger, placeable or door object</param>
        /// <param name="oDetector">The creature that the detected status of the trap is being adjusted for</param>
        /// <param name="bDetected">A Boolean that sets whether the trapped object has been detected or not</param>
        /// <returns>1 if successful, 0 otherwise</returns>
        public static int SetTrapDetectedBy(uint oTrap, uint oDetector, bool bDetected = true)
        {
            return NWScriptRuntime.Current.SetTrapDetectedBy(oTrap, oDetector, bDetected);
        }

        /// <summary>
        /// Checks if the object is trapped.
        /// Note: Only placeables, doors and triggers can be trapped.
        /// </summary>
        /// <param name="oObject">The object to check</param>
        /// <returns>TRUE if the object is trapped</returns>
        public static bool GetIsTrapped(uint oObject)
        {
            return NWScriptRuntime.Current.GetIsTrapped(oObject);
        }

        /// <summary>
        /// Checks if the trap object is disarmable.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>TRUE if the trap object is disarmable</returns>
        public static bool GetTrapDisarmable(uint oTrapObject)
        {
            return NWScriptRuntime.Current.GetTrapDisarmable(oTrapObject);
        }

        /// <summary>
        /// Checks if the trap object is detectable.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>TRUE if the trap object is detectable</returns>
        public static bool GetTrapDetectable(uint oTrapObject)
        {
            return NWScriptRuntime.Current.GetTrapDetectable(oTrapObject);
        }

        /// <summary>
        /// Checks if the creature has detected the trap object.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>TRUE if the creature has detected the trap object</returns>
        public static bool GetTrapDetectedBy(uint oTrapObject, uint oCreature)
        {
            return NWScriptRuntime.Current.GetTrapDetectedBy(oTrapObject, oCreature);
        }

        /// <summary>
        /// Checks if the trap object has been flagged as visible to all creatures.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>TRUE if the trap object has been flagged as visible to all creatures</returns>
        public static bool GetTrapFlagged(uint oTrapObject)
        {
            return NWScriptRuntime.Current.GetTrapFlagged(oTrapObject);
        }

        /// <summary>
        /// Gets the trap base type of the trap object.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>The trap base type (TRAP_BASE_TYPE_*)</returns>
        public static TrapBaseType GetTrapBaseType(uint oTrapObject)
        {
            return NWScriptRuntime.Current.GetTrapBaseType(oTrapObject);
        }

        /// <summary>
        /// Checks if the trap object is one-shot (i.e. it does not reset itself after firing).
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>TRUE if the trap object is one-shot</returns>
        public static bool GetTrapOneShot(uint oTrapObject)
        {
            return NWScriptRuntime.Current.GetTrapOneShot(oTrapObject);
        }

        /// <summary>
        /// Gets the creator of the trap object, the creature that set the trap.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>The creator of the trap, or OBJECT_INVALID if the trap was created in the toolset</returns>
        public static uint GetTrapCreator(uint oTrapObject)
        {
            return NWScriptRuntime.Current.GetTrapCreator(oTrapObject);
        }

        /// <summary>
        /// Gets the tag of the key that will disarm the trap object.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>The tag of the key that will disarm the trap</returns>
        public static string GetTrapKeyTag(uint oTrapObject)
        {
            return NWScriptRuntime.Current.GetTrapKeyTag(oTrapObject);
        }

        /// <summary>
        /// Gets the DC for disarming the trap object.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>The DC for disarming the trap</returns>
        public static int GetTrapDisarmDC(uint oTrapObject)
        {
            return NWScriptRuntime.Current.GetTrapDisarmDC(oTrapObject);
        }

        /// <summary>
        /// Gets the DC for detecting the trap object.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>The DC for detecting the trap</returns>
        public static int GetTrapDetectDC(uint oTrapObject)
        {
            return NWScriptRuntime.Current.GetTrapDetectDC(oTrapObject);
        }

        /// <summary>
        /// Gets the trap nearest to the target.
        /// Note: "trap objects" are actually any trigger, placeable or door that is
        /// trapped in the target's area.
        /// </summary>
        /// <param name="oTarget">The target object (defaults to OBJECT_SELF)</param>
        /// <param name="nTrapDetected">If this is TRUE, the trap returned has to have been detected by the target</param>
        /// <returns>The nearest trap to the target</returns>
        public static uint GetNearestTrapToObject(uint oTarget = OBJECT_INVALID, bool nTrapDetected = true)
        {
            return NWScriptRuntime.Current.GetNearestTrapToObject(oTarget, nTrapDetected);
        }

        /// <summary>
        /// Gets the last trap detected by the target.
        /// </summary>
        /// <param name="oTarget">The target object (defaults to OBJECT_SELF)</param>
        /// <returns>The last trap detected by the target, or OBJECT_INVALID on error</returns>
        public static uint GetLastTrapDetected(uint oTarget = OBJECT_INVALID)
        {
            return NWScriptRuntime.Current.GetLastTrapDetected(oTarget);
        }

        /// <summary>
        /// Checks if the trap object is active.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>TRUE if the trap object is active</returns>
        public static bool GetTrapActive(uint oTrapObject)
        {
            return NWScriptRuntime.Current.GetTrapActive(oTrapObject);
        }

        /// <summary>
        /// Sets whether or not the trap is an active trap.
        /// Setting a trap as inactive will not make the trap disappear if it has already been detected.
        /// Call SetTrapDetectedBy() to make a detected trap disappear.
        /// To make an inactive trap not detectable call SetTrapDetectable().
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="nActive">TRUE/FALSE</param>
        public static void SetTrapActive(uint oTrapObject, bool nActive = true)
        {
            NWScriptRuntime.Current.SetTrapActive(oTrapObject, nActive);
        }

        /// <summary>
        /// Checks if the trap object can be recovered.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>TRUE if the trap object can be recovered</returns>
        public static bool GetTrapRecoverable(uint oTrapObject)
        {
            return NWScriptRuntime.Current.GetTrapRecoverable(oTrapObject);
        }

        /// <summary>
        /// Sets whether or not the trapped object can be recovered.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="nRecoverable">TRUE/FALSE</param>
        public static void SetTrapRecoverable(uint oTrapObject, bool nRecoverable = true)
        {
            NWScriptRuntime.Current.SetTrapRecoverable(oTrapObject, nRecoverable);
        }

        /// <summary>
        /// Sets whether or not the trapped object can be disarmed.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="nDisarmable">TRUE/FALSE</param>
        public static void SetTrapDisarmable(uint oTrapObject, bool nDisarmable = true)
        {
            NWScriptRuntime.Current.SetTrapDisarmable(oTrapObject, nDisarmable);
        }

        /// <summary>
        /// Sets whether or not the trapped object can be detected.
        /// Note: Setting a trapped object to not be detectable will
        /// not make the trap disappear if it has already been detected.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="nDetectable">TRUE/FALSE</param>
        public static void SetTrapDetectable(uint oTrapObject, bool nDetectable = true)
        {
            NWScriptRuntime.Current.SetTrapDetectable(oTrapObject, nDetectable);
        }

        /// <summary>
        /// Sets whether or not the trap is a one-shot trap
        /// (i.e. whether or not the trap resets itself after firing).
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="nOneShot">TRUE/FALSE</param>
        public static void SetTrapOneShot(uint oTrapObject, bool nOneShot = true)
        {
            NWScriptRuntime.Current.SetTrapOneShot(oTrapObject, nOneShot);
        }

        /// <summary>
        /// Sets the tag of the key that will disarm the trap object.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="sKeyTag">The tag of the key that will disarm the trap</param>
        public static void SetTrapKeyTag(uint oTrapObject, string sKeyTag)
        {
            NWScriptRuntime.Current.SetTrapKeyTag(oTrapObject, sKeyTag);
        }

        /// <summary>
        /// Sets the DC for disarming the trap object.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="nDisarmDC">Must be between 0 and 250</param>
        public static void SetTrapDisarmDC(uint oTrapObject, int nDisarmDC)
        {
            NWScriptRuntime.Current.SetTrapDisarmDC(oTrapObject, nDisarmDC);
        }

        /// <summary>
        /// Sets the DC for detecting the trap object.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="nDetectDC">Must be between 0 and 250</param>
        public static void SetTrapDetectDC(uint oTrapObject, int nDetectDC)
        {
            NWScriptRuntime.Current.SetTrapDetectDC(oTrapObject, nDetectDC);
        }

        /// <summary>
        /// Creates a square trap object.
        /// </summary>
        /// <param name="nTrapType">The base type of trap (TRAP_BASE_TYPE_*)</param>
        /// <param name="lLocation">The location and orientation that the trap will be created at</param>
        /// <param name="fSize">The size of the trap. Minimum size allowed is 1.0f</param>
        /// <param name="sTag">The tag of the trap being created</param>
        /// <param name="nFaction">The faction of the trap (STANDARD_FACTION_*)</param>
        /// <param name="sOnDisarmScript">The OnDisarm script that will fire when the trap is disarmed. If empty string, no script will fire</param>
        /// <param name="sOnTrapTriggeredScript">The OnTrapTriggered script that will fire when the trap is triggered. If empty string, the default OnTrapTriggered script for the trap type specified will fire instead (as specified in the traps.2da)</param>
        /// <returns>The created trap object</returns>
        public static uint CreateTrapAtLocation(TrapBaseType nTrapType, Location lLocation, float fSize = 2.0f,
            string sTag = "", Faction nFaction = Faction.Hostile, string sOnDisarmScript = "",
            string sOnTrapTriggeredScript = "")
        {
            return NWScriptRuntime.Current.CreateTrapAtLocation(nTrapType, lLocation, fSize, sTag, nFaction, sOnDisarmScript, sOnTrapTriggeredScript);
        }

        /// <summary>
        /// Creates a trap on the object specified.
        /// Works only on Doors and Placeables.
        /// After creating a trap on an object, you can change the trap's properties
        /// using the various SetTrap* scripting commands by passing in the object
        /// that the trap was created on (i.e. oObject) to any subsequent SetTrap* commands.
        /// </summary>
        /// <param name="nTrapType">The base type of trap (TRAP_BASE_TYPE_*)</param>
        /// <param name="oObject">The object that the trap will be created on. Works only on Doors and Placeables</param>
        /// <param name="nFaction">The faction of the trap (STANDARD_FACTION_*)</param>
        /// <param name="sOnDisarmScript">The OnDisarm script that will fire when the trap is disarmed. If empty string, no script will fire</param>
        /// <param name="sOnTrapTriggeredScript">The OnTrapTriggered script that will fire when the trap is triggered. If empty string, the default OnTrapTriggered script for the trap type specified will fire instead (as specified in the traps.2da)</param>
        public static void CreateTrapOnObject(TrapBaseType nTrapType, uint oObject, Faction nFaction = Faction.Hostile,
            string sOnDisarmScript = "", string sOnTrapTriggeredScript = "")
        {
            NWScriptRuntime.Current.CreateTrapOnObject(nTrapType, oObject, nFaction, sOnDisarmScript, sOnTrapTriggeredScript);
        }

        /// <summary>
        /// Disables the trap.
        /// </summary>
        /// <param name="oTrap">A placeable, door or trigger</param>
        public static void SetTrapDisabled(uint oTrap)
        {
            NWScriptRuntime.Current.SetTrapDisabled(oTrap);
        }
    }
}
