using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.ImplantService;

namespace SWLOR.Game.Server.Service
{
    public static class Implant
    {
        private static readonly Dictionary<string, ImplantDetail> _implants = new Dictionary<string, ImplantDetail>();

        /// <summary>
        /// When the module loads, all implant details are loaded into the cache.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void CacheData()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IImplantListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IImplantListDefinition)Activator.CreateInstance(type);
                var items = instance.BuildImplants();

                foreach (var (itemTag, implantDetail) in items)
                {
                    _implants[itemTag] = implantDetail;
                }
            }

            Console.WriteLine($"Loaded {_implants.Count} implants.");
        }

    }
}
