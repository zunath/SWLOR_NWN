using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class ChatTextService : IChatTextService
    {
        private readonly INWScript _;
        private readonly IColorTokenService _color;

        public ChatTextService(
            INWScript script,
            IColorTokenService color)
        {
            _ = script;
            _color = color;
        }

        public void OnModuleChat()
        {
            string message = _.GetPCChatMessage();
            
            bool foundEmote = false;
            string finalText = string.Empty;
            string coloringText = string.Empty;
            foreach (var @char in message)
            {
                if (foundEmote)
                {
                    coloringText += @char;

                    if (@char == '*')
                    {
                        finalText += _color.Custom(coloringText, 200, 172, 150);
                        coloringText = string.Empty;
                        foundEmote = false;
                    }
                }
                else
                {
                    if (@char == '*')
                    {
                        coloringText += @char;
                        foundEmote = true;
                    }
                    else
                    {
                        finalText += @char;
                    }
                }
            }

            _.SetPCChatMessage(finalText);
        }

    }
}
