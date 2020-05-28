using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation.Skill;
using static SWLOR.Game.Server.NWN._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class has_skill_or_3
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            return HasSkillRank.Check(3, "OR") ? 1 : 0;
        }
    }
}
