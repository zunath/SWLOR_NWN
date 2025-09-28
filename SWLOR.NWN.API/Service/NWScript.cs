using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Constants;
using SWLOR.NWN.API.NWScript.Enum;
using System.Numerics;
using SWLOR.NWN.API.Contracts;

namespace SWLOR.NWN.API.Service
{
    public static class NWScript
    {
        private static INWScriptService _service = new NWScriptService();
        
        /// <summary>
        /// Sets the service implementation to use. This is primarily for testing.
        /// </summary>
        /// <param name="service">The service implementation to use</param>
        internal static void SetService(INWScriptService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        // ReSharper disable once InconsistentNaming
        public static uint OBJECT_SELF => _service.OBJECT_SELF;

        // ReSharper disable once InconsistentNaming
        public const uint OBJECT_INVALID = NWScriptService.OBJECT_INVALID;

        /// <summary>
        /// Number of inventory slots available.
        /// </summary>
        public static int NumberOfInventorySlots => _service.NumberOfInventorySlots;

        /// <summary>
        /// Gets the starting location of the module.
        /// </summary>
        /// <returns>The starting location of the module</returns>
        public static Location GetStartingLocation => _service.GetStartingLocation();

        /// <summary>
        /// Gets the module's name in the language of the server that's running it.
        /// </summary>
        /// <returns>The module's name. Returns an empty string if there is no entry for the language of the server</returns>
        public static string GetModuleName() => _service.GetModuleName();

        /// <summary>
        /// Shuts down the currently loaded module and starts a new one.
        /// </summary>
        /// <param name="sModuleName">The name of the new module to start</param>
        /// <remarks>Moves all currently-connected players to the starting point.</remarks>
        public static void StartNewModule(string sModuleName) => _service.StartNewModule(sModuleName);

        /// <summary>
        /// Auto-saves the game if we are in a single player game.
        /// </summary>
        /// <remarks>Only works in single player games.</remarks>
        public static void DoSinglePlayerAutoSave() => _service.DoSinglePlayerAutoSave();

        /// <summary>
        /// Gets the game difficulty.
        /// </summary>
        /// <returns>The game difficulty (GAME_DIFFICULTY_* constants)</returns>
        public static int GetGameDifficulty() => _service.GetGameDifficulty();

        /// <summary>
        /// Ends the currently running game and returns all players to the main menu.
        /// </summary>
        /// <param name="sEndMovie">The movie to play before ending the game</param>
        public static void EndGame(string sEndMovie) => _service.EndGame(sEndMovie);

        /// <summary>
        /// Gets the XP scale being used for the module.
        /// </summary>
        /// <returns>The XP scale being used for the module</returns>
        public static int GetModuleXPScale() => _service.GetModuleXPScale();

        /// <summary>
        /// Sets the XP scale used by the module.
        /// </summary>
        /// <param name="nXPScale">The XP scale to be used (must be between 0 and 200)</param>
        public static void SetModuleXPScale(int nXPScale) => _service.SetModuleXPScale(nXPScale);

        /// <summary>
        /// Writes a timestamped entry into the log file.
        /// </summary>
        /// <param name="sLogEntry">The log entry to write</param>
        public static void WriteTimestampedLogEntry(string sLogEntry) => _service.WriteTimestampedLogEntry(sLogEntry);

        /// <summary>
        /// Gets the current action that the specified object is executing.
        /// </summary>
        /// <param name="oObject">The object to get the current action for (default: OBJECT_SELF)</param>
        /// <returns>The current action (ACTION_* constants)</returns>
        public static ActionType GetCurrentAction(uint oObject = NWScriptService.OBJECT_INVALID) => _service.GetCurrentAction(oObject);

        /// <summary>
        /// Sets the active game pause state.
        /// </summary>
        /// <param name="bState">The pause state to set</param>
        /// <remarks>Same as if the player requested pause.</remarks>
        public static void SetGameActivePause(bool bState) => _service.SetGameActivePause(bState);

        /// <summary>
        /// Returns a value greater than 0 if the game is currently paused.
        /// </summary>
        /// <returns>0: Game is not paused, 1: Timestop, 2: Active Player Pause (optionally on top of timestop)</returns>
        public static int GetGamePauseState() => _service.GetGamePauseState();

        /// <summary>
        /// Returns the current game tick rate.
        /// </summary>
        /// <returns>The current game tick rate (mainloop iterations per second)</returns>
        /// <remarks>This is equivalent to graphics frames per second when the module is running inside a client.</remarks>
        public static int GetTickRate() => _service.GetTickRate();

        /// <summary>
        /// Gets the experience assigned in the journal editor for the specified plot ID.
        /// </summary>
        /// <param name="szPlotID">The plot ID to get the experience for</param>
        /// <returns>The experience assigned for the plot ID</returns>
        public static int GetJournalQuestExperience(string szPlotID) => _service.GetJournalQuestExperience(szPlotID);

        /// <summary>
        /// Returns the 32-bit integer hash of the specified string.
        /// </summary>
        /// <param name="sString">The string to hash</param>
        /// <returns>The 32-bit integer hash of the string</returns>
        /// <remarks>This hash is stable and will always have the same value for same input string, regardless of platform. The hash algorithm is the same as the one used internally for strings in case statements, so you can do: switch (HashString(sString)) { case "AAA": HandleAAA(); break; case "BBB": HandleBBB(); break; } The exact algorithm used is XXH32(sString) ^ XXH32(""). This means that HashString("") is 0.</remarks>
        public static int HashString(string sString) => _service.HashString(sString);

        public static int GetMicrosecondCounter() => _service.GetMicrosecondCounter();

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
            string sWaypointTag = "", bool bSeamless = false) => _service.ActivatePortal(oTarget, sIPaddress, sPassword, sWaypointTag, bSeamless);

        /// <summary>
        /// Sets a new tag for the object.
        /// Will do nothing for invalid objects or the module object.
        /// Note: Care needs to be taken with this function.
        /// Changing the tag for creature with waypoints will make them stop walking them.
        /// Changing waypoint, door or trigger tags will break their area transitions.
        /// </summary>
        /// <param name="oObject">The object to set the tag for</param>
        /// <param name="sNewTag">The new tag to set</param>
        public static void SetTag(uint oObject, string sNewTag) => _service.SetTag(oObject, sNewTag);

        /// <summary>
        /// Gets the last object that default clicked (left clicked) on the specified placeable object.
        /// Should only be called from a placeable's OnClick event.
        /// </summary>
        /// <returns>The last object that clicked, or OBJECT_INVALID if called by something other than a placeable</returns>
        public static uint GetPlaceableLastClickedBy() => _service.GetPlaceableLastClickedBy();

        /// <summary>
        /// Sets the name of the object.
        /// Note: SetName() does not work on player objects.
        /// Setting an object's name to empty string will make the object
        /// revert to using the name it had originally before any
        /// SetName() calls were made on the object.
        /// </summary>
        /// <param name="oObject">The object for which you are changing the name (a creature, placeable, item, or door)</param>
        /// <param name="sNewName">The new name that the object will use (defaults to empty string)</param>
        public static void SetName(uint oObject, string sNewName = "") => _service.SetName(oObject, sNewName);

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
        public static int GetPortraitId(uint oTarget = NWScriptService.OBJECT_INVALID) => _service.GetPortraitId(oTarget);

        /// <summary>
        /// Changes the portrait of the target to use the Portrait Id specified.
        /// nPortraitId refers to a row in the Portraits.2da
        /// Note: Not all portrait Ids are suitable for use with all object types.
        /// Setting the portrait Id will also cause the portrait ResRef
        /// to be set to the appropriate portrait ResRef for the Id specified.
        /// </summary>
        /// <param name="oTarget">The object for which you are changing the portrait</param>
        /// <param name="nPortraitId">The Id of the new portrait to use</param>
        public static void SetPortraitId(uint oTarget, int nPortraitId) => _service.SetPortraitId(oTarget, nPortraitId);

        /// <summary>
        /// Gets the Portrait ResRef of the target.
        /// The Portrait ResRef will not include a trailing size letter.
        /// </summary>
        /// <param name="oTarget">The object for which you are getting the portrait ResRef (defaults to OBJECT_SELF)</param>
        /// <returns>The Portrait ResRef being used for the object</returns>
        public static string GetPortraitResRef(uint oTarget = NWScriptService.OBJECT_INVALID) => _service.GetPortraitResRef(oTarget);

        /// <summary>
        /// Use this in a trigger's OnClick event script to get the object that last clicked on it.
        /// This is identical to GetEnteringObject.
        /// GetClickingObject() should not be called from a placeable's OnClick event,
        /// instead use GetPlaceableLastClickedBy().
        /// </summary>
        /// <returns>The object that last clicked on the trigger</returns>
        public static uint GetClickingObject() => _service.GetClickingObject();

        /// <summary>
        /// Gets the last object that disarmed the trap on the specified object.
        /// </summary>
        /// <returns>The last object that disarmed the trap, or OBJECT_INVALID if the caller is not a valid placeable, trigger or door</returns>
        public static uint GetLastDisarmed() => _service.GetLastDisarmed();

        /// <summary>
        /// Gets the last object that disturbed the inventory of the specified object.
        /// </summary>
        /// <returns>The last object that disturbed the inventory, or OBJECT_INVALID if the caller is not a valid creature or placeable</returns>
        public static uint GetLastDisturbed(uint oObject = NWScriptService.OBJECT_INVALID) => _service.GetLastDisturbed(oObject);

        /// <summary>
        /// Gets the last object that locked the specified object.
        /// </summary>
        /// <param name="oObject">The object to get the last locker for (defaults to OBJECT_SELF)</param>
        /// <returns>The last object that locked the specified object, or OBJECT_INVALID if the caller is not a valid door or placeable</returns>
        public static uint GetLastLocked(uint oObject = NWScriptService.OBJECT_INVALID) => _service.GetLastLocked(oObject);

        /// <summary>
        /// Gets the last object that unlocked the specified object.
        /// </summary>
        /// <param name="oObject">The object to get the last unlocker for (defaults to OBJECT_SELF)</param>
        /// <returns>The last object that unlocked the specified object, or OBJECT_INVALID if the caller is not a valid door or placeable</returns>
        public static uint GetLastUnlocked(uint oObject = NWScriptService.OBJECT_INVALID) => _service.GetLastUnlocked(oObject);

        /// <summary>
        /// Changes the portrait of the target to use the Portrait ResRef specified.
        /// The ResRef should not include any trailing size letter (e.g. po_el_f_09_).
        /// Note: Not all portrait ResRefs are suitable for use with all object types.
        /// Setting the portrait ResRef will also cause the portrait Id to be set to PORTRAIT_INVALID.
        /// </summary>
        /// <param name="oTarget">The object for which you are changing the portrait</param>
        /// <param name="sPortraitResRef">The ResRef of the new portrait to use</param>
        public static void SetPortraitResRef(uint oTarget, string sPortraitResRef) => _service.SetPortraitResRef(oTarget, sPortraitResRef);

        /// <summary>
        /// Sets the target's useable object status.
        /// Note: Only works on non-placeables, creatures, doors and items.
        /// On items, it affects interactivity when they're on the ground, and not useability in inventory.
        /// </summary>
        /// <param name="oPlaceable">The placeable object to set the useable flag for</param>
        /// <param name="nUseable">Whether the object is useable</param>
        public static void SetUseableFlag(uint oPlaceable, bool nUseable) => _service.SetUseableFlag(oPlaceable, nUseable);

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
            bool bIdentifiedDescription = true) => _service.GetDescription(oObject, bOriginalDescription, bIdentifiedDescription);

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
        public static void SetDescription(uint oObject, string sNewDescription = "", bool bIdentifiedDescription = true) => _service.SetDescription(oObject, sNewDescription, bIdentifiedDescription);

        /// <summary>
        /// Gets the color of the object from the color channel specified.
        /// Can be a creature that has color information (i.e. the playable races).
        /// </summary>
        /// <param name="oObject">The object from which you are obtaining the color</param>
        /// <param name="nColorChannel">The color channel that you want to get the color value of (COLOR_CHANNEL_SKIN, COLOR_CHANNEL_HAIR, COLOR_CHANNEL_TATTOO_1, COLOR_CHANNEL_TATTOO_2)</param>
        /// <returns>The color value, or -1 on error</returns>
        public static int GetColor(uint oObject, ColorChannelType nColorChannel) => _service.GetColor(oObject, nColorChannel);

        /// <summary>
        /// Sets the color channel of the object to the color specified.
        /// Can be a creature that has color information (i.e. the playable races).
        /// </summary>
        /// <param name="oObject">The object for which you are changing the color</param>
        /// <param name="nColorChannel">The color channel that you want to set the color value of (COLOR_CHANNEL_SKIN, COLOR_CHANNEL_HAIR, COLOR_CHANNEL_TATTOO_1, COLOR_CHANNEL_TATTOO_2)</param>
        /// <param name="nColorValue">The color you want to set (0-175)</param>
        public static void SetColor(uint oObject, ColorChannelType nColorChannel, int nColorValue) => _service.SetColor(oObject, nColorChannel, nColorValue);

        /// <summary>
        /// Gets the feedback message that will be displayed when trying to unlock the object.
        /// </summary>
        /// <param name="oObject">A door or placeable</param>
        /// <returns>The feedback message, or empty string on an error or if the game's default feedback message is being used</returns>
        public static string GetKeyRequiredFeedback(uint oObject) => _service.GetKeyRequiredFeedback(oObject);

        /// <summary>
        /// Sets the feedback message that is displayed when trying to unlock the object.
        /// This will only have an effect if the object is set to
        /// "Key required to unlock or lock" either in the toolset
        /// or by using the scripting command SetLockKeyRequired().
        /// To use the game's default message, set sFeedbackMessage to empty string.
        /// </summary>
        /// <param name="oObject">A door or placeable</param>
        /// <param name="sFeedbackMessage">The string to be displayed in the player's text window</param>
        public static void SetKeyRequiredFeedback(uint oObject, string sFeedbackMessage) => _service.SetKeyRequiredFeedback(oObject, sFeedbackMessage);

        /// <summary>
        /// Locks the player's camera pitch to its current pitch setting,
        /// or unlocks the player's camera pitch.
        /// Stops the player from tilting their camera angle.
        /// </summary>
        /// <param name="oPlayer">A player object</param>
        /// <param name="bLocked">TRUE/FALSE (defaults to TRUE)</param>
        public static void LockCameraPitch(uint oPlayer, bool bLocked = true) => _service.LockCameraPitch(oPlayer, bLocked);

        /// <summary>
        /// Locks the player's camera distance to its current distance setting,
        /// or unlocks the player's camera distance.
        /// Stops the player from being able to zoom in/out the camera.
        /// </summary>
        /// <param name="oPlayer">A player object</param>
        /// <param name="bLocked">TRUE/FALSE (defaults to TRUE)</param>
        public static void LockCameraDistance(uint oPlayer, bool bLocked = true) => _service.LockCameraDistance(oPlayer, bLocked);

        /// <summary>
        /// Locks the player's camera direction to its current direction,
        /// or unlocks the player's camera direction to enable it to move freely again.
        /// Stops the player from being able to rotate the camera direction.
        /// </summary>
        /// <param name="oPlayer">A player object</param>
        /// <param name="bLocked">TRUE/FALSE (defaults to TRUE)</param>
        public static void LockCameraDirection(uint oPlayer, bool bLocked = true) => _service.LockCameraDirection(oPlayer, bLocked);

        /// <summary>
        /// Returns the hardness of a door or placeable object.
        /// </summary>
        /// <param name="oObject">A door or placeable object (defaults to OBJECT_INVALID)</param>
        /// <returns>The hardness value, or -1 on an error or if used on an object that is neither a door nor a placeable object</returns>
        public static int GetHardness(uint oObject = NWScriptService.OBJECT_INVALID) => _service.GetHardness(oObject);

        /// <summary>
        /// Sets the hardness of a door or placeable object.
        /// Does nothing if used on an object that is neither a door nor a placeable.
        /// </summary>
        /// <param name="nHardness">Must be between 0 and 250</param>
        /// <param name="oObject">A door or placeable object (defaults to OBJECT_INVALID)</param>
        public static void SetHardness(int nHardness, uint oObject = NWScriptService.OBJECT_INVALID) => _service.SetHardness(nHardness, oObject);

        /// <summary>
        /// When set, the object cannot be opened unless the opener possesses the required key.
        /// The key tag required can be specified either in the toolset, or by using the SetLockKeyTag() scripting command.
        /// </summary>
        /// <param name="oObject">A door or placeable</param>
        /// <param name="nKeyRequired">TRUE/FALSE (defaults to TRUE)</param>
        public static void SetLockKeyRequired(uint oObject, bool nKeyRequired = true) => _service.SetLockKeyRequired(oObject, nKeyRequired);

        /// <summary>
        /// Sets the key tag required to open the object.
        /// This will only have an effect if the object is set to
        /// "Key required to unlock or lock" either in the toolset
        /// or by using the scripting command SetLockKeyRequired().
        /// </summary>
        /// <param name="oObject">A door, placeable or trigger</param>
        /// <param name="sNewKeyTag">The key tag required to open the locked object</param>
        public static void SetLockKeyTag(uint oObject, string sNewKeyTag) => _service.SetLockKeyTag(oObject, sNewKeyTag);

        /// <summary>
        /// Sets whether or not the object can be locked.
        /// </summary>
        /// <param name="oObject">A door or placeable</param>
        /// <param name="nLockable">TRUE/FALSE (defaults to TRUE)</param>
        public static void SetLockLockable(uint oObject, bool nLockable = true) => _service.SetLockLockable(oObject, nLockable);

        /// <summary>
        /// Sets the DC for unlocking the object.
        /// </summary>
        /// <param name="oObject">A door or placeable object</param>
        /// <param name="nNewUnlockDC">Must be between 0 and 250</param>
        public static void SetLockUnlockDC(uint oObject, int nNewUnlockDC) => _service.SetLockUnlockDC(oObject, nNewUnlockDC);

        /// <summary>
        /// Sets the DC for locking the object.
        /// </summary>
        /// <param name="oObject">A door or placeable object</param>
        /// <param name="nNewLockDC">Must be between 0 and 250</param>
        public static void SetLockLockDC(uint oObject, int nNewLockDC) => _service.SetLockLockDC(oObject, nNewLockDC);

        /// <summary>
        /// Sets the Will saving throw value of the door or placeable object.
        /// </summary>
        /// <param name="oObject">A door or placeable object</param>
        /// <param name="nWillSave">Must be between 0 and 250</param>
        public static void SetWillSavingThrow(uint oObject, int nWillSave) => _service.SetWillSavingThrow(oObject, nWillSave);

        /// <summary>
        /// Sets the Reflex saving throw value of the door or placeable object.
        /// </summary>
        /// <param name="oObject">A door or placeable object</param>
        /// <param name="nReflexSave">Must be between 0 and 250</param>
        public static void SetReflexSavingThrow(uint oObject, int nReflexSave) => _service.SetReflexSavingThrow(oObject, nReflexSave);

        /// <summary>
        /// Sets the Fortitude saving throw value of the door or placeable object.
        /// </summary>
        /// <param name="oObject">A door or placeable object</param>
        /// <param name="nFortitudeSave">Must be between 0 and 250</param>
        public static void SetFortitudeSavingThrow(uint oObject, int nFortitudeSave) => _service.SetFortitudeSavingThrow(oObject, nFortitudeSave);

        /// <summary>
        /// Gets the weight of an item, or the total carried weight of a creature in tenths
        /// of pounds (as per the baseitems.2da).
        /// </summary>
        /// <param name="oTarget">The item or creature for which the weight is needed (defaults to OBJECT_SELF)</param>
        /// <returns>The weight in tenths of pounds</returns>
        public static int GetWeight(uint oTarget = NWScriptService.OBJECT_INVALID) => _service.GetWeight(oTarget);

        /// <summary>
        /// Gets the object that acquired the module item. May be a creature, item, or placeable.
        /// </summary>
        /// <returns>The object that acquired the module item</returns>
        public static uint GetModuleItemAcquiredBy() => _service.GetModuleItemAcquiredBy();

        /// <summary>
        /// Causes the object to instantly speak a translated string.
        /// (not an action, not blocked when uncommandable)
        /// </summary>
        /// <param name="nStrRef">Reference of the string in the talk table</param>
        /// <param name="nTalkVolume">TALKVOLUME_* constant (defaults to TalkVolume.Talk)</param>
        public static void SpeakStringByStrRef(int nStrRef, TalkVolumeType nTalkVolume = TalkVolumeType.Talk) => _service.SpeakStringByStrRef(nStrRef, nTalkVolume);

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
        public static uint CopyObject(uint oSource, Location locLocation, uint oOwner = NWScriptService.OBJECT_INVALID, string sNewTag = "", bool bCopyLocalState = false) => _service.CopyObject(oSource, locLocation, oOwner, sNewTag, bCopyLocalState);

        /// <summary>
        /// Returns the template used to create this object (if appropriate).
        /// </summary>
        /// <param name="oObject">The object to get the resref for</param>
        /// <returns>The template resref, or empty string when no template found</returns>
        public static string GetResRef(uint oObject) => _service.GetResRef(oObject);

        /// <summary>
        /// Determines whether the object has an inventory.
        /// Returns TRUE for creatures and stores, and checks to see if an item or placeable object is a container.
        /// Returns FALSE for all other object types.
        /// </summary>
        /// <param name="oObject">The object to check</param>
        /// <returns>TRUE if the object has an inventory, FALSE otherwise</returns>
        public static bool GetHasInventory(uint oObject) => _service.GetHasInventory(oObject);

        /// <summary>
        /// Gets the name of the creature's deity.
        /// </summary>
        /// <param name="oCreature">The creature to get the deity for</param>
        /// <returns>The deity name, or empty string if the creature is invalid or if the deity name is blank</returns>
        public static string GetDeity(uint oCreature) => _service.GetDeity(oCreature);

        /// <summary>
        /// Gets the name of the creature's sub race.
        /// </summary>
        /// <param name="oTarget">The creature to get the sub race for</param>
        /// <returns>The sub race name, or empty string if the creature is invalid or if sub race is blank</returns>
        public static string GetSubRace(uint oTarget) => _service.GetSubRace(oTarget);

        /// <summary>
        /// Gets the target's base fortitude saving throw value (this will only work for
        /// creatures, doors, and placeables).
        /// </summary>
        /// <param name="oTarget">The target to get the fortitude saving throw for</param>
        /// <returns>The fortitude saving throw value, or 0 if the target is invalid</returns>
        public static int GetFortitudeSavingThrow(uint oTarget) => _service.GetFortitudeSavingThrow(oTarget);

        /// <summary>
        /// Gets the target's base will saving throw value (this will only work for creatures,
        /// doors, and placeables).
        /// </summary>
        /// <param name="oTarget">The target to get the will saving throw for</param>
        /// <returns>The will saving throw value, or 0 if the target is invalid</returns>
        public static int GetWillSavingThrow(uint oTarget) => _service.GetWillSavingThrow(oTarget);

        /// <summary>
        /// Gets the target's base reflex saving throw value (this will only work for
        /// creatures, doors, and placeables).
        /// </summary>
        /// <param name="oTarget">The target to get the reflex saving throw for</param>
        /// <returns>The reflex saving throw value, or 0 if the target is invalid</returns>
        public static int GetReflexSavingThrow(uint oTarget) => _service.GetReflexSavingThrow(oTarget);

        /// <summary>
        /// Gets the creature's challenge rating.
        /// </summary>
        /// <param name="oCreature">The creature to get the challenge rating for</param>
        /// <returns>The challenge rating, or 0.0 if the creature is invalid</returns>
        public static float GetChallengeRating(uint oCreature) => _service.GetChallengeRating(oCreature);

        /// <summary>
        /// Gets the creature's age.
        /// </summary>
        /// <param name="oCreature">The creature to get the age for</param>
        /// <returns>The age, or 0 if the creature is invalid</returns>
        public static int GetAge(uint oCreature) => _service.GetAge(oCreature);

        /// <summary>
        /// Gets the creature's movement rate.
        /// </summary>
        /// <param name="oCreature">The creature to get the movement rate for</param>
        /// <returns>The movement rate, or 0 if the creature is invalid</returns>
        public static int GetMovementRate(uint oCreature) => _service.GetMovementRate(oCreature);

        /// <summary>
        /// Determines whether the target is a plot object.
        /// </summary>
        /// <param name="oTarget">The target to check (defaults to OBJECT_INVALID)</param>
        /// <returns>TRUE if the target is a plot object</returns>
        public static bool GetPlotFlag(uint oTarget = NWScriptService.OBJECT_INVALID) => _service.GetPlotFlag(oTarget);

        /// <summary>
        /// Sets the target's plot object status.
        /// </summary>
        /// <param name="oTarget">The target to set the plot flag for</param>
        /// <param name="nPlotFlag">The plot flag status</param>
        public static void SetPlotFlag(uint oTarget, bool nPlotFlag) => _service.SetPlotFlag(oTarget, nPlotFlag);

        /// <summary>
        /// Plays a voice chat.
        /// </summary>
        /// <param name="nVoiceChatID">VOICE_CHAT_* constant</param>
        /// <param name="oTarget">The target (defaults to OBJECT_INVALID)</param>
        public static void PlayVoiceChat(VoiceChatType nVoiceChatID, uint oTarget = NWScriptService.OBJECT_INVALID) => _service.PlayVoiceChat(nVoiceChatID, oTarget);

        /// <summary>
        /// Gets the amount of gold possessed by the target.
        /// </summary>
        /// <param name="oTarget">The target to get the gold for (defaults to OBJECT_SELF)</param>
        /// <returns>The amount of gold</returns>
        public static int GetGold(uint oTarget = NWScriptService.OBJECT_INVALID) => _service.GetGold(oTarget);

        /// <summary>
        /// Plays the sound object.
        /// </summary>
        /// <param name="oSound">The sound object to play</param>
        public static void SoundObjectPlay(uint oSound) => _service.SoundObjectPlay(oSound);

        /// <summary>
        /// Stops playing the sound object.
        /// </summary>
        /// <param name="oSound">The sound object to stop</param>
        public static void SoundObjectStop(uint oSound) => _service.SoundObjectStop(oSound);

        /// <summary>
        /// Sets the volume of the sound object.
        /// </summary>
        /// <param name="oSound">The sound object</param>
        /// <param name="nVolume">Volume level (0-127)</param>
        public static void SoundObjectSetVolume(uint oSound, int nVolume) => _service.SoundObjectSetVolume(oSound, nVolume);

        /// <summary>
        /// Sets the position of the sound object.
        /// </summary>
        /// <param name="oSound">The sound object</param>
        /// <param name="vPosition">The position to set</param>
        public static void SoundObjectSetPosition(uint oSound, Vector3 vPosition) => _service.SoundObjectSetPosition(oSound, vPosition);

        /// <summary>
        /// Immediately speaks a conversation one-liner.
        /// </summary>
        /// <param name="sDialogResRef">The dialog resref (defaults to empty string)</param>
        /// <param name="oTokenTarget">This must be specified if there are creature-specific tokens in the string (defaults to OBJECT_INVALID)</param>
        public static void SpeakOneLinerConversation(string sDialogResRef = "", uint oTokenTarget = NWScriptService.OBJECT_INVALID) => _service.SpeakOneLinerConversation(sDialogResRef, oTokenTarget);

        /// <summary>
        /// Sets the destroyable status of the target object.
        /// </summary>
        /// <param name="bDestroyable">If FALSE, the target does not fade out on death, but sticks around as a corpse (defaults to true)</param>
        /// <param name="bRaiseable">If TRUE, the target can be raised via resurrection (defaults to true)</param>
        /// <param name="bSelectableWhenDead">If TRUE, the target is selectable after death (defaults to false)</param>
        /// <param name="oObject">The target object (defaults to OBJECT_SELF)</param>
        public static void SetIsDestroyable(bool bDestroyable = true, bool bRaiseable = true,
            bool bSelectableWhenDead = false, uint oObject = NWScriptService.OBJECT_INVALID) => _service.SetIsDestroyable(bDestroyable, bRaiseable, bSelectableWhenDead, oObject);

        /// <summary>
        /// Sets the locked state of the target, which can be a door or a placeable object.
        /// </summary>
        /// <param name="oTarget">The door or placeable object</param>
        /// <param name="nLocked">The locked state</param>
        public static void SetLocked(uint oTarget, bool nLocked) => _service.SetLocked(oTarget, nLocked);

        /// <summary>
        /// Gets the locked state of the target, which can be a door or a placeable object.
        /// </summary>
        /// <param name="oTarget">The door or placeable object</param>
        /// <returns>The locked state</returns>
        public static bool GetLocked(uint oTarget) => _service.GetLocked(oTarget);

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
            bool nUseAppearAnimation = false, string sNewTag = "") => _service.CreateObject(nObjectType, sTemplate, lLocation, nUseAppearAnimation, sNewTag);

        /// <summary>
        /// Gets the nth object nearest to the target that is of the specified type.
        /// </summary>
        /// <param name="nObjectType">OBJECT_TYPE_* constant (defaults to ObjectType.All)</param>
        /// <param name="oTarget">The target object (defaults to OBJECT_SELF)</param>
        /// <param name="nNth">The nth object to find (defaults to 1)</param>
        /// <returns>The nearest object, or OBJECT_INVALID on error</returns>
        public static uint GetNearestObject(ObjectType nObjectType = ObjectType.All, uint oTarget = NWScriptService.OBJECT_INVALID, int nNth = 1) => _service.GetNearestObject(nObjectType, oTarget, nNth);

        /// <summary>
        /// Gets the nth object nearest to the location that is of the specified type.
        /// </summary>
        /// <param name="lLocation">The location to search from</param>
        /// <param name="nObjectType">OBJECT_TYPE_* constant (defaults to ObjectType.All)</param>
        /// <param name="nNth">The nth object to find (defaults to 1)</param>
        /// <returns>The nearest object, or OBJECT_INVALID on error</returns>
        public static uint GetNearestObjectToLocation(Location lLocation, ObjectType nObjectType = ObjectType.All,
            int nNth = 1) => _service.GetNearestObjectToLocation(lLocation, nObjectType, nNth);

        /// <summary>
        /// Gets the nth object nearest to the target that has the specified tag.
        /// </summary>
        /// <param name="sTag">The tag to search for</param>
        /// <param name="oTarget">The target object (defaults to OBJECT_SELF)</param>
        /// <param name="nNth">The nth object to find (defaults to 1)</param>
        /// <returns>The nearest object with the tag, or OBJECT_INVALID on error</returns>
        public static uint GetNearestObjectByTag(string sTag, uint oTarget = NWScriptService.OBJECT_INVALID, int nNth = 1) => _service.GetNearestObjectByTag(sTag, oTarget, nNth);

        /// <summary>
        /// If the object is a creature, this will return that creature's armour class.
        /// If the object is an item, door or placeable, this will return zero.
        /// </summary>
        /// <param name="oObject">The object to get the AC for</param>
        /// <returns>The armour class, or -1 if the object is not a creature, item, door or placeable</returns>
        public static int GetAC(uint oObject) => _service.GetAC(oObject);

        /// <summary>
        /// Gets the object type of the target.
        /// </summary>
        /// <param name="oTarget">The target object</param>
        /// <returns>The object type (OBJECT_TYPE_*), or -1 if the target is not a valid object</returns>
        public static ObjectType GetObjectType(uint oTarget) => _service.GetObjectType(oTarget);

        /// <summary>
        /// Gets the current hitpoints of the object.
        /// </summary>
        /// <param name="oObject">The object to get hitpoints for (defaults to OBJECT_SELF)</param>
        /// <returns>The current hitpoints, or 0 on error</returns>
        public static int GetCurrentHitPoints(uint oObject = NWScriptService.OBJECT_INVALID) => _service.GetCurrentHitPoints(oObject);

        /// <summary>
        /// Gets the maximum hitpoints of the object.
        /// </summary>
        /// <param name="oObject">The object to get max hitpoints for (defaults to OBJECT_SELF)</param>
        /// <returns>The maximum hitpoints, or 0 on error</returns>
        public static int GetMaxHitPoints(uint oObject = NWScriptService.OBJECT_INVALID) => _service.GetMaxHitPoints(oObject);

        /// <summary>
        /// Returns TRUE if the object is a valid object.
        /// </summary>
        /// <param name="oObject">The object to check</param>
        /// <returns>TRUE if the object is valid</returns>
        public static bool GetIsObjectValid(uint oObject) => _service.GetIsObjectValid(oObject);

        /// <summary>
        /// Converts a string containing a hexadecimal object id into an object reference.
        /// Counterpart to StringToObject().
        /// </summary>
        /// <param name="sHex">The hexadecimal string</param>
        /// <returns>The object reference</returns>
        public static uint StringToObject(string sHex) => _service.StringToObject(sHex);

        /// <summary>
        /// Replaces the object's texture sOld with sNew.
        /// Specifying sNew = empty string will restore the original texture.
        /// If sNew cannot be found, the original texture will be restored.
        /// sNew must refer to a simple texture, not PLT.
        /// </summary>
        /// <param name="oObject">The object to replace the texture for</param>
        /// <param name="sOld">The old texture name</param>
        /// <param name="sNew">The new texture name (defaults to empty string)</param>
        public static void ReplaceObjectTexture(uint oObject, string sOld, string sNew = "") => _service.ReplaceObjectTexture(oObject, sOld, sNew);

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
        public static void SetCurrentHitPoints(uint oObject, int nHitPoints) => _service.SetCurrentHitPoints(oObject, nHitPoints);

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
        public static uint GetFirstObjectInShape(ShapeType nShape, float fSize, Location lTarget, bool bLineOfSight = false,
            ObjectType nObjectFilter = ObjectType.Creature, Vector3 vOrigin = default) => _service.GetFirstObjectInShape(nShape, fSize, lTarget, bLineOfSight, nObjectFilter, vOrigin);

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
        public static uint GetNextObjectInShape(ShapeType nShape, float fSize, Location lTarget, bool bLineOfSight = false,
            ObjectType nObjectFilter = ObjectType.Creature, Vector3 vOrigin = default) => _service.GetNextObjectInShape(nShape, fSize, lTarget, bLineOfSight, nObjectFilter, vOrigin);

        /// <summary>
        /// Causes the target object to face the target point.
        /// </summary>
        /// <param name="vTarget">The target point to face</param>
        /// <param name="oObject">The target object (defaults to OBJECT_SELF)</param>
        public static void SetFacingPoint(Vector3 vTarget, uint oObject = NWScriptService.OBJECT_INVALID) => _service.SetFacingPoint(vTarget, oObject);

        /// <summary>
        /// Gets the distance in metres between the two objects.
        /// </summary>
        /// <param name="oObjectA">The first object</param>
        /// <param name="oObjectB">The second object</param>
        /// <returns>The distance in metres, or 0.0f if either object is invalid</returns>
        public static float GetDistanceBetween(uint oObjectA, uint oObjectB) => _service.GetDistanceBetween(oObjectA, oObjectB);

        /// <summary>
        /// Sets whether the target's action stack can be modified.
        /// </summary>
        /// <param name="nCommandable">Whether the target is commandable</param>
        /// <param name="oTarget">The target object (defaults to OBJECT_SELF)</param>
        public static void SetCommandable(bool nCommandable, uint oTarget = NWScriptService.OBJECT_INVALID) => _service.SetCommandable(nCommandable, oTarget);

        /// <summary>
        /// Determines whether the target's action stack can be modified.
        /// </summary>
        /// <param name="oTarget">The target object (defaults to OBJECT_SELF)</param>
        /// <returns>TRUE if the target is commandable</returns>
        public static bool GetCommandable(uint oTarget = NWScriptService.OBJECT_INVALID) => _service.GetCommandable(oTarget);

        /// <summary>
        /// Gets the tag of the object.
        /// </summary>
        /// <param name="oObject">The object to get the tag for</param>
        /// <returns>The tag, or empty string if the object is not a valid object</returns>
        public static string GetTag(uint oObject) => _service.GetTag(oObject);

        /// <summary>
        /// Returns TRUE if the object is listening for something.
        /// </summary>
        /// <param name="oObject">The object to check</param>
        /// <returns>TRUE if the object is listening</returns>
        public static bool GetIsListening(uint oObject) => _service.GetIsListening(oObject);

        /// <summary>
        /// Sets whether the object is listening.
        /// </summary>
        /// <param name="oObject">The object to set listening for</param>
        /// <param name="bValue">Whether the object is listening</param>
        public static void SetListening(uint oObject, bool bValue) => _service.SetListening(oObject, bValue);

        /// <summary>
        /// Sets the string for the object to listen for.
        /// Note: this does not set the object to be listening.
        /// </summary>
        /// <param name="oObject">The object to set the listen pattern for</param>
        /// <param name="sPattern">The pattern to listen for</param>
        /// <param name="nNumber">The pattern number (defaults to 0)</param>
        public static void SetListenPattern(uint oObject, string sPattern, int nNumber = 0) => _service.SetListenPattern(oObject, sPattern, nNumber);

        /// <summary>
        /// In an onConversation script this gets the number of the string pattern
        /// matched (the one that triggered the script).
        /// </summary>
        /// <returns>The pattern number, or -1 if no string matched</returns>
        public static int GetListenPatternNumber() => _service.GetListenPatternNumber();

        /// <summary>
        /// Gets the first waypoint with the specified tag.
        /// </summary>
        /// <param name="sWaypointTag">The waypoint tag to search for</param>
        /// <returns>The waypoint, or OBJECT_INVALID if the waypoint cannot be found</returns>
        public static uint GetWaypointByTag(string sWaypointTag) => _service.GetWaypointByTag(sWaypointTag);

        /// <summary>
        /// Gets the destination object for the given object.
        /// All objects can hold a transition target, but only Doors and Triggers
        /// will be made clickable by the game engine (This may change in the
        /// future). You can set and query transition targets on other objects for
        /// your own scripted purposes.
        /// </summary>
        /// <param name="oTransition">The transition object</param>
        /// <returns>The destination object, or OBJECT_INVALID if the transition does not hold a target</returns>
        public static uint GetTransitionTarget(uint oTransition) => _service.GetTransitionTarget(oTransition);

        /// <summary>
        /// Gets the nth object with the specified tag.
        /// Note: The module cannot be retrieved by GetObjectByTag(), use GetModule() instead.
        /// </summary>
        /// <param name="sTag">The tag to search for</param>
        /// <param name="nNth">The nth object with this tag may be requested (defaults to 0)</param>
        /// <returns>The object, or OBJECT_INVALID if the object cannot be found</returns>
        public static uint GetObjectByTag(string sTag, int nNth = 0) => _service.GetObjectByTag(sTag, nNth);

        /// <summary>
        /// Gets the creature that is currently sitting on the specified object.
        /// </summary>
        /// <param name="oChair">The chair object</param>
        /// <returns>The sitting creature, or OBJECT_INVALID if the chair is not a valid placeable</returns>
        public static uint GetSittingCreature(uint oChair) => _service.GetSittingCreature(oChair);

        /// <summary>
        /// The caller will immediately speak the string (this is different from ActionSpeakString).
        /// </summary>
        /// <param name="sStringToSpeak">The string to speak</param>
        /// <param name="nTalkVolume">TALKVOLUME_* constant (defaults to TalkVolume.Talk)</param>
        public static void SpeakString(string sStringToSpeak, TalkVolumeType nTalkVolume = TalkVolumeType.Talk) => _service.SpeakString(sStringToSpeak, nTalkVolume);

        /// <summary>
        /// Gets the name of the object.
        /// </summary>
        /// <param name="oObject">The object from which you are obtaining the name (area, creature, placeable, item, or door)</param>
        /// <param name="bOriginalName">If set to true returns the name that the object had when the module was loaded (i.e. the original name) (defaults to false)</param>
        /// <returns>The name, or empty string on error</returns>
        public static string GetName(uint oObject, bool bOriginalName = false) => _service.GetName(oObject, bOriginalName);

        /// <summary>
        /// Converts the object into a hexadecimal string.
        /// </summary>
        /// <param name="oObject">The object to convert</param>
        /// <returns>The hexadecimal string representation of the object</returns>
        public static string ObjectToString(uint oObject) => _service.ObjectToString(oObject);

        /// <summary>
        /// Gets the area that the target is currently in.
        /// </summary>
        /// <param name="oTarget">The target to get the area for</param>
        /// <returns>The area object. Returns OBJECT_INVALID on error</returns>
        public static uint GetArea(uint oTarget) => _service.GetArea(oTarget);

        /// <summary>
        /// Gets the object that last entered the specified object.
        /// </summary>
        /// <returns>The entering object. Returns OBJECT_INVALID on error</returns>
        /// <remarks>The value returned depends on the object type of the caller: 1) If the caller is a door, it returns the object that last triggered it. 2) If the caller is a trigger, area of effect, module, area or encounter, it returns the object that last entered it. When used for doors, this should only be called from the OnAreaTransitionClick event. Otherwise, it should only be called in OnEnter scripts.</remarks>
        public static uint GetEnteringObject() => _service.GetEnteringObject();

        /// <summary>
        /// Gets the object that last left the specified object.
        /// </summary>
        /// <returns>The exiting object. Returns OBJECT_INVALID on error</returns>
        /// <remarks>This function works on triggers, areas of effect, modules, areas and encounters. Should only be called in OnExit scripts.</remarks>
        public static uint GetExitingObject() => _service.GetExitingObject();

        /// <summary>
        /// Gets the position of the target.
        /// </summary>
        /// <param name="oTarget">The target to get the position for</param>
        /// <returns>The position vector. Returns (0.0f, 0.0f, 0.0f) on error</returns>
        public static Vector3 GetPosition(uint oTarget) => _service.GetPosition(oTarget);

        /// <summary>
        /// Plays the background music for the specified area.
        /// </summary>
        /// <param name="oArea">The area to play background music for</param>
        public static void MusicBackgroundPlay(uint oArea) => _service.MusicBackgroundPlay(oArea);

        /// <summary>
        /// Stops the background music for the specified area.
        /// </summary>
        /// <param name="oArea">The area to stop background music for</param>
        public static void MusicBackgroundStop(uint oArea) => _service.MusicBackgroundStop(oArea);

        /// <summary>
        /// Sets the delay for the background music for the specified area.
        /// </summary>
        /// <param name="oArea">The area to set the music delay for</param>
        /// <param name="nDelay">The delay in milliseconds</param>
        public static void MusicBackgroundSetDelay(uint oArea, int nDelay) => _service.MusicBackgroundSetDelay(oArea, nDelay);

        /// <summary>
        /// Changes the background day track for the specified area.
        /// </summary>
        /// <param name="oArea">The area to change the day track for</param>
        /// <param name="nTrack">The track number to set</param>
        public static void MusicBackgroundChangeDay(uint oArea, int nTrack) => _service.MusicBackgroundChangeDay(oArea, nTrack);

        /// <summary>
        /// Changes the background night track for the specified area.
        /// </summary>
        /// <param name="oArea">The area to change the night track for</param>
        /// <param name="nTrack">The track number to set</param>
        public static void MusicBackgroundChangeNight(uint oArea, int nTrack) => _service.MusicBackgroundChangeNight(oArea, nTrack);

        /// <summary>
        /// Plays the battle music for the specified area.
        /// </summary>
        /// <param name="oArea">The area to play battle music for</param>
        public static void MusicBattlePlay(uint oArea) => _service.MusicBattlePlay(oArea);

        /// <summary>
        /// Stops the battle music for the specified area.
        /// </summary>
        /// <param name="oArea">The area to stop battle music for</param>
        public static void MusicBattleStop(uint oArea) => _service.MusicBattleStop(oArea);

        /// <summary>
        /// Changes the battle track for the specified area.
        /// </summary>
        /// <param name="oArea">The area to change the battle track for</param>
        /// <param name="nTrack">The track number to set</param>
        public static void MusicBattleChange(uint oArea, int nTrack) => _service.MusicBattleChange(oArea, nTrack);

        /// <summary>
        /// Plays the ambient sound for the specified area.
        /// </summary>
        /// <param name="oArea">The area to play ambient sound for</param>
        public static void AmbientSoundPlay(uint oArea) => _service.AmbientSoundPlay(oArea);

        /// <summary>
        /// Stops the ambient sound for the specified area.
        /// </summary>
        /// <param name="oArea">The area to stop ambient sound for</param>
        public static void AmbientSoundStop(uint oArea) => _service.AmbientSoundStop(oArea);

        /// <summary>
        /// Changes the ambient day track for the specified area.
        /// </summary>
        /// <param name="oArea">The area to change the ambient day track for</param>
        /// <param name="nTrack">The track number to set</param>
        public static void AmbientSoundChangeDay(uint oArea, int nTrack) => _service.AmbientSoundChangeDay(oArea, nTrack);

        /// <summary>
        /// Changes the ambient night track for the specified area.
        /// </summary>
        /// <param name="oArea">The area to change the ambient night track for</param>
        /// <param name="nTrack">The track number to set</param>
        public static void AmbientSoundChangeNight(uint oArea, int nTrack) => _service.AmbientSoundChangeNight(oArea, nTrack);

        /// <summary>
        /// Makes all clients in the area recompute the lighting.
        /// </summary>
        /// <param name="oArea">The area to recompute lighting for</param>
        /// <remarks>This can be used to update the lighting after changing any tile lights or if placeables with lights have been added/deleted.</remarks>
        public static void RecomputeStaticLighting(uint oArea) => _service.RecomputeStaticLighting(oArea);

        /// <summary>
        /// Gets the day track for the specified area.
        /// </summary>
        /// <param name="oArea">The area to get the day track for</param>
        /// <returns>The day track number</returns>
        public static int MusicBackgroundGetDayTrack(uint oArea) => _service.MusicBackgroundGetDayTrack(oArea);

        /// <summary>
        /// Gets the night track for the specified area.
        /// </summary>
        /// <param name="oArea">The area to get the night track for</param>
        /// <returns>The night track number</returns>
        public static int MusicBackgroundGetNightTrack(uint oArea) => _service.MusicBackgroundGetNightTrack(oArea);

        /// <summary>
        /// Sets the ambient day volume for the specified area.
        /// </summary>
        /// <param name="oArea">The area to set the ambient day volume for</param>
        /// <param name="nVolume">The volume level (0-100)</param>
        public static void AmbientSoundSetDayVolume(uint oArea, int nVolume) => _service.AmbientSoundSetDayVolume(oArea, nVolume);

        /// <summary>
        /// Sets the ambient night volume for the specified area.
        /// </summary>
        /// <param name="oArea">The area to set the ambient night volume for</param>
        /// <param name="nVolume">The volume level (0-100)</param>
        public static void AmbientSoundSetNightVolume(uint oArea, int nVolume) => _service.AmbientSoundSetNightVolume(oArea, nVolume);

        /// <summary>
        /// Gets the battle track for the specified area.
        /// </summary>
        /// <param name="oArea">The area to get the battle track for</param>
        /// <returns>The battle track number</returns>
        public static int MusicBackgroundGetBattleTrack(uint oArea) => _service.MusicBackgroundGetBattleTrack(oArea);

        /// <summary>
        /// Returns true if the area is flagged as either interior or underground.
        /// </summary>
        /// <param name="oArea">The area to check (default: OBJECT_SELF)</param>
        /// <returns>True if the area is interior or underground</returns>
        public static bool GetIsAreaInterior(uint oArea = NWScriptService.OBJECT_INVALID) => _service.GetIsAreaInterior(oArea);

        /// <summary>
        /// Gets the current weather conditions for the specified area.
        /// </summary>
        /// <param name="oArea">The area to get weather for</param>
        /// <returns>Weather conditions: WEATHER_CLEAR, WEATHER_RAIN, WEATHER_SNOW, or WEATHER_INVALID</returns>
        /// <remarks>If called on an interior area, this will always return WEATHER_CLEAR.</remarks>
        public static WeatherType GetWeather(uint oArea) => _service.GetWeather(oArea);

        /// <summary>
        /// Returns whether the area is natural or artificial.
        /// </summary>
        /// <param name="oArea">The area to check</param>
        /// <returns>AREA_NATURAL if the area is natural, AREA_ARTIFICIAL otherwise. Returns AREA_INVALID on error</returns>
        public static AreaNaturalType GetIsAreaNatural(uint oArea) => _service.GetIsAreaNatural(oArea);

        /// <summary>
        /// Returns whether the area is above ground or underground.
        /// </summary>
        /// <param name="oArea">The area to check</param>
        /// <returns>True if the area is above ground, false if underground</returns>
        public static bool GetIsAreaAboveGround(uint oArea) => _service.GetIsAreaAboveGround(oArea);

        /// <summary>
        /// Changes the sky that is displayed in the specified area.
        /// </summary>
        /// <param name="nSkyBox">The skybox to set (SKYBOX_* constants associated with skyboxes.2da)</param>
        /// <param name="oArea">The area to change the sky for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_SELF)</param>
        public static void SetSkyBox(SkyboxType nSkyBox, uint oArea = NWScriptService.OBJECT_INVALID) => _service.SetSkyBox(nSkyBox, oArea);

        /// <summary>
        /// Sets the fog color in the specified area.
        /// </summary>
        /// <param name="nFogType">Specifies whether the Sun, Moon, or both fog types are set (FOG_TYPE_* constants)</param>
        /// <param name="nFogColor">The color to set the fog to (FOG_COLOR_* constants). Can also be represented as a hex RGB number (e.g., 0xFFEEDD where FF=red, EE=green, DD=blue)</param>
        /// <param name="oArea">The area to set fog color for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_SELF)</param>
        public static void SetFogColor(FogType nFogType, FogColorType nFogColor, uint oArea = NWScriptService.OBJECT_INVALID) => _service.SetFogColor(nFogType, nFogColor, oArea);

        /// <summary>
        /// Gets the skybox that is currently displayed in the specified area.
        /// </summary>
        /// <param name="oArea">The area to get the skybox for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_SELF)</param>
        /// <returns>The skybox constant (SKYBOX_*)</returns>
        public static SkyboxType GetSkyBox(uint oArea = NWScriptService.OBJECT_INVALID) => _service.GetSkyBox(oArea);

        /// <summary>
        /// Gets the fog color in the specified area.
        /// </summary>
        /// <param name="nFogType">Specifies whether the Sun or Moon fog type is returned. Valid values are FOG_TYPE_SUN or FOG_TYPE_MOON</param>
        /// <param name="oArea">The area to get fog color for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_SELF)</param>
        /// <returns>The fog color constant (FOG_COLOR_*)</returns>
        public static FogColorType GetFogColor(FogType nFogType, uint oArea = NWScriptService.OBJECT_INVALID) => _service.GetFogColor(nFogType, oArea);

        /// <summary>
        /// Sets the fog amount in the specified area.
        /// </summary>
        /// <param name="nFogType">Specifies whether the Sun, Moon, or both fog types are set (FOG_TYPE_* constants)</param>
        /// <param name="nFogAmount">The density that the fog is being set to</param>
        /// <param name="oArea">The area to set fog amount for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_SELF)</param>
        public static void SetFogAmount(FogType nFogType, int nFogAmount, uint oArea = NWScriptService.OBJECT_INVALID) => _service.SetFogAmount(nFogType, nFogAmount, oArea);

        /// <summary>
        /// Gets the fog amount in the specified area.
        /// </summary>
        /// <param name="nFogType">Specifies whether the Sun or Moon fog type is returned. Valid values are FOG_TYPE_SUN or FOG_TYPE_MOON</param>
        /// <param name="oArea">The area to get fog amount for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_SELF)</param>
        /// <returns>The fog amount</returns>
        public static int GetFogAmount(FogType nFogType, uint oArea = NWScriptService.OBJECT_INVALID) => _service.GetFogAmount(nFogType, oArea);

        /// <summary>
        /// Returns the resref of the tileset used to create the specified area.
        /// </summary>
        /// <param name="oArea">The area to get the tileset resref for</param>
        /// <returns>The tileset resref (TILESET_RESREF_* constant). Returns empty string on error</returns>
        /// <remarks>Possible values include: TILESET_RESREF_BEHOLDER_CAVES, TILESET_RESREF_CASTLE_INTERIOR, TILESET_RESREF_CITY_EXTERIOR, TILESET_RESREF_CITY_INTERIOR, TILESET_RESREF_CRYPT, TILESET_RESREF_DESERT, TILESET_RESREF_DROW_INTERIOR, TILESET_RESREF_DUNGEON, TILESET_RESREF_FOREST, TILESET_RESREF_FROZEN_WASTES, TILESET_RESREF_ILLITHID_INTERIOR, TILESET_RESREF_MICROSET, TILESET_RESREF_MINES_AND_CAVERNS, TILESET_RESREF_RUINS, TILESET_RESREF_RURAL, TILESET_RESREF_RURAL_WINTER, TILESET_RESREF_SEWERS, TILESET_RESREF_UNDERDARK</remarks>
        public static string GetTilesetResRef(uint oArea) => _service.GetTilesetResRef(oArea);

        /// <summary>
        /// Gets the size of the specified area.
        /// </summary>
        /// <param name="nAreaDimension">The area dimension to determine (AREA_HEIGHT or AREA_WIDTH)</param>
        /// <param name="oArea">The area to get the size for. If no valid area is specified, uses the area of the caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_SELF)</param>
        /// <returns>The number of tiles that the area is wide/high, or zero on error</returns>
        public static int GetAreaSize(AreaDimensionType nAreaDimension, uint oArea = NWScriptService.OBJECT_INVALID) => _service.GetAreaSize(nAreaDimension, oArea);

        /// <summary>
        /// Destroys the given area object and everything in it.
        /// </summary>
        /// <param name="oArea">The area to destroy</param>
        /// <returns>Return values: 0 = Object not an area or invalid, -1 = Area contains spawn location and removal would leave module without entrypoint, -2 = Players in area, 1 = Area destroyed successfully</returns>
        /// <remarks>If the area is in a module, the .are and .git data is left behind and you can spawn from it again. If the area is a temporary copy, the data will be deleted and you cannot spawn it again via the resref.</remarks>
        public static int DestroyArea(uint oArea) => _service.DestroyArea(oArea);

        /// <summary>
        /// Instances a new area from the given source resref, which needs to be an existing module area.
        /// </summary>
        /// <param name="sSourceResRef">The source resref of the area to instance</param>
        /// <param name="sNewTag">Optional new area tag (default: empty string)</param>
        /// <param name="sNewName">Optional new displayed name (default: empty string)</param>
        /// <returns>The new area, or OBJECT_INVALID on failure</returns>
        /// <remarks>The new area is accessible immediately, but initialization scripts for the area and all contained creatures will only run after the current script finishes (so you can clean up objects before returning). When spawning a second instance of an existing area, you will have to manually adjust all transitions (doors, triggers) with the relevant script commands, or players might end up in the wrong area. Areas cannot have duplicate ResRefs, so your new area will have an autogenerated, sequential resref starting with "nw_"; for example: nw_5. You cannot influence this resref. If you destroy an area, that resref will become free for reuse for the next area created. If you need to know the resref of your new area, you can call GetResRef on it. When instancing an area from a loaded savegame, it will spawn the area as it was at time of save, NOT at module creation. This is because the savegame replaces the module data. Due to technical limitations, polymorphed creatures, personal reputation, and associates will currently fail to restore correctly.</remarks>
        public static uint CreateArea(string sSourceResRef, string sNewTag = "", string sNewName = "") => _service.CreateArea(sSourceResRef, sNewTag, sNewName);

        /// <summary>
        /// Creates a copy of an existing area, including everything inside of it (except players).
        /// </summary>
        /// <param name="oArea">The area to copy</param>
        /// <returns>The new area, or OBJECT_INVALID on error</returns>
        /// <remarks>This is similar to CreateArea, except this variant will copy all changes made to the source area since it has spawned. CreateArea() will instance the area from the .are and .git data as it was at creation. The new area is accessible immediately, but initialization scripts for the area and all contained creatures will only run after the current script finishes (so you can clean up objects before returning). You will have to manually adjust all transitions (doors, triggers) with the relevant script commands, or players might end up in the wrong area. Areas cannot have duplicate ResRefs, so your new area will have an autogenerated, sequential resref starting with "nw_"; for example: nw_5. You cannot influence this resref. If you destroy an area, that resref will become free for reuse for the next area created. If you need to know the resref of your new area, you can call GetResRef on it.</remarks>
        public static uint CopyArea(uint oArea) => _service.CopyArea(oArea);

        /// <summary>
        /// Returns the first area in the module.
        /// </summary>
        /// <returns>The first area in the module</returns>
        public static uint GetFirstArea() => _service.GetFirstArea();

        /// <summary>
        /// Returns the next area in the module (after GetFirstArea).
        /// </summary>
        /// <returns>The next area in the module, or OBJECT_INVALID if no more areas are loaded</returns>
        public static uint GetNextArea() => _service.GetNextArea();

        /// <summary>
        /// Gets the first object in the specified area.
        /// </summary>
        /// <param name="oArea">The area to get the first object from. If no valid area is specified, uses the caller's area (default: OBJECT_INVALID)</param>
        /// <param name="nObjectFilter">Allows filtering out undesired object types using bitwise "or". For example, to return only creatures and doors, use OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR (default: ObjectType.All)</param>
        /// <returns>The first object in the area. Returns OBJECT_INVALID on error</returns>
        public static uint GetFirstObjectInArea(uint oArea = NWScriptService.OBJECT_INVALID, ObjectType nObjectFilter = ObjectType.All) => _service.GetFirstObjectInArea(oArea, nObjectFilter);

        /// <summary>
        /// Gets the next object in the specified area.
        /// </summary>
        /// <param name="oArea">The area to get the next object from. If no valid area is specified, uses the caller's area (default: OBJECT_INVALID)</param>
        /// <param name="nObjectFilter">Allows filtering out undesired object types using bitwise "or". For example, to return only creatures and doors, use OBJECT_TYPE_CREATURE | OBJECT_TYPE_DOOR (default: ObjectType.All)</param>
        /// <returns>The next object in the area. Returns OBJECT_INVALID on error</returns>
        public static uint GetNextObjectInArea(uint oArea = NWScriptService.OBJECT_INVALID, ObjectType nObjectFilter = ObjectType.All) => _service.GetNextObjectInArea(oArea, nObjectFilter);

        /// <summary>
        /// Gets the location of the specified object.
        /// </summary>
        /// <param name="oObject">The object to get the location for</param>
        /// <returns>The location of the object</returns>
        public static Location GetLocation(uint oObject) => _service.GetLocation(oObject);

        /// <summary>
        /// Makes the subject jump to the specified location instantly (even between areas).
        /// </summary>
        /// <param name="lLocation">The location to jump to. If invalid, nothing will happen</param>
        public static void ActionJumpToLocation(Location lLocation) => _service.ActionJumpToLocation(lLocation);

        /// <summary>
        /// Creates a location object.
        /// </summary>
        /// <param name="oArea">The area for the location</param>
        /// <param name="vPosition">The position vector</param>
        /// <param name="fOrientation">The orientation angle</param>
        /// <returns>The created location</returns>
        public static Location Location(uint oArea, Vector3 vPosition, float fOrientation) => _service.Location(oArea, vPosition, fOrientation);

        /// <summary>
        /// Applies the specified effect at the given location.
        /// </summary>
        /// <param name="nDurationType">The duration type for the effect</param>
        /// <param name="eEffect">The effect to apply</param>
        /// <param name="lLocation">The location to apply the effect at</param>
        /// <param name="fDuration">The duration of the effect (default: 0.0)</param>
        public static void ApplyEffectAtLocation(DurationType nDurationType, Effect eEffect, Location lLocation,
            float fDuration = 0.0f) => _service.ApplyEffectAtLocation(nDurationType, eEffect, lLocation, fDuration);

        /// <summary>
        /// Exposes or hides the entire map of the specified area for the player.
        /// </summary>
        /// <param name="oArea">The area that the map will be exposed/hidden for</param>
        /// <param name="oPlayer">The player the map will be exposed/hidden for</param>
        /// <param name="bExplored">Whether the map should be completely explored or hidden (default: true)</param>
        public static void ExploreAreaForPlayer(uint oArea, uint oPlayer, bool bExplored = true) => _service.ExploreAreaForPlayer(oArea, oPlayer, bExplored);

        /// <summary>
        /// Sets the transition target for the specified transition.
        /// </summary>
        /// <param name="oTransition">The transition object (can be any valid game object, except areas)</param>
        /// <param name="oTarget">The target object (can be any valid game object with a location, or OBJECT_INVALID to unlink)</param>
        /// <remarks>Rebinding a transition will NOT change the other end of the transition; for example, with normal doors you will have to do either end separately. Any valid game object can hold a transition target, but only some are used by the game engine (doors and triggers). This might change in the future. You can still set and query them for other game objects from NWScriptService. Transition target objects are cached: The toolset-configured destination tag is used for a lookup only once, at first use. Thus, attempting to use SetTag() to change the destination for a transition will not work in a predictable fashion.</remarks>
        public static void SetTransitionTarget(uint oTransition, uint oTarget) => _service.SetTransitionTarget(oTransition, oTarget);

        /// <summary>
        /// Sets the weather for the specified target.
        /// </summary>
        /// <param name="oTarget">If this is GetModule(), all outdoor areas will be modified by the weather constant. If it is an area, the target will play the weather only if it is an outdoor area</param>
        /// <param name="nWeather">The weather type (WEATHER_* constant). WEATHER_USER_AREA_SETTINGS will set the area back to random weather. WEATHER_CLEAR, WEATHER_RAIN, WEATHER_SNOW will make the weather go to the appropriate precipitation without stopping</param>
        public static void SetWeather(uint oTarget, AreaWeatherType nWeather) => _service.SetWeather(oTarget, nWeather);

        /// <summary>
        /// Sets whether the given creature has explored the tile at the specified coordinates.
        /// </summary>
        /// <param name="creature">The creature (must be a player- or player-possessed creature)</param>
        /// <param name="area">The area containing the tile</param>
        /// <param name="x">The x coordinate of the tile</param>
        /// <param name="y">The y coordinate of the tile</param>
        /// <param name="newState">Whether the tile should be explored or not</param>
        /// <returns>Return values: -1 = Area or creature invalid, 0 = Tile was not explored before setting newState, 1 = Tile was explored before setting newState</returns>
        /// <remarks>Keep in mind that tile exploration also controls object visibility in areas and the fog of war for interior and underground areas.</remarks>
        public static int SetTileExplored(uint creature, uint area, int x, int y, bool newState) => _service.SetTileExplored(creature, area, x, y, newState);

        /// <summary>
        /// Returns whether the given tile at the specified coordinates is visible on the map for the creature.
        /// </summary>
        /// <param name="creature">The creature (must be a player- or player-possessed creature)</param>
        /// <param name="area">The area containing the tile</param>
        /// <param name="x">The x coordinate of the tile</param>
        /// <param name="y">The y coordinate of the tile</param>
        /// <returns>Return values: -1 = Area or creature invalid, 0 = Tile is not explored yet, 1 = Tile is explored</returns>
        /// <remarks>Keep in mind that tile exploration also controls object visibility in areas and the fog of war for interior and underground areas.</remarks>
        public static int GetTileExplored(uint creature, uint area, int x, int y) => _service.GetTileExplored(creature, area, x, y);

        /// <summary>
        /// Sets whether the creature auto-explores the map as it walks around.
        /// </summary>
        /// <param name="creature">The creature to set auto-exploration for</param>
        /// <param name="newState">Whether the creature should auto-explore (true/false)</param>
        /// <returns>The previous state (or -1 if non-creature). Does nothing for non-creatures</returns>
        /// <remarks>Keep in mind that tile exploration also controls object visibility in areas and the fog of war for interior and underground areas. This means that if you turn off auto exploration, it falls to you to manage this through SetTileExplored(); otherwise, the player will not be able to see anything.</remarks>
        public static int SetCreatureExploresMinimap(uint creature, bool newState) => _service.SetCreatureExploresMinimap(creature, newState);

        /// <summary>
        /// Returns whether the creature is set to auto-explore the map as it walks around.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <returns>True if the creature is set to auto-explore (on by default), false if creature is not actually a creature</returns>
        public static int GetCreatureExploresMinimap(uint creature) => _service.GetCreatureExploresMinimap(creature);

        /// <summary>
        /// Gets the surface material at the given location (equivalent to the walkmesh type).
        /// </summary>
        /// <param name="at">The location to get the surface material for</param>
        /// <returns>The surface material. Returns 0 if the location is invalid or has no surface type</returns>
        public static int GetSurfaceMaterial(Location at) => _service.GetSurfaceMaterial(at);

        /// <summary>
        /// Returns the z-offset at which the walkmesh is at the given location.
        /// </summary>
        /// <param name="at">The location to get the ground height for</param>
        /// <returns>The z-offset of the walkmesh. Returns -6.0 for invalid locations</returns>
        public static float GetGroundHeight(Location at) => _service.GetGroundHeight(at);

        /// <summary>
        /// Returns whether the creature is in the given subarea (trigger, area of effect object, etc.).
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="oSubArea">The subarea to check (default: OBJECT_SELF)</param>
        /// <returns>True if the creature is in the subarea, false otherwise</returns>
        /// <remarks>This function will tell you if the creature has triggered an onEnter event, not if it is physically within the space of the subarea</remarks>
        public static bool GetIsInSubArea(uint oCreature, uint oSubArea = NWScriptService.OBJECT_INVALID) => _service.GetIsInSubArea(oCreature, oSubArea);

        /// <summary>
        /// Sets the main light color on the tile at the specified location.
        /// </summary>
        /// <param name="lTileLocation">The tile location (the vector part is the tile grid x,y coordinate)</param>
        /// <param name="nMainLight1Color">The main light 1 color (TILE_MAIN_LIGHT_COLOR_* constant)</param>
        /// <param name="nMainLight2Color">The main light 2 color (TILE_MAIN_LIGHT_COLOR_* constant)</param>
        public static void SetTileMainLightColor(Location lTileLocation, int nMainLight1Color, int nMainLight2Color) => _service.SetTileMainLightColor(lTileLocation, nMainLight1Color, nMainLight2Color);

        /// <summary>
        /// Sets the source light color on the tile at the specified location.
        /// </summary>
        /// <param name="lTileLocation">The tile location (the vector part is the tile grid x,y coordinate)</param>
        /// <param name="nSourceLight1Color">The source light 1 color (TILE_SOURCE_LIGHT_COLOR_* constant)</param>
        /// <param name="nSourceLight2Color">The source light 2 color (TILE_SOURCE_LIGHT_COLOR_* constant)</param>
        public static void SetTileSourceLightColor(Location lTileLocation, int nSourceLight1Color,
            int nSourceLight2Color) => _service.SetTileSourceLightColor(lTileLocation, nSourceLight1Color, nSourceLight2Color);

        /// <summary>
        /// Gets the color for the main light 1 of the tile at the specified location.
        /// </summary>
        /// <param name="lTile">The tile location (the vector part is the tile grid x,y coordinate)</param>
        /// <returns>The main light 1 color (TILE_MAIN_LIGHT_COLOR_* constant)</returns>
        public static int GetTileMainLight1Color(Location lTile) => _service.GetTileMainLight1Color(lTile);

        /// <summary>
        /// Gets the color for the main light 2 of the tile at the specified location.
        /// </summary>
        /// <param name="lTile">The tile location (the vector part is the tile grid x,y coordinate)</param>
        /// <returns>The main light 2 color (TILE_MAIN_LIGHT_COLOR_* constant)</returns>
        public static int GetTileMainLight2Color(Location lTile) => _service.GetTileMainLight2Color(lTile);

        /// <summary>
        /// Gets the color for the source light 1 of the tile at the specified location.
        /// </summary>
        /// <param name="lTile">The tile location (the vector part is the tile grid x,y coordinate)</param>
        /// <returns>The source light 1 color (TILE_SOURCE_LIGHT_COLOR_* constant)</returns>
        public static int GetTileSourceLight1Color(Location lTile) => _service.GetTileSourceLight1Color(lTile);

        /// <summary>
        /// Gets the color for the source light 2 of the tile at the specified location.
        /// </summary>
        /// <param name="lTile">The tile location (the vector part is the tile grid x,y coordinate)</param>
        /// <returns>The source light 2 color (TILE_SOURCE_LIGHT_COLOR_* constant)</returns>
        public static int GetTileSourceLight2Color(Location lTile) => _service.GetTileSourceLight2Color(lTile);

        /// <summary>
        /// Sets whether the map pin is enabled.
        /// </summary>
        /// <param name="oMapPin">The map pin to set</param>
        /// <param name="bEnabled">Whether the map pin is enabled (0=Off, 1=On) (default: true)</param>
        public static void SetMapPinEnabled(uint oMapPin, bool bEnabled = true) => _service.SetMapPinEnabled(oMapPin, bEnabled);

        /// <summary>
        /// Gets the area's object ID from the specified location.
        /// </summary>
        /// <param name="lLocation">The location to get the area from</param>
        /// <returns>The area's object ID</returns>
        public static uint GetAreaFromLocation(Location lLocation) => _service.GetAreaFromLocation(lLocation);

        /// <summary>
        /// Gets the position vector from the specified location.
        /// </summary>
        /// <param name="lLocation">The location to get the position from</param>
        /// <returns>The position vector</returns>
        public static Vector3 GetPositionFromLocation(Location lLocation) => _service.GetPositionFromLocation(lLocation);

        /// <summary>
        /// Sets the transition bitmap of a player (should only be called in area transition scripts).
        /// </summary>
        /// <param name="nPredefinedAreaTransition">To use a predefined area transition bitmap, use one of AREA_TRANSITION_*. To use a custom, user-defined area transition bitmap, use AREA_TRANSITION_USER_DEFINED and specify the filename in the second parameter</param>
        /// <param name="sCustomAreaTransitionBMP">The filename of a custom, user-defined area transition bitmap (default: empty string)</param>
        /// <remarks>This action should be run by the person "clicking" the area transition via AssignCommand.</remarks>
        public static void SetAreaTransitionBMP(AreaTransitionType nPredefinedAreaTransition,
            string sCustomAreaTransitionBMP = "") => _service.SetAreaTransitionBMP(nPredefinedAreaTransition, sCustomAreaTransitionBMP);

        /// <summary>
        /// Sets the detailed wind data for the specified area.
        /// </summary>
        /// <param name="oArea">The area to set wind data for</param>
        /// <param name="vDirection">The wind direction vector</param>
        /// <param name="fMagnitude">The wind magnitude</param>
        /// <param name="fYaw">The wind yaw angle</param>
        /// <param name="fPitch">The wind pitch angle</param>
        /// <remarks>The predefined values in the toolset are: NONE: vDirection=(1.0, 1.0, 0.0), fMagnitude=0.0, fYaw=0.0, fPitch=0.0; LIGHT: vDirection=(1.0, 1.0, 0.0), fMagnitude=1.0, fYaw=100.0, fPitch=3.0; HEAVY: vDirection=(1.0, 1.0, 0.0), fMagnitude=2.0, fYaw=150.0, fPitch=5.0</remarks>
        public static void SetAreaWind(uint oArea, Vector3 vDirection, float fMagnitude, float fYaw, float fPitch) => _service.SetAreaWind(oArea, vDirection, fMagnitude, fYaw, fPitch);

        /// <summary>
        /// Gets the light color in the specified area.
        /// </summary>
        /// <param name="nColorType">The color type to return (AREA_LIGHT_COLOR_* values)</param>
        /// <param name="oArea">The area to get light color for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_SELF)</param>
        /// <returns>The light color</returns>
        public static int GetAreaLightColor(AreaLightColorType nColorType, uint oArea = NWScriptService.OBJECT_INVALID) => _service.GetAreaLightColor(nColorType, oArea);

        /// <summary>
        /// Sets the light color in the specified area.
        /// </summary>
        /// <param name="nColorType">The color type (AREA_LIGHT_COLOR_* constants)</param>
        /// <param name="nColor">The color to set (FOG_COLOR_* constants). Can also be represented as a hex RGB number (e.g., 0xFFEEDD where FF=red, EE=green, DD=blue)</param>
        /// <param name="oArea">The area to set light color for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_SELF)</param>
        /// <param name="fFadeTime">If above 0.0, it will fade to the new color in the amount of seconds specified (default: 0.0)</param>
        public static void SetAreaLightColor(
            AreaLightColorType nColorType,
            FogColorType nColor,
            uint oArea = NWScriptService.OBJECT_INVALID,
            float fFadeTime = 0.0f) => _service.SetAreaLightColor(nColorType, nColor, oArea, fFadeTime);

        /// <summary>
        /// Gets the light direction of origin in the specified area.
        /// </summary>
        /// <param name="nLightType">Specifies whether the Moon or Sun light direction is returned (AREA_LIGHT_DIRECTION_* values)</param>
        /// <param name="oArea">The area to get light direction for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_SELF)</param>
        /// <returns>The light direction vector</returns>
        public static Vector3 GetAreaLightDirection(AreaLightDirectionType nLightType, uint oArea = NWScriptService.OBJECT_INVALID) => _service.GetAreaLightDirection(nLightType, oArea);

        /// <summary>
        /// Sets the light direction of origin in the specified area.
        /// </summary>
        /// <param name="nLightType">The light type (AREA_LIGHT_DIRECTION_* constants)</param>
        /// <param name="vDirection">The direction of origin of the light type, i.e. the direction the sun/moon is in from the area</param>
        /// <param name="oArea">The area to set light direction for. If no valid area is specified, uses the area of caller. If an object other than an area is specified, uses the area that the object is currently in (default: OBJECT_SELF)</param>
        /// <param name="fFadeTime">If above 0.0, it will fade to the new direction in the amount of seconds specified (default: 0.0)</param>
        public static void SetAreaLightDirection(
            AreaLightDirectionType nLightType,
            Vector3 vDirection,
            uint oArea = NWScriptService.OBJECT_INVALID,
            float fFadeTime = 0.0f) => _service.SetAreaLightDirection(nLightType, vDirection, oArea, fFadeTime);

        /// <summary>
        /// Gets the first object within the specified persistent object.
        /// </summary>
        /// <param name="oPersistentObject">The persistent object to search within (default: OBJECT_SELF)</param>
        /// <param name="nResidentObjectType">The type of objects to find (OBJECT_TYPE_* constants) (default: ObjectType.Creature)</param>
        /// <param name="nPersistentZone">The persistent zone (PERSISTENT_ZONE_ACTIVE. PERSISTENT_ZONE_FOLLOW is no longer used) (default: PersistentZone.Active)</param>
        /// <returns>The first object found. Returns OBJECT_INVALID if no object is found</returns>
        public static uint GetFirstInPersistentObject(uint oPersistentObject = NWScriptService.OBJECT_INVALID,
            ObjectType nResidentObjectType = ObjectType.Creature,
            PersistentZoneType nPersistentZone = PersistentZoneType.Active) => _service.GetFirstInPersistentObject(oPersistentObject, nResidentObjectType, nPersistentZone);

        /// <summary>
        /// Gets the next object within the specified persistent object.
        /// </summary>
        /// <param name="oPersistentObject">The persistent object to search within (default: OBJECT_SELF)</param>
        /// <param name="nResidentObjectType">The type of objects to find (OBJECT_TYPE_* constants) (default: ObjectType.Creature)</param>
        /// <param name="nPersistentZone">The persistent zone (PERSISTENT_ZONE_ACTIVE. PERSISTENT_ZONE_FOLLOW is no longer used) (default: PersistentZone.Active)</param>
        /// <returns>The next object found. Returns OBJECT_INVALID if no object is found</returns>
        public static uint GetNextInPersistentObject(uint oPersistentObject = NWScriptService.OBJECT_INVALID,
            ObjectType nResidentObjectType = ObjectType.Creature,
            PersistentZoneType nPersistentZone = PersistentZoneType.Active) => _service.GetNextInPersistentObject(oPersistentObject, nResidentObjectType, nPersistentZone);

        /// <summary>
        /// Returns the creator of the specified area of effect object.
        /// </summary>
        /// <param name="oAreaOfEffectObject">The area of effect object to get the creator for (default: OBJECT_SELF)</param>
        /// <returns>The creator of the area of effect object. Returns OBJECT_INVALID if the object is not a valid Area of Effect object</returns>
        public static uint GetAreaOfEffectCreator(uint oAreaOfEffectObject = NWScriptService.OBJECT_INVALID) => _service.GetAreaOfEffectCreator(oAreaOfEffectObject);

        /// <summary>
        /// Gets the distance between two locations.
        /// </summary>
        /// <param name="lLocationA">The first location</param>
        /// <param name="lLocationB">The second location</param>
        /// <returns>The distance between the locations</returns>
        public static float GetDistanceBetweenLocations(Location lLocationA, Location lLocationB) => _service.GetDistanceBetweenLocations(lLocationA, lLocationB);

        /// <summary>
        /// Changes a tile in an area and updates it for all players in the area.
        /// </summary>
        /// <param name="locTile">The location of the tile</param>
        /// <param name="nTileID">The ID of the tile (for values see the .set file of the tileset)</param>
        /// <param name="nOrientation">The orientation of the tile (0-3): 0 = Normal orientation, 1 = 90 degrees counterclockwise, 2 = 180 degrees counterclockwise, 3 = 270 degrees counterclockwise</param>
        /// <param name="nHeight">The height of the tile (default: 0)</param>
        /// <param name="nFlags">A bitmask of SETTILE_FLAG_* constants (default: SetTileFlagType.RecomputeLighting)</param>
        /// <remarks>For optimal use you should be familiar with how tilesets / .set files work. Will not update the height of non-creature objects. Creatures may get stuck on non-walkable terrain. SETTILE_FLAG_RELOAD_GRASS: reloads the area's grass, use if your tile used to have grass or should have grass now. SETTILE_FLAG_RELOAD_BORDER: reloads the edge tile border, use if you changed a tile on the edge of the area. SETTILE_FLAG_RECOMPUTE_LIGHTING: recomputes the area's lighting and shadows, use most of time.</remarks>
        public static void SetTile(
            Location locTile,
            int nTileID,
            int nOrientation,
            int nHeight = 0,
            SetTileFlagType nFlags = SetTileFlagType.RecomputeLighting) => _service.SetTile(locTile, nTileID, nOrientation, nHeight, nFlags);

        /// <summary>
        /// Gets the ID of the tile at the specified location.
        /// </summary>
        /// <param name="locTile">The location to get the tile ID for</param>
        /// <returns>The tile ID. Returns -1 on error</returns>
        public static int GetTileID(Location locTile) => _service.GetTileID(locTile);

        /// <summary>
        /// Gets the orientation of the tile at the specified location.
        /// </summary>
        /// <param name="locTile">The location to get the tile orientation for</param>
        /// <returns>The tile orientation. Returns -1 on error</returns>
        public static int GetTileOrientation(Location locTile) => _service.GetTileOrientation(locTile);

        /// <summary>
        /// Gets the height of the tile at the specified location.
        /// </summary>
        /// <param name="locTile">The location to get the tile height for</param>
        /// <returns>The tile height. Returns -1 on error</returns>
        public static int GetTileHeight(Location locTile) => _service.GetTileHeight(locTile);

        /// <summary>
        /// Makes all clients in the area reload the area's grass.
        /// </summary>
        /// <param name="oArea">The area to reload grass for</param>
        /// <remarks>This can be used to update the grass of an area after changing a tile with SetTile() that will have or used to have grass.</remarks>
        public static void ReloadAreaGrass(uint oArea) => _service.ReloadAreaGrass(oArea);

        /// <summary>
        /// Sets the state of the tile animation loops of the tile at the specified location.
        /// </summary>
        /// <param name="locTile">The location of the tile</param>
        /// <param name="bAnimLoop1">The state of animation loop 1</param>
        /// <param name="bAnimLoop2">The state of animation loop 2</param>
        /// <param name="bAnimLoop3">The state of animation loop 3</param>
        public static void SetTileAnimationLoops(Location locTile, bool bAnimLoop1, bool bAnimLoop2, bool bAnimLoop3) => _service.SetTileAnimationLoops(locTile, bAnimLoop1, bAnimLoop2, bAnimLoop3);

        /// <summary>
        /// Changes multiple tiles in an area and updates them for all players in the area.
        /// </summary>
        /// <param name="oArea">The area to change one or more tiles of</param>
        /// <param name="jTileData">A JsonArray() with one or more JsonObject()s with the following keys: index (tile index as JsonInt()), tileid (tile ID as JsonInt(), defaults to 0), orientation (tile orientation as JsonInt(), defaults to 0), height (tile height as JsonInt(), defaults to 0), animloop1/2/3 (animation state as JsonInt(), defaults to current value)</param>
        /// <param name="nFlags">A bitmask of SETTILE_FLAG_* constants (default: SetTileFlagType.RecomputeLighting)</param>
        /// <param name="sTileset">If not empty, it will also change the area's tileset. Warning: only use this if you really know what you're doing, it's very easy to break things badly. Make sure jTileData changes *all* tiles in the area and to a tile id that's supported by sTileset (default: empty string)</param>
        /// <remarks>See SetTile() for additional information. For example, a 3x3 area has the following tile indexes: 6 7 8, 3 4 5, 0 1 2</remarks>
        public static void SetTileJson(
            uint oArea,
            Json jTileData,
            SetTileFlagType nFlags = SetTileFlagType.RecomputeLighting,
            string sTileset = "") => _service.SetTileJson(oArea, jTileData, nFlags, sTileset);

        /// <summary>
        /// Makes all clients in the area reload the inaccessible border tiles.
        /// </summary>
        /// <param name="oArea">The area to reload border tiles for</param>
        /// <remarks>This can be used to update the edge tiles after changing a tile with SetTile().</remarks>
        public static void ReloadAreaBorder(uint oArea) => _service.ReloadAreaBorder(oArea);

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
        public static void SetCalendar(int nYear, int nMonth, int nDay) => _service.SetCalendar(nYear, nMonth, nDay);

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
        public static void SetTime(int nHour, int nMinute, int nSecond, int nMillisecond) => _service.SetTime(nHour, nMinute, nSecond, nMillisecond);

        /// <summary>
        /// Gets the current calendar year.
        /// </summary>
        /// <returns>The current calendar year</returns>
        public static int GetCalendarYear() => _service.GetCalendarYear();

        /// <summary>
        /// Gets the current calendar month.
        /// </summary>
        /// <returns>The current calendar month</returns>
        public static int GetCalendarMonth() => _service.GetCalendarMonth();

        /// <summary>
        /// Gets the current calendar day.
        /// </summary>
        /// <returns>The current calendar day</returns>
        public static int GetCalendarDay() => _service.GetCalendarDay();

        /// <summary>
        /// Sets whether or not the creature has detected the trapped object.
        /// </summary>
        /// <param name="oTrap">A trapped trigger, placeable or door object</param>
        /// <param name="oDetector">The creature that the detected status of the trap is being adjusted for</param>
        /// <param name="bDetected">A Boolean that sets whether the trapped object has been detected or not</param>
        /// <returns>1 if successful, 0 otherwise</returns>
        public static int SetTrapDetectedBy(uint oTrap, uint oDetector, bool bDetected = true) => _service.SetTrapDetectedBy(oTrap, oDetector, bDetected);

        /// <summary>
        /// Checks if the object is trapped.
        /// Note: Only placeables, doors and triggers can be trapped.
        /// </summary>
        /// <param name="oObject">The object to check</param>
        /// <returns>TRUE if the object is trapped</returns>
        public static bool GetIsTrapped(uint oObject) => _service.GetIsTrapped(oObject);

        /// <summary>
        /// Checks if the trap object is disarmable.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>TRUE if the trap object is disarmable</returns>
        public static bool GetTrapDisarmable(uint oTrapObject) => _service.GetTrapDisarmable(oTrapObject);

        /// <summary>
        /// Checks if the trap object is detectable.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>TRUE if the trap object is detectable</returns>
        public static bool GetTrapDetectable(uint oTrapObject) => _service.GetTrapDetectable(oTrapObject);

        /// <summary>
        /// Checks if the creature has detected the trap object.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>TRUE if the creature has detected the trap object</returns>
        public static bool GetTrapDetectedBy(uint oTrapObject, uint oCreature) => _service.GetTrapDetectedBy(oTrapObject, oCreature);

        /// <summary>
        /// Checks if the trap object has been flagged as visible to all creatures.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>TRUE if the trap object has been flagged as visible to all creatures</returns>
        public static bool GetTrapFlagged(uint oTrapObject) => _service.GetTrapFlagged(oTrapObject);

        /// <summary>
        /// Gets the trap base type of the trap object.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>The trap base type (TRAP_BASE_TYPE_*)</returns>
        public static TrapBaseType GetTrapBaseType(uint oTrapObject) => _service.GetTrapBaseType(oTrapObject);

        /// <summary>
        /// Checks if the trap object is one-shot (i.e. it does not reset itself after firing).
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>TRUE if the trap object is one-shot</returns>
        public static bool GetTrapOneShot(uint oTrapObject) => _service.GetTrapOneShot(oTrapObject);

        /// <summary>
        /// Gets the creator of the trap object, the creature that set the trap.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>The creator of the trap, or OBJECT_INVALID if the trap was created in the toolset</returns>
        public static uint GetTrapCreator(uint oTrapObject) => _service.GetTrapCreator(oTrapObject);

        /// <summary>
        /// Gets the tag of the key that will disarm the trap object.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>The tag of the key that will disarm the trap</returns>
        public static string GetTrapKeyTag(uint oTrapObject) => _service.GetTrapKeyTag(oTrapObject);

        /// <summary>
        /// Gets the DC for disarming the trap object.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>The DC for disarming the trap</returns>
        public static int GetTrapDisarmDC(uint oTrapObject) => _service.GetTrapDisarmDC(oTrapObject);

        /// <summary>
        /// Gets the DC for detecting the trap object.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>The DC for detecting the trap</returns>
        public static int GetTrapDetectDC(uint oTrapObject) => _service.GetTrapDetectDC(oTrapObject);

        /// <summary>
        /// Gets the trap nearest to the target.
        /// Note: "trap objects" are actually any trigger, placeable or door that is
        /// trapped in the target's area.
        /// </summary>
        /// <param name="oTarget">The target object (defaults to OBJECT_SELF)</param>
        /// <param name="nTrapDetected">If this is TRUE, the trap returned has to have been detected by the target</param>
        /// <returns>The nearest trap to the target</returns>
        public static uint GetNearestTrapToObject(uint oTarget = NWScriptService.OBJECT_INVALID, bool nTrapDetected = true) => _service.GetNearestTrapToObject(oTarget, nTrapDetected);

        /// <summary>
        /// Gets the last trap detected by the target.
        /// </summary>
        /// <param name="oTarget">The target object (defaults to OBJECT_SELF)</param>
        /// <returns>The last trap detected by the target, or OBJECT_INVALID on error</returns>
        public static uint GetLastTrapDetected(uint oTarget = NWScriptService.OBJECT_INVALID) => _service.GetLastTrapDetected(oTarget);

        /// <summary>
        /// Checks if the trap object is active.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>TRUE if the trap object is active</returns>
        public static bool GetTrapActive(uint oTrapObject) => _service.GetTrapActive(oTrapObject);

        /// <summary>
        /// Sets whether or not the trap is an active trap.
        /// Setting a trap as inactive will not make the trap disappear if it has already been detected.
        /// Call SetTrapDetectedBy() to make a detected trap disappear.
        /// To make an inactive trap not detectable call SetTrapDetectable().
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="nActive">TRUE/FALSE</param>
        public static void SetTrapActive(uint oTrapObject, bool nActive = true) => _service.SetTrapActive(oTrapObject, nActive);

        /// <summary>
        /// Checks if the trap object can be recovered.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <returns>TRUE if the trap object can be recovered</returns>
        public static bool GetTrapRecoverable(uint oTrapObject) => _service.GetTrapRecoverable(oTrapObject);

        /// <summary>
        /// Sets whether or not the trapped object can be recovered.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="nRecoverable">TRUE/FALSE</param>
        public static void SetTrapRecoverable(uint oTrapObject, bool nRecoverable = true) => _service.SetTrapRecoverable(oTrapObject, nRecoverable);

        /// <summary>
        /// Sets whether or not the trapped object can be disarmed.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="nDisarmable">TRUE/FALSE</param>
        public static void SetTrapDisarmable(uint oTrapObject, bool nDisarmable = true) => _service.SetTrapDisarmable(oTrapObject, nDisarmable);

        /// <summary>
        /// Sets whether or not the trapped object can be detected.
        /// Note: Setting a trapped object to not be detectable will
        /// not make the trap disappear if it has already been detected.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="nDetectable">TRUE/FALSE</param>
        public static void SetTrapDetectable(uint oTrapObject, bool nDetectable = true) => _service.SetTrapDetectable(oTrapObject, nDetectable);

        /// <summary>
        /// Sets whether or not the trap is a one-shot trap
        /// (i.e. whether or not the trap resets itself after firing).
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="nOneShot">TRUE/FALSE</param>
        public static void SetTrapOneShot(uint oTrapObject, bool nOneShot = true) => _service.SetTrapOneShot(oTrapObject, nOneShot);

        /// <summary>
        /// Sets the tag of the key that will disarm the trap object.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="sKeyTag">The tag of the key that will disarm the trap</param>
        public static void SetTrapKeyTag(uint oTrapObject, string sKeyTag) => _service.SetTrapKeyTag(oTrapObject, sKeyTag);

        /// <summary>
        /// Sets the DC for disarming the trap object.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="nDisarmDC">Must be between 0 and 250</param>
        public static void SetTrapDisarmDC(uint oTrapObject, int nDisarmDC) => _service.SetTrapDisarmDC(oTrapObject, nDisarmDC);

        /// <summary>
        /// Sets the DC for detecting the trap object.
        /// </summary>
        /// <param name="oTrapObject">A placeable, door or trigger</param>
        /// <param name="nDetectDC">Must be between 0 and 250</param>
        public static void SetTrapDetectDC(uint oTrapObject, int nDetectDC) => _service.SetTrapDetectDC(oTrapObject, nDetectDC);

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
            string sTag = "", FactionType nFaction = FactionType.Hostile, string sOnDisarmScript = "",
            string sOnTrapTriggeredScript = "") => _service.CreateTrapAtLocation(nTrapType, lLocation, fSize, sTag, nFaction, sOnDisarmScript, sOnTrapTriggeredScript);

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
        public static void CreateTrapOnObject(TrapBaseType nTrapType, uint oObject, FactionType nFaction = FactionType.Hostile,
            string sOnDisarmScript = "", string sOnTrapTriggeredScript = "") => _service.CreateTrapOnObject(nTrapType, oObject, nFaction, sOnDisarmScript, sOnTrapTriggeredScript);

        /// <summary>
        /// Disables the trap.
        /// </summary>
        /// <param name="oTrap">A placeable, door or trigger</param>
        public static void SetTrapDisabled(uint oTrap) => _service.SetTrapDisabled(oTrap);

        /// <summary>
        /// Destroys the given sqlite database, clearing out all data and schema.
        /// This operation is _immediate_ and _irreversible_, even when
        /// inside a transaction or running query.
        /// Existing active/prepared sqlqueries will remain functional, but any references
        /// to stored data or schema members will be invalidated.
        /// To reset a campaign database, please use DestroyCampaignDatabase().
        /// </summary>
        /// <param name="oObject">Same as SqlPrepareQueryObject()</param>
        public static void SqlDestroyDatabase(uint oObject) => _service.SqlDestroyDatabase(oObject);

        /// <summary>
        /// Returns empty string if the last SQL command succeeded; or a human-readable error otherwise.
        /// Additionally, all SQL errors are logged to the server log.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to get the error for</param>
        /// <returns>Empty string if successful, or error message if failed</returns>
        public static string SqlGetError(IntPtr sqlQuery) => _service.SqlGetError(sqlQuery);

        /// <summary>
        /// Sets up a query.
        /// This will NOT run the query; only make it available for parameter binding.
        /// To run the query, you need to call SqlStep(); even if you do not
        /// expect result data.
        /// Note that when accessing campaign databases, you do not write access
        /// to the builtin tables needed for CampaignDB functionality.
        /// N.B.: You can pass sqlqueries into DelayCommand; HOWEVER
        /// *** they will NOT survive a game save/load ***
        /// Any commands on a restored sqlquery will fail.
        /// </summary>
        /// <param name="sDatabase">The name of a campaign database</param>
        /// <param name="sQuery">The SQL query string</param>
        /// <returns>The prepared SQL query</returns>
        public static IntPtr SqlPrepareQueryCampaign(string sDatabase, string sQuery) => _service.SqlPrepareQueryCampaign(sDatabase, sQuery);

        /// <summary>
        /// Sets up a query.
        /// This will NOT run the query; only make it available for parameter binding.
        /// To run the query, you need to call SqlStep(); even if you do not
        /// expect result data.
        /// The database is persisted to savegames in case of the module,
        /// and to character files in case of a player characters.
        /// Other objects cannot carry databases, and this function call
        /// will error for them.
        /// N.B: Databases on objects (especially player characters!) should be kept
        /// to a reasonable size. Delete old data you no longer need.
        /// If you attempt to store more than a few megabytes of data on a
        /// player creature, you may have a bad time.
        /// N.B.: You can pass sqlqueries into DelayCommand; HOWEVER
        /// *** they will NOT survive a game save/load ***
        /// Any commands on a restored sqlquery will fail.
        /// </summary>
        /// <param name="oObject">Can be either the module (GetModule()), or a player character</param>
        /// <param name="sQuery">The SQL query string</param>
        /// <returns>The prepared SQL query</returns>
        public static IntPtr SqlPrepareQueryObject(uint oObject, string sQuery) => _service.SqlPrepareQueryObject(oObject, sQuery);

        /// <summary>
        /// Binds an integer to a named parameter of the given prepared query.
        /// Example:
        /// sqlquery v = SqlPrepareQueryObject(GetModule(), "insert into test (col) values (@myint);");
        /// SqlBindInt(v, "@v", 5);
        /// SqlStep(v);
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query</param>
        /// <param name="sParam">The named parameter to bind to</param>
        /// <param name="nValue">The integer value to bind</param>
        public static void SqlBindInt(IntPtr sqlQuery, string sParam, int nValue) => _service.SqlBindInt(sqlQuery, sParam, nValue);

        /// <summary>
        /// Binds a float to a named parameter of the given prepared query.
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query</param>
        /// <param name="sParam">The named parameter to bind to</param>
        /// <param name="fFloat">The float value to bind</param>
        public static void SqlBindFloat(IntPtr sqlQuery, string sParam, float fFloat) => _service.SqlBindFloat(sqlQuery, sParam, fFloat);

        /// <summary>
        /// Binds a string to a named parameter of the given prepared query.
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query</param>
        /// <param name="sParam">The named parameter to bind to</param>
        /// <param name="sString">The string value to bind</param>
        public static void SqlBindString(IntPtr sqlQuery, string sParam, string sString) => _service.SqlBindString(sqlQuery, sParam, sString);

        /// <summary>
        /// Binds a vector to a named parameter of the given prepared query.
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query</param>
        /// <param name="sParam">The named parameter to bind to</param>
        /// <param name="vVector">The vector value to bind</param>
        public static void SqlBindVector(IntPtr sqlQuery, string sParam, Vector3 vVector) => _service.SqlBindVector(sqlQuery, sParam, vVector);

        /// <summary>
        /// Binds an object to a named parameter of the given prepared query.
        /// Objects are serialized, NOT stored as a reference!
        /// Currently supported object types: Creatures, Items, Placeables, Waypoints, Stores, Doors, Triggers, Areas (CAF format)
        /// If bSaveObjectState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are saved out
        /// (except for Combined Area Format, which always has object state saved out).
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query</param>
        /// <param name="sParam">The named parameter to bind to</param>
        /// <param name="oObject">The object to bind</param>
        /// <param name="bSaveObjectState">Whether to save object state (local vars, effects, action queue, etc.)</param>
        public static void SqlBindObject(IntPtr sqlQuery, string sParam, uint oObject, bool bSaveObjectState = false) => _service.SqlBindObject(sqlQuery, sParam, oObject, bSaveObjectState);

        /// <summary>
        /// Executes the given query and fetches a row; returning true if row data was
        /// made available; false otherwise. Note that this will return false even if
        /// the query ran successfully but did not return data.
        /// You need to call SqlPrepareQuery() and potentially SqlBind* before calling this.
        /// Example:
        /// sqlquery n = SqlPrepareQueryObject(GetFirstPC(), "select widget from widgets;");
        /// while (SqlStep(n))
        ///   SendMessageToPC(GetFirstPC(), "Found widget: " + SqlGetString(n, 0));
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query to execute</param>
        /// <returns>True if row data was made available, false otherwise</returns>
        public static bool SqlStep(IntPtr sqlQuery) => _service.SqlStep(sqlQuery);

        /// <summary>
        /// Retrieves a column cast as an integer of the currently stepped row.
        /// You can call this after SqlStep() returned TRUE.
        /// In case of error, 0 will be returned.
        /// In traditional fashion, nIndex starts at 0.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to get the integer from</param>
        /// <param name="nIndex">The column index (starts at 0)</param>
        /// <returns>The integer value, or 0 on error</returns>
        public static int SqlGetInt(IntPtr sqlQuery, int nIndex) => _service.SqlGetInt(sqlQuery, nIndex);

        /// <summary>
        /// Retrieves a column cast as a float of the currently stepped row.
        /// You can call this after SqlStep() returned TRUE.
        /// In case of error, 0.0f will be returned.
        /// In traditional fashion, nIndex starts at 0.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to get the float from</param>
        /// <param name="nIndex">The column index (starts at 0)</param>
        /// <returns>The float value, or 0.0f on error</returns>
        public static float SqlGetFloat(IntPtr sqlQuery, int nIndex) => _service.SqlGetFloat(sqlQuery, nIndex);

        /// <summary>
        /// Retrieves a column cast as a string of the currently stepped row.
        /// You can call this after SqlStep() returned TRUE.
        /// In case of error, an empty string will be returned.
        /// In traditional fashion, nIndex starts at 0.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to get the string from</param>
        /// <param name="nIndex">The column index (starts at 0)</param>
        /// <returns>The string value, or empty string on error</returns>
        public static string SqlGetString(IntPtr sqlQuery, int nIndex) => _service.SqlGetString(sqlQuery, nIndex);

        /// <summary>
        /// Retrieves a vector of the currently stepped query.
        /// You can call this after SqlStep() returned TRUE.
        /// In case of error, a zero vector will be returned.
        /// In traditional fashion, nIndex starts at 0.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to get the vector from</param>
        /// <param name="nIndex">The column index (starts at 0)</param>
        /// <returns>The vector value, or zero vector on error</returns>
        public static Vector3 SqlGetVector(IntPtr sqlQuery, int nIndex) => _service.SqlGetVector(sqlQuery, nIndex);

        /// <summary>
        /// Retrieves an object of the currently stepped query.
        /// You can call this after SqlStep() returned TRUE.
        /// The object will be spawned into an inventory if it is an item and the receiver
        /// has the capability to receive it, otherwise at lSpawnAt.
        /// Objects are serialized, NOT stored as a reference!
        /// In case of error, INVALID_OBJECT will be returned.
        /// In traditional fashion, nIndex starts at 0.
        /// If bLoadObjectState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are read in.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to get the object from</param>
        /// <param name="nIndex">The column index (starts at 0)</param>
        /// <param name="lSpawnAt">The location to spawn the object at</param>
        /// <param name="oInventory">The inventory to spawn the object into (defaults to OBJECT_SELF)</param>
        /// <param name="bLoadObjectState">Whether to load object state (local vars, effects, action queue, etc.)</param>
        /// <returns>The object, or INVALID_OBJECT on error</returns>
        public static uint SqlGetObject(IntPtr sqlQuery, int nIndex, IntPtr lSpawnAt, uint oInventory = NWScriptService.OBJECT_INVALID, bool bLoadObjectState = false) => _service.SqlGetObject(sqlQuery, nIndex, lSpawnAt, oInventory, bLoadObjectState);

        /// <summary>
        /// Binds a JSON value to a named parameter of the given prepared query.
        /// JSON values are serialized into a string.
        /// Example:
        /// sqlquery v = SqlPrepareQueryObject(GetModule(), "insert into test (col) values (@myjson);");
        /// SqlBindJson(v, "@myjson", myJsonObject);
        /// SqlStep(v);
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query</param>
        /// <param name="sParam">The named parameter to bind to</param>
        /// <param name="jValue">The JSON value to bind</param>
        public static void SqlBindJson(SQLQuery sqlQuery, string sParam, Json jValue) => _service.SqlBindJson(sqlQuery, sParam, jValue);

        /// <summary>
        /// Retrieves a column cast as a JSON value of the currently stepped row.
        /// You can call this after SqlStep() returned TRUE.
        /// In case of error, a JSON null value will be returned.
        /// In traditional fashion, nIndex starts at 0.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to get the JSON from</param>
        /// <param name="nIndex">The column index (starts at 0)</param>
        /// <returns>The JSON value, or null on error</returns>
        public static Json SqlGetJson(SQLQuery sqlQuery, int nIndex) => _service.SqlGetJson(sqlQuery, nIndex);

        /// <summary>
        /// Resets the given SQL query, readying it for re-execution after it has been stepped.
        /// All existing binds are kept untouched, unless bClearBinds is TRUE.
        /// This command only works on successfully-prepared queries that have not errored out.
        /// </summary>
        /// <param name="sqlQuery">The SQL query to reset</param>
        /// <param name="bClearBinds">Whether to clear all existing parameter binds</param>
        public static void SqlResetQuery(SQLQuery sqlQuery, bool bClearBinds = false) => _service.SqlResetQuery(sqlQuery, bClearBinds);

        /// <summary>
        /// Retrieves the column count of a prepared query.
        /// sqlQuery must be prepared before this function is called, but can be called before or after parameters are bound.
        /// If the prepared query contains no columns (such as with an UPDATE or INSERT query), 0 is returned.
        /// If a non-SELECT query contains a RETURNING clause, the number of columns in the RETURNING clause will be returned.
        /// A returned value greater than 0 does not guarantee the query will return rows.
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query</param>
        /// <returns>The number of columns in the query result</returns>
        public static int SqlGetColumnCount(IntPtr sqlQuery) => _service.SqlGetColumnCount(sqlQuery);

        /// <summary>
        /// Retrieves the column name of the Nth column of a prepared query.
        /// sqlQuery must be prepared before this function is called, but can be called before or after parameters are bound.
        /// If the prepared query contains no columns (such as with an UPDATE or INSERT query), an empty string is returned.
        /// If a non-SELECT query contains a RETURNING clause, the name of the nNth column in the RETURNING clause is returned.
        /// If nNth is out of range, an sqlite error is broadcast and an empty string is returned.
        /// The value of the AS clause will be returned, if the clause exists for the nNth column.
        /// A returned non-empty string does not guarantee the query will return rows.
        /// </summary>
        /// <param name="sqlQuery">The prepared SQL query</param>
        /// <param name="nNth">The column index (0-based)</param>
        /// <returns>The column name, or empty string on error</returns>
        public static string SqlGetColumnName(IntPtr sqlQuery, int nNth) => _service.SqlGetColumnName(sqlQuery, nNth);

        /// <summary>
        /// Adjusts the alignment of the specified subject.
        /// </summary>
        /// <param name="oSubject">The subject whose alignment to adjust</param>
        /// <param name="nAlignment">The alignment type to adjust:
        /// - ALIGNMENT_LAWFUL/ALIGNMENT_CHAOTIC/ALIGNMENT_GOOD/ALIGNMENT_EVIL: Subject's alignment will be shifted in the direction specified
        /// - ALIGNMENT_ALL: nShift will be added to subject's law/chaos and good/evil alignment values
        /// - ALIGNMENT_NEUTRAL: nShift is applied to subject's law/chaos and good/evil alignment values in the direction which is towards neutrality</param>
        /// <param name="nShift">The desired shift in alignment. The shift will at most take the alignment value to 50 and not beyond</param>
        /// <param name="bAllPartyMembers">When true, the alignment shift also has a diminished effect on all members of the subject's party (if subject is a Player). When false, the shift only affects the subject (default: true)</param>
        /// <remarks>No return value. For example, if subject has a law/chaos value of 10 (chaotic) and a good/evil value of 80 (good), then if nShift is 15, the law/chaos value will become 25 and the good/evil value will become 55.</remarks>
        public static void AdjustAlignment(uint oSubject, AlignmentType nAlignment, int nShift,
            bool bAllPartyMembers = true) => _service.AdjustAlignment(oSubject, nAlignment, nShift, bAllPartyMembers);

        /// <summary>
        /// Gets an integer between 0 and 100 (inclusive) representing the creature's Law/Chaos alignment.
        /// </summary>
        /// <param name="oCreature">The creature to get the alignment value for</param>
        /// <returns>An integer between 0 and 100 (100=law, 0=chaos). Returns -1 if the creature is not valid</returns>
        public static int GetLawChaosValue(uint oCreature) => _service.GetLawChaosValue(oCreature);

        /// <summary>
        /// Gets an integer between 0 and 100 (inclusive) representing the creature's Good/Evil alignment.
        /// </summary>
        /// <param name="oCreature">The creature to get the alignment value for</param>
        /// <returns>An integer between 0 and 100 (100=good, 0=evil). Returns -1 if the creature is not valid</returns>
        public static int GetGoodEvilValue(uint oCreature) => _service.GetGoodEvilValue(oCreature);

        /// <summary>
        /// Returns an ALIGNMENT_* constant representing the creature's law/chaos alignment.
        /// </summary>
        /// <param name="oCreature">The creature to get the alignment for</param>
        /// <returns>An ALIGNMENT_* constant. Returns -1 if the creature is not valid</returns>
        public static AlignmentType GetAlignmentLawChaos(uint oCreature) => _service.GetAlignmentLawChaos(oCreature);

        /// <summary>
        /// Returns an ALIGNMENT_* constant representing the creature's good/evil alignment.
        /// </summary>
        /// <param name="oCreature">The creature to get the alignment for</param>
        /// <returns>An ALIGNMENT_* constant. Returns -1 if the creature is not valid</returns>
        public static AlignmentType GetAlignmentGoodEvil(uint oCreature) => _service.GetAlignmentGoodEvil(oCreature);

        /// <summary>
        /// Clears all personal feelings that the source has about the target.
        /// </summary>
        /// <param name="oTarget">The target to clear personal feelings about</param>
        /// <param name="oSource">The source whose personal feelings to clear (default: OBJECT_SELF)</param>
        public static void ClearPersonalReputation(uint oTarget, uint oSource = NWScriptService.OBJECT_INVALID) => _service.ClearPersonalReputation(oTarget, oSource);

        /// <summary>
        /// Makes the source temporarily friendly towards the target using personal reputation.
        /// </summary>
        /// <param name="oTarget">The target to befriend</param>
        /// <param name="oSource">The source to make friendly (default: OBJECT_SELF)</param>
        /// <param name="bDecays">If true, the friendship decays over the specified duration; otherwise it is indefinite (default: false)</param>
        /// <param name="fDurationInSeconds">The length of time the temporary friendship lasts (default: 180.0)</param>
        /// <remarks>If bDecays is true, the personal reputation amount decreases over time. Friendship will only be in effect as long as (faction reputation + total personal reputation) >= REPUTATION_TYPE_FRIEND.</remarks>
        public static void SetIsTemporaryFriend(uint oTarget, uint oSource = NWScriptService.OBJECT_INVALID, bool bDecays = false,
            float fDurationInSeconds = 180.0f) => _service.SetIsTemporaryFriend(oTarget, oSource, bDecays, fDurationInSeconds);

        /// <summary>
        /// Makes the source temporarily hostile towards the target using personal reputation.
        /// </summary>
        /// <param name="oTarget">The target to make hostile</param>
        /// <param name="oSource">The source to make hostile (default: OBJECT_SELF)</param>
        /// <param name="bDecays">If true, the enmity decays over the specified duration; otherwise it is indefinite (default: false)</param>
        /// <param name="fDurationInSeconds">The length of time the temporary enmity lasts (default: 180.0)</param>
        /// <remarks>If bDecays is true, the personal reputation amount decreases over time. Enmity will only be in effect as long as (faction reputation + total personal reputation) <= REPUTATION_TYPE_ENEMY.</remarks>
        public static void SetIsTemporaryEnemy(uint oTarget, uint oSource = NWScriptService.OBJECT_INVALID, bool bDecays = false,
            float fDurationInSeconds = 180.0f) => _service.SetIsTemporaryEnemy(oTarget, oSource, bDecays, fDurationInSeconds);

        /// <summary>
        /// Makes the source temporarily neutral towards the target using personal reputation.
        /// </summary>
        /// <param name="oTarget">The target to make neutral</param>
        /// <param name="oSource">The source to make neutral (default: OBJECT_SELF)</param>
        /// <param name="bDecays">If true, the neutrality decays over the specified duration; otherwise it is indefinite (default: false)</param>
        /// <param name="fDurationInSeconds">The length of time the temporary neutrality lasts (default: 180.0)</param>
        /// <remarks>If bDecays is true, the personal reputation amount decreases over time. Neutrality will only be in effect as long as (faction reputation + total personal reputation) > REPUTATION_TYPE_ENEMY and (faction reputation + total personal reputation) < REPUTATION_TYPE_FRIEND.</remarks>
        public static void SetIsTemporaryNeutral(uint oTarget, uint oSource = NWScriptService.OBJECT_INVALID, bool bDecays = false,
            float fDurationInSeconds = 180.0f) => _service.SetIsTemporaryNeutral(oTarget, oSource, bDecays, fDurationInSeconds);

        /// <summary>
        /// Assigns an action to the specified action subject.
        /// </summary>
        /// <param name="oActionSubject">The object to assign the action to</param>
        /// <param name="aActionToAssign">The action to assign</param>
        /// <remarks>No return value, but if an error occurs, the log file will contain "AssignCommand failed." (If the object doesn't exist, nothing happens.)</remarks>
        public static void AssignCommand(uint oActionSubject, Action aActionToAssign) => _service.AssignCommand(oActionSubject, aActionToAssign);

        /// <summary>
        /// Delays an action by the specified number of seconds.
        /// </summary>
        /// <param name="fSeconds">Number of seconds to delay the action</param>
        /// <param name="aActionToDelay">The action to delay</param>
        /// <remarks>No return value, but if an error occurs, the log file will contain "DelayCommand failed." It is suggested that functions which create effects should not be used as parameters to delayed actions. Instead, the effect should be created in the script and then passed into the action.</remarks>
        public static void DelayCommand(float fSeconds, Action aActionToDelay) => _service.DelayCommand(fSeconds, aActionToDelay);

        /// <summary>
        /// Executes the specified action immediately.
        /// </summary>
        /// <param name="aActionToDo">The action to execute</param>
        public static void ActionDoCommand(Action aActionToDo) => _service.ActionDoCommand(aActionToDo);

        /// <summary>
        /// Clears all actions of the target object.
        /// </summary>
        /// <param name="nClearCombatState">If true, immediately clears the combat state on a creature, stopping combat music and allowing rest, dialog, or other actions</param>
        /// <param name="oObject">The target object (defaults to OBJECT_SELF)</param>
        /// <remarks>No return value, but if an error occurs, the log file will contain "ClearAllActions failed."</remarks>
        public static void ClearAllActions(bool nClearCombatState = false, uint oObject = NWScriptService.OBJECT_INVALID) => _service.ClearAllActions(nClearCombatState, oObject);

        /// <summary>
        /// Makes the action subject generate a random location near its current location and pathfind to it.
        /// </summary>
        /// <remarks>ActionRandomWalk never ends, which means it is necessary to call ClearAllActions in order to allow a creature to perform any other action once ActionRandomWalk has been called. No return value, but if an error occurs the log file will contain "ActionRandomWalk failed."</remarks>
        public static void ActionRandomWalk() => _service.ActionRandomWalk();

        /// <summary>
        /// Makes the action subject move to the specified destination.
        /// </summary>
        /// <param name="lDestination">The location to move to. If the location is invalid or a path cannot be found, the command does nothing</param>
        /// <param name="bRun">If true, the action subject will run rather than walk (default: false)</param>
        /// <remarks>No return value, but if an error occurs the log file will contain "MoveToPoint failed."</remarks>
        public static void ActionMoveToLocation(Location lDestination, bool bRun = false) => _service.ActionMoveToLocation(lDestination, bRun);

        /// <summary>
        /// Makes the action subject move to a certain distance from the specified object.
        /// </summary>
        /// <param name="oMoveTo">The object to move to. If there is no path to this object, this command will do nothing</param>
        /// <param name="bRun">If true, the action subject will run rather than walk (default: false)</param>
        /// <param name="fRange">The desired distance between the action subject and the target object (default: 1.0)</param>
        /// <remarks>No return value, but if an error occurs the log file will contain "ActionMoveToObject failed."</remarks>
        public static void ActionMoveToObject(uint oMoveTo, bool bRun = false, float fRange = 1.0f) => _service.ActionMoveToObject(oMoveTo, bRun, fRange);

        /// <summary>
        /// Makes the action subject move away from the specified object to a certain distance.
        /// </summary>
        /// <param name="oFleeFrom">The object to move away from. If this object is not in the same area as the action subject, nothing will happen</param>
        /// <param name="bRun">If true, the action subject will run rather than walk (default: false)</param>
        /// <param name="fMoveAwayRange">The distance to put between the action subject and the target object (default: 40.0)</param>
        /// <remarks>No return value, but if an error occurs the log file will contain "ActionMoveAwayFromObject failed."</remarks>
        public static void ActionMoveAwayFromObject(uint oFleeFrom, bool bRun = false, float fMoveAwayRange = 40.0f) => _service.ActionMoveAwayFromObject(oFleeFrom, bRun, fMoveAwayRange);

        /// <summary>
        /// Makes the action subject play the specified animation.
        /// </summary>
        /// <param name="nAnimation">The animation to play (ANIMATION_* constant)</param>
        /// <param name="fSpeed">Speed of the animation (default: 1.0)</param>
        /// <param name="fDurationSeconds">Duration of the animation in seconds. This is not used for Fire and Forget animations (default: 0.0)</param>
        public static void ActionPlayAnimation(AnimationType nAnimation, float fSpeed = 1.0f, float fDurationSeconds = 0.0f) => _service.ActionPlayAnimation(nAnimation, fSpeed, fDurationSeconds);

        /// <summary>
        /// Makes the action subject cast a spell at the specified target.
        /// </summary>
        /// <param name="nSpell">The spell to cast (SPELL_* constant)</param>
        /// <param name="oTarget">The target for the spell</param>
        /// <param name="nMetaMagic">The metamagic to apply (default: MetaMagic.Any)</param>
        /// <param name="nCheat">If true, the executor doesn't need to be able to cast the spell (default: false)</param>
        /// <param name="nDomainLevel">Domain level (default: 0)</param>
        /// <param name="nProjectilePathType">The projectile path type (default: ProjectilePathType.Default)</param>
        /// <param name="bInstantSpell">If true, the spell is cast immediately, allowing simulation of high-level magic users with advance warning (default: false)</param>
        public static void ActionCastSpellAtObject(SpellType nSpell, uint oTarget, MetaMagicType nMetaMagic = MetaMagicType.Any,
            bool nCheat = false, int nDomainLevel = 0,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default, bool bInstantSpell = false) => _service.ActionCastSpellAtObject(nSpell, oTarget, nMetaMagic, nCheat, nDomainLevel, nProjectilePathType, bInstantSpell);

        /// <summary>
        /// Makes the action subject follow the specified object until ClearAllActions() is called.
        /// </summary>
        /// <param name="oFollow">The object to follow</param>
        /// <param name="fFollowDistance">Follow distance in meters (default: 0.0)</param>
        public static void ActionForceFollowObject(uint oFollow, float fFollowDistance = 0.0f) => _service.ActionForceFollowObject(oFollow, fFollowDistance);

        /// <summary>
        /// Makes the action subject sit in the specified chair.
        /// </summary>
        /// <param name="oChair">The chair to sit in. The object must be marked as usable in the toolset</param>
        /// <remarks>Not all creatures will be able to sit and not all objects can be sat on. To get a player to sit when they click on a chair, place the following script in the OnUsed event: void main() { object oChair = OBJECT_SELF; AssignCommand(GetLastUsedBy(),ActionSit(oChair)); }</remarks>
        public static void ActionSit(uint oChair) => _service.ActionSit(oChair);

        /// <summary>
        /// Makes the action subject jump to the specified object, or as near to it as possible.
        /// </summary>
        /// <param name="oToJumpTo">The object to jump to</param>
        /// <param name="bWalkStraightLineToPoint">If true, walks in a straight line to the point (default: true)</param>
        public static void ActionJumpToObject(uint oToJumpTo, bool bWalkStraightLineToPoint = true) => _service.ActionJumpToObject(oToJumpTo, bWalkStraightLineToPoint);

        /// <summary>
        /// Makes the action subject wait for the specified number of seconds.
        /// </summary>
        /// <param name="fSeconds">Number of seconds to wait</param>
        public static void ActionWait(float fSeconds) => _service.ActionWait(fSeconds);

        /// <summary>
        /// Starts a conversation with the specified object, causing their OnDialog event to fire.
        /// </summary>
        /// <param name="oObjectToConverseWith">The object to start a conversation with</param>
        /// <param name="sDialogResRef">If blank, the creature's own dialogue file will be used (default: empty string)</param>
        /// <param name="bPrivateConversation">Whether the conversation is private (default: true)</param>
        /// <param name="bPlayHello">If false, the initial greeting will not play (default: true)</param>
        public static void ActionStartConversation(uint oObjectToConverseWith, string sDialogResRef = "",
            bool bPrivateConversation = true, bool bPlayHello = true) => _service.ActionStartConversation(oObjectToConverseWith, sDialogResRef, bPrivateConversation, bPlayHello);

        /// <summary>
        /// Pauses the current conversation.
        /// </summary>
        public static void ActionPauseConversation() => _service.ActionPauseConversation();

        /// <summary>
        /// Resumes a conversation after it has been paused.
        /// </summary>
        public static void ActionResumeConversation() => _service.ActionResumeConversation();

        /// <summary>
        /// Makes the creature speak a translated string.
        /// </summary>
        /// <param name="nStrRef">Reference of the string in the talk table</param>
        /// <param name="nTalkVolume">The talk volume (TALKVOLUME_* constant) (default: TalkVolume.Talk)</param>
        public static void ActionSpeakStringByStrRef(int nStrRef, TalkVolumeType nTalkVolume = TalkVolumeType.Talk) => _service.ActionSpeakStringByStrRef(nStrRef, nTalkVolume);

        /// <summary>
        /// Makes the action subject use the specified feat on the target.
        /// </summary>
        /// <param name="nFeat">The feat to use (FEAT_* constant)</param>
        /// <param name="oTarget">The target to use the feat on</param>
        public static void ActionUseFeat(FeatType nFeat, uint oTarget) => _service.ActionUseFeat(nFeat, oTarget);

        /// <summary>
        /// Makes the action subject use the specified skill on the target.
        /// </summary>
        /// <param name="nSkill">The skill to use (SKILL_* constant)</param>
        /// <param name="oTarget">The target to use the skill on</param>
        /// <param name="nSubSkill">The subskill to use (SUBSKILL_* constant) (default: SubSkill.None)</param>
        /// <param name="oItemUsed">Item to use in conjunction with the skill (default: OBJECT_INVALID)</param>
        public static void ActionUseSkill(NWNSkillType nSkill, uint oTarget, SubSkillType nSubSkill = SubSkillType.None,
            uint oItemUsed = NWScriptService.OBJECT_INVALID) => _service.ActionUseSkill(nSkill, oTarget, nSubSkill, oItemUsed);

        /// <summary>
        /// Makes the action subject use the specified talent on the target object.
        /// </summary>
        /// <param name="tChosenTalent">The talent to use</param>
        /// <param name="oTarget">The target object to use the talent on</param>
        public static void ActionUseTalentOnObject(Talent tChosenTalent, uint oTarget) => _service.ActionUseTalentOnObject(tChosenTalent, oTarget);

        /// <summary>
        /// Makes the action subject use the specified talent at the target location.
        /// </summary>
        /// <param name="tChosenTalent">The talent to use</param>
        /// <param name="lTargetLocation">The target location to use the talent at</param>
        public static void ActionUseTalentAtLocation(Talent tChosenTalent, Location lTargetLocation) => _service.ActionUseTalentAtLocation(tChosenTalent, lTargetLocation);

        /// <summary>
        /// Makes the action subject jump to the specified destination. The action is added to the top of the action queue.
        /// </summary>
        /// <param name="lDestination">The destination location to jump to</param>
        public static void JumpToLocation(Location lDestination) => _service.JumpToLocation(lDestination);

        /// <summary>
        /// Queues an action to use an active item property on an object.
        /// </summary>
        /// <param name="oItem">The item that has the item property to use</param>
        /// <param name="ip">The item property to use</param>
        /// <param name="oTarget">The target object</param>
        /// <param name="nSubPropertyIndex">Specify if your item property has subproperties (such as subradial spells) (default: 0)</param>
        /// <param name="bDecrementCharges">Whether to decrement charges if the item property is limited (default: true)</param>
        public static void ActionUseItemOnObject(uint oItem, IntPtr ip, uint oTarget, int nSubPropertyIndex = 0, bool bDecrementCharges = true) => _service.ActionUseItemOnObject(oItem, ip, oTarget, nSubPropertyIndex, bDecrementCharges);

        /// <summary>
        /// Queues an action to use an active item property at a location.
        /// </summary>
        /// <param name="oItem">The item that has the item property to use</param>
        /// <param name="ip">The item property to use</param>
        /// <param name="lTarget">The target location (must be in the same area as item possessor)</param>
        /// <param name="nSubPropertyIndex">Specify if your item property has subproperties (such as subradial spells) (default: 0)</param>
        /// <param name="bDecrementCharges">Whether to decrement charges if the item property is limited (default: true)</param>
        public static void ActionUseItemAtLocation(uint oItem, IntPtr ip, IntPtr lTarget, int nSubPropertyIndex = 0, bool bDecrementCharges = true) => _service.ActionUseItemAtLocation(oItem, ip, lTarget, nSubPropertyIndex, bDecrementCharges);

        /// <summary>
        /// Causes the target object to face the specified direction.
        /// fDirection is expressed as anticlockwise degrees from Due East.
        /// DIRECTION_EAST, DIRECTION_NORTH, DIRECTION_WEST and DIRECTION_SOUTH are
        /// predefined. (0.0f=East, 90.0f=North, 180.0f=West, 270.0f=South)
        /// </summary>
        /// <param name="fDirection">The direction to face in anticlockwise degrees from Due East</param>
        /// <param name="oObject">The target object (defaults to OBJECT_SELF)</param>
        public static void SetFacing(float fDirection, uint oObject = NWScriptService.OBJECT_INVALID) => _service.SetFacing(fDirection, oObject);

        /// <summary>
        /// Gets the direction in which the target is facing, expressed as a float between
        /// 0.0f and 360.0f.
        /// </summary>
        /// <param name="oTarget">The target to get the facing direction for</param>
        /// <returns>The facing direction, or -1.0f on error</returns>
        public static float GetFacing(uint oTarget) => _service.GetFacing(oTarget);

        /// <summary>
        /// Causes the action subject to move away from the specified location.
        /// </summary>
        /// <param name="lMoveAwayFrom">The location to move away from</param>
        /// <param name="bRun">Whether to run (defaults to false)</param>
        /// <param name="fMoveAwayRange">The range to move away (defaults to 40.0f)</param>
        public static void ActionMoveAwayFromLocation(Location lMoveAwayFrom, bool bRun = false,
            float fMoveAwayRange = 40.0f) => _service.ActionMoveAwayFromLocation(lMoveAwayFrom, bRun, fMoveAwayRange);

        /// <summary>
        /// Jumps to the specified object (the action is added to the top of the action queue).
        /// </summary>
        /// <param name="oToJumpTo">The object to jump to</param>
        /// <param name="nWalkStraightLineToPoint">Whether to walk in a straight line to the point (defaults to true)</param>
        public static void JumpToObject(uint oToJumpTo, bool nWalkStraightLineToPoint = true) => _service.JumpToObject(oToJumpTo, nWalkStraightLineToPoint);

        /// <summary>
        /// Checks if the talent is valid.
        /// </summary>
        /// <param name="tTalent">The talent to check</param>
        /// <returns>TRUE if the talent is valid</returns>
        public static bool GetIsTalentValid(Talent tTalent) => _service.GetIsTalentValid(tTalent);

        /// <summary>
        /// Gets the type of the talent.
        /// </summary>
        /// <param name="tTalent">The talent to get the type for</param>
        /// <returns>The talent type (TALENT_TYPE_*)</returns>
        public static TalentType GetTypeFromTalent(Talent tTalent) => _service.GetTypeFromTalent(tTalent);

        /// <summary>
        /// Gets the ID of the talent.
        /// This could be a SPELL_*, FEAT_* or SKILL_*.
        /// </summary>
        /// <param name="tTalent">The talent to get the ID for</param>
        /// <returns>The ID of the talent</returns>
        public static int GetIdFromTalent(Talent tTalent) => _service.GetIdFromTalent(tTalent);

        /// <summary>
        /// Creates a spell talent.
        /// </summary>
        /// <param name="nSpell">SPELL_* constant</param>
        /// <returns>The created spell talent</returns>
        public static Talent TalentSpell(SpellType nSpell) => _service.TalentSpell(nSpell);

        /// <summary>
        /// Creates a feat talent.
        /// </summary>
        /// <param name="nFeat">FEAT_* constant</param>
        /// <returns>The created feat talent</returns>
        public static Talent TalentFeat(FeatType nFeat) => _service.TalentFeat(nFeat);

        /// <summary>
        /// Creates a skill talent.
        /// </summary>
        /// <param name="nSkill">SKILL_* constant</param>
        /// <returns>The created skill talent</returns>
        public static Talent TalentSkill(NWNSkillType nSkill) => _service.TalentSkill(nSkill);

        /// <summary>
        /// Determines whether the creature has the talent.
        /// </summary>
        /// <param name="tTalent">The talent to check for</param>
        /// <param name="oCreature">The creature to check (defaults to OBJECT_SELF)</param>
        /// <returns>TRUE if the creature has the talent</returns>
        public static bool GetCreatureHasTalent(Talent tTalent, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetCreatureHasTalent(tTalent, oCreature);

        /// <summary>
        /// Gets a random talent of the creature within the specified category.
        /// </summary>
        /// <param name="nCategory">TALENT_CATEGORY_* constant</param>
        /// <param name="oCreature">The creature to get the talent from (defaults to OBJECT_SELF)</param>
        /// <returns>A random talent from the specified category</returns>
        public static Talent GetCreatureTalentRandom(TalentCategoryType nCategory, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetCreatureTalentRandom(nCategory, oCreature);

        /// <summary>
        /// Gets the best talent (i.e. closest to nCRMax without going over) of the creature within the specified category.
        /// </summary>
        /// <param name="nCategory">TALENT_CATEGORY_* constant</param>
        /// <param name="nCRMax">Challenge Rating of the talent</param>
        /// <param name="oCreature">The creature to get the talent from (defaults to OBJECT_SELF)</param>
        /// <returns>The best talent from the specified category</returns>
        public static Talent GetCreatureTalentBest(TalentCategoryType nCategory, int nCRMax,
            uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetCreatureTalentBest(nCategory, nCRMax, oCreature);

        /// <summary>
        /// Returns the amount of gold a store currently has.
        /// -1 indicates it is not using gold.
        /// -2 indicates the store could not be located.
        /// </summary>
        /// <param name="oidStore">The store to get the gold amount for</param>
        /// <returns>The amount of gold the store has, or -1 if not using gold, or -2 if store not found</returns>
        public static int GetStoreGold(uint oidStore) => _service.GetStoreGold(oidStore);

        /// <summary>
        /// Sets the amount of gold a store has.
        /// -1 means the store does not use gold.
        /// </summary>
        /// <param name="oidStore">The store to set the gold amount for</param>
        /// <param name="nGold">The amount of gold to set (-1 means the store does not use gold)</param>
        public static void SetStoreGold(uint oidStore, int nGold) => _service.SetStoreGold(oidStore, nGold);

        /// <summary>
        /// Gets the maximum amount a store will pay for any item.
        /// -1 means price unlimited.
        /// -2 indicates the store could not be located.
        /// </summary>
        /// <param name="oidStore">The store to get the max buy price for</param>
        /// <returns>The maximum buy price, or -1 if unlimited, or -2 if store not found</returns>
        public static int GetStoreMaxBuyPrice(uint oidStore) => _service.GetStoreMaxBuyPrice(oidStore);

        /// <summary>
        /// Sets the maximum amount a store will pay for any item.
        /// -1 means price unlimited.
        /// </summary>
        /// <param name="oidStore">The store to set the max buy price for</param>
        /// <param name="nMaxBuy">The maximum amount the store will pay for any item (-1 means unlimited)</param>
        public static void SetStoreMaxBuyPrice(uint oidStore, int nMaxBuy) => _service.SetStoreMaxBuyPrice(oidStore, nMaxBuy);

        /// <summary>
        /// Gets the amount a store charges for identifying an item.
        /// Default is 100. -1 means the store will not identify items.
        /// -2 indicates the store could not be located.
        /// </summary>
        /// <param name="oidStore">The store to get the identify cost for</param>
        /// <returns>The identify cost, or -1 if store will not identify items, or -2 if store not found</returns>
        public static int GetStoreIdentifyCost(uint oidStore) => _service.GetStoreIdentifyCost(oidStore);

        /// <summary>
        /// Sets the amount a store charges for identifying an item.
        /// Default is 100. -1 means the store will not identify items.
        /// </summary>
        /// <param name="oidStore">The store to set the identify cost for</param>
        /// <param name="nCost">The cost for identifying items (-1 means store will not identify items)</param>
        public static void SetStoreIdentifyCost(uint oidStore, int nCost) => _service.SetStoreIdentifyCost(oidStore, nCost);

        /// <summary>
        /// Opens the store for the player character.
        /// </summary>
        /// <param name="oStore">The store to open</param>
        /// <param name="oPC">The player character to open the store for</param>
        /// <param name="nBonusMarkUp">Added to the store's default mark up percentage on items sold (-100 to 100)</param>
        /// <param name="nBonusMarkDown">Added to the store's default mark down percentage on items bought (-100 to 100)</param>
        public static void OpenStore(uint oStore, uint oPC, int nBonusMarkUp = 0, int nBonusMarkDown = 0) => _service.OpenStore(oStore, oPC, nBonusMarkUp, nBonusMarkDown);

        /// <summary>
        /// Converts an integer to hex, returning the hex value as a string.
        /// </summary>
        /// <param name="nInteger">The integer to convert to hex</param>
        /// <returns>Hex value as a string with format "0x????????" where each ? is a hex digit (8 digits in total)</returns>
        public static string IntToHexString(int nInteger) => _service.IntToHexString(nInteger);

        /// <summary>
        /// Spawns the script debugger.
        /// This will cause the script debugger to be executed after this command is executed!
        /// In order to compile the script for debugging go to Tools->Options->Script Editor
        /// and check the box labeled "Generate Debug Information When Compiling Scripts".
        /// After you have checked the above box, recompile the script that you want to debug.
        /// If the script file isn't compiled for debugging, this command will do nothing.
        /// Remove any SpawnScriptDebugger() calls once you have finished debugging the script.
        /// </summary>
        public static void SpawnScriptDebugger() => _service.SpawnScriptDebugger();

        /// <summary>
        /// Executes a script chunk.
        /// The script chunk runs immediately, same as ExecuteScript().
        /// The script is jitted in place and currently not cached: Each invocation will recompile the script chunk.
        /// Note that the script chunk will run as if a separate script. This is not eval().
        /// By default, the script chunk is wrapped into void main() {}. Pass in bWrapIntoMain = FALSE to override.
        /// </summary>
        /// <param name="sScriptChunk">The script chunk to execute</param>
        /// <param name="oObject">The object to execute the script on</param>
        /// <param name="bWrapIntoMain">Whether to wrap the script chunk into void main() {}</param>
        /// <returns>Empty string on success, or the compilation error</returns>
        public static string ExecuteScriptChunk(string sScriptChunk, uint oObject = NWScriptService.OBJECT_INVALID, bool bWrapIntoMain = true) => _service.ExecuteScriptChunk(sScriptChunk, oObject, bWrapIntoMain);

        /// <summary>
        /// Returns a random UUID.
        /// This UUID will not be associated with any object.
        /// The generated UUID is currently a v4.
        /// </summary>
        /// <returns>A random UUID string</returns>
        public static string GetRandomUUID() => _service.GetRandomUUID();

        /// <summary>
        /// Returns the given object's UUID.
        /// This UUID is persisted across save boundaries, like Save/RestoreCampaignObject and save games.
        /// Thus, reidentification is only guaranteed in scenarios where players cannot introduce
        /// new objects (i.e. servervault servers).
        /// UUIDs are guaranteed to be unique in any single running game.
        /// If a loaded object would collide with a UUID already present in the game, the
        /// object receives no UUID and a warning is emitted to the log. Requesting a UUID
        /// for the new object will generate a random one.
        /// This UUID is useful to, for example:
        /// - Safely identify servervault characters
        /// - Track serialisable objects (like items or creatures) as they are saved to the
        /// campaign DB - i.e. persistent storage chests or dropped items.
        /// - Track objects across multiple game instances (in trusted scenarios).
        /// Currently, the following objects can carry UUIDs:
        /// Items, Creatures, Placeables, Triggers, Doors, Waypoints, Stores,
        /// Encounters, Areas.
        /// </summary>
        /// <param name="oObject">The object to get the UUID for</param>
        /// <returns>The object's UUID, or empty string when the given object cannot carry a UUID</returns>
        public static string GetObjectUUID(uint oObject) => _service.GetObjectUUID(oObject);

        /// <summary>
        /// Forces the given object to receive a new UUID, discarding the current value.
        /// </summary>
        /// <param name="oObject">The object to refresh the UUID for</param>
        public static void ForceRefreshObjectUUID(uint oObject) => _service.ForceRefreshObjectUUID(oObject);

        /// <summary>
        /// Looks up an object on the server by its UUID.
        /// </summary>
        /// <param name="sUUID">The UUID to look up</param>
        /// <returns>The object with the given UUID, or OBJECT_INVALID if the UUID is not on the server</returns>
        public static uint GetObjectByUUID(string sUUID) => _service.GetObjectByUUID(sUUID);

        /// <summary>
        /// Reserved function - do not call.
        /// This does nothing on this platform except to return an error.
        /// </summary>
        public static void Reserved899() => _service.Reserved899();

        /// <summary>
        /// Use this in an OnPerception script to get the object that was perceived.
        /// </summary>
        /// <returns>The object that was perceived, or OBJECT_INVALID if the caller is not a valid creature</returns>
        public static uint GetLastPerceived() => _service.GetLastPerceived();

        /// <summary>
        /// Use this in an OnPerception script to determine whether the object that was perceived was heard.
        /// </summary>
        /// <returns>TRUE if the object was heard</returns>
        public static bool GetLastPerceptionHeard() => _service.GetLastPerceptionHeard();

        /// <summary>
        /// Use this in an OnPerception script to determine whether the object that was perceived has become inaudible.
        /// </summary>
        /// <returns>TRUE if the object has become inaudible</returns>
        public static bool GetLastPerceptionInaudible() => _service.GetLastPerceptionInaudible();

        /// <summary>
        /// Use this in an OnPerception script to determine whether the object that was perceived was seen.
        /// </summary>
        /// <returns>TRUE if the object was seen</returns>
        public static bool GetLastPerceptionSeen() => _service.GetLastPerceptionSeen();

        /// <summary>
        /// Use this in an OnPerception script to determine whether the object that was perceived has vanished.
        /// </summary>
        /// <returns>TRUE if the object has vanished</returns>
        public static bool GetLastPerceptionVanished() => _service.GetLastPerceptionVanished();

        /// <summary>
        /// Gets the PC that sent the last player chat (text) message.
        /// </summary>
        /// <returns>The PC that sent the last chat message. Returns OBJECT_INVALID on error</returns>
        /// <remarks>Should only be called from a module's OnPlayerChat event script. Private tells do not trigger an OnPlayerChat event.</remarks>
        public static uint GetPCChatSpeaker() => _service.GetPCChatSpeaker();

        /// <summary>
        /// Sends a message to all the Dungeon Masters currently on the server.
        /// </summary>
        /// <param name="szMessage">The message to send to all DMs</param>
        public static void SendMessageToAllDMs(string szMessage) => _service.SendMessageToAllDMs(szMessage);

        /// <summary>
        /// Gets the last player chat (text) message that was sent.
        /// </summary>
        /// <returns>The last chat message. Returns empty string on error</returns>
        /// <remarks>Should only be called from a module's OnPlayerChat event script. Private tells do not trigger an OnPlayerChat event.</remarks>
        public static string GetPCChatMessage() => _service.GetPCChatMessage();

        /// <summary>
        /// Gets the volume of the last player chat (text) message that was sent.
        /// </summary>
        /// <returns>One of the TALKVOLUME_* constants based on the volume setting that the player used to send the chat message. Returns -1 on error</returns>
        /// <remarks>Should only be called from a module's OnPlayerChat event script. Private tells do not trigger an OnPlayerChat event. Possible values: TALKVOLUME_TALK, TALKVOLUME_WHISPER, TALKVOLUME_SHOUT, TALKVOLUME_SILENT_SHOUT (used for DM chat channel), TALKVOLUME_PARTY</remarks>
        public static TalkVolumeType GetPCChatVolume() => _service.GetPCChatVolume();

        /// <summary>
        /// Sets the last player chat (text) message before it gets sent to other players.
        /// </summary>
        /// <param name="sNewChatMessage">The new chat text to be sent to other players. Setting to an empty string will cause the chat message to be discarded (not sent to other players) (default: empty string)</param>
        /// <remarks>The new chat message gets sent after the OnPlayerChat script exits.</remarks>
        public static void SetPCChatMessage(string sNewChatMessage = "") => _service.SetPCChatMessage(sNewChatMessage);

        /// <summary>
        /// Sets the last player chat (text) volume before it gets sent to other players.
        /// </summary>
        /// <param name="nTalkVolume">The new volume of the chat text to be sent to other players (default: TalkVolume.Talk)</param>
        /// <remarks>The new chat message gets sent after the OnPlayerChat script exits. Possible values: TALKVOLUME_TALK, TALKVOLUME_WHISPER, TALKVOLUME_SHOUT, TALKVOLUME_SILENT_SHOUT (used for DM chat channel), TALKVOLUME_PARTY, TALKVOLUME_TELL (sends the chat message privately back to the original speaker)</remarks>
        public static void SetPCChatVolume(TalkVolumeType nTalkVolume = TalkVolumeType.Talk) => _service.SetPCChatVolume(nTalkVolume);

        /// <summary>
        /// Gets the weakest member of the faction member's faction.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check (default: OBJECT_SELF)</param>
        /// <param name="bMustBeVisible">Whether the member must be visible (default: true)</param>
        /// <returns>The weakest faction member. Returns OBJECT_INVALID if the faction member's faction is invalid</returns>
        public static uint GetFactionWeakestMember(uint oFactionMember = NWScriptService.OBJECT_INVALID, bool bMustBeVisible = true) => _service.GetFactionWeakestMember(oFactionMember, bMustBeVisible);

        /// <summary>
        /// Gets the strongest member of the faction member's faction.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check (default: OBJECT_SELF)</param>
        /// <param name="bMustBeVisible">Whether the member must be visible (default: true)</param>
        /// <returns>The strongest faction member. Returns OBJECT_INVALID if the faction member's faction is invalid</returns>
        public static uint GetFactionStrongestMember(uint oFactionMember = NWScriptService.OBJECT_INVALID, bool bMustBeVisible = true) => _service.GetFactionStrongestMember(oFactionMember, bMustBeVisible);

        /// <summary>
        /// Gets the member of the faction member's faction that has taken the most hit points of damage.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check (default: OBJECT_SELF)</param>
        /// <param name="bMustBeVisible">Whether the member must be visible (default: true)</param>
        /// <returns>The most damaged faction member. Returns OBJECT_INVALID if the faction member's faction is invalid</returns>
        public static uint GetFactionMostDamagedMember(uint oFactionMember = NWScriptService.OBJECT_INVALID, bool bMustBeVisible = true) => _service.GetFactionMostDamagedMember(oFactionMember, bMustBeVisible);

        /// <summary>
        /// Gets the member of the faction member's faction that has taken the fewest hit points of damage.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check (default: OBJECT_SELF)</param>
        /// <param name="bMustBeVisible">Whether the member must be visible (default: true)</param>
        /// <returns>The least damaged faction member. Returns OBJECT_INVALID if the faction member's faction is invalid</returns>
        public static uint GetFactionLeastDamagedMember(uint oFactionMember = NWScriptService.OBJECT_INVALID,
            bool bMustBeVisible = true) => _service.GetFactionLeastDamagedMember(oFactionMember, bMustBeVisible);

        /// <summary>
        /// Gets the amount of gold held by the faction member's faction.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check</param>
        /// <returns>The amount of gold held by the faction. Returns -1 if the faction member's faction is invalid</returns>
        public static int GetFactionGold(uint oFactionMember) => _service.GetFactionGold(oFactionMember);

        /// <summary>
        /// Gets an integer between 0 and 100 (inclusive) that represents how the source faction member's faction feels about the target.
        /// </summary>
        /// <param name="oSourceFactionMember">The source faction member</param>
        /// <param name="oTarget">The target to check reputation for</param>
        /// <returns>An integer between 0 and 100 representing the reputation. Returns -1 on error</returns>
        public static int GetFactionAverageReputation(uint oSourceFactionMember, uint oTarget) => _service.GetFactionAverageReputation(oSourceFactionMember, oTarget);

        /// <summary>
        /// Gets an integer between 0 and 100 (inclusive) that represents the average good/evil alignment of the faction member's faction.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check</param>
        /// <returns>An integer between 0 and 100 representing the average good/evil alignment. Returns -1 on error</returns>
        public static int GetFactionAverageGoodEvilAlignment(uint oFactionMember) => _service.GetFactionAverageGoodEvilAlignment(oFactionMember);

        /// <summary>
        /// Gets an integer between 0 and 100 (inclusive) that represents the average law/chaos alignment of the faction member's faction.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check</param>
        /// <returns>An integer between 0 and 100 representing the average law/chaos alignment. Returns -1 on error</returns>
        public static int GetFactionAverageLawChaosAlignment(uint oFactionMember) => _service.GetFactionAverageLawChaosAlignment(oFactionMember);

        /// <summary>
        /// Gets the average level of the members of the faction.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check</param>
        /// <returns>The average level of the faction members. Returns -1 on error</returns>
        public static int GetFactionAverageLevel(uint oFactionMember) => _service.GetFactionAverageLevel(oFactionMember);

        /// <summary>
        /// Gets the average XP of the members of the faction.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check</param>
        /// <returns>The average XP of the faction members. Returns -1 on error</returns>
        public static int GetFactionAverageXP(uint oFactionMember) => _service.GetFactionAverageXP(oFactionMember);

        /// <summary>
        /// Gets the most frequent class in the faction.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check</param>
        /// <returns>The most frequent class in the faction (can be compared with CLASS_TYPE_* constants). Returns -1 on error</returns>
        public static int GetFactionMostFrequentClass(uint oFactionMember) => _service.GetFactionMostFrequentClass(oFactionMember);

        /// <summary>
        /// Gets the faction member with the lowest armor class.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check (default: OBJECT_SELF)</param>
        /// <param name="bMustBeVisible">Whether the member must be visible (default: true)</param>
        /// <returns>The faction member with the worst AC. Returns OBJECT_INVALID if the faction member's faction is invalid</returns>
        public static uint GetFactionWorstAC(uint oFactionMember = NWScriptService.OBJECT_INVALID, bool bMustBeVisible = true) => _service.GetFactionWorstAC(oFactionMember, bMustBeVisible);

        /// <summary>
        /// Gets the faction member with the highest armor class.
        /// </summary>
        /// <param name="oFactionMember">The faction member to check (default: OBJECT_SELF)</param>
        /// <param name="bMustBeVisible">Whether the member must be visible (default: true)</param>
        /// <returns>The faction member with the best AC. Returns OBJECT_INVALID if the faction member's faction is invalid</returns>
        public static uint GetFactionBestAC(uint oFactionMember = NWScriptService.OBJECT_INVALID, bool bMustBeVisible = true) => _service.GetFactionBestAC(oFactionMember, bMustBeVisible);

        /// <summary>
        /// Gets an integer between 0 and 100 (inclusive) that represents how the source feels about the target.
        /// </summary>
        /// <param name="oSource">The source object</param>
        /// <param name="oTarget">The target object</param>
        /// <returns>An integer between 0 and 100 representing the reputation. 0-10 means hostile, 11-89 means neutral, 90-100 means friendly. Returns -1 if oSource or oTarget does not identify a valid object</returns>
        public static int GetReputation(uint oSource, uint oTarget) => _service.GetReputation(oSource, oTarget);

        /// <summary>
        /// Adjusts how the source faction member's faction feels about the target by the specified amount.
        /// </summary>
        /// <param name="oTarget">The target to adjust reputation for</param>
        /// <param name="oSourceFactionMember">The source faction member</param>
        /// <param name="nAdjustment">The amount to adjust the reputation by</param>
        /// <remarks>This adjusts Faction Reputation, how the entire faction that oSourceFactionMember is in, feels about oTarget. You can't adjust a player character's (PC) faction towards NPCs, so attempting to make an NPC hostile by passing in a PC object as oSourceFactionMember in the following call will fail: AdjustReputation(oNPC,oPC,-100); Instead you should pass in the PC object as the first parameter as in the following call which should succeed: AdjustReputation(oPC,oNPC,-100); Will fail if oSourceFactionMember is a plot object.</remarks>
        public static void AdjustReputation(uint oTarget, uint oSourceFactionMember, int nAdjustment) => _service.AdjustReputation(oTarget, oSourceFactionMember, nAdjustment);

        /// <summary>
        /// Returns true if the source considers the target as an enemy.
        /// </summary>
        /// <param name="oTarget">The target to check</param>
        /// <param name="oSource">The source to check from (default: OBJECT_SELF)</param>
        /// <returns>True if the source considers the target as an enemy, false otherwise</returns>
        public static bool GetIsEnemy(uint oTarget, uint oSource = NWScriptService.OBJECT_INVALID) => _service.GetIsEnemy(oTarget, oSource);

        /// <summary>
        /// Returns true if the source considers the target as a friend.
        /// </summary>
        /// <param name="oTarget">The target to check</param>
        /// <param name="oSource">The source to check from (default: OBJECT_SELF)</param>
        /// <returns>True if the source considers the target as a friend, false otherwise</returns>
        public static bool GetIsFriend(uint oTarget, uint oSource = NWScriptService.OBJECT_INVALID) => _service.GetIsFriend(oTarget, oSource);

        /// <summary>
        /// Returns true if the source considers the target as neutral.
        /// </summary>
        /// <param name="oTarget">The target to check</param>
        /// <param name="oSource">The source to check from (default: OBJECT_SELF)</param>
        /// <returns>True if the source considers the target as neutral, false otherwise</returns>
        public static bool GetIsNeutral(uint oTarget, uint oSource = NWScriptService.OBJECT_INVALID) => _service.GetIsNeutral(oTarget, oSource);

        /// <summary>
        /// Gets the player leader of the faction of which the member is a member.
        /// </summary>
        /// <param name="oMemberOfFaction">The faction member to check</param>
        /// <returns>The faction leader. Returns OBJECT_INVALID if the member is not a valid creature, or the member is a member of an NPC faction</returns>
        public static uint GetFactionLeader(uint oMemberOfFaction) => _service.GetFactionLeader(oMemberOfFaction);

        /// <summary>
        /// Sets how the standard faction feels about the specified creature.
        /// </summary>
        /// <param name="nStandardFaction">The standard faction (STANDARD_FACTION_* constants)</param>
        /// <param name="nNewReputation">The new reputation (0-100 inclusive)</param>
        /// <param name="oCreature">The creature to set the reputation for (default: OBJECT_SELF)</param>
        public static void SetStandardFactionReputation(StandardFactionType nStandardFaction, int nNewReputation,
            uint oCreature = NWScriptService.OBJECT_INVALID) => _service.SetStandardFactionReputation(nStandardFaction, nNewReputation, oCreature);

        /// <summary>
        /// Finds out how the standard faction feels about the specified creature.
        /// </summary>
        /// <param name="nStandardFaction">The standard faction (STANDARD_FACTION_* constants)</param>
        /// <param name="oCreature">The creature to check the reputation for (default: OBJECT_SELF)</param>
        /// <returns>Returns -1 on an error. Returns 0-100 based on the standing of the creature within the faction. 0-10: Hostile, 11-89: Neutral, 90-100: Friendly</returns>
        public static int GetStandardFactionReputation(StandardFactionType nStandardFaction, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetStandardFactionReputation(nStandardFaction, oCreature);

        /// <summary>
        /// Makes the creature join one of the standard factions.
        /// </summary>
        /// <param name="oCreatureToChange">The creature to change the faction for</param>
        /// <param name="nStandardFaction">The standard faction to join (STANDARD_FACTION_* constants)</param>
        /// <remarks>This will only work on an NPC.</remarks>
        public static void ChangeToStandardFaction(uint oCreatureToChange, StandardFactionType nStandardFaction) => _service.ChangeToStandardFaction(oCreatureToChange, nStandardFaction);

        /// <summary>
        /// Gets the first member of the faction member's faction.
        /// </summary>
        /// <param name="oMemberOfFaction">The faction member to get the first member for</param>
        /// <param name="bPCOnly">Whether to only return PC members (default: true)</param>
        /// <returns>The first faction member. Returns OBJECT_INVALID if the faction member's faction is invalid</returns>
        public static uint GetFirstFactionMember(uint oMemberOfFaction, bool bPCOnly = true) => _service.GetFirstFactionMember(oMemberOfFaction, bPCOnly);

        /// <summary>
        /// Gets the next member of the faction member's faction.
        /// </summary>
        /// <param name="oMemberOfFaction">The faction member to get the next member for</param>
        /// <param name="bPCOnly">Whether to only return PC members (default: true)</param>
        /// <returns>The next faction member. Returns OBJECT_INVALID if the faction member's faction is invalid</returns>
        public static uint GetNextFactionMember(uint oMemberOfFaction, bool bPCOnly = true) => _service.GetNextFactionMember(oMemberOfFaction, bPCOnly);

        /// <summary>
        /// Returns true if the faction IDs of the two objects are the same.
        /// </summary>
        /// <param name="oFirstObject">The first object to compare</param>
        /// <param name="oSecondObject">The second object to compare (default: OBJECT_SELF)</param>
        /// <returns>True if the faction IDs are the same, false otherwise</returns>
        public static bool GetFactionEqual(uint oFirstObject, uint oSecondObject = NWScriptService.OBJECT_INVALID) => _service.GetFactionEqual(oFirstObject, oSecondObject);

        /// <summary>
        /// Makes the object join the faction of the specified faction member.
        /// </summary>
        /// <param name="oObjectToChangeFaction">The object to change the faction for</param>
        /// <param name="oMemberOfFactionToJoin">The faction member whose faction to join</param>
        /// <remarks>This will only work for two NPCs.</remarks>
        public static void ChangeFaction(uint oObjectToChangeFaction, uint oMemberOfFactionToJoin) => _service.ChangeFaction(oObjectToChangeFaction, oMemberOfFactionToJoin);

        /// <summary>
        /// Checks if it is currently day.
        /// </summary>
        /// <returns>TRUE if it is currently day</returns>
        public static bool GetIsDay() => _service.GetIsDay();

        /// <summary>
        /// Checks if it is currently night.
        /// </summary>
        /// <returns>TRUE if it is currently night</returns>
        public static bool GetIsNight() => _service.GetIsNight();

        /// <summary>
        /// Checks if it is currently dawn.
        /// </summary>
        /// <returns>TRUE if it is currently dawn</returns>
        public static bool GetIsDawn() => _service.GetIsDawn();

        /// <summary>
        /// Checks if it is currently dusk.
        /// </summary>
        /// <returns>TRUE if it is currently dusk</returns>
        public static bool GetIsDusk() => _service.GetIsDusk();

        /// <summary>
        /// Converts rounds into a number of seconds.
        /// A round is always 6.0 seconds.
        /// </summary>
        /// <param name="nRounds">The number of rounds to convert</param>
        /// <returns>The number of seconds</returns>
        public static float RoundsToSeconds(int nRounds) => _service.RoundsToSeconds(nRounds);

        /// <summary>
        /// Converts hours into a number of seconds.
        /// The result will depend on how many minutes there are per hour (game-time).
        /// </summary>
        /// <param name="nHours">The number of hours to convert</param>
        /// <returns>The number of seconds</returns>
        public static float HoursToSeconds(int nHours) => _service.HoursToSeconds(nHours);

        /// <summary>
        /// Converts turns into a number of seconds.
        /// A turn is always 60.0 seconds.
        /// </summary>
        /// <param name="nTurns">The number of turns to convert</param>
        /// <returns>The number of seconds</returns>
        public static float TurnsToSeconds(int nTurns) => _service.TurnsToSeconds(nTurns);

        /// <summary>
        /// Gets the current hour (0-23).
        /// </summary>
        /// <returns>The current hour</returns>
        public static int GetTimeHour() => _service.GetTimeHour();

        /// <summary>
        /// Gets the current minute (0-59).
        /// </summary>
        /// <returns>The current minute</returns>
        public static int GetTimeMinute() => _service.GetTimeMinute();

        /// <summary>
        /// Gets the current second (0-59).
        /// </summary>
        /// <returns>The current second</returns>
        public static int GetTimeSecond() => _service.GetTimeSecond();

        /// <summary>
        /// Gets the current millisecond (0-999).
        /// </summary>
        /// <returns>The current millisecond</returns>
        public static int GetTimeMillisecond() => _service.GetTimeMillisecond();

        /// <summary>
        /// Determines whether the specified object is in conversation.
        /// </summary>
        /// <param name="oObject">The object to check</param>
        /// <returns>True if the object is in conversation, false otherwise</returns>
        public static bool IsInConversation(uint oObject) => _service.IsInConversation(oObject);

        /// <summary>
        /// Adds a speak action to the action subject.
        /// </summary>
        /// <param name="sStringToSpeak">The string to be spoken</param>
        /// <param name="nTalkVolume">The talk volume (TALKVOLUME_* constants) (default: TalkVolume.Talk)</param>
        public static void ActionSpeakString(string sStringToSpeak, TalkVolumeType nTalkVolume = TalkVolumeType.Talk) => _service.ActionSpeakString(sStringToSpeak, nTalkVolume);

        /// <summary>
        /// Gets the person with whom you are conversing.
        /// </summary>
        /// <returns>The last speaker. Returns OBJECT_INVALID if the caller is not a valid creature</returns>
        /// <remarks>Use this in a conversation script.</remarks>
        public static uint GetLastSpeaker() => _service.GetLastSpeaker();

        /// <summary>
        /// Starts up the dialog tree.
        /// </summary>
        /// <param name="sResRef">The dialog file to use. If not specified, the default dialog file will be used (default: empty string)</param>
        /// <param name="oObjectToDialog">The object to dialog with. If not specified, the person that triggered the event will be used (default: OBJECT_INVALID)</param>
        /// <returns>The result of beginning the conversation</returns>
        /// <remarks>Use this in an OnDialog script.</remarks>
        public static int BeginConversation(string sResRef = "", uint oObjectToDialog = NWScriptService.OBJECT_INVALID) => _service.BeginConversation(sResRef, oObjectToDialog);

        /// <summary>
        /// Plays the specified animation immediately.
        /// </summary>
        /// <param name="nAnimation">The animation to play (ANIMATION_* constant)</param>
        /// <param name="fSpeed">The speed of the animation (default: 1.0)</param>
        /// <param name="fSeconds">The duration of the animation in seconds (default: 0.0)</param>
        public static void PlayAnimation(AnimationType nAnimation, float fSpeed = 1.0f, float fSeconds = 0.0f) => _service.PlayAnimation(nAnimation, fSpeed, fSeconds);

        /// <summary>
        /// Vibrates the player's device or controller. Does nothing if vibration is not supported.
        /// </summary>
        /// <param name="oPlayer">The player whose device should vibrate</param>
        /// <param name="nMotor">One of the VIBRATOR_MOTOR_* constants</param>
        /// <param name="fStrength">Vibration strength between 0.0 and 1.0</param>
        /// <param name="fSeconds">Number of seconds to vibrate</param>
        public static void Vibrate(uint oPlayer, int nMotor, float fStrength, float fSeconds) => _service.Vibrate(oPlayer, nMotor, fStrength, fSeconds);

        /// <summary>
        /// Unlocks an achievement for the given player who must be logged in.
        /// </summary>
        /// <param name="oPlayer">The player for whom to unlock the achievement</param>
        /// <param name="sId">The achievement ID on the remote server</param>
        /// <param name="nLastValue">The previous value of the associated achievement stat (default: 0)</param>
        /// <param name="nCurValue">The current value of the associated achievement stat (default: 0)</param>
        /// <param name="nMaxValue">The maximum value of the associated achievement stat (default: 0)</param>
        public static void UnlockAchievement(uint oPlayer, string sId, int nLastValue = 0, int nCurValue = 0,
            int nMaxValue = 0) => _service.UnlockAchievement(oPlayer, sId, nLastValue, nCurValue, nMaxValue);

        /// <summary>
        /// Creates a NUI window from the given resref(.jui) for the given player.
        /// The resref needs to be available on the client, not the server.
        /// The token is an integer for ease of handling only. You are not supposed to do anything with it, except store/pass it.
        /// The window ID needs to be alphanumeric and short. Only one window (per client) with the same ID can exist at a time.
        /// Re-creating a window with the same id of one already open will immediately close the old one.
        /// See nw_inc_nui.nss for full documentation.
        /// </summary>
        /// <param name="oPlayer">The player to create the window for</param>
        /// <param name="sResRef">The resref of the .jui file</param>
        /// <param name="sWindowId">The window ID (defaults to empty string)</param>
        /// <returns>The window token on success (>0), or 0 on error</returns>
        public static int NuiCreateFromResRef(uint oPlayer, string sResRef, string sWindowId = "") => _service.NuiCreateFromResRef(oPlayer, sResRef, sWindowId);

        /// <summary>
        /// Creates a NUI window inline for the given player.
        /// The token is an integer for ease of handling only. You are not supposed to do anything with it, except store/pass it.
        /// The window ID needs to be alphanumeric and short. Only one window (per client) with the same ID can exist at a time.
        /// Re-creating a window with the same id of one already open will immediately close the old one.
        /// See nw_inc_nui.nss for full documentation.
        /// </summary>
        /// <param name="oPlayer">The player to create the window for</param>
        /// <param name="jNui">The NUI JSON definition</param>
        /// <param name="sWindowId">The window ID (defaults to empty string)</param>
        /// <returns>The window token on success (>0), or 0 on error</returns>
        public static int NuiCreate(uint oPlayer, Json jNui, string sWindowId = "") => _service.NuiCreate(oPlayer, jNui, sWindowId);

        /// <summary>
        /// You can look up windows by ID, if you gave them one.
        /// Windows with an ID present are singletons - attempting to open a second one with the same ID
        /// will fail, even if the json definition is different.
        /// </summary>
        /// <param name="oPlayer">The player to look up the window for</param>
        /// <param name="sId">The window ID to look up</param>
        /// <returns>The token if found, or 0</returns>
        public static int NuiFindWindow(uint oPlayer, string sId) => _service.NuiFindWindow(oPlayer, sId);

        /// <summary>
        /// Destroys the given window, by token, immediately closing it on the client.
        /// Does nothing if nUiToken does not exist on the client.
        /// Does not send a close event - this immediately destroys all serverside state.
        /// The client will close the window asynchronously.
        /// </summary>
        /// <param name="oPlayer">The player to destroy the window for</param>
        /// <param name="nUiToken">The window token to destroy</param>
        public static void NuiDestroy(uint oPlayer, int nUiToken) => _service.NuiDestroy(oPlayer, nUiToken);

        /// <summary>
        /// Returns the originating player of the current event.
        /// </summary>
        /// <returns>The originating player of the current event</returns>
        public static uint NuiGetEventPlayer() => _service.NuiGetEventPlayer();

        /// <summary>
        /// Gets the window token of the current event (or 0 if not in an event).
        /// </summary>
        /// <returns>The window token of the current event, or 0 if not in an event</returns>
        public static int NuiGetEventWindow() => _service.NuiGetEventWindow();

        /// <summary>
        /// Returns the event type of the current event.
        /// See nw_inc_nui.nss for full documentation of all events.
        /// </summary>
        /// <returns>The event type of the current event</returns>
        public static string NuiGetEventType() => _service.NuiGetEventType();

        /// <summary>
        /// Returns the ID of the widget that triggered the event.
        /// </summary>
        /// <returns>The ID of the widget that triggered the event</returns>
        public static string NuiGetEventElement() => _service.NuiGetEventElement();

        /// <summary>
        /// Gets the array index of the current event.
        /// This can be used to get the index into an array, for example when rendering lists of buttons.
        /// </summary>
        /// <returns>The array index of the current event, or -1 if the event is not originating from within an array</returns>
        public static int NuiGetEventArrayIndex() => _service.NuiGetEventArrayIndex();

        /// <summary>
        /// Returns the window ID of the window described by the UI token.
        /// </summary>
        /// <param name="oPlayer">The player to get the window ID for</param>
        /// <param name="nUiToken">The UI token</param>
        /// <returns>The window ID, or empty string on error or if the window has no ID</returns>
        public static string NuiGetWindowId(uint oPlayer, int nUiToken) => _service.NuiGetWindowId(oPlayer, nUiToken);

        /// <summary>
        /// Gets the JSON value for the given player, token and bind.
        /// JSON values can hold all kinds of values; but NUI widgets require specific bind types.
        /// It is up to you to either handle this in NWScript, or just set compatible bind types.
        /// No auto-conversion happens.
        /// </summary>
        /// <param name="oPlayer">The player to get the bind for</param>
        /// <param name="nUiToken">The UI token</param>
        /// <param name="sBindName">The bind name</param>
        /// <returns>A JSON value, or JSON null value if the bind does not exist</returns>
        public static Json NuiGetBind(uint oPlayer, int nUiToken, string sBindName) => _service.NuiGetBind(oPlayer, nUiToken, sBindName);

        /// <summary>
        /// Sets a JSON value for the given player, token and bind.
        /// The value is synced down to the client and can be used in UI binding.
        /// When the UI changes the value, it is returned to the server and can be retrieved via NuiGetBind().
        /// JSON values can hold all kinds of values; but NUI widgets require specific bind types.
        /// It is up to you to either handle this in NWScript, or just set compatible bind types.
        /// No auto-conversion happens.
        /// If the bind is on the watch list, this will immediately invoke the event handler with the "watch"
        /// event type; even before this function returns. Do not update watched binds from within the watch handler
        /// unless you enjoy stack overflows.
        /// Does nothing if the given player+token is invalid.
        /// </summary>
        /// <param name="oPlayer">The player to set the bind for</param>
        /// <param name="nUiToken">The UI token</param>
        /// <param name="sBindName">The bind name</param>
        /// <param name="jValue">The JSON value to set</param>
        public static void NuiSetBind(uint oPlayer, int nUiToken, string sBindName, Json jValue) => _service.NuiSetBind(oPlayer, nUiToken, sBindName, jValue);

        /// <summary>
        /// Swaps out the given element (by id) with the given NUI layout (partial).
        /// This currently only works with the "group" element type, and the special "_window_" root group.
        /// </summary>
        /// <param name="oPlayer">The player to set the group layout for</param>
        /// <param name="nUiToken">The UI token</param>
        /// <param name="sElement">The element ID</param>
        /// <param name="jNui">The NUI JSON layout</param>
        public static void NuiSetGroupLayout(uint oPlayer, int nUiToken, string sElement, Json jNui) => _service.NuiSetGroupLayout(oPlayer, nUiToken, sElement, jNui);

        /// <summary>
        /// Marks the given bind name as watched.
        /// A watched bind will invoke the NUI script event every time it's value changes.
        /// Be careful with binding NUI data inside a watch event handler: It's easy to accidentally recurse yourself into a stack overflow.
        /// </summary>
        /// <param name="oPlayer">The player to set the bind watch for</param>
        /// <param name="nUiToken">The UI token</param>
        /// <param name="sBind">The bind name</param>
        /// <param name="bWatch">Whether to watch the bind</param>
        /// <returns>The result of the operation</returns>
        public static int NuiSetBindWatch(uint oPlayer, int nUiToken, string sBind, bool bWatch) => _service.NuiSetBindWatch(oPlayer, nUiToken, sBind, bWatch);

        /// <summary>
        /// Returns the nth window token of the player, or 0.
        /// nNth starts at 0.
        /// Iterator is not write-safe: Calling DestroyWindow() will invalidate move following offsets by one.
        /// </summary>
        /// <param name="oPlayer">The player to get the window for</param>
        /// <param name="nNth">The nth window (defaults to 0)</param>
        /// <returns>The nth window token, or 0</returns>
        public static int NuiGetNthWindow(uint oPlayer, int nNth = 0) => _service.NuiGetNthWindow(oPlayer, nNth);

        /// <summary>
        /// Returns the nth bind name of the given window, or empty string.
        /// If bWatched is TRUE, iterates only watched binds.
        /// If FALSE, iterates all known binds on the window (either set locally or in UI).
        /// </summary>
        /// <param name="oPlayer">The player to get the bind for</param>
        /// <param name="nToken">The UI token</param>
        /// <param name="bWatched">Whether to iterate only watched binds</param>
        /// <param name="nNth">The nth bind (defaults to 0)</param>
        /// <returns>The nth bind name, or empty string</returns>
        public static string NuiGetNthBind(uint oPlayer, int nToken, bool bWatched, int nNth = 0) => _service.NuiGetNthBind(oPlayer, nToken, bWatched, nNth);

        /// <summary>
        /// Returns the event payload, specific to the event.
        /// </summary>
        /// <returns>The event payload, or JsonNull if event has no payload</returns>
        public static Json NuiGetEventPayload() => _service.NuiGetEventPayload();

        /// <summary>
        /// Gets the userdata of the given window token.
        /// </summary>
        /// <param name="oPlayer">The player to get the userdata for</param>
        /// <param name="nToken">The UI token</param>
        /// <returns>The userdata, or JsonNull if the window does not exist on the given player, or has no userdata set</returns>
        public static Json NuiGetUserData(uint oPlayer, int nToken) => _service.NuiGetUserData(oPlayer, nToken);

        /// <summary>
        /// Sets an arbitrary JSON value as userdata on the given window token.
        /// This userdata is not read or handled by the game engine and not sent to clients.
        /// This mechanism only exists as a convenience for the programmer to store data bound to a window's lifecycle.
        /// Will do nothing if the window does not exist.
        /// </summary>
        /// <param name="oPlayer">The player to set the userdata for</param>
        /// <param name="nToken">The UI token</param>
        /// <param name="jUserData">The JSON userdata to set</param>
        public static void NuiSetUserData(uint oPlayer, int nToken, Json jUserData) => _service.NuiSetUserData(oPlayer, nToken, jUserData);

        /// <summary>
        /// Sets the maximum number of henchmen.
        /// </summary>
        /// <param name="nNumHenchmen">The maximum number of henchmen to allow</param>
        public static void SetMaxHenchmen(int nNumHenchmen) => _service.SetMaxHenchmen(nNumHenchmen);

        /// <summary>
        /// Gets the maximum number of henchmen.
        /// </summary>
        /// <returns>The maximum number of henchmen allowed</returns>
        public static int GetMaxHenchmen() => _service.GetMaxHenchmen();

        /// <summary>
        /// Returns the associate type of the specified creature.
        /// </summary>
        /// <param name="oAssociate">The creature to get the associate type for</param>
        /// <returns>The associate type. Returns ASSOCIATE_TYPE_NONE if the creature is not the associate of anyone</returns>
        public static AssociateType GetAssociateType(uint oAssociate) => _service.GetAssociateType(oAssociate);

        /// <summary>
        /// Levels up a creature using default settings.
        /// </summary>
        /// <param name="oCreature">The creature to level up</param>
        /// <param name="nClass">The class to level up in. If Invalid, uses the creature's current class (default: ClassType.Invalid)</param>
        /// <param name="bReadyAllSpells">If true, all memorized spells will be ready to cast without resting (default: false)</param>
        /// <param name="nPackage">The package to use for levelup choices. If Invalid, uses the starting package assigned to that class or just the class package (default: Package.Invalid)</param>
        /// <returns>The level the creature now is, or 0 if it fails</returns>
        /// <remarks>If you specify a class to which the creature has no package specified, they will use the default package for that class for their levelup choices (e.g., no Barbarian Savage/Wizard Divination combinations).</remarks>
        public static int LevelUpHenchman(
            uint oCreature,
            ClassType nClass = ClassType.Invalid,
            bool bReadyAllSpells = false,
            PackageType nPackage = PackageType.Invalid) => _service.LevelUpHenchman(oCreature, nClass, bReadyAllSpells, nPackage);

        /// <summary>
        /// Initializes the target to listen for the standard Associates commands.
        /// </summary>
        /// <param name="oTarget">The target to initialize (default: OBJECT_SELF)</param>
        public static void SetAssociateListenPatterns(uint oTarget = NWScriptService.OBJECT_INVALID) => _service.SetAssociateListenPatterns(oTarget);

        /// <summary>
        /// Removes the associate from the service of the master, returning them to their original faction.
        /// </summary>
        /// <param name="oMaster">The master to remove the associate from</param>
        /// <param name="oAssociate">The associate to remove (default: OBJECT_SELF)</param>
        public static void RemoveSummonedAssociate(uint oMaster, uint oAssociate = NWScriptService.OBJECT_INVALID) => _service.RemoveSummonedAssociate(oMaster, oAssociate);

        /// <summary>
        /// Gets the creature's familiar creature type.
        /// </summary>
        /// <param name="oCreature">The creature to get the familiar type for</param>
        /// <returns>The familiar creature type (FAMILIAR_CREATURE_TYPE_*). Returns FAMILIAR_CREATURE_TYPE_NONE if the creature is invalid or does not currently have a familiar</returns>
        public static int GetFamiliarCreatureType(uint oCreature) => _service.GetFamiliarCreatureType(oCreature);

        /// <summary>
        /// Gets the creature's animal companion creature type.
        /// </summary>
        /// <param name="oCreature">The creature to get the animal companion type for</param>
        /// <returns>The animal companion creature type (ANIMAL_COMPANION_CREATURE_TYPE_*). Returns ANIMAL_COMPANION_CREATURE_TYPE_NONE if the creature is invalid or does not currently have an animal companion</returns>
        public static int GetAnimalCompanionCreatureType(uint oCreature) => _service.GetAnimalCompanionCreatureType(oCreature);

        /// <summary>
        /// Gets the creature's familiar's name.
        /// </summary>
        /// <param name="oCreature">The creature to get the familiar name for</param>
        /// <returns>The familiar's name. Returns empty string if the creature is invalid, does not currently have a familiar, or if the familiar's name is blank</returns>
        public static string GetFamiliarName(uint oCreature) => _service.GetFamiliarName(oCreature);

        /// <summary>
        /// Gets the creature's animal companion's name.
        /// </summary>
        /// <param name="oTarget">The creature to get the animal companion name for</param>
        /// <returns>The animal companion's name. Returns empty string if the creature is invalid, does not currently have an animal companion, or if the animal companion's name is blank</returns>
        public static string GetAnimalCompanionName(uint oTarget) => _service.GetAnimalCompanionName(oTarget);

        /// <summary>
        /// Gets the associate of the specified type belonging to the master.
        /// </summary>
        /// <param name="nAssociateType">The type of associate to get (ASSOCIATE_TYPE_* constant)</param>
        /// <param name="oMaster">The master to get the associate for (default: OBJECT_SELF)</param>
        /// <param name="nTh">Which associate of the specified type to return (default: 1)</param>
        /// <returns>The associate object. Returns OBJECT_INVALID if no such associate exists</returns>
        public static uint GetAssociate(AssociateType nAssociateType, uint oMaster = NWScriptService.OBJECT_INVALID, int nTh = 1) => _service.GetAssociate(nAssociateType, oMaster, nTh);

        /// <summary>
        /// Adds the henchman to the master.
        /// </summary>
        /// <param name="oMaster">The master to add the henchman to</param>
        /// <param name="oHenchman">The henchman to add (default: OBJECT_SELF)</param>
        /// <remarks>If the henchman is either a DM or a player character, this will have no effect.</remarks>
        public static void AddHenchman(uint oMaster, uint oHenchman = NWScriptService.OBJECT_INVALID) => _service.AddHenchman(oMaster, oHenchman);

        /// <summary>
        /// Removes the henchman from the service of the master, returning them to their original faction.
        /// </summary>
        /// <param name="oMaster">The master to remove the henchman from</param>
        /// <param name="oHenchman">The henchman to remove (default: OBJECT_SELF)</param>
        public static void RemoveHenchman(uint oMaster, uint oHenchman = NWScriptService.OBJECT_INVALID) => _service.RemoveHenchman(oMaster, oHenchman);

        /// <summary>
        /// Gets the henchman belonging to the master.
        /// </summary>
        /// <param name="oMaster">The master to get the henchman for (default: OBJECT_SELF)</param>
        /// <param name="nNth">Which henchman to return (default: 1)</param>
        /// <returns>The henchman object. Returns OBJECT_INVALID if the master does not have a henchman</returns>
        public static uint GetHenchman(uint oMaster = NWScriptService.OBJECT_INVALID, int nNth = 1) => _service.GetHenchman(oMaster, nNth);

        /// <summary>
        /// Summons an animal companion for the master.
        /// </summary>
        /// <param name="oMaster">The master to summon the animal companion for (default: OBJECT_SELF)</param>
        public static void SummonAnimalCompanion(uint oMaster = NWScriptService.OBJECT_INVALID) => _service.SummonAnimalCompanion(oMaster);

        /// <summary>
        /// Summons a familiar for the master.
        /// </summary>
        /// <param name="oMaster">The master to summon the familiar for (default: OBJECT_SELF)</param>
        public static void SummonFamiliar(uint oMaster = NWScriptService.OBJECT_INVALID) => _service.SummonFamiliar(oMaster);

        /// <summary>
        /// Gets the last command issued to the associate.
        /// </summary>
        /// <param name="oAssociate">The associate to get the last command for (default: OBJECT_SELF)</param>
        /// <returns>The last command (ASSOCIATE_COMMAND_* constant)</returns>
        public static int GetLastAssociateCommand(uint oAssociate = NWScriptService.OBJECT_INVALID) => _service.GetLastAssociateCommand(oAssociate);

        /// <summary>
        /// Gets the master of the associate.
        /// </summary>
        /// <param name="oAssociate">The associate to get the master for (default: OBJECT_SELF)</param>
        /// <returns>The master object</returns>
        public static uint GetMaster(uint oAssociate = NWScriptService.OBJECT_INVALID) => _service.GetMaster(oAssociate);

        /// <summary>
        /// Causes the specified object to run the specified event.
        /// </summary>
        /// <param name="oObject">The object to run the event on</param>
        /// <param name="evToRun">The event to run</param>
        /// <remarks>The script on the object that is associated with the event specified will run. Events can be created using the following event functions: EventActivateItem() - This creates an OnActivateItem module event. The script for handling this event can be set in Module Properties on the Event Tab. EventConversation() - This creates an OnConversation creature event. The script for handling this event can be set by viewing the Creature Properties on a creature and then clicking on the Scripts Tab. EventSpellCastAt() - This creates an OnSpellCastAt event. The script for handling this event can be set in the Scripts Tab of the Properties menu for the object. EventUserDefined() - This creates an OnUserDefined event. The script for handling this event can be set in the Scripts Tab of the Properties menu for the object/area/module.</remarks>
        public static void SignalEvent(uint oObject, Event evToRun) => _service.SignalEvent(oObject, evToRun);

        /// <summary>
        /// Creates an event of the specified user-defined event number.
        /// </summary>
        /// <param name="nUserDefinedEventNumber">The user-defined event number</param>
        /// <returns>The created user-defined event</returns>
        /// <remarks>This only creates the event. The event won't actually trigger until SignalEvent() is called using this created UserDefined event as an argument. For example: SignalEvent(oObject, EventUserDefined(9999)); Once the event has been signaled, the script associated with the OnUserDefined event will run on the object oObject. To specify the OnUserDefined script that should run, view the object's Properties and click on the Scripts Tab. Then specify a script for the OnUserDefined event. From inside the OnUserDefined script call: GetUserDefinedEventNumber() to retrieve the value of nUserDefinedEventNumber that was used when the event was signaled.</remarks>
        public static Event EventUserDefined(int nUserDefinedEventNumber) => _service.EventUserDefined(nUserDefinedEventNumber);

        /// <summary>
        /// Gets the user-defined event number.
        /// </summary>
        /// <returns>The user-defined event number</returns>
        /// <remarks>This is for use in a user-defined script.</remarks>
        public static int GetUserDefinedEventNumber() => _service.GetUserDefinedEventNumber();

        /// <summary>
        /// Gets the object that closed the door or placeable.
        /// </summary>
        /// <returns>The object that closed the door or placeable. Returns OBJECT_INVALID if the caller is not a valid door or placeable</returns>
        /// <remarks>Use this in an OnClosed script.</remarks>
        public static uint GetLastClosedBy() => _service.GetLastClosedBy();

        /// <summary>
        /// Returns the event script for the given object and handler.
        /// </summary>
        /// <param name="oObject">The object to get the event script for</param>
        /// <param name="nHandler">The event script handler</param>
        /// <returns>The event script. Will return empty string if unset, the object is invalid, or the object cannot have the requested handler</returns>
        public static string GetEventScript(uint oObject, EventScriptType nHandler) => _service.GetEventScript(oObject, nHandler);

        /// <summary>
        /// Sets the given event script for the given object and handler.
        /// </summary>
        /// <param name="oObject">The object to set the event script for</param>
        /// <param name="nHandler">The event script handler</param>
        /// <param name="sScript">The script to set</param>
        /// <returns>1 on success, 0 on failure. Will fail if oObject is invalid or does not have the requested handler</returns>
        public static int SetEventScript(uint oObject, EventScriptType nHandler, string sScript) => _service.SetEventScript(oObject, nHandler, sScript);

        /// <summary>
        /// Gets an optional vector of specific GUI events in the module OnPlayerGuiEvent event.
        /// </summary>
        /// <returns>The vector of the last GUI event. GUIEVENT_RADIAL_OPEN - World vector position of radial if on tile</returns>
        public static Vector3 GetLastGuiEventVector() => _service.GetLastGuiEventVector();

        /// <summary>
        /// Changes the direction in which the camera is facing.
        /// </summary>
        /// <param name="fDirection">The direction expressed as anticlockwise degrees from Due East (0.0f=East, 90.0f=North, 180.0f=West, 270.0f=South)</param>
        /// <param name="fDistance">The camera distance. A value of -1.0f will use the current camera value (default: -1.0f)</param>
        /// <param name="fPitch">The camera pitch. A value of -1.0f will use the current camera value (default: -1.0f)</param>
        /// <param name="nTransitionType">The transition type (CAMERA_TRANSITION_TYPE_*). SNAP will immediately move the camera, while other types will move gradually (default: CameraTransitionType.Snap)</param>
        /// <remarks>This can be used to change the way the camera is facing after the player emerges from an area transition. Pitch and distance are limited to valid values for the current camera mode: Top Down: Distance = 5-20, Pitch = 1-50; Driving camera: Distance = 6 (can't be changed), Pitch = 1-62; Chase: Distance = 5-20, Pitch = 1-50. In NWN:Hordes of the Underdark the camera limits have been relaxed to: Distance 1-25, Pitch 1-89.</remarks>
        public static void SetCameraFacing(float fDirection, float fDistance = -1.0f, float fPitch = -1.0f,
            CameraTransitionType nTransitionType = CameraTransitionType.Snap) => _service.SetCameraFacing(fDirection, fDistance, fPitch, nTransitionType);

        /// <summary>
        /// Sets the player's camera limits that override any client configuration limits.
        /// </summary>
        /// <param name="oPlayer">The player to set camera limits for</param>
        /// <param name="fMinPitch">The minimum pitch limit. Value of -1.0 means use the client config instead (default: -1.0f)</param>
        /// <param name="fMaxPitch">The maximum pitch limit. Value of -1.0 means use the client config instead (default: -1.0f)</param>
        /// <param name="fMinDist">The minimum distance limit. Value of -1.0 means use the client config instead (default: -1.0f)</param>
        /// <param name="fMaxDist">The maximum distance limit. Value of -1.0 means use the client config instead (default: -1.0f)</param>
        /// <remarks>Like all other camera settings, this is not saved when saving the game.</remarks>
        public static void SetCameraLimits(
            uint oPlayer,
            float fMinPitch = -1.0f,
            float fMaxPitch = -1.0f,
            float fMinDist = -1.0f,
            float fMaxDist = -1.0f) => _service.SetCameraLimits(oPlayer, fMinPitch, fMaxPitch, fMinDist, fMaxDist);

        /// <summary>
        /// Sets the object that the player's camera will be attached to.
        /// </summary>
        /// <param name="oPlayer">The player whose camera to attach</param>
        /// <param name="oTarget">A valid creature or placeable. If OBJECT_INVALID, it will revert the camera back to the player's character. The target must be known to the player's client (same area and within visible distance). If the target is a creature, it must also be within the player's perception range and perceived</param>
        /// <param name="bFindClearView">If true, the client will attempt to find a camera position where the target is in view (default: false)</param>
        /// <remarks>SetObjectVisibleDistance() can be used to increase the visible distance range. If the target gets destroyed while the player's camera is attached to it, the camera will revert back to the player's character. If the player goes through a transition with its camera attached to a different object, it will revert back to the player's character. The object the player's camera is attached to is not saved when saving the game.</remarks>
        public static void AttachCamera(uint oPlayer, uint oTarget, bool bFindClearView = false) => _service.AttachCamera(oPlayer, oTarget, bFindClearView);

        /// <summary>
        /// Sets the player's camera settings that override any client configuration settings.
        /// </summary>
        /// <param name="oPlayer">The player to set camera flags for</param>
        /// <param name="nFlags">A bitmask of CAMERA_FLAG_* constants (default: 0)</param>
        /// <remarks>Like all other camera settings, this is not saved when saving the game.</remarks>
        public static void SetCameraFlags(uint oPlayer, int nFlags = 0) => _service.SetCameraFlags(oPlayer, nFlags);

        /// <summary>
        /// Adds an item property to the specified item.
        /// Only temporary and permanent duration types are allowed.
        /// </summary>
        /// <param name="nDurationType">The duration type of the property</param>
        /// <param name="ipProperty">The item property to add</param>
        /// <param name="oItem">The item to add the property to</param>
        /// <param name="fDuration">The duration in seconds (defaults to 0.0f)</param>
        public static void AddItemProperty(DurationType nDurationType, ItemProperty ipProperty, uint oItem,
            float fDuration = 0.0f) => _service.AddItemProperty(nDurationType, ipProperty, oItem, fDuration);

        /// <summary>
        /// Removes an item property from the specified item.
        /// </summary>
        /// <param name="oItem">The item to remove the property from</param>
        /// <param name="ipProperty">The item property to remove</param>
        public static void RemoveItemProperty(uint oItem, ItemProperty ipProperty) => _service.RemoveItemProperty(oItem, ipProperty);

        /// <summary>
        /// Checks if the item property is valid.
        /// </summary>
        /// <param name="ipProperty">The item property to check</param>
        /// <returns>TRUE if the item property is valid</returns>
        public static bool GetIsItemPropertyValid(ItemProperty ipProperty) => _service.GetIsItemPropertyValid(ipProperty);

        /// <summary>
        /// Gets the first item property on an item.
        /// </summary>
        /// <param name="oItem">The item to get the first property from</param>
        /// <returns>The first item property, or an invalid property if none exist</returns>
        public static ItemProperty GetFirstItemProperty(uint oItem) => _service.GetFirstItemProperty(oItem);

        /// <summary>
        /// Will keep retrieving the next item property on an item.
        /// Will return an invalid item property when the list is empty.
        /// </summary>
        /// <param name="oItem">The item to get the next property from</param>
        /// <returns>The next item property, or an invalid property if none remain</returns>
        public static ItemProperty GetNextItemProperty(uint oItem) => _service.GetNextItemProperty(oItem);

        /// <summary>
        /// Returns the item property type (e.g. holy avenger).
        /// </summary>
        /// <param name="ip">The item property to get the type of</param>
        /// <returns>The item property type</returns>
        public static ItemPropertyType GetItemPropertyType(ItemProperty ip) => _service.GetItemPropertyType(ip);

        /// <summary>
        /// Returns the duration type of the item property.
        /// </summary>
        /// <param name="ip">The item property to get the duration type of</param>
        /// <returns>The duration type</returns>
        public static DurationType GetItemPropertyDurationType(ItemProperty ip) => _service.GetItemPropertyDurationType(ip);

        /// <summary>
        /// Returns an item property ability bonus.
        /// You need to specify an ability constant (IP_CONST_ABILITY_*) and the bonus.
        /// The bonus should be a positive integer between 1 and 12.
        /// </summary>
        /// <param name="nAbility">The ability type constant</param>
        /// <param name="nBonus">The bonus amount (1-12)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyAbilityBonus(AbilityType nAbility, int nBonus) => _service.ItemPropertyAbilityBonus(nAbility, nBonus);

        /// <summary>
        /// Returns an item property AC bonus.
        /// You need to specify the bonus.
        /// The bonus should be a positive integer between 1 and 20. The modifier
        /// type depends on the item it is being applied to.
        /// </summary>
        /// <param name="nBonus">The AC bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyACBonus(int nBonus) => _service.ItemPropertyACBonus(nBonus);

        /// <summary>
        /// Returns an item property AC bonus vs. alignment group.
        /// An example of an alignment group is Chaotic, or Good.
        /// You need to specify the alignment group constant (IP_CONST_ALIGNMENTGROUP_*) and the AC bonus.
        /// The AC bonus should be an integer between 1 and 20. The modifier
        /// type depends on the item it is being applied to.
        /// </summary>
        /// <param name="nAlignGroup">The alignment group constant</param>
        /// <param name="ACBonus">The AC bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyACBonusVsAlign(ItemPropertyAlignmentGroupType nAlignGroup, int ACBonus) => _service.ItemPropertyACBonusVsAlign(nAlignGroup, ACBonus);

        /// <summary>
        /// Returns an item property AC bonus vs. damage type (e.g. piercing).
        /// You need to specify the damage type constant (IP_CONST_DAMAGETYPE_*) and the AC bonus.
        /// The AC bonus should be an integer between 1 and 20. The modifier type depends on the item it is being applied to.
        /// NOTE: Only the first 3 damage types may be used here, the 3 basic physical types.
        /// </summary>
        /// <param name="nDamageType">The damage type constant</param>
        /// <param name="ACBonus">The AC bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyACBonusVsDmgType(ItemPropertyDamageType nDamageType, int ACBonus) => _service.ItemPropertyACBonusVsDmgType(nDamageType, ACBonus);

        /// <summary>
        /// Returns an item property AC bonus vs. racial group.
        /// You need to specify the racial group constant (IP_CONST_RACIALTYPE_*) and the AC bonus.
        /// The AC bonus should be an integer between 1 and 20. The modifier type depends on the item it is being applied to.
        /// </summary>
        /// <param name="nRace">The racial type constant</param>
        /// <param name="nACBonus">The AC bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyACBonusVsRace(RacialType nRace, int nACBonus) => _service.ItemPropertyACBonusVsRace(nRace, nACBonus);

        /// <summary>
        /// Returns an item property AC bonus vs. specific alignment.
        /// You need to specify the specific alignment constant (IP_CONST_ALIGNMENT_*) and the AC bonus.
        /// The AC bonus should be an integer between 1 and 20. The modifier type depends on the item it is being applied to.
        /// </summary>
        /// <param name="nAlign">The alignment constant</param>
        /// <param name="nACBonus">The AC bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyACBonusVsSAlign(ItemPropertyAlignmentType nAlign, int nACBonus) => _service.ItemPropertyACBonusVsSAlign(nAlign, nACBonus);

        /// <summary>
        /// Returns an item property enhancement bonus.
        /// You need to specify the enhancement bonus.
        /// The enhancement bonus should be an integer between 1 and 20.
        /// </summary>
        /// <param name="nEnhancementBonus">The enhancement bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyEnhancementBonus(int nEnhancementBonus) => _service.ItemPropertyEnhancementBonus(nEnhancementBonus);

        /// <summary>
        /// Returns an item property enhancement bonus vs. an alignment group.
        /// You need to specify the alignment group constant (IP_CONST_ALIGNMENTGROUP_*) and the enhancement bonus.
        /// The enhancement bonus should be an integer between 1 and 20.
        /// </summary>
        /// <param name="nAlignGroup">The alignment group constant</param>
        /// <param name="nBonus">The enhancement bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyEnhancementBonusVsAlign(ItemPropertyAlignmentGroupType nAlignGroup,
            int nBonus) => _service.ItemPropertyEnhancementBonusVsAlign(nAlignGroup, nBonus);

        /// <summary>
        /// Returns an item property enhancement bonus vs. racial group.
        /// You need to specify the racial group constant (IP_CONST_RACIALTYPE_*) and the enhancement bonus.
        /// The enhancement bonus should be an integer between 1 and 20.
        /// </summary>
        /// <param name="nRace">The racial type constant</param>
        /// <param name="nBonus">The enhancement bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyEnhancementBonusVsRace(RacialType nRace, int nBonus) => _service.ItemPropertyEnhancementBonusVsRace(nRace, nBonus);

        /// <summary>
        /// Returns an item property enhancement bonus vs. a specific alignment.
        /// You need to specify the alignment constant (IP_CONST_ALIGNMENT_*) and the enhancement bonus.
        /// The enhancement bonus should be an integer between 1 and 20.
        /// </summary>
        /// <param name="nAlign">The alignment constant</param>
        /// <param name="nBonus">The enhancement bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyEnhancementBonusVsSAlign(ItemPropertyAlignmentType nAlign,
            int nBonus) => _service.ItemPropertyEnhancementBonusVsSAlign(nAlign, nBonus);

        /// <summary>
        /// Returns an item property enhancement penalty.
        /// You need to specify the enhancement penalty.
        /// The enhancement penalty should be a POSITIVE integer between 1 and 5 (e.g. 1 = -1).
        /// </summary>
        /// <param name="nPenalty">The enhancement penalty amount (1-5)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyEnhancementPenalty(int nPenalty) => _service.ItemPropertyEnhancementPenalty(nPenalty);

        /// <summary>
        /// Returns an item property weight reduction.
        /// You need to specify the weight reduction constant (IP_CONST_REDUCEDWEIGHT_*).
        /// </summary>
        /// <param name="nReduction">The weight reduction constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyWeightReduction(ItemPropertyReducedWeightType nReduction) => _service.ItemPropertyWeightReduction(nReduction);

        /// <summary>
        /// Returns an item property bonus feat.
        /// You need to specify the feat constant (IP_CONST_FEAT_*).
        /// </summary>
        /// <param name="nFeat">The feat constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyBonusFeat(ItemPropertyFeatType nFeat) => _service.ItemPropertyBonusFeat(nFeat);

        /// <summary>
        /// Returns an item property bonus level spell (bonus spell of level).
        /// You must specify the class constant (IP_CONST_CLASS_*) of the bonus spell (MUST BE a spell casting class) and the level of the bonus spell.
        /// The level of the bonus spell should be an integer between 0 and 9.
        /// </summary>
        /// <param name="nClass">The class constant (must be a spell casting class)</param>
        /// <param name="nSpellLevel">The spell level (0-9)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyBonusLevelSpell(ItemPropertyClassType nClass, ItemPropertySpellLevelType nSpellLevel) => _service.ItemPropertyBonusLevelSpell(nClass, nSpellLevel);

        /// <summary>
        /// Returns an item property cast spell.
        /// You must specify the spell constant (IP_CONST_CASTSPELL_*) and the number of uses constant (IP_CONST_CASTSPELL_NUMUSES_*).
        /// NOTE: The number after the name of the spell in the constant is the level at which the spell will be cast.
        /// Sometimes there are multiple copies of the same spell but they each are cast at a different level.
        /// The higher the level, the more cost will be added to the item.
        /// NOTE: The list of spells that can be applied to an item will depend on the item type.
        /// For instance there are spells that can be applied to a wand that cannot be applied to a potion.
        /// If you try to put a cast spell effect on an item that is not allowed to have that effect it will not work.
        /// </summary>
        /// <param name="nSpell">The spell constant</param>
        /// <param name="nNumUses">The number of uses constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyCastSpell(ItemPropertyCastSpellType nSpell, ItemPropertyCastSpellNumberUsesType nNumUses) => _service.ItemPropertyCastSpell(nSpell, nNumUses);

        /// <summary>
        /// Returns an item property damage bonus.
        /// You must specify the damage type constant (IP_CONST_DAMAGETYPE_*) and the amount of damage constant (IP_CONST_DAMAGEBONUS_*).
        /// NOTE: Not all the damage types will work, use only the following: Acid, Bludgeoning, Cold, Electrical, Fire, Piercing, Slashing, Sonic.
        /// </summary>
        /// <param name="nDamageType">The damage type constant</param>
        /// <param name="nDamage">The damage bonus constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamageBonus(ItemPropertyDamageType nDamageType,
            ItemPropertyDamageBonusType nDamage) => _service.ItemPropertyDamageBonus(nDamageType, nDamage);

        /// <summary>
        /// Returns an item property damage bonus vs. alignment groups.
        /// You must specify the alignment group constant (IP_CONST_ALIGNMENTGROUP_*) and the damage type constant
        /// (IP_CONST_DAMAGETYPE_*) and the amount of damage constant (IP_CONST_DAMAGEBONUS_*).
        /// NOTE: Not all the damage types will work, use only the following: Acid, Bludgeoning, Cold, Electrical, Fire, Piercing, Slashing, Sonic.
        /// </summary>
        /// <param name="nAlignGroup">The alignment group constant</param>
        /// <param name="nDamageType">The damage type constant</param>
        /// <param name="nDamage">The damage bonus constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamageBonusVsAlign(ItemPropertyAlignmentGroupType nAlignGroup,
            ItemPropertyDamageType nDamageType, ItemPropertyDamageBonusType nDamage) => _service.ItemPropertyDamageBonusVsAlign(nAlignGroup, nDamageType, nDamage);

        /// <summary>
        /// Returns an item property damage bonus vs. specific race.
        /// You must specify the racial group constant (IP_CONST_RACIALTYPE_*) and the damage type constant
        /// (IP_CONST_DAMAGETYPE_*) and the amount of damage constant (IP_CONST_DAMAGEBONUS_*).
        /// NOTE: Not all the damage types will work, use only the following: Acid, Bludgeoning, Cold, Electrical, Fire, Piercing, Slashing, Sonic.
        /// </summary>
        /// <param name="nRace">The racial type constant</param>
        /// <param name="nDamageType">The damage type constant</param>
        /// <param name="nDamage">The damage bonus constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamageBonusVsRace(RacialType nRace,
            ItemPropertyDamageType nDamageType, ItemPropertyDamageBonusType nDamage) => _service.ItemPropertyDamageBonusVsRace(nRace, nDamageType, nDamage);

        /// <summary>
        /// Returns an item property damage bonus vs. specific alignment.
        /// You must specify the specific alignment constant (IP_CONST_ALIGNMENT_*) and the damage type constant
        /// (IP_CONST_DAMAGETYPE_*) and the amount of damage constant (IP_CONST_DAMAGEBONUS_*).
        /// NOTE: Not all the damage types will work, use only the following: Acid, Bludgeoning, Cold, Electrical, Fire, Piercing, Slashing, Sonic.
        /// </summary>
        /// <param name="nAlign">The alignment constant</param>
        /// <param name="nDamageType">The damage type constant</param>
        /// <param name="nDamage">The damage bonus constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamageBonusVsSAlign(ItemPropertyAlignmentType nAlign,
            ItemPropertyDamageType nDamageType, ItemPropertyDamageBonusType nDamage) => _service.ItemPropertyDamageBonusVsSAlign(nAlign, nDamageType, nDamage);

        /// <summary>
        /// Returns an item property damage immunity.
        /// You must specify the damage type constant (IP_CONST_DAMAGETYPE_*) that you want to be immune to and the immune bonus percentage
        /// constant (IP_CONST_DAMAGEIMMUNITY_*).
        /// NOTE: Not all the damage types will work, use only the following: Acid, Bludgeoning, Cold, Electrical, Fire, Piercing, Slashing, Sonic.
        /// </summary>
        /// <param name="nDamageType">The damage type constant</param>
        /// <param name="nImmuneBonus">The immune bonus percentage constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamageImmunity(ItemPropertyDamageType nDamageType,
            ItemPropertyDamageImmunityType nImmuneBonus) => _service.ItemPropertyDamageImmunity(nDamageType, nImmuneBonus);

        /// <summary>
        /// Returns an item property damage penalty.
        /// You must specify the damage penalty.
        /// The damage penalty should be a uint, 1-5 only.
        /// Will reduce any value less than 5 to 5.
        /// </summary>
        /// <param name="nPenalty">The damage penalty amount (1-5)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamagePenalty(int nPenalty) => _service.ItemPropertyDamagePenalty(nPenalty);

        /// <summary>
        /// Returns an item property damage reduction.
        /// You must specify the enhancement level (IP_CONST_DAMAGEREDUCTION_*) that is required to get past the damage reduction
        /// and the amount of HP of damage constant (IP_CONST_DAMAGESOAK_*) will be soaked up if your weapon is not of high enough enhancement.
        /// </summary>
        /// <param name="nEnhancement">The enhancement level constant</param>
        /// <param name="nHPSoak">The HP soak amount constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamageReduction(ItemPropertyDamageReductionType nEnhancement, ItemPropertyDamageSoakType nHPSoak) => _service.ItemPropertyDamageReduction(nEnhancement, nHPSoak);

        /// <summary>
        /// Returns an item property damage resistance.
        /// You must specify the damage type constant (IP_CONST_DAMAGETYPE_*) and the amount of HP of damage constant
        /// (IP_CONST_DAMAGERESIST_*) that will be resisted against each round.
        /// </summary>
        /// <param name="nDamageType">The damage type constant</param>
        /// <param name="nHPResist">The HP resistance amount constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamageResistance(ItemPropertyDamageType nDamageType,
            ItemPropertyDamageResistType nHPResist) => _service.ItemPropertyDamageResistance(nDamageType, nHPResist);

        /// <summary>
        /// Returns an item property damage vulnerability.
        /// You must specify the damage type constant (IP_CONST_DAMAGETYPE_*) that you want the user to be extra vulnerable to
        /// and the percentage vulnerability constant (IP_CONST_DAMAGEVULNERABILITY_*).
        /// </summary>
        /// <param name="nDamageType">The damage type constant</param>
        /// <param name="nVulnerability">The vulnerability percentage constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDamageVulnerability(ItemPropertyDamageType nDamageType,
            ItemPropertyDamageVulnerabilityType nVulnerability) => _service.ItemPropertyDamageVulnerability(nDamageType, nVulnerability);

        /// <summary>
        /// Returns an item property darkvision.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDarkvision() => _service.ItemPropertyDarkvision();

        /// <summary>
        /// Returns an item property decrease ability score.
        /// You must specify the ability constant (IP_CONST_ABILITY_*) and the modifier constant.
        /// The modifier must be a POSITIVE integer between 1 and 10 (e.g. 1 = -1).
        /// </summary>
        /// <param name="nAbility">The ability constant</param>
        /// <param name="nModifier">The modifier amount (1-10)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDecreaseAbility(ItemPropertyAbilityType nAbility, int nModifier) => _service.ItemPropertyDecreaseAbility(nAbility, nModifier);

        /// <summary>
        /// Returns an item property decrease armor class.
        /// You must specify the armor modifier type constant (IP_CONST_ACMODIFIERTYPE_*) and the armor class penalty.
        /// The penalty must be a POSITIVE integer between 1 and 5 (e.g. 1 = -1).
        /// </summary>
        /// <param name="nModifierType">The armor modifier type constant</param>
        /// <param name="nPenalty">The armor class penalty (1-5)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDecreaseAC(ItemPropertyArmorClassModiferType nModifierType, int nPenalty) => _service.ItemPropertyDecreaseAC(nModifierType, nPenalty);

        /// <summary>
        /// Returns an item property decrease skill.
        /// You must specify the constant for the skill to be decreased (SKILL_*) and the amount of the penalty.
        /// The penalty must be a POSITIVE integer between 1 and 10 (e.g. 1 = -1).
        /// </summary>
        /// <param name="nSkill">The skill type constant</param>
        /// <param name="nPenalty">The skill penalty (1-10)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyDecreaseSkill(NWNSkillType nSkill, int nPenalty) => _service.ItemPropertyDecreaseSkill(nSkill, nPenalty);

        /// <summary>
        /// Returns an item property container reduced weight.
        /// This is used for special containers that reduce the weight of the objects inside them.
        /// You must specify the container weight reduction type constant (IP_CONST_CONTAINERWEIGHTRED_*).
        /// </summary>
        /// <param name="nContainerType">The container weight reduction type constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyContainerReducedWeight(ItemPropertyContainerWeightType nContainerType) => _service.ItemPropertyContainerReducedWeight(nContainerType);

        /// <summary>
        /// Returns an item property extra melee damage type.
        /// You must specify the extra melee base damage type that you want applied. It is a constant (IP_CONST_DAMAGETYPE_*).
        /// NOTE: Only the first 3 base types (piercing, slashing, & bludgeoning) are applicable here.
        /// NOTE: It is also only applicable to melee weapons.
        /// </summary>
        /// <param name="nDamageType">The damage type constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyExtraMeleeDamageType(ItemPropertyDamageType nDamageType) => _service.ItemPropertyExtraMeleeDamageType(nDamageType);

        /// <summary>
        /// Returns an item property extra ranged damage type.
        /// You must specify the extra ranged base damage type that you want applied. It is a constant (IP_CONST_DAMAGETYPE_*).
        /// NOTE: Only the first 3 base types (piercing, slashing, & bludgeoning) are applicable here.
        /// NOTE: It is also only applicable to ranged weapons.
        /// </summary>
        /// <param name="nDamageType">The damage type constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyExtraRangeDamageType(ItemPropertyDamageType nDamageType) => _service.ItemPropertyExtraRangeDamageType(nDamageType);

        /// <summary>
        /// Returns an item property haste.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyHaste() => _service.ItemPropertyHaste();

        /// <summary>
        /// Returns an item property holy avenger.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyHolyAvenger() => _service.ItemPropertyHolyAvenger();

        /// <summary>
        /// Returns an item property immunity to miscellaneous effects.
        /// You must specify the effect to which the user is immune, it is a constant (IP_CONST_IMMUNITYMISC_*).
        /// </summary>
        /// <param name="nImmunityType">The immunity type constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyImmunityMisc(ItemPropertyImmunityMiscType nImmunityType) => _service.ItemPropertyImmunityMisc(nImmunityType);

        /// <summary>
        /// Returns an item property improved evasion.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyImprovedEvasion() => _service.ItemPropertyImprovedEvasion();

        /// <summary>
        /// Returns an item property bonus spell resistance.
        /// You must specify the bonus spell resistance constant (IP_CONST_SPELLRESISTANCEBONUS_*).
        /// </summary>
        /// <param name="nBonus">The spell resistance bonus constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyBonusSpellResistance(ItemPropertySpellResistanceBonusType nBonus) => _service.ItemPropertyBonusSpellResistance(nBonus);

        /// <summary>
        /// Returns an item property saving throw bonus vs. a specific effect or damage type.
        /// You must specify the save type constant (IP_CONST_SAVEVS_*) that the bonus is applied to and the bonus that is be applied.
        /// The bonus must be an integer between 1 and 20.
        /// </summary>
        /// <param name="nBonusType">The save type constant</param>
        /// <param name="nBonus">The bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyBonusSavingThrowVsX(ItemPropertySaveVsType nBonusType, int nBonus) => _service.ItemPropertyBonusSavingThrowVsX(nBonusType, nBonus);

        /// <summary>
        /// Returns an item property saving throw bonus to the base type (e.g. will, reflex, fortitude).
        /// You must specify the base type constant (IP_CONST_SAVEBASETYPE_*) to which the user gets the bonus and the bonus that he/she will get.
        /// The bonus must be an integer between 1 and 20.
        /// </summary>
        /// <param name="nBaseSaveType">The base save type constant</param>
        /// <param name="nBonus">The bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyBonusSavingThrow(ItemPropertySaveBaseType nBaseSaveType, int nBonus) => _service.ItemPropertyBonusSavingThrow(nBaseSaveType, nBonus);

        /// <summary>
        /// Returns an item property keen.
        /// This means a critical threat range of 19-20 on a weapon will be increased to 17-20 etc.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyKeen() => _service.ItemPropertyKeen();

        /// <summary>
        /// Returns an item property light.
        /// You must specify the intensity constant of the light (IP_CONST_LIGHTBRIGHTNESS_*) and the color constant of the light (IP_CONST_LIGHTCOLOR_*).
        /// </summary>
        /// <param name="nBrightness">The light brightness constant</param>
        /// <param name="nColor">The light color constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyLight(ItemPropertyLightBrightnessType nBrightness, ItemPropertyLightColorType nColor) => _service.ItemPropertyLight(nBrightness, nColor);

        /// <summary>
        /// Returns an item property max range strength modification (e.g. mighty).
        /// You must specify the maximum modifier for strength that is allowed on a ranged weapon.
        /// The modifier must be a positive integer between 1 and 20.
        /// </summary>
        /// <param name="nModifier">The strength modifier (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyMaxRangeStrengthMod(int nModifier) => _service.ItemPropertyMaxRangeStrengthMod(nModifier);

        /// <summary>
        /// Returns an item property no damage.
        /// This means the weapon will do no damage in combat.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyNoDamage() => _service.ItemPropertyNoDamage();

        /// <summary>
        /// Returns an item property on hit effect property.
        /// You must specify the on hit property constant (IP_CONST_ONHIT_*) and the save DC constant (IP_CONST_ONHIT_SAVEDC_*).
        /// Some of the item properties require a special parameter as well. If the property does not require one you may leave out the last one.
        /// The list of the ones with 3 parameters and what they are are as follows:
        /// ABILITYDRAIN: nSpecial is the ability it is to drain. constant(IP_CONST_ABILITY_*)
        /// BLINDNESS: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// CONFUSION: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// DAZE: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// DEAFNESS: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// DISEASE: nSpecial is the type of disease that will effect the victim. constant(DISEASE_*)
        /// DOOM: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// FEAR: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// HOLD: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// ITEMPOISON: nSpecial is the type of poison that will effect the victim. constant(IP_CONST_POISON_*)
        /// SILENCE: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// SLAYRACE: nSpecial is the race that will be slain. constant(IP_CONST_RACIALTYPE_*)
        /// SLAYALIGNMENTGROUP: nSpecial is the alignment group that will be slain (e.g. chaotic). constant(IP_CONST_ALIGNMENTGROUP_*)
        /// SLAYALIGNMENT: nSpecial is the specific alignment that will be slain. constant(IP_CONST_ALIGNMENT_*)
        /// SLEEP: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// SLOW: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// STUN: nSpecial is the duration/percentage of effecting victim. constant(IP_CONST_ONHIT_DURATION_*)
        /// </summary>
        /// <param name="nProperty">The on hit property constant</param>
        /// <param name="nSaveDC">The save DC constant</param>
        /// <param name="nSpecial">The special parameter (defaults to 0)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyOnHitProps(int nProperty, int nSaveDC, int nSpecial = 0) => _service.ItemPropertyOnHitProps(nProperty, nSaveDC, nSpecial);

        /// <summary>
        /// Returns an item property reduced saving throw vs. an effect or damage type.
        /// You must specify the constant to which the penalty applies (IP_CONST_SAVEVS_*) and the penalty to be applied.
        /// The penalty must be a POSITIVE integer between 1 and 20 (e.g. 1 = -1).
        /// </summary>
        /// <param name="nBaseSaveType">The save type constant</param>
        /// <param name="nPenalty">The penalty amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyReducedSavingThrowVsX(ItemPropertySaveVsType nBaseSaveType, int nPenalty) => _service.ItemPropertyReducedSavingThrowVsX(nBaseSaveType, nPenalty);

        /// <summary>
        /// Returns an item property reduced saving to base type.
        /// You must specify the base type to which the penalty applies (e.g. will, reflex, or fortitude) and the penalty to be applied.
        /// The constant for the base type starts with (IP_CONST_SAVEBASETYPE_*).
        /// The penalty must be a POSITIVE integer between 1 and 20 (e.g. 1 = -1).
        /// </summary>
        /// <param name="nBonusType">The base save type constant</param>
        /// <param name="nPenalty">The penalty amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyReducedSavingThrow(ItemPropertySaveBaseType nBonusType, int nPenalty) => _service.ItemPropertyReducedSavingThrow(nBonusType, nPenalty);

        /// <summary>
        /// Returns an item property regeneration.
        /// You must specify the regeneration amount.
        /// The amount must be an integer between 1 and 20.
        /// </summary>
        /// <param name="nRegenAmount">The regeneration amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyRegeneration(int nRegenAmount) => _service.ItemPropertyRegeneration(nRegenAmount);

        /// <summary>
        /// Returns an item property skill bonus.
        /// You must specify the skill to which the user will get a bonus (SKILL_*) and the amount of the bonus.
        /// The bonus amount must be an integer between 1 and 50.
        /// </summary>
        /// <param name="nSkill">The skill type constant</param>
        /// <param name="nBonus">The bonus amount (1-50)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertySkillBonus(NWNSkillType nSkill, int nBonus) => _service.ItemPropertySkillBonus(nSkill, nBonus);

        /// <summary>
        /// Returns an item property spell immunity vs. specific spell.
        /// You must specify the spell to which the user will be immune (IP_CONST_IMMUNITYSPELL_*).
        /// </summary>
        /// <param name="nSpell">The immunity spell constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertySpellImmunitySpecific(ItemPropertyImmunitySpellType nSpell) => _service.ItemPropertySpellImmunitySpecific(nSpell);

        /// <summary>
        /// Returns an item property spell immunity vs. spell school.
        /// You must specify the school to which the user will be immune (IP_CONST_SPELLSCHOOL_*).
        /// </summary>
        /// <param name="nSchool">The spell school constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertySpellImmunitySchool(SpellSchool nSchool) => _service.ItemPropertySpellImmunitySchool(nSchool);

        /// <summary>
        /// Returns an item property thieves tools.
        /// You must specify the modifier you wish the tools to have.
        /// The modifier must be an integer between 1 and 12.
        /// </summary>
        /// <param name="nModifier">The modifier amount (1-12)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyThievesTools(int nModifier) => _service.ItemPropertyThievesTools(nModifier);

        /// <summary>
        /// Returns an item property attack bonus.
        /// You must specify an attack bonus.
        /// The bonus must be an integer between 1 and 20.
        /// </summary>
        /// <param name="nBonus">The attack bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyAttackBonus(int nBonus) => _service.ItemPropertyAttackBonus(nBonus);

        /// <summary>
        /// Returns an item property attack bonus vs. alignment group.
        /// You must specify the alignment group constant (IP_CONST_ALIGNMENTGROUP_*) and the attack bonus.
        /// The bonus must be an integer between 1 and 20.
        /// </summary>
        /// <param name="nAlignGroup">The alignment group constant</param>
        /// <param name="nBonus">The attack bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyAttackBonusVsAlign(ItemPropertyAlignmentGroupType nAlignGroup,
            int nBonus) => _service.ItemPropertyAttackBonusVsAlign(nAlignGroup, nBonus);

        /// <summary>
        /// Returns an item property attack bonus vs. racial group.
        /// You must specify the racial group constant (IP_CONST_RACIALTYPE_*) and the attack bonus.
        /// The bonus must be an integer between 1 and 20.
        /// </summary>
        /// <param name="nRace">The racial type constant</param>
        /// <param name="nBonus">The attack bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyAttackBonusVsRace(RacialType nRace, int nBonus) => _service.ItemPropertyAttackBonusVsRace(nRace, nBonus);

        /// <summary>
        /// Returns an item property attack bonus vs. a specific alignment.
        /// You must specify the alignment you want the bonus to work against (IP_CONST_ALIGNMENT_*) and the attack bonus.
        /// The bonus must be an integer between 1 and 20.
        /// </summary>
        /// <param name="nAlignment">The alignment constant</param>
        /// <param name="nBonus">The attack bonus amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyAttackBonusVsSAlign(ItemPropertyAlignmentType nAlignment, int nBonus) => _service.ItemPropertyAttackBonusVsSAlign(nAlignment, nBonus);

        /// <summary>
        /// Returns an item property attack penalty.
        /// You must specify the attack penalty.
        /// The penalty must be a POSITIVE integer between 1 and 5 (e.g. 1 = -1).
        /// </summary>
        /// <param name="nPenalty">The attack penalty amount (1-5)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyAttackPenalty(int nPenalty) => _service.ItemPropertyAttackPenalty(nPenalty);

        /// <summary>
        /// Returns an item property unlimited ammo.
        /// If you leave the parameter field blank it will be just a normal bolt, arrow, or bullet.
        /// However you may specify that you want the ammunition to do special damage (e.g. +1d6 Fire, or +1 enhancement bonus).
        /// For this parameter you use the constants beginning with (IP_CONST_UNLIMITEDAMMO_*).
        /// </summary>
        /// <param name="nAmmoDamage">The ammo damage type (defaults to Basic)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyUnlimitedAmmo(ItemPropertyUnlimitedType nAmmoDamage = ItemPropertyUnlimitedType.Basic) => _service.ItemPropertyUnlimitedAmmo(nAmmoDamage);

        /// <summary>
        /// Returns an item property limit use by alignment group.
        /// You must specify the alignment group(s) that you want to be able to use this item (IP_CONST_ALIGNMENTGROUP_*).
        /// </summary>
        /// <param name="nAlignGroup">The alignment group constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyLimitUseByAlign(ItemPropertyAlignmentGroupType nAlignGroup) => _service.ItemPropertyLimitUseByAlign(nAlignGroup);

        /// <summary>
        /// Returns an item property limit use by class.
        /// You must specify the class(es) who are able to use this item (IP_CONST_CLASS_*).
        /// </summary>
        /// <param name="nClass">The class constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyLimitUseByClass(ItemPropertyClassType nClass) => _service.ItemPropertyLimitUseByClass(nClass);

        /// <summary>
        /// Returns an item property limit use by race.
        /// You must specify the race(s) who are allowed to use this item (IP_CONST_RACIALTYPE_*).
        /// </summary>
        /// <param name="nRace">The racial type constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyLimitUseByRace(RacialType nRace) => _service.ItemPropertyLimitUseByRace(nRace);

        /// <summary>
        /// Returns an item property limit use by specific alignment.
        /// You must specify the alignment(s) of those allowed to use the item (IP_CONST_ALIGNMENT_*).
        /// </summary>
        /// <param name="nAlignment">The alignment constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyLimitUseBySAlign(ItemPropertyAlignmentType nAlignment) => _service.ItemPropertyLimitUseBySAlign(nAlignment);

        /// <summary>
        /// Replace this function it does nothing.
        /// </summary>
        /// <returns>An invalid item property</returns>
        public static ItemProperty BadBadReplaceMeThisDoesNothing() => _service.BadBadReplaceMeThisDoesNothing();

        /// <summary>
        /// Returns an item property vampiric regeneration.
        /// You must specify the amount of regeneration.
        /// The regen amount must be an integer between 1 and 20.
        /// </summary>
        /// <param name="nRegenAmount">The regeneration amount (1-20)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyVampiricRegeneration(int nRegenAmount) => _service.ItemPropertyVampiricRegeneration(nRegenAmount);

        /// <summary>
        /// Returns an item property trap.
        /// You must specify the trap level constant (IP_CONST_TRAPSTRENGTH_*) and the trap type constant (IP_CONST_TRAPTYPE_*).
        /// </summary>
        /// <param name="nTrapLevel">The trap level constant</param>
        /// <param name="nTrapType">The trap type constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyTrap(ItemPropertyTrapStrengthType nTrapLevel, ItemPropertyTrapType nTrapType) => _service.ItemPropertyTrap(nTrapLevel, nTrapType);

        /// <summary>
        /// Returns an item property true seeing.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyTrueSeeing() => _service.ItemPropertyTrueSeeing();

        /// <summary>
        /// Returns an item property monster on hit apply effect property.
        /// You must specify the property that you want applied on hit.
        /// There are some properties that require an additional special parameter to be specified.
        /// The others that don't require any additional parameter you may just put in the one.
        /// The special cases are as follows:
        /// ABILITYDRAIN: nSpecial is the ability to drain. constant(IP_CONST_ABILITY_*)
        /// DISEASE: nSpecial is the disease that you want applied. constant(DISEASE_*)
        /// LEVELDRAIN: nSpecial is the number of levels that you want drained. integer between 1 and 5.
        /// POISON: nSpecial is the type of poison that will effect the victim. constant(IP_CONST_POISON_*)
        /// WOUNDING: nSpecial is the amount of wounding. integer between 1 and 5.
        /// NOTE: Any that do not appear in the above list do not require the second parameter.
        /// NOTE: These can only be applied to monster NATURAL weapons (e.g. bite, claw, gore, and slam). IT WILL NOT WORK ON NORMAL WEAPONS.
        /// </summary>
        /// <param name="nProperty">The property constant</param>
        /// <param name="nSpecial">The special parameter (defaults to 0)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyOnMonsterHitProperties(int nProperty, int nSpecial = 0) => _service.ItemPropertyOnMonsterHitProperties(nProperty, nSpecial);

        /// <summary>
        /// Returns an item property turn resistance.
        /// You must specify the resistance bonus.
        /// The bonus must be an integer between 1 and 50.
        /// </summary>
        /// <param name="nModifier">The resistance bonus (1-50)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyTurnResistance(int nModifier) => _service.ItemPropertyTurnResistance(nModifier);

        /// <summary>
        /// Returns an item property massive critical.
        /// You must specify the extra damage constant (IP_CONST_DAMAGEBONUS_*) of the criticals.
        /// </summary>
        /// <param name="nDamage">The damage bonus constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyMassiveCritical(ItemPropertyDamageBonusType nDamage) => _service.ItemPropertyMassiveCritical(nDamage);

        /// <summary>
        /// Returns an item property free action.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyFreeAction() => _service.ItemPropertyFreeAction();

        /// <summary>
        /// Returns an item property monster damage.
        /// You must specify the amount of damage the monster's attack will do (IP_CONST_MONSTERDAMAGE_*).
        /// NOTE: These can only be applied to monster NATURAL weapons (e.g. bite, claw, gore, and slam). IT WILL NOT WORK ON NORMAL WEAPONS.
        /// </summary>
        /// <param name="nDamage">The monster damage constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyMonsterDamage(ItemPropertyMonsterDamageType nDamage) => _service.ItemPropertyMonsterDamage(nDamage);

        /// <summary>
        /// Returns an item property immunity to spell level.
        /// You must specify the level of which that and below the user will be immune.
        /// The level must be an integer between 1 and 9.
        /// By putting in a 3 it will mean the user is immune to all 3rd level and lower spells.
        /// </summary>
        /// <param name="nLevel">The spell level (1-9)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyImmunityToSpellLevel(int nLevel) => _service.ItemPropertyImmunityToSpellLevel(nLevel);

        /// <summary>
        /// Returns an item property special walk.
        /// If no parameters are specified it will automatically use the zombie walk.
        /// This will apply the special walk animation to the user.
        /// </summary>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertySpecialWalk() => _service.ItemPropertySpecialWalk();

        /// <summary>
        /// Returns an item property healers kit.
        /// You must specify the level of the kit.
        /// The modifier must be an integer between 1 and 12.
        /// </summary>
        /// <param name="nModifier">The kit level modifier (1-12)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyHealersKit(int nModifier) => _service.ItemPropertyHealersKit(nModifier);

        /// <summary>
        /// Returns an item property weight increase.
        /// You must specify the weight increase constant (IP_CONST_WEIGHTINCREASE_*).
        /// </summary>
        /// <param name="nWeight">The weight increase constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyWeightIncrease(ItemPropertyWeightIncreaseType nWeight) => _service.ItemPropertyWeightIncrease(nWeight);

        /// <summary>
        /// Returns the string tag set for the provided item property.
        /// If no tag has been set, returns an empty string.
        /// </summary>
        /// <param name="nProperty">The item property to get the tag from</param>
        /// <returns>The string tag, or empty string if no tag is set</returns>
        public static string GetItemPropertyTag(ItemProperty nProperty) => _service.GetItemPropertyTag(nProperty);

        /// <summary>
        /// Returns the cost table number of the item property.
        /// See the 2DA files for value definitions.
        /// </summary>
        /// <param name="iProp">The item property to get the cost table from</param>
        /// <returns>The cost table number</returns>
        public static int GetItemPropertyCostTable(ItemProperty iProp) => _service.GetItemPropertyCostTable(iProp);

        /// <summary>
        /// Returns the cost table value (index of the cost table) of the item property.
        /// See the 2DA files for value definitions.
        /// </summary>
        /// <param name="iProp">The item property to get the cost table value from</param>
        /// <returns>The cost table value</returns>
        public static int GetItemPropertyCostTableValue(ItemProperty iProp) => _service.GetItemPropertyCostTableValue(iProp);

        /// <summary>
        /// Returns the param1 number of the item property.
        /// See the 2DA files for value definitions.
        /// </summary>
        /// <param name="iProp">The item property to get the param1 from</param>
        /// <returns>The param1 number</returns>
        public static int GetItemPropertyParam1(ItemProperty iProp) => _service.GetItemPropertyParam1(iProp);

        /// <summary>
        /// Returns the param1 value of the item property.
        /// See the 2DA files for value definitions.
        /// </summary>
        /// <param name="iProp">The item property to get the param1 value from</param>
        /// <returns>The param1 value</returns>
        public static int GetItemPropertyParam1Value(ItemProperty iProp) => _service.GetItemPropertyParam1Value(iProp);

        /// <summary>
        /// Creates a new copy of an item, while making a single change to the appearance of the item.
        /// Helmet models and simple items ignore iIndex.
        /// iType                            iIndex                              iNewValue
        /// ITEM_APPR_TYPE_SIMPLE_MODEL      [Ignored]                           Model #
        /// ITEM_APPR_TYPE_WEAPON_COLOR      ITEM_APPR_WEAPON_COLOR_*            1-9
        /// ITEM_APPR_TYPE_WEAPON_MODEL      ITEM_APPR_WEAPON_MODEL_*            Model #
        /// ITEM_APPR_TYPE_ARMOR_MODEL       ITEM_APPR_ARMOR_MODEL_*             Model #
        /// ITEM_APPR_TYPE_ARMOR_COLOR       ITEM_APPR_ARMOR_COLOR_* [0]         0-175 [1]
        /// [0] Alternatively, where ITEM_APPR_TYPE_ARMOR_COLOR is specified, if per-part coloring is
        /// desired, the following equation can be used for nIndex to achieve that:
        /// ITEM_APPR_ARMOR_NUM_COLORS + (ITEM_APPR_ARMOR_MODEL_ * ITEM_APPR_ARMOR_NUM_COLORS) + ITEM_APPR_ARMOR_COLOR_
        /// For example, to change the CLOTH1 channel of the torso, nIndex would be:
        /// 6 + (7 * 6) + 2 = 50
        /// [1] When specifying per-part coloring, the value 255 is allowed and corresponds with the logical
        /// function 'clear colour override', which clears the per-part override for that part.
        /// </summary>
        /// <param name="oItem">The item to copy and modify</param>
        /// <param name="nType">The appearance type</param>
        /// <param name="nIndex">The appearance index</param>
        /// <param name="nNewValue">The new value</param>
        /// <param name="bCopyVars">Whether to copy variables (defaults to false)</param>
        /// <returns>The new item</returns>
        public static uint CopyItemAndModify(uint oItem, ItemModelColorType nType, int nIndex, int nNewValue,
            bool bCopyVars = false) => _service.CopyItemAndModify(oItem, nType, nIndex, nNewValue, bCopyVars);

        /// <summary>
        /// Creates an item property that (when applied to a weapon item) causes a spell to be cast
        /// when a successful strike is made, or (when applied to armor) is struck by an opponent.
        /// nSpell uses the IP_CONST_ONHIT_CASTSPELL_* constants
        /// </summary>
        /// <param name="nSpellType">The spell type constant</param>
        /// <param name="nLevel">The spell level</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyOnHitCastSpell(ItemPropertyOnHitCastSpellType nSpellType, int nLevel) => _service.ItemPropertyOnHitCastSpell(nSpellType, nLevel);

        /// <summary>
        /// Returns the sub type number of the item property.
        /// See the 2DA files for value definitions.
        /// </summary>
        /// <param name="iProperty">The item property to get the sub type from</param>
        /// <returns>The sub type number</returns>
        public static int GetItemPropertySubType(ItemProperty iProperty) => _service.GetItemPropertySubType(iProperty);

        /// <summary>
        /// Tags the item property with the provided string.
        /// Any tags currently set on the item property will be overwritten.
        /// </summary>
        /// <param name="nProperty">The item property to tag</param>
        /// <param name="sNewTag">The new tag string</param>
        /// <returns>The tagged item property</returns>
        public static ItemProperty TagItemProperty(ItemProperty nProperty, string sNewTag) => _service.TagItemProperty(nProperty, sNewTag);

        /// <summary>
        /// Returns the total duration of the item property in seconds.
        /// Returns 0 if the duration type of the item property is not DURATION_TYPE_TEMPORARY.
        /// </summary>
        /// <param name="nProperty">The item property to get the duration from</param>
        /// <returns>The total duration in seconds</returns>
        public static int GetItemPropertyDuration(ItemProperty nProperty) => _service.GetItemPropertyDuration(nProperty);

        /// <summary>
        /// Returns the remaining duration of the item property in seconds.
        /// Returns 0 if the duration type of the item property is not DURATION_TYPE_TEMPORARY.
        /// </summary>
        /// <param name="nProperty">The item property to get the remaining duration from</param>
        /// <returns>The remaining duration in seconds</returns>
        public static int GetItemPropertyDurationRemaining(ItemProperty nProperty) => _service.GetItemPropertyDurationRemaining(nProperty);

        /// <summary>
        /// Returns an item property material.
        /// You need to specify the material type.
        /// nMaterialType: The material type should be a positive integer between 0 and 77 (see iprp_matcost.2da).
        /// Note: The material type property will only affect the cost of the item if you modify the cost in the iprp_matcost.2da.
        /// </summary>
        /// <param name="nMaterialType">The material type (0-77)</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyMaterial(int nMaterialType) => _service.ItemPropertyMaterial(nMaterialType);

        /// <summary>
        /// Returns an item property quality.
        /// You need to specify the quality.
        /// nQuality: The quality of the item property to create (see iprp_qualcost.2da).
        /// IP_CONST_QUALITY_*
        /// Note: The quality property will only affect the cost of the item if you modify the cost in the iprp_qualcost.2da.
        /// </summary>
        /// <param name="nQuality">The quality constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyQuality(ItemPropertyQualityType nQuality) => _service.ItemPropertyQuality(nQuality);

        /// <summary>
        /// Returns a generic additional item property.
        /// You need to specify the additional property.
        /// nProperty: The item property to create (see iprp_addcost.2da).
        /// IP_CONST_ADDITIONAL_*
        /// Note: The additional property only affects the cost of the item if you modify the cost in the iprp_addcost.2da.
        /// </summary>
        /// <param name="nAdditionalProperty">The additional property constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyAdditional(ItemPropertyAdditionalType nAdditionalProperty) => _service.ItemPropertyAdditional(nAdditionalProperty);

        /// <summary>
        /// Creates an item property that offsets the effect on arcane spell failure
        /// that a particular item has. Parameters come from the ITEM_PROP_ASF_* group.
        /// </summary>
        /// <param name="nModLevel">The arcane spell failure modification level</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyArcaneSpellFailure(ItemPropertyArcaneSpellFailureType nModLevel) => _service.ItemPropertyArcaneSpellFailure(nModLevel);

        /// <summary>
        /// Creates a visual effect (ITEM_VISUAL_*) that may be applied to
        /// melee weapons only.
        /// </summary>
        /// <param name="nEffect">The visual effect constant</param>
        /// <returns>The item property</returns>
        public static ItemProperty ItemPropertyVisualEffect(ItemVisualType nEffect) => _service.ItemPropertyVisualEffect(nEffect);

        /// <summary>
        /// Returns the number of uses per day remaining of the given item and item property.
        /// Will return 0 if the given item does not have the requested item property,
        /// or the item property is not uses/day.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <param name="ip">The item property to check</param>
        /// <returns>The number of uses per day remaining</returns>
        public static int GetItemPropertyUsesPerDayRemaining(uint oItem, IntPtr ip) => _service.GetItemPropertyUsesPerDayRemaining(oItem, ip);

        /// <summary>
        /// Sets the number of uses per day remaining of the given item and item property.
        /// Will do nothing if the given item and item property is not uses/day.
        /// Will constrain nUsesPerDay to the maximum allowed as the cost table defines.
        /// </summary>
        /// <param name="oItem">The item to set</param>
        /// <param name="ip">The item property to set</param>
        /// <param name="nUsesPerDay">The number of uses per day</param>
        public static void SetItemPropertyUsesPerDayRemaining(uint oItem, IntPtr ip, int nUsesPerDay) => _service.SetItemPropertyUsesPerDayRemaining(oItem, ip, nUsesPerDay);

        /// <summary>
        /// Constructs a custom item property given all the parameters explicitly.
        /// This function can be used in place of all the other ItemPropertyXxx constructors
        /// Use GetItemProperty{Type,SubType,CostTableValue,Param1Value} to see the values for a given item property.
        /// </summary>
        /// <param name="nType">The item property type</param>
        /// <param name="nSubType">The sub type (defaults to -1)</param>
        /// <param name="nCostTableValue">The cost table value (defaults to -1)</param>
        /// <param name="nParam1Value">The param1 value (defaults to -1)</param>
        /// <returns>The custom item property</returns>
        public static ItemProperty ItemPropertyCustom(ItemPropertyType nType, int nSubType = -1, int nCostTableValue = -1, int nParam1Value = -1) => _service.ItemPropertyCustom(nType, nSubType, nCostTableValue, nParam1Value);

        /// <summary>
        /// Returns the footstep type of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the footstep type for (default: OBJECT_SELF)</param>
        /// <returns>The footstep type. Returns FOOTSTEP_TYPE_INVALID if used on a non-creature object, or if used on creature that has no footstep sounds by default (e.g., Will-O'-Wisp)</returns>
        /// <remarks>The footstep type determines what the creature's footsteps sound like whenever they take a step.</remarks>
        public static FootstepType GetFootstepType(uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetFootstepType(oCreature);

        /// <summary>
        /// Sets the footstep type of the specified creature.
        /// </summary>
        /// <param name="nFootstepType">The footstep type (FOOTSTEP_TYPE_* constants)</param>
        /// <param name="oCreature">The creature to change the footstep sound for (default: OBJECT_SELF)</param>
        /// <remarks>Changing a creature's footstep type will change the sound that its feet make whenever the creature takes a step. By default a creature's footsteps are determined by the appearance type of the creature. SetFootstepType() allows you to make a creature use a different footstep type than it would use by default for its given appearance. Possible values: FOOTSTEP_TYPE_NORMAL, FOOTSTEP_TYPE_LARGE, FOOTSTEP_TYPE_DRAGON, FOOTSTEP_TYPE_SOFT, FOOTSTEP_TYPE_HOOF, FOOTSTEP_TYPE_HOOF_LARGE, FOOTSTEP_TYPE_BEETLE, FOOTSTEP_TYPE_SPIDER, FOOTSTEP_TYPE_SKELETON, FOOTSTEP_TYPE_LEATHER_WING, FOOTSTEP_TYPE_FEATHER_WING, FOOTSTEP_TYPE_DEFAULT (makes the creature use its original default footstep sounds), FOOTSTEP_TYPE_NONE</remarks>
        public static void SetFootstepType(FootstepType nFootstepType, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.SetFootstepType(nFootstepType, oCreature);

        /// <summary>
        /// Returns the wing type of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the wing type for (default: OBJECT_SELF)</param>
        /// <returns>The wing type. Returns CREATURE_WING_TYPE_NONE if used on a non-creature object, if the creature has no wings, or if the creature cannot have its wing type changed in the toolset</returns>
        /// <remarks>Possible values: CREATURE_WING_TYPE_NONE, CREATURE_WING_TYPE_DEMON, CREATURE_WING_TYPE_ANGEL, CREATURE_WING_TYPE_BAT, CREATURE_WING_TYPE_DRAGON, CREATURE_WING_TYPE_BUTTERFLY, CREATURE_WING_TYPE_BIRD</remarks>
        public static CreatureWingType GetCreatureWingType(uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetCreatureWingType(oCreature);

        /// <summary>
        /// Sets the wing type of the specified creature.
        /// </summary>
        /// <param name="nWingType">The wing type (CREATURE_WING_TYPE_* constants)</param>
        /// <param name="oCreature">The creature to change the wing type for (default: OBJECT_INVALID)</param>
        /// <remarks>Only two creature model types will support wings. The MODELTYPE for the part based (playable races) 'P' and MODELTYPE 'W' in the appearance.2da. Possible values: CREATURE_WING_TYPE_NONE, CREATURE_WING_TYPE_DEMON, CREATURE_WING_TYPE_ANGEL, CREATURE_WING_TYPE_BAT, CREATURE_WING_TYPE_DRAGON, CREATURE_WING_TYPE_BUTTERFLY, CREATURE_WING_TYPE_BIRD</remarks>
        public static void SetCreatureWingType(CreatureWingType nWingType, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.SetCreatureWingType(nWingType, oCreature);

        /// <summary>
        /// Returns the model number being used for the specified body part and creature.
        /// </summary>
        /// <param name="nPart">The body part (CREATURE_PART_* constants)</param>
        /// <param name="oCreature">The creature to get the body part for (default: OBJECT_SELF)</param>
        /// <returns>The model number for the body part. Returns CREATURE_PART_INVALID if used on a non-creature object, or if the creature does not use a part based model</returns>
        /// <remarks>The model number returned is for the body part when the creature is not wearing armor (i.e. whether or not the creature is wearing armor does not affect the return value). Only works on part based creatures, which is typically restricted to the playable races (unless some new part based custom content has been added to the module). Possible body parts: CREATURE_PART_RIGHT_FOOT, CREATURE_PART_LEFT_FOOT, CREATURE_PART_RIGHT_SHIN, CREATURE_PART_LEFT_SHIN, CREATURE_PART_RIGHT_THIGH, CREATURE_PART_LEFT_THIGH, CREATURE_PART_PELVIS, CREATURE_PART_TORSO, CREATURE_PART_BELT, CREATURE_PART_NECK, CREATURE_PART_RIGHT_FOREARM, CREATURE_PART_LEFT_FOREARM, CREATURE_PART_RIGHT_BICEP, CREATURE_PART_LEFT_BICEP, CREATURE_PART_RIGHT_SHOULDER, CREATURE_PART_LEFT_SHOULDER, CREATURE_PART_RIGHT_HAND, CREATURE_PART_LEFT_HAND, CREATURE_PART_HEAD</remarks>
        public static int GetCreatureBodyPart(CreaturePartType nPart, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetCreatureBodyPart(nPart, oCreature);

        /// <summary>
        /// Sets the body part model to be used on the specified creature.
        /// </summary>
        /// <param name="nPart">The body part (CREATURE_PART_* constants)</param>
        /// <param name="nModelNumber">The model number (CREATURE_MODEL_TYPE_* constants)</param>
        /// <param name="oCreature">The creature to change the body part for (default: OBJECT_SELF)</param>
        /// <remarks>The model names for parts need to be in the following format: p&lt;m/f&gt;&lt;race letter&gt;&lt;phenotype&gt;_&lt;body part&gt;&lt;model number&gt;.mdl. Only part based creature appearance types are supported (i.e. The model types for the playable races ('P') in the appearance.2da). Possible body parts: CREATURE_PART_RIGHT_FOOT, CREATURE_PART_LEFT_FOOT, CREATURE_PART_RIGHT_SHIN, CREATURE_PART_LEFT_SHIN, CREATURE_PART_RIGHT_THIGH, CREATURE_PART_LEFT_THIGH, CREATURE_PART_PELVIS, CREATURE_PART_TORSO, CREATURE_PART_BELT, CREATURE_PART_NECK, CREATURE_PART_RIGHT_FOREARM, CREATURE_PART_LEFT_FOREARM, CREATURE_PART_RIGHT_BICEP, CREATURE_PART_LEFT_BICEP, CREATURE_PART_RIGHT_SHOULDER, CREATURE_PART_LEFT_SHOULDER, CREATURE_PART_RIGHT_HAND, CREATURE_PART_LEFT_HAND, CREATURE_PART_HEAD. Possible model types: CREATURE_MODEL_TYPE_NONE, CREATURE_MODEL_TYPE_SKIN (not for use on shoulders, pelvis or head), CREATURE_MODEL_TYPE_TATTOO (for body parts that support tattoos, i.e. not heads/feet/hands), CREATURE_MODEL_TYPE_UNDEAD (undead model only exists for the right arm parts).</remarks>
        public static void SetCreatureBodyPart(CreaturePartType nPart, int nModelNumber, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.SetCreatureBodyPart(nPart, nModelNumber, oCreature);

        /// <summary>
        /// Returns the tail type of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the tail type for (default: OBJECT_SELF)</param>
        /// <returns>The tail type. Returns CREATURE_TAIL_TYPE_NONE if used on a non-creature object, if the creature has no tail, or if the creature cannot have its tail type changed in the toolset</returns>
        /// <remarks>Possible values: CREATURE_TAIL_TYPE_NONE, CREATURE_TAIL_TYPE_LIZARD, CREATURE_TAIL_TYPE_BONE, CREATURE_TAIL_TYPE_DEVIL</remarks>
        public static CreatureTailType GetCreatureTailType(uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetCreatureTailType(oCreature);

        /// <summary>
        /// Sets the tail type of the specified creature.
        /// </summary>
        /// <param name="nTailType">The tail type (CREATURE_TAIL_TYPE_* constants)</param>
        /// <param name="oCreature">The creature to change the tail type for (default: OBJECT_SELF)</param>
        /// <remarks>Only two creature model types will support tails. The MODELTYPE for the part based (playable) races 'P' and MODELTYPE 'T' in the appearance.2da. Possible values: CREATURE_TAIL_TYPE_NONE, CREATURE_TAIL_TYPE_LIZARD, CREATURE_TAIL_TYPE_BONE, CREATURE_TAIL_TYPE_DEVIL</remarks>
        public static void SetCreatureTailType(CreatureTailType nTailType, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.SetCreatureTailType(nTailType, oCreature);

        /// <summary>
        /// Returns the creature's currently set phenotype (body type).
        /// </summary>
        /// <param name="oCreature">The creature to get the phenotype for</param>
        /// <returns>The creature's phenotype</returns>
        public static PhenoType GetPhenoType(uint oCreature) => _service.GetPhenoType(oCreature);

        /// <summary>
        /// Sets the creature's phenotype (body type) to the specified type.
        /// </summary>
        /// <param name="nPhenoType">The phenotype type to set</param>
        /// <param name="oCreature">The creature to change the phenotype for (default: OBJECT_SELF)</param>
        /// <remarks>SetPhenoType will only work on part based creatures (i.e. the starting default playable races). Possible values: PHENOTYPE_NORMAL, PHENOTYPE_BIG, PHENOTYPE_CUSTOM* (custom phenotypes should only be used if you have specifically created your own custom content that requires the use of a new phenotype and you have specified the appropriate custom phenotype in your custom content)</remarks>
        public static void SetPhenoType(PhenoType nPhenoType, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.SetPhenoType(nPhenoType, oCreature);

        /// <summary>
        /// Returns whether this creature is able to be disarmed.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>True if the creature can be disarmed, false otherwise</returns>
        /// <remarks>Checks disarm flag on creature, and if the creature actually has a weapon equipped in their right hand that is droppable</remarks>
        public static bool GetIsCreatureDisarmable(uint oCreature) => _service.GetIsCreatureDisarmable(oCreature);

        /// <summary>
        /// Returns the class that the specified spellcaster cast the spell as.
        /// </summary>
        /// <returns>The class type. Returns CLASS_TYPE_INVALID if the caster has no valid class (placeables, etc.)</returns>
        public static ClassType GetLastSpellCastClass() => _service.GetLastSpellCastClass();

        /// <summary>
        /// Sets the number of base attacks for the specified creature.
        /// </summary>
        /// <param name="nBaseAttackBonus">The number of base attacks (range: 1 to 6)</param>
        /// <param name="oCreature">The creature to set the base attack bonus for (default: OBJECT_SELF)</param>
        /// <remarks>This function does not work on Player Characters.</remarks>
        public static void SetBaseAttackBonus(int nBaseAttackBonus, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.SetBaseAttackBonus(nBaseAttackBonus, oCreature);

        /// <summary>
        /// Restores the number of base attacks back to its original state.
        /// </summary>
        /// <param name="oCreature">The creature to restore the base attack bonus for (default: OBJECT_SELF)</param>
        public static void RestoreBaseAttackBonus(uint oCreature = NWScriptService.OBJECT_INVALID) => _service.RestoreBaseAttackBonus(oCreature);

        /// <summary>
        /// Sets the creature's appearance type to the specified value.
        /// </summary>
        /// <param name="oCreature">The creature to change the appearance type for</param>
        /// <param name="nAppearanceType">The appearance type (APPEARANCE_TYPE_* constants)</param>
        public static void SetCreatureAppearanceType(uint oCreature, AppearanceType nAppearanceType) => _service.SetCreatureAppearanceType(oCreature, nAppearanceType);

        /// <summary>
        /// Returns the default package selected for this creature to level up with.
        /// </summary>
        /// <param name="oCreature">The creature to get the starting package for</param>
        /// <returns>The starting package. Returns PACKAGE_INVALID if error occurs</returns>
        public static int GetCreatureStartingPackage(uint oCreature) => _service.GetCreatureStartingPackage(oCreature);

        /// <summary>
        /// Returns the spell resistance of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get spell resistance for</param>
        /// <returns>The spell resistance value. Returns 0 if the creature has no spell resistance or an invalid creature is passed in</returns>
        public static int GetSpellResistance(uint oCreature) => _service.GetSpellResistance(oCreature);

        /// <summary>
        /// Sets the lootable state of a living NPC creature.
        /// </summary>
        /// <param name="oCreature">The creature to set the lootable state for</param>
        /// <param name="bLootable">Whether the creature is lootable</param>
        /// <remarks>This function will not work on players or dead creatures.</remarks>
        public static void SetLootable(uint oCreature, bool bLootable) => _service.SetLootable(oCreature, bLootable);

        /// <summary>
        /// Returns the lootable state of a creature.
        /// </summary>
        /// <param name="oCreature">The creature to check the lootable state for</param>
        /// <returns>True if the creature is lootable, false otherwise</returns>
        public static bool GetLootable(uint oCreature) => _service.GetLootable(oCreature);

        /// <summary>
        /// Gets the status of the specified action mode on a creature.
        /// </summary>
        /// <param name="oCreature">The creature to check the action mode for</param>
        /// <param name="nMode">The action mode to check (ACTION_MODE_* constants)</param>
        /// <returns>True if the action mode is active, false otherwise</returns>
        public static bool GetActionMode(uint oCreature, ActionModeType nMode) => _service.GetActionMode(oCreature, nMode);

        /// <summary>
        /// Sets the status of the specified action mode on a creature.
        /// </summary>
        /// <param name="oCreature">The creature to set the action mode for</param>
        /// <param name="nMode">The action mode to set (ACTION_MODE_* constants)</param>
        /// <param name="nStatus">The status to set (true/false)</param>
        public static void SetActionMode(uint oCreature, ActionModeType nMode, bool nStatus) => _service.SetActionMode(oCreature, nMode, nStatus);

        /// <summary>
        /// Returns the current arcane spell failure factor of a creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the arcane spell failure for</param>
        /// <returns>The arcane spell failure factor</returns>
        public static int GetArcaneSpellFailure(uint oCreature) => _service.GetArcaneSpellFailure(oCreature);

        /// <summary>
        /// Sets the name of the creature's sub race.
        /// </summary>
        /// <param name="oCreature">The creature to set the sub race for</param>
        /// <param name="sSubRace">The sub race name to set</param>
        public static void SetSubRace(uint oCreature, string sSubRace) => _service.SetSubRace(oCreature, sSubRace);

        /// <summary>
        /// Sets the name of the creature's deity.
        /// </summary>
        /// <param name="oCreature">The creature to set the deity for</param>
        /// <param name="sDeity">The deity name to set</param>
        public static void SetDeity(uint oCreature, string sDeity) => _service.SetDeity(oCreature, sDeity);

        /// <summary>
        /// Returns true if the creature is currently possessed by a DM character.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>True if the creature is possessed by a DM, false otherwise</returns>
        /// <remarks>GetIsDMPossessed() will return false if oCreature is the DM character. To determine if oCreature is a DM character use GetIsDM()</remarks>
        public static bool GetIsDMPossessed(uint oCreature) => _service.GetIsDMPossessed(oCreature);

        /// <summary>
        /// Increments the remaining uses per day for the specified feat on the creature by one.
        /// </summary>
        /// <param name="oCreature">The creature to modify</param>
        /// <param name="nFeat">The feat constant (FEAT_* constants)</param>
        /// <remarks>Total number of feats per day cannot exceed the maximum.</remarks>
        public static void IncrementRemainingFeatUses(uint oCreature, FeatType nFeat) => _service.IncrementRemainingFeatUses(oCreature, nFeat);

        /// <summary>
        /// Gets the current AI level that the creature is running at.
        /// </summary>
        /// <param name="oTarget">The creature to get the AI level for (default: OBJECT_SELF)</param>
        /// <returns>One of the following: AI_LEVEL_INVALID, AI_LEVEL_VERY_LOW, AI_LEVEL_LOW, AI_LEVEL_NORMAL, AI_LEVEL_HIGH, AI_LEVEL_VERY_HIGH</returns>
        public static AILevelType GetAILevel(uint oTarget = NWScriptService.OBJECT_INVALID) => _service.GetAILevel(oTarget);

        /// <summary>
        /// Sets the current AI level of the creature to the specified value.
        /// </summary>
        /// <param name="oTarget">The creature to set the AI level for</param>
        /// <param name="nAILevel">The AI level to set</param>
        /// <remarks>Does not work on Players. The game by default will choose an appropriate AI level for creatures based on the circumstances that the creature is in. Explicitly setting an AI level will override the game AI settings. The new setting will last until SetAILevel is called again with the argument AI_LEVEL_DEFAULT. AI_LEVEL_DEFAULT - Default setting. The game will take over setting the appropriate AI level when required. AI_LEVEL_VERY_LOW - Very Low priority, very stupid, but low CPU usage for AI. Typically used when no players are in the area. AI_LEVEL_LOW - Low priority, mildly stupid, but slightly more CPU usage for AI. Typically used when not in combat, but a player is in the area. AI_LEVEL_NORMAL - Normal priority, average AI, but more CPU usage required for AI. Typically used when creature is in combat. AI_LEVEL_HIGH - High priority, smartest AI, but extremely high CPU usage required for AI. Avoid using this. It is most likely only ever needed for cutscenes.</remarks>
        public static void SetAILevel(uint oTarget, AILevelType nAILevel) => _service.SetAILevel(oTarget, nAILevel);

        /// <summary>
        /// Returns true if the creature is a familiar currently possessed by its master.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>True if the creature is a possessed familiar, false if not or if the creature object is invalid</returns>
        public static bool GetIsPossessedFamiliar(uint oCreature) => _service.GetIsPossessedFamiliar(oCreature);

        /// <summary>
        /// Causes a Player Creature to unpossess their familiar.
        /// </summary>
        /// <param name="oCreature">The creature to unpossess the familiar for</param>
        /// <remarks>It will work if run on the player creature or the possessed familiar. It does not work in conjunction with any DM possession.</remarks>
        public static void UnpossessFamiliar(uint oCreature) => _service.UnpossessFamiliar(oCreature);

        /// <summary>
        /// Gets the immortal flag on a creature.
        /// </summary>
        /// <param name="oTarget">The creature to check the immortal flag for (default: OBJECT_SELF)</param>
        /// <returns>True if the creature is immortal, false otherwise</returns>
        public static bool GetImmortal(uint oTarget = NWScriptService.OBJECT_INVALID) => _service.GetImmortal(oTarget);

        /// <summary>
        /// Performs a single attack on every hostile creature within 10ft of the attacker and determines damage accordingly.
        /// </summary>
        /// <param name="bDisplayFeedback">Whether or not feedback should be displayed (default: true)</param>
        /// <param name="bImproved">If true, the improved version of whirlwind is used (default: false)</param>
        /// <remarks>If the attacker has a ranged weapon equipped, this will have no effect. This is meant to be called inside the spell script for whirlwind attack, it is not meant to be used to queue up a new whirlwind attack. To do that you need to call ActionUseFeat(FEAT_WHIRLWIND_ATTACK, oEnemy)</remarks>
        public static void DoWhirlwindAttack(bool bDisplayFeedback = true, bool bImproved = false) => _service.DoWhirlwindAttack(bDisplayFeedback, bImproved);

        /// <summary>
        /// Returns the base attack bonus for the given creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the base attack bonus for</param>
        /// <returns>The base attack bonus</returns>
        public static int GetBaseAttackBonus(uint oCreature) => _service.GetBaseAttackBonus(oCreature);

        /// <summary>
        /// Sets a creature's immortality flag.
        /// </summary>
        /// <param name="oCreature">The creature to set the immortality flag for</param>
        /// <param name="bImmortal">True = creature is immortal and cannot be killed (but still takes damage), False = creature is not immortal and is damaged normally</param>
        /// <remarks>This scripting command only works on Creature objects.</remarks>
        public static void SetImmortal(uint oCreature, bool bImmortal) => _service.SetImmortal(oCreature, bImmortal);

        /// <summary>
        /// Returns true if 1d20 roll + skill rank is greater than or equal to difficulty.
        /// </summary>
        /// <param name="oTarget">The creature using the skill</param>
        /// <param name="nSkill">The skill being used (SKILL_* constants)</param>
        /// <param name="nDifficulty">The difficulty class of the skill</param>
        /// <returns>True if the skill check succeeds, false otherwise</returns>
        public static bool GetIsSkillSuccessful(uint oTarget, NWNSkillType nSkill, int nDifficulty) => _service.GetIsSkillSuccessful(oTarget, nSkill, nDifficulty);

        /// <summary>
        /// Decrements the remaining uses per day for the specified feat on the creature by one.
        /// </summary>
        /// <param name="oCreature">The creature to modify</param>
        /// <param name="nFeat">The feat constant (FEAT_* constants)</param>
        public static void DecrementRemainingFeatUses(uint oCreature, int nFeat) => _service.DecrementRemainingFeatUses(oCreature, nFeat);

        /// <summary>
        /// Decrements the remaining uses per day for the specified spell on the creature by one.
        /// </summary>
        /// <param name="oCreature">The creature to modify</param>
        /// <param name="nSpell">The spell constant (SPELL_* constants)</param>
        public static void DecrementRemainingSpellUses(uint oCreature, int nSpell) => _service.DecrementRemainingSpellUses(oCreature, nSpell);

        /// <summary>
        /// Returns the stealth mode of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the stealth mode for</param>
        /// <returns>A constant STEALTH_MODE_*</returns>
        public static StealthModeType GetStealthMode(uint oCreature) => _service.GetStealthMode(oCreature);

        /// <summary>
        /// Returns the detection mode of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the detection mode for</param>
        /// <returns>A constant DETECT_MODE_*</returns>
        public static DetectModeType GetDetectMode(uint oCreature) => _service.GetDetectMode(oCreature);

        /// <summary>
        /// Returns the defensive casting mode of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the defensive casting mode for</param>
        /// <returns>A constant DEFENSIVE_CASTING_MODE_*</returns>
        public static CastingModeType GetDefensiveCastingMode(uint oCreature) => _service.GetDefensiveCastingMode(oCreature);

        /// <summary>
        /// Returns the appearance type of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the appearance type for</param>
        /// <returns>A constant APPEARANCE_TYPE_* for valid creatures, APPEARANCE_TYPE_INVALID for non-creatures/invalid creatures</returns>
        public static AppearanceType GetAppearanceType(uint oCreature) => _service.GetAppearanceType(oCreature);

        /// <summary>
        /// Gets the last object that was sent as a GetLastAttacker(), GetLastDamager(), GetLastSpellCaster() (for a hostile spell), or GetLastDisturbed() (when a creature is pickpocketed).
        /// </summary>
        /// <param name="oVictim">The victim object (default: OBJECT_SELF)</param>
        /// <returns>The last hostile actor</returns>
        /// <remarks>Return values may only ever be: 1) A Creature, 2) Plot Characters will never have this value set, 3) Area of Effect Objects will return the AOE creator if they are registered as this value, otherwise they will return INVALID_OBJECT_ID, 4) Traps will not return the creature that set the trap, 5) This value will never be overwritten by another non-creature object, 6) This value will never be a dead/destroyed creature</remarks>
        public static uint GetLastHostileActor(uint oVictim = NWScriptService.OBJECT_INVALID) => _service.GetLastHostileActor(oVictim);

        /// <summary>
        /// Gets the number of hit dice worth of Turn Resistance that the undead creature may have.
        /// </summary>
        /// <param name="oUndead">The undead creature to check (default: OBJECT_SELF)</param>
        /// <returns>The number of hit dice of turn resistance</returns>
        /// <remarks>This will only work on undead creatures.</remarks>
        public static int GetTurnResistanceHD(uint oUndead = NWScriptService.OBJECT_INVALID) => _service.GetTurnResistanceHD(oUndead);

        /// <summary>
        /// Gets the size of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the size for</param>
        /// <returns>The creature size (CREATURE_SIZE_* constants)</returns>
        public static CreatureSizeType GetCreatureSize(uint oCreature) => _service.GetCreatureSize(oCreature);

        /// <summary>
        /// Causes all creatures within a 10-metre radius to stop what they are doing and sets the NPC's enemies within this range to be neutral towards the NPC for roughly 3 minutes.
        /// </summary>
        /// <remarks>Use this on an NPC. If this command is run on a PC or an object that is not a creature, nothing will happen.</remarks>
        public static void SurrenderToEnemies() => _service.SurrenderToEnemies();

        /// <summary>
        /// Determines whether the source has a friendly reaction towards the target.
        /// </summary>
        /// <param name="oTarget">The target to check the reaction for</param>
        /// <param name="oSource">The source to check the reaction from (default: OBJECT_SELF)</param>
        /// <returns>True if the source has a friendly reaction towards the target</returns>
        /// <remarks>This depends on the reputation, PVP setting and (if both oSource and oTarget are PCs), oSource's Like/Dislike setting for oTarget. If you just want to know how two objects feel about each other in terms of faction and personal reputation, use GetIsFriend() instead.</remarks>
        public static int GetIsReactionTypeFriendly(uint oTarget, uint oSource = NWScriptService.OBJECT_INVALID) => _service.GetIsReactionTypeFriendly(oTarget, oSource);

        /// <summary>
        /// Determines whether the source has a neutral reaction towards the target.
        /// </summary>
        /// <param name="oTarget">The target to check the reaction for</param>
        /// <param name="oSource">The source to check the reaction from (default: OBJECT_SELF)</param>
        /// <returns>True if the source has a neutral reaction towards the target</returns>
        /// <remarks>This depends on the reputation, PVP setting and (if both oSource and oTarget are PCs), oSource's Like/Dislike setting for oTarget. If you just want to know how two objects feel about each other in terms of faction and personal reputation, use GetIsNeutral() instead.</remarks>
        public static int GetIsReactionTypeNeutral(uint oTarget, uint oSource = NWScriptService.OBJECT_INVALID) => _service.GetIsReactionTypeNeutral(oTarget, oSource);

        /// <summary>
        /// Determines whether the source has a hostile reaction towards the target.
        /// </summary>
        /// <param name="oTarget">The target to check the reaction for</param>
        /// <param name="oSource">The source to check the reaction from (default: OBJECT_SELF)</param>
        /// <returns>True if the source has a hostile reaction towards the target</returns>
        /// <remarks>This depends on the reputation, PVP setting and (if both oSource and oTarget are PCs), oSource's Like/Dislike setting for oTarget. If you just want to know how two objects feel about each other in terms of faction and personal reputation, use GetIsEnemy() instead.</remarks>
        public static bool GetIsReactionTypeHostile(uint oTarget, uint oSource = NWScriptService.OBJECT_INVALID) => _service.GetIsReactionTypeHostile(oTarget, oSource);

        /// <summary>
        /// Takes the specified amount of gold from the creature.
        /// </summary>
        /// <param name="nAmount">The amount of gold to take</param>
        /// <param name="oCreatureToTakeFrom">The creature to take gold from. If this is not a valid creature, nothing will happen</param>
        /// <param name="bDestroy">If true, the caller will not get the gold. Instead, the gold will be destroyed and will vanish from the game (default: false)</param>
        public static void TakeGoldFromCreature(int nAmount, uint oCreatureToTakeFrom, bool bDestroy = false) => _service.TakeGoldFromCreature(nAmount, oCreatureToTakeFrom, bDestroy);

        /// <summary>
        /// Gets the object that killed the specified object.
        /// </summary>
        /// <returns>The object that killed the specified object</returns>
        public static uint GetLastKiller() => _service.GetLastKiller();

        /// <summary>
        /// Returns true if the creature is the Dungeon Master.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>True if the creature is the Dungeon Master, false otherwise</returns>
        /// <remarks>This will return false if oCreature is a DM Possessed creature. To determine if oCreature is a DM Possessed creature, use GetIsDMPossessed()</remarks>
        public static bool GetIsDM(uint oCreature) => _service.GetIsDM(oCreature);

        /// <summary>
        /// Gets the object ID of the player who last pressed the respawn button.
        /// </summary>
        /// <returns>The object ID of the player who last pressed the respawn button</returns>
        /// <remarks>Use this in an OnRespawnButtonPressed module script.</remarks>
        public static uint GetLastRespawnButtonPresser() => _service.GetLastRespawnButtonPresser();

        /// <summary>
        /// Makes the creature equip the armor in its possession that has the highest armor class.
        /// </summary>
        public static void ActionEquipMostEffectiveArmor() => _service.ActionEquipMostEffectiveArmor();

        /// <summary>
        /// Returns true if the creature was spawned from an encounter.
        /// </summary>
        /// <param name="oCreature">The creature to check (default: OBJECT_SELF)</param>
        /// <returns>True if the creature was spawned from an encounter, false otherwise</returns>
        public static int GetIsEncounterCreature(uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetIsEncounterCreature(oCreature);

        /// <summary>
        /// Makes the creature equip the melee weapon in its possession that can do the most damage.
        /// </summary>
        /// <param name="oVersus">You can try to get the most damaging weapon against this target (default: OBJECT_INVALID)</param>
        /// <param name="bOffHand">Whether to equip in the off-hand (default: false)</param>
        /// <remarks>If no valid melee weapon is found, it will equip the most damaging range weapon. This function should only ever be called in the EndOfCombatRound scripts, because otherwise it would have to stop the combat round to run simulation.</remarks>
        public static void ActionEquipMostDamagingMelee(uint oVersus = NWScriptService.OBJECT_INVALID, bool bOffHand = false) => _service.ActionEquipMostDamagingMelee(oVersus, bOffHand);

        /// <summary>
        /// Makes the creature equip the range weapon in its possession that can do the most damage.
        /// </summary>
        /// <param name="oVersus">You can try to get the most damaging weapon against this target (default: OBJECT_INVALID)</param>
        /// <remarks>If no valid range weapon can be found, it will equip the most damaging melee weapon.</remarks>
        public static void ActionEquipMostDamagingRanged(uint oVersus = NWScriptService.OBJECT_INVALID) => _service.ActionEquipMostDamagingRanged(oVersus);

        /// <summary>
        /// Gives the specified amount of experience points to the creature.
        /// </summary>
        /// <param name="oCreature">The creature to give experience to</param>
        /// <param name="nXpAmount">The amount of experience points to give</param>
        public static void GiveXPToCreature(uint oCreature, int nXpAmount) => _service.GiveXPToCreature(oCreature, nXpAmount);

        /// <summary>
        /// Sets the creature's experience to the specified amount.
        /// </summary>
        /// <param name="oCreature">The creature to set the experience for</param>
        /// <param name="nXpAmount">The amount of experience points to set</param>
        public static void SetXP(uint oCreature, int nXpAmount) => _service.SetXP(oCreature, nXpAmount);

        /// <summary>
        /// Gets the creature's experience points.
        /// </summary>
        /// <param name="oCreature">The creature to get the experience for</param>
        /// <returns>The creature's experience points</returns>
        public static int GetXP(uint oCreature) => _service.GetXP(oCreature);

        /// <summary>
        /// Forces the action subject to move to the specified location.
        /// </summary>
        /// <param name="lDestination">The destination location to move to</param>
        /// <param name="bRun">Whether to run to the destination (default: false)</param>
        /// <param name="fTimeout">The timeout in seconds (default: 30.0f)</param>
        public static void ActionForceMoveToLocation(Location lDestination, bool bRun = false, float fTimeout = 30.0f) => _service.ActionForceMoveToLocation(lDestination, bRun, fTimeout);

        /// <summary>
        /// Forces the action subject to move to the specified object.
        /// </summary>
        /// <param name="oMoveTo">The object to move to</param>
        /// <param name="bRun">Whether to run to the object (default: false)</param>
        /// <param name="fRange">The range to stop at (default: 1.0f)</param>
        /// <param name="fTimeout">The timeout in seconds (default: 30.0f)</param>
        public static void ActionForceMoveToObject(uint oMoveTo, bool bRun = false, float fRange = 1.0f,
            float fTimeout = 30.0f) => _service.ActionForceMoveToObject(oMoveTo, bRun, fRange, fTimeout);

        /// <summary>
        /// Gets the last creature that opened the specified object.
        /// </summary>
        /// <param name="oObject">The object to check (defaults to OBJECT_SELF)</param>
        /// <returns>The last creature that opened the object. Returns OBJECT_INVALID if the object is not a valid door, placeable or store</returns>
        public static uint GetLastOpenedBy(uint oObject = NWScriptService.OBJECT_INVALID) => _service.GetLastOpenedBy(oObject);

        /// <summary>
        /// Determines the number of times that the creature has the specified spell memorized.
        /// </summary>
        /// <param name="nSpell">The spell to check (SPELL_* constants)</param>
        /// <param name="oCreature">The creature to check the spell for (default: OBJECT_SELF)</param>
        /// <returns>The number of times the creature has the spell memorized</returns>
        public static int GetHasSpell(SpellType nSpell, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetHasSpell(nSpell, oCreature);

        /// <summary>
        /// Gets the gender of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the gender for</param>
        /// <returns>The creature's gender</returns>
        public static GenderType GetGender(uint oCreature) => _service.GetGender(oCreature);

        /// <summary>
        /// Gets the type of disturbance that caused the specified object's OnInventoryDisturbed script to fire.
        /// </summary>
        /// <param name="oObject">The object to get the inventory disturb type for (defaults to OBJECT_SELF)</param>
        /// <returns>The type of disturbance (INVENTORY_DISTURB_* constants)</returns>
        /// <remarks>This will only work for creatures and placeables.</remarks>
        public static DisturbType GetInventoryDisturbType(uint oObject = NWScriptService.OBJECT_INVALID) => _service.GetInventoryDisturbType(oObject);

        /// <summary>
        /// Gets the item that caused the specified object's OnInventoryDisturbed script to fire.
        /// </summary>
        /// <param name="oObject">The object to get the inventory disturb item for (defaults to OBJECT_SELF)</param>
        /// <returns>The item that caused the disturbance. Returns OBJECT_INVALID if the caller is not a valid object</returns>
        public static uint GetInventoryDisturbItem(uint oObject = NWScriptService.OBJECT_INVALID) => _service.GetInventoryDisturbItem(oObject);

        /// <summary>
        /// Determines the creature's class based on the class position.
        /// </summary>
        /// <param name="nClassPosition">The class position (1, 2, or 3)</param>
        /// <param name="oCreature">The creature to get the class for (default: OBJECT_SELF)</param>
        /// <returns>The creature's class (CLASS_TYPE_* constants). Returns CLASS_TYPE_INVALID if the creature does not have a class in the specified position or if the creature is not valid</returns>
        /// <remarks>A creature can have up to three classes. A single-class creature will only have a value in nClassPosition=1.</remarks>
        public static ClassType GetClassByPosition(int nClassPosition, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetClassByPosition(nClassPosition, oCreature);

        /// <summary>
        /// Determines the creature's class level based on the class position.
        /// </summary>
        /// <param name="nClassPosition">The class position (1, 2, or 3)</param>
        /// <param name="oCreature">The creature to get the class level for (default: OBJECT_SELF)</param>
        /// <returns>The creature's class level. Returns 0 if the creature does not have a class in the specified position or if the creature is not valid</returns>
        /// <remarks>A creature can have up to three classes. A single-class creature will only have a value in nClassPosition=1.</remarks>
        public static int GetLevelByPosition(int nClassPosition, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetLevelByPosition(nClassPosition, oCreature);

        /// <summary>
        /// Determines the levels that the creature holds in the specified class type.
        /// </summary>
        /// <param name="nClassType">The class type (CLASS_TYPE_* constants)</param>
        /// <param name="oCreature">The creature to get the class level for (default: OBJECT_SELF)</param>
        /// <returns>The number of levels the creature has in the specified class</returns>
        public static int GetLevelByClass(ClassType nClassType, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetLevelByClass(nClassType, oCreature);

        /// <summary>
        /// Returns the ability modifier for the specified ability.
        /// </summary>
        /// <param name="nAbility">The ability type (ABILITY_* constants)</param>
        /// <param name="oCreature">The creature to get the ability modifier for (default: OBJECT_SELF)</param>
        /// <returns>The ability modifier for the specified ability</returns>
        public static int GetAbilityModifier(AbilityType nAbility, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetAbilityModifier(nAbility, oCreature);

        /// <summary>
        /// Returns true if the creature is in combat.
        /// </summary>
        /// <param name="oCreature">The creature to check (default: OBJECT_SELF)</param>
        /// <returns>True if the creature is in combat, false otherwise</returns>
        public static bool GetIsInCombat(uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetIsInCombat(oCreature);

        /// <summary>
        /// Gives the specified amount of gold to the creature.
        /// </summary>
        /// <param name="oCreature">The creature to give gold to</param>
        /// <param name="nGP">The amount of gold to give</param>
        public static void GiveGoldToCreature(uint oCreature, int nGP) => _service.GiveGoldToCreature(oCreature, nGP);

        /// <summary>
        /// Gets the creature nearest to the specified location, subject to all the criteria specified.
        /// </summary>
        /// <param name="nFirstCriteriaType">The first criteria type (CREATURE_TYPE_* constants)</param>
        /// <param name="nFirstCriteriaValue">The first criteria value</param>
        /// <param name="lLocation">The location to find the nearest creature to</param>
        /// <param name="nNth">The Nth nearest creature to find (default: 1)</param>
        /// <param name="nSecondCriteriaType">The second criteria type (default: -1)</param>
        /// <param name="nSecondCriteriaValue">The second criteria value (default: -1)</param>
        /// <param name="nThirdCriteriaType">The third criteria type (default: -1)</param>
        /// <param name="nThirdCriteriaValue">The third criteria value (default: -1)</param>
        /// <returns>The nearest creature. Returns OBJECT_INVALID on error</returns>
        /// <remarks>Criteria values: CLASS_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_CLASS, SPELL_* if nFirstCriteriaType was CREATURE_TYPE_DOES_NOT_HAVE_SPELL_EFFECT or CREATURE_TYPE_HAS_SPELL_EFFECT, TRUE or FALSE if nFirstCriteriaType was CREATURE_TYPE_IS_ALIVE, PERCEPTION_* if nFirstCriteriaType was CREATURE_TYPE_PERCEPTION, PLAYER_CHAR_IS_PC or PLAYER_CHAR_NOT_PC if nFirstCriteriaType was CREATURE_TYPE_PLAYER_CHAR, RACIAL_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_RACIAL_TYPE, REPUTATION_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_REPUTATION. For example, to get the nearest PC, use (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC).</remarks>
        public static uint GetNearestCreatureToLocation(CreatureType nFirstCriteriaType, bool nFirstCriteriaValue,
            Location lLocation, int nNth = 1, int nSecondCriteriaType = -1, int nSecondCriteriaValue = -1,
            int nThirdCriteriaType = -1, int nThirdCriteriaValue = -1) => _service.GetNearestCreatureToLocation(nFirstCriteriaType, nFirstCriteriaValue, lLocation, nNth, nSecondCriteriaType, nSecondCriteriaValue, nThirdCriteriaType, nThirdCriteriaValue);

        /// <summary>
        /// Gets the level at which the creature cast its last spell (or spell-like ability).
        /// </summary>
        /// <param name="oCreature">The creature to get the caster level for</param>
        /// <returns>The caster level. Returns 0 on error, or if the creature has not yet cast a spell</returns>
        public static int GetCasterLevel(uint oCreature) => _service.GetCasterLevel(oCreature);

        /// <summary>
        /// Gets the racial type of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the racial type for</param>
        /// <returns>The racial type (RACIAL_TYPE_* constants). Returns RACIAL_TYPE_INVALID if the creature is not valid</returns>
        public static RacialType GetRacialType(uint oCreature) => _service.GetRacialType(oCreature);

        /// <summary>
        /// Gets the creature nearest to the specified target, subject to all the criteria specified.
        /// </summary>
        /// <param name="nFirstCriteriaType">The first criteria type (CREATURE_TYPE_* constants)</param>
        /// <param name="nFirstCriteriaValue">The first criteria value</param>
        /// <param name="oTarget">The target to find the nearest creature to (default: OBJECT_SELF)</param>
        /// <param name="nNth">The Nth nearest creature to find (default: 1)</param>
        /// <param name="nSecondCriteriaType">The second criteria type (default: -1)</param>
        /// <param name="nSecondCriteriaValue">The second criteria value (default: -1)</param>
        /// <param name="nThirdCriteriaType">The third criteria type (default: -1)</param>
        /// <param name="nThirdCriteriaValue">The third criteria value (default: -1)</param>
        /// <returns>The nearest creature. Returns OBJECT_INVALID on error</returns>
        /// <remarks>Criteria values: CLASS_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_CLASS, SPELL_* if nFirstCriteriaType was CREATURE_TYPE_DOES_NOT_HAVE_SPELL_EFFECT or CREATURE_TYPE_HAS_SPELL_EFFECT, TRUE or FALSE if nFirstCriteriaType was CREATURE_TYPE_IS_ALIVE, PERCEPTION_* if nFirstCriteriaType was CREATURE_TYPE_PERCEPTION, PLAYER_CHAR_IS_PC or PLAYER_CHAR_NOT_PC if nFirstCriteriaType was CREATURE_TYPE_PLAYER_CHAR, RACIAL_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_RACIAL_TYPE, REPUTATION_TYPE_* if nFirstCriteriaType was CREATURE_TYPE_REPUTATION. For example, to get the nearest PC, use: (CREATURE_TYPE_PLAYER_CHAR, PLAYER_CHAR_IS_PC).</remarks>
        public static uint GetNearestCreature(CreatureType nFirstCriteriaType, int nFirstCriteriaValue,
            uint oTarget = NWScriptService.OBJECT_INVALID, int nNth = 1, int nSecondCriteriaType = -1, int nSecondCriteriaValue = -1,
            int nThirdCriteriaType = -1, int nThirdCriteriaValue = -1) => _service.GetNearestCreature(nFirstCriteriaType, nFirstCriteriaValue, oTarget, nNth, nSecondCriteriaType, nSecondCriteriaValue, nThirdCriteriaType, nThirdCriteriaValue);

        /// <summary>
        /// Gets the ability score of the specified type for a creature.
        /// </summary>
        /// <param name="oCreature">The creature whose ability score to find out</param>
        /// <param name="nAbilityType">The ability type (ABILITY_* constants)</param>
        /// <param name="nBaseAbilityScore">If set to true, will return the base ability score without bonuses (e.g., ability bonuses granted from equipped items) (default: false)</param>
        /// <returns>The ability score. Returns 0 on error</returns>
        public static int GetAbilityScore(uint oCreature, AbilityType nAbilityType, bool nBaseAbilityScore = false) => _service.GetAbilityScore(oCreature, nAbilityType, nBaseAbilityScore);

        /// <summary>
        /// Returns true if the creature is a dead NPC, dead PC or a dying PC.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>True if the creature is dead or dying, false otherwise</returns>
        public static bool GetIsDead(uint oCreature) => _service.GetIsDead(oCreature);

        /// <summary>
        /// Gets the number of hit dice for the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the hit dice for</param>
        /// <returns>The number of hit dice. Returns 0 if the creature is not valid</returns>
        public static int GetHitDice(uint oCreature) => _service.GetHitDice(oCreature);

        /// <summary>
        /// Gets the creature that is going to attack the specified target.
        /// </summary>
        /// <param name="oTarget">The target to check</param>
        /// <returns>The creature that is going to attack the target. Returns OBJECT_INVALID if the target is not a valid creature</returns>
        /// <remarks>This value is cleared out at the end of every combat round and should not be used in any case except when getting a "going to be attacked" shout from the master creature (and this creature is a henchman).</remarks>
        public static uint GetGoingToBeAttackedBy(uint oTarget) => _service.GetGoingToBeAttackedBy(oTarget);

        /// <summary>
        /// Returns true if the creature is a Player Controlled character.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>True if the creature is a PC, false otherwise</returns>
        public static bool GetIsPC(uint oCreature) => _service.GetIsPC(oCreature);

        /// <summary>
        /// Returns true if the creature has immunity of the specified type.
        /// </summary>
        /// <param name="oCreature">The creature to check immunity for</param>
        /// <param name="nImmunityType">The immunity type (IMMUNITY_TYPE_* constants)</param>
        /// <param name="oVersus">If specified, also checks for the race and alignment of this target (default: OBJECT_INVALID)</param>
        /// <returns>True if the creature has immunity of the specified type</returns>
        public static bool GetIsImmune(uint oCreature, ImmunityType nImmunityType, uint oVersus = NWScriptService.OBJECT_INVALID) => _service.GetIsImmune(oCreature, nImmunityType, oVersus);

        /// <summary>
        /// Determines whether the creature has the specified feat and it is usable.
        /// </summary>
        /// <param name="nFeat">The feat to check (FEAT_* constants)</param>
        /// <param name="oCreature">The creature to check the feat for (default: OBJECT_SELF)</param>
        /// <returns>True if the creature has the feat and it is usable, false otherwise</returns>
        public static bool GetHasFeat(FeatType nFeat, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetHasFeat(nFeat, oCreature);

        /// <summary>
        /// Determines whether the creature has the specified skill and it is usable.
        /// </summary>
        /// <param name="nSkill">The skill to check (SKILL_* constants)</param>
        /// <param name="oCreature">The creature to check the skill for (default: OBJECT_SELF)</param>
        /// <returns>True if the creature has the skill and it is usable, false otherwise</returns>
        public static bool GetHasSkill(NWNSkillType nSkill, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetHasSkill(nSkill, oCreature);

        /// <summary>
        /// Determines whether the source sees the target.
        /// </summary>
        /// <param name="oTarget">The target to check visibility for</param>
        /// <param name="oSource">The source to check visibility from (default: OBJECT_SELF)</param>
        /// <returns>True if the source sees the target, false otherwise</returns>
        /// <remarks>This only works on creatures, as visibility lists are not maintained for non-creature objects.</remarks>
        public static bool GetObjectSeen(uint oTarget, uint oSource = NWScriptService.OBJECT_INVALID) => _service.GetObjectSeen(oTarget, oSource);

        /// <summary>
        /// Determines whether the source hears the target.
        /// </summary>
        /// <param name="oTarget">The target to check hearing for</param>
        /// <param name="oSource">The source to check hearing from (default: OBJECT_SELF)</param>
        /// <returns>True if the source hears the target, false otherwise</returns>
        /// <remarks>This only works on creatures, as visibility lists are not maintained for non-creature objects.</remarks>
        public static bool GetObjectHeard(uint oTarget, uint oSource = NWScriptService.OBJECT_INVALID) => _service.GetObjectHeard(oTarget, oSource);

        /// <summary>
        /// Returns true if the creature is of a playable racial type.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>True if the creature is of a playable racial type, false otherwise</returns>
        public static bool GetIsPlayableRacialType(uint oCreature) => _service.GetIsPlayableRacialType(oCreature);

        /// <summary>
        /// Gets the number of ranks that the target has in the specified skill.
        /// </summary>
        /// <param name="nSkill">The skill to check (SKILL_* constants)</param>
        /// <param name="oTarget">The target to get the skill rank for (default: OBJECT_SELF)</param>
        /// <param name="nBaseSkillRank">If set to true, returns the number of base skill ranks the target has (i.e., not including any bonuses from ability scores, feats, etc.) (default: false)</param>
        /// <returns>The number of skill ranks. Returns -1 if the target doesn't have the skill, 0 if the skill is untrained</returns>
        public static int GetSkillRank(NWNSkillType nSkill, uint oTarget = NWScriptService.OBJECT_INVALID, bool nBaseSkillRank = false) => _service.GetSkillRank(nSkill, oTarget, nBaseSkillRank);

        /// <summary>
        /// Gets the attack target of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the attack target for (default: OBJECT_SELF)</param>
        /// <returns>The attack target of the creature</returns>
        /// <remarks>This only works when the creature is in combat.</remarks>
        public static uint GetAttackTarget(uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetAttackTarget(oCreature);

        /// <summary>
        /// Gets the attack type of the creature's last attack.
        /// </summary>
        /// <param name="oCreature">The creature to get the last attack type for (default: OBJECT_SELF)</param>
        /// <returns>The attack type (SPECIAL_ATTACK_* constants)</returns>
        /// <remarks>This only works when the creature is in combat.</remarks>
        public static SpecialAttackType GetLastAttackType(uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetLastAttackType(oCreature);

        /// <summary>
        /// Sets the gender of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to set the gender for</param>
        /// <param name="nGender">The gender to set (GENDER_* constants)</param>
        public static void SetGender(uint oCreature, GenderType nGender) => _service.SetGender(oCreature, nGender);

        /// <summary>
        /// Gets the soundset of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the soundset for</param>
        /// <returns>The soundset. Returns -1 on error</returns>
        public static int GetSoundset(uint oCreature) => _service.GetSoundset(oCreature);

        /// <summary>
        /// Sets the soundset of the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to set the soundset for</param>
        /// <param name="nSoundset">The soundset to set (see soundset.2da for possible values)</param>
        public static void SetSoundset(uint oCreature, int nSoundset) => _service.SetSoundset(oCreature, nSoundset);

        /// <summary>
        /// Readies a spell level for the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to ready the spell level for</param>
        /// <param name="nSpellLevel">The spell level to ready (0-9)</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant or CLASS_TYPE_INVALID to ready the spell level for all classes (default: ClassType.Invalid)</param>
        public static void ReadySpellLevel(uint oCreature, int nSpellLevel, ClassType nClassType = ClassType.Invalid) => _service.ReadySpellLevel(oCreature, nSpellLevel, nClassType);

        /// <summary>
        /// Makes the creature controllable by the specified player, if player party control is enabled.
        /// </summary>
        /// <param name="oCreature">The creature to set the commanding player for</param>
        /// <param name="oPlayer">The player to make the creature controllable by. Setting to OBJECT_INVALID removes the override and reverts to regular party control behavior</param>
        /// <remarks>A creature is only controllable by one player, so if you set oPlayer to a non-Player object (e.g., the module) it will disable regular party control for this creature.</remarks>
        public static void SetCommandingPlayer(uint oCreature, uint oPlayer) => _service.SetCommandingPlayer(oCreature, oPlayer);

        /// <summary>
        /// Gets the current discoverability mask of the specified object.
        /// </summary>
        /// <param name="oObject">The object to get the discovery mask for</param>
        /// <returns>The discoverability mask. Returns -1 if the object cannot have a discovery mask</returns>
        public static int GetObjectUiDiscoveryMask(uint oObject) => _service.GetObjectUiDiscoveryMask(oObject);

        /// <summary>
        /// Sets the discoverability mask on the specified object.
        /// </summary>
        /// <param name="oObject">The object to set the discovery mask for</param>
        /// <param name="nMask">A mask of OBJECT_UI_DISCOVERY_MODE_* constants (default: ObjectUIDiscoveryType.Default)</param>
        /// <remarks>This allows toggling areahilite (TAB key by default) and mouseover discovery in the area view. Will currently only work on Creatures, Doors (Hilite only), Items and Useable Placeables. Does not affect inventory items.</remarks>
        public static void SetObjectUiDiscoveryMask(uint oObject, ObjectUIDiscoveryType nMask = ObjectUIDiscoveryType.Default) => _service.SetObjectUiDiscoveryMask(oObject, nMask);

        /// <summary>
        /// Sets a text override for the mouseover/tab-highlight text bubble of the specified object.
        /// </summary>
        /// <param name="oObject">The object to set the text bubble override for</param>
        /// <param name="nMode">One of OBJECT_UI_TEXT_BUBBLE_OVERRIDE_* constants</param>
        /// <param name="sText">The text to display in the bubble</param>
        /// <remarks>Will currently only work on Creatures, Items and Useable Placeables.</remarks>
        public static void SetObjectTextBubbleOverride(uint oObject, ObjectUITextBubbleOverrideType nMode, string sText) => _service.SetObjectTextBubbleOverride(oObject, nMode, sText);

        /// <summary>
        /// Returns the string tag set for the provided effect.
        /// </summary>
        /// <param name="eEffect">The effect to get the tag for</param>
        /// <returns>The string tag. Returns an empty string if no tag has been set</returns>
        public static string GetEffectTag(Effect eEffect) => _service.GetEffectTag(eEffect);

        /// <summary>
        /// Tags the effect with the provided string.
        /// </summary>
        /// <param name="eEffect">The effect to tag</param>
        /// <param name="sNewTag">The new tag to set</param>
        /// <returns>The tagged effect</returns>
        /// <remarks>Any other tags in the link will be overwritten.</remarks>
        public static Effect TagEffect(Effect eEffect, string sNewTag) => _service.TagEffect(eEffect, sNewTag);

        /// <summary>
        /// Returns the caster level of the creature who created the effect.
        /// </summary>
        /// <param name="eEffect">The effect to get the caster level for</param>
        /// <returns>The caster level. Returns 0 if not created by a creature or if created by a spell-like ability</returns>
        public static int GetEffectCasterLevel(Effect eEffect) => _service.GetEffectCasterLevel(eEffect);

        /// <summary>
        /// Returns the total duration of the effect in seconds.
        /// </summary>
        /// <param name="eEffect">The effect to get the duration for</param>
        /// <returns>The total duration in seconds. Returns 0 if the duration type of the effect is not DURATION_TYPE_TEMPORARY</returns>
        public static int GetEffectDuration(Effect eEffect) => _service.GetEffectDuration(eEffect);

        /// <summary>
        /// Returns the remaining duration of the effect in seconds.
        /// </summary>
        /// <param name="eEffect">The effect to get the remaining duration for</param>
        /// <returns>The remaining duration in seconds. Returns 0 if the duration type of the effect is not DURATION_TYPE_TEMPORARY</returns>
        public static int GetEffectDurationRemaining(Effect eEffect) => _service.GetEffectDurationRemaining(eEffect);

        /// <summary>
        /// Returns an effect that when applied will paralyze the target's legs, rendering them unable to walk but otherwise unpenalized.
        /// </summary>
        /// <returns>The cutscene immobilize effect</returns>
        /// <remarks>This effect cannot be resisted.</remarks>
        public static Effect EffectCutsceneImmobilize() => _service.EffectCutsceneImmobilize();

        /// <summary>
        /// Creates a cutscene ghost effect.
        /// </summary>
        /// <returns>The cutscene ghost effect</returns>
        /// <remarks>This will allow creatures to pathfind through other creatures without bumping into them for the duration of the effect.</remarks>
        public static Effect EffectCutsceneGhost() => _service.EffectCutsceneGhost();

        /// <summary>
        /// Returns true if the item is cursed and cannot be dropped.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <returns>True if the item is cursed, false otherwise</returns>
        public static bool GetItemCursedFlag(uint oItem) => _service.GetItemCursedFlag(oItem);

        /// <summary>
        /// Sets the cursed flag on the specified item.
        /// </summary>
        /// <param name="oItem">The item to set the cursed flag for</param>
        /// <param name="nCursed">Whether the item is cursed</param>
        /// <remarks>When cursed, items cannot be dropped.</remarks>
        public static void SetItemCursedFlag(uint oItem, bool nCursed) => _service.SetItemCursedFlag(oItem, nCursed);

        /// <summary>
        /// Gets the possessor of the specified item.
        /// </summary>
        /// <param name="oItem">The item to get the possessor for</param>
        /// <returns>The possessor of the item. Returns OBJECT_INVALID on error</returns>
        public static uint GetItemPossessor(uint oItem) => _service.GetItemPossessor(oItem);

        /// <summary>
        /// Gets the object possessed by the creature with the specified tag.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="sItemTag">The item tag to search for</param>
        /// <returns>The possessed object. Returns OBJECT_INVALID on error</returns>
        public static uint GetItemPossessedBy(uint oCreature, string sItemTag) => _service.GetItemPossessedBy(oCreature, sItemTag);

        /// <summary>
        /// Creates an item with the specified template in the target's inventory.
        /// </summary>
        /// <param name="sResRef">The item template to create</param>
        /// <param name="oTarget">The target to create the item for (default: OBJECT_SELF)</param>
        /// <param name="nStackSize">The stack size of the item to be created (default: 1)</param>
        /// <param name="sNewTag">If this string is not empty, it will replace the default tag from the template (default: empty string)</param>
        /// <returns>The object that has been created. Returns OBJECT_INVALID on error. If the item created was merged into an existing stack of similar items, the function will return the merged stack object. If the merged stack overflowed, the function will return the overflowed stack that was created</returns>
        public static uint CreateItemOnObject(string sResRef, uint oTarget = NWScriptService.OBJECT_INVALID, int nStackSize = 1,
            string sNewTag = "") => _service.CreateItemOnObject(sResRef, oTarget, nStackSize, sNewTag);

        /// <summary>
        /// Equips the specified item into the specified inventory slot.
        /// </summary>
        /// <param name="oItem">The item to equip</param>
        /// <param name="nInventorySlot">The inventory slot to equip the item to (INVENTORY_SLOT_* constants)</param>
        /// <remarks>If an error occurs, the log file will contain "ActionEquipItem failed." If the creature already has an item equipped in the slot specified, it will be unequipped automatically by the call to ActionEquipItem. In order for ActionEquipItem to succeed the creature must be able to equip the item normally. This means that: 1) The item is in the creature's inventory, 2) The item must already be identified (if magical), 3) The creature has the level required to equip the item (if magical and ILR is on), 4) The creature possesses the required feats to equip the item (such as weapon proficiencies).</remarks>
        public static void ActionEquipItem(uint oItem, InventorySlotType nInventorySlot) => _service.ActionEquipItem(oItem, nInventorySlot);

        /// <summary>
        /// Unequips the specified item from whatever slot it is currently in.
        /// </summary>
        /// <param name="oItem">The item to unequip</param>
        public static void ActionUnequipItem(uint oItem) => _service.ActionUnequipItem(oItem);

        /// <summary>
        /// Picks up the specified item from the ground.
        /// </summary>
        /// <param name="oItem">The item to pick up</param>
        /// <remarks>If an error occurs, the log file will contain "ActionPickUpItem failed."</remarks>
        public static void ActionPickUpItem(uint oItem) => _service.ActionPickUpItem(oItem);

        /// <summary>
        /// Puts down the specified item on the ground.
        /// </summary>
        /// <param name="oItem">The item to put down</param>
        /// <remarks>If an error occurs, the log file will contain "ActionPutDownItem failed."</remarks>
        public static void ActionPutDownItem(uint oItem) => _service.ActionPutDownItem(oItem);

        /// <summary>
        /// Gives the specified item to the specified target.
        /// </summary>
        /// <param name="oItem">The item to give</param>
        /// <param name="oGiveTo">The target to give the item to</param>
        /// <remarks>If oItem is not a valid item, or oGiveTo is not a valid object, nothing will happen.</remarks>
        public static void ActionGiveItem(uint oItem, uint oGiveTo) => _service.ActionGiveItem(oItem, oGiveTo);

        /// <summary>
        /// Takes the specified item from the specified source.
        /// </summary>
        /// <param name="oItem">The item to take</param>
        /// <param name="oTakeFrom">The source to take the item from</param>
        /// <remarks>If oItem is not a valid item, or oTakeFrom is not a valid object, nothing will happen.</remarks>
        public static void ActionTakeItem(uint oItem, uint oTakeFrom) => _service.ActionTakeItem(oItem, oTakeFrom);

        /// <summary>
        /// Creates a Death effect.
        /// </summary>
        /// <param name="nSpectacularDeath">If true, the creature to which this effect is applied will die in an extraordinary fashion (default: false)</param>
        /// <param name="nDisplayFeedback">Whether to display feedback (default: true)</param>
        /// <returns>The Death effect</returns>
        public static Effect EffectDeath(bool nSpectacularDeath = false, bool nDisplayFeedback = true) => _service.EffectDeath(nSpectacularDeath, nDisplayFeedback);

        /// <summary>
        /// Creates a Knockdown effect.
        /// </summary>
        /// <returns>The Knockdown effect</returns>
        /// <remarks>This effect knocks creatures off their feet, they will sit until the effect is removed. This should be applied as a temporary effect with a 3 second duration minimum (1 second to fall, 1 second sitting, 1 second to get up).</remarks>
        public static Effect EffectKnockdown() => _service.EffectKnockdown();

        /// <summary>
        /// Creates a Curse effect.
        /// </summary>
        /// <param name="nStrMod">Strength modifier (default: 1)</param>
        /// <param name="nDexMod">Dexterity modifier (default: 1)</param>
        /// <param name="nConMod">Constitution modifier (default: 1)</param>
        /// <param name="nIntMod">Intelligence modifier (default: 1)</param>
        /// <param name="nWisMod">Wisdom modifier (default: 1)</param>
        /// <param name="nChaMod">Charisma modifier (default: 1)</param>
        /// <returns>The Curse effect</returns>
        public static Effect EffectCurse(int nStrMod = 1, int nDexMod = 1, int nConMod = 1, int nIntMod = 1,
            int nWisMod = 1, int nChaMod = 1) => _service.EffectCurse(nStrMod, nDexMod, nConMod, nIntMod, nWisMod, nChaMod);

        /// <summary>
        /// Creates an Entangle effect.
        /// </summary>
        /// <returns>The Entangle effect</returns>
        /// <remarks>When applied, this effect will restrict the creature's movement and apply a (-2) to all attacks and a -4 to AC.</remarks>
        public static Effect EffectEntangle() => _service.EffectEntangle();

        /// <summary>
        /// Creates a Saving Throw Increase effect.
        /// </summary>
        /// <param name="nSave">The saving throw type (SAVING_THROW_* constants, not SAVING_THROW_TYPE_*)</param>
        /// <param name="nValue">The size of the saving throw increase</param>
        /// <param name="nSaveType">The saving throw type (SAVING_THROW_TYPE_* constants, e.g., SAVING_THROW_TYPE_ACID) (default: SavingThrowType.All)</param>
        /// <returns>The Saving Throw Increase effect</returns>
        /// <remarks>Possible save types: SAVING_THROW_ALL, SAVING_THROW_FORT, SAVING_THROW_REFLEX, SAVING_THROW_WILL</remarks>
        public static Effect EffectSavingThrowIncrease(int nSave, int nValue,
            SavingThrowType nSaveType = SavingThrowType.All) => _service.EffectSavingThrowIncrease(nSave, nValue, nSaveType);

        /// <summary>
        /// Creates an Attack Increase effect.
        /// </summary>
        /// <param name="nBonus">The size of attack bonus</param>
        /// <param name="nModifierType">The attack bonus type (ATTACK_BONUS_* constants) (default: AttackBonus.Misc)</param>
        /// <returns>The Attack Increase effect</returns>
        /// <remarks>On SWLOR, this is used for Accuracy.</remarks>
        public static Effect EffectAccuracyIncrease(int nBonus, AttackBonusType nModifierType = AttackBonusType.Misc) => _service.EffectAccuracyIncrease(nBonus, nModifierType);

        /// <summary>
        /// Creates a Damage Reduction effect.
        /// </summary>
        /// <param name="nAmount">The amount of damage reduction</param>
        /// <param name="nDamagePower">The damage power type (DAMAGE_POWER_* constants)</param>
        /// <param name="nLimit">How much damage the effect can absorb before disappearing. Set to zero for infinite (default: 0)</param>
        /// <returns>The Damage Reduction effect</returns>
        public static Effect EffectDamageReduction(int nAmount, DamagePowerType nDamagePower, int nLimit = 0) => _service.EffectDamageReduction(nAmount, nDamagePower, nLimit);

        /// <summary>
        /// Creates a Damage Increase effect.
        /// </summary>
        /// <param name="nBonus">The damage bonus (DAMAGE_BONUS_* constants)</param>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants) (default: DamageType.Force)</param>
        /// <returns>The Damage Increase effect</returns>
        /// <remarks>You must use the DAMAGE_BONUS_* constants! Using other values may result in odd behavior.</remarks>
        public static Effect EffectDamageIncrease(int nBonus, DamageType nDamageType = DamageType.Force) => _service.EffectDamageIncrease(nBonus, nDamageType);

        /// <summary>
        /// Sets the subtype of the effect to Magical and returns the effect.
        /// </summary>
        /// <param name="eEffect">The effect to set as magical</param>
        /// <returns>The magical effect</returns>
        /// <remarks>Effects default to magical if the subtype is not set. Magical effects are removed by resting, and by dispel magic.</remarks>
        public static Effect MagicalEffect(Effect eEffect) => _service.MagicalEffect(eEffect);

        /// <summary>
        /// Sets the subtype of the effect to Supernatural and returns the effect.
        /// </summary>
        /// <param name="eEffect">The effect to set as supernatural</param>
        /// <returns>The supernatural effect</returns>
        /// <remarks>Effects default to magical if the subtype is not set. Permanent supernatural effects are not removed by resting.</remarks>
        public static Effect SupernaturalEffect(Effect eEffect) => _service.SupernaturalEffect(eEffect);

        /// <summary>
        /// Sets the subtype of the effect to Extraordinary and returns the effect.
        /// </summary>
        /// <param name="eEffect">The effect to set as extraordinary</param>
        /// <returns>The extraordinary effect</returns>
        /// <remarks>Effects default to magical if the subtype is not set. Extraordinary effects are removed by resting, but not by dispel magic.</remarks>
        public static Effect ExtraordinaryEffect(Effect eEffect) => _service.ExtraordinaryEffect(eEffect);

        /// <summary>
        /// Creates an AC Increase effect.
        /// </summary>
        /// <param name="nValue">The size of AC increase</param>
        /// <param name="nModifyType">The armor class modifier type (AC_*_BONUS constants) (default: ArmorClassModiferType.Dodge)</param>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants) (default: AC.VsDamageTypeAll)</param>
        /// <returns>The AC Increase effect</returns>
        /// <remarks>Default value for nDamageType should only ever be used in this function prototype.</remarks>
        public static Effect EffectACIncrease(int nValue,
            ItemPropertyArmorClassModiferType nModifyType = ItemPropertyArmorClassModiferType.Dodge,
            ACType nDamageType = ACType.VsDamageTypeAll) => _service.EffectACIncrease(nValue, nModifyType, nDamageType);

        /// <summary>
        /// Gets the first in-game effect on the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the first effect for</param>
        /// <returns>The first effect on the creature</returns>
        public static Effect GetFirstEffect(uint oCreature) => _service.GetFirstEffect(oCreature);

        /// <summary>
        /// Gets the next in-game effect on the specified creature.
        /// </summary>
        /// <param name="oCreature">The creature to get the next effect for</param>
        /// <returns>The next effect on the creature</returns>
        public static Effect GetNextEffect(uint oCreature) => _service.GetNextEffect(oCreature);

        /// <summary>
        /// Removes the specified effect from the creature.
        /// </summary>
        /// <param name="oCreature">The creature to remove the effect from</param>
        /// <param name="eEffect">The effect to remove</param>
        public static void RemoveEffect(uint oCreature, Effect eEffect) => _service.RemoveEffect(oCreature, eEffect);

        /// <summary>
        /// Returns true if the effect is a valid effect.
        /// </summary>
        /// <param name="eEffect">The effect to check</param>
        /// <returns>True if the effect is valid, false otherwise</returns>
        /// <remarks>The effect must have been applied to an object or else it will return false.</remarks>
        public static bool GetIsEffectValid(Effect eEffect) => _service.GetIsEffectValid(eEffect);

        /// <summary>
        /// Gets the duration type of the specified effect.
        /// </summary>
        /// <param name="eEffect">The effect to get the duration type for</param>
        /// <returns>The duration type (DURATION_TYPE_* constants). Returns -1 if the effect is not valid</returns>
        public static int GetEffectDurationType(Effect eEffect) => _service.GetEffectDurationType(eEffect);

        /// <summary>
        /// Gets the subtype of the specified effect.
        /// </summary>
        /// <param name="eEffect">The effect to get the subtype for</param>
        /// <returns>The subtype (SUBTYPE_* constants). Returns 0 on error</returns>
        public static int GetEffectSubType(Effect eEffect) => _service.GetEffectSubType(eEffect);

        /// <summary>
        /// Gets the object that created the specified effect.
        /// </summary>
        /// <param name="eEffect">The effect to get the creator for</param>
        /// <returns>The object that created the effect. Returns OBJECT_INVALID if the effect is not valid</returns>
        public static uint GetEffectCreator(Effect eEffect) => _service.GetEffectCreator(eEffect);

        /// <summary>
        /// Creates a Heal effect.
        /// </summary>
        /// <param name="nDamageToHeal">The amount of damage to heal</param>
        /// <returns>The Heal effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDamageToHeal < 0</returns>
        /// <remarks>This should be applied as an instantaneous effect.</remarks>
        public static Effect EffectHeal(int nDamageToHeal) => _service.EffectHeal(nDamageToHeal);

        /// <summary>
        /// Creates a Damage effect.
        /// </summary>
        /// <param name="nDamageAmount">The amount of damage to be dealt</param>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants) (default: DamageType.Force)</param>
        /// <param name="nDamagePower">The damage power (DAMAGE_POWER_* constants) (default: DamagePower.Normal)</param>
        /// <returns>The Damage effect</returns>
        /// <remarks>This should be applied as an instantaneous effect.</remarks>
        public static Effect EffectDamage(int nDamageAmount, DamageType nDamageType = DamageType.Force,
            DamagePowerType nDamagePower = DamagePowerType.Normal) => _service.EffectDamage(nDamageAmount, nDamageType, nDamagePower);

        /// <summary>
        /// Creates an Ability Increase effect.
        /// </summary>
        /// <param name="nAbilityToIncrease">The ability to increase (ABILITY_* constants)</param>
        /// <param name="nModifyBy">The amount to modify the ability by</param>
        /// <returns>The Ability Increase effect</returns>
        public static Effect EffectAbilityIncrease(AbilityType nAbilityToIncrease, int nModifyBy) => _service.EffectAbilityIncrease(nAbilityToIncrease, nModifyBy);

        /// <summary>
        /// Creates a Damage Resistance effect that removes the first nAmount points of damage of the specified type.
        /// </summary>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants)</param>
        /// <param name="nAmount">The amount of damage resistance</param>
        /// <param name="nLimit">The limit of damage resistance (infinite if 0) (default: 0)</param>
        /// <returns>The Damage Resistance effect</returns>
        public static Effect EffectDamageResistance(DamageType nDamageType, int nAmount, int nLimit = 0) => _service.EffectDamageResistance(nDamageType, nAmount, nLimit);

        /// <summary>
        /// Creates a Resurrection effect.
        /// </summary>
        /// <returns>The Resurrection effect</returns>
        /// <remarks>This should be applied as an instantaneous effect.</remarks>
        public static Effect EffectResurrection() => _service.EffectResurrection();

        /// <summary>
        /// Creates a Summon Creature effect.
        /// </summary>
        /// <param name="sCreatureResref">The creature resource reference to summon</param>
        /// <param name="nVisualEffectId">The visual effect ID (VFX_* constants) (default: VisualEffect.Vfx_Com_Sparks_Parry)</param>
        /// <param name="fDelaySeconds">Delay between the visual effect being played and the creature being added to the area (default: 0.0f)</param>
        /// <param name="nUseAppearAnimation">Whether the creature should play its "appear" animation when summoned (default: false)</param>
        /// <param name="nUnsummonVisualEffectId">The visual effect ID for unsummoning (default: VisualEffect.Vfx_Imp_Unsummon)</param>
        /// <param name="oSummonToAdd">The object to add the summoned creature to (default: OBJECT_INVALID)</param>
        /// <returns>The Summon Creature effect</returns>
        /// <remarks>The creature is created and placed into the caller's party/faction. If nUseAppearAnimation is zero, it will just fade in somewhere near the target. If the value is 1 it will use the appear animation, and if it's 2 it will use appear2 (which doesn't exist for most creatures).</remarks>
        public static Effect EffectSummonCreature(string sCreatureResref, VisualEffectType nVisualEffectId = VisualEffectType.Vfx_Com_Sparks_Parry,
            float fDelaySeconds = 0.0f, bool nUseAppearAnimation = false, VisualEffectType nUnsummonVisualEffectId = VisualEffectType.Vfx_Imp_Unsummon, uint oSummonToAdd = NWScriptService.OBJECT_INVALID) => _service.EffectSummonCreature(sCreatureResref, nVisualEffectId, fDelaySeconds, nUseAppearAnimation, nUnsummonVisualEffectId, oSummonToAdd);

        /// <summary>
        /// Returns an effect of type EFFECT_TYPE_ETHEREAL.
        /// </summary>
        /// <returns>The Ethereal effect</returns>
        /// <remarks>Works just like EffectSanctuary except that the observers get no saving throw.</remarks>
        public static Effect EffectEthereal() => _service.EffectEthereal();

        /// <summary>
        /// Creates an effect that inhibits spells.
        /// </summary>
        /// <param name="nPercent">The percentage of failure (default: 100)</param>
        /// <param name="nSpellSchool">The school of spells affected (default: SpellSchool.General)</param>
        /// <returns>The Spell Failure effect</returns>
        public static Effect EffectSpellFailure(int nPercent = 100,
            SpellSchool nSpellSchool = SpellSchool.General) => _service.EffectSpellFailure(nPercent, nSpellSchool);

        /// <summary>
        /// Returns an effect that is guaranteed to dominate a creature.
        /// </summary>
        /// <returns>The Cutscene Dominated effect</returns>
        /// <remarks>Like EffectDominated but cannot be resisted.</remarks>
        public static Effect EffectCutsceneDominated() => _service.EffectCutsceneDominated();

        /// <summary>
        /// Returns an effect that will petrify the target.
        /// </summary>
        /// <returns>The Petrify effect</returns>
        /// <remarks>Currently applies EffectParalyze and the stoneskin visual effect.</remarks>
        public static Effect EffectPetrify() => _service.EffectPetrify();

        /// <summary>
        /// Returns an effect that is guaranteed to paralyze a creature.
        /// </summary>
        /// <returns>The Cutscene Paralyze effect</returns>
        /// <remarks>This effect is identical to EffectParalyze except that it cannot be resisted.</remarks>
        public static Effect EffectCutsceneParalyze() => _service.EffectCutsceneParalyze();

        /// <summary>
        /// Creates a Turn Resistance Decrease effect.
        /// </summary>
        /// <param name="nHitDice">A positive number representing the number of hit dice for the decrease</param>
        /// <returns>The Turn Resistance Decrease effect</returns>
        public static Effect EffectTurnResistanceDecrease(int nHitDice) => _service.EffectTurnResistanceDecrease(nHitDice);

        /// <summary>
        /// Creates a Turn Resistance Increase effect.
        /// </summary>
        /// <param name="nHitDice">A positive number representing the number of hit dice for the increase</param>
        /// <returns>The Turn Resistance Increase effect</returns>
        public static Effect EffectTurnResistanceIncrease(int nHitDice) => _service.EffectTurnResistanceIncrease(nHitDice);

        /// <summary>
        /// Creates a Swarm effect.
        /// </summary>
        /// <param name="nLooping">If true, for the duration of the effect when one creature created by this effect dies, the next one in the list will be created</param>
        /// <param name="sCreatureTemplate1">The first creature template</param>
        /// <param name="sCreatureTemplate2">The second creature template (default: empty string)</param>
        /// <param name="sCreatureTemplate3">The third creature template (default: empty string)</param>
        /// <param name="sCreatureTemplate4">The fourth creature template (default: empty string)</param>
        /// <returns>The Swarm effect</returns>
        /// <remarks>If the last creature in the list dies, we loop back to the beginning and sCreatureTemplate1 will be created, and so on...</remarks>
        public static Effect EffectSwarm(int nLooping, string sCreatureTemplate1, string sCreatureTemplate2 = "",
            string sCreatureTemplate3 = "", string sCreatureTemplate4 = "") => _service.EffectSwarm(nLooping, sCreatureTemplate1, sCreatureTemplate2, sCreatureTemplate3, sCreatureTemplate4);

        /// <summary>
        /// Creates a Disappear/Appear effect.
        /// </summary>
        /// <param name="lLocation">The location where the object will reappear</param>
        /// <param name="nAnimation">Determines which appear and disappear animations to use (default: 1)</param>
        /// <returns>The Disappear/Appear effect</returns>
        /// <remarks>The object will "fly away" for the duration of the effect and will reappear at the specified location. Most creatures only have animation 1, although a few have 2 (like beholders).</remarks>
        public static Effect EffectDisappearAppear(Location lLocation, int nAnimation = 1) => _service.EffectDisappearAppear(lLocation, nAnimation);

        /// <summary>
        /// Creates a Disappear effect to make the object "fly away" and then destroy itself.
        /// </summary>
        /// <param name="nAnimation">Determines which appear and disappear animations to use (default: 1)</param>
        /// <returns>The Disappear effect</returns>
        /// <remarks>Most creatures only have animation 1, although a few have 2 (like beholders).</remarks>
        public static Effect EffectDisappear(int nAnimation = 1) => _service.EffectDisappear(nAnimation);

        /// <summary>
        /// Creates an Appear effect to make the object "fly in".
        /// </summary>
        /// <param name="nAnimation">Determines which appear and disappear animations to use (default: 1)</param>
        /// <returns>The Appear effect</returns>
        /// <remarks>Most creatures only have animation 1, although a few have 2 (like beholders).</remarks>
        public static Effect EffectAppear(int nAnimation = 1) => _service.EffectAppear(nAnimation);

        /// <summary>
        /// Creates a Modify Attacks effect to add attacks.
        /// </summary>
        /// <param name="nAttacks">The number of attacks to add (maximum is 5, even with the effect stacked)</param>
        /// <returns>The Modify Attacks effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nAttacks > 5</returns>
        public static Effect EffectModifyAttacks(int nAttacks) => _service.EffectModifyAttacks(nAttacks);

        /// <summary>
        /// Creates a Damage Shield effect which does (nDamageAmount + nRandomAmount) damage to any melee attacker on a successful attack.
        /// </summary>
        /// <param name="nDamageAmount">An integer value for the base damage</param>
        /// <param name="nRandomAmount">The random damage bonus (DAMAGE_BONUS_* constants)</param>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants)</param>
        /// <returns>The Damage Shield effect</returns>
        /// <remarks>You must use the DAMAGE_BONUS_* constants! Using other values may result in odd behavior.</remarks>
        public static Effect EffectDamageShield(int nDamageAmount, ItemPropertyDamageBonusType nRandomAmount, DamageType nDamageType) => _service.EffectDamageShield(nDamageAmount, nRandomAmount, nDamageType);

        /// <summary>
        /// Creates a Miss Chance effect.
        /// </summary>
        /// <param name="nPercentage">The miss chance percentage (1-100 inclusive)</param>
        /// <param name="nMissChanceType">The miss chance type (MISS_CHANCE_TYPE_* constants) (default: MissChanceType.Normal)</param>
        /// <returns>The Miss Chance effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nPercentage < 1 or nPercentage > 100</returns>
        public static Effect EffectMissChance(int nPercentage, MissChanceType nMissChanceType = MissChanceType.Normal) => _service.EffectMissChance(nPercentage, nMissChanceType);

        /// <summary>
        /// Creates a Spell Level Absorption effect.
        /// </summary>
        /// <param name="nMaxSpellLevelAbsorbed">The maximum spell level that will be absorbed by the effect</param>
        /// <param name="nTotalSpellLevelsAbsorbed">The maximum number of spell levels that will be absorbed by the effect (default: 0)</param>
        /// <param name="nSpellSchool">The spell school (SPELL_SCHOOL_* constants) (default: SpellSchool.General)</param>
        /// <returns>The Spell Level Absorption effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nMaxSpellLevelAbsorbed is not between -1 and 9 inclusive, or nSpellSchool is invalid</returns>
        public static Effect EffectSpellLevelAbsorption(int nMaxSpellLevelAbsorbed, int nTotalSpellLevelsAbsorbed = 0,
            SpellSchool nSpellSchool = SpellSchool.General) => _service.EffectSpellLevelAbsorption(nMaxSpellLevelAbsorbed, nTotalSpellLevelsAbsorbed, nSpellSchool);

        /// <summary>
        /// Creates a Dispel Magic Best effect.
        /// </summary>
        /// <param name="nCasterLevel">The caster level for the dispel effect (default: USE_CREATURE_LEVEL)</param>
        /// <returns>The Dispel Magic Best effect</returns>
        /// <remarks>If no parameter is specified, USE_CREATURE_LEVEL will be used. This will cause the dispel effect to use the level of the creature that created the effect.</remarks>
        public static Effect EffectDispelMagicBest(int nCasterLevel = NWScriptService.USE_CREATURE_LEVEL) => _service.EffectDispelMagicBest(nCasterLevel);

        /// <summary>
        /// Creates an Invisibility effect.
        /// </summary>
        /// <param name="nInvisibilityType">The invisibility type (INVISIBILITY_TYPE_* constants)</param>
        /// <returns>The Invisibility effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nInvisibilityType is invalid</returns>
        public static Effect EffectInvisibility(InvisibilityType nInvisibilityType) => _service.EffectInvisibility(nInvisibilityType);

        /// <summary>
        /// Creates a Concealment effect.
        /// </summary>
        /// <param name="nPercentage">The concealment percentage (1-100 inclusive)</param>
        /// <param name="nMissType">The miss chance type (MISS_CHANCE_TYPE_* constants) (default: MissChanceType.Normal)</param>
        /// <returns>The Concealment effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nPercentage < 1 or nPercentage > 100</returns>
        public static Effect EffectConcealment(int nPercentage, MissChanceType nMissType = MissChanceType.Normal) => _service.EffectConcealment(nPercentage, nMissType);

        /// <summary>
        /// Creates a Darkness effect.
        /// </summary>
        /// <returns>The Darkness effect</returns>
        public static Effect EffectDarkness() => _service.EffectDarkness();

        /// <summary>
        /// Creates a Dispel Magic All effect.
        /// </summary>
        /// <param name="nCasterLevel">The caster level for the dispel effect (default: USE_CREATURE_LEVEL)</param>
        /// <returns>The Dispel Magic All effect</returns>
        /// <remarks>If no parameter is specified, USE_CREATURE_LEVEL will be used. This will cause the dispel effect to use the level of the creature that created the effect.</remarks>
        public static Effect EffectDispelMagicAll(int nCasterLevel = NWScriptService.USE_CREATURE_LEVEL) => _service.EffectDispelMagicAll(nCasterLevel);

        /// <summary>
        /// Creates an Ultravision effect.
        /// </summary>
        /// <returns>The Ultravision effect</returns>
        public static Effect EffectUltravision() => _service.EffectUltravision();

        /// <summary>
        /// Creates a Negative Level effect.
        /// </summary>
        /// <param name="nNumLevels">The number of negative levels to apply</param>
        /// <param name="bHPBonus">Whether to apply HP bonus (default: false)</param>
        /// <returns>The Negative Level effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nNumLevels > 100</returns>
        public static Effect EffectNegativeLevel(int nNumLevels, bool bHPBonus = false) => _service.EffectNegativeLevel(nNumLevels, bHPBonus);

        /// <summary>
        /// Creates a Polymorph effect.
        /// </summary>
        /// <param name="nPolymorphSelection">The polymorph selection</param>
        /// <param name="nLocked">Whether the polymorph is locked (default: false)</param>
        /// <returns>The Polymorph effect</returns>
        public static Effect EffectPolymorph(int nPolymorphSelection, bool nLocked = false) => _service.EffectPolymorph(nPolymorphSelection, nLocked);

        /// <summary>
        /// Creates a Sanctuary effect.
        /// </summary>
        /// <param name="nDifficultyClass">The difficulty class (must be a non-zero, positive number)</param>
        /// <returns>The Sanctuary effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nDifficultyClass <= 0</returns>
        public static Effect EffectSanctuary(int nDifficultyClass) => _service.EffectSanctuary(nDifficultyClass);

        /// <summary>
        /// Creates a True Seeing effect.
        /// </summary>
        /// <returns>The True Seeing effect</returns>
        public static Effect EffectTrueSeeing() => _service.EffectTrueSeeing();

        /// <summary>
        /// Creates a See Invisible effect.
        /// </summary>
        /// <returns>The See Invisible effect</returns>
        public static Effect EffectSeeInvisible() => _service.EffectSeeInvisible();

        /// <summary>
        /// Creates a Time Stop effect.
        /// </summary>
        /// <returns>The Time Stop effect</returns>
        public static Effect EffectTimeStop() => _service.EffectTimeStop();

        /// <summary>
        /// Creates a Blindness effect.
        /// </summary>
        /// <returns>The Blindness effect</returns>
        public static Effect EffectBlindness() => _service.EffectBlindness();

        /// <summary>
        /// Creates an Ability Decrease effect.
        /// </summary>
        /// <param name="nAbility">The ability to decrease (ABILITY_* constants)</param>
        /// <param name="nModifyBy">The amount by which to decrement the ability</param>
        /// <returns>The Ability Decrease effect</returns>
        public static Effect EffectAbilityDecrease(AbilityType nAbility, int nModifyBy) => _service.EffectAbilityDecrease(nAbility, nModifyBy);

        /// <summary>
        /// Creates an Attack Decrease effect.
        /// </summary>
        /// <param name="nPenalty">The penalty amount</param>
        /// <param name="nModifierType">The attack bonus type (ATTACK_BONUS_* constants) (default: AttackBonus.Misc)</param>
        /// <returns>The Attack Decrease effect</returns>
        /// <remarks>On SWLOR, this is used for Accuracy.</remarks>
        public static Effect EffectAccuracyDecrease(int nPenalty, AttackBonusType nModifierType = AttackBonusType.Misc) => _service.EffectAccuracyDecrease(nPenalty, nModifierType);

        /// <summary>
        /// Creates a Damage Decrease effect.
        /// </summary>
        /// <param name="nPenalty">The penalty amount</param>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants) (default: DamageType.Force)</param>
        /// <returns>The Damage Decrease effect</returns>
        public static Effect EffectDamageDecrease(int nPenalty, DamageType nDamageType = DamageType.Force) => _service.EffectDamageDecrease(nPenalty, nDamageType);

        /// <summary>
        /// Creates a Damage Immunity Decrease effect.
        /// </summary>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants)</param>
        /// <param name="nPercentImmunity">The percentage of immunity to decrease</param>
        /// <returns>The Damage Immunity Decrease effect</returns>
        public static Effect EffectDamageImmunityDecrease(int nDamageType, int nPercentImmunity) => _service.EffectDamageImmunityDecrease(nDamageType, nPercentImmunity);

        /// <summary>
        /// Creates an AC Decrease effect.
        /// </summary>
        /// <param name="nValue">The AC decrease value</param>
        /// <param name="nModifyType">The armor class modifier type (AC_* constants) (default: ArmorClassModiferType.Dodge)</param>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants) (default: AC.VsDamageTypeAll)</param>
        /// <returns>The AC Decrease effect</returns>
        /// <remarks>Default value for nDamageType should only ever be used in this function prototype.</remarks>
        public static Effect EffectACDecrease(int nValue,
            ItemPropertyArmorClassModiferType nModifyType = ItemPropertyArmorClassModiferType.Dodge,
            ACType nDamageType = ACType.VsDamageTypeAll) => _service.EffectACDecrease(nValue, nModifyType, nDamageType);

        /// <summary>
        /// Creates a Movement Speed Decrease effect.
        /// </summary>
        /// <param name="nPercentChange">The percentage change (range 0 through 99)</param>
        /// <returns>The Movement Speed Decrease effect</returns>
        /// <remarks>0 = no change in speed, 50 = 50% slower, 99 = almost immobile</remarks>
        public static Effect EffectMovementSpeedDecrease(int nPercentChange) => _service.EffectMovementSpeedDecrease(nPercentChange);

        /// <summary>
        /// Creates a Saving Throw Decrease effect.
        /// </summary>
        /// <param name="nSave">The saving throw type (SAVING_THROW_* constants, not SAVING_THROW_TYPE_*)</param>
        /// <param name="nValue">The size of the saving throw decrease</param>
        /// <param name="nSaveType">The saving throw type (SAVING_THROW_TYPE_* constants, e.g., SAVING_THROW_TYPE_ACID) (default: SavingThrowType.All)</param>
        /// <returns>The Saving Throw Decrease effect</returns>
        /// <remarks>Possible save types: SAVING_THROW_ALL, SAVING_THROW_FORT, SAVING_THROW_REFLEX, SAVING_THROW_WILL</remarks>
        public static Effect EffectSavingThrowDecrease(int nSave, int nValue,
            SavingThrowType nSaveType = SavingThrowType.All) => _service.EffectSavingThrowDecrease(nSave, nValue, nSaveType);

        /// <summary>
        /// Creates a Skill Decrease effect.
        /// </summary>
        /// <param name="nSkill">The skill to decrease</param>
        /// <param name="nValue">The amount to decrease the skill by</param>
        /// <returns>The Skill Decrease effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid</returns>
        public static Effect EffectSkillDecrease(int nSkill, int nValue) => _service.EffectSkillDecrease(nSkill, nValue);

        /// <summary>
        /// Creates a Spell Resistance Decrease effect.
        /// </summary>
        /// <param name="nValue">The amount to decrease spell resistance by</param>
        /// <returns>The Spell Resistance Decrease effect</returns>
        public static Effect EffectSpellResistanceDecrease(int nValue) => _service.EffectSpellResistanceDecrease(nValue);

        /// <summary>
        /// Activates the specified item.
        /// </summary>
        /// <param name="oItem">The item to activate</param>
        /// <param name="lTarget">The target location</param>
        /// <param name="oTarget">The target object (default: OBJECT_SELF)</param>
        /// <returns>The Activate Item event</returns>
        public static Event EventActivateItem(uint oItem, Location lTarget, uint oTarget = NWScriptService.OBJECT_INVALID) => _service.EventActivateItem(oItem, lTarget, oTarget);

        /// <summary>
        /// Creates a Hit Point Change When Dying effect.
        /// </summary>
        /// <param name="fHitPointChangePerRound">The hit point change per round (can be positive or negative, but not zero)</param>
        /// <returns>The Hit Point Change When Dying effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if fHitPointChangePerRound is 0</returns>
        public static Effect EffectHitPointChangeWhenDying(float fHitPointChangePerRound) => _service.EffectHitPointChangeWhenDying(fHitPointChangePerRound);

        /// <summary>
        /// Creates a Turned effect.
        /// </summary>
        /// <returns>The Turned effect</returns>
        /// <remarks>Turned effects are supernatural by default.</remarks>
        public static Effect EffectTurned() => _service.EffectTurned();

        /// <summary>
        /// Sets the effect to be versus a specific alignment.
        /// </summary>
        /// <param name="eEffect">The effect to modify</param>
        /// <param name="nLawChaos">The law/chaos alignment (ALIGNMENT_LAWFUL/ALIGNMENT_CHAOTIC/ALIGNMENT_ALL) (default: Alignment.All)</param>
        /// <param name="nGoodEvil">The good/evil alignment (ALIGNMENT_GOOD/ALIGNMENT_EVIL/ALIGNMENT_ALL) (default: Alignment.All)</param>
        /// <returns>The modified effect</returns>
        public static Effect VersusAlignmentEffect(Effect eEffect,
            AlignmentType nLawChaos = AlignmentType.All,
            AlignmentType nGoodEvil = AlignmentType.All) => _service.VersusAlignmentEffect(eEffect, nLawChaos, nGoodEvil);

        /// <summary>
        /// Sets the effect to be versus a specific racial type.
        /// </summary>
        /// <param name="eEffect">The effect to modify</param>
        /// <param name="nRacialType">The racial type (RACIAL_TYPE_* constants)</param>
        /// <returns>The modified effect</returns>
        public static Effect VersusRacialTypeEffect(Effect eEffect, RacialType nRacialType) => _service.VersusRacialTypeEffect(eEffect, nRacialType);

        /// <summary>
        /// Sets the effect to be versus traps.
        /// </summary>
        /// <param name="eEffect">The effect to modify</param>
        /// <returns>The modified effect</returns>
        public static Effect VersusTrapEffect(Effect eEffect) => _service.VersusTrapEffect(eEffect);

        /// <summary>
        /// Creates a Skill Increase effect.
        /// </summary>
        /// <param name="nSkill">The skill to increase (SKILL_* constants)</param>
        /// <param name="nValue">The amount to increase the skill by</param>
        /// <returns>The Skill Increase effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nSkill is invalid</returns>
        public static Effect EffectSkillIncrease(NWNSkillType nSkill, int nValue) => _service.EffectSkillIncrease(nSkill, nValue);

        /// <summary>
        /// Creates a Temporary Hitpoints effect.
        /// </summary>
        /// <param name="nHitPoints">A positive integer for the hit points</param>
        /// <returns>The Temporary Hitpoints effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nHitPoints < 0</returns>
        public static Effect EffectTemporaryHitpoints(int nHitPoints) => _service.EffectTemporaryHitpoints(nHitPoints);

        /// <summary>
        /// Creates a conversation event.
        /// </summary>
        /// <returns>The conversation event</returns>
        /// <remarks>This only creates the event. The event won't actually trigger until SignalEvent() is called using this created conversation event as an argument. For example: SignalEvent(oCreature, EventConversation()); Once the event has been signaled, the script associated with the OnConversation event will run on the creature oCreature. To specify the OnConversation script that should run, view the Creature Properties on the creature and click on the Scripts Tab. Then specify a script for the OnConversation event.</remarks>
        public static Event EventConversation() => _service.EventConversation();

        /// <summary>
        /// Creates a Damage Immunity Increase effect.
        /// </summary>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants)</param>
        /// <param name="nPercentImmunity">The percentage of immunity to increase</param>
        /// <returns>The Damage Immunity Increase effect</returns>
        public static Effect EffectDamageImmunityIncrease(DamageType nDamageType, int nPercentImmunity) => _service.EffectDamageImmunityIncrease(nDamageType, nPercentImmunity);

        /// <summary>
        /// Creates an Immunity effect.
        /// </summary>
        /// <param name="nImmunityType">The immunity type (IMMUNITY_TYPE_* constants)</param>
        /// <returns>The Immunity effect</returns>
        public static Effect EffectImmunity(ImmunityType nImmunityType) => _service.EffectImmunity(nImmunityType);

        /// <summary>
        /// Creates a Haste effect.
        /// </summary>
        /// <returns>The Haste effect</returns>
        public static Effect EffectHaste() => _service.EffectHaste();

        /// <summary>
        /// Creates a Slow effect.
        /// </summary>
        /// <returns>The Slow effect</returns>
        public static Effect EffectSlow() => _service.EffectSlow();

        /// <summary>
        /// Creates a Poison effect.
        /// </summary>
        /// <param name="nPoisonType">The poison type (POISON_* constants)</param>
        /// <returns>The Poison effect</returns>
        public static Effect EffectPoison(PoisonType nPoisonType) => _service.EffectPoison(nPoisonType);

        /// <summary>
        /// Creates a Disease effect.
        /// </summary>
        /// <param name="nDiseaseType">The disease type (DISEASE_* constants)</param>
        /// <returns>The Disease effect</returns>
        public static Effect EffectDisease(DiseaseType nDiseaseType) => _service.EffectDisease(nDiseaseType);

        /// <summary>
        /// Creates a Silence effect.
        /// </summary>
        /// <returns>The Silence effect</returns>
        public static Effect EffectSilence() => _service.EffectSilence();

        /// <summary>
        /// Creates a Spell Resistance Increase effect.
        /// </summary>
        /// <param name="nValue">The size of spell resistance increase</param>
        /// <returns>The Spell Resistance Increase effect</returns>
        public static Effect EffectSpellResistanceIncrease(int nValue) => _service.EffectSpellResistanceIncrease(nValue);

        /// <summary>
        /// Creates a Beam effect.
        /// </summary>
        /// <param name="nBeamVisualEffect">The beam visual effect (VFX_BEAM_* constants)</param>
        /// <param name="oEffector">The creature the beam is emitted from</param>
        /// <param name="nBodyPart">The body part (BODY_NODE_* constants)</param>
        /// <param name="bMissEffect">If true, the beam will fire to a random vector near or past the target (default: false)</param>
        /// <returns>The Beam effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nBeamVisualEffect is not valid</returns>
        public static Effect EffectBeam(VisualEffectType nBeamVisualEffect, uint oEffector, BodyNodeType nBodyPart, bool bMissEffect = false) => _service.EffectBeam(nBeamVisualEffect, oEffector, nBodyPart, bMissEffect);

        /// <summary>
        /// Links the two supplied effects, returning the child effect as a child of the parent effect.
        /// </summary>
        /// <param name="eChildEffect">The child effect to link</param>
        /// <param name="eParentEffect">The parent effect to link to</param>
        /// <returns>The linked child effect</returns>
        /// <remarks>When applying linked effects if the target is immune to all valid effects all other effects will be removed as well. This means that if you apply a visual effect and a silence effect (in a link) and the target is immune to the silence effect that the visual effect will get removed as well. Visual Effects are not considered "valid" effects for the purposes of determining if an effect will be removed or not and as such should never be packaged only with other visual effects in a link.</remarks>
        public static Effect EffectLinkEffects(Effect eChildEffect, Effect eParentEffect) => _service.EffectLinkEffects(eChildEffect, eParentEffect);

        /// <summary>
        /// Creates a Visual Effect that can be applied to an object.
        /// </summary>
        /// <param name="visualEffectID">The visual effect ID</param>
        /// <param name="nMissEffect">If true, a random vector near or past the target will be generated, on which to play the effect (default: false)</param>
        /// <param name="fScale">The scale of the effect (default: 1.0f)</param>
        /// <param name="vTranslate">The translation vector (default: new Vector3())</param>
        /// <param name="vRotate">The rotation vector (default: new Vector3())</param>
        /// <returns>The Visual Effect</returns>
        public static Effect EffectVisualEffect(VisualEffectType visualEffectID, bool nMissEffect = false, float fScale = 1.0f, Vector3 vTranslate = new(), Vector3 vRotate = new()) => _service.EffectVisualEffect(visualEffectID, nMissEffect, fScale, vTranslate, vRotate);

        /// <summary>
        /// Applies the specified effect to the target object.
        /// </summary>
        /// <param name="nDurationType">The duration type of the effect</param>
        /// <param name="eEffect">The effect to apply</param>
        /// <param name="oTarget">The target object to apply the effect to</param>
        /// <param name="fDuration">The duration of the effect (default: 0.0f)</param>
        public static void ApplyEffectToObject(DurationType nDurationType, Effect eEffect, uint oTarget,
            float fDuration = 0.0f) => _service.ApplyEffectToObject(nDurationType, eEffect, oTarget, fDuration);

        /// <summary>
        /// Gets the effect type of the specified effect.
        /// </summary>
        /// <param name="eEffect">The effect to get the type for</param>
        /// <returns>The effect type (EFFECT_TYPE_* constants). Returns EFFECT_INVALIDEFFECT if the effect is invalid</returns>
        public static EffectScriptType GetEffectType(Effect eEffect) => _service.GetEffectType(eEffect);

        /// <summary>
        /// Creates an Area Of Effect effect in the area of the creature it is applied to.
        /// </summary>
        /// <param name="nAreaEffect">The area of effect type</param>
        /// <param name="sOnEnterScript">The script to run when entering the area (default: empty string)</param>
        /// <param name="sHeartbeatScript">The script to run on heartbeat (default: empty string)</param>
        /// <param name="sOnExitScript">The script to run when exiting the area (default: empty string)</param>
        /// <returns>The Area Of Effect effect</returns>
        /// <remarks>If the scripts are not specified, default ones will be used.</remarks>
        public static Effect EffectAreaOfEffect(AreaOfEffectType nAreaEffect, string sOnEnterScript = "",
            string sHeartbeatScript = "", string sOnExitScript = "") => _service.EffectAreaOfEffect(nAreaEffect, sOnEnterScript, sHeartbeatScript, sOnExitScript);

        /// <summary>
        /// Creates a Regenerate effect.
        /// </summary>
        /// <param name="nAmount">The amount of damage to be regenerated per time interval</param>
        /// <param name="fIntervalSeconds">The length of interval in seconds</param>
        /// <returns>The Regenerate effect</returns>
        public static Effect EffectRegenerate(int nAmount, float fIntervalSeconds) => _service.EffectRegenerate(nAmount, fIntervalSeconds);

        /// <summary>
        /// Creates a Movement Speed Increase effect.
        /// </summary>
        /// <param name="nPercentChange">The percentage change (range 0 through 99)</param>
        /// <returns>The Movement Speed Increase effect</returns>
        /// <remarks>0 = no change in speed, 50 = 50% faster, 99 = almost twice as fast</remarks>
        public static Effect EffectMovementSpeedIncrease(int nPercentChange) => _service.EffectMovementSpeedIncrease(nPercentChange);

        /// <summary>
        /// Creates a Charm effect.
        /// </summary>
        /// <returns>The Charm effect</returns>
        public static Effect EffectCharmed() => _service.EffectCharmed();

        /// <summary>
        /// Creates a Confuse effect.
        /// </summary>
        /// <returns>The Confuse effect</returns>
        public static Effect EffectConfused() => _service.EffectConfused();

        /// <summary>
        /// Creates a Frighten effect.
        /// </summary>
        /// <returns>The Frighten effect</returns>
        public static Effect EffectFrightened() => _service.EffectFrightened();

        /// <summary>
        /// Creates a Dominate effect.
        /// </summary>
        /// <returns>The Dominate effect</returns>
        public static Effect EffectDominated() => _service.EffectDominated();

        /// <summary>
        /// Creates a Daze effect.
        /// </summary>
        /// <returns>The Daze effect</returns>
        public static Effect EffectDazed() => _service.EffectDazed();

        /// <summary>
        /// Creates a Stun effect.
        /// </summary>
        /// <returns>The Stun effect</returns>
        public static Effect EffectStunned() => _service.EffectStunned();

        /// <summary>
        /// Creates a Sleep effect.
        /// </summary>
        /// <returns>The Sleep effect</returns>
        public static Effect EffectSleep() => _service.EffectSleep();

        /// <summary>
        /// Creates a Paralyze effect.
        /// </summary>
        /// <returns>The Paralyze effect</returns>
        public static Effect EffectParalyze() => _service.EffectParalyze();

        /// <summary>
        /// Creates a Spell Immunity effect.
        /// </summary>
        /// <param name="nImmunityToSpell">The spell to be immune to (SPELL_* constants) (default: Spell.AllSpells)</param>
        /// <returns>The Spell Immunity effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT if nImmunityToSpell is invalid</returns>
        /// <remarks>There is a known bug with this function. There must be a parameter specified when this is called (even if the desired parameter is SPELL_ALL_SPELLS), otherwise an effect of type EFFECT_TYPE_INVALIDEFFECT will be returned.</remarks>
        public static Effect EffectSpellImmunity(SpellType nImmunityToSpell = SpellType.AllSpells) => _service.EffectSpellImmunity(nImmunityToSpell);

        /// <summary>
        /// Creates a Deaf effect.
        /// </summary>
        /// <returns>The Deaf effect</returns>
        public static Effect EffectDeaf() => _service.EffectDeaf();

        /// <summary>
        /// Gets the integer parameter of the effect at the specified index.
        /// </summary>
        /// <param name="eEffect">The effect to get the integer parameter from</param>
        /// <param name="nIndex">The index bounds (0 to 7 inclusive)</param>
        /// <returns>The integer value or 0 on error/when not set</returns>
        /// <remarks>Some experimentation will be needed to find the right index for the value you wish to determine.</remarks>
        public static int GetEffectInteger(Effect eEffect, int nIndex) => _service.GetEffectInteger(eEffect, nIndex);

        /// <summary>
        /// Gets the float parameter of the effect at the specified index.
        /// </summary>
        /// <param name="eEffect">The effect to get the float parameter from</param>
        /// <param name="nIndex">The index bounds (0 to 3 inclusive)</param>
        /// <returns>The float value or 0.0f on error/when not set</returns>
        /// <remarks>Some experimentation will be needed to find the right index for the value you wish to determine.</remarks>
        public static float GetEffectFloat(Effect eEffect, int nIndex) => _service.GetEffectFloat(eEffect, nIndex);

        /// <summary>
        /// Gets the string parameter of the effect at the specified index.
        /// </summary>
        /// <param name="eEffect">The effect to get the string parameter from</param>
        /// <param name="nIndex">The index bounds (0 to 5 inclusive)</param>
        /// <returns>The string value or empty string on error/when not set</returns>
        /// <remarks>Some experimentation will be needed to find the right index for the value you wish to determine.</remarks>
        public static string GetEffectString(Effect eEffect, int nIndex) => _service.GetEffectString(eEffect, nIndex);

        /// <summary>
        /// Gets the object parameter of the effect at the specified index.
        /// </summary>
        /// <param name="eEffect">The effect to get the object parameter from</param>
        /// <param name="nIndex">The index bounds (0 to 3 inclusive)</param>
        /// <returns>The object value or OBJECT_INVALID on error/when not set</returns>
        /// <remarks>Some experimentation will be needed to find the right index for the value you wish to determine.</remarks>
        public static uint GetEffectObject(Effect eEffect, int nIndex) => _service.GetEffectObject(eEffect, nIndex);

        /// <summary>
        /// Gets the vector parameter of the effect at the specified index.
        /// </summary>
        /// <param name="eEffect">The effect to get the vector parameter from</param>
        /// <param name="nIndex">The index bounds (0 to 1 inclusive)</param>
        /// <returns>The vector value or {0.0f, 0.0f, 0.0f} on error/when not set</returns>
        /// <remarks>Some experimentation will be needed to find the right index for the value you wish to determine.</remarks>
        public static Vector3 GetEffectVector(Effect eEffect, int nIndex) => _service.GetEffectVector(eEffect, nIndex);

        /// <summary>
        /// Creates a RunScript effect.
        /// </summary>
        /// <param name="sOnAppliedScript">An optional script to execute when the effect is applied (default: empty string)</param>
        /// <param name="sOnRemovedScript">An optional script to execute when the effect is removed (default: empty string)</param>
        /// <param name="sOnIntervalScript">An optional script to execute every fInterval seconds (default: empty string)</param>
        /// <param name="fInterval">The interval in seconds, must be >0.0f if an interval script is set (default: 0.0f)</param>
        /// <param name="sData">An optional string of data saved in the effect, retrievable with GetEffectString() at index 0 (default: empty string)</param>
        /// <returns>The RunScript effect</returns>
        /// <remarks>When applied as instant effect, only sOnAppliedScript will fire. In the scripts, OBJECT_SELF will be the object the effect is applied to. Very low interval values may have an adverse effect on performance.</remarks>
        public static Effect EffectRunScript(string sOnAppliedScript = "", string sOnRemovedScript = "", string sOnIntervalScript = "", float fInterval = 0.0f, string sData = "") => _service.EffectRunScript(sOnAppliedScript, sOnRemovedScript, sOnIntervalScript, fInterval, sData);

        /// <summary>
        /// Gets the effect that last triggered an EffectRunScript() script.
        /// </summary>
        /// <returns>The effect that last triggered an EffectRunScript() script</returns>
        /// <remarks>This can be used to get the creator or tag, among others, of the EffectRunScript() in one of its scripts.</remarks>
        public static Effect GetLastRunScriptEffect() => _service.GetLastRunScriptEffect();

        /// <summary>
        /// Gets the script type of the last triggered EffectRunScript() script.
        /// </summary>
        /// <returns>The script type (RUNSCRIPT_EFFECT_SCRIPT_TYPE_* constants). Returns 0 when called outside of an EffectRunScript() script</returns>
        public static int GetLastRunScriptEffectScriptType() => _service.GetLastRunScriptEffectScriptType();

        /// <summary>
        /// Hides the effect icon of the effect and of all effects currently linked to it.
        /// </summary>
        /// <param name="eEffect">The effect to hide the icon for</param>
        /// <returns>The effect with hidden icon</returns>
        public static Effect HideEffectIcon(Effect eEffect) => _service.HideEffectIcon(eEffect);

        /// <summary>
        /// Creates an Icon effect.
        /// </summary>
        /// <param name="nIconId">The effect icon (EFFECT_ICON_* constants) to display</param>
        /// <returns>The Icon effect. Returns an effect of type EFFECT_TYPE_INVALIDEFFECT when nIconID is < 1 or > 255</returns>
        /// <remarks>Using the icon for Poison/Disease will also color the health bar green/brown, useful to simulate custom poisons/diseases.</remarks>
        public static Effect EffectIcon(EffectIconType nIconId) => _service.EffectIcon(nIconId);

        /// <summary>
        /// Sets the subtype of the effect to Unyielding and returns the effect.
        /// </summary>
        /// <param name="eEffect">The effect to set as unyielding</param>
        /// <returns>The unyielding effect</returns>
        /// <remarks>Effects default to magical if the subtype is not set. Unyielding effects are not removed by resting, death or dispel magic, only by RemoveEffect(). Note: effects that modify state, Stunned/Knockdown/Deaf etc, WILL be removed on death.</remarks>
        public static Effect UnyieldingEffect(Effect eEffect) => _service.UnyieldingEffect(eEffect);

        /// <summary>
        /// Sets the effect to ignore immunities and returns the effect.
        /// </summary>
        /// <param name="eEffect">The effect to set to ignore immunities</param>
        /// <returns>The effect that ignores immunities</returns>
        public static Effect IgnoreEffectImmunity(Effect eEffect) => _service.IgnoreEffectImmunity(eEffect);

        /// <summary>
        /// Creates a Pacified effect, making the creature unable to attack anyone.
        /// </summary>
        /// <returns>The Pacified effect</returns>
        public static Effect EffectPacified() => _service.EffectPacified();

        /// <summary>
        /// Returns the given effect's Link ID.
        /// </summary>
        /// <param name="eEffect">The effect to get the link ID for</param>
        /// <returns>The link ID string</returns>
        /// <remarks>There are no guarantees about this identifier other than it is unique and the same for all effects linked to it.</remarks>
        public static string GetEffectLinkId(Effect eEffect) => _service.GetEffectLinkId(eEffect);

        /// <summary>
        /// Creates a bonus feat effect.
        /// </summary>
        /// <param name="nFeat">The feat (FEAT_* constants)</param>
        /// <returns>The Bonus Feat effect</returns>
        /// <remarks>These act like the Bonus Feat item property, and do not work as feat prerequisites for levelup purposes.</remarks>
        public static Effect EffectBonusFeat(int nFeat) => _service.EffectBonusFeat(nFeat);

        /// <summary>
        /// Provides immunity to the effects of EffectTimeStop.
        /// </summary>
        /// <returns>The Time Stop Immunity effect</returns>
        /// <remarks>This allows actions during other creatures' time stop effects.</remarks>
        public static Effect EffectTimeStopImmunity() => _service.EffectTimeStopImmunity();

        /// <summary>
        /// Forces the creature to always walk.
        /// </summary>
        /// <returns>The Force Walk effect</returns>
        public static Effect EffectForceWalk() => _service.EffectForceWalk();

        /// <summary>
        /// Sets the effect creator.
        /// </summary>
        /// <param name="eEffect">The effect to modify</param>
        /// <param name="oCreator">The creator of the effect. Can be OBJECT_INVALID</param>
        /// <returns>The modified effect</returns>
        public static Effect SetEffectCreator(Effect eEffect, uint oCreator) => _service.SetEffectCreator(eEffect, oCreator);

        /// <summary>
        /// Sets the effect caster level.
        /// </summary>
        /// <param name="eEffect">The effect to modify</param>
        /// <param name="nCasterLevel">The caster level of the effect for the purposes of dispel magic and GetEffectCasterLevel. Must be >= 0</param>
        /// <returns>The modified effect</returns>
        public static Effect SetEffectCasterLevel(Effect eEffect, int nCasterLevel) => _service.SetEffectCasterLevel(eEffect, nCasterLevel);

        /// <summary>
        /// Sets the effect spell ID.
        /// </summary>
        /// <param name="eEffect">The effect to modify</param>
        /// <param name="nSpellId">The spell ID for the purposes of effect stacking, dispel magic and GetEffectSpellId. Must be >= -1 (-1 being invalid/no spell)</param>
        /// <returns>The modified effect</returns>
        public static Effect SetEffectSpellId(Effect eEffect, SpellType nSpellId) => _service.SetEffectSpellId(eEffect, nSpellId);

        /// <summary>
        /// Gets the duration (in seconds) of the sound attached to the specified string reference.
        /// </summary>
        /// <param name="nStrRef">The string reference to get the sound duration for</param>
        /// <returns>The duration in seconds. Returns 0.0f if no duration is stored or if no sound is attached</returns>
        public static float GetStrRefSoundDuration(int nStrRef) => _service.GetStrRefSoundDuration(nStrRef);

        /// <summary>
        /// Gets the length of the specified wavefile in seconds.
        /// </summary>
        /// <param name="nStrRef">The string reference to get the dialog sound length for</param>
        /// <returns>The length in seconds</returns>
        /// <remarks>Only works for sounds used for dialog.</remarks>
        public static float GetDialogSoundLength(int nStrRef) => _service.GetDialogSoundLength(nStrRef);

        /// <summary>
        /// Plays a sound that is associated with a string reference as a mono sound from the location of the object running the command.
        /// </summary>
        /// <param name="nStrRef">The string reference of the sound to play</param>
        /// <param name="nRunAsAction">If false, the sound is forced to play instantly (default: true)</param>
        public static void PlaySoundByStrRef(int nStrRef, bool nRunAsAction = true) => _service.PlaySoundByStrRef(nStrRef, nRunAsAction);

        /// <summary>
        /// Plays the specified sound as a mono sound from the location of the object running the command.
        /// </summary>
        /// <param name="sSoundName">The name of the sound to play</param>
        public static void PlaySound(string sSoundName) => _service.PlaySound(sSoundName);

        /// <summary>
        /// Un/pauses the given audio stream.
        /// fFadeTime is in seconds to gradually fade the audio out/in instead of pausing/resuming directly.
        /// Only one type of fading can be active at once, for example:
        /// If you call StartAudioStream() with fFadeInTime = 10.0f, any other audio stream functions with a fade time > 0.0f will have no effect
        /// until StartAudioStream() is done fading.
        /// Will do nothing if the stream is currently not in use.
        /// </summary>
        /// <param name="oPlayer">The player to set audio stream pause for</param>
        /// <param name="nStreamIdentifier">The stream identifier</param>
        /// <param name="bPaused">Whether the stream should be paused</param>
        /// <param name="fFadeTime">The fade time in seconds (default: 0.0f)</param>
        public static void SetAudioStreamPaused(uint oPlayer, int nStreamIdentifier, bool bPaused, float fFadeTime = 0.0f) => _service.SetAudioStreamPaused(oPlayer, nStreamIdentifier, bPaused, fFadeTime);

        /// <summary>
        /// Changes volume of audio stream.
        /// Volume is from 0.0 to 1.0.
        /// fFadeTime is in seconds to gradually change the volume.
        /// Only one type of fading can be active at once, for example:
        /// If you call StartAudioStream() with fFadeInTime = 10.0f, any other audio stream functions with a fade time > 0.0f will have no effect
        /// until StartAudioStream() is done fading.
        /// Subsequent calls to this function with fFadeTime > 0.0f while already fading the volume
        /// will start the new fade with the previous fade's progress as starting point.
        /// Will do nothing if the stream is currently not in use.
        /// </summary>
        /// <param name="oPlayer">The player to set audio stream volume for</param>
        /// <param name="nStreamIdentifier">The stream identifier</param>
        /// <param name="fVolume">The volume level (0.0 to 1.0) (default: 1.0f)</param>
        /// <param name="fFadeTime">The fade time in seconds (default: 0.0f)</param>
        public static void SetAudioStreamVolume(uint oPlayer, int nStreamIdentifier, float fVolume = 1.0f, float fFadeTime = 0.0f) => _service.SetAudioStreamVolume(oPlayer, nStreamIdentifier, fVolume, fFadeTime);

        /// <summary>
        /// Seeks the audio stream to the given offset.
        /// When seeking at or beyond the end of a stream, the seek offset will wrap around, even if the file is configured not to loop.
        /// Will do nothing if the stream is currently not in use.
        /// Will do nothing if the stream is in ended state (reached end of file and looping is off). In this
        /// case, you need to restart the stream.
        /// </summary>
        /// <param name="oPlayer">The player to seek audio stream for</param>
        /// <param name="nStreamIdentifier">The stream identifier</param>
        /// <param name="fSeconds">The offset in seconds to seek to</param>
        public static void SeekAudioStream(uint oPlayer, int nStreamIdentifier, float fSeconds) => _service.SeekAudioStream(oPlayer, nStreamIdentifier, fSeconds);

        /// <summary>
        /// Adds the PC to the party leader's party. This will only work on two PCs.
        /// </summary>
        /// <param name="oPC">Player to add to a party</param>
        /// <param name="oPartyLeader">Player already in the party</param>
        public static void AddToParty(uint oPC, uint oPartyLeader) => _service.AddToParty(oPC, oPartyLeader);

        /// <summary>
        /// Removes the PC from their current party. This will only work on a PC.
        /// </summary>
        /// <param name="oPC">Removes this player from whatever party they're currently in</param>
        public static void RemoveFromParty(uint oPC) => _service.RemoveFromParty(oPC);

        /// <summary>
        /// Makes the corresponding panel button on the player's client start or stop flashing.
        /// </summary>
        /// <param name="oPlayer">The player</param>
        /// <param name="nButton">PANEL_BUTTON_* constant</param>
        /// <param name="nEnableFlash">If TRUE, the button will start flashing. If FALSE, the button will stop flashing</param>
        public static void SetPanelButtonFlash(uint oPlayer, int nButton, int nEnableFlash) => _service.SetPanelButtonFlash(oPlayer, nButton, nEnableFlash);

        /// <summary>
        /// Gets the last attacker of the specified target.
        /// </summary>
        /// <param name="oAttackee">The target that was attacked (default: OBJECT_SELF)</param>
        /// <returns>The last attacker. Returns OBJECT_INVALID on error</returns>
        /// <remarks>This should only be used in the OnAttacked events for creatures, placeables and doors.</remarks>
        public static uint GetLastAttacker(uint oAttackee = NWScriptService.OBJECT_INVALID) => _service.GetLastAttacker(oAttackee);

        /// <summary>
        /// Makes the action subject attack the specified target.
        /// </summary>
        /// <param name="oAttackee">The target to attack</param>
        /// <param name="bPassive">If true, attack is in passive mode (default: false)</param>
        public static void ActionAttack(uint oAttackee, bool bPassive = false) => _service.ActionAttack(oAttackee, bPassive);

        /// <summary>
        /// Performs a Fortitude Save check for the given difficulty class.
        /// </summary>
        /// <param name="oCreature">The creature making the save</param>
        /// <param name="nDC">The difficulty class to beat</param>
        /// <param name="nSaveType">The type of saving throw (SAVING_THROW_TYPE_* constants) (default: SavingThrowType.All)</param>
        /// <param name="oSaveVersus">The object the save is against (default: OBJECT_SELF)</param>
        /// <returns>0 if the saving throw roll failed, 1 if the saving throw roll succeeded, 2 if the target was immune to the save type specified</returns>
        /// <remarks>If used within an Area of Effect Object Script (On Enter, OnExit, OnHeartbeat), you MUST pass GetAreaOfEffectCreator() into oSaveVersus!!</remarks>
        public static SavingThrowResultType FortitudeSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All,
            uint oSaveVersus = NWScriptService.OBJECT_INVALID) => _service.FortitudeSave(oCreature, nDC, nSaveType, oSaveVersus);

        /// <summary>
        /// Performs a Reflex Save check for the given difficulty class.
        /// </summary>
        /// <param name="oCreature">The creature making the save</param>
        /// <param name="nDC">The difficulty class to beat</param>
        /// <param name="nSaveType">The type of saving throw (SAVING_THROW_TYPE_* constants) (default: SavingThrowType.All)</param>
        /// <param name="oSaveVersus">The object the save is against (default: OBJECT_SELF)</param>
        /// <returns>0 if the saving throw roll failed, 1 if the saving throw roll succeeded, 2 if the target was immune to the save type specified</returns>
        /// <remarks>If used within an Area of Effect Object Script (On Enter, OnExit, OnHeartbeat), you MUST pass GetAreaOfEffectCreator() into oSaveVersus!!</remarks>
        public static SavingThrowResultType ReflexSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All,
            uint oSaveVersus = NWScriptService.OBJECT_INVALID) => _service.ReflexSave(oCreature, nDC, nSaveType, oSaveVersus);

        /// <summary>
        /// Performs a Will Save check for the given difficulty class.
        /// </summary>
        /// <param name="oCreature">The creature making the save</param>
        /// <param name="nDC">The difficulty class to beat</param>
        /// <param name="nSaveType">The type of saving throw (SAVING_THROW_TYPE_* constants) (default: SavingThrowType.All)</param>
        /// <param name="oSaveVersus">The object the save is against (default: OBJECT_SELF)</param>
        /// <returns>0 if the saving throw roll failed, 1 if the saving throw roll succeeded, 2 if the target was immune to the save type specified</returns>
        /// <remarks>If used within an Area of Effect Object Script (On Enter, OnExit, OnHeartbeat), you MUST pass GetAreaOfEffectCreator() into oSaveVersus!!</remarks>
        public static SavingThrowResultType WillSave(uint oCreature, int nDC, SavingThrowType nSaveType = SavingThrowType.All,
            uint oSaveVersus = NWScriptService.OBJECT_INVALID) => _service.WillSave(oCreature, nDC, nSaveType, oSaveVersus);

        /// <summary>
        /// Performs a Spell Resistance check between the caster and target.
        /// </summary>
        /// <param name="oCaster">The spell caster</param>
        /// <param name="oTarget">The target of the spell</param>
        /// <returns>Return values: FALSE if oCaster or oTarget is an invalid object, -1 if spell cast is not a player spell, 1 if spell resisted, 2 if spell resisted via magic immunity, 3 if spell resisted via spell absorption</returns>
        public static int ResistSpell(uint oCaster, uint oTarget) => _service.ResistSpell(oCaster, oTarget);

        /// <summary>
        /// Makes the attacker perform a Melee Touch Attack on the target.
        /// </summary>
        /// <param name="oTarget">The target to attack</param>
        /// <param name="bDisplayFeedback">Whether to display feedback (default: true)</param>
        /// <param name="oAttacker">The attacker object (defaults to OBJECT_SELF)</param>
        /// <returns>0 on a miss, 1 on a hit, and 2 on a critical hit</returns>
        /// <remarks>This is not an action, and it assumes the attacker is already within range of the target.</remarks>
        public static TouchAttackReturnType TouchAttackMelee(uint oTarget, bool bDisplayFeedback = true, uint oAttacker = NWScriptService.OBJECT_INVALID) => _service.TouchAttackMelee(oTarget, bDisplayFeedback, oAttacker);

        /// <summary>
        /// Makes the attacker perform a Ranged Touch Attack on the target.
        /// </summary>
        /// <param name="oTarget">The target to attack</param>
        /// <param name="bDisplayFeedback">Whether to display feedback (default: true)</param>
        /// <param name="oAttacker">The attacker object (defaults to OBJECT_SELF)</param>
        /// <returns>0 on a miss, 1 on a hit, and 2 on a critical hit</returns>
        public static TouchAttackReturnType TouchAttackRanged(uint oTarget, bool bDisplayFeedback = true, uint oAttacker = NWScriptService.OBJECT_INVALID) => _service.TouchAttackRanged(oTarget, bDisplayFeedback, oAttacker);

        /// <summary>
        /// Gets the attack mode of the creature's last attack.
        /// </summary>
        /// <param name="oCreature">The creature to get the attack mode for (default: OBJECT_SELF)</param>
        /// <returns>The attack mode (COMBAT_MODE_* constants)</returns>
        /// <remarks>This only works when the creature is in combat.</remarks>
        public static CombatModeType GetLastAttackMode(uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetLastAttackMode(oCreature);

        /// <summary>
        /// Gets the last weapon that the creature used in an attack.
        /// </summary>
        /// <param name="oCreature">The creature to get the last weapon for</param>
        /// <returns>The last weapon used. Returns OBJECT_INVALID if the creature did not attack or has no weapon equipped</returns>
        public static uint GetLastWeaponUsed(uint oCreature) => _service.GetLastWeaponUsed(oCreature);

        /// <summary>
        /// Gets the amount of damage of the specified type that has been dealt to the caller.
        /// </summary>
        /// <param name="nDamageType">The damage type (DAMAGE_TYPE_* constants)</param>
        /// <returns>The amount of damage dealt</returns>
        public static int GetDamageDealtByType(DamageType nDamageType) => _service.GetDamageDealtByType(nDamageType);

        /// <summary>
        /// Gets the total amount of damage that has been dealt to the specified object.
        /// </summary>
        /// <returns>The total amount of damage dealt</returns>
        public static int GetTotalDamageDealt() => _service.GetTotalDamageDealt();

        /// <summary>
        /// Gets the last object that damaged the specified object.
        /// </summary>
        /// <param name="oObject">The object that was damaged (default: OBJECT_SELF)</param>
        /// <returns>The last object that damaged the target. Returns OBJECT_INVALID if the passed in object is not a valid object</returns>
        public static uint GetLastDamager(uint oObject = NWScriptService.OBJECT_INVALID) => _service.GetLastDamager(oObject);

        /// <summary>
        /// Gets the target that the specified creature attempted to attack.
        /// </summary>
        /// <returns>The attempted attack target. Returns OBJECT_INVALID if the caller is not a valid creature</returns>
        /// <remarks>This should be used in conjunction with GetAttackTarget(). This value is set every time an attack is made, and is reset at the end of combat.</remarks>
        public static uint GetAttemptedAttackTarget() => _service.GetAttemptedAttackTarget();

        /// <summary>
        /// Returns true if the weapon equipped is capable of damaging the specified target.
        /// </summary>
        /// <param name="oVersus">The target to check effectiveness against (default: OBJECT_INVALID)</param>
        /// <param name="bOffHand">Whether to check the off-hand weapon (default: false)</param>
        /// <returns>True if the weapon is effective against the target</returns>
        public static bool GetIsWeaponEffective(uint oVersus = NWScriptService.OBJECT_INVALID, bool bOffHand = false) => _service.GetIsWeaponEffective(oVersus, bOffHand);

        /// <summary>
        /// Returns true if the object has effects on it originating from the specified feat.
        /// </summary>
        /// <param name="nFeat">The feat to check for (FEAT_* constants)</param>
        /// <param name="oObject">The object to check (default: OBJECT_SELF)</param>
        /// <returns>True if the object has effects from the feat</returns>
        public static int GetHasFeatEffect(int nFeat, uint oObject = NWScriptService.OBJECT_INVALID) => _service.GetHasFeatEffect(nFeat, oObject);

        /// <summary>
        /// Gets the number of remaining uses for the specified feat on the creature.
        /// </summary>
        /// <param name="nFeat">The feat to check (FEAT_* constants)</param>
        /// <param name="oCreature">The creature to check the feat for</param>
        /// <returns>The number of remaining uses left, or the maximum int value if the feat has unlimited uses (e.g., FEAT_KNOCKDOWN)</returns>
        /// <remarks>Only returns a value if the creature has the feat and it is usable.</remarks>
        public static int GetFeatRemainingUses(FeatType nFeat, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetFeatRemainingUses(nFeat, oCreature);

        /// <summary>
        /// Parses the given string as a valid JSON value, and returns the corresponding type.
        /// Returns a JSON_TYPE_NULL on error.
        /// Check JsonGetError() to see the parse error, if any.
        /// NB: The parsed string needs to be in game-local encoding, but the generated JSON structure
        /// will contain UTF-8 data.
        /// </summary>
        /// <param name="jValue">The string to parse as JSON</param>
        /// <returns>The parsed JSON value, or JSON_TYPE_NULL on error</returns>
        public static Json JsonParse(string jValue) => _service.JsonParse(jValue);

        /// <summary>
        /// Dumps the given JSON value into a string that can be read back in via JsonParse.
        /// nIndent describes the indentation level for pretty-printing; a value of -1 means no indentation and no linebreaks.
        /// Returns a string describing JSON_TYPE_NULL on error.
        /// NB: The dumped string is in game-local encoding, with all non-ASCII characters escaped.
        /// </summary>
        /// <param name="jValue">The JSON value to dump</param>
        /// <param name="nIndent">The indentation level for pretty-printing (defaults to -1)</param>
        /// <returns>The JSON string, or error description on error</returns>
        public static string JsonDump(Json jValue, int nIndent = -1) => _service.JsonDump(jValue, nIndent);

        /// <summary>
        /// Describes the type of the given JSON value.
        /// Returns JSON_TYPE_NULL if the value is empty.
        /// </summary>
        /// <param name="jValue">The JSON value to get the type of</param>
        /// <returns>The JSON type, or JSON_TYPE_NULL if the value is empty</returns>
        public static JsonType JsonGetType(Json jValue) => _service.JsonGetType(jValue);

        /// <summary>
        /// Returns the length of the given JSON type.
        /// For objects, returns the number of top-level keys present.
        /// For arrays, returns the number of elements.
        /// Null types are of size 0.
        /// All other types return 1.
        /// </summary>
        /// <param name="jValue">The JSON value to get the length of</param>
        /// <returns>The length of the JSON value</returns>
        public static int JsonGetLength(Json jValue) => _service.JsonGetLength(jValue);

        /// <summary>
        /// Returns the error message if the value has errored out.
        /// Currently only describes parse errors.
        /// </summary>
        /// <param name="jValue">The JSON value to get the error for</param>
        /// <returns>The error message, or empty string if no error</returns>
        public static string JsonGetError(Json jValue) => _service.JsonGetError(jValue);

        /// <summary>
        /// Creates a NULL JSON value, seeded with an optional error message for JsonGetError().
        /// </summary>
        /// <param name="sError">Optional error message (defaults to empty string)</param>
        /// <returns>A NULL JSON value</returns>
        public static Json JsonNull(string sError = "") => _service.JsonNull(sError);

        /// <summary>
        /// Creates an empty JSON object.
        /// </summary>
        /// <returns>An empty JSON object</returns>
        public static Json JsonObject() => _service.JsonObject();

        /// <summary>
        /// Creates an empty JSON array.
        /// </summary>
        /// <returns>An empty JSON array</returns>
        public static Json JsonArray() => _service.JsonArray();

        /// <summary>
        /// Creates a JSON string value.
        /// NB: Strings are encoded to UTF-8 from the game-local charset.
        /// </summary>
        /// <param name="sValue">The string value to create</param>
        /// <returns>A JSON string value</returns>
        public static Json JsonString(string sValue) => _service.JsonString(sValue);

        /// <summary>
        /// Creates a JSON integer value.
        /// </summary>
        /// <param name="nValue">The integer value to create</param>
        /// <returns>A JSON integer value</returns>
        public static Json JsonInt(int nValue) => _service.JsonInt(nValue);

        /// <summary>
        /// Creates a JSON floating point value.
        /// </summary>
        /// <param name="fValue">The float value to create</param>
        /// <returns>A JSON float value</returns>
        public static Json JsonFloat(float fValue) => _service.JsonFloat(fValue);

        /// <summary>
        /// Creates a JSON boolean value.
        /// </summary>
        /// <param name="bValue">The boolean value to create</param>
        /// <returns>A JSON boolean value</returns>
        public static Json JsonBool(bool bValue) => _service.JsonBool(bValue);

        /// <summary>
        /// Returns a string representation of the JSON value.
        /// Returns empty string if the value cannot be represented as a string, or is empty.
        /// NB: Strings are decoded from UTF-8 to the game-local charset.
        /// </summary>
        /// <param name="jValue">The JSON value to get the string representation of</param>
        /// <returns>The string representation, or empty string if not representable</returns>
        public static string JsonGetString(Json jValue) => _service.JsonGetString(jValue);

        /// <summary>
        /// Returns an integer representation of the JSON value, casting where possible.
        /// Returns 0 if the value cannot be represented as an integer.
        /// Use this to parse JSON boolean types.
        /// NB: This will narrow down to signed 32 bit, as that is what NWScript int is.
        /// If you are trying to read a 64 bit or unsigned integer, you will lose data.
        /// You will not lose data if you keep the value as a JSON element (via Object/ArrayGet).
        /// </summary>
        /// <param name="jValue">The JSON value to get the integer representation of</param>
        /// <returns>The integer representation, or 0 if not representable</returns>
        public static int JsonGetInt(Json jValue) => _service.JsonGetInt(jValue);

        /// <summary>
        /// Returns a float representation of the JSON value, casting where possible.
        /// Returns 0.0 if the value cannot be represented as a float.
        /// NB: This will narrow doubles down to float.
        /// If you are trying to read a double, you will lose data.
        /// You will not lose data if you keep the value as a JSON element (via Object/ArrayGet).
        /// </summary>
        /// <param name="jValue">The JSON value to get the float representation of</param>
        /// <returns>The float representation, or 0.0 if not representable</returns>
        public static float JsonGetFloat(Json jValue) => _service.JsonGetFloat(jValue);

        /// <summary>
        /// Returns a JSON array containing all keys of the object.
        /// Returns an empty array if the object is empty or not a JSON object, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jObject">The JSON object to get keys from</param>
        /// <returns>A JSON array of keys, or empty array on error</returns>
        public static Json JsonObjectKeys(Json jObject) => _service.JsonObjectKeys(jObject);

        /// <summary>
        /// Returns the key value of sKey on the object.
        /// Returns a null JSON value if jObject is not an object or sKey does not exist on the object, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jObject">The JSON object to get the key from</param>
        /// <param name="sKey">The key to retrieve</param>
        /// <returns>The key value, or null JSON value on error</returns>
        public static Json JsonObjectGet(Json jObject, string sKey) => _service.JsonObjectGet(jObject, sKey);

        /// <summary>
        /// Returns a modified copy of the object with the key at sKey set to jValue.
        /// Returns a JSON null value if jObject is not an object, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jObject">The JSON object to modify</param>
        /// <param name="sKey">The key to set</param>
        /// <param name="jValue">The value to set</param>
        /// <returns>A modified copy of the object, or null JSON value on error</returns>
        public static Json JsonObjectSet(Json jObject, string sKey, Json jValue) => _service.JsonObjectSet(jObject, sKey, jValue);

        /// <summary>
        /// Returns a modified copy of the object with the key at sKey deleted.
        /// Returns a JSON null value if jObject is not an object, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jObject">The JSON object to modify</param>
        /// <param name="sKey">The key to delete</param>
        /// <returns>A modified copy of the object, or null JSON value on error</returns>
        public static Json JsonObjectDel(Json jObject, string sKey) => _service.JsonObjectDel(jObject, sKey);

        /// <summary>
        /// Gets the JSON object at the array index position.
        /// Returns a JSON null value if the index is out of bounds, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jArray">The JSON array to get from</param>
        /// <param name="nIndex">The index position</param>
        /// <returns>The JSON object at the index, or null JSON value on error</returns>
        public static Json JsonArrayGet(Json jArray, int nIndex) => _service.JsonArrayGet(jArray, nIndex);

        /// <summary>
        /// Returns a modified copy of the array with position nIndex set to jValue.
        /// Returns a JSON null value if jArray is not actually an array, with GetJsonError() filled in.
        /// Returns a JSON null value if nIndex is out of bounds, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jArray">The JSON array to modify</param>
        /// <param name="nIndex">The index position to set</param>
        /// <param name="jValue">The value to set</param>
        /// <returns>A modified copy of the array, or null JSON value on error</returns>
        public static Json JsonArraySet(Json jArray, int nIndex, Json jValue) => _service.JsonArraySet(jArray, nIndex, jValue);

        /// <summary>
        /// Returns a modified copy of the array with jValue inserted at position nIndex.
        /// All succeeding objects in the array will move by one.
        /// By default (-1), inserts objects at the end of the array ("push").
        /// nIndex = 0 inserts at the beginning of the array.
        /// Returns a JSON null value if jArray is not actually an array, with GetJsonError() filled in.
        /// Returns a JSON null value if nIndex is not 0 or -1 and out of bounds, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jArray">The JSON array to modify</param>
        /// <param name="jValue">The value to insert</param>
        /// <param name="nIndex">The index position to insert at (defaults to -1)</param>
        /// <returns>A modified copy of the array, or null JSON value on error</returns>
        public static Json JsonArrayInsert(Json jArray, Json jValue, int nIndex = -1) => _service.JsonArrayInsert(jArray, jValue, nIndex);

        /// <summary>
        /// Returns a modified copy of the array with the element at position nIndex removed,
        /// and the array resized by one.
        /// Returns a JSON null value if jArray is not actually an array, with GetJsonError() filled in.
        /// Returns a JSON null value if nIndex is out of bounds, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jArray">The JSON array to modify</param>
        /// <param name="nIndex">The index position to remove</param>
        /// <returns>A modified copy of the array, or null JSON value on error</returns>
        public static Json JsonArrayDel(Json jArray, int nIndex) => _service.JsonArrayDel(jArray, nIndex);

        /// <summary>
        /// Transforms the given object into a JSON structure.
        /// The JSON format is compatible with what https://github.com/niv/neverwinter.nim@1.4.3+ emits.
        /// Returns the null JSON type on errors, or if oObject is not serializable, with GetJsonError() filled in.
        /// Supported object types: creature, item, trigger, placeable, door, waypoint, encounter, store, area (combined format)
        /// If bSaveObjectState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are saved out
        /// (except for Combined Area Format, which always has object state saved out).
        /// </summary>
        /// <param name="oObject">The object to transform</param>
        /// <param name="bSaveObjectState">Whether to save object state (defaults to false)</param>
        /// <returns>The JSON structure, or null JSON type on error</returns>
        public static Json ObjectToJson(uint oObject, bool bSaveObjectState = false) => _service.ObjectToJson(oObject, bSaveObjectState);

        /// <summary>
        /// Deserializes the game object described in the JSON object.
        /// Returns OBJECT_INVALID on errors.
        /// Supported object types: creature, item, trigger, placeable, door, waypoint, encounter, store, area (combined format)
        /// For areas, locLocation is ignored.
        /// If bLoadObjectState is TRUE, local vars, effects, action queue, and transition info (triggers, doors) are read in.
        /// </summary>
        /// <param name="jObject">The JSON object describing the game object</param>
        /// <param name="locLocation">The location to create the object at</param>
        /// <param name="oOwner">The owner of the object (defaults to OBJECT_SELF)</param>
        /// <param name="bLoadObjectState">Whether to load object state (defaults to false)</param>
        /// <returns>The created object, or OBJECT_INVALID on error</returns>
        public static uint JsonToObject(Json jObject, Location locLocation, uint oOwner = NWScriptService.OBJECT_INVALID, bool bLoadObjectState = false) => _service.JsonToObject(jObject, locLocation, oOwner, bLoadObjectState);

        /// <summary>
        /// Returns the element at the given JSON pointer value.
        /// See https://datatracker.ietf.org/doc/html/rfc6901 for details.
        /// Returns a JSON null value on error, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jData">The JSON data to search</param>
        /// <param name="sPointer">The JSON pointer path</param>
        /// <returns>The element at the pointer, or null JSON value on error</returns>
        public static Json JsonPointer(Json jData, string sPointer) => _service.JsonPointer(jData, sPointer);

        /// <summary>
        /// Returns a modified copy of jData with jValue inserted at the path described by sPointer.
        /// See https://datatracker.ietf.org/doc/html/rfc6901 for details.
        /// Returns a JSON null value on error, with GetJsonError() filled in.
        /// jPatch is an array of patch elements, each containing an op, a path, and a value field. Example:
        /// [
        ///   { "op": "replace", "path": "/baz", "value": "boo" },
        ///   { "op": "add", "path": "/hello", "value": ["world"] },
        ///   { "op": "remove", "path": "/foo"}
        /// ]
        /// Valid operations are: add, remove, replace, move, copy, test
        /// </summary>
        /// <param name="jData">The JSON data to patch</param>
        /// <param name="jPatch">The patch array</param>
        /// <returns>A modified copy of the data, or null JSON value on error</returns>
        public static Json JsonPatch(Json jData, Json jPatch) => _service.JsonPatch(jData, jPatch);

        /// <summary>
        /// Returns the diff (described as a JSON structure you can pass into JsonPatch) between the two objects.
        /// Returns a JSON null value on error, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jLHS">The left-hand side JSON object</param>
        /// <param name="jRHS">The right-hand side JSON object</param>
        /// <returns>The diff as a JSON structure, or null JSON value on error</returns>
        public static Json JsonDiff(Json jLHS, Json jRHS) => _service.JsonDiff(jLHS, jRHS);

        /// <summary>
        /// Returns a modified copy of jData with jMerge merged into it. This is an alternative to
        /// JsonPatch/JsonDiff, with a syntax more closely resembling the final object.
        /// See https://datatracker.ietf.org/doc/html/rfc7386 for details.
        /// Returns a JSON null value on error, with GetJsonError() filled in.
        /// </summary>
        /// <param name="jData">The JSON data to merge into</param>
        /// <param name="jMerge">The JSON data to merge</param>
        /// <returns>A modified copy with merged data, or null JSON value on error</returns>
        public static Json JsonMerge(Json jData, Json jMerge) => _service.JsonMerge(jData, jMerge);

        /// <summary>
        /// Gets the object's local JSON variable.
        /// </summary>
        /// <param name="oObject">The object to get the variable from</param>
        /// <param name="sVarName">The variable name</param>
        /// <returns>The JSON variable value, or JSON null type on error</returns>
        public static Json GetLocalJson(uint oObject, string sVarName) => _service.GetLocalJson(oObject, sVarName);

        /// <summary>
        /// Sets the object's local JSON variable to the specified value.
        /// </summary>
        /// <param name="oObject">The object to set the variable on</param>
        /// <param name="sVarName">The variable name</param>
        /// <param name="jValue">The JSON value to set</param>
        public static void SetLocalJson(uint oObject, string sVarName, Json jValue) => _service.SetLocalJson(oObject, sVarName, jValue);

        /// <summary>
        /// Deletes the object's local JSON variable.
        /// </summary>
        /// <param name="oObject">The object to delete the variable from</param>
        /// <param name="sVarName">The variable name to delete</param>
        public static void DeleteLocalJson(uint oObject, string sVarName) => _service.DeleteLocalJson(oObject, sVarName);

        /// <summary>
        /// Deserializes the given resref/template into a JSON structure.
        /// Supported GFF resource types:
        /// RESTYPE_CAF (and RESTYPE_ARE, RESTYPE_GIT, RESTYPE_GIC)
        /// RESTYPE_UTC, RESTYPE_UTI, RESTYPE_UTT, RESTYPE_UTP, RESTYPE_UTD, RESTYPE_UTW, RESTYPE_UTE, RESTYPE_UTM
        /// Returns a valid GFF-type JSON structure, or a null value with GetJsonError() set.
        /// </summary>
        /// <param name="sResRef">The resource reference</param>
        /// <param name="nResType">The resource type</param>
        /// <returns>A valid GFF-type JSON structure, or null value on error</returns>
        public static Json TemplateToJson(string sResRef, ResType nResType) => _service.TemplateToJson(sResRef, nResType);

        /// <summary>
        /// Returns a modified copy of the array with the value order changed according to nTransform:
        /// JSON_ARRAY_SORT_ASCENDING, JSON_ARRAY_SORT_DESCENDING
        /// Sorting is dependent on the type and follows JSON standards (e.g. 99 < "100").
        /// JSON_ARRAY_SHUFFLE: Randomizes the order of elements.
        /// JSON_ARRAY_REVERSE: Reverses the array.
        /// JSON_ARRAY_UNIQUE: Returns a modified copy of the array with duplicate values removed.
        /// Coercible but different types are not considered equal (e.g. 99 != "99"); int/float equivalence however applies: 4.0 == 4.
        /// Order is preserved.
        /// JSON_ARRAY_COALESCE: Returns the first non-null entry. Empty-ish values (e.g. "", 0) are not considered null, only the JSON scalar type.
        /// </summary>
        /// <param name="jArray">The JSON array to transform</param>
        /// <param name="nTransform">The transformation to apply</param>
        /// <returns>A modified copy of the array</returns>
        public static Json JsonArrayTransform(Json jArray, JsonArraySortType nTransform) => _service.JsonArrayTransform(jArray, nTransform);

        /// <summary>
        /// Returns the nth-matching index or key of jNeedle in jHaystack.
        /// Supported haystacks: object, array
        /// Ordering behavior for objects is unspecified.
        /// Returns null when not found or on any error.
        /// </summary>
        /// <param name="jHaystack">The JSON object or array to search in</param>
        /// <param name="jNeedle">The JSON value to search for</param>
        /// <param name="nNth">The nth match to find (defaults to 0)</param>
        /// <param name="nConditional">The conditional type for matching (defaults to Equal)</param>
        /// <returns>The index or key of the match, or null if not found</returns>
        public static Json JsonFind(
            Json jHaystack,
            Json jNeedle,
            int nNth = 0,
            JsonFindType nConditional = JsonFindType.Equal) => _service.JsonFind(jHaystack, jNeedle, nNth, nConditional);

        /// <summary>
        /// Returns a copy of the range (nBeginIndex, nEndIndex) inclusive of jArray.
        /// Negative nEndIndex values count from the other end.
        /// Out-of-bound values are clamped to the array range.
        /// Examples:
        ///  json a = JsonParse("[0, 1, 2, 3, 4]");
        ///  JsonArrayGetRange(a, 0, 1)    // => [0, 1]
        ///  JsonArrayGetRange(a, 1, -1)   // => [1, 2, 3, 4]
        ///  JsonArrayGetRange(a, 0, 4)    // => [0, 1, 2, 3, 4]
        ///  JsonArrayGetRange(a, 0, 999)  // => [0, 1, 2, 3, 4]
        ///  JsonArrayGetRange(a, 1, 0)    // => []
        ///  JsonArrayGetRange(a, 1, 1)    // => [1]
        /// Returns a null type on error, including type mismatches.
        /// </summary>
        public static Json JsonArrayGetRange(Json jArray, int nBeginIndex, int nEndIndex) => _service.JsonArrayGetRange(jArray, nBeginIndex, nEndIndex);

        /// <summary>
        /// Returns the result of a set operation on two arrays.
        /// Operations:
        /// JSON_SET_SUBSET (v <= o):
        ///   Returns true if every element in jValue is also in jOther.
        /// JSON_SET_UNION (v | o):
        ///   Returns a new array containing values from both sides.
        /// JSON_SET_INTERSECT (v & o):
        ///   Returns a new array containing only values common to both sides.
        /// JSON_SET_DIFFERENCE (v - o):
        ///   Returns a new array containing only values not in jOther.
        /// JSON_SET_SYMMETRIC_DIFFERENCE (v ^ o):
        ///   Returns a new array containing all elements present in either array, but not both.
        /// </summary>
        public static Json JsonSetOp(Json jValue, JsonSetType nOp, Json jOther) => _service.JsonSetOp(jValue, nOp, jOther);

        /// <summary>
        /// Applies sRegExp on sValue, returning an array containing all matching groups.
        /// * The regexp is not bounded by default (so /t/ will match "test").
        /// * A matching result with always return a JSON_ARRAY with the full match as the first element.
        /// * All matching groups will be returned as additional elements, depth-first.
        /// * A non-matching result will return a empty JSON_ARRAY.
        /// * If there was an error, the function will return JSON_NULL, with a error string filled in.
        /// * nSyntaxFlags is a mask of REGEXP_*
        /// * nMatchFlags is a mask of REGEXP_MATCH_* and REGEXP_FORMAT_*.
        /// Examples:
        /// * RegExpMatch("[", "test value")             -> null (error: "The expression contained mismatched [ and ].")
        /// * RegExpMatch("nothing", "test value")       -> []
        /// * RegExpMatch("^test", "test value")         -> ["test"]
        /// * RegExpMatch("^(test) (.+)$", "test value") -> ["test value", "test", "value"]
        /// </summary>
        public static Json RegExpMatch(
            string sRegExp,
            string sValue,
            RegularExpressionType nSyntaxFlags = RegularExpressionType.Ecmascript,
            RegularExpressionFormatType nMatchFlags = RegularExpressionFormatType.Default) => _service.RegExpMatch(sRegExp, sValue, nSyntaxFlags, nMatchFlags);

        /// <summary>
        /// Iterates the value with the regular expression.
        /// Returns an array of arrays; where each sub-array contains first the full match and then all matched groups.
        /// Returns empty JSON_ARRAY if no matches are found.
        /// If there was an error, the function will return JSON_NULL, with an error string filled in.
        /// Example: RegExpIterate("(\\d)(\\S+)", "1i 2am 3 4asentence"); -> [["1i", "1", "i"], ["2am", "2", "am"], ["4sentence", "4", "sentence"]]
        /// </summary>
        /// <param name="sRegExp">The regular expression pattern</param>
        /// <param name="sValue">The value to iterate over</param>
        /// <param name="nSyntaxFlags">A mask of REGEXP_* flags (defaults to Ecmascript)</param>
        /// <param name="nMatchFlags">A mask of REGEXP_MATCH_* and REGEXP_FORMAT_* flags (defaults to Default)</param>
        /// <returns>A JSON array of match arrays, or JSON_NULL on error</returns>
        public static Json RegExpIterate(
            string sRegExp,
            string sValue,
            RegularExpressionType nSyntaxFlags = RegularExpressionType.Ecmascript,
            RegularExpressionFormatType nMatchFlags = RegularExpressionFormatType.Default) => _service.RegExpIterate(sRegExp, sValue, nSyntaxFlags, nMatchFlags);

        /// <summary>
        /// Maths operation: absolute value of the value.
        /// </summary>
        /// <param name="fValue">The value to get the absolute value of</param>
        /// <returns>The absolute value</returns>
        public static float fabs(float fValue) => _service.fabs(fValue);

        /// <summary>
        /// Maths operation: cosine of the value.
        /// </summary>
        /// <param name="fValue">The value to get the cosine of</param>
        /// <returns>The cosine value</returns>
        public static float cos(float fValue) => _service.cos(fValue);

        /// <summary>
        /// Maths operation: sine of the value.
        /// </summary>
        /// <param name="fValue">The value to get the sine of</param>
        /// <returns>The sine value</returns>
        public static float sin(float fValue) => _service.sin(fValue);

        /// <summary>
        /// Maths operation: tangent of the value.
        /// </summary>
        /// <param name="fValue">The value to get the tangent of</param>
        /// <returns>The tangent value</returns>
        public static float tan(float fValue) => _service.tan(fValue);

        /// <summary>
        /// Maths operation: arccosine of the value.
        /// </summary>
        /// <param name="fValue">The value to get the arccosine of</param>
        /// <returns>The arccosine value, or zero if fValue > 1 or fValue < -1</returns>
        public static float acos(float fValue) => _service.acos(fValue);

        /// <summary>
        /// Maths operation: arcsine of the value.
        /// </summary>
        /// <param name="fValue">The value to get the arcsine of</param>
        /// <returns>The arcsine value, or zero if fValue > 1 or fValue < -1</returns>
        public static float asin(float fValue) => _service.asin(fValue);

        /// <summary>
        /// Maths operation: arctangent of the value.
        /// </summary>
        /// <param name="fValue">The value to get the arctangent of</param>
        /// <returns>The arctangent value</returns>
        public static float atan(float fValue) => _service.atan(fValue);

        /// <summary>
        /// Maths operation: logarithm of the value.
        /// </summary>
        /// <param name="fValue">The value to get the logarithm of</param>
        /// <returns>The logarithm value, or zero if fValue <= zero</returns>
        public static float log(float fValue) => _service.log(fValue);

        /// <summary>
        /// Maths operation: the value is raised to the power of the exponent.
        /// </summary>
        /// <param name="fValue">The base value</param>
        /// <param name="fExponent">The exponent</param>
        /// <returns>The result, or zero if fValue == 0 and fExponent < 0</returns>
        public static float pow(float fValue, float fExponent) => _service.pow(fValue, fExponent);

        /// <summary>
        /// Maths operation: square root of the value.
        /// </summary>
        /// <param name="fValue">The value to get the square root of</param>
        /// <returns>The square root, or zero if fValue < 0</returns>
        public static float sqrt(float fValue) => _service.sqrt(fValue);

        /// <summary>
        /// Maths operation: integer absolute value of the value.
        /// </summary>
        /// <param name="nValue">The value to get the absolute value of</param>
        /// <returns>The absolute value, or 0 on error</returns>
        public static int abs(int nValue) => _service.abs(nValue);

        /// <summary>
        /// Normalizes the vector.
        /// </summary>
        /// <param name="vVector">The vector to normalize</param>
        /// <returns>The normalized vector</returns>
        public static Vector3 VectorNormalize(Vector3 vVector) => _service.VectorNormalize(vVector);

        /// <summary>
        /// Gets the magnitude of the vector; this can be used to determine the
        /// distance between two points.
        /// </summary>
        /// <param name="vVector">The vector to get the magnitude of</param>
        /// <returns>The magnitude, or 0.0f on error</returns>
        public static float VectorMagnitude(Vector3 vVector) => _service.VectorMagnitude(vVector);

        /// <summary>
        /// Converts feet into a number of meters.
        /// </summary>
        /// <param name="fFeet">The feet value to convert</param>
        /// <returns>The equivalent value in meters</returns>
        public static float FeetToMeters(float fFeet) => _service.FeetToMeters(fFeet);

        /// <summary>
        /// Converts yards into a number of meters.
        /// </summary>
        /// <param name="fYards">The yards value to convert</param>
        /// <returns>The equivalent value in meters</returns>
        public static float YardsToMeters(float fYards) => _service.YardsToMeters(fYards);

        /// <summary>
        /// Gets the distance from the source object to the target object in metres.
        /// </summary>
        /// <param name="oObject">The object to get the distance to</param>
        /// <param name="oFrom">The object to measure distance from (defaults to OBJECT_SELF)</param>
        /// <returns>The distance in metres, or -1.0f on error</returns>
        public static float GetDistanceToObject(uint oObject, uint oFrom = NWScriptService.OBJECT_INVALID) => _service.GetDistanceToObject(oObject, oFrom);

        /// <summary>
        /// Returns whether or not there is a direct line of sight
        /// between the two objects. (Not blocked by any geometry).
        /// PLEASE NOTE: This is an expensive function and may
        /// degrade performance if used frequently.
        /// </summary>
        /// <param name="oSource">The source object</param>
        /// <param name="oTarget">The target object</param>
        /// <returns>TRUE if there is line of sight</returns>
        public static bool LineOfSightObject(uint oSource, uint oTarget) => _service.LineOfSightObject(oSource, oTarget);

        /// <summary>
        /// Returns whether or not there is a direct line of sight
        /// between the two vectors. (Not blocked by any geometry).
        /// This function must be run on a valid object in the area
        /// it will not work on the module or area.
        /// PLEASE NOTE: This is an expensive function and may
        /// degrade performance if used frequently.
        /// </summary>
        /// <param name="vSource">The source vector</param>
        /// <param name="vTarget">The target vector</param>
        /// <returns>TRUE if there is line of sight</returns>
        public static bool LineOfSightVector(Vector3 vSource, Vector3 vTarget) => _service.LineOfSightVector(vSource, vTarget);

        /// <summary>
        /// Converts the angle to a vector.
        /// </summary>
        /// <param name="fAngle">The angle to convert</param>
        /// <returns>The vector representation of the angle</returns>
        public static Vector3 AngleToVector(float fAngle) => _service.AngleToVector(fAngle);

        /// <summary>
        /// Converts the vector to an angle.
        /// </summary>
        /// <param name="vVector">The vector to convert</param>
        /// <returns>The angle representation of the vector</returns>
        public static float VectorToAngle(Vector3 vVector) => _service.VectorToAngle(vVector);

        /// <summary>
        /// Gets the total from rolling the specified number of d2 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d2(int nNumDice = 1) => _service.d2(nNumDice);

        /// <summary>
        /// Gets the total from rolling the specified number of d3 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d3(int nNumDice = 1) => _service.d3(nNumDice);

        /// <summary>
        /// Gets the total from rolling the specified number of d4 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d4(int nNumDice = 1) => _service.d4(nNumDice);

        /// <summary>
        /// Gets the total from rolling the specified number of d6 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d6(int nNumDice = 1) => _service.d6(nNumDice);

        /// <summary>
        /// Gets the total from rolling the specified number of d8 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d8(int nNumDice = 1) => _service.d8(nNumDice);

        /// <summary>
        /// Gets the total from rolling the specified number of d10 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d10(int nNumDice = 1) => _service.d10(nNumDice);

        /// <summary>
        /// Gets the total from rolling the specified number of d12 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d12(int nNumDice = 1) => _service.d12(nNumDice);

        /// <summary>
        /// Gets the total from rolling the specified number of d20 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d20(int nNumDice = 1) => _service.d20(nNumDice);

        /// <summary>
        /// Gets the total from rolling the specified number of d100 dice.
        /// If nNumDice is less than 1, the value 1 will be used.
        /// </summary>
        /// <param name="nNumDice">The number of dice to roll (defaults to 1)</param>
        /// <returns>The total from rolling the dice</returns>
        public static int d100(int nNumDice = 1) => _service.d100(nNumDice);

        /// <summary>
        /// Outputs the vector to the logfile.
        /// </summary>
        /// <param name="vVector">The vector to output</param>
        /// <param name="bPrepend">If TRUE, the message will be prefixed with "PRINTVECTOR:" (defaults to false)</param>
        public static void PrintVector(Vector3 vVector, bool bPrepend = false) => _service.PrintVector(vVector, bPrepend);

        /// <summary>
        /// Creates a vector with the specified values for x, y and z.
        /// </summary>
        /// <param name="x">The x component (defaults to 0.0f)</param>
        /// <param name="y">The y component (defaults to 0.0f)</param>
        /// <param name="z">The z component (defaults to 0.0f)</param>
        /// <returns>The created vector</returns>
        public static Vector3 Vector3(float x = 0.0f, float y = 0.0f, float z = 0.0f) => _service.Vector3(x, y, z);

        /// <summary>
        /// Converts the integer into a floating point number.
        /// </summary>
        /// <param name="nInteger">The integer to convert</param>
        /// <returns>The floating point number</returns>
        public static float IntToFloat(int nInteger) => _service.IntToFloat(nInteger);

        /// <summary>
        /// Converts the float into the nearest integer.
        /// </summary>
        /// <param name="fFloat">The float to convert</param>
        /// <returns>The nearest integer</returns>
        public static int FloatToInt(float fFloat) => _service.FloatToInt(fFloat);

        /// <summary>
        /// Converts the string into an integer.
        /// </summary>
        /// <param name="sNumber">The string to convert</param>
        /// <returns>The integer value</returns>
        public static int StringToInt(string sNumber) => _service.StringToInt(sNumber);

        /// <summary>
        /// Converts the string into a floating point number.
        /// </summary>
        /// <param name="sNumber">The string to convert</param>
        /// <returns>The floating point number</returns>
        public static float StringToFloat(string sNumber) => _service.StringToFloat(sNumber);

        /// <summary>
        /// Stores a float value to the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="flFloat">The float value to store</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        public static void SetCampaignFloat(string sCampaignName, string sVarName, float flFloat,
            uint oPlayer = NWScriptService.OBJECT_INVALID) => _service.SetCampaignFloat(sCampaignName, sVarName, flFloat, oPlayer);

        /// <summary>
        /// Stores an integer value to the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="nInt">The integer value to store</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        public static void SetCampaignInt(string sCampaignName, string sVarName, int nInt,
            uint oPlayer = NWScriptService.OBJECT_INVALID) => _service.SetCampaignInt(sCampaignName, sVarName, nInt, oPlayer);

        /// <summary>
        /// Stores a vector value to the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="vVector">The vector value to store</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        public static void SetCampaignVector(string sCampaignName, string sVarName, Vector3 vVector,
            uint oPlayer = NWScriptService.OBJECT_INVALID) => _service.SetCampaignVector(sCampaignName, sVarName, vVector, oPlayer);

        /// <summary>
        /// Stores a location value to the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="locLocation">The location value to store</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        public static void SetCampaignLocation(string sCampaignName, string sVarName, Location locLocation,
            uint oPlayer = NWScriptService.OBJECT_INVALID) => _service.SetCampaignLocation(sCampaignName, sVarName, locLocation, oPlayer);

        /// <summary>
        /// Stores a string value to the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="sString">The string value to store</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        public static void SetCampaignString(string sCampaignName, string sVarName, string sString,
            uint oPlayer = NWScriptService.OBJECT_INVALID) => _service.SetCampaignString(sCampaignName, sVarName, sString, oPlayer);

        /// <summary>
        /// Deletes the entire campaign database if it exists.
        /// </summary>
        /// <param name="sCampaignName">The name of the campaign database to delete</param>
        public static void DestroyCampaignDatabase(string sCampaignName) => _service.DestroyCampaignDatabase(sCampaignName);

        /// <summary>
        /// Reads a float value from the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <returns>The float value from the database</returns>
        public static float GetCampaignFloat(string sCampaignName, string sVarName, uint oPlayer = NWScriptService.OBJECT_INVALID) => _service.GetCampaignFloat(sCampaignName, sVarName, oPlayer);

        /// <summary>
        /// Reads an integer value from the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <returns>The integer value from the database</returns>
        public static int GetCampaignInt(string sCampaignName, string sVarName, uint oPlayer = NWScriptService.OBJECT_INVALID) => _service.GetCampaignInt(sCampaignName, sVarName, oPlayer);

        /// <summary>
        /// Reads a vector value from the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <returns>The vector value from the database</returns>
        public static Vector3 GetCampaignVector(string sCampaignName, string sVarName, uint oPlayer = NWScriptService.OBJECT_INVALID) => _service.GetCampaignVector(sCampaignName, sVarName, oPlayer);

        /// <summary>
        /// Reads a location value from the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <returns>The location value from the database</returns>
        public static Location GetCampaignLocation(string sCampaignName, string sVarName, uint oPlayer = NWScriptService.OBJECT_INVALID) => _service.GetCampaignLocation(sCampaignName, sVarName, oPlayer);

        /// <summary>
        /// Reads a string value from the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case sensitive, must be the same for both set and get functions)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <returns>The string value from the database</returns>
        public static string GetCampaignString(string sCampaignName, string sVarName, uint oPlayer = NWScriptService.OBJECT_INVALID) => _service.GetCampaignString(sCampaignName, sVarName, oPlayer);

        /// <summary>
        /// Removes any campaign variable regardless of type.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name</param>
        /// <param name="sVarName">The variable name to delete</param>
        /// <param name="oPlayer">If the variable pertains to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <remarks>By normal database standards, deleting does not actually remove the entry from the database, but flags it as deleted. Do not expect the database files to shrink in size from this command. If you want to 'pack' the database, you will have to do it externally from the game.</remarks>
        public static void DeleteCampaignVariable(string sCampaignName, string sVarName, uint oPlayer = NWScriptService.OBJECT_INVALID) => _service.DeleteCampaignVariable(sCampaignName, sVarName, oPlayer);

        /// <summary>
        /// Stores an object with the given ID in the campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name</param>
        /// <param name="sVarName">The variable name to store the object under</param>
        /// <param name="oObject">The object to store (can only be used for Creatures and Items)</param>
        /// <param name="oPlayer">If the object pertains to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <param name="bSaveObjectState">If true, local vars, effects, action queue, and transition info (triggers, doors) are saved out (except for Combined Area Format, which always has object state saved out) (default: false)</param>
        /// <returns>0 if it failed, 1 if it worked</returns>
        public static int StoreCampaignObject(string sCampaignName, string sVarName, uint oObject, uint oPlayer = NWScriptService.OBJECT_INVALID, bool bSaveObjectState = false) => _service.StoreCampaignObject(sCampaignName, sVarName, oObject, oPlayer, bSaveObjectState);

        /// <summary>
        /// Retrieves a campaign object with the given ID and restores it.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name</param>
        /// <param name="sVarName">The variable name to retrieve the object from</param>
        /// <param name="locLocation">The location where the object will be created</param>
        /// <param name="oOwner">If specified, the object will try to be created in their repository. If the owner can't handle the item (or if it's a creature) it will be created on the ground (default: OBJECT_INVALID)</param>
        /// <param name="oPlayer">If the object pertains to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <param name="bLoadObjectState">If true, local vars, effects, action queue, and transition info (triggers, doors) are read in (default: false)</param>
        /// <returns>The retrieved object</returns>
        public static uint RetrieveCampaignObject(string sCampaignName, string sVarName, Location locLocation, uint oOwner = NWScriptService.OBJECT_INVALID, uint oPlayer = NWScriptService.OBJECT_INVALID, bool bLoadObjectState = false) => _service.RetrieveCampaignObject(sCampaignName, sVarName, locLocation, oOwner, oPlayer, bLoadObjectState);

        /// <summary>
        /// Stores a JSON value to the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case insensitive, must be the same for both set and get functions, can only contain alphanumeric characters, no spaces)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="jValue">The JSON value to store</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        public static void SetCampaignJson(string sCampaignName, string sVarName, Json jValue, uint oPlayer = NWScriptService.OBJECT_INVALID) => _service.SetCampaignJson(sCampaignName, sVarName, jValue, oPlayer);

        /// <summary>
        /// Reads a JSON value from the specified campaign database.
        /// </summary>
        /// <param name="sCampaignName">The campaign database name (case insensitive, must be the same for both set and get functions, can only contain alphanumeric characters, no spaces)</param>
        /// <param name="sVarName">The variable name (must be unique across the entire database, regardless of variable type)</param>
        /// <param name="oPlayer">If you want a variable to pertain to a specific player, provide a player object (default: OBJECT_INVALID)</param>
        /// <returns>The JSON value from the database</returns>
        public static Json GetCampaignJson(string sCampaignName, string sVarName, uint oPlayer = NWScriptService.OBJECT_INVALID) => _service.GetCampaignJson(sCampaignName, sVarName, oPlayer);

        /// <summary>
        /// Gets a random integer between 0 and nMaxInteger-1.
        /// </summary>
        /// <param name="nMaxInteger">The maximum integer value (exclusive)</param>
        /// <returns>A random integer between 0 and nMaxInteger-1. Returns 0 on error</returns>
        public static int Random(int nMaxInteger) => _service.Random(nMaxInteger);

        /// <summary>
        /// Gets the module object.
        /// </summary>
        /// <returns>The module object. Returns OBJECT_INVALID on error</returns>
        public static uint GetModule() => _service.GetModule();

        /// <summary>
        /// Outputs a string to the log file.
        /// </summary>
        /// <param name="sString">The string to output to the log</param>
        public static void PrintString(string sString) => _service.PrintString(sString);

        /// <summary>
        /// Outputs a formatted float to the log file.
        /// </summary>
        /// <param name="fFloat">The float value to output</param>
        /// <param name="nWidth">The width should be a value from 0 to 18 inclusive (default: 18)</param>
        /// <param name="nDecimals">The number of decimals should be a value from 0 to 9 inclusive (default: 9)</param>
        public static void PrintFloat(float fFloat, int nWidth = 18, int nDecimals = 9) => _service.PrintFloat(fFloat, nWidth, nDecimals);

        /// <summary>
        /// Outputs an integer to the log file.
        /// </summary>
        /// <param name="nInteger">The integer to output to the log</param>
        public static void PrintInteger(int nInteger) => _service.PrintInteger(nInteger);

        /// <summary>
        /// Outputs the object's ID to the log file.
        /// </summary>
        /// <param name="oObject">The object whose ID to output to the log</param>
        public static void PrintObject(uint oObject) => _service.PrintObject(oObject);

        /// <summary>
        /// Gets the attack bonus limit.
        /// The default value is 20.
        /// </summary>
        /// <returns>The attack bonus limit</returns>
        public static int GetAttackBonusLimit() => _service.GetAttackBonusLimit();

        /// <summary>
        /// Gets the damage bonus limit.
        /// The default value is 100.
        /// </summary>
        /// <returns>The damage bonus limit</returns>
        public static int GetDamageBonusLimit() => _service.GetDamageBonusLimit();

        /// <summary>
        /// Gets the saving throw bonus limit.
        /// The default value is 20.
        /// </summary>
        /// <returns>The saving throw bonus limit</returns>
        public static int GetSavingThrowBonusLimit() => _service.GetSavingThrowBonusLimit();

        /// <summary>
        /// Gets the ability bonus limit.
        /// The default value is 12.
        /// </summary>
        /// <returns>The ability bonus limit</returns>
        public static int GetAbilityBonusLimit() => _service.GetAbilityBonusLimit();

        /// <summary>
        /// Gets the ability penalty limit.
        /// The default value is 30.
        /// </summary>
        /// <returns>The ability penalty limit</returns>
        public static int GetAbilityPenaltyLimit() => _service.GetAbilityPenaltyLimit();

        /// <summary>
        /// Gets the skill bonus limit.
        /// The default value is 50.
        /// </summary>
        /// <returns>The skill bonus limit</returns>
        public static int GetSkillBonusLimit() => _service.GetSkillBonusLimit();

        /// <summary>
        /// Sets the attack bonus limit.
        /// The minimum value is 0.
        /// </summary>
        /// <param name="nNewLimit">The new attack bonus limit</param>
        public static void SetAttackBonusLimit(int nNewLimit) => _service.SetAttackBonusLimit(nNewLimit);

        /// <summary>
        /// Sets the damage bonus limit.
        /// The minimum value is 0.
        /// </summary>
        /// <param name="nNewLimit">The new damage bonus limit</param>
        public static void SetDamageBonusLimit(int nNewLimit) => _service.SetDamageBonusLimit(nNewLimit);

        /// <summary>
        /// Sets the saving throw bonus limit.
        /// The minimum value is 0.
        /// </summary>
        /// <param name="nNewLimit">The new saving throw bonus limit</param>
        public static void SetSavingThrowBonusLimit(int nNewLimit) => _service.SetSavingThrowBonusLimit(nNewLimit);

        /// <summary>
        /// Sets the ability bonus limit.
        /// The minimum value is 0.
        /// </summary>
        /// <param name="nNewLimit">The new ability bonus limit</param>
        public static void SetAbilityBonusLimit(int nNewLimit) => _service.SetAbilityBonusLimit(nNewLimit);

        /// <summary>
        /// Sets the ability penalty limit.
        /// The minimum value is 0.
        /// </summary>
        /// <param name="nNewLimit">The new ability penalty limit</param>
        public static void SetAbilityPenaltyLimit(int nNewLimit) => _service.SetAbilityPenaltyLimit(nNewLimit);

        /// <summary>
        /// Sets the skill bonus limit.
        /// The minimum value is 0.
        /// </summary>
        /// <param name="nNewLimit">The new skill bonus limit</param>
        public static void SetSkillBonusLimit(int nNewLimit) => _service.SetSkillBonusLimit(nNewLimit);

        /// <summary>
        /// Gets the object's local integer variable.
        /// </summary>
        /// <param name="oObject">The object to get the variable from</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <returns>The integer value, or 0 on error</returns>
        public static int GetLocalInt(uint oObject, string sVarName) => _service.GetLocalInt(oObject, sVarName);

        /// <summary>
        /// Gets the object's local boolean variable.
        /// </summary>
        /// <param name="oObject">The object to get the variable from</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <returns>The boolean value, or false on error</returns>
        public static bool GetLocalBool(uint oObject, string sVarName) => _service.GetLocalBool(oObject, sVarName);

        /// <summary>
        /// Gets the object's local float variable.
        /// </summary>
        /// <param name="oObject">The object to get the variable from</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <returns>The float value, or 0.0f on error</returns>
        public static float GetLocalFloat(uint oObject, string sVarName) => _service.GetLocalFloat(oObject, sVarName);

        /// <summary>
        /// Gets the object's local string variable.
        /// </summary>
        /// <param name="oObject">The object to get the variable from</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <returns>The string value, or empty string on error</returns>
        public static string GetLocalString(uint oObject, string sVarName) => _service.GetLocalString(oObject, sVarName);

        /// <summary>
        /// Gets the object's local object variable.
        /// </summary>
        /// <param name="oObject">The object to get the variable from</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <returns>The object value, or OBJECT_INVALID on error</returns>
        public static uint GetLocalObject(uint oObject, string sVarName) => _service.GetLocalObject(oObject, sVarName);

        /// <summary>
        /// Sets the object's local integer variable.
        /// </summary>
        /// <param name="oObject">The object to set the variable on</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <param name="nValue">The integer value to set</param>
        public static void SetLocalInt(uint oObject, string sVarName, int nValue) => _service.SetLocalInt(oObject, sVarName, nValue);

        /// <summary>
        /// Sets the object's local boolean variable.
        /// </summary>
        /// <param name="oObject">The object to set the variable on</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <param name="nValue">The boolean value to set</param>
        public static void SetLocalBool(uint oObject, string sVarName, bool nValue) => _service.SetLocalBool(oObject, sVarName, nValue);

        /// <summary>
        /// Sets the object's local float variable.
        /// </summary>
        /// <param name="oObject">The object to set the variable on</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <param name="fValue">The float value to set</param>
        public static void SetLocalFloat(uint oObject, string sVarName, float fValue) => _service.SetLocalFloat(oObject, sVarName, fValue);

        /// <summary>
        /// Sets the object's local string variable.
        /// </summary>
        /// <param name="oObject">The object to set the variable on</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <param name="sValue">The string value to set</param>
        public static void SetLocalString(uint oObject, string sVarName, string sValue) => _service.SetLocalString(oObject, sVarName, sValue);

        /// <summary>
        /// Sets the object's local object variable.
        /// </summary>
        /// <param name="oObject">The object to set the variable on</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <param name="oValue">The object value to set</param>
        public static void SetLocalObject(uint oObject, string sVarName, uint oValue) => _service.SetLocalObject(oObject, sVarName, oValue);

        /// <summary>
        /// Sets the object's local location variable.
        /// </summary>
        /// <param name="oObject">The object to set the variable on</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <param name="lValue">The location value to set</param>
        public static void SetLocalLocation(uint oObject, string sVarName, Location lValue) => _service.SetLocalLocation(oObject, sVarName, lValue);

        /// <summary>
        /// Gets the object's local location variable.
        /// </summary>
        /// <param name="oObject">The object to get the variable from</param>
        /// <param name="sVarName">The name of the variable</param>
        /// <returns>The location value</returns>
        public static Location GetLocalLocation(uint oObject, string sVarName) => _service.GetLocalLocation(oObject, sVarName);

        /// <summary>
        /// Deletes the object's local integer variable.
        /// </summary>
        /// <param name="oObject">The object to delete the variable from</param>
        /// <param name="sVarName">The name of the variable to delete</param>
        public static void DeleteLocalInt(uint oObject, string sVarName) => _service.DeleteLocalInt(oObject, sVarName);

        /// <summary>
        /// Deletes the object's local boolean variable.
        /// </summary>
        /// <param name="oObject">The object to delete the variable from</param>
        /// <param name="sVarName">The name of the variable to delete</param>
        public static void DeleteLocalBool(uint oObject, string sVarName) => _service.DeleteLocalBool(oObject, sVarName);

        /// <summary>
        /// Deletes the object's local float variable.
        /// </summary>
        /// <param name="oObject">The object to delete the variable from</param>
        /// <param name="sVarName">The name of the variable to delete</param>
        public static void DeleteLocalFloat(uint oObject, string sVarName) => _service.DeleteLocalFloat(oObject, sVarName);

        /// <summary>
        /// Deletes the object's local string variable.
        /// </summary>
        /// <param name="oObject">The object to delete the variable from</param>
        /// <param name="sVarName">The name of the variable to delete</param>
        public static void DeleteLocalString(uint oObject, string sVarName) => _service.DeleteLocalString(oObject, sVarName);

        /// <summary>
        /// Deletes the object's local object variable.
        /// </summary>
        /// <param name="oObject">The object to delete the variable from</param>
        /// <param name="sVarName">The name of the variable to delete</param>
        public static void DeleteLocalObject(uint oObject, string sVarName) => _service.DeleteLocalObject(oObject, sVarName);

        /// <summary>
        /// Deletes the object's local location variable.
        /// </summary>
        /// <param name="oObject">The object to delete the variable from</param>
        /// <param name="sVarName">The name of the variable to delete</param>
        public static void DeleteLocalLocation(uint oObject, string sVarName) => _service.DeleteLocalLocation(oObject, sVarName);

        /// <summary>
        /// Returns the creature's spell school specialization in the specified class.
        /// </summary>
        /// <param name="creature">The creature to get the specialization for</param>
        /// <param name="playerClass">The class to get the specialization for</param>
        /// <returns>The spell school specialization (SPELL_SCHOOL_* constants). Returns -1 on error</returns>
        /// <remarks>Unless custom content is used, only Wizards have spell schools.</remarks>
        public static SpellSchool GetSpecialization(uint creature, ClassType playerClass) => _service.GetSpecialization(creature, playerClass);

        /// <summary>
        /// Returns the creature's domain in the specified class.
        /// </summary>
        /// <param name="creature">The creature to get the domain for</param>
        /// <param name="DomainIndex">The domain index (1 or 2) (default: 1)</param>
        /// <param name="playerClass">The class to get the domain for (default: ClassType.Cleric)</param>
        /// <returns>The domain (DOMAIN_* constants). Returns -1 on error</returns>
        /// <remarks>Unless custom content is used, only Clerics have domains.</remarks>
        public static ClericDomainType GetDomain(uint creature, int DomainIndex = 1, ClassType playerClass = ClassType.Cleric) => _service.GetDomain(creature, DomainIndex, playerClass);

        /// <summary>
        /// Gets a value from a 2DA file on the server and returns it as a string.
        /// </summary>
        /// <param name="s2DA">The name of the 2da file (16 chars max)</param>
        /// <param name="sColumn">The name of the column in the 2da</param>
        /// <param name="nRow">The row in the 2da</param>
        /// <returns>The value as a string. Returns an empty string if file, row, or column not found</returns>
        /// <remarks>Avoid using this function in loops.</remarks>
        public static string Get2DAString(string s2DA, string sColumn, int nRow) => _service.Get2DAString(s2DA, sColumn, nRow);

        /// <summary>
        /// Returns the column name of the 2DA file at the specified column index.
        /// </summary>
        /// <param name="s2DA">The name of the 2da file</param>
        /// <param name="nColumnIdx">The column index (starting at 0)</param>
        /// <returns>The column name. Returns empty string if column doesn't exist (at end)</returns>
        public static string Get2DAColumn(string s2DA, int nColumnIdx) => _service.Get2DAColumn(s2DA, nColumnIdx);

        /// <summary>
        /// Returns the number of defined rows in the specified 2DA file.
        /// </summary>
        /// <param name="s2DA">The name of the 2da file</param>
        /// <returns>The number of defined rows in the 2DA file</returns>
        public static int Get2DARowCount(string s2DA) => _service.Get2DARowCount(s2DA);

        /// <summary>
        /// Returns TRUE if a specific key is required to open the lock on the object.
        /// </summary>
        /// <param name="oObject">The object to check</param>
        /// <returns>TRUE if a key is required</returns>
        public static bool GetLockKeyRequired(uint oObject) => _service.GetLockKeyRequired(oObject);

        /// <summary>
        /// Gets the tag of the key that will open the lock on the object.
        /// </summary>
        /// <param name="oObject">The object to get the key tag for</param>
        /// <returns>The key tag</returns>
        public static string GetLockKeyTag(uint oObject) => _service.GetLockKeyTag(oObject);

        /// <summary>
        /// Returns TRUE if the lock on the object is lockable.
        /// </summary>
        /// <param name="oObject">The object to check</param>
        /// <returns>TRUE if the lock is lockable</returns>
        public static bool GetLockLockable(uint oObject) => _service.GetLockLockable(oObject);

        /// <summary>
        /// Gets the DC for unlocking the object.
        /// </summary>
        /// <param name="oObject">The object to get the unlock DC for</param>
        /// <returns>The unlock DC</returns>
        public static int GetLockUnlockDC(uint oObject) => _service.GetLockUnlockDC(oObject);

        /// <summary>
        /// Gets the DC for locking the object.
        /// </summary>
        /// <param name="oObject">The object to get the lock DC for</param>
        /// <returns>The lock DC</returns>
        public static int GetLockLockDC(uint oObject) => _service.GetLockLockDC(oObject);

        /// <summary>
        /// Queries the current value of the appearance settings on an item.
        /// </summary>
        /// <param name="oItem">The item to query</param>
        /// <param name="nType">The appearance type</param>
        /// <param name="nIndex">The appearance index</param>
        /// <returns>The appearance value</returns>
        /// <remarks>The parameters are identical to those of CopyItemAndModify().</remarks>
        public static int GetItemAppearance(uint oItem, ItemModelColorType nType, int nIndex) => _service.GetItemAppearance(oItem, nType, nIndex);

        /// <summary>
        /// Returns the stack size of an item.
        /// </summary>
        /// <param name="oItem">The item to query</param>
        /// <returns>The stack size of the item</returns>
        public static int GetItemStackSize(uint oItem) => _service.GetItemStackSize(oItem);

        /// <summary>
        /// Sets the stack size of an item.
        /// </summary>
        /// <param name="oItem">The item to change</param>
        /// <param name="nSize">The new size of stack</param>
        /// <remarks>Will be restricted to be between 1 and the maximum stack size for the item type. If a value less than 1 is passed it will set the stack to 1. If a value greater than the max is passed then it will set the stack to the maximum size.</remarks>
        public static void SetItemStackSize(uint oItem, int nSize) => _service.SetItemStackSize(oItem, nSize);

        /// <summary>
        /// Returns the charges left on an item.
        /// </summary>
        /// <param name="oItem">The item to query</param>
        /// <returns>The number of charges left</returns>
        public static int GetItemCharges(uint oItem) => _service.GetItemCharges(oItem);

        /// <summary>
        /// Sets the charges left on an item.
        /// </summary>
        /// <param name="oItem">The item to change</param>
        /// <param name="nCharges">The number of charges</param>
        /// <remarks>If value below 0 is passed, charges will be set to 0. If value greater than maximum is passed, charges will be set to maximum. If the charges drops to 0 the item will be destroyed.</remarks>
        public static void SetItemCharges(uint oItem, int nCharges) => _service.SetItemCharges(oItem, nCharges);

        /// <summary>
        /// Duplicates the item and returns a new object.
        /// </summary>
        /// <param name="oItem">The item to copy</param>
        /// <param name="oTargetInventory">Create item in this object's inventory. If this parameter is not valid, the item will be created in oItem's location (default: OBJECT_INVALID)</param>
        /// <param name="bCopyVars">Copy the local variables from the old item to the new one (default: false)</param>
        /// <returns>The new item. Returns OBJECT_INVALID for non-items</returns>
        /// <remarks>Can only copy empty item containers. Will return OBJECT_INVALID if oItem contains other items. If it is possible to merge this item with any others in the target location, then it will do so and return the merged object.</remarks>
        public static uint CopyItem(uint oItem, uint oTargetInventory = NWScriptService.OBJECT_INVALID, bool bCopyVars = false) => _service.CopyItem(oItem, oTargetInventory, bCopyVars);

        /// <summary>
        /// In an onItemAcquired script, returns the size of the stack of the item that was just acquired.
        /// </summary>
        /// <returns>The stack size of the item acquired</returns>
        public static int GetModuleItemAcquiredStackSize() => _service.GetModuleItemAcquiredStackSize();

        /// <summary>
        /// Gets the number of stacked items that the item comprises.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <returns>The number of stacked items</returns>
        public static int GetNumStackedItems(uint oItem) => _service.GetNumStackedItems(oItem);

        /// <summary>
        /// Sets whether the provided item should be hidden when equipped.
        /// </summary>
        /// <param name="oItem">The item to modify</param>
        /// <param name="nValue">Whether the item should be hidden when equipped</param>
        /// <remarks>The intended usage of this function is to provide an easy way to hide helmets, but it can be used equally for any slot which has creature mesh visibility when equipped, e.g.: armour, helm, cloak, left hand, and right hand.</remarks>
        public static void SetHiddenWhenEquipped(uint oItem, bool nValue) => _service.SetHiddenWhenEquipped(oItem, nValue);

        /// <summary>
        /// Returns whether the provided item is hidden when equipped.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <returns>1 if the item is hidden when equipped, 0 otherwise</returns>
        public static int GetHiddenWhenEquipped(uint oItem) => _service.GetHiddenWhenEquipped(oItem);

        /// <summary>
        /// Returns true if the item is flagged as infinite.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <returns>1 if the item is infinite, 0 otherwise</returns>
        /// <remarks>The infinite property affects the buying/selling behavior of the item in a store. An infinite item will still be available to purchase from a store after a player buys the item (non-infinite items will disappear from the store when purchased).</remarks>
        public static int GetInfiniteFlag(uint oItem) => _service.GetInfiniteFlag(oItem);

        /// <summary>
        /// Sets the Infinite flag on an item.
        /// </summary>
        /// <param name="oItem">The item to change</param>
        /// <param name="bInfinite">Whether the item should be infinite (default: true)</param>
        /// <remarks>The infinite property affects the buying/selling behavior of the item in a store. An infinite item will still be available to purchase from a store after a player buys the item (non-infinite items will disappear from the store when purchased).</remarks>
        public static void SetInfiniteFlag(uint oItem, bool bInfinite = true) => _service.SetInfiniteFlag(oItem, bInfinite);

        /// <summary>
        /// Sets whether this item is 'stolen' or not.
        /// </summary>
        /// <param name="oItem">The item to modify</param>
        /// <param name="nStolen">Whether the item is stolen</param>
        public static void SetStolenFlag(uint oItem, bool nStolen) => _service.SetStolenFlag(oItem, nStolen);

        /// <summary>
        /// Returns true if the item can be pickpocketed.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <returns>True if the item can be pickpocketed</returns>
        public static bool GetPickpocketableFlag(uint oItem) => _service.GetPickpocketableFlag(oItem);

        /// <summary>
        /// Sets the Pickpocketable flag on an item.
        /// </summary>
        /// <param name="oItem">The item to change</param>
        /// <param name="bPickpocketable">Whether the item can be pickpocketed</param>
        public static void SetPickpocketableFlag(uint oItem, bool bPickpocketable) => _service.SetPickpocketableFlag(oItem, bPickpocketable);

        /// <summary>
        /// Sets the droppable flag on an item.
        /// </summary>
        /// <param name="oItem">The item to change</param>
        /// <param name="bDroppable">Whether the item should be droppable</param>
        /// <remarks>Droppable items will appear on a creature's remains when the creature is killed.</remarks>
        public static void SetDroppableFlag(uint oItem, bool bDroppable) => _service.SetDroppableFlag(oItem, bDroppable);

        /// <summary>
        /// Returns true if the item can be dropped.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <returns>True if the item can be dropped</returns>
        /// <remarks>Droppable items will appear on a creature's remains when the creature is killed.</remarks>
        public static bool GetDroppableFlag(uint oItem) => _service.GetDroppableFlag(oItem);

        /// <summary>
        /// Returns true if the placeable object is usable.
        /// </summary>
        /// <param name="oObject">The object to check (default: OBJECT_SELF)</param>
        /// <returns>True if the object is usable</returns>
        public static bool GetUseableFlag(uint oObject = NWScriptService.OBJECT_INVALID) => _service.GetUseableFlag(oObject);

        /// <summary>
        /// Returns true if the item is stolen.
        /// </summary>
        /// <param name="oStolen">The item to check</param>
        /// <returns>True if the item is stolen</returns>
        public static bool GetStolenFlag(uint oStolen) => _service.GetStolenFlag(oStolen);

        /// <summary>
        /// Returns true if the item is a ranged weapon.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <returns>True if the item is a ranged weapon</returns>
        public static bool GetWeaponRanged(uint oItem) => _service.GetWeaponRanged(oItem);

        /// <summary>
        /// Use this in a spell script to get the item used to cast the spell.
        /// </summary>
        /// <returns>The item used to cast the spell</returns>
        public static uint GetSpellCastItem() => _service.GetSpellCastItem();

        /// <summary>
        /// Use this in an OnItemActivated module script to get the item that was activated.
        /// </summary>
        /// <returns>The item that was activated</returns>
        public static uint GetItemActivated() => _service.GetItemActivated();

        /// <summary>
        /// Use this in an OnItemActivated module script to get the creature that activated the item.
        /// </summary>
        /// <returns>The creature that activated the item</returns>
        public static uint GetItemActivator() => _service.GetItemActivator();

        /// <summary>
        /// Use this in an OnItemActivated module script to get the location of the item's target.
        /// </summary>
        /// <returns>The location of the item's target</returns>
        public static Location GetItemActivatedTargetLocation() => _service.GetItemActivatedTargetLocation();

        /// <summary>
        /// Use this in an OnItemActivated module script to get the item's target.
        /// </summary>
        /// <returns>The item's target</returns>
        public static uint GetItemActivatedTarget() => _service.GetItemActivatedTarget();

        /// <summary>
        /// Gets the Armor Class value of an item.
        /// </summary>
        /// <param name="oItem">The item to query</param>
        /// <returns>The Armor Class value, or 0 if the item is not valid or has no armor value</returns>
        public static int GetItemACValue(uint oItem) => _service.GetItemACValue(oItem);

        /// <summary>
        /// Gets the base item type of an item.
        /// </summary>
        /// <param name="oItem">The item to query</param>
        /// <returns>The base item type (BASE_ITEM_*), or BASE_ITEM_INVALID if the item is not valid</returns>
        public static BaseItemType GetBaseItemType(uint oItem) => _service.GetBaseItemType(oItem);

        /// <summary>
        /// Determines whether an item has a specific item property.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <param name="nProperty">The item property type to check for (ITEM_PROPERTY_*)</param>
        /// <returns>True if the item has the property, false if the item is not valid or does not have the property</returns>
        public static bool GetItemHasItemProperty(uint oItem, ItemPropertyType nProperty) => _service.GetItemHasItemProperty(oItem, nProperty);

        /// <summary>
        /// Gets the first item in a target's inventory to start cycling through items.
        /// </summary>
        /// <param name="oTarget">The target object to check inventory of (default: OBJECT_SELF)</param>
        /// <returns>The first item in the inventory, or OBJECT_INVALID if the target is not a creature, item, placeable, or store, or if no item is found</returns>
        public static uint GetFirstItemInInventory(uint oTarget = NWScriptService.OBJECT_INVALID) => _service.GetFirstItemInInventory(oTarget);

        /// <summary>
        /// Gets the next item in a target's inventory to continue cycling through items.
        /// </summary>
        /// <param name="oTarget">The target object to check inventory of (default: OBJECT_SELF)</param>
        /// <returns>The next item in the inventory, or OBJECT_INVALID if the target is not a creature, item, placeable, or store, or if no more items are found</returns>
        public static uint GetNextItemInInventory(uint oTarget = NWScriptService.OBJECT_INVALID) => _service.GetNextItemInInventory(oTarget);

        /// <summary>
        /// Determines whether an item has been identified.
        /// </summary>
        /// <param name="oItem">The item to check</param>
        /// <returns>True if the item has been identified, false otherwise</returns>
        public static bool GetIdentified(uint oItem) => _service.GetIdentified(oItem);

        /// <summary>
        /// Sets whether an item has been identified.
        /// </summary>
        /// <param name="oItem">The item to modify</param>
        /// <param name="bIdentified">Whether the item should be identified</param>
        public static void SetIdentified(uint oItem, bool bIdentified) => _service.SetIdentified(oItem, bIdentified);

        /// <summary>
        /// Gets the gold piece value of an item.
        /// </summary>
        /// <param name="oItem">The item to query</param>
        /// <returns>The gold piece value, or 0 if the item is not valid</returns>
        public static int GetGoldPieceValue(uint oItem) => _service.GetGoldPieceValue(oItem);

        /// <summary>
        /// Gets the item that was acquired in an OnItemAcquired script.
        /// </summary>
        /// <returns>The item that was acquired, or OBJECT_INVALID if the module is not valid</returns>
        public static uint GetModuleItemAcquired() => _service.GetModuleItemAcquired();

        /// <summary>
        /// Gets the creature that previously possessed the item in an OnItemAcquired script.
        /// </summary>
        /// <returns>The creature that previously possessed the item, or OBJECT_INVALID if the item was picked up from the ground</returns>
        public static uint GetModuleItemAcquiredFrom() => _service.GetModuleItemAcquiredFrom();

        /// <summary>
        /// Gets the object in a creature's specified inventory slot.
        /// </summary>
        /// <param name="nInventorySlot">The inventory slot to check (INVENTORY_SLOT_*)</param>
        /// <param name="oCreature">The creature to check (default: OBJECT_SELF)</param>
        /// <returns>The item in the specified slot, or OBJECT_INVALID if the creature is not valid or there is no item in the slot</returns>
        public static uint GetItemInSlot(InventorySlotType nInventorySlot, uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetItemInSlot(nInventorySlot, oCreature);

        /// <summary>
        /// Checks if a base item type fits in a target's inventory.
        /// </summary>
        /// <param name="baseItemType">The base item type to check (BASE_ITEM_*)</param>
        /// <param name="target">The target creature, placeable, or item to check</param>
        /// <returns>True if the base item type fits in the inventory, false if not or on error</returns>
        /// <remarks>Does not check inside any container items possessed by the target</remarks>
        public static bool GetBaseItemFitsInInventory(BaseItemType baseItemType, uint target) => _service.GetBaseItemFitsInInventory(baseItemType, target);

        /// <summary>
        /// Determines whether the specified encounter is active.
        /// </summary>
        /// <param name="oEncounter">The encounter to check (default: OBJECT_SELF)</param>
        /// <returns>1 if the encounter is active, 0 otherwise</returns>
        public static int GetEncounterActive(uint oEncounter = NWScriptService.OBJECT_INVALID) => _service.GetEncounterActive(oEncounter);

        /// <summary>
        /// Sets the encounter's active state to the specified value.
        /// </summary>
        /// <param name="nNewValue">The new active state (1 for TRUE, 0 for FALSE)</param>
        /// <param name="oEncounter">The encounter to set the active state for (default: OBJECT_SELF)</param>
        public static void SetEncounterActive(int nNewValue, uint oEncounter = NWScriptService.OBJECT_INVALID) => _service.SetEncounterActive(nNewValue, oEncounter);

        /// <summary>
        /// Gets the maximum number of times that the encounter will spawn.
        /// </summary>
        /// <param name="oEncounter">The encounter to check (default: OBJECT_SELF)</param>
        /// <returns>The maximum number of spawns</returns>
        public static int GetEncounterSpawnsMax(uint oEncounter = NWScriptService.OBJECT_INVALID) => _service.GetEncounterSpawnsMax(oEncounter);

        /// <summary>
        /// Sets the maximum number of times that the encounter can spawn.
        /// </summary>
        /// <param name="nNewValue">The new maximum number of spawns</param>
        /// <param name="oEncounter">The encounter to set the maximum spawns for (default: OBJECT_SELF)</param>
        public static void SetEncounterSpawnsMax(int nNewValue, uint oEncounter = NWScriptService.OBJECT_INVALID) => _service.SetEncounterSpawnsMax(nNewValue, oEncounter);

        /// <summary>
        /// Gets the number of times that the encounter has spawned so far.
        /// </summary>
        /// <param name="oEncounter">The encounter to check (default: OBJECT_SELF)</param>
        /// <returns>The current number of spawns</returns>
        public static int GetEncounterSpawnsCurrent(uint oEncounter = NWScriptService.OBJECT_INVALID) => _service.GetEncounterSpawnsCurrent(oEncounter);

        /// <summary>
        /// Sets the number of times that the encounter has spawned so far.
        /// </summary>
        /// <param name="nNewValue">The new current number of spawns</param>
        /// <param name="oEncounter">The encounter to set the current spawns for (default: OBJECT_SELF)</param>
        public static void SetEncounterSpawnsCurrent(int nNewValue, uint oEncounter = NWScriptService.OBJECT_INVALID) => _service.SetEncounterSpawnsCurrent(nNewValue, oEncounter);

        /// <summary>
        /// Sets the difficulty level of the specified encounter.
        /// </summary>
        /// <param name="nEncounterDifficulty">The encounter difficulty (ENCOUNTER_DIFFICULTY_* constants)</param>
        /// <param name="oEncounter">The encounter to set the difficulty for (default: OBJECT_SELF)</param>
        public static void SetEncounterDifficulty(EncounterDifficultyType nEncounterDifficulty,
            uint oEncounter = NWScriptService.OBJECT_INVALID) => _service.SetEncounterDifficulty(nEncounterDifficulty, oEncounter);

        /// <summary>
        /// Gets the difficulty level of the specified encounter.
        /// </summary>
        /// <param name="oEncounter">The encounter to get the difficulty for (default: OBJECT_SELF)</param>
        /// <returns>The encounter difficulty level</returns>
        public static int GetEncounterDifficulty(uint oEncounter = NWScriptService.OBJECT_INVALID) => _service.GetEncounterDifficulty(oEncounter);

        /// <summary>
        /// Returns true if the object (which is a placeable or a door) is currently open.
        /// </summary>
        /// <param name="oObject">The object to check</param>
        /// <returns>True if the object is open, false otherwise</returns>
        public static bool GetIsOpen(uint oObject) => _service.GetIsOpen(oObject);

        /// <summary>
        /// Makes the action subject unlock the target object.
        /// </summary>
        /// <param name="oTarget">The target object to unlock (can be a door or a placeable object)</param>
        public static void ActionUnlockObject(uint oTarget) => _service.ActionUnlockObject(oTarget);

        /// <summary>
        /// Makes the action subject lock the target object.
        /// </summary>
        /// <param name="oTarget">The target object to lock (can be a door or a placeable object)</param>
        public static void ActionLockObject(uint oTarget) => _service.ActionLockObject(oTarget);

        /// <summary>
        /// Makes the action subject open the specified door.
        /// </summary>
        /// <param name="oDoor">The door to open</param>
        public static void ActionOpenDoor(uint oDoor) => _service.ActionOpenDoor(oDoor);

        /// <summary>
        /// Makes the action subject close the specified door.
        /// </summary>
        /// <param name="oDoor">The door to close</param>
        public static void ActionCloseDoor(uint oDoor) => _service.ActionCloseDoor(oDoor);

        /// <summary>
        /// Gets the last blocking door encountered by the specified creature.
        /// </summary>
        /// <returns>The last blocking door. Returns OBJECT_INVALID if the caller is not a valid creature</returns>
        public static uint GetBlockingDoor() => _service.GetBlockingDoor();

        /// <summary>
        /// Returns true if the specified door action can be performed on the target door.
        /// </summary>
        /// <param name="oTargetDoor">The target door</param>
        /// <param name="nDoorAction">The door action to check (DOOR_ACTION_* constants)</param>
        /// <returns>True if the door action can be performed, false otherwise</returns>
        public static bool GetIsDoorActionPossible(uint oTargetDoor, DoorActionType nDoorAction) => _service.GetIsDoorActionPossible(oTargetDoor, nDoorAction);

        /// <summary>
        /// Performs the specified door action on the target door.
        /// </summary>
        /// <param name="oTargetDoor">The target door</param>
        /// <param name="nDoorAction">The door action to perform (DOOR_ACTION_* constants)</param>
        public static void DoDoorAction(uint oTargetDoor, DoorActionType nDoorAction) => _service.DoDoorAction(oTargetDoor, nDoorAction);

        /// <summary>
        /// Gets the orientation value from the location.
        /// </summary>
        /// <param name="lLocation">The location to get the orientation from</param>
        /// <returns>The orientation value</returns>
        public static float GetFacingFromLocation(Location lLocation) => _service.GetFacingFromLocation(lLocation);

        /// <summary>
        /// Gets the current cutscene state of the player specified by the creature.
        /// </summary>
        /// <param name="oCreature">The creature to check (defaults to OBJECT_SELF)</param>
        /// <returns>TRUE if the player is in cutscene mode, FALSE if not in cutscene mode or on error</returns>
        public static bool GetCutsceneMode(uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetCutsceneMode(oCreature);

        /// <summary>
        /// Forces this player's camera to be set to this height.
        /// Setting this value to zero will restore the camera to the racial default height.
        /// </summary>
        /// <param name="oPlayer">The player to set the camera height for</param>
        /// <param name="fHeight">The camera height (defaults to 0.0f)</param>
        public static void SetCameraHeight(uint oPlayer, float fHeight = 0.0f) => _service.SetCameraHeight(oPlayer, fHeight);

        /// <summary>
        /// Changes the current Day/Night cycle for this player to night.
        /// </summary>
        /// <param name="oPlayer">Which player to change the lighting for</param>
        /// <param name="fTransitionTime">How long the transition should take (defaults to 0.0f)</param>
        public static void DayToNight(uint oPlayer, float fTransitionTime = 0.0f) => _service.DayToNight(oPlayer, fTransitionTime);

        /// <summary>
        /// Changes the current Day/Night cycle for this player to daylight.
        /// </summary>
        /// <param name="oPlayer">Which player to change the lighting for</param>
        /// <param name="fTransitionTime">How long the transition should take (defaults to 0.0f)</param>
        public static void NightToDay(uint oPlayer, float fTransitionTime = 0.0f) => _service.NightToDay(oPlayer, fTransitionTime);

        /// <summary>
        /// Returns the current movement rate factor of the cutscene 'camera man'.
        /// NOTE: This will be a value between 0.1, 2.0 (10%-200%)
        /// </summary>
        /// <param name="oCreature">The creature to get the camera move rate for</param>
        /// <returns>The movement rate factor between 0.1 and 2.0</returns>
        public static float GetCutsceneCameraMoveRate(uint oCreature) => _service.GetCutsceneCameraMoveRate(oCreature);

        /// <summary>
        /// Sets the current movement rate factor for the cutscene camera man.
        /// NOTE: You can only set values between 0.1, 2.0 (10%-200%)
        /// </summary>
        /// <param name="oCreature">The creature to set the camera move rate for</param>
        /// <param name="fRate">The movement rate factor (between 0.1 and 2.0)</param>
        public static void SetCutsceneCameraMoveRate(uint oCreature, float fRate) => _service.SetCutsceneCameraMoveRate(oCreature, fRate);

        /// <summary>
        /// Makes a player examine the object. This causes the examination
        /// pop-up box to appear for the object specified.
        /// </summary>
        /// <param name="oExamine">The object to examine</param>
        public static void ActionExamine(uint oExamine) => _service.ActionExamine(oExamine);

        /// <summary>
        /// Use this to get the item last equipped by a player character in OnPlayerEquipItem.
        /// </summary>
        /// <returns>The item last equipped</returns>
        public static uint GetPCItemLastEquipped() => _service.GetPCItemLastEquipped();

        /// <summary>
        /// Use this to get the player character who last equipped an item in OnPlayerEquipItem.
        /// </summary>
        /// <returns>The player character who last equipped an item</returns>
        public static uint GetPCItemLastEquippedBy() => _service.GetPCItemLastEquippedBy();

        /// <summary>
        /// Use this to get the item last unequipped by a player character in OnPlayerEquipItem.
        /// </summary>
        /// <returns>The item last unequipped</returns>
        public static uint GetPCItemLastUnequipped() => _service.GetPCItemLastUnequipped();

        /// <summary>
        /// Use this to get the player character who last unequipped an item in OnPlayerUnEquipItem.
        /// </summary>
        /// <returns>The player character who last unequipped an item</returns>
        public static uint GetPCItemLastUnequippedBy() => _service.GetPCItemLastUnequippedBy();

        /// <summary>
        /// Sends a server message to the player using a string reference.
        /// </summary>
        /// <param name="oPlayer">The player to send the message to</param>
        /// <param name="nStrRef">The string reference to send</param>
        public static void SendMessageToPCByStrRef(uint oPlayer, int nStrRef) => _service.SendMessageToPCByStrRef(oPlayer, nStrRef);

        /// <summary>
        /// Opens this creature's inventory panel for this player.
        /// DM's can view any creature's inventory.
        /// Players can view their own inventory, or that of their henchman, familiar or animal companion.
        /// </summary>
        /// <param name="oCreature">Creature to view</param>
        /// <param name="oPlayer">The owner of this creature will see the panel pop up</param>
        public static void OpenInventory(uint oCreature, uint oPlayer) => _service.OpenInventory(oCreature, oPlayer);

        /// <summary>
        /// Stores the current camera mode and position so that it can be restored (using RestoreCameraFacing()).
        /// </summary>
        public static void StoreCameraFacing() => _service.StoreCameraFacing();

        /// <summary>
        /// Restores the camera mode and position to what they were last time StoreCameraFacing was called.
        /// RestoreCameraFacing can only be called once, and must correspond to a previous call to StoreCameraFacing.
        /// </summary>
        public static void RestoreCameraFacing() => _service.RestoreCameraFacing();

        /// <summary>
        /// Fades the screen for the given creature/player from black to regular screen.
        /// </summary>
        /// <param name="oCreature">Creature controlled by player that should fade from black</param>
        /// <param name="fSpeed">The fade speed (defaults to FadeSpeed.Medium)</param>
        public static void FadeFromBlack(uint oCreature, float fSpeed = FadeSpeed.Medium) => _service.FadeFromBlack(oCreature, fSpeed);

        /// <summary>
        /// Fades the screen for the given creature/player from regular screen to black.
        /// </summary>
        /// <param name="oCreature">Creature controlled by player that should fade to black</param>
        /// <param name="fSpeed">The fade speed (defaults to FadeSpeed.Medium)</param>
        public static void FadeToBlack(uint oCreature, float fSpeed = FadeSpeed.Medium) => _service.FadeToBlack(oCreature, fSpeed);

        /// <summary>
        /// Removes any fading or black screen.
        /// </summary>
        /// <param name="oCreature">Creature controlled by player that should be cleared</param>
        public static void StopFade(uint oCreature) => _service.StopFade(oCreature);

        /// <summary>
        /// Sets the screen to black. Can be used in preparation for a fade-in (FadeFromBlack).
        /// Can be cleared by either doing a FadeFromBlack, or by calling StopFade.
        /// </summary>
        /// <param name="oCreature">Creature controlled by player that should see black screen</param>
        public static void BlackScreen(uint oCreature) => _service.BlackScreen(oCreature);

        /// <summary>
        /// Sets the given creature into cutscene mode. This prevents the player from
        /// using the GUI and camera controls.
        /// Note: SetCutsceneMode(oPlayer, TRUE) will also make the player 'plot' (unkillable).
        /// SetCutsceneMode(oPlayer, FALSE) will restore the player's plot flag to what it
        /// was when SetCutsceneMode(oPlayer, TRUE) was called.
        /// </summary>
        /// <param name="oCreature">Creature in a cutscene</param>
        /// <param name="nInCutscene">TRUE to move them into cutscene, FALSE to remove cutscene mode</param>
        /// <param name="nLeftClickingEnabled">TRUE to allow the user to interact with the game world using the left mouse button only.
        /// FALSE to stop the user from interacting with the game world</param>
        public static void SetCutsceneMode(uint oCreature, bool nInCutscene = true, bool nLeftClickingEnabled = false) => _service.SetCutsceneMode(oCreature, nInCutscene, nLeftClickingEnabled);

        /// <summary>
        /// Gets the last player character to cancel from a cutscene.
        /// </summary>
        /// <returns>The last player character to cancel from a cutscene</returns>
        public static uint GetLastPCToCancelCutscene() => _service.GetLastPCToCancelCutscene();

        /// <summary>
        /// Removes the player from the server.
        /// You can optionally specify a reason to override the text shown to the player.
        /// </summary>
        /// <param name="oPlayer">The player to remove from the server</param>
        /// <param name="sReason">Optional reason to override the text shown to the player</param>
        public static void BootPC(uint oPlayer, string sReason = "") => _service.BootPC(oPlayer, sReason);

        /// <summary>
        /// Spawns in the Death GUI.
        /// The default (as defined by BioWare) can be spawned in by PopUpGUIPanel, but
        /// if you want to turn off the "Respawn" or "Wait for Help" buttons, this is the
        /// function to use.
        /// Note: The "Wait For Help" button will not appear in single player games.
        /// </summary>
        /// <param name="oPC">The player character</param>
        /// <param name="bRespawnButtonEnabled">If TRUE, the "Respawn" button will be enabled on the Death GUI</param>
        /// <param name="bWaitForHelpButtonEnabled">If TRUE, the "Wait For Help" button will be enabled on the Death GUI</param>
        /// <param name="nHelpStringReference">Help string reference</param>
        /// <param name="sHelpString">Help string</param>
        public static void PopUpDeathGUIPanel(uint oPC, bool bRespawnButtonEnabled = true,
            bool bWaitForHelpButtonEnabled = true, int nHelpStringReference = 0, string sHelpString = "") => _service.PopUpDeathGUIPanel(oPC, bRespawnButtonEnabled, bWaitForHelpButtonEnabled, nHelpStringReference, sHelpString);

        /// <summary>
        /// Gets the first PC in the player list.
        /// This resets the position in the player list for GetNextPC().
        /// </summary>
        /// <returns>The first PC in the player list</returns>
        public static uint GetFirstPC() => _service.GetFirstPC();

        /// <summary>
        /// Gets the next PC in the player list.
        /// This picks up where the last GetFirstPC() or GetNextPC() left off.
        /// </summary>
        /// <returns>The next PC in the player list</returns>
        public static uint GetNextPC() => _service.GetNextPC();

        /// <summary>
        /// Gets the last PC that levelled up.
        /// </summary>
        /// <returns>The last PC that levelled up</returns>
        public static uint GetPCLevellingUp() => _service.GetPCLevellingUp();

        /// <summary>
        /// Sets the camera mode for the player.
        /// If the player is not player-controlled or nCameraMode is invalid, nothing happens.
        /// </summary>
        /// <param name="oPlayer">The player to set the camera mode for</param>
        /// <param name="nCameraMode">CAMERA_MODE_* constant</param>
        public static void SetCameraMode(uint oPlayer, int nCameraMode) => _service.SetCameraMode(oPlayer, nCameraMode);

        /// <summary>
        /// Use this in an OnPlayerDying module script to get the last player who is dying.
        /// </summary>
        /// <returns>The last player who is dying</returns>
        public static uint GetLastPlayerDying() => _service.GetLastPlayerDying();

        /// <summary>
        /// Spawns a GUI panel for the client that controls the PC.
        /// Will force show panels disabled with SetGuiPanelDisabled().
        /// Nothing happens if the PC is not a player character or if an invalid value is used for nGUIPanel.
        /// </summary>
        /// <param name="oPC">The player character</param>
        /// <param name="nGUIPanel">GUI_PANEL_* constant, except GUI_PANEL_COMPASS / GUI_PANEL_LEVELUP / GUI_PANEL_GOLD_* / GUI_PANEL_EXAMINE_*</param>
        public static void PopUpGUIPanel(uint oPC, GuiPanelType nGUIPanel) => _service.PopUpGUIPanel(oPC, nGUIPanel);

        /// <summary>
        /// Returns the build number of the player (i.e. 8193).
        /// </summary>
        /// <param name="oPlayer">The player to get the build version for</param>
        /// <returns>The build number, or 0 if the given object isn't a player or did not advertise their build info</returns>
        public static int GetPlayerBuildVersionMajor(uint oPlayer) => _service.GetPlayerBuildVersionMajor(oPlayer);

        /// <summary>
        /// Returns the patch revision of the player (i.e. 8).
        /// </summary>
        /// <param name="oPlayer">The player to get the patch revision for</param>
        /// <returns>The patch revision, or 0 if the given object isn't a player or did not advertise their build info</returns>
        public static int GetPlayerBuildVersionMinor(uint oPlayer) => _service.GetPlayerBuildVersionMinor(oPlayer);

        /// <summary>
        /// Returns TRUE if the given player-controlled creature has DM privileges
        /// gained through a player login (as opposed to the DM client).
        /// Note: GetIsDM() also returns TRUE for player creature DMs.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>TRUE if the creature has player DM privileges</returns>
        public static bool GetIsPlayerDM(uint oCreature) => _service.GetIsPlayerDM(oCreature);

        /// <summary>
        /// Gets the player that last triggered the module OnPlayerGuiEvent event.
        /// </summary>
        /// <returns>The player that last triggered the GUI event</returns>
        public static uint GetLastGuiEventPlayer() => _service.GetLastGuiEventPlayer();

        /// <summary>
        /// Gets the last triggered GUIEVENT_* in the module OnPlayerGuiEvent event.
        /// </summary>
        /// <returns>The last triggered GUI event type</returns>
        public static GuiEventType GetLastGuiEventType() => _service.GetLastGuiEventType();

        /// <summary>
        /// Gets an optional integer of specific GUI events in the module OnPlayerGuiEvent event.
        /// GUIEVENT_CHATBAR_*: The selected chat channel. Does not indicate the actual used channel.
        /// 0 = Shout, 1 = Whisper, 2 = Talk, 3 = Party, 4 = DM
        /// GUIEVENT_CHARACTERSHEET_SKILL_SELECT: The skill ID.
        /// GUIEVENT_CHARACTERSHEET_FEAT_SELECT: The feat ID.
        /// GUIEVENT_EFFECTICON_CLICK: The effect icon ID (EFFECT_ICON_*)
        /// GUIEVENT_DISABLED_PANEL_ATTEMPT_OPEN: The GUI_PANEL_* the player attempted to open.
        /// GUIEVENT_QUICKCHAT_SELECT: The hotkey character representing the option
        /// GUIEVENT_EXAMINE_OBJECT: A GUI_PANEL_EXAMINE_* constant
        /// </summary>
        /// <returns>The integer value for the specific GUI event</returns>
        public static int GetLastGuiEventInteger() => _service.GetLastGuiEventInteger();

        /// <summary>
        /// Gets an optional object of specific GUI events in the module OnPlayerGuiEvent event.
        /// GUIEVENT_MINIMAP_MAPPIN_CLICK: The waypoint the map note is attached to.
        /// GUIEVENT_CHARACTERSHEET_*_SELECT: The owner of the character sheet.
        /// GUIEVENT_PLAYERLIST_PLAYER_CLICK: The player clicked on.
        /// GUIEVENT_PARTYBAR_PORTRAIT_CLICK: The creature clicked on.
        /// GUIEVENT_DISABLED_PANEL_ATTEMPT_OPEN: For GUI_PANEL_CHARACTERSHEET, the owner of the character sheet.
        /// </summary>
        /// <returns>The object for the specific GUI event</returns>
        public static uint GetLastGuiEventObject() => _service.GetLastGuiEventObject();

        /// <summary>
        /// Disables a GUI panel for the client that controls the player.
        /// Notes: Will close the GUI panel if currently open, except GUI_PANEL_LEVELUP / GUI_PANEL_GOLD_*
        /// Does not persist through relogging or in savegames.
        /// Will fire a GUIEVENT_DISABLED_PANEL_ATTEMPT_OPEN OnPlayerGuiEvent for some GUI panels if a player attempts to open them.
        /// You can still force show a panel with PopUpGUIPanel().
        /// You can still force examine an object with ActionExamine().
        /// </summary>
        /// <param name="oPlayer">The player to disable the GUI panel for</param>
        /// <param name="nGuiPanel">A GUI_PANEL_* constant, except GUI_PANEL_PLAYER_DEATH</param>
        /// <param name="bDisabled">Whether to disable the panel</param>
        /// <param name="oTarget">The target object</param>
        public static void SetGuiPanelDisabled(uint oPlayer, GuiPanelType nGuiPanel, bool bDisabled, uint oTarget = NWScriptService.OBJECT_INVALID) => _service.SetGuiPanelDisabled(oPlayer, nGuiPanel, bDisabled, oTarget);

        /// <summary>
        /// Gets the ID (1..8) of the last tile action performed in OnPlayerTileAction.
        /// </summary>
        /// <returns>The ID of the last tile action</returns>
        public static int GetLastTileActionId() => _service.GetLastTileActionId();

        /// <summary>
        /// Gets the target position in the module OnPlayerTileAction event.
        /// </summary>
        /// <returns>The target position of the last tile action</returns>
        public static Vector3 GetLastTileActionPosition() => _service.GetLastTileActionPosition();

        /// <summary>
        /// Gets the player object that triggered the OnPlayerTileAction event.
        /// </summary>
        /// <returns>The player object that triggered the tile action</returns>
        public static uint GetLastPlayerToDoTileAction() => _service.GetLastPlayerToDoTileAction();

        /// <summary>
        /// Gets a device property/capability as advertised by the client.
        /// Returns -1 if the property was never set by the client, the actual value is -1,
        /// the player is running an older build that does not advertise device properties,
        /// or the player has disabled sending device properties (Options->Game->Privacy).
        /// </summary>
        /// <param name="oPlayer">The player to get the device property for</param>
        /// <param name="sProperty">One of PLAYER_DEVICE_PROPERTY_xxx constants</param>
        /// <returns>The device property value, or -1 if unavailable</returns>
        public static int GetPlayerDeviceProperty(uint oPlayer, string sProperty) => _service.GetPlayerDeviceProperty(oPlayer, sProperty);

        /// <summary>
        /// Returns the LANGUAGE_xx code of the given player, or -1 if unavailable.
        /// </summary>
        /// <param name="oPlayer">The player to get the language for</param>
        /// <returns>The language code, or -1 if unavailable</returns>
        public static PlayerLanguageType GetPlayerLanguage(uint oPlayer) => _service.GetPlayerLanguage(oPlayer);

        /// <summary>
        /// Returns one of PLAYER_DEVICE_PLATFORM_xxx, or 0 if unavailable.
        /// </summary>
        /// <param name="oPlayer">The player to get the device platform for</param>
        /// <returns>The device platform type, or 0 if unavailable</returns>
        public static PlayerDevicePlatformType GetPlayerDevicePlatform(uint oPlayer) => _service.GetPlayerDevicePlatform(oPlayer);

        /// <summary>
        /// Returns the patch postfix of the player (i.e. the 29 out of "87.8193.35-29 abcdef01").
        /// </summary>
        /// <param name="oPlayer">The player to get the build version postfix for</param>
        /// <returns>The patch postfix, or 0 if the given object isn't a player or did not advertise their build info</returns>
        public static int GetPlayerBuildVersionPostfix(uint oPlayer) => _service.GetPlayerBuildVersionPostfix(oPlayer);

        /// <summary>
        /// Returns the patch commit sha1 of the player (i.e. the "abcdef01" out of "87.8193.35-29 abcdef01").
        /// </summary>
        /// <param name="oPlayer">The player to get the build version commit sha1 for</param>
        /// <returns>The patch commit sha1, or empty string if unavailable</returns>
        public static string GetPlayerBuildVersionCommitSha1(uint oPlayer) => _service.GetPlayerBuildVersionCommitSha1(oPlayer);

        /// <summary>
        /// Gets the PC that is involved in the conversation.
        /// </summary>
        /// <returns>The PC involved in the conversation, or OBJECT_INVALID on error</returns>
        public static uint GetPCSpeaker() => _service.GetPCSpeaker();

        /// <summary>
        /// Use this in an OnPlayerDeath module script to get the last player that died.
        /// </summary>
        /// <returns>The last player that died</returns>
        public static uint GetLastPlayerDied() => _service.GetLastPlayerDied();

        /// <summary>
        /// Use this in an OnItemLost script to get the item that was lost/dropped.
        /// </summary>
        /// <returns>The item that was lost/dropped, or OBJECT_INVALID if the module is not valid</returns>
        public static uint GetModuleItemLost() => _service.GetModuleItemLost();

        /// <summary>
        /// Use this in an OnItemLost script to get the creature that lost the item.
        /// </summary>
        /// <returns>The creature that lost the item, or OBJECT_INVALID if the module is not valid</returns>
        public static uint GetModuleItemLostBy() => _service.GetModuleItemLostBy();

        /// <summary>
        /// Gets the public part of the CD Key that the player used when logging in.
        /// </summary>
        /// <param name="oPlayer">The player to get the CD key for</param>
        /// <param name="nSinglePlayerCDKey">If TRUE, the player's public CD Key will be returned when the player is playing in single player mode (otherwise returns an empty string in single player mode)</param>
        /// <returns>The public CD key</returns>
        public static string GetPCPublicCDKey(uint oPlayer, bool nSinglePlayerCDKey = false) => _service.GetPCPublicCDKey(oPlayer, nSinglePlayerCDKey);

        /// <summary>
        /// Gets the IP address from which the player has connected.
        /// </summary>
        /// <param name="oPlayer">The player to get the IP address for</param>
        /// <returns>The IP address</returns>
        public static string GetPCIPAddress(uint oPlayer) => _service.GetPCIPAddress(oPlayer);

        /// <summary>
        /// Gets the name of the player.
        /// </summary>
        /// <param name="oPlayer">The player to get the name for</param>
        /// <returns>The player name</returns>
        public static string GetPCPlayerName(uint oPlayer) => _service.GetPCPlayerName(oPlayer);

        /// <summary>
        /// Sets the player and target to like each other.
        /// </summary>
        /// <param name="oPlayer">The player</param>
        /// <param name="oTarget">The target</param>
        public static void SetPCLike(uint oPlayer, uint oTarget) => _service.SetPCLike(oPlayer, oTarget);

        /// <summary>
        /// Sets the player and target to dislike each other.
        /// </summary>
        /// <param name="oPlayer">The player</param>
        /// <param name="oTarget">The target</param>
        public static void SetPCDislike(uint oPlayer, uint oTarget) => _service.SetPCDislike(oPlayer, oTarget);

        /// <summary>
        /// Sends a server message to the player.
        /// </summary>
        /// <param name="oPlayer">The player to send the message to</param>
        /// <param name="szMessage">The message to send</param>
        public static void SendMessageToPC(uint oPlayer, string szMessage) => _service.SendMessageToPC(oPlayer, szMessage);

        /// <summary>
        /// Gets if the player is currently connected over a relay (instead of directly).
        /// Returns FALSE for any other object, including OBJECT_INVALID.
        /// </summary>
        /// <param name="oPlayer">The player to check</param>
        /// <returns>TRUE if connected over a relay, FALSE otherwise</returns>
        public static int GetIsPlayerConnectionRelayed(uint oPlayer) => _service.GetIsPlayerConnectionRelayed(oPlayer);

        /// <summary>
        /// Forces all the characters of the players who are currently in the game to
        /// be exported to their respective directories i.e. LocalVault/ServerVault/ etc.
        /// </summary>
        public static void ExportAllCharacters() => _service.ExportAllCharacters();

        /// <summary>
        /// Forces the character of the player specified to be exported to its respective directory
        /// i.e. LocalVault/ServerVault/ etc.
        /// </summary>
        /// <param name="oPlayer">The player to export the character for</param>
        public static void ExportSingleCharacter(uint oPlayer) => _service.ExportSingleCharacter(oPlayer);

        /// <summary>
        /// Returns the INVENTORY_SLOT_* constant of the last item equipped.
        /// Can only be used in the module's OnPlayerEquip event.
        /// </summary>
        /// <returns>The inventory slot constant, or -1 on error</returns>
        public static InventorySlotType GetPCItemLastEquippedSlot() => _service.GetPCItemLastEquippedSlot();

        /// <summary>
        /// Returns the INVENTORY_SLOT_* constant of the last item unequipped.
        /// Can only be used in the module's OnPlayerUnequip event.
        /// </summary>
        /// <returns>The inventory slot constant, or -1 on error</returns>
        public static InventorySlotType GetPCItemLastUnequippedSlot() => _service.GetPCItemLastUnequippedSlot();

        /// <summary>
        /// Returns the network latency of the given player in milliseconds.
        /// Returns -1 if the player is not connected or the information is unavailable.
        /// </summary>
        /// <param name="oPlayer">The player to get the network latency for</param>
        /// <returns>The network latency in milliseconds, or -1 if unavailable</returns>
        public static int GetPlayerNetworkLatency(uint oPlayer) => _service.GetPlayerNetworkLatency(oPlayer);

        /// <summary>
        /// Gets the body bag object for the given creature.
        /// Returns OBJECT_INVALID if the creature has no body bag or is not dead.
        /// </summary>
        /// <param name="oCreature">The creature to get the body bag for</param>
        /// <returns>The body bag object, or OBJECT_INVALID if none exists</returns>
        public static int GetBodyBag(uint oCreature) => _service.GetBodyBag(oCreature);

        /// <summary>
        /// Sets the body bag object for the given creature.
        /// This is typically used when a creature dies to create a body bag for loot.
        /// </summary>
        /// <param name="oCreature">The creature to set the body bag for</param>
        /// <param name="oBodyBag">The body bag object to set</param>
        public static void SetBodyBag(uint oCreature, int oBodyBag) => _service.SetBodyBag(oCreature, oBodyBag);

        /// <summary>
        /// Determines whether the object has any effects applied by the spell.
        /// The spell id on effects is only valid if the effect is created
        /// when the spell script runs. If it is created in a delayed command
        /// then the spell id on the effect will be invalid.
        /// </summary>
        /// <param name="nSpell">SPELL_* constant</param>
        /// <param name="oObject">The object to check (defaults to OBJECT_SELF)</param>
        /// <returns>TRUE if the object has effects from the spell</returns>
        public static bool GetHasSpellEffect(SpellType nSpell, uint oObject = NWScriptService.OBJECT_INVALID) => _service.GetHasSpellEffect(nSpell, oObject);

        /// <summary>
        /// Gets the spell that applied the spell effect.
        /// </summary>
        /// <param name="eSpellEffect">The spell effect to check</param>
        /// <returns>The spell ID (SPELL_*), or -1 if the effect was applied outside a spell script</returns>
        public static int GetEffectSpellId(Effect eSpellEffect) => _service.GetEffectSpellId(eSpellEffect);

        /// <summary>
        /// Use this in spell scripts to get damage adjusted by the target's reflex and
        /// evasion saves.
        /// </summary>
        /// <param name="nDamage">The base damage</param>
        /// <param name="oTarget">The target to check saves for</param>
        /// <param name="nDC">Difficulty check</param>
        /// <param name="nSaveType">SAVING_THROW_TYPE_* constant</param>
        /// <param name="oSaveVersus">The object to save versus (defaults to OBJECT_INVALID)</param>
        /// <returns>The adjusted damage after saves</returns>
        public static int GetReflexAdjustedDamage(int nDamage, uint oTarget, int nDC,
            SavingThrowType nSaveType = SavingThrowType.All, uint oSaveVersus = NWScriptService.OBJECT_INVALID) => _service.GetReflexAdjustedDamage(nDamage, oTarget, nDC, nSaveType, oSaveVersus);

        /// <summary>
        /// Gets the object at which the specified caster last cast a spell.
        /// </summary>
        /// <returns>The target object, or OBJECT_INVALID on error</returns>
        public static uint GetSpellTargetObject() => _service.GetSpellTargetObject();

        /// <summary>
        /// Gets the metamagic type of the last spell cast by the caller.
        /// </summary>
        /// <returns>The metamagic type (METAMAGIC_*), or -1 if the caster is not a valid object</returns>
        public static int GetMetaMagicFeat() => _service.GetMetaMagicFeat();

        /// <summary>
        /// Gets the DC to save against for a spell (10 + spell level + relevant ability bonus).
        /// This can be called by a creature or by an Area of Effect object.
        /// </summary>
        /// <returns>The spell save DC</returns>
        public static int GetSpellSaveDC() => _service.GetSpellSaveDC();

        /// <summary>
        /// Gets the location of the specified caster's last spell target.
        /// </summary>
        /// <returns>The location of the last spell target</returns>
        public static Location GetSpellTargetLocation() => _service.GetSpellTargetLocation();

        /// <summary>
        /// Casts a spell at the target location.
        /// If bCheat is TRUE, then the executor of the action doesn't have to be
        /// able to cast the spell.
        /// If bInstantSpell is TRUE, the spell is cast immediately; this allows
        /// the end-user to simulate a high-level magic user having lots of advance warning of impending trouble.
        /// </summary>
        /// <param name="nSpell">SPELL_* constant</param>
        /// <param name="lTargetLocation">The target location to cast the spell at</param>
        /// <param name="nMetaMagic">METAMAGIC_* constant</param>
        /// <param name="bCheat">If TRUE, the executor doesn't have to be able to cast the spell</param>
        /// <param name="nProjectilePathType">PROJECTILE_PATH_TYPE_* constant</param>
        /// <param name="bInstantSpell">If TRUE, the spell is cast immediately</param>
        public static void ActionCastSpellAtLocation(SpellType nSpell, Location lTargetLocation,
            MetaMagicType nMetaMagic = MetaMagicType.Any, bool bCheat = false,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default, bool bInstantSpell = false) => _service.ActionCastSpellAtLocation(nSpell, lTargetLocation, nMetaMagic, bCheat, nProjectilePathType, bInstantSpell);

        /// <summary>
        /// Creates an event which triggers the "SpellCastAt" script.
        /// Note: This only creates the event. The event won't actually trigger until SignalEvent()
        /// is called using this created SpellCastAt event as an argument.
        /// For example:
        /// SignalEvent(oCreature, EventSpellCastAt(oCaster, SPELL_MAGIC_MISSILE, TRUE));
        /// This function doesn't cast the spell specified, it only creates an event so that
        /// when the event is signaled on an object, the object will use its OnSpellCastAt script
        /// to react to the spell being cast.
        /// To specify the OnSpellCastAt script that should run, view the Object's Properties
        /// and click on the Scripts Tab. Then specify a script for the OnSpellCastAt event.
        /// From inside the OnSpellCastAt script call:
        /// GetLastSpellCaster() to get the object that cast the spell (oCaster).
        /// GetLastSpell() to get the type of spell cast (nSpell)
        /// GetLastSpellHarmful() to determine if the spell cast at the object was harmful.
        /// </summary>
        /// <param name="oCaster">The object that cast the spell</param>
        /// <param name="nSpell">The spell that was cast (SPELL_*)</param>
        /// <param name="bHarmful">Whether the spell is harmful</param>
        /// <returns>The SpellCastAt event</returns>
        public static Event EventSpellCastAt(uint oCaster, SpellType nSpell, bool bHarmful = true) => _service.EventSpellCastAt(oCaster, nSpell, bHarmful);

        /// <summary>
        /// This is for use in a "Spell Cast" script, it gets who cast the spell.
        /// The spell could have been cast by a creature, placeable or door.
        /// </summary>
        /// <returns>The object that cast the spell, or OBJECT_INVALID if the caller is not a creature, placeable or door</returns>
        public static uint GetLastSpellCaster() => _service.GetLastSpellCaster();

        /// <summary>
        /// This is for use in a "Spell Cast" script, it gets the ID of the spell that was cast.
        /// </summary>
        /// <returns>The ID of the spell that was cast</returns>
        public static int GetLastSpell() => _service.GetLastSpell();

        /// <summary>
        /// This is for use in a Spell script, it gets the ID of the spell that is being cast.
        /// </summary>
        /// <returns>The ID of the spell being cast (SPELL_*)</returns>
        public static int GetSpellId() => _service.GetSpellId();

        /// <summary>
        /// Use this in a SpellCast script to determine whether the spell was considered harmful.
        /// </summary>
        /// <returns>TRUE if the last spell cast was harmful</returns>
        public static bool GetLastSpellHarmful() => _service.GetLastSpellHarmful();

        /// <summary>
        /// The action subject will fake casting a spell at the target; the conjure and cast
        /// animations and visuals will occur, nothing else.
        /// </summary>
        /// <param name="nSpell">The spell to fake cast</param>
        /// <param name="oTarget">The target to fake cast at</param>
        /// <param name="nProjectilePathType">PROJECTILE_PATH_TYPE_* constant</param>
        public static void ActionCastFakeSpellAtObject(SpellType nSpell, uint oTarget,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default) => _service.ActionCastFakeSpellAtObject(nSpell, oTarget, nProjectilePathType);

        /// <summary>
        /// The action subject will fake casting a spell at the location; the conjure and
        /// cast animations and visuals will occur, nothing else.
        /// </summary>
        /// <param name="nSpell">The spell to fake cast</param>
        /// <param name="lTarget">The target location to fake cast at</param>
        /// <param name="nProjectilePathType">PROJECTILE_PATH_TYPE_* constant</param>
        public static void ActionCastFakeSpellAtLocation(SpellType nSpell, Location lTarget,
            ProjectilePathType nProjectilePathType = ProjectilePathType.Default) => _service.ActionCastFakeSpellAtLocation(nSpell, lTarget, nProjectilePathType);

        /// <summary>
        /// Counterspells the target.
        /// </summary>
        /// <param name="oCounterSpellTarget">The target to counterspell</param>
        public static void ActionCounterSpell(uint oCounterSpellTarget) => _service.ActionCounterSpell(oCounterSpellTarget);

        /// <summary>
        /// Gets the target at which the specified creature attempted to cast a spell.
        /// This value is set every time a spell is cast and is reset at the end of combat.
        /// </summary>
        /// <returns>The attempted spell target, or OBJECT_INVALID if the caller is not a valid creature</returns>
        public static uint GetAttemptedSpellTarget() => _service.GetAttemptedSpellTarget();

        /// <summary>
        /// In the spell script returns the feat used, or -1 if no feat was used.
        /// </summary>
        /// <returns>The feat ID used, or -1 if no feat was used</returns>
        public static int GetSpellFeatId() => _service.GetSpellFeatId();

        /// <summary>
        /// Returns TRUE if the last spell was cast spontaneously.
        /// e.g. a Cleric casting SPELL_CURE_LIGHT_WOUNDS when it is not prepared, using another level 1 slot.
        /// </summary>
        /// <returns>TRUE if the last spell was cast spontaneously</returns>
        public static bool GetSpellCastSpontaneously() => _service.GetSpellCastSpontaneously();

        /// <summary>
        /// Returns the level of the last spell cast. This value is only valid in a Spell script.
        /// </summary>
        /// <returns>The level of the last spell cast</returns>
        public static int GetLastSpellLevel() => _service.GetLastSpellLevel();

        /// <summary>
        /// Gets the number of memorized spell slots for a given spell level.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <returns>The number of spell slots</returns>
        public static int GetMemorizedSpellCountByLevel(uint oCreature, ClassType nClassType, int nSpellLevel) => _service.GetMemorizedSpellCountByLevel(oCreature, nClassType, nSpellLevel);

        /// <summary>
        /// Gets the spell ID of a memorized spell slot.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <param name="nIndex">The index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()</param>
        /// <returns>A SPELL_* constant or -1 if the slot is not set</returns>
        public static int GetMemorizedSpellId(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex) => _service.GetMemorizedSpellId(oCreature, nClassType, nSpellLevel, nIndex);

        /// <summary>
        /// Gets the ready state of a memorized spell slot.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <param name="nIndex">The index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()</param>
        /// <returns>TRUE/FALSE or -1 if the slot is not set</returns>
        public static int GetMemorizedSpellReady(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex) => _service.GetMemorizedSpellReady(oCreature, nClassType, nSpellLevel, nIndex);

        /// <summary>
        /// Gets the metamagic of a memorized spell slot.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <param name="nIndex">The index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()</param>
        /// <returns>A METAMAGIC_* constant or -1 if the slot is not set</returns>
        public static int GetMemorizedSpellMetaMagic(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex) => _service.GetMemorizedSpellMetaMagic(oCreature, nClassType, nSpellLevel, nIndex);

        /// <summary>
        /// Gets if the memorized spell slot has a domain spell.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <param name="nIndex">The index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()</param>
        /// <returns>TRUE/FALSE or -1 if the slot is not set</returns>
        public static int GetMemorizedSpellIsDomainSpell(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex) => _service.GetMemorizedSpellIsDomainSpell(oCreature, nClassType, nSpellLevel, nIndex);

        /// <summary>
        /// Sets a memorized spell slot.
        /// </summary>
        /// <param name="oCreature">The creature to set the spell for</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <param name="nIndex">The index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()</param>
        /// <param name="nSpellId">A SPELL_* constant</param>
        /// <param name="bReady">TRUE to mark the slot ready</param>
        /// <param name="nMetaMagic">A METAMAGIC_* constant</param>
        /// <param name="bIsDomainSpell">TRUE for a domain spell</param>
        public static void SetMemorizedSpell(
            uint oCreature,
            ClassType nClassType,
            int nSpellLevel,
            int nIndex,
            SpellType nSpellId,
            bool bReady = true,
            MetaMagicType nMetaMagic = MetaMagicType.None,
            bool bIsDomainSpell = false) => _service.SetMemorizedSpell(oCreature, nClassType, nSpellLevel, nIndex, nSpellId, bReady, nMetaMagic, bIsDomainSpell);

        /// <summary>
        /// Sets the ready state of a memorized spell slot.
        /// </summary>
        /// <param name="oCreature">The creature to set the spell for</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <param name="nIndex">The index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()</param>
        /// <param name="bReady">TRUE to mark the slot ready</param>
        public static void SetMemorizedSpellReady(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex, bool bReady) => _service.SetMemorizedSpellReady(oCreature, nClassType, nSpellLevel, nIndex, bReady);

        /// <summary>
        /// Clears a specific memorized spell slot.
        /// </summary>
        /// <param name="oCreature">The creature to clear the spell for</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <param name="nIndex">The index of the spell slot. Bounds: 0 <= nIndex < GetMemorizedSpellCountByLevel()</param>
        public static void ClearMemorizedSpell(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex) => _service.ClearMemorizedSpell(oCreature, nClassType, nSpellLevel, nIndex);

        /// <summary>
        /// Clears all memorized spell slots of a specific spell ID, including metamagic'd ones.
        /// </summary>
        /// <param name="oCreature">The creature to clear the spells for</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a MemorizesSpells class</param>
        /// <param name="nSpellId">A SPELL_* constant</param>
        public static void ClearMemorizedSpellBySpellId(uint oCreature, ClassType nClassType, int nSpellId) => _service.ClearMemorizedSpellBySpellId(oCreature, nClassType, nSpellId);

        /// <summary>
        /// Gets the number of known spells for a given spell level.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a SpellBookRestricted class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <returns>The number of known spells</returns>
        public static int GetKnownSpellCount(uint oCreature, ClassType nClassType, int nSpellLevel) => _service.GetKnownSpellCount(oCreature, nClassType, nSpellLevel);

        /// <summary>
        /// Gets the spell ID of a known spell.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a SpellBookRestricted class</param>
        /// <param name="nSpellLevel">The spell level, 0-9</param>
        /// <param name="nIndex">The index of the known spell. Bounds: 0 <= nIndex < GetKnownSpellCount()</param>
        /// <returns>A SPELL_* constant or -1 on error</returns>
        public static int GetKnownSpellId(uint oCreature, ClassType nClassType, int nSpellLevel, int nIndex) => _service.GetKnownSpellId(oCreature, nClassType, nSpellLevel, nIndex);

        /// <summary>
        /// Gets if a spell is in the known spell list.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant. Must be a SpellBookRestricted class</param>
        /// <param name="nSpellId">A SPELL_* constant</param>
        /// <returns>TRUE if the spell is in the known spell list</returns>
        public static bool GetIsInKnownSpellList(uint oCreature, ClassType nClassType, SpellType nSpellId) => _service.GetIsInKnownSpellList(oCreature, nClassType, nSpellId);

        /// <summary>
        /// Gets the amount of uses a spell has left.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <param name="nClassType">A CLASS_TYPE_* constant</param>
        /// <param name="nSpellId">A SPELL_* constant</param>
        /// <param name="nMetaMagic">A METAMAGIC_* constant</param>
        /// <param name="nDomainLevel">The domain level, if a domain spell</param>
        /// <returns>The amount of spell uses left</returns>
        public static int GetSpellUsesLeft(
            uint oCreature,
            ClassType nClassType,
            SpellType nSpellId,
            MetaMagicType nMetaMagic = MetaMagicType.None,
            int nDomainLevel = 0) => _service.GetSpellUsesLeft(oCreature, nClassType, nSpellId, nMetaMagic, nDomainLevel);

        /// <summary>
        /// Gets the spell level at which a class gets a spell.
        /// </summary>
        /// <param name="nClassType">A CLASS_TYPE_* constant</param>
        /// <param name="nSpellId">A SPELL_* constant</param>
        /// <returns>The spell level or -1 if the class does not get the spell</returns>
        public static int GetSpellLevelByClass(ClassType nClassType, SpellType nSpellId) => _service.GetSpellLevelByClass(nClassType, nSpellId);

        /// <summary>
        /// Performs a spell resistance check. The roll is 1d20 + nCasterLevel + nCasterBonus vs. nSpellResistance.
        /// </summary>
        /// <param name="oTarget">The target of the spell</param>
        /// <param name="oCaster">The caster of the spell</param>
        /// <param name="nSpellId">The spell ID to use if other variables are not set. If -1 it will attempt to be auto-detected (default: -1)</param>
        /// <param name="nCasterLevel">The caster level. If -1 it attempts to find it automatically from oCaster (default: -1)</param>
        /// <param name="nSpellResistance">The spell resistance to penetrate. If -1 it will use the spell resistance of oTarget (default: -1)</param>
        /// <param name="bFeedback">If true displays feedback automatically, false suppresses it (default: true)</param>
        /// <returns>True if the target resists the caster's spell resistance roll, false if failed or an error occurred</returns>
        public static bool SpellResistanceCheck(uint oTarget, uint oCaster, SpellType nSpellId = (SpellType)(-1), int nCasterLevel = -1, int nSpellResistance = -1, bool bFeedback = true) => _service.SpellResistanceCheck(oTarget, oCaster, nSpellId, nCasterLevel, nSpellResistance, bFeedback);

        /// <summary>
        /// Performs a spell immunity check. This checks for EffectSpellImmunity and related item properties.
        /// </summary>
        /// <param name="oTarget">The target of the spell</param>
        /// <param name="oCaster">The caster of the spell</param>
        /// <param name="nSpellId">The spell ID to check immunity of. If -1 it will attempt to be auto-detected (default: -1)</param>
        /// <param name="bFeedback">If true displays feedback automatically, false suppresses it (default: true)</param>
        /// <returns>True if the target is immune to the spell, false if failed or an error occurred</returns>
        public static bool SpellImmunityCheck(uint oTarget, uint oCaster, SpellType nSpellId = (SpellType)(-1), bool bFeedback = true) => _service.SpellImmunityCheck(oTarget, oCaster, nSpellId, bFeedback);

        /// <summary>
        /// Performs a spell absorption check that checks limited absorption effects (e.g. Spell Mantle).
        /// </summary>
        /// <param name="oTarget">The target of the spell</param>
        /// <param name="oCaster">The caster of the spell</param>
        /// <param name="nSpellId">The spell ID. If -1 it will attempt to be auto-detected (default: -1)</param>
        /// <param name="nSpellSchool">The spell school to check for. If -1 uses the spell's school (default: -1)</param>
        /// <param name="nSpellLevel">The spell level. If -1 uses the spell's level (given the caster's last spell cast class) (default: -1)</param>
        /// <param name="bRemoveLevels">If true this removes spell levels from the effect that would stop it (and remove it if 0 or less remain), but if false they will not be removed (default: true)</param>
        /// <param name="bFeedback">If true displays feedback automatically, false suppresses it (default: true)</param>
        /// <returns>True if the target absorbs the caster's spell, false if failed or an error occurred</returns>
        public static bool SpellAbsorptionLimitedCheck(uint oTarget, uint oCaster, SpellType nSpellId = (SpellType)(-1), SpellSchool nSpellSchool = (SpellSchool)(-1), int nSpellLevel = -1, bool bRemoveLevels = true, bool bFeedback = true) => _service.SpellAbsorptionLimitedCheck(oTarget, oCaster, nSpellId, nSpellSchool, nSpellLevel, bRemoveLevels, bFeedback);

        /// <summary>
        /// Performs a spell absorption check that checks unlimited spell absorption effects (e.g. Globes).
        /// </summary>
        /// <param name="oTarget">The target of the spell</param>
        /// <param name="oCaster">The caster of the spell</param>
        /// <param name="nSpellId">The spell ID. If -1 it will attempt to be auto-detected (default: -1)</param>
        /// <param name="nSpellSchool">The spell school to check for. If -1 uses the spell's school (default: -1)</param>
        /// <param name="nSpellLevel">The spell level. If -1 uses the spell's level (given the caster's last spell cast class) (default: -1)</param>
        /// <param name="bFeedback">If true displays feedback automatically, false suppresses it. As per existing ResistSpell convention it defaults to false (default: false)</param>
        /// <returns>True if the target absorbs the caster's spell, false if failed or an error occurred</returns>
        public static bool SpellAbsorptionUnlimitedCheck(uint oTarget, uint oCaster, SpellType nSpellId = (SpellType)(-1), SpellSchool nSpellSchool = (SpellSchool)(-1), int nSpellLevel = -1, bool bFeedback = false) => _service.SpellAbsorptionUnlimitedCheck(oTarget, oCaster, nSpellId, nSpellSchool, nSpellLevel, bFeedback);

        /// <summary>
        /// Returns the script parameter value for a given parameter name.
        /// Script parameters can be set for conversation scripts in the toolset's
        /// Conversation Editor, or for any script with SetScriptParam().
        /// </summary>
        /// <param name="sParamName">The name of the parameter to get</param>
        /// <returns>The parameter value, or empty string if a parameter with the given name does not exist</returns>
        public static string GetScriptParam(string sParamName) => _service.GetScriptParam(sParamName);

        /// <summary>
        /// Sets a script parameter value for the next script to be run.
        /// Call this function to set parameters right before calling ExecuteScript().
        /// </summary>
        /// <param name="sParamName">The name of the parameter to set</param>
        /// <param name="sParamValue">The value to set for the parameter</param>
        public static void SetScriptParam(string sParamName, string sParamValue) => _service.SetScriptParam(sParamName, sParamValue);

        /// <summary>
        /// Returns the currently executing event (EVENT_SCRIPT_*) or 0 if not determinable.
        /// Note: Will return 0 in DelayCommand/AssignCommand.
        /// Some events can run in the same script context as a previous event (for example: CreatureOnDeath, CreatureOnDamaged)
        /// In cases like these calling the function with bInheritParent = TRUE will return the wrong event ID.
        /// </summary>
        /// <param name="bInheritParent">If TRUE, ExecuteScript(Chunk) will inherit their event ID from their parent event.
        /// If FALSE, it will return the event ID of the current script, which may be 0</param>
        /// <returns>The currently executing event or 0 if not determinable</returns>
        public static EventScriptType GetCurrentlyRunningEvent(bool bInheritParent = true) => _service.GetCurrentlyRunningEvent(bInheritParent);

        /// <summary>
        /// Returns the number of script instructions remaining for the currently-running script.
        /// Once this value hits zero, the script will abort with TOO MANY INSTRUCTIONS.
        /// The instruction limit is configurable by the user, so if you have a really long-running
        /// process, this value can guide you with splitting it up into smaller, discretely schedulable parts.
        /// Note: Running this command and checking/handling the value also takes up some instructions.
        /// </summary>
        /// <returns>The number of script instructions remaining</returns>
        public static int GetScriptInstructionsRemaining() => _service.GetScriptInstructionsRemaining();

        /// <summary>
        /// Compiles a script and places it in the server's CURRENTGAME: folder.
        /// Note: Scripts will persist for as long as the module is running.
        /// SinglePlayer / Saves: Scripts that overwrite existing module scripts will persist to the save file.
        /// New scripts, unknown to the module, will have to be re-compiled on module load when loading a save.
        /// </summary>
        /// <param name="sScriptName">The name of the script to compile</param>
        /// <param name="sScriptData">The script source code to compile</param>
        /// <param name="bWrapIntoMain">Whether to wrap the script into a main function</param>
        /// <param name="bGenerateNDB">Whether to generate debug information (NDB file)</param>
        /// <returns>Empty string on success or the error on failure</returns>
        public static string CompileScript(string sScriptName, string sScriptData, bool bWrapIntoMain = false, bool bGenerateNDB = false) => _service.CompileScript(sScriptName, sScriptData, bWrapIntoMain, bGenerateNDB);

        /// <summary>
        /// This immediately aborts the running script.
        /// Will not emit an error to the server log by default.
        /// You can specify the optional sError to emit as a script error, which will be printed
        /// to the log and sent to all players, just like any other script error.
        /// Will not terminate other script recursion (e.g. nested ExecuteScript()) will resume as if the
        /// called script exited cleanly.
        /// This call will never return.
        /// </summary>
        /// <param name="sError">Optional error message to emit as a script error</param>
        public static void AbortRunningScript(string sError = "") => _service.AbortRunningScript(sError);

        /// <summary>
        /// Generates a VM debug view into the current execution location.
        /// Names and symbols can only be resolved if debug information is available (NDB file).
        /// This call can be a slow call for large scripts.
        /// Setting bIncludeStack = TRUE will include stack info in the output, which could be a
        /// lot of data for large scripts. You can turn it off if you do not need the info.
        /// Returned data format (JSON object):
        /// "frames": array of stack frames:
        ///   "ip": instruction pointer into code
        ///   "bp", "sp": current base/stack pointer
        ///   "file", "line", "function": available only if NDB loaded correctly
        /// "stack": abbreviated stack data (only if bIncludeStack is TRUE)
        ///   "type": one of the nwscript object types, OR:
        ///   "type_unknown": hex code of AUX
        ///   "data": type-specific payload. Not all type info is rendered in the interest of brevity.
        ///           Only enough for you to re-identify which variable this might belong to.
        /// </summary>
        /// <param name="bIncludeStack">Whether to include stack info in the output</param>
        /// <returns>JSON object containing debug information</returns>
        public static Json GetScriptBacktrace(bool bIncludeStack = true) => _service.GetScriptBacktrace(bIncludeStack);

        /// <summary>
        /// Marks the current location in code as a jump target, identified by sLabel.
        /// Returns 0 on initial invocation, but will return nRetVal if jumped-to by LongJmp.
        /// sLabel can be any valid string (including empty); though it is recommended to pick
        /// something distinct. The responsibility of namespacing lies with you.
        /// Calling repeatedly with the same label will overwrite the previous jump location.
        /// If you want to nest them, you need to manage nesting state externally.
        /// </summary>
        /// <param name="sLabel">The label to identify this jump target</param>
        /// <returns>0 on initial invocation, but will return nRetVal if jumped-to by LongJmp</returns>
        public static int SetJmp(string sLabel) => _service.SetJmp(sLabel);

        /// <summary>
        /// Jumps execution back in time to the point where you called SetJmp with the same label.
        /// This function is a GREAT way to get really hard-to-debug stack under/overflows.
        /// Will not work across script runs or script recursion; only within the same script.
        /// (However, it WILL work across includes - those go into the same script data in compilation)
        /// Will throw a script error if sLabel does not exist.
        /// Will throw a script error if no valid jump destination exists.
        /// You CAN jump to locations with compatible stack layout, including sibling functions.
        /// For the script to successfully finish, the entire stack needs to be correct (either in code or
        /// by jumping elsewhere again). Making sure this is the case is YOUR responsibility.
        /// The parameter nRetVal is passed to SetJmp, resuming script execution as if SetJmp returned
        /// that value (instead of 0).
        /// If you accidentally pass 0 as nRetVal, it will be silently rewritten to 1.
        /// Any other integer value is valid, including negative ones.
        /// This call will never return.
        /// </summary>
        /// <param name="sLabel">The label to jump to</param>
        /// <param name="nRetVal">The return value to pass to SetJmp (defaults to 1)</param>
        public static void LongJmp(string sLabel, int nRetVal = 1) => _service.LongJmp(sLabel, nRetVal);

        /// <summary>
        /// Returns TRUE if the given label is a valid jump target at the current code location.
        /// </summary>
        /// <param name="sLabel">The label to check</param>
        /// <returns>TRUE if the label is a valid jump target</returns>
        public static bool GetIsValidJmp(string sLabel) => _service.GetIsValidJmp(sLabel);

        /// <summary>
        /// Gets the current script recursion level.
        /// </summary>
        /// <returns>The current script recursion level</returns>
        public static int GetScriptRecursionLevel() => _service.GetScriptRecursionLevel();

        /// <summary>
        /// Gets the name of the script at a script recursion level.
        /// </summary>
        /// <param name="nRecursionLevel">Between 0 and <= GetScriptRecursionLevel() or -1 for the current recursion level</param>
        /// <returns>The script name or empty string on error</returns>
        public static string GetScriptName(int nRecursionLevel = -1) => _service.GetScriptName(nRecursionLevel);

        /// <summary>
        /// Gets the script chunk attached to a script recursion level.
        /// </summary>
        /// <param name="nRecursionLevel">Between 0 and <= GetScriptRecursionLevel() or -1 for the current recursion level</param>
        /// <returns>The script chunk or empty string on error / no script chunk attached</returns>
        public static string GetScriptChunk(int nRecursionLevel = -1) => _service.GetScriptChunk(nRecursionLevel);

        /// <summary>
        /// Display floaty text above the specified creature.
        /// The text will also appear in the chat buffer of each player that receives the
        /// floaty text.
        /// </summary>
        /// <param name="nStrRefToDisplay">String ref (therefore text is translated)</param>
        /// <param name="oCreatureToFloatAbove">The creature to display the text above</param>
        /// <param name="bBroadcastToFaction">If this is TRUE then only creatures in the same faction
        /// as oCreatureToFloatAbove will see the floaty text, and only if they are within range (30 metres)</param>
        public static void FloatingTextStrRefOnCreature(int nStrRefToDisplay, uint oCreatureToFloatAbove,
            bool bBroadcastToFaction = true) => _service.FloatingTextStrRefOnCreature(nStrRefToDisplay, oCreatureToFloatAbove, bBroadcastToFaction);

        /// <summary>
        /// Display floaty text above the specified creature.
        /// The text will also appear in the chat buffer of each player that receives the
        /// floaty text.
        /// </summary>
        /// <param name="sStringToDisplay">String to display</param>
        /// <param name="oCreatureToFloatAbove">The creature to display the text above</param>
        /// <param name="bBroadcastToFaction">If this is TRUE then only creatures in the same faction
        /// as oCreatureToFloatAbove will see the floaty text, and only if they are within range (30 metres)</param>
        public static void FloatingTextStringOnCreature(string sStringToDisplay, uint oCreatureToFloatAbove,
            bool bBroadcastToFaction = true) => _service.FloatingTextStringOnCreature(sStringToDisplay, oCreatureToFloatAbove, bBroadcastToFaction);

        /// <summary>
        /// Displays a message on the player's screen.
        /// The message is displayed on top of whatever is on the screen, including UI elements.
        /// </summary>
        /// <param name="PC">The player character to display the message to</param>
        /// <param name="Msg">The message to display</param>
        /// <param name="X">X coordinate of the first character to be displayed. The value is in terms
        /// of character 'slot' relative to the anchor point. If negative, applied from the right</param>
        /// <param name="Y">Y coordinate of the first character to be displayed. The value is in terms
        /// of character 'slot' relative to the anchor point. If negative, applied from the bottom</param>
        /// <param name="anchor">Screen anchor point constant</param>
        /// <param name="life">Duration in seconds until the string disappears</param>
        /// <param name="RGBA">Color of the string in 0xRRGGBBAA format</param>
        /// <param name="RGBA2">End color in 0xRRGGBBAA format. String starts at RGBA but slowly blends into RGBA2 as it nears end of life</param>
        /// <param name="ID">Optional ID of a string. If not 0, subsequent calls to PostString will
        /// remove the old string with the same ID, even if its lifetime has not elapsed. Only positive values allowed</param>
        /// <param name="font">If specified, use this custom font instead of default console font</param>
        public static void PostString(uint PC, string Msg, int X = 0, int Y = 0, ScreenAnchorType anchor = ScreenAnchorType.TopLeft,
            float life = 10.0f, int RGBA = 2147418367, int RGBA2 = 2147418367, int ID = 0, string font = "") => _service.PostString(PC, Msg, X, Y, anchor, life, RGBA, RGBA2, ID, font);

        /// <summary>
        /// Sets the object's hilite color.
        /// </summary>
        /// <param name="oObject">The object to set the hilite color for</param>
        /// <param name="nColor">Color in 0xRRGGBB format; -1 clears the color override</param>
        public static void SetObjectHiliteColor(uint oObject, int nColor = -1) => _service.SetObjectHiliteColor(oObject, nColor);

        /// <summary>
        /// Sets the cursor to use when hovering over the object.
        /// </summary>
        /// <param name="oObject">The object to set the mouse cursor for</param>
        /// <param name="nCursor">The mouse cursor type to use when hovering over the object</param>
        public static void SetObjectMouseCursor(uint oObject, MouseCursorType nCursor = MouseCursorType.Invalid) => _service.SetObjectMouseCursor(oObject, nCursor);

        /// <summary>
        /// Makes a player load a different texture instead of the original.
        /// </summary>
        /// <param name="OldName">The original texture name to replace</param>
        /// <param name="NewName">The new texture name to load instead. Setting to empty string will clear the override and revert to original</param>
        /// <param name="PC">The player character to apply the override to. If OBJECT_SELF, applies to all active players</param>
        public static void SetTextureOverride(string OldName, string NewName = "", uint PC = NWScriptService.OBJECT_INVALID) => _service.SetTextureOverride(OldName, NewName, PC);

        /// <summary>
        /// Gets a visual transform on the given object.
        /// </summary>
        /// <param name="oObject">Any valid Creature, Placeable, Item or Door</param>
        /// <param name="nTransform">One of OBJECT_VISUAL_TRANSFORM_* constants</param>
        /// <returns>The current (or default) value of the visual transform</returns>
        public static float GetObjectVisualTransform(uint oObject, ObjectVisualTransformType nTransform) => _service.GetObjectVisualTransform(oObject, nTransform);

        /// <summary>
        /// Sets a visual transform on the given object.
        /// </summary>
        /// <param name="oObject">Any valid Creature, Placeable, Item or Door</param>
        /// <param name="nTransform">One of OBJECT_VISUAL_TRANSFORM_* constants</param>
        /// <param name="fValue">Value depends on the transformation to apply</param>
        /// <param name="nLerpType">Lerp type for the transformation</param>
        /// <param name="fLerpDuration">Duration of the lerp transformation</param>
        /// <param name="bPauseWithGame">Whether to pause the transformation when the game is paused</param>
        /// <param name="nScope">One of OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_* constants, specific to the object type being transformed</param>
        /// <param name="nBehaviorFlags">Bitmask of OBJECT_VISUAL_TRANSFORM_BEHAVIOR_* constants</param>
        /// <param name="nRepeats">If > 0: N times, jump back to initial/from state after completing the transform. If -1: Do forever</param>
        /// <returns>The old/previous value of the visual transform</returns>
        public static float SetObjectVisualTransform(
            uint oObject,
            ObjectVisualTransformType nTransform,
            float fValue,
            LerpType nLerpType = LerpType.None,
            float fLerpDuration = 0.0f,
            bool bPauseWithGame = true,
            ObjectVisualTransformDataScopeType nScope = ObjectVisualTransformDataScopeType.Base,
            ObjectVisualTransformBehaviorType nBehaviorFlags = ObjectVisualTransformBehaviorType.Default,
            int nRepeats = 0) => _service.SetObjectVisualTransform(oObject, nTransform, fValue, nLerpType, fLerpDuration, bPauseWithGame, nScope, nBehaviorFlags, nRepeats);

        /// <summary>
        /// Sets an integer material shader uniform override.
        /// </summary>
        /// <param name="oObject">The object to set the shader uniform on</param>
        /// <param name="sMaterial">Material on that object</param>
        /// <param name="sParam">Valid shader parameter already defined on the material</param>
        /// <param name="nValue">Integer value to set</param>
        public static void SetMaterialShaderUniformInt(uint oObject, string sMaterial, string sParam, int nValue) => _service.SetMaterialShaderUniformInt(oObject, sMaterial, sParam, nValue);

        /// <summary>
        /// Sets a vec4 material shader uniform override.
        /// </summary>
        /// <param name="oObject">The object to set the shader uniform on</param>
        /// <param name="sMaterial">Material on that object</param>
        /// <param name="sParam">Valid shader parameter already defined on the material</param>
        /// <param name="fValue1">First float value (required)</param>
        /// <param name="fValue2">Second float value (optional, defaults to 0.0f)</param>
        /// <param name="fValue3">Third float value (optional, defaults to 0.0f)</param>
        /// <param name="fValue4">Fourth float value (optional, defaults to 0.0f). You can specify a single float value to set just a float, instead of a vec4</param>
        public static void SetMaterialShaderUniformVec4(uint oObject, string sMaterial, string sParam, float fValue1,
            float fValue2 = 0.0f, float fValue3 = 0.0f, float fValue4 = 0.0f) => _service.SetMaterialShaderUniformVec4(oObject, sMaterial, sParam, fValue1, fValue2, fValue3, fValue4);

        /// <summary>
        /// Resets material shader parameters on the given object.
        /// </summary>
        /// <param name="oObject">The object to reset shader uniforms on</param>
        /// <param name="sMaterial">Supply a material to only reset shader uniforms for meshes with that material</param>
        /// <param name="sParam">Supply a parameter to only reset shader uniforms of that name. Supply both to only reset shader uniforms of that name on meshes with that material</param>
        public static void ResetMaterialShaderUniforms(uint oObject, string sMaterial = "", string sParam = "") => _service.ResetMaterialShaderUniforms(oObject, sMaterial, sParam);

        /// <summary>
        /// Sets whether or not the creature's icon is flashing in their GUI icon bar.
        /// If the creature does not have an icon associated with the icon ID, nothing happens.
        /// This function does not add icons to the creature's GUI icon bar.
        /// The icon will flash until the underlying effect is removed or this function is called again with bFlashing = FALSE.
        /// </summary>
        /// <param name="oCreature">Player object to affect</param>
        /// <param name="nIconId">Referenced to effecticons.2da or EFFECT_ICON_* constants</param>
        /// <param name="bFlashing">TRUE to force an existing icon to flash, FALSE to stop</param>
        public static void SetEffectIconFlashing(uint oCreature, int nIconId, bool bFlashing = true) => _service.SetEffectIconFlashing(oCreature, nIconId, bFlashing);

        /// <summary>
        /// Immediately unsets visual transforms for the given object, with no lerp.
        /// </summary>
        /// <param name="oObject">The object to clear visual transforms from</param>
        /// <param name="nScope">One of OBJECT_VISUAL_TRANSFORM_DATA_SCOPE_ constants, or Invalid for all scopes</param>
        /// <returns>TRUE only if transforms were successfully removed (valid object, transforms existed)</returns>
        public static bool ClearObjectVisualTransform(uint oObject, ObjectVisualTransformDataScopeType nScope = ObjectVisualTransformDataScopeType.Invalid) => _service.ClearObjectVisualTransform(oObject, nScope);

        /// <summary>
        /// Sets the distance (in meters) at which object info will be sent to clients.
        /// This is still subject to other limitations, such as perception ranges for creatures.
        /// Note: Increasing visibility ranges of many objects can have a severe negative effect on
        /// network latency and server performance, and rendering additional objects will
        /// impact graphics performance of clients. Use cautiously.
        /// </summary>
        /// <param name="oObject">The object to set the visible distance for</param>
        /// <param name="fDistance">Distance in meters (default 45.0)</param>
        public static void SetObjectVisibleDistance(uint oObject, float fDistance = 45.0f) => _service.SetObjectVisibleDistance(oObject, fDistance);

        /// <summary>
        /// Gets the object's visible distance, as set by SetObjectVisibleDistance().
        /// </summary>
        /// <param name="oObject">The object to get the visible distance for</param>
        /// <returns>The visible distance in meters, or -1.0f on error</returns>
        public static float GetObjectVisibleDistance(uint oObject) => _service.GetObjectVisibleDistance(oObject);

        /// <summary>
        /// Replaces the object's animation with a new one.
        /// </summary>
        /// <param name="oObject">The object to replace the animation for</param>
        /// <param name="sOld">The old animation name to replace</param>
        /// <param name="sNew">The new animation name. Specifying empty string will restore the original animation</param>
        public static void ReplaceObjectAnimation(uint oObject, string sOld, string sNew = "") => _service.ReplaceObjectAnimation(oObject, sOld, sNew);

        /// <summary>
        /// Uses the placeable object.
        /// </summary>
        /// <param name="oPlaceable">The placeable object to use</param>
        public static void ActionInteractObject(uint oPlaceable) => _service.ActionInteractObject(oPlaceable);

        /// <summary>
        /// Gets the last object that used the specified placeable object.
        /// </summary>
        /// <returns>The last object that used the placeable, or OBJECT_INVALID if called by something other than a placeable or door</returns>
        public static uint GetLastUsedBy() => _service.GetLastUsedBy();

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
        /// <param name="oPlaceable">The placeable object (defaults to OBJECT_SELF)</param>
        /// <param name="bIlluminate">If TRUE, the placeable's illumination will be turned on. If FALSE, it will be turned off</param>
        public static void SetPlaceableIllumination(uint oPlaceable = NWScriptService.OBJECT_INVALID, bool bIlluminate = true) => _service.SetPlaceableIllumination(oPlaceable, bIlluminate);

        /// <summary>
        /// Returns TRUE if the illumination for the placeable is on.
        /// </summary>
        /// <param name="oPlaceable">The placeable object to check (defaults to OBJECT_SELF)</param>
        /// <returns>TRUE if the illumination is on</returns>
        public static bool GetPlaceableIllumination(uint oPlaceable = NWScriptService.OBJECT_INVALID) => _service.GetPlaceableIllumination(oPlaceable);

        /// <summary>
        /// Returns TRUE if the placeable action is valid for the placeable.
        /// </summary>
        /// <param name="oPlaceable">The placeable object</param>
        /// <param name="nPlaceableAction">PLACEABLE_ACTION_* constant</param>
        /// <returns>TRUE if the action is valid for the placeable</returns>
        public static int GetIsPlaceableObjectActionPossible(uint oPlaceable, int nPlaceableAction) => _service.GetIsPlaceableObjectActionPossible(oPlaceable, nPlaceableAction);

        /// <summary>
        /// The caller performs the placeable action on the placeable.
        /// </summary>
        /// <param name="oPlaceable">The placeable object</param>
        /// <param name="nPlaceableAction">PLACEABLE_ACTION_* constant</param>
        public static void DoPlaceableObjectAction(uint oPlaceable, int nPlaceableAction) => _service.DoPlaceableObjectAction(oPlaceable, nPlaceableAction);

        /// <summary>
        /// Instantly gives this creature the benefits of a rest (restored hitpoints, spells, feats, etc.).
        /// </summary>
        /// <param name="oCreature">The creature to force rest</param>
        public static void ForceRest(uint oCreature) => _service.ForceRest(oCreature);

        /// <summary>
        /// Returns TRUE if the creature is resting.
        /// </summary>
        /// <param name="oCreature">The creature to check (defaults to OBJECT_SELF)</param>
        /// <returns>TRUE if the creature is resting</returns>
        public static bool GetIsResting(uint oCreature = NWScriptService.OBJECT_INVALID) => _service.GetIsResting(oCreature);

        /// <summary>
        /// Gets the last PC that has rested in the module.
        /// </summary>
        /// <returns>The last PC that has rested</returns>
        public static uint GetLastPCRested() => _service.GetLastPCRested();

        /// <summary>
        /// Determines the type of the last rest event (as returned from the OnPCRested module event).
        /// </summary>
        /// <returns>The type (REST_EVENTTYPE_REST_*) of the last rest event</returns>
        public static RestEventType GetLastRestEventType() => _service.GetLastRestEventType();

        /// <summary>
        /// The creature will rest if not in combat and no enemies are nearby.
        /// </summary>
        /// <param name="bCreatureToEnemyLineOfSightCheck">TRUE to allow the creature to rest if enemies
        /// are nearby, but the creature can't see the enemy.
        /// FALSE the creature will not rest if enemies are
        /// nearby regardless of whether or not the creature
        /// can see them, such as if an enemy is close by,
        /// but is in a different room behind a closed door</param>
        public static void ActionRest(bool bCreatureToEnemyLineOfSightCheck = false) => _service.ActionRest(bCreatureToEnemyLineOfSightCheck);

        /// <summary>
        /// Sets the global shader uniform for the player to the specified float.
        /// These uniforms are not used by the base game and are reserved for module-specific scripting.
        /// You need to add custom shaders that will make use of them.
        /// In multiplayer, these need to be reapplied when a player rejoins.
        /// </summary>
        /// <param name="oPlayer">The player to set the shader uniform for</param>
        /// <param name="nShader">SHADER_UNIFORM_* constant</param>
        /// <param name="fValue">The float value to set</param>
        public static void SetShaderUniformFloat(uint oPlayer, ShaderUniformType nShader, float fValue) => _service.SetShaderUniformFloat(oPlayer, nShader, fValue);

        /// <summary>
        /// Sets the global shader uniform for the player to the specified integer.
        /// These uniforms are not used by the base game and are reserved for module-specific scripting.
        /// You need to add custom shaders that will make use of them.
        /// In multiplayer, these need to be reapplied when a player rejoins.
        /// </summary>
        /// <param name="oPlayer">The player to set the shader uniform for</param>
        /// <param name="nShader">SHADER_UNIFORM_* constant</param>
        /// <param name="nValue">The integer value to set</param>
        public static void SetShaderUniformInt(uint oPlayer, ShaderUniformType nShader, int nValue) => _service.SetShaderUniformInt(oPlayer, nShader, nValue);

        /// <summary>
        /// Sets the global shader uniform for the player to the specified vec4.
        /// These uniforms are not used by the base game and are reserved for module-specific scripting.
        /// You need to add custom shaders that will make use of them.
        /// In multiplayer, these need to be reapplied when a player rejoins.
        /// </summary>
        /// <param name="oPlayer">The player to set the shader uniform for</param>
        /// <param name="nShader">SHADER_UNIFORM_* constant</param>
        /// <param name="fX">The X component of the vec4</param>
        /// <param name="fY">The Y component of the vec4</param>
        /// <param name="fZ">The Z component of the vec4</param>
        /// <param name="fW">The W component of the vec4</param>
        public static void SetShaderUniformVec(uint oPlayer, ShaderUniformType nShader, float fX, float fY, float fZ, float fW) => _service.SetShaderUniformVec(oPlayer, nShader, fX, fY, fZ, fW);

        /// <summary>
        /// Makes a player character enter a targeting mode, letting them select an object as a target.
        /// If a PC selects a target, it will trigger the module OnPlayerTarget event.
        /// </summary>
        /// <param name="oPC">The player character to enter targeting mode</param>
        /// <param name="nValidObjectTypes">The valid object types that can be targeted</param>
        /// <param name="nMouseCursorId">The mouse cursor to display when hovering over valid targets</param>
        /// <param name="nBadTargetCursor">The mouse cursor to display when hovering over invalid targets</param>
        public static void EnterTargetingMode(uint oPC, ObjectType nValidObjectTypes = ObjectType.All, MouseCursorType nMouseCursorId = MouseCursorType.Magic, MouseCursorType nBadTargetCursor = MouseCursorType.NoMagic) => _service.EnterTargetingMode(oPC, nValidObjectTypes, nMouseCursorId, nBadTargetCursor);

        /// <summary>
        /// Gets the target object in the module OnPlayerTarget event.
        /// Returns the area object when the target is the ground.
        /// </summary>
        /// <returns>The selected target object, or the area object when targeting the ground</returns>
        public static uint GetTargetingModeSelectedObject() => _service.GetTargetingModeSelectedObject();

        /// <summary>
        /// Gets the target position in the module OnPlayerTarget event.
        /// </summary>
        /// <returns>The selected target position as a Vector3</returns>
        public static Vector3 GetTargetingModeSelectedPosition() => _service.GetTargetingModeSelectedPosition();

        /// <summary>
        /// Gets the player object that triggered the OnPlayerTarget event.
        /// </summary>
        /// <returns>The player object that last selected a target</returns>
        public static uint GetLastPlayerToSelectTarget() => _service.GetLastPlayerToSelectTarget();

        /// <summary>
        /// Sets the spell targeting data manually for the player.
        /// This data is usually specified in spells.2da.
        /// This data persists through spell casts; you're overwriting the entry in spells.2da for this session.
        /// In multiplayer, these need to be reapplied when a player rejoins.
        /// </summary>
        /// <param name="oPlayer">The player to set the targeting data for</param>
        /// <param name="nSpell">SPELL_* constant</param>
        /// <param name="nShape">SPELL_TARGETING_SHAPE_* constant</param>
        /// <param name="fSizeX">Size X for the targeting area</param>
        /// <param name="fSizeY">Size Y for the targeting area</param>
        /// <param name="nFlags">SPELL_TARGETING_FLAGS_* constants</param>
        public static void SetSpellTargetingData(uint oPlayer, SpellType nSpell, int nShape, float fSizeX, float fSizeY, int nFlags) => _service.SetSpellTargetingData(oPlayer, nSpell, nShape, fSizeX, fSizeY, nFlags);

        /// <summary>
        /// Sets the spell targeting data which is used for the next call to EnterTargetingMode() for this player.
        /// If the shape is set to SPELL_TARGETING_SHAPE_NONE and the range is provided, the dotted line range indicator will still appear.
        /// </summary>
        /// <param name="oPlayer">The player to set the targeting data for</param>
        /// <param name="nShape">SPELL_TARGETING_SHAPE_* constant</param>
        /// <param name="fSizeX">Size X for the targeting area</param>
        /// <param name="fSizeY">Size Y for the targeting area</param>
        /// <param name="nFlags">SPELL_TARGETING_FLAGS_* constants</param>
        /// <param name="fRange">Range for the targeting area (optional, defaults to 0.0f)</param>
        /// <param name="nSpell">SPELL_* constant (optional, passed to the shader but does nothing by default, you need to edit the shader to use it)</param>
        /// <param name="nFeat">FEAT_* constant (optional, passed to the shader but does nothing by default, you need to edit the shader to use it)</param>
        public static void SetEnterTargetingModeData(
            uint oPlayer,
            int nShape,
            float fSizeX,
            float fSizeY,
            int nFlags,
            float fRange = 0.0f,
            SpellType nSpell = SpellType.AllSpells,
            FeatType nFeat = FeatType.Invalid) => _service.SetEnterTargetingModeData(oPlayer, nShape, fSizeX, fSizeY, nFlags, fRange, nSpell, nFeat);

        /// <summary>
        /// Adds a journal quest entry to the creature.
        /// </summary>
        /// <param name="szPlotID">The plot identifier used in the toolset's Journal Editor</param>
        /// <param name="nState">The state of the plot as seen in the toolset's Journal Editor</param>
        /// <param name="oCreature">The creature to add the journal entry to</param>
        /// <param name="bAllPartyMembers">If TRUE, the entry will show up in the journal of everyone in the party</param>
        /// <param name="bAllPlayers">If TRUE, the entry will show up in the journal of everyone in the world</param>
        /// <param name="bAllowOverrideHigher">If TRUE, you can set the state to a lower number than the one it is currently on</param>
        public static void AddJournalQuestEntry(string szPlotID, int nState, uint oCreature,
            bool bAllPartyMembers = true, bool bAllPlayers = false, bool bAllowOverrideHigher = false) => _service.AddJournalQuestEntry(szPlotID, nState, oCreature, bAllPartyMembers, bAllPlayers, bAllowOverrideHigher);

        /// <summary>
        /// Removes a journal quest entry from the creature.
        /// </summary>
        /// <param name="szPlotID">The plot identifier used in the toolset's Journal Editor</param>
        /// <param name="oCreature">The creature to remove the journal entry from</param>
        /// <param name="bAllPartyMembers">If TRUE, the entry will be removed from the journal of everyone in the party</param>
        /// <param name="bAllPlayers">If TRUE, the entry will be removed from the journal of everyone in the world</param>
        public static void RemoveJournalQuestEntry(string szPlotID, uint oCreature, bool bAllPartyMembers = true,
            bool bAllPlayers = false) => _service.RemoveJournalQuestEntry(szPlotID, oCreature, bAllPartyMembers, bAllPlayers);

        /// <summary>
        /// Returns the resource location of the specified resource, as seen by the running module.
        /// Note for dedicated servers: Checks on the module/server side, not the client.
        /// </summary>
        /// <param name="sResRef">The resource reference</param>
        /// <param name="nResType">The resource type</param>
        /// <returns>The resource location, or empty string if the resource does not exist in the search space</returns>
        public static string ResManGetAliasFor(string sResRef, ResType nResType) => _service.ResManGetAliasFor(sResRef, nResType);

        /// <summary>
        /// Finds the nth available resref starting with the specified prefix.
        /// Set bSearchBaseData to TRUE to also search base game content stored in your game installation directory.
        /// WARNING: This can be very slow.
        /// Set sOnlyKeyTable to a specific keytable to only search the given named keytable (e.g. "OVERRIDE:").
        /// </summary>
        /// <param name="sPrefix">The prefix to search for</param>
        /// <param name="nResType">The resource type</param>
        /// <param name="nNth">The nth occurrence to find (defaults to 1)</param>
        /// <param name="bSearchBaseData">Whether to also search base game content (WARNING: This can be very slow)</param>
        /// <param name="sOnlyKeyTable">Specific keytable to search (e.g. "OVERRIDE:")</param>
        /// <returns>The found resref, or empty string if no such resref exists</returns>
        public static string ResManFindPrefix(string sPrefix, ResType nResType, int nNth = 1, bool bSearchBaseData = false, string sOnlyKeyTable = "") => _service.ResManFindPrefix(sPrefix, nResType, nNth, bSearchBaseData, sOnlyKeyTable);

        /// <summary>
        /// Gets the contents of a file as string, as seen by the server's resman.
        /// Note: If the file contains binary data it will return data up to the first null byte.
        /// </summary>
        /// <param name="sResRef">The resource reference</param>
        /// <param name="nResType">A RESTYPE_* constant</param>
        /// <returns>The file contents, or empty string if the file does not exist</returns>
        public static string ResManGetFileContents(string sResRef, int nResType) => _service.ResManGetFileContents(sResRef, nResType);

        /// <summary>
        /// Gets the length of the string.
        /// </summary>
        /// <param name="sString">The string to get the length of</param>
        /// <returns>The length of the string, or -1 on error</returns>
        public static int GetStringLength(string sString) => _service.GetStringLength(sString);

        /// <summary>
        /// Converts the string to upper case.
        /// </summary>
        /// <param name="sString">The string to convert</param>
        /// <returns>The string in upper case, or empty string on error</returns>
        public static string GetStringUpperCase(string sString) => _service.GetStringUpperCase(sString);

        /// <summary>
        /// Converts the string to lower case.
        /// </summary>
        /// <param name="sString">The string to convert</param>
        /// <returns>The string in lower case, or empty string on error</returns>
        public static string GetStringLowerCase(string sString) => _service.GetStringLowerCase(sString);

        /// <summary>
        /// Gets the specified number of characters from the right end of the string.
        /// </summary>
        /// <param name="sString">The string to get characters from</param>
        /// <param name="nCount">The number of characters to get from the right end</param>
        /// <returns>The rightmost characters, or empty string on error</returns>
        public static string GetStringRight(string sString, int nCount) => _service.GetStringRight(sString, nCount);

        /// <summary>
        /// Gets the specified number of characters from the left end of the string.
        /// </summary>
        /// <param name="sString">The string to get characters from</param>
        /// <param name="nCount">The number of characters to get from the left end</param>
        /// <returns>The leftmost characters, or empty string on error</returns>
        public static string GetStringLeft(string sString, int nCount) => _service.GetStringLeft(sString, nCount);

        /// <summary>
        /// Inserts a string into the destination string at the specified position.
        /// </summary>
        /// <param name="sDestination">The destination string to insert into</param>
        /// <param name="sString">The string to insert</param>
        /// <param name="nPosition">The position to insert at</param>
        /// <returns>The resulting string, or empty string on error</returns>
        public static string InsertString(string sDestination, string sString, int nPosition) => _service.InsertString(sDestination, sString, nPosition);

        /// <summary>
        /// Gets the specified number of characters from the string, starting at the specified position.
        /// </summary>
        /// <param name="sString">The string to get characters from</param>
        /// <param name="nStart">The starting position</param>
        /// <param name="nCount">The number of characters to get</param>
        /// <returns>The substring, or empty string on error</returns>
        public static string GetSubString(string sString, int nStart, int nCount) => _service.GetSubString(sString, nStart, nCount);

        /// <summary>
        /// Finds the position of the substring inside the string.
        /// </summary>
        /// <param name="sString">The string to search in</param>
        /// <param name="sSubString">The substring to search for</param>
        /// <param name="nStart">The character position to start searching at (from the left end of the string)</param>
        /// <returns>The position of the substring, or -1 on error</returns>
        public static int FindSubString(string sString, string sSubString, int nStart = 0) => _service.FindSubString(sString, sSubString, nStart);

        /// <summary>
        /// Tests if the string matches the pattern.
        /// </summary>
        /// <param name="sPattern">The pattern to match against</param>
        /// <param name="sStringToTest">The string to test</param>
        /// <returns>TRUE if the string matches the pattern</returns>
        public static bool TestStringAgainstPattern(string sPattern, string sStringToTest) => _service.TestStringAgainstPattern(sPattern, sStringToTest);

        /// <summary>
        /// Gets the appropriate matched string (this should only be used in OnConversation scripts).
        /// </summary>
        /// <param name="nString">The string index</param>
        /// <returns>The appropriate matched string, otherwise returns empty string</returns>
        public static string GetMatchedSubstring(int nString) => _service.GetMatchedSubstring(nString);

        /// <summary>
        /// Gets the number of string parameters available.
        /// </summary>
        /// <returns>The number of string parameters available, or -1 if no string matched (this could be because of a dialogue event)</returns>
        public static int GetMatchedSubstringsCount() => _service.GetMatchedSubstringsCount();

        /// <summary>
        /// Replaces all matching regular expression in the value with the replacement.
        /// Returns an empty string on error.
        /// Please see the format documentation for replacement patterns.
        /// FORMAT_DEFAULT replacement patterns:
        /// $$    $
        /// $&    The matched substring.
        /// $`    The portion of string that precedes the matched substring.
        /// $'    The portion of string that follows the matched substring.
        /// $n    The nth capture, where n is a single digit in the range 1 to 9 and $n is not followed by a decimal digit.
        /// $nn   The nnth capture, where nn is a two-digit decimal number in the range 01 to 99.
        /// Example: RegExpReplace("a+", "vaaalue", "[$&]")    => "v[aaa]lue"
        /// </summary>
        /// <param name="sRegExp">The regular expression to match</param>
        /// <param name="sValue">The string to search and replace in</param>
        /// <param name="sReplacement">The replacement string</param>
        /// <param name="nSyntaxFlags">A mask of REGEXP_* constants</param>
        /// <param name="nMatchFlags">A mask of REGEXP_MATCH_* and REGEXP_FORMAT_* constants</param>
        /// <returns>The string with replacements made, or empty string on error</returns>
        public static string RegExpReplace(
            string sRegExp,
            string sValue,
            string sReplacement,
            RegularExpressionType nSyntaxFlags = RegularExpressionType.Ecmascript,
            RegularExpressionFormatType nMatchFlags = RegularExpressionFormatType.Default) => _service.RegExpReplace(sRegExp, sValue, sReplacement, nSyntaxFlags, nMatchFlags);

        /// <summary>
        /// Overrides a given strref to always return the specified value instead of what is in the TLK file.
        /// Setting sValue to empty string will delete the override.
        /// </summary>
        /// <param name="nStrRef">The string reference to override</param>
        /// <param name="sValue">The value to return instead of the TLK file value (empty string to delete override)</param>
        public static void SetTlkOverride(int nStrRef, string sValue = "") => _service.SetTlkOverride(nStrRef, sValue);

        /// <summary>
        /// Converts a float into a string.
        /// </summary>
        /// <param name="fFloat">The float to convert</param>
        /// <param name="nWidth">Should be a value from 0 to 18 inclusive</param>
        /// <param name="nDecimals">Should be a value from 0 to 9 inclusive</param>
        /// <returns>The float as a string</returns>
        public static string FloatToString(float fFloat, int nWidth = 18, int nDecimals = 9) => _service.FloatToString(fFloat, nWidth, nDecimals);

        /// <summary>
        /// Converts an integer into a string.
        /// </summary>
        /// <param name="nInteger">The integer to convert</param>
        /// <returns>The integer as a string, or empty string on error</returns>
        public static string IntToString(int nInteger) => _service.IntToString(nInteger);

        /// <summary>
        /// Gets a string from the talk table using the string reference.
        /// </summary>
        /// <param name="nStrRef">The string reference to look up</param>
        /// <param name="nGender">The gender for gender-specific strings</param>
        /// <returns>The string from the talk table</returns>
        public static string GetStringByStrRef(int nStrRef, GenderType nGender = GenderType.Male) => _service.GetStringByStrRef(nStrRef, nGender);

        /// <summary>
        /// Generates a random name.
        /// </summary>
        /// <param name="nNameType">The type of random name to be generated (NAME_*)</param>
        /// <returns>A random name of the specified type</returns>
        public static string RandomName(NameType nNameType = NameType.FirstGenericMale) => _service.RandomName(nNameType);

        /// <summary>
        /// Sets the value for a custom token.
        /// </summary>
        /// <param name="nCustomTokenNumber">The custom token number</param>
        /// <param name="sTokenValue">The value to set for the token</param>
        public static void SetCustomToken(int nCustomTokenNumber, string sTokenValue) => _service.SetCustomToken(nCustomTokenNumber, sTokenValue);


        /// <summary>
        /// Make oTarget run sScript and then return execution to the calling script.
        /// If sScript does not specify a compiled script, nothing happens.
        ///
        /// This method automatically chooses between C# and NWScript execution:
        /// - If a C# script exists, it will be executed using proper VM context management
        /// - If no C# script exists, it falls back to NWScript execution
        ///
        /// This solves OBJECT_SELF corruption issues when executing C# scripts by bypassing
        /// the problematic NWScript round-trip.
        /// </summary>
        public static void ExecuteNWScript(string sScript, uint oTarget) => _service.ExecuteNWScript(sScript, oTarget);

        /// <summary>
        /// Destroy oObject (irrevocably).
        /// This will not work on modules and areas.
        /// </summary>
        /// <remarks>
        /// This is a custom implementation that extends the base NWScript DestroyObject function
        /// to call a cleanup script after object destruction for proper event handling.
        /// </remarks>
        /// <param name="oDestroy">The object to destroy.</param>
        /// <param name="fDelay">The delay before destruction (default: 0.0f).</param>
        public static void DestroyObject(uint oDestroy, float fDelay = 0.0f) => _service.DestroyObject(oDestroy, fDelay);

        /// <summary>
        /// Removes all effects with the specified tag(s) from a creature.
        /// </summary>
        /// <param name="creature">The creature to remove effects from.</param>
        /// <param name="tags">The tags to look for.</param>
        public static void RemoveEffectByTag(uint creature, params string[] tags) => _service.RemoveEffectByTag(creature, tags);

        /// <summary>
        /// Removes all effects with the specified types from a creature.
        /// </summary>
        /// <param name="creature">The creature to remove from.</param>
        /// <param name="types">The types of effects to look for.</param>
        public static void RemoveEffect(uint creature, params EffectScriptType[] types) => _service.RemoveEffect(creature, types);

        /// <summary>
        /// Determines if creature has at least one effect with the specified tags.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <param name="tags">The effect tags to check for</param>
        /// <returns>true if at least one effect was found, false otherwise</returns>
        public static bool HasEffectByTag(uint creature, params string[] tags) => _service.HasEffectByTag(creature, tags);

        /// <summary>
        /// Determines if a creature has a more powerful effect active based on the provided effect tag/level mapping provided.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <param name="tier">The tier attempting to be applied</param>
        /// <param name="effectLevels">The tag/level mapping of all levels.</param>
        /// <returns>true if a more powerful effect is in place, false otherwise</returns>
        public static bool HasMorePowerfulEffect(uint creature, int tier, params (string, int)[] effectLevels) => _service.HasMorePowerfulEffect(creature, tier, effectLevels);

    }
}
