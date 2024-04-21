using System;
using System.Numerics;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Entity;


namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        public static uint OBJECT_SELF
        {
            get
            {
                return NWNCore.FunctionHandler.ObjectSelf;
            }
            set
            {
                NWNCore.FunctionHandler.ObjectSelf = value;
            }
        }

        /// <summary>
        ///   Assign aActionToAssign to oActionSubject.
        ///   * No return value, but if an error occurs, the log file will contain
        ///   "AssignCommand failed."
        ///   (If the object doesn't exist, nothing happens.)
        /// </summary>
        public static void AssignCommand(uint oActionSubject, Action aActionToAssign)
        {
            NWNCore.FunctionHandler!.ClosureAssignCommand(oActionSubject, aActionToAssign);
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
        public static void DelayCommand(float fSeconds, Action aActionToDelay)
        {
            NWNCore.FunctionHandler!.ClosureDelayCommand(OBJECT_SELF, fSeconds, aActionToDelay);
        }


        /// <summary>
        ///   Do aActionToDo.
        /// </summary>
        public static void ActionDoCommand(Action aActionToDo)
        {
            NWNCore.FunctionHandler!.ClosureActionDoCommand(OBJECT_SELF, aActionToDo);
        }

        /// <summary>
        ///   Get an integer between 0 and nMaxInteger-1.
        ///   Return value on error: 0
        /// </summary>
        public static int Random(int nMaxInteger)
        {
            VM.StackPush(nMaxInteger);
            VM.Call(0);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Output sString to the log file.
        /// </summary>
        public static void PrintString(string sString)
        {
            VM.StackPush(sString);
            VM.Call(1);
        }

        /// <summary>
        ///   Output a formatted float to the log file.
        ///   - nWidth should be a value from 0 to 18 inclusive.
        ///   - nDecimals should be a value from 0 to 9 inclusive.
        /// </summary>
        public static void PrintFloat(float fFloat, int nWidth = 18, int nDecimals = 9)
        {
            VM.StackPush(nDecimals);
            VM.StackPush(nWidth);
            VM.StackPush(fFloat);
            VM.Call(2);
        }

        /// <summary>
        ///   Convert fFloat into a string.
        ///   - nWidth should be a value from 0 to 18 inclusive.
        ///   - nDecimals should be a value from 0 to 9 inclusive.
        /// </summary>
        public static string FloatToString(float fFloat, int nWidth = 18, int nDecimals = 9)
        {
            VM.StackPush(nDecimals);
            VM.StackPush(nWidth);
            VM.StackPush(fFloat);
            VM.Call(3);
            return NWNCore.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Output nInteger to the log file.
        /// </summary>
        public static void PrintInteger(int nInteger)
        {
            VM.StackPush(nInteger);
            VM.Call(4);
        }

        /// <summary>
        ///   Output oObject's ID to the log file.
        /// </summary>
        public static void PrintObject(uint oObject)
        {
            VM.StackPush(oObject);
            VM.Call(5);
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
            VM.StackPush(nClearCombatState ? 1 : 0);
            VM.Call(9);
        }

        /// <summary>
        ///   Cause the caller to face fDirection.
        ///   - fDirection is expressed as anticlockwise degrees from Due East.
        ///   DIRECTION_EAST, DIRECTION_NORTH, DIRECTION_WEST and DIRECTION_SOUTH are
        ///   predefined. (0.0f=East, 90.0f=North, 180.0f=West, 270.0f=South)
        /// </summary>
        public static void SetFacing(float fDirection)
        {
            VM.StackPush(fDirection);
            VM.Call(10);
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
            VM.Call(20);
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
            VM.StackPush(bRun ? 1 : 0);
            VM.StackPush((int)EngineStructure.Location, lDestination);
            VM.Call(21);
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
            VM.StackPush(fRange);
            VM.StackPush(bRun ? 1 : 0);
            VM.StackPush(oMoveTo);
            VM.Call(22);
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
            VM.StackPush(fMoveAwayRange);
            VM.StackPush(bRun ? 1 : 0);
            VM.StackPush(oFleeFrom);
            VM.Call(23);
        }

        /// <summary>
        ///   Get the direction in which oTarget is facing, expressed as a float between
        ///   0.0f and 360.0f
        ///   * Return value on error: -1.0f
        /// </summary>
        public static float GetFacing(uint oTarget)
        {
            VM.StackPush(oTarget);
            VM.Call(28);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Get the last attacker of oAttackee.  This should only be used ONLY in the
        ///   OnAttacked events for creatures, placeables and doors.
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetLastAttacker(uint oAttackee = OBJECT_INVALID)
        {
            VM.StackPush(oAttackee);
            VM.Call(36);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Attack oAttackee.
        ///   - bPassive: If this is TRUE, attack is in passive mode.
        /// </summary>
        public static void ActionAttack(uint oAttackee, bool bPassive = false)
        {
            VM.StackPush(bPassive ? 1 : 0);
            VM.StackPush(oAttackee);
            VM.Call(37);
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
            VM.StackPush(nThirdCriteriaValue);
            VM.StackPush(nThirdCriteriaType);
            VM.StackPush(nSecondCriteriaValue);
            VM.StackPush(nSecondCriteriaType);
            VM.StackPush(nNth);
            VM.StackPush(oTarget);
            VM.StackPush(nFirstCriteriaValue);
            VM.StackPush((int)nFirstCriteriaType);
            VM.Call(38);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Add a speak action to the action subject.
        ///   - sStringToSpeak: String to be spoken
        ///   - nTalkVolume: TALKVOLUME_*
        /// </summary>
        public static void ActionSpeakString(string sStringToSpeak, TalkVolume nTalkVolume = TalkVolume.Talk)
        {
            VM.StackPush((int)nTalkVolume);
            VM.StackPush(sStringToSpeak);
            VM.Call(39);
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
            VM.StackPush(fDurationSeconds);
            VM.StackPush(fSpeed);
            VM.StackPush((int)nAnimation);
            VM.Call(40);
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
            VM.StackPush((int)nTransitionType);
            VM.StackPush(fPitch);
            VM.StackPush(fDistance);
            VM.StackPush(fDirection);
            VM.Call(45);
        }

        /// <summary>
        ///   Play sSoundName
        ///   - sSoundName: TBD - SS
        ///   This will play a mono sound from the location of the object running the command.
        /// </summary>
        public static void PlaySound(string sSoundName)
        {
            VM.StackPush(sSoundName);
            VM.Call(46);
        }

        /// <summary>
        ///   Get the object at which the caller last cast a spell
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetSpellTargetObject()
        {
            VM.Call(47);
            return VM.StackPopObject();
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
            VM.StackPush(bInstantSpell ? 1 : 0);
            VM.StackPush((int)nProjectilePathType);
            VM.StackPush(nDomainLevel);
            VM.StackPush(nCheat ? 1 : 0);
            VM.StackPush((int)nMetaMagic);
            VM.StackPush(oTarget);
            VM.StackPush((int)nSpell);
            VM.Call(48);
        }

        /// <summary>
        ///   Get oObject's local integer variable sVarName
        ///   * Return value on error: 0
        /// </summary>
        public static int GetLocalInt(uint oObject, string sVarName)
        {
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(51);
            return VM.StackPopInt();
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
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(52);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Get oObject's local string variable sVarName
        ///   * Return value on error: ""
        /// </summary>
        public static string GetLocalString(uint oObject, string sVarName)
        {
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(53);
            return VM.StackPopString();
        }

        /// <summary>
        ///   Get oObject's local object variable sVarName
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetLocalObject(uint oObject, string sVarName)
        {
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(54);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Set oObject's local integer variable sVarName to nValue
        /// </summary>
        public static void SetLocalInt(uint oObject, string sVarName, int nValue)
        {
            VM.StackPush(nValue);
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(55);
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
            VM.StackPush(fValue);
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(56);
        }

        /// <summary>
        ///   Set oObject's local string variable sVarName to nValue
        /// </summary>
        public static void SetLocalString(uint oObject, string sVarName, string sValue)
        {
            VM.StackPush(sValue);
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(57);
        }

        /// <summary>
        ///   Set oObject's local object variable sVarName to nValue
        /// </summary>
        public static void SetLocalObject(uint oObject, string sVarName, uint oValue)
        {
            VM.StackPush(oValue);
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(58);
        }

        /// <summary>
        ///   Convert nInteger into a string.
        ///   * Return value on error: ""
        /// </summary>
        public static string IntToString(int nInteger)
        {
            VM.StackPush(nInteger);
            VM.Call(92);
            return NWNCore.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d2 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d2(int nNumDice = 1)
        {
            VM.StackPush(nNumDice);
            VM.Call(95);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d3 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d3(int nNumDice = 1)
        {
            VM.StackPush(nNumDice);
            VM.Call(96);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d4 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d4(int nNumDice = 1)
        {
            VM.StackPush(nNumDice);
            VM.Call(97);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d6 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d6(int nNumDice = 1)
        {
            VM.StackPush(nNumDice);
            VM.Call(98);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d8 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d8(int nNumDice = 1)
        {
            VM.StackPush(nNumDice);
            VM.Call(99);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d10 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d10(int nNumDice = 1)
        {
            VM.StackPush(nNumDice);
            VM.Call(100);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d12 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d12(int nNumDice = 1)
        {
            VM.StackPush(nNumDice);
            VM.Call(101);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d20 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d20(int nNumDice = 1)
        {
            VM.StackPush(nNumDice);
            VM.Call(102);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the total from rolling (nNumDice x d100 dice).
        ///   - nNumDice: If this is less than 1, the value 1 will be used.
        /// </summary>
        public static int d100(int nNumDice = 1)
        {
            VM.StackPush(nNumDice);
            VM.Call(103);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the metamagic type (METAMAGIC_*) of the last spell cast by the caller
        ///   * Return value if the caster is not a valid object: -1
        /// </summary>
        public static int GetMetaMagicFeat()
        {
            VM.Call(105);
            return VM.StackPopInt();
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
        public static SavingThrowResultType FortitudeSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All,
            uint oSaveVersus = OBJECT_INVALID)
        {
            VM.StackPush(oSaveVersus);
            VM.StackPush((int)nSaveType);
            VM.StackPush(nDC);
            VM.StackPush(oCreature);
            VM.Call(108);
            return (SavingThrowResultType)VM.StackPopInt();
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
        public static SavingThrowResultType ReflexSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All,
            uint oSaveVersus = OBJECT_INVALID)
        {
            VM.StackPush(oSaveVersus);
            VM.StackPush((int)nSaveType);
            VM.StackPush(nDC);
            VM.StackPush(oCreature);
            VM.Call(109);
            return (SavingThrowResultType)VM.StackPopInt();
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
        public static SavingThrowResultType WillSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All,
            uint oSaveVersus = OBJECT_INVALID)
        {
            VM.StackPush(oSaveVersus);
            VM.StackPush((int)nSaveType);
            VM.StackPush(nDC);
            VM.StackPush(oCreature);
            VM.Call(110);
            return (SavingThrowResultType)VM.StackPopInt();
        }

        /// <summary>
        ///   Get the DC to save against for a spell (10 + spell level + relevant ability
        ///   bonus).  This can be called by a creature or by an Area of Effect object.
        /// </summary>
        public static int GetSpellSaveDC()
        {
            VM.Call(111);
            return VM.StackPopInt();
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
            VM.StackPush(vOrigin);
            VM.StackPush((int)nObjectFilter);
            VM.StackPush(bLineOfSight ? 1 : 0);
            VM.StackPush((int)EngineStructure.Location, lTarget);
            VM.StackPush(fSize);
            VM.StackPush((int)nShape);
            VM.Call(128);
            return VM.StackPopObject();
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
            VM.StackPush(vOrigin);
            VM.StackPush((int)nObjectFilter);
            VM.StackPush(bLineOfSight ? 1 : 0);
            VM.StackPush((int)EngineStructure.Location, lTarget);
            VM.StackPush(fSize);
            VM.StackPush((int)nShape);
            VM.Call(129);
            return VM.StackPopObject();
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
            VM.StackPush((int)EngineStructure.Event, evToRun);
            VM.StackPush(oObject);
            VM.Call(131);
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
            VM.StackPush(nUserDefinedEventNumber);
            VM.Call(132);
            return VM.StackPopStruct((int)EngineStructure.Event);
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
            VM.StackPush(nBaseAbilityScore ? 1 : 0);
            VM.StackPush((int)nAbilityType);
            VM.StackPush(oCreature);
            VM.Call(139);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   * Returns TRUE if oCreature is a dead NPC, dead PC or a dying PC.
        /// </summary>
        public static bool GetIsDead(uint oCreature)
        {
            VM.StackPush(oCreature);
            VM.Call(140);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Output vVector to the logfile.
        ///   - vVector
        ///   - bPrepend: if this is TRUE, the message will be prefixed with "PRINTVECTOR:"
        /// </summary>
        public static void PrintVector(Vector3 vVector, bool bPrepend = false)
        {
            VM.StackPush(bPrepend ? 1 : 0);
            VM.StackPush(vVector);
            VM.Call(141);
        }

        /// <summary>
        ///   Create a vector with the specified values for x, y and z
        /// </summary>
        public static Vector3 Vector3(float x = 0.0f, float y = 0.0f, float z = 0.0f)
        {
            VM.StackPush(z);
            VM.StackPush(y);
            VM.StackPush(x);
            VM.Call(142);
            return VM.StackPopVector();
        }

        /// <summary>
        ///   Cause the caller to face vTarget
        /// </summary>
        public static void SetFacingPoint(Vector3 vTarget)
        {
            VM.StackPush(vTarget);
            VM.Call(143);
        }

        /// <summary>
        ///   The caller will perform a Melee Touch Attack on oTarget
        ///   This is not an action, and it assumes the caller is already within range of
        ///   oTarget
        ///   * Returns 0 on a miss, 1 on a hit and 2 on a critical hit
        /// </summary>
        public static TouchAttackReturn TouchAttackMelee(uint oTarget, bool bDisplayFeedback = true)
        {
            VM.StackPush(bDisplayFeedback ? 1 : 0);
            VM.StackPush(oTarget);
            VM.Call(146);
            return (TouchAttackReturn)VM.StackPopInt();
        }

        /// <summary>
        ///   The caller will perform a Ranged Touch Attack on oTarget
        ///   * Returns 0 on a miss, 1 on a hit and 2 on a critical hit
        /// </summary>
        public static TouchAttackReturn TouchAttackRanged(uint oTarget, bool bDisplayFeedback = true)
        {
            VM.StackPush(bDisplayFeedback ? 1 : 0);
            VM.StackPush(oTarget);
            VM.Call(147);
            return (TouchAttackReturn)VM.StackPopInt();
        }

        /// <summary>
        ///   Get the distance in metres between oObjectA and oObjectB.
        ///   * Return value if either object is invalid: 0.0f
        /// </summary>
        public static float GetDistanceBetween(uint oObjectA, uint oObjectB)
        {
            VM.StackPush(oObjectB);
            VM.StackPush(oObjectA);
            VM.Call(151);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Set oObject's local location variable sVarname to lValue
        /// </summary>
        public static void SetLocalLocation(uint oObject, string sVarName, Location lValue)
        {
            VM.StackPush((int)EngineStructure.Location, lValue);
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(152);
        }

        /// <summary>
        ///   Get oObject's local location variable sVarname
        /// </summary>
        public static Location GetLocalLocation(uint oObject, string sVarName)
        {
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(153);
            return VM.StackPopStruct((int)EngineStructure.Location);
        }

        /// <summary>
        ///   Set whether oTarget's action stack can be modified
        /// </summary>
        public static void SetCommandable(bool nCommandable, uint oTarget = OBJECT_INVALID)
        {
            VM.StackPush(oTarget);
            VM.StackPush(nCommandable ? 1 : 0);
            VM.Call(162);
        }

        /// <summary>
        ///   Determine whether oTarget's action stack can be modified.
        /// </summary>
        public static bool GetCommandable(uint oTarget = OBJECT_INVALID)
        {
            VM.StackPush(oTarget);
            VM.Call(163);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Get the number of hitdice for oCreature.
        ///   * Return value if oCreature is not a valid creature: 0
        /// </summary>
        public static int GetHitDice(uint oCreature)
        {
            VM.StackPush(oCreature);
            VM.Call(166);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   The action subject will follow oFollow until a ClearAllActions() is called.
        ///   - oFollow: this is the object to be followed
        ///   - fFollowDistance: follow distance in metres
        ///   * No return value
        /// </summary>
        public static void ActionForceFollowObject(uint oFollow, float fFollowDistance = 0.0f)
        {
            VM.StackPush(fFollowDistance);
            VM.StackPush(oFollow);
            VM.Call(167);
        }

        /// <summary>
        ///   Get the Tag of oObject
        ///   * Return value if oObject is not a valid object: ""
        /// </summary>
        public static string GetTag(uint oObject)
        {
            VM.StackPush(oObject);
            VM.Call(168);
            return NWNCore.NativeFunctions.StackPopStringUTF8();
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
            VM.StackPush(oTarget);
            VM.StackPush(oCaster);
            VM.Call(169);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   * Returns TRUE if oObject is listening for something
        /// </summary>
        public static bool GetIsListening(uint oObject)
        {
            VM.StackPush(oObject);
            VM.Call(174);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Set whether oObject is listening.
        /// </summary>
        public static void SetListening(uint oObject, bool bValue)
        {
            VM.StackPush(bValue ? 1 : 0);
            VM.StackPush(oObject);
            VM.Call(175);
        }

        /// <summary>
        ///   Set the string for oObject to listen for.
        ///   Note: this does not set oObject to be listening.
        /// </summary>
        public static void SetListenPattern(uint oObject, string sPattern, int nNumber = 0)
        {
            VM.StackPush(nNumber);
            VM.StackPush(sPattern);
            VM.StackPush(oObject);
            VM.Call(176);
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
            VM.StackPush(oChair);
            VM.Call(194);
        }

        /// <summary>
        ///   In an onConversation script this gets the number of the string pattern
        ///   matched (the one that triggered the script).
        ///   * Returns -1 if no string matched
        /// </summary>
        public static int GetListenPatternNumber()
        {
            VM.Call(195);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Jump to an object ID, or as near to it as possible.
        /// </summary>
        public static void ActionJumpToObject(uint oToJumpTo, bool bWalkStraightLineToPoint = true)
        {
            VM.StackPush(bWalkStraightLineToPoint ? 1 : 0);
            VM.StackPush(oToJumpTo);
            VM.Call(196);
        }

        /// <summary>
        ///   Get the first waypoint with the specified tag.
        ///   * Returns OBJECT_INVALID if the waypoint cannot be found.
        /// </summary>
        public static uint GetWaypointByTag(string sWaypointTag)
        {
            VM.StackPush(sWaypointTag);
            VM.Call(197);
            return VM.StackPopObject();
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
            VM.StackPush(oTransition);
            VM.Call(198);
            return VM.StackPopObject();
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
            VM.StackPush(nNth);
            VM.StackPush(sTag);
            VM.Call(200);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Do nothing for fSeconds seconds.
        /// </summary>
        public static void ActionWait(float fSeconds)
        {
            VM.StackPush(fSeconds);
            VM.Call(202);
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
            VM.StackPush(bPlayHello ? 1 : 0);
            VM.StackPush(bPrivateConversation ? 1 : 0);
            VM.StackPush(sDialogResRef);
            VM.StackPush(oObjectToConverseWith);
            VM.Call(204);
        }

        /// <summary>
        ///   Pause the current conversation.
        /// </summary>
        public static void ActionPauseConversation()
        {
            VM.Call(205);
        }


        /// <summary>
        ///   Resume a conversation after it has been paused.
        /// </summary>
        public static void ActionResumeConversation()
        {
            VM.Call(206);
        }


        /// <summary>
        ///   Get the creature that is currently sitting on the specified object.
        ///   - oChair
        ///   * Returns OBJECT_INVALID if oChair is not a valid placeable.
        /// </summary>
        public static uint GetSittingCreature(uint oChair)
        {
            VM.StackPush(oChair);
            VM.Call(210);
            return VM.StackPopObject();
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
            VM.StackPush(oTarget);
            VM.Call(211);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   * Returns TRUE if oCreature is a Player Controlled character.
        /// </summary>
        public static bool GetIsPC(uint oCreature)
        {
            VM.StackPush(oCreature);
            VM.Call(217);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   The caller will immediately speak sStringToSpeak (this is different from
        ///   ActionSpeakString)
        ///   - sStringToSpeak
        ///   - nTalkVolume: TALKVOLUME_*
        /// </summary>
        public static void SpeakString(string sStringToSpeak, TalkVolume nTalkVolume = TalkVolume.Talk)
        {
            VM.StackPush((int)nTalkVolume);
            VM.StackPush(sStringToSpeak);
            VM.Call(221);
        }

        /// <summary>
        ///   Get the location of the caller's last spell target.
        /// </summary>
        public static Location GetSpellTargetLocation()
        {
            VM.Call(222);
            return VM.StackPopStruct((int)EngineStructure.Location);
        }

        /// <summary>
        ///   Get the orientation value from lLocation.
        /// </summary>
        public static float GetFacingFromLocation(Location lLocation)
        {
            VM.StackPush((int)EngineStructure.Location, lLocation);
            VM.Call(225);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Convert nInteger into a floating point number.
        /// </summary>
        public static float IntToFloat(int nInteger)
        {
            VM.StackPush(nInteger);
            VM.Call(230);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Convert fFloat into the nearest integer.
        /// </summary>
        public static int FloatToInt(float fFloat)
        {
            VM.StackPush(fFloat);
            VM.Call(231);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Convert sNumber into an integer.
        /// </summary>
        public static int StringToInt(string sNumber)
        {
            VM.StackPush(sNumber);
            VM.Call(232);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Convert sNumber into a floating point number.
        /// </summary>
        public static float StringToFloat(string sNumber)
        {
            VM.StackPush(sNumber);
            VM.Call(233);
            return VM.StackPopFloat();
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
            VM.StackPush(bInstantSpell ? 1 : 0);
            VM.StackPush((int)nProjectilePathType);
            VM.StackPush(bCheat ? 1 : 0);
            VM.StackPush((int)nMetaMagic);
            VM.StackPush((int)EngineStructure.Location, lTargetLocation);
            VM.StackPush((int)nSpell);
            VM.Call(234);
        }

        /// <summary>
        ///   Get the PC that is involved in the conversation.
        ///   * Returns OBJECT_INVALID on error.
        /// </summary>
        public static uint GetPCSpeaker()
        {
            VM.Call(238);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Get a string from the talk table using nStrRef.
        /// </summary>
        public static string GetStringByStrRef(int nStrRef, Gender nGender = Gender.Male)
        {
            VM.StackPush((int)nGender);
            VM.StackPush(nStrRef);
            VM.Call(239);
            return VM.StackPopString();
        }

        /// <summary>
        ///   Causes the creature to speak a translated string.
        ///   - nStrRef: Reference of the string in the talk table
        ///   - nTalkVolume: TALKVOLUME_*
        /// </summary>
        public static void ActionSpeakStringByStrRef(int nStrRef, TalkVolume nTalkVolume = TalkVolume.Talk)
        {
            VM.StackPush((int)nTalkVolume);
            VM.StackPush(nStrRef);
            VM.Call(240);
        }

        /// <summary>
        ///   Get the module.
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetModule()
        {
            VM.Call(242);
            return VM.StackPopObject();
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
            VM.StackPush(bHarmful ? 1 : 0);
            VM.StackPush((int)nSpell);
            VM.StackPush(oCaster);
            VM.Call(244);
            return VM.StackPopStruct((int)EngineStructure.Event);
        }

        /// <summary>
        ///   This is for use in a "Spell Cast" script, it gets who cast the spell.
        ///   The spell could have been cast by a creature, placeable or door.
        ///   * Returns OBJECT_INVALID if the caller is not a creature, placeable or door.
        /// </summary>
        public static uint GetLastSpellCaster()
        {
            VM.Call(245);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   This is for use in a "Spell Cast" script, it gets the ID of the spell that
        ///   was cast.
        /// </summary>
        public static int GetLastSpell()
        {
            VM.Call(246);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   This is for use in a user-defined script, it gets the event number.
        /// </summary>
        public static int GetUserDefinedEventNumber()
        {
            VM.Call(247);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   This is for use in a Spell script, it gets the ID of the spell that is being
        ///   cast (SPELL_*).
        /// </summary>
        public static int GetSpellId()
        {
            VM.Call(248);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Generate a random name.
        ///   nNameType: The type of random name to be generated (NAME_*)
        /// </summary>
        public static string RandomName(Name nNameType = Name.FirstGenericMale)
        {
            VM.StackPush((int)nNameType);
            VM.Call(249);
            return NWNCore.NativeFunctions.StackPopStringUTF8();
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
            VM.StackPush(bOriginalName ? 1 : 0);
            VM.StackPush(oObject);
            VM.Call(253);
            return VM.StackPopString();
        }

        /// <summary>
        ///   Use this in a conversation script to get the person with whom you are conversing.
        ///   * Returns OBJECT_INVALID if the caller is not a valid creature.
        /// </summary>
        public static uint GetLastSpeaker()
        {
            VM.Call(254);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnDialog script to start up the dialog tree.
        ///   - sResRef: if this is not specified, the default dialog file will be used
        ///   - oObjectToDialog: if this is not specified the person that triggered the
        ///   event will be used
        /// </summary>
        public static int BeginConversation(string sResRef = "", uint oObjectToDialog = OBJECT_INVALID)
        {
            VM.StackPush(oObjectToDialog);
            VM.StackPush(sResRef);
            VM.Call(255);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Use this in an OnPerception script to get the object that was perceived.
        ///   * Returns OBJECT_INVALID if the caller is not a valid creature.
        /// </summary>
        public static uint GetLastPerceived()
        {
            VM.Call(256);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnPerception script to determine whether the object that was
        ///   perceived was heard.
        /// </summary>
        public static bool GetLastPerceptionHeard()
        {
            VM.Call(257);
            return Convert.ToBoolean(VM.StackPopInt());
        }

        /// <summary>
        ///   Use this in an OnPerception script to determine whether the object that was
        ///   perceived has become inaudible.
        /// </summary>
        public static bool GetLastPerceptionInaudible()
        {
            VM.Call(258);
            return Convert.ToBoolean(VM.StackPopInt());
        }

        /// <summary>
        ///   Use this in an OnPerception script to determine whether the object that was
        ///   perceived was seen.
        /// </summary>
        public static bool GetLastPerceptionSeen()
        {
            VM.Call(259);
            return Convert.ToBoolean(VM.StackPopInt());
        }

        /// <summary>
        ///   Use this in an OnClosed script to get the object that closed the door or placeable.
        ///   * Returns OBJECT_INVALID if the caller is not a valid door or placeable.
        /// </summary>
        public static uint GetLastClosedBy()
        {
            VM.Call(260);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnPerception script to determine whether the object that was
        ///   perceived has vanished.
        /// </summary>
        public static bool GetLastPerceptionVanished()
        {
            VM.Call(261);
            return Convert.ToBoolean(VM.StackPopInt());
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
            VM.StackPush((int)nPersistentZone);
            VM.StackPush((int)nResidentObjectType);
            VM.StackPush(oPersistentObject);
            VM.Call(262);
            return VM.StackPopObject();
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
            VM.StackPush((int)nPersistentZone);
            VM.StackPush((int)nResidentObjectType);
            VM.StackPush(oPersistentObject);
            VM.Call(263);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   This returns the creator of oAreaOfEffectObject.
        ///   * Returns OBJECT_INVALID if oAreaOfEffectObject is not a valid Area of Effect object.
        /// </summary>
        public static uint GetAreaOfEffectCreator(uint oAreaOfEffectObject = OBJECT_INVALID)
        {
            VM.StackPush(oAreaOfEffectObject);
            VM.Call(264);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Delete oObject's local integer variable sVarName
        /// </summary>
        public static void DeleteLocalInt(uint oObject, string sVarName)
        {
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(265);
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
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(266);
        }

        /// <summary>
        ///   Delete oObject's local string variable sVarName
        /// </summary>
        public static void DeleteLocalString(uint oObject, string sVarName)
        {
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(267);
        }

        /// <summary>
        ///   Delete oObject's local object variable sVarName
        /// </summary>
        public static void DeleteLocalObject(uint oObject, string sVarName)
        {
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(268);
        }

        /// <summary>
        ///   Delete oObject's local location variable sVarName
        /// </summary>
        public static void DeleteLocalLocation(uint oObject, string sVarName)
        {
            VM.StackPush(sVarName);
            VM.StackPush(oObject);
            VM.Call(269);
        }

        /// <summary>
        ///   Convert oObject into a hexadecimal string.
        /// </summary>
        public static string ObjectToString(uint oObject)
        {
            VM.StackPush(oObject);
            VM.Call(272);
            return NWNCore.NativeFunctions.StackPopStringUTF8();
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
            VM.StackPush(oVersus);
            VM.StackPush((int)nImmunityType);
            VM.StackPush(oCreature);
            VM.Call(274);
            return VM.StackPopInt() == 1;
        }

        /// <summary>
        ///   Determine whether oEncounter is active.
        /// </summary>
        public static int GetEncounterActive(uint oEncounter = OBJECT_INVALID)
        {
            VM.StackPush(oEncounter);
            VM.Call(276);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Set oEncounter's active state to nNewValue.
        ///   - nNewValue: TRUE/FALSE
        ///   - oEncounter
        /// </summary>
        public static void SetEncounterActive(int nNewValue, uint oEncounter = OBJECT_INVALID)
        {
            VM.StackPush(oEncounter);
            VM.StackPush(nNewValue);
            VM.Call(277);
        }

        /// <summary>
        ///   Get the maximum number of times that oEncounter will spawn.
        /// </summary>
        public static int GetEncounterSpawnsMax(uint oEncounter = OBJECT_INVALID)
        {
            VM.StackPush(oEncounter);
            VM.Call(278);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Set the maximum number of times that oEncounter can spawn
        /// </summary>
        public static void SetEncounterSpawnsMax(int nNewValue, uint oEncounter = OBJECT_INVALID)
        {
            VM.StackPush(oEncounter);
            VM.StackPush(nNewValue);
            VM.Call(279);
        }

        /// <summary>
        ///   Get the number of times that oEncounter has spawned so far
        /// </summary>
        public static int GetEncounterSpawnsCurrent(uint oEncounter = OBJECT_INVALID)
        {
            VM.StackPush(oEncounter);
            VM.Call(280);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Set the number of times that oEncounter has spawned so far
        /// </summary>
        public static void SetEncounterSpawnsCurrent(int nNewValue, uint oEncounter = OBJECT_INVALID)
        {
            VM.StackPush(oEncounter);
            VM.StackPush(nNewValue);
            VM.Call(281);
        }

        /// <summary>
        ///   Set the value for a custom token.
        /// </summary>
        public static void SetCustomToken(int nCustomTokenNumber, string sTokenValue)
        {
            VM.StackPush(sTokenValue);
            VM.StackPush(nCustomTokenNumber);
            VM.Call(284);
        }

        /// <summary>
        ///   Determine whether oCreature has nFeat, and nFeat is useable.
        ///   - nFeat: FEAT_*
        ///   - oCreature
        /// </summary>
        public static bool GetHasFeat(FeatType nFeat, uint oCreature = OBJECT_INVALID)
        {
            VM.StackPush(oCreature);
            VM.StackPush((int)nFeat);
            VM.Call(285);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Determine whether oCreature has nSkill, and nSkill is useable.
        ///   - nSkill: SKILL_*
        ///   - oCreature
        /// </summary>
        public static bool GetHasSkill(NWNSkillType nSkill, uint oCreature = OBJECT_INVALID)
        {
            VM.StackPush(oCreature);
            VM.StackPush((int)nSkill);
            VM.Call(286);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Use nFeat on oTarget.
        ///   - nFeat: FEAT_*
        ///   - oTarget
        /// </summary>
        public static void ActionUseFeat(FeatType nFeat, uint oTarget)
        {
            VM.StackPush(oTarget);
            VM.StackPush((int)nFeat);
            VM.Call(287);
        }

        /// <summary>
        ///   Runs the action "UseSkill" on the current creature
        ///   Use nSkill on oTarget.
        ///   - nSkill: SKILL_*
        ///   - oTarget
        ///   - nSubSkill: SUBSKILL_*
        ///   - oItemUsed: Item to use in conjunction with the skill
        /// </summary>
        public static void ActionUseSkill(NWNSkillType nSkill, uint oTarget, SubSkill nSubSkill = SubSkill.None,
            uint oItemUsed = OBJECT_INVALID)
        {
            VM.StackPush(oItemUsed);
            VM.StackPush((int)nSubSkill);
            VM.StackPush(oTarget);
            VM.StackPush((int)nSkill);
            VM.Call(288);
        }

        /// <summary>
        ///   Determine whether oSource sees oTarget.
        ///   NOTE: This *only* works on creatures, as visibility lists are not
        ///   maintained for non-creature objects.
        /// </summary>
        public static bool GetObjectSeen(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            VM.StackPush(oSource);
            VM.StackPush(oTarget);
            VM.Call(289);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Determine whether oSource hears oTarget.
        ///   NOTE: This *only* works on creatures, as visibility lists are not
        ///   maintained for non-creature objects.
        /// </summary>
        public static bool GetObjectHeard(uint oTarget, uint oSource = OBJECT_INVALID)
        {
            VM.StackPush(oSource);
            VM.StackPush(oTarget);
            VM.Call(290);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Use this in an OnPlayerDeath module script to get the last player that died.
        /// </summary>
        public static uint GetLastPlayerDied()
        {
            VM.Call(291);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnItemLost script to get the item that was lost/dropped.
        ///   * Returns OBJECT_INVALID if the module is not valid.
        /// </summary>
        public static uint GetModuleItemLost()
        {
            VM.Call(292);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Use this in an OnItemLost script to get the creature that lost the item.
        ///   * Returns OBJECT_INVALID if the module is not valid.
        /// </summary>
        public static uint GetModuleItemLostBy()
        {
            VM.Call(293);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Set the difficulty level of oEncounter.
        ///   - nEncounterDifficulty: ENCOUNTER_DIFFICULTY_*
        ///   - oEncounter
        /// </summary>
        public static void SetEncounterDifficulty(EncounterDifficulty nEncounterDifficulty,
            uint oEncounter = OBJECT_INVALID)
        {
            VM.StackPush(oEncounter);
            VM.StackPush((int)nEncounterDifficulty);
            VM.Call(296);
        }

        /// <summary>
        ///   Get the difficulty level of oEncounter.
        /// </summary>
        public static int GetEncounterDifficulty(uint oEncounter = OBJECT_INVALID)
        {
            VM.StackPush(oEncounter);
            VM.Call(297);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the distance between lLocationA and lLocationB.
        /// </summary>
        public static float GetDistanceBetweenLocations(Location lLocationA, Location lLocationB)
        {
            VM.StackPush((int)EngineStructure.Location, lLocationB);
            VM.StackPush((int)EngineStructure.Location, lLocationA);
            VM.Call(298);
            return VM.StackPopFloat();
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
            VM.StackPush(oSaveVersus);
            VM.StackPush((int)nSaveType);
            VM.StackPush(nDC);
            VM.StackPush(oTarget);
            VM.StackPush(nDamage);
            VM.Call(299);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Play nAnimation immediately.
        ///   - nAnimation: ANIMATION_*
        ///   - fSpeed
        ///   - fSeconds
        /// </summary>
        public static void PlayAnimation(Animation nAnimation, float fSpeed = 1.0f, float fSeconds = 0.0f)
        {
            VM.StackPush(fSeconds);
            VM.StackPush(fSpeed);
            VM.StackPush((int)nAnimation);
            VM.Call(300);
        }

        /// <summary>
        ///   Create a Spell Talent.
        ///   - nSpell: SPELL_*
        /// </summary>
        public static Talent TalentSpell(Spell nSpell)
        {
            VM.StackPush((int)nSpell);
            VM.Call(301);
            return VM.StackPopStruct((int)EngineStructure.Talent);
        }

        /// <summary>
        ///   Create a Feat Talent.
        ///   - nFeat: FEAT_*
        /// </summary>
        public static Talent TalentFeat(FeatType nFeat)
        {
            VM.StackPush((int)nFeat);
            VM.Call(302);
            return VM.StackPopStruct((int)EngineStructure.Talent);
        }

        /// <summary>
        ///   Create a Skill Talent.
        ///   - nSkill: SKILL_*
        /// </summary>
        public static Talent TalentSkill(NWNSkillType nSkill)
        {
            VM.StackPush((int)nSkill);
            VM.Call(303);
            return VM.StackPopStruct((int)EngineStructure.Talent);
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
            VM.StackPush(oObject);
            VM.StackPush((int)nSpell);
            VM.Call(304);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Get the spell (SPELL_*) that applied eSpellEffect.
        ///   * Returns -1 if eSpellEffect was applied outside a spell script.
        /// </summary>
        public static int GetEffectSpellId(Effect eSpellEffect)
        {
            VM.StackPush((int)EngineStructure.Effect, eSpellEffect);
            VM.Call(305);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Determine whether oCreature has tTalent.
        /// </summary>
        public static bool GetCreatureHasTalent(Talent tTalent, uint oCreature = OBJECT_INVALID)
        {
            VM.StackPush(oCreature);
            VM.StackPush((int)EngineStructure.Talent, tTalent);
            VM.Call(306);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Get a random talent of oCreature, within nCategory.
        ///   - nCategory: TALENT_CATEGORY_*
        ///   - oCreature
        /// </summary>
        public static Talent GetCreatureTalentRandom(TalentCategory nCategory, uint oCreature = OBJECT_INVALID)
        {
            VM.StackPush(oCreature);
            VM.StackPush((int)nCategory);
            VM.Call(307);
            return VM.StackPopStruct((int)EngineStructure.Talent);
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
            VM.StackPush(oCreature);
            VM.StackPush(nCRMax);
            VM.StackPush((int)nCategory);
            VM.Call(308);
            return VM.StackPopStruct((int)EngineStructure.Talent);
        }

        /// <summary>
        ///   Use tChosenTalent on oTarget.
        /// </summary>
        public static void ActionUseTalentOnObject(Talent tChosenTalent, uint oTarget)
        {
            VM.StackPush(oTarget);
            VM.StackPush((int)EngineStructure.Talent, tChosenTalent);
            VM.Call(309);
        }

        /// <summary>
        ///   Use tChosenTalent at lTargetLocation.
        /// </summary>
        public static void ActionUseTalentAtLocation(Talent tChosenTalent, Location lTargetLocation)
        {
            VM.StackPush((int)EngineStructure.Location, lTargetLocation);
            VM.StackPush((int)EngineStructure.Talent, tChosenTalent);
            VM.Call(310);
        }

        /// <summary>
        ///   * Returns TRUE if oCreature is of a playable racial type.
        /// </summary>
        public static bool GetIsPlayableRacialType(uint oCreature)
        {
            VM.StackPush(oCreature);
            VM.Call(312);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Jump to lDestination.  The action is added to the TOP of the action queue.
        /// </summary>
        public static void JumpToLocation(Location lDestination)
        {
            VM.StackPush((int)EngineStructure.Location, lDestination);
            VM.Call(313);
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
        public static int GetSkillRank(NWNSkillType nSkill, uint oTarget = OBJECT_INVALID, bool nBaseSkillRank = false)
        {
            VM.StackPush(nBaseSkillRank ? 1 : 0);
            VM.StackPush(oTarget);
            VM.StackPush((int)nSkill);
            VM.Call(315);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the attack target of oCreature.
        ///   This only works when oCreature is in combat.
        /// </summary>
        public static uint GetAttackTarget(uint oCreature = OBJECT_INVALID)
        {
            VM.StackPush(oCreature);
            VM.Call(316);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Get the attack type (SPECIAL_ATTACK_*) of oCreature's last attack.
        ///   This only works when oCreature is in combat.
        /// </summary>
        public static SpecialAttack GetLastAttackType(uint oCreature = OBJECT_INVALID)
        {
            VM.StackPush(oCreature);
            VM.Call(317);
            return (SpecialAttack)VM.StackPopInt();
        }

        /// <summary>
        ///   Get the attack mode (COMBAT_MODE_*) of oCreature's last attack.
        ///   This only works when oCreature is in combat.
        /// </summary>
        public static CombatMode GetLastAttackMode(uint oCreature = OBJECT_INVALID)
        {
            VM.StackPush(oCreature);
            VM.Call(318);
            return (CombatMode)VM.StackPopInt();
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
            VM.Call(326);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Initialise oTarget to listen for the standard Associates commands.
        /// </summary>
        public static void SetAssociateListenPatterns(uint oTarget = OBJECT_INVALID)
        {
            VM.StackPush(oTarget);
            VM.Call(327);
        }

        /// <summary>
        ///   Get the last weapon that oCreature used in an attack.
        ///   * Returns OBJECT_INVALID if oCreature did not attack, or has no weapon equipped.
        /// </summary>
        public static uint GetLastWeaponUsed(uint oCreature)
        {
            VM.StackPush(oCreature);
            VM.Call(328);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Use oPlaceable.
        /// </summary>
        public static void ActionInteractObject(uint oPlaceable)
        {
            VM.StackPush(oPlaceable);
            VM.Call(329);
        }

        /// <summary>
        ///   Get the last object that used the placeable object that is calling this function.
        ///   * Returns OBJECT_INVALID if it is called by something other than a placeable or
        ///   a door.
        /// </summary>
        public static uint GetLastUsedBy()
        {
            VM.Call(330);
            return VM.StackPopObject();
        }


        /// <summary>
        ///   Get the amount of damage of type nDamageType that has been dealt to the caller.
        ///   - nDamageType: DAMAGE_TYPE_*
        /// </summary>
        public static int GetDamageDealtByType(DamageType nDamageType)
        {
            VM.StackPush((int)nDamageType);
            VM.Call(344);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the total amount of damage that has been dealt to the caller.
        /// </summary>
        public static int GetTotalDamageDealt()
        {
            VM.Call(345);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the last object that damaged oObject
        ///   * Returns OBJECT_INVALID if the passed in object is not a valid object.
        /// </summary>
        public static uint GetLastDamager(uint oObject = OBJECT_INVALID)
        {
            VM.StackPush(oObject);
            VM.Call(346);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Get the last object that disarmed the trap on the caller.
        ///   * Returns OBJECT_INVALID if the caller is not a valid placeable, trigger or
        ///   door.
        /// </summary>
        public static uint GetLastDisarmed()
        {
            VM.Call(347);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Get the last object that disturbed the inventory of the caller.
        ///   * Returns OBJECT_INVALID if the caller is not a valid creature or placeable.
        /// </summary>
        public static uint GetLastDisturbed()
        {
            VM.Call(348);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Get the last object that locked the caller.
        ///   * Returns OBJECT_INVALID if the caller is not a valid door or placeable.
        /// </summary>
        public static uint GetLastLocked()
        {
            VM.Call(349);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Get the last object that unlocked the caller.
        ///   * Returns OBJECT_INVALID if the caller is not a valid door or placeable.
        /// </summary>
        public static uint GetLastUnlocked()
        {
            VM.Call(350);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   * Returns TRUE if tTalent is valid.
        /// </summary>
        public static bool GetIsTalentValid(Talent tTalent)
        {
            VM.StackPush((int)EngineStructure.Talent, tTalent);
            VM.Call(359);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Causes the action subject to move away from lMoveAwayFrom.
        /// </summary>
        public static void ActionMoveAwayFromLocation(Location lMoveAwayFrom, bool bRun = false,
            float fMoveAwayRange = 40.0f)
        {
            VM.StackPush(fMoveAwayRange);
            VM.StackPush(bRun ? 1 : 0);
            VM.StackPush((int)EngineStructure.Location, lMoveAwayFrom);
            VM.Call(360);
        }

        /// <summary>
        ///   Get the target that the caller attempted to attack - this should be used in
        ///   conjunction with GetAttackTarget(). This value is set every time an attack is
        ///   made, and is reset at the end of combat.
        ///   * Returns OBJECT_INVALID if the caller is not a valid creature.
        /// </summary>
        public static uint GetAttemptedAttackTarget()
        {
            VM.Call(361);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Get the type (TALENT_TYPE_*) of tTalent.
        /// </summary>
        public static TalentType GetTypeFromTalent(Talent tTalent)
        {
            VM.StackPush((int)EngineStructure.Talent, tTalent);
            VM.Call(362);
            return (TalentType)VM.StackPopInt();
        }

        /// <summary>
        ///   Get the ID of tTalent.  This could be a SPELL_*, FEAT_* or SKILL_*.
        /// </summary>
        public static int GetIdFromTalent(Talent tTalent)
        {
            VM.StackPush((int)EngineStructure.Talent, tTalent);
            VM.Call(363);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the public part of the CD Key that oPlayer used when logging in.
        ///   - nSinglePlayerCDKey: If set to TRUE, the player's public CD Key will
        ///   be returned when the player is playing in single player mode
        ///   (otherwise returns an empty string in single player mode).
        /// </summary>
        public static string GetPCPublicCDKey(uint oPlayer, bool nSinglePlayerCDKey = false)
        {
            VM.StackPush(nSinglePlayerCDKey ? 1 : 0);
            VM.StackPush(oPlayer);
            VM.Call(369);
            return NWNCore.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Get the IP address from which oPlayer has connected.
        /// </summary>
        public static string GetPCIPAddress(uint oPlayer)
        {
            VM.StackPush(oPlayer);
            VM.Call(370);
            return NWNCore.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Get the name of oPlayer.
        /// </summary>
        public static string GetPCPlayerName(uint oPlayer)
        {
            VM.StackPush(oPlayer);
            VM.Call(371);
            return VM.StackPopString();
        }

        /// <summary>
        ///   Sets oPlayer and oTarget to like each other.
        /// </summary>
        public static void SetPCLike(uint oPlayer, uint oTarget)
        {
            VM.StackPush(oTarget);
            VM.StackPush(oPlayer);
            VM.Call(372);
        }

        /// <summary>
        ///   Sets oPlayer and oTarget to dislike each other.
        /// </summary>
        public static void SetPCDislike(uint oPlayer, uint oTarget)
        {
            VM.StackPush(oTarget);
            VM.StackPush(oPlayer);
            VM.Call(373);
        }

        /// <summary>
        ///   Send a server message (szMessage) to the oPlayer.
        /// </summary>
        public static void SendMessageToPC(uint oPlayer, string szMessage)
        {
            VM.StackPush(szMessage);
            VM.StackPush(oPlayer);
            VM.Call(374);
        }

        /// <summary>
        ///   Get the target at which the caller attempted to cast a spell.
        ///   This value is set every time a spell is cast and is reset at the end of
        ///   combat.
        ///   * Returns OBJECT_INVALID if the caller is not a valid creature.
        /// </summary>
        public static uint GetAttemptedSpellTarget()
        {
            VM.Call(375);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Get the experience assigned in the journal editor for szPlotID.
        /// </summary>
        public static int GetJournalQuestExperience(string szPlotID)
        {
            VM.StackPush(szPlotID);
            VM.Call(384);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Jump to oToJumpTo (the action is added to the top of the action queue).
        /// </summary>
        public static void JumpToObject(uint oToJumpTo, bool nWalkStraightLineToPoint = true)
        {
            VM.StackPush(nWalkStraightLineToPoint ? 1 : 0);
            VM.StackPush(oToJumpTo);
            VM.Call(385);
        }

        /// <summary>
        ///   Convert nInteger to hex, returning the hex value as a string.
        ///   * Return value has the format "0x????????" where each ? will be a hex digit
        ///   (8 digits in total).
        /// </summary>
        public static string IntToHexString(int nInteger)
        {
            VM.StackPush(nInteger);
            VM.Call(396);
            return NWNCore.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Get the starting location of the module.
        /// </summary>
        public static Location GetStartingLocation()
        {
            VM.Call(411);
            return VM.StackPopStruct((int)EngineStructure.Location);
        }

        /// <summary>
        ///   * Returns TRUE if the weapon equipped is capable of damaging oVersus.
        /// </summary>
        public static bool GetIsWeaponEffective(uint oVersus = OBJECT_INVALID, bool bOffHand = false)
        {
            VM.StackPush(bOffHand ? 1 : 0);
            VM.StackPush(oVersus);
            VM.Call(422);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Use this in a SpellCast script to determine whether the spell was considered
        ///   harmful.
        ///   * Returns TRUE if the last spell cast was harmful.
        /// </summary>
        public static bool GetLastSpellHarmful()
        {
            VM.Call(423);
            return VM.StackPopInt() != 0;
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
            string sWaypointTag = "", bool bSeamless = false)
        {
            VM.StackPush(bSeamless ? 1 : 0);
            VM.StackPush(sWaypointTag);
            VM.StackPush(sPassword);
            VM.StackPush(sIPaddress);
            VM.StackPush(oTarget);
            VM.Call(474);
        }

        /// <summary>
        ///   The action subject will fake casting a spell at oTarget; the conjure and cast
        ///   animations and visuals will occur, nothing else.
        ///   - nSpell
        ///   - oTarget
        ///   - nProjectilePathType: PROJECTILE_PATH_TYPE_*
        /// </summary>
        public static void ActionCastFakeSpellAtObject(Spell nSpell, uint oTarget,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default)
        {
            VM.StackPush((int)nProjectilePathType);
            VM.StackPush(oTarget);
            VM.StackPush((int)nSpell);
            VM.Call(501);
        }

        /// <summary>
        ///   The action subject will fake casting a spell at lLocation; the conjure and
        ///   cast animations and visuals will occur, nothing else.
        ///   - nSpell
        ///   - lTarget
        ///   - nProjectilePathType: PROJECTILE_PATH_TYPE_*
        /// </summary>
        public static void ActionCastFakeSpellAtLocation(Spell nSpell, Location lTarget,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default)
        {
            VM.StackPush((int)nProjectilePathType);
            VM.StackPush((int)EngineStructure.Location, lTarget);
            VM.StackPush((int)nSpell);
            VM.Call(502);
        }

        /// <summary>
        ///   Shut down the currently loaded module and start a new one (moving all
        ///   currently-connected players to the starting point.
        /// </summary>
        public static void StartNewModule(string sModuleName)
        {
            VM.StackPush(sModuleName);
            VM.Call(509);
        }

        /// <summary>
        ///   Only if we are in a single player game, AutoSave the game.
        /// </summary>
        public static void DoSinglePlayerAutoSave()
        {
            VM.Call(512);
        }

        /// <summary>
        ///   Get the game difficulty (GAME_DIFFICULTY_*).
        /// </summary>
        public static int GetGameDifficulty()
        {
            VM.Call(513);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the current action (ACTION_*) that oObject is executing.
        /// </summary>
        public static ActionType GetCurrentAction(uint oObject = OBJECT_INVALID)
        {
            VM.StackPush(oObject);
            VM.Call(522);
            return (ActionType)VM.StackPopInt();
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
            VM.StackPush(bBroadcastToFaction ? 1 : 0);
            VM.StackPush(oCreatureToFloatAbove);
            VM.StackPush(nStrRefToDisplay);
            VM.Call(525);
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
            VM.StackPush(bBroadcastToFaction ? 1 : 0);
            VM.StackPush(oCreatureToFloatAbove);
            VM.StackPush(sStringToDisplay);
            VM.Call(526);
        }

        /// <summary>
        ///   * Returns TRUE if a specific key is required to open the lock on oObject.
        /// </summary>
        public static bool GetLockKeyRequired(uint oObject)
        {
            VM.StackPush(oObject);
            VM.Call(537);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Get the tag of the key that will open the lock on oObject.
        /// </summary>
        public static string GetLockKeyTag(uint oObject)
        {
            VM.StackPush(oObject);
            VM.Call(538);
            return NWNCore.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   * Returns TRUE if the lock on oObject is lockable.
        /// </summary>
        public static bool GetLockLockable(uint oObject)
        {
            VM.StackPush(oObject);
            VM.Call(539);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   Get the DC for unlocking oObject.
        /// </summary>
        public static int GetLockUnlockDC(uint oObject)
        {
            VM.StackPush(oObject);
            VM.Call(540);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Get the DC for locking oObject.
        /// </summary>
        public static int GetLockLockDC(uint oObject)
        {
            VM.StackPush(oObject);
            VM.Call(541);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   - nFeat: FEAT_*
        ///   - oObject
        ///   * Returns TRUE if oObject has effects on it originating from nFeat.
        /// </summary>
        public static int GetHasFeatEffect(int nFeat, uint oObject = OBJECT_INVALID)
        {
            VM.StackPush(oObject);
            VM.StackPush(nFeat);
            VM.Call(543);
            return VM.StackPopInt();
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
            VM.StackPush(bIlluminate ? 1 : 0);
            VM.StackPush(oPlaceable);
            VM.Call(544);
        }

        /// <summary>
        ///   * Returns TRUE if the illumination for oPlaceable is on
        /// </summary>
        public static bool GetPlaceableIllumination(uint oPlaceable = OBJECT_INVALID)
        {
            VM.StackPush(oPlaceable);
            VM.Call(545);
            return VM.StackPopInt() != 0;
        }

        /// <summary>
        ///   - oPlaceable
        ///   - nPlaceableAction: PLACEABLE_ACTION_*
        ///   * Returns TRUE if nPlacebleAction is valid for oPlaceable.
        /// </summary>
        public static int GetIsPlaceableObjectActionPossible(uint oPlaceable, int nPlaceableAction)
        {
            VM.StackPush(nPlaceableAction);
            VM.StackPush(oPlaceable);
            VM.Call(546);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   The caller performs nPlaceableAction on oPlaceable.
        ///   - oPlaceable
        ///   - nPlaceableAction: PLACEABLE_ACTION_*
        /// </summary>
        public static void DoPlaceableObjectAction(uint oPlaceable, int nPlaceableAction)
        {
            VM.StackPush(nPlaceableAction);
            VM.StackPush(oPlaceable);
            VM.Call(547);
        }

        /// <summary>
        ///   Force all the characters of the players who are currently in the game to
        ///   be exported to their respective directories i.e. LocalVault/ServerVault/ etc.
        /// </summary>
        public static void ExportAllCharacters()
        {
            VM.Call(557);
        }

        /// <summary>
        ///   Write sLogEntry as a timestamped entry into the log file
        /// </summary>
        public static void WriteTimestampedLogEntry(string sLogEntry)
        {
            VM.StackPush(sLogEntry);
            VM.Call(560);
        }

        /// <summary>
        ///   Get the module's name in the language of the server that's running it.
        ///   * If there is no entry for the language of the server, it will return an
        ///   empty string
        /// </summary>
        public static string GetModuleName()
        {
            VM.Call(561);
            return VM.StackPopString();
        }

        /// <summary>
        ///   End the currently running game, play sEndMovie then return all players to the
        ///   game's main menu.
        /// </summary>
        public static void EndGame(string sEndMovie)
        {
            VM.StackPush(sEndMovie);
            VM.Call(564);
        }

        /// <summary>
        ///   Counterspell oCounterSpellTarget.
        /// </summary>
        public static void ActionCounterSpell(uint oCounterSpellTarget)
        {
            VM.StackPush(oCounterSpellTarget);
            VM.Call(566);
        }

        /// <summary>
        ///   Get the duration (in seconds) of the sound attached to nStrRef
        ///   * Returns 0.0f if no duration is stored or if no sound is attached
        /// </summary>
        public static float GetStrRefSoundDuration(int nStrRef)
        {
            VM.StackPush(nStrRef);
            VM.Call(571);
            return VM.StackPopFloat();
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
            VM.Call(578);
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
            VM.StackPush(oPlayer);
            VM.StackPush(flFloat);
            VM.StackPush(sVarName);
            VM.StackPush(sCampaignName);
            VM.Call(589);
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
            VM.StackPush(oPlayer);
            VM.StackPush(nInt);
            VM.StackPush(sVarName);
            VM.StackPush(sCampaignName);
            VM.Call(590);
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
            VM.StackPush(oPlayer);
            VM.StackPush(vVector);
            VM.StackPush(sVarName);
            VM.StackPush(sCampaignName);
            VM.Call(591);
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
            VM.StackPush(oPlayer);
            VM.StackPush((int)EngineStructure.Location, locLocation);
            VM.StackPush(sVarName);
            VM.StackPush(sCampaignName);
            VM.Call(592);
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
            VM.StackPush(oPlayer);
            VM.StackPush(sString);
            VM.StackPush(sVarName);
            VM.StackPush(sCampaignName);
            VM.Call(593);
        }

        /// <summary>
        ///   This will delete the entire campaign database if it exists.
        /// </summary>
        public static void DestroyCampaignDatabase(string sCampaignName)
        {
            VM.StackPush(sCampaignName);
            VM.Call(594);
        }

        /// <summary>
        ///   This will read a float from the  specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static float GetCampaignFloat(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            VM.StackPush(oPlayer);
            VM.StackPush(sVarName);
            VM.StackPush(sCampaignName);
            VM.Call(595);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   This will read an int from the  specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static int GetCampaignInt(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            VM.StackPush(oPlayer);
            VM.StackPush(sVarName);
            VM.StackPush(sCampaignName);
            VM.Call(596);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   This will read a vector from the  specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static Vector3 GetCampaignVector(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            VM.StackPush(oPlayer);
            VM.StackPush(sVarName);
            VM.StackPush(sCampaignName);
            VM.Call(597);
            return VM.StackPopVector();
        }

        /// <summary>
        ///   This will read a location from the  specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static Location GetCampaignLocation(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            VM.StackPush(oPlayer);
            VM.StackPush(sVarName);
            VM.StackPush(sCampaignName);
            VM.Call(598);
            return VM.StackPopStruct((int)EngineStructure.Location);
        }

        /// <summary>
        ///   This will read a string from the  specified campaign database
        ///   The database name IS case sensitive and it must be the same for both set and get functions.
        ///   The var name must be unique across the entire database, regardless of the variable type.
        ///   If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static string GetCampaignString(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            VM.StackPush(oPlayer);
            VM.StackPush(sVarName);
            VM.StackPush(sCampaignName);
            VM.Call(599);
            return VM.StackPopString();
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
            VM.StackPush(oPlayer);
            VM.StackPush(sVarName);
            VM.StackPush(sCampaignName);
            VM.Call(601);
        }

        /// <summary>
        ///   Stores an object with the given id.
        ///   NOTE: this command can only be used for storing Creatures and Items.
        ///   Returns 0 if it failled, 1 if it worked.
        ///   If bSaveObjectState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are saved out
        ///   (except for Combined Area Format, which always has object state saved out).
        /// </summary>
        public static int StoreCampaignObject(string sCampaignName, string sVarName, uint oObject, uint oPlayer = OBJECT_INVALID, bool bSaveObjectState = false)
        {
            VM.StackPush(bSaveObjectState ? 1 : 0);
            VM.StackPush(oPlayer);
            VM.StackPush(oObject);
            VM.StackPush(sVarName);
            VM.StackPush(sCampaignName);
            VM.Call(602);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Use RetrieveCampaign with the given id to restore it.
        ///   If you specify an owner, the object will try to be created in their repository
        ///   If the owner can't handle the item (or if it's a creature) it will be created on the ground.
        ///   If bLoadObjectState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are read in.
        /// </summary>
        public static uint RetrieveCampaignObject(string sCampaignName, string sVarName, Location locLocation, uint oOwner = OBJECT_INVALID, uint oPlayer = OBJECT_INVALID, bool bLoadObjectState = false)
        {
            VM.StackPush(bLoadObjectState ? 1 : 0);
            VM.StackPush(oPlayer);
            VM.StackPush(oOwner);
            VM.StackPush((int)EngineStructure.Location, locLocation);
            VM.StackPush(sVarName);
            VM.StackPush(sCampaignName);
            VM.Call(603);
            return VM.StackPopObject();
        }

        /// <summary>
        /// This stores a json out to the specified campaign database
        /// The database name:
        ///  - is case insensitive and it must be the same for both set and get functions.
        ///  - can only contain alphanumeric characters, no spaces.
        /// The var name must be unique across the entire database, regardless of the variable type.
        /// If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static void SetCampaignJson(string sCampaignName, string sVarName, Json jValue, uint oPlayer = OBJECT_INVALID)
        {
            VM.StackPush(oPlayer);
            VM.StackPush((int)EngineStructure.Json, jValue);
            VM.StackPush(sVarName);
            VM.StackPush(sCampaignName);

            VM.Call(1002);
        }

        /// <summary>
        /// This will read a json from the  specified campaign database
        /// The database name:
        ///  - is case insensitive and it must be the same for both set and get functions.
        ///  - can only contain alphanumeric characters, no spaces.
        /// The var name must be unique across the entire database, regardless of the variable type.
        /// If you want a variable to pertain to a specific player in the game, provide a player object.
        /// </summary>
        public static Json GetCampaignJson(string sCampaignName, string sVarName, uint oPlayer = OBJECT_INVALID)
        {
            VM.StackPush(oPlayer);
            VM.StackPush(sVarName);
            VM.StackPush(sCampaignName);
            VM.Call(1003);

            return VM.StackPopStruct((int)EngineStructure.Json);
        }

        /// <summary>
        ///   Gets the length of the specified wavefile, in seconds
        ///   Only works for sounds used for dialog.
        /// </summary>
        public static float GetDialogSoundLength(int nStrRef)
        {
            VM.StackPush(nStrRef);
            VM.Call(694);
            return VM.StackPopFloat();
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
            VM.StackPush(nRow);
            VM.StackPush(sColumn);
            VM.StackPush(s2DA);
            VM.Call(710);
            return NWNCore.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Force the character of the player specified to be exported to its respective directory
        ///   i.e. LocalVault/ServerVault/ etc.
        /// </summary>
        public static void ExportSingleCharacter(uint oPlayer)
        {
            VM.StackPush(oPlayer);
            VM.Call(719);
        }

        /// <summary>
        ///   This will play a sound that is associated with a stringRef, it will be a mono sound from the location of the object
        ///   running the command.
        ///   if nRunAsAction is off then the sound is forced to play intantly.
        /// </summary>
        public static void PlaySoundByStrRef(int nStrRef, bool nRunAsAction = true)
        {
            VM.StackPush(nRunAsAction ? 1 : 0);
            VM.StackPush(nStrRef);
            VM.Call(720);
        }

        /// <summary>
        ///   Get the XP scale being used for the module.
        /// </summary>
        public static int GetModuleXPScale()
        {
            VM.Call(817);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Set the XP scale used by the module.
        ///   - nXPScale: The XP scale to be used. Must be between 0 and 200.
        /// </summary>
        public static void SetModuleXPScale(int nXPScale)
        {
            VM.StackPush(nXPScale);
            VM.Call(818);
        }

        /// <summary>
        ///   Gets the attack bonus limit.
        ///   - The default value is 20.
        /// </summary>
        public static int GetAttackBonusLimit()
        {
            VM.Call(872);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Gets the damage bonus limit.
        ///   - The default value is 100.
        /// </summary>
        public static int GetDamageBonusLimit()
        {
            VM.Call(873);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Gets the saving throw bonus limit.
        ///   - The default value is 20.
        /// </summary>
        public static int GetSavingThrowBonusLimit()
        {
            VM.Call(874);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Gets the ability bonus limit.
        ///   - The default value is 12.
        /// </summary>
        public static int GetAbilityBonusLimit()
        {
            VM.Call(875);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Gets the ability penalty limit.
        ///   - The default value is 30.
        /// </summary>
        public static int GetAbilityPenaltyLimit()
        {
            VM.Call(876);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Gets the skill bonus limit.
        ///   - The default value is 50.
        /// </summary>
        public static int GetSkillBonusLimit()
        {
            VM.Call(877);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Sets the attack bonus limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetAttackBonusLimit(int nNewLimit)
        {
            VM.StackPush(nNewLimit);
            VM.Call(878);
        }

        /// <summary>
        ///   Sets the damage bonus limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetDamageBonusLimit(int nNewLimit)
        {
            VM.StackPush(nNewLimit);
            VM.Call(879);
        }

        /// <summary>
        ///   Sets the saving throw bonus limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetSavingThrowBonusLimit(int nNewLimit)
        {
            VM.StackPush(nNewLimit);
            VM.Call(880);
        }

        /// <summary>
        ///   Sets the ability bonus limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetAbilityBonusLimit(int nNewLimit)
        {
            VM.StackPush(nNewLimit);
            VM.Call(881);
        }

        /// <summary>
        ///   Sets the ability penalty limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetAbilityPenaltyLimit(int nNewLimit)
        {
            VM.StackPush(nNewLimit);
            VM.Call(882);
        }

        /// <summary>
        ///   Sets the skill bonus limit.
        ///   - The minimum value is 0.
        /// </summary>
        public static void SetSkillBonusLimit(int nNewLimit)
        {
            VM.StackPush(nNewLimit);
            VM.Call(883);
        }

        /// <summary>
        ///   Get if oPlayer is currently connected over a relay (instead of directly).
        ///   Returns FALSE for any other object, including OBJECT_INVALID.
        /// </summary>
        public static int GetIsPlayerConnectionRelayed(uint oPlayer)
        {
            VM.StackPush(oPlayer);
            VM.Call(884);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Returns the event script for the given object and handler.
        ///   Will return "" if unset, the object is invalid, or the object cannot
        ///   have the requested handler.
        /// </summary>
        public static string GetEventScript(uint oObject, EventScript nHandler)
        {
            VM.StackPush((int)nHandler);
            VM.StackPush(oObject);
            VM.Call(885);
            return NWNCore.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Sets the given event script for the given object and handler.
        ///   Returns 1 on success, 0 on failure.
        ///   Will fail if oObject is invalid or does not have the requested handler.
        /// </summary>
        public static int SetEventScript(uint oObject, EventScript nHandler, string sScript)
        {
            VM.StackPush(sScript);
            VM.StackPush((int)nHandler);
            VM.StackPush(oObject);
            VM.Call(886);
            return VM.StackPopInt();
        }

        /// <summary>
        ///   Gets a visual transform on the given object.
        ///   - oObject can be any valid Creature, Placeable, Item or Door.
        ///   - nTransform is one of OBJECT_VISUAL_TRANSFORM_*
        ///   Returns the current (or default) value.
        /// </summary>
        public static float GetObjectVisualTransform(uint oObject, ObjectVisualTransform nTransform)
        {
            VM.StackPush((int)nTransform);
            VM.StackPush(oObject);
            VM.Call(887);
            return VM.StackPopFloat();
        }

        /// <summary>
        /// Sets a visual transform on the given object.
        /// - oObject can be any valid Creature, Placeable, Item or Door.
        /// - nTransform is one of OBJECT_VISUAL_TRANSFORM_*
        /// - fValue depends on the transformation to apply.
        /// - nScope is one of OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_* and specific to the object type being VT'ed.
        /// - nBehaviorFlags: bitmask of OBJECT_VISUAL_TRANSFORM_BEHAVIOR_*.
        /// - nRepeats: If > 0: N times, jump back to initial/from state after completing the transform. If -1: Do forever.
        /// Returns the old/previous value.
        /// </summary>
        public static float SetObjectVisualTransform(
            uint oObject, 
            ObjectVisualTransform nTransform, 
            float fValue, 
            Lerp nLerpType = Lerp.None, 
            float fLerpDuration = 0.0f, 
            bool bPauseWithGame = true, 
            ObjectVisualTransformDataScopeType nScope = ObjectVisualTransformDataScopeType.Base, 
            ObjectVisualTransformBehaviorType nBehaviorFlags = ObjectVisualTransformBehaviorType.Default , 
            int nRepeats = 0)
        {
            VM.StackPush(nRepeats);
            VM.StackPush((int)nBehaviorFlags);
            VM.StackPush((int)nScope);
            VM.StackPush(bPauseWithGame ? 1 : 0);
            VM.StackPush(fLerpDuration);
            VM.StackPush((int)nLerpType);
            VM.StackPush(fValue);
            VM.StackPush((int)nTransform);
            VM.StackPush(oObject);
            VM.Call(888);
            return VM.StackPopFloat();
        }

        /// <summary>
        ///   Sets an integer material shader uniform override.
        ///   - sMaterial needs to be a material on that object.
        ///   - sParam needs to be a valid shader parameter already defined on the material.
        /// </summary>
        public static void SetMaterialShaderUniformInt(uint oObject, string sMaterial, string sParam, int nValue)
        {
            VM.StackPush(nValue);
            VM.StackPush(sParam);
            VM.StackPush(sMaterial);
            VM.StackPush(oObject);
            VM.Call(889);
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
            VM.StackPush(fValue4);
            VM.StackPush(fValue3);
            VM.StackPush(fValue2);
            VM.StackPush(fValue1);
            VM.StackPush(sParam);
            VM.StackPush(sMaterial);
            VM.StackPush(oObject);
            VM.Call(890);
        }

        /// <summary>
        ///   Resets material shader parameters on the given object:
        ///   - Supply a material to only reset shader uniforms for meshes with that material.
        ///   - Supply a parameter to only reset shader uniforms of that name.
        ///   - Supply both to only reset shader uniforms of that name on meshes with that material.
        /// </summary>
        public static void ResetMaterialShaderUniforms(uint oObject, string sMaterial = "", string sParam = "")
        {
            VM.StackPush(sParam);
            VM.StackPush(sMaterial);
            VM.StackPush(oObject);
            VM.Call(891);
        }

        /// <summary>
        ///   Vibrate the player's device or controller. Does nothing if vibration is not supported.
        ///   - nMotor is one of VIBRATOR_MOTOR_*
        ///   - fStrength is between 0.0 and 1.0
        ///   - fSeconds is the number of seconds to vibrate
        /// </summary>
        public static void Vibrate(uint oPlayer, int nMotor, float fStrength, float fSeconds)
        {
            VM.StackPush(fSeconds);
            VM.StackPush(fStrength);
            VM.StackPush(nMotor);
            VM.StackPush(oPlayer);
            VM.Call(892);
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
            VM.StackPush(nMaxValue);
            VM.StackPush(nCurValue);
            VM.StackPush(nLastValue);
            VM.StackPush(sId);
            VM.StackPush(oPlayer);
            VM.Call(893);
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
            VM.StackPush(bWrapIntoMain ? 1 : 0);
            VM.StackPush(oObject);
            VM.StackPush(sScriptChunk);
            VM.Call(894);
            return NWNCore.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Returns a UUID. This UUID will not be associated with any object.
        ///   The generated UUID is currently a v4.
        /// </summary>
        public static string GetRandomUUID()
        {
            VM.Call(895);
            return NWNCore.NativeFunctions.StackPopStringUTF8();
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
            VM.StackPush(oObject);
            VM.Call(896);
            return NWNCore.NativeFunctions.StackPopStringUTF8();
        }

        /// <summary>
        ///   Forces the given object to receive a new UUID, discarding the current value.
        /// </summary>
        public static void ForceRefreshObjectUUID(uint oObject)
        {
            VM.StackPush(oObject);
            VM.Call(897);
        }

        /// <summary>
        ///   Looks up a object on the server by it's UUID.
        ///   Returns OBJECT_INVALID if the UUID is not on the server.
        /// </summary>
        public static uint GetObjectByUUID(string sUUID)
        {
            VM.StackPush(sUUID);
            VM.Call(898);
            return VM.StackPopObject();
        }

        /// <summary>
        ///   Do not call. This does nothing on this platform except to return an error.
        /// </summary>
        public static void Reserved899()
        {
            VM.Call(899);
        }

        // Makes oPC load texture sNewName instead of sOldName.
        // If oPC is OBJECT_INVALID, it will apply the override to all active players
        // Setting sNewName to "" will clear the override and revert to original.
        // void SetTextureOverride();
        public static void SetTextureOverride(string OldName, string NewName = "", uint PC = OBJECT_INVALID)
        {
            VM.StackPush(PC);
            VM.StackPush(NewName);
            VM.StackPush(OldName);
            VM.Call(900);
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
            VM.StackPush(font);
            VM.StackPush(ID);
            VM.StackPush(RGBA2);
            VM.StackPush(RGBA);
            VM.StackPush(life);
            VM.StackPush((int)anchor);
            VM.StackPush(Y);
            VM.StackPush(X);
            VM.StackPush(Msg);
            VM.StackPush(PC);
            VM.Call(901);
        }

        // Returns oCreature's spell school specialization in nClass (SPELL_SCHOOL_* constants)
        // Unless custom content is used, only Wizards have spell schools
        // Returns -1 on error
        public static SpellSchool GetSpecialization(uint creature, ClassType playerClass)
        {
            VM.StackPush((int)playerClass);
            VM.StackPush(creature);
            VM.Call(902);
            return (SpellSchool)VM.StackPopInt();
        }

        // Returns oCreature's domain in nClass (DOMAIN_* constants)
        // nDomainIndex - 1 or 2
        // Unless custom content is used, only Clerics have domains
        // Returns -1 on error
        public static ClericDomain GetDomain(uint creature, int DomainIndex = 1, ClassType playerClass = ClassType.Cleric)
        {
            VM.StackPush((int)playerClass);
            VM.StackPush(creature);
            VM.Call(903);
            return (ClericDomain)VM.StackPopInt();
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
            VM.StackPush(bDecrementCharges ? 1 : 0);
            VM.StackPush(nSubPropertyIndex);
            VM.StackPush(oTarget);
            VM.StackPush((int)EngineStructure.ItemProperty, ip);
            VM.StackPush(oItem);
            VM.Call(910);
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
            VM.StackPush(bDecrementCharges ? 1 : 0);
            VM.StackPush(nSubPropertyIndex);
            VM.StackPush((int)EngineStructure.Location, lTarget);
            VM.StackPush((int)EngineStructure.ItemProperty, ip);
            VM.StackPush(oItem);
            VM.Call(911);
        }

        /// <summary>
        /// Makes oPC enter a targeting mode, letting them select an object as a target
        /// If a PC selects a target, it will trigger the module OnPlayerTarget event.
        /// </summary>
        public static void EnterTargetingMode(uint oPC, ObjectType nValidObjectTypes = ObjectType.All, MouseCursor nMouseCursorId = MouseCursor.Magic, MouseCursor nBadTargetCursor = MouseCursor.NoMagic)
        {
            VM.StackPush((int)nBadTargetCursor);
            VM.StackPush((int)nMouseCursorId);
            VM.StackPush((int)nValidObjectTypes);
            VM.StackPush(oPC);
            VM.Call(912);
        }

        /// <summary>
        /// Gets the target object in the module OnPlayerTarget event.
        /// Returns the area object when the target is the ground.
        /// </summary>
        public static uint GetTargetingModeSelectedObject()
        {
            VM.Call(913);
            return VM.StackPopObject();
        }

        /// <summary>
        /// Gets the target position in the module OnPlayerTarget event.
        /// </summary>
        public static Vector3 GetTargetingModeSelectedPosition()
        {
            VM.Call(914);
            return VM.StackPopVector();
        }

        /// <summary>
        /// Gets the player object that triggered the OnPlayerTarget event.
        /// </summary>
        public static uint GetLastPlayerToSelectTarget()
        {
            VM.Call(915);
            return VM.StackPopObject();
        }

        /// <summary>
        /// Sets oObject's hilite color to nColor
        /// The nColor format is 0xRRGGBB; -1 clears the color override.
        /// </summary>
        public static void SetObjectHiliteColor(uint oObject, int nColor = -1)
        {
            VM.StackPush(nColor);
            VM.StackPush(oObject);
            VM.Call(916);
        }

        /// <summary>
        /// Sets the cursor (MOUSECURSOR_*) to use when hovering over oObject
        /// </summary>
        public static void SetObjectMouseCursor(uint oObject, MouseCursor nCursor = MouseCursor.Invalid)
        {
            VM.StackPush((int)nCursor);
            VM.StackPush(oObject);
            VM.Call(917);
        }

        /// <summary>
        /// Overrides a given strref to always return sValue instead of what is in the TLK file.
        /// Setting sValue to "" will delete the override
        /// </summary>
        public static void SetTlkOverride(int nStrRef, string sValue = "")
        {
            VM.StackPush(sValue);
            VM.StackPush(nStrRef);
            VM.Call(953);
        }

        /// <summary>
        ///  Returns the column name of s2DA at nColumn index (starting at 0).
        /// Returns "" if column nColumn doesn't exist (at end).
        /// </summary>
        public static string Get2DAColumn(string s2DA, int nColumnIdx)
        {
            VM.StackPush(nColumnIdx);
            VM.StackPush(s2DA);
            VM.Call(1034);

            return VM.StackPopString();
        }

        /// <summary>
        /// Returns the number of defined rows in the 2da s2DA.
        /// </summary>
        public static int Get2DARowCount(string s2DA)
        {
            VM.StackPush(s2DA);
            VM.Call(1035);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Sets the spell targeting data manually for the player. This data is usually specified in spells.2da.
        /// This data persists through spell casts; you're overwriting the entry in spells.2da for this session.
        /// In multiplayer, these need to be reapplied when a player rejoins.
        /// - nSpell: SPELL_*
        /// - nShape: SPELL_TARGETING_SHAPE_*
        /// - nFlags: SPELL_TARGETING_FLAGS_*
        /// </summary>
        public static void SetSpellTargetingData(uint oPlayer, Spell nSpell, int nShape, float fSizeX, float fSizeY, int nFlags)
        {
            VM.StackPush(nFlags);
            VM.StackPush(fSizeY);
            VM.StackPush(fSizeX);
            VM.StackPush(nShape);
            VM.StackPush((int)nSpell);
            VM.StackPush(oPlayer);
            VM.Call(1041);
        }

        /// <summary>
        /// Sets the spell targeting data which is used for the next call to EnterTargetingMode() for this player.
        /// If the shape is set to SPELL_TARGETING_SHAPE_NONE and the range is provided, the dotted line range indicator will still appear.
        /// - nShape: SPELL_TARGETING_SHAPE_*
        /// - nFlags: SPELL_TARGETING_FLAGS_*
        /// - nSpell: SPELL_* (optional, passed to the shader but does nothing by default, you need to edit the shader to use it)
        /// - nFeat: FEAT_* (optional, passed to the shader but does nothing by default, you need to edit the shader to use it)
        /// </summary>
        public static void SetEnterTargetingModeData(
            uint oPlayer, 
            int nShape, 
            float fSizeX, 
            float fSizeY, 
            int nFlags, 
            float fRange = 0.0f, 
            Spell nSpell = Spell.AllSpells, 
            FeatType nFeat = FeatType.Invalid)
        {
            VM.StackPush((int)nFeat);
            VM.StackPush((int)nSpell);
            VM.StackPush(fRange);
            VM.StackPush(nFlags);
            VM.StackPush(fSizeY);
            VM.StackPush(fSizeX);
            VM.StackPush(nShape);
            VM.StackPush(oPlayer);
            VM.Call(1042);
        }
        /// <summary>
        /// Gets the number of memorized spell slots for a given spell level.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellLevel: the spell level, 0-9.
        /// Returns: the number of spell slots.
        /// </summary>
        public static int GetMemorizedSpellCountByLevel(uint oCreature, ClassType nClassType, int nSpellLevel)
        {
            VM.StackPush(nSpellLevel);
            VM.StackPush((int)nClassType);
            VM.StackPush(oCreature);
            VM.Call(1043);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Gets the spell id of a memorized spell slot.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellLevel: the spell level, 0-9.
        /// - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()
        /// Returns: a SPELL_* constant or -1 if the slot is not set.
        /// </summary>
        public static int GetMemorizedSpellId(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            VM.StackPush(nIndex);
            VM.StackPush(nSpellLevel);
            VM.StackPush((int)nClassType);
            VM.StackPush(oCreature);
            VM.Call(1044);

            return VM.StackPopInt();
        }
        /// <summary>
        /// Gets the ready state of a memorized spell slot.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellLevel: the spell level, 0-9.
        /// - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()
        /// Returns: TRUE/FALSE or -1 if the slot is not set.
        /// </summary>
        public static int GetMemorizedSpellReady(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            VM.StackPush(nIndex);
            VM.StackPush(nSpellLevel);
            VM.StackPush((int)nClassType);
            VM.StackPush(oCreature);
            VM.Call(1045);

            return VM.StackPopInt();
        }
        /// <summary>
        /// Gets the metamagic of a memorized spell slot.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellLevel: the spell level, 0-9.
        /// - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()
        /// Returns: a METAMAGIC_* constant or -1 if the slot is not set.
        /// </summary>
        public static int GetMemorizedSpellMetaMagic(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            VM.StackPush(nIndex);
            VM.StackPush(nSpellLevel);
            VM.StackPush((int)nClassType);
            VM.StackPush(oCreature);
            VM.Call(1046);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Gets if the memorized spell slot has a domain spell.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellLevel: the spell level, 0-9.
        /// - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()
        /// Returns: TRUE/FALSE or -1 if the slot is not set.
        /// </summary>
        public static int GetMemorizedSpellIsDomainSpell(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            VM.StackPush(nIndex);
            VM.StackPush(nSpellLevel);
            VM.StackPush((int)nClassType);
            VM.StackPush(oCreature);
            VM.Call(1047);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Set a memorized spell slot.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellLevel: the spell level, 0-9.
        /// - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()
        /// - nSpellId: a SPELL_* constant.
        /// - bReady: TRUE to mark the slot ready.
        /// - nMetaMagic: a METAMAGIC_* constant.
        /// - bIsDomainSpell: TRUE for a domain spell.
        /// </summary>
        public static void SetMemorizedSpell(
            uint oCreature, 
            ClassType nClassType, 
            int nSpellLevel, 
            int nIndex, 
            Spell nSpellId, 
            bool bReady = true, 
            MetaMagic nMetaMagic = MetaMagic.None, 
            bool bIsDomainSpell = false)
        {
            VM.StackPush(bIsDomainSpell ? 1 : 0);
            VM.StackPush((int)nMetaMagic);
            VM.StackPush(bReady ? 1 : 0);
            VM.StackPush((int)nSpellId);
            VM.StackPush(nIndex);
            VM.StackPush(nSpellLevel);
            VM.StackPush((int)nClassType);
            VM.StackPush(oCreature);
            VM.Call(1048);
        }

        /// <summary>
        /// Set the ready state of a memorized spell slot.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellLevel: the spell level, 0-9.
        /// - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()
        /// - bReady: TRUE to mark the slot ready.
        /// </summary>
        public static void SetMemorizedSpellReady(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex, bool bReady)
        {
            VM.StackPush(bReady ? 1 : 0);
            VM.StackPush(nIndex);
            VM.StackPush(nSpellLevel);
            VM.StackPush((int)nClassType);
            VM.StackPush(oCreature);
            VM.Call(1049);
        }

        /// <summary>
        /// Clear a specific memorized spell slot.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellLevel: the spell level, 0-9.
        /// - nIndex: the index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()
        /// </summary>
        public static void ClearMemorizedSpell(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            VM.StackPush(nIndex);
            VM.StackPush(nSpellLevel);
            VM.StackPush((int)nClassType);
            VM.StackPush(oCreature);
            VM.Call(1050);
        }

        /// <summary>
        /// Clear all memorized spell slots of a specific spell id, including metamagic'd ones.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a MemorizesSpells class.
        /// - nSpellId: a SPELL_* constant.
        /// </summary>
        public static void ClearMemorizedSpellBySpellId(uint oCreature, ClassType nClassType, int nSpellId)
        {
            VM.StackPush(nSpellId);
            VM.StackPush((int)nClassType);
            VM.StackPush(oCreature);
            VM.Call(1051);
        }

        /// <summary>
        ///  Gets the number of known spells for a given spell level.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a SpellBookRestricted class.
        /// - nSpellLevel: the spell level, 0-9.
        /// Returns: the number of known spells.
        /// </summary>
        public static int GetKnownSpellCount(uint oCreature, ClassType nClassType, int nSpellLevel)
        {
            VM.StackPush(nSpellLevel);
            VM.StackPush((int)nClassType);
            VM.StackPush(oCreature);
            VM.Call(1052);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Gets the spell id of a known spell.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a SpellBookRestricted class.
        /// - nSpellLevel: the spell level, 0-9.
        /// - nIndex: the index of the known spell. Bounds: 0 <= nIndex < GetKnownSpellCount()
        /// Returns: a SPELL_* constant or -1 on error.
        /// </summary>
        public static int GetKnownSpellId(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex)
        {
            VM.StackPush(nIndex);
            VM.StackPush(nSpellLevel);
            VM.StackPush((int)nClassType);
            VM.StackPush(oCreature);
            VM.Call(1053);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Gets if a spell is in the known spell list.
        /// - nClassType: a CLASS_TYPE_* constant. Must be a SpellBookRestricted class.
        /// - nSpellId: a SPELL_* constant.
        /// Returns: TRUE if the spell is in the known spell list.
        /// </summary>
        public static bool GetIsInKnownSpellList(uint oCreature, ClassType nClassType, Spell nSpellId)
        {
            VM.StackPush((int)nSpellId);
            VM.StackPush((int)nClassType);
            VM.StackPush(oCreature);
            VM.Call(1054);

            return VM.StackPopInt() == 1;
        }

        /// <summary>
        /// Gets the amount of uses a spell has left.
        /// - nClassType: a CLASS_TYPE_* constant.
        /// - nSpellid: a SPELL_* constant.
        /// - nMetaMagic: a METAMAGIC_* constant.
        /// - nDomainLevel: the domain level, if a domain spell.
        /// Returns: the amount of spell uses left.
        /// </summary>
        public static int GetSpellUsesLeft(
            uint oCreature, 
            ClassType nClassType, 
            Spell nSpellId, 
            MetaMagic nMetaMagic = MetaMagic.None, 
            int nDomainLevel = 0)
        {
            VM.StackPush(nDomainLevel);
            VM.StackPush((int)nMetaMagic);
            VM.StackPush((int)nSpellId);
            VM.StackPush((int)nClassType);
            VM.StackPush(oCreature);
            VM.Call(1055);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Gets the spell level at which a class gets a spell.
        /// - nClassType: a CLASS_TYPE_* constant.
        /// - nSpellId: a SPELL_* constant.
        /// Returns: the spell level or -1 if the class does not get the spell.
        /// </summary>
        public static int GetSpellLevelByClass(ClassType nClassType, Spell nSpellId)
        {
            VM.StackPush((int)nSpellId);
            VM.StackPush((int)nClassType);
            VM.Call(1056);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Replaces oObject's animation sOld with sNew.
        /// Specifying sNew = "" will restore the original animation.
        /// </summary>
        public static void ReplaceObjectAnimation(uint oObject, string sOld, string sNew = "")
        {
            VM.StackPush(sNew);
            VM.StackPush(sOld);
            VM.StackPush(oObject);
            VM.Call(1057);
        }

        /// <summary>
        /// Sets the distance (in meters) at which oObject info will be sent to clients (default 45.0)
        /// This is still subject to other limitations, such as perception ranges for creatures
        /// Note: Increasing visibility ranges of many objects can have a severe negative effect on
        ///       network latency and server performance, and rendering additional objects will
        ///       impact graphics performance of clients. Use cautiously.
        /// </summary>
        public static void SetObjectVisibleDistance(uint oObject, float fDistance = 45.0f)
        {
            VM.StackPush(fDistance);
            VM.StackPush(oObject);
            VM.Call(1058);
        }

        /// <summary>
        /// Gets oObject's visible distance, as set by SetObjectVisibleDistance()
        /// Returns -1.0f on error
        /// </summary>
        public static float GetObjectVisibleDistance(uint oObject)
        {
            VM.StackPush(oObject);
            VM.Call(1059);

            return VM.StackPopFloat();
        }

        /// <summary>
        /// Sets the active game pause state - same as if the player requested pause.
        /// </summary>
        public static void SetGameActivePause(bool bState)
        {
            VM.StackPush(bState ? 1 : 0);
            VM.Call(1060);
        }

        /// <summary>
        /// Returns >0 if the game is currently paused:
        /// - 0: Game is not paused.
        /// - 1: Timestop
        /// - 2: Active Player Pause (optionally on top of timestop)
        /// </summary>
        public static int GetGamePauseState()
        {
            VM.Call(1061);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Set the gender of oCreature.
        /// - nGender: a GENDER_* constant.
        /// </summary>
        public static void SetGender(uint oCreature, Gender nGender)
        {
            VM.StackPush((int)nGender);
            VM.StackPush(oCreature);
            VM.Call(1062);
        }

        /// <summary>
        /// Get the soundset of oCreature.
        /// Returns -1 on error.
        /// </summary>
        public static int GetSoundset(uint oCreature)
        {
            VM.StackPush(oCreature);
            VM.Call(1063);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Set the soundset of oCreature, see soundset.2da for possible values.
        /// </summary>
        public static void SetSoundset(uint oCreature, int nSoundset)
        {
            VM.StackPush(nSoundset);
            VM.StackPush(oCreature);
            VM.Call(1064);
        }

        /// <summary>
        /// Ready a spell level for oCreature.
        /// - nSpellLevel: 0-9
        /// - nClassType: a CLASS_TYPE_* constant or CLASS_TYPE_INVALID to ready the spell level for all classes.
        /// </summary>
        public static void ReadySpellLevel(uint oCreature, int nSpellLevel, ClassType nClassType = ClassType.Invalid)
        {
            VM.StackPush((int)nClassType);
            VM.StackPush(nSpellLevel);
            VM.StackPush(oCreature);
            VM.Call(1065);
        }

        /// <summary>
        /// Makes oCreature controllable by oPlayer, if player party control is enabled
        /// Setting oPlayer=OBJECT_INVALID removes the override and reverts to regular party control behavior
        /// NB: A creature is only controllable by one player, so if you set oPlayer to a non-Player object
        ///    (e.g. the module) it will disable regular party control for this creature
        /// </summary>
        public static void SetCommandingPlayer(uint oCreature, uint oPlayer)
        {
            VM.StackPush(oPlayer);
            VM.StackPush(oCreature);
            VM.Call(1066);
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
            VM.StackPush(fMaxDist);
            VM.StackPush(fMinDist);
            VM.StackPush(fMaxPitch);
            VM.StackPush(fMinPitch);
            VM.StackPush(oPlayer);
            VM.Call(1067);
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
            VM.StackPush(bFindClearView ? 1 : 0);
            VM.StackPush(oTarget);
            VM.StackPush(oPlayer);
            VM.Call(1073);
        }

        /// <summary>
        /// Get the current discoverability mask of oObject.
        /// Returns -1 if oObject cannot have a discovery mask.
        /// </summary>
        public static int GetObjectUiDiscoveryMask(uint oObject)
        {
            VM.StackPush(oObject);
            VM.Call(1074);

            return VM.StackPopInt();
        }

        /// <summary>
        /// Sets the discoverability mask on oObject.
        /// This allows toggling areahilite (TAB key by default) and mouseover discovery in the area view.
        /// * nMask is a mask of OBJECT_UI_DISCOVERY_MODE_*
        /// Will currently only work on Creatures, Doors (Hilite only), Items and Useable Placeables.
        /// Does not affect inventory items.
        /// </summary>
        public static void SetObjectUiDiscoveryMask(uint oObject, ObjectUIDiscoveryType nMask = ObjectUIDiscoveryType.Default)
        {
            VM.StackPush((int)nMask);
            VM.StackPush(oObject);
            VM.Call(1075);
        }

        /// <summary>
        /// Sets a text override for the mouseover/tab-highlight text bubble of oObject.
        /// Will currently only work on Creatures, Items and Useable Placeables.
        /// * nMode is one of OBJECT_UI_TEXT_BUBBLE_OVERRIDE_*.
        /// </summary>
        public static void SetObjectTextBubbleOverride(uint oObject, ObjectUITextBubbleOverrideType nMode, string sText)
        {
            VM.StackPush(sText);
            VM.StackPush((int)nMode);
            VM.StackPush(oObject);
            VM.Call(1076);
        }

        /// <summary>
        /// Immediately unsets a VTs for the given object, with no lerp.
        /// * nScope: one of OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_, or -1 for all scopes
        /// Returns TRUE only if transforms were successfully removed (valid object, transforms existed).
        /// </summary>
        public static bool ClearObjectVisualTransform(uint oObject, ObjectVisualTransformDataScopeType nScope = ObjectVisualTransformDataScopeType.Invalid)
        {
            VM.StackPush((int)nScope);
            VM.StackPush(oObject);
            VM.Call(1077);

            return VM.StackPopInt() == 1;
        }

        /// <summary>
        /// Gets an optional vector of specific gui events in the module OnPlayerGuiEvent event.
        /// GUIEVENT_RADIAL_OPEN - World vector position of radial if on tile.
        /// </summary>
        /// <returns></returns>
        public static Vector3 GetLastGuiEventVector()
        {
            VM.Call(1078);

            return VM.StackPopVector();
        }

        /// <summary>
        /// Sets oPlayer's camera settings that override any client configuration settings
        /// nFlags is a bitmask of CAMERA_FLAG_* constants;
        /// NB: Like all other camera settings, this is not saved when saving the game
        /// </summary>
        public static void SetCameraFlags(uint oPlayer, int nFlags = 0)
        {
            VM.StackPush(nFlags);
            VM.StackPush(oPlayer);
            VM.Call(1079);
        }

        /// <summary>
        /// In the spell script returns the feat used, or -1 if no feat was used
        /// </summary>
        public static int GetSpellFeatId()
        {
            VM.Call(1095);
            return VM.StackPopInt();
        }

        /// <summary>
        /// If oCreature has nFeat, and nFeat is useable, returns the number of remaining uses left
        /// or the maximum int value if the feat has unlimited uses (eg FEAT_KNOCKDOWN)
        /// - nFeat: FEAT_*
        /// - oCreature: Creature to check the feat of
        /// </summary>
        public static int GetFeatRemainingUses(FeatType nFeat, uint oCreature)
        {
            VM.StackPush(oCreature);
            VM.StackPush((int)nFeat);
            VM.Call(1097);
            return VM.StackPopInt();
        }

        /// <summary>
        /// Change a tile in an area, it will also update the tile for all players in the area.
        /// * Notes:
        ///   - For optimal use you should be familiar with how tilesets / .set files work.
        ///   - Will not update the height of non-creature objects.
        ///   - Creatures may get stuck on non-walkable terrain.
        ///
        /// - locTile: The location of the tile.
        /// - nTileID: the ID of the tile, for values see the .set file of the tileset.
        /// - nOrientation: the orientation of the tile, 0-3.
        ///                 0 = Normal orientation
        ///                 1 = 90 degrees counterclockwise
        ///                 2 = 180 degrees counterclockwise
        ///                 3 = 270 degrees counterclockwise
        /// - nHeight: the height of the tile.
        /// - nFlags: a bitmask of SETTILE_FLAG_* constants.
        ///           - SETTILE_FLAG_RELOAD_GRASS: reloads the area's grass, use if your tile used to have grass or should have grass now.
        ///           - SETTILE_FLAG_RELOAD_BORDER: reloads the edge tile border, use if you changed a tile on the edge of the area.
        ///           - SETTILE_FLAG_RECOMPUTE_LIGHTING: recomputes the area's lighting and static shadows, use most of time.
        /// </summary>
        public static void SetTile(
            Location locTile, 
            int nTileID, 
            int nOrientation, 
            int nHeight = 0, 
            SetTileFlagType nFlags = SetTileFlagType.RecomputeLighting)
        {
            VM.StackPush((int)nFlags);
            VM.StackPush(nHeight);
            VM.StackPush(nOrientation);
            VM.StackPush(nTileID);
            VM.StackPush(locTile);
            VM.Call(1098);
        }

        /// <summary>
        ///  Get the ID of the tile at location locTile.
        /// Returns -1 on error.
        /// </summary>
        public static int GetTileID(Location locTile)
        {
            VM.StackPush(locTile);
            VM.Call(1099);
            return VM.StackPopInt();
        }

        /// <summary>
        /// Get the orientation of the tile at location locTile.
        /// Returns -1 on error.
        /// </summary>
        public static int GetTileOrientation(Location locTile)
        {
            VM.StackPush(locTile);
            VM.Call(1100);
            return VM.StackPopInt();
        }

        /// <summary>
        /// Get the height of the tile at location locTile.
        /// Returns -1 on error.
        /// </summary>
        public static int GetTileHeight(Location locTile)
        {
            VM.StackPush(locTile);
            VM.Call(1101);
            return VM.StackPopInt();
        }

        /// <summary>
        /// All clients in oArea will reload the area's grass.
        /// This can be used to update the grass of an area after changing a tile with SetTile() that will have or used to have grass.
        /// </summary>
        public static void ReloadAreaGrass(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(1102);
        }

        /// <summary>
        /// Set the state of the tile animation loops of the tile at location locTile.
        /// </summary>
        public static void SetTileAnimationLoops(Location locTile, bool bAnimLoop1, bool bAnimLoop2, bool bAnimLoop3)
        {
            VM.StackPush(bAnimLoop3 ? 1 : 0);
            VM.StackPush(bAnimLoop2 ? 1 : 0);
            VM.StackPush(bAnimLoop1 ? 1 : 0);
            VM.StackPush(locTile);
            VM.Call(1103);
        }

        /// <summary>
        /// Change multiple tiles in an area, it will also update the tiles for all players in the area.
        /// Note: See SetTile() for additional information.
        /// - oArea: the area to change one or more tiles of.
        /// - jTileData: a JsonArray() with one or more JsonObject()s with the following keys:
        ///               - index: the index of the tile as a JsonInt()
        ///                        For example, a 3x3 area has the following tile indexes:
        ///                        6 7 8
        ///                        3 4 5
        ///                        0 1 2
        ///               - tileid: the ID of the tile as a JsonInt(), defaults to 0 if not set
        ///               - orientation: the orientation of the tile as JsonInt(), defaults to 0 if not set
        ///               - height: the height of the tile as JsonInt(), defaults to 0 if not set
        ///               - animloop1: the state of a tile animation, 1/0 as JsonInt(), defaults to the current value if not set
        ///               - animloop2: the state of a tile animation, 1/0 as JsonInt(), defaults to the current value if not set
        ///               - animloop3: the state of a tile animation, 1/0 as JsonInt(), defaults to the current value if not set
        /// - nFlags: a bitmask of SETTILE_FLAG_* constants.
        /// - sTileset: if not empty, it will also change the area's tileset
        ///             Warning: only use this if you really know what you're doing, it's very easy to break things badly.
        ///                      Make sure jTileData changes *all* tiles in the area and to a tile id that's supported by sTileset.
        /// </summary>
        public static void SetTileJson(
            uint oArea, 
            Json jTileData, 
            SetTileFlagType nFlags = SetTileFlagType.RecomputeLighting, 
            string sTileset = "")
        {
            VM.StackPush(sTileset);
            VM.StackPush((int)nFlags);
            VM.StackPush(jTileData);
            VM.StackPush(oArea);
            VM.Call(1104);
        }

        /// <summary>
        /// All clients in oArea will reload the inaccesible border tiles.
        /// This can be used to update the edge tiles after changing a tile with SetTile().
        /// </summary>
        public static void ReloadAreaBorder(uint oArea)
        {
            VM.StackPush(oArea);
            VM.Call(1105);
        }

        /// <summary>
        /// Sets whether or not oCreatures's nIconId is flashing in their GUI icon bar.  If oCreature does not
        /// have an icon associated with nIconId, nothing happens. This function does not add icons to 
        /// oCreatures's GUI icon bar. The icon will flash until the underlying effect is removed or this 
        /// function is called again with bFlashing = FALSE.
        /// - oCreature: Player object to affect
        /// - nIconId: Referenced to effecticons.2da or EFFECT_ICON_*
        /// - bFlashing: TRUE to force an existing icon to flash, FALSE to to stop.
        /// </summary>
        public static void SetEffectIconFlashing(uint oCreature, int nIconId, bool bFlashing = true)
        {
            VM.StackPush(bFlashing ? 1 : 0);
            VM.StackPush(nIconId);
            VM.StackPush(oCreature);
            VM.Call(1106);
        }

        /// <summary>
        /// Returns the INVENTORY_SLOT_* constant of the last item equipped.  Can only be used in the
        /// module's OnPlayerEquip event.  Returns -1 on error.
        /// </summary>
        public static InventorySlot GetPCItemLastEquippedSlot()
        {
            VM.Call(1108);
            return (InventorySlot)VM.StackPopInt();
        }

        /// <summary>
        /// Returns the INVENTORY_SLOT_* constant of the last item unequipped.  Can only be used in the
        /// module's OnPlayerUnequip event.  Returns -1 on error.
        /// </summary>
        public static InventorySlot GetPCItemLastUnequippedSlot()
        {
            VM.Call(1109);
            return (InventorySlot)VM.StackPopInt();
        }

        /// <summary>
        /// Returns TRUE if the last spell was cast spontaneously
        /// eg; a Cleric casting SPELL_CURE_LIGHT_WOUNDS when it is not prepared, using another level 1 slot
        /// </summary>
        public static bool GetSpellCastSpontaneously()
        {
            VM.Call(1110);
            return VM.StackPopInt() == 1;
        }

        /// <summary>
        /// Return the current game tick rate (mainloop iterations per second).
        /// This is equivalent to graphics frames per second when the module is running inside a client.
        /// </summary>
        public static int GetTickRate()
        {
            VM.Call(1113);
            return VM.StackPopInt();
        }

        /// <summary>
        /// Returns the level of the last spell cast. This value is only valid in a Spell script.
        /// </summary>
        public static int GetLastSpellLevel()
        {
            VM.Call(1114);
            return VM.StackPopInt();
        }

        /// <summary>
        /// Returns the 32bit integer hash of sString
        /// This hash is stable and will always have the same value for same input string, regardless of platform.
        /// The hash algorithm is the same as the one used internally for strings in case statements, so you can do:
        ///    switch (HashString(sString))
        ///    {
        ///         case "AAA":    HandleAAA(); break;
        ///         case "BBB":    HandleBBB(); break;
        ///    }
        /// NOTE: The exact algorithm used is XXH32(sString) ^ XXH32(""). This means that HashString("") is 0.
        /// </summary>
        public static int HashString(string sString)
        {
            VM.StackPush(sString);
            VM.Call(1115);
            return VM.StackPopInt();
        }

        public static int GetMicrosecondCounter()
        {
            VM.Call(1116);
            return VM.StackPopInt();
        }
    }
}