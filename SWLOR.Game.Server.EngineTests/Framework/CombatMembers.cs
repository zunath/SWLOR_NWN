using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Finds the private members of <c>Combat</c> that tests reach by reflection (swing debts, activity records, trigger
    /// cooldowns, private helpers). Today they are static members of <see cref="Combat"/>; when the code moves into the
    /// part services (see <see cref="CombatParts"/>), only the lookup here changes, not the tests.
    /// </summary>
    public static class CombatMembers
    {
        private const BindingFlags StaticMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        /// <summary>
        /// The static method of <c>Combat</c> (or of a part that holds it) with this name, or null when there is none.
        /// <paramref name="parameterTypes"/> picks one overload; leave it out when the name is unique.
        /// </summary>
        public static MethodInfo FindMethod(string name, params Type[] parameterTypes)
        {
            foreach (var owner in Owners())
            {
                var method = parameterTypes.Length == 0
                    ? owner.GetMethod(name, StaticMembers)
                    : owner.GetMethod(name, StaticMembers, null, parameterTypes, null);
                if (method != null)
                    return method;
            }

            return null;
        }

        /// <summary>The static field of <c>Combat</c> (or of a part that holds it) with this name, or null.</summary>
        public static FieldInfo FindField(string name)
        {
            return Owners().Select(owner => owner.GetField(name, StaticMembers)).FirstOrDefault(field => field != null);
        }

        /// <summary>The current value of a static field such as <c>_attackSwingDebts</c>.</summary>
        public static TField FieldValue<TField>(string name)
        {
            var field = FindField(name) ?? throw new MissingFieldException(typeof(Combat).FullName, name);
            return (TField)field.GetValue(null);
        }

        /// <summary>Calls a static method found by <see cref="FindMethod"/> and returns its result.</summary>
        public static object Invoke(string name, params object[] arguments)
        {
            var method = FindMethod(name) ?? throw new MissingMethodException(typeof(Combat).FullName, name);
            return method.Invoke(null, arguments);
        }

        // Combat first, then the part types the split adds to the server assembly.
        private static IEnumerable<Type> Owners()
        {
            yield return typeof(Combat);
            foreach (var type in typeof(Combat).Assembly.GetTypes()
                         .Where(type => type.Namespace == typeof(CombatRecorderService).Namespace &&
                                        CombatParts.IsPartTypeName(type.Name))
                         .OrderBy(type => type.Name, StringComparer.Ordinal))
                yield return type;
        }
    }
}
