using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ChatCommandService;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>Farm chat commands (Player City Revamp): the farmhand menu for the house you stand in or the corral beside you.</summary>
    public class FarmChatCommand : IChatCommandListDefinition
    {
        /// <summary>How close a player must stand to a corral for /farmhand to find it.</summary>
        private const float CorralMeters = 15f;

        private readonly ChatCommandBuilder _builder = new();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            Farmhand();

            return _builder.Build();
        }

        private void Farmhand()
        {
            _builder.Create("farmhand")
                .Description("Manages the farmhand droid of the house you stand in, or of the corral beside you.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    if (!GetIsPC(user) || GetIsDM(user) || GetIsDMPossessed(user))
                        return "Only player characters can manage farmhands.";

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var corral = Corrals.GetNearestCorral(user, CorralMeters);
                    ConversationMenu.Start(user, GetIsObjectValid(corral) ? corral : user, nameof(FarmhandDialog));
                });
        }
    }
}
