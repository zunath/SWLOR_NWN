using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    internal class ChangeHead: ConversationBase
    {
        private class Model
        {
            public int OriginalHeadID { get; set; }
            public int CurrentHeadID { get; set; }
        }

        private readonly IColorTokenService _color;

        public ChangeHead(
            INWScript script, 
            IDialogService dialog,
            IColorTokenService color) 
            : base(script, dialog)
        {
            _color = color;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");
            DialogPage mainPage = new DialogPage(
                "<SET LATER>",
                "Advance Head ID by 1",
                "Advance Head ID by 10",
                "Advance Head ID by 100",
                "Decrease Head ID by 1",
                "Decrease Head ID by 10",
                "Decrease Head ID by 100",
                "Set Head",
                "Reset Head",
                "Back"
            );
            
            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            var model = GetDialogCustomData<Model>();
            model.OriginalHeadID = _.GetCreatureBodyPart(NWScript.CREATURE_PART_HEAD, GetPC().Object);
            model.CurrentHeadID = model.OriginalHeadID;
            SetPageHeader("MainPage", BuildHeader());
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                {
                    switch (responseID)
                    {
                        case 1: // Advance by 1
                            ChangeHeadID(1);
                            break;
                        case 2: // Advance by 10
                            ChangeHeadID(10);
                            break;
                        case 3: // Advance by 100
                            ChangeHeadID(100);
                            break;
                        case 4: // Decrease by 1
                            ChangeHeadID(-1);
                            break;
                        case 5: // Decrease by 10
                            ChangeHeadID(-10);
                            break;
                        case 6: // Decrease by 100
                            ChangeHeadID(-100);
                            break;
                        case 7: // Set Portrait
                            SetHeadID();
                            break;
                        case 8: // Reset Portrait
                            ResetHeadID();
                            break;
                        case 9: // Back
                            ResetHeadID();
                            SwitchConversation("CharacterManagement");
                            break;
                    }

                    break;
                }
            }
        }

        public override void EndDialog()
        {
            ResetHeadID();
        }


        private void ChangeHeadID(int adjustBy)
        {
            NWObject oPC = GetPC();
            Model model = GetDialogCustomData<Model>();
            model.CurrentHeadID = model.CurrentHeadID + adjustBy;
            const int numberOfHeads = 202;

            if (model.CurrentHeadID > numberOfHeads)
            {
                model.CurrentHeadID = model.CurrentHeadID - numberOfHeads;
            }

            if (model.CurrentHeadID < 1)
            {
                model.CurrentHeadID = numberOfHeads + model.CurrentHeadID;
            }

            _.SetCreatureBodyPart(NWScript.CREATURE_PART_HEAD, model.CurrentHeadID, oPC.Object);
            SetPageHeader("MainPage", BuildHeader());
        }

        private void ResetHeadID()
        {
            NWObject oPC = GetPC();
            Model dto = GetDialogCustomData<Model>();
            dto.CurrentHeadID = dto.OriginalHeadID;
            _.SetCreatureBodyPart(NWScript.CREATURE_PART_HEAD, dto.OriginalHeadID, oPC.Object);
            SetPageHeader("MainPage", BuildHeader());
        }

        private void SetHeadID()
        {
            Model dto = GetDialogCustomData<Model>();
            dto.OriginalHeadID = dto.CurrentHeadID;
            SetPageHeader("MainPage", BuildHeader());
        }

        private string BuildHeader()
        {
            Model model = GetDialogCustomData<Model>();
            string header = "You may adjust your character's head here." + "\n\n";
            header += _color.Green("Current Set Head ID: ") + model.OriginalHeadID + "\n";
            header += _color.Green("Viewing Head ID: ") + model.CurrentHeadID + "\n";
            
            return header;
        }
    }
}
