using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Contracts;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Native;

/// <summary>
/// P4-11: each hook file's install moment now enables its hooks through the container's <see cref="IHookControl"/> (the
/// process's own table on the native entry, the host's on the host entry) and does nothing else, so the moment a hook is
/// installed is the same on both entries and as before.
/// </summary>
[TestFixture]
public class NativeHookHandlerTests
{
    private static readonly (Type HookFile, string Handler, NativeHookId Hook)[] ModuleLoadHandlers =
    [
        (typeof(ChatPortraitSpeakers), nameof(ChatPortraitSpeakers.RegisterHooks), NativeHookId.ChatPortraitSpeakers),
        (typeof(ClientJsonUtf8Guard), nameof(ClientJsonUtf8Guard.RegisterHook), NativeHookId.ClientJsonUtf8Guard),
        (typeof(GetDamageRoll), nameof(GetDamageRoll.RegisterHook), NativeHookId.GetDamageRoll),
        (typeof(NeckAppearanceSerialization), nameof(NeckAppearanceSerialization.RegisterHook), NativeHookId.NeckAppearanceSerialization),
        (typeof(OnAIActionAttackObject), nameof(OnAIActionAttackObject.RegisterHook), NativeHookId.OnAIActionAttackObject),
        (typeof(PlayerCreation), nameof(PlayerCreation.RegisterHook), NativeHookId.PlayerCreation),
        (typeof(PlayerFileCheckpoint), nameof(PlayerFileCheckpoint.RegisterHooks), NativeHookId.PlayerFileCheckpoint),
        (typeof(PlayerListNamePrivacy), nameof(PlayerListNamePrivacy.RegisterHook), NativeHookId.PlayerListNamePrivacy),
        (typeof(PlayerSlotAssignment), nameof(PlayerSlotAssignment.RegisterHook), NativeHookId.PlayerSlotAssignment),
        (typeof(QuickbarFeatLoading), nameof(QuickbarFeatLoading.RegisterHook), NativeHookId.QuickbarFeatLoading),
        (typeof(ResolveAttackRoll), nameof(ResolveAttackRoll.RegisterHook), NativeHookId.ResolveAttackRoll),
        (typeof(ChatPortraitSpeakers).Assembly.GetType("SWLOR.Game.Server.Native.SavedPlayerInventory")!, "RegisterHook",
            NativeHookId.SavedPlayerInventory),
        (typeof(StaleDriveAfterJump), nameof(StaleDriveAfterJump.RegisterHooks), NativeHookId.StaleDriveAfterJump),
        (typeof(VitalityHitPoints), nameof(VitalityHitPoints.RegisterHooks), NativeHookId.VitalityHitPoints),
        (typeof(WeaponAttackAnimation), nameof(WeaponAttackAnimation.RegisterHooks), NativeHookId.WeaponAttackAnimation),
    ];

    private static IEnumerable<TestCaseData> Handlers() =>
        ModuleLoadHandlers.Select(entry => new TestCaseData(entry.HookFile, entry.Handler, entry.Hook).SetArgDisplayNames(entry.HookFile.Name));

    private static (RecordingHookControl Hooks, IDisposable Installed) InstallHooks()
    {
        var hooks = new RecordingHookControl();
        var provider = ContainerHost.Create(new ServiceCollection().AddSingleton<IHookControl>(hooks));
        var installed = CompositionRoot.InstallForTests(provider);
        return (hooks, new Scope(installed, provider));
    }

    [TestCaseSource(nameof(Handlers))]
    public void A_mod_load_handler_enables_the_hooks_of_its_file_through_the_containers_hook_control(Type hookFile, string handler, NativeHookId hook)
    {
        var (hooks, installed) = InstallHooks();
        using (installed)
        {
            var method = hookFile.GetMethod(handler, BindingFlags.Public | BindingFlags.Static);
            method.Should().NotBeNull();
            method!.GetCustomAttributes<NWNEventHandler>().Select(attribute => attribute.Script)
                .Should().Equal(new[] { ScriptName.OnModuleLoad }, "the hook is enabled at the moment it was always installed");
            method.GetCustomAttribute<BootPhaseAttribute>()!.Reload.Should().Be(BootReload.NativeHook);

            method.Invoke(null, null);

            hooks.Calls.Should().Equal("enable " + hook);
        }
    }

    [Test]
    public void The_15_mod_load_handlers_and_the_two_lazy_installs_cover_every_hook_file()
    {
        ModuleLoadHandlers.Select(entry => entry.Hook)
            .Append(NativeHookId.CombatRecorderEffectHooks)
            .Append(NativeHookId.LegacyItemProperties)
            .Should().BeEquivalentTo(Enum.GetValues<NativeHookId>());
    }

    [Test]
    public void The_combat_recorder_enables_its_hooks_when_it_installs_them()
    {
        var (hooks, installed) = InstallHooks();
        using (installed)
        {
            CombatRecorderEffectHooks.Install();
            CombatRecorderEffectHooks.Install();

            hooks.Calls.Should().Equal("enable CombatRecorderEffectHooks", "enable CombatRecorderEffectHooks");
            hooks.IsEnabled(NativeHookId.CombatRecorderEffectHooks).Should().BeTrue();
        }
    }

    [Test]
    public void The_slot_rotation_is_active_once_its_hook_is_enabled_and_while_it_has_not_turned_itself_off()
    {
        var (hooks, installed) = InstallHooks();
        using (installed)
        {
            var hookDisabled = typeof(PlayerSlotAssignment).GetField("HookDisabled", BindingFlags.Static | BindingFlags.NonPublic)!;
            var disabled = hookDisabled.GetValue(null);
            try
            {
                hookDisabled.SetValue(null, false);
                PlayerSlotAssignment.IsActive.Should().BeFalse();

                hooks.Enable(NativeHookId.PlayerSlotAssignment);
                PlayerSlotAssignment.IsActive.Should().BeTrue();

                hookDisabled.SetValue(null, true);
                PlayerSlotAssignment.IsActive.Should().BeFalse();
            }
            finally
            {
                hookDisabled.SetValue(null, disabled);
            }
        }
    }

    private sealed class Scope : IDisposable
    {
        private readonly IDisposable _installed;
        private readonly ServiceProvider _provider;

        public Scope(IDisposable installed, ServiceProvider provider)
        {
            _installed = installed;
            _provider = provider;
        }

        public void Dispose()
        {
            _installed.Dispose();
            _provider.Dispose();
        }
    }
}
