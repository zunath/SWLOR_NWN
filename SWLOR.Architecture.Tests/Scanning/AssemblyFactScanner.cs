using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>
/// Reads the facts the architecture rules need from a compiled assembly's metadata and IL (see
/// <see cref="AssemblyFacts"/>). The assembly is never loaded or run.
/// </summary>
internal static class AssemblyFactScanner
{
    private const string HandlerAttribute = "SWLOR.Game.Server.Core.NWNEventHandler";
    private const string CompilerGeneratedAttribute = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";
    private const string BackingFieldSuffix = ">k__BackingField";

    public static AssemblyFacts Scan(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        return new Walker(peReader).Run();
    }

    private sealed class Walker
    {
        private readonly PEReader _pe;
        private readonly MetadataReader _metadata;
        private readonly ExternalTypeCollector _externalTypes = new();
        private readonly Dictionary<TypeDefinitionHandle, bool> _generated = new();
        private readonly Dictionary<(TypeKey Source, string External), HashSet<string>> _external = new();

        private TypeKey _source;
        private string _member = string.Empty;

        public Walker(PEReader pe)
        {
            _pe = pe;
            _metadata = pe.GetMetadataReader();
        }

        public AssemblyFacts Run()
        {
            var types = new List<TypeFact>();
            var fields = new List<StaticFieldFact>();
            var handlers = new List<HandlerFact>();

            foreach (var handle in _metadata.TypeDefinitions)
            {
                var definition = _metadata.GetTypeDefinition(handle);
                var top = TopLevelOf(handle);
                _source = KeyOf(top);

                if (definition.GetDeclaringType().IsNil)
                    types.Add(DescribeType(definition));

                var generated = IsGenerated(handle);
                ScanTypeSignatures(definition);

                foreach (var fieldHandle in definition.GetFields())
                {
                    var field = _metadata.GetFieldDefinition(fieldHandle);
                    var name = _metadata.GetString(field.Name);
                    _member = DisplayMember(handle, name);
                    AddExternal(field.DecodeSignature(_externalTypes, null));

                    if (generated || (field.Attributes & FieldAttributes.Static) == 0)
                        continue;

                    // The compiler's own fields (lambda caches, closures) are not the service's state, but the field behind
                    // a static auto-property is: report it under the property's name.
                    var isBackingField = name.StartsWith('<') && name.EndsWith(BackingFieldSuffix, StringComparison.Ordinal);
                    if (!isBackingField && (name.StartsWith('<') || HasAttribute(field.GetCustomAttributes(), CompilerGeneratedAttribute)))
                        continue;

                    fields.Add(new StaticFieldFact(
                        _source,
                        DeclaringName(handle),
                        isBackingField ? name.Substring(1, name.Length - 1 - BackingFieldSuffix.Length) : name,
                        (field.Attributes & FieldAttributes.Literal) != 0,
                        (field.Attributes & FieldAttributes.InitOnly) != 0,
                        isBackingField));
                }

                foreach (var methodHandle in definition.GetMethods())
                {
                    var method = _metadata.GetMethodDefinition(methodHandle);
                    var name = _metadata.GetString(method.Name);
                    _member = DisplayMember(handle, name);

                    if (!generated && HasAttribute(method.GetCustomAttributes(), HandlerAttribute))
                        handlers.Add(new HandlerFact(_source, name));

                    var signature = method.DecodeSignature(_externalTypes, null);
                    AddExternal(signature.ReturnType);
                    foreach (var parameter in signature.ParameterTypes)
                        AddExternal(parameter);

                    if (method.RelativeVirtualAddress != 0)
                        ScanBody(_pe.GetMethodBody(method.RelativeVirtualAddress));
                }
            }

            var external = _external
                .Select(pair => new ExternalReference(pair.Key.Source, pair.Key.External, pair.Value))
                .OrderBy(reference => reference.Source.ToString(), StringComparer.Ordinal)
                .ThenBy(reference => reference.ExternalType, StringComparer.Ordinal)
                .ToList();
            return new AssemblyFacts(types, fields, handlers, external);
        }

        private TypeFact DescribeType(TypeDefinition definition)
        {
            var attributes = definition.Attributes;
            var interfaces = new List<string>();
            foreach (var implementation in definition.GetInterfaceImplementations())
            {
                var interfaceHandle = _metadata.GetInterfaceImplementation(implementation).Interface;
                if (interfaceHandle.Kind == HandleKind.TypeDefinition)
                {
                    var interfaceType = _metadata.GetTypeDefinition((TypeDefinitionHandle)interfaceHandle);
                    interfaces.Add(FullName(interfaceType));
                }
            }

            var isInterface = (attributes & TypeAttributes.Interface) != 0;
            var isSealed = (attributes & TypeAttributes.Sealed) != 0;
            var isAbstract = (attributes & TypeAttributes.Abstract) != 0;
            return new TypeFact(
                new TypeKey(_metadata.GetString(definition.Namespace), TrimArity(_metadata.GetString(definition.Name))),
                isInterface,
                !isInterface && isSealed && isAbstract,
                !isInterface && isSealed,
                interfaces);
        }

        private void ScanTypeSignatures(TypeDefinition type)
        {
            _member = KeyOfName(type);
            AddEntity(type.BaseType);
            foreach (var implementation in type.GetInterfaceImplementations())
                AddEntity(_metadata.GetInterfaceImplementation(implementation).Interface);
        }

        private void ScanBody(MethodBodyBlock body)
        {
            if (!body.LocalSignature.IsNil)
            {
                foreach (var local in _metadata.GetStandaloneSignature(body.LocalSignature).DecodeLocalSignature(_externalTypes, null))
                    AddExternal(local);
            }

            foreach (var region in body.ExceptionRegions)
            {
                if (!region.CatchType.IsNil)
                    AddEntity(region.CatchType);
            }

            foreach (var operand in IlOperandReader.ReadEntityOperands(body))
                AddEntity(operand);
        }

        /// <summary>Records the types of other assemblies a metadata token points at: a type, or the declaring type of a member.</summary>
        private void AddEntity(EntityHandle handle)
        {
            if (handle.IsNil)
                return;

            switch (handle.Kind)
            {
                case HandleKind.TypeReference:
                    AddExternal([ExternalTypeCollector.FullName(_metadata, (TypeReferenceHandle)handle)]);
                    break;
                case HandleKind.TypeSpecification:
                    AddExternal(_externalTypes.GetTypeFromSpecification(_metadata, null, (TypeSpecificationHandle)handle, 0));
                    break;
                case HandleKind.MemberReference:
                    var reference = _metadata.GetMemberReference((MemberReferenceHandle)handle);
                    AddEntity(reference.Parent);
                    if (reference.GetKind() == MemberReferenceKind.Method)
                    {
                        var signature = reference.DecodeMethodSignature(_externalTypes, null);
                        AddExternal(signature.ReturnType);
                        foreach (var parameter in signature.ParameterTypes)
                            AddExternal(parameter);
                    }
                    else
                    {
                        AddExternal(reference.DecodeFieldSignature(_externalTypes, null));
                    }

                    break;
                case HandleKind.MethodSpecification:
                    var specification = _metadata.GetMethodSpecification((MethodSpecificationHandle)handle);
                    AddEntity(specification.Method);
                    foreach (var argument in specification.DecodeSignature(_externalTypes, null))
                        AddExternal(argument);
                    break;
                // TypeDefinition, MethodDefinition and FieldDefinition point inside the assembly.
            }
        }

        private void AddExternal(IEnumerable<string> names)
        {
            foreach (var name in names)
            {
                var key = (_source, name);
                if (!_external.TryGetValue(key, out var members))
                {
                    members = new HashSet<string>(StringComparer.Ordinal);
                    _external[key] = members;
                }

                members.Add(_member);
            }
        }

        private bool HasAttribute(CustomAttributeHandleCollection attributes, string fullName)
        {
            foreach (var attributeHandle in attributes)
            {
                var constructor = _metadata.GetCustomAttribute(attributeHandle).Constructor;
                string? owner = constructor.Kind switch
                {
                    HandleKind.MemberReference => AttributeOwner(_metadata.GetMemberReference((MemberReferenceHandle)constructor).Parent),
                    HandleKind.MethodDefinition => FullName(_metadata.GetTypeDefinition(_metadata.GetMethodDefinition((MethodDefinitionHandle)constructor).GetDeclaringType())),
                    _ => null
                };
                if (owner == fullName)
                    return true;
            }

            return false;
        }

        private string? AttributeOwner(EntityHandle parent)
        {
            return parent.Kind switch
            {
                HandleKind.TypeReference => ExternalTypeCollector.FullName(_metadata, (TypeReferenceHandle)parent),
                HandleKind.TypeDefinition => FullName(_metadata.GetTypeDefinition((TypeDefinitionHandle)parent)),
                _ => null
            };
        }

        private bool IsGenerated(TypeDefinitionHandle handle)
        {
            if (_generated.TryGetValue(handle, out var cached))
                return cached;

            var definition = _metadata.GetTypeDefinition(handle);
            var generated = HasAttribute(definition.GetCustomAttributes(), CompilerGeneratedAttribute)
                || _metadata.GetString(definition.Name).StartsWith('<')
                || (!definition.GetDeclaringType().IsNil && IsGenerated(definition.GetDeclaringType()));
            _generated[handle] = generated;
            return generated;
        }

        private TypeDefinitionHandle TopLevelOf(TypeDefinitionHandle handle)
        {
            var current = handle;
            while (true)
            {
                var declaring = _metadata.GetTypeDefinition(current).GetDeclaringType();
                if (declaring.IsNil)
                    return current;
                current = declaring;
            }
        }

        private TypeKey KeyOf(TypeDefinitionHandle topLevel)
        {
            var definition = _metadata.GetTypeDefinition(topLevel);
            return new TypeKey(_metadata.GetString(definition.Namespace), TrimArity(_metadata.GetString(definition.Name)));
        }

        private string KeyOfName(TypeDefinition type) => TrimArity(_metadata.GetString(type.Name)) + "::(type)";

        private string FullName(TypeDefinition definition)
        {
            var @namespace = _metadata.GetString(definition.Namespace);
            var name = TrimArity(_metadata.GetString(definition.Name));
            return @namespace.Length == 0 ? name : @namespace + "." + name;
        }

        /// <summary>The declaring type as the source writes it: <c>Outer+Nested</c>, without generic arity.</summary>
        private string DeclaringName(TypeDefinitionHandle handle)
        {
            var definition = _metadata.GetTypeDefinition(handle);
            var name = TrimArity(_metadata.GetString(definition.Name));
            return definition.GetDeclaringType().IsNil ? name : DeclaringName(definition.GetDeclaringType()) + "+" + name;
        }

        /// <summary>Names a member for messages: <c>Type::Method</c>; a lambda or closure method is named after the method that owns it.</summary>
        private string DisplayMember(TypeDefinitionHandle owner, string memberName)
        {
            var type = DeclaringName(owner);
            var cleaned = memberName.StartsWith('<')
                ? memberName.IndexOf('>') > 1 ? memberName.Substring(1, memberName.IndexOf('>') - 1) : "(generated)"
                : memberName;
            return type + "::" + cleaned;
        }

        private static string TrimArity(string name)
        {
            var tick = name.IndexOf('`');
            return tick < 0 ? name : name.Substring(0, tick);
        }
    }
}
