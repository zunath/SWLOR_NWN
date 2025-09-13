using SWLOR.NWN.API.Core.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Set the calendar to the specified date.
        ///   - nYear should be from 0 to 32000 inclusive
        ///   - nMonth should be from 1 to 12 inclusive
        ///   - nDay should be from 1 to 28 inclusive
        ///   1) Time can only be advanced forwards; attempting to set the time backwards
        ///   will result in no change to the calendar.
        ///   2) If values larger than the month or day are specified, they will be wrapped
        ///   around and the overflow will be used to advance the next field.
        ///   e.g. Specifying a year of 1350, month of 33 and day of 10 will result in
        ///   the calender being set to a year of 1352, a month of 9 and a day of 10.
        /// </summary>
        public static void SetCalendar(int nYear, int nMonth, int nDay)
        {
            global::NWN.Core.NWScript.SetCalendar(nYear, nMonth, nDay);
        }

        /// <summary>
        ///   Set the time to the time specified.
        ///   - nHour should be from 0 to 23 inclusive
        ///   - nMinute should be from 0 to 59 inclusive
        ///   - nSecond should be from 0 to 59 inclusive
        ///   - nMillisecond should be from 0 to 999 inclusive
        ///   1) Time can only be advanced forwards; attempting to set the time backwards
        ///   will result in the day advancing and then the time being set to that
        ///   specified, e.g. if the current hour is 15 and then the hour is set to 3,
        ///   the day will be advanced by 1 and the hour will be set to 3.
        ///   2) If values larger than the max hour, minute, second or millisecond are
        ///   specified, they will be wrapped around and the overflow will be used to
        ///   advance the next field, e.g. specifying 62 hours, 250 minutes, 10 seconds
        ///   and 10 milliseconds will result in the calendar day being advanced by 2
        ///   and the time being set to 18 hours, 10 minutes, 10 milliseconds.
        /// </summary>
        public static void SetTime(int nHour, int nMinute, int nSecond, int nMillisecond)
        {
            global::NWN.Core.NWScript.SetTime(nHour, nMinute, nSecond, nMillisecond);
        }

        /// <summary>
        ///   Get the current calendar year.
        /// </summary>
        public static int GetCalendarYear()
        {
            return global::NWN.Core.NWScript.GetCalendarYear();
        }

        /// <summary>
        ///   Get the current calendar month.
        /// </summary>
        public static int GetCalendarMonth()
        {
            return global::NWN.Core.NWScript.GetCalendarMonth();
        }

        /// <summary>
        ///   Get the current calendar day.
        /// </summary>
        public static int GetCalendarDay()
        {
            return global::NWN.Core.NWScript.GetCalendarDay();
        }

        /// <summary>
        ///   Get the current hour.
        /// </summary>
        public static int GetTimeHour()
        {
            return global::NWN.Core.NWScript.GetTimeHour();
        }

        /// <summary>
        ///   Get the current minute
        /// </summary>
        public static int GetTimeMinute()
        {
            return global::NWN.Core.NWScript.GetTimeMinute();
        }

        /// <summary>
        ///   Get the current second
        /// </summary>
        public static int GetTimeSecond()
        {
            return global::NWN.Core.NWScript.GetTimeSecond();
        }

        /// <summary>
        ///   Get the current millisecond
        /// </summary>
        public static int GetTimeMillisecond()
        {
            return global::NWN.Core.NWScript.GetTimeMillisecond();
        }

        /// <summary>
        ///   Set whether or not the creature oDetector has detected the trapped object oTrap.
        ///   - oTrap: A trapped trigger, placeable or door object.
        ///   - oDetector: This is the creature that the detected status of the trap is being adjusted for.
        ///   - bDetected: A Boolean that sets whether the trapped object has been detected or not.
        /// </summary>
        public static int SetTrapDetectedBy(uint oTrap, uint oDetector, bool bDetected = true)
        {
            return global::NWN.Core.NWScript.SetTrapDetectedBy(oTrap, oDetector, bDetected ? 1 : 0);
        }

        /// <summary>
        ///   Note: Only placeables, doors and triggers can be trapped.
        ///   * Returns TRUE if oObject is trapped.
        /// </summary>
        public static bool GetIsTrapped(uint oObject)
        {
            return global::NWN.Core.NWScript.GetIsTrapped(oObject) != 0;
        }

        /// <summary>
        ///   - oTrapObject: a placeable, door or trigger
        ///   * Returns TRUE if oTrapObject is disarmable.
        /// </summary>
        public static bool GetTrapDisarmable(uint oTrapObject)
        {
            return global::NWN.Core.NWScript.GetTrapDisarmable(oTrapObject) != 0;
        }

        /// <summary>
        ///   - oTrapObject: a placeable, door or trigger
        ///   * Returns TRUE if oTrapObject is detectable.
        /// </summary>
        public static bool GetTrapDetectable(uint oTrapObject)
        {
            return global::NWN.Core.NWScript.GetTrapDetectable(oTrapObject) != 0;
        }

        /// <summary>
        ///   - oTrapObject: a placeable, door or trigger
        ///   - oCreature
        ///   * Returns TRUE if oCreature has detected oTrapObject
        /// </summary>
        public static bool GetTrapDetectedBy(uint oTrapObject, uint oCreature)
        {
            return global::NWN.Core.NWScript.GetTrapDetectedBy(oTrapObject, oCreature) != 0;
        }

        /// <summary>
        ///   - oTrapObject: a placeable, door or trigger
        ///   * Returns TRUE if oTrapObject has been flagged as visible to all creatures.
        /// </summary>
        public static bool GetTrapFlagged(uint oTrapObject)
        {
            return global::NWN.Core.NWScript.GetTrapFlagged(oTrapObject) != 0;
        }

        /// <summary>
        ///   Get the trap base type (TRAP_BASE_TYPE_*) of oTrapObject.
        ///   - oTrapObject: a placeable, door or trigger
        /// </summary>
        public static TrapBaseType GetTrapBaseType(uint oTrapObject)
        {
            return (TrapBaseType)global::NWN.Core.NWScript.GetTrapBaseType(oTrapObject);
        }

        /// <summary>
        ///   - oTrapObject: a placeable, door or trigger
        ///   * Returns TRUE if oTrapObject is one-shot (i.e. it does not reset itself
        ///   after firing.
        /// </summary>
        public static bool GetTrapOneShot(uint oTrapObject)
        {
            return global::NWN.Core.NWScript.GetTrapOneShot(oTrapObject) != 0;
        }

        /// <summary>
        ///   Get the creator of oTrapObject, the creature that set the trap.
        ///   - oTrapObject: a placeable, door or trigger
        ///   * Returns OBJECT_INVALID if oTrapObject was created in the toolset.
        /// </summary>
        public static uint GetTrapCreator(uint oTrapObject)
        {
            return global::NWN.Core.NWScript.GetTrapCreator(oTrapObject);
        }

        /// <summary>
        ///   Get the tag of the key that will disarm oTrapObject.
        ///   - oTrapObject: a placeable, door or trigger
        /// </summary>
        public static string GetTrapKeyTag(uint oTrapObject)
        {
            return global::NWN.Core.NWScript.GetTrapKeyTag(oTrapObject);
        }

        /// <summary>
        ///   Get the DC for disarming oTrapObject.
        ///   - oTrapObject: a placeable, door or trigger
        /// </summary>
        public static int GetTrapDisarmDC(uint oTrapObject)
        {
            return global::NWN.Core.NWScript.GetTrapDisarmDC(oTrapObject);
        }

        /// <summary>
        ///   Get the DC for detecting oTrapObject.
        ///   - oTrapObject: a placeable, door or trigger
        /// </summary>
        public static int GetTrapDetectDC(uint oTrapObject)
        {
            return global::NWN.Core.NWScript.GetTrapDetectDC(oTrapObject);
        }

        /// <summary>
        ///   Get the trap nearest to oTarget.
        ///   Note : "trap objects" are actually any trigger, placeable or door that is
        ///   trapped in oTarget's area.
        ///   - oTarget
        ///   - nTrapDetected: if this is TRUE, the trap returned has to have been detected
        ///   by oTarget.
        /// </summary>
        public static uint GetNearestTrapToObject(uint oTarget = OBJECT_INVALID, bool nTrapDetected = true)
        {
            return global::NWN.Core.NWScript.GetNearestTrapToObject(oTarget, nTrapDetected ? 1 : 0);
        }

        /// <summary>
        ///   Get the last trap detected by oTarget.
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetLastTrapDetected(uint oTarget = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetLastTrapDetected(oTarget);
        }

        /// <summary>
        ///   - oTrapObject: a placeable, door or trigger
        ///   * Returns TRUE if oTrapObject is active
        /// </summary>
        public static bool GetTrapActive(uint oTrapObject)
        {
            return global::NWN.Core.NWScript.GetTrapActive(oTrapObject) != 0;
        }

        /// <summary>
        ///   Sets whether or not the trap is an active trap
        ///   - oTrapObject: a placeable, door or trigger
        ///   - nActive: TRUE/FALSE
        ///   Notes:
        ///   Setting a trap as inactive will not make the
        ///   trap disappear if it has already been detected.
        ///   Call SetTrapDetectedBy() to make a detected trap disappear.
        ///   To make an inactive trap not detectable call SetTrapDetectable()
        /// </summary>
        public static void SetTrapActive(uint oTrapObject, bool nActive = true)
        {
            global::NWN.Core.NWScript.SetTrapActive(oTrapObject, nActive ? 1 : 0);
        }

        /// <summary>
        ///   - oTrapObject: a placeable, door or trigger
        ///   * Returns TRUE if oTrapObject can be recovered.
        /// </summary>
        public static bool GetTrapRecoverable(uint oTrapObject)
        {
            return global::NWN.Core.NWScript.GetTrapRecoverable(oTrapObject) != 0;
        }

        /// <summary>
        ///   Sets whether or not the trapped object can be recovered.
        ///   - oTrapObject: a placeable, door or trigger
        /// </summary>
        public static void SetTrapRecoverable(uint oTrapObject, bool nRecoverable = true)
        {
            global::NWN.Core.NWScript.SetTrapRecoverable(oTrapObject, nRecoverable ? 1 : 0);
        }

        /// <summary>
        ///   Sets whether or not the trapped object can be disarmed.
        ///   - oTrapObject: a placeable, door or trigger
        ///   - nDisarmable: TRUE/FALSE
        /// </summary>
        public static void SetTrapDisarmable(uint oTrapObject, bool nDisarmable = true)
        {
            global::NWN.Core.NWScript.SetTrapDisarmable(oTrapObject, nDisarmable ? 1 : 0);
        }

        /// <summary>
        ///   Sets whether or not the trapped object can be detected.
        ///   - oTrapObject: a placeable, door or trigger
        ///   - nDetectable: TRUE/FALSE
        ///   Note: Setting a trapped object to not be detectable will
        ///   not make the trap disappear if it has already been detected.
        /// </summary>
        public static void SetTrapDetectable(uint oTrapObject, bool nDetectable = true)
        {
            global::NWN.Core.NWScript.SetTrapDetectable(oTrapObject, nDetectable ? 1 : 0);
        }

        /// <summary>
        ///   Sets whether or not the trap is a one-shot trap
        ///   (i.e. whether or not the trap resets itself after firing).
        ///   - oTrapObject: a placeable, door or trigger
        ///   - nOneShot: TRUE/FALSE
        /// </summary>
        public static void SetTrapOneShot(uint oTrapObject, bool nOneShot = true)
        {
            global::NWN.Core.NWScript.SetTrapOneShot(oTrapObject, nOneShot ? 1 : 0);
        }

        /// <summary>
        ///   Set the tag of the key that will disarm oTrapObject.
        ///   - oTrapObject: a placeable, door or trigger
        /// </summary>
        public static void SetTrapKeyTag(uint oTrapObject, string sKeyTag)
        {
            global::NWN.Core.NWScript.SetTrapKeyTag(oTrapObject, sKeyTag);
        }

        /// <summary>
        ///   Set the DC for disarming oTrapObject.
        ///   - oTrapObject: a placeable, door or trigger
        ///   - nDisarmDC: must be between 0 and 250.
        /// </summary>
        public static void SetTrapDisarmDC(uint oTrapObject, int nDisarmDC)
        {
            global::NWN.Core.NWScript.SetTrapDisarmDC(oTrapObject, nDisarmDC);
        }

        /// <summary>
        ///   Set the DC for detecting oTrapObject.
        ///   - oTrapObject: a placeable, door or trigger
        ///   - nDetectDC: must be between 0 and 250.
        /// </summary>
        public static void SetTrapDetectDC(uint oTrapObject, int nDetectDC)
        {
            global::NWN.Core.NWScript.SetTrapDetectDC(oTrapObject, nDetectDC);
        }

        /// <summary>
        ///   Creates a square Trap object.
        ///   - nTrapType: The base type of trap (TRAP_BASE_TYPE_*)
        ///   - lLocation: The location and orientation that the trap will be created at.
        ///   - fSize: The size of the trap. Minimum size allowed is 1.0f.
        ///   - sTag: The tag of the trap being created.
        ///   - nFaction: The faction of the trap (STANDARD_FACTION_*).
        ///   - sOnDisarmScript: The OnDisarm script that will fire when the trap is disarmed.
        ///   If "" no script will fire.
        ///   - sOnTrapTriggeredScript: The OnTrapTriggered script that will fire when the
        ///   trap is triggered.
        ///   If "" the default OnTrapTriggered script for the trap
        ///   type specified will fire instead (as specified in the
        ///   traps.2da).
        /// </summary>
        public static uint CreateTrapAtLocation(TrapBaseType nTrapType, Location lLocation, float fSize = 2.0f,
            string sTag = "", Faction nFaction = Faction.Hostile, string sOnDisarmScript = "",
            string sOnTrapTriggeredScript = "")
        {
            return global::NWN.Core.NWScript.CreateTrapAtLocation((int)nTrapType, lLocation, fSize, sTag, (int)nFaction, sOnDisarmScript, sOnTrapTriggeredScript);
        }

        /// <summary>
        ///   Creates a Trap on the object specified.
        ///   - nTrapType: The base type of trap (TRAP_BASE_TYPE_*)
        ///   - oObject: The object that the trap will be created on. Works only on Doors and Placeables.
        ///   - nFaction: The faction of the trap (STANDARD_FACTION_*).
        ///   - sOnDisarmScript: The OnDisarm script that will fire when the trap is disarmed.
        ///   If "" no script will fire.
        ///   - sOnTrapTriggeredScript: The OnTrapTriggered script that will fire when the
        ///   trap is triggered.
        ///   If "" the default OnTrapTriggered script for the trap
        ///   type specified will fire instead (as specified in the
        ///   traps.2da).
        ///   Note: After creating a trap on an object, you can change the trap's properties
        ///   using the various SetTrap* scripting commands by passing in the object
        ///   that the trap was created on (i.e. oObject) to any subsequent SetTrap* commands.
        /// </summary>
        public static void CreateTrapOnObject(TrapBaseType nTrapType, uint oObject, Faction nFaction = Faction.Hostile,
            string sOnDisarmScript = "", string sOnTrapTriggeredScript = "")
        {
            global::NWN.Core.NWScript.CreateTrapOnObject((int)nTrapType, oObject, (int)nFaction, sOnDisarmScript, sOnTrapTriggeredScript);
        }

        /// <summary>
        ///   Disable oTrap.
        ///   - oTrap: a placeable, door or trigger.
        /// </summary>
        public static void SetTrapDisabled(uint oTrap)
        {
            global::NWN.Core.NWScript.SetTrapDisabled(oTrap);
        }
    }
}