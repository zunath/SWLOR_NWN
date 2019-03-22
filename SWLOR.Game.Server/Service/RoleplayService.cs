using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class RoleplayService
    {
        private readonly INWScript _;
        private readonly INWNXChat _nwnxChat;
        private readonly IDataService _data;

        public RoleplayService(
            INWScript script,
            INWNXChat nwnxChat,
            IDataService data)
        {
            _ = script;
            _nwnxChat = nwnxChat;
            _data = data;
        }

        private static readonly int[] ValidChannels =
        {
            NWNXChat.NWNX_CHAT_CHANNEL_PLAYER_PARTY,
            NWNXChat.NWNX_CHAT_CHANNEL_PLAYER_SHOUT,
            NWNXChat.NWNX_CHAT_CHANNEL_PLAYER_TALK,
            NWNXChat.NWNX_CHAT_CHANNEL_PLAYER_WHISPER
        };

        public void OnNWNXChat()
        {
            int channel = _nwnxChat.GetChannel();

            // Must be one of the valid channels
            if (!ValidChannels.Contains(channel)) return;
            var sender = _nwnxChat.GetSender();

            // Must be a player
            if (!sender.IsPlayer) return;

            // Validate whether player should receive an RP Point
            bool canReceivePoint = CanReceiveRPPoint(sender.Object, channel);

            var dbPlayer = _data.Get<Player>(sender.GlobalID);

        }

        private bool CanReceiveRPPoint(NWPlayer player, int channel)
        {
            // Party - Must be in a party with another PC.
            if (channel == NWNXChat.NWNX_CHAT_CHANNEL_PLAYER_PARTY)
            {

            }

            // Shout - Another player must be online.
            else if (channel == NWNXChat.NWNX_CHAT_CHANNEL_PLAYER_SHOUT)
            {

            }

            // Talk - Another player must be nearby.
            else if(channel == NWNXChat.NWNX_CHAT_CHANNEL_PLAYER_TALK)
            {
            }


            // Whisper - Another player must be nearby.
            else if (channel == NWNXChat.NWNX_CHAT_CHANNEL_PLAYER_WHISPER)
            {

            }

            return false;
        }
    }
}
