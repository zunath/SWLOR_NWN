using SWLOR.Core.Enumeration;
using SWLOR.Core.Service;
using SWLOR.Core.Service.ChatCommandService;

namespace SWLOR.Core.Feature.ChatCommandDefinition
{
    public class SpaceChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            ExitSpaceCommand();

            return _builder.Build();
        }

        private void ExitSpaceCommand()
        {
            _builder.Create("exit")
                .Description("Exits the pilot seat when controlling a starship.")
                .Permissions(AuthorizationLevel.All)
                .Action((user, target, location, args) =>
                {
                    if (!Space.IsPlayerInSpaceMode(user))
                    {
                        SendMessageToPC(user, "This command can only be used while piloting a starship.");
                        return;
                    }

                    if (Enmity.HasEnmity(user))
                    {
                        SendMessageToPC(user, "This command cannot be used while you're targeted.");
                        return;
                    }

                    Space.WarpPlayerInsideShip(user);
                });
        }
    }
}
