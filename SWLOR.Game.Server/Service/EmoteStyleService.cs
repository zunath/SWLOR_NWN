using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Data.Entity;

namespace SWLOR.Game.Server.Service
{
    public static class EmoteStyleService
    {
        public static EmoteStyle GetEmoteStyle(NWObject obj)
        {
            bool novelStyle = false;

            if (obj.IsPlayer)
            {
                NWPlayer player = obj.Object;
                Player pc = DataService.Player.GetByID(player.GlobalID);
                novelStyle = pc.IsUsingNovelEmoteStyle;
            }

            return novelStyle ? EmoteStyle.Novel : EmoteStyle.Regular;
        }

        public static void SetEmoteStyle(NWObject obj, EmoteStyle style)
        {
            if (obj.IsPlayer)
            {
                NWPlayer player = obj.Object;
                Player pc = DataService.Player.GetByID(player.GlobalID);
                pc.IsUsingNovelEmoteStyle = style == EmoteStyle.Novel;
                DataService.SubmitDataChange(pc, DatabaseActionType.Update);
            }
        }
    }
}
