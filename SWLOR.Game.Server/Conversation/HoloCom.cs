using static SWLOR.Game.Server.NWN._;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class HoloCom: ConversationBase
    {
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage("Holocom Main Menu:\n\n" + 
                                                 "How may we help you?"
                                                ); // Responses dynamically generated            

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            LoadMainMenu();
        }

        public void LoadMainMenu()
        {            
            ClearPageResponses("MainPage");

            string header = ColorTokenService.Green("HoloCom Menu\n\n");
            SetPageHeader("MainPage", header);

            NWPlayer user = GetPC();

            AddResponseToPage("MainPage", "End current call with " + HoloComService.GetTargetForActiveCall(user).Name, HoloComService.IsInCall(user), HoloComService.GetTargetForActiveCall(user));
            AddResponseToPage("MainPage", "Answer incoming call from " + HoloComService.GetCallSender(user).Name, HoloComService.IsCallReceiver(user) && !HoloComService.IsInCall(user), HoloComService.GetCallSender(user));
            AddResponseToPage("MainPage", "Decline incoming call from " + HoloComService.GetCallSender(user).Name, HoloComService.IsCallReceiver(user) && !HoloComService.IsInCall(user), HoloComService.GetCallSender(user));

            if (HoloComService.IsCallReceiver(user) || HoloComService.IsInCall(user) || HoloComService.IsCallSender(user)) return;

            foreach (var player in NWModule.Get().Players)
            {
                if (player == user || !player.IsPlayer) continue;
                string message = "Call " + player.Name;
                if (HoloComService.IsInCall(player))
                {
                    message += ColorTokenService.Red(" (LINE BUSY)");
                }
                AddResponseToPage("MainPage", message, true, player);
            }            
        }

        public void CallPlayer(NWPlayer sender, NWPlayer receiver)
        {     
            if (HoloComService.IsInCall(sender) || HoloComService.IsInCall(receiver)) return;

            if (!HoloComService.IsCallSender(sender)) return;

            SendMessageToPC(sender, "You wait for " + receiver.Name + " to answer their HoloCom.");

            HoloComService.SetIsCallSender(sender);
            HoloComService.SetIsCallSender(receiver, false);
            HoloComService.SetCallSender(sender, sender);
            HoloComService.SetCallSender(receiver, sender);
            HoloComService.SetIsCallReceiver(sender, false);
            HoloComService.SetIsCallReceiver(receiver);
            HoloComService.SetCallReceiver(sender, receiver);
            HoloComService.SetCallReceiver(receiver, receiver);

            string message = "Your HoloCom buzzes as you are receiving a call.";
            if (Random(10) == 1)
            {
                message += " " + ColorTokenService.Green("Maybe you should answer it.");
            }            
            SendMessageToPC(receiver, message);
            if (HoloComService.GetCallAttempt(sender) % 5 == 0)
            {
                FloatingTextStringOnCreature(message, receiver);
            }

            if (HoloComService.GetCallAttempt(sender) <= 15)
            {
                HoloComService.SetCallAttempt(sender, HoloComService.GetCallAttempt(sender) + 1);                
                DelayCommand(5.0f, () => { CallPlayer(sender, receiver); });
            }
            else
            {
                SendMessageToPC(sender, "Your HoloCom call went unanswered.");
                SendMessageToPC(receiver, "Your HoloCom stops buzzing.");

                // the following call cleans everything up even if a call isn't currently connected.
                HoloComService.SetIsInCall(sender, receiver, false);
            }
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            DialogResponse response = GetResponseByID("MainPage", responseID);
            switch (responseID)
            {
                case 1: // End current call
                    HoloComService.SetIsInCall(player, HoloComService.GetTargetForActiveCall(player), false);
                    EndConversation();
                    break;
                case 2: // Accept incoming call
                    HoloComService.SetIsInCall(player, HoloComService.GetCallSender(player), true);
                    EndConversation();
                    break;
                case 3: // Decline incoming call
                    HoloComService.SetIsInCall(player, HoloComService.GetCallSender(player), false);
                    SendMessageToPC(player, "Your HoloCom call was declined.");
                    EndConversation();
                    break;
                default: // Make a call to (NWPlayer) response.CustomData
                    if (!HoloComService.IsInCall((NWPlayer)response.CustomData))
                    {
                        HoloComService.SetIsCallSender(player);
                        DelayCommand(1.0f, () => { CallPlayer(player, (NWPlayer)response.CustomData); });
                    }
                    EndConversation();                    
                    break;
            }
        }
        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }
        public override void EndDialog()
        {
        }
    }
}
