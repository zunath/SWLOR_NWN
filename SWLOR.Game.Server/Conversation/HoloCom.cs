using System;
using NWN;
using static NWN._;
using SWLOR.Game.Server.Data.Entity;
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

            NWPlayer sourcePlayer = GetPC();
            NWPlayer destinationPlayer = GetLocalObject(sourcePlayer, "HOLOCOM_DESTINATION");

            Console.WriteLine("LoadMainMenu: source = " + sourcePlayer.Name + ", dest = " + destinationPlayer.Name);

            if (GetLocalInt(sourcePlayer, "HOLOCOM_CALL_CONNECTED") == 1)
            {
                AddResponseToPage("MainPage", "End current call with " + GetName(destinationPlayer), true);
                return;
            }

            if (GetIsObjectValid(destinationPlayer) == TRUE)
            {
                Console.WriteLine("Adding accept/decline to menu.");
                AddResponseToPage("MainPage", "Answer incoming call from " + GetName(destinationPlayer), true, sourcePlayer);
                AddResponseToPage("MainPage", "Decline incoming call from " + GetName(destinationPlayer), true, sourcePlayer);
            }
            else
            {
                foreach (var player in NWModule.Get().Players)
                {
                    if (player == sourcePlayer || !player.IsPlayer) continue;

                    string message = "Call " + player.Name;
                    if (GetLocalInt(player, "HOLOCOM_CALL_CONNECTED") == 1)
                    {
                        message += ColorTokenService.Red(" (LINE BUSY)");
                    }
                    AddResponseToPage("MainPage", message, true, player);
                }
            }
        }

        public void CallPlayer(NWPlayer sourcePlayer, NWPlayer destinationPlayer)
        {     
            if (GetLocalInt(sourcePlayer, "HOLOCOM_CALL_CONNECTED") == 1 || 
                GetLocalInt(destinationPlayer, "HOLOCOM_CALL_CONNECTED") == 1)
            {
                EndDialog();
                return;
            }
            string message = "Your HoloCom buzzes as you are receiving a call.";
            if (Random(10) == 1)
            {
                message += " " + ColorTokenService.Green("Maybe you should answer it.");
            }
            SendMessageToPC(sourcePlayer, "You wait for " + GetName(destinationPlayer) + " to answer their HoloCom.");
            SendMessageToPC(destinationPlayer, message);

            if ((NWPlayer)GetLocalObject(sourcePlayer, "HOLOCOM_DESTINATION") == destinationPlayer &&
                GetLocalInt(sourcePlayer, "HOLOCOM_ATTEMPT") <= 15 &&
                (NWPlayer)GetLocalObject(destinationPlayer, "HOLOCOM_DESTINATION") == sourcePlayer &&
                GetLocalInt(destinationPlayer, "HOLOCOM_ATTEMPT") <= 15
                )
            {

                SetLocalInt(sourcePlayer, "HOLOCOM_ATTEMPT", GetLocalInt(sourcePlayer, "HOLOCOM_ATTEMPT") + 1);
                SetLocalInt(destinationPlayer, "HOLOCOM_DESTINATION", GetLocalInt(destinationPlayer, "HOLOCOM_DESTINATION") + 1);

                DelayCommand(5.0f, () => { CallPlayer(sourcePlayer, destinationPlayer); });
            }
            else
            {
                SendMessageToPC(sourcePlayer, "Your HoloCom call went unanswered.");
                SendMessageToPC(destinationPlayer, "Your HoloCom stops buzzing.");

                DeleteLocalObject(sourcePlayer, "HOLOCOM_DESTINATION");
                DeleteLocalInt(sourcePlayer, "HOLOCOM_ATTEMPT");
                DeleteLocalObject(destinationPlayer, "HOLOCOM_DESTINATION");
                DeleteLocalInt(destinationPlayer, "HOLOCOM_ATTEMPT");
            }            
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            DialogResponse response = GetResponseByID("MainPage", responseID);
            NWPlayer sourcePlayer = player;
            NWPlayer destinationPlayer = GetLocalObject(sourcePlayer, "HOLOCOM_DESTINATION");

            if (response.CustomData == null)
            {
                RemoveEffect(sourcePlayer, EffectCutsceneImmobilize());
                RemoveEffect(destinationPlayer, EffectCutsceneImmobilize());

                sourcePlayer.AssignCommand(() =>
                {
                    PlaySound("hologram_off");
                });
                destinationPlayer.AssignCommand(() =>
                {
                    PlaySound("hologram_off");
                });
                //AssignCommand(GetLocalObject(sourcePlayer, "HOLOGRAM_DESTINATION"), () => PlaySound("hologram_off"));
                //AssignCommand(GetLocalObject(destinationPlayer, "HOLOGRAM_DESTINATION"), () => PlaySound("hologram_off"));

                DestroyObject(GetLocalObject(sourcePlayer, "HOLOGRAM_DESTINATION"));
                DestroyObject(GetLocalObject(destinationPlayer, "HOLOGRAM_DESTINATION"));

                DeleteLocalObject(sourcePlayer, "HOLOCOM_DESTINATION");
                DeleteLocalObject(sourcePlayer, "HOLOGRAM_DESTINATION");
                DeleteLocalInt(sourcePlayer, "HOLOCOM_ATTEMPT");
                DeleteLocalInt(sourcePlayer, "HOLOCOM_CALL_CONNECTED");
                DeleteLocalObject(destinationPlayer, "HOLOCOM_DESTINATION");
                DeleteLocalObject(destinationPlayer, "HOLOGRAM_DESTINATION");
                DeleteLocalInt(destinationPlayer, "HOLOCOM_ATTEMPT");
                DeleteLocalInt(destinationPlayer, "HOLOCOM_CALL_CONNECTED");
                return;
            }

            if (player == (NWPlayer)response.CustomData)
            {
                // holos are duplicating and swapped also need position offset slightly.
                // accepting or declining a call.
                switch (responseID)
                {
                    case 1: // Accept Call
                        string message = "Call Connected. (Use chat command /endcall to terminate the call)";
                        SendMessageToPC(sourcePlayer, message);
                        SendMessageToPC(destinationPlayer, message);
                        //ApplyEffectToObject(DURATION_TYPE_PERMANENT, EffectCutsceneImmobilize(), sourcePlayer);
                        //ApplyEffectToObject(DURATION_TYPE_PERMANENT, EffectCutsceneImmobilize(), destinationPlayer);
                        SetLocalInt(sourcePlayer, "HOLOCOM_CALL_CONNECTED", 1);
                        SetLocalInt(destinationPlayer, "HOLOCOM_CALL_CONNECTED", 1);

                        var holoreceiver = CopyObject(sourcePlayer, GetLocation(destinationPlayer));
                        var holosender = CopyObject(destinationPlayer, GetLocation(sourcePlayer));
                        ApplyEffectToObject(DURATION_TYPE_PERMANENT, EffectVisualEffect(VFX_DUR_GHOSTLY_VISAGE_NO_SOUND, FALSE), holosender);
                        ApplyEffectToObject(DURATION_TYPE_PERMANENT, EffectVisualEffect(VFX_DUR_GHOSTLY_VISAGE_NO_SOUND, FALSE), holoreceiver);
                        SetPlotFlag(holoreceiver, TRUE);
                        SetPlotFlag(holosender, TRUE);

                        sourcePlayer.AssignCommand(() =>
                        {
                            PlaySound("hologram_on");
                        });
                        destinationPlayer.AssignCommand(() =>
                        {
                            PlaySound("hologram_on");
                        });
                        //AssignCommand(holoreceiver, () => PlaySound("hologram_on"));
                        //AssignCommand(holosender, () => PlaySound("hologram_on"));

                        SetLocalObject(sourcePlayer, "HOLOGRAM_DESTINATION", holoreceiver);
                        SetLocalObject(destinationPlayer, "HOLOGRAM_DESTINATION", holosender);

                        SetLocalObject(holoreceiver, "HOLOGRAM_SOURCE", sourcePlayer);
                        SetLocalObject(holosender, "HOLOGRAM_SOURCE", destinationPlayer);

                        EndDialog();
                        break;
                    case 2: // Decline call

                        SendMessageToPC(destinationPlayer, "Your HoloCom call was declined.");

                        DeleteLocalObject(sourcePlayer, "HOLOCOM_DESTINATION");
                        DeleteLocalInt(sourcePlayer, "HOLOCOM_ATTEMPT");
                        DeleteLocalObject(destinationPlayer, "HOLOCOM_DESTINATION");
                        DeleteLocalInt(destinationPlayer, "HOLOCOM_ATTEMPT");
                        LoadMainMenu();
                        break;
                }
            }
            else
            {                
                // Make call
                destinationPlayer = (NWPlayer)response.CustomData;

                if (GetLocalInt(destinationPlayer, "HOLOCOM_CALL_CONNECTED") != 1)
                {
                    SetLocalObject(sourcePlayer, "HOLOCOM_DESTINATION", destinationPlayer);
                    SetLocalInt(sourcePlayer, "HOLOCOM_ATTEMPT", 1);
                    SetLocalObject(destinationPlayer, "HOLOCOM_DESTINATION", sourcePlayer);
                    SetLocalInt(destinationPlayer, "HOLOCOM_ATTEMPT", 1);

                    DelayCommand(1.0f, () => { CallPlayer(sourcePlayer, destinationPlayer); });
                    //LoadMainMenu();
                }
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
