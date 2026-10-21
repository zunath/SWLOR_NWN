using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class DroidEquipmentDiagnosticsTests
{
    [Test]
    public void SummarizeStack_KeepsCallingGameCodeAndDropsPlumbing()
    {
        const string stack = """
               at System.Environment.get_StackTrace()
               at SWLOR.Game.Server.Service.DroidEquipmentDiagnostics.Record(String change, UInt32 droid, UInt32 item, String slot) in C:\x\DroidEquipmentDiagnostics.cs:line 40
               at SWLOR.Game.Server.Service.DroidEquipmentDiagnostics.OnUnequipped() in C:\x\DroidEquipmentDiagnostics.cs:line 33
               at SWLOR.Game.Server.Core.ScriptDispatcher.Run(String script)
               at SWLOR.NWN.API.NWNX.CreaturePlugin.RunUnequip(UInt32 creature, UInt32 item)
               at SWLOR.Game.Server.Feature.TwinPistolEquipmentValidation.ReturnEquippedItem(UInt32 creature, UInt32 item, InventorySlot slot) in C:\x\T.cs:line 116
               at SWLOR.Game.Server.Feature.TwinPistolEquipmentValidation.ReturnEquippedItem(UInt32 creature, UInt32 item, InventorySlot slot) in C:\x\T.cs:line 117
               at SWLOR.Game.Server.Feature.TwinPistolEquipmentValidation.<>c__DisplayClass.<Revalidate>b__0()
               at NWN.Core.NWScript.AssignCommand(UInt32 obj, Action action)
            """;

        DroidEquipmentDiagnostics.SummarizeStack(stack).Should().Be(
            "Feature.TwinPistolEquipmentValidation.ReturnEquippedItem<" +
            "Feature.TwinPistolEquipmentValidation.<>c__DisplayClass.<Revalidate>b__0");
    }

    [Test]
    public void SummarizeStack_IsEmptyWhenNoGameCodeStartedTheChange()
    {
        const string stack = """
               at System.Environment.get_StackTrace()
               at SWLOR.Game.Server.Service.DroidEquipmentDiagnostics.OnUnequipped()
               at SWLOR.Game.Server.Core.ScriptDispatcher.Run(String script)
            """;

        DroidEquipmentDiagnostics.SummarizeStack(stack).Should().BeEmpty();
        DroidEquipmentDiagnostics.SummarizeStack(null).Should().BeEmpty();
    }

    [Test]
    public void SummarizeStack_KeepsAtMostEightFrames()
    {
        var lines = new List<string>();
        for (var index = 0; index < 12; index++)
            lines.Add($"   at SWLOR.Game.Server.Feature.Caller.Method{index}()");

        DroidEquipmentDiagnostics.SummarizeStack(string.Join("\n", lines)).Split('<').Should().HaveCount(8);
    }

    [Test]
    public void Format_WritesFieldsInOrder()
    {
        var fields = new List<(string, string)> { ("change", "unequipped"), ("item", "oph_rifle"), ("stack", "") };

        DroidEquipmentDiagnostics.Format(fields).Should().Be("change=unequipped item=oph_rifle stack=");
    }
}
