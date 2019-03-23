using System.Linq;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.QuestRule.Contracts;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.QuestRule
{
    public class ShowQuestObjectRule: IQuestRule
    {

        public void Run(NWPlayer player, NWObject questSource, int questID, string[] args)
        {
            int count = args.Length;

            for (int index = 0; index < count; index++)
            {
                if (string.IsNullOrWhiteSpace(args[index])) return;

                string visibilityObjectID = args[index];

                var obj = AppCache.VisibilityObjects.Single(x => x.Key == visibilityObjectID).Value;
                ObjectVisibilityService.AdjustVisibility(player, obj, true);
            }
            
        }
    }
}
