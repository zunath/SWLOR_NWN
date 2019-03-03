using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;
using static NWN.NWScript;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Conversation
{
    public class MessageBoard: ConversationBase
    {
        private class Model
        {
            public bool IsConfirming { get; set; }
            public Guid MessageID { get; set; }
            public string Title { get; set; }
            public string Message { get; set; }
        }

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
            DialogPage postDetailsPage = new DialogPage("<SET LATER>",
                "Remove Post");
            DialogPage createPostPage = new DialogPage("<SET LATER>",
                "Set Title",
                "Set Message",
                "Post Message");

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
            var messages = _data.Where<Message>(x => x.BoardID == boardID && x.DateExpires > now && x.DateRemoved == null)
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

            var model = GetDialogCustomData<Model>();
            model.MessageID = (Guid)response.CustomData;
            LoadPostDetailsPage();
            ChangePage("PostDetailsPage");
        }

        private void LoadPostDetailsPage()
        {
            NWPlayer player = GetPC();
            Model model = GetDialogCustomData<Model>();
            Message message = _data.Get<Message>(model.MessageID);
            Player poster = _data.Get<Player>(message.PlayerID);
            string header = _color.Green("Title: ") + message.Title + "\n";
            header += _color.Green("Posted By: ") + poster.CharacterName + "\n";
            header += _color.Green("Date: ") + message.DatePosted + "\n\n";
            header += message.Text;

            SetPageHeader("PostDetailsPage", header);

            // Show or hide the "Remove Post" option depending on if the player is the poster.
            SetResponseVisible("PostDetailsPage", 1, player.GlobalID == message.PlayerID);
        }

        private void PostDetailsPageResponses(int responseID)
        {
            var model = GetDialogCustomData<Model>();
            var message = _data.Get<Message>(model.MessageID);

            switch (responseID)
            {
                case 1: // Remove Post
                    if (model.IsConfirming)
                    {
                        model.IsConfirming = false;
                        SetResponseText("PostDetailsPage", 1, "Remove Post");
                        message.DateRemoved = DateTime.UtcNow;
                        _data.SubmitDataChange(message, DatabaseActionType.Update);
                        ClearNavigationStack();
                        LoadMainPage();
                        ChangePage("MainPage", false);
                    }
                    else
                    {
                        model.IsConfirming = true;
                        SetResponseText("PostDetailsPage", 1, "CONFIRM REMOVE POST");
                    }
                    break;
            }
        }

        private void LoadCreatePostPage()
        {
            var player = GetPC();
            Model model = GetDialogCustomData<Model>();
            NWPlaceable terminal = Object.OBJECT_SELF;
            int price = terminal.GetLocalInt("PRICE");
            string header = "Please enter text and then click the 'Set Title' or 'Set Message' buttons. Titles must be 256 characters or less. Messages must be 4000 characters or less.\n\n";
            header += "Posting a message costs " + price + " credits. Posts last for 30 days (real world time) before they will expire.\n\n";
            header += _color.Green("Title: ") + model.Title + "\n";
            header += _color.Green("Message: ") + model.Message + "\n";

            SetPageHeader("CreatePostPage", header);

            player.SetLocalInt("MESSAGE_BOARD_LISTENING", TRUE);
        }

        private void CreatePostPageResponses(int responseID)
        {
            var player = GetPC();
            NWPlaceable terminal = Object.OBJECT_SELF;
            var model = GetDialogCustomData<Model>();
            int price = terminal.GetLocalInt("PRICE");
            string text = player.GetLocalString("MESSAGE_BOARD_TEXT");
            Guid boardID = new Guid(terminal.GetLocalString("MESSAGE_BOARD_ID"));
            DateTime now = DateTime.UtcNow;

            switch (responseID)
            {
                case 1: // Set Title
                    if (text.Length <= 0)
                    {
                        player.FloatingText("Please enter text via chat.");
                        return;
                    }
                    else if (text.Length > 256)
                    {
                        player.FloatingText("Titles cannot be longer than 50 characters. Your text has been truncated.");
                        text = text.Substring(0, 50);
                    }

                    model.Title = text.Trim();
                    player.DeleteLocalString("MESSAGE_BOARD_TEXT");
                    LoadCreatePostPage();
                    break;
                case 2: // Set Message
                    if (text.Length <= 0)
                    {
                        player.FloatingText("Please enter text via chat.");
                        return;
                    }
                    else if (text.Length > 4000)
                    {
                        player.FloatingText("Messages cannot be longer than 4000 characters. Your text has been truncated.");
                        text = text.Substring(0, 4000);
                    }

                    model.Message = text.Trim();
                    player.DeleteLocalString("MESSAGE_BOARD_TEXT");
                    LoadCreatePostPage();
                    break;
                case 3: // Post Message
                    if(player.Gold < price)
                    {
                        player.FloatingText("You need " + price + " credits to post this message.");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(model.Title) ||
                        string.IsNullOrWhiteSpace(model.Message))
                    {
                        player.FloatingText("Both the title and the message text must be set before you may post.");
                        return;
                    }

                    if (model.IsConfirming)
                    {
                        model.IsConfirming = false;
                        SetResponseText("CreatePostPage", 3, "Post Message");

                        var post = new Message
                        {
                            ID = Guid.NewGuid(),
                            BoardID = boardID,
                            PlayerID = player.GlobalID,
                            Title = model.Title,
                            Text = model.Message,
                            DatePosted = now,
                            DateExpires = now.AddDays(30),
                            DateRemoved = null
                        };
                        _data.SubmitDataChange(post, DatabaseActionType.Insert);
                        _.TakeGoldFromCreature(price, player, TRUE);

                        LoadMainPage();
                        ClearNavigationStack();
                        ChangePage("MainPage", false);
                    }
                    else
                    {
                        model.IsConfirming = true;
                        SetResponseText("CreatePostPage", 3, "CONFIRM POST MESSAGE");
                    }
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
            var model = GetDialogCustomData<Model>();
            model.IsConfirming = false;
            model.Title = string.Empty;
            model.Message = string.Empty;
            SetResponseText("PostDetailsPage", 1, "Remove Post");
            SetResponseText("CreatePostPage", 3, "Post Message");
            player.DeleteLocalInt("MESSAGE_BOARD_LISTENING");
            player.DeleteLocalString("MESSAGE_BOARD_TEXT");
        }

        public override void EndDialog()
        {
        }
    }
}
