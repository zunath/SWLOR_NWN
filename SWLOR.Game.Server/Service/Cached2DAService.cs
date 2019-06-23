using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;

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

            // There's no way at the time of writing to identify the difference between end of file
            // and an empty value. For this reason we have to hard code the row number. If someone's got
            // a better solution, please implement it.
            const int NumberOfRows = 150;
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
