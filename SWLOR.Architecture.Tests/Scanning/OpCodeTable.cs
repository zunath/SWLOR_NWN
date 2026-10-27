using System.Reflection;
using System.Reflection.Emit;

namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>Operand kinds of the CIL opcodes, needed to walk a method body instruction by instruction.</summary>
internal static class OpCodeTable
{
    private static readonly Dictionary<ushort, OperandType> Operands = Build();

    public static OperandType OperandOf(ushort code)
    {
        if (!Operands.TryGetValue(code, out var operand))
            throw new InvalidOperationException($"Unknown CIL opcode 0x{code:X4}.");

        return operand;
    }

    private static Dictionary<ushort, OperandType> Build()
    {
        var table = new Dictionary<ushort, OperandType>();
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode opCode)
                table[unchecked((ushort)opCode.Value)] = opCode.OperandType;
        }

        return table;
    }
}
