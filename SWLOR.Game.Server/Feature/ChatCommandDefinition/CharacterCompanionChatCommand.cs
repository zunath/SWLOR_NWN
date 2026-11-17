using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.AppearanceDefinition.Species;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using Player = SWLOR.Game.Server.Entity.Player;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Companions;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    /// <summary>
    /// Associate and companion commands.
    /// </summary>
    public class CharacterCompanionChatCommand: IChatCommandListDefinition
    {
        private readonly ChatCommandBuilder _builder = new ChatCommandBuilder();

        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            AssociateCommands();
            Follow();
            OrderCompanion();

            return _builder.Build();
        }

        private void AssociateCommands()
        {
            _builder.Create("associate", "a")
                .Description("Makes your associate (droid, pet, etc.) speak a message.")
                .Permissions(AuthorizationLevel.All)
                .Validate((user, args) =>
                {
                    var associate = GetAssociate(AssociateType.Henchman, user);
                    if (!GetIsObjectValid(associate))
                    {
                        return "You do not have an active associate.";
                    }

                    if (GetIsDead(associate))
                    {
                        return "Your associate is dead.";
                    }

                    if (args.Length <= 0)
                    {
                        return "Please enter a message. Example: /associate Hello!";
                    }

                    return string.Empty;
                })
                .Action((user, target, location, args) =>
                {
                    var associate = GetAssociate(AssociateType.Henchman, user);
                    var message = string.Join(' ', args);

                    AssignCommand(associate, () => SpeakString(message));
                });
        }

        private void Follow()
        {
            _builder.Create("follow")
                .Description("Makes you follow a selected target.")
                .Permissions(AuthorizationLevel.All)
                .RequiresTarget()
                .Action((user, target, location, args) =>
                {
                    AssignCommand(user, () =>
                    {
                        ActionMoveToObject(target, true);
                    });
                });
        }

        private void OrderCompanion()
        {
            _builder.Create("ordercompanion", "order", "oc")
                .Description("Orders your active companion to move to perform an interaction with a target. Use this command on your henchman to cancel all actions.")
                .Permissions(AuthorizationLevel.All)
                .RequiresTarget()
                .Action((user, target, _, _) =>
                {
                    var associate = GetHenchman(user);
                    if (target == associate)
                    {
                        CompanionControl.CancelExplicitOrder(associate);
                        AssignCommand(associate, () =>
                        {
                            ClearAllActions();
                        });
                    }
                    else if (GetIsEnemy(target, user) || GetObjectType(target) == ObjectType.Placeable)
                    {
                        CompanionControl.BeginExplicitOrder(associate);
                        AssignCommand(associate, () =>
                        {
                            ClearAllActions();
                            ActionAttack(target);
                        });
                    }
                    else if (GetObjectType(target) == ObjectType.Door && !GetLocked(target))
                    {
                        CompanionControl.BeginExplicitOrder(associate);
                        AssignCommand(associate, () =>
                        {
                            ClearAllActions();
                            ActionOpenDoor(target);
                        });
                    }
                });
        }
    }
}
