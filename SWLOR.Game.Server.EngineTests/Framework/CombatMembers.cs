using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.EngineTests.Framework
{
    /// <summary>
    /// Finds the private members of <c>Combat</c> that tests reach by reflection (swing debts, activity records, trigger
    /// cooldowns, private helpers). They were static members of <see cref="Combat"/>; as the code moves into the part
    /// services (see <see cref="CombatParts"/>) they become instance members of a part, and only the lookup here changes:
    /// a static member is found first, then an instance member of a part, whose value or call goes to the part's instance
    /// in the container the facades resolve from (the server's, or the one a test installed).
    /// </summary>
    public static class CombatMembers
    {
        private const BindingFlags StaticMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags InstanceMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        /// <summary>
        /// The method of <c>Combat</c> (or of a part that holds it) with this name, or null when there is none: a static one
        /// first, then an instance method of a part service. <paramref name="parameterTypes"/> picks one overload; leave it
        /// out when the name is unique. Call it through <see cref="Invoke"/> or <see cref="CreateDelegate{TDelegate}"/>,
        /// which pass the part's instance.
        /// </summary>
        public static MethodInfo FindMethod(string name, params Type[] parameterTypes)
        {
            return Find(name, parameterTypes, StaticMembers) ?? Find(name, parameterTypes, InstanceMembers);
        }

        /// <summary>
        /// The field of <c>Combat</c> (or of a part that holds it) with this name, or null: a static one first, then an
        /// instance field of a part service.
        /// </summary>
        public static FieldInfo FindField(string name)
        {
            return Owners().Select(owner => owner.GetField(name, StaticMembers)).FirstOrDefault(field => field != null)
                   ?? Owners().Select(owner => owner.GetField(name, InstanceMembers)).FirstOrDefault(field => field != null);
        }

        /// <summary>The current value of a field such as <c>_attackSwingDebts</c> (of the part's instance for an instance field).</summary>
        public static TField FieldValue<TField>(string name)
        {
            var field = FindField(name) ?? throw new MissingFieldException(typeof(Combat).FullName, name);
            return (TField)field.GetValue(Target(field.IsStatic, field.DeclaringType));
        }

        /// <summary>Calls a method found by <see cref="FindMethod"/> (on the part's instance for an instance method) and returns its result.</summary>
        public static object Invoke(string name, params object[] arguments)
        {
            var method = FindMethod(name) ?? throw new MissingMethodException(typeof(Combat).FullName, name);
            return method.Invoke(Target(method.IsStatic, method.DeclaringType), arguments);
        }

        /// <summary>A delegate to a method found by <see cref="FindMethod"/>, bound to the part's instance for an instance method.</summary>
        public static TDelegate CreateDelegate<TDelegate>(string name, params Type[] parameterTypes)
            where TDelegate : Delegate
        {
            var method = FindMethod(name, parameterTypes) ?? throw new MissingMethodException(typeof(Combat).FullName, name);
            return method.IsStatic
                ? method.CreateDelegate<TDelegate>()
                : method.CreateDelegate<TDelegate>(Target(false, method.DeclaringType));
        }

        private static MethodInfo Find(string name, Type[] parameterTypes, BindingFlags flags)
        {
            foreach (var owner in Owners())
            {
                var method = parameterTypes.Length == 0
                    ? owner.GetMethod(name, flags)
                    : owner.GetMethod(name, flags, null, parameterTypes, null);
                if (method != null)
                    return method;
            }

            return null;
        }

        // The part's instance in the container the Combat facade resolves from, found by its interface I<Part>.
        private static object Target(bool isStatic, Type declaringType)
        {
            if (isStatic)
                return null;

            var contract = declaringType.GetInterface("I" + declaringType.Name)
                           ?? throw new InvalidOperationException($"{declaringType.Name} has no interface I{declaringType.Name}.");
            var accessor = typeof(CompositionRoot).Assembly.GetType("SWLOR.Game.Server.Composition.FacadeAccessor", throwOnError: true);
            var resolve = accessor.GetMethod("Resolve", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .MakeGenericMethod(contract);
            try
            {
                return resolve.Invoke(null, null);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                throw exception.InnerException;
            }
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
