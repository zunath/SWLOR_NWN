using System;
using SWLOR.Game.Server.NWN;

namespace SWLOR.Game.Server.NWNX
{
    public class NWNXPlayer
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
            if (_.GetLocalInt(player, "NWNX_PLAYER_GUI_TIMING_ACTIVE") == 1) return;
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "StartGuiTimingBar");
            Internal.NativeFunctions.nwnxPushInt((int)type);
            Internal.NativeFunctions.nwnxPushFloat(seconds);
            Internal.NativeFunctions.nwnxPushObject(player);
            Internal.NativeFunctions.nwnxCallFunction();

            var id = _.GetLocalInt(player, "NWNX_PLAYER_GUI_TIMING_ID") + 1;
            _.SetLocalInt(player, "NWNX_PLAYER_GUI_TIMING_ACTIVE", id);
            _.SetLocalInt(player, "NWNX_PLAYER_GUI_TIMING_ID", id);

            _.DelayCommand(seconds, () => StopGuiTimingBar(player, script, id));
        }

        // Stops displaying a timing bar.
        // Runs a script if specified.
        public static void StopGuiTimingBar(uint creature, string script, int id)
        {
            var activeId = _.GetLocalInt(creature, "NWNX_PLAYER_GUI_TIMING_ACTIVE");
            // Either the timing event was never started, or it already finished.
            if (activeId == 0) return;
            // If id != -1, we ended up here through DelayCommand. Make sure it's for the right ID
            if (id != -1 && id != activeId) return;
            _.DeleteLocalInt(creature, "NWNX_PLAYER_GUI_TIMING_ACTIVE");
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "StopGuiTimingBar");
            Internal.NativeFunctions.nwnxPushObject(creature);
            Internal.NativeFunctions.nwnxCallFunction();
            if (!string.IsNullOrWhiteSpace(script)) _.ExecuteScript(script, creature);
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
        public static void ShowVisualEffect(uint player, int effectId, Vector position)
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
        public static void ApplyInstantVisualEffectToObject(uint player, uint target, int visualEffect)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ApplyInstantVisualEffectToObject");
            Internal.NativeFunctions.nwnxPushInt(visualEffect);
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
        public static void ApplyLoopingVisualEffectToObject(uint player, uint target, int visualEffect)
        {
            Internal.NativeFunctions.nwnxSetFunction(PLUGIN_NAME, "ApplyLoopingVisualEffectToObject");
            Internal.NativeFunctions.nwnxPushInt(visualEffect);
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
    }
}
