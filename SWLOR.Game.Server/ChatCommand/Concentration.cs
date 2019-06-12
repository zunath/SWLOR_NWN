using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Tells you what concentration ability you have active. Follow with 'end' (no quotes) to turn your concentration ability off.", CommandPermissionType.Player)]
    public class Concentration: IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            // Did the player pass in the 'end' argument? If so, we're ending the concentration effect.
            bool doEnd = args.Length > 0 && args[0] == "end";

            if (doEnd)
            {
                AbilityService.EndConcentrationEffect(user);
                user.SendMessage("You have ended your concentration ability.");
            }
            // Otherwise just notify them about what effect is currently active.
            else
            {
                var effect = AbilityService.GetActiveConcentrationEffect(user);
                if (effect.Type == PerkType.Unknown)
                {
                    user.SendMessage("No concentration ability is currently active.");
                }
                else
                {
                    var perk = PerkService.GetPerkByID((int)effect.Type);
                    user.SendMessage("Currently active concentration ability: " + perk.Name);
                }
            }
        }

        public bool RequiresTarget => false;
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return null;
        }
    }
}
