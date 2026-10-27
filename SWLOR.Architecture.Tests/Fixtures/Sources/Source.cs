using SWLOR.Architecture.Tests.Fixtures.Targets;

namespace SWLOR.Architecture.Tests.Fixtures.Sources;

/// <summary>
/// Reaches each target type through exactly one mechanism, so a scanner test can tell which mechanism found it.
/// The targets <c>ConstantTarget</c>, <c>NamedOnlyTarget</c> and <c>UnrelatedTarget</c> leave no IL reference.
/// </summary>
public static class Source
{
    public static void CallsMethod() => MethodTarget.Ping();

    public static int ReadsStaticField() => FieldTarget.Count;

    public static object Constructs() => new ConstructedTarget();

    public static bool Casts(object value) => value is CastTarget;

    public static Type TakesTypeof() => typeof(TypeofTarget);

    public static void TakesParameter(ParameterTarget parameter)
    {
    }

    public static ReturnTarget? Returns() => null;

    public static void UsesLocal()
    {
        LocalTarget? local = null;
        GC.KeepAlive(local);
    }

    public static void UsesGenericArgument() => GC.KeepAlive(new List<GenericArgumentTarget>());

    public static void UsesGenericMethod() => GC.KeepAlive(Enumerable.Empty<GenericMethodTarget>());

    public static void UsesArray() => GC.KeepAlive(new ArrayTarget[0]);

    public static int UsesLambda()
    {
        Func<int> read = () => LambdaTarget.Value();
        return read();
    }

    public static async Task UsesAsync()
    {
        await Task.Yield();
        AsyncTarget.Run();
    }

    public static void Catches()
    {
        try
        {
            GC.KeepAlive(null);
        }
        catch (CatchTarget)
        {
        }
    }

    public static string Names() => nameof(NamedOnlyTarget) + "UnrelatedTarget";

    // The compiler replaces an enum member with its number, so this leaves no reference to ConstantTarget.
    public static bool ComparesConstant(int value) => value == (int)ConstantTarget.Second;

    public sealed class Nested
    {
        public void Inner() => NestedCallTarget.Ping();
    }
}
