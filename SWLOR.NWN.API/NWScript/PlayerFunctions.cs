using System.Numerics;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Gets the current cutscene state of the player specified by the creature.
        /// </summary>
        /// <param name="oCreature">The creature to check (defaults to OBJECT_INVALID)</param>
        /// <returns>TRUE if the player is in cutscene mode, FALSE if not in cutscene mode or on error</returns>
        public static bool GetCutsceneMode(uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCutsceneMode(oCreature) != 0;
        }

        /// <summary>
        /// Forces this player's camera to be set to this height.
        /// Setting this value to zero will restore the camera to the racial default height.
        /// </summary>
        /// <param name="oPlayer">The player to set the camera height for</param>
        /// <param name="fHeight">The camera height (defaults to 0.0f)</param>
        public static void SetCameraHeight(uint oPlayer, float fHeight = 0.0f)
        {
            global::NWN.Core.NWScript.SetCameraHeight(oPlayer, fHeight);
        }

        /// <summary>
        /// Changes the current Day/Night cycle for this player to night.
        /// </summary>
        /// <param name="oPlayer">Which player to change the lighting for</param>
        /// <param name="fTransitionTime">How long the transition should take (defaults to 0.0f)</param>
        public static void DayToNight(uint oPlayer, float fTransitionTime = 0.0f)
        {
            global::NWN.Core.NWScript.DayToNight(oPlayer, fTransitionTime);
        }

        /// <summary>
        /// Changes the current Day/Night cycle for this player to daylight.
        /// </summary>
        /// <param name="oPlayer">Which player to change the lighting for</param>
        /// <param name="fTransitionTime">How long the transition should take (defaults to 0.0f)</param>
        public static void NightToDay(uint oPlayer, float fTransitionTime = 0.0f)
        {
            global::NWN.Core.NWScript.NightToDay(oPlayer, fTransitionTime);
        }

        /// <summary>
        /// Returns the current movement rate factor of the cutscene 'camera man'.
        /// NOTE: This will be a value between 0.1, 2.0 (10%-200%)
        /// </summary>
        /// <param name="oCreature">The creature to get the camera move rate for</param>
        /// <returns>The movement rate factor between 0.1 and 2.0</returns>
        public static float GetCutsceneCameraMoveRate(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetCutsceneCameraMoveRate(oCreature);
        }

        /// <summary>
        /// Sets the current movement rate factor for the cutscene camera man.
        /// NOTE: You can only set values between 0.1, 2.0 (10%-200%)
        /// </summary>
        /// <param name="oCreature">The creature to set the camera move rate for</param>
        /// <param name="fRate">The movement rate factor (between 0.1 and 2.0)</param>
        public static void SetCutsceneCameraMoveRate(uint oCreature, float fRate)
        {
            global::NWN.Core.NWScript.SetCutsceneCameraMoveRate(oCreature, fRate);
        }

        /// <summary>
        /// Makes a player examine the object. This causes the examination
        /// pop-up box to appear for the object specified.
        /// </summary>
        /// <param name="oExamine">The object to examine</param>
        public static void ActionExamine(uint oExamine)
        {
            global::NWN.Core.NWScript.ActionExamine(oExamine);
        }

        /// <summary>
        /// Use this to get the item last equipped by a player character in OnPlayerEquipItem.
        /// </summary>
        /// <returns>The item last equipped</returns>
        public static uint GetPCItemLastEquipped()
        {
            return global::NWN.Core.NWScript.GetPCItemLastEquipped();
        }

        /// <summary>
        /// Use this to get the player character who last equipped an item in OnPlayerEquipItem.
        /// </summary>
        /// <returns>The player character who last equipped an item</returns>
        public static uint GetPCItemLastEquippedBy()
        {
            return global::NWN.Core.NWScript.GetPCItemLastEquippedBy();
        }

        /// <summary>
        /// Use this to get the item last unequipped by a player character in OnPlayerEquipItem.
        /// </summary>
        /// <returns>The item last unequipped</returns>
        public static uint GetPCItemLastUnequipped()
        {
            return global::NWN.Core.NWScript.GetPCItemLastUnequipped();
        }

        /// <summary>
        /// Use this to get the player character who last unequipped an item in OnPlayerUnEquipItem.
        /// </summary>
        /// <returns>The player character who last unequipped an item</returns>
        public static uint GetPCItemLastUnequippedBy()
        {
            return global::NWN.Core.NWScript.GetPCItemLastUnequippedBy();
        }

        /// <summary>
        /// Sends a server message to the player using a string reference.
        /// </summary>
        /// <param name="oPlayer">The player to send the message to</param>
        /// <param name="nStrRef">The string reference to send</param>
        public static void SendMessageToPCByStrRef(uint oPlayer, int nStrRef)
        {
            global::NWN.Core.NWScript.SendMessageToPCByStrRef(oPlayer, nStrRef);
        }

        /// <summary>
        /// Opens this creature's inventory panel for this player.
        /// DM's can view any creature's inventory.
        /// Players can view their own inventory, or that of their henchman, familiar or animal companion.
        /// </summary>
        /// <param name="oCreature">Creature to view</param>
        /// <param name="oPlayer">The owner of this creature will see the panel pop up</param>
        public static void OpenInventory(uint oCreature, uint oPlayer)
        {
            global::NWN.Core.NWScript.OpenInventory(oCreature, oPlayer);
        }

        /// <summary>
        /// Stores the current camera mode and position so that it can be restored (using RestoreCameraFacing()).
        /// </summary>
        public static void StoreCameraFacing()
        {
            global::NWN.Core.NWScript.StoreCameraFacing();
        }

        /// <summary>
        /// Restores the camera mode and position to what they were last time StoreCameraFacing was called.
        /// RestoreCameraFacing can only be called once, and must correspond to a previous call to StoreCameraFacing.
        /// </summary>
        public static void RestoreCameraFacing()
        {
            global::NWN.Core.NWScript.RestoreCameraFacing();
        }

        /// <summary>
        /// Fades the screen for the given creature/player from black to regular screen.
        /// </summary>
        /// <param name="oCreature">Creature controlled by player that should fade from black</param>
        /// <param name="fSpeed">The fade speed (defaults to FadeSpeed.Medium)</param>
        public static void FadeFromBlack(uint oCreature, float fSpeed = FadeSpeed.Medium)
        {
            global::NWN.Core.NWScript.FadeFromBlack(oCreature, fSpeed);
        }

        /// <summary>
        /// Fades the screen for the given creature/player from regular screen to black.
        /// </summary>
        /// <param name="oCreature">Creature controlled by player that should fade to black</param>
        /// <param name="fSpeed">The fade speed (defaults to FadeSpeed.Medium)</param>
        public static void FadeToBlack(uint oCreature, float fSpeed = FadeSpeed.Medium)
        {
            global::NWN.Core.NWScript.FadeToBlack(oCreature, fSpeed);
        }

        /// <summary>
        /// Removes any fading or black screen.
        /// </summary>
        /// <param name="oCreature">Creature controlled by player that should be cleared</param>
        public static void StopFade(uint oCreature)
        {
            global::NWN.Core.NWScript.StopFade(oCreature);
        }

        /// <summary>
        /// Sets the screen to black. Can be used in preparation for a fade-in (FadeFromBlack).
        /// Can be cleared by either doing a FadeFromBlack, or by calling StopFade.
        /// </summary>
        /// <param name="oCreature">Creature controlled by player that should see black screen</param>
        public static void BlackScreen(uint oCreature)
        {
            global::NWN.Core.NWScript.BlackScreen(oCreature);
        }

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
        public static void SetCutsceneMode(uint oCreature, bool nInCutscene = true, bool nLeftClickingEnabled = false)
        {
            global::NWN.Core.NWScript.SetCutsceneMode(oCreature, nInCutscene ? 1 : 0, nLeftClickingEnabled ? 1 : 0);
        }

        /// <summary>
        /// Gets the last player character to cancel from a cutscene.
        /// </summary>
        /// <returns>The last player character to cancel from a cutscene</returns>
        public static uint GetLastPCToCancelCutscene()
        {
            return global::NWN.Core.NWScript.GetLastPCToCancelCutscene();
        }

        /// <summary>
        /// Removes the player from the server.
        /// You can optionally specify a reason to override the text shown to the player.
        /// </summary>
        /// <param name="oPlayer">The player to remove from the server</param>
        /// <param name="sReason">Optional reason to override the text shown to the player</param>
        public static void BootPC(uint oPlayer, string sReason = "")
        {
            global::NWN.Core.NWScript.BootPC(oPlayer, sReason);
        }

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
            bool bWaitForHelpButtonEnabled = true, int nHelpStringReference = 0, string sHelpString = "")
        {
            global::NWN.Core.NWScript.PopUpDeathGUIPanel(oPC, bRespawnButtonEnabled ? 1 : 0, bWaitForHelpButtonEnabled ? 1 : 0, nHelpStringReference, sHelpString);
        }

        /// <summary>
        /// Gets the first PC in the player list.
        /// This resets the position in the player list for GetNextPC().
        /// </summary>
        /// <returns>The first PC in the player list</returns>
        public static uint GetFirstPC()
        {
            return global::NWN.Core.NWScript.GetFirstPC();
        }

        /// <summary>
        /// Gets the next PC in the player list.
        /// This picks up where the last GetFirstPC() or GetNextPC() left off.
        /// </summary>
        /// <returns>The next PC in the player list</returns>
        public static uint GetNextPC()
        {
            return global::NWN.Core.NWScript.GetNextPC();
        }

        /// <summary>
        /// Gets the last PC that levelled up.
        /// </summary>
        /// <returns>The last PC that levelled up</returns>
        public static uint GetPCLevellingUp()
        {
            return global::NWN.Core.NWScript.GetPCLevellingUp();
        }

        /// <summary>
        /// Sets the camera mode for the player.
        /// If the player is not player-controlled or nCameraMode is invalid, nothing happens.
        /// </summary>
        /// <param name="oPlayer">The player to set the camera mode for</param>
        /// <param name="nCameraMode">CAMERA_MODE_* constant</param>
        public static void SetCameraMode(uint oPlayer, int nCameraMode)
        {
            global::NWN.Core.NWScript.SetCameraMode(oPlayer, nCameraMode);
        }

        /// <summary>
        /// Use this in an OnPlayerDying module script to get the last player who is dying.
        /// </summary>
        /// <returns>The last player who is dying</returns>
        public static uint GetLastPlayerDying()
        {
            return global::NWN.Core.NWScript.GetLastPlayerDying();
        }

        /// <summary>
        /// Spawns a GUI panel for the client that controls the PC.
        /// Will force show panels disabled with SetGuiPanelDisabled().
        /// Nothing happens if the PC is not a player character or if an invalid value is used for nGUIPanel.
        /// </summary>
        /// <param name="oPC">The player character</param>
        /// <param name="nGUIPanel">GUI_PANEL_* constant, except GUI_PANEL_COMPASS / GUI_PANEL_LEVELUP / GUI_PANEL_GOLD_* / GUI_PANEL_EXAMINE_*</param>
        public static void PopUpGUIPanel(uint oPC, GuiPanel nGUIPanel)
        {
            global::NWN.Core.NWScript.PopUpGUIPanel(oPC, (int)nGUIPanel);
        }


        /// <summary>
        /// Returns the build number of the player (i.e. 8193).
        /// </summary>
        /// <param name="oPlayer">The player to get the build version for</param>
        /// <returns>The build number, or 0 if the given object isn't a player or did not advertise their build info</returns>
        public static int GetPlayerBuildVersionMajor(uint oPlayer)
        {
            return global::NWN.Core.NWScript.GetPlayerBuildVersionMajor(oPlayer);
        }

        /// <summary>
        /// Returns the patch revision of the player (i.e. 8).
        /// </summary>
        /// <param name="oPlayer">The player to get the patch revision for</param>
        /// <returns>The patch revision, or 0 if the given object isn't a player or did not advertise their build info</returns>
        public static int GetPlayerBuildVersionMinor(uint oPlayer)
        {
            return global::NWN.Core.NWScript.GetPlayerBuildVersionMinor(oPlayer);
        }


        /// <summary>
        /// Returns TRUE if the given player-controlled creature has DM privileges
        /// gained through a player login (as opposed to the DM client).
        /// Note: GetIsDM() also returns TRUE for player creature DMs.
        /// </summary>
        /// <param name="oCreature">The creature to check</param>
        /// <returns>TRUE if the creature has player DM privileges</returns>
        public static bool GetIsPlayerDM(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetIsPlayerDM(oCreature) != 0;
        }

        /// <summary>
        /// Gets the player that last triggered the module OnPlayerGuiEvent event.
        /// </summary>
        /// <returns>The player that last triggered the GUI event</returns>
        public static uint GetLastGuiEventPlayer()
        {
            return global::NWN.Core.NWScript.GetLastGuiEventPlayer();
        }

        /// <summary>
        /// Gets the last triggered GUIEVENT_* in the module OnPlayerGuiEvent event.
        /// </summary>
        /// <returns>The last triggered GUI event type</returns>
        public static GuiEventType GetLastGuiEventType()
        {
            return (GuiEventType)global::NWN.Core.NWScript.GetLastGuiEventType();
        }

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
        public static int GetLastGuiEventInteger()
        {
            return global::NWN.Core.NWScript.GetLastGuiEventInteger();
        }

        /// <summary>
        /// Gets an optional object of specific GUI events in the module OnPlayerGuiEvent event.
        /// GUIEVENT_MINIMAP_MAPPIN_CLICK: The waypoint the map note is attached to.
        /// GUIEVENT_CHARACTERSHEET_*_SELECT: The owner of the character sheet.
        /// GUIEVENT_PLAYERLIST_PLAYER_CLICK: The player clicked on.
        /// GUIEVENT_PARTYBAR_PORTRAIT_CLICK: The creature clicked on.
        /// GUIEVENT_DISABLED_PANEL_ATTEMPT_OPEN: For GUI_PANEL_CHARACTERSHEET, the owner of the character sheet.
        /// </summary>
        /// <returns>The object for the specific GUI event</returns>
        public static uint GetLastGuiEventObject()
        {
            return global::NWN.Core.NWScript.GetLastGuiEventObject();
        }

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
        /// <param name="oTarget">The target object (defaults to OBJECT_INVALID)</param>
        public static void SetGuiPanelDisabled(uint oPlayer, GuiPanel nGuiPanel, bool bDisabled, uint oTarget = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetGuiPanelDisabled(oPlayer, (int)nGuiPanel, bDisabled ? 1 : 0, oTarget);
        }

        /// <summary>
        /// Gets the ID (1..8) of the last tile action performed in OnPlayerTileAction.
        /// </summary>
        /// <returns>The ID of the last tile action</returns>
        public static int GetLastTileActionId()
        {
            return global::NWN.Core.NWScript.GetLastTileActionId();
        }

        /// <summary>
        /// Gets the target position in the module OnPlayerTileAction event.
        /// </summary>
        /// <returns>The target position of the last tile action</returns>
        public static Vector3 GetLastTileActionPosition()
        {
            return global::NWN.Core.NWScript.GetLastTileActionPosition();
        }

        /// <summary>
        /// Gets the player object that triggered the OnPlayerTileAction event.
        /// </summary>
        /// <returns>The player object that triggered the tile action</returns>
        public static uint GetLastPlayerToDoTileAction()
        {
            return global::NWN.Core.NWScript.GetLastPlayerToDoTileAction();
        }

        /// <summary>
        /// Gets a device property/capability as advertised by the client.
        /// Returns -1 if the property was never set by the client, the actual value is -1,
        /// the player is running an older build that does not advertise device properties,
        /// or the player has disabled sending device properties (Options->Game->Privacy).
        /// </summary>
        /// <param name="oPlayer">The player to get the device property for</param>
        /// <param name="sProperty">One of PLAYER_DEVICE_PROPERTY_xxx constants</param>
        /// <returns>The device property value, or -1 if unavailable</returns>
        public static int GetPlayerDeviceProperty(uint oPlayer, string sProperty)
        {
            return global::NWN.Core.NWScript.GetPlayerDeviceProperty(oPlayer, sProperty);
        }

        /// <summary>
        /// Returns the LANGUAGE_xx code of the given player, or -1 if unavailable.
        /// </summary>
        /// <param name="oPlayer">The player to get the language for</param>
        /// <returns>The language code, or -1 if unavailable</returns>
        public static PlayerLanguageType GetPlayerLanguage(uint oPlayer)
        {
            return (PlayerLanguageType)global::NWN.Core.NWScript.GetPlayerLanguage(oPlayer);
        }

        /// <summary>
        /// Returns one of PLAYER_DEVICE_PLATFORM_xxx, or 0 if unavailable.
        /// </summary>
        /// <param name="oPlayer">The player to get the device platform for</param>
        /// <returns>The device platform type, or 0 if unavailable</returns>
        public static PlayerDevicePlatformType GetPlayerDevicePlatform(uint oPlayer)
        {
            return (PlayerDevicePlatformType)global::NWN.Core.NWScript.GetPlayerDevicePlatform(oPlayer);
        }

        /// <summary>
        /// Returns the patch postfix of the player (i.e. the 29 out of "87.8193.35-29 abcdef01").
        /// </summary>
        /// <param name="oPlayer">The player to get the build version postfix for</param>
        /// <returns>The patch postfix, or 0 if the given object isn't a player or did not advertise their build info</returns>
        public static int GetPlayerBuildVersionPostfix(uint oPlayer)
        {
            return global::NWN.Core.NWScript.GetPlayerBuildVersionPostfix(oPlayer);
        }

        /// <summary>
        /// Returns the patch commit sha1 of the player (i.e. the "abcdef01" out of "87.8193.35-29 abcdef01").
        /// </summary>
        /// <param name="oPlayer">The player to get the build version commit sha1 for</param>
        /// <returns>The patch commit sha1, or empty string if unavailable</returns>
        public static string GetPlayerBuildVersionCommitSha1(uint oPlayer)
        {
            return global::NWN.Core.NWScript.GetPlayerBuildVersionCommitSha1(oPlayer);
        }

        /// <summary>
        /// Gets the PC that is involved in the conversation.
        /// </summary>
        /// <returns>The PC involved in the conversation, or OBJECT_INVALID on error</returns>
        public static uint GetPCSpeaker()
        {
            return global::NWN.Core.NWScript.GetPCSpeaker();
        }

        /// <summary>
        /// Use this in an OnPlayerDeath module script to get the last player that died.
        /// </summary>
        /// <returns>The last player that died</returns>
        public static uint GetLastPlayerDied()
        {
            return global::NWN.Core.NWScript.GetLastPlayerDied();
        }

        /// <summary>
        /// Use this in an OnItemLost script to get the item that was lost/dropped.
        /// </summary>
        /// <returns>The item that was lost/dropped, or OBJECT_INVALID if the module is not valid</returns>
        public static uint GetModuleItemLost()
        {
            return global::NWN.Core.NWScript.GetModuleItemLost();
        }

        /// <summary>
        /// Use this in an OnItemLost script to get the creature that lost the item.
        /// </summary>
        /// <returns>The creature that lost the item, or OBJECT_INVALID if the module is not valid</returns>
        public static uint GetModuleItemLostBy()
        {
            return global::NWN.Core.NWScript.GetModuleItemLostBy();
        }

        /// <summary>
        /// Gets the public part of the CD Key that the player used when logging in.
        /// </summary>
        /// <param name="oPlayer">The player to get the CD key for</param>
        /// <param name="nSinglePlayerCDKey">If TRUE, the player's public CD Key will be returned when the player is playing in single player mode (otherwise returns an empty string in single player mode)</param>
        /// <returns>The public CD key</returns>
        public static string GetPCPublicCDKey(uint oPlayer, bool nSinglePlayerCDKey = false)
        {
            return global::NWN.Core.NWScript.GetPCPublicCDKey(oPlayer, nSinglePlayerCDKey ? 1 : 0);
        }

        /// <summary>
        /// Gets the IP address from which the player has connected.
        /// </summary>
        /// <param name="oPlayer">The player to get the IP address for</param>
        /// <returns>The IP address</returns>
        public static string GetPCIPAddress(uint oPlayer)
        {
            return global::NWN.Core.NWScript.GetPCIPAddress(oPlayer);
        }

        /// <summary>
        /// Gets the name of the player.
        /// </summary>
        /// <param name="oPlayer">The player to get the name for</param>
        /// <returns>The player name</returns>
        public static string GetPCPlayerName(uint oPlayer)
        {
            return global::NWN.Core.NWScript.GetPCPlayerName(oPlayer);
        }

        /// <summary>
        /// Sets the player and target to like each other.
        /// </summary>
        /// <param name="oPlayer">The player</param>
        /// <param name="oTarget">The target</param>
        public static void SetPCLike(uint oPlayer, uint oTarget)
        {
            global::NWN.Core.NWScript.SetPCLike(oPlayer, oTarget);
        }

        /// <summary>
        /// Sets the player and target to dislike each other.
        /// </summary>
        /// <param name="oPlayer">The player</param>
        /// <param name="oTarget">The target</param>
        public static void SetPCDislike(uint oPlayer, uint oTarget)
        {
            global::NWN.Core.NWScript.SetPCDislike(oPlayer, oTarget);
        }

        /// <summary>
        /// Sends a server message to the player.
        /// </summary>
        /// <param name="oPlayer">The player to send the message to</param>
        /// <param name="szMessage">The message to send</param>
        public static void SendMessageToPC(uint oPlayer, string szMessage)
        {
            global::NWN.Core.NWScript.SendMessageToPC(oPlayer, szMessage);
        }

        /// <summary>
        /// Gets if the player is currently connected over a relay (instead of directly).
        /// Returns FALSE for any other object, including OBJECT_INVALID.
        /// </summary>
        /// <param name="oPlayer">The player to check</param>
        /// <returns>TRUE if connected over a relay, FALSE otherwise</returns>
        public static int GetIsPlayerConnectionRelayed(uint oPlayer)
        {
            return global::NWN.Core.NWScript.GetIsPlayerConnectionRelayed(oPlayer);
        }

        /// <summary>
        /// Forces all the characters of the players who are currently in the game to
        /// be exported to their respective directories i.e. LocalVault/ServerVault/ etc.
        /// </summary>
        public static void ExportAllCharacters()
        {
            global::NWN.Core.NWScript.ExportAllCharacters();
        }

        /// <summary>
        /// Forces the character of the player specified to be exported to its respective directory
        /// i.e. LocalVault/ServerVault/ etc.
        /// </summary>
        /// <param name="oPlayer">The player to export the character for</param>
        public static void ExportSingleCharacter(uint oPlayer)
        {
            global::NWN.Core.NWScript.ExportSingleCharacter(oPlayer);
        }

        /// <summary>
        /// Returns the INVENTORY_SLOT_* constant of the last item equipped.
        /// Can only be used in the module's OnPlayerEquip event.
        /// </summary>
        /// <returns>The inventory slot constant, or -1 on error</returns>
        public static InventorySlot GetPCItemLastEquippedSlot()
        {
            return (InventorySlot)global::NWN.Core.NWScript.GetPCItemLastEquippedSlot();
        }

        /// <summary>
        /// Returns the INVENTORY_SLOT_* constant of the last item unequipped.
        /// Can only be used in the module's OnPlayerUnequip event.
        /// </summary>
        /// <returns>The inventory slot constant, or -1 on error</returns>
        public static InventorySlot GetPCItemLastUnequippedSlot()
        {
            return (InventorySlot)global::NWN.Core.NWScript.GetPCItemLastUnequippedSlot();
        }
    }
}