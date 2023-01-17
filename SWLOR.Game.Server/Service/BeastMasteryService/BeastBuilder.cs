using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    public class BeastBuilder
    {
        private readonly Dictionary<BeastType, BeastDetail> _beasts = new();


        public Dictionary<BeastType, BeastDetail> Build()
        {
            return _beasts;
        }

    }
}
