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
        private readonly IDataService _data;

        public EmoteStyleService(
            IDataService data)
        {
            _data = data;
        }

        public EmoteStyle GetEmoteStyle(NWObject obj)
        {
            bool novelStyle = false;

            if (obj.IsPlayer)
            {
                NWPlayer player = obj.Object;
                PlayerCharacter pc = _data.Single<PlayerCharacter>(x => x.ID == player.GlobalID);
                novelStyle = pc.IsUsingNovelEmoteStyle;
            }

            return novelStyle ? EmoteStyle.Novel : EmoteStyle.Regular;
        }

        public void SetEmoteStyle(NWObject obj, EmoteStyle style)
        {
            if (obj.IsPlayer)
            {
                NWPlayer player = obj.Object;
                PlayerCharacter pc = _data.Single<PlayerCharacter>(x => x.ID == player.GlobalID);
                pc.IsUsingNovelEmoteStyle = style == EmoteStyle.Novel;
                _data.SubmitDataChange(pc, DatabaseActionType.Update);
            }
        }
    }
}
