namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Builds made content (definitions, view models, menus, status effects, payloads, engine-test classes) with its services from
    /// the container and its values from the caller. Never builds or returns a registered type: services and content singletons
    /// come from the container through constructors.
    /// </summary>
    /// <remarks>
    /// A type is built through its public constructor whose leading parameters are registered types and whose next parameters are
    /// exactly the value types of the call, with any parameters after them optional and filled with their declared defaults; when
    /// several match, the one with the fewest parameters wins, as C# picks. Each signature compiles one delegate on first use, so a
    /// call allocates the object and nothing else. Content code names the factory <c>Content</c>, a service <c>_content</c>.
    /// </remarks>
    public interface IContentFactory
    {
        /// <summary>Builds <typeparamref name="T"/> through its values-free constructor.</summary>
        T Create<T>() where T : class;

        /// <summary>Builds <paramref name="type"/>, which must be assignable to <typeparamref name="T"/>, through its values-free constructor.</summary>
        T Create<T>(Type type) where T : class;

        /// <summary>Builds <typeparamref name="T"/> with one value.</summary>
        T Create<T, A1>(A1 a1) where T : class;

        /// <summary>Builds <typeparamref name="T"/> with two values.</summary>
        T Create<T, A1, A2>(A1 a1, A2 a2) where T : class;

        /// <summary>Builds <typeparamref name="T"/> with three values.</summary>
        T Create<T, A1, A2, A3>(A1 a1, A2 a2, A3 a3) where T : class;

        /// <summary>Builds <typeparamref name="T"/> with four values.</summary>
        T Create<T, A1, A2, A3, A4>(A1 a1, A2 a2, A3 a3, A4 a4) where T : class;

        /// <summary>Builds <typeparamref name="T"/> with five values.</summary>
        T Create<T, A1, A2, A3, A4, A5>(A1 a1, A2 a2, A3 a3, A4 a4, A5 a5) where T : class;

        /// <summary>Builds <typeparamref name="T"/> with six values, the most any made type takes.</summary>
        T Create<T, A1, A2, A3, A4, A5, A6>(A1 a1, A2 a2, A3 a3, A4 a4, A5 a5, A6 a6) where T : class;
    }
}
