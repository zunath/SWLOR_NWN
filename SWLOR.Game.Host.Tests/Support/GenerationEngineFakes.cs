using System.Reflection;
using System.Runtime.Loader;

namespace SWLOR.Game.Host.Tests.Support;

/// <summary>
/// Puts the engine fakes of <c>SWLOR.NWN.API.Testing</c> inside each game generation, so the real game build boots and
/// runs its handlers in a test process with no engine. Each generation loads its own <c>SWLOR.NWN.API</c>, so the fakes
/// are loaded into the generation's load context too (from bytes, beside its <c>SWLOR.NWN.API</c>), and every engine seam
/// of that copy (<c>NWScriptRuntime</c> and one runtime per NWNX plugin) is overridden with its fake.
/// </summary>
/// <remarks>
/// The seams keep an override in an <see cref="AsyncLocal{T}"/>, so <see cref="Sync"/> runs on the engine thread before
/// every engine-thread operation (<see cref="FakeInstallingBindings"/>), outside any async method, which leaves the
/// overrides on that thread for the generation's activation, scripts and frames. A generation's overrides come off,
/// on the same thread, at the first operation after its load context starts unloading, so neither the engine thread's
/// execution context nor this class keeps a reference into a retired generation. Only weak references to the contexts
/// are kept.
/// </remarks>
internal sealed class GenerationEngineFakes
{
    private const string GameAssemblyName = "SWLOR.Game.Server";

    private readonly byte[] _fakesAssembly;
    private readonly string _contextNamePrefix;
    private readonly HashSet<string> _earlierContexts;
    private readonly List<Generation> _generations = [];
    private readonly object _gate = new();

    /// <param name="fakesAssemblyPath">The <c>SWLOR.NWN.API.Testing</c> assembly.</param>
    /// <param name="contextNamePrefix">The prefix of the generations' load context names.</param>
    public GenerationEngineFakes(string fakesAssemblyPath, string contextNamePrefix)
    {
        _fakesAssembly = File.ReadAllBytes(fakesAssemblyPath);
        _contextNamePrefix = contextNamePrefix;
        // Contexts with the same prefix that other tests of the process left behind (their fixture builds) are not ours.
        _earlierContexts = AssemblyLoadContext.All.Select(context => context.Name).OfType<string>().ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>The load context of every generation seen so far, by weak reference, in load order.</summary>
    public IReadOnlyList<WeakReference> Contexts
    {
        get
        {
            lock (_gate)
                return _generations.Select(generation => generation.Context).ToArray();
        }
    }

    /// <summary>The engine seams overridden in each generation (the same number every time).</summary>
    public int SeamsPerGeneration { get; private set; }

    /// <summary>Overrides the seams of every new generation and takes them off every generation that is unloading.</summary>
    public void Sync()
    {
        lock (_gate)
        {
            InstallNew();
            RemoveUnloading();
        }
    }

    private void InstallNew()
    {
        foreach (var context in AssemblyLoadContext.All)
        {
            if (context.Name is not { } name || !name.StartsWith(_contextNamePrefix, StringComparison.Ordinal) ||
                _earlierContexts.Contains(name) || _generations.Any(generation => generation.Name == name) ||
                !context.Assemblies.Any(assembly => assembly.GetName().Name == GameAssemblyName))
            {
                continue;
            }

            var generation = new Generation(name, new WeakReference(context));
            context.Unloading += _ => generation.Unloading = true;
            generation.Overrides = Install(context);
            _generations.Add(generation);
        }
    }

    private void RemoveUnloading()
    {
        foreach (var generation in _generations.Where(generation => generation.Unloading && generation.Overrides != null))
        {
            // Each seam has its own AsyncLocal, so the order does not matter; reverse keeps it tidy.
            for (var index = generation.Overrides!.Count - 1; index >= 0; index--)
                generation.Overrides[index].Dispose();
            generation.Overrides = null;
        }
    }

    private List<IDisposable> Install(AssemblyLoadContext context)
    {
        var api = context.LoadFromAssemblyName(new AssemblyName("SWLOR.NWN.API"));
        var fakes = context.LoadFromStream(new MemoryStream(_fakesAssembly));
        var fakeTypes = fakes.GetTypes().Where(type => type is { IsClass: true, IsAbstract: false }).ToArray();

        var overrides = new List<IDisposable>();
        foreach (var runtime in api.GetTypes().Where(type => type is { IsClass: true, IsAbstract: true, IsSealed: true }))
        {
            var overrideSeam = runtime.GetMethod("Override", BindingFlags.Public | BindingFlags.Static);
            if (overrideSeam == null || overrideSeam.GetParameters() is not [var parameter] || overrideSeam.ReturnType != typeof(IDisposable))
                continue;

            var fakeType = fakeTypes.SingleOrDefault(type => parameter.ParameterType.IsAssignableFrom(type))
                           ?? throw new InvalidOperationException($"SWLOR.NWN.API.Testing has no fake for {runtime.FullName} ({parameter.ParameterType.Name}).");
            overrides.Add((IDisposable)overrideSeam.Invoke(null, [Activator.CreateInstance(fakeType)])!);
        }

        if (overrides.Count == 0)
            throw new InvalidOperationException("SWLOR.NWN.API has no engine seam to override.");
        if (SeamsPerGeneration != 0 && SeamsPerGeneration != overrides.Count)
            throw new InvalidOperationException($"A generation has {overrides.Count} engine seams, an earlier one {SeamsPerGeneration}.");

        SeamsPerGeneration = overrides.Count;
        return overrides;
    }

    private sealed class Generation(string name, WeakReference context)
    {
        public string Name { get; } = name;
        public WeakReference Context { get; } = context;
        public volatile bool Unloading;
        public List<IDisposable>? Overrides { get; set; }
    }
}
