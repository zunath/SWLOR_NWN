using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using System.Linq;

namespace SWLOR.Game.Server.Service
{
    public class EmoteStyleService : IEmoteStyleService
    {
        private readonly IDataContext _db;

        public EmoteStyleService(
            IDataContext db)
        {
            _db = db;
        }

        public EmoteStyle GetEmoteStyle(NWObject obj)
        {
            bool novelStyle = false;

            if (obj.IsPlayer)
            {
                NWPlayer player = obj.Object;
                PlayerCharacter pc = _db.PlayerCharacters.Single(x => x.PlayerID == player.GlobalID);
                novelStyle = pc.IsUsingNovelEmoteStyle;
            }

            return novelStyle ? EmoteStyle.Novel : EmoteStyle.Regular;
        }

        public void SetEmoteStyle(NWObject obj, EmoteStyle style)
        {
            if (obj.IsPlayer)
            {
                NWPlayer player = obj.Object;
                PlayerCharacter pc = _db.PlayerCharacters.Single(x => x.PlayerID == player.GlobalID);
                pc.IsUsingNovelEmoteStyle = style == EmoteStyle.Novel;
                _db.SaveChanges();
            }
        }
    }
}
