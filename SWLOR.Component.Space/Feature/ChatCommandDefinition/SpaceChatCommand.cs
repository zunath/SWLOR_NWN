using SWLOR.Component.Communication.Contracts;
using SWLOR.Component.Communication.Model;
using SWLOR.Component.Communication.Service;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Space.Contracts;

namespace SWLOR.Component.Space.Feature.ChatCommandDefinition
{
    public class SpaceChatCommand: IChatCommandListDefinition
    {
        private readonly IChatCommandBuilder _builder;
        private readonly IEnmityService _enmity;
        private readonly ISpaceService _space;

        public SpaceChatCommand(
            IChatCommandBuilder builder,
            IEnmityService enmity,
            ISpaceService space)
        {
            _builder = builder;
            _enmity = enmity;
            _space = space;
        }

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
                    if (!_space.IsPlayerInSpaceMode(user))
                    {
                        SendMessageToPC(user, "This command can only be used while piloting a starship.");
                        return;
                    }

                    if (_enmity.HasEnmity(user))
                    {
                        SendMessageToPC(user, "This command cannot be used while you're targeted.");
                        return;
                    }

                    _space.WarpPlayerInsideShip(user);
                });
        }
    }
}
