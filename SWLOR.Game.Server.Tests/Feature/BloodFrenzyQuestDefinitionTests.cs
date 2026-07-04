using FluentAssertions;
using NUnit.Framework;
using System.Reflection;
using System.Reflection.Emit;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Tests.Feature;

public class BloodFrenzyQuestDefinitionTests
{
    [Test]
    public void Quests_RequireLevel50Vibroblade()
    {
        var quests = new BloodFrenzyQuestDefinition().BuildQuests();

        foreach (var quest in quests.Values)
        {
            var skillRequirement = quest.Prerequisites
                .OfType<RequiredSkillRankPrerequisite>()
                .Should()
                .ContainSingle()
                .Which;

            skillRequirement.SkillType.Should().Be(SkillType.Vibroblade);
            skillRequirement.RequiredRank.Should().Be(50);
        }
    }

    [Test]
    public void FollowUpQuests_ChainFromPreviousBloodFrenzyQuest()
    {
        var quests = new BloodFrenzyQuestDefinition().BuildQuests();

        AssertQuestPrerequisite(quests, "blood_frenzy_beat", "blood_frenzy_blade");
        AssertQuestPrerequisite(quests, "blood_frenzy_glass", "blood_frenzy_beat");
        AssertQuestPrerequisite(quests, "blood_frenzy_restraint", "blood_frenzy_glass");
        AssertQuestPrerequisite(quests, BloodFrenzyQuestDefinition.FinalQuestId, "blood_frenzy_restraint");
    }

    [Test]
    public void OpeningQuest_GrantsViscaraSewersDepthsKeyOnAccept()
    {
        var quests = new BloodFrenzyQuestDefinition().BuildQuests();
        var openingQuest = quests["blood_frenzy_blade"];
        var giveKeyItem = GetKeyItemMethod(nameof(KeyItem.GiveKeyItem));
        var removeKeyItem = GetKeyItemMethod(nameof(KeyItem.RemoveKeyItem));
        var sewersDepthsKey = (int)KeyItemType.ViscaraSewersDepthsKey;

        openingQuest.OnAcceptActions.Should()
            .ContainSingle(action => DelegateCallsMethodWithIntArgument(action, giveKeyItem, sewersDepthsKey));
        openingQuest.OnAbandonActions.Should()
            .ContainSingle(action => DelegateCallsMethodWithIntArgument(action, removeKeyItem, sewersDepthsKey));
    }

    [Test]
    public void BloodFrenzyProofs_AreGrantedFromQuestCreditInsteadOfCollectObjectives()
    {
        var quests = new BloodFrenzyQuestDefinition().BuildQuests();
        var proofQuests = new[]
        {
            ("blood_frenzy_blade", NPCGroupType.Viscara_RedVeinScavenger, KeyItemType.BloodFrenzyRedVeinCodex),
            ("blood_frenzy_beat", NPCGroupType.Viscara_PulseFrameTrainingDroid, KeyItemType.BloodFrenzyPulseMetronome),
            ("blood_frenzy_glass", NPCGroupType.Viscara_BloodFrenzyButcher, KeyItemType.BloodFrenzyAdrenalGlass),
            ("blood_frenzy_restraint", NPCGroupType.Viscara_BloodFrenzyDuelist, KeyItemType.BloodFrenzyCharmFragments),
        };

        foreach (var (questId, npcGroupType, keyItemType) in proofQuests)
        {
            var quest = quests[questId];
            var state = quest.States[1];

            state.GetObjectives()
                .OfType<CollectItemObjective>()
                .Should()
                .BeEmpty();

            state.GetObjectives()
                .OfType<KillTargetObjective>()
                .Should()
                .ContainSingle(objective => objective.Group == npcGroupType);

            state.KeyItemsGrantedOnAdvance.Should().ContainSingle().Which.Should().Be(keyItemType);
            quest.KeyItemsRemovedOnAbandon.Should().Contain(keyItemType);
            quest.KeyItemsRemovedOnComplete.Should().Contain(keyItemType);
        }

        quests[BloodFrenzyQuestDefinition.FinalQuestId]
            .States[1]
            .GetObjectives()
            .OfType<CollectItemObjective>()
            .Should()
            .BeEmpty();
    }

    [Test]
    public void ViscaraSewersDepthsAccessLock_UsesStandardTeleportKeyItemGate()
    {
        ((int)KeyItemType.ViscaraSewersDepthsKey).Should().Be(81);

        var root = FindRepositoryRoot();
        using var area = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "git",
            "veles_sewers.git.json")));

        var json = EnumerateObjects(area.RootElement)
            .Single(element =>
                GetString(element, "OnUsed") == "teleport" &&
                GetOptionalLocalString(element, "DESTINATION") == "VISC_SEWER_DEPTHS_INSIDE");

        json.GetProperty("OnUsed").GetProperty("value").GetString().Should().Be("teleport");
        json.GetProperty("OnHeartbeat").GetProperty("value").GetString().Should().BeEmpty();
        json.GetProperty("Tag").GetProperty("value").GetString().Should().Be("tele_obj");
        json.GetProperty("TemplateResRef").GetProperty("value").GetString().Should().Be("tele_obj");
        json.GetProperty("TemplateResRef").GetProperty("value").GetString()!.Length.Should().BeLessThanOrEqualTo(16);
        json.GetProperty("LocName").GetProperty("value").GetProperty("0").GetString().Should().Be("Enter Sewers Depths");
        GetLocalString(json, "DESTINATION").Should().Be("VISC_SEWER_DEPTHS_INSIDE");
        GetLocalInt(json, "KEY_ITEM_ID").Should().Be((int)KeyItemType.ViscaraSewersDepthsKey);
        GetLocalInt(json, "TELEPORT_PARTY_MEMBERS").Should().Be(1);
        GetLocalString(json, "MISSING_KEY_ITEM_MESSAGE").Should().Contain("Sera Vonn's key");
        GetLocalString(json, "MISSING_KEY_ITEM_MESSAGE").Should().Contain("Viscara Sewers Depths");

        json.GetProperty("VarTable")
            .GetProperty("value")
            .EnumerateArray()
            .Select(entry => entry.GetProperty("Name").GetProperty("value").GetString())
            .Should()
            .NotContain("CONVERSATION")
            .And
            .NotContain("REQUIRED_KEY_ITEM_ID_1");

        using var palette = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "itp",
            "placeablepalcus.itp.json")));

        EnumerateResrefs(palette.RootElement).Should().Contain("tele_obj");
        EnumerateResrefs(palette.RootElement).Should().NotContain("bf_depth_access");
        File.Exists(Path.Combine(root.FullName, "Module", "utp", "bf_depth_access.utp.json")).Should().BeFalse();

        using var waypointPalette = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "itp",
            "waypointpalcus.itp.json")));

        EnumerateResrefs(waypointPalette.RootElement).Should().NotContain("bf_depth_entry");
    }

    [Test]
    public void ViscaraSewersDepthsKey_IsPlayerFacingKey()
    {
        var attribute = typeof(KeyItemType)
            .GetMember(nameof(KeyItemType.ViscaraSewersDepthsKey))
            .Single()
            .GetCustomAttribute<KeyItemAttribute>()!;

        attribute.Category.Should().Be(KeyItemCategoryType.Keys);
        attribute.Name.Should().Be("Viscara Sewers Depths Key");
        attribute.Description.Should().Contain("key");
        attribute.Description.Should().Contain("Viscara Sewers Depths");
    }

    [Test]
    public void BloodFrenzyProofKeyItems_ArePlayerFacingQuestItems()
    {
        var proofKeyItems = new[]
        {
            KeyItemType.BloodFrenzyRedVeinCodex,
            KeyItemType.BloodFrenzyPulseMetronome,
            KeyItemType.BloodFrenzyAdrenalGlass,
            KeyItemType.BloodFrenzyCharmFragments,
        };

        foreach (var keyItemType in proofKeyItems)
        {
            var attribute = typeof(KeyItemType)
                .GetMember(keyItemType.ToString())
                .Single()
                .GetCustomAttribute<KeyItemAttribute>()!;

            attribute.Category.Should().Be(KeyItemCategoryType.QuestItems);
            attribute.IsActive.Should().BeTrue();
            attribute.Description.Should().Contain("Viscara Sewers Depths");
        }
    }

    [Test]
    public void SeraVonnDialogue_DoesNotRequestBloodFrenzyQuestItems()
    {
        var root = FindRepositoryRoot();
        var dialogue = File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "dlg",
            "sera_vonn.dlg.json"));

        dialogue.Should().NotContain("action-request-quest-items");
        dialogue.Should().Contain("action-advance-quest");
    }

    [Test]
    public void SeraVonnDialogue_OffersLoreAndTacticalBranchesForEachLesson()
    {
        var root = FindRepositoryRoot();
        using var dialogue = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "dlg",
            "sera_vonn.dlg.json")));

        var entries = dialogue.RootElement
            .GetProperty("EntryList")
            .GetProperty("value")
            .EnumerateArray()
            .ToArray();
        var replies = dialogue.RootElement
            .GetProperty("ReplyList")
            .GetProperty("value")
            .EnumerateArray()
            .ToArray();
        var replyTexts = replies.Select(GetDialogueText).ToArray();
        var entryTexts = entries.Select(GetDialogueText).ToArray();

        AssertEntryOffersReplies(entries, replies, "Blood Frenzy starts below Veles", new[]
        {
            "I'm listening. Give me the first cut.",
            "What is Blood Frenzy, really?",
            "Why is the Red Vein Codex in the Sewers Depths?",
            "How do I reach the Sewers Depths?",
            "Not now.",
        });
        AssertEntryOffersReplies(entries, replies, "Pulse-Frame Training Droids are hammering my old cadence", new[]
        {
            "I will break their rhythm.",
            "Why the thirteen beats?",
            "What is the pulse metronome?",
            "Not now.",
        });
        AssertEntryOffersReplies(entries, replies, "The next lesson stinks of stim smoke", new[]
        {
            "The Butcher falls.",
            "What did the Butcher do to the lesson?",
            "What is adrenal glass?",
            "Not now.",
        });
        AssertEntryOffersReplies(entries, replies, "Now we make restraint you can hold", new[]
        {
            "I will take the fragments.",
            "Why make a restraint charm?",
            "What are Kess's duelists doing?",
            "Not now.",
        });
        AssertEntryOffersReplies(entries, replies, "Kess Draavo calls himself the Blood Frenzy King", new[]
        {
            "I will end his circle.",
            "Who is Kess Draavo?",
            "What happens after he dies?",
            "Not now.",
        });

        replyTexts.Should().Contain("What should I keep practicing?");
        entryTexts.Should().Contain(text => text.Contains("Victory gives heat; discipline gives it a shape."));
        entryTexts.Should().Contain(text => text.Contains("It opens for the key holder"));
        entryTexts.Should().Contain(text => text.Contains("teacher would make him answer for what he made"));
        entryTexts.Should().Contain(text => text.Contains("The refusal"));
        entryTexts.Should().NotContain(text => text.Contains("Vibroblade 50"), "Sera's visible dialogue should not expose the mechanical skill requirement");
    }

    [Test]
    public void BloodFrenzyPhysicalProofItems_AreNotPaletteBlueprints()
    {
        var root = FindRepositoryRoot();
        var physicalProofResrefs = new[]
        {
            "redvein_codex",
            "pulse_metron",
            "adren_glass",
            "bf_charm_frag",
        };

        using var itemPalette = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            "itp",
            "itempalcus.itp.json")));

        var paletteResrefs = EnumerateResrefs(itemPalette.RootElement).ToArray();
        foreach (var resref in physicalProofResrefs)
        {
            File.Exists(Path.Combine(root.FullName, "Module", "uti", $"{resref}.uti.json"))
                .Should()
                .BeFalse();
            paletteResrefs.Should().NotContain(resref);
        }
    }

    [Test]
    public void PlayerFacingBloodFrenzyContent_UsesBloodFrenzyTerminology()
    {
        var quests = new BloodFrenzyQuestDefinition().BuildQuests();
        var questText = quests.Values
            .Select(quest => quest.Name)
            .Concat(quests.Values.SelectMany(quest => quest.States.Values.Select(state => state.JournalText)));

        questText.Should().Contain(text => text.Contains("Blood Frenzy"));

        var root = FindRepositoryRoot();
        var contentFiles = new[]
        {
            Path.Combine(root.FullName, "Module", "dlg", "sera_vonn.dlg.json"),
            Path.Combine(root.FullName, "Module", "utc", "bf_butcher.utc.json"),
            Path.Combine(root.FullName, "Module", "utc", "bf_duelist.utc.json"),
            Path.Combine(root.FullName, "Module", "utc", "bf_kess.utc.json"),
            Path.Combine(root.FullName, "Module", "utc", "bf_pulsedroid.utc.json"),
            Path.Combine(root.FullName, "Module", "utc", "bf_scavenger.utc.json"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Readmes", "CapstoneQuestLinePlan.md"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "KeyItemService", "KeyItemType.cs"),
            Path.Combine(root.FullName, "SWLOR.Game.Server", "Service", "NPCService", "NPCGroupType.cs"),
        };

        foreach (var file in contentFiles)
        {
            File.ReadAllText(file)
                .Should()
                .Contain("Blood Frenzy", $"{Path.GetRelativePath(root.FullName, file)} should use player-facing Blood Frenzy terminology");
        }
    }

    private static bool DelegateCallsMethodWithIntArgument(
        Delegate action,
        MethodInfo expectedMethod,
        int expectedIntArgument)
    {
        var body = action.Method.GetMethodBody();
        var il = body?.GetILAsByteArray();
        if (il == null)
            return false;

        var module = action.Method.Module;
        var index = 0;
        int? lastInteger = null;
        while (index < il.Length)
        {
            var opCode = ReadOpCode(il, ref index);
            var operandSize = GetOperandSize(opCode.OperandType, il, index);
            lastInteger = ReadIntegerOperand(opCode, il, index) ?? lastInteger;

            if (opCode.OperandType == OperandType.InlineMethod)
            {
                var token = BitConverter.ToInt32(il, index);
                var calledMethod = module.ResolveMethod(token);
                if (calledMethod.Module == expectedMethod.Module &&
                    calledMethod.MetadataToken == expectedMethod.MetadataToken &&
                    lastInteger == expectedIntArgument)
                {
                    return true;
                }
            }

            index += operandSize;
        }

        return false;
    }

    private static int? ReadIntegerOperand(OpCode opCode, byte[] il, int index)
    {
        if (opCode == OpCodes.Ldc_I4_M1) return -1;
        if (opCode == OpCodes.Ldc_I4_0) return 0;
        if (opCode == OpCodes.Ldc_I4_1) return 1;
        if (opCode == OpCodes.Ldc_I4_2) return 2;
        if (opCode == OpCodes.Ldc_I4_3) return 3;
        if (opCode == OpCodes.Ldc_I4_4) return 4;
        if (opCode == OpCodes.Ldc_I4_5) return 5;
        if (opCode == OpCodes.Ldc_I4_6) return 6;
        if (opCode == OpCodes.Ldc_I4_7) return 7;
        if (opCode == OpCodes.Ldc_I4_8) return 8;
        if (opCode == OpCodes.Ldc_I4_S) return (sbyte)il[index];
        if (opCode == OpCodes.Ldc_I4) return BitConverter.ToInt32(il, index);

        return null;
    }

    private static MethodInfo GetKeyItemMethod(string methodName)
    {
        return typeof(KeyItem)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method =>
                method.Name == methodName &&
                method.GetParameters()
                    .Select(parameter => parameter.ParameterType)
                    .SequenceEqual(new[] { typeof(uint), typeof(KeyItemType) }));
    }

    private static OpCode ReadOpCode(byte[] il, ref int index)
    {
        var value = il[index++];
        if (value != 0xFE)
            return SingleByteOpCodes[(short)value];

        return MultiByteOpCodes[unchecked((short)(0xFE00 | il[index++]))];
    }

    private static int GetOperandSize(OperandType operandType, byte[] il, int index)
    {
        return operandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineI or OperandType.InlineMethod or
                OperandType.InlineSig or OperandType.InlineString or OperandType.InlineTok or OperandType.InlineType or
                OperandType.ShortInlineR => 4,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch => 4 + BitConverter.ToInt32(il, index) * 4,
            _ => throw new NotSupportedException($"Unsupported IL operand type '{operandType}'.")
        };
    }

    private static readonly IReadOnlyDictionary<short, OpCode> SingleByteOpCodes = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (OpCode)field.GetValue(null)!)
        .Where(opCode => opCode.Size == 1)
        .ToDictionary(opCode => opCode.Value);

    private static readonly IReadOnlyDictionary<short, OpCode> MultiByteOpCodes = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (OpCode)field.GetValue(null)!)
        .Where(opCode => opCode.Size == 2)
        .ToDictionary(opCode => opCode.Value);

    private static void AssertQuestPrerequisite(
        Dictionary<string, QuestDetail> quests,
        string questId,
        string prerequisiteQuestId)
    {
        quests[questId].Prerequisites
            .OfType<RequiredQuestPrerequisite>()
            .Should()
            .ContainSingle()
            .Which
            .QuestId
            .Should()
            .Be(prerequisiteQuestId);
    }

    private static string GetLocalString(System.Text.Json.JsonElement json, string variableName)
    {
        return GetLocal(json, variableName).GetProperty("Value").GetProperty("value").GetString()!;
    }

    private static string GetOptionalLocalString(System.Text.Json.JsonElement json, string variableName)
    {
        if (!TryGetLocal(json, variableName, out var local))
            return string.Empty;

        return local.GetProperty("Value").GetProperty("value").GetString() ?? string.Empty;
    }

    private static int GetLocalInt(System.Text.Json.JsonElement json, string variableName)
    {
        return GetLocal(json, variableName).GetProperty("Value").GetProperty("value").GetInt32();
    }

    private static System.Text.Json.JsonElement GetLocal(System.Text.Json.JsonElement json, string variableName)
    {
        if (!TryGetLocal(json, variableName, out var local))
            throw new InvalidOperationException($"Missing local variable '{variableName}'.");

        return local;
    }

    private static bool TryGetLocal(
        System.Text.Json.JsonElement json,
        string variableName,
        out System.Text.Json.JsonElement local)
    {
        local = default;

        if (!json.TryGetProperty("VarTable", out var varTable) ||
            !varTable.TryGetProperty("value", out var variables) ||
            variables.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            return false;
        }

        foreach (var variable in variables.EnumerateArray())
        {
            if (variable.GetProperty("Name").GetProperty("value").GetString() == variableName)
            {
                local = variable;
                return true;
            }
        }

        return false;
    }

    private static string GetString(System.Text.Json.JsonElement json, string propertyName)
    {
        return json.TryGetProperty(propertyName, out var property) &&
               property.TryGetProperty("value", out var value) &&
               value.ValueKind == System.Text.Json.JsonValueKind.String
            ? value.GetString()!
            : string.Empty;
    }

    private static IEnumerable<System.Text.Json.JsonElement> EnumerateObjects(System.Text.Json.JsonElement element)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            yield return element;

            foreach (var property in element.EnumerateObject())
            {
                foreach (var nested in EnumerateObjects(property.Value))
                {
                    yield return nested;
                }
            }
        }
        else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var nested in EnumerateObjects(item))
                {
                    yield return nested;
                }
            }
        }
    }

    private static IEnumerable<string> EnumerateResrefs(System.Text.Json.JsonElement element)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var resref in EnumerateResrefs(item))
                {
                    yield return resref;
                }
            }
        }
        else if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (element.TryGetProperty("RESREF", out var resref))
            {
                yield return resref.GetProperty("value").GetString()!;
            }

            foreach (var property in element.EnumerateObject())
            {
                foreach (var nestedResref in EnumerateResrefs(property.Value))
                {
                    yield return nestedResref;
                }
            }
        }
    }

    private static void AssertEntryOffersReplies(
        System.Text.Json.JsonElement[] entries,
        System.Text.Json.JsonElement[] replies,
        string entryTextFragment,
        string[] expectedReplyTexts)
    {
        var entry = entries
            .Single(candidate => GetDialogueText(candidate).Contains(entryTextFragment));
        var actualReplyTexts = entry
            .GetProperty("RepliesList")
            .GetProperty("value")
            .EnumerateArray()
            .Select(link => link.GetProperty("Index").GetProperty("value").GetInt32())
            .Select(index => GetDialogueText(replies[index]))
            .ToArray();

        actualReplyTexts.Should().BeEquivalentTo(expectedReplyTexts);
    }

    private static string GetDialogueText(System.Text.Json.JsonElement node)
    {
        return node
            .GetProperty("Text")
            .GetProperty("value")
            .GetProperty("0")
            .GetString() ?? string.Empty;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
