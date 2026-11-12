using System.Reflection;
using System.Runtime.InteropServices;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Contracts;
using SWLOR.Game.NativeHooks;
using SWLOR.Game.Server.NativeSinks;

namespace SWLOR.Game.Server.Tests.Native;

/// <summary>
/// P4-9 and P4-11: every hook file of <c>Native/</c> enables the trampolines of <c>SWLOR.Game.NativeHooks</c>, which forward
/// to one sink of <c>NativeSinks/</c> implementing the sink interface of <c>SWLOR.Game.Contracts</c>. The architecture tests
/// prove the trampoline bodies only forward or run the engine function; these prove the pairing is complete, that the
/// interfaces have the shape the host binds and that the game binds a full set.
/// </summary>
[TestFixture]
public class NativeSinkBindingTests
{
    private const string HookNamespace = "SWLOR.Game.Server.Native";
    private const string SinkNamespace = "SWLOR.Game.Server.NativeSinks";

    private const string TrampolineNamespace = "SWLOR.Game.NativeHooks";

    private static readonly Assembly Server = typeof(ChatPortraitSpeakersSink).Assembly;
    private static readonly Assembly Contracts = typeof(IChatPortraitSpeakersSink).Assembly;
    private static readonly Assembly Hooks = typeof(NativeHookTable).Assembly;

    /// <summary>Each hook file, its sink, the hooked engine functions it forwards (its entry points) and whether it calls the engine function.</summary>
    private static readonly (string HookFile, Type Sink, Type Interface, int EntryPoints, int SinkMethods, bool CallsOriginal)[] Pairs =
    [
        ("ChatPortraitSpeakers", typeof(ChatPortraitSpeakersSink), typeof(IChatPortraitSpeakersSink), 9, 1, true),
        ("VitalityHitPoints", typeof(VitalityHitPointsSink), typeof(IVitalityHitPointsSink), 7, 7, true),
        ("PlayerFileCheckpoint", typeof(PlayerFileCheckpointSink), typeof(IPlayerFileCheckpointSink), 3, 3, true),
        ("StaleDriveAfterJump", typeof(StaleDriveAfterJumpSink), typeof(IStaleDriveAfterJumpSink), 2, 2, true),
        ("WeaponAttackAnimation", typeof(WeaponAttackAnimationSink), typeof(IWeaponAttackAnimationSink), 2, 2, true),
        ("CombatRecorderEffectHooks", typeof(CombatRecorderEffectHooksSink), typeof(ICombatRecorderEffectHooksSink), 2, 2, true),
        ("LegacyItemProperties", typeof(LegacyItemPropertiesSink), typeof(ILegacyItemPropertiesSink), 1, 1, true),
        ("ClientJsonUtf8Guard", typeof(ClientJsonUtf8GuardSink), typeof(IClientJsonUtf8GuardSink), 1, 1, true),
        ("GetDamageRoll", typeof(GetDamageRollSink), typeof(IGetDamageRollSink), 1, 1, false),
        ("NeckAppearanceSerialization", typeof(NeckAppearanceSerializationSink), typeof(INeckAppearanceSerializationSink), 1, 1, true),
        ("OnAIActionAttackObject", typeof(OnAIActionAttackObjectSink), typeof(IOnAIActionAttackObjectSink), 1, 1, false),
        ("PlayerCreation", typeof(PlayerCreationSink), typeof(IPlayerCreationSink), 1, 1, true),
        ("PlayerListNamePrivacy", typeof(PlayerListNamePrivacySink), typeof(IPlayerListNamePrivacySink), 1, 1, true),
        ("PlayerSlotAssignment", typeof(PlayerSlotAssignmentSink), typeof(IPlayerSlotAssignmentSink), 1, 1, true),
        ("QuickbarFeatLoading", typeof(QuickbarFeatLoadingSink), typeof(IQuickbarFeatLoadingSink), 1, 1, true),
        ("ResolveAttackRoll", typeof(ResolveAttackRollSink), typeof(IResolveAttackRollSink), 1, 1, false),
        ("SavedPlayerInventory", typeof(SavedPlayerInventorySink), typeof(ISavedPlayerInventorySink), 1, 1, true),
    ];

    private static IEnumerable<TestCaseData> AllPairs() =>
        Pairs.Select(pair => new TestCaseData(pair.HookFile, pair.Sink, pair.Interface, pair.SinkMethods, pair.CallsOriginal).SetArgDisplayNames(pair.HookFile));

    [TestCaseSource(nameof(AllPairs))]
    public void Each_hook_file_has_a_public_sealed_sink_in_NativeSinks_that_implements_its_interface(
        string hookFile, Type sink, Type contract, int sinkMethods, bool callsOriginal)
    {
        Server.GetType($"{HookNamespace}.{hookFile}").Should().NotBeNull($"{hookFile} is a hook file of Native/");
        sink.Namespace.Should().Be(SinkNamespace);
        sink.Name.Should().Be(hookFile + "Sink");
        sink.IsPublic.Should().BeTrue();
        sink.IsSealed.Should().BeTrue();
        contract.Assembly.Should().BeSameAs(Contracts, "the sink interface is part of the contracts project");
        contract.Name.Should().Be("I" + hookFile + "Sink");
        sink.GetInterfaces().Should().Contain(contract);
        contract.GetMethods().Should().HaveCount(sinkMethods);
    }

    [TestCaseSource(nameof(AllPairs))]
    public void A_sink_method_takes_the_trampoline_last_unless_the_hook_replaces_the_engine_function(
        string hookFile, Type sink, Type contract, int sinkMethods, bool callsOriginal)
    {
        foreach (var method in contract.GetMethods())
        {
            var last = method.GetParameters().LastOrDefault();
            if (callsOriginal)
            {
                last.Should().NotBeNull($"{method.Name} of {contract.Name}");
                last!.Name.Should().Be("original");
                last.ParameterType.Should().Be(typeof(nint));
            }
            else
            {
                method.GetParameters().Should().NotContain(parameter => parameter.Name == "original",
                    $"{hookFile} replaces the engine function and never calls it");
            }
        }
    }

    [Test]
    public void The_sink_interfaces_hold_only_native_pointers_and_numbers()
    {
        var allowed = new[]
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(nint), typeof(void), typeof(float), typeof(bool),
        };

        foreach (var pair in Pairs)
        {
            foreach (var method in pair.Interface.GetMethods())
            {
                var types = method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType);
                foreach (var type in types)
                {
                    var core = type.IsPointer ? type.GetElementType()! : type;
                    allowed.Should().Contain(core, $"{pair.Interface.Name}.{method.Name} must not name a game type");
                }
            }
        }
    }

    [Test]
    public void The_contracts_project_holds_exactly_the_paired_sink_interfaces()
    {
        Contracts.GetTypes().Where(type => type.IsInterface && type.Name.EndsWith("Sink"))
            .Should().BeEquivalentTo(Pairs.Select(pair => pair.Interface));
        Pairs.Should().HaveCount(17);
    }

    [Test]
    public void The_trampolines_of_the_hook_files_declare_36_native_entry_points_and_the_game_declares_none()
    {
        var entryPoints = EntryPoints(Hooks)
            .Where(method => method.DeclaringType!.Namespace == TrampolineNamespace)
            .GroupBy(method => method.DeclaringType!.Name)
            .ToDictionary(group => group.Key, group => group.Count());

        entryPoints.Values.Sum().Should().Be(36);
        entryPoints.Keys.Should().BeEquivalentTo(Pairs.Select(pair => pair.HookFile + "Trampolines"));
        foreach (var pair in Pairs)
            entryPoints[pair.HookFile + "Trampolines"].Should().Be(pair.EntryPoints, pair.HookFile);
        EntryPoints(Server).Where(method => method.DeclaringType!.Namespace == HookNamespace)
            .Should().BeEmpty("the game build only enables the hooks and binds its sinks; NWNX calls the trampolines");
    }

    [Test]
    public void The_game_binds_a_new_set_of_its_sinks_one_per_hook_file()
    {
        var first = NativeSinkFactory.Create();
        var second = NativeSinkFactory.Create();
        var properties = typeof(NativeSinkSet).GetProperties();

        properties.Should().HaveCount(Pairs.Length);
        foreach (var pair in Pairs)
        {
            var property = typeof(NativeSinkSet).GetProperty(pair.HookFile);
            property.Should().NotBeNull(pair.HookFile);
            property!.PropertyType.Should().Be(pair.Interface);
            property.GetValue(first).Should().BeOfType(pair.Sink);
            property.GetValue(first).Should().NotBeSameAs(property.GetValue(second), "each build has sinks of its own");
        }
    }

    private static IEnumerable<MethodInfo> EntryPoints(Assembly assembly) =>
        assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttribute<UnmanagedCallersOnlyAttribute>() != null);

    [Test]
    public void Contracts_that_are_not_sinks_are_defined_for_the_host()
    {
        typeof(IScriptContext).IsInterface.Should().BeTrue();
        typeof(IHostLifecycle).IsInterface.Should().BeTrue();
        typeof(IRedisConnectionSource).IsInterface.Should().BeTrue();
        typeof(IRedisConnectionSource).Assembly.Should().BeSameAs(Contracts);
        typeof(IRedisConnectionSource).GetMethod("Open")!.ReturnType.Should().Be(typeof(StackExchange.Redis.IConnectionMultiplexer));
    }
}
