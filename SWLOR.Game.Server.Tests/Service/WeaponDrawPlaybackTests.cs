using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class WeaponDrawPlaybackTests
{
    [Test]
    public void RapidCosmeticEditsSendTheGuardFirstAndRestoreOnlyAfterTheLastEdit()
    {
        var runtime = new Runtime();
        var playback = new WeaponDrawPlayback(runtime);
        playback.Begin(1);
        runtime.Events.Should().Equal("drwright=sw_nohold", "drwleft=sw_nohold", "send", "schedule");
        playback.Begin(1);
        runtime.Callbacks[0]();
        runtime.Replacement(1, "drwright").Should().Be(WeaponDrawPlayback.EmptyDraw);
        runtime.Sends.Should().Be(1);
        runtime.Callbacks[1]();
        runtime.Replacement(1, "drwright").Should().BeEmpty();
        runtime.Replacement(1, "drwleft").Should().BeEmpty();
        runtime.Sends.Should().Be(2);
    }

    [Test]
    public void GenuineEquipmentChangesReleaseTheGuardAndInvalidateItsOldCallback()
    {
        var runtime = new Runtime();
        var playback = new WeaponDrawPlayback(runtime);
        playback.Begin(1);
        playback.Release(1);
        playback.Begin(1);
        runtime.Callbacks[0]();
        runtime.Replacement(1, "drwright").Should().Be(WeaponDrawPlayback.EmptyDraw);
        runtime.Callbacks[1]();
        runtime.Replacement(1, "drwright").Should().BeEmpty();
    }

    [Test]
    public void AnotherMappingOwnerIsPreservedBeforeAndDuringTheGuard()
    {
        var runtime = new Runtime();
        var playback = new WeaponDrawPlayback(runtime);
        runtime.Replace(1, "drwright", "custom_draw");
        playback.Begin(1);
        runtime.Callbacks.Should().BeEmpty();
        runtime.Replacement(1, "drwright").Should().Be("custom_draw");
        runtime.Replace(1, "drwright", "");
        playback.Begin(1);
        runtime.Replace(1, "drwright", "new_owner");
        runtime.Callbacks[0]();
        runtime.Replacement(1, "drwright").Should().Be("new_owner");
        runtime.Replacement(1, "drwleft").Should().BeEmpty();
    }

    [Test]
    public void MappingSendFailureRestoresTheOwnedKeysInsteadOfLeavingDrawingDisabled()
    {
        var runtime = new Runtime { FailNextSend = true };
        var playback = new WeaponDrawPlayback(runtime);
        var begin = () => playback.Begin(1);
        begin.Should().Throw<InvalidOperationException>();
        runtime.Replacement(1, "drwright").Should().BeEmpty();
        runtime.Replacement(1, "drwleft").Should().BeEmpty();
        runtime.Callbacks.Should().BeEmpty();
    }

    [Test]
    public void DisappearingCreaturesDoNotReceiveDelayedMappingWrites()
    {
        var runtime = new Runtime();
        var playback = new WeaponDrawPlayback(runtime);
        playback.Begin(1);
        runtime.Valid = false;
        playback.Prune();
        runtime.Events.Clear();
        runtime.Callbacks[0]();
        runtime.Events.Should().BeEmpty();
    }

    private sealed class Runtime : IWeaponDrawRuntime
    {
        private readonly Dictionary<(uint, string), string> _maps = new();
        public readonly List<Action> Callbacks = new();
        public readonly List<string> Events = new();
        public bool Valid = true;
        public bool FailNextSend;
        public int Sends;
        public bool IsValid(uint creature) => Valid;
        public string Replacement(uint creature, string source) => _maps.GetValueOrDefault((creature, source), "");
        public void Replace(uint creature, string source, string target)
        {
            _maps[(creature, source)] = target;
            Events.Add(source + "=" + target);
        }
        public void SendMappings(uint creature)
        {
            if (FailNextSend) { FailNextSend = false; throw new InvalidOperationException("Test send failure"); }
            Sends++;
            Events.Add("send");
        }
        public void Schedule(float seconds, Action callback)
        {
            seconds.Should().Be(.7f);
            Callbacks.Add(callback);
            Events.Add("schedule");
        }
    }
}
