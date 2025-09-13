using System;
using System.Numerics;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Core.NWNX
{
    public class PlayerPlugin
    {
        private const string PLUGIN_NAME = "NWNX_Player";

        // Force display placeable examine window for player
        // If used on a placeable in a different area than the player, the portait will not be shown.
        public static void ForcePlaceableExamineWindow(uint player, uint placeable)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ForcePlaceableExamineWindow");
            NWNCore.NativeFunctions.nwnxPushObject(placeable);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
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
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ForcePlaceableInventoryWindow");
            NWNCore.NativeFunctions.nwnxPushObject(placeable);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Starts displaying a timing bar.
        // Will run a script at the end of the timing bar, if specified.
        public static void StartGuiTimingBar(uint player, float seconds, string script = "",
            TimingBarType type = TimingBarType.Custom)
        {
            if (GetLocalInt(player, "NWNX_PLAYER_GUI_TIMING_ACTIVE") == 1) return;
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "StartGuiTimingBar");
            NWNCore.NativeFunctions.nwnxPushInt((int)type);
            NWNCore.NativeFunctions.nwnxPushFloat(seconds);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();

            var id = GetLocalInt(player, "NWNX_PLAYER_GUI_TIMING_ID") + 1;
            SetLocalInt(player, "NWNX_PLAYER_GUI_TIMING_ACTIVE", id);
            SetLocalInt(player, "NWNX_PLAYER_GUI_TIMING_ID", id);

            DelayCommand(seconds, () => StopGuiTimingBar(player, script, id));
        }

        // Stops displaying a timing bar.
        // Runs a script if specified.
        private static void StopGuiTimingBar(uint creature, string script, int id)
        {
            var activeId = GetLocalInt(creature, "NWNX_PLAYER_GUI_TIMING_ACTIVE");
            // Either the timing event was never started, or it already finished.
            if (activeId == 0) return;
            // If id != -1, we ended up here through DelayCommand. Make sure it's for the right ID
            if (id != -1 && id != activeId) return;
            DeleteLocalInt(creature, "NWNX_PLAYER_GUI_TIMING_ACTIVE");
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "StopGuiTimingBar");
            NWNCore.NativeFunctions.nwnxPushObject(creature);
            NWNCore.NativeFunctions.nwnxCallFunction();
            if (!string.IsNullOrWhiteSpace(script)) ExecuteScript(script, creature);
        }

        // Stops displaying a timing bar.
        // Runs a script if specified.
        public static void StopGuiTimingBar(uint player, string script = "")
        {
            StopGuiTimingBar(player, script, -1);
        }

        // Sets whether the player should always walk when given movement commands.
        // If true, clicking on the ground or using WASD will trigger walking instead of running.
        public static void SetAlwaysWalk(uint player, bool walk)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAlwaysWalk");
            NWNCore.NativeFunctions.nwnxPushInt(walk ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Gets the player's quickbar slot info
        public static QuickBarSlot GetQuickBarSlot(uint player, int slot)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetQuickBarSlot");
            var qbs = new QuickBarSlot();
            NWNCore.NativeFunctions.nwnxPushInt(slot);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
            qbs.Associate = NWNCore.NativeFunctions.nwnxPopObject();
            qbs.AssociateType = NWNCore.NativeFunctions.nwnxPopInt();
            qbs.DomainLevel = NWNCore.NativeFunctions.nwnxPopInt();
            qbs.MetaType = NWNCore.NativeFunctions.nwnxPopInt();
            qbs.INTParam1 = NWNCore.NativeFunctions.nwnxPopInt();
            qbs.ToolTip = NWNCore.NativeFunctions.nwnxPopString();
            qbs.CommandLine = NWNCore.NativeFunctions.nwnxPopString();
            qbs.CommandLabel = NWNCore.NativeFunctions.nwnxPopString();
            qbs.Resref = NWNCore.NativeFunctions.nwnxPopString();
            qbs.MultiClass = NWNCore.NativeFunctions.nwnxPopInt();
            qbs.ObjectType = (QuickBarSlotType)NWNCore.NativeFunctions.nwnxPopInt();
            qbs.SecondaryItem = NWNCore.NativeFunctions.nwnxPopObject();
            qbs.Item = NWNCore.NativeFunctions.nwnxPopObject();
            return qbs;
        }

        // Sets a player's quickbar slot
        public static void SetQuickBarSlot(uint player, int slot, QuickBarSlot qbs)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetQuickBarSlot");
            NWNCore.NativeFunctions.nwnxPushObject(qbs.Item ?? OBJECT_INVALID);
            NWNCore.NativeFunctions.nwnxPushObject(qbs.SecondaryItem ?? OBJECT_INVALID);
            NWNCore.NativeFunctions.nwnxPushInt((int)qbs.ObjectType);
            NWNCore.NativeFunctions.nwnxPushInt(qbs.MultiClass);
            NWNCore.NativeFunctions.nwnxPushString(qbs.Resref!);
            NWNCore.NativeFunctions.nwnxPushString(qbs.CommandLabel!);
            NWNCore.NativeFunctions.nwnxPushString(qbs.CommandLine!);
            NWNCore.NativeFunctions.nwnxPushString(qbs.ToolTip!);
            NWNCore.NativeFunctions.nwnxPushInt(qbs.INTParam1);
            NWNCore.NativeFunctions.nwnxPushInt(qbs.MetaType);
            NWNCore.NativeFunctions.nwnxPushInt(qbs.DomainLevel);
            NWNCore.NativeFunctions.nwnxPushInt(qbs.AssociateType);
            NWNCore.NativeFunctions.nwnxPushObject(qbs.Associate ?? OBJECT_INVALID);
            NWNCore.NativeFunctions.nwnxPushInt(slot);
            NWNCore.NativeFunctions.nwnxPushObject(player!);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }


        // Get the name of the .bic file associated with the player's character.
        public static string GetBicFileName(uint player)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetBicFileName");
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        // Plays the VFX at the target position in current area for the given player only
        public static void ShowVisualEffect(uint player, int effectId, float scale, Vector3 position, Vector3 translate, Vector3 rotate)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ShowVisualEffect");
            
            NWNCore.NativeFunctions.nwnxPushFloat(rotate.X);
            NWNCore.NativeFunctions.nwnxPushFloat(rotate.Y);
            NWNCore.NativeFunctions.nwnxPushFloat(rotate.Z);
            NWNCore.NativeFunctions.nwnxPushFloat(translate.X);
            NWNCore.NativeFunctions.nwnxPushFloat(translate.Y);
            NWNCore.NativeFunctions.nwnxPushFloat(translate.Z);
            NWNCore.NativeFunctions.nwnxPushFloat(scale);
            NWNCore.NativeFunctions.nwnxPushFloat(position.X);
            NWNCore.NativeFunctions.nwnxPushFloat(position.Y);
            NWNCore.NativeFunctions.nwnxPushFloat(position.Z);
            NWNCore.NativeFunctions.nwnxPushInt(effectId);
            NWNCore.NativeFunctions.nwnxPushObject(player);

            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Changes the nighttime music track for the given player only
        public static void MusicBackgroundChangeTimeToggle(uint player, int track, bool night)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ChangeBackgroundMusic");
            NWNCore.NativeFunctions.nwnxPushInt(track);
            NWNCore.NativeFunctions.nwnxPushInt(night ? 1 : 0); // bool day = false
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Toggle the background music for the given player only
        public static void MusicBackgroundToggle(uint player, bool on)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "PlayBackgroundMusic");
            NWNCore.NativeFunctions.nwnxPushInt(on ? 1 : 0); // bool play = false
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Changes the battle music track for the given player only
        public static void MusicBattleChange(uint player, int track)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ChangeBattleMusic");
            NWNCore.NativeFunctions.nwnxPushInt(track);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Toggle the background music for the given player only
        public static void MusicBattleToggle(uint player, bool on)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "PlayBattleMusic");
            NWNCore.NativeFunctions.nwnxPushInt(on ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Play a sound at the location of target for the given player only
        // If target is OBJECT_INVALID the sound will play at the location of the player
        public static void PlaySound(uint player, string sound, uint target)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "PlaySound");
            NWNCore.NativeFunctions.nwnxPushObject(target);
            NWNCore.NativeFunctions.nwnxPushString(sound);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Toggle a placeable's usable flag for the given player only
        public static void SetPlaceableUseable(uint player, uint placeable, bool usable)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPlaceableUsable");
            NWNCore.NativeFunctions.nwnxPushInt(usable ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(placeable);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Override player's rest duration
        // Duration is in milliseconds, 1000 = 1 second
        // Minimum duration of 10ms
        // -1 clears the override
        public static void SetRestDuration(uint player, int duration)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetRestDuration");
            NWNCore.NativeFunctions.nwnxPushInt(duration);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Apply visualeffect to target that only player can see
        // Note: Only works with instant effects: VFX_COM_*, VFX_FNF_*, VFX_IMP_*
        public static void ApplyInstantVisualEffectToObject(uint player, uint target, VisualEffect visualEffect)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ApplyInstantVisualEffectToObject");
            NWNCore.NativeFunctions.nwnxPushInt((int)visualEffect);
            NWNCore.NativeFunctions.nwnxPushObject(target);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Refreshes the players character sheet
        // Note: You may need to use DelayCommand if you're manipulating values
        // through nwnx and forcing a UI refresh, 0.5s seemed to be fine
        public static void UpdateCharacterSheet(uint player)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "UpdateCharacterSheet");
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Allows player to open target's inventory
        // Target must be a creature or another player
        // Note: only works if player and target are in the same area
        public static void OpenInventory(uint player, uint target, bool open = true)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "OpenInventory");
            NWNCore.NativeFunctions.nwnxPushInt(open ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(target);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Get player's area exploration state
        public static string GetAreaExplorationState(uint player, uint area)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetAreaExplorationState");
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopString();
        }

        // Set player's area exploration state (str is an encoded string obtained with NWNX_Player_GetAreaExplorationState)
        public static void SetAreaExplorationState(uint player, uint area, string encodedString)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetAreaExplorationState");
            NWNCore.NativeFunctions.nwnxPushString(encodedString);
            NWNCore.NativeFunctions.nwnxPushObject(area);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Override oPlayer's rest animation to nAnimation
        //
        // NOTE: nAnimation does not take ANIMATION_LOOPING_* or ANIMATION_FIREFORGET_* constants
        //       Use NWNX_Consts_TranslateNWScriptAnimation() in nwnx_consts.nss to get their NWNX equivalent
        //       -1 to clear the override
        public static void SetRestAnimation(uint player, int animation)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetRestAnimation");
            NWNCore.NativeFunctions.nwnxPushInt(animation);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Override a visual transform on the given object that only oPlayer will see.
        // - oObject can be any valid Creature, Placeable, Item or Door.
        // - nTransform is one of OBJECT_VISUAL_TRANSFORM_* or -1 to remove the override
        // - fValue depends on the transformation to apply.
        public static void SetObjectVisualTransformOverride(uint player, uint target, int transform,
            float valueToApply)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetObjectVisualTransformOverride");
            NWNCore.NativeFunctions.nwnxPushFloat(valueToApply);
            NWNCore.NativeFunctions.nwnxPushInt(transform);
            NWNCore.NativeFunctions.nwnxPushObject(target);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
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
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ApplyLoopingVisualEffectToObject");
            NWNCore.NativeFunctions.nwnxPushInt((int)visualEffect);
            NWNCore.NativeFunctions.nwnxPushObject(target);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Override the name of placeable for player only
        // "" to clear the override
        public static void SetPlaceableNameOverride(uint player, uint placeable, string name)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPlaceableNameOverride");
            NWNCore.NativeFunctions.nwnxPushString(name);
            NWNCore.NativeFunctions.nwnxPushObject(placeable);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Gets whether a quest has been completed by a player
        // Returns -1 if they don't have the journal entry
        public static int GetQuestCompleted(uint player, string questName)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetQuestCompleted");
            NWNCore.NativeFunctions.nwnxPushString(questName);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // This will require storing the PC's cd key or community name (depending on how you store in your vault)
        // and bic_filename along with routinely updating their location in some persistent method like OnRest,
        // OnAreaEnter and OnClentExit.
        //
        // Place waypoints on module load representing where a PC should start
        public static void SetPersistentLocation(string cdKeyOrCommunityName, string bicFileName, uint wayPoint,
            bool firstConnect = true)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetPersistentLocation");
            NWNCore.NativeFunctions.nwnxPushInt(firstConnect ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(wayPoint);
            NWNCore.NativeFunctions.nwnxPushString(bicFileName);
            NWNCore.NativeFunctions.nwnxPushString(cdKeyOrCommunityName);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // Force an item name to be updated.
        // This is a workaround for bug that occurs when updating item names in open containers.
        public static void UpdateItemName(uint player, uint item)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "UpdateItemName");
            NWNCore.NativeFunctions.nwnxPushObject(item);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
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
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "PossessCreature");
            NWNCore.NativeFunctions.nwnxPushInt(createDefaultQB ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(mindImmune ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(possessed);
            NWNCore.NativeFunctions.nwnxPushObject(possessor);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return Convert.ToBoolean(NWNCore.NativeFunctions.nwnxPopInt());
        }

        // Returns the platform ID of the given player (NWNX_PLAYER_PLATFORM_*)
        public static int GetPlatformId(uint player)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetPlatformId");
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Returns the game language of the given player (uses NWNX_DIALOG_LANGUAGE_*)
        public static int GetLanguage(uint player)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetLanguage");
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
        }

        // Override sOldResName with sNewResName of nResType for oPlayer.
        public static void SetResManOverride(uint player, int resType, string resName, string newResName)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetResManOverride");
            NWNCore.NativeFunctions.nwnxPushString(newResName);
            NWNCore.NativeFunctions.nwnxPushString(resName);
            NWNCore.NativeFunctions.nwnxPushInt(resType);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        // @brief Toggle oPlayer's PlayerDM status.
        // @note This function does nothing for actual DMClient DMs or players with a client version < 8193.14
        // @param oPlayer The player.
        // @param bIsDM TRUE to toggle dm mode on, FALSE for off.
        public static void ToggleDM(uint oPlayer, bool bIsDM)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ToggleDM");

            NWNCore.NativeFunctions.nwnxPushInt(bIsDM ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushObject(oPlayer);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }


        /// @brief Override the mouse cursor of oObject for oPlayer only
        /// @param oPlayer The player object.
        /// @param oObject The object.
        /// @param nCursor The cursor, one of MOUSECURSOR_*. -1 to clear the override.
        public static void SetObjectMouseCursorOverride(uint oPlayer, uint oObject, MouseCursor nCursor)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetObjectMouseCursorOverride");

            NWNCore.NativeFunctions.nwnxPushInt((int)nCursor);
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxPushObject(oPlayer);

            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Override the hilite color of oObject for oPlayer only
        /// @param oPlayer The player object.
        /// @param oObject The object.
        /// @param nColor The color in 0xRRGGBB format, -1 to clear the override.
        public static void SetObjectHiliteColorOverride(uint oPlayer, uint oObject, int nColor)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetObjectHiliteColorOverride");

            NWNCore.NativeFunctions.nwnxPushInt(nColor);
            NWNCore.NativeFunctions.nwnxPushObject(oObject);
            NWNCore.NativeFunctions.nwnxPushObject(oPlayer);

            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Remove effects with sEffectTag from oPlayer's TURD
        /// @note This function should be called in the NWNX_ON_CLIENT_DISCONNECT_AFTER event, OnClientLeave is too early for the TURD to exist.
        /// @param oPlayer The player object.
        /// @param sEffectTag The effect tag.
        public static void RemoveEffectFromTURD(uint oPlayer, string sEffectTag)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "RemoveEffectFromTURD");
            NWNCore.NativeFunctions.nwnxPushString(sEffectTag);
            NWNCore.NativeFunctions.nwnxPushObject(oPlayer);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Set the location oPlayer will spawn when logging in to the server.
        /// @note This function is best called in the NWNX_ON_ELC_VALIDATE_CHARACTER_BEFORE event, OnClientEnter will be too late.
        /// @param The player object.
        /// @param locSpawn The location.
        public static void SetSpawnLocation(uint oPlayer, Location locSpawn)
        {
            var vPosition = GetPositionFromLocation(locSpawn);

            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetSpawnLocation");
            NWNCore.NativeFunctions.nwnxPushFloat(GetFacingFromLocation(locSpawn));
            NWNCore.NativeFunctions.nwnxPushFloat(vPosition.Z);
            NWNCore.NativeFunctions.nwnxPushFloat(vPosition.Y);
            NWNCore.NativeFunctions.nwnxPushFloat(vPosition.X);
            NWNCore.NativeFunctions.nwnxPushObject(GetAreaFromLocation(locSpawn));
            NWNCore.NativeFunctions.nwnxPushObject(oPlayer);

            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetCustomToken(uint oPlayer, int nCustomTokenNumber, string sTokenValue)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCustomToken");
            NWNCore.NativeFunctions.nwnxPushString(sTokenValue);
            NWNCore.NativeFunctions.nwnxPushInt(nCustomTokenNumber);
            NWNCore.NativeFunctions.nwnxPushObject(oPlayer);

            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetCreatureNameOverride(uint oPlayer, uint oCreature, string sName)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCreatureNameOverride");

            NWNCore.NativeFunctions.nwnxPushString(sName);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxPushObject(oPlayer);

            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// @brief Display floaty text above oCreature for oPlayer only.
        /// @note This will also display the floaty text above creatures that are not part of oPlayer's faction.
        /// @param oPlayer The player to display the text to.
        /// @param oCreature The creature to display the text above.
        /// @param sText The text to display.
        public static void FloatingTextStringOnCreature(uint oPlayer, uint oCreature, string sText)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetCreatureNameOverride");

            NWNCore.NativeFunctions.nwnxPushString(sText);
            NWNCore.NativeFunctions.nwnxPushObject(oCreature);
            NWNCore.NativeFunctions.nwnxPushObject(oPlayer);

            NWNCore.NativeFunctions.nwnxCallFunction();
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
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "AddCustomJournalEntry");
            NWNCore.NativeFunctions.nwnxPushInt(isSilentUpdate ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(journalEntry.TimeOfDay);
            NWNCore.NativeFunctions.nwnxPushInt(journalEntry.CalendarDay);
            NWNCore.NativeFunctions.nwnxPushInt(journalEntry.Updated);
            NWNCore.NativeFunctions.nwnxPushInt(journalEntry.IsQuestDisplayed ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(journalEntry.IsQuestCompleted ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushInt(journalEntry.Priority);
            NWNCore.NativeFunctions.nwnxPushInt(journalEntry.State);
            NWNCore.NativeFunctions.nwnxPushString(journalEntry.Tag);
            NWNCore.NativeFunctions.nwnxPushString(journalEntry.Text);
            NWNCore.NativeFunctions.nwnxPushString(journalEntry.Name);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopInt();
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
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetJournalEntry");
            var entry = new JournalEntry();

            NWNCore.NativeFunctions.nwnxPushString(questTag);
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();

            entry.Updated = NWNCore.NativeFunctions.nwnxPopInt();
            if(entry.Updated == -1) // -1 set as an indicator to say that the entry was not found
            {
                return entry;
            }

            entry.IsQuestDisplayed = NWNCore.NativeFunctions.nwnxPopInt() == 1;
            entry.IsQuestCompleted = NWNCore.NativeFunctions.nwnxPopInt() == 1;
            entry.Priority = NWNCore.NativeFunctions.nwnxPopInt();
            entry.State = NWNCore.NativeFunctions.nwnxPopInt();
            entry.TimeOfDay = NWNCore.NativeFunctions.nwnxPopInt();
            entry.CalendarDay = NWNCore.NativeFunctions.nwnxPopInt();
            entry.Name = NWNCore.NativeFunctions.nwnxPopString();
            entry.Text = NWNCore.NativeFunctions.nwnxPopString();
            entry.Tag = questTag;
            return entry;
        }

        public static void CloseStore(uint oPlayer)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "CloseStore");
            NWNCore.NativeFunctions.nwnxPushObject(oPlayer);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        public static void SetTlkOverride(uint oPlayer, int nStrRef, string sOverride, bool bRestoreGlobal = true)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "SetTlkOverride");
            NWNCore.NativeFunctions.nwnxPushInt(bRestoreGlobal ? 1 : 0);
            NWNCore.NativeFunctions.nwnxPushString(sOverride);
            NWNCore.NativeFunctions.nwnxPushInt(nStrRef);
            NWNCore.NativeFunctions.nwnxPushObject(oPlayer);
            NWNCore.NativeFunctions.nwnxCallFunction();
        }

        /// <summary>
        /// Get the current open store of player.
        /// Returns OBJECT_INVALID if no store is open.
        /// </summary>
        public static uint GetOpenStore(uint player)
        {
            NWNCore.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "GetOpenStore");
            NWNCore.NativeFunctions.nwnxPushObject(player);
            NWNCore.NativeFunctions.nwnxCallFunction();
            return NWNCore.NativeFunctions.nwnxPopObject();
        }
    }
}