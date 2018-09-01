using System;
using NWN;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    internal class BackgroundMenu: ConversationBase
    {
        private class Model
        {
            public Background SelectedBackground { get; set; }
            public bool IsConfirming { get; set; }
        }
        
        private readonly IColorTokenService _color;
        private readonly IBackgroundService _background;

        public BackgroundMenu(
            INWScript script, 
            IDialogService dialog, 
            IColorTokenService color,
            IBackgroundService background) 
            : base(script, dialog)
        {
            _background = background;
            _color = color;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "Please select a background out of the following list."
            );

            DialogPage detailsPage = new DialogPage(
                "<SET LATER>",
                "Select Background",
                "Back"
            );

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("DetailsPage", detailsPage);
            return dialog;
        }

        public override void Initialize()
        {
            LoadBackgroundOptionsList();
        }

        private void LoadBackgroundOptionsList()
        {
            foreach (Background entity in _background.GetActiveBackgrounds())
            {
                AddResponseToPage("MainPage", entity.Name, true, new Tuple<string, dynamic>("Entity", entity));
            }
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    HandleMainPageActions(responseID);
                    break;
                case "DetailsPage":
                    HandleDetailsPageActions(responseID);
                    break;
            }
        }

        private void HandleMainPageActions(int responseID)
        {
            DialogResponse response = GetResponseByID("MainPage", responseID);
            Background entity = response.CustomData["Entity"];
            Model model = GetDialogCustomData<Model>();
            model.SelectedBackground = entity;

            string header = _color.Green("Background Name: ") + entity.Name + "\n";
            header += _color.Green("Description: ") + entity.Description + "\n\n";
            header += _color.Green("Bonuses: ") + "\n\n";
            header += entity.Bonuses + "\n\n";
            header += "Will you select this background?";

            SetPageHeader("DetailsPage", header);
            ChangePage("DetailsPage");
        }

        private void HandleDetailsPageActions(int responseID)
        {
            NWPlayer oPC = GetPC();
            Model model = GetDialogCustomData<Model>();

            switch (responseID)
            {
                case 1: // Select Background
                    if (model.IsConfirming)
                    {
                        _background.SetPlayerBackground(oPC, model.SelectedBackground);
                        EndConversation();
                    }
                    else
                    {
                        model.IsConfirming = true;
                        SetResponseText("DetailsPage", 1, "CONFIRM SELECT BACKGROUND");
                    }

                    break;
                case 2: // Back
                    model.IsConfirming = false;
                    SetResponseText("DetailsPage", 1, "Select Background");
                    model.SelectedBackground = null;
                    ChangePage("MainPage");
                    break;
            }
        }
        public override void EndDialog()
        {
        }
    }
}
