using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// The digests a hot reload is checked with (MaintainabilityRefactorHotReload.md, P4-14), each a count and a content hash
    /// (<see cref="ReloadDigestHasher"/>). Cache: one per class that owns a rerun boot handler, over its fields except those marked
    /// <see cref="ReloadStateAttribute"/>; after a reload each must equal the boot's. Adopted: one per class with fields marked
    /// <see cref="ReloadStateKind.Adopted"/>, over those fields; after a reload each must equal the old build's at quiescence.
    /// The world digests (<see cref="WorldDigest"/>) complete them. The server logs them only when hot reload is on;
    /// <c>scripts/run-hot-reload-check.sh</c> compares them (<see cref="Compare"/> is the same rule).
    /// </summary>
    public static class ReloadDigests
    {
        public const string CacheKind = "cache";
        public const string AdoptedKind = "adopted";
        public const string WorldKind = "world";

        /// <summary>
        /// One cache digest per class that declares a rerun handler of the boot scripts, in the order of its first handler, over its
        /// static fields and, for an instance handler, the instance's fields, without the fields marked
        /// <see cref="ReloadStateAttribute"/>.
        /// </summary>
        public static IReadOnlyList<ReloadDigestLine> Cache(ScriptRegistry registry)
        {
            var owners = ReloadBootstrap.Plan(registry)
                .Where(step => step.Kind == ReloadStepKind.Rerun)
                .Select(step => (Type: step.Method.DeclaringType, Instance: step.Run.Target))
                .Where(owner => owner.Type != null)
                .GroupBy(owner => owner.Type)
                .Select(group => group.First())
                .ToArray();

            var hasher = new ReloadDigestHasher(typeof(ReloadDigests).Assembly);
            return owners
                .Select(owner => hasher.Describe(CacheKind, owner.Type.FullName, owner.Type, owner.Instance,
                    field => ReloadDigestHasher.StateOf(field) == null))
                .ToArray();
        }

        /// <summary>
        /// One adopted digest per class of the game assembly that has fields marked <see cref="ReloadStateKind.Adopted"/>, over
        /// those fields, in the order of the class names. An instance field is read from <paramref name="resolveOwner"/>'s instance
        /// of the class (the container's singleton); a class it cannot resolve gets a digest that never matches.
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
                var adopted = type.GetFields(flags).Where(field => ReloadDigestHasher.StateOf(field) == ReloadStateKind.Adopted).ToArray();
                if (adopted.Length == 0)
                    continue;

                object instance = null;
                if (adopted.Any(field => !field.IsStatic))
                {
                    instance = resolveOwner(type);
                    if (instance == null)
                    {
                        lines.Add(new ReloadDigestLine(AdoptedKind, type.FullName, -1, "unresolved"));
                        continue;
                    }
                }

                lines.Add(hasher.Describe(AdoptedKind, type.FullName, type, instance,
                    field => field.DeclaringType == type && ReloadDigestHasher.StateOf(field) == ReloadStateKind.Adopted));
            }

            return lines;
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
