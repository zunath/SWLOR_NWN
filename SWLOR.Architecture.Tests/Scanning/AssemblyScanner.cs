using System.Collections.Immutable;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>
/// Reads a compiled assembly's IL and records every reference one top-level type makes to another top-level type
/// of the same assembly. The assembly is never loaded or run (only an enum of another assembly is loaded, to learn
/// its underlying type), and comments and strings cannot cause a reference.
/// </summary>
/// <remarks>
/// <para>A reference is any of: a method, field, property accessor or type token in a method body (calls, field
/// access, <c>newobj</c>, casts, <c>typeof</c>, boxing, <c>ldftn</c>, <c>calli</c> signatures), a catch type, a type
/// in a local variable, a type in a method or field signature (return value, parameter, field type, generic
/// arguments, arrays), a base type or interface, a generic constraint, or a custom attribute on a
/// type, member, parameter, return value or generic parameter (its type, plus the types named by <c>typeof</c>
/// arguments and the enum types of enum arguments).</para>
/// <para>Nested and compiler-generated types (closures, lambdas, async state machines, local functions) are
/// attributed to the top-level type that declares them.</para>
/// <para><b>Limit.</b> The C# compiler inlines constants (enum members, <c>const</c> fields), so a dependency made
/// only through a constant leaves no IL reference. It is still seen whenever the type also appears in a
/// signature, local variable, generic argument or call.</para>
/// </remarks>
internal static class AssemblyScanner
{
    public static TypeReferenceGraph Scan(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        return new Walker(peReader).Run();
    }

    private sealed class Walker
    {
        private const ushort TwoByteOpCodePrefix = 0xFE;

        private readonly PEReader _pe;
        private readonly MetadataReader _metadata;
        private readonly DefinedTypeCollector _signatures = new();
        private readonly AttributeArgumentCollector _attributeArguments;
        private readonly Dictionary<TypeDefinitionHandle, TypeDefinitionHandle> _topLevel = new();
        private readonly Dictionary<(TypeKey Source, TypeKey Target), HashSet<string>> _references = new();
        private readonly Dictionary<TypeDefinitionHandle, TypeKey> _keys = new();

        private TypeDefinitionHandle _source;
        private TypeKey _sourceKey;
        private string _member = string.Empty;

        public Walker(PEReader pe)
        {
            _pe = pe;
            _metadata = pe.GetMetadataReader();
            _attributeArguments = new AttributeArgumentCollector(_metadata, TypesByName());
        }

        public TypeReferenceGraph Run()
        {
            var topLevelTypes = new List<TypeKey>();

            foreach (var handle in _metadata.TypeDefinitions)
            {
                var definition = _metadata.GetTypeDefinition(handle);
                if (definition.GetDeclaringType().IsNil)
                    topLevelTypes.Add(KeyOf(handle));

                _source = TopLevelOf(handle);
                _sourceKey = KeyOf(_source);
                ScanType(handle, definition);
            }

            var references = _references.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlySet<string>)pair.Value);
            return new TypeReferenceGraph(topLevelTypes, references);
        }

        private void ScanType(TypeDefinitionHandle handle, TypeDefinition type)
        {
            _member = MemberId(handle, "(type)");
            AddEntity(type.BaseType);
            foreach (var implementation in type.GetInterfaceImplementations())
                AddEntity(_metadata.GetInterfaceImplementation(implementation).Interface);
            AddGenericParameters(type.GetGenericParameters());
            AddAttributes(type.GetCustomAttributes());

            foreach (var fieldHandle in type.GetFields())
            {
                var field = _metadata.GetFieldDefinition(fieldHandle);
                _member = MemberId(handle, _metadata.GetString(field.Name));
                AddAll(field.DecodeSignature(_signatures, null));
                AddAttributes(field.GetCustomAttributes());
            }

            foreach (var propertyHandle in type.GetProperties())
            {
                var property = _metadata.GetPropertyDefinition(propertyHandle);
                _member = MemberId(handle, _metadata.GetString(property.Name));
                AddAttributes(property.GetCustomAttributes());
            }

            foreach (var eventHandle in type.GetEvents())
            {
                var eventDefinition = _metadata.GetEventDefinition(eventHandle);
                _member = MemberId(handle, _metadata.GetString(eventDefinition.Name));
                AddAttributes(eventDefinition.GetCustomAttributes());
            }

            foreach (var methodHandle in type.GetMethods())
                ScanMethod(handle, _metadata.GetMethodDefinition(methodHandle));
        }

        private void ScanMethod(TypeDefinitionHandle owner, MethodDefinition method)
        {
            _member = MemberId(owner, _metadata.GetString(method.Name));

            AddSignature(method.DecodeSignature(_signatures, null));

            AddGenericParameters(method.GetGenericParameters());
            AddAttributes(method.GetCustomAttributes());

            // Attributes on the return value and on parameters sit on the parameter rows, not on the method.
            foreach (var parameterHandle in method.GetParameters())
                AddAttributes(_metadata.GetParameter(parameterHandle).GetCustomAttributes());

            if (method.RelativeVirtualAddress != 0)
                ScanBody(_pe.GetMethodBody(method.RelativeVirtualAddress));
        }

        private void ScanBody(MethodBodyBlock body)
        {
            if (!body.LocalSignature.IsNil)
            {
                foreach (var local in _metadata.GetStandaloneSignature(body.LocalSignature).DecodeLocalSignature(_signatures, null))
                    AddAll(local);
            }

            foreach (var region in body.ExceptionRegions)
            {
                if (!region.CatchType.IsNil)
                    AddEntity(region.CatchType);
            }

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
                        AddEntity(MetadataTokens.EntityHandle(il.ReadInt32()));
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported CIL operand type for opcode 0x{code:X4}.");
                }
            }
        }

        private void AddGenericParameters(GenericParameterHandleCollection parameters)
        {
            foreach (var parameterHandle in parameters)
            {
                var parameter = _metadata.GetGenericParameter(parameterHandle);
                foreach (var constraintHandle in parameter.GetConstraints())
                    AddEntity(_metadata.GetGenericParameterConstraint(constraintHandle).Type);
                AddAttributes(parameter.GetCustomAttributes());
            }
        }

        private void AddSignature(MethodSignature<TypeDefinitionHandle[]> signature)
        {
            AddAll(signature.ReturnType);
            foreach (var parameter in signature.ParameterTypes)
                AddAll(parameter);
        }

        private void AddAttributes(CustomAttributeHandleCollection attributes)
        {
            foreach (var attributeHandle in attributes)
            {
                var attribute = _metadata.GetCustomAttribute(attributeHandle);
                AddEntity(attribute.Constructor);

                // typeof(X) and enum values sit in the attribute blob, not behind a metadata token.
                var value = attribute.DecodeValue(_attributeArguments);
                foreach (var argument in value.FixedArguments)
                    AddAttributeArgument(argument.Type, argument.Value);
                foreach (var argument in value.NamedArguments)
                    AddAttributeArgument(argument.Type, argument.Value);
            }
        }

        private void AddAttributeArgument(AttributeArgumentType type, object? value)
        {
            AddAll(type.Defined);
            switch (value)
            {
                case AttributeArgumentType named:
                    AddAll(named.Defined);
                    break;
                case ImmutableArray<CustomAttributeTypedArgument<AttributeArgumentType>> elements:
                    foreach (var element in elements)
                        AddAttributeArgument(element.Type, element.Value);
                    break;
            }
        }

        private void AddAll(IEnumerable<TypeDefinitionHandle> targets)
        {
            foreach (var target in targets)
                AddTarget(target);
        }

        /// <summary>Records what a metadata token points at: a type, a member (its declaring type), or a signature.</summary>
        private void AddEntity(EntityHandle handle)
        {
            if (handle.IsNil)
                return;

            switch (handle.Kind)
            {
                case HandleKind.TypeDefinition:
                    AddTarget((TypeDefinitionHandle)handle);
                    break;
                case HandleKind.TypeSpecification:
                    AddAll(_signatures.GetTypeFromSpecification(_metadata, null, (TypeSpecificationHandle)handle, 0));
                    break;
                case HandleKind.MethodDefinition:
                    AddTarget(_metadata.GetMethodDefinition((MethodDefinitionHandle)handle).GetDeclaringType());
                    break;
                case HandleKind.FieldDefinition:
                    AddTarget(_metadata.GetFieldDefinition((FieldDefinitionHandle)handle).GetDeclaringType());
                    break;
                case HandleKind.MemberReference:
                    // A member of a constructed generic type (List<Perk>.Add) carries the type arguments on the parent.
                    // One of a generic type of this assembly also names its other types in the signature.
                    var reference = _metadata.GetMemberReference((MemberReferenceHandle)handle);
                    AddEntity(reference.Parent);
                    if (reference.GetKind() == MemberReferenceKind.Method)
                        AddSignature(reference.DecodeMethodSignature(_signatures, null));
                    else
                        AddAll(reference.DecodeFieldSignature(_signatures, null));
                    break;
                case HandleKind.MethodSpecification:
                    var specification = _metadata.GetMethodSpecification((MethodSpecificationHandle)handle);
                    AddEntity(specification.Method);
                    foreach (var argument in specification.DecodeSignature(_signatures, null))
                        AddAll(argument);
                    break;
                case HandleKind.StandaloneSignature:
                    var standalone = _metadata.GetStandaloneSignature((StandaloneSignatureHandle)handle);
                    if (standalone.GetKind() == StandaloneSignatureKind.Method)
                        AddSignature(standalone.DecodeMethodSignature(_signatures, null));
                    break;
                // TypeReference, ModuleReference and the rest point outside the assembly.
            }
        }

        private void AddTarget(TypeDefinitionHandle target)
        {
            var targetTop = TopLevelOf(target);
            if (targetTop == _source)
                return;

            var key = (_sourceKey, KeyOf(targetTop));
            if (!_references.TryGetValue(key, out var members))
            {
                members = new HashSet<string>(StringComparer.Ordinal);
                _references[key] = members;
            }

            members.Add(_member);
        }

        private Dictionary<string, TypeDefinitionHandle> TypesByName()
        {
            var names = new Dictionary<string, TypeDefinitionHandle>(StringComparer.Ordinal);
            foreach (var handle in _metadata.TypeDefinitions)
                names[MetadataName(handle)] = handle;
            return names;
        }

        // The name a serialized typeof(...) uses: Namespace.Outer+Nested`1.
        private string MetadataName(TypeDefinitionHandle handle)
        {
            var definition = _metadata.GetTypeDefinition(handle);
            var name = _metadata.GetString(definition.Name);
            var declaring = definition.GetDeclaringType();
            if (!declaring.IsNil)
                return MetadataName(declaring) + "+" + name;

            var @namespace = _metadata.GetString(definition.Namespace);
            return @namespace.Length == 0 ? name : @namespace + "." + name;
        }

        private TypeDefinitionHandle TopLevelOf(TypeDefinitionHandle handle)
        {
            if (_topLevel.TryGetValue(handle, out var cached))
                return cached;

            var current = handle;
            while (true)
            {
                var declaring = _metadata.GetTypeDefinition(current).GetDeclaringType();
                if (declaring.IsNil)
                    break;
                current = declaring;
            }

            _topLevel[handle] = current;
            return current;
        }

        private TypeKey KeyOf(TypeDefinitionHandle topLevel)
        {
            if (_keys.TryGetValue(topLevel, out var cached))
                return cached;

            var definition = _metadata.GetTypeDefinition(topLevel);
            var key = new TypeKey(
                _metadata.GetString(definition.Namespace),
                TrimArity(_metadata.GetString(definition.Name)));
            _keys[topLevel] = key;
            return key;
        }

        /// <summary>
        /// Names a member as the source wrote it: <c>Type+Nested::Method</c>. Closures, lambdas, local functions and
        /// state machines (<c>&lt;Method&gt;b__0_0</c>, <c>&lt;Method&gt;d__3</c>) are named after the method that owns them.
        /// </summary>
        private string MemberId(TypeDefinitionHandle owner, string memberName)
        {
            var path = new List<string>();
            string? generatedOwner = null;
            for (var current = owner; !current.IsNil; current = _metadata.GetTypeDefinition(current).GetDeclaringType())
            {
                var name = TrimArity(_metadata.GetString(_metadata.GetTypeDefinition(current).Name));
                if (IsGenerated(name))
                    generatedOwner ??= OriginalName(name);
                else
                    path.Insert(0, name);
            }

            var member = IsGenerated(memberName)
                ? OriginalName(memberName)
                : generatedOwner ?? memberName;
            if (string.IsNullOrEmpty(member))
                member = "(generated)";

            return string.Join('+', path) + "::" + member;
        }

        private static bool IsGenerated(string name) => name.StartsWith('<');

        // "<Method>b__0_0" -> "Method"; "<>c__DisplayClass1_0" -> "" (no owner recorded in the name).
        private static string OriginalName(string generatedName)
        {
            var end = generatedName.IndexOf('>');
            return end > 1 ? generatedName.Substring(1, end - 1) : string.Empty;
        }

        private static string TrimArity(string name)
        {
            var tick = name.IndexOf('`');
            return tick < 0 ? name : name.Substring(0, tick);
        }
    }
}
