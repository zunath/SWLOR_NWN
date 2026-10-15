using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Service
{
    public static partial class Skill
    {
        private static readonly SkillXPMessageQueue _xpMessages = new(
            callback => Scheduler.Schedule(callback, TimeSpan.Zero),
            (player, skill, xp) =>
            {
                if (GetIsObjectValid(player) && GetIsPC(player))
                    SendMessageToPC(player, $"You earned {GetSkillDetails(skill).Name} skill experience. ({xp})");
            });
    }
}
