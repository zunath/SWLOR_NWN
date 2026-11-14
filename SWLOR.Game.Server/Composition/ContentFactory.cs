using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// The container's <see cref="IContentFactory"/> (MaintainabilityRefactorContentInjection.md, "Made content"). For each made type
    /// and argument signature it picks the constructor on first use, resolves the services once and compiles a delegate with them as
    /// constants, kept in a static slot of that signature (<see cref="ContentSlot{TDelegate}"/>), so a call costs one reference check
    /// and one delegate call and allocates the object only.
    /// </summary>
    /// <remarks>
    /// It builds only made content: a class of the game's content namespace (<see cref="ContentNamespace"/>) or of a test assembly
    /// (one whose name ends in <c>Tests</c>: engine tests, unit tests), never an abstract type, a type of another assembly or a type
    /// the container registers (a service or a content singleton). Its constructor only stores what it is given (HotReload.md rule
    /// 9); the container builds it with every other singleton.
    /// </remarks>
    public sealed class ContentFactory : IContentFactory
    {
        /// <summary>The namespace of the game's content. A type of the game build outside it is not made content.</summary>
        public const string ContentNamespace = "SWLOR.Game.Server.Feature";

        private readonly Func<Type, bool> _isRegistered;
        private readonly Func<Type, object> _resolve;
        private readonly ConcurrentDictionary<Type, Delegate> _compiled = new();
        private readonly ConcurrentDictionary<Type, Func<object>> _byType = new();

        /// <summary>The factory of <paramref name="provider"/>: services resolve from it, and its registrations are never built.</summary>
        public ContentFactory(IServiceProvider provider)
            : this(RegisteredTypes.Of(provider), (provider ?? throw new ArgumentNullException(nameof(provider))).GetRequiredService)
        {
        }

        private ContentFactory(Func<Type, bool> isRegistered, Func<Type, object> resolve)
        {
            _isRegistered = isRegistered;
            _resolve = resolve;
        }

        /// <summary>
        /// A factory that passes null for every service parameter, for unit tests without a container: the object behaves as it did
        /// before it took services, and a service call fails with a null reference. <paramref name="isRegistered"/> names the service
        /// parameters (the server's registrations, read without building a container).
        /// </summary>
        public static ContentFactory WithoutServices(Func<Type, bool> isRegistered)
        {
            return new ContentFactory(isRegistered ?? throw new ArgumentNullException(nameof(isRegistered)), _ => null);
        }

        /// <summary><see cref="WithoutServices(Func{Type, bool})"/> naming the services <paramref name="services"/> registers.</summary>
        public static ContentFactory WithoutServices(IServiceCollection services)
        {
            return WithoutServices(RegisteredTypes.Of(services));
        }

        /// <summary>True when <paramref name="type"/> is a service or content singleton of this factory's container.</summary>
        public bool IsRegistered(Type type)
        {
            return _isRegistered(type);
        }

        public T Create<T>() where T : class => Slot<Func<T>>()();

        public T Create<T>(Type type) where T : class
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (!typeof(T).IsAssignableFrom(type))
                throw new InvalidOperationException($"The content factory cannot build {type.FullName} as a {typeof(T).FullName}: it is not one.");

            if (!_byType.TryGetValue(type, out var create))
                create = _byType.GetOrAdd(type, static (made, factory) => factory.CompileByType(made), this);

            return (T)create();
        }

        public T Create<T, A1>(A1 a1) where T : class => Slot<Func<A1, T>>()(a1);

        public T Create<T, A1, A2>(A1 a1, A2 a2) where T : class => Slot<Func<A1, A2, T>>()(a1, a2);

        public T Create<T, A1, A2, A3>(A1 a1, A2 a2, A3 a3) where T : class => Slot<Func<A1, A2, A3, T>>()(a1, a2, a3);

        public T Create<T, A1, A2, A3, A4>(A1 a1, A2 a2, A3 a3, A4 a4) where T : class =>
            Slot<Func<A1, A2, A3, A4, T>>()(a1, a2, a3, a4);

        public T Create<T, A1, A2, A3, A4, A5>(A1 a1, A2 a2, A3 a3, A4 a4, A5 a5) where T : class =>
            Slot<Func<A1, A2, A3, A4, A5, T>>()(a1, a2, a3, a4, a5);

        public T Create<T, A1, A2, A3, A4, A5, A6>(A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6) where T : class =>
            Slot<Func<A1, A2, A3, A4, A5, A6, T>>()(a1, a2, a3, a4, a5, a6);

        /// <summary>
        /// The content check of the reload validation (HotReload.md rule 9), which builds nothing: every made type among
        /// <paramref name="types"/> (a class this factory may build, abstract bases included) takes its registered types before its
        /// values in each public constructor, and every <paramref name="signatures"/> entry resolves to exactly one constructor.
        /// </summary>
        /// <returns>One line per problem; empty when the content is sound.</returns>
        public IReadOnlyList<string> Check(IEnumerable<Type> types, IEnumerable<ContentSignature> signatures)
        {
            if (types == null)
                throw new ArgumentNullException(nameof(types));
            if (signatures == null)
                throw new ArgumentNullException(nameof(signatures));

            var problems = new List<string>();
            foreach (var type in types.Where(type => type.IsClass && !type.IsGenericTypeDefinition && InContentScope(type) && !_isRegistered(type)))
            {
                foreach (var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
                {
                    var parameters = constructor.GetParameters();
                    var firstValue = Array.FindIndex(parameters, parameter => !_isRegistered(parameter.ParameterType));
                    if (firstValue >= 0 && parameters.Skip(firstValue).Any(parameter => _isRegistered(parameter.ParameterType)))
                        problems.Add($"{Describe(constructor)} takes a registered type after a value: services come first.");
                }
            }

            foreach (var signature in signatures)
            {
                var problem = WhyNotContent(signature.Made) ?? Match(signature.Made, signature.Values.ToArray(), out _);
                if (problem != null)
                    problems.Add($"{signature}: {problem}");
            }

            return problems;
        }

        private TDelegate Slot<TDelegate>() where TDelegate : Delegate
        {
            return ContentSlot<TDelegate>.Get(this) ?? Refill<TDelegate>();
        }

        private TDelegate Refill<TDelegate>() where TDelegate : Delegate
        {
            if (!_compiled.TryGetValue(typeof(TDelegate), out var compiled))
                compiled = _compiled.GetOrAdd(typeof(TDelegate), static (_, factory) => factory.CompileSignature<TDelegate>(), this);

            var create = (TDelegate)compiled;
            ContentSlot<TDelegate>.Set(this, create);
            return create;
        }

        private TDelegate CompileSignature<TDelegate>() where TDelegate : Delegate
        {
            var arguments = typeof(TDelegate).GetGenericArguments();
            var made = arguments[^1];
            var values = arguments[..^1].Select(Expression.Parameter).ToArray();
            var body = Build(made, values);
            return Expression.Lambda<TDelegate>(body, values).Compile();
        }

        private Func<object> CompileByType(Type made)
        {
            var body = Build(made, Array.Empty<ParameterExpression>());
            return Expression.Lambda<Func<object>>(Expression.Convert(body, typeof(object))).Compile();
        }

        /// <summary>The construction of <paramref name="made"/> with the services resolved now and <paramref name="values"/> passed through.</summary>
        private NewExpression Build(Type made, ParameterExpression[] values)
        {
            ConstructorInfo constructor = null;
            var problem = WhyNotContent(made) ?? Match(made, values.Select(value => value.Type).ToArray(), out constructor);
            if (problem != null)
                throw new InvalidOperationException($"The content factory cannot build {made.FullName}: {problem}");

            var parameters = constructor.GetParameters();
            var services = LeadingServices(parameters);
            var arguments = new List<Expression>(parameters.Length);
            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                if (index < services)
                    arguments.Add(Expression.Constant(_resolve(parameter.ParameterType), parameter.ParameterType));
                else if (index < services + values.Length)
                    arguments.Add(values[index - services]);
                else
                    arguments.Add(DefaultOf(parameter));
            }

            return Expression.New(constructor, arguments);
        }

        /// <summary>
        /// The constructor of <paramref name="made"/> for a call with <paramref name="values"/>: the public constructor whose leading
        /// registered parameters are followed by exactly the value types and then only optional parameters; the fewest parameters win.
        /// </summary>
        /// <returns>Null when one constructor matches, otherwise why not.</returns>
        private string Match(Type made, Type[] values, out ConstructorInfo constructor)
        {
            constructor = null;
            var matches = made.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Where(candidate => Fits(candidate.GetParameters(), values))
                .GroupBy(candidate => candidate.GetParameters().Length)
                .OrderBy(group => group.Key)
                .FirstOrDefault()
                ?.ToArray();

            var shape = values.Length == 0 ? "no values" : "the values (" + string.Join(", ", values.Select(value => value.Name)) + ")";
            if (matches == null)
                return $"no public constructor takes registered types followed by {shape}.";
            if (matches.Length > 1)
                return $"{matches.Length} public constructors take registered types followed by {shape} with as many parameters: " +
                       string.Join("; ", matches.Select(Describe)) + ".";

            constructor = matches[0];
            return null;
        }

        private bool Fits(ParameterInfo[] parameters, Type[] values)
        {
            var services = LeadingServices(parameters);
            if (parameters.Length < services + values.Length)
                return false;

            for (var index = 0; index < values.Length; index++)
            {
                if (parameters[services + index].ParameterType != values[index])
                    return false;
            }

            return parameters.Skip(services + values.Length).All(parameter => parameter.IsOptional);
        }

        private int LeadingServices(ParameterInfo[] parameters)
        {
            var count = 0;
            while (count < parameters.Length && _isRegistered(parameters[count].ParameterType))
                count++;
            return count;
        }

        /// <summary>Why the factory never builds <paramref name="type"/>, or null when it is made content.</summary>
        private string WhyNotContent(Type type)
        {
            if (type.IsInterface || type.IsAbstract)
                return "it is abstract.";
            if (!type.IsClass || type.ContainsGenericParameters)
                return "it is not a class the factory can build.";
            if (!InContentScope(type))
                return type.Assembly == typeof(ContentFactory).Assembly
                    ? $"it is not content (only types of {ContentNamespace} and test assemblies are made content)."
                    : "it is neither a type of the game build nor of a test assembly.";
            if (_isRegistered(type))
                return "the container registers it (a service or content singleton comes from the container, never from the factory).";
            return null;
        }

        private static bool InContentScope(Type type)
        {
            var assembly = type.Assembly;
            if (assembly == typeof(ContentFactory).Assembly)
            {
                var name = type.Namespace ?? string.Empty;
                return name == ContentNamespace || name.StartsWith(ContentNamespace + ".", StringComparison.Ordinal);
            }

            return assembly.GetName().Name?.EndsWith("Tests", StringComparison.Ordinal) == true;
        }

        private static Expression DefaultOf(ParameterInfo parameter)
        {
            var type = parameter.ParameterType;
            var value = parameter.HasDefaultValue ? parameter.DefaultValue : null;
            if (value == null || value is DBNull || value is Missing)
                return Expression.Default(type);

            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            if (underlying.IsEnum && value.GetType() != underlying)
                value = Enum.ToObject(underlying, value);

            var constant = Expression.Constant(value, value.GetType());
            return constant.Type == type ? constant : Expression.Convert(constant, type);
        }

        private static string Describe(ConstructorInfo constructor)
        {
            return $"{constructor.DeclaringType?.Name}({string.Join(", ", constructor.GetParameters().Select(parameter => parameter.ParameterType.Name))})";
        }
    }
}
