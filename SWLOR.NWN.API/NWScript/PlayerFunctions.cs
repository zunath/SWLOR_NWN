using System.Numerics;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Gets the current cutscene state of the player specified by oCreature.
        ///   Returns TRUE if the player is in cutscene mode.
        ///   Returns FALSE if the player is not in cutscene mode, or on an error
        ///   (such as specifying a non creature object).
        /// </summary>
        public static bool GetCutsceneMode(uint oCreature = OBJECT_INVALID)
        {
            return global::NWN.Core.NWScript.GetCutsceneMode(oCreature) != 0;
        }

        /// <summary>
        ///   Forces this player's camera to be set to this height. Setting this value to zero will
        ///   restore the camera to the racial default height.
        /// </summary>
        public static void SetCameraHeight(uint oPlayer, float fHeight = 0.0f)
        {
            global::NWN.Core.NWScript.SetCameraHeight(oPlayer, fHeight);
        }

        /// <summary>
        ///   Changes the current Day/Night cycle for this player to night
        ///   - oPlayer: which player to change the lighting for
        ///   - fTransitionTime: how long the transition should take
        /// </summary>
        public static void DayToNight(uint oPlayer, float fTransitionTime = 0.0f)
        {
            global::NWN.Core.NWScript.DayToNight(oPlayer, fTransitionTime);
        }

        /// <summary>
        ///   Changes the current Day/Night cycle for this player to daylight
        ///   - oPlayer: which player to change the lighting for
        ///   - fTransitionTime: how long the transition should take
        /// </summary>
        public static void NightToDay(uint oPlayer, float fTransitionTime = 0.0f)
        {
            global::NWN.Core.NWScript.NightToDay(oPlayer, fTransitionTime);
        }

        /// <summary>
        ///   Returns the current movement rate factor
        ///   of the cutscene 'camera man'.
        ///   NOTE: This will be a value between 0.1, 2.0 (10%-200%)
        /// </summary>
        public static float GetCutsceneCameraMoveRate(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetCutsceneCameraMoveRate(oCreature);
        }

        /// <summary>
        ///   Sets the current movement rate factor for the cutscene
        ///   camera man.
        ///   NOTE: You can only set values between 0.1, 2.0 (10%-200%)
        /// </summary>
        public static void SetCutsceneCameraMoveRate(uint oCreature, float fRate)
        {
            global::NWN.Core.NWScript.SetCutsceneCameraMoveRate(oCreature, fRate);
        }

        /// <summary>
        ///   Makes a player examine the object oExamine. This causes the examination
        ///   pop-up box to appear for the object specified.
        /// </summary>
        public static void ActionExamine(uint oExamine)
        {
            global::NWN.Core.NWScript.ActionExamine(oExamine);
        }

        /// <summary>
        ///   Use this to get the item last equipped by a player character in OnPlayerEquipItem..
        /// </summary>
        public static uint GetPCItemLastEquipped()
        {
            return global::NWN.Core.NWScript.GetPCItemLastEquipped();
        }

        /// <summary>
        ///   Use this to get the player character who last equipped an item in OnPlayerEquipItem..
        /// </summary>
        public static uint GetPCItemLastEquippedBy()
        {
            return global::NWN.Core.NWScript.GetPCItemLastEquippedBy();
        }

        /// <summary>
        ///   Use this to get the item last unequipped by a player character in OnPlayerEquipItem..
        /// </summary>
        public static uint GetPCItemLastUnequipped()
        {
            return global::NWN.Core.NWScript.GetPCItemLastUnequipped();
        }

        /// <summary>
        ///   Use this to get the player character who last unequipped an item in OnPlayerUnEquipItem..
        /// </summary>
        public static uint GetPCItemLastUnequippedBy()
        {
            return global::NWN.Core.NWScript.GetPCItemLastUnequippedBy();
        }

        /// <summary>
        ///   Send a server message (szMessage) to the oPlayer.
        /// </summary>
        public static void SendMessageToPCByStrRef(uint oPlayer, int nStrRef)
        {
            global::NWN.Core.NWScript.SendMessageToPCByStrRef(oPlayer, nStrRef);
        }

        /// <summary>
        ///   Open's this creature's inventory panel for this player
        ///   - oCreature: creature to view
        ///   - oPlayer: the owner of this creature will see the panel pop up
        ///   * DM's can view any creature's inventory
        ///   * Players can view their own inventory, or that of their henchman, familiar or animal companion
        /// </summary>
        public static void OpenInventory(uint oCreature, uint oPlayer)
        {
            global::NWN.Core.NWScript.OpenInventory(oCreature, oPlayer);
        }

        /// <summary>
        ///   Stores the current camera mode and position so that it can be restored (using
        ///   RestoreCameraFacing())
        /// </summary>
        public static void StoreCameraFacing()
        {
            global::NWN.Core.NWScript.StoreCameraFacing();
        }

        /// <summary>
        ///   Restores the camera mode and position to what they were last time StoreCameraFacing
        ///   was called.  RestoreCameraFacing can only be called once, and must correspond to a
        ///   previous call to StoreCameraFacing.
        /// </summary>
        public static void RestoreCameraFacing()
        {
            global::NWN.Core.NWScript.RestoreCameraFacing();
        }

        /// <summary>
        ///   Fades the screen for the given creature/player from black to regular screen
        ///   - oCreature: creature controlled by player that should fade from black
        /// </summary>
        public static void FadeFromBlack(uint oCreature, float fSpeed = FadeSpeed.Medium)
        {
            global::NWN.Core.NWScript.FadeFromBlack(oCreature, fSpeed);
        }

        /// <summary>
        ///   Fades the screen for the given creature/player from regular screen to black
        ///   - oCreature: creature controlled by player that should fade to black
        /// </summary>
        public static void FadeToBlack(uint oCreature, float fSpeed = FadeSpeed.Medium)
        {
            global::NWN.Core.NWScript.FadeToBlack(oCreature, fSpeed);
        }

        /// <summary>
        ///   Removes any fading or black screen.
        ///   - oCreature: creature controlled by player that should be cleared
        /// </summary>
        public static void StopFade(uint oCreature)
        {
            global::NWN.Core.NWScript.StopFade(oCreature);
        }

        /// <summary>
        ///   Sets the screen to black.  Can be used in preparation for a fade-in (FadeFromBlack)
        ///   Can be cleared by either doing a FadeFromBlack, or by calling StopFade.
        ///   - oCreature: creature controlled by player that should see black screen
        /// </summary>
        public static void BlackScreen(uint oCreature)
        {
            global::NWN.Core.NWScript.BlackScreen(oCreature);
        }

        /// <summary>
        ///   Sets the given creature into cutscene mode.  This prevents the player from
        ///   using the GUI and camera controls.
        ///   - oCreature: creature in a cutscene
        ///   - nInCutscene: TRUE to move them into cutscene, FALSE to remove cutscene mode
        ///   - nLeftClickingEnabled: TRUE to allow the user to interact with the game world using the left mouse button only.
        ///   FALSE to stop the user from interacting with the game world.
        ///   Note: SetCutsceneMode(oPlayer, TRUE) will also make the player 'plot' (unkillable).
        ///   SetCutsceneMode(oPlayer, FALSE) will restore the player's plot flag to what it
        ///   was when SetCutsceneMode(oPlayer, TRUE) was called.
        /// </summary>
        public static void SetCutsceneMode(uint oCreature, bool nInCutscene = true, bool nLeftClickingEnabled = false)
        {
            global::NWN.Core.NWScript.SetCutsceneMode(oCreature, nInCutscene ? 1 : 0, nLeftClickingEnabled ? 1 : 0);
        }

        /// <summary>
        ///   Gets the last player character to cancel from a cutscene.
        /// </summary>
        public static uint GetLastPCToCancelCutscene()
        {
            return global::NWN.Core.NWScript.GetLastPCToCancelCutscene();
        }

        /// <summary>
        ///   Remove oPlayer from the server.
        ///   You can optionally specify a reason to override the text shown to the player.
        /// </summary>
        public static void BootPC(uint oPlayer, string sReason = "")
        {
            global::NWN.Core.NWScript.BootPC(oPlayer, sReason);
        }

        /// <summary>
        ///   Spawn in the Death GUI.
        ///   The default (as defined by BioWare) can be spawned in by PopUpGUIPanel, but
        ///   if you want to turn off the "Respawn" or "Wait for Help" buttons, this is the
        ///   function to use.
        ///   - oPC
        ///   - bRespawnButtonEnabled: if this is TRUE, the "Respawn" button will be enabled
        ///   on the Death GUI.
        ///   - bWaitForHelpButtonEnabled: if this is TRUE, the "Wait For Help" button will
        ///   be enabled on the Death GUI (Note: This button will not appear in single player games).
        ///   - nHelpStringReference
        ///   - sHelpString
        /// </summary>
        public static void PopUpDeathGUIPanel(uint oPC, bool bRespawnButtonEnabled = true,
            bool bWaitForHelpButtonEnabled = true, int nHelpStringReference = 0, string sHelpString = "")
        {
            global::NWN.Core.NWScript.PopUpDeathGUIPanel(oPC, bRespawnButtonEnabled ? 1 : 0, bWaitForHelpButtonEnabled ? 1 : 0, nHelpStringReference, sHelpString);
        }

        /// <summary>
        ///   Get the first PC in the player list.
        ///   This resets the position in the player list for GetNextPC().
        /// </summary>
        public static uint GetFirstPC()
        {
            return global::NWN.Core.NWScript.GetFirstPC();
        }

        /// <summary>
        ///   Get the next PC in the player list.
        ///   This picks up where the last GetFirstPC() or GetNextPC() left off.
        /// </summary>
        public static uint GetNextPC()
        {
            return global::NWN.Core.NWScript.GetNextPC();
        }

        /// <summary>
        ///   Get the last PC that levelled up.
        /// </summary>
        public static uint GetPCLevellingUp()
        {
            return global::NWN.Core.NWScript.GetPCLevellingUp();
        }

        /// <summary>
        ///   Set the camera mode for oPlayer.
        ///   - oPlayer
        ///   - nCameraMode: CAMERA_MODE_*
        ///   * If oPlayer is not player-controlled or nCameraMode is invalid, nothing
        ///   happens.
        /// </summary>
        public static void SetCameraMode(uint oPlayer, int nCameraMode)
        {
            global::NWN.Core.NWScript.SetCameraMode(oPlayer, nCameraMode);
        }

        /// <summary>
        ///   Use this in an OnPlayerDying module script to get the last player who is dying.
        /// </summary>
        public static uint GetLastPlayerDying()
        {
            return global::NWN.Core.NWScript.GetLastPlayerDying();
        }

        /// <summary>
        ///   Spawn a GUI panel for the client that controls oPC.
        ///   Will force show panels disabled with SetGuiPanelDisabled()
        ///   - oPC
        ///   - nGUIPanel: GUI_PANEL_*, except GUI_PANEL_COMPASS / GUI_PANEL_LEVELUP / GUI_PANEL_GOLD_* / GUI_PANEL_EXAMINE_*
        ///   * Nothing happens if oPC is not a player character or if an invalid value is used for nGUIPanel.
        /// </summary>
        public static void PopUpGUIPanel(uint oPC, GuiPanel nGUIPanel)
        {
            global::NWN.Core.NWScript.PopUpGUIPanel(oPC, (int)nGUIPanel);
        }


        /// <summary>
        /// Returns the build number of oPlayer (i.e. 8193).
        /// Returns 0 if the given object isn't a player or did not advertise their build info.
        /// </summary>
        public static int GetPlayerBuildVersionMajor(uint oPlayer)
        {
            return global::NWN.Core.NWScript.GetPlayerBuildVersionMajor(oPlayer);
        }

        /// <summary>
        /// Returns the patch revision of oPlayer (i.e. 8).
        /// Returns 0 if the given object isn't a player or did not advertise their build info.
        /// </summary>
        public static int GetPlayerBuildVersionMinor(uint oPlayer)
        {
            return global::NWN.Core.NWScript.GetPlayerBuildVersionMinor(oPlayer);
        }


        /// <summary>
        /// Returns TRUE if the given player-controlled creature has DM privileges
        /// gained through a player login (as opposed to the DM client).
        /// Note: GetIsDM() also returns TRUE for player creature DMs.
        /// </summary>
        public static bool GetIsPlayerDM(uint oCreature)
        {
            return global::NWN.Core.NWScript.GetIsPlayerDM(oCreature) != 0;
        }

        /// <summary>
        /// Gets the player that last triggered the module OnPlayerGuiEvent event.
        /// </summary>
        /// <returns></returns>
        public static uint GetLastGuiEventPlayer()
        {
            return global::NWN.Core.NWScript.GetLastGuiEventPlayer();
        }

        /// <summary>
        /// Gets the last triggered GUIEVENT_* in the module OnPlayerGuiEvent event.
        /// </summary>
        /// <returns></returns>
        public static GuiEventType GetLastGuiEventType()
        {
            return (GuiEventType)global::NWN.Core.NWScript.GetLastGuiEventType();
        }

        /// <summary>
        /// Gets an optional integer of specific gui events in the module OnPlayerGuiEvent event.
        /// * GUIEVENT_CHATBAR_*: The selected chat channel. Does not indicate the actual used channel.
        ///                       0 = Shout, 1 = Whisper, 2 = Talk, 3 = Party, 4 = DM
        /// * GUIEVENT_CHARACTERSHEET_SKILL_SELECT: The skill ID.
        /// * GUIEVENT_CHARACTERSHEET_FEAT_SELECT: The feat ID.
        /// * GUIEVENT_EFFECTICON_CLICK: The effect icon ID (EFFECT_ICON_*)
        /// * GUIEVENT_DISABLED_PANEL_ATTEMPT_OPEN: The GUI_PANEL_* the player attempted to open.
        /// * GUIEVENT_QUICKCHAT_SELECT: The hotkey character representing the option
        /// * GUIEVENT_EXAMINE_OBJECT: A GUI_PANEL_EXAMINE_* constant
        /// </summary>
        /// <returns></returns>
        public static int GetLastGuiEventInteger()
        {
            return global::NWN.Core.NWScript.GetLastGuiEventInteger();
        }

        /// <summary>
        /// Gets an optional object of specific gui events in the module OnPlayerGuiEvent event.
        /// * GUIEVENT_MINIMAP_MAPPIN_CLICK: The waypoint the map note is attached to.
        /// * GUIEVENT_CHARACTERSHEET_*_SELECT: The owner of the character sheet.
        /// * GUIEVENT_PLAYERLIST_PLAYER_CLICK: The player clicked on.
        /// * GUIEVENT_PARTYBAR_PORTRAIT_CLICK: The creature clicked on.
        /// * GUIEVENT_DISABLED_PANEL_ATTEMPT_OPEN: For GUI_PANEL_CHARACTERSHEET, the owner of the character sheet.
        /// </summary>
        /// <returns></returns>
        public static uint GetLastGuiEventObject()
        {
            return global::NWN.Core.NWScript.GetLastGuiEventObject();
        }

        /// <summary>
        /// Disable a gui panel for the client that controls oPlayer.
        /// Notes: Will close the gui panel if currently open, except GUI_PANEL_LEVELUP / GUI_PANEL_GOLD_*
        ///        Does not persist through relogging or in savegames.
        ///        Will fire a GUIEVENT_DISABLED_PANEL_ATTEMPT_OPEN OnPlayerGuiEvent for some gui panels if a player attempts to open them.
        ///        You can still force show a panel with PopUpGUIPanel().
        ///        You can still force examine an object with ActionExamine().
        /// * nGuiPanel: A GUI_PANEL_* constant, except GUI_PANEL_PLAYER_DEATH.
        /// </summary>
        public static void SetGuiPanelDisabled(uint oPlayer, GuiPanel nGuiPanel, bool bDisabled, uint oTarget = OBJECT_INVALID)
        {
            global::NWN.Core.NWScript.SetGuiPanelDisabled(oPlayer, (int)nGuiPanel, bDisabled ? 1 : 0, oTarget);
        }

        /// <summary>
        /// Gets the ID (1..8) of the last tile action performed in OnPlayerTileAction
        /// </summary>
        /// <returns></returns>
        public static int GetLastTileActionId()
        {
            return global::NWN.Core.NWScript.GetLastTileActionId();
        }

        /// <summary>
        /// Gets the target position in the module OnPlayerTileAction event.
        /// </summary>
        /// <returns></returns>
        public static Vector3 GetLastTileActionPosition()
        {
            return global::NWN.Core.NWScript.GetLastTileActionPosition();
        }

        /// <summary>
        /// Gets the player object that triggered the OnPlayerTileAction event.
        /// </summary>
        /// <returns></returns>
        public static uint GetLastPlayerToDoTileAction()
        {
            return global::NWN.Core.NWScript.GetLastPlayerToDoTileAction();
        }

        /// <summary>
        /// Gets a device property/capability as advertised by the client.
        /// sProperty is one of PLAYER_DEVICE_PROPERTY_xxx.
        /// Returns -1 if
        /// - the property was never set by the client,
        /// - the the actual value is -1,
        /// - the player is running a older build that does not advertise device properties,
        /// - the player has disabled sending device properties (Options->Game->Privacy).
        /// </summary>
        public static int GetPlayerDeviceProperty(uint oPlayer, string sProperty)
        {
            return global::NWN.Core.NWScript.GetPlayerDeviceProperty(oPlayer, sProperty);
        }

        /// <summary>
        /// Returns the LANGUAGE_xx code of the given player, or -1 if unavailable.
        /// </summary>
        public static PlayerLanguageType GetPlayerLanguage(uint oPlayer)
        {
            return (PlayerLanguageType)global::NWN.Core.NWScript.GetPlayerLanguage(oPlayer);
        }

        /// <summary>
        /// Returns one of PLAYER_DEVICE_PLATFORM_xxx, or 0 if unavailable.
        /// </summary>
        public static PlayerDevicePlatformType GetPlayerDevicePlatform(uint oPlayer)
        {
            return (PlayerDevicePlatformType)global::NWN.Core.NWScript.GetPlayerDevicePlatform(oPlayer);
        }

        /// <summary>
        /// Returns the patch postfix of oPlayer (i.e. the 29 out of "87.8193.35-29 abcdef01").
        /// Returns 0 if the given object isn't a player or did not advertise their build info, or the
        /// player version is old enough not to send this bit of build info to the server.
        /// </summary>
        public static int GetPlayerBuildVersionPostfix(uint oPlayer)
        {
            return global::NWN.Core.NWScript.GetPlayerBuildVersionPostfix(oPlayer);
        }

        /// <summary>
        /// Returns the patch commit sha1 of oPlayer (i.e. the "abcdef01" out of "87.8193.35-29 abcdef01").
        /// Returns "" if the given object isn't a player or did not advertise their build info, or the
        /// player version is old enough not to send this bit of build info to the server.
        /// </summary>
        public static string GetPlayerBuildVersionCommitSha1(uint oPlayer)
        {
            return global::NWN.Core.NWScript.GetPlayerBuildVersionCommitSha1(oPlayer);
        }

        /// <summary>
        ///   Get the PC that is involved in the conversation.
        ///   * Returns OBJECT_INVALID on error.
        /// </summary>
        public static uint GetPCSpeaker()
        {
            return global::NWN.Core.NWScript.GetPCSpeaker();
        }

        /// <summary>
        ///   Use this in an OnPlayerDeath module script to get the last player that died.
        /// </summary>
        public static uint GetLastPlayerDied()
        {
            return global::NWN.Core.NWScript.GetLastPlayerDied();
        }

        /// <summary>
        ///   Use this in an OnItemLost script to get the item that was lost/dropped.
        ///   * Returns OBJECT_INVALID if the module is not valid.
        /// </summary>
        public static uint GetModuleItemLost()
        {
            return global::NWN.Core.NWScript.GetModuleItemLost();
        }

        /// <summary>
        ///   Use this in an OnItemLost script to get the creature that lost the item.
        ///   * Returns OBJECT_INVALID if the module is not valid.
        /// </summary>
        public static uint GetModuleItemLostBy()
        {
            return global::NWN.Core.NWScript.GetModuleItemLostBy();
        }

        /// <summary>
        ///   Get the public part of the CD Key that oPlayer used when logging in.
        ///   - nSinglePlayerCDKey: If set to TRUE, the player's public CD Key will
        ///   be returned when the player is playing in single player mode
        ///   (otherwise returns an empty string in single player mode).
        /// </summary>
        public static string GetPCPublicCDKey(uint oPlayer, bool nSinglePlayerCDKey = false)
        {
            return global::NWN.Core.NWScript.GetPCPublicCDKey(oPlayer, nSinglePlayerCDKey ? 1 : 0);
        }

        /// <summary>
        ///   Get the IP address from which oPlayer has connected.
        /// </summary>
        public static string GetPCIPAddress(uint oPlayer)
        {
            return global::NWN.Core.NWScript.GetPCIPAddress(oPlayer);
        }

        /// <summary>
        ///   Get the name of oPlayer.
        /// </summary>
        public static string GetPCPlayerName(uint oPlayer)
        {
            return global::NWN.Core.NWScript.GetPCPlayerName(oPlayer);
        }

        /// <summary>
        ///   Sets oPlayer and oTarget to like each other.
        /// </summary>
        public static void SetPCLike(uint oPlayer, uint oTarget)
        {
            global::NWN.Core.NWScript.SetPCLike(oPlayer, oTarget);
        }

        /// <summary>
        ///   Sets oPlayer and oTarget to dislike each other.
        /// </summary>
        public static void SetPCDislike(uint oPlayer, uint oTarget)
        {
            global::NWN.Core.NWScript.SetPCDislike(oPlayer, oTarget);
        }

        /// <summary>
        ///   Send a server message (szMessage) to the oPlayer.
        /// </summary>
        public static void SendMessageToPC(uint oPlayer, string szMessage)
        {
            global::NWN.Core.NWScript.SendMessageToPC(oPlayer, szMessage);
        }

        /// <summary>
        ///   Get if oPlayer is currently connected over a relay (instead of directly).
        ///   Returns FALSE for any other object, including OBJECT_INVALID.
        /// </summary>
        public static int GetIsPlayerConnectionRelayed(uint oPlayer)
        {
            return global::NWN.Core.NWScript.GetIsPlayerConnectionRelayed(oPlayer);
        }

        /// <summary>
        ///   Force all the characters of the players who are currently in the game to
        ///   be exported to their respective directories i.e. LocalVault/ServerVault/ etc.
        /// </summary>
        public static void ExportAllCharacters()
        {
            global::NWN.Core.NWScript.ExportAllCharacters();
        }

        /// <summary>
        ///   Force the character of the player specified to be exported to its respective directory
        ///   i.e. LocalVault/ServerVault/ etc.
        /// </summary>
        public static void ExportSingleCharacter(uint oPlayer)
        {
            global::NWN.Core.NWScript.ExportSingleCharacter(oPlayer);
        }

        /// <summary>
        /// Returns the INVENTORY_SLOT_* constant of the last item equipped.  Can only be used in the
        /// module's OnPlayerEquip event.  Returns -1 on error.
        /// </summary>
        public static InventorySlot GetPCItemLastEquippedSlot()
        {
            return (InventorySlot)global::NWN.Core.NWScript.GetPCItemLastEquippedSlot();
        }

        /// <summary>
        /// Returns the INVENTORY_SLOT_* constant of the last item unequipped.  Can only be used in the
        /// module's OnPlayerUnequip event.  Returns -1 on error.
        /// </summary>
        public static InventorySlot GetPCItemLastUnequippedSlot()
        {
            return (InventorySlot)global::NWN.Core.NWScript.GetPCItemLastUnequippedSlot();
        }
    }
}