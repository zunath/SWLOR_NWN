using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.GameObject.Contracts;


namespace SWLOR.Game.Server.GameObject
{
    public class NWModule: NWObject, INWModule
    {
        public NWModule(INWScript script,
            AppState state) 
            : base(script, state)
        {
        }

        public static NWModule Get()
        {
            var module = (NWModule) App.Resolve<INWModule>();
            INWScript _ = App.Resolve<INWScript>();
            module.Object = _.GetModule();

            return module;
        }

        public List<NWPlayer> Players
        {
            get
            {
                List<NWPlayer> players = new List<NWPlayer>();

                NWPlayer player = NWPlayer.Wrap(_.GetFirstPC());
                while (player.IsValid)
                {
                    players.Add(player);
                    player = NWPlayer.Wrap(_.GetNextPC());
                }

                return players;
            }
        }

        public List<NWArea> Areas
        {
            get
            {
                List<NWArea> areas = new List<NWArea>();

                NWArea area = NWArea.Wrap(_.GetFirstArea());
                while (area.IsValid)
                {
                    areas.Add(area);
                    area = NWArea.Wrap(_.GetNextArea());
                }

                return areas;
            }
        }


        public static implicit operator Object(NWModule o)
        {
            return o.Object;
        }
    }
}
