using SWLOR.Game.Server.Core.NWScript.Enum.Item;

// ReSharper disable once CheckNamespace
namespace SWLOR.Game.Server.Core.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Looks for a specific type of item property, gets the value, removes the item property and returns the result.
        /// This can be useful for converting an item property into a local variable.
        /// Returns -1 if item property couldn't be found.
        /// </summary>
        /// <param name="item">The item to check and process.</param>
        /// <param name="type">The type of item property to look for.</param>
        /// <returns>The cost table value of the item property or -1 if it could not be found.</returns>
        public static int GetItemPropertyValueAndRemove(uint item, ItemPropertyType type)
        {
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == type)
                {
                    var result = GetItemPropertyCostTableValue(ip);
                    RemoveItemProperty(item, ip);
                    return result;
                }
            }

            return -1;
        }
    }
}
