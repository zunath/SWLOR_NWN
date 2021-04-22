using System;
using System.Numerics;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;


namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        public static uint OBJECT_SELF => Internal.OBJECT_SELF;

        /// <summary>
        ///   Assign aActionToAssign to oActionSubject.
        ///   * No return value, but if an error occurs, the log file will contain
        ///   "AssignCommand failed."
        ///   (If the object doesn't exist, nothing happens.)
        /// </summary>
        public static void AssignCommand(uint oActionSubject, ActionDelegate aActionToAssign)
        {
            Internal.ClosureAssignCommand(oActionSubject, aActionToAssign);
        }

        /// <summary>
        ///   Delay aActionToDelay by fSeconds.
        ///   * No return value, but if an error occurs, the log file will contain
        ///   "DelayCommand failed.".
        ///   It is suggested that functions which create effects should not be used
        ///   as parameters to delayed actions.  Instead, the effect should be created in the
        ///   script and then passed into the action.  For example:
        ///   effect eDamage = EffectDamage(nDamage, DAMAGE_TYPE_MAGICAL);
        ///   DelayCommand(fDelay, ApplyEffectToObject(DURATION_TYPE_INSTANT, eDamage, oTarget);
        /// </summary>
        public static void DelayCommand(float fSeconds, ActionDelegate aActionToDelay)
        {
            Internal.ClosureDelayCommand(Internal.OBJECT_SELF, fSeconds, aActionToDelay);
        }


        /// <summary>
        ///   Do aActionToDo.
        /// </summary>
        public static void ActionDoCommand(ActionDelegate aActionToDo)
        {
            Internal.ClosureActionDoCommand(Internal.OBJECT_SELF, aActionToDo);
        }

        /// <summary>
        ///   Get an integer between 0 and nMaxInteger-1.
        ///   Return value on error: 0
        /// </summary>
        public static int Random(int nMaxInteger)
        {
            Internal.NativeFunctions.StackPushInteger(nMaxInteger);
            Internal.NativeFunctions.CallBuiltIn(0);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Output sString to the log file.
        /// </summary>
        public static void PrintString(string sString)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sString);
            Internal.NativeFunctions.CallBuiltIn(1);
        }

        /// <summary>
        ///   Output a formatted float to the log file.
        ///   - nWidth should be a value from 0 to 18 inclusive.
        ///   - nDecimals should be a value from 0 to 9 inclusive.
        /// </summary>
        public static void PrintFloat(float fFloat, int nWidth = 18, int nDecimals = 9)
        {
            Internal.NativeFunctions.StackPushInteger(nDecimals);
            Internal.NativeFunctions.StackPushInteger(nWidth);
            Internal.NativeFunctions.StackPushFloat(fFloat);
            Internal.NativeFunctions.CallBuiltIn(2);
        }

        /// <summary>
        ///   Convert fFloat into a string.
        ///   - nWidth should be a value from 0 to 18 inclusive.
        ///   - nDecimals should be a value from 0 to 9 inclusive.
        /// </summary>
        public static string FloatToString(float fFloat, int nWidth = 18, int nDecimals = 9)
        {
            Internal.NativeFunctions.StackPushInteger(nDecimals);
            Internal.NativeFunctions.StackPushInteger(nWidth);
            Internal.NativeFunctions.StackPushFloat(fFloat);
            Internal.NativeFunctions.CallBuiltIn(3);
            return Internal.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Output nInteger to the log file.
        /// </summary>
        public static void PrintInteger(int nInteger)
        {
            Internal.NativeFunctions.StackPushInteger(nInteger);
            Internal.NativeFunctions.CallBuiltIn(4);
        }

        /// <summary>
        ///   Output oObject's ID to the log file.
        /// </summary>
        public static void PrintObject(uint oObject)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(5);
        }

        /// <summary>
        ///   Make oTarget run sScript and then return execution to the calling script.
        ///   If sScript does not specify a compiled script, nothing happens.
        /// </summary>
        public static void ExecuteScript(string sScript, uint oTarget)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushStringUTF8(sScript);
            Internal.NativeFunctions.CallBuiltIn(8);
        }

        /// <summary>
        ///   Clear all the actions of the caller.
        ///   * No return value, but if an error occurs, the log file will contain
        ///   "ClearAllActions failed.".
        ///   - nClearCombatState: if true, this will immediately clear the combat state
        ///   on a creature, which will stop the combat music and allow them to rest,
        ///   engage in dialog, or other actions that they would normally have to wait for.
        /// </summary>
        public static void ClearAllActions(bool nClearCombatState = false)
        {
            Internal.NativeFunctions.StackPushInteger(nClearCombatState ? 1 : 0);
            Internal.NativeFunctions.CallBuiltIn(9);
        }

        /// <summary>
        ///   Cause the caller to face fDirection.
        ///   - fDirection is expressed as anticlockwise degrees from Due East.
        ///   DIRECTION_EAST, DIRECTION_NORTH, DIRECTION_WEST and DIRECTION_SOUTH are
        ///   predefined. (0.0f=East, 90.0f=North, 180.0f=West, 270.0f=South)
        /// </summary>
        public static void SetFacing(float fDirection)
        {
            Internal.NativeFunctions.StackPushFloat(fDirection);
            Internal.NativeFunctions.CallBuiltIn(10);
        }

        /// <summary>
        ///   The action subject will generate a random location near its current location
        ///   and pathfind to it.  ActionRandomwalk never ends, which means it is neccessary
        ///   to call ClearAllActions in order to allow a creature to perform any other action
        ///   once ActionRandomWalk has been called.
        ///   * No return value, but if an error occurs the log file will contain
        ///   "ActionRandomWalk failed."
        /// </summary>
        public static void ActionRandomWalk()
        {
            Internal.NativeFunctions.CallBuiltIn(20);
        }

        /// <summary>
        ///   The action subject will move to lDestination.
        ///   - lDestination: The object will move to this location.  If the location is
        ///   invalid or a path cannot be found to it, the command does nothing.
        ///   - bRun: If this is TRUE, the action subject will run rather than walk
        ///   * No return value, but if an error occurs the log file will contain
        ///   "MoveToPoint failed."
        /// </summary>
        public static void ActionMoveToLocation(Location lDestination, bool bRun = false)
        {
            Internal.NativeFunctions.StackPushInteger(bRun ? 1 : 0);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lDestination);
            Internal.NativeFunctions.CallBuiltIn(21);
        }

        /// <summary>
        ///   Cause the action subject to move to a certain distance from oMoveTo.
        ///   If there is no path to oMoveTo, this command will do nothing.
        ///   - oMoveTo: This is the object we wish the action subject to move to
        ///   - bRun: If this is TRUE, the action subject will run rather than walk
        ///   - fRange: This is the desired distance between the action subject and oMoveTo
        ///   * No return value, but if an error occurs the log file will contain
        ///   "ActionMoveToObject failed."
        /// </summary>
        public static void ActionMoveToObject(uint oMoveTo, bool bRun = false, float fRange = 1.0f)
        {
            Internal.NativeFunctions.StackPushFloat(fRange);
            Internal.NativeFunctions.StackPushInteger(bRun ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oMoveTo);
            Internal.NativeFunctions.CallBuiltIn(22);
        }

        /// <summary>
        ///   Cause the action subject to move to a certain distance away from oFleeFrom.
        ///   - oFleeFrom: This is the object we wish the action subject to move away from.
        ///   If oFleeFrom is not in the same area as the action subject, nothing will
        ///   happen.
        ///   - bRun: If this is TRUE, the action subject will run rather than walk
        ///   - fMoveAwayRange: This is the distance we wish the action subject to put
        ///   between themselves and oFleeFrom
        ///   * No return value, but if an error occurs the log file will contain
        ///   "ActionMoveAwayFromObject failed."
        /// </summary>
        public static void ActionMoveAwayFromObject(uint oFleeFrom, bool bRun = false, float fMoveAwayRange = 40.0f)
        {
            Internal.NativeFunctions.StackPushFloat(fMoveAwayRange);
            Internal.NativeFunctions.StackPushInteger(bRun ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oFleeFrom);
            Internal.NativeFunctions.CallBuiltIn(23);
        }

        /// <summary>
        ///   Get the direction in which oTarget is facing, expressed as a float between
        ///   0.0f and 360.0f
        ///   * Return value on error: -1.0f
        /// </summary>
        public static float GetFacing(uint oTarget)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(28);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Get the last attacker of oAttackee.  This should only be used ONLY in the
        ///   OnAttacked events for creatures, placeables and doors.
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetLastAttacker(uint oAttackee = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oAttackee);
            Internal.NativeFunctions.CallBuiltIn(36);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Attack oAttackee.
        ///   - bPassive: If this is TRUE, attack is in passive mode.
        /// </summary>
        public static void ActionAttack(uint oAttackee, bool bPassive = false)
        {
            Internal.NativeFunctions.StackPushInteger(bPassive ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oAttackee);
            Internal.NativeFunctions.CallBuiltIn(37);
        }

        /// <summary>
        ///   Get the creature nearest to oTarget, subject to all the criteria specified.
        ///   - nFirstCriteriaType: CREATURE_TYPE_*
        ///   - nFirstCriteriaValue:
        ///   -> CLASS_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_CLASS
        ///   -> SPELL_* if nFirstCriteriaType was CREATURE_TYPE_DOES_NOT_HAVE_SPELL_EFFECT
        ///   or CREATURE_TYPE_HAS_SPELL_EFFECT
        ///   -> TRUE or FALSE if nFirstCriteriaType was CREATURE_TYPE_IS_ALIVE
        ///   -> PERCEPTION_* if nFirstCriteriaType was CREATURE_TYPE_PERCEPTION
        ///   -> PLAYER_CHAR_IS_PC or PLAYER_CHAR_NOT_PC if nFirstCriteriaType was
        ///   CREATURE_TYPE_PLAYER_CHAR
        ///   -> RACIAL_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_RACIAL_TYPE
        ///   -> REPUTATION_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_REPUTATION
        ///   For example, to get the nearest PC, use:
        ///   (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC)
        ///   - oTarget: We're trying to find the creature of the specified type that is
        ///   nearest to oTarget
        ///   - nNth: We don't have to find the first nearest: we can find the Nth nearest...
        ///   - nSecondCriteriaType: This is used in the same way as nFirstCriteriaType to
        ///   further specify the type of creature that we are looking for.
        ///   - nSecondCriteriaValue: This is used in the same way as nFirstCriteriaValue
        ///   to further specify the type of creature that we are looking for.
        ///   - nThirdCriteriaType: This is used in the same way as nFirstCriteriaType to
        ///   further specify the type of creature that we are looking for.
        ///   - nThirdCriteriaValue: This is used in the same way as nFirstCriteriaValue to
        ///   further specify the type of creature that we are looking for.
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetNearestCreature(CreatureType nFirstCriteriaType, int nFirstCriteriaValue,
            uint oTarget = OBJECT_INVALID, int nNth = 1, int nSecondCriteriaType = -1, int nSecondCriteriaValue = -1,
            int nThirdCriteriaType = -1, int nThirdCriteriaValue = -1)
        {
            Internal.NativeFunctions.StackPushInteger(nThirdCriteriaValue);
            Internal.NativeFunctions.StackPushInteger(nThirdCriteriaType);
            Internal.NativeFunctions.StackPushInteger(nSecondCriteriaValue);
            Internal.NativeFunctions.StackPushInteger(nSecondCriteriaType);
            Internal.NativeFunctions.StackPushInteger(nNth);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushInteger(nFirstCriteriaValue);
            Internal.NativeFunctions.StackPushInteger((int)nFirstCriteriaType);
            Internal.NativeFunctions.CallBuiltIn(38);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Add a speak action to the action subject.
        ///   - sStringToSpeak: String to be spoken
        ///   - nTalkVolume: TALKVOLUME_*
        /// </summary>
        public static void ActionSpeakString(string sStringToSpeak, TalkVolume nTalkVolume = TalkVolume.Talk)
        {
            Internal.NativeFunctions.StackPushInteger((int)nTalkVolume);
            Internal.NativeFunctions.StackPushString(sStringToSpeak);
            Internal.NativeFunctions.CallBuiltIn(39);
        }

        /// <summary>
        ///   Cause the action subject to play an animation
        ///   - nAnimation: ANIMATION_*
        ///   - fSpeed: Speed of the animation
        ///   - fDurationSeconds: Duration of the animation (this is not used for Fire and
        ///   Forget animations)
        /// </summary>
        public static void ActionPlayAnimation(Animation nAnimation, float fSpeed = 1.0f, float fDurationSeconds = 0.0f)
        {
            Internal.NativeFunctions.StackPushFloat(fDurationSeconds);
            Internal.NativeFunctions.StackPushFloat(fSpeed);
            Internal.NativeFunctions.StackPushInteger((int)nAnimation);
            Internal.NativeFunctions.CallBuiltIn(40);
        }

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
            Internal.NativeFunctions.StackPushInteger((int)nTransitionType);
            Internal.NativeFunctions.StackPushFloat(fPitch);
            Internal.NativeFunctions.StackPushFloat(fDistance);
            Internal.NativeFunctions.StackPushFloat(fDirection);
            Internal.NativeFunctions.CallBuiltIn(45);
        }

        /// <summary>
        ///   Play sSoundName
        ///   - sSoundName: TBD - SS
        ///   This will play a mono sound from the location of the object running the command.
        /// </summary>
        public static void PlaySound(string sSoundName)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sSoundName);
            Internal.NativeFunctions.CallBuiltIn(46);
        }

        /// <summary>
        ///   Get the object at which the caller last cast a spell
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetSpellTargetObject()
        {
            Internal.NativeFunctions.CallBuiltIn(47);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   This action casts a spell at oTarget.
        ///   - nSpell: SPELL_*
        ///   - oTarget: Target for the spell
        ///   - nMetamagic: METAMAGIC_*
        ///   - bCheat: If this is TRUE, then the executor of the action doesn't have to be
        ///   able to cast the spell.
        ///   - nDomainLevel: TBD - SS
        ///   - nProjectilePathType: PROJECTILE_PATH_TYPE_*
        ///   - bInstantSpell: If this is TRUE, the spell is cast immediately. This allows
        ///   the end-user to simulate a high-level magic-user having lots of advance
        ///   warning of impending trouble
        /// </summary>
        public static void ActionCastSpellAtObject(Spell nSpell, uint oTarget, MetaMagic nMetaMagic = MetaMagic.Any,
            bool nCheat = false, int nDomainLevel = 0,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default, bool bInstantSpell = false)
        {
            Internal.NativeFunctions.StackPushInteger(bInstantSpell ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger((int)nProjectilePathType);
            Internal.NativeFunctions.StackPushInteger(nDomainLevel);
            Internal.NativeFunctions.StackPushInteger(nCheat ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger((int)nMetaMagic);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushInteger((int)nSpell);
            Internal.NativeFunctions.CallBuiltIn(48);
        }

        /// <summary>
        ///   Get oObject's local integer variable sVarName
        ///   * Return value on error: 0
        /// </summary>
        public static int GetLocalInt(uint oObject, string sVarName)
        {
            Internal.NativeFunctions.StackPushString(sVarName);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(51);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        /// Get oObject's local boolean variable sVarName
        /// * Return value on error: false
        /// </summary>
        public static bool GetLocalBool(uint oObject, string sVarName)
        {
            return Convert.ToBoolean(GetLocalInt(oObject, sVarName));
        }

        /// <summary>
        ///   Get oObject's local float variable sVarName
        ///   * Return value on error: 0.0f
        /// </summary>
        public static float GetLocalFloat(uint oObject, string sVarName)
        {
            Internal.NativeFunctions.StackPushString(sVarName);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(52);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Get oObject's local string variable sVarName
        ///   * Return value on error: ""
        /// </summary>
        public static string GetLocalString(uint oObject, string sVarName)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(53);
            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        ///   Get oObject's local object variable sVarName
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetLocalObject(uint oObject, string sVarName)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(54);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Set oObject's local integer variable sVarName to nValue
        /// </summary>
        public static void SetLocalInt(uint oObject, string sVarName, int nValue)
        {
            Internal.NativeFunctions.StackPushInteger(nValue);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(55);
        }

        /// <summary>
        /// Set oObject's local boolean variable sVarName to nValue
        /// </summary>
        public static void SetLocalBool(uint oObject, string sVarName, bool nValue)
        {
            SetLocalInt(oObject, sVarName, Convert.ToInt32(nValue));
        }

        /// <summary>
        ///   Set oObject's local float variable sVarName to nValue
        /// </summary>
        public static void SetLocalFloat(uint oObject, string sVarName, float fValue)
        {
            Internal.NativeFunctions.StackPushFloat(fValue);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(56);
        }

        /// <summary>
        ///   Set oObject's local string variable sVarName to nValue
        /// </summary>
        public static void SetLocalString(uint oObject, string sVarName, string sValue)
        {
            Internal.NativeFunctions.StackPushString(sValue);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(57);
        }

        /// <summary>
        ///   Set oObject's local object variable sVarName to nValue
        /// </summary>
        public static void SetLocalObject(uint oObject, string sVarName, uint oValue)
        {
            Internal.NativeFunctions.StackPushObject(oValue);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(58);
        }

        /// <summary>
        ///   Convert nInteger into a string.
        ///   * Return value on error: ""
        /// </summary>
        public static string IntToString(int nInteger)
        {
            Internal.NativeFunctions.StackPushInteger(nInteger);
            Internal.NativeFunctions.CallBuiltIn(92);
            return Internal.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d2 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d2(int nNumDice = 1)
        {
            Internal.NativeFunctions.StackPushInteger(nNumDice);
            Internal.NativeFunctions.CallBuiltIn(95);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d3 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d3(int nNumDice = 1)
        {
            Internal.NativeFunctions.StackPushInteger(nNumDice);
            Internal.NativeFunctions.CallBuiltIn(96);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d4 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d4(int nNumDice = 1)
        {
            Internal.NativeFunctions.StackPushInteger(nNumDice);
            Internal.NativeFunctions.CallBuiltIn(97);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d6 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d6(int nNumDice = 1)
        {
            Internal.NativeFunctions.StackPushInteger(nNumDice);
            Internal.NativeFunctions.CallBuiltIn(98);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d8 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d8(int nNumDice = 1)
        {
            Internal.NativeFunctions.StackPushInteger(nNumDice);
            Internal.NativeFunctions.CallBuiltIn(99);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d10 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d10(int nNumDice = 1)
        {
            Internal.NativeFunctions.StackPushInteger(nNumDice);
            Internal.NativeFunctions.CallBuiltIn(100);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d12 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d12(int nNumDice = 1)
        {
            Internal.NativeFunctions.StackPushInteger(nNumDice);
            Internal.NativeFunctions.CallBuiltIn(101);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d20 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d20(int nNumDice = 1)
        {
            Internal.NativeFunctions.StackPushInteger(nNumDice);
            Internal.NativeFunctions.CallBuiltIn(102);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d100 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d100(int nNumDice = 1)
        {
            Internal.NativeFunctions.StackPushInteger(nNumDice);
            Internal.NativeFunctions.CallBuiltIn(103);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the metamagic type (METAMAGIC_*) of the last spell cast by the caller
        ///   * Return value if the caster is not a valid object: -1
        /// </summary>
        public static int GetMetaMagicFeat()
        {
            Internal.NativeFunctions.CallBuiltIn(105);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Do a Fortitude Save check for the given DC
        ///   - oCreature
        ///   - nDC: Difficulty check
        ///   - nSaveType: SAVING_THROW_TYPE_*
        ///   - oSaveVersus
        ///   Returns: 0 if the saving throw roll failed
        ///   Returns: 1 if the saving throw roll succeeded
        ///   Returns: 2 if the target was immune to the save type specified
        ///   Note: If used within an Area of Effect Object Script (On Enter, OnExit, OnHeartbeat), you MUST pass
        ///   GetAreaOfEffectCreator() into oSaveVersus!!
        /// </summary>
        public static int FortitudeSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All,
            uint oSaveVersus = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oSaveVersus);
            Internal.NativeFunctions.StackPushInteger((int)nSaveType);
            Internal.NativeFunctions.StackPushInteger(nDC);
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.CallBuiltIn(108);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Does a Reflex Save check for the given DC
        ///   - oCreature
        ///   - nDC: Difficulty check
        ///   - nSaveType: SAVING_THROW_TYPE_*
        ///   - oSaveVersus
        ///   Returns: 0 if the saving throw roll failed
        ///   Returns: 1 if the saving throw roll succeeded
        ///   Returns: 2 if the target was immune to the save type specified
        ///   Note: If used within an Area of Effect Object Script (On Enter, OnExit, OnHeartbeat), you MUST pass
        ///   GetAreaOfEffectCreator() into oSaveVersus!!
        /// </summary>
        public static SaveReturn ReflexSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All,
            uint oSaveVersus = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oSaveVersus);
            Internal.NativeFunctions.StackPushInteger((int)nSaveType);
            Internal.NativeFunctions.StackPushInteger(nDC);
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.CallBuiltIn(109);
            return (SaveReturn)Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Does a Will Save check for the given DC
        ///   - oCreature
        ///   - nDC: Difficulty check
        ///   - nSaveType: SAVING_THROW_TYPE_*
        ///   - oSaveVersus
        ///   Returns: 0 if the saving throw roll failed
        ///   Returns: 1 if the saving throw roll succeeded
        ///   Returns: 2 if the target was immune to the save type specified
        ///   Note: If used within an Area of Effect Object Script (On Enter, OnExit, OnHeartbeat), you MUST pass
        ///   GetAreaOfEffectCreator() into oSaveVersus!!
        /// </summary>
        public static int WillSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All,
            uint oSaveVersus = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oSaveVersus);
            Internal.NativeFunctions.StackPushInteger((int)nSaveType);
            Internal.NativeFunctions.StackPushInteger(nDC);
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.CallBuiltIn(110);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the DC to save against for a spell (10 + spell level + relevant ability
        ///   bonus).  This can be called by a creature or by an Area of Effect object.
        /// </summary>
        public static int GetSpellSaveDC()
        {
            Internal.NativeFunctions.CallBuiltIn(111);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the first object in nShape
        ///   - nShape: SHAPE_*
        ///   - fSize:
        ///   -> If nShape == SHAPE_SPHERE, this is the radius of the sphere
        ///   -> If nShape == SHAPE_SPELLCYLINDER, this is the length of the cylinder
        ///   Spell Cylinder's always have a radius of 1.5m.
        ///   -> If nShape == SHAPE_CONE, this is the widest radius of the cone
        ///   -> If nShape == SHAPE_SPELLCONE, this is the length of the cone in the
        ///   direction of lTarget. Spell cones are always 60 degrees with the origin
        ///   at OBJECT_SELF.
        ///   -> If nShape == SHAPE_CUBE, this is half the length of one of the sides of
        ///   the cube
        ///   - lTarget: This is the centre of the effect, usually GetSpellTargetLocation(),
        ///   or the end of a cylinder or cone.
        ///   - bLineOfSight: This controls whether to do a line-of-sight check on the
        ///   object returned. Line of sight check is done from origin to target object
        ///   at a height 1m above the ground
        ///   (This can be used to ensure that spell effects do not go through walls.)
        ///   - nObjectFilter: This allows you to filter out undesired object types, using
        ///   bitwise "or".
        ///   For example, to return only creatures and doors, the value for this
        ///   parameter would be OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR
        ///   - vOrigin: This is only used for cylinders and cones, and specifies the
        ///   origin of the effect(normally the spell-caster's position).
        ///   Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetFirstObjectInShape(Shape nShape, float fSize, Location lTarget, bool bLineOfSight = false,
            ObjectType nObjectFilter = ObjectType.Creature, Vector3 vOrigin = default)
        {
            Internal.NativeFunctions.StackPushVector(vOrigin);
            Internal.NativeFunctions.StackPushInteger((int)nObjectFilter);
            Internal.NativeFunctions.StackPushInteger(bLineOfSight ? 1 : 0);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lTarget);
            Internal.NativeFunctions.StackPushFloat(fSize);
            Internal.NativeFunctions.StackPushInteger((int)nShape);
            Internal.NativeFunctions.CallBuiltIn(128);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the next object in nShape
        ///   - nShape: SHAPE_*
        ///   - fSize:
        ///   -> If nShape == SHAPE_SPHERE, this is the radius of the sphere
        ///   -> If nShape == SHAPE_SPELLCYLINDER, this is the length of the cylinder.
        ///   Spell Cylinder's always have a radius of 1.5m.
        ///   -> If nShape == SHAPE_CONE, this is the widest radius of the cone
        ///   -> If nShape == SHAPE_SPELLCONE, this is the length of the cone in the
        ///   direction of lTarget. Spell cones are always 60 degrees with the origin
        ///   at OBJECT_SELF.
        ///   -> If nShape == SHAPE_CUBE, this is half the length of one of the sides of
        ///   the cube
        ///   - lTarget: This is the centre of the effect, usually GetSpellTargetLocation(),
        ///   or the end of a cylinder or cone.
        ///   - bLineOfSight: This controls whether to do a line-of-sight check on the
        ///   object returned. (This can be used to ensure that spell effects do not go
        ///   through walls.) Line of sight check is done from origin to target object
        ///   at a height 1m above the ground
        ///   - nObjectFilter: This allows you to filter out undesired object types, using
        ///   bitwise "or". For example, to return only creatures and doors, the value for
        ///   this parameter would be OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR
        ///   - vOrigin: This is only used for cylinders and cones, and specifies the origin
        ///   of the effect (normally the spell-caster's position).
        ///   Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetNextObjectInShape(Shape nShape, float fSize, Location lTarget, bool bLineOfSight = false,
            ObjectType nObjectFilter = ObjectType.Creature, Vector3 vOrigin = default)
        {
            Internal.NativeFunctions.StackPushVector(vOrigin);
            Internal.NativeFunctions.StackPushInteger((int)nObjectFilter);
            Internal.NativeFunctions.StackPushInteger(bLineOfSight ? 1 : 0);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lTarget);
            Internal.NativeFunctions.StackPushFloat(fSize);
            Internal.NativeFunctions.StackPushInteger((int)nShape);
            Internal.NativeFunctions.CallBuiltIn(129);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Causes object oObject to run the event evToRun. The script on the object that is
        ///   associated with the event specified will run.
        ///   Events can be created using the following event functions:
        ///   EventActivateItem() - This creates an OnActivateItem module event. The script for handling
        ///   this event can be set in Module Properties on the Event Tab.
        ///   EventConversation() - This creates on OnConversation creature event. The script for handling
        ///   this event can be set by viewing the Creature Properties on a
        ///   creature and then clicking on the Scripts Tab.
        ///   EventSpellCastAt()  - This creates an OnSpellCastAt event. The script for handling this
        ///   event can be set in the Scripts Tab of the Properties menu
        ///   for the object.
        ///   EventUserDefined()  - This creates on OnUserDefined event. The script for handling this event
        ///   can be set in the Scripts Tab of the Properties menu for the object/area/module.
        /// </summary>
        public static void SignalEvent(uint oObject, Event evToRun)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Event, evToRun);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(131);
        }

        /// <summary>
        ///   Create an event of the type nUserDefinedEventNumber
        ///   Note: This only creates the event. The event wont actually trigger until SignalEvent()
        ///   is called using this created UserDefined event as an argument.
        ///   For example:
        ///   SignalEvent(oObject, EventUserDefined(9999));
        ///   Once the event has been signaled. The script associated with the OnUserDefined event will
        ///   run on the object oObject.
        ///   To specify the OnUserDefined script that should run, view the object's Properties
        ///   and click on the Scripts Tab. Then specify a script for the OnUserDefined event.
        ///   From inside the OnUserDefined script call:
        ///   GetUserDefinedEventNumber() to retrieve the value of nUserDefinedEventNumber
        ///   that was used when the event was signaled.
        /// </summary>
        public static Event EventUserDefined(int nUserDefinedEventNumber)
        {
            Internal.NativeFunctions.StackPushInteger(nUserDefinedEventNumber);
            Internal.NativeFunctions.CallBuiltIn(132);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Event);
        }

        /// <summary>
        ///   Get the ability score of type nAbility for a creature (otherwise 0)
        ///   - oCreature: the creature whose ability score we wish to find out
        ///   - nAbilityType: ABILITY_*
        ///   - nBaseAbilityScore: if set to true will return the base ability score without
        ///   bonuses (e.g. ability bonuses granted from equipped items).
        ///   Return value on error: 0
        /// </summary>
        public static int GetAbilityScore(uint oCreature, AbilityType nAbilityType, bool nBaseAbilityScore = false)
        {
            Internal.NativeFunctions.StackPushInteger(nBaseAbilityScore ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger((int)nAbilityType);
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.CallBuiltIn(139);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   * Returns TRUE if oCreature is a dead NPC, dead PC or a dying PC.
        /// </summary>
        public static bool GetIsDead(uint oCreature)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.CallBuiltIn(140);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Output vVector to the logfile.
        ///   - vVector
        ///   - bPrepend: if this is TRUE, the message will be prefixed with "PRINTVECTOR:"
        /// </summary>
        public static void PrintVector(Vector3 vVector, bool bPrepend = false)
        {
            Internal.NativeFunctions.StackPushInteger(bPrepend ? 1 : 0);
            Internal.NativeFunctions.StackPushVector(vVector);
            Internal.NativeFunctions.CallBuiltIn(141);
        }

        /// <summary>
        ///   Create a vector with the specified values for x, y and z
        /// </summary>
        public static Vector3 Vector3(float x = 0.0f, float y = 0.0f, float z = 0.0f)
        {
            Internal.NativeFunctions.StackPushFloat(z);
            Internal.NativeFunctions.StackPushFloat(y);
            Internal.NativeFunctions.StackPushFloat(x);
            Internal.NativeFunctions.CallBuiltIn(142);
            return Internal.NativeFunctions.StackPopVector();
        }

        /// <summary>
        ///   Cause the caller to face vTarget
        /// </summary>
        public static void SetFacingPoint(Vector3 vTarget)
        {
            Internal.NativeFunctions.StackPushVector(vTarget);
            Internal.NativeFunctions.CallBuiltIn(143);
        }

        /// <summary>
        ///   The caller will perform a Melee Touch Attack on oTarget
        ///   This is not an action, and it assumes the caller is already within range of
        ///   oTarget
        ///   * Returns 0 on a miss, 1 on a hit and 2 on a critical hit
        /// </summary>
        public static TouchAttackReturn TouchAttackMelee(uint oTarget, bool bDisplayFeedback = true)
        {
            Internal.NativeFunctions.StackPushInteger(bDisplayFeedback ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(146);
            return (TouchAttackReturn)Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   The caller will perform a Ranged Touch Attack on oTarget
        ///   * Returns 0 on a miss, 1 on a hit and 2 on a critical hit
        /// </summary>
        public static TouchAttackReturn TouchAttackRanged(uint oTarget, bool bDisplayFeedback = true)
        {
            Internal.NativeFunctions.StackPushInteger(bDisplayFeedback ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(147);
            return (TouchAttackReturn)Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the distance in metres between oObjectA and oObjectB.
        ///   * Return value if either object is invalid: 0.0f
        /// </summary>
        public static float GetDistanceBetween(uint oObjectA, uint oObjectB)
        {
            Internal.NativeFunctions.StackPushObject(oObjectB);
            Internal.NativeFunctions.StackPushObject(oObjectA);
            Internal.NativeFunctions.CallBuiltIn(151);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Set oObject's local location variable sVarname to lValue
        /// </summary>
        public static void SetLocalLocation(uint oObject, string sVarName, Location lValue)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lValue);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(152);
        }

        /// <summary>
        ///   Get oObject's local location variable sVarname
        /// </summary>
        public static Location GetLocalLocation(uint oObject, string sVarName)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(153);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Location);
        }

        /// <summary>
        ///   Set whether oTarget's action stack can be modified
        /// </summary>
        public static void SetCommandable(bool nCommandable, uint oTarget = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushInteger(nCommandable ? 1 : 0);
            Internal.NativeFunctions.CallBuiltIn(162);
        }

        /// <summary>
        ///   Determine whether oTarget's action stack can be modified.
        /// </summary>
        public static bool GetCommandable(uint oTarget = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(163);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Get the number of hitdice for oCreature.
        ///   * Return value if oCreature is not a valid creature: 0
        /// </summary>
        public static int GetHitDice(uint oCreature)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.CallBuiltIn(166);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   The action subject will follow oFollow until a ClearAllActions() is called.
        ///   - oFollow: this is the object to be followed
        ///   - fFollowDistance: follow distance in metres
        ///   * No return value
        /// </summary>
        public static void ActionForceFollowObject(uint oFollow, float fFollowDistance = 0.0f)
        {
            Internal.NativeFunctions.StackPushFloat(fFollowDistance);
            Internal.NativeFunctions.StackPushObject(oFollow);
            Internal.NativeFunctions.CallBuiltIn(167);
        }

        /// <summary>
        ///   Get the Tag of oObject
        ///   * Return value if oObject is not a valid object: ""
        /// </summary>
        public static string GetTag(uint oObject)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(168);
            return Internal.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Do a Spell Resistance check between oCaster and oTarget, returning TRUE if
        ///   the spell was resisted.
        ///   * Return value if oCaster or oTarget is an invalid object: FALSE
        ///   * Return value if spell cast is not a player spell: - 1
        ///   * Return value if spell resisted: 1
        ///   * Return value if spell resisted via magic immunity: 2
        ///   * Return value if spell resisted via spell absorption: 3
        /// </summary>
        public static int ResistSpell(uint oCaster, uint oTarget)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushObject(oCaster);
            Internal.NativeFunctions.CallBuiltIn(169);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   * Returns TRUE if oObject is listening for something
        /// </summary>
        public static bool GetIsListening(uint oObject)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(174);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Set whether oObject is listening.
        /// </summary>
        public static void SetListening(uint oObject, bool bValue)
        {
            Internal.NativeFunctions.StackPushInteger(bValue ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(175);
        }

        /// <summary>
        ///   Set the string for oObject to listen for.
        ///   Note: this does not set oObject to be listening.
        /// </summary>
        public static void SetListenPattern(uint oObject, string sPattern, int nNumber = 0)
        {
            Internal.NativeFunctions.StackPushInteger(nNumber);
            Internal.NativeFunctions.StackPushString(sPattern);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(176);
        }

        /// <summary>
        ///   Sit in oChair.
        ///   Note: Not all creatures will be able to sit and not all
        ///   objects can be sat on.
        ///   The object oChair must also be marked as usable in the toolset.
        ///   For Example: To get a player to sit in oChair when they click on it,
        ///   place the following script in the OnUsed event for the object oChair.
        ///   void main()
        ///   {
        ///   object oChair = OBJECT_SELF;
        ///   AssignCommand(GetLastUsedBy(),ActionSit(oChair));
        ///   }
        /// </summary>
        public static void ActionSit(uint oChair)
        {
            Internal.NativeFunctions.StackPushObject(oChair);
            Internal.NativeFunctions.CallBuiltIn(194);
        }

        /// <summary>
        ///   In an onConversation script this gets the number of the string pattern
        ///   matched (the one that triggered the script).
        ///   * Returns -1 if no string matched
        /// </summary>
        public static int GetListenPatternNumber()
        {
            Internal.NativeFunctions.CallBuiltIn(195);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Jump to an object ID, or as near to it as possible.
        /// </summary>
        public static void ActionJumpToObject(uint oToJumpTo, bool bWalkStraightLineToPoint = true)
        {
            Internal.NativeFunctions.StackPushInteger(bWalkStraightLineToPoint ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oToJumpTo);
            Internal.NativeFunctions.CallBuiltIn(196);
        }

        /// <summary>
        ///   Get the first waypoint with the specified tag.
        ///   * Returns OBJECT_INVALID if the waypoint cannot be found.
        /// </summary>
        public static uint GetWaypointByTag(string sWaypointTag)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sWaypointTag);
            Internal.NativeFunctions.CallBuiltIn(197);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the destination object for the given object.
        ///   All objects can hold a transition target, but only Doors and Triggers
        ///   will be made clickable by the game engine (This may change in the
        ///   future). You can set and query transition targets on other objects for
        ///   your own scripted purposes.
        ///   * Returns OBJECT_INVALID if oTransition does not hold a target.
        /// </summary>
        public static uint GetTransitionTarget(uint oTransition)
        {
            Internal.NativeFunctions.StackPushObject(oTransition);
            Internal.NativeFunctions.CallBuiltIn(198);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the nNth object with the specified tag.
        ///   - sTag
        ///   - nNth: the nth object with this tag may be requested
        ///   * Returns OBJECT_INVALID if the object cannot be found.
        ///   Note: The module cannot be retrieved by GetObjectByTag(), use GetModule() instead.
        /// </summary>
        public static uint GetObjectByTag(string sTag, int nNth = 0)
        {
            Internal.NativeFunctions.StackPushInteger(nNth);
            Internal.NativeFunctions.StackPushStringUTF8(sTag);
            Internal.NativeFunctions.CallBuiltIn(200);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Do nothing for fSeconds seconds.
        /// </summary>
        public static void ActionWait(float fSeconds)
        {
            Internal.NativeFunctions.StackPushFloat(fSeconds);
            Internal.NativeFunctions.CallBuiltIn(202);
        }

        /// <summary>
        ///   Starts a conversation with oObjectToConverseWith - this will cause their
        ///   OnDialog event to fire.
        ///   - oObjectToConverseWith
        ///   - sDialogResRef: If this is blank, the creature's own dialogue file will be used
        ///   - bPrivateConversation
        ///   Turn off bPlayHello if you don't want the initial greeting to play
        /// </summary>
        public static void ActionStartConversation(uint oObjectToConverseWith, string sDialogResRef = "",
            bool bPrivateConversation = true, bool bPlayHello = true)
        {
            Internal.NativeFunctions.StackPushInteger(bPlayHello ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger(bPrivateConversation ? 1 : 0);
            Internal.NativeFunctions.StackPushStringUTF8(sDialogResRef);
            Internal.NativeFunctions.StackPushObject(oObjectToConverseWith);
            Internal.NativeFunctions.CallBuiltIn(204);
        }

        /// <summary>
        ///   Pause the current conversation.
        /// </summary>
        public static void ActionPauseConversation()
        {
            Internal.NativeFunctions.CallBuiltIn(205);
        }


        /// <summary>
        ///   Resume a conversation after it has been paused.
        /// </summary>
        public static void ActionResumeConversation()
        {
            Internal.NativeFunctions.CallBuiltIn(206);
        }


        /// <summary>
        ///   Get the creature that is currently sitting on the specified object.
        ///   - oChair
        ///   * Returns OBJECT_INVALID if oChair is not a valid placeable.
        /// </summary>
        public static uint GetSittingCreature(uint oChair)
        {
            Internal.NativeFunctions.StackPushObject(oChair);
            Internal.NativeFunctions.CallBuiltIn(210);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the creature that is going to attack oTarget.
        ///   Note: This value is cleared out at the end of every combat round and should
        ///   not be used in any case except when getting a "going to be attacked" shout
        ///   from the master creature (and this creature is a henchman)
        ///   * Returns OBJECT_INVALID if oTarget is not a valid creature.
        /// </summary>
        public static uint GetGoingToBeAttackedBy(uint oTarget)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(211);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   * Returns TRUE if oCreature is a Player Controlled character.
        /// </summary>
        public static bool GetIsPC(uint oCreature)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.CallBuiltIn(217);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   The caller will immediately speak sStringToSpeak (this is different from
        ///   ActionSpeakString)
        ///   - sStringToSpeak
        ///   - nTalkVolume: TALKVOLUME_*
        /// </summary>
        public static void SpeakString(string sStringToSpeak, TalkVolume nTalkVolume = TalkVolume.Talk)
        {
            Internal.NativeFunctions.StackPushInteger((int)nTalkVolume);
            Internal.NativeFunctions.StackPushString(sStringToSpeak);
            Internal.NativeFunctions.CallBuiltIn(221);
        }

        /// <summary>
        ///   Get the location of the caller's last spell target.
        /// </summary>
        public static Location GetSpellTargetLocation()
        {
            Internal.NativeFunctions.CallBuiltIn(222);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Location);
        }

        /// <summary>
        ///   Get the orientation value from lLocation.
        /// </summary>
        public static float GetFacingFromLocation(Location lLocation)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lLocation);
            Internal.NativeFunctions.CallBuiltIn(225);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Convert nInteger into a floating point number.
        /// </summary>
        public static float IntToFloat(int nInteger)
        {
            Internal.NativeFunctions.StackPushInteger(nInteger);
            Internal.NativeFunctions.CallBuiltIn(230);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Convert fFloat into the nearest integer.
        /// </summary>
        public static int FloatToInt(float fFloat)
        {
            Internal.NativeFunctions.StackPushFloat(fFloat);
            Internal.NativeFunctions.CallBuiltIn(231);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Convert sNumber into an integer.
        /// </summary>
        public static int StringToInt(string sNumber)
        {
            Internal.NativeFunctions.StackPushString(sNumber);
            Internal.NativeFunctions.CallBuiltIn(232);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Convert sNumber into a floating point number.
        /// </summary>
        public static float StringToFloat(string sNumber)
        {
            Internal.NativeFunctions.StackPushString(sNumber);
            Internal.NativeFunctions.CallBuiltIn(233);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Cast spell nSpell at lTargetLocation.
        ///   - nSpell: SPELL_*
        ///   - lTargetLocation
        ///   - nMetaMagic: METAMAGIC_*
        ///   - bCheat: If this is TRUE, then the executor of the action doesn't have to be
        ///   able to cast the spell.
        ///   - nProjectilePathType: PROJECTILE_PATH_TYPE_*
        ///   - bInstantSpell: If this is TRUE, the spell is cast immediately; this allows
        ///   the end-user to simulate
        ///   a high-level magic user having lots of advance warning of impending trouble.
        /// </summary>
        public static void ActionCastSpellAtLocation(Spell nSpell, Location lTargetLocation,
            MetaMagic nMetaMagic = MetaMagic.Any, bool bCheat = false,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default, bool bInstantSpell = false)
        {
            Internal.NativeFunctions.StackPushInteger(bInstantSpell ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger((int)nProjectilePathType);
            Internal.NativeFunctions.StackPushInteger(bCheat ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger((int)nMetaMagic);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lTargetLocation);
            Internal.NativeFunctions.StackPushInteger((int)nSpell);
            Internal.NativeFunctions.CallBuiltIn(234);
        }

        /// <summary>
        ///   Get the PC that is involved in the conversation.
        ///   * Returns OBJECT_INVALID on error.
        /// </summary>
        public static uint GetPCSpeaker()
        {
            Internal.NativeFunctions.CallBuiltIn(238);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get a string from the talk table using nStrRef.
        /// </summary>
        public static string GetStringByStrRef(int nStrRef, Gender nGender = Gender.Male)
        {
            Internal.NativeFunctions.StackPushInteger((int)nGender);
            Internal.NativeFunctions.StackPushInteger(nStrRef);
            Internal.NativeFunctions.CallBuiltIn(239);
            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        ///   Causes the creature to speak a translated string.
        ///   - nStrRef: Reference of the string in the talk table
        ///   - nTalkVolume: TALKVOLUME_*
        /// </summary>
        public static void ActionSpeakStringByStrRef(int nStrRef, TalkVolume nTalkVolume = TalkVolume.Talk)
        {
            Internal.NativeFunctions.StackPushInteger((int)nTalkVolume);
            Internal.NativeFunctions.StackPushInteger(nStrRef);
            Internal.NativeFunctions.CallBuiltIn(240);
        }

        /// <summary>
        ///   Get the module.
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetModule()
        {
            Internal.NativeFunctions.CallBuiltIn(242);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Create an event which triggers the "SpellCastAt" script
        ///   Note: This only creates the event. The event wont actually trigger until SignalEvent()
        ///   is called using this created SpellCastAt event as an argument.
        ///   For example:
        ///   SignalEvent(oCreature, EventSpellCastAt(oCaster, SPELL_MAGIC_MISSILE, TRUE));
        ///   This function doesn't cast the spell specified, it only creates an event so that
        ///   when the event is signaled on an object, the object will use its OnSpellCastAt script
        ///   to react to the spell being cast.
        ///   To specify the OnSpellCastAt script that should run, view the Object's Properties
        ///   and click on the Scripts Tab. Then specify a script for the OnSpellCastAt event.
        ///   From inside the OnSpellCastAt script call:
        ///   GetLastSpellCaster() to get the object that cast the spell (oCaster).
        ///   GetLastSpell() to get the type of spell cast (nSpell)
        ///   GetLastSpellHarmful() to determine if the spell cast at the object was harmful.
        /// </summary>
        public static Event EventSpellCastAt(uint oCaster, Spell nSpell, bool bHarmful = true)
        {
            Internal.NativeFunctions.StackPushInteger(bHarmful ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger((int)nSpell);
            Internal.NativeFunctions.StackPushObject(oCaster);
            Internal.NativeFunctions.CallBuiltIn(244);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Event);
        }

        /// <summary>
        ///   This is for use in a "Spell Cast" script, it gets who cast the spell.
        ///   The spell could have been cast by a creature, placeable or door.
        ///   * Returns OBJECT_INVALID if the caller is not a creature, placeable or door.
        /// </summary>
        public static uint GetLastSpellCaster()
        {
            Internal.NativeFunctions.CallBuiltIn(245);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   This is for use in a "Spell Cast" script, it gets the ID of the spell that
        ///   was cast.
        /// </summary>
        public static int GetLastSpell()
        {
            Internal.NativeFunctions.CallBuiltIn(246);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   This is for use in a user-defined script, it gets the event number.
        /// </summary>
        public static int GetUserDefinedEventNumber()
        {
            Internal.NativeFunctions.CallBuiltIn(247);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   This is for use in a Spell script, it gets the ID of the spell that is being
        ///   cast (SPELL_*).
        /// </summary>
        public static int GetSpellId()
        {
            Internal.NativeFunctions.CallBuiltIn(248);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Generate a random name.
        ///   nNameType: The type of random name to be generated (NAME_*)
        /// </summary>
        public static string RandomName(Name nNameType = Name.FirstGenericMale)
        {
            Internal.NativeFunctions.StackPushInteger((int)nNameType);
            Internal.NativeFunctions.CallBuiltIn(249);
            return Internal.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Set the name of oObject.
        ///   - oObject: the object for which you are changing the name (area, creature, placeable, item, or door).
        ///   - sNewName: the new name that the object will use.
        ///   Note: SetName() does not work on player objects.
        ///   Setting an object's name to "" will make the object
        ///   revert to using the name it had originally before any
        ///   SetName() calls were made on the object.
        /// </summary>
        public static string GetName(uint oObject, bool bOriginalName = false)
        {
            Internal.NativeFunctions.StackPushInteger(bOriginalName ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(253);
            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        ///   Use this in a conversation script to get the person with whom you are conversing.
        ///   * Returns OBJECT_INVALID if the caller is not a valid creature.
        /// </summary>
        public static uint GetLastSpeaker()
        {
            Internal.NativeFunctions.CallBuiltIn(254);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnDialog script to start up the dialog tree.
        ///   - sResRef: if this is not specified, the default dialog file will be used
        ///   - oObjectToDialog: if this is not specified the person that triggered the
        ///   event will be used
        /// </summary>
        public static int BeginConversation(string sResRef = "", uint oObjectToDialog = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oObjectToDialog);
            Internal.NativeFunctions.StackPushStringUTF8(sResRef);
            Internal.NativeFunctions.CallBuiltIn(255);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Use this in an OnPerception script to get the object that was perceived.
        ///   * Returns OBJECT_INVALID if the caller is not a valid creature.
        /// </summary>
        public static uint GetLastPerceived()
        {
            Internal.NativeFunctions.CallBuiltIn(256);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnPerception script to determine whether the object that was
        ///   perceived was heard.
        /// </summary>
        public static bool GetLastPerceptionHeard()
        {
            Internal.NativeFunctions.CallBuiltIn(257);
            return Convert.ToBoolean(Internal.NativeFunctions.StackPopInteger());
        }

        /// <summary>
        ///   Use this in an OnPerception script to determine whether the object that was
        ///   perceived has become inaudible.
        /// </summary>
        public static bool GetLastPerceptionInaudible()
        {
            Internal.NativeFunctions.CallBuiltIn(258);
            return Convert.ToBoolean(Internal.NativeFunctions.StackPopInteger());
        }

        /// <summary>
        ///   Use this in an OnPerception script to determine whether the object that was
        ///   perceived was seen.
        /// </summary>
        public static bool GetLastPerceptionSeen()
        {
            Internal.NativeFunctions.CallBuiltIn(259);
            return Convert.ToBoolean(Internal.NativeFunctions.StackPopInteger());
        }

        /// <summary>
        ///   Use this in an OnClosed script to get the object that closed the door or placeable.
        ///   * Returns OBJECT_INVALID if the caller is not a valid door or placeable.
        /// </summary>
        public static uint GetLastClosedBy()
        {
            Internal.NativeFunctions.CallBuiltIn(260);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnPerception script to determine whether the object that was
        ///   perceived has vanished.
        /// </summary>
        public static bool GetLastPerceptionVanished()
        {
            Internal.NativeFunctions.CallBuiltIn(261);
            return Convert.ToBoolean(Internal.NativeFunctions.StackPopInteger());
        }

        /// <summary>
        ///   Get the first object within oPersistentObject.
        ///   - oPersistentObject
        ///   - nResidentObjectType: OBJECT_TYPE_*
        ///   - nPersistentZone: PERSISTENT_ZONE_ACTIVE. [This could also take the value
        ///   PERSISTENT_ZONE_FOLLOW, but this is no longer used.]
        ///   * Returns OBJECT_INVALID if no object is found.
        /// </summary>
        public static uint GetFirstInPersistentObject(uint oPersistentObject = OBJECT_INVALID,
            ObjectType nResidentObjectType = ObjectType.Creature,
            PersistentZone nPersistentZone = PersistentZone.Active)
        {
            Internal.NativeFunctions.StackPushInteger((int)nPersistentZone);
            Internal.NativeFunctions.StackPushInteger((int)nResidentObjectType);
            Internal.NativeFunctions.StackPushObject(oPersistentObject);
            Internal.NativeFunctions.CallBuiltIn(262);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the next object within oPersistentObject.
        ///   - oPersistentObject
        ///   - nResidentObjectType: OBJECT_TYPE_*
        ///   - nPersistentZone: PERSISTENT_ZONE_ACTIVE. [This could also take the value
        ///   PERSISTENT_ZONE_FOLLOW, but this is no longer used.]
        ///   * Returns OBJECT_INVALID if no object is found.
        /// </summary>
        public static uint GetNextInPersistentObject(uint oPersistentObject = OBJECT_INVALID,
            ObjectType nResidentObjectType = ObjectType.Creature,
            PersistentZone nPersistentZone = PersistentZone.Active)
        {
            Internal.NativeFunctions.StackPushInteger((int)nPersistentZone);
            Internal.NativeFunctions.StackPushInteger((int)nResidentObjectType);
            Internal.NativeFunctions.StackPushObject(oPersistentObject);
            Internal.NativeFunctions.CallBuiltIn(263);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   This returns the creator of oAreaOfEffectObject.
        ///   * Returns OBJECT_INVALID if oAreaOfEffectObject is not a valid Area of Effect object.
        /// </summary>
        public static uint GetAreaOfEffectCreator(uint oAreaOfEffectObject = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oAreaOfEffectObject);
            Internal.NativeFunctions.CallBuiltIn(264);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Delete oObject's local integer variable sVarName
        /// </summary>
        public static void DeleteLocalInt(uint oObject, string sVarName)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(265);
        }

        /// <summary>
        /// Delete oObject's local boolean variable sVarName
        /// </summary>
        public static void DeleteLocalBool(uint oObject, string sVarName)
        {
            DeleteLocalInt(oObject, sVarName);
        }

        /// <summary>
        ///   Delete oObject's local float variable sVarName
        /// </summary>
        public static void DeleteLocalFloat(uint oObject, string sVarName)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(266);
        }

        /// <summary>
        ///   Delete oObject's local string variable sVarName
        /// </summary>
        public static void DeleteLocalString(uint oObject, string sVarName)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(267);
        }

        /// <summary>
        ///   Delete oObject's local object variable sVarName
        /// </summary>
        public static void DeleteLocalObject(uint oObject, string sVarName)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(268);
        }

        /// <summary>
        ///   Delete oObject's local location variable sVarName
        /// </summary>
        public static void DeleteLocalLocation(uint oObject, string sVarName)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(269);
        }

        /// <summary>
        ///   Convert oObject into a hexadecimal string.
        /// </summary>
        public static string ObjectToString(uint oObject)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(272);
            return Internal.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   - oCreature
        ///   - nImmunityType: IMMUNITY_TYPE_*
        ///   - oVersus: if this is specified, then we also check for the race and
        ///   alignment of oVersus
        ///   * Returns TRUE if oCreature has immunity of type nImmunity versus oVersus.
        /// </summary>
        public static bool GetIsImmune(uint oCreature, ImmunityType nImmunityType, uint oVersus = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oVersus);
            Internal.NativeFunctions.StackPushInteger((int)nImmunityType);
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.CallBuiltIn(274);
            return Internal.NativeFunctions.StackPopInteger() == 1;
        }

        /// <summary>
        ///   Determine whether oEncounter is active.
        /// </summary>
        public static int GetEncounterActive(uint oEncounter = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oEncounter);
            Internal.NativeFunctions.CallBuiltIn(276);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Set oEncounter's active state to nNewValue.
        ///   - nNewValue: TRUE/FALSE
        ///   - oEncounter
        /// </summary>
        public static void SetEncounterActive(int nNewValue, uint oEncounter = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oEncounter);
            Internal.NativeFunctions.StackPushInteger(nNewValue);
            Internal.NativeFunctions.CallBuiltIn(277);
        }

        /// <summary>
        ///   Get the maximum number of times that oEncounter will spawn.
        /// </summary>
        public static int GetEncounterSpawnsMax(uint oEncounter = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oEncounter);
            Internal.NativeFunctions.CallBuiltIn(278);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Set the maximum number of times that oEncounter can spawn
        /// </summary>
        public static void SetEncounterSpawnsMax(int nNewValue, uint oEncounter = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oEncounter);
            Internal.NativeFunctions.StackPushInteger(nNewValue);
            Internal.NativeFunctions.CallBuiltIn(279);
        }

        /// <summary>
        ///   Get the number of times that oEncounter has spawned so far
        /// </summary>
        public static int GetEncounterSpawnsCurrent(uint oEncounter = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oEncounter);
            Internal.NativeFunctions.CallBuiltIn(280);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Set the number of times that oEncounter has spawned so far
        /// </summary>
        public static void SetEncounterSpawnsCurrent(int nNewValue, uint oEncounter = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oEncounter);
            Internal.NativeFunctions.StackPushInteger(nNewValue);
            Internal.NativeFunctions.CallBuiltIn(281);
        }

        /// <summary>
        ///   Set the value for a custom token.
        /// </summary>
        public static void SetCustomToken(int nCustomTokenNumber, string sTokenValue)
        {
            Internal.NativeFunctions.StackPushString(sTokenValue);
            Internal.NativeFunctions.StackPushInteger(nCustomTokenNumber);
            Internal.NativeFunctions.CallBuiltIn(284);
        }

        /// <summary>
        ///   Determine whether oCreature has nFeat, and nFeat is useable.
        ///   - nFeat: FEAT_*
        ///   - oCreature
        /// </summary>
        public static bool GetHasFeat(FeatType nFeat, uint oCreature = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.StackPushInteger((int)nFeat);
            Internal.NativeFunctions.CallBuiltIn(285);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Determine whether oCreature has nSkill, and nSkill is useable.
        ///   - nSkill: SKILL_*
        ///   - oCreature
        /// </summary>
        public static bool GetHasSkill(Skill nSkill, uint oCreature = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.StackPushInteger((int)nSkill);
            Internal.NativeFunctions.CallBuiltIn(286);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Use nFeat on oTarget.
        ///   - nFeat: FEAT_*
        ///   - oTarget
        /// </summary>
        public static void ActionUseFeat(FeatType nFeat, uint oTarget)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushInteger((int)nFeat);
            Internal.NativeFunctions.CallBuiltIn(287);
        }

        /// <summary>
        ///   Runs the action "UseSkill" on the current creature
        ///   Use nSkill on oTarget.
        ///   - nSkill: SKILL_*
        ///   - oTarget
        ///   - nSubSkill: SUBSKILL_*
        ///   - oItemUsed: Item to use in conjunction with the skill
        /// </summary>
        public static void ActionUseSkill(Skill nSkill, uint oTarget, SubSkill nSubSkill = SubSkill.None,
            uint oItemUsed = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oItemUsed);
            Internal.NativeFunctions.StackPushInteger((int)nSubSkill);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushInteger((int)nSkill);
            Internal.NativeFunctions.CallBuiltIn(288);
        }

        /// <summary>
        ///   Determine whether oSource sees oTarget.
        ///   NOTE: This *only* works on creatures, as visibility lists are not
        ///   maintained for non-creature objects.
        /// </summary>
        public static bool GetObjectSeen(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oSource);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(289);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Determine whether oSource hears oTarget.
        ///   NOTE: This *only* works on creatures, as visibility lists are not
        ///   maintained for non-creature objects.
        /// </summary>
        public static bool GetObjectHeard(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oSource);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(290);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Use this in an OnPlayerDeath module script to get the last player that died.
        /// </summary>
        public static uint GetLastPlayerDied()
        {
            Internal.NativeFunctions.CallBuiltIn(291);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnItemLost script to get the item that was lost/dropped.
        ///   * Returns OBJECT_INVALID if the module is not valid.
        /// </summary>
        public static uint GetModuleItemLost()
        {
            Internal.NativeFunctions.CallBuiltIn(292);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnItemLost script to get the creature that lost the item.
        ///   * Returns OBJECT_INVALID if the module is not valid.
        /// </summary>
        public static uint GetModuleItemLostBy()
        {
            Internal.NativeFunctions.CallBuiltIn(293);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Set the difficulty level of oEncounter.
        ///   - nEncounterDifficulty: ENCOUNTER_DIFFICULTY_*
        ///   - oEncounter
        /// </summary>
        public static void SetEncounterDifficulty(EncounterDifficulty nEncounterDifficulty,
            uint oEncounter = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oEncounter);
            Internal.NativeFunctions.StackPushInteger((int)nEncounterDifficulty);
            Internal.NativeFunctions.CallBuiltIn(296);
        }

        /// <summary>
        ///   Get the difficulty level of oEncounter.
        /// </summary>
        public static int GetEncounterDifficulty(uint oEncounter = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oEncounter);
            Internal.NativeFunctions.CallBuiltIn(297);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the distance between lLocationA and lLocationB.
        /// </summary>
        public static float GetDistanceBetweenLocations(Location lLocationA, Location lLocationB)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lLocationB);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lLocationA);
            Internal.NativeFunctions.CallBuiltIn(298);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Use this in spell scripts to get nDamage adjusted by oTarget's reflex and
        ///   evasion saves.
        ///   - nDamage
        ///   - oTarget
        ///   - nDC: Difficulty check
        ///   - nSaveType: SAVING_THROW_TYPE_*
        ///   - oSaveVersus
        /// </summary>
        public static int GetReflexAdjustedDamage(int nDamage, uint oTarget, int nDC,
            SavingThrowType nSaveType = SavingThrowType.All, uint oSaveVersus = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oSaveVersus);
            Internal.NativeFunctions.StackPushInteger((int)nSaveType);
            Internal.NativeFunctions.StackPushInteger(nDC);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushInteger(nDamage);
            Internal.NativeFunctions.CallBuiltIn(299);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Play nAnimation immediately.
        ///   - nAnimation: ANIMATION_*
        ///   - fSpeed
        ///   - fSeconds
        /// </summary>
        public static void PlayAnimation(Animation nAnimation, float fSpeed = 1.0f, float fSeconds = 0.0f)
        {
            Internal.NativeFunctions.StackPushFloat(fSeconds);
            Internal.NativeFunctions.StackPushFloat(fSpeed);
            Internal.NativeFunctions.StackPushInteger((int)nAnimation);
            Internal.NativeFunctions.CallBuiltIn(300);
        }

        /// <summary>
        ///   Create a Spell Talent.
        ///   - nSpell: SPELL_*
        /// </summary>
        public static Talent TalentSpell(Spell nSpell)
        {
            Internal.NativeFunctions.StackPushInteger((int)nSpell);
            Internal.NativeFunctions.CallBuiltIn(301);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Talent);
        }

        /// <summary>
        ///   Create a Feat Talent.
        ///   - nFeat: FEAT_*
        /// </summary>
        public static Talent TalentFeat(FeatType nFeat)
        {
            Internal.NativeFunctions.StackPushInteger((int)nFeat);
            Internal.NativeFunctions.CallBuiltIn(302);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Talent);
        }

        /// <summary>
        ///   Create a Skill Talent.
        ///   - nSkill: SKILL_*
        /// </summary>
        public static Talent TalentSkill(Skill nSkill)
        {
            Internal.NativeFunctions.StackPushInteger((int)nSkill);
            Internal.NativeFunctions.CallBuiltIn(303);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Talent);
        }

        /// <summary>
        ///   Determines whether oObject has any effects applied by nSpell
        ///   - nSpell: SPELL_*
        ///   - oObject
        ///   * The spell id on effects is only valid if the effect is created
        ///   when the spell script runs. If it is created in a delayed command
        ///   then the spell id on the effect will be invalid.
        /// </summary>
        public static bool GetHasSpellEffect(Spell nSpell, uint oObject = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.StackPushInteger((int)nSpell);
            Internal.NativeFunctions.CallBuiltIn(304);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Get the spell (SPELL_*) that applied eSpellEffect.
        ///   * Returns -1 if eSpellEffect was applied outside a spell script.
        /// </summary>
        public static int GetEffectSpellId(Effect eSpellEffect)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Effect, eSpellEffect);
            Internal.NativeFunctions.CallBuiltIn(305);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Determine whether oCreature has tTalent.
        /// </summary>
        public static bool GetCreatureHasTalent(Talent tTalent, uint oCreature = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Talent, tTalent);
            Internal.NativeFunctions.CallBuiltIn(306);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Get a random talent of oCreature, within nCategory.
        ///   - nCategory: TALENT_CATEGORY_*
        ///   - oCreature
        /// </summary>
        public static Talent GetCreatureTalentRandom(TalentCategory nCategory, uint oCreature = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.StackPushInteger((int)nCategory);
            Internal.NativeFunctions.CallBuiltIn(307);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Talent);
        }

        /// <summary>
        ///   Get the best talent (i.e. closest to nCRMax without going over) of oCreature,
        ///   within nCategory.
        ///   - nCategory: TALENT_CATEGORY_*
        ///   - nCRMax: Challenge Rating of the talent
        ///   - oCreature
        /// </summary>
        public static Talent GetCreatureTalentBest(TalentCategory nCategory, int nCRMax,
            uint oCreature = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.StackPushInteger(nCRMax);
            Internal.NativeFunctions.StackPushInteger((int)nCategory);
            Internal.NativeFunctions.CallBuiltIn(308);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Talent);
        }

        /// <summary>
        ///   Use tChosenTalent on oTarget.
        /// </summary>
        public static void ActionUseTalentOnObject(Talent tChosenTalent, uint oTarget)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Talent, tChosenTalent);
            Internal.NativeFunctions.CallBuiltIn(309);
        }

        /// <summary>
        ///   Use tChosenTalent at lTargetLocation.
        /// </summary>
        public static void ActionUseTalentAtLocation(Talent tChosenTalent, Location lTargetLocation)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lTargetLocation);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Talent, tChosenTalent);
            Internal.NativeFunctions.CallBuiltIn(310);
        }

        /// <summary>
        ///   * Returns TRUE if oCreature is of a playable racial type.
        /// </summary>
        public static bool GetIsPlayableRacialType(uint oCreature)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.CallBuiltIn(312);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Jump to lDestination.  The action is added to the TOP of the action queue.
        /// </summary>
        public static void JumpToLocation(Location lDestination)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lDestination);
            Internal.NativeFunctions.CallBuiltIn(313);
        }

        /// <summary>
        ///   Get the number of ranks that oTarget has in nSkill.
        ///   - nSkill: SKILL_*
        ///   - oTarget
        ///   - nBaseSkillRank: if set to true returns the number of base skill ranks the target
        ///   has (i.e. not including any bonuses from ability scores, feats, etc).
        ///   * Returns -1 if oTarget doesn't have nSkill.
        ///   * Returns 0 if nSkill is untrained.
        /// </summary>
        public static int GetSkillRank(Skill nSkill, uint oTarget = OBJECT_INVALID, bool nBaseSkillRank = false)
        {
            Internal.NativeFunctions.StackPushInteger(nBaseSkillRank ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushInteger((int)nSkill);
            Internal.NativeFunctions.CallBuiltIn(315);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the attack target of oCreature.
        ///   This only works when oCreature is in combat.
        /// </summary>
        public static uint GetAttackTarget(uint oCreature = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.CallBuiltIn(316);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the attack type (SPECIAL_ATTACK_*) of oCreature's last attack.
        ///   This only works when oCreature is in combat.
        /// </summary>
        public static SpecialAttack GetLastAttackType(uint oCreature = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.CallBuiltIn(317);
            return (SpecialAttack)Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the attack mode (COMBAT_MODE_*) of oCreature's last attack.
        ///   This only works when oCreature is in combat.
        /// </summary>
        public static CombatMode GetLastAttackMode(uint oCreature = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.CallBuiltIn(318);
            return (CombatMode)Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Use this in a trigger's OnClick event script to get the object that last
        ///   clicked on it.
        ///   This is identical to GetEnteringObject.
        ///   GetClickingObject() should not be called from a placeable's OnClick event,
        ///   instead use GetPlaceableLastClickedBy();
        /// </summary>
        public static uint GetClickingObject()
        {
            Internal.NativeFunctions.CallBuiltIn(326);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Initialise oTarget to listen for the standard Associates commands.
        /// </summary>
        public static void SetAssociateListenPatterns(uint oTarget = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(327);
        }

        /// <summary>
        ///   Get the last weapon that oCreature used in an attack.
        ///   * Returns OBJECT_INVALID if oCreature did not attack, or has no weapon equipped.
        /// </summary>
        public static uint GetLastWeaponUsed(uint oCreature)
        {
            Internal.NativeFunctions.StackPushObject(oCreature);
            Internal.NativeFunctions.CallBuiltIn(328);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Use oPlaceable.
        /// </summary>
        public static void ActionInteractObject(uint oPlaceable)
        {
            Internal.NativeFunctions.StackPushObject(oPlaceable);
            Internal.NativeFunctions.CallBuiltIn(329);
        }

        /// <summary>
        ///   Get the last object that used the placeable object that is calling this function.
        ///   * Returns OBJECT_INVALID if it is called by something other than a placeable or
        ///   a door.
        /// </summary>
        public static uint GetLastUsedBy()
        {
            Internal.NativeFunctions.CallBuiltIn(330);
            return Internal.NativeFunctions.StackPopObject();
        }


        /// <summary>
        ///   Get the amount of damage of type nDamageType that has been dealt to the caller.
        ///   - nDamageType: DAMAGE_TYPE_*
        /// </summary>
        public static int GetDamageDealtByType(DamageType nDamageType)
        {
            Internal.NativeFunctions.StackPushInteger((int)nDamageType);
            Internal.NativeFunctions.CallBuiltIn(344);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the total amount of damage that has been dealt to the caller.
        /// </summary>
        public static int GetTotalDamageDealt()
        {
            Internal.NativeFunctions.CallBuiltIn(345);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the last object that damaged oObject
        ///   * Returns OBJECT_INVALID if the passed in object is not a valid object.
        /// </summary>
        public static uint GetLastDamager(uint oObject = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(346);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the last object that disarmed the trap on the caller.
        ///   * Returns OBJECT_INVALID if the caller is not a valid placeable, trigger or
        ///   door.
        /// </summary>
        public static uint GetLastDisarmed()
        {
            Internal.NativeFunctions.CallBuiltIn(347);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the last object that disturbed the inventory of the caller.
        ///   * Returns OBJECT_INVALID if the caller is not a valid creature or placeable.
        /// </summary>
        public static uint GetLastDisturbed()
        {
            Internal.NativeFunctions.CallBuiltIn(348);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the last object that locked the caller.
        ///   * Returns OBJECT_INVALID if the caller is not a valid door or placeable.
        /// </summary>
        public static uint GetLastLocked()
        {
            Internal.NativeFunctions.CallBuiltIn(349);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the last object that unlocked the caller.
        ///   * Returns OBJECT_INVALID if the caller is not a valid door or placeable.
        /// </summary>
        public static uint GetLastUnlocked()
        {
            Internal.NativeFunctions.CallBuiltIn(350);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   * Returns TRUE if tTalent is valid.
        /// </summary>
        public static bool GetIsTalentValid(Talent tTalent)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Talent, tTalent);
            Internal.NativeFunctions.CallBuiltIn(359);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Causes the action subject to move away from lMoveAwayFrom.
        /// </summary>
        public static void ActionMoveAwayFromLocation(Location lMoveAwayFrom, bool bRun = false,
            float fMoveAwayRange = 40.0f)
        {
            Internal.NativeFunctions.StackPushFloat(fMoveAwayRange);
            Internal.NativeFunctions.StackPushInteger(bRun ? 1 : 0);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lMoveAwayFrom);
            Internal.NativeFunctions.CallBuiltIn(360);
        }

        /// <summary>
        ///   Get the target that the caller attempted to attack - this should be used in
        ///   conjunction with GetAttackTarget(). This value is set every time an attack is
        ///   made, and is reset at the end of combat.
        ///   * Returns OBJECT_INVALID if the caller is not a valid creature.
        /// </summary>
        public static uint GetAttemptedAttackTarget()
        {
            Internal.NativeFunctions.CallBuiltIn(361);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the type (TALENT_TYPE_*) of tTalent.
        /// </summary>
        public static TalentType GetTypeFromTalent(Talent tTalent)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Talent, tTalent);
            Internal.NativeFunctions.CallBuiltIn(362);
            return (TalentType)Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the ID of tTalent.  This could be a SPELL_*, FEAT_* or SKILL_*.
        /// </summary>
        public static int GetIdFromTalent(Talent tTalent)
        {
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Talent, tTalent);
            Internal.NativeFunctions.CallBuiltIn(363);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the public part of the CD Key that oPlayer used when logging in.
        ///   - nSinglePlayerCDKey: If set to TRUE, the player's public CD Key will
        ///   be returned when the player is playing in single player mode
        ///   (otherwise returns an empty string in single player mode).
        /// </summary>
        public static string GetPCPublicCDKey(uint oPlayer, bool nSinglePlayerCDKey = false)
        {
            Internal.NativeFunctions.StackPushInteger(nSinglePlayerCDKey ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.CallBuiltIn(369);
            return Internal.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Get the IP address from which oPlayer has connected.
        /// </summary>
        public static string GetPCIPAddress(uint oPlayer)
        {
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.CallBuiltIn(370);
            return Internal.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Get the name of oPlayer.
        /// </summary>
        public static string GetPCPlayerName(uint oPlayer)
        {
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.CallBuiltIn(371);
            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        ///   Sets oPlayer and oTarget to like each other.
        /// </summary>
        public static void SetPCLike(uint oPlayer, uint oTarget)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.CallBuiltIn(372);
        }

        /// <summary>
        ///   Sets oPlayer and oTarget to dislike each other.
        /// </summary>
        public static void SetPCDislike(uint oPlayer, uint oTarget)
        {
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.CallBuiltIn(373);
        }

        /// <summary>
        ///   Send a server message (szMessage) to the oPlayer.
        /// </summary>
        public static void SendMessageToPC(uint oPlayer, string szMessage)
        {
            Internal.NativeFunctions.StackPushString(szMessage);
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.CallBuiltIn(374);
        }

        /// <summary>
        ///   Get the target at which the caller attempted to cast a spell.
        ///   This value is set every time a spell is cast and is reset at the end of
        ///   combat.
        ///   * Returns OBJECT_INVALID if the caller is not a valid creature.
        /// </summary>
        public static uint GetAttemptedSpellTarget()
        {
            Internal.NativeFunctions.CallBuiltIn(375);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Get the experience assigned in the journal editor for szPlotID.
        /// </summary>
        public static int GetJournalQuestExperience(string szPlotID)
        {
            Internal.NativeFunctions.StackPushStringUTF8(szPlotID);
            Internal.NativeFunctions.CallBuiltIn(384);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Jump to oToJumpTo (the action is added to the top of the action queue).
        /// </summary>
        public static void JumpToObject(uint oToJumpTo, bool nWalkStraightLineToPoint = true)
        {
            Internal.NativeFunctions.StackPushInteger(nWalkStraightLineToPoint ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oToJumpTo);
            Internal.NativeFunctions.CallBuiltIn(385);
        }

        /// <summary>
        ///   Convert nInteger to hex, returning the hex value as a string.
        ///   * Return value has the format "0x????????" where each ? will be a hex digit
        ///   (8 digits in total).
        /// </summary>
        public static string IntToHexString(int nInteger)
        {
            Internal.NativeFunctions.StackPushInteger(nInteger);
            Internal.NativeFunctions.CallBuiltIn(396);
            return Internal.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Get the starting location of the module.
        /// </summary>
        public static Location GetStartingLocation()
        {
            Internal.NativeFunctions.CallBuiltIn(411);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Location);
        }

        /// <summary>
        ///   * Returns TRUE if the weapon equipped is capable of damaging oVersus.
        /// </summary>
        public static bool GetIsWeaponEffective(uint oVersus = OBJECT_INVALID, bool bOffHand = false)
        {
            Internal.NativeFunctions.StackPushInteger(bOffHand ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oVersus);
            Internal.NativeFunctions.CallBuiltIn(422);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Use this in a SpellCast script to determine whether the spell was considered
        ///   harmful.
        ///   * Returns TRUE if the last spell cast was harmful.
        /// </summary>
        public static bool GetLastSpellHarmful()
        {
            Internal.NativeFunctions.CallBuiltIn(423);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Try to send oTarget to a new server defined by sIPaddress.
        ///   - oTarget
        ///   - sIPaddress: this can be numerical "192.168.0.84" or alphanumeric
        ///   "www.bioware.com". It can also contain a port "192.168.0.84:5121" or
        ///   "www.bioware.com:5121"; if the port is not specified, it will default to
        ///   5121.
        ///   - sPassword: login password for the destination server
        ///   - sWaypointTag: if this is set, after portalling the character will be moved
        ///   to this waypoint if it exists
        ///   - bSeamless: if this is set, the client wil not be prompted with the
        ///   information window telling them about the server, and they will not be
        ///   allowed to save a copy of their character if they are using a local vault
        ///   character.
        /// </summary>
        public static void ActivatePortal(uint oTarget, string sIPaddress = "", string sPassword = "",
            string sWaypointTag = "", bool bSeemless = false)
        {
            Internal.NativeFunctions.StackPushInteger(bSeemless ? 1 : 0);
            Internal.NativeFunctions.StackPushStringUTF8(sWaypointTag);
            Internal.NativeFunctions.StackPushStringUTF8(sPassword);
            Internal.NativeFunctions.StackPushStringUTF8(sIPaddress);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.CallBuiltIn(474);
        }

        /// <summary>
        ///   The action subject will fake casting a spell at oTarget; the conjure and cast
        ///   animations and visuals will occur, nothing else.
        ///   - nSpell
        ///   - oTarget
        ///   - nProjectilePathType: PROJECTILE_PATH_TYPE_*
        /// </summary>
        public static void ActionCastFakeSpellAtObject(int nSpell, uint oTarget,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default)
        {
            Internal.NativeFunctions.StackPushInteger((int)nProjectilePathType);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushInteger(nSpell);
            Internal.NativeFunctions.CallBuiltIn(501);
        }

        /// <summary>
        ///   The action subject will fake casting a spell at lLocation; the conjure and
        ///   cast animations and visuals will occur, nothing else.
        ///   - nSpell
        ///   - lTarget
        ///   - nProjectilePathType: PROJECTILE_PATH_TYPE_*
        /// </summary>
        public static void ActionCastFakeSpellAtLocation(int nSpell, Location lTarget,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default)
        {
            Internal.NativeFunctions.StackPushInteger((int)nProjectilePathType);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lTarget);
            Internal.NativeFunctions.StackPushInteger(nSpell);
            Internal.NativeFunctions.CallBuiltIn(502);
        }

        /// <summary>
        ///   Shut down the currently loaded module and start a new one (moving all
        ///   currently-connected players to the starting point.
        /// </summary>
        public static void StartNewModule(string sModuleName)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sModuleName);
            Internal.NativeFunctions.CallBuiltIn(509);
        }

        /// <summary>
        ///   Only if we are in a single player game, AutoSave the game.
        /// </summary>
        public static void DoSinglePlayerAutoSave()
        {
            Internal.NativeFunctions.CallBuiltIn(512);
        }

        /// <summary>
        ///   Get the game difficulty (GAME_DIFFICULTY_*).
        /// </summary>
        public static int GetGameDifficulty()
        {
            Internal.NativeFunctions.CallBuiltIn(513);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the current action (ACTION_*) that oObject is executing.
        /// </summary>
        public static ActionType GetCurrentAction(uint oObject = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(522);
            return (ActionType)Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Display floaty text above the specified creature.
        ///   The text will also appear in the chat buffer of each player that receives the
        ///   floaty text.
        ///   - nStrRefToDisplay: String ref (therefore text is translated)
        ///   - oCreatureToFloatAbove
        ///   - bBroadcastToFaction: If this is TRUE then only creatures in the same faction
        ///   as oCreatureToFloatAbove
        ///   will see the floaty text, and only if they are within range (30 metres).
        /// </summary>
        public static void FloatingTextStrRefOnCreature(int nStrRefToDisplay, uint oCreatureToFloatAbove,
            bool bBroadcastToFaction = true)
        {
            Internal.NativeFunctions.StackPushInteger(bBroadcastToFaction ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oCreatureToFloatAbove);
            Internal.NativeFunctions.StackPushInteger(nStrRefToDisplay);
            Internal.NativeFunctions.CallBuiltIn(525);
        }

        /// <summary>
        ///   Display floaty text above the specified creature.
        ///   The text will also appear in the chat buffer of each player that receives the
        ///   floaty text.
        ///   - sStringToDisplay: String
        ///   - oCreatureToFloatAbove
        ///   - bBroadcastToFaction: If this is TRUE then only creatures in the same faction
        ///   as oCreatureToFloatAbove
        ///   will see the floaty text, and only if they are within range (30 metres).
        /// </summary>
        public static void FloatingTextStringOnCreature(string sStringToDisplay, uint oCreatureToFloatAbove,
            bool bBroadcastToFaction = true)
        {
            Internal.NativeFunctions.StackPushInteger(bBroadcastToFaction ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oCreatureToFloatAbove);
            Internal.NativeFunctions.StackPushString(sStringToDisplay);
            Internal.NativeFunctions.CallBuiltIn(526);
        }

        /// <summary>
        ///   * Returns TRUE if a specific key is required to open the lock on oObject.
        /// </summary>
        public static bool GetLockKeyRequired(uint oObject)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(537);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Get the tag of the key that will open the lock on oObject.
        /// </summary>
        public static string GetLockKeyTag(uint oObject)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(538);
            return Internal.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   * Returns TRUE if the lock on oObject is lockable.
        /// </summary>
        public static bool GetLockLockable(uint oObject)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(539);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   Get the DC for unlocking oObject.
        /// </summary>
        public static int GetLockUnlockDC(uint oObject)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(540);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Get the DC for locking oObject.
        /// </summary>
        public static int GetLockLockDC(uint oObject)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(541);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   - nFeat: FEAT_*
        ///   - oObject
        ///   * Returns TRUE if oObject has effects on it originating from nFeat.
        /// </summary>
        public static int GetHasFeatEffect(int nFeat, uint oObject = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.StackPushInteger(nFeat);
            Internal.NativeFunctions.CallBuiltIn(543);
            return Internal.NativeFunctions.StackPopInteger();
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
            Internal.NativeFunctions.StackPushInteger(bIlluminate ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oPlaceable);
            Internal.NativeFunctions.CallBuiltIn(544);
        }

        /// <summary>
        ///   * Returns TRUE if the illumination for oPlaceable is on
        /// </summary>
        public static bool GetPlaceableIllumination(uint oPlaceable = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oPlaceable);
            Internal.NativeFunctions.CallBuiltIn(545);
            return Internal.NativeFunctions.StackPopInteger() != 0;
        }

        /// <summary>
        ///   - oPlaceable
        ///   - nPlaceableAction: PLACEABLE_ACTION_*
        ///   * Returns TRUE if nPlacebleAction is valid for oPlaceable.
        /// </summary>
        public static int GetIsPlaceableObjectActionPossible(uint oPlaceable, int nPlaceableAction)
        {
            Internal.NativeFunctions.StackPushInteger(nPlaceableAction);
            Internal.NativeFunctions.StackPushObject(oPlaceable);
            Internal.NativeFunctions.CallBuiltIn(546);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   The caller performs nPlaceableAction on oPlaceable.
        ///   - oPlaceable
        ///   - nPlaceableAction: PLACEABLE_ACTION_*
        /// </summary>
        public static void DoPlaceableObjectAction(uint oPlaceable, int nPlaceableAction)
        {
            Internal.NativeFunctions.StackPushInteger(nPlaceableAction);
            Internal.NativeFunctions.StackPushObject(oPlaceable);
            Internal.NativeFunctions.CallBuiltIn(547);
        }

        /// <summary>
        ///   Force all the characters of the players who are currently in the game to
        ///   be exported to their respective directories i.e. LocalVault/ServerVault/ etc.
        /// </summary>
        public static void ExportAllCharacters()
        {
            Internal.NativeFunctions.CallBuiltIn(557);
        }

        /// <summary>
        ///   Write sLogEntry as a timestamped entry into the log file
        /// </summary>
        public static void WriteTimestampedLogEntry(string sLogEntry)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sLogEntry);
            Internal.NativeFunctions.CallBuiltIn(560);
        }

        /// <summary>
        ///   Get the module's name in the language of the server that's running it.
        ///   * If there is no entry for the language of the server, it will return an
        ///   empty string
        /// </summary>
        public static string GetModuleName()
        {
            Internal.NativeFunctions.CallBuiltIn(561);
            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        ///   End the currently running game, play sEndMovie then return all players to the
        ///   game's main menu.
        /// </summary>
        public static void EndGame(string sEndMovie)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sEndMovie);
            Internal.NativeFunctions.CallBuiltIn(564);
        }

        /// <summary>
        ///   Counterspell oCounterSpellTarget.
        /// </summary>
        public static void ActionCounterSpell(uint oCounterSpellTarget)
        {
            Internal.NativeFunctions.StackPushObject(oCounterSpellTarget);
            Internal.NativeFunctions.CallBuiltIn(566);
        }

        /// <summary>
        ///   Get the duration (in seconds) of the sound attached to nStrRef
        ///   * Returns 0.0f if no duration is stored or if no sound is attached
        /// </summary>
        public static float GetStrRefSoundDuration(int nStrRef)
        {
            Internal.NativeFunctions.StackPushInteger(nStrRef);
            Internal.NativeFunctions.CallBuiltIn(571);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   SpawnScriptDebugger() will cause the script debugger to be executed
        ///   after this command is executed!
        ///   In order to compile the script for debugging go to Tools->Options->Script Editor
        ///   and check the box labeled "Generate Debug Information When Compiling Scripts"
        ///   After you have checked the above box, recompile the script that you want to debug.
        ///   If the script file isn't compiled for debugging, this command will do nothing.
        ///   Remove any SpawnScriptDebugger() calls once you have finished
        ///   debugging the script.
        /// </summary>
        public static void SpawnScriptDebugger()
        {
            Internal.NativeFunctions.CallBuiltIn(578);
        }

        /// <summary>
        ///   This stores a float out to the specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static void SetCampaignFloat(string sCampaignName, string sVarName, float flFloat,
            uint oPlayer = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.StackPushFloat(flFloat);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushStringUTF8(sCampaignName);
            Internal.NativeFunctions.CallBuiltIn(589);
        }

        /// <summary>
        ///   This stores an int out to the specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static void SetCampaignInt(string sCampaignName, string sVarName, int nInt,
            uint oPlayer = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.StackPushInteger(nInt);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushStringUTF8(sCampaignName);
            Internal.NativeFunctions.CallBuiltIn(590);
        }

        /// <summary>
        ///   This stores a vector out to the specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static void SetCampaignVector(string sCampaignName, string sVarName, Vector3 vVector,
            uint oPlayer = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.StackPushVector(vVector);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushStringUTF8(sCampaignName);
            Internal.NativeFunctions.CallBuiltIn(591);
        }

        /// <summary>
        ///   This stores a location out to the specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static void SetCampaignLocation(string sCampaignName, string sVarName, Location locLocation,
            uint oPlayer = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, locLocation);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushStringUTF8(sCampaignName);
            Internal.NativeFunctions.CallBuiltIn(592);
        }

        /// <summary>
        ///   This stores a string out to the specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static void SetCampaignString(string sCampaignName, string sVarName, string sString,
            uint oPlayer = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.StackPushString(sString);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushStringUTF8(sCampaignName);
            Internal.NativeFunctions.CallBuiltIn(593);
        }

        /// <summary>
        ///   This will delete the entire campaign database if it exists.
        /// </summary>
        public static void DestroyCampaignDatabase(string sCampaignName)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sCampaignName);
            Internal.NativeFunctions.CallBuiltIn(594);
        }

        /// <summary>
        ///   This will read a float from the  specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static float GetCampaignFloat(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushStringUTF8(sCampaignName);
            Internal.NativeFunctions.CallBuiltIn(595);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   This will read an int from the  specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static int GetCampaignInt(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushStringUTF8(sCampaignName);
            Internal.NativeFunctions.CallBuiltIn(596);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   This will read a vector from the  specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static Vector3 GetCampaignVector(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushStringUTF8(sCampaignName);
            Internal.NativeFunctions.CallBuiltIn(597);
            return Internal.NativeFunctions.StackPopVector();
        }

        /// <summary>
        ///   This will read a location from the  specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static Location GetCampaignLocation(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushStringUTF8(sCampaignName);
            Internal.NativeFunctions.CallBuiltIn(598);
            return Internal.NativeFunctions.StackPopGameDefinedStructure((int)EngineStructure.Location);
        }

        /// <summary>
        ///   This will read a string from the  specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static string GetCampaignString(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushStringUTF8(sCampaignName);
            Internal.NativeFunctions.CallBuiltIn(599);
            return Internal.NativeFunctions.StackPopString();
        }

        /// <summary>
        ///   This will remove ANY campaign variable. Regardless of type.
        ///   Note that by normal database standards, deleting does not actually removed the entry from
        ///   the database, but flags it as deleted. Do not expect the database files to shrink in size
        ///   from this command. If you want to 'pack' the database, you will have to do it externally
        ///   from the game.
        /// </summary>
        public static void DeleteCampaignVariable(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushStringUTF8(sCampaignName);
            Internal.NativeFunctions.CallBuiltIn(601);
        }

        /// <summary>
        ///   Stores an object with the given id.
        ///   NOTE: this command can only be used for storing Creatures and Items.
        ///   Returns 0 if it failled, 1 if it worked.
        /// </summary>
        public static int StoreCampaignObject(string sCampaignName, string sVarName, uint oObject,
            uint oPlayer = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushStringUTF8(sCampaignName);
            Internal.NativeFunctions.CallBuiltIn(602);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Use RetrieveCampaign with the given id to restore it.
        ///   If you specify an owner, the object will try to be created in their repository
        ///   If the owner can't handle the item (or if it's a creature) it will be created on the ground.
        /// </summary>
        public static uint RetrieveCampaignObject(string sCampaignName, string sVarName, Location locLocation,
            uint oOwner = OBJECT_INVALID, uint oPlayer = OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.StackPushObject(oOwner);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, locLocation);
            Internal.NativeFunctions.StackPushStringUTF8(sVarName);
            Internal.NativeFunctions.StackPushStringUTF8(sCampaignName);
            Internal.NativeFunctions.CallBuiltIn(603);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Gets the length of the specified wavefile, in seconds
        ///   Only works for sounds used for dialog.
        /// </summary>
        public static float GetDialogSoundLength(int nStrRef)
        {
            Internal.NativeFunctions.StackPushInteger(nStrRef);
            Internal.NativeFunctions.CallBuiltIn(694);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Gets a value from a 2DA file on the server and returns it as a string
        ///   avoid using this function in loops
        ///   - s2DA: the name of the 2da file, 16 chars max
        ///   - sColumn: the name of the column in the 2da
        ///   - nRow: the row in the 2da
        ///   * returns an empty string if file, row, or column not found
        /// </summary>
        public static string Get2DAString(string s2DA, string sColumn, int nRow)
        {
            Internal.NativeFunctions.StackPushInteger(nRow);
            Internal.NativeFunctions.StackPushStringUTF8(sColumn);
            Internal.NativeFunctions.StackPushStringUTF8(s2DA);
            Internal.NativeFunctions.CallBuiltIn(710);
            return Internal.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Force the character of the player specified to be exported to its respective directory
        ///   i.e. LocalVault/ServerVault/ etc.
        /// </summary>
        public static void ExportSingleCharacter(uint oPlayer)
        {
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.CallBuiltIn(719);
        }

        /// <summary>
        ///   This will play a sound that is associated with a stringRef, it will be a mono sound from the location of the object
        ///   running the command.
        ///   if nRunAsAction is off then the sound is forced to play intantly.
        /// </summary>
        public static void PlaySoundByStrRef(int nStrRef, bool nRunAsAction = true)
        {
            Internal.NativeFunctions.StackPushInteger(nRunAsAction ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger(nStrRef);
            Internal.NativeFunctions.CallBuiltIn(720);
        }

        /// <summary>
        ///   Get the XP scale being used for the module.
        /// </summary>
        public static int GetModuleXPScale()
        {
            Internal.NativeFunctions.CallBuiltIn(817);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Set the XP scale used by the module.
        ///   - nXPScale: The XP scale to be used. Must be between 0 and 200.
        /// </summary>
        public static void SetModuleXPScale(int nXPScale)
        {
            Internal.NativeFunctions.StackPushInteger(nXPScale);
            Internal.NativeFunctions.CallBuiltIn(818);
        }

        /// <summary>
        ///   Gets the attack bonus limit.
        ///   - The default value is 20.
        /// </summary>
        public static int GetAttackBonusLimit()
        {
            Internal.NativeFunctions.CallBuiltIn(872);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Gets the damage bonus limit.
        ///   - The default value is 100.
        /// </summary>
        public static int GetDamageBonusLimit()
        {
            Internal.NativeFunctions.CallBuiltIn(873);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Gets the saving throw bonus limit.
        ///   - The default value is 20.
        /// </summary>
        public static int GetSavingThrowBonusLimit()
        {
            Internal.NativeFunctions.CallBuiltIn(874);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Gets the ability bonus limit.
        ///   - The default value is 12.
        /// </summary>
        public static int GetAbilityBonusLimit()
        {
            Internal.NativeFunctions.CallBuiltIn(875);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Gets the ability penalty limit.
        ///   - The default value is 30.
        /// </summary>
        public static int GetAbilityPenaltyLimit()
        {
            Internal.NativeFunctions.CallBuiltIn(876);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Gets the skill bonus limit.
        ///   - The default value is 50.
        /// </summary>
        public static int GetSkillBonusLimit()
        {
            Internal.NativeFunctions.CallBuiltIn(877);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Sets the attack bonus limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetAttackBonusLimit(int nNewLimit)
        {
            Internal.NativeFunctions.StackPushInteger(nNewLimit);
            Internal.NativeFunctions.CallBuiltIn(878);
        }

        /// <summary>
        ///   Sets the damage bonus limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetDamageBonusLimit(int nNewLimit)
        {
            Internal.NativeFunctions.StackPushInteger(nNewLimit);
            Internal.NativeFunctions.CallBuiltIn(879);
        }

        /// <summary>
        ///   Sets the saving throw bonus limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetSavingThrowBonusLimit(int nNewLimit)
        {
            Internal.NativeFunctions.StackPushInteger(nNewLimit);
            Internal.NativeFunctions.CallBuiltIn(880);
        }

        /// <summary>
        ///   Sets the ability bonus limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetAbilityBonusLimit(int nNewLimit)
        {
            Internal.NativeFunctions.StackPushInteger(nNewLimit);
            Internal.NativeFunctions.CallBuiltIn(881);
        }

        /// <summary>
        ///   Sets the ability penalty limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetAbilityPenaltyLimit(int nNewLimit)
        {
            Internal.NativeFunctions.StackPushInteger(nNewLimit);
            Internal.NativeFunctions.CallBuiltIn(882);
        }

        /// <summary>
        ///   Sets the skill bonus limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetSkillBonusLimit(int nNewLimit)
        {
            Internal.NativeFunctions.StackPushInteger(nNewLimit);
            Internal.NativeFunctions.CallBuiltIn(883);
        }

        /// <summary>
        ///   Get if oPlayer is currently connected over a relay (instead of directly).
        ///   Returns FALSE for any other object, including OBJECT_INVALID.
        /// </summary>
        public static int GetIsPlayerConnectionRelayed(uint oPlayer)
        {
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.CallBuiltIn(884);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Returns the event script for the given object and handler.
        ///   Will return "" if unset, the object is invalid, or the object cannot
        ///   have the requested handler.
        /// </summary>
        public static string GetEventScript(uint oObject, EventScript nHandler)
        {
            Internal.NativeFunctions.StackPushInteger((int)nHandler);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(885);
            return Internal.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Sets the given event script for the given object and handler.
        ///   Returns 1 on success, 0 on failure.
        ///   Will fail if oObject is invalid or does not have the requested handler.
        /// </summary>
        public static int SetEventScript(uint oObject, EventScript nHandler, string sScript)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sScript);
            Internal.NativeFunctions.StackPushInteger((int)nHandler);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(886);
            return Internal.NativeFunctions.StackPopInteger();
        }

        /// <summary>
        ///   Gets a visual transform on the given object.
        ///   - oObject can be any valid Creature, Placeable, Item or Door.
        ///   - nTransform is one of OBJECT_VISUAL_TRANSFORM_*
        ///   Returns the current (or default) value.
        /// </summary>
        public static float GetObjectVisualTransform(uint oObject, ObjectVisualTransform nTransform)
        {
            Internal.NativeFunctions.StackPushInteger((int)nTransform);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(887);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///  Sets a visual transform on the given object.
        /// - oObject can be any valid Creature, Placeable, Item or Door.
        /// - nTransform is one of OBJECT_VISUAL_TRANSFORM_*
        /// - fValue depends on the transformation to apply.
        /// Returns the old/previous value.
        /// </summary>
        public static float SetObjectVisualTransform(uint oObject, ObjectVisualTransform nTransform, float fValue, Lerp nLerpType = Lerp.None, float fLerpDuration = 0.0f, bool bPauseWithGame = true)
        {
            Internal.NativeFunctions.StackPushInteger(bPauseWithGame ? 1 : 0);
            Internal.NativeFunctions.StackPushFloat(fLerpDuration);
            Internal.NativeFunctions.StackPushInteger((int)nLerpType);
            Internal.NativeFunctions.StackPushFloat(fValue);
            Internal.NativeFunctions.StackPushInteger((int)nTransform);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(888);
            return Internal.NativeFunctions.StackPopFloat();
        }

        /// <summary>
        ///   Sets an integer material shader uniform override.
        ///   - sMaterial needs to be a material on that object.
        ///   - sParam needs to be a valid shader parameter already defined on the material.
        /// </summary>
        public static void SetMaterialShaderUniformInt(uint oObject, string sMaterial, string sParam, int nValue)
        {
            Internal.NativeFunctions.StackPushInteger(nValue);
            Internal.NativeFunctions.StackPushStringUTF8(sParam);
            Internal.NativeFunctions.StackPushStringUTF8(sMaterial);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(889);
        }

        /// <summary>
        ///   Sets a vec4 material shader uniform override.
        ///   - sMaterial needs to be a material on that object.
        ///   - sParam needs to be a valid shader parameter already defined on the material.
        ///   - You can specify a single float value to set just a float, instead of a vec4.
        /// </summary>
        public static void SetMaterialShaderUniformVec4(uint oObject, string sMaterial, string sParam, float fValue1,
            float fValue2 = 0.0f, float fValue3 = 0.0f, float fValue4 = 0.0f)
        {
            Internal.NativeFunctions.StackPushFloat(fValue4);
            Internal.NativeFunctions.StackPushFloat(fValue3);
            Internal.NativeFunctions.StackPushFloat(fValue2);
            Internal.NativeFunctions.StackPushFloat(fValue1);
            Internal.NativeFunctions.StackPushStringUTF8(sParam);
            Internal.NativeFunctions.StackPushStringUTF8(sMaterial);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(890);
        }

        /// <summary>
        ///   Resets material shader parameters on the given object:
        ///   - Supply a material to only reset shader uniforms for meshes with that material.
        ///   - Supply a parameter to only reset shader uniforms of that name.
        ///   - Supply both to only reset shader uniforms of that name on meshes with that material.
        /// </summary>
        public static void ResetMaterialShaderUniforms(uint oObject, string sMaterial = "", string sParam = "")
        {
            Internal.NativeFunctions.StackPushStringUTF8(sParam);
            Internal.NativeFunctions.StackPushStringUTF8(sMaterial);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(891);
        }

        /// <summary>
        ///   Vibrate the player's device or controller. Does nothing if vibration is not supported.
        ///   - nMotor is one of VIBRATOR_MOTOR_*
        ///   - fStrength is between 0.0 and 1.0
        ///   - fSeconds is the number of seconds to vibrate
        /// </summary>
        public static void Vibrate(uint oPlayer, int nMotor, float fStrength, float fSeconds)
        {
            Internal.NativeFunctions.StackPushFloat(fSeconds);
            Internal.NativeFunctions.StackPushFloat(fStrength);
            Internal.NativeFunctions.StackPushInteger(nMotor);
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.CallBuiltIn(892);
        }

        /// <summary>
        ///   Unlock an achievement for the given player who must be logged in.
        ///   - sId is the achievement ID on the remote server
        ///   - nLastValue is the previous value of the associated achievement stat
        ///   - nCurValue is the current value of the associated achievement stat
        ///   - nMaxValue is the maximum value of the associate achievement stat
        /// </summary>
        public static void UnlockAchievement(uint oPlayer, string sId, int nLastValue = 0, int nCurValue = 0,
            int nMaxValue = 0)
        {
            Internal.NativeFunctions.StackPushInteger(nMaxValue);
            Internal.NativeFunctions.StackPushInteger(nCurValue);
            Internal.NativeFunctions.StackPushInteger(nLastValue);
            Internal.NativeFunctions.StackPushStringUTF8(sId);
            Internal.NativeFunctions.StackPushObject(oPlayer);
            Internal.NativeFunctions.CallBuiltIn(893);
        }

        /// <summary>
        ///   Execute a script chunk.
        ///   The script chunk runs immediately, same as ExecuteScript().
        ///   The script is jitted in place and currently not cached: Each invocation will recompile the script chunk.
        ///   Note that the script chunk will run as if a separate script. This is not eval().
        ///   By default, the script chunk is wrapped into void main() {}. Pass in bWrapIntoMain = FALSE to override.
        ///   Returns "" on success, or the compilation error.
        /// </summary>
        public static string ExecuteScriptChunk(string sScriptChunk, uint oObject, bool bWrapIntoMain = true)
        {
            Internal.NativeFunctions.StackPushInteger(bWrapIntoMain ? 1 : 0);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.StackPushString(sScriptChunk);
            Internal.NativeFunctions.CallBuiltIn(894);
            return Internal.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Returns a UUID. This UUID will not be associated with any object.
        ///   The generated UUID is currently a v4.
        /// </summary>
        public static string GetRandomUUID()
        {
            Internal.NativeFunctions.CallBuiltIn(895);
            return Internal.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Returns the given objects' UUID. This UUID is persisted across save boundaries,
        ///   like Save/RestoreCampaignObject and save games.
        ///   Thus, reidentification is only guaranteed in scenarios where players cannot introduce
        ///   new objects (i.e. servervault servers).
        ///   UUIDs are guaranteed to be unique in any single running game.
        ///   If a loaded object would collide with a UUID already present in the game, the
        ///   object receives no UUID and a warning is emitted to the log. Requesting a UUID
        ///   for the new object will generate a random one.
        ///   This UUID is useful to, for example:
        ///   - Safely identify servervault characters
        ///   - Track serialisable objects (like items or creatures) as they are saved to the
        ///   campaign DB - i.e. persistent storage chests or dropped items.
        ///   - Track objects across multiple game instances (in trusted scenarios).
        ///   Currently, the following objects can carry UUIDs:
        ///   Items, Creatures, Placeables, Triggers, Doors, Waypoints, Stores,
        ///   Encounters, Areas.
        ///   Will return "" (empty string) when the given object cannot carry a UUID.
        /// </summary>
        public static string GetObjectUUID(uint oObject)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(896);
            return Internal.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Forces the given object to receive a new UUID, discarding the current value.
        /// </summary>
        public static void ForceRefreshObjectUUID(uint oObject)
        {
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(897);
        }

        /// <summary>
        ///   Looks up a object on the server by it's UUID.
        ///   Returns OBJECT_INVALID if the UUID is not on the server.
        /// </summary>
        public static uint GetObjectByUUID(string sUUID)
        {
            Internal.NativeFunctions.StackPushStringUTF8(sUUID);
            Internal.NativeFunctions.CallBuiltIn(898);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        ///   Do not call. This does nothing on this platform except to return an error.
        /// </summary>
        public static void Reserved899()
        {
            Internal.NativeFunctions.CallBuiltIn(899);
        }

        // Makes oPC load texture sNewName instead of sOldName.
        // If oPC is OBJECT_INVALID, it will apply the override to all active players
        // Setting sNewName to "" will clear the override and revert to original.
        // void SetTextureOverride();
        public static void SetTextureOverride(string OldName, string NewName = "", uint PC = Internal.OBJECT_INVALID)
        {
            Internal.NativeFunctions.StackPushObject(PC);
            Internal.NativeFunctions.StackPushStringUTF8(NewName);
            Internal.NativeFunctions.StackPushStringUTF8(OldName);
            Internal.NativeFunctions.CallBuiltIn(900);
        }



        /// <summary>
        /// Displays sMsg on oPC's screen.
        /// The message is displayed on top of whatever is on the screen, including UI elements
        ///  nX, nY - coordinates of the first character to be displayed. The value is in terms
        ///           of character 'slot' relative to the nAnchor anchor point.
        ///           If the number is negative, it is applied from the bottom/right.
        ///  nAnchor - SCREEN_ANCHOR_* constant
        ///  fLife - Duration in seconds until the string disappears.
        ///  nRGBA, nRGBA2 - Colors of the string in 0xRRGGBBAA format. String starts at nRGBA,
        ///                  but as it nears end of life, it will slowly blend into nRGBA2.
        ///  nID - Optional ID of a string. If not 0, subsequent calls to PostString will
        ///        remove the old string with the same ID, even if it's lifetime has not elapsed.
        ///        Only positive values are allowed.
        ///  sFont - If specified, use this custom font instead of default console font.
        /// </summary>
        /// <param name="PC"></param>
        /// <param name="Msg"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="anchor"></param>
        /// <param name="life"></param>
        /// <param name="RGBA"></param>
        /// <param name="RGBA2"></param>
        /// <param name="ID"></param>
        /// <param name="font"></param>
        public static void PostString(uint PC, string Msg, int X = 0, int Y = 0, ScreenAnchor anchor = ScreenAnchor.TopLeft,
            float life = 10.0f, int RGBA = 2147418367, int RGBA2 = 2147418367, int ID = 0, string font = "")
        {
            Internal.NativeFunctions.StackPushStringUTF8(font);
            Internal.NativeFunctions.StackPushInteger(ID);
            Internal.NativeFunctions.StackPushInteger(RGBA2);
            Internal.NativeFunctions.StackPushInteger(RGBA);
            Internal.NativeFunctions.StackPushFloat(life);
            Internal.NativeFunctions.StackPushInteger((int)anchor);
            Internal.NativeFunctions.StackPushInteger(Y);
            Internal.NativeFunctions.StackPushInteger(X);
            Internal.NativeFunctions.StackPushStringUTF8(Msg);
            Internal.NativeFunctions.StackPushObject(PC);
            Internal.NativeFunctions.CallBuiltIn(901);
        }

        // Returns oCreature's spell school specialization in nClass (SPELL_SCHOOL_* constants)
        // Unless custom content is used, only Wizards have spell schools
        // Returns -1 on error
        public static SpellSchool GetSpecialization(uint creature, ClassType playerClass)
        {
            Internal.NativeFunctions.StackPushInteger((int)playerClass);
            Internal.NativeFunctions.StackPushObject(creature);
            Internal.NativeFunctions.CallBuiltIn(902);
            return (SpellSchool)Internal.NativeFunctions.StackPopInteger();
        }

        // Returns oCreature's domain in nClass (DOMAIN_* constants)
        // nDomainIndex - 1 or 2
        // Unless custom content is used, only Clerics have domains
        // Returns -1 on error
        public static ClericDomain GetDomain(uint creature, int DomainIndex = 1, ClassType playerClass = ClassType.Cleric)
        {
            Internal.NativeFunctions.StackPushInteger((int)playerClass);
            Internal.NativeFunctions.StackPushObject(creature);
            Internal.NativeFunctions.CallBuiltIn(903);
            return (ClericDomain)Internal.NativeFunctions.StackPopInteger();
        }



        /// <summary>
        /// Queue an action to use an active item property.
        /// * oItem - item that has the item property to use
        /// * ip - item property to use
        /// * object oTarget - target
        /// * nSubPropertyIndex - specify if your itemproperty has subproperties (such as subradial spells)
        /// * bDecrementCharges - decrement charges if item property is limited
        /// </summary>
        public static void ActionUseItemOnObject(uint oItem, IntPtr ip, uint oTarget, int nSubPropertyIndex = 0, bool bDecrementCharges = true)
        {
            Internal.NativeFunctions.StackPushInteger(bDecrementCharges ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger(nSubPropertyIndex);
            Internal.NativeFunctions.StackPushObject(oTarget);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.ItemProperty, ip);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(910);
        }

        /// <summary>
        /// Queue an action to use an active item property.
        /// * oItem - item that has the item property to use
        /// * ip - item property to use
        /// * location lTarget - target location (must be in the same area as item possessor)
        /// * nSubPropertyIndex - specify if your itemproperty has subproperties (such as subradial spells)
        /// * bDecrementCharges - decrement charges if item property is limited
        /// </summary>
        public static void ActionUseItemAtLocation(uint oItem, IntPtr ip, IntPtr lTarget, int nSubPropertyIndex = 0, bool bDecrementCharges = true)
        {
            Internal.NativeFunctions.StackPushInteger(bDecrementCharges ? 1 : 0);
            Internal.NativeFunctions.StackPushInteger(nSubPropertyIndex);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.Location, lTarget);
            Internal.NativeFunctions.StackPushGameDefinedStructure((int)EngineStructure.ItemProperty, ip);
            Internal.NativeFunctions.StackPushObject(oItem);
            Internal.NativeFunctions.CallBuiltIn(911);
        }

        /// <summary>
        /// Makes oPC enter a targeting mode, letting them select an object as a target
        /// If a PC selects a target, it will trigger the module OnPlayerTarget event.
        /// </summary>
        public static void EnterTargetingMode(uint oPC, ObjectType nValidObjectTypes = ObjectType.All, MouseCursor nMouseCursorId = MouseCursor.Magic, MouseCursor nBadTargetCursor = MouseCursor.NoMagic)
        {
            Internal.NativeFunctions.StackPushInteger((int)nBadTargetCursor);
            Internal.NativeFunctions.StackPushInteger((int)nMouseCursorId);
            Internal.NativeFunctions.StackPushInteger((int)nValidObjectTypes);
            Internal.NativeFunctions.StackPushObject(oPC);
            Internal.NativeFunctions.CallBuiltIn(912);
        }

        /// <summary>
        /// Gets the target object in the module OnPlayerTarget event.
        /// Returns the area object when the target is the ground.
        /// </summary>
        public static uint GetTargetingModeSelectedObject()
        {
            Internal.NativeFunctions.CallBuiltIn(913);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        /// Gets the target position in the module OnPlayerTarget event.
        /// </summary>
        public static Vector3 GetTargetingModeSelectedPosition()
        {
            Internal.NativeFunctions.CallBuiltIn(914);
            return Internal.NativeFunctions.StackPopVector();
        }

        /// <summary>
        /// Gets the player object that triggered the OnPlayerTarget event.
        /// </summary>
        public static uint GetLastPlayerToSelectTarget()
        {
            Internal.NativeFunctions.CallBuiltIn(915);
            return Internal.NativeFunctions.StackPopObject();
        }

        /// <summary>
        /// Sets oObject's hilite color to nColor
        /// The nColor format is 0xRRGGBB; -1 clears the color override.
        /// </summary>
        public static void SetObjectHiliteColor(uint oObject, int nColor = -1)
        {
            Internal.NativeFunctions.StackPushInteger(nColor);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(916);
        }

        /// <summary>
        /// Sets the cursor (MOUSECURSOR_*) to use when hovering over oObject
        /// </summary>
        public static void SetObjectMouseCursor(uint oObject, MouseCursor nCursor = MouseCursor.Invalid)
        {
            Internal.NativeFunctions.StackPushInteger((int)nCursor);
            Internal.NativeFunctions.StackPushObject(oObject);
            Internal.NativeFunctions.CallBuiltIn(917);
        }

        /// <summary>
        /// Overrides a given strref to always return sValue instead of what is in the TLK file.
        /// Setting sValue to "" will delete the override
        /// </summary>
        public static void SetTlkOverride(int nStrRef, string sValue = "")
        {
            Internal.NativeFunctions.StackPushString(sValue);
            Internal.NativeFunctions.StackPushInteger(nStrRef);
            Internal.NativeFunctions.CallBuiltIn(953);
        }

    }
}