using System;
using System.Reflection;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN.Contracts;
using SWLOR.Game.Server.NWNX.Contracts;

namespace SWLOR.Game.Server.NWNX
{
    public class NWNXPlayer : NWNXBase, INWNXPlayer
    {
        public NWNXPlayer(INWScript script)
            : base(script)
        {
        }


        private const string NWNX_Player = "NWNX_Player";

        // Force display placeable examine window for player
        public void ForcePlaceableExamineWindow(NWPlayer player, NWPlaceable placeable)
        {
            string sFunc = "ForcePlaceableExamineWindow";
            NWNX_PushArgumentObject(NWNX_Player, sFunc, placeable.Object);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player.Object);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        public void StopGuiTimingBar(NWPlayer player, string script, int id)
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

        // Starts displaying a timing bar.
        // Will run a script at the end of the timing bar, if specified.
        public void StartGuiTimingBar(NWPlayer player, float seconds, string script)
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

            player.DelayCommand(() =>
            {
                StopGuiTimingBar(player, script, -1);
            }, seconds);
        }

        // Stops displaying a timing bar.
        // Runs a script if specified.
        public void StopGuiTimingBar(NWPlayer player, string script)
        {
            StopGuiTimingBar(player, script, -1);
        }

        // Sets whether the player should always walk when given movement commands.
        // If true, clicking on the ground or using WASD will trigger walking instead of running.
        public void SetAlwaysWalk(NWPlayer player, int bWalk)
        {
            string sFunc = "SetAlwaysWalk";
            NWNX_PushArgumentInt(NWNX_Player, sFunc, bWalk);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player.Object);

            NWNX_CallFunction(NWNX_Player, sFunc);
        }

        // Gets the player's quickbar slot info
        public QuickBarSlot GetQuickBarSlot(NWPlayer player, int slot)
        {
            string sFunc = "GetQuickBarSlot";
            QuickBarSlot qbs = new QuickBarSlot();

            NWNX_PushArgumentInt(NWNX_Player, sFunc, slot);
            NWNX_PushArgumentObject(NWNX_Player, sFunc, player.Object);
            NWNX_CallFunction(NWNX_Player, sFunc);

            qbs.Associate = NWObject.Wrap(NWNX_GetReturnValueObject(NWNX_Player, sFunc));
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
            qbs.SecondaryItem = NWItem.Wrap(NWNX_GetReturnValueObject(NWNX_Player, sFunc));
            qbs.Item = NWItem.Wrap(NWNX_GetReturnValueObject(NWNX_Player, sFunc));

            return qbs;
        }

        // Sets a player's quickbar slot
        public void SetQuickBarSlot(NWPlayer player, int slot, QuickBarSlot qbs)
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

    }
}
