using System.Reflection;
using System.Reflection.Emit;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// An engine that answers from constants and records nothing, for tests that count the bytes a read allocates:
/// <see cref="FakeNWScript"/> builds an argument array per call, which would hide what the code under test does. Each
/// member of <see cref="INWScript"/> is generated, returning the constant named here or its default.
/// </summary>
internal static class StatReadEngine
{
    /// <summary>Makes an engine whose members answer as <paramref name="answers"/> says and with their default otherwise.</summary>
    public static INWScript Create(IReadOnlyDictionary<string, object> answers)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("StatReadEngine"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("StatReadEngine");
        var type = module.DefineType("StatReadEngineImpl", TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed, typeof(object), new[] { typeof(INWScript) });
        type.DefineDefaultConstructor(MethodAttributes.Public);

        foreach (var method in typeof(INWScript).GetMethods())
            Implement(type, method, answers);

        return (INWScript)Activator.CreateInstance(type.CreateType())!;
    }

    private static void Implement(TypeBuilder type, MethodInfo method, IReadOnlyDictionary<string, object> answers)
    {
        var parameters = method.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        var implementation = type.DefineMethod(
            method.Name,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
            method.ReturnType,
            parameters);
        if (method.IsGenericMethodDefinition)
            implementation.DefineGenericParameters(method.GetGenericArguments().Select(argument => argument.Name).ToArray());

        var il = implementation.GetILGenerator();
        var returnType = method.ReturnType;
        if (returnType != typeof(void))
        {
            if (answers.TryGetValue(method.Name, out var answer))
                EmitAnswer(il, returnType, answer);
            else
                EmitDefault(il, returnType);
        }

        il.Emit(OpCodes.Ret);
        type.DefineMethodOverride(implementation, method);
    }

    private static void EmitAnswer(ILGenerator il, Type returnType, object answer)
    {
        switch (answer)
        {
            case float number:
                il.Emit(OpCodes.Ldc_R4, number);
                break;
            case bool flag:
                il.Emit(flag ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                break;
            default:
                il.Emit(OpCodes.Ldc_I4, Convert.ToInt32(answer));
                break;
        }
    }

    private static void EmitDefault(ILGenerator il, Type returnType)
    {
        if (returnType.IsValueType)
        {
            var local = il.DeclareLocal(returnType);
            il.Emit(OpCodes.Ldloca, local);
            il.Emit(OpCodes.Initobj, returnType);
            il.Emit(OpCodes.Ldloc, local);
        }
        else
        {
            il.Emit(OpCodes.Ldnull);
        }
    }

    /// <summary>The answers of an ordinary valid creature that is not a player.</summary>
    public static Dictionary<string, object> NpcAnswers() => new()
    {
        ["GetIsObjectValid"] = true,
        ["GetObjectType"] = ObjectType.Creature,
        ["GetLocalInt"] = 40,
        ["GetAbilityScore"] = 12,
        ["GetItemInSlot"] = unchecked((int)0x7F000000),
    };
}
