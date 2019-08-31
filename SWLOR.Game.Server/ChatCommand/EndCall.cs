using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using static NWN._;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Ends your current HoloCom call.", CommandPermissionType.Player | CommandPermissionType.DM | CommandPermissionType.Admin)]
    public class EndCall : IChatCommand
    {
        // not showing in chat menu and not a valid command.
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            NWPlayer sourcePlayer = user;
            NWPlayer destinationPlayer = GetLocalObject(sourcePlayer, "HOLOCOM_DESTINATION");

            RemoveEffect(sourcePlayer, EffectCutsceneImmobilize());
            RemoveEffect(destinationPlayer, EffectCutsceneImmobilize());

            sourcePlayer.AssignCommand(() =>
            {
                PlaySound("hologram_off");
            });
            destinationPlayer.AssignCommand(() =>
            {
                PlaySound("hologram_off");
            });
            //AssignCommand(GetLocalObject(sourcePlayer, "HOLOGRAM_DESTINATION"), () => PlaySound("hologram_off"));
            //AssignCommand(GetLocalObject(destinationPlayer, "HOLOGRAM_DESTINATION"), () => PlaySound("hologram_off"));

            DestroyObject(GetLocalObject(sourcePlayer, "HOLOGRAM_DESTINATION"));
            DestroyObject(GetLocalObject(destinationPlayer, "HOLOGRAM_DESTINATION"));

            DeleteLocalObject(sourcePlayer, "HOLOCOM_DESTINATION");
            DeleteLocalObject(sourcePlayer, "HOLOGRAM_DESTINATION");
            DeleteLocalInt(sourcePlayer, "HOLOCOM_ATTEMPT");
            DeleteLocalInt(sourcePlayer, "HOLOCOM_CALL_CONNECTED");
            DeleteLocalObject(destinationPlayer, "HOLOCOM_DESTINATION");
            DeleteLocalObject(destinationPlayer, "HOLOGRAM_DESTINATION");
            DeleteLocalInt(destinationPlayer, "HOLOCOM_ATTEMPT");
            DeleteLocalInt(destinationPlayer, "HOLOCOM_CALL_CONNECTED");
        }

        public bool RequiresTarget => false;
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }
    }
}
