using System.Numerics;
using SWLOR.NWN.API.Core.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Sets a new tag for oObject.
        ///   Will do nothing for invalid objects or the module object.
        ///   Note: Care needs to be taken with this function.
        ///   Changing the tag for creature with waypoints will make them stop walking them.
        ///   Changing waypoint, door or trigger tags will break their area transitions.
        /// </summary>
        public static void SetTag(uint oObject, string sNewTag)
        {
            global::NWN.Core.NWScript.SetTag(oObject, sNewTag);
        }

        /// <summary>
        ///   Get the last object that default clicked (left clicked) on the placeable object
        ///   that is calling this function.
        ///   Should only be called from a placeables OnClick event.
        ///   * Returns OBJECT_INVALID if it is called by something other than a placeable.
        /// </summary>
        public static uint GetPlaceableLastClickedBy()
        {
            return global::NWN.Core.NWScript.GetPlaceableLastClickedBy();
        }

        /// <summary>
        ///   Set the name of oObject.
        ///   - oObject: the object for which you are changing the name (a creature, placeable, item, or door).
        ///   - sNewName: the new name that the object will use.
        ///   Note: SetName() does not work on player objects.
        ///   Setting an object's name to "" will make the object
        ///   revert to using the name it had originally before any
        ///   SetName() calls were made on the object.
        /// </summary>
        public static void SetName(uint oObject, string sNewName = "")
        {
            global::NWN.Core.NWScript.SetName(oObject, sNewName);
        }

        /// <summary>
        ///   Get the PortraitId of oTarget.
        ///   - oTarget: the object for which you are getting the portrait Id.
        ///   Returns: The Portrait Id number being used for the object oTarget.
        ///   The Portrait Id refers to the row number of the Portraits.2da
        ///   that this portrait is from.
        ///   If a custom portrait is being used, oTarget is a player object,
        ///   or on an error returns PORTRAIT_INVALID. In these instances
        ///   try using GetPortraitResRef() instead.
        /// </summary>
        public static int GetPortraitId(uint oTarget = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetPortraitId(oTarget);
        }

        /// <summary>
        ///   Change the portrait of oTarget to use the Portrait Id specified.
        ///   - oTarget: the object for which you are changing the portrait.
        ///   - nPortraitId: The Id of the new portrait to use.
        ///   nPortraitId refers to a row in the Portraits.2da
        ///   Note: Not all portrait Ids are suitable for use with all object types.
        ///   Setting the portrait Id will also cause the portrait ResRef
        ///   to be set to the appropriate portrait ResRef for the Id specified.
        /// </summary>
        public static void SetPortraitId(uint oTarget, int nPortraitId)
        {
            global::NWN.Core.NWScript.SetPortraitId(oTarget, nPortraitId);
        }

        /// <summary>
        ///   Get the Portrait ResRef of oTarget.
        ///   - oTarget: the object for which you are getting the portrait ResRef.
        ///   Returns: The Portrait ResRef being used for the object oTarget.
        ///   The Portrait ResRef will not include a trailing size letter.
        /// </summary>
        public static string GetPortraitResRef(uint oTarget = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetPortraitResRef(oTarget);
        }

        /// <summary>
        ///   Change the portrait of oTarget to use the Portrait ResRef specified.
        ///   - oTarget: the object for which you are changing the portrait.
        ///   - sPortraitResRef: The ResRef of the new portrait to use.
        ///   The ResRef should not include any trailing size letter ( e.g. po_el_f_09_ ).
        ///   Note: Not all portrait ResRefs are suitable for use with all object types.
        ///   Setting the portrait ResRef will also cause the portrait Id
        ///   to be set to PORTRAIT_INVALID.
        /// </summary>
        public static void SetPortraitResRef(uint oTarget, string sPortraitResRef)
        {
            global::NWN.Core.NWScript.SetPortraitResRef(oTarget, sPortraitResRef);
        }

        /// <summary>
        /// Set oTarget's useable object status.
        /// Note: Only works on non-static placeables, creatures, doors and items.
        /// On items, it affects interactivity when they're on the ground, and not useability in inventory.
        /// </summary>
        public static void SetUseableFlag(uint oPlaceable, bool nUseable)
        {
            global::NWN.Core.NWScript.SetUseableFlag(oPlaceable, nUseable ? 1 : 0);
        }

        /// <summary>
        ///   Get the description of oObject.
        ///   - oObject: the object from which you are obtaining the description.
        ///   Can be a creature, item, placeable, door, trigger or module object.
        ///   - bOriginalDescription:  if set to true any new description specified via a SetDescription scripting command
        ///   is ignored and the original object's description is returned instead.
        ///   - bIdentified: If oObject is an item, setting this to TRUE will return the identified description,
        ///   setting this to FALSE will return the unidentified description. This flag has no
        ///   effect on objects other than items.
        /// </summary>
        public static string GetDescription(uint oObject, bool bOriginalDescription = false,
            bool bIdentifiedDescription = true)
        {
            return global::NWN.Core.NWScript.GetDescription(oObject, bOriginalDescription ? 1 : 0, bIdentifiedDescription ? 1 : 0);
        }

        /// <summary>
        ///   Set the description of oObject.
        ///   - oObject: the object for which you are changing the description
        ///   Can be a creature, placeable, item, door, or trigger.
        ///   - sNewDescription: the new description that the object will use.
        ///   - bIdentified: If oObject is an item, setting this to TRUE will set the identified description,
        ///   setting this to FALSE will set the unidentified description. This flag has no
        ///   effect on objects other than items.
        ///   Note: Setting an object's description to "" will make the object
        ///   revert to using the description it originally had before any
        ///   SetDescription() calls were made on the object.
        /// </summary>
        public static void SetDescription(uint oObject, string sNewDescription = "", bool bIdentifiedDescription = true)
        {
            global::NWN.Core.NWScript.SetDescription(oObject, sNewDescription, bIdentifiedDescription ? 1 : 0);
        }

        /// <summary>
        ///   Get the Color of oObject from the color channel specified.
        ///   - oObject: the object from which you are obtaining the color.
        ///   Can be a creature that has color information (i.e. the playable races).
        ///   - nColorChannel: The color channel that you want to get the color value of.
        ///   COLOR_CHANNEL_SKIN
        ///   COLOR_CHANNEL_HAIR
        ///   COLOR_CHANNEL_TATTOO_1
        ///   COLOR_CHANNEL_TATTOO_2
        ///   * Returns -1 on error.
        /// </summary>
        public static int GetColor(uint oObject, ColorChannel nColorChannel)
        {
            return global::NWN.Core.NWScript.GetColor(oObject, (int)nColorChannel);
        }

        /// <summary>
        ///   Set the color channel of oObject to the color specified.
        ///   - oObject: the object for which you are changing the color.
        ///   Can be a creature that has color information (i.e. the playable races).
        ///   - nColorChannel: The color channel that you want to set the color value of.
        ///   COLOR_CHANNEL_SKIN
        ///   COLOR_CHANNEL_HAIR
        ///   COLOR_CHANNEL_TATTOO_1
        ///   COLOR_CHANNEL_TATTOO_2
        ///   - nColorValue: The color you want to set (0-175).
        /// </summary>
        public static void SetColor(uint oObject, ColorChannel nColorChannel, int nColorValue)
        {
            global::NWN.Core.NWScript.SetColor(oObject, (int)nColorChannel, nColorValue);
        }

        /// <summary>
        ///   Get the feedback message that will be displayed when trying to unlock the object oObject.
        ///   - oObject: a door or placeable.
        ///   Returns an empty string "" on an error or if the game's default feedback message is being used
        /// </summary>
        public static string GetKeyRequiredFeedback(uint oObject)
        {
            return global::NWN.Core.NWScript.GetKeyRequiredFeedback(oObject);
        }

        /// <summary>
        ///   Set the feedback message that is displayed when trying to unlock the object oObject.
        ///   This will only have an effect if the object is set to
        ///   "Key required to unlock or lock" either in the toolset
        ///   or by using the scripting command SetLockKeyRequired().
        ///   - oObject: a door or placeable.
        ///   - sFeedbackMessage: the string to be displayed in the player's text window.
        ///   to use the game's default message, set sFeedbackMessage to ""
        /// </summary>
        public static void SetKeyRequiredFeedback(uint oObject, string sFeedbackMessage)
        {
            global::NWN.Core.NWScript.SetKeyRequiredFeedback(oObject, sFeedbackMessage);
        }

        /// <summary>
        ///   Locks the player's camera pitch to its current pitch setting,
        ///   or unlocks the player's camera pitch.
        ///   Stops the player from tilting their camera angle.
        ///   - oPlayer: A player object.
        ///   - bLocked: TRUE/FALSE.
        /// </summary>
        public static void LockCameraPitch(uint oPlayer, bool bLocked = true)
        {
            global::NWN.Core.NWScript.LockCameraPitch(oPlayer, bLocked ? 1 : 0);
        }

        /// <summary>
        ///   Locks the player's camera distance to its current distance setting,
        ///   or unlocks the player's camera distance.
        ///   Stops the player from being able to zoom in/out the camera.
        ///   - oPlayer: A player object.
        ///   - bLocked: TRUE/FALSE.
        /// </summary>
        public static void LockCameraDistance(uint oPlayer, bool bLocked = true)
        {
            global::NWN.Core.NWScript.LockCameraDistance(oPlayer, bLocked ? 1 : 0);
        }

        /// <summary>
        ///   Locks the player's camera direction to its current direction,
        ///   or unlocks the player's camera direction to enable it to move
        ///   freely again.
        ///   Stops the player from being able to rotate the camera direction.
        ///   - oPlayer: A player object.
        ///   - bLocked: TRUE/FALSE.
        /// </summary>
        public static void LockCameraDirection(uint oPlayer, bool bLocked = true)
        {
            global::NWN.Core.NWScript.LockCameraDirection(oPlayer, bLocked ? 1 : 0);
        }

        /// <summary>
        ///   returns the Hardness of a Door or Placeable object.
        ///   - oObject: a door or placeable object.
        ///   returns -1 on an error or if used on an object that is
        ///   neither a door nor a placeable object.
        /// </summary>
        public static int GetHardness(uint oObject = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetHardness(oObject);
        }

        /// <summary>
        ///   Sets the Hardness of a Door or Placeable object.
        ///   - nHardness: must be between 0 and 250.
        ///   - oObject: a door or placeable object.
        ///   Does nothing if used on an object that is neither
        ///   a door nor a placeable.
        /// </summary>
        public static void SetHardness(int nHardness, uint oObject = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetHardness(nHardness, oObject);
        }

        /// <summary>
        ///   When set the object can not be opened unless the
        ///   opener possesses the required key. The key tag required
        ///   can be specified either in the toolset, or by using
        ///   the SetLockKeyTag() scripting command.
        ///   - oObject: a door, or placeable.
        ///   - nKeyRequired: TRUE/FALSE
        /// </summary>
        public static void SetLockKeyRequired(uint oObject, bool nKeyRequired = true)
        {
            global::NWN.Core.NWScript.SetLockKeyRequired(oObject, nKeyRequired ? 1 : 0);
        }

        /// <summary>
        ///   Set the key tag required to open object oObject.
        ///   This will only have an effect if the object is set to
        ///   "Key required to unlock or lock" either in the toolset
        ///   or by using the scripting command SetLockKeyRequired().
        ///   - oObject: a door, placeable or trigger.
        ///   - sNewKeyTag: the key tag required to open the locked object.
        /// </summary>
        public static void SetLockKeyTag(uint oObject, string sNewKeyTag)
        {
            global::NWN.Core.NWScript.SetLockKeyTag(oObject, sNewKeyTag);
        }

        /// <summary>
        ///   Sets whether or not the object can be locked.
        ///   - oObject: a door or placeable.
        ///   - nLockable: TRUE/FALSE
        /// </summary>
        public static void SetLockLockable(uint oObject, bool nLockable = true)
        {
            global::NWN.Core.NWScript.SetLockLockable(oObject, nLockable ? 1 : 0);
        }

        /// <summary>
        ///   Sets the DC for unlocking the object.
        ///   - oObject: a door or placeable object.
        ///   - nNewUnlockDC: must be between 0 and 250.
        /// </summary>
        public static void SetLockUnlockDC(uint oObject, int nNewUnlockDC)
        {
            global::NWN.Core.NWScript.SetLockUnlockDC(oObject, nNewUnlockDC);
        }

        /// <summary>
        ///   Sets the DC for locking the object.
        ///   - oObject: a door or placeable object.
        ///   - nNewLockDC: must be between 0 and 250.
        /// </summary>
        public static void SetLockLockDC(uint oObject, int nNewLockDC)
        {
            global::NWN.Core.NWScript.SetLockLockDC(oObject, nNewLockDC);
        }

        /// <summary>
        ///   Set the Will saving throw value of the Door or Placeable object oObject.
        ///   - oObject: a door or placeable object.
        ///   - nWillSave: must be between 0 and 250.
        /// </summary>
        public static void SetWillSavingThrow(uint oObject, int nWillSave)
        {
            global::NWN.Core.NWScript.SetWillSavingThrow(oObject, nWillSave);
        }

        /// <summary>
        ///   Set the Reflex saving throw value of the Door or Placeable object oObject.
        ///   - oObject: a door or placeable object.
        ///   - nReflexSave: must be between 0 and 250.
        /// </summary>
        public static void SetReflexSavingThrow(uint oObject, int nReflexSave)
        {
            global::NWN.Core.NWScript.SetReflexSavingThrow(oObject, nReflexSave);
        }

        /// <summary>
        ///   Set the Fortitude saving throw value of the Door or Placeable object oObject.
        ///   - oObject: a door or placeable object.
        ///   - nFortitudeSave: must be between 0 and 250.
        /// </summary>
        public static void SetFortitudeSavingThrow(uint oObject, int nFortitudeSave)
        {
            global::NWN.Core.NWScript.SetFortitudeSavingThrow(oObject, nFortitudeSave);
        }

        /// <summary>
        ///   Gets the weight of an item, or the total carried weight of a creature in tenths
        ///   of pounds (as per the baseitems.2da).
        ///   - oTarget: the item or creature for which the weight is needed
        /// </summary>
        public static int GetWeight(uint oTarget = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetWeight(oTarget);
        }

        /// <summary>
        ///   Gets the object that acquired the module item.  May be a creature, item, or placeable
        /// </summary>
        public static uint GetModuleItemAcquiredBy()
        {
            return global::NWN.Core.NWScript.GetModuleItemAcquiredBy();
        }

        /// <summary>
        ///   Causes the object to instantly speak a translated string.
        ///   (not an action, not blocked when uncommandable)
        ///   - nStrRef: Reference of the string in the talk table
        ///   - nTalkVolume: TALKVOLUME_*
        /// </summary>
        public static void SpeakStringByStrRef(int nStrRef, TalkVolume nTalkVolume = TalkVolume.Talk)
        {
            global::NWN.Core.NWScript.SpeakStringByStrRef(nStrRef, (int)nTalkVolume);
        }

        /// <summary>
        ///   Duplicates the object specified by oSource.
        ///   ONLY creatures and items can be specified.
        ///   If an owner is specified and the object is an item, it will be put into their inventory
        ///   If the object is a creature, they will be created at the location.
        ///   If a new tag is specified, it will be assigned to the new object.
        ///   If bCopyLocalState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are copied over.
        /// </summary>
        public static uint CopyObject(uint oSource, Location locLocation, uint oOwner = OBJECT_INVALID, string sNewTag = "", bool bCopyLocalState = false)
        {
            return global::NWN.Core.NWScript.CopyObject(oSource, locLocation, oOwner, sNewTag, bCopyLocalState ? 1 : 0);
        }

        /// <summary>
        ///   returns the template used to create this object (if appropriate)
        ///   * returns an empty string when no template found
        /// </summary>
        public static string GetResRef(uint oObject)
        {
            return global::NWN.Core.NWScript.GetResRef(oObject);
        }

        /// <summary>
        ///   Determine whether oObject has an inventory.
        ///   * Returns TRUE for creatures and stores, and checks to see if an item or placeable object is a container.
        ///   * Returns FALSE for all other object types.
        /// </summary>
        public static bool GetHasInventory(uint oObject)
        {
            return global::NWN.Core.NWScript.GetHasInventory(oObject) == 1;
        }

        /// <summary>
        ///   Get the name of oCreature's deity.
        ///   * Returns "" if oCreature is invalid (or if the deity name is blank for
        ///   oCreature).
        /// </summary>
        public static string GetDeity(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetDeity(oCreature);
        }

        /// <summary>
        ///   Get the name of oCreature's sub race.
        ///   * Returns "" if oCreature is invalid (or if sub race is blank for oCreature).
        /// </summary>
        public static string GetSubRace(uint oTarget)
        {
            return global::NWN.Core.NWScript.GetSubRace(oTarget);
        }

        /// <summary>
        ///   Get oTarget's base fortitude saving throw value (this will only work for
        ///   creatures, doors, and placeables).
        ///   * Returns 0 if oTarget is invalid.
        /// </summary>
        public static int GetFortitudeSavingThrow(uint oTarget)
        {
            return global::NWN.Core.NWScript.GetFortitudeSavingThrow(oTarget);
        }

        /// <summary>
        ///   Get oTarget's base will saving throw value (this will only work for creatures,
        ///   doors, and placeables).
        ///   * Returns 0 if oTarget is invalid.
        /// </summary>
        public static int GetWillSavingThrow(uint oTarget)
        {
            return global::NWN.Core.NWScript.GetWillSavingThrow(oTarget);
        }

        /// <summary>
        ///   Get oTarget's base reflex saving throw value (this will only work for
        ///   creatures, doors, and placeables).
        ///   * Returns 0 if oTarget is invalid.
        /// </summary>
        public static int GetReflexSavingThrow(uint oTarget)
        {
            return global::NWN.Core.NWScript.GetReflexSavingThrow(oTarget);
        }

        /// <summary>
        ///   Get oCreature's challenge rating.
        ///   * Returns 0.0 if oCreature is invalid.
        /// </summary>
        public static float GetChallengeRating(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetChallengeRating(oCreature);
        }

        /// <summary>
        ///   Get oCreature's age.
        ///   * Returns 0 if oCreature is invalid.
        /// </summary>
        public static int GetAge(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetAge(oCreature);
        }

        /// <summary>
        ///   Get oCreature's movement rate.
        ///   * Returns 0 if oCreature is invalid.
        /// </summary>
        public static int GetMovementRate(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetMovementRate(oCreature);
        }

        /// <summary>
        ///   Determine whether oTarget is a plot object.
        /// </summary>
        public static bool GetPlotFlag(uint oTarget = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetPlotFlag(oTarget) != 0;
        }

        /// <summary>
        ///   Set oTarget's plot object status.
        /// </summary>
        public static void SetPlotFlag(uint oTarget, bool nPlotFlag)
        {
            global::NWN.Core.NWScript.SetPlotFlag(oTarget, nPlotFlag ? 1 : 0);
        }

        /// <summary>
        ///   Play a voice chat.
        ///   - nVoiceChatID: VOICE_CHAT_*
        ///   - oTarget
        /// </summary>
        public static void PlayVoiceChat(VoiceChat nVoiceChatID, uint oTarget = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.PlayVoiceChat((int)nVoiceChatID, oTarget);
        }

        /// <summary>
        ///   Get the amount of gold possessed by oTarget.
        /// </summary>
        public static int GetGold(uint oTarget = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetGold(oTarget);
        }

        /// <summary>
        ///   Play oSound.
        /// </summary>
        public static void SoundObjectPlay(uint oSound)
        {
            global::NWN.Core.NWScript.SoundObjectPlay(oSound);
        }

        /// <summary>
        ///   Stop playing oSound.
        /// </summary>
        public static void SoundObjectStop(uint oSound)
        {
            global::NWN.Core.NWScript.SoundObjectStop(oSound);
        }

        /// <summary>
        ///   Set the volume of oSound.
        ///   - oSound
        ///   - nVolume: 0-127
        /// </summary>
        public static void SoundObjectSetVolume(uint oSound, int nVolume)
        {
            global::NWN.Core.NWScript.SoundObjectSetVolume(oSound, nVolume);
        }

        /// <summary>
        ///   Set the position of oSound.
        /// </summary>
        public static void SoundObjectSetPosition(uint oSound, Vector3 vPosition)
        {
            global::NWN.Core.NWScript.SoundObjectSetPosition(oSound, vPosition);
        }

        /// <summary>
        ///   Immediately speak a conversation one-liner.
        ///   - sDialogResRef
        ///   - oTokenTarget: This must be specified if there are creature-specific tokens
        ///   in the string.
        /// </summary>
        public static void SpeakOneLinerConversation(string sDialogResRef = "", uint oTokenTarget = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SpeakOneLinerConversation(sDialogResRef, oTokenTarget);
        }

        /// <summary>
        ///   Set the destroyable status of the caller.
        ///   - bDestroyable: If this is FALSE, the caller does not fade out on death, but
        ///   sticks around as a corpse.
        ///   - bRaiseable: If this is TRUE, the caller can be raised via resurrection.
        ///   - bSelectableWhenDead: If this is TRUE, the caller is selectable after death.
        /// </summary>
        public static void SetIsDestroyable(bool bDestroyable = true, bool bRaiseable = true,
            bool bSelectableWhenDead = false)
        {
            global::NWN.Core.NWScript.SetIsDestroyable(bDestroyable ? 1 : 0, bRaiseable ? 1 : 0, bSelectableWhenDead ? 1 : 0);
        }

        /// <summary>
        ///   Set the locked state of oTarget, which can be a door or a placeable object.
        /// </summary>
        public static void SetLocked(uint oTarget, bool nLocked)
        {
            global::NWN.Core.NWScript.SetLocked(oTarget, nLocked ? 1 : 0);
        }

        /// <summary>
        ///   Get the locked state of oTarget, which can be a door or a placeable object.
        /// </summary>
        public static bool GetLocked(uint oTarget)
        {
            return global::NWN.Core.NWScript.GetLocked(oTarget) != 0;
        }

        /// <summary>
        ///   Create an object of the specified type at lLocation.
        ///   - nObjectType: OBJECT_TYPE_ITEM, OBJECT_TYPE_CREATURE, OBJECT_TYPE_PLACEABLE,
        ///   OBJECT_TYPE_STORE, OBJECT_TYPE_WAYPOINT
        ///   - sTemplate
        ///   - lLocation
        ///   - bUseAppearAnimation
        ///   - sNewTag - if this string is not empty, it will replace the default tag from the template
        /// </summary>
        public static uint CreateObject(ObjectType nObjectType, string sTemplate, Location lLocation,
            bool nUseAppearAnimation = false, string sNewTag = "")
        {
            return global::NWN.Core.NWScript.CreateObject((int)nObjectType, sTemplate, lLocation, nUseAppearAnimation ? 1 : 0, sNewTag);
        }

        /// <summary>
        ///   Get the Nth object nearest to oTarget that is of the specified type.
        ///   - nObjectType: OBJECT_TYPE_*
        ///   - oTarget
        ///   - nNth
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetNearestObject(ObjectType nObjectType = ObjectType.All, uint oTarget = OBJECT_INVALID, int nNth = 1)
        {
            return global::NWN.Core.NWScript.GetNearestObject((int)nObjectType, oTarget, nNth);
        }

        /// <summary>
        ///   Get the nNth object nearest to lLocation that is of the specified type.
        ///   - nObjectType: OBJECT_TYPE_*
        ///   - lLocation
        ///   - nNth
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetNearestObjectToLocation(Location lLocation, ObjectType nObjectType = ObjectType.All,
            int nNth = 1)
        {
            return global::NWN.Core.NWScript.GetNearestObjectToLocation((int)nObjectType, lLocation, nNth);
        }

        /// <summary>
        ///   Get the nth Object nearest to oTarget that has sTag as its tag.
        ///   * Return value on error: OBJECT_INVALID
        /// </summary>
        public static uint GetNearestObjectByTag(string sTag, uint oTarget = OBJECT_INVALID, int nNth = 1)
        {
            return global::NWN.Core.NWScript.GetNearestObjectByTag(sTag, oTarget, nNth);
        }

        /// <summary>
        ///   If oObject is a creature, this will return that creature's armour class
        ///   If oObject is an item, door or placeable, this will return zero.
        ///   - nForFutureUse: this parameter is not currently used
        ///   * Return value if oObject is not a creature, item, door or placeable: -1
        /// </summary>
        public static int GetAC(uint oObject)
        {
            return global::NWN.Core.NWScript.GetAC(oObject, 0);
        }

        /// <summary>
        ///   Get the object type (OBJECT_TYPE_*) of oTarget
        ///   * Return value if oTarget is not a valid object: -1
        /// </summary>
        public static ObjectType GetObjectType(uint oTarget)
        {
            return (ObjectType)global::NWN.Core.NWScript.GetObjectType(oTarget);
        }

        /// <summary>
        ///   Get the current hitpoints of oObject
        ///   * Return value on error: 0
        /// </summary>
        public static int GetCurrentHitPoints(uint oObject = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCurrentHitPoints(oObject);
        }

        /// <summary>
        ///   Get the maximum hitpoints of oObject
        ///   * Return value on error: 0
        /// </summary>
        public static int GetMaxHitPoints(uint oObject = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetMaxHitPoints(oObject);
        }

        /// <summary>
        ///   * Returns TRUE if oObject is a valid object.
        /// </summary>
        public static bool GetIsObjectValid(uint oObject)
        {
            return global::NWN.Core.NWScript.GetIsObjectValid(oObject) != 0;
        }

        /// <summary>
        /// Convert sHex, a string containing a hexadecimal object id,
        /// into a object reference. Counterpart to StringToObject().
        /// </summary>
        public static uint StringToObject(string sHex)
        {
            return global::NWN.Core.NWScript.StringToObject(sHex);
        }


        /// <summary>
        /// Replace's oObject's texture sOld with sNew.
        /// Specifying sNew = "" will restore the original texture.
        /// If sNew cannot be found, the original texture will be restored.
        /// sNew must refer to a simple texture, not PLT
        /// </summary>
        public static void ReplaceObjectTexture(uint oObject, string sOld, string sNew = "")
        {
            global::NWN.Core.NWScript.ReplaceObjectTexture(oObject, sOld, sNew);
        }

        /// <summary>
        /// Sets the current hitpoints of oObject.
        /// * You cannot destroy or revive objects or creatures with this function.
        /// * For currently dying PCs, you can only set hitpoints in the range of -9 to 0.
        /// * All other objects need to be alive and the range is clamped to 1 and max hitpoints.
        /// * This is not considered damage (or healing). It circumvents all combat logic, including damage resistance and reduction.
        /// * This is not considered a friendly or hostile combat action. It will not affect factions, nor will it trigger script events.
        /// * This will not advise player parties in the combat log.
        /// </summary>
        public static void SetCurrentHitPoints(uint oObject, int nHitPoints)
        {
            global::NWN.Core.NWScript.SetCurrentHitPoints(oObject, nHitPoints);
        }
    }
}