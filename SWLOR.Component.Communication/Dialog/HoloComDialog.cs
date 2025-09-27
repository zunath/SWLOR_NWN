using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.ValueObjects;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Communication.Dialog
{
    public class HoloComDialog: DialogBase
    {
        private readonly IHoloComService _holoComService;
        private readonly ISpaceService _space;
        private const string MainPageId = "MAIN_PAGE";

        public HoloComDialog(
            IHoloComService holoComService, 
            IDialogService dialogService,
            IDialogBuilder dialogBuilder,
            ISpaceService spaceService) 
            : base(dialogService, dialogBuilder)
        {
            _holoComService = holoComService;
            _space = spaceService;
        }
        
        public override PlayerDialog SetUp(uint player)
        {
            var builder = DialogBuilder
                .AddPage(MainPageId, MainPageInit);

            return builder.Build();
        }

        private void MainPageInit(DialogPage page)
        {
            page.Header = ColorToken.Green("HoloCom Menu\n\n");

            var player  = GetPC();

            if (_space.IsPlayerInSpaceMode(player))
            {
                return;
            }

            if (_holoComService.IsInCall(player))
            {
                var activeCallTarget = _holoComService.GetTargetForActiveCall(player);
                page.AddResponse($"End current call with {GetName(activeCallTarget)}", () =>
                {
                    _holoComService.SetIsInCall(player, activeCallTarget, false);
                    EndConversation();
                });
            }

            if (_holoComService.IsCallReceiver(player) && !_holoComService.IsInCall(player))
            {
                var callSender = _holoComService.GetCallSender(player);
                var callerName = GetName(callSender);
                page.AddResponse($"Answer incoming call from {callerName}", () =>
                {
                    _holoComService.SetIsInCall(player, callSender, true);
                    EndConversation();
                });
                page.AddResponse($"Decline incoming call from {callerName}", () =>
                {
                    // Notify the sender that their call was declined
                    SendMessageToPC(callSender, "Your HoloCom call was declined.");
                    
                    // Clean up call attempt state
                    _holoComService.CleanupCallAttempt(callSender, player);
                    
                    EndConversation();
                });
            }

            // Add cancel call option for outgoing calls
            if (_holoComService.IsCallSender(player) && !_holoComService.IsInCall(player))
            {
                var callReceiver = _holoComService.GetCallReceiver(player);
                if (GetIsObjectValid(callReceiver))
                {
                    var receiverName = GetName(callReceiver);
                    page.AddResponse($"Cancel outgoing call to {receiverName}", () =>
                    {
                        // Notify the receiver that the call attempt has ended
                        SendMessageToPC(callReceiver, "Your HoloCom stops buzzing.");
                        
                        // Clean up call attempt state
                        _holoComService.CleanupCallAttempt(player, callReceiver);
                        
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
                        _holoComService.CleanupCallAttempt(player, callReceiver);
                        
                        SendMessageToPC(player, "You cancel your HoloCom call.");
                        EndConversation();
                    });
                }
            }

            if (_holoComService.IsCallReceiver(player) || _holoComService.IsInCall(player) || _holoComService.IsCallSender(player)) 
                return;

            for (var pc = GetFirstPC(); GetIsObjectValid(pc); pc = GetNextPC())
            {
                if (GetIsDM(pc) || pc == player || GetIsDMPossessed(pc) || _space.IsPlayerInSpaceMode(pc)) 
                    continue;

                var message = $"Call {GetName(pc)}";
                if (_holoComService.IsInCall(pc))
                {
                    message += ColorToken.Red(" (LINE BUSY)");
                }

                var receiver = pc;
                page.AddResponse(message, () =>
                {
                    if (!_holoComService.IsInCall(receiver) && !_space.IsPlayerInSpaceMode(player) && !_space.IsPlayerInSpaceMode(receiver))
                    {
                        _holoComService.SetIsCallSender(player);
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
                    _holoComService.CleanupCallAttempt(sender, receiver);
                    SendMessageToPC(sender, "Your HoloCom call went unanswered.");
                }
                return;
            }

            if (_holoComService.IsInCall(sender) || _holoComService.IsInCall(receiver)) return;

            if (!_holoComService.IsCallSender(sender)) return;

            var receiverName = GetName(receiver);
            SendMessageToPC(sender, "You wait for " + receiverName + " to answer their _holoComService.");

            _holoComService.SetIsCallSender(sender);
            _holoComService.SetIsCallSender(receiver, false);
            _holoComService.SetCallSender(sender, sender);
            _holoComService.SetCallSender(receiver, sender);
            _holoComService.SetIsCallReceiver(sender, false);
            _holoComService.SetIsCallReceiver(receiver);
            _holoComService.SetCallReceiver(sender, receiver);
            _holoComService.SetCallReceiver(receiver, receiver);

            var message = "Your HoloCom buzzes as you are receiving a call.";
            if (Random(10) == 1)
            {
                message += " " + ColorToken.Green("Maybe you should answer it.");
            }
            SendMessageToPC(receiver, message);
            if (_holoComService.GetCallAttempt(sender) % 5 == 0)
            {
                FloatingTextStringOnCreature(message, receiver);
            }

            if (_holoComService.GetCallAttempt(sender) <= 15)
            {
                _holoComService.SetCallAttempt(sender, _holoComService.GetCallAttempt(sender) + 1);
                DelayCommand(5.0f, () => { CallPlayer(sender, receiver); });
            }
            else
            {
                SendMessageToPC(sender, "Your HoloCom call went unanswered.");
                SendMessageToPC(receiver, "Your HoloCom stops buzzing.");

                // the following call cleans everything up even if a call isn't currently connected.
                _holoComService.SetIsInCall(sender, receiver, false);
            }
        }
    }
}
