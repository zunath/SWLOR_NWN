using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace SWLOR.Architecture.Tests.Scanning;

/// <summary>
/// Reads the native hook entry points, the sink implementers and the sink interfaces of a compiled assembly (see
/// <see cref="NativeHookFacts"/>). The assembly is never loaded or run.
/// </summary>
internal static class NativeHookScanner
{
    private const string HookAttribute = "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute";
    private const string SinkSuffix = "Sink";

    /// <param name="assemblyPath">The assembly to read.</param>
    /// <param name="sinkNamespace">The namespace whose interfaces named <c>I...Sink</c> are the sink interfaces.</param>
    public static NativeHookFacts Scan(string assemblyPath, string sinkNamespace)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();

        var hooks = new List<NativeHook>();
        var implementers = new List<SinkImplementer>();
        var interfaces = new List<TypeKey>();

        foreach (var handle in metadata.TypeDefinitions)
        {
            var definition = metadata.GetTypeDefinition(handle);
            var @namespace = metadata.GetString(definition.Namespace);
            var name = metadata.GetString(definition.Name);

            if (definition.GetDeclaringType().IsNil && @namespace == sinkNamespace
                && (definition.Attributes & TypeAttributes.Interface) != 0 && IsSinkName(name))
            {
                interfaces.Add(new TypeKey(@namespace, name));
            }

            var owner = TopLevelKey(metadata, handle);
            foreach (var implementation in definition.GetInterfaceImplementations())
            {
                var (interfaceNamespace, interfaceName) = NameOf(metadata, metadata.GetInterfaceImplementation(implementation).Interface);
                if (interfaceNamespace == sinkNamespace && IsSinkName(interfaceName))
                    implementers.Add(new SinkImplementer(owner, interfaceName));
            }

            foreach (var methodHandle in definition.GetMethods())
            {
                var method = metadata.GetMethodDefinition(methodHandle);
                if (!HasAttribute(metadata, method.GetCustomAttributes(), HookAttribute))
                    continue;

                var operations = new List<HookOperation>();
                if (method.RelativeVirtualAddress != 0)
                {
                    foreach (var operand in IlOperandReader.ReadEntityOperands(pe.GetMethodBody(method.RelativeVirtualAddress)))
                    {
                        var operation = Describe(metadata, operand, sinkNamespace);
                        if (operation != null)
                            operations.Add(operation);
                    }
                }

                hooks.Add(new NativeHook(owner, metadata.GetString(method.Name), operations));
            }
        }

        return new NativeHookFacts(hooks, implementers, interfaces);
    }

    private static bool IsSinkName(string name) =>
        name.Length > 1 + SinkSuffix.Length && name[0] == 'I' && name.EndsWith(SinkSuffix, StringComparison.Ordinal);

    /// <summary>A call the hook body makes, or null for an operand that is not one (a field, a type).</summary>
    private static HookOperation? Describe(MetadataReader metadata, EntityHandle handle, string sinkNamespace)
    {
        switch (handle.Kind)
        {
            case HandleKind.MethodDefinition:
            {
                var method = metadata.GetMethodDefinition((MethodDefinitionHandle)handle);
                var declaring = metadata.GetTypeDefinition(method.GetDeclaringType());
                var @namespace = metadata.GetString(declaring.Namespace);
                var name = metadata.GetString(declaring.Name);
                return new HookOperation($"{Join(@namespace, name)}::{metadata.GetString(method.Name)}",
                    @namespace == sinkNamespace && IsSinkName(name) && (declaring.Attributes & TypeAttributes.Interface) != 0);
            }
            case HandleKind.MemberReference:
            {
                var reference = metadata.GetMemberReference((MemberReferenceHandle)handle);
                if (reference.GetKind() == MemberReferenceKind.Field)
                    return null;

                var (@namespace, name) = NameOf(metadata, reference.Parent);
                return new HookOperation($"{Join(@namespace, name)}::{metadata.GetString(reference.Name)}",
                    @namespace == sinkNamespace && IsSinkName(name));
            }
            case HandleKind.MethodSpecification:
                return new HookOperation("a generic method call", false);
            case HandleKind.StandaloneSignature:
                return new HookOperation("a call through a function pointer", false);
            default:
                return null;
        }
    }

    private static (string Namespace, string Name) NameOf(MetadataReader metadata, EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
            {
                var definition = metadata.GetTypeDefinition((TypeDefinitionHandle)handle);
                return (metadata.GetString(definition.Namespace), metadata.GetString(definition.Name));
            }
            case HandleKind.TypeReference:
            {
                var reference = metadata.GetTypeReference((TypeReferenceHandle)handle);
                return (metadata.GetString(reference.Namespace), metadata.GetString(reference.Name));
            }
            default:
                // A generic instantiation or a module-level member: never a sink interface.
                return (string.Empty, "(" + handle.Kind + ")");
        }
    }

    private static string Join(string @namespace, string name) => @namespace.Length == 0 ? name : @namespace + "." + name;

    private static TypeKey TopLevelKey(MetadataReader metadata, TypeDefinitionHandle handle)
    {
        var current = handle;
        while (true)
        {
            var declaring = metadata.GetTypeDefinition(current).GetDeclaringType();
            if (declaring.IsNil)
                break;
            current = declaring;
        }

        var definition = metadata.GetTypeDefinition(current);
        var name = metadata.GetString(definition.Name);
        var tick = name.IndexOf('`');
        return new TypeKey(metadata.GetString(definition.Namespace), tick < 0 ? name : name.Substring(0, tick));
    }

    private static bool HasAttribute(MetadataReader metadata, CustomAttributeHandleCollection attributes, string fullName)
    {
        foreach (var attributeHandle in attributes)
        {
            var constructor = metadata.GetCustomAttribute(attributeHandle).Constructor;
            if (constructor.Kind != HandleKind.MemberReference)
                continue;

            var (@namespace, name) = NameOf(metadata, metadata.GetMemberReference((MemberReferenceHandle)constructor).Parent);
            if (Join(@namespace, name) == fullName)
                return true;
        }

        return false;
    }

    /// <summary>The simple names of the assemblies <paramref name="assemblyPath"/> references.</summary>
    public static IReadOnlyList<string> ReadAssemblyReferences(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        return metadata.AssemblyReferences
            .Select(handle => metadata.GetString(metadata.GetAssemblyReference(handle).Name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
    }
}
