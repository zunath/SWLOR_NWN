using System.Reflection;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.SkillService;

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

        /// <summary>Reads <c>FactionType</c> values to their <c>[Faction]</c> display names.</summary>
        /// <remarks>
        /// The faction snippets take a faction id as a number, so an editor needs the value-to-name
        /// map to offer "Czerka Corporation" where the file says 7.
        /// </remarks>
        public static IReadOnlyDictionary<int, string> ReadFactions()
        {
            var result = new Dictionary<int, string>();

            foreach (var value in Enum.GetValues<FactionType>())
            {
                var field = typeof(FactionType).GetField(value.ToString());
                var attribute = field?.GetCustomAttribute<FactionAttribute>();
                if (attribute == null)
                    continue;

                result[(int)value] = attribute.Name;
            }

            return result;
        }

        /// <summary>Reads <c>SkillType</c> values to their <c>[Skill]</c> display names.</summary>
        /// <remarks>
        /// Skills are accepted by name or by number in a conversation, so this map serves both
        /// directions: it labels a stored number, and it validates a stored name.
        /// </remarks>
        public static IReadOnlyDictionary<int, string> ReadSkills()
        {
            var result = new Dictionary<int, string>();

            foreach (var value in Enum.GetValues<SkillType>())
            {
                var field = typeof(SkillType).GetField(value.ToString());
                var attribute = field?.GetCustomAttribute<SkillAttribute>();
                if (attribute == null)
                    continue;

                result[(int)value] = attribute.Name;
            }

            return result;
        }

        /// <summary>Reads <c>SkillType</c> enum member names, for the by-name form.</summary>
        public static IReadOnlyDictionary<int, string> ReadSkillEnumNames()
        {
            var result = new Dictionary<int, string>();

            foreach (var value in Enum.GetValues<SkillType>())
                result[(int)value] = value.ToString();

            return result;
        }
    }
}
