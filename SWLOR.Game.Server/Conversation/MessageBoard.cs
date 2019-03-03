using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Conversation
{
    public class MessageBoard: ConversationBase
    {
        private readonly IDataService _data;
        private readonly IColorTokenService _color;

        public MessageBoard(
            INWScript script, 
            IDialogService dialog,
            IDataService data,
            IColorTokenService color) 
            : base(script, dialog)
        {
            _data = data;
            _color = color;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage("Messages may be viewed or posted here. Please select a message from the list below or create a new one.");
            DialogPage postDetailsPage = new DialogPage();
            DialogPage createPostPage = new DialogPage();

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("PostDetailsPage", postDetailsPage);
            dialog.AddPage("CreatePostPage", createPostPage);
            return dialog;
        }

        public override void Initialize()
        {
            LoadMainPage();
        }


        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainPageResponses(responseID);
                    break;
                case "PostDetailsPage":
                    PostDetailsPageResponses(responseID);
                    break;
                case "CreatePostPage":
                    CreatePostPageResponses(responseID);
                    break;
            }
        }

        private void LoadMainPage()
        {
            NWPlayer player = GetPC();
            NWPlaceable terminal = Object.OBJECT_SELF;
            DateTime now = DateTime.UtcNow;
            Guid boardID = new Guid(terminal.GetLocalString("MESSAGE_BOARD_ID"));
            var messages = _data.Where<Message>(x => x.BoardID == boardID && x.DateExpires > now)
                .OrderByDescending(o => o.DatePosted);

            ClearPageResponses("MainPage");
            AddResponseToPage("MainPage", _color.Green("Create New Post"));
            foreach (var message in messages)
            {
                string title = message.Title;
                if (message.PlayerID == player.GlobalID)
                    title = _color.Cyan(title);
                AddResponseToPage("MainPage", title, true, message.ID);
            }
        }

        private void MainPageResponses(int responseID)
        {
            DialogResponse response = GetResponseByID("MainPage", responseID);
            if (!response.HasCustomData)
            {
                LoadCreatePostPage();
                ChangePage("CreatePostPage");
                return;
            }

            Guid messageID = (Guid)response.CustomData;
            LoadPostDetailsPage(messageID);
            ChangePage("PostDetailsPage");
        }

        private void LoadPostDetailsPage(Guid messageID)
        {
            Message message = _data.Get<Message>(messageID);
            Player poster = _data.Get<Player>(message.PlayerID);
            string header = _color.Green("Title: ") + message.Title + "\n";
            header += _color.Green("Posted By: ") + poster.CharacterName + "\n";
            header += _color.Green("Date: ") + message.DatePosted + "\n\n";
            header += message.Text;

            SetPageHeader("PostDetailsPage", header);
        }

        private void PostDetailsPageResponses(int responseID)
        {

        }

        private void LoadCreatePostPage()
        {
        }

        private void CreatePostPageResponses(int responseID)
        {

        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }
}
