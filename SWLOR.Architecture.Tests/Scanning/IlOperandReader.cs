using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>Walks a method body instruction by instruction and returns the metadata tokens its instructions point at.</summary>
internal static class IlOperandReader
{
    private const ushort TwoByteOpCodePrefix = 0xFE;

    /// <summary>
    /// The method, field, type, token and signature operands of every instruction of <paramref name="body"/>, in
    /// order: what a call, <c>newobj</c>, field access, cast, <c>typeof</c>, <c>ldftn</c> or <c>calli</c> refers to.
    /// </summary>
    public static IReadOnlyList<EntityHandle> ReadEntityOperands(MethodBodyBlock body)
    {
        var handles = new List<EntityHandle>();
        var il = body.GetILReader();
        while (il.RemainingBytes > 0)
        {
            var code = (ushort)il.ReadByte();
            if (code == TwoByteOpCodePrefix)
                code = (ushort)(TwoByteOpCodePrefix << 8 | il.ReadByte());

            switch (OpCodeTable.OperandOf(code))
            {
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    il.Offset += 1;
                    break;
                case OperandType.InlineVar:
                    il.Offset += 2;
                    break;
                case OperandType.InlineI:
                case OperandType.InlineBrTarget:
                case OperandType.ShortInlineR:
                case OperandType.InlineString:
                    il.Offset += 4;
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    il.Offset += 8;
                    break;
                case OperandType.InlineSwitch:
                    var targets = il.ReadInt32();
                    il.Offset += 4 * targets;
                    break;
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineType:
                case OperandType.InlineTok:
                case OperandType.InlineSig:
                    handles.Add(MetadataTokens.EntityHandle(il.ReadInt32()));
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported CIL operand type for opcode 0x{code:X4}.");
            }
        }

        return handles;
    }

    /// <summary>The static fields <paramref name="body"/> loads with the <c>volatile.</c> prefix (<c>volatile. ldsfld</c>).</summary>
    public static IReadOnlyList<EntityHandle> ReadVolatileStaticFieldLoads(MethodBodyBlock body)
    {
        const ushort VolatilePrefix = 0xFE13;
        const ushort LoadStaticField = 0x7E;

        var handles = new List<EntityHandle>();
        var il = body.GetILReader();
        var afterVolatile = false;
        while (il.RemainingBytes > 0)
        {
            var code = (ushort)il.ReadByte();
            if (code == TwoByteOpCodePrefix)
                code = (ushort)(TwoByteOpCodePrefix << 8 | il.ReadByte());

            var operand = OpCodeTable.OperandOf(code);
            if (code == LoadStaticField && afterVolatile)
            {
                handles.Add(MetadataTokens.EntityHandle(il.ReadInt32()));
                afterVolatile = false;
                continue;
            }

            afterVolatile = code == VolatilePrefix;
            SkipOperand(ref il, operand, code);
        }

        return handles;
    }

    private static void SkipOperand(ref BlobReader il, OperandType operand, ushort code)
    {
        switch (operand)
        {
            case OperandType.InlineNone:
                break;
            case OperandType.ShortInlineBrTarget:
            case OperandType.ShortInlineI:
            case OperandType.ShortInlineVar:
                il.Offset += 1;
                break;
            case OperandType.InlineVar:
                il.Offset += 2;
                break;
            case OperandType.InlineI:
            case OperandType.InlineBrTarget:
            case OperandType.ShortInlineR:
            case OperandType.InlineString:
            case OperandType.InlineField:
            case OperandType.InlineMethod:
            case OperandType.InlineType:
            case OperandType.InlineTok:
            case OperandType.InlineSig:
                il.Offset += 4;
                break;
            case OperandType.InlineI8:
            case OperandType.InlineR:
                il.Offset += 8;
                break;
            case OperandType.InlineSwitch:
                var targets = il.ReadInt32();
                il.Offset += 4 * targets;
                break;
            default:
                throw new InvalidOperationException($"Unsupported CIL operand type for opcode 0x{code:X4}.");
        }
    }
}
