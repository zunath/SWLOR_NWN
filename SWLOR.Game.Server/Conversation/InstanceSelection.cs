using System.Linq;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class InstanceSelection: ConversationBase
    {
        private class Model
        {
            public string AreaResref { get; set; }
            public string DestinationTag { get; set; }
        }
        
        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage(
                "One or more party member is in this instance. Which will you enter?");

            dialog.AddPage("MainPage", mainPage);
            return dialog;
        }

        public override void Initialize()
        {
            LoadMainMenu();
        }

        private void LoadMainMenu()
        {
            var player = GetPC();
            var model = GetDialogCustomData<Model>();
            model.AreaResref = player.GetLocalString("INSTANCE_RESREF");
            model.DestinationTag = player.GetLocalString("INSTANCE_DESTINATION_TAG");

            player.DeleteLocalString("INSTANCE_RESREF");
            player.DeleteLocalLocation("INSTANCE_ORIGINAL_ENTRANCE_LOCATION");

            var members = player.PartyMembers.Where(x => x.Area.GetLocalString("ORIGINAL_RESREF") == model.AreaResref).ToList();

            ClearPageResponses("MainPage");
            AddResponseToPage("MainPage", "Enter new instance");

            foreach (var member in members)
            {
                AddResponseToPage("MainPage", "Enter " + member.Name + "'s instance", true, member);
            }
        }

        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            var model = GetDialogCustomData<Model>();
            NWLocation location;

            if (responseID == 1) // Create new instance
            {
                var instance = AreaService.CreateAreaInstance(player, model.AreaResref, string.Empty, model.DestinationTag);
                location = instance.GetLocalLocation("INSTANCE_ENTRANCE");
            }
            else
            {
                var response = GetResponseByID("MainPage", responseID);
                NWObject member = (NWObject)response.CustomData;

                if (!member.IsValid)
                {
                    player.SendMessage("Unable to locate party member.");
                    return;
                }

                var area = member.Area;
                location = area.GetLocalLocation("INSTANCE_ENTRANCE");
            }
            
            PlayerService.SaveLocation(player);

            player.AssignCommand(() =>
            {
                _.ActionJumpToLocation(location);
            });

            EndConversation();
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
        }

        public override void EndDialog()
        {
        }
    }
}
