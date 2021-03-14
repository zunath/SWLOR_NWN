using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.ItemModService;

namespace SWLOR.Game.Server.Service
{
    public static class ItemMod
    {
        private static readonly Dictionary<string, ItemModDetail> _itemMods = new Dictionary<string, ItemModDetail>();

        /// <summary>
        /// When the module loads, all item mod details are loaded into the cache.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void CacheData()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IItemModListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IItemModListDefinition)Activator.CreateInstance(type);
                var items = instance.BuildItemMods();

                foreach (var (itemTag, itemDetail) in items)
                {
                    _itemMods[itemTag] = itemDetail;
                }
            }

            Console.WriteLine($"Loaded {_itemMods.Count} item mods.");
        }

        /// <summary>
        /// Checks whether an item mod is registered by the specified key.
        /// </summary>
        /// <param name="key">The key of the item mod.</param>
        /// <returns>true if registered, false otherwise</returns>
        public static bool IsRegistered(string key)
        {
            return _itemMods.ContainsKey(key);
        }

        /// <summary>
        /// Retrieves an item mod detail by its key.
        /// If key is not registered, an exception will be thrown.
        /// </summary>
        /// <param name="key">The key of the item mod.</param>
        /// <returns>The item mod detail</returns>
        public static ItemModDetail GetByKey(string key)
        {
            return _itemMods[key];
        }
    }
}
