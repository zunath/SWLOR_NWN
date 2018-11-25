using System;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class PlayerDescriptionService: IPlayerDescriptionService
    {
        private readonly INWScript _;

        public PlayerDescriptionService(INWScript script)
        {
            _ = script;
        }

        public void OnModuleChat()
        {
            NWPlayer sender = (_.GetPCChatSpeaker());
            if (sender.GetLocalInt("LISTENING_FOR_DESCRIPTION") != 1) return;
            if (!sender.IsPlayer) return;

            string text = _.GetPCChatMessage().Trim();
            sender.SetLocalString("NEW_DESCRIPTION_TO_SET", text);

            _.SetPCChatMessage(string.Empty); // Skip the message

            _.SendMessageToPC(sender.Object, "New description received. Please press the 'Next' button in the conversation window.");
        }

        public void ChangePlayerDescription(NWPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (player.Object == null) throw new ArgumentNullException(nameof(player.Object));

            string newDescription = player.GetLocalString("NEW_DESCRIPTION_TO_SET");
            _.SetDescription(player.Object, newDescription);
            _.SetDescription(player.Object, newDescription, NWScript.FALSE);

            _.FloatingTextStringOnCreature("New description set!", player.Object, NWScript.FALSE);
        }
    }
}
