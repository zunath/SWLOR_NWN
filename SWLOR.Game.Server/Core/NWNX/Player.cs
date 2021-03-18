using System;
using System.Numerics;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Core.NWNX
{
    public class Player
    {
        private const string PLUGIN_NAME = "NWNX_Player";

        // Force display placeable examine window for player
        // If used on a placeable in a different area than the player, the portait will not be shown.
        public static void ForcePlaceableExamineWindow(uint player, uint placeable)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ForcePlaceableExamineWindow");
            Internal.NativeFunctions.nwnxPushObject(placeable);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Force opens the target object's inventory for the player.
        // A few notes about this function:
        // - If the placeable is in a different area than the player, the portrait will not be shown
        // - The placeable's open/close animations will be played
        // - Clicking the 'close' button will cause the player to walk to the placeable;
        //     If the placeable is in a different area, the player will just walk to the edge
        //     of the current area and stop. This action can be cancelled manually.
        // - Walking will close the placeable automatically.
        public static void ForcePlaceableInventoryWindow(uint player, uint placeable)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ForcePlaceableInventoryWindow");
            Internal.NativeFunctions.nwnxPushObject(placeable);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Starts displaying a timing bar.
        // Will run a script at the end of the timing bar, if specified.
        public static void StartGuiTimingBar(uint player, float seconds, string script = "",
            TimingBarType type = TimingBarType.Custom)
        {
            if (NWScript.NWScript.GetLocalInt(player, "NWNX_PLAYER_GUI_TIMING_ACTIVE") == 1) return;
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "StartGuiTimingBar");
            Internal.NativeFunctions.nwnxPushInt((int)type);
            Internal.NativeFunctions.nwnxPushFloat(seconds);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();

            var id = NWScript.NWScript.GetLocalInt(player, "NWNX_PLAYER_GUI_TIMING_ID") + 1;
            NWScript.NWScript.SetLocalInt(player, "NWNX_PLAYER_GUI_TIMING_ACTIVE", id);
            NWScript.NWScript.SetLocalInt(player, "NWNX_PLAYER_GUI_TIMING_ID", id);

            NWScript.NWScript.DelayCommand(seconds, () => StopGuiTimingBar(player, script, id));
        }

        // Stops displaying a timing bar.
        // Runs a script if specified.
        public static void StopGuiTimingBar(uint creature, string script, int id)
        {
            var activeId = NWScript.NWScript.GetLocalInt(creature, "NWNX_PLAYER_GUI_TIMING_ACTIVE");
            // Either the timing event was never started, or it already finished.
            if (activeId == 0) return;
            // If id != -1, we ended up here through DelayCommand. Make sure it's for the right ID
            if (id != -1 && id != activeId) return;
            NWScript.NWScript.DeleteLocalInt(creature, "NWNX_PLAYER_GUI_TIMING_ACTIVE");
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "StopGuiTimingBar");
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            if (!string.IsNullOrWhiteSpace(script)) NWScript.NWScript.ExecuteScript(script, creature);
        }

        // Stops displaying a timing bar.
        // Runs a script if specified.
        public static void StopGuiTimingBar(uint player, string script)
        {
            StopGuiTimingBar(player, script, -1);
        }

        // Sets whether the player should always walk when given movement commands.
        // If true, clicking on the ground or using WASD will trigger walking instead of running.
        public static void SetAlwaysWalk(uint player, bool walk)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAlwaysWalk");
            Internal.NativeFunctions.nwnxPushInt(walk ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Gets the player's quickbar slot info
        public static QuickBarSlot GetQuickBarSlot(uint player, int slot)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetQuickBarSlot");
            var qbs = new QuickBarSlot();
            Internal.NativeFunctions.nwnxPushInt(slot);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
            qbs.Associate = Internal.NativeFunctions.nwnxPopObject();
            qbs.AssociateType = Internal.NativeFunctions.nwnxPopInt();
            qbs.DomainLevel = Internal.NativeFunctions.nwnxPopInt();
            qbs.MetaType = Internal.NativeFunctions.nwnxPopInt();
            qbs.INTParam1 = Internal.NativeFunctions.nwnxPopInt();
            qbs.ToolTip = Internal.NativeFunctions.nwnxPopString();
            qbs.CommandLine = Internal.NativeFunctions.nwnxPopString();
            qbs.CommandLabel = Internal.NativeFunctions.nwnxPopString();
            qbs.Resref = Internal.NativeFunctions.nwnxPopString();
            qbs.MultiClass = Internal.NativeFunctions.nwnxPopInt();
            qbs.ObjectType = (QuickBarSlotType)Internal.NativeFunctions.nwnxPopInt();
            qbs.SecondaryItem = Internal.NativeFunctions.nwnxPopObject();
            qbs.Item = Internal.NativeFunctions.nwnxPopObject();
            return qbs;
        }

        // Sets a player's quickbar slot
        public static void SetQuickBarSlot(uint player, int slot, QuickBarSlot qbs)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetQuickBarSlot");
            Internal.NativeFunctions.nwnxPushObject(qbs.Item ?? Internal.OBJECT_INVALID);
            Internal.NativeFunctions.nwnxPushObject(qbs.SecondaryItem ?? Internal.OBJECT_INVALID);
            Internal.NativeFunctions.nwnxPushInt((int)qbs.ObjectType);
            Internal.NativeFunctions.nwnxPushInt(qbs.MultiClass);
            Internal.NativeFunctions.nwnxPushString(qbs.Resref!);
            Internal.NativeFunctions.nwnxPushString(qbs.CommandLabel!);
            Internal.NativeFunctions.nwnxPushString(qbs.CommandLine!);
            Internal.NativeFunctions.nwnxPushString(qbs.ToolTip!);
            Internal.NativeFunctions.nwnxPushInt(qbs.INTParam1);
            Internal.NativeFunctions.nwnxPushInt(qbs.MetaType);
            Internal.NativeFunctions.nwnxPushInt(qbs.DomainLevel);
            Internal.NativeFunctions.nwnxPushInt(qbs.AssociateType);
            Internal.NativeFunctions.nwnxPushObject(qbs.Associate ?? Internal.OBJECT_INVALID);
            Internal.NativeFunctions.nwnxPushInt(slot);
            Internal.NativeFunctions.nwnxPushObject(player!);
            Internal.NativeFunctions.nwnxCallFunction();
        }


        // Get the name of the .bic file associated with the player's character.
        public static string GetBicFileName(uint player)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetBicFileName");
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopString();
        }

        // Plays the VFX at the target position in current area for the given player only
        public static void ShowVisualEffect(uint player, int effectId, Vector3 position)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ShowVisualEffect");
            Internal.NativeFunctions.nwnxPushFloat(position.X);
            Internal.NativeFunctions.nwnxPushFloat(position.Y);
            Internal.NativeFunctions.nwnxPushFloat(position.Z);
            Internal.NativeFunctions.nwnxPushInt(effectId);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Changes the nighttime music track for the given player only
        public static void MusicBackgroundChangeTimeToggle(uint player, int track, bool night)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ChangeBackgroundMusic");
            Internal.NativeFunctions.nwnxPushInt(track);
            Internal.NativeFunctions.nwnxPushInt(night ? 1 : 0); // bool day = false
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Toggle the background music for the given player only
        public static void MusicBackgroundToggle(uint player, bool on)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "PlayBackgroundMusic");
            Internal.NativeFunctions.nwnxPushInt(on ? 1 : 0); // bool play = false
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Changes the battle music track for the given player only
        public static void MusicBattleChange(uint player, int track)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ChangeBattleMusic");
            Internal.NativeFunctions.nwnxPushInt(track);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Toggle the background music for the given player only
        public static void MusicBattleToggle(uint player, bool on)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "PlayBattleMusic");
            Internal.NativeFunctions.nwnxPushInt(on ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Play a sound at the location of target for the given player only
        // If target is OBJECT_INVALID the sound will play at the location of the player
        public static void PlaySound(uint player, string sound, uint target)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "PlaySound");
            Internal.NativeFunctions.nwnxPushObject(target);
            Internal.NativeFunctions.nwnxPushString(sound);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Toggle a placeable's usable flag for the given player only
        public static void SetPlaceableUseable(uint player, uint placeable, bool usable)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPlaceableUsable");
            Internal.NativeFunctions.nwnxPushInt(usable ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(placeable);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Override player's rest duration
        // Duration is in milliseconds, 1000 = 1 second
        // Minimum duration of 10ms
        // -1 clears the override
        public static void SetRestDuration(uint player, int duration)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetRestDuration");
            Internal.NativeFunctions.nwnxPushInt(duration);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Apply visualeffect to target that only player can see
        // Note: Only works with instant effects: VFX_COM_*, VFX_FNF_*, VFX_IMP_*
        public static void ApplyInstantVisualEffectToObject(uint player, uint target, VisualEffect visualEffect)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ApplyInstantVisualEffectToObject");
            Internal.NativeFunctions.nwnxPushInt((int)visualEffect);
            Internal.NativeFunctions.nwnxPushObject(target);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Refreshes the players character sheet
        // Note: You may need to use DelayCommand if you're manipulating values
        // through nwnx and forcing a UI refresh, 0.5s seemed to be fine
        public static void UpdateCharacterSheet(uint player)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "UpdateCharacterSheet");
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Allows player to open target's inventory
        // Target must be a creature or another player
        // Note: only works if player and target are in the same area
        public static void OpenInventory(uint player, uint target, bool open = true)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "OpenInventory");
            Internal.NativeFunctions.nwnxPushInt(open ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(target);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Get player's area exploration state
        public static string GetAreaExplorationState(uint player, uint area)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAreaExplorationState");
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopString();
        }

        // Set player's area exploration state (str is an encoded string obtained with NWNX_Player_GetAreaExplorationState)
        public static void SetAreaExplorationState(uint player, uint area, string encodedString)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAreaExplorationState");
            Internal.NativeFunctions.nwnxPushString(encodedString);
            Internal.NativeFunctions.nwnxPushObject(area);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Override oPlayer's rest animation to nAnimation
        //
        // NOTE: nAnimation does not take ANIMATION_LOOPING_* or ANIMATION_FIREFORGET_* constants
        //       Use NWNX_Consts_TranslateNWScriptAnimation() in nwnx_consts.nss to get their NWNX equivalent
        //       -1 to clear the override
        public static void SetRestAnimation(uint player, int animation)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetRestAnimation");
            Internal.NativeFunctions.nwnxPushInt(animation);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Override a visual transform on the given object that only oPlayer will see.
        // - oObject can be any valid Creature, Placeable, Item or Door.
        // - nTransform is one of OBJECT_VISUAL_TRANSFORM_* or -1 to remove the override
        // - fValue depends on the transformation to apply.
        public static void SetObjectVisualTransformOverride(uint player, uint target, int transform,
            float valueToApply)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetObjectVisualTransformOverride");
            Internal.NativeFunctions.nwnxPushFloat(valueToApply);
            Internal.NativeFunctions.nwnxPushInt(transform);
            Internal.NativeFunctions.nwnxPushObject(target);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Apply a looping visualeffect to target that only player can see
        // visualeffect: VFX_DUR_*, call again to remove an applied effect
        //               -1 to remove all effects
        //
        // Note: Only really works with looping effects: VFX_DUR_*
        //       Other types *kind* of work, they'll play when reentering the area and the object is in view
        //       or when they come back in view range.
        public static void ApplyLoopingVisualEffectToObject(uint player, uint target, VisualEffect visualEffect)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ApplyLoopingVisualEffectToObject");
            Internal.NativeFunctions.nwnxPushInt((int)visualEffect);
            Internal.NativeFunctions.nwnxPushObject(target);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Override the name of placeable for player only
        // "" to clear the override
        public static void SetPlaceableNameOverride(uint player, uint placeable, string name)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPlaceableNameOverride");
            Internal.NativeFunctions.nwnxPushString(name);
            Internal.NativeFunctions.nwnxPushObject(placeable);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Gets whether a quest has been completed by a player
        // Returns -1 if they don't have the journal entry
        public static int GetQuestCompleted(uint player, string questName)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetQuestCompleted");
            Internal.NativeFunctions.nwnxPushString(questName);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // This will require storing the PC's cd key or community name (depending on how you store in your vault)
        // and bic_filename along with routinely updating their location in some persistent method like OnRest,
        // OnAreaEnter and OnClentExit.
        //
        // Place waypoints on module load representing where a PC should start
        public static void SetPersistentLocation(string cdKeyOrCommunityName, string bicFileName, uint wayPoint,
            bool firstConnect = true)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPersistentLocation");
            Internal.NativeFunctions.nwnxPushInt(firstConnect ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(wayPoint);
            Internal.NativeFunctions.nwnxPushString(bicFileName);
            Internal.NativeFunctions.nwnxPushString(cdKeyOrCommunityName);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Force an item name to be updated.
        // This is a workaround for bug that occurs when updating item names in open containers.
        public static void UpdateItemName(uint player, uint item)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "UpdateItemName");
            Internal.NativeFunctions.nwnxPushObject(item);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // Possesses a creature by temporarily making them a familiar
        // This command allows a PC to possess an NPC by temporarily adding them as a familiar. It will work
        // if the player already has an existing familiar. The creatures must be in the same area. Unpossession can be
        // done with the regular @nwn{UnpossessFamiliar} commands.
        // The possessed creature will send automap data back to the possessor.
        // If you wish to prevent this you may wish to use NWNX_Player_GetAreaExplorationState() and
        // NWNX_Player_SetAreaExplorationState() before and after the possession.
        // The possessing creature will be left wherever they were when beginning the possession. You may wish
        // to use @nwn{EffectCutsceneImmobilize} and @nwn{EffectCutsceneGhost} to hide them.
        public static bool PossessCreature(uint possessor, uint possessed, bool mindImmune = true,
            bool createDefaultQB = false)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "PossessCreature");
            Internal.NativeFunctions.nwnxPushInt(createDefaultQB ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(mindImmune ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(possessed);
            Internal.NativeFunctions.nwnxPushObject(possessor);
            Internal.NativeFunctions.nwnxCallFunction();
            return Convert.ToBoolean(Internal.NativeFunctions.nwnxPopInt());
        }

        // Returns the platform ID of the given player (NWNX_PLAYER_PLATFORM_*)
        public static int GetPlatformId(uint player)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPlatformId");
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Returns the game language of the given player (uses NWNX_DIALOG_LANGUAGE_*)
        public static int GetLanguage(uint player)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetLanguage");
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }

        // Override sOldResName with sNewResName of nResType for oPlayer.
        public static void SetResManOverride(uint player, int resType, string resName, string newResName)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetResManOverride");
            Internal.NativeFunctions.nwnxPushString(newResName);
            Internal.NativeFunctions.nwnxPushString(resName);
            Internal.NativeFunctions.nwnxPushInt(resType);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        // @brief Toggle oPlayer's PlayerDM status.
        // @note This function does nothing for actual DMClient DMs or players with a client version < 8193.14
        // @param oPlayer The player.
        // @param bIsDM TRUE to toggle dm mode on, FALSE for off.
        public static void ToggleDM(uint oPlayer, bool bIsDM)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ToggleDM");

            Internal.NativeFunctions.nwnxPushInt(bIsDM ? 1 : 0);
            Internal.NativeFunctions.nwnxPushObject(oPlayer);
            Internal.NativeFunctions.nwnxCallFunction();
        }


        /// @brief Override the mouse cursor of oObject for oPlayer only
        /// @param oPlayer The player object.
        /// @param oObject The object.
        /// @param nCursor The cursor, one of MOUSECURSOR_*. -1 to clear the override.
        public static void SetObjectMouseCursorOverride(uint oPlayer, uint oObject, MouseCursor nCursor)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetObjectMouseCursorOverride");

            Internal.NativeFunctions.nwnxPushInt((int)nCursor);
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxPushObject(oPlayer);

            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Override the hilite color of oObject for oPlayer only
        /// @param oPlayer The player object.
        /// @param oObject The object.
        /// @param nColor The color in 0xRRGGBB format, -1 to clear the override.
        public static void SetObjectHiliteColorOverride(uint oPlayer, uint oObject, int nColor)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetObjectHiliteColorOverride");

            Internal.NativeFunctions.nwnxPushInt(nColor);
            Internal.NativeFunctions.nwnxPushObject(oObject);
            Internal.NativeFunctions.nwnxPushObject(oPlayer);

            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Remove effects with sEffectTag from oPlayer's TURD
        /// @note This function should be called in the NWNX_ON_CLIENT_DISCONNECT_AFTER event, OnClientLeave is too early for the TURD to exist.
        /// @param oPlayer The player object.
        /// @param sEffectTag The effect tag.
        public static void RemoveEffectFromTURD(uint oPlayer, string sEffectTag)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveEffectFromTURD");
            Internal.NativeFunctions.nwnxPushString(sEffectTag);
            Internal.NativeFunctions.nwnxPushObject(oPlayer);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Set the location oPlayer will spawn when logging in to the server.
        /// @note This function is best called in the NWNX_ON_ELC_VALIDATE_CHARACTER_BEFORE event, OnClientEnter will be too late.
        /// @param The player object.
        /// @param locSpawn The location.
        public static void SetSpawnLocation(uint oPlayer, Location locSpawn)
        {
            var vPosition = NWScript.NWScript.GetPositionFromLocation(locSpawn);

            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSpawnLocation");
            Internal.NativeFunctions.nwnxPushFloat(NWScript.NWScript.GetFacingFromLocation(locSpawn));
            Internal.NativeFunctions.nwnxPushFloat(vPosition.Z);
            Internal.NativeFunctions.nwnxPushFloat(vPosition.Y);
            Internal.NativeFunctions.nwnxPushFloat(vPosition.X);
            Internal.NativeFunctions.nwnxPushObject(NWScript.NWScript.GetAreaFromLocation(locSpawn));
            Internal.NativeFunctions.nwnxPushObject(oPlayer);

            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetCustomToken(uint oPlayer, int nCustomTokenNumber, string sTokenValue)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCustomToken");
            Internal.NativeFunctions.nwnxPushString(sTokenValue);
            Internal.NativeFunctions.nwnxPushInt(nCustomTokenNumber);
            Internal.NativeFunctions.nwnxPushObject(oPlayer);

            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetCreatureNameOverride(uint oPlayer, uint oCreature, string sName)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCreatureNameOverride");

            Internal.NativeFunctions.nwnxPushString(sName);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxPushObject(oPlayer);

            Internal.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Display floaty text above oCreature for oPlayer only.
        /// @note This will also display the floaty text above creatures that are not part of oPlayer's faction.
        /// @param oPlayer The player to display the text to.
        /// @param oCreature The creature to display the text above.
        /// @param sText The text to display.
        public static void FloatingTextStringOnCreature(uint oPlayer, uint oCreature, string sText)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCreatureNameOverride");

            Internal.NativeFunctions.nwnxPushString(sText);
            Internal.NativeFunctions.nwnxPushObject(oCreature);
            Internal.NativeFunctions.nwnxPushObject(oPlayer);

            Internal.NativeFunctions.nwnxCallFunction();
        }
        /// @brief Give a custom journal entry to oPlayer.
        /// @warning Custom entries are wiped on client enter - they must be reapplied.
        /// @param oPlayer The player object.
        /// @param journalEntry The journal entry in the form of a struct.
        /// @param silentUpdate 0 = Notify player via sound effects and feedback message, 1 = Suppress sound effects and feedback message
        /// @return a positive number to indicate the new amount of journal entries on the player.
        /// @note In contrast to conventional nwn journal entries - this method will overwrite entries with the same tag, so the index / count of entries
        /// will only increase if you add new entries with unique tags
        public static int AddCustomJournalEntry(uint player, JournalEntry journalEntry, bool isSilentUpdate = false)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddCustomJournalEntry");
            Internal.NativeFunctions.nwnxPushInt(isSilentUpdate ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(journalEntry.TimeOfDay);
            Internal.NativeFunctions.nwnxPushInt(journalEntry.CalendarDay);
            Internal.NativeFunctions.nwnxPushInt(journalEntry.Updated);
            Internal.NativeFunctions.nwnxPushInt(journalEntry.IsQuestDisplayed ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(journalEntry.IsQuestCompleted ? 1 : 0);
            Internal.NativeFunctions.nwnxPushInt(journalEntry.Priority);
            Internal.NativeFunctions.nwnxPushInt(journalEntry.State);
            Internal.NativeFunctions.nwnxPushString(journalEntry.Tag);
            Internal.NativeFunctions.nwnxPushString(journalEntry.Text);
            Internal.NativeFunctions.nwnxPushString(journalEntry.Name);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();
            return Internal.NativeFunctions.nwnxPopInt();
        }


        /// @brief Returns a struct containing a journal entry that can then be modified.
        /// @param oPlayer The player object.
        /// @param questTag The quest tag you wish to get the journal entry for.
        /// @return a struct containing the journal entry data.
        /// @note This method will return -1 for the Updated field in the event that no matching journal entry was found,
        /// only the last matching quest tag will be returned. Eg: If you add 3 journal updates to a player, only the 3rd one will be returned as
        /// that is the active one that the player currently sees.
        public static JournalEntry GetJournalEntry(uint player, string questTag)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetJournalEntry");
            var entry = new JournalEntry();

            Internal.NativeFunctions.nwnxPushString(questTag);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();

            entry.Updated = Internal.NativeFunctions.nwnxPopInt();
            if(entry.Updated == -1) // -1 set as an indicator to say that the entry was not found
            {
                return entry;
            }

            entry.IsQuestDisplayed = Internal.NativeFunctions.nwnxPopInt() == 1;
            entry.IsQuestCompleted = Internal.NativeFunctions.nwnxPopInt() == 1;
            entry.Priority = Internal.NativeFunctions.nwnxPopInt();
            entry.State = Internal.NativeFunctions.nwnxPopInt();
            entry.TimeOfDay = Internal.NativeFunctions.nwnxPopInt();
            entry.CalendarDay = Internal.NativeFunctions.nwnxPopInt();
            entry.Name = Internal.NativeFunctions.nwnxPopString();
            entry.Text = Internal.NativeFunctions.nwnxPopString();
            entry.Tag = questTag;
            return entry;
        }

        public static void CloseStore(uint oPlayer)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "CloseStore");
            Internal.NativeFunctions.nwnxPushObject(oPlayer);
            Internal.NativeFunctions.nwnxCallFunction();
        }

        public static void SetTlkOverride(uint oPlayer, int nStrRef, string sOverride, bool bRestoreGlobal = true)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetTlkOverride");
            Internal.NativeFunctions.nwnxPushInt(bRestoreGlobal ? 1 : 0);
            Internal.NativeFunctions.nwnxPushString(sOverride);
            Internal.NativeFunctions.nwnxPushInt(nStrRef);
            Internal.NativeFunctions.nwnxPushObject(oPlayer);
            Internal.NativeFunctions.nwnxCallFunction();
        }
    }
}