using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ChatCommand
{
    public abstract class LoopingAnimationCommand: IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {
            float duration = 9999.0f;

            if (args.Length > 0)
            {
                if (!float.TryParse(args[0], out duration))
                {
                    duration = 9999.0f;
                }
            }

            DoAction(user, duration);
        }

        public bool RequiresTarget => false;

        protected abstract void DoAction(NWPlayer user, float duration);

        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return string.Empty;
        }
    }
}
