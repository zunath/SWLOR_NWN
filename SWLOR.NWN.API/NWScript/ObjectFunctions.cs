using System.Numerics;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Sets a new tag for the object.
        /// Will do nothing for invalid objects or the module object.
        /// Note: Care needs to be taken with this function.
        /// Changing the tag for creature with waypoints will make them stop walking them.
        /// Changing waypoint, door or trigger tags will break their area transitions.
        /// </summary>
        /// <param name="oObject">The object to set the tag for</param>
        /// <param name="sNewTag">The new tag to set</param>
        public static void SetTag(uint oObject, string sNewTag)
        {
            global::NWN.Core.NWScript.SetTag(oObject, sNewTag);
        }

        /// <summary>
        /// Gets the last object that default clicked (left clicked) on the specified placeable object.
        /// Should only be called from a placeable's OnClick event.
        /// </summary>
        /// <returns>The last object that clicked, or OBJECT_INVALID if called by something other than a placeable</returns>
        public static uint GetPlaceableLastClickedBy()
        {
            return global::NWN.Core.NWScript.GetPlaceableLastClickedBy();
        }

        /// <summary>
        /// Sets the name of the object.
        /// Note: SetName() does not work on player objects.
        /// Setting an object's name to empty string will make the object
        /// revert to using the name it had originally before any
        /// SetName() calls were made on the object.
        /// </summary>
        /// <param name="oObject">The object for which you are changing the name (a creature, placeable, item, or door)</param>
        /// <param name="sNewName">The new name that the object will use (defaults to empty string)</param>
        public static void SetName(uint oObject, string sNewName = "")
        {
            global::NWN.Core.NWScript.SetName(oObject, sNewName);
        }

        /// <summary>
        /// Gets the PortraitId of the target.
        /// The Portrait Id refers to the row number of the Portraits.2da
        /// that this portrait is from.
        /// If a custom portrait is being used, the target is a player object,
        /// or on an error returns PORTRAIT_INVALID. In these instances
        /// try using GetPortraitResRef() instead.
        /// </summary>
        /// <param name="oTarget">The object for which you are getting the portrait Id (defaults to OBJECT_SELF)</param>
        /// <returns>The Portrait Id number being used for the object</returns>
        public static int GetPortraitId(uint oTarget = OBJECT_INVALID)
        {
            if (oTarget == OBJECT_INVALID)
                oTarget = OBJECT_SELF;
            return global::NWN.Core.NWScript.GetPortraitId(oTarget);
        }

        /// <summary>
        /// Changes the portrait of the target to use the Portrait Id specified.
        /// nPortraitId refers to a row in the Portraits.2da
        /// Note: Not all portrait Ids are suitable for use with all object types.
        /// Setting the portrait Id will also cause the portrait ResRef
        /// to be set to the appropriate portrait ResRef for the Id specified.
        /// </summary>
        /// <param name="oTarget">The object for which you are changing the portrait</param>
        /// <param name="nPortraitId">The Id of the new portrait to use</param>
        public static void SetPortraitId(uint oTarget, int nPortraitId)
        {
            global::NWN.Core.NWScript.SetPortraitId(oTarget, nPortraitId);
        }

        /// <summary>
        /// Gets the Portrait ResRef of the target.
        /// The Portrait ResRef will not include a trailing size letter.
        /// </summary>
        /// <param name="oTarget">The object for which you are getting the portrait ResRef (defaults to OBJECT_SELF)</param>
        /// <returns>The Portrait ResRef being used for the object</returns>
        public static string GetPortraitResRef(uint oTarget = OBJECT_INVALID)
        {
            if (oTarget == OBJECT_INVALID)
                oTarget = OBJECT_SELF;
            return global::NWN.Core.NWScript.GetPortraitResRef(oTarget);
        }

        /// <summary>
        /// Use this in a trigger's OnClick event script to get the object that last clicked on it.
        /// This is identical to GetEnteringObject.
        /// GetClickingObject() should not be called from a placeable's OnClick event,
        /// instead use GetPlaceableLastClickedBy().
        /// </summary>
        /// <returns>The object that last clicked on the trigger</returns>
        public static uint GetClickingObject()
        {
            return global::NWN.Core.NWScript.GetClickingObject();
        }

        /// <summary>
        /// Gets the last object that disarmed the trap on the specified object.
        /// </summary>
        /// <returns>The last object that disarmed the trap, or OBJECT_INVALID if the caller is not a valid placeable, trigger or door</returns>
        public static uint GetLastDisarmed()
        {
            return global::NWN.Core.NWScript.GetLastDisarmed();
        }

        /// <summary>
        /// Gets the last object that disturbed the inventory of the specified object.
        /// </summary>
        /// <returns>The last object that disturbed the inventory, or OBJECT_INVALID if the caller is not a valid creature or placeable</returns>
        public static uint GetLastDisturbed()
        {
            return global::NWN.Core.NWScript.GetLastDisturbed();
        }

        /// <summary>
        /// Gets the last object that locked the specified object.
        /// </summary>
        /// <param name="oObject">The object to get the last locker for (defaults to OBJECT_SELF)</param>
        /// <returns>The last object that locked the specified object, or OBJECT_INVALID if the caller is not a valid door or placeable</returns>
        public static uint GetLastLocked(uint oObject = OBJECT_INVALID)
        {
            if (oObject == OBJECT_INVALID)
                oObject = OBJECT_SELF;
            return global::NWN.Core.NWScript.GetLastLocked(oObject);
        }

        /// <summary>
        /// Gets the last object that unlocked the specified object.
        /// </summary>
        /// <param name="oObject">The object to get the last unlocker for (defaults to OBJECT_SELF)</param>
        /// <returns>The last object that unlocked the specified object, or OBJECT_INVALID if the caller is not a valid door or placeable</returns>
        public static uint GetLastUnlocked(uint oObject = OBJECT_INVALID)
        {
            if (oObject == OBJECT_INVALID)
                oObject = OBJECT_SELF;
            return global::NWN.Core.NWScript.GetLastUnlocked(oObject);
        }

        /// <summary>
        /// Changes the portrait of the target to use the Portrait ResRef specified.
        /// The ResRef should not include any trailing size letter (e.g. po_el_f_09_).
        /// Note: Not all portrait ResRefs are suitable for use with all object types.
        /// Setting the portrait ResRef will also cause the portrait Id to be set to PORTRAIT_INVALID.
        /// </summary>
        /// <param name="oTarget">The object for which you are changing the portrait</param>
        /// <param name="sPortraitResRef">The ResRef of the new portrait to use</param>
        public static void SetPortraitResRef(uint oTarget, string sPortraitResRef)
        {
            global::NWN.Core.NWScript.SetPortraitResRef(oTarget, sPortraitResRef);
        }

        /// <summary>
        /// Sets the target's useable object status.
        /// Note: Only works on non-static placeables, creatures, doors and items.
        /// On items, it affects interactivity when they're on the ground, and not useability in inventory.
        /// </summary>
        /// <param name="oPlaceable">The placeable object to set the useable flag for</param>
        /// <param name="nUseable">Whether the object is useable</param>
        public static void SetUseableFlag(uint oPlaceable, bool nUseable)
        {
            global::NWN.Core.NWScript.SetUseableFlag(oPlaceable, nUseable ? 1 : 0);
        }

        /// <summary>
        /// Gets the description of the object.
        /// Can be a creature, item, placeable, door, trigger or module object.
        /// If bOriginalDescription is set to true, any new description specified via a SetDescription scripting command
        /// is ignored and the original object's description is returned instead.
        /// If the object is an item, setting bIdentifiedDescription to TRUE will return the identified description,
        /// setting this to FALSE will return the unidentified description. This flag has no
        /// effect on objects other than items.
        /// </summary>
        /// <param name="oObject">The object from which you are obtaining the description</param>
        /// <param name="bOriginalDescription">If set to true, any new description specified via a SetDescription scripting command is ignored and the original object's description is returned instead</param>
        /// <param name="bIdentifiedDescription">If the object is an item, setting this to TRUE will return the identified description, setting this to FALSE will return the unidentified description</param>
        /// <returns>The description of the object</returns>
        public static string GetDescription(uint oObject, bool bOriginalDescription = false,
            bool bIdentifiedDescription = true)
        {
            return global::NWN.Core.NWScript.GetDescription(oObject, bOriginalDescription ? 1 : 0, bIdentifiedDescription ? 1 : 0);
        }

        /// <summary>
        /// Sets the description of the object.
        /// Can be a creature, placeable, item, door, or trigger.
        /// If the object is an item, setting bIdentifiedDescription to TRUE will set the identified description,
        /// setting this to FALSE will set the unidentified description. This flag has no
        /// effect on objects other than items.
        /// Note: Setting an object's description to empty string will make the object
        /// revert to using the description it originally had before any
        /// SetDescription() calls were made on the object.
        /// </summary>
        /// <param name="oObject">The object for which you are changing the description</param>
        /// <param name="sNewDescription">The new description that the object will use (defaults to empty string)</param>
        /// <param name="bIdentifiedDescription">If the object is an item, setting this to TRUE will set the identified description, setting this to FALSE will set the unidentified description</param>
        public static void SetDescription(uint oObject, string sNewDescription = "", bool bIdentifiedDescription = true)
        {
            global::NWN.Core.NWScript.SetDescription(oObject, sNewDescription, bIdentifiedDescription ? 1 : 0);
        }

        /// <summary>
        /// Gets the color of the object from the color channel specified.
        /// Can be a creature that has color information (i.e. the playable races).
        /// </summary>
        /// <param name="oObject">The object from which you are obtaining the color</param>
        /// <param name="nColorChannel">The color channel that you want to get the color value of (COLOR_CHANNEL_SKIN, COLOR_CHANNEL_HAIR, COLOR_CHANNEL_TATTOO_1, COLOR_CHANNEL_TATTOO_2)</param>
        /// <returns>The color value, or -1 on error</returns>
        public static int GetColor(uint oObject, ColorChannel nColorChannel)
        {
            return global::NWN.Core.NWScript.GetColor(oObject, (int)nColorChannel);
        }

        /// <summary>
        /// Sets the color channel of the object to the color specified.
        /// Can be a creature that has color information (i.e. the playable races).
        /// </summary>
        /// <param name="oObject">The object for which you are changing the color</param>
        /// <param name="nColorChannel">The color channel that you want to set the color value of (COLOR_CHANNEL_SKIN, COLOR_CHANNEL_HAIR, COLOR_CHANNEL_TATTOO_1, COLOR_CHANNEL_TATTOO_2)</param>
        /// <param name="nColorValue">The color you want to set (0-175)</param>
        public static void SetColor(uint oObject, ColorChannel nColorChannel, int nColorValue)
        {
            global::NWN.Core.NWScript.SetColor(oObject, (int)nColorChannel, nColorValue);
        }

        /// <summary>
        /// Gets the feedback message that will be displayed when trying to unlock the object.
        /// </summary>
        /// <param name="oObject">A door or placeable</param>
        /// <returns>The feedback message, or empty string on an error or if the game's default feedback message is being used</returns>
        public static string GetKeyRequiredFeedback(uint oObject)
        {
            return global::NWN.Core.NWScript.GetKeyRequiredFeedback(oObject);
        }

        /// <summary>
        /// Sets the feedback message that is displayed when trying to unlock the object.
        /// This will only have an effect if the object is set to
        /// "Key required to unlock or lock" either in the toolset
        /// or by using the scripting command SetLockKeyRequired().
        /// To use the game's default message, set sFeedbackMessage to empty string.
        /// </summary>
        /// <param name="oObject">A door or placeable</param>
        /// <param name="sFeedbackMessage">The string to be displayed in the player's text window</param>
        public static void SetKeyRequiredFeedback(uint oObject, string sFeedbackMessage)
        {
            global::NWN.Core.NWScript.SetKeyRequiredFeedback(oObject, sFeedbackMessage);
        }

        /// <summary>
        /// Locks the player's camera pitch to its current pitch setting,
        /// or unlocks the player's camera pitch.
        /// Stops the player from tilting their camera angle.
        /// </summary>
        /// <param name="oPlayer">A player object</param>
        /// <param name="bLocked">TRUE/FALSE (defaults to TRUE)</param>
        public static void LockCameraPitch(uint oPlayer, bool bLocked = true)
        {
            global::NWN.Core.NWScript.LockCameraPitch(oPlayer, bLocked ? 1 : 0);
        }

        /// <summary>
        /// Locks the player's camera distance to its current distance setting,
        /// or unlocks the player's camera distance.
        /// Stops the player from being able to zoom in/out the camera.
        /// </summary>
        /// <param name="oPlayer">A player object</param>
        /// <param name="bLocked">TRUE/FALSE (defaults to TRUE)</param>
        public static void LockCameraDistance(uint oPlayer, bool bLocked = true)
        {
            global::NWN.Core.NWScript.LockCameraDistance(oPlayer, bLocked ? 1 : 0);
        }

        /// <summary>
        /// Locks the player's camera direction to its current direction,
        /// or unlocks the player's camera direction to enable it to move freely again.
        /// Stops the player from being able to rotate the camera direction.
        /// </summary>
        /// <param name="oPlayer">A player object</param>
        /// <param name="bLocked">TRUE/FALSE (defaults to TRUE)</param>
        public static void LockCameraDirection(uint oPlayer, bool bLocked = true)
        {
            global::NWN.Core.NWScript.LockCameraDirection(oPlayer, bLocked ? 1 : 0);
        }

        /// <summary>
        /// Returns the hardness of a door or placeable object.
        /// </summary>
        /// <param name="oObject">A door or placeable object (defaults to OBJECT_INVALID)</param>
        /// <returns>The hardness value, or -1 on an error or if used on an object that is neither a door nor a placeable object</returns>
        public static int GetHardness(uint oObject = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetHardness(oObject);
        }

        /// <summary>
        /// Sets the hardness of a door or placeable object.
        /// Does nothing if used on an object that is neither a door nor a placeable.
        /// </summary>
        /// <param name="nHardness">Must be between 0 and 250</param>
        /// <param name="oObject">A door or placeable object (defaults to OBJECT_INVALID)</param>
        public static void SetHardness(int nHardness, uint oObject = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetHardness(nHardness, oObject);
        }

        /// <summary>
        /// When set, the object cannot be opened unless the opener possesses the required key.
        /// The key tag required can be specified either in the toolset, or by using the SetLockKeyTag() scripting command.
        /// </summary>
        /// <param name="oObject">A door or placeable</param>
        /// <param name="nKeyRequired">TRUE/FALSE (defaults to TRUE)</param>
        public static void SetLockKeyRequired(uint oObject, bool nKeyRequired = true)
        {
            global::NWN.Core.NWScript.SetLockKeyRequired(oObject, nKeyRequired ? 1 : 0);
        }

        /// <summary>
        /// Sets the key tag required to open the object.
        /// This will only have an effect if the object is set to
        /// "Key required to unlock or lock" either in the toolset
        /// or by using the scripting command SetLockKeyRequired().
        /// </summary>
        /// <param name="oObject">A door, placeable or trigger</param>
        /// <param name="sNewKeyTag">The key tag required to open the locked object</param>
        public static void SetLockKeyTag(uint oObject, string sNewKeyTag)
        {
            global::NWN.Core.NWScript.SetLockKeyTag(oObject, sNewKeyTag);
        }

        /// <summary>
        /// Sets whether or not the object can be locked.
        /// </summary>
        /// <param name="oObject">A door or placeable</param>
        /// <param name="nLockable">TRUE/FALSE (defaults to TRUE)</param>
        public static void SetLockLockable(uint oObject, bool nLockable = true)
        {
            global::NWN.Core.NWScript.SetLockLockable(oObject, nLockable ? 1 : 0);
        }

        /// <summary>
        /// Sets the DC for unlocking the object.
        /// </summary>
        /// <param name="oObject">A door or placeable object</param>
        /// <param name="nNewUnlockDC">Must be between 0 and 250</param>
        public static void SetLockUnlockDC(uint oObject, int nNewUnlockDC)
        {
            global::NWN.Core.NWScript.SetLockUnlockDC(oObject, nNewUnlockDC);
        }

        /// <summary>
        /// Sets the DC for locking the object.
        /// </summary>
        /// <param name="oObject">A door or placeable object</param>
        /// <param name="nNewLockDC">Must be between 0 and 250</param>
        public static void SetLockLockDC(uint oObject, int nNewLockDC)
        {
            global::NWN.Core.NWScript.SetLockLockDC(oObject, nNewLockDC);
        }

        /// <summary>
        /// Sets the Will saving throw value of the door or placeable object.
        /// </summary>
        /// <param name="oObject">A door or placeable object</param>
        /// <param name="nWillSave">Must be between 0 and 250</param>
        public static void SetWillSavingThrow(uint oObject, int nWillSave)
        {
            global::NWN.Core.NWScript.SetWillSavingThrow(oObject, nWillSave);
        }

        /// <summary>
        /// Sets the Reflex saving throw value of the door or placeable object.
        /// </summary>
        /// <param name="oObject">A door or placeable object</param>
        /// <param name="nReflexSave">Must be between 0 and 250</param>
        public static void SetReflexSavingThrow(uint oObject, int nReflexSave)
        {
            global::NWN.Core.NWScript.SetReflexSavingThrow(oObject, nReflexSave);
        }

        /// <summary>
        /// Sets the Fortitude saving throw value of the door or placeable object.
        /// </summary>
        /// <param name="oObject">A door or placeable object</param>
        /// <param name="nFortitudeSave">Must be between 0 and 250</param>
        public static void SetFortitudeSavingThrow(uint oObject, int nFortitudeSave)
        {
            global::NWN.Core.NWScript.SetFortitudeSavingThrow(oObject, nFortitudeSave);
        }

        /// <summary>
        /// Gets the weight of an item, or the total carried weight of a creature in tenths
        /// of pounds (as per the baseitems.2da).
        /// </summary>
        /// <param name="oTarget">The item or creature for which the weight is needed (defaults to OBJECT_SELF)</param>
        /// <returns>The weight in tenths of pounds</returns>
        public static int GetWeight(uint oTarget = OBJECT_INVALID)
        {
            if (oTarget == OBJECT_INVALID)
                oTarget = OBJECT_SELF;
            return global::NWN.Core.NWScript.GetWeight(oTarget);
        }

        /// <summary>
        /// Gets the object that acquired the module item. May be a creature, item, or placeable.
        /// </summary>
        /// <returns>The object that acquired the module item</returns>
        public static uint GetModuleItemAcquiredBy()
        {
            return global::NWN.Core.NWScript.GetModuleItemAcquiredBy();
        }

        /// <summary>
        /// Causes the object to instantly speak a translated string.
        /// (not an action, not blocked when uncommandable)
        /// </summary>
        /// <param name="nStrRef">Reference of the string in the talk table</param>
        /// <param name="nTalkVolume">TALKVOLUME_* constant (defaults to TalkVolume.Talk)</param>
        public static void SpeakStringByStrRef(int nStrRef, TalkVolume nTalkVolume = TalkVolume.Talk)
        {
            global::NWN.Core.NWScript.SpeakStringByStrRef(nStrRef, (int)nTalkVolume);
        }

        /// <summary>
        /// Duplicates the object specified by the source.
        /// ONLY creatures and items can be specified.
        /// If an owner is specified and the object is an item, it will be put into their inventory.
        /// If the object is a creature, they will be created at the location.
        /// If a new tag is specified, it will be assigned to the new object.
        /// If bCopyLocalState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are copied over.
        /// </summary>
        /// <param name="oSource">The object to duplicate</param>
        /// <param name="locLocation">The location to create the copy at</param>
        /// <param name="oOwner">The owner of the copied object (defaults to OBJECT_INVALID)</param>
        /// <param name="sNewTag">The new tag to assign to the copied object (defaults to empty string)</param>
        /// <param name="bCopyLocalState">Whether to copy local state (defaults to false)</param>
        /// <returns>The copied object</returns>
        public static uint CopyObject(uint oSource, Location locLocation, uint oOwner = OBJECT_INVALID, string sNewTag = "", bool bCopyLocalState = false)
        {
            return global::NWN.Core.NWScript.CopyObject(oSource, locLocation, oOwner, sNewTag, bCopyLocalState ? 1 : 0);
        }

        /// <summary>
        /// Returns the template used to create this object (if appropriate).
        /// </summary>
        /// <param name="oObject">The object to get the resref for</param>
        /// <returns>The template resref, or empty string when no template found</returns>
        public static string GetResRef(uint oObject)
        {
            return global::NWN.Core.NWScript.GetResRef(oObject);
        }

        /// <summary>
        /// Determines whether the object has an inventory.
        /// Returns TRUE for creatures and stores, and checks to see if an item or placeable object is a container.
        /// Returns FALSE for all other object types.
        /// </summary>
        /// <param name="oObject">The object to check</param>
        /// <returns>TRUE if the object has an inventory, FALSE otherwise</returns>
        public static bool GetHasInventory(uint oObject)
        {
            return global::NWN.Core.NWScript.GetHasInventory(oObject) == 1;
        }

        /// <summary>
        /// Gets the name of the creature's deity.
        /// </summary>
        /// <param name="oCreature">The creature to get the deity for</param>
        /// <returns>The deity name, or empty string if the creature is invalid or if the deity name is blank</returns>
        public static string GetDeity(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetDeity(oCreature);
        }

        /// <summary>
        /// Gets the name of the creature's sub race.
        /// </summary>
        /// <param name="oTarget">The creature to get the sub race for</param>
        /// <returns>The sub race name, or empty string if the creature is invalid or if sub race is blank</returns>
        public static string GetSubRace(uint oTarget)
        {
            return global::NWN.Core.NWScript.GetSubRace(oTarget);
        }

        /// <summary>
        /// Gets the target's base fortitude saving throw value (this will only work for
        /// creatures, doors, and placeables).
        /// </summary>
        /// <param name="oTarget">The target to get the fortitude saving throw for</param>
        /// <returns>The fortitude saving throw value, or 0 if the target is invalid</returns>
        public static int GetFortitudeSavingThrow(uint oTarget)
        {
            return global::NWN.Core.NWScript.GetFortitudeSavingThrow(oTarget);
        }

        /// <summary>
        /// Gets the target's base will saving throw value (this will only work for creatures,
        /// doors, and placeables).
        /// </summary>
        /// <param name="oTarget">The target to get the will saving throw for</param>
        /// <returns>The will saving throw value, or 0 if the target is invalid</returns>
        public static int GetWillSavingThrow(uint oTarget)
        {
            return global::NWN.Core.NWScript.GetWillSavingThrow(oTarget);
        }

        /// <summary>
        /// Gets the target's base reflex saving throw value (this will only work for
        /// creatures, doors, and placeables).
        /// </summary>
        /// <param name="oTarget">The target to get the reflex saving throw for</param>
        /// <returns>The reflex saving throw value, or 0 if the target is invalid</returns>
        public static int GetReflexSavingThrow(uint oTarget)
        {
            return global::NWN.Core.NWScript.GetReflexSavingThrow(oTarget);
        }

        /// <summary>
        /// Gets the creature's challenge rating.
        /// </summary>
        /// <param name="oCreature">The creature to get the challenge rating for</param>
        /// <returns>The challenge rating, or 0.0 if the creature is invalid</returns>
        public static float GetChallengeRating(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetChallengeRating(oCreature);
        }

        /// <summary>
        /// Gets the creature's age.
        /// </summary>
        /// <param name="oCreature">The creature to get the age for</param>
        /// <returns>The age, or 0 if the creature is invalid</returns>
        public static int GetAge(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetAge(oCreature);
        }

        /// <summary>
        /// Gets the creature's movement rate.
        /// </summary>
        /// <param name="oCreature">The creature to get the movement rate for</param>
        /// <returns>The movement rate, or 0 if the creature is invalid</returns>
        public static int GetMovementRate(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetMovementRate(oCreature);
        }

        /// <summary>
        /// Determines whether the target is a plot object.
        /// </summary>
        /// <param name="oTarget">The target to check (defaults to OBJECT_INVALID)</param>
        /// <returns>TRUE if the target is a plot object</returns>
        public static bool GetPlotFlag(uint oTarget = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetPlotFlag(oTarget) != 0;
        }

        /// <summary>
        /// Sets the target's plot object status.
        /// </summary>
        /// <param name="oTarget">The target to set the plot flag for</param>
        /// <param name="nPlotFlag">The plot flag status</param>
        public static void SetPlotFlag(uint oTarget, bool nPlotFlag)
        {
            global::NWN.Core.NWScript.SetPlotFlag(oTarget, nPlotFlag ? 1 : 0);
        }

        /// <summary>
        /// Plays a voice chat.
        /// </summary>
        /// <param name="nVoiceChatID">VOICE_CHAT_* constant</param>
        /// <param name="oTarget">The target (defaults to OBJECT_INVALID)</param>
        public static void PlayVoiceChat(VoiceChat nVoiceChatID, uint oTarget = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.PlayVoiceChat((int)nVoiceChatID, oTarget);
        }

        /// <summary>
        /// Gets the amount of gold possessed by the target.
        /// </summary>
        /// <param name="oTarget">The target to get the gold for (defaults to OBJECT_SELF)</param>
        /// <returns>The amount of gold</returns>
        public static int GetGold(uint oTarget = OBJECT_INVALID)
        {
            if (oTarget == OBJECT_INVALID)
                oTarget = OBJECT_SELF;
            return global::NWN.Core.NWScript.GetGold(oTarget);
        }

        /// <summary>
        /// Plays the sound object.
        /// </summary>
        /// <param name="oSound">The sound object to play</param>
        public static void SoundObjectPlay(uint oSound)
        {
            global::NWN.Core.NWScript.SoundObjectPlay(oSound);
        }

        /// <summary>
        /// Stops playing the sound object.
        /// </summary>
        /// <param name="oSound">The sound object to stop</param>
        public static void SoundObjectStop(uint oSound)
        {
            global::NWN.Core.NWScript.SoundObjectStop(oSound);
        }

        /// <summary>
        /// Sets the volume of the sound object.
        /// </summary>
        /// <param name="oSound">The sound object</param>
        /// <param name="nVolume">Volume level (0-127)</param>
        public static void SoundObjectSetVolume(uint oSound, int nVolume)
        {
            global::NWN.Core.NWScript.SoundObjectSetVolume(oSound, nVolume);
        }

        /// <summary>
        /// Sets the position of the sound object.
        /// </summary>
        /// <param name="oSound">The sound object</param>
        /// <param name="vPosition">The position to set</param>
        public static void SoundObjectSetPosition(uint oSound, Vector3 vPosition)
        {
            global::NWN.Core.NWScript.SoundObjectSetPosition(oSound, vPosition);
        }

        /// <summary>
        /// Immediately speaks a conversation one-liner.
        /// </summary>
        /// <param name="sDialogResRef">The dialog resref (defaults to empty string)</param>
        /// <param name="oTokenTarget">This must be specified if there are creature-specific tokens in the string (defaults to OBJECT_INVALID)</param>
        public static void SpeakOneLinerConversation(string sDialogResRef = "", uint oTokenTarget = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SpeakOneLinerConversation(sDialogResRef, oTokenTarget);
        }

        /// <summary>
        /// Sets the destroyable status of the caller.
        /// </summary>
        /// <param name="bDestroyable">If FALSE, the caller does not fade out on death, but sticks around as a corpse (defaults to true)</param>
        /// <param name="bRaiseable">If TRUE, the caller can be raised via resurrection (defaults to true)</param>
        /// <param name="bSelectableWhenDead">If TRUE, the caller is selectable after death (defaults to false)</param>
        public static void SetIsDestroyable(bool bDestroyable = true, bool bRaiseable = true,
            bool bSelectableWhenDead = false)
        {
            global::NWN.Core.NWScript.SetIsDestroyable(bDestroyable ? 1 : 0, bRaiseable ? 1 : 0, bSelectableWhenDead ? 1 : 0);
        }

        /// <summary>
        /// Sets the locked state of the target, which can be a door or a placeable object.
        /// </summary>
        /// <param name="oTarget">The door or placeable object</param>
        /// <param name="nLocked">The locked state</param>
        public static void SetLocked(uint oTarget, bool nLocked)
        {
            global::NWN.Core.NWScript.SetLocked(oTarget, nLocked ? 1 : 0);
        }

        /// <summary>
        /// Gets the locked state of the target, which can be a door or a placeable object.
        /// </summary>
        /// <param name="oTarget">The door or placeable object</param>
        /// <returns>The locked state</returns>
        public static bool GetLocked(uint oTarget)
        {
            return global::NWN.Core.NWScript.GetLocked(oTarget) != 0;
        }

        /// <summary>
        /// Creates an object of the specified type at the location.
        /// If sNewTag is not empty, it will replace the default tag from the template.
        /// </summary>
        /// <param name="nObjectType">OBJECT_TYPE_ITEM, OBJECT_TYPE_CREATURE, OBJECT_TYPE_PLACEABLE, OBJECT_TYPE_STORE, OBJECT_TYPE_WAYPOINT</param>
        /// <param name="sTemplate">The template to use</param>
        /// <param name="lLocation">The location to create the object at</param>
        /// <param name="nUseAppearAnimation">Whether to use appear animation (defaults to false)</param>
        /// <param name="sNewTag">If not empty, replaces the default tag from the template (defaults to empty string)</param>
        /// <returns>The created object</returns>
        public static uint CreateObject(ObjectType nObjectType, string sTemplate, Location lLocation,
            bool nUseAppearAnimation = false, string sNewTag = "")
        {
            return global::NWN.Core.NWScript.CreateObject((int)nObjectType, sTemplate, lLocation, nUseAppearAnimation ? 1 : 0, sNewTag);
        }

        /// <summary>
        /// Gets the nth object nearest to the target that is of the specified type.
        /// </summary>
        /// <param name="nObjectType">OBJECT_TYPE_* constant (defaults to ObjectType.All)</param>
        /// <param name="oTarget">The target object (defaults to OBJECT_INVALID)</param>
        /// <param name="nNth">The nth object to find (defaults to 1)</param>
        /// <returns>The nearest object, or OBJECT_INVALID on error</returns>
        public static uint GetNearestObject(ObjectType nObjectType = ObjectType.All, uint oTarget = OBJECT_INVALID, int nNth = 1)
        {
            return global::NWN.Core.NWScript.GetNearestObject((int)nObjectType, oTarget, nNth);
        }

        /// <summary>
        /// Gets the nth object nearest to the location that is of the specified type.
        /// </summary>
        /// <param name="lLocation">The location to search from</param>
        /// <param name="nObjectType">OBJECT_TYPE_* constant (defaults to ObjectType.All)</param>
        /// <param name="nNth">The nth object to find (defaults to 1)</param>
        /// <returns>The nearest object, or OBJECT_INVALID on error</returns>
        public static uint GetNearestObjectToLocation(Location lLocation, ObjectType nObjectType = ObjectType.All,
            int nNth = 1)
        {
            return global::NWN.Core.NWScript.GetNearestObjectToLocation((int)nObjectType, lLocation, nNth);
        }

        /// <summary>
        /// Gets the nth object nearest to the target that has the specified tag.
        /// </summary>
        /// <param name="sTag">The tag to search for</param>
        /// <param name="oTarget">The target object (defaults to OBJECT_INVALID)</param>
        /// <param name="nNth">The nth object to find (defaults to 1)</param>
        /// <returns>The nearest object with the tag, or OBJECT_INVALID on error</returns>
        public static uint GetNearestObjectByTag(string sTag, uint oTarget = OBJECT_INVALID, int nNth = 1)
        {
            return global::NWN.Core.NWScript.GetNearestObjectByTag(sTag, oTarget, nNth);
        }

        /// <summary>
        /// If the object is a creature, this will return that creature's armour class.
        /// If the object is an item, door or placeable, this will return zero.
        /// </summary>
        /// <param name="oObject">The object to get the AC for</param>
        /// <returns>The armour class, or -1 if the object is not a creature, item, door or placeable</returns>
        public static int GetAC(uint oObject)
        {
            return global::NWN.Core.NWScript.GetAC(oObject, 0);
        }

        /// <summary>
        /// Gets the object type of the target.
        /// </summary>
        /// <param name="oTarget">The target object</param>
        /// <returns>The object type (OBJECT_TYPE_*), or -1 if the target is not a valid object</returns>
        public static ObjectType GetObjectType(uint oTarget)
        {
            return (ObjectType)global::NWN.Core.NWScript.GetObjectType(oTarget);
        }

        /// <summary>
        /// Gets the current hitpoints of the object.
        /// </summary>
        /// <param name="oObject">The object to get hitpoints for (defaults to OBJECT_SELF)</param>
        /// <returns>The current hitpoints, or 0 on error</returns>
        public static int GetCurrentHitPoints(uint oObject = OBJECT_INVALID)
        {
            if (oObject == OBJECT_INVALID)
                oObject = OBJECT_SELF;
            return global::NWN.Core.NWScript.GetCurrentHitPoints(oObject);
        }

        /// <summary>
        /// Gets the maximum hitpoints of the object.
        /// </summary>
        /// <param name="oObject">The object to get max hitpoints for (defaults to OBJECT_SELF)</param>
        /// <returns>The maximum hitpoints, or 0 on error</returns>
        public static int GetMaxHitPoints(uint oObject = OBJECT_INVALID)
        {
            if (oObject == OBJECT_INVALID)
                oObject = OBJECT_SELF;
            return global::NWN.Core.NWScript.GetMaxHitPoints(oObject);
        }

        /// <summary>
        /// Returns TRUE if the object is a valid object.
        /// </summary>
        /// <param name="oObject">The object to check</param>
        /// <returns>TRUE if the object is valid</returns>
        public static bool GetIsObjectValid(uint oObject)
        {
            return global::NWN.Core.NWScript.GetIsObjectValid(oObject) != 0;
        }

        /// <summary>
        /// Converts a string containing a hexadecimal object id into an object reference.
        /// Counterpart to StringToObject().
        /// </summary>
        /// <param name="sHex">The hexadecimal string</param>
        /// <returns>The object reference</returns>
        public static uint StringToObject(string sHex)
        {
            return global::NWN.Core.NWScript.StringToObject(sHex);
        }


        /// <summary>
        /// Replaces the object's texture sOld with sNew.
        /// Specifying sNew = empty string will restore the original texture.
        /// If sNew cannot be found, the original texture will be restored.
        /// sNew must refer to a simple texture, not PLT.
        /// </summary>
        /// <param name="oObject">The object to replace the texture for</param>
        /// <param name="sOld">The old texture name</param>
        /// <param name="sNew">The new texture name (defaults to empty string)</param>
        public static void ReplaceObjectTexture(uint oObject, string sOld, string sNew = "")
        {
            global::NWN.Core.NWScript.ReplaceObjectTexture(oObject, sOld, sNew);
        }

        /// <summary>
        /// Sets the current hitpoints of the object.
        /// You cannot destroy or revive objects or creatures with this function.
        /// For currently dying PCs, you can only set hitpoints in the range of -9 to 0.
        /// All other objects need to be alive and the range is clamped to 1 and max hitpoints.
        /// This is not considered damage (or healing). It circumvents all combat logic, including damage resistance and reduction.
        /// This is not considered a friendly or hostile combat action. It will not affect factions, nor will it trigger script events.
        /// This will not advise player parties in the combat log.
        /// </summary>
        /// <param name="oObject">The object to set hitpoints for</param>
        /// <param name="nHitPoints">The hitpoints to set</param>
        public static void SetCurrentHitPoints(uint oObject, int nHitPoints)
        {
            global::NWN.Core.NWScript.SetCurrentHitPoints(oObject, nHitPoints);
        }

        /// <summary>
        /// Gets the first object in the specified shape.
        /// If nShape == SHAPE_SPHERE, fSize is the radius of the sphere.
        /// If nShape == SHAPE_SPELLCYLINDER, fSize is the length of the cylinder. Spell Cylinders always have a radius of 1.5m.
        /// If nShape == SHAPE_CONE, fSize is the widest radius of the cone.
        /// If nShape == SHAPE_SPELLCONE, fSize is the length of the cone in the direction of lTarget. Spell cones are always 60 degrees with the origin at OBJECT_SELF.
        /// If nShape == SHAPE_CUBE, fSize is half the length of one of the sides of the cube.
        /// This can be used to ensure that spell effects do not go through walls.
        /// For example, to return only creatures and doors, the value for nObjectFilter would be OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR.
        /// </summary>
        /// <param name="nShape">SHAPE_* constant</param>
        /// <param name="fSize">The size parameter (see summary for details)</param>
        /// <param name="lTarget">The centre of the effect, usually GetSpellTargetLocation(), or the end of a cylinder or cone</param>
        /// <param name="bLineOfSight">Whether to do a line-of-sight check on the object returned (defaults to false)</param>
        /// <param name="nObjectFilter">Allows you to filter out undesired object types using bitwise "or" (defaults to ObjectType.Creature)</param>
        /// <param name="vOrigin">Only used for cylinders and cones, specifies the origin of the effect (normally the spell-caster's position) (defaults to default)</param>
        /// <returns>The first object in the shape, or OBJECT_INVALID on error</returns>
        public static uint GetFirstObjectInShape(Shape nShape, float fSize, Location lTarget, bool bLineOfSight = false,
            ObjectType nObjectFilter = ObjectType.Creature, Vector3 vOrigin = default)
        {
            return global::NWN.Core.NWScript.GetFirstObjectInShape((int)nShape, fSize, lTarget, bLineOfSight ? 1 : 0, (int)nObjectFilter, vOrigin);
        }

        /// <summary>
        /// Gets the next object in the specified shape.
        /// If nShape == SHAPE_SPHERE, fSize is the radius of the sphere.
        /// If nShape == SHAPE_SPELLCYLINDER, fSize is the length of the cylinder. Spell Cylinders always have a radius of 1.5m.
        /// If nShape == SHAPE_CONE, fSize is the widest radius of the cone.
        /// If nShape == SHAPE_SPELLCONE, fSize is the length of the cone in the direction of lTarget. Spell cones are always 60 degrees with the origin at OBJECT_SELF.
        /// If nShape == SHAPE_CUBE, fSize is half the length of one of the sides of the cube.
        /// This can be used to ensure that spell effects do not go through walls.
        /// For example, to return only creatures and doors, the value for nObjectFilter would be OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR.
        /// </summary>
        /// <param name="nShape">SHAPE_* constant</param>
        /// <param name="fSize">The size parameter (see summary for details)</param>
        /// <param name="lTarget">The centre of the effect, usually GetSpellTargetLocation(), or the end of a cylinder or cone</param>
        /// <param name="bLineOfSight">Whether to do a line-of-sight check on the object returned (defaults to false)</param>
        /// <param name="nObjectFilter">Allows you to filter out undesired object types using bitwise "or" (defaults to ObjectType.Creature)</param>
        /// <param name="vOrigin">Only used for cylinders and cones, specifies the origin of the effect (normally the spell-caster's position) (defaults to default)</param>
        /// <returns>The next object in the shape, or OBJECT_INVALID on error</returns>
        public static uint GetNextObjectInShape(Shape nShape, float fSize, Location lTarget, bool bLineOfSight = false,
            ObjectType nObjectFilter = ObjectType.Creature, Vector3 vOrigin = default)
        {
            return global::NWN.Core.NWScript.GetNextObjectInShape((int)nShape, fSize, lTarget, bLineOfSight ? 1 : 0, (int)nObjectFilter, vOrigin);
        }

        /// <summary>
        /// Causes the caller to face the target point.
        /// </summary>
        /// <param name="vTarget">The target point to face</param>
        public static void SetFacingPoint(Vector3 vTarget)
        {
            global::NWN.Core.NWScript.SetFacingPoint(vTarget);
        }

        /// <summary>
        /// Gets the distance in metres between the two objects.
        /// </summary>
        /// <param name="oObjectA">The first object</param>
        /// <param name="oObjectB">The second object</param>
        /// <returns>The distance in metres, or 0.0f if either object is invalid</returns>
        public static float GetDistanceBetween(uint oObjectA, uint oObjectB)
        {
            return global::NWN.Core.NWScript.GetDistanceBetween(oObjectA, oObjectB);
        }

        /// <summary>
        /// Sets whether the target's action stack can be modified.
        /// </summary>
        /// <param name="nCommandable">Whether the target is commandable</param>
        /// <param name="oTarget">The target object (defaults to OBJECT_SELF)</param>
        public static void SetCommandable(bool nCommandable, uint oTarget = OBJECT_INVALID)
        {
            if (oTarget == OBJECT_INVALID)
                oTarget = OBJECT_SELF;
            global::NWN.Core.NWScript.SetCommandable(nCommandable ? 1 : 0, oTarget);
        }

        /// <summary>
        /// Determines whether the target's action stack can be modified.
        /// </summary>
        /// <param name="oTarget">The target object (defaults to OBJECT_SELF)</param>
        /// <returns>TRUE if the target is commandable</returns>
        public static bool GetCommandable(uint oTarget = OBJECT_INVALID)
        {
            if (oTarget == OBJECT_INVALID)
                oTarget = OBJECT_SELF;
            return global::NWN.Core.NWScript.GetCommandable(oTarget) != 0;
        }

        /// <summary>
        /// Gets the tag of the object.
        /// </summary>
        /// <param name="oObject">The object to get the tag for</param>
        /// <returns>The tag, or empty string if the object is not a valid object</returns>
        public static string GetTag(uint oObject)
        {
            return global::NWN.Core.NWScript.GetTag(oObject);
        }

        /// <summary>
        /// Returns TRUE if the object is listening for something.
        /// </summary>
        /// <param name="oObject">The object to check</param>
        /// <returns>TRUE if the object is listening</returns>
        public static bool GetIsListening(uint oObject)
        {
            return global::NWN.Core.NWScript.GetIsListening(oObject) != 0;
        }

        /// <summary>
        /// Sets whether the object is listening.
        /// </summary>
        /// <param name="oObject">The object to set listening for</param>
        /// <param name="bValue">Whether the object is listening</param>
        public static void SetListening(uint oObject, bool bValue)
        {
            global::NWN.Core.NWScript.SetListening(oObject, bValue ? 1 : 0);
        }

        /// <summary>
        /// Sets the string for the object to listen for.
        /// Note: this does not set the object to be listening.
        /// </summary>
        /// <param name="oObject">The object to set the listen pattern for</param>
        /// <param name="sPattern">The pattern to listen for</param>
        /// <param name="nNumber">The pattern number (defaults to 0)</param>
        public static void SetListenPattern(uint oObject, string sPattern, int nNumber = 0)
        {
            global::NWN.Core.NWScript.SetListenPattern(oObject, sPattern, nNumber);
        }

        /// <summary>
        /// In an onConversation script this gets the number of the string pattern
        /// matched (the one that triggered the script).
        /// </summary>
        /// <returns>The pattern number, or -1 if no string matched</returns>
        public static int GetListenPatternNumber()
        {
            return global::NWN.Core.NWScript.GetListenPatternNumber();
        }

        /// <summary>
        /// Gets the first waypoint with the specified tag.
        /// </summary>
        /// <param name="sWaypointTag">The waypoint tag to search for</param>
        /// <returns>The waypoint, or OBJECT_INVALID if the waypoint cannot be found</returns>
        public static uint GetWaypointByTag(string sWaypointTag)
        {
            return global::NWN.Core.NWScript.GetWaypointByTag(sWaypointTag);
        }

        /// <summary>
        /// Gets the destination object for the given object.
        /// All objects can hold a transition target, but only Doors and Triggers
        /// will be made clickable by the game engine (This may change in the
        /// future). You can set and query transition targets on other objects for
        /// your own scripted purposes.
        /// </summary>
        /// <param name="oTransition">The transition object</param>
        /// <returns>The destination object, or OBJECT_INVALID if the transition does not hold a target</returns>
        public static uint GetTransitionTarget(uint oTransition)
        {
            return global::NWN.Core.NWScript.GetTransitionTarget(oTransition);
        }

        /// <summary>
        /// Gets the nth object with the specified tag.
        /// Note: The module cannot be retrieved by GetObjectByTag(), use GetModule() instead.
        /// </summary>
        /// <param name="sTag">The tag to search for</param>
        /// <param name="nNth">The nth object with this tag may be requested (defaults to 0)</param>
        /// <returns>The object, or OBJECT_INVALID if the object cannot be found</returns>
        public static uint GetObjectByTag(string sTag, int nNth = 0)
        {
            return global::NWN.Core.NWScript.GetObjectByTag(sTag, nNth);
        }

        /// <summary>
        /// Gets the creature that is currently sitting on the specified object.
        /// </summary>
        /// <param name="oChair">The chair object</param>
        /// <returns>The sitting creature, or OBJECT_INVALID if the chair is not a valid placeable</returns>
        public static uint GetSittingCreature(uint oChair)
        {
            return global::NWN.Core.NWScript.GetSittingCreature(oChair);
        }

        /// <summary>
        /// The caller will immediately speak the string (this is different from ActionSpeakString).
        /// </summary>
        /// <param name="sStringToSpeak">The string to speak</param>
        /// <param name="nTalkVolume">TALKVOLUME_* constant (defaults to TalkVolume.Talk)</param>
        public static void SpeakString(string sStringToSpeak, TalkVolume nTalkVolume = TalkVolume.Talk)
        {
            global::NWN.Core.NWScript.SpeakString(sStringToSpeak, (int)nTalkVolume);
        }

        /// <summary>
        /// Gets the name of the object.
        /// </summary>
        /// <param name="oObject">The object from which you are obtaining the name (area, creature, placeable, item, or door)</param>
        /// <param name="bOriginalName">If set to true returns the name that the object had when the module was loaded (i.e. the original name) (defaults to false)</param>
        /// <returns>The name, or empty string on error</returns>
        public static string GetName(uint oObject, bool bOriginalName = false)
        {
            return global::NWN.Core.NWScript.GetName(oObject, bOriginalName ? 1 : 0);
        }

        /// <summary>
        /// Converts the object into a hexadecimal string.
        /// </summary>
        /// <param name="oObject">The object to convert</param>
        /// <returns>The hexadecimal string representation of the object</returns>
        public static string ObjectToString(uint oObject)
        {
            return global::NWN.Core.NWScript.ObjectToString(oObject);
        }
    }
}