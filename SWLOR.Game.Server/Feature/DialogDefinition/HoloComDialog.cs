﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class HoloComDialog: DialogBase
    {
        private const string MainPageId = "MAIN_PAGE";
        
        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            page.Header = ColorToken.Green("HoloCom Menu\n\n");

            var player  = GetPC();

            if (HoloCom.IsInCall(player))
            {
                var activeCallTarget = HoloCom.GetTargetForActiveCall(player);
                page.AddResponse($"End current call with {GetName(activeCallTarget)}", () =>
                {
                    HoloCom.SetIsInCall(player, activeCallTarget, false);
                    EndConversation();
                });
            }

            if (HoloCom.IsCallReceiver(player) && !HoloCom.IsInCall(player))
            {
                var callSender = HoloCom.GetCallSender(player);
                var callerName = GetName(callSender);
                page.AddResponse($"Answer incoming call from {callerName}", () =>
                {
                    HoloCom.SetIsInCall(player, callSender, true);
                    EndConversation();
                });
                page.AddResponse($"Decline incoming call from {callerName}", () =>
                {
                    HoloCom.SetIsInCall(player, callSender, false);
                    EndConversation();
                });
            }

            if (HoloCom.IsCallReceiver(player) || HoloCom.IsInCall(player) || HoloCom.IsCallSender(player)) return;

            for (var pc = GetFirstPC(); GetIsObjectValid(pc); pc = GetNextPC())
            {
                if (GetIsDM(pc) || pc == player) continue;

                var message = $"Call {GetName(pc)}";
                if (HoloCom.IsInCall(pc))
                {
                    message += ColorToken.Red(" (LINE BUSY)");
                }

                var receiver = pc;
                page.AddResponse(message, () =>
                {
                    if (!HoloCom.IsInCall(receiver))
                    {
                        HoloCom.SetIsCallSender(player);
                        DelayCommand(1.0f, () =>
                        {
                            CallPlayer(player, receiver);
                        });
                    }
                });
            }
        }

        private void CallPlayer(uint sender, uint receiver)
        {
            if (HoloCom.IsInCall(sender) || HoloCom.IsInCall(receiver)) return;

            if (!HoloCom.IsCallSender(sender)) return;

            var receiverName = GetName(receiver);
            SendMessageToPC(sender, "You wait for " + receiverName + " to answer their HoloCom.");

            HoloCom.SetIsCallSender(sender);
            HoloCom.SetIsCallSender(receiver, false);
            HoloCom.SetCallSender(sender, sender);
            HoloCom.SetCallSender(receiver, sender);
            HoloCom.SetIsCallReceiver(sender, false);
            HoloCom.SetIsCallReceiver(receiver);
            HoloCom.SetCallReceiver(sender, receiver);
            HoloCom.SetCallReceiver(receiver, receiver);

            var message = "Your HoloCom buzzes as you are receiving a call.";
            if (Random(10) == 1)
            {
                message += " " + ColorToken.Green("Maybe you should answer it.");
            }
            SendMessageToPC(receiver, message);
            if (HoloCom.GetCallAttempt(sender) % 5 == 0)
            {
                FloatingTextStringOnCreature(message, receiver);
            }

            if (HoloCom.GetCallAttempt(sender) <= 15)
            {
                HoloCom.SetCallAttempt(sender, HoloCom.GetCallAttempt(sender) + 1);
                DelayCommand(5.0f, () => { CallPlayer(sender, receiver); });
            }
            else
            {
                SendMessageToPC(sender, "Your HoloCom call went unanswered.");
                SendMessageToPC(receiver, "Your HoloCom stops buzzing.");

                // the following call cleans everything up even if a call isn't currently connected.
                HoloCom.SetIsInCall(sender, receiver, false);
            }
        }
    }
}
