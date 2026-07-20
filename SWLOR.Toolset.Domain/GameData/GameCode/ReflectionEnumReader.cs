using System.Reflection;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Toolset.Domain.GameData.GameCode
{
    /// <summary>
    /// Reads enum value -> display name maps from SWLOR.Game.Server enums via direct reflection
    /// over the compile-time referenced types (SWLOR.Toolset.Domain already has a ProjectReference
    /// to SWLOR.Game.Server). This is safe because:
    ///   - enums have no static constructors, so touching <c>typeof(NPCGroupType)</c>/
    ///     <c>typeof(KeyItemType)</c> cannot run arbitrary game/native initialization code;
    ///   - <see cref="NPCGroupAttribute"/> and <see cref="KeyItemAttribute"/> are plain
    ///     data-holding <see cref="Attribute"/> subclasses with no static state and no references
    ///     to NWN native types;
    ///   - no definition classes (which do carry static state tied to NWN natives) are touched.
    /// </summary>
    internal static class ReflectionEnumReader
    {
        /// <summary>Reads <c>NPCGroupType</c> values to their <c>[NPCGroup]</c> display names.</summary>
        public static IReadOnlyDictionary<int, string> ReadNpcGroups()
        {
            var result = new Dictionary<int, string>();

            foreach (NPCGroupType value in Enum.GetValues<NPCGroupType>())
            {
                var field = typeof(NPCGroupType).GetField(value.ToString());
                var attribute = field?.GetCustomAttribute<NPCGroupAttribute>();
                if (attribute == null)
                    continue;

                result[(int)value] = attribute.Name;
            }

            return result;
        }

        /// <summary>Reads <c>KeyItemType</c> values to their <c>[KeyItem]</c> display names.</summary>
        public static IReadOnlyDictionary<int, string> ReadKeyItems()
        {
            var result = new Dictionary<int, string>();

            foreach (KeyItemType value in Enum.GetValues<KeyItemType>())
            {
                var field = typeof(KeyItemType).GetField(value.ToString());
                var attribute = field?.GetCustomAttribute<KeyItemAttribute>();
                if (attribute == null)
                    continue;

                result[(int)value] = attribute.Name;
            }

            return result;
        }
    }
}
