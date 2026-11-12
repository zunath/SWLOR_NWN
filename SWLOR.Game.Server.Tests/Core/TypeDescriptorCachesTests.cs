using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// System.ComponentModel's <see cref="TypeDescriptor"/> keeps every type it is asked about in process-wide tables, so a
/// collectible assembly whose types it saw is never unloaded. Newtonsoft.Json asks it about every type it builds a contract
/// for, so a retired game build stays loaded unless <see cref="TypeDescriptorCaches"/> lets go of its types. A type of a
/// collectible dynamic assembly stands in for a game build's types.
/// </summary>
[TestFixture]
public class TypeDescriptorCachesTests
{
    [Test]
    public void A_collectible_type_the_type_descriptor_described_is_collected_once_its_types_are_released()
    {
        var type = DescribeCollectibleType();

        TypeDescriptorCaches.ReleaseCollectibleTypes().Should().BeGreaterThan(0, "the descriptor kept the collectible type");

        Collected(type).Should().BeTrue("nothing outside the collectible assembly refers to it any more");
    }

    [Test]
    public void A_collectible_type_the_type_descriptor_described_stays_loaded_until_its_types_are_released()
    {
        var type = DescribeCollectibleType();

        try
        {
            Collected(type).Should().BeFalse(
                "TypeDescriptor's static tables keep the type; if this fails the framework no longer leaks and the release can go");
        }
        finally
        {
            TypeDescriptorCaches.ReleaseCollectibleTypes();
        }
    }

    [Test]
    public void Types_of_the_process_keep_their_converters_after_a_release()
    {
        TypeDescriptor.GetConverter(typeof(Guid)).Should().BeOfType<GuidConverter>();

        TypeDescriptorCaches.ReleaseCollectibleTypes();

        TypeDescriptor.GetConverter(typeof(Guid)).Should().BeOfType<GuidConverter>("the caches only fill again");
        TypeDescriptor.GetConverter(typeof(DayOfWeek)).Should().BeOfType<EnumConverter>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference DescribeCollectibleType()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("TypeDescriptorCachesTests" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.RunAndCollect);
        var type = assembly.DefineDynamicModule("Module")
            .DefineType("Described", TypeAttributes.Public | TypeAttributes.Class)
            .CreateType();

        type.IsCollectible.Should().BeTrue();
        // What Newtonsoft.Json asks of every type it builds a contract for (JsonTypeReflector.CanTypeDescriptorConvertString).
        TypeDescriptor.GetConverter(type).Should().NotBeNull();
        return new WeakReference(type);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Collected(WeakReference type)
    {
        for (var attempt = 0; type.IsAlive && attempt < 10; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        return !type.IsAlive;
    }
}
