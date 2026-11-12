using System.Reflection;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A combat recorder that only reports that a recording runs, so the managed entry points count their depth
/// (<c>ManagedEntryDepth</c>); every other member does nothing and returns the default.
/// </summary>
public class RecordingFlagCombatRecorder : DispatchProxy
{
    public static ICombatRecorderService Create() => Create<ICombatRecorderService, RecordingFlagCombatRecorder>();

    protected override object Invoke(MethodInfo targetMethod, object[] args)
    {
        if (targetMethod!.Name == "get_" + nameof(ICombatRecorderService.IsRecording))
            return true;

        var returnType = targetMethod.ReturnType;
        return returnType == typeof(void) || !returnType.IsValueType ? null : Activator.CreateInstance(returnType);
    }
}
