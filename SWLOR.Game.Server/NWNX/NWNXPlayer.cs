using NWN;
using SWLOR.Game.Server.NWScript;
using static SWLOR.Game.Server.NWScript._;
using static SWLOR.Game.Server.NWNX.NWNXCore;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXPlayer
    {
        private const string NWNX_Player = "NWNX_Player";

        /// <summary> 
        /// Force display placeable examine window for player
        /// If used on a placeable in a different area than the player, the portait will not be shown.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="placeable"></param>
        public static void ForcePlaceableExamineWindow(NWGameObject player, NWGameObject placeable)
        {
            string sFunc = "ForcePlaceableExamineWindow";
            NWNX_PushArgumentObject(NWNX_Player, sFunc, placeable);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Force opens the target object's inventory for the player.
        /// A few notes about this function:
        /// - If the placeable is in a different area than the player, the portrait will not be shown
        /// - The placeable's open/close animations will be played
        /// - Clicking the 'close' button will cause the player to walk to the placeable;
        ///     If the placeable is in a different area, the player will just walk to the edge
        ///     of the current area and stop. This action can be cancelled manually.
        /// - Walking will close the placeable automatically.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="placeable"></param>
        public static void ForcePlaceableInventoryWindow(NWGameObject player, NWGameObject placeable)
        {
            string sFunc = "ForcePlaceableInventoryWindow";
            NWNX_PushArgumentObject(NWNX_Player, sFunc, placeable);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Starts displaying a timing bar.
        /// Will run a script at the end of the timing bar, if specified.
        /// </summary>
        /// <param name="player">The creature who will see the timing bar.</param>
        /// <param name="seconds">How long the timing bar should come on screen.</param>
        /// <param name="script">The script to run at the end of the timing bar.</param>
        /// <param name="type">The type of timing bar to display.</param>
        public static void StartGuiTimingBar(NWGameObject player, float seconds, string script = "", TimingBarType type = TimingBarType.Custom)
        {
            if (GetLocalInt(player, "NWNX_PLAYER_GUI_TIMING_ACTIVE") == 1)
                return;

            string sFunc = "StartGuiTimingBar";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, (int)type);
            NWNX_PushArgumentFloat(NWNX_Player, sFunc, seconds);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);

            int id = GetLocalInt(player, "NWNX_PLAYER_GUI_TIMING_ID") + 1;
            SetLocalInt(player, "NWNX_PLAYER_GUI_TIMING_ACTIVE", id);
            SetLocalInt(player, "NWNX_PLAYER_GUI_TIMING_ID", id);

            DelayCommand(seconds, () => StopGuiTimingBar(player, script, id));
        }


        /// <summary>
        /// Stops displaying a timing bar.
        /// Runs a script if specified.
        /// </summary>
        /// <param name="creature">The creature's timing bar to stop.</param>
        /// <param name="script">The script to run once ended.</param>
        /// <param name="id">ID number of this timing bar.</param>
        public static void StopGuiTimingBar(NWGameObject creature, string script, int id)
        {
            int activeId = GetLocalInt(creature, "NWNX_PLAYER_GUI_TIMING_ACTIVE");
            // Either the timing event was never started, or it already finished.
            if (activeId == 0)
                return;

            // If id != -1, we ended up here through DelayCommand. Make sure it's for the right ID
            if (id != -1 && id != activeId)
                return;

            DeleteLocalInt(creature, "NWNX_PLAYER_GUI_TIMING_ACTIVE");

            string sFunc = "StopGuiTimingBar";
            NWNX_PushArgumentObject(NWNX_Player, sFunc, creature);
            NWNX_CallFunction(NWNX_Player, sFunc);

            if (!string.IsNullOrWhiteSpace(script))
            {
                ExecuteScript(script, creature);
            }
        }


        /// <summary>
        /// Stops displaying a timing bar.
        /// Runs a script if specified.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="script"></param>
        public static void StopGuiTimingBar(NWGameObject player, string script)
        {
            StopGuiTimingBar(player, script, -1);
        }

        /// <summary>
        /// Sets whether the player should always walk when given movement commands.
        /// If true, clicking on the ground or using WASD will trigger walking instead of running.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="bWalk"></param>
        public static void SetAlwaysWalk(NWGameObject player, int bWalk)
        {
            string sFunc = "SetAlwaysWalk";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, bWalk);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Gets the player's quickbar slot info
        /// </summary>
        /// <param name="player"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        public static QuickBarSlot GetQuickBarSlot(NWGameObject player, int slot)
        {
            string sFunc = "GetQuickBarSlot";
            QuickBarSlot qbs = new QuickBarSlot();

            NWNX_PushArgumentInt(NWNX_Player, sFunc, slot);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);
            NWNX_CallFunction(NWNX_Player, sFunc);

            qbs.Associate = (NWNX_GetReturnValueObject(NWNX_Player, sFunc));
            qbs.AssociateType = NWNX_GetReturnValueInt(NWNX_Player, sFunc);
            qbs.DomainLevel = NWNX_GetReturnValueInt(NWNX_Player, sFunc);
            qbs.MetaType = NWNX_GetReturnValueInt(NWNX_Player, sFunc);
            qbs.INTParam1 = NWNX_GetReturnValueInt(NWNX_Player, sFunc);
            qbs.ToolTip = NWNX_GetReturnValueString(NWNX_Player, sFunc);
            qbs.CommandLine = NWNX_GetReturnValueString(NWNX_Player, sFunc);
            qbs.CommandLabel = NWNX_GetReturnValueString(NWNX_Player, sFunc);
            qbs.Resref = NWNX_GetReturnValueString(NWNX_Player, sFunc);
            qbs.MultiClass = NWNX_GetReturnValueInt(NWNX_Player, sFunc);
            qbs.ObjectType = (QuickBarSlotType)NWNX_GetReturnValueInt(NWNX_Player, sFunc);
            qbs.SecondaryItem = (NWNX_GetReturnValueObject(NWNX_Player, sFunc));
            qbs.Item = (NWNX_GetReturnValueObject(NWNX_Player, sFunc));

            return qbs;
        }

        /// <summary>
        /// Sets a player's quickbar slot
        /// </summary>
        /// <param name="player"></param>
        /// <param name="slot"></param>
        /// <param name="qbs"></param>
        public static void SetQuickBarSlot(NWGameObject player, int slot, QuickBarSlot qbs)
        {
            string sFunc = "SetQuickBarSlot";

            NWNX_PushArgumentObject(NWNX_Player, sFunc, qbs.Item);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, qbs.SecondaryItem);
            NWNX_PushArgumentInt(NWNX_Player, sFunc, (int)qbs.ObjectType);
            NWNX_PushArgumentInt(NWNX_Player, sFunc, qbs.MultiClass);
            NWNX_PushArgumentString(NWNX_Player, sFunc, qbs.Resref);
            NWNX_PushArgumentString(NWNX_Player, sFunc, qbs.CommandLabel);
            NWNX_PushArgumentString(NWNX_Player, sFunc, qbs.CommandLine);
            NWNX_PushArgumentString(NWNX_Player, sFunc, qbs.ToolTip);
            NWNX_PushArgumentInt(NWNX_Player, sFunc, qbs.INTParam1);
            NWNX_PushArgumentInt(NWNX_Player, sFunc, qbs.MetaType);
            NWNX_PushArgumentInt(NWNX_Player, sFunc, qbs.DomainLevel);
            NWNX_PushArgumentInt(NWNX_Player, sFunc, qbs.AssociateType);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, qbs.Associate);

            NWNX_PushArgumentInt(NWNX_Player, sFunc, slot);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);
            NWNX_CallFunction(NWNX_Player, sFunc);
        }


        /// <summary>
        /// Get the name of the .bic file associated with the player's character.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static string GetBicFileName(NWGameObject player)
        {
            string sFunc = "GetBicFileName";
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);
            NWNX_CallFunction(NWNX_Player, sFunc);
            return NWNX_GetReturnValueString(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Plays the VFX at the target position in current area for the given player only
        /// </summary>
        /// <param name="player"></param>
        /// <param name="effectId"></param>
        /// <param name="position"></param>
        public static void ShowVisualEffect(NWGameObject player, int effectId, Vector position)
        {
            string sFunc = "ShowVisualEffect";
            NWNX_PushArgumentFloat(NWNX_Player, sFunc, position.X);
            NWNX_PushArgumentFloat(NWNX_Player, sFunc, position.Y);
            NWNX_PushArgumentFloat(NWNX_Player, sFunc, position.Z);
            NWNX_PushArgumentInt(NWNX_Player, sFunc, effectId);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Changes the daytime music track for the given player only
        /// </summary>
        /// <param name="player"></param>
        /// <param name="track"></param>
        public static void MusicBackgroundChangeDay(NWGameObject player, int track)
        {
            string sFunc = "ChangeBackgroundMusic";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, track);
            NWNX_PushArgumentInt(NWNX_Player, sFunc, 1); // bool day = true
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Changes the nighttime music track for the given player only
        /// </summary>
        /// <param name="player"></param>
        /// <param name="track"></param>
        public static void MusicBackgroundChangeNight(NWGameObject player, int track)
        {
            string sFunc = "ChangeBackgroundMusic";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, track);
            NWNX_PushArgumentInt(NWNX_Player, sFunc, 0); // bool day = false
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Starts the background music for the given player only
        /// </summary>
        /// <param name="player"></param>
        public static void MusicBackgroundStart(NWGameObject player)
        {
            string sFunc = "PlayBackgroundMusic";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, 1); // bool play = true
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Stops the background music for the given player only
        /// </summary>
        /// <param name="player"></param>
        public static void MusicBackgroundStop(NWGameObject player)
        {
            string sFunc = "PlayBackgroundMusic";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, 0); // bool play = false
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Changes the battle music track for the given player only
        /// </summary>
        /// <param name="player"></param>
        /// <param name="track"></param>
        public static void MusicBattleChange(NWGameObject player, int track)
        {
            string sFunc = "ChangeBattleMusic";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, track);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Starts the battle music for the given player only
        /// </summary>
        /// <param name="player"></param>
        public static void MusicBattleStart(NWGameObject player)
        {
            string sFunc = "PlayBattleMusic";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, 1); // bool play = true
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Stops the background music for the given player only
        /// </summary>
        /// <param name="player"></param>
        public static void MusicBattleStop(NWGameObject player)
        {
            string sFunc = "PlayBattleMusic";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, 0); // bool play = false
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Play a sound at the location of target for the given player only
        /// If target is OBJECT_INVALID the sound will play at the location of the player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="sound"></param>
        /// <param name="target"></param>
        public static void PlaySound(NWGameObject player, string sound, NWGameObject target)
        {
            string sFunc = "PlaySound";
            NWNX_PushArgumentObject(NWNX_Player, sFunc, target);
            NWNX_PushArgumentString(NWNX_Player, sFunc, sound);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Toggle a placeable's usable flag for the given player only
        /// </summary>
        /// <param name="player"></param>
        /// <param name="placeable"></param>
        /// <param name="isUseable"></param>
        public static void SetPlaceableUseable(NWGameObject player, NWGameObject placeable, bool isUseable)
        {
            string sFunc = "SetPlaceableUsable";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, isUseable ? 1 : 0);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, placeable);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Override player's rest duration
        /// Duration is in milliseconds, 1000 = 1 second
        /// Minimum duration of 10ms
        /// -1 clears the override
        /// </summary>
        /// <param name="player"></param>
        /// <param name="duration"></param>
        public static void SetRestDuration(NWGameObject player, int duration)
        {
            string sFunc = "SetRestDuration";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, duration);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }


        /// <summary>
        /// Apply visualeffect to target that only player can see
        /// Note: Only works with instant effects: VFX_COM_*, VFX_FNF_*, VFX_IMP_*
        /// </summary>
        /// <param name="player"></param>
        /// <param name="target"></param>
        /// <param name="visualeffect"></param>
        public static void ApplyInstantVisualEffectToObject(NWGameObject player, NWGameObject target, int visualeffect)
        {
            string sFunc = "ApplyInstantVisualEffectToObject";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, visualeffect);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, target);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// // Refreshes the players character sheet
        /// Note: You may need to use DelayCommand if you're manipulating values
        /// through nwnx and forcing a UI refresh, 0.5s seemed to be fine
        /// </summary>
        /// <param name="player"></param>
        public static void UpdateCharacterSheet(NWGameObject player)
        {
            string sFunc = "UpdateCharacterSheet";
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Allows player to open target's inventory
        /// Target must be a creature or another player
        /// Note: only works if player and target are in the same area
        /// </summary>
        /// <param name="player"></param>
        /// <param name="target"></param>
        /// <param name="open"></param>
        public static void OpenInventory(NWGameObject player, NWGameObject target, bool open = true)
        {
            string sFunc = "OpenInventory";

            NWNX_PushArgumentInt(NWNX_Player, sFunc, open ? 1 : 0);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, target);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Get player's area exploration state
        /// </summary>
        /// <param name="player">The player object</param>
        /// <param name="area">The area</param>
        /// <returns></returns>
        public static string GetAreaExplorationState(NWGameObject player, NWGameObject area)
        {
            string sFunc = "GetAreaExplorationState";
            NWNX_PushArgumentObject(NWNX_Player, sFunc, area);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
            return NWNX_GetReturnValueString(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Set player's area exploration state (str is an encoded string obtained with NWNX_Player_GetAreaExplorationState)
        /// </summary>
        /// <param name="player">The player object</param>
        /// <param name="area">The area</param>
        /// <param name="str">The encoded exploration state string</param>
        public static void SetAreaExplorationState(NWGameObject player, NWGameObject area, string str)
        {
            string sFunc = "SetAreaExplorationState";
            NWNX_PushArgumentString(NWNX_Player, sFunc, str);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, area);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Override oPlayer's rest animation to nAnimation
        ///
        /// NOTE: nAnimation does not take ANIMATION_LOOPING_* or ANIMATION_FIREFORGET_* constants
        ///       Use NWNX_Consts_TranslateNWScriptAnimation() in nwnx_consts.nss to get their NWNX equivalent
        ///       -1 to clear the override
        /// </summary>
        /// <param name="oPlayer">The player object</param>
        /// <param name="nAnimation">The rest animation</param>
        public static void SetRestAnimation(NWGameObject oPlayer, int nAnimation)
        {
            string sFunc = "SetRestAnimation";

            NWNX_PushArgumentInt(NWNX_Player, sFunc, nAnimation);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, oPlayer);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Override a visual transform on the given object that only oPlayer will see.
        /// - oObject can be any valid Creature, Placeable, Item or Door.
        /// - nTransform is one of OBJECT_VISUAL_TRANSFORM_* or -1 to remove the override
        /// - fValue depends on the transformation to apply.
        /// </summary>
        /// <param name="oPlayer">The player object</param>
        /// <param name="oObject">The object to transform</param>
        /// <param name="nTransform">The transformation type</param>
        /// <param name="fValue">The amount to transform by</param>
        public static void SetObjectVisualTransformOverride(NWGameObject oPlayer, NWGameObject oObject, int nTransform, float fValue)
        {
            string sFunc = "SetObjectVisualTransformOverride";

            NWNX_PushArgumentFloat(NWNX_Player, sFunc, fValue);
            NWNX_PushArgumentInt(NWNX_Player, sFunc, nTransform);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, oObject);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, oPlayer);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Apply a looping visualeffect to target that only player can see
        /// visualeffect: VFX_DUR_*, call again to remove an applied effect
        ///               -1 to remove all effects
        ///
        /// Note: Only really works with looping effects: VFX_DUR_*
        ///       Other types *kind* of work, they'll play when reentering the area and the object is in view
        ///       or when they come back in view range.
        /// </summary>
        /// <param name="player">The player object</param>
        /// <param name="target">The target to apply the visual effect to</param>
        /// <param name="visualeffect">The visual effect to use</param>
        public static void ApplyLoopingVisualEffectToObject(NWGameObject player, NWGameObject target, int visualeffect)
        {
            string sFunc = "ApplyLoopingVisualEffectToObject";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, visualeffect);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, target);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Override the name of placeable for player only
        /// "" to clear the override
        /// </summary>
        /// <param name="player">The player object</param>
        /// <param name="placeable">The placeable object</param>
        /// <param name="name">The new name to use</param>
        public static void SetPlaceableNameOverride(NWGameObject player, NWGameObject placeable, string name)
        {
            string sFunc = "SetPlaceableNameOverride";

            NWNX_PushArgumentString(NWNX_Player, sFunc, name);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, placeable);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Gets whether a quest has been completed by a player
        /// Returns -1 if they don't have the journal entry
        /// </summary>
        /// <param name="player">The player object</param>
        /// <param name="sQuestName">The name of the quest</param>
        /// <returns></returns>
        public static int GetQuestCompleted(NWGameObject player, string sQuestName)
        {
            string sFunc = "GetQuestCompleted";
            NWNX_PushArgumentString(NWNX_Player, sFunc, sQuestName);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Player, sFunc);
        }

        /// <summary>
        /// This will require storing the PC's cd key or community name (depending on how you store in your vault)
        /// and bic_filename along with routinely updating their location in some persistent method like OnRest,
        /// OnAreaEnter and OnClentExit.
        ///
        /// Place waypoints on module load representing where a PC should start
        /// </summary>
        /// <param name="sCDKeyOrCommunityName">The Public CD Key or Community Name of the player, this will depend on your vault type.</param>
        /// <param name="sBicFileName">The filename for the character. Retrieved with NWNX_Player_GetBicFileName().</param>
        /// <param name="oWP">The waypoint object to place where the PC should start.</param>
        /// <param name="bFirstConnectOnly">Set to false if you would like the PC to go to this location every time they login instead of just every server restart.</param>
        public static void SetPersistentLocation(string sCDKeyOrCommunityName, string sBicFileName, NWGameObject oWP, bool bFirstConnectOnly = true)
        {
            string sFunc = "SetPersistentLocation";

            NWNX_PushArgumentInt(NWNX_Player, sFunc, bFirstConnectOnly ? 1 : 0);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, oWP);
            NWNX_PushArgumentString(NWNX_Player, sFunc, sBicFileName);
            NWNX_PushArgumentString(NWNX_Player, sFunc, sCDKeyOrCommunityName);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Force an item name to be updated.
        /// This is a workaround for bug that occurs when updating item names in open containers.
        /// </summary>
        /// <param name="oPlayer">The player</param>
        /// <param name="oItem">The item</param>
        public static void UpdateItemName(NWGameObject oPlayer, NWGameObject oItem)
        {
            string sFunc = "UpdateItemName";

            NWNX_PushArgumentObject(NWNX_Player, sFunc, oItem);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, oPlayer);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Possesses a creature by temporarily making them a familiar
        /// This command allows a PC to possess an NPC by temporarily adding them as a familiar. It will work
        /// if the player already has an existing familiar. The creatures must be in the same area. Unpossession can be
        /// done with the regular @nwn{UnpossessFamiliar} commands.
        /// The possessed creature will send automap data back to the possessor.
        /// If you wish to prevent this you may wish to use NWNX_Player_GetAreaExplorationState() and
        /// NWNX_Player_SetAreaExplorationState() before and after the possession.
        /// The possessing creature will be left wherever they were when beginning the possession. You may wish
        /// to use @nwn{EffectCutsceneImmobilize} and @nwn{EffectCutsceneGhost} to hide them.
        /// </summary>
        /// <param name="oPossessor">The possessor player object</param>
        /// <param name="oPossessed">The possessed creature object. Only works on NPCs.</param>
        /// <param name="bMindImmune">If false will remove the mind immunity effect on the possessor.</param>
        /// <param name="bCreateDefaultQB">If true will populate the quick bar with default buttons.</param>
        /// <returns>true if possession succeeded.</returns>
        public static bool PossessCreature(NWGameObject oPossessor, NWGameObject oPossessed, bool bMindImmune = true, bool bCreateDefaultQB = false)
        {
            string sFunc = "PossessCreature";

            NWNX_PushArgumentInt(NWNX_Player, sFunc, bCreateDefaultQB ? 1 : 0);
            NWNX_PushArgumentInt(NWNX_Player, sFunc, bMindImmune ? 1 : 0);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, oPossessed);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, oPossessor);

            NWNX_CallFunction(NWNX_Player, sFunc);
            return NWNX_GetReturnValueInt(NWNX_Player, sFunc) == 1;
        }
    }
}
