using SWLOR.Game.Server;
using SWLOR.Game.Server.Event.Conversation.Skill;
using static SWLOR.Game.Server.NWN._;

// ReSharper disable once CheckNamespace
namespace NWN.Scripts
{
#pragma warning disable IDE1006 // Naming Styles
    public class has_skill_or_8
#pragma warning restore IDE1006 // Naming Styles
    {
        public static int Main()
        {
            return HasSkillRank.Check(8, "OR") ? true : false;
        }
    }
}
