using SWLOR.Game.Server.Core.NWScript.Enum;

namespace SWLOR.Game.Server.Core.NWScript
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
            Internal.NativeFunctions.StackPushInteger(nDay);
            Internal.NativeFunctions.StackPushInteger(nMonth);
            Internal.NativeFunctions.StackPushInteger(nYear);
            Internal.NativeFunctions.CallBuiltIn(11);
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
            Internal.NativeFunctions.StackPushInteger(nMillisecond);
            Internal.NativeFunctions.StackPushInteger(nSecond);
            Internal.NativeFunctions.StackPushInteger(nMinute);
            Internal.NativeFunctions.StackPushInteger(nHour);
            Internal.NativeFunctions.CallBuiltIn(12);
        }

        /// <summary>
        ///   Get the current calendar year.
        /// </summary>
        public static int GetCalendarYear()
        {
            Internal.NativeFunctions.CallBuiltIn(13);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the current calendar month.
        /// </summary>
        public static int GetCalendarMonth()
        {
            Internal.NativeFunctions.CallBuiltIn(14);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the current calendar day.
        /// </summary>
        public static int GetCalendarDay()
        {
            Internal.NativeFunctions.CallBuiltIn(15);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the current hour.
        /// </summary>
        public static int GetTimeHour()
        {
            Internal.NativeFunctions.CallBuiltIn(16);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the current minute
        /// </summary>
        public static int GetTimeMinute()
        {
            Internal.NativeFunctions.CallBuiltIn(17);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the current second
        /// </summary>
        public static int GetTimeSecond()
        {
            Internal.NativeFunctions.CallBuiltIn(18);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the current millisecond
        /// </summary>
        public static int GetTimeMillisecond()
        {
            Internal.NativeFunctions.CallBuiltIn(19);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Set whether or not the creature oDetector has detected the trapped object oTrap.
        ///   - oTrap: A trapped trigger, placeable or door object.
        ///   - oDetector: This is the creature that the detected status of the trap is being adjusted for.
        ///   - bDetected: A Boolean that sets whether the trapped object has been detected or not.
        /// </summary>
        public static int SetTrapDetectedBy(uint oTrap, uint oDetector, bool bDetected = true)
        {
            Internal.NativeFunctions.StackPushInteger(bDetected ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oDetector);
            Internal.NativeFunctions.StackPushObject(oTrap);
            Internal.NativeFunctions.CallBuiltIn(550);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Note: Only placeables, doors and triggers can be trapped.
        ///   * Returns TRUE if oObject is trapped.
        /// </summary>
        public static int GetIsTrapped(uint oObject)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(551);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   - oTrapObject: a placeable, door or trigger
        ///   * Returns TRUE if oTrapObject is disarmable.
        /// </summary>
        public static bool GetTrapDisarmable(uint oTrapObject)
        {
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(527);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   - oTrapObject: a placeable, door or trigger
        ///   * Returns TRUE if oTrapObject is detectable.
        /// </summary>
        public static bool GetTrapDetectable(uint oTrapObject)
        {
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(528);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   - oTrapObject: a placeable, door or trigger
        ///   - oCreature
        ///   * Returns TRUE if oCreature has detected oTrapObject
        /// </summary>
        public static bool GetTrapDetectedBy(uint oTrapObject, uint oCreature)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(529);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   - oTrapObject: a placeable, door or trigger
        ///   * Returns TRUE if oTrapObject has been flagged as visible to all creatures.
        /// </summary>
        public static bool GetTrapFlagged(uint oTrapObject)
        {
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(530);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Get the trap base type (TRAP_BASE_TYPE_*) of oTrapObject.
        ///   - oTrapObject: a placeable, door or trigger
        /// </summary>
        public static int GetTrapBaseType(uint oTrapObject)
        {
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(531);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   - oTrapObject: a placeable, door or trigger
        ///   * Returns TRUE if oTrapObject is one-shot (i.e. it does not reset itself
        ///   after firing.
        /// </summary>
        public static bool GetTrapOneShot(uint oTrapObject)
        {
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(532);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Get the creator of oTrapObject, the creature that set the trap.
        ///   - oTrapObject: a placeable, door or trigger
        ///   * Returns OBJECT_INVALID if oTrapObject was created in the toolset.
        /// </summary>
        public static uint GetTrapCreator(uint oTrapObject)
        {
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(533);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the tag of the key that will disarm oTrapObject.
        ///   - oTrapObject: a placeable, door or trigger
        /// </summary>
        public static string GetTrapKeyTag(uint oTrapObject)
        {
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(534);
            return Internal.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Get the DC for disarming oTrapObject.
        ///   - oTrapObject: a placeable, door or trigger
        /// </summary>
        public static int GetTrapDisarmDC(uint oTrapObject)
        {
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(535);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the DC for detecting oTrapObject.
        ///   - oTrapObject: a placeable, door or trigger
        /// </summary>
        public static int GetTrapDetectDC(uint oTrapObject)
        {
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(536);
            return Internal.NativeFunctions.StackPopInteger();
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
            Internal.NativeFunctions.StackPushInteger(nTrapDetected ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(488);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the last trap detected by oTarget.
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetLastTrapDetected(uint oTarget = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(486);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   - oTrapObject: a placeable, door or trigger
        ///   * Returns TRUE if oTrapObject is active
        /// </summary>
        public static bool GetTrapActive(uint oTrapObject)
        {
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(821);
            return Internal.NativeFunctions.StackPopInteger() != 0;
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
            Internal.NativeFunctions.StackPushInteger(nActive ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(822);
        }

        /// <summary>
        ///   - oTrapObject: a placeable, door or trigger
        ///   * Returns TRUE if oTrapObject can be recovered.
        /// </summary>
        public static bool GetTrapRecoverable(uint oTrapObject)
        {
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(815);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Sets whether or not the trapped object can be recovered.
        ///   - oTrapObject: a placeable, door or trigger
        /// </summary>
        public static void SetTrapRecoverable(uint oTrapObject, bool nRecoverable = true)
        {
            Internal.NativeFunctions.StackPushInteger(nRecoverable ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(816);
        }

        /// <summary>
        ///   Sets whether or not the trapped object can be disarmed.
        ///   - oTrapObject: a placeable, door or trigger
        ///   - nDisarmable: TRUE/FALSE
        /// </summary>
        public static void SetTrapDisarmable(uint oTrapObject, bool nDisarmable = true)
        {
            Internal.NativeFunctions.StackPushInteger(nDisarmable ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(803);
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
            Internal.NativeFunctions.StackPushInteger(nDetectable ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(804);
        }

        /// <summary>
        ///   Sets whether or not the trap is a one-shot trap
        ///   (i.e. whether or not the trap resets itself after firing).
        ///   - oTrapObject: a placeable, door or trigger
        ///   - nOneShot: TRUE/FALSE
        /// </summary>
        public static void SetTrapOneShot(uint oTrapObject, bool nOneShot = true)
        {
            Internal.NativeFunctions.StackPushInteger(nOneShot ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(805);
        }

        /// <summary>
        ///   Set the tag of the key that will disarm oTrapObject.
        ///   - oTrapObject: a placeable, door or trigger
        /// </summary>
        public static void SetTrapKeyTag(uint oTrapObject, string sKeyTag)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sKeyTag);
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(806);
        }

        /// <summary>
        ///   Set the DC for disarming oTrapObject.
        ///   - oTrapObject: a placeable, door or trigger
        ///   - nDisarmDC: must be between 0 and 250.
        /// </summary>
        public static void SetTrapDisarmDC(uint oTrapObject, int nDisarmDC)
        {
            Internal.NativeFunctions.StackPushInteger(nDisarmDC);
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(807);
        }

        /// <summary>
        ///   Set the DC for detecting oTrapObject.
        ///   - oTrapObject: a placeable, door or trigger
        ///   - nDetectDC: must be between 0 and 250.
        /// </summary>
        public static void SetTrapDetectDC(uint oTrapObject, int nDetectDC)
        {
            Internal.NativeFunctions.StackPushInteger(nDetectDC);
            Internal.NativeFunctions.StackPushObject(oTrapObject);
            Internal.NativeFunctions.CallBuiltIn(808);
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
            Internal.NativeFunctions.StackPushStringUTF8(sOnTrapTriggeredScript);
            Internal.NativeFunctions.StackPushStringUTF8(sOnDisarmScript);
            Internal.NativeFunctions.StackPushInteger((int)nFaction);
            Internal.NativeFunctions.StackPushStringUTF8(sTag);
            Internal.NativeFunctions.StackPushFloat(fSize);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lLocation);
            Internal.NativeFunctions.StackPushInteger((int)nTrapType);
            Internal.NativeFunctions.CallBuiltIn(809);
            return Internal.NativeFunctions.StackPopObject();
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
        public static void CreateTrapOnObject(int nTrapType, uint oObject, Faction nFaction = Faction.Hostile,
            string sOnDisarmScript = "", string sOnTrapTriggeredScript = "")
        {
            Internal.NativeFunctions.StackPushStringUTF8(sOnTrapTriggeredScript);
            Internal.NativeFunctions.StackPushStringUTF8(sOnDisarmScript);
            Internal.NativeFunctions.StackPushInteger((int)nFaction);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.StackPushInteger(nTrapType);
            Internal.NativeFunctions.CallBuiltIn(810);
        }

        /// <summary>
        ///   Disable oTrap.
        ///   - oTrap: a placeable, door or trigger.
        /// </summary>
        public static void SetTrapDisabled(uint oTrap)
        {
            Internal.NativeFunctions.StackPushObject(oTrap);
            Internal.NativeFunctions.CallBuiltIn(555);
        }
    }
}