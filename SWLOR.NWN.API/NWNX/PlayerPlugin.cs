using System.Numerics;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.Core.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using QuickBarSlot = SWLOR.Game.Server.Core.NWNX.Enum.QuickBarSlot;

namespace SWLOR.NWN.API.NWNX
{
    public static class PlayerPlugin
    {
        /// <summary>
        /// Force display placeable examine window for player.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="placeable">The placeable object.</param>
        /// <remarks>If used on a placeable in a different area than the player, the portrait will not be shown.</remarks>
        public static void ForcePlaceableExamineWindow(uint player, uint placeable)
        {
            global::NWN.Core.NWNX.PlayerPlugin.ForcePlaceableExamineWindow(player, placeable);
        }

        /// <summary>
        /// Force opens the target object's inventory for the player.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="placeable">The placeable object.</param>
        /// <remarks>
        /// A few notes about this function:
        /// - If the placeable is in a different area than the player, the portrait will not be shown
        /// - The placeable's open/close animations will be played
        /// - Clicking the 'close' button will cause the player to walk to the placeable; If the placeable is in a different area, the player will just walk to the edge of the current area and stop. This action can be cancelled manually.
        /// - Walking will close the placeable automatically.
        /// </remarks>
        public static void ForcePlaceableInventoryWindow(uint player, uint placeable)
        {
            global::NWN.Core.NWNX.PlayerPlugin.ForcePlaceableInventoryWindow(player, placeable);
        }

        /// <summary>
        /// Starts displaying a timing bar.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="seconds">The length of time the timing bar will complete.</param>
        /// <param name="script">The script to run at the bar's completion.</param>
        /// <param name="type">The Timing Bar Type.</param>
        /// <remarks>Only one timing bar can be ran at the same time. Will run a script at the end of the timing bar, if specified.</remarks>
        public static void StartGuiTimingBar(uint player, float seconds, string script = "",
            TimingBarType type = TimingBarType.Custom)
        {
            if (GetLocalInt(player, "NWNX_PLAYER_GUI_TIMING_ACTIVE") == 1) return;
            global::NWN.Core.NWNX.PlayerPlugin.StartGuiTimingBar(player, seconds, script, (int)type);
        }

        /// <summary>
        /// Stops displaying a timing bar.
        /// </summary>
        /// <param name="creature">The player object.</param>
        /// <param name="script">The script to run when stopped.</param>
        /// <param name="id">Internal ID for tracking.</param>
        /// <remarks>Runs a script if specified.</remarks>
        private static void StopGuiTimingBar(uint creature, string script, int id)
        {
            var activeId = GetLocalInt(creature, "NWNX_PLAYER_GUI_TIMING_ACTIVE");
            // Either the timing event was never started, or it already finished.
            if (activeId == 0) return;
            // If id != -1, we ended up here through DelayCommand. Make sure it's for the right ID
            if (id != -1 && id != activeId) return;
            DeleteLocalInt(creature, "NWNX_PLAYER_GUI_TIMING_ACTIVE");
            global::NWN.Core.NWNX.PlayerPlugin.StopGuiTimingBar(creature, script);
        }

        /// <summary>
        /// Stops displaying a timing bar.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="script">The script to run when stopped.</param>
        /// <remarks>Runs a script if specified.</remarks>
        public static void StopGuiTimingBar(uint player, string script = "")
        {
            StopGuiTimingBar(player, script, -1);
        }

        /// <summary>
        /// Sets whether the player should always walk when given movement commands.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="walk">True to set the player to always walk.</param>
        /// <remarks>If true, clicking on the ground or using WASD will trigger walking instead of running.</remarks>
        public static void SetAlwaysWalk(uint player, bool walk)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetAlwaysWalk(player, walk ? 1 : 0);
        }

        /// <summary>
        /// Gets the player's quickbar slot info.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="slot">Slot ID 0-35.</param>
        /// <returns>An QuickBarSlot struct.</returns>
        public static QuickBarSlot GetQuickBarSlot(uint player, int slot)
        {
            var coreResult = global::NWN.Core.NWNX.PlayerPlugin.GetQuickBarSlot(player, slot);
            return new QuickBarSlot
            {
                Associate = coreResult.oAssociate,
                AssociateType = coreResult.nAssociateType,
                DomainLevel = coreResult.nDomainLevel,
                MetaType = coreResult.nMetaType,
                INTParam1 = coreResult.nINTParam1,
                ToolTip = coreResult.sToolTip,
                CommandLine = coreResult.sCommandLine,
                CommandLabel = coreResult.sCommandLabel,
                Resref = coreResult.sResRef,
                MultiClass = coreResult.nMultiClass,
                ObjectType = (QuickBarSlotType)coreResult.nObjectType,
                SecondaryItem = coreResult.oSecondaryItem,
                Item = coreResult.oItem
            };
        }

        /// <summary>
        /// Sets a player's quickbar slot.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="slot">Slot ID 0-35.</param>
        /// <param name="qbs">An QuickBarSlot struct.</param>
        public static void SetQuickBarSlot(uint player, int slot, QuickBarSlot qbs)
        {
            var coreQbs = new global::NWN.Core.NWNX.QuickBarSlot
            {
                oItem = qbs.Item ?? OBJECT_INVALID,
                oSecondaryItem = qbs.SecondaryItem ?? OBJECT_INVALID,
                nObjectType = (int)qbs.ObjectType,
                nMultiClass = qbs.MultiClass,
                sResRef = qbs.Resref ?? "",
                sCommandLabel = qbs.CommandLabel ?? "",
                sCommandLine = qbs.CommandLine ?? "",
                sToolTip = qbs.ToolTip ?? "",
                nINTParam1 = qbs.INTParam1,
                nMetaType = qbs.MetaType,
                nDomainLevel = qbs.DomainLevel,
                nAssociateType = qbs.AssociateType,
                oAssociate = qbs.Associate ?? OBJECT_INVALID
            };
            global::NWN.Core.NWNX.PlayerPlugin.SetQuickBarSlot(player, slot, coreQbs);
        }


        /// <summary>
        /// Get the name of the .bic file associated with the player's character.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <returns>The filename for this player's bic. (Not including the ".bic").</returns>
        public static string GetBicFileName(uint player)
        {
            return global::NWN.Core.NWNX.PlayerPlugin.GetBicFileName(player);
        }

        /// <summary>
        /// Plays the VFX at the target position in current area for the given player only.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="effectId">The effect id.</param>
        /// <param name="position">The position to play the visual effect.</param>
        /// <param name="scale">The scale of the effect.</param>
        /// <param name="translate">A translation vector to offset the position of the effect.</param>
        /// <param name="rotate">A rotation vector to rotate the effect.</param>
        public static void ShowVisualEffect(uint player, int effectId, float scale, Vector3 position, Vector3 translate, Vector3 rotate)
        {
            global::NWN.Core.NWNX.PlayerPlugin.ShowVisualEffect(player, effectId, position, scale, translate, rotate);
        }

        /// <summary>
        /// Changes the nighttime music track for the given player only.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="track">The track id to play.</param>
        /// <param name="night">True for night, false for day.</param>
        public static void MusicBackgroundChangeTimeToggle(uint player, int track, bool night)
        {
            if (night)
                global::NWN.Core.NWNX.PlayerPlugin.MusicBackgroundChangeNight(player, track);
            else
                global::NWN.Core.NWNX.PlayerPlugin.MusicBackgroundChangeDay(player, track);
        }

        /// <summary>
        /// Toggle the background music for the given player only.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="on">True to start, false to stop.</param>
        public static void MusicBackgroundToggle(uint player, bool on)
        {
            if (on)
                global::NWN.Core.NWNX.PlayerPlugin.MusicBackgroundStart(player);
            else
                global::NWN.Core.NWNX.PlayerPlugin.MusicBackgroundStop(player);
        }

        /// <summary>
        /// Changes the battle music track for the given player only.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="track">The track id to play.</param>
        public static void MusicBattleChange(uint player, int track)
        {
            global::NWN.Core.NWNX.PlayerPlugin.MusicBattleChange(player, track);
        }

        /// <summary>
        /// Toggle the battle music for the given player only.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="on">True to start, false to stop.</param>
        public static void MusicBattleToggle(uint player, bool on)
        {
            if (on)
                global::NWN.Core.NWNX.PlayerPlugin.MusicBattleStart(player);
            else
                global::NWN.Core.NWNX.PlayerPlugin.MusicBattleStop(player);
        }

        /// <summary>
        /// Play a sound at the location of target for the given player only.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="sound">The sound resref.</param>
        /// <param name="target">The target object for the sound to originate. If target OBJECT_INVALID the sound will play at the location of the player.</param>
        public static void PlaySound(uint player, string sound, uint target)
        {
            global::NWN.Core.NWNX.PlayerPlugin.PlaySound(player, sound, target);
        }

        /// <summary>
        /// Toggle a placeable's usable flag for the given player only.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="placeable">The placeable object.</param>
        /// <param name="usable">True for usable.</param>
        public static void SetPlaceableUseable(uint player, uint placeable, bool usable)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetPlaceableUsable(player, placeable, usable ? 1 : 0);
        }

        /// <summary>
        /// Override player's rest duration.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="duration">The duration of rest in milliseconds, 1000 = 1 second. Minimum duration of 10ms. -1 clears the override.</param>
        public static void SetRestDuration(uint player, int duration)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetRestDuration(player, duration);
        }

        /// <summary>
        /// Apply visualeffect to target that only player can see.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="target">The target object to play the effect upon.</param>
        /// <param name="visualEffect">The visual effect id.</param>
        /// <remarks>Only works with instant effects: VFX_COM_*, VFX_FNF_*, VFX_IMP_*</remarks>
        public static void ApplyInstantVisualEffectToObject(uint player, uint target, VisualEffect visualEffect)
        {
            global::NWN.Core.NWNX.PlayerPlugin.ApplyInstantVisualEffectToObject(player, target, (int)visualEffect);
        }

        /// <summary>
        /// Refreshes the players character sheet.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <remarks>You may need to use DelayCommand if you're manipulating values through nwnx and forcing a UI refresh, 0.5s seemed to be fine.</remarks>
        public static void UpdateCharacterSheet(uint player)
        {
            global::NWN.Core.NWNX.PlayerPlugin.UpdateCharacterSheet(player);
        }

        /// <summary>
        /// Allows player to open target's inventory.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="target">The target object, must be a creature or another player.</param>
        /// <param name="open">True to open.</param>
        /// <remarks>Only works if player and target are in the same area.</remarks>
        public static void OpenInventory(uint player, uint target, bool open = true)
        {
            global::NWN.Core.NWNX.PlayerPlugin.OpenInventory(player, target, open ? 1 : 0);
        }

        /// <summary>
        /// Get player's area exploration state.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="area">The area object.</param>
        /// <returns>A string representation of the tiles explored for that area.</returns>
        public static string GetAreaExplorationState(uint player, uint area)
        {
            return global::NWN.Core.NWNX.PlayerPlugin.GetAreaExplorationState(player, area);
        }

        /// <summary>
        /// Set player's area exploration state.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="area">The area object.</param>
        /// <param name="encodedString">An encoded string obtained with GetAreaExplorationState().</param>
        public static void SetAreaExplorationState(uint player, uint area, string encodedString)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetAreaExplorationState(player, area, encodedString);
        }

        /// <summary>
        /// Override oPlayer's rest animation to nAnimation.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="animation">The NWNX animation id. This does not take ANIMATION_LOOPING_* or ANIMATION_FIREFORGET_* constants. Instead use NWNX_Consts_TranslateNWScriptAnimation() to get the NWNX equivalent. -1 to clear the override.</param>
        public static void SetRestAnimation(uint player, int animation)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetRestAnimation(player, animation);
        }

        /// <summary>
        /// Override a visual transform on the given object that only oPlayer will see.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="target">The target object. Can be any valid Creature, Placeable, Item or Door.</param>
        /// <param name="transform">One of OBJECT_VISUAL_TRANSFORM_* or -1 to remove the override.</param>
        /// <param name="valueToApply">Depends on the transformation to apply.</param>
        public static void SetObjectVisualTransformOverride(uint player, uint target, int transform,
            float valueToApply)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetObjectVisualTransformOverride(player, target, transform, valueToApply);
        }

        /// <summary>
        /// Apply a looping visualeffect to target that only player can see.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="target">The target object.</param>
        /// <param name="visualEffect">A VFX_DUR_*. Calling again will remove an applied effect. -1 to remove all effects.</param>
        /// <remarks>Only really works with looping effects: VFX_DUR_*. Other types *kind* of work, they'll play when reentering the area and the object is in view or when they come back in view range.</remarks>
        public static void ApplyLoopingVisualEffectToObject(uint player, uint target, VisualEffect visualEffect)
        {
            global::NWN.Core.NWNX.PlayerPlugin.ApplyLoopingVisualEffectToObject(player, target, (int)visualEffect);
        }

        /// <summary>
        /// Override the name of placeable for player only.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="placeable">The placeable object.</param>
        /// <param name="name">The name for the placeable for this player, "" to clear the override.</param>
        public static void SetPlaceableNameOverride(uint player, uint placeable, string name)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetPlaceableNameOverride(player, placeable, name);
        }

        /// <summary>
        /// Gets whether a quest has been completed by a player.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="questName">The name identifier of the quest from the Journal Editor.</param>
        /// <returns>True if the quest has been completed. -1 if the player does not have the journal entry.</returns>
        public static int GetQuestCompleted(uint player, string questName)
        {
            return global::NWN.Core.NWNX.PlayerPlugin.GetQuestCompleted(player, questName);
        }

        /// <summary>
        /// Place waypoints on module load representing where a PC should start.
        /// </summary>
        /// <param name="cdKeyOrCommunityName">The Public CD Key or Community Name of the player, this will depend on your vault type.</param>
        /// <param name="bicFileName">The filename for the character. Retrieved with GetBicFileName().</param>
        /// <param name="wayPoint">The waypoint object to place where the PC should start.</param>
        /// <param name="firstConnect">Set to false if you would like the PC to go to this location every time they login instead of just every server restart.</param>
        /// <remarks>This will require storing the PC's cd key or community name (depending on how you store in your vault) and bic_filename along with routinely updating their location in some persistent method like OnRest, OnAreaEnter and OnClientExit.</remarks>
        public static void SetPersistentLocation(string cdKeyOrCommunityName, string bicFileName, uint wayPoint,
            bool firstConnect = true)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetPersistentLocation(cdKeyOrCommunityName, bicFileName, wayPoint, firstConnect ? 1 : 0);
        }

        /// <summary>
        /// Force an item name to be updated.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="item">The item object.</param>
        /// <remarks>This is a workaround for bug that occurs when updating item names in open containers.</remarks>
        public static void UpdateItemName(uint player, uint item)
        {
            global::NWN.Core.NWNX.PlayerPlugin.UpdateItemName(player, item);
        }

        /// <summary>
        /// Possesses a creature by temporarily making them a familiar.
        /// </summary>
        /// <param name="possessor">The possessor player object.</param>
        /// <param name="possessed">The possessed creature object. Only works on NPCs.</param>
        /// <param name="mindImmune">If false will remove the mind immunity effect on the possessor.</param>
        /// <param name="createDefaultQB">If true will populate the quick bar with default buttons.</param>
        /// <returns>True if possession succeeded.</returns>
        /// <remarks>This command allows a PC to possess an NPC by temporarily adding them as a familiar. It will work if the player already has an existing familiar. The creatures must be in the same area. Unpossession can be done with the regular UnpossessFamiliar commands. The possessed creature will send automap data back to the possessor. If you wish to prevent this you may wish to use GetAreaExplorationState() and SetAreaExplorationState() before and after the possession. The possessing creature will be left wherever they were when beginning the possession. You may wish to use EffectCutsceneImmobilize and EffectCutsceneGhost to hide them.</remarks>
        public static bool PossessCreature(uint possessor, uint possessed, bool mindImmune = true,
            bool createDefaultQB = false)
        {
            return global::NWN.Core.NWNX.PlayerPlugin.PossessCreature(possessor, possessed, mindImmune ? 1 : 0, createDefaultQB ? 1 : 0) != 0;
        }

        /// <summary>
        /// Returns the platform ID of the given player (NWNX_PLAYER_PLATFORM_*).
        /// </summary>
        /// <param name="oPlayer">The player object.</param>
        public static int GetPlatformId(uint oPlayer)
        {
            return global::NWN.Core.NWNX.PlayerPlugin.GetPlatformId(oPlayer);
        }

        /// <summary>
        /// Returns the game language of the given player (uses NWNX_DIALOG_LANGUAGE_*).
        /// </summary>
        /// <param name="oPlayer">The player object.</param>
        /// <remarks>This function returns the ID of the game language displayed to the player. Uses the same constants as nwnx_dialog.</remarks>
        public static int GetLanguage(uint oPlayer)
        {
            return global::NWN.Core.NWNX.PlayerPlugin.GetLanguage(oPlayer);
        }

        /// <summary>
        /// Override sOldResName with sNewResName of nResType for oPlayer.
        /// </summary>
        /// <param name="oPlayer">The player object.</param>
        /// <param name="nResType">The res type, see nwnx_util.nss for constants.</param>
        /// <param name="sOldResName">The old res name, 16 characters or less.</param>
        /// <param name="sNewResName">The new res name or "" to clear a previous override, 16 characters or less.</param>
        /// <remarks>If sNewResName does not exist on oPlayer's client it will crash their game.</remarks>
        public static void SetResManOverride(uint oPlayer, int nResType, string sOldResName, string sNewResName)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetResManOverride(oPlayer, nResType, sOldResName, sNewResName);
        }

        // @brief Toggle oPlayer's PlayerDM status.
        /// <summary>
        /// Toggle oPlayer's PlayerDM status.
        /// </summary>
        /// <param name="oPlayer">The player.</param>
        /// <param name="bIsDM">True to toggle dm mode on, false for off.</param>
        /// <remarks>This function does nothing for actual DMClient DMs or players with a client version < 8193.14.</remarks>
        public static void ToggleDM(uint oPlayer, bool bIsDM)
        {
            global::NWN.Core.NWNX.PlayerPlugin.ToggleDM(oPlayer, bIsDM ? 1 : 0);
        }


        /// <summary>
        /// Override the mouse cursor of oObject for oPlayer only.
        /// </summary>
        /// <param name="oPlayer">The player object.</param>
        /// <param name="oObject">The object.</param>
        /// <param name="nCursor">The cursor, one of MOUSECURSOR_*. -1 to clear the override.</param>
        public static void SetObjectMouseCursorOverride(uint oPlayer, uint oObject, MouseCursor nCursor)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetObjectMouseCursorOverride(oPlayer, oObject, (int)nCursor);
        }

        /// <summary>
        /// Override the hilite color of oObject for oPlayer only.
        /// </summary>
        /// <param name="oPlayer">The player object.</param>
        /// <param name="oObject">The object.</param>
        /// <param name="nColor">The color in 0xRRGGBB format, -1 to clear the override.</param>
        public static void SetObjectHiliteColorOverride(uint oPlayer, uint oObject, int nColor)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetObjectHiliteColorOverride(oPlayer, oObject, nColor);
        }

        /// <summary>
        /// Remove effects with sEffectTag from oPlayer's TURD.
        /// </summary>
        /// <param name="oPlayer">The player object.</param>
        /// <param name="sEffectTag">The effect tag.</param>
        /// <remarks>This function should be called in the NWNX_ON_CLIENT_DISCONNECT_AFTER event, OnClientLeave is too early for the TURD to exist.</remarks>
        public static void RemoveEffectFromTURD(uint oPlayer, string sEffectTag)
        {
            global::NWN.Core.NWNX.PlayerPlugin.RemoveEffectFromTURD(oPlayer, sEffectTag);
        }

        /// <summary>
        /// Set the location oPlayer will spawn when logging in to the server.
        /// </summary>
        /// <param name="oPlayer">The player object.</param>
        /// <param name="locSpawn">The location.</param>
        /// <remarks>This function is best called in the NWNX_ON_ELC_VALIDATE_CHARACTER_BEFORE event, OnClientEnter will be too late.</remarks>
        public static void SetSpawnLocation(uint oPlayer, Location locSpawn)
        {
            var vPosition = GetPositionFromLocation(locSpawn);
            var locationPtr = Location(GetAreaFromLocation(locSpawn), vPosition, GetFacingFromLocation(locSpawn));
            global::NWN.Core.NWNX.PlayerPlugin.SetSpawnLocation(oPlayer, locationPtr);
        }

        /// <summary>
        /// Set nCustomTokenNumber to sTokenValue for oPlayer only.
        /// </summary>
        /// <param name="oPlayer">The player object.</param>
        /// <param name="nCustomTokenNumber">The token number.</param>
        /// <param name="sTokenValue">The token text.</param>
        /// <remarks>The basegame SetCustomToken() will override any personal tokens.</remarks>
        public static void SetCustomToken(uint oPlayer, int nCustomTokenNumber, string sTokenValue)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetCustomToken(oPlayer, nCustomTokenNumber, sTokenValue);
        }

        /// <summary>
        /// Override the name of creature for player only.
        /// </summary>
        /// <param name="oPlayer">The player object.</param>
        /// <param name="oCreature">The creature object.</param>
        /// <param name="sName">The name for the creature for this player, "" to clear the override.</param>
        public static void SetCreatureNameOverride(uint oPlayer, uint oCreature, string sName)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetCreatureNameOverride(oPlayer, oCreature, sName);
        }

        /// <summary>
        /// Display floaty text above oCreature for oPlayer only.
        /// </summary>
        /// <param name="oPlayer">The player to display the text to.</param>
        /// <param name="oCreature">The creature to display the text above.</param>
        /// <param name="sText">The text to display.</param>
        /// <param name="bChatWindow">If true, sText will be displayed in oPlayer's chat window.</param>
        /// <remarks>This will also display the floaty text above creatures that are not part of oPlayer's faction.</remarks>
        public static void FloatingTextStringOnCreature(uint oPlayer, uint oCreature, string sText, bool bChatWindow = true)
        {
            global::NWN.Core.NWNX.PlayerPlugin.FloatingTextStringOnCreature(oPlayer, oCreature, sText, bChatWindow ? 1 : 0);
        }
        /// <summary>
        /// Give a custom journal entry to oPlayer.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="journalEntry">The journal entry in the form of a struct.</param>
        /// <param name="isSilentUpdate">0 = Notify player via sound effects and feedback message, 1 = Suppress sound effects and feedback message.</param>
        /// <returns>A positive number to indicate the new amount of journal entries on the player.</returns>
        /// <remarks>Custom entries are wiped on client enter - they must be reapplied. In contrast to conventional nwn journal entries - this method will overwrite entries with the same tag, so the index / count of entries will only increase if you add new entries with unique tags.</remarks>
        public static int AddCustomJournalEntry(uint player, JournalEntry journalEntry, bool isSilentUpdate = false)
        {
            var coreEntry = new global::NWN.Core.NWNX.JournalEntry
            {
                sName = journalEntry.Name,
                sText = journalEntry.Text,
                sTag = journalEntry.Tag,
                nState = journalEntry.State,
                nPriority = journalEntry.Priority,
                nQuestCompleted = journalEntry.IsQuestCompleted ? 1 : 0,
                nQuestDisplayed = journalEntry.IsQuestDisplayed ? 1 : 0,
                nUpdated = journalEntry.Updated,
                nCalendarDay = journalEntry.CalendarDay,
                nTimeOfDay = journalEntry.TimeOfDay
            };
            return global::NWN.Core.NWNX.PlayerPlugin.AddCustomJournalEntry(player, coreEntry, isSilentUpdate ? 1 : 0);
        }


        /// <summary>
        /// Returns a struct containing a journal entry that can then be modified.
        /// </summary>
        /// <param name="player">The player object.</param>
        /// <param name="questTag">The quest tag you wish to get the journal entry for.</param>
        /// <returns>A struct containing the journal entry data.</returns>
        /// <remarks>This method will return -1 for the Updated field in the event that no matching journal entry was found, only the last matching quest tag will be returned. Eg: If you add 3 journal updates to a player, only the 3rd one will be returned as that is the active one that the player currently sees.</remarks>
        public static JournalEntry GetJournalEntry(uint player, string questTag)
        {
            var coreResult = global::NWN.Core.NWNX.PlayerPlugin.GetJournalEntry(player, questTag);
            return new JournalEntry
            {
                Name = coreResult.sName,
                Text = coreResult.sText,
                Tag = coreResult.sTag,
                State = coreResult.nState,
                Priority = coreResult.nPriority,
                IsQuestCompleted = coreResult.nQuestCompleted != 0,
                IsQuestDisplayed = coreResult.nQuestDisplayed != 0,
                Updated = coreResult.nUpdated,
                CalendarDay = coreResult.nCalendarDay,
                TimeOfDay = coreResult.nTimeOfDay
            };
        }

        /// <summary>
        /// Closes any store oPlayer may have open.
        /// </summary>
        /// <param name="oPlayer">The player object.</param>
        public static void CloseStore(uint oPlayer)
        {
            global::NWN.Core.NWNX.PlayerPlugin.CloseStore(oPlayer);
        }

        /// <summary>
        /// Override nStrRef from the TlkTable with sOverride for oPlayer only.
        /// </summary>
        /// <param name="oPlayer">The player.</param>
        /// <param name="nStrRef">The StrRef.</param>
        /// <param name="sOverride">The new value for nStrRef or "" to remove the override.</param>
        /// <param name="bRestoreGlobal">If true, when removing a personal override it will attempt to restore the global override if it exists.</param>
        /// <remarks>Overrides will not persist through relogging.</remarks>
        public static void SetTlkOverride(uint oPlayer, int nStrRef, string sOverride, bool bRestoreGlobal = true)
        {
            global::NWN.Core.NWNX.PlayerPlugin.SetTlkOverride(oPlayer, nStrRef, sOverride, bRestoreGlobal ? 1 : 0);
        }

        /// <summary>
        /// Get the current open store of player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>The open store or OBJECT_INVALID if no store is open.</returns>
        public static uint GetOpenStore(uint player)
        {
            return global::NWN.Core.NWNX.PlayerPlugin.GetOpenStore(player);
        }
    }
}