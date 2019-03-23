using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;



namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Exits from space system flight mode.", CommandPermissionType.Player)]
    public class Exit : IChatCommand
    {
        /// <summary>
        /// Exits flight mode in the space system. 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="target"></param>
        /// <param name="targetLocation"></param>
        /// <param name="args"></param>
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            // Covered in SpaceSystem.OnNWNXChat()
        }

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            if (args.Length > 0)
            {
                return "Just type /exit to return to the ship interior.";
            }
            
            return string.Empty;
        }

        public bool RequiresTarget => false;
    }
}
