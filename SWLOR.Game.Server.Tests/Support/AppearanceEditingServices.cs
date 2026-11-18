using SWLOR.Game.Server.Feature.AppearanceEditing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// The appearance editor's content singletons built by hand with null services, for tests that run their code without a
/// container (the content factory refuses to build a singleton).
/// </summary>
public static class AppearanceEditingServices
{
    public static T Create<T>() where T : class => (T)Create(typeof(T));

    private static object Create(Type type)
    {
        var constructor = type.GetConstructors().OrderByDescending(candidate => candidate.GetParameters().Length).First();
        var arguments = constructor.GetParameters()
            .Select(parameter => parameter.ParameterType.IsClass ? Create(parameter.ParameterType) : null)
            .ToArray();
        return constructor.Invoke(arguments);
    }
}
