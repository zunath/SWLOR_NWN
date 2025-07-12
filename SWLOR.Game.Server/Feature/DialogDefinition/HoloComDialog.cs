using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;

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

            if (Space.IsPlayerInSpaceMode(player))
            {
                return;
            }

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
                    // Notify the sender that their call was declined
                    SendMessageToPC(callSender, "Your HoloCom call was declined.");
                    
                    // Clean up call attempt state
                    HoloCom.CleanupCallAttempt(callSender, player);
                    
                    EndConversation();
                });
            }

            // Add cancel call option for outgoing calls
            if (HoloCom.IsCallSender(player) && !HoloCom.IsInCall(player))
            {
                var callReceiver = HoloCom.GetCallReceiver(player);
                if (GetIsObjectValid(callReceiver))
                {
                    var receiverName = GetName(callReceiver);
                    page.AddResponse($"Cancel outgoing call to {receiverName}", () =>
                    {
                        // Notify the receiver that the call attempt has ended
                        SendMessageToPC(callReceiver, "Your HoloCom stops buzzing.");
                        
                        // Clean up call attempt state
                        HoloCom.CleanupCallAttempt(player, callReceiver);
                        
                        SendMessageToPC(player, "You cancel your HoloCom call.");
                        EndConversation();
                    });
                }
                else
                {
                    // If receiver is no longer valid, just show a generic cancel option
                    page.AddResponse("Cancel outgoing call", () =>
                    {
                        // Clean up call attempt state
                        HoloCom.CleanupCallAttempt(player, callReceiver);
                        
                        SendMessageToPC(player, "You cancel your HoloCom call.");
                        EndConversation();
                    });
                }
            }

            if (HoloCom.IsCallReceiver(player) || HoloCom.IsInCall(player) || HoloCom.IsCallSender(player)) 
                return;

            for (var pc = GetFirstPC(); GetIsObjectValid(pc); pc = GetNextPC())
            {
                if (GetIsDM(pc) || pc == player || GetIsDMPossessed(pc) || Space.IsPlayerInSpaceMode(pc)) 
                    continue;

                var message = $"Call {GetName(pc)}";
                if (HoloCom.IsInCall(pc))
                {
                    message += ColorToken.Red(" (LINE BUSY)");
                }

                var receiver = pc;
                page.AddResponse(message, () =>
                {
                    if (!HoloCom.IsInCall(receiver) && !Space.IsPlayerInSpaceMode(player) && !Space.IsPlayerInSpaceMode(receiver))
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
            // Check if both players are still valid before proceeding
            if (!GetIsObjectValid(sender) || !GetIsObjectValid(receiver))
            {
                // Clean up any remaining call attempt state
                if (GetIsObjectValid(sender))
                {
                    HoloCom.CleanupCallAttempt(sender, receiver);
                    SendMessageToPC(sender, "Your HoloCom call went unanswered.");
                }
                return;
            }

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
