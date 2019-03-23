using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Service
{
    public class EmoteStyleService : IEmoteStyleService
    {
        

        public EmoteStyleService(
            )
        {
            
        }

        public EmoteStyle GetEmoteStyle(NWObject obj)
        {
            bool novelStyle = false;

            if (obj.IsPlayer)
            {
                NWPlayer player = obj.Object;
                Player pc = DataService.Single<Player>(x => x.ID == player.GlobalID);
                novelStyle = pc.IsUsingNovelEmoteStyle;
            }

            return novelStyle ? EmoteStyle.Novel : EmoteStyle.Regular;
        }

        public void SetEmoteStyle(NWObject obj, EmoteStyle style)
        {
            if (obj.IsPlayer)
            {
                NWPlayer player = obj.Object;
                Player pc = DataService.Single<Player>(x => x.ID == player.GlobalID);
                pc.IsUsingNovelEmoteStyle = style == EmoteStyle.Novel;
                DataService.SubmitDataChange(pc, DatabaseActionType.Update);
            }
        }
    }
}
