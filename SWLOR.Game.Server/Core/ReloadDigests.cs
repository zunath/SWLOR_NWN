using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The digests a hot reload is checked with (MaintainabilityRefactorHotReload.md, P4-14), each a count and a content hash
    /// (<see cref="ReloadDigestHasher"/>) of one field, named <c>Namespace.Class.field</c>. Cache: every field of each class that
    /// owns a rerun boot handler or a handler of a script those run, except the fields marked <see cref="ReloadStateAttribute"/>
    /// and those that hold another service; after a reload each must equal the boot's. Adopted: every field marked
    /// <see cref="ReloadStateKind.Adopted"/> or <see cref="ReloadStateKind.Carried"/>; after a reload each must equal the old build's at
    /// quiescence. The world digests
    /// (<see cref="WorldDigest"/>) complete them. The server logs them only when hot reload is on;
    /// <c>scripts/run-hot-reload-check.sh</c> compares them (<see cref="Compare"/> is the same rule).
    /// </summary>
    public static class ReloadDigests
    {
        public const string CacheKind = "cache";
        public const string AdoptedKind = "adopted";
        public const string WorldKind = "world";

        /// <summary>
        /// The cache digests: for each class that declares a rerun handler of the boot scripts or a handler of a script those
        /// handlers run (<see cref="ReloadBootstrap.NestedScripts"/>), in the order of its first handler, one digest per static
        /// field and, for an instance handler, per field of the instance, without the fields marked
        /// <see cref="ReloadStateAttribute"/> and those that hold another service.
        /// </summary>
        public static IReadOnlyList<ReloadDigestLine> Cache(ScriptRegistry registry)
        {
            var nested = ReloadBootstrap.NestedScripts
                .SelectMany(script => registry.GetActionScripts(script))
                .Select(handler => (Type: handler.Action.Method.DeclaringType, Instance: handler.Action.Target));
            var owners = ReloadBootstrap.Plan(registry)
                .Where(step => step.Kind == ReloadStepKind.Rerun)
                .Select(step => (Type: step.Method.DeclaringType, Instance: step.Run.Target))
                .Concat(nested)
                .Where(owner => owner.Type != null)
                .GroupBy(owner => owner.Type)
                .Select(group => group.First())
                .ToArray();

            var hasher = new ReloadDigestHasher(typeof(ReloadDigests).Assembly);
            var lines = new List<ReloadDigestLine>();
            foreach (var (type, instance) in owners)
                lines.AddRange(DescribeFields(hasher, CacheKind, type, instance, field => ReloadDigestHasher.StateOf(field) == null));

            return lines;
        }

        /// <summary>
        /// The adopted digests: one per field marked <see cref="ReloadStateKind.Adopted"/> or <see cref="ReloadStateKind.Carried"/> of
        /// the classes of the game assembly, in
        /// the order of the class names. An instance field is read from <paramref name="resolveOwner"/>'s instance of the class (the
        /// container's singleton); a class it cannot resolve gets one digest that never matches.
        /// </summary>
        public static IReadOnlyList<ReloadDigestLine> Adopted(Func<Type, object> resolveOwner)
        {
            return Adopted(typeof(ReloadDigests).Assembly, resolveOwner);
        }

        /// <summary><see cref="Adopted(Func{Type, object})"/> over the classes of <paramref name="assembly"/>.</summary>
        public static IReadOnlyList<ReloadDigestLine> Adopted(Assembly assembly, Func<Type, object> resolveOwner)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            if (resolveOwner == null)
                throw new ArgumentNullException(nameof(resolveOwner));

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            var hasher = new ReloadDigestHasher(assembly);
            var lines = new List<ReloadDigestLine>();
            foreach (var type in assembly.GetTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
            {
                var adopted = type.GetFields(flags).Where(field => IsComparedAtQuiescence(ReloadDigestHasher.StateOf(field))).ToArray();
                if (adopted.Length == 0)
                    continue;

                object instance = null;
                if (adopted.Any(field => !field.IsStatic))
                {
                    instance = resolveOwner(type);
                    if (instance == null)
                    {
                        lines.Add(new ReloadDigestLine(AdoptedKind, type.FullName, -1, ReloadDigestLine.UnresolvedHash));
                        continue;
                    }
                }

                lines.AddRange(DescribeFields(hasher, AdoptedKind, type, instance,
                    field => field.DeclaringType == type && IsComparedAtQuiescence(ReloadDigestHasher.StateOf(field))));
            }

            return lines;
        }

        /// <summary>The kinds whose digest after a reload must equal the old build's at quiescence: adopted and carried state.</summary>
        public static bool IsComparedAtQuiescence(ReloadStateKind? kind)
        {
            return kind is ReloadStateKind.Adopted or ReloadStateKind.Carried;
        }

        /// <summary>
        /// The digest of each field of <paramref name="type"/> that <paramref name="include"/> selects, skipping those that hold
        /// another service. A field whose value cannot be read (a static initializer that throws, say) gets a digest that never
        /// matches (<see cref="ReloadDigestLine.Matches"/>) naming the exception, so it cannot hide the other fields' digests.
        /// </summary>
        private static IEnumerable<ReloadDigestLine> DescribeFields(ReloadDigestHasher hasher, string kind, Type type, object instance,
            Func<FieldInfo, bool> include)
        {
            List<FieldInfo> fields;
            try
            {
                fields = ReloadDigestHasher.RootFields(type, instance)
                    .Where(field => include(field) && !ReloadDigestHasher.IsDependency(field.FieldType))
                    .ToList();
            }
            catch (Exception exception)
            {
                return new[] { Failed(kind, type.FullName, exception) };
            }

            var lines = new List<ReloadDigestLine>(fields.Count);
            foreach (var field in fields)
            {
                var name = FieldName(type, field);
                try
                {
                    lines.Add(hasher.Describe(kind, name, type, instance, candidate => candidate == field));
                }
                catch (Exception exception)
                {
                    lines.Add(Failed(kind, name, exception));
                }
            }

            return lines;
        }

        /// <summary>
        /// <c>Namespace.Class.field</c>, with an auto-property's backing field shown as the property and a field of a base class
        /// prefixed with that class's name.
        /// </summary>
        private static string FieldName(Type type, FieldInfo field)
        {
            var name = field.Name;
            if (name.StartsWith("<", StringComparison.Ordinal) && name.EndsWith(">k__BackingField", StringComparison.Ordinal))
                name = name.Substring(1, name.Length - "<>k__BackingField".Length);

            return field.DeclaringType == type || field.DeclaringType == null
                ? $"{type.FullName}.{name}"
                : $"{type.FullName}.{field.DeclaringType.Name}.{name}";
        }

        private static ReloadDigestLine Failed(string kind, string name, Exception exception)
        {
            return new ReloadDigestLine(kind, name, -1, ReloadDigestLine.ErrorHashPrefix + exception.GetBaseException().GetType().Name);
        }

        /// <summary>
        /// The differences between a later digest set and the one it must equal: every name of <paramref name="expected"/> that is
        /// missing from <paramref name="actual"/> or has another count or hash, and every name only <paramref name="actual"/> has.
        /// </summary>
        public static IReadOnlyList<string> Compare(IEnumerable<ReloadDigestLine> expected, IEnumerable<ReloadDigestLine> actual)
        {
            var before = expected.ToDictionary(line => (line.Kind, line.Name));
            var after = actual.ToDictionary(line => (line.Kind, line.Name));
            var differences = new List<string>();
            foreach (var (key, line) in before)
            {
                if (!after.TryGetValue(key, out var other))
                    differences.Add($"{key.Kind} {key.Name}: missing after the reload");
                else if (!line.Matches(other))
                    differences.Add($"{key.Kind} {key.Name}: count {line.Count} hash {line.Hash} before, count {other.Count} hash {other.Hash} after");
            }

            differences.AddRange(after.Keys.Where(key => !before.ContainsKey(key)).Select(key => $"{key.Kind} {key.Name}: only after the reload"));
            return differences;
        }
    }
}
