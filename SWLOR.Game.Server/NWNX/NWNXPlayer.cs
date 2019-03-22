using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using static SWLOR.Game.Server.NWNX.NWNXCore;
using System;
using System.Reflection;
using static NWN._;

namespace SWLOR.Game.Server.NWNX
{
    public static class NWNXPlayer
    {
        private const string NWNX_Player = "NWNX_Player";

        /// <summary>
        /// // Force opens the target object's inventory for the player.
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
        public static void ForcePlaceableExamineWindow(NWPlayer player, NWPlaceable placeable)
        {
            string sFunc = "ForcePlaceableExamineWindow";
            NWNX_PushArgumentObject(NWNX_Player, sFunc, placeable.Object);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player.Object);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Starts displaying a timing bar.
        /// Will run a script at the end of the timing bar, if specified.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="seconds"></param>
        /// <param name="script"></param>
        public static void StartGuiTimingBar(NWPlayer player, float seconds, string script)
        {
            // only one timing bar at a time!
            if (_.GetLocalInt(player.Object, "NWNX_PLAYER_GUI_TIMING_ACTIVE") == 1)
                return;

            string sFunc = "StartGuiTimingBar";
            NWNX_PushArgumentFloat(NWNX_Player, sFunc, seconds);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player.Object);

            NWNX_CallFunction(NWNX_Player, sFunc);

            int id = _.GetLocalInt(player.Object, "NWNX_PLAYER_GUI_TIMING_ID") + 1;
            _.SetLocalInt(player.Object, "NWNX_PLAYER_GUI_TIMING_ACTIVE", id);
            _.SetLocalInt(player.Object, "NWNX_PLAYER_GUI_TIMING_ID", id);

            _.DelayCommand(seconds, () =>
            {
                StopGuiTimingBar(player, script, -1);
            });
        }

        /// <summary>
        /// Stops displaying a timing bar.
        /// Runs a script if specified.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="script"></param>
        /// <param name="id"></param>
        public static void StopGuiTimingBar(NWPlayer player, string script, int id)
        {
            int activeId = _.GetLocalInt(player.Object, "NWNX_PLAYER_GUI_TIMING_ACTIVE");
            // Either the timing event was never started, or it already finished.
            if (activeId == 0)
                return;

            // If id != -1, we ended up here through DelayCommand. Make sure it's for the right ID
            if (id != -1 && id != activeId)
                return;

            _.DeleteLocalInt(player.Object, "NWNX_PLAYER_GUI_TIMING_ACTIVE");

            string sFunc = "StopGuiTimingBar";
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player.Object);
            NWNX_CallFunction(NWNX_Player, sFunc);

            if (!string.IsNullOrWhiteSpace(script))
            {
                // "." is an invalid character in NWN script files, but valid for the C# classes.
                // Assume this is intended to be a C# call.
                if (script.Contains("."))
                {
                    Type type = Assembly.GetExecutingAssembly().GetType(script);
                    App.RunEvent(type);
                }
                // Everything else is assumed to be an NWN script.
                else
                {
                    _.ExecuteScript(script, player.Object);
                }
            }
        }


        /// <summary>
        /// Stops displaying a timing bar.
        /// Runs a script if specified.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="script"></param>
        public static void StopGuiTimingBar(NWPlayer player, string script)
        {
            StopGuiTimingBar(player, script, -1);
        }

        /// <summary>
        /// Sets whether the player should always walk when given movement commands.
        /// If true, clicking on the ground or using WASD will trigger walking instead of running.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="bWalk"></param>
        public static void SetAlwaysWalk(NWPlayer player, int bWalk)
        {
            string sFunc = "SetAlwaysWalk";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, bWalk);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player.Object);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Gets the player's quickbar slot info
        /// </summary>
        /// <param name="player"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        public static QuickBarSlot GetQuickBarSlot(NWPlayer player, int slot)
        {
            string sFunc = "GetQuickBarSlot";
            QuickBarSlot qbs = new QuickBarSlot();

            NWNX_PushArgumentInt(NWNX_Player, sFunc, slot);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player.Object);
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
        public static void SetQuickBarSlot(NWPlayer player, int slot, QuickBarSlot qbs)
        {
            string sFunc = "SetQuickBarSlot";

            NWNX_PushArgumentObject(NWNX_Player, sFunc, qbs.Item.Object);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, qbs.SecondaryItem.Object);
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
            NWNX_PushArgumentObject(NWNX_Player, sFunc, qbs.Associate.Object);

            NWNX_PushArgumentInt(NWNX_Player, sFunc, slot);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player.Object);
            NWNX_CallFunction(NWNX_Player, sFunc);
        }


        /// <summary>
        /// Get the name of the .bic file associated with the player's character.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static string GetBicFileName(NWPlayer player)
        {
            string sFunc = "GetBicFileName";
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player.Object);
            NWNX_CallFunction(NWNX_Player, sFunc);
            return NWNX_GetReturnValueString(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Plays the VFX at the target position in current area for the given player only
        /// </summary>
        /// <param name="player"></param>
        /// <param name="effectId"></param>
        /// <param name="position"></param>
        public static void ShowVisualEffect(NWPlayer player, int effectId, Vector position)
        {
            string sFunc = "ShowVisualEffect";
            NWNX_PushArgumentFloat(NWNX_Player, sFunc, position.m_X);
            NWNX_PushArgumentFloat(NWNX_Player, sFunc, position.m_Y);
            NWNX_PushArgumentFloat(NWNX_Player, sFunc, position.m_Z);
            NWNX_PushArgumentInt(NWNX_Player, sFunc, effectId);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Changes the daytime music track for the given player only
        /// </summary>
        /// <param name="player"></param>
        /// <param name="track"></param>
        public static void MusicBackgroundChangeDay(NWPlayer player, int track)
        {
            string sFunc = "ChangeBackgroundMusic";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, track);
            NWNX_PushArgumentInt(NWNX_Player, sFunc, TRUE); // bool day = TRUE
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Changes the nighttime music track for the given player only
        /// </summary>
        /// <param name="player"></param>
        /// <param name="track"></param>
        public static void MusicBackgroundChangeNight(NWPlayer player, int track)
        {
            string sFunc = "ChangeBackgroundMusic";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, track);
            NWNX_PushArgumentInt(NWNX_Player, sFunc, FALSE); // bool day = FALSE
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Starts the background music for the given player only
        /// </summary>
        /// <param name="player"></param>
        public static void MusicBackgroundStart(NWPlayer player)
        {
            string sFunc = "PlayBackgroundMusic";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, TRUE); // bool play = TRUE
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Stops the background music for the given player only
        /// </summary>
        /// <param name="player"></param>
        public static void MusicBackgroundStop(NWPlayer player)
        {
            string sFunc = "PlayBackgroundMusic";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, FALSE); // bool play = FALSE
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Changes the battle music track for the given player only
        /// </summary>
        /// <param name="player"></param>
        /// <param name="track"></param>
        public static void MusicBattleChange(NWPlayer player, int track)
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
        public static void MusicBattleStart(NWPlayer player)
        {
            string sFunc = "PlayBattleMusic";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, TRUE); // bool play = TRUE
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        /// <summary>
        /// Stops the background music for the given player only
        /// </summary>
        /// <param name="player"></param>
        public static void MusicBattleStop(NWPlayer player)
        {
            string sFunc = "PlayBattleMusic";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, FALSE); // bool play = FALSE
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
        public static void PlaySound(NWPlayer player, string sound, NWObject target)
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
        public static void SetPlaceableUseable(NWPlayer player, NWPlaceable placeable, bool isUseable)
        {
            string sFunc = "SetPlaceableUsable";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, isUseable ? TRUE : FALSE);
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
        public static void SetRestDuration(NWPlayer player, int duration)
        {
            string sFunc = "SetRestDuration";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, duration);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }
    }
}
