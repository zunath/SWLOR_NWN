using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Service
{
    public static class Cached2DAService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(a => Register2DAs());
        }

        private static void Register2DAs()
        {
            RegisterImmunityCosts();
        }

        public static Dictionary<int, int> ImmunityCosts { get; private set; }
        private static void RegisterImmunityCosts()
        {
            ImmunityCosts= new Dictionary<int, int>();

            const string File = "iprp_immuncost";

            int NumberOfRows = NWNXUtil.Get2DARowCount(File);
            for (int x = 0; x <= NumberOfRows; x++)
            {
                string value = _.Get2DAString(File, "Value", x);
                if (string.IsNullOrWhiteSpace(value) || value == "0") continue; // Ignore empty / zero values.

                int valueNumber = Convert.ToInt32(value);
                if (ImmunityCosts.Values.Contains(valueNumber)) continue; // Ignore duplicate values.

                ImmunityCosts.Add(x, valueNumber);
            }
        }
    }
}
